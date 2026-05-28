using System.Text;
using System.Text.RegularExpressions;
using LibHaru;
using static LibHaru.HPdf;

public static class CompatibilityDemos
{
    public static void Test(string repoRoot, string artifactsRoot)
    {
        var outputDir = Path.Combine(artifactsRoot, "compatibility-demos");
        Directory.CreateDirectory(outputDir);

        var reports = new[]
        {
            PortArcDemo(Path.Combine(outputDir, "compat-arc-demo.pdf")),
            PortAttachDemo(repoRoot, Path.Combine(outputDir, "compat-attach-demo.pdf")),
            PortCharacterMapDemo(Path.Combine(outputDir, "compat-character-map-demo.pdf")),
            PortChFontDemo(repoRoot, Path.Combine(outputDir, "compat-chfont-demo.pdf")),
            PortEncodingListDemo(repoRoot, Path.Combine(outputDir, "compat-encoding-list-demo.pdf")),
            PortEncryptionDemo(Path.Combine(outputDir, "compat-encryption-demo.pdf")),
            PortExtGStateDemo(Path.Combine(outputDir, "compat-ext-gstate-demo.pdf")),
            PortFontDemo(Path.Combine(outputDir, "compat-font-demo.pdf")),
            PortGridSheetDemo(Path.Combine(outputDir, "compat-grid-sheet-demo.pdf")),
            PortLineDemo(Path.Combine(outputDir, "compat-line-demo.pdf")),
            PortLinkAnnotationDemo(Path.Combine(outputDir, "compat-link-annotation-demo.pdf")),
            PortTextDemo(Path.Combine(outputDir, "compat-text-demo.pdf")),
            PortTextDemo2(Path.Combine(outputDir, "compat-text-demo2.pdf")),
            PortTextAnnotationDemo(Path.Combine(outputDir, "compat-text-annotation-demo.pdf")),
            PortImageDemo(repoRoot, Path.Combine(outputDir, "compat-image-demo.pdf")),
            PortImageFixturesDemo(repoRoot, outputDir, Path.Combine(outputDir, "compat-image-fixtures-demo.pdf")),
            PortJpegDemo(repoRoot, Path.Combine(outputDir, "compat-jpeg-demo.pdf")),
            PortJpFontDemo(repoRoot, Path.Combine(outputDir, "compat-jpfont-demo.pdf")),
            PortOutlineDemo(Path.Combine(outputDir, "compat-outline-demo.pdf")),
            PortOutlineDemoJp(Path.Combine(outputDir, "compat-outline-demo-jp.pdf")),
            PortPdfAConformanceDemo(repoRoot, Path.Combine(outputDir, "compat-pdf-a-conformance-demo.pdf")),
            PortPngDemo(repoRoot, Path.Combine(outputDir, "compat-png-demo.pdf")),
            PortRawImageDemo(repoRoot, Path.Combine(outputDir, "compat-raw-image-demo.pdf")),
            PortSlideShowDemo(Path.Combine(outputDir, "compat-slide-show-demo.pdf")),
            PortTtFontDemo(repoRoot, Path.Combine(outputDir, "compat-ttfont-demo.pdf")),
            PortTtFontDemoJp(repoRoot, Path.Combine(outputDir, "compat-ttfont-demo-jp.pdf")),
            PortDocumentDemo(repoRoot, Path.Combine(outputDir, "compat-document-demo.pdf")),
            PortPermissionDemo(Path.Combine(outputDir, "compat-permission-demo.pdf"))
        };

        File.WriteAllText(
            Path.Combine(outputDir, "compatibility-index.structure.txt"),
            string.Join(Environment.NewLine + Environment.NewLine, reports.Select(static report => report.Text)));
        File.WriteAllText(
            Path.Combine(outputDir, "libharu-demo-inventory.txt"),
            BuildDemoInventory(repoRoot, reports));

        Console.WriteLine($"Generated {reports.Length} compatibility demo PDFs in {outputDir}");
    }

    private static PdfStructureReport PortArcDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetHeight(page, 220);
        HPDF_Page_SetWidth(page, 200);

        DrawGrid(page);

        HPDF_Page_SetRGBFill(page, 1, 0, 0);
        HPDF_Page_MoveTo(page, 100, 100);
        HPDF_Page_LineTo(page, 100, 180);
        HPDF_Page_Arc(page, 100, 100, 80, 0, 360 * 0.45);
        var pos = HPDF_Page_GetCurrentPos(page);
        HPDF_Page_LineTo(page, 100, 100);
        HPDF_Page_Fill(page);

        HPDF_Page_SetRGBFill(page, 0, 0, 1);
        HPDF_Page_MoveTo(page, 100, 100);
        HPDF_Page_LineTo(page, pos.X, pos.Y);
        HPDF_Page_Arc(page, 100, 100, 80, 360 * 0.45, 360 * 0.7);
        pos = HPDF_Page_GetCurrentPos(page);
        HPDF_Page_LineTo(page, 100, 100);
        HPDF_Page_Fill(page);

        HPDF_Page_SetRGBFill(page, 0, 1, 0);
        HPDF_Page_MoveTo(page, 100, 100);
        HPDF_Page_LineTo(page, pos.X, pos.Y);
        HPDF_Page_Arc(page, 100, 100, 80, 360 * 0.7, 360 * 0.85);
        pos = HPDF_Page_GetCurrentPos(page);
        HPDF_Page_LineTo(page, 100, 100);
        HPDF_Page_Fill(page);

        HPDF_Page_SetRGBFill(page, 1, 1, 0);
        HPDF_Page_MoveTo(page, 100, 100);
        HPDF_Page_LineTo(page, pos.X, pos.Y);
        HPDF_Page_Arc(page, 100, 100, 80, 360 * 0.85, 360);
        HPDF_Page_LineTo(page, 100, 100);
        HPDF_Page_Fill(page);

