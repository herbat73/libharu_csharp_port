using LibHaru;

namespace LibHaru.Tests;

public sealed class PdfPageTests
{
    [Fact]
    public void NewPage_DefaultsToA4Portrait()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        Assert.Equal(595.276, page.Width, precision: 3);
        Assert.Equal(841.89, page.Height, precision: 2);
        Assert.Equal(PdfGraphicsMode.PageDescription, page.GraphicsMode);
        Assert.Equal(new PdfPoint(0, 0), page.CurrentPosition);
        Assert.Equal(new PdfPoint(0, 0), page.CurrentTextPosition);
    }

    [Fact]
    public void PageSizeBoundaryAndRotate_UpdatePageState()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        page.SetSize(PdfPageSize.Letter, PdfPageDirection.Landscape);
        page.SetBoundary(PdfPageBoundary.MediaBox, new PdfRect(0, 0, 200, 100));
        page.SetRotate(180);
        page.SetZoom(1.5);
        page.SetWidth(300);
        page.SetHeight(400);

        Assert.Equal(300, page.Width);
        Assert.Equal(400, page.Height);
    }

    [Fact]
    public void PageSizeBoundaryAndRotate_RejectInvalidValues()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        TestHelpers.AssertHaruException(HaruStatus.PageInvalidSize, () => page.SetWidth(0));
        TestHelpers.AssertHaruException(HaruStatus.PageInvalidDirection,
            () => page.SetSize(PdfPageSize.A4, (PdfPageDirection)99));
        TestHelpers.AssertHaruException(HaruStatus.PageInvalidBoundary,
            () => page.SetBoundary((PdfPageBoundary)99, new PdfRect(0, 0, 10, 10)));
        TestHelpers.AssertHaruException(HaruStatus.PageInvalidRotateValue, () => page.SetRotate(45));
        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter, () => page.SetZoom(40));
    }

    [Fact]
    public void GraphicsStateSetters_UpdateReadableState()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        page.SetLineWidth(2);
        page.SetLineCap(PdfLineCap.RoundEnd);
        page.SetLineJoin(PdfLineJoin.BevelJoin);
        page.SetMiterLimit(5);
        page.SetDash([1, 2, 3], 4);
        page.SetFlat(10);
        page.SetRGBFill(0.1, 0.2, 0.3);
        page.SetRGBStroke(0.4, 0.5, 0.6);

        Assert.Equal(new PdfRgbColor(0.1, 0.2, 0.3), page.RgbFill);
        Assert.Equal(new PdfRgbColor(0.4, 0.5, 0.6), page.RgbStroke);

        page.SetGrayFill(0.7);
        page.SetGrayStroke(0.8);
        page.SetCMYKFill(0.1, 0.2, 0.3, 0.4);
        page.SetCMYKStroke(0.5, 0.6, 0.7, 0.8);
        page.Concat(1, 0, 0, 1, 10, 20);

        Assert.Equal(2, page.LineWidth);
        Assert.Equal(PdfLineCap.RoundEnd, page.LineCap);
        Assert.Equal(PdfLineJoin.BevelJoin, page.LineJoin);
        Assert.Equal(5, page.MiterLimit);
        Assert.Equal(new[] { 1d, 2d, 3d }, page.Dash.Pattern);
        Assert.Equal(4, page.Dash.Phase);
        Assert.Equal(10, page.Flatness);
        Assert.Equal(new PdfCmykColor(0.1, 0.2, 0.3, 0.4), page.CmykFill);
        Assert.Equal(new PdfCmykColor(0.5, 0.6, 0.7, 0.8), page.CmykStroke);
        Assert.Equal(PdfColorSpace.DeviceCmyk, page.FillingColorSpace);
        Assert.Equal(PdfColorSpace.DeviceCmyk, page.StrokingColorSpace);
        Assert.Equal(new PdfTransMatrix(1, 0, 0, 1, 10, 20), page.TransMatrix);
    }

    [Fact]
    public void GraphicsStateStack_RestoresSavedState()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.SetLineWidth(2);

        page.GSave();
        page.SetLineWidth(5);

        Assert.Equal(2u, page.GStateDepth);
        Assert.Equal(5, page.LineWidth);

        page.GRestore();

        Assert.Equal(1u, page.GStateDepth);
        Assert.Equal(2, page.LineWidth);
        TestHelpers.AssertHaruException(HaruStatus.PageCannotRestoreGstate, () => page.GRestore());
    }

    [Fact]
    public void PathOperations_UpdateCurrentPositionUntilPathIsPainted()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        page.MoveTo(10, 20);
        page.LineTo(30, 40);

        Assert.Equal(PdfGraphicsMode.PathObject, page.GraphicsMode);
        Assert.Equal(new PdfPoint(30, 40), page.CurrentPosition);

        page.Stroke();

        Assert.Equal(PdfGraphicsMode.PageDescription, page.GraphicsMode);
        Assert.Equal(new PdfPoint(0, 0), page.CurrentPosition);
    }

    [Fact]
    public void TextOperations_UpdateFontAndTextState()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        var font = document.GetFont("Helvetica");

        page.SetFontAndSize(font, 12);
        page.BeginText();
        page.SetCharSpace(1);
        page.SetWordSpace(2);
        page.SetHorizontalScalling(90);
        page.SetTextLeading(14);
        page.SetTextRise(3);
        page.SetTextRenderingMode(PdfTextRenderingMode.FillThenStroke);
        page.MoveTextPos(10, 20);
        page.ShowText("Hello");
        page.MoveToNextLine();
        page.ShowTextNextLineEx(4, 5, "World");
        page.EndText();

        Assert.Same(font, page.CurrentFont);
        Assert.Equal(12, page.CurrentFontSize);
        Assert.Equal(5, page.CharSpace);
        Assert.Equal(4, page.WordSpace);
        Assert.Equal(90, page.HorizontalScalling);
        Assert.Equal(14, page.TextLeading);
        Assert.Equal(3, page.TextRise);
        Assert.Equal(PdfTextRenderingMode.FillThenStroke, page.TextRenderingMode);
        Assert.Equal(new PdfPoint(0, 0), page.CurrentTextPosition);
    }

    [Fact]
    public void TextMeasurement_UsesCurrentFont()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.SetFontAndSize(document.GetFont("Helvetica"), 12);

        var width = page.TextWidth("Hello");
        var count = page.MeasureText("Hello world", width + 0.1, true, out var realWidth);

        Assert.True(width > 0);
        Assert.Equal(5, count);
        Assert.Equal(width, realWidth, precision: 6);
    }

    [Fact]
    public void TextRect_WritesTextAndReportsConsumedLength()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.SetFontAndSize(document.GetFont("Helvetica"), 12);
        page.BeginText();

        var status = page.TextRect(10, 100, 200, 10, "A line of text", PdfTextAlignment.Left, out var length);

        page.EndText();

        Assert.Equal(HaruStatus.OK, status);
        Assert.Equal(14u, length);
    }

    [Fact]
    public void TextOperations_RejectInvalidGraphicsMode()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        TestHelpers.AssertHaruException(HaruStatus.PageInvalidGmode, () => page.EndText());
        TestHelpers.AssertHaruException(HaruStatus.PageInvalidFont, () => page.SetFontAndSize(null!, 12));
        TestHelpers.AssertHaruException(HaruStatus.PageInvalidGmode, () => page.ShowText("outside text object"));
    }

    [Fact]
    public void DrawingHelpers_RejectInvalidDimensions()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        TestHelpers.AssertHaruException(HaruStatus.PageOutOfRange, () => page.Arc(0, 0, 10, 0, 360));
        TestHelpers.AssertHaruException(HaruStatus.PageOutOfRange, () => page.Circle(0, 0, 0));
        TestHelpers.AssertHaruException(HaruStatus.PageOutOfRange, () => page.Ellipse(0, 0, 1, 0));
        TestHelpers.AssertHaruException(HaruStatus.PageOutOfRange, () => page.SetDash([0], 0));
        TestHelpers.AssertHaruException(HaruStatus.PageOutOfRange, () => page.SetFlat(101));
        TestHelpers.AssertHaruException(HaruStatus.InvalidColorSpace, () => page.SetRGBFill(-0.1, 0, 0));
    }
}
