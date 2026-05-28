using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class PdfStructuralReader
{
    public static void CheckGeneratedPdfs(string artifactsRoot)
    {
        var pdfPaths = Directory.GetFiles(artifactsRoot, "*.pdf", SearchOption.AllDirectories)
            .OrderBy(static path => path, StringComparer.Ordinal)
            .ToArray();

        Require(pdfPaths.Length > 0, "No generated PDF artifacts were found for reader-level structural checks.");

        foreach (var pdfPath in pdfPaths)
            CheckPdf(pdfPath);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Reader-level structural checks passed for {pdfPaths.Length} generated PDF artifact(s).");
        Console.ResetColor();
    }

    private static void CheckPdf(string pdfPath)
    {
        var bytes = File.ReadAllBytes(pdfPath);
        var text = Encoding.Latin1.GetString(bytes);
        var name = Path.GetFileName(pdfPath);

        Require(text.StartsWith("%PDF-", StringComparison.Ordinal), $"{name}: missing PDF header.");
        Require(text.TrimEnd().EndsWith("%%EOF", StringComparison.Ordinal), $"{name}: missing EOF marker.");

        var startXref = LastStartXref(text, name);
        Require(startXref > 0 && startXref < text.Length, $"{name}: startxref offset is outside the PDF.");
        Require(text[startXref..].StartsWith("xref", StringComparison.Ordinal), $"{name}: startxref does not point at xref.");

        var xref = ReadXref(text, startXref, name);
        Require(xref.Entries.TryGetValue(0, out var freeEntry) && !freeEntry.InUse, $"{name}: xref object 0 must be free.");
        Require(xref.TrailerSize == xref.Entries.Count, $"{name}: trailer Size does not match xref entry count.");
        Require(xref.RootObject > 0 && IsInUse(xref, xref.RootObject), $"{name}: trailer Root does not reference an in-use object.");

        foreach (var (objectNumber, entry) in xref.Entries)
        {
            if (!entry.InUse)
                continue;

            Require(entry.Offset > 0 && entry.Offset < text.Length, $"{name}: object {objectNumber} offset is outside the PDF.");
            Require(
                text[(int)entry.Offset..].StartsWith($"{objectNumber} {entry.Generation} obj", StringComparison.Ordinal),
                $"{name}: xref offset for object {objectNumber} does not point at that object.");
        }

        foreach (var reference in ReadIndirectReferences(text))
            Require(IsInUse(xref, reference.ObjectNumber), $"{name}: unresolved indirect reference {reference.ObjectNumber} {reference.Generation} R.");
    }

    private static int LastStartXref(string text, string name)
    {
        var marker = text.LastIndexOf("startxref", StringComparison.Ordinal);
        Require(marker >= 0, $"{name}: missing startxref marker.");

        var numberStart = text.IndexOf('\n', marker);
        Require(numberStart >= 0, $"{name}: malformed startxref marker.");
        numberStart++;

        var numberEnd = text.IndexOf('\n', numberStart);
        Require(numberEnd > numberStart, $"{name}: malformed startxref offset.");

        return int.Parse(text[numberStart..numberEnd].Trim(), CultureInfo.InvariantCulture);
    }

    private static XrefTable ReadXref(string text, int offset, string name)
    {
        using var reader = new StringReader(text[offset..]);
        Require(reader.ReadLine() == "xref", $"{name}: malformed xref header.");

        var entries = new SortedDictionary<int, XrefEntry>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0)
                continue;

            if (line == "trailer")
                break;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Require(parts.Length == 2, $"{name}: malformed xref subsection header.");
            var firstObject = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var count = int.Parse(parts[1], CultureInfo.InvariantCulture);

            for (var i = 0; i < count; i++)
            {
                var entryLine = reader.ReadLine();
                if (entryLine is null)
                    throw new InvalidOperationException($"{name}: truncated xref subsection.");

                Require(entryLine.Length >= 17, $"{name}: malformed xref entry.");

                var entryParts = entryLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Require(entryParts.Length >= 3, $"{name}: malformed xref entry.");
                entries[firstObject + i] = new XrefEntry(
                    long.Parse(entryParts[0], CultureInfo.InvariantCulture),
                    int.Parse(entryParts[1], CultureInfo.InvariantCulture),
                    entryParts[2] == "n");
            }
        }

        Require(line == "trailer", $"{name}: missing xref trailer.");

        var trailer = new StringBuilder();
        while ((line = reader.ReadLine()) is not null && line != "startxref")
            trailer.AppendLine(line);

        var trailerText = trailer.ToString();
        var size = RequiredInt(trailerText, @"/Size\s+(\d+)", $"{name}: missing trailer Size.");
        var root = RequiredInt(trailerText, @"/Root\s+(\d+)\s+\d+\s+R", $"{name}: missing trailer Root.");
        return new XrefTable(entries, size, root);
    }

    private static IEnumerable<(int ObjectNumber, int Generation)> ReadIndirectReferences(string text)
    {
        var withoutStreams = Regex.Replace(
            text,
            @"stream\r?\n.*?\r?\nendstream",
            "stream\nendstream",
            RegexOptions.Singleline);

        foreach (Match match in Regex.Matches(withoutStreams, @"(?<!\d)(\d+)\s+(\d+)\s+R\b"))
        {
            yield return (
                int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }
    }

    private static int RequiredInt(string text, string pattern, string message)
    {
        var match = Regex.Match(text, pattern);
        Require(match.Success, message);
        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static bool IsInUse(XrefTable xref, int objectNumber) =>
        xref.Entries.TryGetValue(objectNumber, out var entry) && entry.InUse;

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record XrefTable(SortedDictionary<int, XrefEntry> Entries, int TrailerSize, int RootObject);

    private readonly record struct XrefEntry(long Offset, int Generation, bool InUse);
}
