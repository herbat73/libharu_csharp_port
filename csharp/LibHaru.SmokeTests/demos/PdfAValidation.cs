using System.Text;
using LibHaru;
using static LibHaru.HPdf;

public static class PdfAValidation
{
    public static void Test(string repoRoot)
    {
        using (var missingIntent = HPDF_New())
        {
            HPDF_AddPage(missingIntent);
            HPDF_PDFA_SetPDFAConformance(missingIntent, PdfPdfAType.PdfA1B);
            RequireThrows(HaruStatus.InvalidDocumentState, () => HPDF_SaveToStream(missingIntent));
        }

        using (var encrypted = HPDF_New())
        {
            HPDF_AddPage(encrypted);
            HPDF_AppendOutputIntents(encrypted, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_SetPassword(encrypted, "owner", "user");
            HPDF_PDFA_SetPDFAConformance(encrypted, PdfPdfAType.PdfA1B);
            RequireThrows(HaruStatus.InvalidDocumentState, () => HPDF_SaveToStream(encrypted));
        }

        using (var wrongMetadata = HPDF_New())
        {
            HPDF_AddPage(wrongMetadata);
            HPDF_AppendOutputIntents(wrongMetadata, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_PDFA_SetPDFAConformance(wrongMetadata, PdfPdfAType.PdfA1B);
            HPDF_SetXmpMetadata(wrongMetadata, """
                <x:xmpmeta xmlns:x='adobe:ns:meta/'>
                  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
                    <rdf:Description rdf:about='' xmlns:pdfaid='http://www.aiim.org/pdfa/ns/id/'>
                      <pdfaid:part>2</pdfaid:part>
                      <pdfaid:conformance>B</pdfaid:conformance>
                    </rdf:Description>
                  </rdf:RDF>
                </x:xmpmeta>
                """);
            RequireThrows(HaruStatus.InvalidDocumentState, () => HPDF_SaveToStream(wrongMetadata));
        }

        using (var pdfA4 = HPDF_New())
        {
            HPDF_AddPage(pdfA4);
            HPDF_AppendOutputIntents(pdfA4, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_PDFA_SetPDFAConformance(pdfA4, PdfPdfAType.PdfA4F);
            var bytes = HPDF_SaveToStream(pdfA4);
            var latin1 = Encoding.Latin1.GetString(bytes);
            Require(latin1.StartsWith("%PDF-2.0", StringComparison.Ordinal), "PDF/A-4 should bump the PDF header to 2.0.");
            Require(latin1.Contains("<pdfaid:rev>2020</pdfaid:rev>", StringComparison.Ordinal), "PDF/A-4 XMP should include pdfaid:rev 2020.");
        }

        using (var embeddedInPdfA1 = HPDF_New())
        {
            HPDF_AddPage(embeddedInPdfA1);
            HPDF_AppendOutputIntents(embeddedInPdfA1, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_AttachFile(embeddedInPdfA1, Path.Combine(repoRoot, "demo", "pdf_a", "factur-x.xml"));
            HPDF_PDFA_SetPDFAConformance(embeddedInPdfA1, PdfPdfAType.PdfA1B);
            RequireThrows(HaruStatus.InvalidDocumentState, () => HPDF_SaveToStream(embeddedInPdfA1));
        }

        using (var embeddedInPdfA3 = HPDF_New())
        {
            HPDF_AddPage(embeddedInPdfA3);
            HPDF_AppendOutputIntents(embeddedInPdfA3, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_AttachFile(embeddedInPdfA3, Path.Combine(repoRoot, "demo", "pdf_a", "factur-x.xml"));
            HPDF_PDFA_SetPDFAConformance(embeddedInPdfA3, PdfPdfAType.PdfA3B);
            var latin1 = Encoding.Latin1.GetString(HPDF_SaveToStream(embeddedInPdfA3));
            Require(latin1.Contains("/AFRelationship /Unspecified", StringComparison.Ordinal), "PDF/A-3 embedded file should get a default AFRelationship.");
            Require(latin1.Contains("/AF [", StringComparison.Ordinal), "PDF/A-3 embedded file should be associated from the catalog.");
        }

        foreach (var (type, header, part, conformance, requiresRevision) in PdfAIdentificationCases())
        {
            using var pdf = HPDF_New();
            HPDF_AddPage(pdf);
            HPDF_AppendOutputIntents(pdf, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_PDFA_SetPDFAConformance(pdf, type);
            var latin1 = Encoding.Latin1.GetString(HPDF_SaveToStream(pdf));
            Require(latin1.StartsWith(header, StringComparison.Ordinal), $"{type} should use PDF header {header}.");
            Require(latin1.Contains($"<pdfaid:part>{part}</pdfaid:part>", StringComparison.Ordinal), $"{type} XMP part mismatch.");
            Require(latin1.Contains($"<pdfaid:conformance>{conformance}</pdfaid:conformance>", StringComparison.Ordinal), $"{type} XMP conformance mismatch.");

            if (requiresRevision)
                Require(latin1.Contains("<pdfaid:rev>2020</pdfaid:rev>", StringComparison.Ordinal), $"{type} should include pdfaid:rev 2020.");
            else
                Require(!latin1.Contains("<pdfaid:rev>", StringComparison.Ordinal), $"{type} should not include a PDF/A revision.");
        }

        using (var pdfA4MissingRevision = HPDF_New())
        {
            HPDF_AddPage(pdfA4MissingRevision);
            HPDF_AppendOutputIntents(pdfA4MissingRevision, "sRGB", [1, 2, 3, 4], "sRGB");
            HPDF_PDFA_SetPDFAConformance(pdfA4MissingRevision, PdfPdfAType.PdfA4);
            HPDF_SetXmpMetadata(pdfA4MissingRevision, """
                <x:xmpmeta xmlns:x='adobe:ns:meta/'>
                  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
                    <rdf:Description rdf:about='' xmlns:pdfaid='http://www.aiim.org/pdfa/ns/id/'>
                      <pdfaid:part>4</pdfaid:part>
                      <pdfaid:conformance></pdfaid:conformance>
                    </rdf:Description>
                  </rdf:RDF>
                </x:xmpmeta>
                """);
            RequireThrows(HaruStatus.InvalidDocumentState, () => HPDF_SaveToStream(pdfA4MissingRevision));
        }

        Console.WriteLine("PDF/A standalone rule smoke passed");
    }

    private static (PdfPdfAType Type, string Header, string Part, string Conformance, bool RequiresRevision)[] PdfAIdentificationCases() =>
    [
        (PdfPdfAType.PdfA1A, "%PDF-1.4", "1", "A", false),
        (PdfPdfAType.PdfA1B, "%PDF-1.4", "1", "B", false),
        (PdfPdfAType.PdfA2A, "%PDF-1.7", "2", "A", false),
        (PdfPdfAType.PdfA2B, "%PDF-1.7", "2", "B", false),
        (PdfPdfAType.PdfA2U, "%PDF-1.7", "2", "U", false),
        (PdfPdfAType.PdfA3A, "%PDF-1.7", "3", "A", false),
        (PdfPdfAType.PdfA3B, "%PDF-1.7", "3", "B", false),
        (PdfPdfAType.PdfA3U, "%PDF-1.7", "3", "U", false),
        (PdfPdfAType.PdfA4, "%PDF-2.0", "4", string.Empty, true),
        (PdfPdfAType.PdfA4E, "%PDF-2.0", "4", "E", true),
        (PdfPdfAType.PdfA4F, "%PDF-2.0", "4", "F", true)
    ];

    private static void RequireThrows(uint status, Action action)
    {
        try
        {
            action();
        }
        catch (HaruException ex) when (ex.Status == status)
        {
            return;
        }

        throw new InvalidOperationException($"Expected Haru status 0x{status:X4}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
