using System.Text;
using LibHaru;
using static LibHaru.HPdf;

public static class FontAndEncoder
{
    public static void Test(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.All);

        var afmPath = Path.Combine(repoRoot, "demo", "type1", "a010013l.afm");
        var pfbPath = Path.Combine(repoRoot, "demo", "type1", "a010013l.pfb");
        var type1Name = HPDF_LoadType1FontFromFile(pdf, afmPath, pfbPath);
        var type1 = HPDF_GetFont(pdf, type1Name, "WinAnsiEncoding");

        var ttPath = Path.Combine(repoRoot, "demo", "ttfont", "PenguinAttack.ttf");
        var ttName = HPDF_LoadTTFontFromFile(pdf, ttPath, embedding: true);
        var tt = HPDF_GetFont(pdf, ttName, "WinAnsiEncoding");
        var ttUtf = HPDF_GetFont(pdf, ttName, "UTF-8");
        var ttVertical = HPDF_GetFont(pdf, ttName, "Identity-V");
        var supplementaryUtf = TryLoadSupplementaryCMapFont(pdf);
        var ttSourceLength = new FileInfo(ttPath).Length;

        Require(type1Name == "URWGothicL-Book", "Unexpected Type1 font name.");
        Require(HPDF_Font_GetEncodingName(type1) == "WinAnsiEncoding", "Type1 font encoding was not retained.");
        Require(HPDF_Font_GetUnicodeWidth(type1, 'A') == 740, "Type1 AFM width for A was not parsed.");
        Require(HPDF_Font_GetBBox(type1).Right == 1151, "Type1 AFM bounding box was not parsed.");
        Require(HPDF_Font_GetUnicodeWidth(tt, 'A') > 0, "TrueType cmap/hmtx width lookup failed.");
        Require(HPDF_Font_GetAscent(tt) > 0, "TrueType hhea ascent was not parsed.");
        Require(HPDF_Font_GetEncodingName(ttUtf) == "UTF-8", "UTF encoder name was not retained.");
        Require(HPDF_Font_GetEncodingName(ttVertical) == "Identity-V", "Vertical Identity encoder name was not retained.");
        var verticalAdvance = (uint)Math.Max(0, (int)Math.Round(ttVertical.BBox.Top - ttVertical.BBox.Bottom));
        var verticalTextWidth = HPDF_Font_TextWidth(ttVertical, "AV");
        Require(verticalTextWidth.NumChars == 2 && verticalTextWidth.Width == verticalAdvance * 2, "Vertical TrueType text width did not use DW2 displacement metrics.");

        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(page, type1, 18);
        HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 72, "Type1 AFM/PFB embedding: Cafe \u20ac");

        HPDF_Page_SetFontAndSize(page, tt, 18);
        HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 110, "TrueType parsing and embedding");

        HPDF_Page_SetFontAndSize(page, ttUtf, 18);
        HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 148, "Type0 CID UTF text path");

        HPDF_Page_SetFontAndSize(page, ttVertical, 18);
        HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 186, "AV");

        if (supplementaryUtf is not null)
        {
            HPDF_Page_SetFontAndSize(page, supplementaryUtf, 18);
            HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 224, "Supplementary cmap: \U0001F600");
        }

        HPDF_SaveToFile(pdf, pdfPath);

        var bytes = File.ReadAllBytes(pdfPath);
        var latin1 = Encoding.Latin1.GetString(bytes);

        Require(latin1.Contains("/BaseFont /URWGothicL-Book", StringComparison.Ordinal), "Missing Type1 BaseFont.");
        Require(latin1.Contains("/Subtype /Type1", StringComparison.Ordinal), "Missing Type1 font dictionary.");
        Require(latin1.Contains("/Subtype /TrueType", StringComparison.Ordinal), "Missing TrueType font dictionary.");
        Require(latin1.Contains("/Subtype /Type0", StringComparison.Ordinal), "Missing Type0 composite font dictionary.");
        Require(latin1.Contains("/Subtype /CIDFontType2", StringComparison.Ordinal), "Missing CIDFontType2 descendant font.");
        Require(latin1.Contains("/Encoding /Identity-H", StringComparison.Ordinal), "Missing Identity-H CMap encoding.");
        Require(latin1.Contains("/Encoding /Identity-V", StringComparison.Ordinal), "Missing Identity-V CMap encoding.");
        Require(latin1.Contains($"/DW2 [{(int)Math.Round(ttVertical.BBox.Bottom)} {(int)Math.Round(ttVertical.BBox.Bottom - ttVertical.BBox.Top)}]", StringComparison.Ordinal), "Missing TrueType CID vertical metrics.");
        Require(latin1.Contains("/ToUnicode", StringComparison.Ordinal), "Missing ToUnicode CMap.");
        Require(latin1.Contains("/CIDToGIDMap ", StringComparison.Ordinal), "Missing explicit CIDToGIDMap stream.");
        Require(!latin1.Contains("/CIDToGIDMap /Identity", StringComparison.Ordinal), "Composite TrueType font should use a remapped CIDToGIDMap stream.");
        Require(latin1.Contains("<0001> <0054>", StringComparison.Ordinal), "Missing dense-CID ToUnicode entry for Type0 text.");
        Require(latin1.Contains("/W [1 [", StringComparison.Ordinal), "Missing dense-CID width array.");
        if (supplementaryUtf is not null)
            Require(latin1.Contains("<D83DDE00>", StringComparison.Ordinal), "Missing supplementary-plane ToUnicode mapping from cmap format 12.");
        Require(latin1.Contains("/FontDescriptor", StringComparison.Ordinal), "Missing font descriptor.");
        Require(latin1.Contains("/FontFile ", StringComparison.Ordinal), "Missing Type1 FontFile stream.");
        Require(latin1.Contains("/FontFile2 ", StringComparison.Ordinal), "Missing TrueType FontFile2 stream.");
        Require(latin1.Contains("/FirstChar 32", StringComparison.Ordinal), "Missing FirstChar entry.");
        Require(latin1.Contains("/LastChar 255", StringComparison.Ordinal), "Missing LastChar entry.");
        Require(latin1.Contains("/Widths [", StringComparison.Ordinal), "Missing Widths array.");
        Require(latin1.Contains("/Length1", StringComparison.Ordinal), "Missing font program Length1 entry.");
        Require(!latin1.Contains($"/Length1 {ttSourceLength}", StringComparison.Ordinal), "TrueType FontFile2 embedded the full source font instead of a subset.");
        var trueTypeLength = FindFontFile2Length1(latin1);
        Require(trueTypeLength > 0 && trueTypeLength < ttSourceLength, $"Missing shortened embedded TrueType font program length; got {trueTypeLength} from source length {ttSourceLength}.");
        Require(Count(latin1, "/FlateDecode") >= 3, "Expected Flate filters for page content and embedded font programs.");

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"{bytes.Length} bytes with Type1, TrueType, and Type0 CID fonts");
    }

    private static int Count(string value, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static PdfFont? TryLoadSupplementaryCMapFont(PdfDocument pdf)
    {
        var fontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (string.IsNullOrWhiteSpace(fontsPath))
            return null;

        foreach (var fileName in new[] { "seguisym.ttf", "seguiemj.ttf" })
        {
            var path = Path.Combine(fontsPath, fileName);
            if (!File.Exists(path))
                continue;

            var fontName = HPDF_LoadTTFontFromFile(pdf, path, embedding: true);
            return HPDF_GetFont(pdf, fontName, "UTF-8");
        }

        return null;
    }

    private static long FindFontFile2Length1(string value)
    {
        var index = 0;
        while ((index = value.IndexOf("/FontFile2", index, StringComparison.Ordinal)) >= 0)
        {
            var objectNumberStart = index + "/FontFile2".Length;
            if (!TryReadInteger(value, objectNumberStart, out var objectNumber, out _))
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
            if (length1Index >= 0 && TryReadInteger(value, length1Index + "/Length1".Length, out var length1, out _))
                return length1;

            index += "/FontFile2".Length;
        }

        return -1;
    }

    private static bool TryReadInteger(string value, int start, out long number, out int end)
    {
        var numberStart = start;
        while (numberStart < value.Length && char.IsWhiteSpace(value[numberStart]))
            numberStart++;

        end = numberStart;
        while (end < value.Length && char.IsDigit(value[end]))
            end++;

        if (end == numberStart)
        {
            number = 0;
            return false;
        }

        return long.TryParse(value.AsSpan(numberStart, end - numberStart), out number);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
