using LibHaru;

namespace LibHaru.Tests;

public sealed class HPdfCompatibilityTests
{
    [Fact]
    public void DocumentWrappers_MapToPdfDocumentOperations()
    {
        var userData = new object();
        var handlerCalls = new List<(uint ErrorNo, object? UserData)>();
        using var pdf = HPdf.HPDF_New((errorNo, _, data) => handlerCalls.Add((errorNo, data)), userData);

        Assert.True(HPdf.HPDF_HasDoc(pdf));
        Assert.Same(pdf, HPdf.HPDF_GetDocMMgr(pdf));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_SetPagesConfiguration(pdf, 8));
        Assert.Equal(8u, pdf.PagePerPages);

        var page = HPdf.HPDF_AddPage(pdf);

        Assert.Same(page, HPdf.HPDF_GetCurrentPage(pdf));
        Assert.Same(page, HPdf.HPDF_GetPageByIndex(pdf, 0));
        Assert.Same(pdf, HPdf.HPDF_GetPageMMgr(page));

        HPdf.HPDF_FreeDoc(pdf);

        Assert.False(HPdf.HPDF_HasDoc(pdf));
        Assert.Contains(handlerCalls, call => call.ErrorNo == HaruStatus.InvalidDocument && ReferenceEquals(call.UserData, userData));

        Assert.Equal(HaruStatus.OK, HPdf.HPDF_NewDoc(pdf));
        Assert.True(HPdf.HPDF_HasDoc(pdf));

