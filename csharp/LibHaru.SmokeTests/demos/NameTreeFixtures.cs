using System.Text;
using LibHaru;
using static LibHaru.HPdf;

public static class NameTreeFixtures
{
    public static void Test(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.None);

        var page = HPDF_AddPage(pdf);
        var font = HPDF_GetFont(pdf, "Helvetica");
        HPDF_Page_SetFontAndSize(page, font, 12);
        HPDF_Page_TextOut(page, 48, HPDF_Page_GetHeight(page) - 72, "Large name-tree fixture");

        var destination = HPDF_Page_CreateDestination(page);
        HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(page), 1);

        var javaScript = HPDF_CreateJavaScript(pdf, "app.alert('large name tree');");
        var attachmentPath = Path.Combine(repoRoot, "demo", "pdf_a", "factur-x.xml");

        for (var i = 0; i < 150; i++)
        {
            var suffix = i.ToString("D3");
            HPDF_AddNamedDestination(pdf, $"dest-{suffix}", destination);
            HPDF_AddNamedJavaScript(pdf, $"script-{suffix}", javaScript);

            var embedded = HPDF_AttachFile(pdf, attachmentPath);
            HPDF_EmbeddedFile_SetName(embedded, $"fixture-{suffix}.xml");
            HPDF_EmbeddedFile_SetDescription(embedded, $"Large name tree fixture {suffix}");
        }

        HPDF_SaveToFile(pdf, pdfPath);

        var bytes = File.ReadAllBytes(pdfPath);
        var latin1 = Encoding.Latin1.GetString(bytes);

        Require(latin1.Contains("/Names", StringComparison.Ordinal), "Missing catalog Names dictionary.");
        Require(latin1.Contains("/Dests", StringComparison.Ordinal), "Missing Dests name tree.");
        Require(latin1.Contains("/JavaScript", StringComparison.Ordinal), "Missing JavaScript name tree.");
        Require(latin1.Contains("/EmbeddedFiles", StringComparison.Ordinal), "Missing EmbeddedFiles name tree.");
        Require(Count(latin1, "/Kids [") >= 3, "Expected multi-leaf name trees for large fixtures.");
        Require(Count(latin1, "/Limits [") >= 12, "Expected root and leaf Limits entries across large name trees.");
        Require(Count(latin1, "/Names [") >= 9, "Expected leaf Names arrays across large name trees.");
        Require(latin1.Contains("(dest-000)", StringComparison.Ordinal), "Missing first destination name.");
        Require(latin1.Contains("(dest-149)", StringComparison.Ordinal), "Missing last destination name.");
        Require(latin1.Contains("(script-000)", StringComparison.Ordinal), "Missing first JavaScript name.");
        Require(latin1.Contains("(script-149)", StringComparison.Ordinal), "Missing last JavaScript name.");
        Require(latin1.Contains("(fixture-000.xml)", StringComparison.Ordinal), "Missing first embedded-file name.");
        Require(latin1.Contains("(fixture-149.xml)", StringComparison.Ordinal), "Missing last embedded-file name.");

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"{bytes.Length} bytes with large name-tree fixtures");
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
