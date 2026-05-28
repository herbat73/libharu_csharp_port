using System.Text;
using System.Text.Json;
using LibHaru;
using static LibHaru.HPdf;

public static class FontFixtureCoverage
{
    private static readonly string[] FontExtensions = [".ttf", ".otf", ".ttc"];

    public static void Test(string repoRoot, string artifactsDir)
    {
        var fixtureDir = Path.Combine(repoRoot, "LibHaru.SmokeTests", "fixtures", "fonts");
        if (!Directory.Exists(fixtureDir))
        {
            Console.WriteLine($"No optional real-font fixtures found at {fixtureDir}");
            return;
        }

        var outputDir = Path.Combine(artifactsDir, "font-fixtures");
        Directory.CreateDirectory(outputDir);

        var fixtures = Directory.EnumerateFiles(fixtureDir, "*", SearchOption.AllDirectories)
            .Where(static path => FontExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Select(LoadFixture)
            .ToArray();

        if (fixtures.Length == 0)
        {
            Console.WriteLine($"No optional real-font fixtures found at {fixtureDir}");
            return;
        }

        Require(fixtures.Length >= 3, "Expected at least three checked-in real-font fixtures.");

        foreach (var fixture in fixtures)
            TestFixtureFont(fixture, Path.Combine(outputDir, $"{SafeName(Path.GetFileNameWithoutExtension(fixture.FontPath))}.pdf"));

        Console.WriteLine($"Exercised {fixtures.Length} optional real-font fixture(s)");
    }

    private static void TestFixtureFont(FontFixture fixture, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.All);

        var flavor = ReadSfntFlavor(fixture.FontPath);
        var fontName = string.Equals(Path.GetExtension(fixture.FontPath), ".ttc", StringComparison.OrdinalIgnoreCase)
            ? HPDF_LoadTTFontFromFile2(pdf, fixture.FontPath, 0, embedding: true)
            : HPDF_LoadTTFontFromFile(pdf, fixture.FontPath, embedding: true);

        Require(fontName == fixture.ExpectedBaseFont, $"{fixture.FontPath} loaded as {fontName}; expected {fixture.ExpectedBaseFont}.");

        var page = HPDF_AddPage(pdf);
        if (fixture.ExpectsCidKeyedCff)
        {
            Require(flavor == "OTTO", $"{fixture.FontPath} manifest marks a CID-keyed CFF fixture but the SFNT flavor is {flavor}.");
            var horizontal = HPDF_GetFont(pdf, fontName, "Identity-H");
            var vertical = HPDF_GetFont(pdf, fontName, "Identity-V");
            ValidateLoadedFont(fixture, horizontal, fixture.SampleText[0]);

            HPDF_Page_SetFontAndSize(page, horizontal, 16);
            HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 72, $"CID-keyed CFF Type0: {fixture.SampleText}");

            var verticalAdvance = (uint)Math.Max(0, (int)Math.Round(vertical.BBox.Top - vertical.BBox.Bottom));
            var verticalText = fixture.SampleText.Length >= 2 ? fixture.SampleText[..2] : fixture.SampleText;
            var verticalWidth = HPDF_Font_TextWidth(vertical, verticalText);
            Require(verticalWidth.NumChars == verticalText.Length && verticalWidth.Width == verticalAdvance * verticalText.Length, $"{fixture.FontPath} did not use vertical DW2 displacement metrics.");

            HPDF_Page_SetFontAndSize(page, vertical, 16);
            HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 104, verticalText);
        }
        else
        {
            var font = HPDF_GetFont(pdf, fontName, "WinAnsiEncoding");
            ValidateLoadedFont(fixture, font, 'A');

            HPDF_Page_SetFontAndSize(page, font, 16);
            HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 72, "OpenType/TrueType fixture ABC 123");

            if (flavor != "OTTO")
            {
                var utf = HPDF_GetFont(pdf, fontName, "UTF-8");
                var vertical = HPDF_GetFont(pdf, fontName, "Identity-V");
                var verticalAdvance = (uint)Math.Max(0, (int)Math.Round(vertical.BBox.Top - vertical.BBox.Bottom));
                var verticalWidth = HPDF_Font_TextWidth(vertical, "AV");
                Require(verticalWidth.NumChars == 2 && verticalWidth.Width == verticalAdvance * 2, $"{fixture.FontPath} did not use vertical DW2 displacement metrics.");

                HPDF_Page_SetFontAndSize(page, utf, 16);
                HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 104, $"Type0 UTF path: {fixture.SampleText}");

                HPDF_Page_SetFontAndSize(page, vertical, 16);
                HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 136, "AV");
            }
        }

        HPDF_SaveToFile(pdf, pdfPath);

        var latin1 = Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));
        Require(latin1.Contains($"/BaseFont /{fontName}", StringComparison.Ordinal), $"{fixture.FontPath} did not write the expected BaseFont.");
        Require(latin1.Contains("/FontDescriptor", StringComparison.Ordinal), $"{fixture.FontPath} did not write a FontDescriptor.");

        if (fixture.ExpectsCidKeyedCff)
        {
            Require(latin1.StartsWith("%PDF-1.6", StringComparison.Ordinal), $"{fixture.FontPath} did not raise the PDF version for OpenType embedding.");
            Require(latin1.Contains("/Subtype /Type0", StringComparison.Ordinal), $"{fixture.FontPath} did not write a Type0 font dictionary.");
            Require(latin1.Contains("/Subtype /CIDFontType0", StringComparison.Ordinal), $"{fixture.FontPath} did not write a CIDFontType0 descendant.");
            Require(latin1.Contains("/Encoding /Identity-H", StringComparison.Ordinal), $"{fixture.FontPath} did not write Identity-H encoding.");
            Require(latin1.Contains("/Encoding /Identity-V", StringComparison.Ordinal), $"{fixture.FontPath} did not write Identity-V encoding.");
            Require(latin1.Contains("/ToUnicode", StringComparison.Ordinal), $"{fixture.FontPath} did not write a ToUnicode CMap.");
            Require(latin1.Contains("/DW2 [", StringComparison.Ordinal), $"{fixture.FontPath} did not write vertical DW2 metrics.");
            Require(latin1.Contains("/W [", StringComparison.Ordinal), $"{fixture.FontPath} did not write CID widths.");
            Require(latin1.Contains("/FontFile3 ", StringComparison.Ordinal), $"{fixture.FontPath} did not embed an OpenType FontFile3 stream.");
            Require(latin1.Contains("/Subtype /OpenType", StringComparison.Ordinal), $"{fixture.FontPath} did not identify the embedded OpenType program.");
            Require(!latin1.Contains("/CIDToGIDMap", StringComparison.Ordinal), $"{fixture.FontPath} should not write a CIDToGIDMap for CIDFontType0.");
        }
        else if (flavor == "OTTO")
        {
            Require(latin1.StartsWith("%PDF-1.6", StringComparison.Ordinal), $"{fixture.FontPath} did not raise the PDF version for OpenType embedding.");
            Require(latin1.Contains("/Subtype /Type1", StringComparison.Ordinal), $"{fixture.FontPath} did not write a CFF-backed Type1 font dictionary.");
            Require(latin1.Contains("/FontFile3 ", StringComparison.Ordinal), $"{fixture.FontPath} did not embed an OpenType FontFile3 stream.");
            Require(latin1.Contains("/Subtype /OpenType", StringComparison.Ordinal), $"{fixture.FontPath} did not identify the embedded OpenType program.");
        }
        else
        {
            Require(latin1.Contains("/Subtype /TrueType", StringComparison.Ordinal), $"{fixture.FontPath} did not write a TrueType font dictionary.");
            Require(latin1.Contains("/Subtype /Type0", StringComparison.Ordinal), $"{fixture.FontPath} did not write a Type0 font dictionary.");
            Require(latin1.Contains("/Subtype /CIDFontType2", StringComparison.Ordinal), $"{fixture.FontPath} did not write a CIDFontType2 descendant.");
            Require(latin1.Contains("/Encoding /Identity-H", StringComparison.Ordinal), $"{fixture.FontPath} did not write Identity-H encoding.");
            Require(latin1.Contains("/Encoding /Identity-V", StringComparison.Ordinal), $"{fixture.FontPath} did not write Identity-V encoding.");
            Require(latin1.Contains("/ToUnicode", StringComparison.Ordinal), $"{fixture.FontPath} did not write a ToUnicode CMap.");
            Require(latin1.Contains("/CIDToGIDMap ", StringComparison.Ordinal), $"{fixture.FontPath} did not write a CIDToGIDMap stream.");
            Require(latin1.Contains("/DW2 [", StringComparison.Ordinal), $"{fixture.FontPath} did not write vertical DW2 metrics.");
            Require(latin1.Contains("/FontFile2 ", StringComparison.Ordinal), $"{fixture.FontPath} did not embed a FontFile2 stream.");
            var embeddedLength = FindFontFile2Length1(latin1);
            var sourceLength = new FileInfo(fixture.FontPath).Length;
            Require(embeddedLength > 0 && embeddedLength < sourceLength, $"{fixture.FontPath} embedded the full font instead of a subset.");
        }
    }

    private static void ValidateLoadedFont(FontFixture fixture, PdfFont font, char probe)
    {
        Require(!string.IsNullOrWhiteSpace(HPDF_Font_GetFontName(font)), $"{fixture.FontPath} did not expose a font name.");
        Require(HPDF_Font_GetFontName(font) == fixture.ExpectedBaseFont, $"{fixture.FontPath} returned an unexpected font handle name.");
        Require(HPDF_Font_GetUnicodeWidth(font, probe) > 0, $"{fixture.FontPath} did not expose width data for U+{(int)probe:X4}.");
        Require(HPDF_Font_GetAscent(font) > 0, $"{fixture.FontPath} did not expose ascent metrics.");
        Require(HPDF_Font_GetBBox(font).Right > HPDF_Font_GetBBox(font).Left, $"{fixture.FontPath} did not expose a valid bounding box.");
    }

    private static FontFixture LoadFixture(string fontPath)
    {
        var manifestPath = Path.Combine(
            Path.GetDirectoryName(fontPath)!,
            $"{Path.GetFileNameWithoutExtension(fontPath)}_manifest.json");
        Require(File.Exists(manifestPath), $"Real-font fixture is missing its manifest: {manifestPath}.");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;
        var family = ReadRequiredString(root, "family", manifestPath);
        var name = ReadRequiredString(root, "name", manifestPath);
        var license = ReadRequiredString(root, "license", manifestPath);
        var styles = ReadStringArray(root, "styles", manifestPath);
        var formats = ReadStringArray(root, "formats", manifestPath);
        var tags = ReadOptionalStringArray(root, "tags");
        var extension = Path.GetExtension(fontPath).TrimStart('.').ToLowerInvariant();

        Require(styles.Length > 0, $"Font fixture manifest has no styles: {manifestPath}.");
        Require(formats.Contains(extension, StringComparer.OrdinalIgnoreCase), $"Font fixture manifest does not list {extension}: {manifestPath}.");
        Require(license.Contains("Open Font License", StringComparison.OrdinalIgnoreCase), $"Font fixture manifest should record the open font license: {manifestPath}.");

        var expectedBaseFont = Path.GetFileNameWithoutExtension(fontPath);
        var sampleText = ReadOptionalString(root, "sampleText") ?? $"{name} {styles[0]} ABC 123";
        Require(sampleText.Length > 0, $"Font fixture manifest has an empty sampleText: {manifestPath}.");
        var expectsCidKeyedCff = tags.Contains("cid-keyed", StringComparer.OrdinalIgnoreCase)
            || tags.Contains("cid-keyed-cff", StringComparer.OrdinalIgnoreCase);
        return new FontFixture(fontPath, manifestPath, family, expectedBaseFont, sampleText, expectsCidKeyedCff);
    }

    private static string ReadRequiredString(JsonElement root, string propertyName, string manifestPath)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Font fixture manifest is missing '{propertyName}': {manifestPath}.");

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Font fixture manifest has an empty '{propertyName}': {manifestPath}.");

        return value;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        return property.GetString();
    }

    private static string[] ReadStringArray(JsonElement root, string propertyName, string manifestPath)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Font fixture manifest is missing '{propertyName}': {manifestPath}.");

        return property.EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String)
            .Select(static item => item.GetString())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item!)
            .ToArray();
    }

    private static string[] ReadOptionalStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            return [];

        return property.EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String)
            .Select(static item => item.GetString())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item!)
            .ToArray();
    }

    private static string ReadSfntFlavor(string path)
    {
        Span<byte> header = stackalloc byte[4];
        using var stream = File.OpenRead(path);
        if (stream.Read(header) != header.Length)
            return string.Empty;

        if (header[0] == 0x00 && header[1] == 0x01 && header[2] == 0x00 && header[3] == 0x00)
            return "true";

        return Encoding.ASCII.GetString(header);
    }

    private static long FindFontFile2Length1(string value)
    {
        var index = 0;
        while ((index = value.IndexOf("/FontFile2", index, StringComparison.Ordinal)) >= 0)
        {
            var objectNumberStart = index + "/FontFile2".Length;
            if (!TryReadInteger(value, objectNumberStart, out var objectNumber))
            {
                index += "/FontFile2".Length;
                continue;
            }

            var objectHeader = $"{objectNumber} 0 obj";
            var objectIndex = value.IndexOf(objectHeader, StringComparison.Ordinal);
            if (objectIndex < 0)
            {
                index += "/FontFile2".Length;
                continue;
            }

            var objectEnd = value.IndexOf("endobj", objectIndex, StringComparison.Ordinal);
            if (objectEnd < 0)
                objectEnd = value.Length;

            var length1Index = value.IndexOf("/Length1", objectIndex, objectEnd - objectIndex, StringComparison.Ordinal);
            if (length1Index >= 0 && TryReadInteger(value, length1Index + "/Length1".Length, out var length1))
                return length1;

            index += "/FontFile2".Length;
        }

        return -1;
    }

    private static bool TryReadInteger(string value, int start, out long number)
    {
        var numberStart = start;
        while (numberStart < value.Length && char.IsWhiteSpace(value[numberStart]))
            numberStart++;

        var end = numberStart;
        while (end < value.Length && char.IsDigit(value[end]))
            end++;

        if (end == numberStart)
        {
            number = 0;
            return false;
        }

        return long.TryParse(value.AsSpan(numberStart, end - numberStart), out number);
    }

    private static string SafeName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
            builder.Append(char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_');

        return builder.Length == 0 ? "font-fixture" : builder.ToString();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record FontFixture(
        string FontPath,
        string ManifestPath,
        string Family,
        string ExpectedBaseFont,
        string SampleText,
        bool ExpectsCidKeyedCff);
}
