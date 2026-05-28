using System.Text;
using System.Globalization;
using LibHaru;
using static LibHaru.HPdf;

var root = FindRepoRoot();
var artifacts = Path.Combine(root.FullName, "artifacts");
Directory.CreateDirectory(artifacts);

var pdfPath = Path.Combine(artifacts, "libharu-managed-smoke.pdf");
SmokeTest(pdfPath);

var compressionPdfPath = Path.Combine(artifacts, "libharu-managed-compression.pdf");
CompressionFilters.Test(root.FullName, compressionPdfPath);
var fontPdfPath = Path.Combine(artifacts, "libharu-managed-fonts.pdf");
FontAndEncoder.Test(root.FullName, fontPdfPath);
var documentFeaturesPdfPath = Path.Combine(artifacts, "libharu-managed-document-features.pdf");
DocumentFeatures.Test(root.FullName, documentFeaturesPdfPath);
var imageFeaturesPdfPath = Path.Combine(artifacts, "libharu-managed-images.pdf");
ImageFeatures.Test(root.FullName, imageFeaturesPdfPath);
var nameTreePdfPath = Path.Combine(artifacts, "libharu-managed-name-trees.pdf");
NameTreeFixtures.Test(root.FullName, nameTreePdfPath);
CompatibilityDemos.Test(root.FullName, artifacts);
ReferenceOutputRegression.TryCompareExactPdfs(
    root.FullName,
    artifacts,
    Path.Combine(root.FullName, "csharp", "LibHaru.SmokeTests", "fixtures", "reference-output.tsv"));
PdfAValidation.Test(root.FullName);
ErrorModel.Test();
ObjectSemantics.Test();
SecuritySemantics.Test();
VisualRegression.TryRenderSmokePdfs(
    artifacts,
    Path.Combine(root.FullName, "csharp", "LibHaru.SmokeTests", "fixtures", "visual-reference.tsv"),
    pdfPath,
    documentFeaturesPdfPath,
    Path.Combine(artifacts, "compatibility-demos", "compat-pdf-a-conformance-demo.pdf"));