        HPDF_Page_SetGrayStroke(page, 0);
        HPDF_Page_SetGrayFill(page, 1);
        HPDF_Page_Circle(page, 100, 100, 30);
        HPDF_Page_Fill(page);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "arc_demo.c managed port",
            pdfPath,
            RequireToken("/MediaBox [0 0 200 220]", 1),
            RequireToken(" rg\n", 4),
            RequireToken(" G\n", 1),
            RequireToken(" g\n", 1),
            RequireToken(" c\n", 9),
            RequireToken("\nf\n", 5));
    }

    private static PdfStructureReport PortAttachDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.UseAttachments);

        var page = HPDF_AddPage(pdf);
        var font = HPDF_GetFont(pdf, "Helvetica");
        HPDF_Page_SetFontAndSize(page, font, 18);
        HPDF_Page_TextOut(page, 72, HPDF_Page_GetHeight(page) - 90, "attach.c managed port");

        var embedded = HPDF_AttachFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "basn3p08.png"));
        HPDF_EmbeddedFile_SetName(embedded, "basn3p08.png");
        HPDF_EmbeddedFile_SetDescription(embedded, "libharu attachment demo fixture");
        HPDF_EmbeddedFile_SetSubtype(embedded, "image/png");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "attach.c managed port",
            pdfPath,
            RequireToken("/PageMode /UseAttachments", 1),
            RequireToken("/Names", 1),
            RequireToken("/EmbeddedFiles", 1),
            RequireToken("/Type /Filespec", 1),
            RequireToken("/Type /EmbeddedFile", 1),
            RequireToken("/Desc (libharu attachment demo fixture)", 1),
            RequireToken("/Subtype /image#2Fpng", 1));
    }

    private static PdfStructureReport PortCharacterMapDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);
        HPDF_UseJPEncodings(pdf);
        HPDF_UseJPFonts(pdf);

        var root = HPDF_CreateOutline(pdf, null, "90ms-RKSJ-H");
        HPDF_Outline_SetOpened(root, true);
        var titleFont = HPDF_GetFont(pdf, "Helvetica");
        var font = HPDF_GetFont(pdf, "MS-Mincho", "90ms-RKSJ-H");

        for (var high = 0x81; high <= 0x83; high++)
        {
            var page = HPDF_AddPage(pdf);
            HPDF_Page_SetWidth(page, 420);
            HPDF_Page_SetHeight(page, 420);

            var outline = HPDF_CreateOutline(pdf, root, $"0x{high:X2}40-0x{high:X2}7E");
            var destination = HPDF_Page_CreateDestination(page);
            HPDF_Outline_SetDestination(outline, destination);

            DrawCMapPage(page, titleFont, font, (byte)high, 0x40, 0x7E, "90ms-RKSJ-H (MS-Mincho)");
        }

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "character_map.c managed port",
            pdfPath,
            RequireToken("/PageMode /UseOutlines", 1),
            RequireToken("/Outlines", 1),
            RequireToken("/Type /Page\n", 3),
            RequireToken("/Subtype /Type0", 1),
            RequireToken("/Subtype /CIDFontType0", 1),
            RequireToken("/Encoding /90ms-RKSJ-H", 1),
            RequireToken("/Ordering (Japan1)", 1),
            RequireToken("90ms-RKSJ-H", 1));
    }

    private static PdfStructureReport PortChFontDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_UseJPEncodings(pdf);

        var fontPath = Path.Combine(repoRoot, "demo", "ttfont", "PenguinAttack.ttf");
        var fontName = HPDF_LoadTTFontFromFile(pdf, fontPath, embedding: true);
        var cp936Font = HPDF_GetFont(pdf, fontName, "GBK-EUC-H");
        var cp932Font = HPDF_GetFont(pdf, fontName, "90ms-RKSJ-H");

        var cp936Lines = ReadLatin1Lines(Path.Combine(repoRoot, "demo", "mbtext", "cp936.txt"), 4);
        var cp932Lines = ReadLatin1Lines(Path.Combine(repoRoot, "demo", "mbtext", "cp932.txt"), 4);

        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetHeight(page, 300);
        HPDF_Page_SetWidth(page, 550);
        DrawGrid(page);
        HPDF_Page_SetTextLeading(page, 24);

        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 50, 250);
        for (var i = 0; i < Math.Min(cp936Lines.Length, cp932Lines.Length); i++)
        {
            HPDF_Page_SetFontAndSize(page, cp936Font, 18);
            HPDF_Page_ShowText(page, cp936Lines[i]);
            HPDF_Page_ShowText(page, "  ");
            HPDF_Page_SetFontAndSize(page, cp932Font, 18);
            HPDF_Page_ShowText(page, cp932Lines[i]);
            HPDF_Page_ShowTextNextLine(page, string.Empty);
        }

        HPDF_Page_EndText(page);
        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "chfont_demo.c managed port",
            pdfPath,
            RequireToken("/Type /Page\n", 1),
            RequireToken("/Subtype /Type0", 2),
            RequireToken("/Subtype /CIDFontType2", 2),
            RequireToken("/Encoding /GBK-EUC-H", 1),
            RequireToken("/Encoding /90ms-RKSJ-H", 1),
            RequireToken("/FontFile2", 1));
    }

    private static PdfStructureReport PortEncodingListDemo(string repoRoot, string pdfPath)
    {
        const int pageWidth = 420;
        const int pageHeight = 400;
        var encodings = new[]
        {
            "StandardEncoding",
            "MacRomanEncoding",
            "WinAnsiEncoding",
            "ISO8859-2",
            "ISO8859-3",
            "ISO8859-4",
            "ISO8859-5",
            "ISO8859-9",
            "ISO8859-10",
            "ISO8859-13",
            "ISO8859-14",
            "ISO8859-15",
            "ISO8859-16",
            "CP1250",
            "CP1251",
            "CP1252",
            "CP1254",
            "CP1257",
            "KOI8-R",
            "Symbol-Set",
            "ZapfDingbats-Set"
        };

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);

        var labelFont = HPDF_GetFont(pdf, "Helvetica");
        var type1Name = HPDF_LoadType1FontFromFile(
            pdf,
            Path.Combine(repoRoot, "demo", "type1", "a010013l.afm"),
            Path.Combine(repoRoot, "demo", "type1", "a010013l.pfb"));

        var root = HPDF_CreateOutline(pdf, null, "Encoding list");
        HPDF_Outline_SetOpened(root, true);

        foreach (var encoding in encodings)
        {
            var page = HPDF_AddPage(pdf);
            HPDF_Page_SetWidth(page, pageWidth);
            HPDF_Page_SetHeight(page, pageHeight);

            var outline = HPDF_CreateOutline(pdf, root, encoding);
            var destination = HPDF_Page_CreateDestination(page);
            HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(page), 1);
            HPDF_Outline_SetDestination(outline, destination);

            HPDF_Page_SetFontAndSize(page, labelFont, 15);
            DrawEncodingGraph(page);

            HPDF_Page_BeginText(page);
            HPDF_Page_SetFontAndSize(page, labelFont, 20);
            HPDF_Page_MoveTextPos(page, 40, pageHeight - 50);
            HPDF_Page_ShowText(page, encoding);
            HPDF_Page_ShowText(page, " Encoding");
            HPDF_Page_EndText(page);

            var glyphFont = encoding switch
            {
                "Symbol-Set" => HPDF_GetFont(pdf, "Symbol"),
                "ZapfDingbats-Set" => HPDF_GetFont(pdf, "ZapfDingbats"),
                _ => HPDF_GetFont(pdf, type1Name, encoding)
            };

            HPDF_Page_SetFontAndSize(page, glyphFont, 14);
            DrawEncodingFonts(page);
        }

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "encoding_list.c managed port",
            pdfPath,
            RequireToken("/PageMode /UseOutlines", 1),
            RequireToken("/Outlines", 1),
            RequireToken("/Dest ", encodings.Length),
            RequireToken("/Type /Page\n", encodings.Length),
            RequireToken("/MediaBox [0 0 420 400]", encodings.Length),
            RequireToken("/BaseFont /URWGothicL-Book", 1),
            RequireToken("/BaseFont /Symbol", 1),
            RequireToken("/BaseFont /ZapfDingbats", 1),
            RequireToken("/Encoding /MacRomanEncoding", 1),
            RequireToken("/Encoding /WinAnsiEncoding", 1),
            RequireToken("/Encoding /ISO8859-2", 1),
            RequireToken("/Encoding /ISO8859-16", 1),
            RequireToken("/Encoding /CP1250", 1),
            RequireToken("/Encoding /CP1251", 1),
            RequireToken("/Encoding /CP1254", 1),
            RequireToken("/Encoding /CP1257", 1),
            RequireToken("/Encoding /KOI8-R", 1),
            RequireToken("/FontFile ", 1));
    }

    private static PdfStructureReport PortEncryptionDemo(string pdfPath)
    {
        const string text = "This is an encrypt document example.";

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);

        HPDF_Page_SetSize(page, PdfPageSize.B5, PdfPageDirection.Landscape);
        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, font, 20);
        var textWidth = HPDF_Page_TextWidth(page, text);
        HPDF_Page_MoveTextPos(page, (HPDF_Page_GetWidth(page) - textWidth) / 2, (HPDF_Page_GetHeight(page) - 20) / 2);
        HPDF_Page_ShowText(page, text);
        HPDF_Page_EndText(page);

        HPDF_SetPassword(pdf, "owner", "user");
        HPDF_SaveToFile(pdf, pdfPath);

        var report = CheckPdf(
            "encryption.c managed port",
            pdfPath,
            RequireToken("/Encrypt", 1),
            RequireToken("/Filter /Standard", 1),
            RequireToken("/V 1", 1),
            RequireToken("/R 2", 1),
            RequireToken("/O <", 1),
            RequireToken("/U <", 1),
            RequireToken("/P ", 1),
            RequireToken("/ID [<", 1));

        var latin1 = Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));
        Require(!latin1.Contains(text, StringComparison.Ordinal), "Encrypted compatibility demo leaked plaintext page content.");
        Require(!latin1.Contains("/Length 128", StringComparison.Ordinal), "Revision 2 encryption should not emit a 128-bit Length entry.");
        return report;
    }

    private static PdfStructureReport PortExtGStateDemo(string pdfPath)
    {
        const double pageWidth = 600;
        const double pageHeight = 900;

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica-Bold");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(page, font, 10);
        HPDF_Page_SetHeight(page, pageHeight);
        HPDF_Page_SetWidth(page, pageWidth);

        HPDF_Page_GSave(page);
        DrawExtGStateCircles(page, "normal", 40, pageHeight - 170);
        HPDF_Page_GRestore(page);

        HPDF_Page_GSave(page);
        var alpha80 = HPDF_CreateExtGState(pdf);
        HPDF_ExtGState_SetAlphaFill(alpha80, 0.8);
        HPDF_ExtGState_SetAlphaStroke(alpha80, 0.8);
        HPDF_Page_SetExtGState(page, alpha80);
        DrawExtGStateCircles(page, "alpha fill = 0.8", 230, pageHeight - 170);
        HPDF_Page_GRestore(page);

        HPDF_Page_GSave(page);
        var alpha40 = HPDF_CreateExtGState(pdf);
        HPDF_ExtGState_SetAlphaFill(alpha40, 0.4);
        HPDF_Page_SetExtGState(page, alpha40);
        DrawExtGStateCircles(page, "alpha fill = 0.4", 420, pageHeight - 170);
        HPDF_Page_GRestore(page);

        DrawBlendModeGroup(pdf, page, PdfBlendMode.Multiply, "HPDF_BM_MULTIPLY", 40, pageHeight - 340);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.Screen, "HPDF_BM_SCREEN", 230, pageHeight - 340);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.Overlay, "HPDF_BM_OVERLAY", 420, pageHeight - 340);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.Darken, "HPDF_BM_DARKEN", 40, pageHeight - 510);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.Lighten, "HPDF_BM_LIGHTEN", 230, pageHeight - 510);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.ColorDodge, "HPDF_BM_COLOR_DODGE", 420, pageHeight - 510);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.ColorBurn, "HPDF_BM_COLOR_BURN", 40, pageHeight - 680);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.HardLight, "HPDF_BM_HARD_LIGHT", 230, pageHeight - 680);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.SoftLight, "HPDF_BM_SOFT_LIGHT", 420, pageHeight - 680);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.Difference, "HPDF_BM_DIFFERENCE", 40, pageHeight - 850);
        DrawBlendModeGroup(pdf, page, PdfBlendMode.Exclusion, "HPDF_BM_EXCLUSION", 230, pageHeight - 850);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "ext_gstate_demo.c managed port",
            pdfPath,
            RequireToken("/MediaBox [0 0 600 900]", 1),
            RequireToken("/ExtGState", 1),
            RequireToken("/Type /ExtGState", 13),
            RequireToken(" gs\n", 13),
            RequireToken("/CA 0.8", 1),
            RequireToken("/ca 0.8", 1),
            RequireToken("/ca 0.4", 1),
            RequireToken("/BM /Multiply", 1),
            RequireToken("/BM /Screen", 1),
            RequireToken("/BM /Overlay", 1),
            RequireToken("/BM /Darken", 1),
            RequireToken("/BM /Lighten", 1),
            RequireToken("/BM /ColorDodge", 1),
            RequireToken("/BM /ColorBurn", 1),
            RequireToken("/BM /HardLight", 1),
            RequireToken("/BM /SoftLight", 1),
            RequireToken("/BM /Difference", 1),
            RequireToken("/BM /Exclusion", 1),
            RequireToken("\nb\n", 42));
    }

    private static PdfStructureReport PortFontDemo(string pdfPath)
    {
        const string pageTitle = "Font Demo";
        const string sampleText = "abcdefgABCDEFG12345!#$%&+-@?";
        var fontNames = new[]
        {
            "Courier",
            "Courier-Bold",
            "Courier-Oblique",
            "Courier-BoldOblique",
            "Helvetica",
            "Helvetica-Bold",
            "Helvetica-Oblique",
            "Helvetica-BoldOblique",
            "Times-Roman",
            "Times-Bold",
            "Times-Italic",
            "Times-BoldItalic",
            "Symbol",
            "ZapfDingbats"
        };

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        var height = HPDF_Page_GetHeight(page);
        var width = HPDF_Page_GetWidth(page);

        HPDF_Page_SetLineWidth(page, 1);
        HPDF_Page_Rectangle(page, 50, 50, width - 100, height - 110);
        HPDF_Page_Stroke(page);

        var defaultFont = HPDF_GetFont(pdf, "Helvetica");
        HPDF_Page_SetFontAndSize(page, defaultFont, 24);
        var titleWidth = HPDF_Page_TextWidth(page, pageTitle);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, (width - titleWidth) / 2, height - 50, pageTitle);
        HPDF_Page_EndText(page);

        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, defaultFont, 16);
        HPDF_Page_TextOut(page, 60, height - 80, "<Standard Type1 fonts samples>");
        HPDF_Page_EndText(page);

        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 60, height - 105);

        foreach (var fontName in fontNames)
        {
            var sampleFont = HPDF_GetFont(pdf, fontName);
            var expectedEncoding = fontName is "Symbol" or "ZapfDingbats" ? "FontSpecific" : "StandardEncoding";
            Require(HPDF_Font_GetFontName(sampleFont) == fontName, $"Unexpected font name for {fontName}.");
            Require(HPDF_Font_GetEncodingName(sampleFont) == expectedEncoding, $"{fontName} did not use {expectedEncoding} by default.");

            HPDF_Page_SetFontAndSize(page, defaultFont, 9);
            HPDF_Page_ShowText(page, fontName);
            HPDF_Page_MoveTextPos(page, 0, -18);

            HPDF_Page_SetFontAndSize(page, sampleFont, 20);
            HPDF_Page_ShowText(page, sampleText);
            HPDF_Page_MoveTextPos(page, 0, -20);
        }

        HPDF_Page_EndText(page);
        HPDF_SaveToFile(pdf, pdfPath);

        var requirements = new List<StructuralRequirement>
        {
            RequireToken("/Type /Page\n", 1),
            RequireToken("/Font <<", 1),
            RequireToken("/Subtype /Type1", fontNames.Length),
            RequireToken(" Tf\n", (fontNames.Length * 2) + 2),
            RequireToken(" Tj\n", (fontNames.Length * 2) + 2),
            RequireToken("Font Demo", 1),
            RequireToken("<Standard Type1 fonts samples>", 1),
            RequireToken(sampleText, fontNames.Length)
        };

        requirements.AddRange(fontNames.Select(static fontName => RequireToken($"/BaseFont /{fontName}\n", 1)));

        return CheckPdf("font_demo.c managed port", pdfPath, [.. requirements]);
    }

    private static PdfStructureReport PortGridSheetDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetHeight(page, 600);
        HPDF_Page_SetWidth(page, 400);
        PrintGridSheet(pdf, page);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "grid_sheet.c managed port",
            pdfPath,
            RequireToken("/Type /Page\n", 1),
            RequireToken("/MediaBox [0 0 400 600]", 1),
            RequireToken("/BaseFont /Helvetica", 1),
            RequireToken("0.5 g\n", 1),
            RequireToken("0.8 G\n", 1),
            RequireToken("0 G\n", 1),
            RequireToken("0.5 w\n", 1),
            RequireToken("0.25 w\n", 1),
            RequireToken("\nS\n", 273),
            RequireToken(" Tj\n", 73));
    }

    private static PdfStructureReport PortLineDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        const string pageTitle = "Line Example";
        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);

        HPDF_Page_SetLineWidth(page, 1);
        HPDF_Page_Rectangle(page, 50, 50, HPDF_Page_GetWidth(page) - 100, HPDF_Page_GetHeight(page) - 110);
        HPDF_Page_Stroke(page);

        HPDF_Page_SetFontAndSize(page, font, 24);
        var titleWidth = HPDF_Page_TextWidth(page, pageTitle);
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, (HPDF_Page_GetWidth(page) - titleWidth) / 2, HPDF_Page_GetHeight(page) - 50);
        HPDF_Page_ShowText(page, pageTitle);
        HPDF_Page_EndText(page);

        HPDF_Page_SetFontAndSize(page, font, 10);

        HPDF_Page_SetLineWidth(page, 0);
        DrawLine(page, 60, 770, "line width = 0");
        HPDF_Page_SetLineWidth(page, 1);
        DrawLine(page, 60, 740, "line width = 1.0");
        HPDF_Page_SetLineWidth(page, 2);
        DrawLine(page, 60, 710, "line width = 2.0");

        HPDF_Page_SetLineWidth(page, 1);
        HPDF_Page_SetDash(page, [3.0], 1, 1);
        DrawLine(page, 60, 680, "dash_ptn=[3], phase=1");
        HPDF_Page_SetDash(page, [3.0, 7.0], 2, 2);
        DrawLine(page, 60, 650, "dash_ptn=[7, 3], phase=2");
        HPDF_Page_SetDash(page, [8.0, 7.0, 2.0, 7.0], 4, 0);
        DrawLine(page, 60, 620, "dash_ptn=[8, 7, 2, 7], phase=0");
        HPDF_Page_SetDash(page, null, 0, 0);

        HPDF_Page_SetLineWidth(page, 30);
        HPDF_Page_SetRGBStroke(page, 0, 0.5, 0);
        HPDF_Page_SetLineCap(page, PdfLineCap.ButtEnd);
        DrawLine2(page, 60, 570, "PDF_BUTT_END");
        HPDF_Page_SetLineCap(page, PdfLineCap.RoundEnd);
        DrawLine2(page, 60, 505, "PDF_ROUND_END");
        HPDF_Page_SetLineCap(page, PdfLineCap.ProjectingSquareEnd);
        DrawLine2(page, 60, 440, "PDF_PROJECTING_SQUARE_END");

        HPDF_Page_SetRGBStroke(page, 0, 0, 0.5);
        HPDF_Page_SetMiterLimit(page, 10);
        DrawJoin(page, PdfLineJoin.MiterJoin, 120, 300, "PDF_MITER_JOIN");
        DrawJoin(page, PdfLineJoin.RoundJoin, 120, 195, "PDF_ROUND_JOIN");
        DrawJoin(page, PdfLineJoin.BevelJoin, 120, 90, "PDF_BEVEL_JOIN");

        HPDF_Page_SetLineWidth(page, 2);
        HPDF_Page_SetRGBStroke(page, 0, 0, 0);
        HPDF_Page_SetRGBFill(page, 0.75, 0, 0);
        DrawRect(page, 300, 770, "Stroke");
        HPDF_Page_Stroke(page);
        DrawRect(page, 300, 720, "Fill");
        HPDF_Page_Fill(page);
        DrawRect(page, 300, 670, "Fill then Stroke");
        HPDF_Page_FillStroke(page);

        HPDF_Page_GSave(page);
        DrawRect(page, 300, 620, "Clip Rectangle");
        HPDF_Page_Clip(page);
        HPDF_Page_Stroke(page);
        HPDF_Page_SetFontAndSize(page, font, 13);
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 290, 600);
        HPDF_Page_SetTextLeading(page, 12);
        HPDF_Page_ShowText(page, "Clip Clip Clip Clip Clip Clip Clip");
        HPDF_Page_ShowTextNextLine(page, "Clip Clip Clip Clip Clip Clip Clip");
        HPDF_Page_EndText(page);
        HPDF_Page_GRestore(page);

        HPDF_Page_SetRGBStroke(page, 0, 0, 0);
        HPDF_Page_SetLineWidth(page, 1.5);
        HPDF_Page_MoveTo(page, 330, 440);
        HPDF_Page_CurveTo2(page, 430, 530, 480, 470);
        HPDF_Page_Stroke(page);
        HPDF_Page_MoveTo(page, 330, 290);
        HPDF_Page_CurveTo3(page, 430, 380, 480, 320);
        HPDF_Page_Stroke(page);
        HPDF_Page_MoveTo(page, 330, 140);
        HPDF_Page_CurveTo(page, 430, 280, 490, 210, 480, 90);
        HPDF_Page_Stroke(page);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "line_demo.c managed port",
            pdfPath,
            RequireToken("/BaseFont /Helvetica", 1),
            RequireToken("\n0 w\n", 1),
            RequireToken("[3] 1 d", 1),
            RequireToken("[3 7] 2 d", 1),
            RequireToken("[8 7 2 7] 0 d", 1),
            RequireToken("[] 0 d", 1),
            RequireToken(" J\n", 3),
            RequireToken(" j\n", 3),
            RequireToken("\nB\n", 1),
            RequireToken("\nW\n", 1),
            RequireToken(" v\n", 1),
            RequireToken(" y\n", 1),
            RequireToken(" c\n", 1));
    }

    private static PdfStructureReport PortLinkAnnotationDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var indexPage = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(indexPage, font, 18);
        HPDF_Page_TextOut(indexPage, 70, HPDF_Page_GetHeight(indexPage) - 70, "link_annotation.c managed port");

        var pages = Enumerable.Range(1, 7).Select(pageNumber =>
        {
            var page = HPDF_AddPage(pdf);
            HPDF_Page_SetFontAndSize(page, font, 24);
            HPDF_Page_TextOut(page, 120, HPDF_Page_GetHeight(page) - 120, $"Page {pageNumber}");
            return page;
        }).ToArray();

        for (var i = 0; i < pages.Length; i++)
        {
            var destination = HPDF_Page_CreateDestination(pages[i]);
            switch (i)
            {
                case 0:
                    HPDF_Destination_SetFit(destination);
                    break;
                case 1:
                    HPDF_Destination_SetFitH(destination, HPDF_Page_GetHeight(pages[i]));
                    break;
                case 2:
                    HPDF_Destination_SetFitV(destination, 0);
                    break;
                case 3:
                    HPDF_Destination_SetFitR(destination, 50, 50, 300, 400);
                    break;
                case 4:
                    HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(pages[i]), 1.5);
                    break;
                case 5:
                    HPDF_Destination_SetFitB(destination);
                    break;
                default:
                    HPDF_Destination_SetFitBH(destination, HPDF_Page_GetHeight(pages[i]));
                    break;
            }

            var y = HPDF_Page_GetHeight(indexPage) - 115 - i * 32;
            HPDF_Page_SetFontAndSize(indexPage, font, 12);
            HPDF_Page_TextOut(indexPage, 80, y, $"Jump to page {i + 1}");
            var link = HPDF_Page_CreateLinkAnnot(indexPage, new PdfRect(78, y - 4, 190, y + 14), destination);
            HPDF_LinkAnnot_SetBorderStyle(link, 1, (ushort)(i % 3), 2);
            HPDF_LinkAnnot_SetHighlightMode(link, (PdfAnnotHighlightMode)(i % 4));
        }

        HPDF_Page_CreateURILinkAnnot(indexPage, new PdfRect(78, 300, 260, 320), "https://libharu.org");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "link_annotation.c managed port",
            pdfPath,
            RequireToken("/Type /Page\n", 8),
            RequireToken("/Annots [", 1),
            RequireToken("/Subtype /Link", 8),
            RequireToken("/Dest ", 7),
            RequireToken("/S /URI", 1),
            RequireToken("/H /N", 1),
            RequireToken("/H /I", 1),
            RequireToken("/H /O", 1),
            RequireToken("/H /P", 1));
    }

    private static PdfStructureReport PortTextDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        const string pageTitle = "Text Demo";
        const string sampleText = "abcdefgABCDEFG123!#$%&+-@?";
        const string sampleText2 = "The quick brown fox jumps over the lazy dog.";

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        DrawGrid(page);

        HPDF_Page_SetFontAndSize(page, font, 24);
        var titleWidth = HPDF_Page_TextWidth(page, pageTitle);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, (HPDF_Page_GetWidth(page) - titleWidth) / 2, HPDF_Page_GetHeight(page) - 50, pageTitle);
        HPDF_Page_EndText(page);

        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 60, HPDF_Page_GetHeight(page) - 60);
        for (var size = 8.0; size < 60; size *= 1.5)
        {
            HPDF_Page_SetFontAndSize(page, font, size);
            HPDF_Page_MoveTextPos(page, 0, -5 - size);
            var length = HPDF_Page_MeasureText(page, sampleText, HPDF_Page_GetWidth(page) - 120, false, out _);
            HPDF_Page_ShowText(page, sampleText[..length]);
            HPDF_Page_MoveTextPos(page, 0, -10);
            HPDF_Page_SetFontAndSize(page, font, 8);
            HPDF_Page_ShowText(page, $"Fontsize={size:0}");
        }

        HPDF_Page_SetFontAndSize(page, font, 18);
        HPDF_Page_MoveTextPos(page, 0, -30);
        for (var i = 0; i < sampleText.Length; i++)
        {
            var r = (double)i / sampleText.Length;
            var g = 1 - r;
            HPDF_Page_SetRGBFill(page, r, g, 0);
            HPDF_Page_ShowText(page, sampleText[i].ToString());
        }

        HPDF_Page_EndText(page);

        const double y = 450;
        HPDF_Page_SetFontAndSize(page, font, 32);
        HPDF_Page_SetRGBFill(page, 0.5, 0.5, 0);
        HPDF_Page_SetLineWidth(page, 1.5);

        ShowDescription(page, font, 60, y, "RenderingMode=PDF_FILL");
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.Fill);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, y, "ABCabc123");
        HPDF_Page_EndText(page);

        ShowDescription(page, font, 60, y - 50, "RenderingMode=PDF_STROKE");
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.Stroke);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, y - 50, "ABCabc123");
        HPDF_Page_EndText(page);

        ShowDescription(page, font, 60, y - 100, "RenderingMode=PDF_FILL_THEN_STROKE");
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.FillThenStroke);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, y - 100, "ABCabc123");
        HPDF_Page_EndText(page);

        ShowDescription(page, font, 60, y - 150, "RenderingMode=PDF_FILL_CLIPPING");
        HPDF_Page_GSave(page);
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.FillClipping);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, y - 150, "ABCabc123");
        HPDF_Page_EndText(page);
        ShowStripePattern(page, 60, y - 150);
        HPDF_Page_GRestore(page);

        ShowDescription(page, font, 60, y - 200, "RenderingMode=PDF_STROKE_CLIPPING");
        HPDF_Page_GSave(page);
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.StrokeClipping);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, y - 200, "ABCabc123");
        HPDF_Page_EndText(page);
        ShowStripePattern(page, 60, y - 200);
        HPDF_Page_GRestore(page);

        ShowDescription(page, font, 60, y - 250, "RenderingMode=PDF_FILL_STROKE_CLIPPING");
        HPDF_Page_GSave(page);
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.FillStrokeClipping);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, y - 250, "ABCabc123");
        HPDF_Page_EndText(page);
        ShowStripePattern(page, 60, y - 250);
        HPDF_Page_GRestore(page);

        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.Fill);
        HPDF_Page_SetRGBFill(page, 0, 0, 0);
        HPDF_Page_SetFontAndSize(page, font, 30);

        ShowDescription(page, font, 320, y - 60, "Rotating text");
        var rad = 30.0 / 180 * Math.PI;
        HPDF_Page_BeginText(page);
        HPDF_Page_SetTextMatrix(page, Math.Cos(rad), Math.Sin(rad), -Math.Sin(rad), Math.Cos(rad), 330, y - 60);
        HPDF_Page_ShowText(page, "ABCabc123");
        HPDF_Page_EndText(page);

        ShowDescription(page, font, 320, y - 120, "Skewing text");
        var rad1 = 10.0 / 180 * Math.PI;
        var rad2 = 20.0 / 180 * Math.PI;
        HPDF_Page_BeginText(page);
        HPDF_Page_SetTextMatrix(page, 1, Math.Tan(rad1), Math.Tan(rad2), 1, 320, y - 120);
        HPDF_Page_ShowText(page, "ABCabc123");
        HPDF_Page_EndText(page);

        ShowDescription(page, font, 320, y - 175, "Scaling text X");
        HPDF_Page_BeginText(page);
        HPDF_Page_SetTextMatrix(page, 1.5, 0, 0, 1, 320, y - 175);
        HPDF_Page_ShowText(page, "ABCabc123");
        HPDF_Page_EndText(page);

        ShowDescription(page, font, 320, y - 250, "Scaling text Y");
        HPDF_Page_BeginText(page);
        HPDF_Page_SetTextMatrix(page, 1, 0, 0, 2, 320, y - 250);
        HPDF_Page_ShowText(page, "ABCabc123");
        HPDF_Page_EndText(page);

        HPDF_Page_SetFontAndSize(page, font, 20);
        HPDF_Page_SetRGBFill(page, 0.1, 0.3, 0.1);
        HPDF_Page_SetHorizontalScalling(page, 85);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, 140, sampleText2);
        HPDF_Page_EndText(page);

        HPDF_Page_SetCharSpace(page, 1.5);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, 100, sampleText2);
        HPDF_Page_EndText(page);

        HPDF_Page_SetWordSpace(page, 2.5);
        HPDF_Page_SetTextRise(page, 5);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, 60, 60, sampleText2);
        HPDF_Page_EndText(page);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "text_demo.c managed port",
            pdfPath,
            RequireToken("/BaseFont /Helvetica", 1),
            RequireToken(" Tr\n", 10),
            RequireToken(" Tm\n", 20),
            RequireToken(" Tc\n", 1),
            RequireToken(" Tw\n", 1),
            RequireToken(" Tz\n", 1),
            RequireToken(" Ts\n", 1),
            RequireToken(" rg\n", 4),
            RequireToken(" RG\n", 1));
    }

    private static PdfStructureReport PortTextDemo2(string pdfPath)
    {
        const string sample = "The quick brown fox jumps over the lazy dog.";

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetSize(page, PdfPageSize.A5, PdfPageDirection.Portrait);
        PrintGridSheet(pdf, page);

        var font = HPDF_GetFont(pdf, "Helvetica");
        HPDF_Page_SetTextLeading(page, 20);

        DrawTextRectDemo(page, font, new PdfRect(25, 505, 200, 545), sample, PdfTextAlignment.Left, "HPDF_TALIGN_LEFT");
        DrawTextRectDemo(page, font, new PdfRect(220, 505, 395, 545), sample, PdfTextAlignment.Right, "HPDF_TALIGN_RIGHT");
        DrawTextRectDemo(page, font, new PdfRect(25, 435, 200, 475), sample, PdfTextAlignment.Center, "HPDF_TALIGN_CENTER");
        DrawTextRectDemo(page, font, new PdfRect(220, 435, 395, 475), sample, PdfTextAlignment.Justify, "HPDF_TALIGN_JUSTIFY");

        HPDF_Page_GSave(page);
        HPDF_Page_Concat(page, 1, Math.Tan(5 / 180.0 * Math.PI), Math.Tan(10 / 180.0 * Math.PI), 1, 25, 350);
        DrawTextRectDemo(page, font, new PdfRect(0, 0, 175, 40), sample, PdfTextAlignment.Left, "Skewed coordinate system");
        HPDF_Page_GRestore(page);

        HPDF_Page_GSave(page);
        var radians = 5 / 180.0 * Math.PI;
        HPDF_Page_Concat(page, Math.Cos(radians), Math.Sin(radians), -Math.Sin(radians), Math.Cos(radians), 220, 350);
        DrawTextRectDemo(page, font, new PdfRect(0, 0, 175, 40), sample, PdfTextAlignment.Left, "Rotated coordinate system");
        HPDF_Page_GRestore(page);

        HPDF_Page_SetGrayStroke(page, 0);
        HPDF_Page_Circle(page, 210, 190, 145);
        HPDF_Page_Circle(page, 210, 190, 113);
        HPDF_Page_Stroke(page);

        var circleFont = HPDF_GetFont(pdf, "Courier-Bold");
        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, circleFont, 30);
        var angle = 180.0;
        var step = 360.0 / sample.Length;
        foreach (var ch in sample)
        {
            var textRadians = (angle - 90) / 180.0 * Math.PI;
            var pointRadians = angle / 180.0 * Math.PI;
            var x = 210 + Math.Cos(pointRadians) * 122;
            var y = 190 + Math.Sin(pointRadians) * 122;
            HPDF_Page_SetTextMatrix(page, Math.Cos(textRadians), Math.Sin(textRadians), -Math.Sin(textRadians), Math.Cos(textRadians), x, y);
            HPDF_Page_ShowText(page, ch.ToString());
            angle -= step;
        }

        HPDF_Page_EndText(page);
        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "text_demo2.c managed port",
            pdfPath,
            RequireToken("/MediaBox [0 0 419.528 595.276]", 1),
            RequireToken(" cm\n", 2),
            RequireToken(" re\n", 6),
            RequireToken(" Tm\n", sample.Length + 6),
            RequireToken(" Tj\n", 20),
            RequireToken(" c\n", 8));
    }

    private static PdfStructureReport PortTextAnnotationDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        var font = HPDF_GetFont(pdf, "Helvetica");
        HPDF_Page_SetFontAndSize(page, font, 18);
        HPDF_Page_TextOut(page, 60, HPDF_Page_GetHeight(page) - 70, "text_annotation.c managed port");

        var icons = new[]
        {
            PdfAnnotIcon.Comment,
            PdfAnnotIcon.Key,
            PdfAnnotIcon.Note,
            PdfAnnotIcon.Help,
            PdfAnnotIcon.NewParagraph,
            PdfAnnotIcon.Paragraph,
            PdfAnnotIcon.Insert
        };

        for (var i = 0; i < icons.Length; i++)
        {
            var x = 70 + i * 65;
            var annotation = HPDF_Page_CreateTextAnnot(page, new PdfRect(x, 620, x + 20, 640), $"Annotation with {icons[i]} icon");
            HPDF_TextAnnot_SetIcon(annotation, icons[i]);
            HPDF_TextAnnot_SetOpened(annotation, i == 0);
            HPDF_Annot_SetRGBColor(annotation, new PdfRgbColor(i / 7.0, 0.2, 1.0 - i / 7.0));
        }

        var freeText = HPDF_Page_CreateFreeTextAnnot(page, new PdfRect(70, 540, 260, 590), "FreeText annotation");
        HPDF_Annotation_SetBorderStyle(freeText, PdfAnnotBorderStyle.Dashed, 1, 3, 2);
        HPDF_Page_CreateSquareAnnot(page, new PdfRect(70, 470, 140, 520), "Square annotation");
        HPDF_Page_CreateCircleAnnot(page, new PdfRect(170, 470, 240, 520), "Circle annotation");
        HPDF_Page_CreateHighlightAnnot(page, new PdfRect(70, 420, 220, 440), "Highlight annotation");
        HPDF_Page_CreateUnderlineAnnot(page, new PdfRect(70, 390, 220, 410), "Underline annotation");
        HPDF_Page_CreateSquigglyAnnot(page, new PdfRect(70, 360, 220, 380), "Squiggly annotation");
        HPDF_Page_CreateStrikeOutAnnot(page, new PdfRect(70, 330, 220, 350), "StrikeOut annotation");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "text_annotation.c managed port",
            pdfPath,
            RequireToken("/Annots [", 1),
            RequireToken("/Subtype /Text", 7),
            RequireToken("/Subtype /FreeText", 1),
            RequireToken("/Subtype /Square", 1),
            RequireToken("/Subtype /Circle", 1),
            RequireToken("/Subtype /Highlight", 1),
            RequireToken("/Subtype /Underline", 1),
            RequireToken("/Subtype /Squiggly", 1),
            RequireToken("/Subtype /StrikeOut", 1),
            RequireToken("/Open true", 1),
            RequireToken("/C [", 7),
            RequireToken("/QuadPoints [", 4));
    }

    private static PdfStructureReport PortImageDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetWidth(page, 550);
        HPDF_Page_SetHeight(page, 500);

        var destination = HPDF_Page_CreateDestination(page);
        HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(page), 1);
        HPDF_SetOpenAction(pdf, destination);

        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, font, 20);
        HPDF_Page_MoveTextPos(page, 220, HPDF_Page_GetHeight(page) - 70);
        HPDF_Page_ShowText(page, "ImageDemo");
        HPDF_Page_EndText(page);

        var image = HPDF_LoadPngImageFromFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "basn3p02.png"));
        var image1 = HPDF_LoadPngImageFromFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "basn3p02.png"));
        var image2 = HPDF_LoadPngImageFromFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "basn0g01.png"));
        var image3 = HPDF_LoadPngImageFromFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "maskimage.png"));

        Require(HPDF_Image_Validate(image), "image_demo base PNG failed validation.");
        Require(HPDF_Image_Validate(image1), "image_demo masked PNG failed validation.");
        Require(HPDF_Image_Validate(image2), "image_demo mask PNG failed validation.");
        Require(HPDF_Image_Validate(image3), "image_demo color-mask PNG failed validation.");

        var imageWidth = HPDF_Image_GetWidth(image);
        var imageHeight = HPDF_Image_GetHeight(image);

        HPDF_Page_SetFontAndSize(page, font, 8);
        HPDF_Page_SetLineWidth(page, 0.5);

        var x = 100.0;
        var y = HPDF_Page_GetHeight(page) - 150;

        HPDF_Page_DrawImage(page, image, x, y, imageWidth, imageHeight);
        ShowImageDescription(page, font, x, y, "Actual Size");

        x += 150;
        HPDF_Page_DrawImage(page, image, x, y, imageWidth * 1.5, imageHeight);
        ShowImageDescription(page, font, x, y, "Scalling image (X direction)");

        x += 150;
        HPDF_Page_DrawImage(page, image, x, y, imageWidth, imageHeight * 1.5);
        ShowImageDescription(page, font, x, y, "Scalling image (Y direction)");

        x = 100;
        y -= 120;
        var rad1 = 10.0 / 180 * Math.PI;
        var rad2 = 20.0 / 180 * Math.PI;

        HPDF_Page_GSave(page);
        HPDF_Page_Concat(page, imageWidth, Math.Tan(rad1) * imageWidth, Math.Tan(rad2) * imageHeight, imageHeight, x, y);
        HPDF_Page_ExecuteXObject(page, image);
        HPDF_Page_GRestore(page);
        ShowImageDescription(page, font, x, y, "Skewing image");

        x += 150;
        var rad = 30.0 / 180 * Math.PI;

        HPDF_Page_GSave(page);
        HPDF_Page_Concat(page, imageWidth * Math.Cos(rad), imageWidth * Math.Sin(rad), imageHeight * -Math.Sin(rad), imageHeight * Math.Cos(rad), x, y);
        HPDF_Page_ExecuteXObject(page, image);
        HPDF_Page_GRestore(page);
        ShowImageDescription(page, font, x, y, "Rotating image");

        x += 150;
        HPDF_Image_SetMaskImage(image1, image2);
        HPDF_Page_SetRGBFill(page, 0, 0, 0);
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x - 6, y + 14);
        HPDF_Page_ShowText(page, "MASKMASK");
        HPDF_Page_EndText(page);
        HPDF_Page_DrawImage(page, image1, x - 3, y - 3, imageWidth + 6, imageHeight + 6);
        ShowImageDescription(page, font, x, y, "masked image");

        x = 100;
        y -= 120;
        HPDF_Page_SetRGBFill(page, 0, 0, 0);
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x - 6, y + 14);
        HPDF_Page_ShowText(page, "MASKMASK");
        HPDF_Page_EndText(page);

        HPDF_Image_SetColorMask(image3, 0, 255, 0, 0, 0, 255);
        HPDF_Page_DrawImage(page, image3, x, y, imageWidth, imageHeight);
        ShowImageDescription(page, font, x, y, "Color Mask");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "image_demo.c managed port",
            pdfPath,
            RequireToken("/MediaBox [0 0 550 500]", 1),
            RequireToken("/OpenAction", 1),
            RequireToken("/Subtype /Image", 4),
            RequireToken("/XObject", 1),
            RequireToken("/ImageMask true", 1),
            RequireToken("/Mask ", 2),
            RequireToken("/Mask [0 255 0 0 0 255]", 1),
            RequireToken(" cm\n", 7),
            RequireToken(" Do\n", 7),
            RequireToken("ImageDemo", 1),
            RequireToken("MASKMASK", 2),
            RequireToken("Color Mask", 1));
    }

    private static PdfStructureReport PortImageFixturesDemo(string repoRoot, string outputDir, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(page, font, 14);
        HPDF_Page_TextOut(page, 40, HPDF_Page_GetHeight(page) - 50, "Managed image compatibility fixtures");

        var png = HPDF_LoadPngImageFromFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "basn2c08.png"));
        var jpeg = HPDF_LoadJpegImageFromFile(pdf, Path.Combine(repoRoot, "demo", "images", "rgb.jpg"));
        var rawRgb = new byte[]
        {
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 255
        };
        var raw = HPDF_LoadRawImageFromMem(pdf, rawRgb, 2, 2, PdfColorSpace.DeviceRgb, 8);
        var rawPath = Path.Combine(outputDir, "compat-raw-rgb.bin");
        File.WriteAllBytes(rawPath, rawRgb);
        var rawFromFile = HPDF_LoadRawImageFromFile(pdf, rawPath, 2, 2, PdfColorSpace.DeviceRgb);

        Require(HPDF_Image_Validate(png), "PNG compatibility image failed validation.");
        Require(HPDF_Image_Validate(jpeg), "JPEG compatibility image failed validation.");
        Require(HPDF_Image_Validate(raw), "Raw memory compatibility image failed validation.");
        Require(HPDF_Image_Validate(rawFromFile), "Raw file compatibility image failed validation.");

        HPDF_Page_DrawImage(page, png, 40, HPDF_Page_GetHeight(page) - 170, 100, 100);
        HPDF_Page_DrawImage(page, jpeg, 160, HPDF_Page_GetHeight(page) - 170, 100, 100);
        HPDF_Page_DrawImage(page, raw, 280, HPDF_Page_GetHeight(page) - 170, 100, 100);
        HPDF_Page_DrawImage(page, rawFromFile, 400, HPDF_Page_GetHeight(page) - 170, 100, 100);

        var jpegName = HPDF_Page_GetXObjectName(page, jpeg);
        Require(jpegName.StartsWith("Im", StringComparison.Ordinal), "JPEG image was not assigned an XObject resource name.");
        HPDF_Page_ExecuteXObject(page, jpeg);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "image/png/jpeg/raw compatibility fixtures",
            pdfPath,
            RequireToken("/Subtype /Image", 4),
            RequireToken("/XObject", 1),
            RequireToken("/DCTDecode", 1),
            RequireToken("/ColorSpace /DeviceRGB", 3),
            RequireToken("/BitsPerComponent 8", 4),
            RequireToken(" Do\n", 5));
    }

    private static PdfStructureReport PortJpegDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetWidth(page, 650);
        HPDF_Page_SetHeight(page, 500);

        var destination = HPDF_Page_CreateDestination(page);
        HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(page), 1);
        HPDF_SetOpenAction(pdf, destination);

        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, font, 20);
        HPDF_Page_MoveTextPos(page, 220, HPDF_Page_GetHeight(page) - 70);
        HPDF_Page_ShowText(page, "JpegDemo");
        HPDF_Page_EndText(page);

        HPDF_Page_SetFontAndSize(page, font, 12);
        DrawJpegDemoImage(pdf, page, Path.Combine(repoRoot, "demo", "images", "rgb.jpg"), "rgb.jpg", 70, HPDF_Page_GetHeight(page) - 410, "24bit color image");
        DrawJpegDemoImage(pdf, page, Path.Combine(repoRoot, "demo", "images", "gray.jpg"), "gray.jpg", 340, HPDF_Page_GetHeight(page) - 410, "8bit grayscale image");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "jpeg_demo.c managed port",
            pdfPath,
            RequireToken("/MediaBox [0 0 650 500]", 1),
            RequireToken("/OpenAction", 1),
            RequireToken("/Subtype /Image", 2),
            RequireToken("/DCTDecode", 2),
            RequireToken("/ColorSpace /DeviceRGB", 1),
            RequireToken("/ColorSpace /DeviceGray", 1),
            RequireToken("/BitsPerComponent 8", 2),
            RequireToken(" cm\n", 2),
            RequireToken(" Do\n", 2),
            RequireToken("JpegDemo", 1),
            RequireToken("rgb.jpg", 1),
            RequireToken("gray.jpg", 1));
    }

    private static PdfStructureReport PortJpFontDemo(string repoRoot, string pdfPath)
    {
        const uint pageHeight = 210;
        var fontSpecs = new (string FontName, string EncodingName)[]
        {
            ("MS-Mincho", "90ms-RKSJ-H"),
            ("MS-Mincho,Bold", "90ms-RKSJ-H"),
            ("MS-Mincho,Italic", "90ms-RKSJ-H"),
            ("MS-Mincho,BoldItalic", "90ms-RKSJ-H"),
            ("MS-PMincho", "90msp-RKSJ-H"),
            ("MS-PMincho,Bold", "90msp-RKSJ-H"),
            ("MS-PMincho,Italic", "90msp-RKSJ-H"),
            ("MS-PMincho,BoldItalic", "90msp-RKSJ-H"),
            ("MS-Gothic", "90ms-RKSJ-H"),
            ("MS-Gothic,Bold", "90ms-RKSJ-H"),
            ("MS-Gothic,Italic", "90ms-RKSJ-H"),
            ("MS-Gothic,BoldItalic", "90ms-RKSJ-H"),
            ("MS-PGothic", "90msp-RKSJ-H"),
            ("MS-PGothic,Bold", "90msp-RKSJ-H"),
            ("MS-PGothic,Italic", "90msp-RKSJ-H"),
            ("MS-PGothic,BoldItalic", "90msp-RKSJ-H")
        };
        var sampleText = ReadLatin1Line(Path.Combine(repoRoot, "demo", "mbtext", "sjis.txt"));

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_UseJPEncodings(pdf);
        HPDF_UseJPFonts(pdf);
        HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);

        var titleFont = HPDF_GetFont(pdf, "Helvetica");
        var detailFonts = fontSpecs
            .Select(spec => HPDF_GetFont(pdf, spec.FontName, spec.EncodingName))
            .ToArray();

        var root = HPDF_CreateOutline(pdf, null, "JP font demo");
        HPDF_Outline_SetOpened(root, true);

        for (var i = 0; i < detailFonts.Length; i++)
        {
            var page = HPDF_AddPage(pdf);
            var font = detailFonts[i];
            var outline = HPDF_CreateOutline(pdf, root, HPDF_Font_GetFontName(font));
            var destination = HPDF_Page_CreateDestination(page);
            HPDF_Outline_SetDestination(outline, destination);

            HPDF_Page_SetWidth(page, 720);
            HPDF_Page_SetHeight(page, pageHeight);
            HPDF_Page_SetFontAndSize(page, titleFont, 10);

            HPDF_Page_BeginText(page);
            HPDF_Page_MoveTextPos(page, 10, 190);
            HPDF_Page_ShowText(page, HPDF_Font_GetFontName(font));

            HPDF_Page_SetFontAndSize(page, font, 15);
            HPDF_Page_MoveTextPos(page, 10, -20);
            HPDF_Page_ShowText(page, "abcdefghijklmnopqrstuvwxyz");
            HPDF_Page_MoveTextPos(page, 0, -20);
            HPDF_Page_ShowText(page, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            HPDF_Page_MoveTextPos(page, 0, -20);
            HPDF_Page_ShowText(page, "1234567890");
            HPDF_Page_MoveTextPos(page, 0, -20);

            HPDF_Page_SetFontAndSize(page, font, 10);
            HPDF_Page_ShowText(page, sampleText);
            HPDF_Page_MoveTextPos(page, 0, -18);

            HPDF_Page_SetFontAndSize(page, font, 16);
            HPDF_Page_ShowText(page, sampleText);
            HPDF_Page_MoveTextPos(page, 0, -27);

            HPDF_Page_SetFontAndSize(page, font, 23);
            HPDF_Page_ShowText(page, sampleText);
            HPDF_Page_MoveTextPos(page, 0, -36);

            HPDF_Page_SetFontAndSize(page, font, 30);
            HPDF_Page_ShowText(page, sampleText);
            HPDF_Page_EndText(page);

            HPDF_Page_SetLineWidth(page, 0.5);
            for (var x = 20.0; x <= 20 + (sampleText.Length / 2.0 * 30); x += 30)
            {
                HPDF_Page_MoveTo(page, x, 12);
                HPDF_Page_LineTo(page, x, 10);
                HPDF_Page_Stroke(page);
            }

            HPDF_Page_MoveTo(page, 10, pageHeight - 25);
            HPDF_Page_LineTo(page, 700, pageHeight - 25);
            HPDF_Page_Stroke(page);

            HPDF_Page_MoveTo(page, 10, pageHeight - 85);
            HPDF_Page_LineTo(page, 700, pageHeight - 85);
            HPDF_Page_Stroke(page);

            HPDF_Page_MoveTo(page, 10, 10);
            HPDF_Page_LineTo(page, 700, 10);
            HPDF_Page_Stroke(page);
        }

        HPDF_SaveToFile(pdf, pdfPath);

        var requirements = new List<StructuralRequirement>
        {
            RequireToken("/PageMode /UseOutlines", 1),
            RequireToken("/Outlines", 1),
            RequireToken("/Type /Page\n", fontSpecs.Length),
            RequireToken("/Subtype /Type0", fontSpecs.Length),
            RequireToken("/Subtype /CIDFontType0", fontSpecs.Length),
            RequireToken("/Ordering (Japan1)", fontSpecs.Length),
            RequireToken("/Encoding /90ms-RKSJ-H", 8),
            RequireToken("/Encoding /90msp-RKSJ-H", 8),
            RequireToken("/DescendantFonts [", fontSpecs.Length),
            RequireToken("JP font demo", 1)
        };
        requirements.AddRange(fontSpecs.Select(static spec => RequireToken($"/BaseFont /{spec.FontName}\n", 1)));

        return CheckPdf("jpfont_demo.c managed port", pdfPath, [.. requirements]);
    }

    private static PdfStructureReport PortOutlineDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var pages = Enumerable.Range(1, 3).Select(pageNumber =>
        {
            var page = HPDF_AddPage(pdf);
            HPDF_Page_SetFontAndSize(page, font, 24);
            HPDF_Page_TextOut(page, 70, HPDF_Page_GetHeight(page) - 90, $"Outline page {pageNumber}");
            return page;
        }).ToArray();

        var root = HPDF_CreateOutline(pdf, null, "OutlineRoot");
        HPDF_Outline_SetOpened(root, true);
        for (var i = 0; i < pages.Length; i++)
        {
            var outline = HPDF_CreateOutline(pdf, root, i == 2 ? "ISO8859-2 text sample" : $"page{i + 1}");
            var destination = HPDF_Page_CreateDestination(pages[i]);
            if (i == 0)
                HPDF_Destination_SetFit(destination);
            else if (i == 1)
                HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(pages[i]), 1);
            else
                HPDF_Destination_SetFitH(destination, HPDF_Page_GetHeight(pages[i]));
            HPDF_Outline_SetDestination(outline, destination);
        }

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "outline_demo.c managed port",
            pdfPath,
            RequireToken("/PageMode /UseOutlines", 1),
            RequireToken("/Outlines", 1),
            RequireToken("/Type /Page\n", 3),
            RequireToken("/Dest ", 3),
            RequireToken("OutlineRoot", 1),
            RequireToken("page1", 1),
            RequireToken("page2", 1));
    }

    private static PdfStructureReport PortOutlineDemoJp(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);
        HPDF_UseJPEncodings(pdf);
        HPDF_UseJPFonts(pdf);

        var latinFont = HPDF_GetFont(pdf, "Helvetica");
        var jpFont = HPDF_GetFont(pdf, "MS-Mincho", "90ms-RKSJ-H");
        var pages = Enumerable.Range(1, 3).Select(pageNumber =>
        {
            var page = HPDF_AddPage(pdf);
            HPDF_Page_SetFontAndSize(page, latinFont, 18);
            HPDF_Page_TextOut(page, 70, HPDF_Page_GetHeight(page) - 90, $"outline_demo_jp page {pageNumber}");
            HPDF_Page_SetFontAndSize(page, jpFont, 18);
            HPDF_Page_TextOut(page, 70, HPDF_Page_GetHeight(page) - 125, "JP outline target");
            return page;
        }).ToArray();

        var root = HPDF_CreateOutline(pdf, null, "OutlineRoot");
        HPDF_Outline_SetOpened(root, true);
        var titles = new[] { "page1", "page2", "Japanese outline sample" };
        for (var i = 0; i < pages.Length; i++)
        {
            var outline = HPDF_CreateOutline(pdf, root, titles[i]);
            var destination = HPDF_Page_CreateDestination(pages[i]);
            HPDF_Destination_SetFit(destination);
            HPDF_Outline_SetDestination(outline, destination);
        }

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "outline_demo_jp.c managed port",
            pdfPath,
            RequireToken("/PageMode /UseOutlines", 1),
            RequireToken("/Outlines", 1),
            RequireToken("/Type /Page\n", 3),
            RequireToken("/Dest ", 3),
            RequireToken("/Subtype /Type0", 1),
            RequireToken("/Subtype /CIDFontType0", 1),
            RequireToken("/Encoding /90ms-RKSJ-H", 1),
            RequireToken("/Ordering (Japan1)", 1));
    }

    private static PdfStructureReport PortPdfAConformanceDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var fontName = HPDF_LoadTTFontFromFile(pdf, Path.Combine(repoRoot, "demo", "ttfont", "PenguinAttack.ttf"), embedding: true);
        var font = HPDF_GetFont(pdf, fontName, "WinAnsiEncoding");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetSize(page, PdfPageSize.Letter, PdfPageDirection.Portrait);

        const string text1 = "This PDF should have an attachment named factur-x.xml";
        const string text2 = "and should be PDF-A/3 compliant.";
        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, font, 20);
        var textWidth = HPDF_Page_TextWidth(page, text1);
        HPDF_Page_MoveTextPos(page, (HPDF_Page_GetWidth(page) - textWidth) / 2, HPDF_Page_GetHeight(page) / 2);
        HPDF_Page_ShowText(page, text1);
        HPDF_Page_MoveTextPos(page, (textWidth - HPDF_Page_TextWidth(page, text2)) / 2, -24);
        HPDF_Page_ShowText(page, text2);
        HPDF_Page_EndText(page);

        var attachmentPath = Path.Combine(repoRoot, "demo", "pdf_a", "factur-x.xml");
        var embedded = HPDF_AttachFile(pdf, attachmentPath);
        HPDF_EmbeddedFile_SetAFRelationship(embedded, PdfAFRelationship.Data);
        HPDF_EmbeddedFile_SetName(embedded, "factur-x.xml");
        HPDF_EmbeddedFile_SetDescription(embedded, "Factur-X invoice");
        HPDF_EmbeddedFile_SetSubtype(embedded, "text/xml");
        HPDF_EmbeddedFile_SetSize(embedded, new FileInfo(attachmentPath).Length);
        HPDF_EmbeddedFile_SetCreationDate(embedded, new DateTimeOffset(2024, 1, 20, 17, 10, 30, TimeSpan.FromHours(1)));
        HPDF_EmbeddedFile_SetLastModificationDate(embedded, new DateTimeOffset(2024, 2, 5, 11, 14, 45, TimeSpan.FromHours(1)));

        HPDF_SetInfoAttr(pdf, PdfInfoType.Title, "PDF-A Title");
        HPDF_SetInfoAttr(pdf, PdfInfoType.Subject, "PDF-A Subject");
        HPDF_SetInfoAttr(pdf, PdfInfoType.Author, "PDF-A Author");
        HPDF_SetInfoAttr(pdf, PdfInfoType.Creator, "libharu");
        HPDF_PDFA_SetPDFAConformance(pdf, PdfPdfAType.PdfA3B);
        HPDF_AppendOutputIntents(pdf, "sRGB", File.ReadAllBytes(Path.Combine(repoRoot, "demo", "pdf_a", "device_rgb.icc")), "sRGB IEC61966-2.1");
        HPDF_SetXmpMetadata(pdf, CreatePdfAXmpExtensionMetadata());

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "pdf_a_conformance.c managed port",
            pdfPath,
            RequireToken("/Metadata", 1),
            RequireToken("/Subtype /XML", 1),
            RequireToken("pdfaid:part>3", 1),
            RequireToken("pdfaid:conformance>B", 1),
            RequireToken("/OutputIntents [", 1),
            RequireToken("/S /GTS_PDFA1", 1),
            RequireToken("/Type /Filespec", 1),
            RequireToken("/AFRelationship /Data", 1),
            RequireToken("factur-x.xml", 2),
            RequireToken("/FontFile2", 1));
    }

    private static PdfStructureReport PortPngDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetWidth(page, 550);
        HPDF_Page_SetHeight(page, 500);

        DrawPngDemoImage(pdf, page, font, Path.Combine(repoRoot, "demo", "pngsuite", "basn3p02.png"), "basn3p02.png", 100, 300);
        DrawPngDemoImage(pdf, page, font, Path.Combine(repoRoot, "demo", "pngsuite", "basn0g08.png"), "basn0g08.png", 260, 300);
        DrawPngDemoImage(pdf, page, font, Path.Combine(repoRoot, "demo", "pngsuite", "basn6a08.png"), "basn6a08.png", 100, 140);
        DrawPngDemoImage(pdf, page, font, Path.Combine(repoRoot, "demo", "pngsuite", "maskimage.png"), "maskimage.png", 260, 140);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "png_demo.c managed port",
            pdfPath,
            RequireToken("/Subtype /Image", 5),
            RequireToken("/Indexed", 1),
            RequireToken("/CalGray", 1),
            RequireToken("/CalRGB", 1),
            RequireToken("/ColorSpace /DeviceGray", 1),
            RequireToken("/ColorSpace /DeviceRGB", 1),
            RequireToken("/SMask", 1),
            RequireToken("/XObject <<", 1),
            RequireToken(" Do\n", 4));
    }

    private static PdfStructureReport PortRawImageDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetWidth(page, 400);
        HPDF_Page_SetHeight(page, 220);

        var rgb = HPDF_LoadRawImageFromFile(pdf, Path.Combine(repoRoot, "demo", "rawimage", "32_32_rgb.dat"), 32, 32, PdfColorSpace.DeviceRgb);
        var gray = HPDF_LoadRawImageFromFile(pdf, Path.Combine(repoRoot, "demo", "rawimage", "32_32_gray.dat"), 32, 32, PdfColorSpace.DeviceGray);
        var twoColor = HPDF_LoadRawImageFromMem(pdf, File.ReadAllBytes(Path.Combine(repoRoot, "demo", "rawimage", "32_32_2color.dat")), 32, 32, PdfColorSpace.DeviceGray, 1);
        HPDF_Page_DrawImage(page, rgb, 60, 120, 64, 64);
        HPDF_Page_DrawImage(page, gray, 160, 120, 64, 64);
        HPDF_Page_DrawImage(page, twoColor, 260, 120, 64, 64);

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "raw_image_demo.c managed port",
            pdfPath,
            RequireToken("/Subtype /Image", 3),
            RequireToken("/ColorSpace /DeviceRGB", 1),
            RequireToken("/ColorSpace /DeviceGray", 2),
            RequireToken("/BitsPerComponent 8", 2),
            RequireToken("/BitsPerComponent 1", 1),
            RequireToken(" Do\n", 3));
    }

    private static PdfStructureReport PortSlideShowDemo(string pdfPath)
    {
        var transitions = new[]
        {
            ("HPDF_TS_WIPE_RIGHT", PdfTransitionStyle.WipeRight),
            ("HPDF_TS_WIPE_UP", PdfTransitionStyle.WipeUp),
            ("HPDF_TS_WIPE_LEFT", PdfTransitionStyle.WipeLeft),
            ("HPDF_TS_WIPE_DOWN", PdfTransitionStyle.WipeDown),
            ("HPDF_TS_BARN_DOORS_HORIZONTAL_OUT", PdfTransitionStyle.BarnDoorsHorizontalOut),
            ("HPDF_TS_BARN_DOORS_HORIZONTAL_IN", PdfTransitionStyle.BarnDoorsHorizontalIn),
            ("HPDF_TS_BARN_DOORS_VERTICAL_OUT", PdfTransitionStyle.BarnDoorsVerticalOut),
            ("HPDF_TS_BARN_DOORS_VERTICAL_IN", PdfTransitionStyle.BarnDoorsVerticalIn),
            ("HPDF_TS_BOX_OUT", PdfTransitionStyle.BoxOut),
            ("HPDF_TS_BOX_IN", PdfTransitionStyle.BoxIn),
            ("HPDF_TS_BLINDS_HORIZONTAL", PdfTransitionStyle.BlindsHorizontal),
            ("HPDF_TS_BLINDS_VERTICAL", PdfTransitionStyle.BlindsVertical),
            ("HPDF_TS_DISSOLVE", PdfTransitionStyle.Dissolve),
            ("HPDF_TS_GLITTER_RIGHT", PdfTransitionStyle.GlitterRight),
            ("HPDF_TS_GLITTER_DOWN", PdfTransitionStyle.GlitterDown),
            ("HPDF_TS_GLITTER_TOP_LEFT_TO_BOTTOM_RIGHT", PdfTransitionStyle.GlitterTopLeftToBottomRight),
            ("HPDF_TS_REPLACE", PdfTransitionStyle.Replace)
        };

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.FullScreen);

        var font = HPDF_GetFont(pdf, "Courier");
        var pages = transitions.Select(_ => HPDF_AddPage(pdf)).ToArray();
        for (var i = 0; i < pages.Length; i++)
        {
            DrawSlideShowPage(pages[i], transitions[i].Item1, transitions[i].Item2, font, i > 0 ? pages[i - 1] : null, i < pages.Length - 1 ? pages[i + 1] : null);
        }

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "slide_show_demo.c managed port",
            pdfPath,
            RequireToken("/PageMode /FullScreen", 1),
            RequireToken("/Type /Page\n", transitions.Length),
            RequireToken("/Trans <<", transitions.Length),
            RequireToken("/Dur 5", transitions.Length),
            RequireToken("/S /Wipe", 4),
            RequireToken("/S /Split", 4),
            RequireToken("/S /Box", 2),
            RequireToken("/S /Blinds", 2),
            RequireToken("/S /Dissolve", 1),
            RequireToken("/S /Glitter", 3),
            RequireToken("/S /R", 1),
            RequireToken("/Subtype /Link", 32));
    }

    private static PdfStructureReport PortTtFontDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        var titleFont = HPDF_GetFont(pdf, "Helvetica");
        var fontName = HPDF_LoadTTFontFromFile(pdf, Path.Combine(repoRoot, "demo", "ttfont", "PenguinAttack.ttf"), embedding: true);
        var detailFont = HPDF_GetFont(pdf, fontName);
        DrawTtFontSample(page, titleFont, detailFont, fontName, "The quick brown fox jumps over the lazy dog.", "Embedded Subset");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "ttfont_demo.c managed port",
            pdfPath,
            RequireToken("/Subtype /TrueType", 1),
            RequireToken("/FontFile2", 1),
            RequireToken("/BaseFont /PenguinAttack", 1),
            RequireToken("/Widths [", 1),
            RequireToken("Embedded Subset", 1));
    }

    private static PdfStructureReport PortTtFontDemoJp(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_UseJPEncodings(pdf);

        var page = HPDF_AddPage(pdf);
        var titleFont = HPDF_GetFont(pdf, "Helvetica");
        var fontName = HPDF_LoadTTFontFromFile(pdf, Path.Combine(repoRoot, "demo", "ttfont", "PenguinAttack.ttf"), embedding: true);
        var detailFont = HPDF_GetFont(pdf, fontName, "90msp-RKSJ-H");
        DrawTtFontSample(page, titleFont, detailFont, fontName, ReadLatin1Line(Path.Combine(repoRoot, "demo", "mbtext", "sjis.txt")), "90msp-RKSJ-H");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "ttfont_demo_jp.c managed port",
            pdfPath,
            RequireToken("/Subtype /Type0", 1),
            RequireToken("/Subtype /CIDFontType2", 1),
            RequireToken("/Encoding /90msp-RKSJ-H", 1),
            RequireToken("/CIDToGIDMap", 1),
            RequireToken("/FontFile2", 1),
            RequireToken("/ToUnicode", 1),
            RequireToken("<82A0> <3042>", 1));
    }

    private static PdfStructureReport PortDocumentDemo(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);
        HPDF_SetPageMode(pdf, PdfPageMode.UseAttachments);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetSize(page, PdfPageSize.Letter, PdfPageDirection.Portrait);
        HPDF_Page_SetFontAndSize(page, font, 18);
        HPDF_Page_TextOut(page, 72, HPDF_Page_GetHeight(page) - 90, "Managed attach/link/outline demo port");

        var targetPage = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(targetPage, font, 18);
        HPDF_Page_TextOut(targetPage, 72, HPDF_Page_GetHeight(targetPage) - 90, "Outline and link destination");
        var destination = HPDF_Page_CreateDestination(targetPage);
        HPDF_Destination_SetFit(destination);
        HPDF_SetOpenAction(pdf, destination);
        HPDF_AddNamedDestination(pdf, "compat-target", destination);

        var root = HPDF_CreateOutline(pdf, null, "Managed compatibility demos");
        HPDF_Outline_SetOpened(root, true);
        var child = HPDF_CreateOutline(pdf, root, "Attach/link target");
        HPDF_Outline_SetDestination(child, destination);

        var link = HPDF_Page_CreateLinkAnnot(page, new PdfRect(70, 690, 260, 720), destination);
        HPDF_LinkAnnot_SetHighlightMode(link, PdfAnnotHighlightMode.InvertBorder);
        HPDF_LinkAnnot_SetBorderStyle(link, 1, 3, 2);
        HPDF_Page_CreateURILinkAnnot(page, new PdfRect(70, 650, 260, 680), "https://libharu.org");

        var embedded = HPDF_AttachFile(pdf, Path.Combine(repoRoot, "demo", "pngsuite", "basn3p08.png"));
        HPDF_EmbeddedFile_SetName(embedded, "basn3p08.png");
        HPDF_EmbeddedFile_SetDescription(embedded, "Compatibility attachment fixture");

        HPDF_SaveToFile(pdf, pdfPath);

        return CheckPdf(
            "attach/link/outline demos managed port",
            pdfPath,
            RequireToken("/PageMode /UseAttachments", 1),
            RequireToken("/OpenAction", 1),
            RequireToken("/Outlines", 1),
            RequireToken("/Names", 1),
            RequireToken("/Dests", 1),
            RequireToken("/EmbeddedFiles", 1),
            RequireToken("/Type /Filespec", 1),
            RequireToken("/Annots [", 1),
            RequireToken("/Subtype /Link", 2),
            RequireToken("/S /URI", 1),
            RequireToken("/BS <<", 1),
            RequireToken("/S /D", 1));
    }

    private static PdfStructureReport PortPermissionDemo(string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        const string text = "User cannot print and copy this document.";
        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetSize(page, PdfPageSize.B5, PdfPageDirection.Landscape);
        HPDF_Page_BeginText(page);
        HPDF_Page_SetFontAndSize(page, font, 20);
        var width = HPDF_Page_TextWidth(page, text);
        HPDF_Page_MoveTextPos(page, (HPDF_Page_GetWidth(page) - width) / 2, (HPDF_Page_GetHeight(page) - 20) / 2);
        HPDF_Page_ShowText(page, text);
        HPDF_Page_EndText(page);

        HPDF_SetPassword(pdf, "owner", "");
        HPDF_SetPermission(pdf, Permission.EnableRead);
        HPDF_SetEncryptionMode(pdf, PdfEncryptMode.R3, 16);
        HPDF_SaveToFile(pdf, pdfPath);

        var report = CheckPdf(
            "permission.c managed port",
            pdfPath,
            RequireToken("/Encrypt", 1),
            RequireToken("/Filter /Standard", 1),
            RequireToken("/R 3", 1),
            RequireToken("/Length 128", 1),
            RequireToken("/P 0", 1),
            RequireToken("/ID [<", 1));

        var latin1 = Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));
        Require(!latin1.Contains(text, StringComparison.Ordinal), "Encrypted permission demo leaked plaintext page content.");
        return report;
    }

    private static string BuildDemoInventory(string repoRoot, IReadOnlyCollection<PdfStructureReport> reports)
    {
        var ported = new HashSet<string>(StringComparer.Ordinal)
        {
            "arc_demo.c",
            "attach.c",
            "character_map.c",
            "chfont_demo.c",
            "encoding_list.c",
            "encryption.c",
            "ext_gstate_demo.c",
            "font_demo.c",
            "grid_sheet.c",
            "image_demo.c",
            "jpeg_demo.c",
            "jpfont_demo.c",
            "line_demo.c",
            "link_annotation.c",
            "outline_demo.c",
            "outline_demo_jp.c",
            "pdf_a_conformance.c",
            "permission.c",
            "png_demo.c",
            "raw_image_demo.c",
            "slide_show_demo.c",
            "text_annotation.c",
            "text_demo.c",
            "text_demo2.c",
            "ttfont_demo.c",
            "ttfont_demo_jp.c"
        };

        var demoDir = Path.Combine(repoRoot, "demo");
        var sources = Directory
            .EnumerateFiles(demoDir, "*.*", SearchOption.TopDirectoryOnly)
            .Where(static path => path.EndsWith(".c", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Select(static name => name!)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        var lines = new List<string>
        {
            "libharu demo inventory",
            $"source directory: {demoDir}",
            $"managed compatibility PDFs: {reports.Count}",
            string.Empty,
            "sources:"
        };

        foreach (var source in sources)
        {
            var status = source switch
            {
                "make_rawimage.c" => "utility fixture generator; no PDF smoke port",
                "font_demo.cpp" => "C++ wrapper sample; font_demo.c covers the managed PDF smoke port",
                _ when ported.Contains(source) => "managed PDF smoke port",
                _ => "missing managed PDF smoke port"
            };
            lines.Add($"- {source}: {status}");
        }

        lines.Add(string.Empty);
        lines.Add("generated PDFs:");
        lines.AddRange(reports.Select(static report => $"- {Path.GetFileName(report.Path)}: {report.Demo}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static void DrawCMapPage(PdfPage page, PdfFont titleFont, PdfFont font, byte highByte, byte lowFrom, byte lowTo, string label)
    {
        const int cellWidth = 20;
        const int cellHeight = 20;

        HPDF_Page_SetFontAndSize(page, titleFont, 10);
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 40, HPDF_Page_GetHeight(page) - 35);
        HPDF_Page_ShowText(page, $"{label} 0x{highByte:X2}{lowFrom:X2}-0x{highByte:X2}{lowTo:X2}");
        HPDF_Page_EndText(page);

        for (var row = 0; row <= 16; row++)
        {
            var y = 60 + row * cellHeight;
            HPDF_Page_MoveTo(page, 40, y);
            HPDF_Page_LineTo(page, 380, y);
            HPDF_Page_Stroke(page);
        }

        for (var col = 0; col <= 17; col++)
        {
            var x = 40 + col * cellWidth;
            HPDF_Page_MoveTo(page, x, 60);
            HPDF_Page_LineTo(page, x, 380);
            HPDF_Page_Stroke(page);
        }

        HPDF_Page_SetFontAndSize(page, titleFont, 8);
        for (var i = 0; i < 16; i++)
        {
            HPDF_Page_TextOut(page, 67 + i * cellWidth, 365, i.ToString("X", System.Globalization.CultureInfo.InvariantCulture));
            HPDF_Page_TextOut(page, 47, 345 - i * cellHeight, i.ToString("X", System.Globalization.CultureInfo.InvariantCulture));
        }

        HPDF_Page_SetFontAndSize(page, font, 14);
        for (var low = lowFrom; low <= lowTo; low++)
        {
            var nibble = low & 0x0F;
            var row = (low - lowFrom) / 16;
            var x = 62 + nibble * cellWidth;
            var y = 340 - row * cellHeight;
            var text = Encoding.Latin1.GetString([(byte)highByte, (byte)low]);
            HPDF_Page_TextOut(page, x, y, text);
        }
    }

    private static void DrawTextRectDemo(PdfPage page, PdfFont font, PdfRect rect, string text, PdfTextAlignment alignment, string label)
    {
        HPDF_Page_Rectangle(page, rect.Left, rect.Bottom, rect.Right - rect.Left, rect.Top - rect.Bottom);
        HPDF_Page_Stroke(page);

        HPDF_Page_SetFontAndSize(page, font, 10);
        HPDF_Page_TextOut(page, rect.Left, rect.Top + 3, label);

        HPDF_Page_SetFontAndSize(page, font, 13);
        DrawWrappedLine(page, rect, text, alignment);
    }

    private static void DrawWrappedLine(PdfPage page, PdfRect rect, string text, PdfTextAlignment alignment)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;
        var maxWidth = rect.Right - rect.Left - 8;

        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (current.Length > 0 && HPDF_Page_TextWidth(page, candidate) > maxWidth)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (current.Length > 0)
            lines.Add(current);

        var y = rect.Top - 16;
        foreach (var line in lines.Take(2))
        {
            var lineWidth = HPDF_Page_TextWidth(page, line);
            var x = alignment switch
            {
                PdfTextAlignment.Right => rect.Right - lineWidth - 4,
                PdfTextAlignment.Center => rect.Left + (rect.Right - rect.Left - lineWidth) / 2,
                _ => rect.Left + 4
            };

            if (alignment == PdfTextAlignment.Justify)
                HPDF_Page_SetWordSpace(page, 3);

            HPDF_Page_TextOut(page, x, y, line);

            if (alignment == PdfTextAlignment.Justify)
                HPDF_Page_SetWordSpace(page, 0);

            y -= 16;
        }
    }

    private static void DrawPngDemoImage(PdfDocument pdf, PdfPage page, PdfFont font, string path, string fileName, double x, double y)
    {
        var image = HPDF_LoadPngImageFromFile(pdf, path);
        Require(HPDF_Image_Validate(image), $"{fileName} failed PNG validation.");
        HPDF_Page_DrawImage(page, image, x, y, 96, 96);
        HPDF_Page_SetFontAndSize(page, font, 10);
        HPDF_Page_TextOut(page, x, y - 16, fileName);
        HPDF_Page_TextOut(page, x, y - 30, HPDF_Image_GetColorSpace(image));
    }

    private static void DrawSlideShowPage(PdfPage page, string caption, PdfTransitionStyle style, PdfFont font, PdfPage? previous, PdfPage? next)
    {
        HPDF_Page_SetWidth(page, 800);
        HPDF_Page_SetHeight(page, 600);
        var index = (int)style;
        var r = ((index * 47) % 100) / 100.0;
        var g = ((index * 71 + 20) % 100) / 100.0;
        var b = ((index * 29 + 40) % 100) / 100.0;

        HPDF_Page_SetRGBFill(page, r, g, b);
        HPDF_Page_Rectangle(page, 0, 0, 800, 600);
        HPDF_Page_Fill(page);

        HPDF_Page_SetRGBFill(page, 1.0 - r, 1.0 - g, 1.0 - b);
        HPDF_Page_SetFontAndSize(page, font, 30);
        HPDF_Page_TextOut(page, 50, 530, caption);
        HPDF_Page_SetFontAndSize(page, font, 20);
        HPDF_Page_TextOut(page, 55, 300, "Type \"Ctrl+L\" in order to return from full screen mode.");
        HPDF_Page_SetSlideShow(page, style, 5.0, 1.0);

        if (next is not null)
        {
            HPDF_Page_TextOut(page, 680, 50, "Next=>");
            var destination = HPDF_Page_CreateDestination(next);
            HPDF_Destination_SetFit(destination);
            var annotation = HPDF_Page_CreateLinkAnnot(page, new PdfRect(680, 50, 750, 70), destination);
            HPDF_LinkAnnot_SetBorderStyle(annotation, 0, 0, 0);
            HPDF_LinkAnnot_SetHighlightMode(annotation, PdfAnnotHighlightMode.InvertBox);
        }

        if (previous is not null)
        {
            HPDF_Page_TextOut(page, 50, 50, "<=Prev");
            var destination = HPDF_Page_CreateDestination(previous);
            HPDF_Destination_SetFit(destination);
            var annotation = HPDF_Page_CreateLinkAnnot(page, new PdfRect(50, 50, 110, 70), destination);
            HPDF_LinkAnnot_SetBorderStyle(annotation, 0, 0, 0);
            HPDF_LinkAnnot_SetHighlightMode(annotation, PdfAnnotHighlightMode.InvertBox);
        }
    }

    private static void DrawTtFontSample(PdfPage page, PdfFont titleFont, PdfFont detailFont, string fontName, string sampleText, string suffix)
    {
        HPDF_Page_SetFontAndSize(page, titleFont, 10);
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 10, 190);
        HPDF_Page_ShowText(page, fontName);
        HPDF_Page_ShowText(page, $" ({suffix})");

        HPDF_Page_SetFontAndSize(page, detailFont, 15);
        HPDF_Page_MoveTextPos(page, 10, -20);
        HPDF_Page_ShowText(page, "abcdefghijklmnopqrstuvwxyz");
        HPDF_Page_MoveTextPos(page, 0, -20);
        HPDF_Page_ShowText(page, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        HPDF_Page_MoveTextPos(page, 0, -20);
        HPDF_Page_ShowText(page, "1234567890");
        HPDF_Page_MoveTextPos(page, 0, -20);

        foreach (var size in new[] { 10, 16, 23, 30 })
        {
            HPDF_Page_SetFontAndSize(page, detailFont, size);
            HPDF_Page_ShowText(page, sampleText);
            HPDF_Page_MoveTextPos(page, 0, size == 30 ? -36 : -size - 8);
        }

        HPDF_Page_EndText(page);

        var pageWidth = Math.Max(260, HPDF_Page_TextWidth(page, sampleText) + 40);
        HPDF_Page_SetWidth(page, pageWidth);
        HPDF_Page_SetHeight(page, 210);
        HPDF_Page_SetLineWidth(page, 0.5);
        HPDF_Page_MoveTo(page, 10, 185);
        HPDF_Page_LineTo(page, pageWidth - 10, 185);
        HPDF_Page_Stroke(page);
        HPDF_Page_MoveTo(page, 10, 125);
        HPDF_Page_LineTo(page, pageWidth - 10, 125);
        HPDF_Page_Stroke(page);
    }

    private static string[] ReadLatin1Lines(string path, int count)
    {
        var text = Encoding.Latin1.GetString(File.ReadAllBytes(path));
        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Take(count)
            .ToArray();
    }

    private static string CreatePdfAXmpExtensionMetadata() =>
        """
        <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
            <rdf:Description rdf:about="" xmlns:pdfaid="http://www.aiim.org/pdfa/ns/id/">
              <pdfaid:part>3</pdfaid:part>
              <pdfaid:conformance>B</pdfaid:conformance>
            </rdf:Description>
            <rdf:Description rdf:about="" xmlns:fx="urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#">
              <fx:DocumentType>INVOICE</fx:DocumentType>
              <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>
              <fx:Version>1.0</fx:Version>
              <fx:ConformanceLevel>EN 16931</fx:ConformanceLevel>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        <?xpacket end="w"?>
        """;

    private static void DrawLine(PdfPage page, double x, double y, string label)
    {
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x, y - 10);
        HPDF_Page_ShowText(page, label);
        HPDF_Page_EndText(page);

        HPDF_Page_MoveTo(page, x, y - 15);
        HPDF_Page_LineTo(page, x + 220, y - 15);
        HPDF_Page_Stroke(page);
    }

    private static void DrawLine2(PdfPage page, double x, double y, string label)
    {
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x, y);
        HPDF_Page_ShowText(page, label);
        HPDF_Page_EndText(page);

        HPDF_Page_MoveTo(page, x + 30, y - 25);
        HPDF_Page_LineTo(page, x + 160, y - 25);
        HPDF_Page_Stroke(page);
    }

    private static void DrawJoin(PdfPage page, PdfLineJoin join, double x, double y, string label)
    {
        HPDF_Page_SetLineJoin(page, join);
        HPDF_Page_MoveTo(page, x, y);
        HPDF_Page_LineTo(page, x + 40, y + 40);
        HPDF_Page_LineTo(page, x + 80, y);
        HPDF_Page_Stroke(page);

        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, 60, y + 60);
        HPDF_Page_ShowText(page, label);
        HPDF_Page_EndText(page);
    }

    private static void DrawRect(PdfPage page, double x, double y, string label)
    {
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x, y - 10);
        HPDF_Page_ShowText(page, label);
        HPDF_Page_EndText(page);

        HPDF_Page_Rectangle(page, x, y - 40, 220, 25);
    }

    private static void DrawBlendModeGroup(PdfDocument pdf, PdfPage page, PdfBlendMode mode, string description, double x, double y)
    {
        HPDF_Page_GSave(page);
        var state = HPDF_CreateExtGState(pdf);
        HPDF_ExtGState_SetBlendMode(state, mode);
        HPDF_Page_SetExtGState(page, state);
        DrawExtGStateCircles(page, description, x, y);
        HPDF_Page_GRestore(page);
    }

    private static void DrawExtGStateCircles(PdfPage page, string description, double x, double y)
    {
        HPDF_Page_SetLineWidth(page, 1);
        HPDF_Page_SetRGBStroke(page, 0, 0, 0);

        HPDF_Page_SetRGBFill(page, 1, 0, 0);
        HPDF_Page_Circle(page, x + 40, y + 40, 40);
        HPDF_Page_ClosePathFillStroke(page);

        HPDF_Page_SetRGBFill(page, 0, 1, 0);
        HPDF_Page_Circle(page, x + 100, y + 40, 40);
        HPDF_Page_ClosePathFillStroke(page);

        HPDF_Page_SetRGBFill(page, 0, 0, 1);
        HPDF_Page_Circle(page, x + 70, y + 74.64, 40);
        HPDF_Page_ClosePathFillStroke(page);

        HPDF_Page_SetRGBFill(page, 0, 0, 0);
        HPDF_Page_BeginText(page);
        HPDF_Page_TextOut(page, x, y + 130, description);
        HPDF_Page_EndText(page);
    }

    private static void ShowImageDescription(PdfPage page, PdfFont font, double x, double y, string text)
    {
        HPDF_Page_MoveTo(page, x, y - 10);
        HPDF_Page_LineTo(page, x, y + 10);
        HPDF_Page_MoveTo(page, x - 10, y);
        HPDF_Page_LineTo(page, x + 10, y);
        HPDF_Page_Stroke(page);

        HPDF_Page_SetFontAndSize(page, font, 8);
        HPDF_Page_SetRGBFill(page, 0, 0, 0);

        var coordinates = $"(x={(int)x},y={(int)y})";
        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x - HPDF_Page_TextWidth(page, coordinates) - 5, y - 10);
        HPDF_Page_ShowText(page, coordinates);
        HPDF_Page_EndText(page);

        HPDF_Page_BeginText(page);
        HPDF_Page_MoveTextPos(page, x - 20, y - 25);
        HPDF_Page_ShowText(page, text);
        HPDF_Page_EndText(page);
    }

    private static void DrawJpegDemoImage(PdfDocument pdf, PdfPage page, string path, string fileName, double x, double y, string text)
    {
        var image = HPDF_LoadJpegImageFromFile(pdf, path);
        Require(HPDF_Image_Validate(image), $"{fileName} failed JPEG validation.");

        HPDF_Page_DrawImage(page, image, x, y, HPDF_Image_GetWidth(image), HPDF_Image_GetHeight(image));

        HPDF_Page_BeginText(page);
        HPDF_Page_SetTextLeading(page, 16);
        HPDF_Page_MoveTextPos(page, x, y);
        HPDF_Page_ShowTextNextLine(page, fileName);
        HPDF_Page_ShowTextNextLine(page, text);
        HPDF_Page_EndText(page);
    }

    private static string ReadLatin1Line(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var length = Array.FindIndex(bytes, static value => value is (byte)'\r' or (byte)'\n');
        if (length < 0)
            length = bytes.Length;

        return Encoding.Latin1.GetString(bytes, 0, length);
    }

    private static void PrintGridSheet(PdfDocument pdf, PdfPage page)
    {
        var height = HPDF_Page_GetHeight(page);
        var width = HPDF_Page_GetWidth(page);
        var font = HPDF_GetFont(pdf, "Helvetica");

        HPDF_Page_SetFontAndSize(page, font, 5);
        HPDF_Page_SetGrayFill(page, 0.5);
        HPDF_Page_SetGrayStroke(page, 0.8);

        for (var y = 0; y < height; y += 5)
        {
            if (y % 10 == 0)
            {
                HPDF_Page_SetLineWidth(page, 0.5);
            }
            else if (HPDF_Page_GetLineWidth(page) != 0.25)
            {
                HPDF_Page_SetLineWidth(page, 0.25);
            }

            HPDF_Page_MoveTo(page, 0, y);
            HPDF_Page_LineTo(page, width, y);
            HPDF_Page_Stroke(page);

            if (y % 10 == 0 && y > 0)
            {
                HPDF_Page_SetGrayStroke(page, 0.5);
                HPDF_Page_MoveTo(page, 0, y);
                HPDF_Page_LineTo(page, 5, y);
                HPDF_Page_Stroke(page);
                HPDF_Page_SetGrayStroke(page, 0.8);
            }
        }

        for (var x = 0; x < width; x += 5)
        {
            if (x % 10 == 0)
            {
                HPDF_Page_SetLineWidth(page, 0.5);
            }
            else if (HPDF_Page_GetLineWidth(page) != 0.25)
            {
                HPDF_Page_SetLineWidth(page, 0.25);
            }

            HPDF_Page_MoveTo(page, x, 0);
            HPDF_Page_LineTo(page, x, height);
            HPDF_Page_Stroke(page);

            if (x % 50 == 0 && x > 0)
            {
                HPDF_Page_SetGrayStroke(page, 0.5);
                HPDF_Page_MoveTo(page, x, 0);
                HPDF_Page_LineTo(page, x, 5);
                HPDF_Page_Stroke(page);
                HPDF_Page_MoveTo(page, x, height);
                HPDF_Page_LineTo(page, x, height - 5);
                HPDF_Page_Stroke(page);
                HPDF_Page_SetGrayStroke(page, 0.8);
            }
        }

        for (var y = 0; y < height; y += 5)
        {
            if (y % 10 != 0 || y <= 0)
                continue;

            HPDF_Page_BeginText(page);
            HPDF_Page_MoveTextPos(page, 5, y - 2);
            HPDF_Page_ShowText(page, y.ToString(System.Globalization.CultureInfo.InvariantCulture));
            HPDF_Page_EndText(page);
        }

        for (var x = 0; x < width; x += 5)
        {
            if (x % 50 != 0 || x <= 0)
                continue;

            var text = x.ToString(System.Globalization.CultureInfo.InvariantCulture);
            HPDF_Page_BeginText(page);
            HPDF_Page_MoveTextPos(page, x, 5);
            HPDF_Page_ShowText(page, text);
            HPDF_Page_EndText(page);

            HPDF_Page_BeginText(page);
            HPDF_Page_MoveTextPos(page, x, height - 10);
            HPDF_Page_ShowText(page, text);
            HPDF_Page_EndText(page);
        }

        HPDF_Page_SetGrayFill(page, 0);
        HPDF_Page_SetGrayStroke(page, 0);
    }

    private static void DrawEncodingGraph(PdfPage page)
    {
        const int pageWidth = 420;
        const int pageHeight = 400;
        const int cellWidth = 20;
        const int cellHeight = 20;

        HPDF_Page_SetLineWidth(page, 0.5);

        for (var i = 0; i <= 17; i++)
        {
            var x = i * cellWidth + 40;

            HPDF_Page_MoveTo(page, x, pageHeight - 60);
            HPDF_Page_LineTo(page, x, 40);
            HPDF_Page_Stroke(page);

            if (i is > 0 and <= 16)
            {
                HPDF_Page_BeginText(page);
                HPDF_Page_MoveTextPos(page, x + 5, pageHeight - 75);
                HPDF_Page_ShowText(page, (i - 1).ToString("X", System.Globalization.CultureInfo.InvariantCulture));
                HPDF_Page_EndText(page);
            }
        }

        for (var i = 0; i <= 15; i++)
        {
            var y = i * cellHeight + 40;

            HPDF_Page_MoveTo(page, 40, y);
            HPDF_Page_LineTo(page, pageWidth - 40, y);
            HPDF_Page_Stroke(page);

            if (i < 14)
            {
                HPDF_Page_BeginText(page);
                HPDF_Page_MoveTextPos(page, 45, y + 5);
                HPDF_Page_ShowText(page, (15 - i).ToString("X", System.Globalization.CultureInfo.InvariantCulture));
                HPDF_Page_EndText(page);
            }
        }
    }

    private static void DrawEncodingFonts(PdfPage page)
    {
        const int pageHeight = 400;
        const int cellWidth = 20;
        const int cellHeight = 20;

        HPDF_Page_BeginText(page);

        for (var i = 1; i < 17; i++)
        {
            for (var j = 1; j < 17; j++)
            {
                var code = (i - 1) * 16 + (j - 1);
                if (code < 32)
                    continue;

                var text = new string((char)code, 1);
                var y = pageHeight - 55 - ((i - 1) * cellHeight);
                var x = j * cellWidth + 50;
                var centeredX = x - HPDF_Page_TextWidth(page, text) / 2;
                HPDF_Page_TextOut(page, centeredX, y, text);
            }
        }

        HPDF_Page_EndText(page);
    }

    private static void DrawGrid(PdfPage page)
    {
        HPDF_Page_SetLineWidth(page, 0.25);
        HPDF_Page_SetRGBStroke(page, 0.85, 0.85, 0.85);

        for (var x = 0.0; x <= HPDF_Page_GetWidth(page); x += 50)
        {
            HPDF_Page_MoveTo(page, x, 0);
            HPDF_Page_LineTo(page, x, HPDF_Page_GetHeight(page));
            HPDF_Page_Stroke(page);
        }

        for (var y = 0.0; y <= HPDF_Page_GetHeight(page); y += 50)
        {
            HPDF_Page_MoveTo(page, 0, y);
            HPDF_Page_LineTo(page, HPDF_Page_GetWidth(page), y);
            HPDF_Page_Stroke(page);
        }

        HPDF_Page_SetRGBStroke(page, 0, 0, 0);
    }

    private static void ShowStripePattern(PdfPage page, double x, double y)
    {
        for (var iy = 0; iy < 50; iy += 3)
        {
            HPDF_Page_SetRGBStroke(page, 0, 0, 0.5);
            HPDF_Page_SetLineWidth(page, 1);
            HPDF_Page_MoveTo(page, x, y + iy);
            HPDF_Page_LineTo(page, x + HPDF_Page_TextWidth(page, "ABCabc123"), y + iy);
            HPDF_Page_Stroke(page);
        }

        HPDF_Page_SetLineWidth(page, 2.5);
    }

    private static void ShowDescription(PdfPage page, PdfFont font, double x, double y, string text)
    {
        var previousSize = HPDF_Page_GetCurrentFontSize(page);
        var previousFill = page.RgbFill;

        HPDF_Page_BeginText(page);
        HPDF_Page_SetRGBFill(page, 0, 0, 0);
        HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.Fill);
        HPDF_Page_SetFontAndSize(page, font, 10);
        HPDF_Page_TextOut(page, x, y - 12, text);
        HPDF_Page_EndText(page);

        HPDF_Page_SetFontAndSize(page, font, previousSize);
        HPDF_Page_SetRGBFill(page, previousFill.R, previousFill.G, previousFill.B);
    }

    private static StructuralRequirement RequireToken(string token, int minimumCount) => new(token, minimumCount);

    private static PdfStructureReport CheckPdf(string demo, string pdfPath, params StructuralRequirement[] requirements)
    {
        var bytes = File.ReadAllBytes(pdfPath);
        var latin1 = Encoding.Latin1.GetString(bytes);
        Require(latin1.StartsWith("%PDF-", StringComparison.Ordinal), $"{demo}: missing PDF header.");
        Require(latin1.Contains("xref", StringComparison.Ordinal), $"{demo}: missing xref table.");
        Require(latin1.Contains("startxref", StringComparison.Ordinal), $"{demo}: missing startxref marker.");
        RequireStartXrefIsAccurate(latin1, demo);

        var counts = requirements
            .Select(requirement => (requirement, Actual: Count(latin1, requirement.Token)))
            .ToArray();
        var failures = counts
            .Where(static item => item.Actual < item.requirement.MinimumCount)
            .Select(static item => $"{DisplayToken(item.requirement.Token)} expected >= {item.requirement.MinimumCount}, actual {item.Actual}")
            .ToArray();

        var report = BuildStructureReport(demo, pdfPath, bytes.Length, latin1, counts);
        File.WriteAllText(Path.ChangeExtension(pdfPath, ".structure.txt"), report);

        if (failures.Length > 0)
            throw new InvalidOperationException($"{demo} structural diff:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}{Environment.NewLine}{Environment.NewLine}{report}");

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"{bytes.Length} bytes, structural profile matched");
        return new PdfStructureReport(demo, pdfPath, report);
    }

    private static string BuildStructureReport(
        string demo,
        string pdfPath,
        int byteLength,
        string latin1,
        IEnumerable<(StructuralRequirement Requirement, int Actual)> checkedCounts)
    {
        var objectCount = Regex.Matches(latin1, @"(?m)^\d+ 0 obj").Count;
        var pageCount = Count(latin1, "/Type /Page") - Count(latin1, "/Type /Pages");
        var tokens = checkedCounts
            .Select(static item => $"  {DisplayToken(item.Requirement.Token)}: {item.Actual} (expected >= {item.Requirement.MinimumCount})");

        return string.Join(Environment.NewLine, [
            $"demo: {demo}",
            $"file: {Path.GetFileName(pdfPath)}",
            $"bytes: {byteLength}",
            $"header: {latin1[..Math.Min(latin1.IndexOf('\n') >= 0 ? latin1.IndexOf('\n') : latin1.Length, 16)].Trim()}",
            $"objects: {objectCount}",
            $"pages: {pageCount}",
            "checks:",
            ..tokens
        ]);
    }

    private static void RequireStartXrefIsAccurate(string pdfText, string demo)
    {
        var marker = pdfText.LastIndexOf("startxref", StringComparison.Ordinal);
        Require(marker >= 0, $"{demo}: missing startxref marker.");

        var numberStart = pdfText.IndexOf('\n', marker);
        Require(numberStart >= 0, $"{demo}: malformed startxref marker.");
        numberStart++;

        var numberEnd = pdfText.IndexOf('\n', numberStart);
        Require(numberEnd > numberStart, $"{demo}: malformed startxref offset.");

        var offsetText = pdfText[numberStart..numberEnd].Trim();
        var offset = int.Parse(offsetText, System.Globalization.CultureInfo.InvariantCulture);
        Require(offset > 0 && offset < pdfText.Length, $"{demo}: startxref offset is outside the PDF.");
        Require(pdfText[offset..].StartsWith("xref", StringComparison.Ordinal), $"{demo}: startxref does not point at xref.");
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

    private static string DisplayToken(string token) => token
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed record StructuralRequirement(string Token, int MinimumCount);

    private sealed record PdfStructureReport(string Demo, string Path, string Text);
}
