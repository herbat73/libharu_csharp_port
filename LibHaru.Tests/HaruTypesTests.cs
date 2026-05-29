using LibHaru;

namespace LibHaru.Tests;

public sealed class HaruTypesTests
{
    [Fact]
    public void HaruException_StoresPrimaryAndDetailStatuses()
    {
        var exception = new HaruException(HaruStatus.InvalidStream, HaruStatus.StreamEof, "stream failed");

        Assert.Equal(HaruStatus.InvalidStream, exception.Status);
        Assert.Equal(HaruStatus.StreamEof, exception.DetailStatus);
        Assert.Equal("stream failed", exception.Message);
    }

    [Fact]
    public void HaruException_DefaultsDetailStatusToNoError()
    {
        var exception = new HaruException(HaruStatus.InvalidPage, "page failed");

        Assert.Equal(HaruStatus.InvalidPage, exception.Status);
        Assert.Equal(HaruStatus.NoError, exception.DetailStatus);
    }

    [Fact]
    public void VersionConstants_MatchManagedPortVersion()
    {
        Assert.Equal(2, HaruVersion.Major);
        Assert.Equal(4, HaruVersion.Minor);
        Assert.Equal(6, HaruVersion.Bugfix);
        Assert.Equal("2.4.6-managed", HaruVersion.Text);
        Assert.Equal(HaruVersion.Text, HPdf.HPDF_GetVersion());
    }

    [Fact]
    public void HaruStatus_CStyleAliasesMatchManagedConstants()
    {
        Assert.Equal(HaruStatus.ArrayCountErr, HaruStatus.HPDF_ARRAY_COUNT_ERR);
        Assert.Equal(HaruStatus.FailedToAllocMem, HaruStatus.HPDF_FAILD_TO_ALLOC_MEM);
        Assert.Equal(HaruStatus.TtfInvalidFormat, HaruStatus.HPDF_TTF_INVALID_FOMAT);
        Assert.Equal(HaruStatus.UnsupportedFunction, HaruStatus.UnsupportedFeature);
        Assert.Equal(HaruStatus.NoError, HaruStatus.OK);
    }

    [Fact]
    public void MatrixAndColorConstants_ExposeExpectedValues()
    {
        Assert.Equal(new PdfTransMatrix(1, 0, 0, 1, 0, 0), PdfTransMatrix.Identity);
        Assert.Equal(new Pdf3DMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0), Pdf3DMatrix.Identity);
        Assert.Equal(new PdfRgbColor(0, 0, 0), PdfRgbColor.Black);
        Assert.Equal(new PdfCmykColor(0, 0, 0, 1), PdfCmykColor.Black);
    }

    [Fact]
    public void PdfDashMode_CopiesPatternAndExposesCount()
    {
        var pattern = new List<double> { 1, 2 };
        var dash = new PdfDashMode(pattern, 3);

        pattern[0] = 99;

        Assert.Equal(2u, dash.Count);
        Assert.Equal(3, dash.Phase);
        Assert.Equal(new[] { 1d, 2d }, dash.Pattern);
        Assert.Empty(PdfDashMode.Solid.Pattern);
        Assert.Equal(0u, PdfDashMode.Solid.Count);
    }
}
