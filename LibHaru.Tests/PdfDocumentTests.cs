using LibHaru;

namespace LibHaru.Tests;

public sealed class PdfDocumentTests
{
    [Fact]
    public void NewDocument_HasExpectedDefaults()
    {
        using var document = new PdfDocument();

        Assert.True(document.HasDoc());
        Assert.Empty(document.Pages);
        Assert.Null(document.CurrentPage);
        Assert.Null(document.CurrentEncoder);
        Assert.Equal(0u, document.PagePerPages);
        Assert.Equal(CompressionMode.None, document.CompressionMode);
        Assert.Equal(PdfPageLayout.Single, document.PageLayout);
        Assert.Equal(PdfPageMode.UseNone, document.PageMode);
        Assert.Equal(PdfViewerPreference.None, document.ViewerPreference);
        Assert.Equal(PdfPdfAType.NonPdfA, document.PdfAType);
        Assert.False(document.IsEncrypted);
        Assert.Equal(HaruStatus.NoError, document.GetError());
    }

    [Fact]
    public void AddInsertAndGetPage_KeepPageOrderAndCurrentPage()
    {
        using var document = new PdfDocument();

        var first = document.AddPage();
        var inserted = document.InsertPage(first);

        Assert.Equal(2, document.Pages.Count);
        Assert.Same(inserted, document.Pages[0]);
        Assert.Same(first, document.Pages[1]);
        Assert.Same(inserted, document.GetPageByIndex(0));
        Assert.Same(first, document.GetPageByIndex(1));
        Assert.Same(inserted, document.CurrentPage);
    }

    [Fact]
    public void GetPageByIndex_WhenOutOfRange_ThrowsAndSetsDocumentError()
    {
        using var document = new PdfDocument();

        TestHelpers.AssertHaruException(HaruStatus.InvalidPageIndex, () => document.GetPageByIndex(0));

        Assert.Equal(HaruStatus.InvalidPageIndex, document.GetError());

        document.ResetError();

        Assert.Equal(HaruStatus.NoError, document.GetError());
    }

    [Fact]
    public void NewDoc_AfterFreeDoc_ReinitializesDocumentState()
    {
        using var document = new PdfDocument();
        document.SetCompressionMode(CompressionMode.Text);
        document.AddPage();

        document.FreeDoc();

        Assert.False(document.HasDoc());
        TestHelpers.AssertHaruException(HaruStatus.InvalidDocument, () => document.AddPage());

        document.NewDoc();

        Assert.True(document.HasDoc());
        Assert.Empty(document.Pages);
        Assert.Equal(CompressionMode.Text, document.CompressionMode);
        Assert.Equal(HaruStatus.NoError, document.GetError());
    }

    [Fact]
    public void FreeDocAll_ReinitializesDocumentAndClearsCompression()
    {
        using var document = new PdfDocument();
        document.SetCompressionMode(CompressionMode.All);
        document.AddPage();

        document.FreeDocAll();
        document.NewDoc();

        Assert.True(document.HasDoc());
        Assert.Empty(document.Pages);
        Assert.Equal(CompressionMode.None, document.CompressionMode);
    }

    [Fact]
    public void DocumentSettings_RoundTripThroughProperties()
    {
        using var document = new PdfDocument();

        document.SetPagesConfiguration(32);
        document.SetCompressionMode(CompressionMode.Text | CompressionMode.Image);
        document.SetPageLayout(PdfPageLayout.TwoColumnLeft);
        document.SetPageMode(PdfPageMode.UseOutline);
        document.SetViewerPreference(PdfViewerPreference.HideToolbar | PdfViewerPreference.FitWindow);

        Assert.Equal(32u, document.PagePerPages);
        Assert.Equal(CompressionMode.Text | CompressionMode.Image, document.CompressionMode);
        Assert.Equal(PdfPageLayout.TwoColumnLeft, document.PageLayout);
        Assert.Equal(PdfPageMode.UseOutline, document.PageMode);
        Assert.Equal(PdfViewerPreference.HideToolbar | PdfViewerPreference.FitWindow, document.ViewerPreference);
    }

    [Fact]
    public void DocumentSettings_RejectInvalidValues()
    {
        using var document = new PdfDocument();

        TestHelpers.AssertHaruException(HaruStatus.InvalidCompressionMode,
            () => document.SetCompressionMode((CompressionMode)0x10));
        TestHelpers.AssertHaruException(HaruStatus.PageLayoutOutOfRange,
            () => document.SetPageLayout((PdfPageLayout)99));
        TestHelpers.AssertHaruException(HaruStatus.PageModeOutOfRange,
            () => document.SetPageMode((PdfPageMode)99));
        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter,
            () => document.SetViewerPreference((PdfViewerPreference)1024));

        document.ResetError();
        document.AddPage();

