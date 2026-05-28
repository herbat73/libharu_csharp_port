using LibHaru;
using static LibHaru.HPdf;

public static class ErrorModel
{
    public static void Test()
    {
        var callbackCount = 0;
        uint callbackError = 0;
        uint callbackDetail = 0;
        object? callbackUserData = null;
        var userData = new object();

        using var pdf = HPDF_New((errorNo, detailNo, data) =>
        {
            callbackCount++;
            callbackError = errorNo;
            callbackDetail = detailNo;
            callbackUserData = data;
        }, userData);

        pdf.Error.SetError(HaruStatus.InvalidDateTime, 42);
        Require(HPDF_CheckError(pdf) == HaruStatus.InvalidDateTime, "CheckError returned the wrong code.");
        Require(callbackCount == 1, "CheckError did not invoke the callback exactly once.");
        Require(callbackError == HaruStatus.InvalidDateTime, "Callback received the wrong error.");
        Require(callbackDetail == 42, "Callback received the wrong detail code.");
        Require(ReferenceEquals(callbackUserData, userData), "Callback received the wrong user data.");

        HPDF_ResetError(pdf);
        Require(HPDF_GetError(pdf) == HaruStatus.NoError, "ResetError did not clear the error code.");
        Require(HPDF_GetErrorDetail(pdf) == HaruStatus.NoError, "ResetError did not clear the detail code.");

        try
        {
            HPDF_GetPageByIndex(pdf, 0);
            throw new InvalidOperationException("Invalid page index did not throw.");
        }
        catch (HaruException ex) when (ex.Status == HaruStatus.InvalidPageIndex)
        {
            Require(HPDF_GetError(pdf) == HaruStatus.InvalidPageIndex, "Raised error was not recorded on the document.");
            Require(HPDF_GetErrorDetail(pdf) == HaruStatus.NoError, "Raised detail code should be zero.");
        }

        HPDF_ResetError(pdf);
        ExpectDocumentError(pdf, HaruStatus.InvalidCompressionMode, () => pdf.SetCompressionMode((CompressionMode)0x100));

        HPDF_ResetError(pdf);
        ExpectDocumentError(pdf, HaruStatus.PageLayoutOutOfRange, () => pdf.SetPageLayout((PdfPageLayout)999));

        HPDF_ResetError(pdf);
        ExpectDocumentError(pdf, HaruStatus.InvalidFontName, () => pdf.GetFont(null!));

        HPDF_ResetError(pdf);
        ExpectDocumentError(pdf, HaruStatus.InvalidEncodingName, () => pdf.GetFont("Helvetica", "DefinitelyMissingEncoding"));

        HPDF_ResetError(pdf);
        ExpectDocumentError(pdf, HaruStatus.MissingFileNameEntry, () => pdf.SaveToFile(""));

        HPDF_ResetError(pdf);
        var page = HPDF_AddPage(pdf);
        ExpectDocumentError(pdf, HaruStatus.RealOutOfRange, () => page.MoveTo(double.NaN, 10));

        HPDF_ResetError(pdf);
        var destination = HPDF_Page_CreateDestination(page);
        ExpectDocumentError(pdf, HaruStatus.InvalidParameter, () => destination.SetXYZ(0, 0, 0.01));

        HPDF_ResetError(pdf);
        var annotation = HPDF_Page_CreateTextAnnot(page, new PdfRect(10, 10, 40, 40), "note");
        ExpectDocumentError(pdf, HaruStatus.AnnotInvalidIcon, () => annotation.SetIcon((PdfAnnotIcon)999));

        HPDF_ResetError(pdf);
        var extGState = HPDF_CreateExtGState(pdf);
        ExpectDocumentError(pdf, HaruStatus.ExtGStateOutOfRange, () => extGState.SetAlphaFill(1.5));

        HPDF_ResetError(pdf);
        ExpectDocumentError(pdf, HaruStatus.InvalidImage, () => pdf.LoadRawImageFromMem([0], 0, 1, PdfColorSpace.DeviceRgb));

        HPDF_ResetError(pdf);
        var view = pdf.Create3DView("default");
        ExpectDocumentError(pdf, HaruStatus.NameOutOfRange, () => view.SetLighting(new string('A', 128)));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Error model parity smoke passed");
        Console.ResetColor();
    }

    private static void ExpectDocumentError(PdfDocument pdf, uint expectedStatus, Action action)
    {
        try
        {
            action();
            throw new InvalidOperationException($"Expected error 0x{expectedStatus:X4} was not raised.");
        }
        catch (HaruException ex) when (ex.Status == expectedStatus)
        {
            Require(HPDF_GetError(pdf) == expectedStatus, $"Document error was not set to 0x{expectedStatus:X4}.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
