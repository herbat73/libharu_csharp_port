using LibHaru;

namespace LibHaru.Tests;

public sealed class PdfFontAndEncoderTests
{
    [Fact]
    public void GetFont_ReturnsCachedBase14FontWithMetrics()
    {
        using var document = new PdfDocument();

        var font = document.GetFont("Helvetica");

        Assert.Same(font, document.GetFont("Helvetica"));
        Assert.Equal("Helvetica", font.BaseFont);
        Assert.Equal("StandardEncoding", font.Encoding);
        Assert.True(font.Ascent > 0);
        Assert.True(font.Descent < 0);
        Assert.True(font.CapHeight > 0);
        Assert.True(font.BBox.Right > font.BBox.Left);
    }

    [Fact]
    public void Font_TextWidthAndMeasureText_ReportExpectedValues()
    {
        using var document = new PdfDocument();
        var font = document.GetFont("Helvetica");

        var width = font.TextWidth("Hello", 12);
        var info = font.TextWidthInfo("Hello world");
        var count = font.MeasureText("Hello world", width + 0.1, 12, 0, 0, false, out var realWidth);

        Assert.True(width > 0);
        Assert.Equal(11u, info.NumChars);
        Assert.Equal(1u, info.NumSpace);
        Assert.True(info.Width > 0);
        Assert.Equal(5u, count);
        Assert.Equal(width, realWidth, precision: 6);
        Assert.True(font.GetUnicodeWidth('A') > 0);
    }

    [Fact]
    public void Font_MethodsRejectInvalidInputs()
    {
        using var document = new PdfDocument();
        var font = document.GetFont("Helvetica");

        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter, () => font.TextWidth(null!, 12));
        TestHelpers.AssertHaruException(HaruStatus.PageInvalidFontSize, () => font.TextWidth("text", 0));
        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter,
            () => font.MeasureText("text", 0, 12, 0, 0, false, out _));
    }

    [Fact]
    public void Encoder_ExposesEncodingMetadataAndUnicodeMapping()
    {
        using var document = new PdfDocument();

        var encoder = document.GetEncoder("StandardEncoding");

        Assert.Same(encoder, document.GetEncoder("StandardEncoding"));
        Assert.Equal("StandardEncoding", encoder.Name);
        Assert.Equal(PdfEncoderType.SingleByte, encoder.Type);
        Assert.Equal(PdfWritingMode.Horizontal, encoder.WritingMode);
        Assert.Equal(PdfByteType.Single, encoder.GetByteType("ABC", 1));
        Assert.Equal((ushort)'A', encoder.GetUnicode((ushort)'A'));
    }

    [Fact]
    public void SetCurrentEncoder_StoresResolvedEncoder()
    {
        using var document = new PdfDocument();

        document.SetCurrentEncoder("UTF8");

        Assert.NotNull(document.CurrentEncoder);
        Assert.Equal("UTF-8", document.CurrentEncoder.Name);
        Assert.Equal(PdfEncoderType.DoubleByte, document.CurrentEncoder.Type);
    }

    [Fact]
    public void GetEncoder_RejectsUnknownEncoding()
    {
        using var document = new PdfDocument();

        TestHelpers.AssertHaruException(HaruStatus.InvalidEncodingName, () => document.GetEncoder("missing"));
        Assert.Equal(HaruStatus.InvalidEncodingName, document.GetError());
    }

    [Fact]
    public void UsePredefinedEncodingAndFontFamilies_DoesNotThrow()
    {
        using var document = new PdfDocument();

        document.UseJPEncodings();
        document.UseKREncodings();
        document.UseCNSEncodings();
        document.UseCNTEncodings();
        document.UseUTFEncodings();
        document.UseJPFonts();
        document.UseKRFonts();
        document.UseCNSFonts();
        document.UseCNTFonts();

        Assert.Equal(HaruStatus.NoError, document.GetError());
    }
}