static void SmokeTest(string pdfPath)
{
    using var pdf = HPDF_New();
    using (var exPdf = HPDF_NewEx(userData: "newex"))
        Require(HPDF_HasDoc(exPdf), "NewEx should create an active document.");

    HPDF_SetCompressionMode(pdf, CompressionMode.All);
    HPDF_SetPageLayout(pdf, PdfPageLayout.Single);
    HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);
    HPDF_SetViewerPreference(pdf, PdfViewerPreference.HideToolbar | PdfViewerPreference.FitWindow);
    HPDF_SetInfoAttr(pdf, PdfInfoType.Title, "Managed libharu smoke test");
    HPDF_SetInfoAttr(pdf, PdfInfoType.Author, "LibHaru C# port");
    HPDF_SetInfoDateAttr(pdf, PdfInfoType.ModDate, new DateTimeOffset(2026, 5, 27, 12, 34, 56, TimeSpan.Zero));

    Require(HPDF_HasDoc(pdf), "Fresh document should have active content.");
    Require(ReferenceEquals(HPDF_GetDocMMgr(pdf), pdf), "Document manager helper should return the managed document.");
    Require(HPDF_GetPageLayout(pdf) == PdfPageLayout.Single, "Page layout getter mismatch.");
    Require(HPDF_GetPageMode(pdf) == PdfPageMode.UseOutline, "Page mode getter mismatch.");
    Require(HPDF_GetViewerPreference(pdf).HasFlag(PdfViewerPreference.HideToolbar), "Viewer preference getter mismatch.");
    Require(HPDF_GetInfoAttr(pdf, PdfInfoType.Title) == "Managed libharu smoke test", "Info attribute getter mismatch.");
    Require(HPDF_GetInfoAttr(pdf, PdfInfoType.ModDate)?.StartsWith("D:20260527123456", StringComparison.Ordinal) == true, "Info date setter mismatch.");

    using (var scratch = HPDF_New())
    {
        HPDF_SetPagesConfiguration(scratch, 16);
        Require(scratch.PagePerPages == 16, "Pages configuration was not retained.");
        HPDF_AddPage(scratch);
        HPDF_FreeDoc(scratch);
        Require(!HPDF_HasDoc(scratch), "FreeDoc should remove active document content.");
        HPDF_NewDoc(scratch);
        Require(HPDF_HasDoc(scratch) && scratch.Pages.Count == 0 && scratch.PagePerPages == 0, "NewDoc should rebuild an empty active document.");
        HPDF_FreeDocAll(scratch);
        Require(!HPDF_HasDoc(scratch), "FreeDocAll should remove active document content.");
    }

    var memoryIcc = HPDF_ICC_LoadIccFromMem(pdf, [0, 1, 2, 3, 4, 5], 3);
    Require(memoryIcc.ComponentCount == 3, "Memory ICC component count mismatch.");
    HPDF_AppendOutputIntents(pdf, "Managed RGB profile", memoryIcc, "Managed RGB profile");

    var iccPath = Path.ChangeExtension(pdfPath, ".gray.icc");
    File.WriteAllBytes(iccPath, [9, 8, 7, 6]);
    var fileIcc = HPDF_LoadIccProfileFromFile(pdf, iccPath, 1);
    Require(fileIcc.ComponentCount == 1, "File ICC component count mismatch.");
    HPDF_AppendOutputIntents(pdf, "Managed gray profile", fileIcc, "Managed gray profile");

    Require(HPDF_GetCurrentEncoder(pdf) is null, "Current encoder should start unset.");
    var winAnsiEncoder = HPDF_GetEncoder(pdf, "WinAnsiEncoding");
    Require(ReferenceEquals(winAnsiEncoder, HPDF_GetEncoder(pdf, "WinAnsiEncoding")), "Encoder lookup should be cached.");
    Require(HPDF_Encoder_GetType(winAnsiEncoder) == PdfEncoderType.SingleByte, "WinAnsi should report single-byte encoding.");
    Require(HPDF_Encoder_GetByteType(winAnsiEncoder, "ABC", 1) == PdfByteType.Single, "Single-byte encoder byte type mismatch.");
    Require(HPDF_Encoder_GetUnicode(winAnsiEncoder, 0x80) == 0x20AC, "WinAnsi euro mapping mismatch.");
    Require(HPDF_Encoder_GetUnicode(winAnsiEncoder, 0x81) == 0, "WinAnsi undefined-code mapping mismatch.");
    Require(HPDF_Encoder_GetWritingMode(winAnsiEncoder) == PdfWritingMode.Horizontal, "WinAnsi writing mode mismatch.");
    Require(HPDF_Encoder_GetUnicode(HPDF_GetEncoder(pdf, "StandardEncoding"), 0x27) == 0x2019, "StandardEncoding quote mapping mismatch.");
    Require(HPDF_Encoder_GetUnicode(HPDF_GetEncoder(pdf, "MacRomanEncoding"), 0xDB) == 0x20AC, "MacRoman euro mapping mismatch.");
    Require(HPDF_Encoder_GetUnicode(HPDF_GetEncoder(pdf, "ISO8859-5"), 0xB0) == 0x0410, "ISO8859-5 Cyrillic mapping mismatch.");
    Require(HPDF_Encoder_GetUnicode(HPDF_GetEncoder(pdf, "CP1251"), 0xC0) == 0x0410, "CP1251 Cyrillic mapping mismatch.");
    Require(HPDF_Encoder_GetUnicode(HPDF_GetEncoder(pdf, "CP1258"), 0xD5) == 0x01A0, "CP1258 Vietnamese mapping mismatch.");
    Require(HPDF_Encoder_GetUnicode(HPDF_GetEncoder(pdf, "KOI8-R"), 0xE1) == 0x0410, "KOI8-R Cyrillic mapping mismatch.");

    var shiftJisVertical = HPDF_GetEncoder(pdf, "90ms-RKSJ-V");
    var shiftJisBytes = Encoding.Latin1.GetString([0x82, 0xA0]);
    Require(HPDF_Encoder_GetType(shiftJisVertical) == PdfEncoderType.DoubleByte, "CMap encoder should report double-byte encoding.");
    Require(HPDF_Encoder_GetByteType(shiftJisVertical, shiftJisBytes, 0) == PdfByteType.Lead, "CMap lead byte detection mismatch.");
    Require(HPDF_Encoder_GetByteType(shiftJisVertical, shiftJisBytes, 1) == PdfByteType.Trail, "CMap trail byte detection mismatch.");
    Require(HPDF_Encoder_GetByteType(shiftJisVertical, Encoding.Latin1.GetString([0x82]), 0) == PdfByteType.Lead, "CMap orphan lead-byte classification mismatch.");
    Require(HPDF_Encoder_GetByteType(shiftJisVertical, Encoding.Latin1.GetString([0x82, 0x20]), 1) == PdfByteType.Unknown, "CMap invalid trail-byte classification mismatch.");
    Require(HPDF_Encoder_GetUnicode(shiftJisVertical, 0x82A0) == 0x3042, "Shift-JIS Unicode map mismatch.");
    Require(HPDF_Encoder_GetWritingMode(shiftJisVertical) == PdfWritingMode.Vertical, "Vertical CMap writing mode mismatch.");
    var eucJapanese = HPDF_GetEncoder(pdf, "EUC-H");
    var eucJapaneseBytes = Encoding.Latin1.GetString([0xA4, 0xA2]);
    Require(HPDF_Encoder_GetByteType(eucJapanese, eucJapaneseBytes, 0) == PdfByteType.Lead, "EUC-JP lead byte detection mismatch.");
    Require(HPDF_Encoder_GetByteType(eucJapanese, eucJapaneseBytes, 1) == PdfByteType.Trail, "EUC-JP trail byte detection mismatch.");
    Require(HPDF_Encoder_GetUnicode(eucJapanese, 0xA4A2) == 0x3042, "EUC-JP Unicode map mismatch.");
    HPDF_SetCurrentEncoder(pdf, "WinAnsiEncoding");
    Require(ReferenceEquals(HPDF_GetCurrentEncoder(pdf), winAnsiEncoder), "Current encoder was not retained.");

    HPDF_UseJPEncodings(pdf);
    HPDF_UseKREncodings(pdf);
    HPDF_UseCNSEncodings(pdf);
    HPDF_UseCNTEncodings(pdf);
    HPDF_UseUTFEncodings(pdf);
    HPDF_UseJPFonts(pdf);
    HPDF_UseKRFonts(pdf);
    HPDF_UseCNSFonts(pdf);
    HPDF_UseCNTFonts(pdf);
    Require(HPDF_Encoder_GetType(HPDF_GetEncoder(pdf, "KSCms-UHC-H")) == PdfEncoderType.DoubleByte, "Korean CMap encoder type mismatch.");
    Require(HPDF_Encoder_GetWritingMode(HPDF_GetEncoder(pdf, "KSCms-UHC-HW-V")) == PdfWritingMode.Vertical, "Korean vertical CMap writing mode mismatch.");
    Require(HPDF_Encoder_GetType(HPDF_GetEncoder(pdf, "GBK-EUC-H")) == PdfEncoderType.DoubleByte, "Simplified Chinese CMap encoder type mismatch.");
    Require(HPDF_Encoder_GetWritingMode(HPDF_GetEncoder(pdf, "ETen-B5-V")) == PdfWritingMode.Vertical, "Traditional Chinese vertical CMap writing mode mismatch.");
    Require(HPDF_Encoder_GetType(HPDF_GetEncoder(pdf, "UTF-8")) == PdfEncoderType.DoubleByte, "UTF-8 encoder type mismatch.");
    Require(HPDF_Encoder_GetByteType(HPDF_GetEncoder(pdf, "KSCms-UHC-H"), Encoding.Latin1.GetString([0x81, 0x40]), 1) == PdfByteType.Unknown, "KSCms-UHC trail byte range mismatch.");
    Require(HPDF_Encoder_GetByteType(HPDF_GetEncoder(pdf, "GB-EUC-H"), Encoding.Latin1.GetString([0xA1, 0xA0]), 1) == PdfByteType.Unknown, "GB-EUC trail byte range mismatch.");

    var font = HPDF_GetFont(pdf, "Helvetica");
    var bold = HPDF_GetFont(pdf, "Helvetica-Bold");
    var times = HPDF_GetFont(pdf, "Times-Roman");
    var symbol = HPDF_GetFont(pdf, "Symbol");
    var zapfDingbats = HPDF_GetFont(pdf, "ZapfDingbats");
    var japaneseFont = HPDF_GetFont(pdf, "MS-PGothic", "90msp-RKSJ-H");
    var koreanFont = HPDF_GetFont(pdf, "Dotum", "KSC-EUC-H");
    var simplifiedChineseFont = HPDF_GetFont(pdf, "SimSun", "GB-EUC-H");
    var traditionalChineseFont = HPDF_GetFont(pdf, "MingLiU", "ETen-B5-H");
    var ttFontDef = HPDF_GetTTFontDefFromFile(pdf, Path.Combine(FindRepoRoot().FullName, "demo", "ttfont", "PenguinAttack.ttf"), embedding: true);
    Require(!string.IsNullOrWhiteSpace(ttFontDef.BaseFont), "TT font-def alias did not return a base font.");
    Require(HPDF_Font_GetBBox(bold) == new PdfRect(-170, -228, 1003, 962), "Base14 descriptor table mismatch.");
    Require(HPDF_Font_GetEncodingName(symbol) == "FontSpecific", "Symbol should default to FontSpecific encoding.");
    Require(HPDF_Font_GetEncodingName(zapfDingbats) == "FontSpecific", "ZapfDingbats should default to FontSpecific encoding.");
    Require(HPDF_Font_TextWidth(font, "C").Width == 722, "Helvetica Base14 width table mismatch.");
    Require(HPDF_Font_TextWidth(times, "f").Width == 333, "Times-Roman Base14 width table mismatch.");
    Require(HPDF_Font_TextWidth(symbol, "A").Width == 722, "Symbol Base14 width table mismatch.");
    var fontWidth = HPDF_Font_TextWidth(font, "abc def");
    Require(fontWidth.NumChars == 7 && fontWidth.NumSpace == 1 && fontWidth.Width > 0, "Font text-width alias mismatch.");
    var measuredChars = HPDF_Font_MeasureText(font, "abc def", 80, 12, 0, 0, true, out var measuredWidth);
    Require(measuredChars > 0 && measuredWidth > 0, "Font measure-text alias mismatch.");
    var japaneseText = Encoding.Latin1.GetString([0x82, 0xA0]);
    var japaneseWidth = HPDF_Font_TextWidth(japaneseFont, japaneseText);
    Require(japaneseWidth.NumChars == 1 && japaneseWidth.Width == 941, "Predefined CID proportional width mismatch.");
    Require(HPDF_Font_GetUnicodeWidth(japaneseFont, 'あ') == 941, "Predefined CID Unicode width mismatch.");
    var japaneseBBox = HPDF_Font_GetBBox(japaneseFont);
    Require(japaneseBBox == new PdfRect(-121, -136, 996, 859), "Predefined CID font descriptor mismatch.");
    var page = HPDF_AddPage(pdf);
    HPDF_Page_SetSize(page, PdfPageSize.A4, PdfPageDirection.Portrait);
    HPDF_Page_SetBoundary(page, PdfPageBoundary.CropBox, 0, 0, HPDF_Page_GetWidth(page), HPDF_Page_GetHeight(page));
    HPDF_Page_SetRotate(page, 90);
    HPDF_Page_SetZoom(page, 1.25);
    Require(ReferenceEquals(HPDF_GetPageMMgr(page), pdf), "Page manager helper should return the owning document.");

    var rawImage = HPDF_LoadRawImageFromMem(
        pdf,
        [
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 0
        ],
        2,
        2,
        PdfColorSpace.DeviceRgb);
    var imageForm = HPDF_Page_CreateXObjectFromImage(pdf, page, new PdfRect(0, 0, 16, 16), rawImage, zoom: true);
    var whiteForm = HPDF_Page_CreateXObjectAsWhiteRect(pdf, page, new PdfRect(0, 0, 12, 10));
    Require(HPDF_Page_GetXObjectName(page, imageForm).StartsWith("X", StringComparison.Ordinal), "Image form XObject name mismatch.");
    Require(HPDF_Page_GetXObjectName(page, whiteForm).StartsWith("X", StringComparison.Ordinal), "White form XObject name mismatch.");

    Require(HPDF_Page_GetGMode(page) == (ushort)PdfGraphicsMode.PageDescription, "Initial graphics mode mismatch.");
    Require(HPDF_Page_GetGStateDepth(page) == 1, "Initial graphics state depth mismatch.");
    Require(HPDF_Page_GetLineWidth(page) == 1, "Initial line width mismatch.");
    Require(HPDF_Page_GetLineCap(page) == PdfLineCap.ButtEnd, "Initial line cap mismatch.");
    Require(HPDF_Page_GetLineJoin(page) == PdfLineJoin.MiterJoin, "Initial line join mismatch.");
    Require(HPDF_Page_GetMiterLimit(page) == 10, "Initial miter limit mismatch.");
    Require(HPDF_Page_GetFlat(page) == 1, "Initial flatness mismatch.");
    Require(HPDF_Page_GetCharSpace(page) == 0, "Initial char spacing mismatch.");
    Require(HPDF_Page_GetWordSpace(page) == 0, "Initial word spacing mismatch.");
    Require(HPDF_Page_GetHorizontalScalling(page) == 100, "Initial horizontal scaling mismatch.");
    Require(HPDF_Page_GetTextLeading(page) == 0, "Initial text leading mismatch.");
    Require(HPDF_Page_GetTextRenderingMode(page) == PdfTextRenderingMode.Fill, "Initial text rendering mode mismatch.");
    Require(HPDF_Page_GetTextRise(page) == 0, "Initial text rise mismatch.");
    Require(HPDF_Page_GetFillingColorSpace(page) == PdfColorSpace.DeviceGray, "Initial fill color space mismatch.");
    Require(HPDF_Page_GetStrokingColorSpace(page) == PdfColorSpace.DeviceGray, "Initial stroke color space mismatch.");
    Require(HPDF_Page_GetGrayFill(page) == 0, "Initial gray fill mismatch.");
    Require(HPDF_Page_GetGrayStroke(page) == 0, "Initial gray stroke mismatch.");

    HPDF_Page_GSave(page);
    HPDF_Page_SetLineWidth(page, 7);
    HPDF_Page_SetLineCap(page, PdfLineCap.RoundEnd);
    HPDF_Page_SetLineJoin(page, PdfLineJoin.BevelJoin);
    HPDF_Page_SetMiterLimit(page, 12);
    HPDF_Page_SetDash(page, [2.0, 4.0], 2, 1);
    HPDF_Page_SetFlat(page, 2);
    HPDF_Page_SetCharSpace(page, 1.25);
    HPDF_Page_SetWordSpace(page, 2.5);
    HPDF_Page_SetHorizontalScalling(page, 90);
    HPDF_Page_SetTextLeading(page, 14);
    HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.Stroke);
    HPDF_Page_SetTextRise(page, 3);
    HPDF_Page_SetCMYKFill(page, 0.1, 0.2, 0.3, 0.4);
    HPDF_Page_SetCMYKStroke(page, 0.2, 0.3, 0.4, 0.5);
    var dash = HPDF_Page_GetDash(page);
    Require(HPDF_Page_GetGStateDepth(page) == 2, "Saved graphics state depth mismatch.");
    Require(HPDF_Page_GetLineWidth(page) == 7, "Line width getter mismatch.");
    Require(HPDF_Page_GetLineCap(page) == PdfLineCap.RoundEnd, "Line cap getter mismatch.");
    Require(HPDF_Page_GetLineJoin(page) == PdfLineJoin.BevelJoin, "Line join getter mismatch.");
    Require(HPDF_Page_GetMiterLimit(page) == 12, "Miter limit getter mismatch.");
    Require(dash.Count == 2 && dash.Pattern[0] == 2.0 && dash.Pattern[1] == 4.0 && dash.Phase == 1, "Dash getter mismatch.");
    Require(HPDF_Page_GetFlat(page) == 2, "Flatness getter mismatch.");
    Require(HPDF_Page_GetCharSpace(page) == 1.25, "Char spacing getter mismatch.");
    Require(HPDF_Page_GetWordSpace(page) == 2.5, "Word spacing getter mismatch.");
    Require(HPDF_Page_GetHorizontalScalling(page) == 90, "Horizontal scaling getter mismatch.");
    Require(HPDF_Page_GetTextLeading(page) == 14, "Text leading getter mismatch.");
    Require(HPDF_Page_GetTextRenderingMode(page) == PdfTextRenderingMode.Stroke, "Text rendering getter mismatch.");
    Require(HPDF_Page_GetTextRaise(page) == 3, "Text raise getter mismatch.");
    Require(HPDF_Page_GetFillingColorSpace(page) == PdfColorSpace.DeviceCmyk, "CMYK fill color space mismatch.");
    Require(HPDF_Page_GetStrokingColorSpace(page) == PdfColorSpace.DeviceCmyk, "CMYK stroke color space mismatch.");
    Require(HPDF_Page_GetCMYKFill(page) == new PdfCmykColor(0.1, 0.2, 0.3, 0.4), "CMYK fill getter mismatch.");
    Require(HPDF_Page_GetCMYKStroke(page) == new PdfCmykColor(0.2, 0.3, 0.4, 0.5), "CMYK stroke getter mismatch.");
    HPDF_Page_Concat(page, 1, 0, 0, 1, 5, 6);
    Require(HPDF_Page_GetTransMatrix(page) == new PdfTransMatrix(1, 0, 0, 1, 5, 6), "Transformation matrix getter mismatch.");
    HPDF_Page_GRestore(page);
    Require(HPDF_Page_GetGStateDepth(page) == 1 && HPDF_Page_GetLineWidth(page) == 1, "Graphics restore did not restore state.");

    HPDF_Page_SetFontAndSize(page, font, 10);
    HPDF_Page_BeginText(page);
    HPDF_Page_MoveTextPos(page, 10, 20);
    Require(HPDF_Page_GetCurrentTextPos(page) == new PdfPoint(10, 20), "Current text position getter mismatch.");
    HPDF_Page_MoveTextPos2(page, 5, -12);
    Require(HPDF_Page_GetTextLeading(page) == 12, "MoveTextPos2 should update text leading.");
    HPDF_Page_SetTextMatrix(page, 1, 0, 0, 1, 30, 40);
    Require(HPDF_Page_GetTextMatrix(page) == new PdfTransMatrix(1, 0, 0, 1, 30, 40), "Text matrix getter mismatch.");
    HPDF_Page_ShowTextNextLineEx(page, 1.5, 0.5, "spaced alias");
    Require(HPDF_Page_GetWordSpace(page) == 1.5 && HPDF_Page_GetCharSpace(page) == 0.5, "ShowTextNextLineEx spacing mismatch.");
    HPDF_Page_EndText(page);

    HPDF_Page_SetLineWidth(page, 1.25);
    HPDF_Page_SetRGBStroke(page, 0.1, 0.25, 0.55);
    HPDF_Page_Rectangle(page, 48, 48, HPDF_Page_GetWidth(page) - 96, HPDF_Page_GetHeight(page) - 96);
    HPDF_Page_Stroke(page);

    HPDF_Page_GSave(page);
    HPDF_Page_Rectangle(page, 72, 120, 32, 24);
    HPDF_Page_Eofill(page);
    HPDF_Page_Rectangle(page, 112, 120, 32, 24);
    HPDF_Page_EofillStroke(page);
    HPDF_Page_MoveTo(page, 152, 120);
    HPDF_Page_LineTo(page, 184, 120);
    HPDF_Page_LineTo(page, 184, 144);
    HPDF_Page_ClosePathEofillStroke(page);
    HPDF_Page_Rectangle(page, 196, 120, 24, 24);
    HPDF_Page_Eoclip(page);
    HPDF_Page_EndPath(page);
    HPDF_Page_Ellipse(page, 250, 132, 24, 12);
    HPDF_Page_Stroke(page);
    HPDF_Page_GRestore(page);

    HPDF_Page_GSave(page);
    HPDF_Page_Concat(page, 1, 0, 0, 1, 72, 72);
    HPDF_Page_ExecuteXObject(page, imageForm);
    HPDF_Page_Concat(page, 1, 0, 0, 1, 24, 0);
    HPDF_Page_ExecuteXObject(page, whiteForm);
    HPDF_Page_GRestore(page);

    HPDF_Page_SetFontAndSize(page, bold, 24);
    var title = "libharu C# source port";
    var titleWidth = HPDF_Page_TextWidth(page, title);
    HPDF_Page_TextOut(page, (HPDF_Page_GetWidth(page) - titleWidth) / 2, HPDF_Page_GetHeight(page) - 96, title);

    HPDF_Page_SetFontAndSize(page, font, 12);
    HPDF_Page_BeginText(page);
    HPDF_Page_MoveTextPos(page, 72, HPDF_Page_GetHeight(page) - 140);
    HPDF_Page_ShowText(page, "This PDF was generated by managed C# code, not a native binding.");
    HPDF_Page_MoveTextPos(page, 0, -18);
    HPDF_Page_ShowText(page, $"Version: {HPDF_GetVersion()}");
    HPDF_Page_EndText(page);

    HPDF_Page_SetFontAndSize(page, font, 10);
    HPDF_Page_BeginText(page);
    var textRectStatus = HPDF_Page_TextRect(page, 72, 180, 260, 130, "TextRect alias wraps this sentence.", PdfTextAlignment.Left, out var textRectLength);
    HPDF_Page_EndText(page);
    Require(textRectStatus == HaruStatus.OK && textRectLength > 0, "TextRect alias failed.");

    HPDF_Page_SetFontAndSize(page, koreanFont, 9);
    HPDF_Page_TextOut(page, 72, 108, "KR CID");
    HPDF_Page_SetFontAndSize(page, simplifiedChineseFont, 9);
    HPDF_Page_TextOut(page, 132, 108, "CNS CID");
    HPDF_Page_SetFontAndSize(page, traditionalChineseFont, 9);
    HPDF_Page_TextOut(page, 204, 108, "CNT CID");
    HPDF_Page_SetFontAndSize(page, japaneseFont, 9);
    HPDF_Page_TextOut(page, 270, 108, japaneseText);

    var sharedContent = HPDF_Page_New_Content_Stream(page);
    HPDF_Page_SetRGBStroke(page, 0.8, 0.1, 0.1);
    HPDF_Page_MoveTo(page, 72, 92);
    HPDF_Page_LineTo(page, 220, 92);
    HPDF_Page_Stroke(page);
    _ = HPDF_Page_New_Content_Stream(page);

    var markup = HPDF_Page_CreateTextMarkupAnnot(page, new PdfRect(72, 188, 220, 202), "generic markup", null, PdfAnnotType.Highlight);
    HPDF_TextMarkupAnnot_SetQuadPoints(markup, new PdfPoint(72, 188), new PdfPoint(220, 188), new PdfPoint(220, 202), new PdfPoint(72, 202));
    _ = HPDF_Page_CreateWidgetAnnot_WhiteOnlyWhilePrint(pdf, page, new PdfRect(300, 96, 340, 120));
    var projection = HPDF_Page_CreateProjectionAnnot(page, new PdfRect(350, 96, 390, 120), "projection");
    var measure = HPDF_Page_Create3DC3DMeasure(page, new PdfPoint3D(0, 0, 0), new PdfPoint3D(1, 1, 1));
    var exData = HPDF_Page_Create3DAnnotExData(page);
    HPDF_3DAnnotExData_Set3DMeasurement(exData, measure);
    HPDF_ProjectionAnnot_SetExData(projection, exData);

    var sharedPage = HPDF_AddPage(pdf);
    HPDF_Page_SetSize(sharedPage, PdfPageSize.A4, PdfPageDirection.Portrait);
    HPDF_Page_Insert_Shared_Content_Stream(sharedPage, sharedContent);

    HPDF_SaveToFile(pdf, pdfPath);
    var savedStream = HPDF_SaveToStream(pdf);
    var streamPrefix = HPDF_ReadFromStream(pdf, 5);
    Require(Encoding.ASCII.GetString(streamPrefix) == "%PDF-", "ReadFromStream prefix mismatch.");
    var readBuffer = new byte[4];
    var readSize = 4u;
    HPDF_ReadFromStream(pdf, readBuffer, ref readSize);
    Require(readSize == 4 && savedStream.Length >= 9, "ReadFromStream buffer overload mismatch.");

    var bytes = File.ReadAllBytes(pdfPath);
    var latin1 = Encoding.Latin1.GetString(bytes);

    Require(bytes.Length > 700, "Generated PDF is unexpectedly small.");
    Require(latin1.StartsWith("%PDF-1.4", StringComparison.Ordinal), "Missing PDF header.");
    Require(latin1.Contains("/Type /Catalog", StringComparison.Ordinal), "Missing catalog object.");
    Require(latin1.Contains("/Type /Page", StringComparison.Ordinal), "Missing page object.");
    Require(latin1.Contains("/BaseFont /Helvetica", StringComparison.Ordinal), "Missing Helvetica font object.");
    Require(latin1.Contains("/BaseFont /Dotum", StringComparison.Ordinal), "Missing Korean CID font object.");
    Require(latin1.Contains("/BaseFont /SimSun", StringComparison.Ordinal), "Missing Simplified Chinese CID font object.");
    Require(latin1.Contains("/BaseFont /MingLiU", StringComparison.Ordinal), "Missing Traditional Chinese CID font object.");
    Require(latin1.Contains("/CropBox", StringComparison.Ordinal), "Missing page boundary alias output.");
    Require(latin1.Contains("/Rotate 90", StringComparison.Ordinal), "Missing page rotate alias output.");
    Require(latin1.Contains("/PZ 1.25", StringComparison.Ordinal), "Missing page zoom alias output.");
    Require(latin1.Contains("/Contents [", StringComparison.Ordinal), "Missing shared content stream array output.");
    Require(latin1.Contains("/Subtype /Widget", StringComparison.Ordinal), "Missing widget alias output.");
    Require(latin1.Contains("/FT /Btn", StringComparison.Ordinal), "Missing widget field type output.");
    Require(latin1.Contains("/F 36", StringComparison.Ordinal), "Missing widget print/no-view flags.");
    Require(latin1.Contains("/T (Blind)", StringComparison.Ordinal), "Missing widget title output.");
    Require(latin1.Contains("/MK", StringComparison.Ordinal), "Missing widget MK dictionary.");
    Require(latin1.Contains("/BG [1 1 1]", StringComparison.Ordinal), "Missing widget background color.");
    Require(latin1.Contains("/Subtype /Projection", StringComparison.Ordinal), "Missing projection alias output.");
    Require(latin1.Contains("/ExData", StringComparison.Ordinal), "Missing projection ExData output.");
    Require(latin1.Contains("/Subtype /Form", StringComparison.Ordinal), "Missing form XObject.");
    Require(latin1.Contains("/FormType 1", StringComparison.Ordinal), "Missing appearance form type.");
    Require(latin1.Contains("/Matrix [1 0 0 1 0 0]", StringComparison.Ordinal), "Missing appearance identity matrix.");
    Require(latin1.Contains("/DW 1000", StringComparison.Ordinal), "Missing predefined CID default width.");
    Require(latin1.Contains("/DW2 [880 -1000]", StringComparison.Ordinal), "Missing predefined CID vertical metrics.");
    Require(latin1.Contains("/MissingWidth 500", StringComparison.Ordinal), "Missing predefined CID descriptor missing width.");
    Require(latin1.Contains("/FontBBox [-121 -136 996 859]", StringComparison.Ordinal), "Missing exact MS-PGothic font bbox.");
    Require(latin1.Contains("/Alternate /DeviceRGB", StringComparison.Ordinal), "Missing RGB ICC alternate.");
    Require(latin1.Contains("/Alternate /DeviceGray", StringComparison.Ordinal), "Missing gray ICC alternate.");
    Require(latin1.Contains("xref", StringComparison.Ordinal), "Missing xref table.");
    Require(latin1.Contains("startxref", StringComparison.Ordinal), "Missing startxref.");
    RequireStartXrefIsAccurate(latin1);

    Console.WriteLine($"Generated {pdfPath}");
    Console.WriteLine($"{bytes.Length} bytes");
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void RequireStartXrefIsAccurate(string pdfText)
{
    var marker = pdfText.LastIndexOf("startxref", StringComparison.Ordinal);
    Require(marker >= 0, "Missing startxref marker.");

    var numberStart = pdfText.IndexOf('\n', marker);
    Require(numberStart >= 0, "Malformed startxref marker.");
    numberStart++;

    var numberEnd = pdfText.IndexOf('\n', numberStart);
    Require(numberEnd > numberStart, "Malformed startxref offset.");

    var offsetText = pdfText[numberStart..numberEnd].Trim();
    var offset = int.Parse(offsetText, CultureInfo.InvariantCulture);
    Require(offset > 0 && offset < pdfText.Length, "startxref offset is outside the PDF.");
    Require(pdfText[offset..].StartsWith("xref", StringComparison.Ordinal), "startxref does not point at the xref table.");
}

static DirectoryInfo FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);

    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LibHaruSharp.sln")))
        dir = dir.Parent;

    return dir ?? throw new DirectoryNotFoundException("Could not locate repository root.");
}