        TestHelpers.AssertHaruException(HaruStatus.InvalidDocumentState,
            () => document.SetPagesConfiguration(4));
    }

    [Fact]
    public void InfoAttributes_RoundTripStringAndPdfDateValues()
    {
        using var document = new PdfDocument();
        var modified = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);

        document.SetInfoAttr(PdfInfoType.Title, "Unit test document");
        document.SetInfoDateAttr(PdfInfoType.ModDate, modified);

        Assert.Equal("Unit test document", document.GetInfoAttr(PdfInfoType.Title));
        Assert.Equal("D:20240102030405+00'00'", document.GetInfoAttr(PdfInfoType.ModDate));
        Assert.NotNull(document.GetInfoAttr(PdfInfoType.Producer));
    }

    [Fact]
    public void SaveToStream_GetContentsAndReadFromStream_ProducePdfBytes()
    {
        using var document = CreateTextDocument("Hello from tests");

        var bytes = document.SaveToStream();

        TestHelpers.AssertPdf(bytes);
        Assert.Contains("Hello from tests", TestHelpers.PdfText(bytes));
        Assert.Equal((uint)bytes.Length, document.GetStreamSize());
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(document.ReadFromStream(5)));
        Assert.Equal(bytes, document.GetContents());

        document.ResetStream();

        Assert.Equal(0u, document.GetStreamSize());
    }

    [Fact]
    public void SaveToFile_CreatesParentDirectoriesAndPdfFile()
    {
        using var document = CreateTextDocument("Saved to a file");
        var path = TestHelpers.NewArtifactPath(Path.Combine("nested", "document.pdf"));

        document.SaveToFile(path);

        var bytes = File.ReadAllBytes(path);
        TestHelpers.AssertPdf(bytes);
        Assert.Contains("Saved to a file", TestHelpers.PdfText(bytes));
    }

    [Fact]
    public void Save_RejectsNonWritableStream()
    {
        using var document = CreateTextDocument("stream");
        var output = new MemoryStream();
        output.Dispose();

        TestHelpers.AssertHaruException(HaruStatus.InvalidStream, () => document.Save(output));
    }

    [Fact]
    public void EncryptionSettings_RequirePasswordAndCanBeDisabled()
    {
        using var document = new PdfDocument();

        TestHelpers.AssertHaruException(HaruStatus.DocEncryptDictNotFound,
            () => document.SetPermission(Permission.EnablePrint));

        document.SetPassword("owner", "user");
        document.SetPermission(Permission.EnablePrint | Permission.EnableCopy);
        document.SetEncryptionMode(PdfEncryptMode.R3, 16);

        Assert.True(document.IsEncrypted);

        document.SetEncryptOff();

        Assert.False(document.IsEncrypted);
    }

    [Fact]
    public void PdfAConformance_RequiresOutputIntentWhenSaving()
    {
        using var document = new PdfDocument();
        document.AddPage();
        document.SetPdfAConformance(PdfPdfAType.PdfA3B);

        TestHelpers.AssertHaruException(HaruStatus.InvalidDocumentState, () => document.SaveToStream());

        document.ResetError();
        document.AppendOutputIntent("sRGB", [1, 2, 3], "Test profile");

        TestHelpers.AssertPdf(document.SaveToStream());
        Assert.Equal(PdfPdfAType.PdfA3B, document.PdfAType);
    }

    [Fact]
    public void EmbeddedFilesAndCustomMetadata_CanBeWrittenToPdf()
    {
        using var document = new PdfDocument();
        document.AddPage();
        document.SetXmpMetadata("<x:xmpmeta>unit-test</x:xmpmeta>");
        var attachmentPath = TestHelpers.NewArtifactPath("attachment.txt");
        File.WriteAllText(attachmentPath, "attachment content");

        var embeddedFile = document.AttachFile(attachmentPath);
        embeddedFile.SetName("renamed.txt");
        embeddedFile.SetDescription("Test attachment");
        embeddedFile.SetSubtype("text/plain");
        embeddedFile.SetAFRelationship(PdfAFRelationship.Data);
        embeddedFile.SetSize(18);
        embeddedFile.SetCreationDate(new DateTimeOffset(2024, 2, 3, 4, 5, 6, TimeSpan.Zero));
        embeddedFile.SetLastModificationDate(new DateTimeOffset(2024, 2, 4, 4, 5, 6, TimeSpan.Zero));

        var bytes = document.SaveToStream();

        TestHelpers.AssertPdf(bytes);
        Assert.Contains("renamed.txt", TestHelpers.PdfText(bytes));
        Assert.Contains("Test attachment", TestHelpers.PdfText(bytes));
    }

    private static PdfDocument CreateTextDocument(string text)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var font = document.GetFont("Helvetica");
        page.SetFontAndSize(font, 12);
        page.TextOut(20, 40, text);
        return document;
    }
}