        HPdf.HPDF_Free(null);
        HPdf.HPDF_FreeDoc(null);
        HPdf.HPDF_FreeDocAll(null);
    }

    [Fact]
    public void PageFontAndTextWrappers_MapToPdfPageOperations()
    {
        using var pdf = HPdf.HPDF_New();
        var page = HPdf.HPDF_AddPage(pdf);
        var font = HPdf.HPDF_GetFont(pdf, "Helvetica");

        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetSize(page, PdfPageSize.Letter, PdfPageDirection.Portrait));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetWidth(page, 300));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetHeight(page, 400));
        Assert.Equal(300, HPdf.HPDF_Page_GetWidth(page));
        Assert.Equal(400, HPdf.HPDF_Page_GetHeight(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetFontAndSize(page, font, 12));
        Assert.Same(font, HPdf.HPDF_Page_GetCurrentFont(page));
        Assert.Equal(12, HPdf.HPDF_Page_GetCurrentFontSize(page));

        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_BeginText(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_MoveTextPos(page, 20, 30));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_ShowText(page, "wrapped text"));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetTextRenderingMode(page, PdfTextRenderingMode.Stroke));
        Assert.Equal(PdfTextRenderingMode.Stroke, HPdf.HPDF_Page_GetTextRenderingMode(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetCharSpace(page, 1));
        Assert.Equal(1, HPdf.HPDF_Page_GetCharSpace(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetWordSpace(page, 2));
        Assert.Equal(2, HPdf.HPDF_Page_GetWordSpace(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetHorizontalScalling(page, 90));
        Assert.Equal(90, HPdf.HPDF_Page_GetHorizontalScalling(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetTextLeading(page, 14));
        Assert.Equal(14, HPdf.HPDF_Page_GetTextLeading(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetTextRise(page, 3));
        Assert.Equal(3, HPdf.HPDF_Page_GetTextRise(page));
        Assert.Equal(3, HPdf.HPDF_Page_GetTextRaise(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_EndText(page));

        Assert.True(HPdf.HPDF_Page_TextWidth(page, "wrapped text") > 0);
        Assert.True(HPdf.HPDF_Page_MeasureText(page, "wrapped text", 200, true, out var realWidth) > 0);
        Assert.True(realWidth > 0);
        TestHelpers.AssertPdf(HPdf.HPDF_SaveToStream(pdf));
    }

    [Fact]
    public void GraphicsAndColorWrappers_MapToPdfPageOperations()
    {
        using var pdf = HPdf.HPDF_New();
        var page = HPdf.HPDF_AddPage(pdf);

        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_MoveTo(page, 10, 20));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_LineTo(page, 30, 40));
        Assert.Equal(new PdfPoint(30, 40), HPdf.HPDF_Page_GetCurrentPos(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_GetCurrentPos2(page, out var currentPos));
        Assert.Equal(new PdfPoint(30, 40), currentPos);
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_CurveTo(page, 1, 2, 3, 4, 5, 6));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_CurveTo2(page, 7, 8, 9, 10));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_CurveTo3(page, 11, 12, 13, 14));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_Rectangle(page, 0, 0, 10, 10));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_Circle(page, 10, 10, 5));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_Ellipse(page, 10, 10, 5, 3));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_Stroke(page));

        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_GSave(page));
        Assert.Equal(2u, HPdf.HPDF_Page_GetGStateDepth(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetLineWidth(page, 2));
        Assert.Equal(2, HPdf.HPDF_Page_GetLineWidth(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetLineCap(page, PdfLineCap.RoundEnd));
        Assert.Equal(PdfLineCap.RoundEnd, HPdf.HPDF_Page_GetLineCap(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetLineJoin(page, PdfLineJoin.BevelJoin));
        Assert.Equal(PdfLineJoin.BevelJoin, HPdf.HPDF_Page_GetLineJoin(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetMiterLimit(page, 4));
        Assert.Equal(4, HPdf.HPDF_Page_GetMiterLimit(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetDash(page, [2d, 3d, 99d], 2, 1));
        Assert.Equal(new[] { 2d, 3d }, HPdf.HPDF_Page_GetDash(page).Pattern);
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetFlat(page, 20));
        Assert.Equal(20, HPdf.HPDF_Page_GetFlat(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetRGBFill(page, 0.1, 0.2, 0.3));
        Assert.Equal(new PdfRgbColor(0.1, 0.2, 0.3), HPdf.HPDF_Page_GetRGBFill(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetRGBStroke(page, 0.4, 0.5, 0.6));
        Assert.Equal(new PdfRgbColor(0.4, 0.5, 0.6), HPdf.HPDF_Page_GetRGBStroke(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetGrayFill(page, 0.7));
        Assert.Equal(0.7, HPdf.HPDF_Page_GetGrayFill(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetGrayStroke(page, 0.8));
        Assert.Equal(0.8, HPdf.HPDF_Page_GetGrayStroke(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetCMYKFill(page, 0.1, 0.2, 0.3, 0.4));
        Assert.Equal(new PdfCmykColor(0.1, 0.2, 0.3, 0.4), HPdf.HPDF_Page_GetCMYKFill(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_SetCMYKStroke(page, 0.5, 0.6, 0.7, 0.8));
        Assert.Equal(new PdfCmykColor(0.5, 0.6, 0.7, 0.8), HPdf.HPDF_Page_GetCMYKStroke(page));
        Assert.Equal(PdfColorSpace.DeviceCmyk, HPdf.HPDF_Page_GetFillingColorSpace(page));
        Assert.Equal(PdfColorSpace.DeviceCmyk, HPdf.HPDF_Page_GetStrokingColorSpace(page));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_GRestore(page));
    }

    [Fact]
    public void ImageDestinationAndAnnotationWrappers_MapToPublicObjects()
    {
        using var pdf = HPdf.HPDF_New();
        var page = HPdf.HPDF_AddPage(pdf);
        var image = HPdf.HPDF_LoadRawImageFromMem(pdf, [255, 255, 255], 1, 1, PdfColorSpace.DeviceRgb);
        var mask = HPdf.HPDF_Image_LoadRaw1BitImageFromMem(pdf, [0b1000_0000], 1, 1, 1, true, true);

        Assert.True(HPdf.HPDF_Image_Validate(image));
        Assert.False(HPdf.HPDF_Image_Validate(null));
        Assert.Equal(new PdfPoint(1, 1), HPdf.HPDF_Image_GetSize(image));
        Assert.Equal(1u, HPdf.HPDF_Image_GetWidth(image));
        Assert.Equal(1u, HPdf.HPDF_Image_GetHeight(image));
        Assert.Equal(8u, HPdf.HPDF_Image_GetBitsPerComponent(image));
        Assert.Equal("DeviceRGB", HPdf.HPDF_Image_GetColorSpace(image));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Image_GetSize2(image, out var size));
        Assert.Equal(new PdfPoint(1, 1), size);
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Image_SetColorMask(image, 0, 255, 0, 255, 0, 255));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Image_SetMaskImage(image, mask));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Page_DrawImage(page, image, 10, 10, 20, 20));

        var destination = HPdf.HPDF_Page_CreateDestination(page);
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Destination_SetXYZ(destination, 0, 100, 1));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Destination_SetFit(destination));
        var link = HPdf.HPDF_Page_CreateLinkAnnot(page, new PdfRect(10, 10, 20, 20), destination);
        var text = HPdf.HPDF_Page_CreateTextAnnot(page, new PdfRect(20, 20, 30, 30), "note");
        var popup = HPdf.HPDF_Page_CreatePopupAnnot(page, new PdfRect(30, 30, 40, 40), text);

        Assert.Equal("Link", link.Subtype);
        Assert.Equal("Text", text.Subtype);
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_LinkAnnot_SetHighlightMode(link, PdfAnnotHighlightMode.DownAppearance));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_TextAnnot_SetIcon(text, PdfAnnotIcon.Note));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_TextAnnot_SetOpened(text, true));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_PopupAnnot_SetOpened(popup, false));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_Annot_SetRGBColor(text, new PdfRgbColor(0.1, 0.2, 0.3)));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_MarkupAnnot_SetPopup(text, popup));

        TestHelpers.AssertPdf(HPdf.HPDF_SaveToStream(pdf));
    }

    [Fact]
    public void StreamWrappers_ReadSavedBytesAndValidateBuffer()
    {
        using var pdf = HPdf.HPDF_New();
        var page = HPdf.HPDF_AddPage(pdf);
        var font = HPdf.HPDF_GetFont(pdf, "Helvetica");
        HPdf.HPDF_Page_SetFontAndSize(page, font, 12);
        HPdf.HPDF_Page_TextOut(page, 20, 40, "stream wrapper");

        var bytes = HPdf.HPDF_SaveToStream(pdf);
        var buffer = new byte[5];
        var size = 5u;

        Assert.Equal((uint)bytes.Length, HPdf.HPDF_GetStreamSize(pdf));
        Assert.Equal(HaruStatus.OK, HPdf.HPDF_ReadFromStream(pdf, buffer, ref size));
        Assert.Equal(5u, size);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(buffer));

        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter,
            () => HPdf.HPDF_ReadFromStream(pdf, null!, ref size));
    }
}
