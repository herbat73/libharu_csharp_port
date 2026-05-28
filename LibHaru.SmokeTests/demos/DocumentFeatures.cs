using System.Text;
using LibHaru;
using static LibHaru.HPdf;

public static class DocumentFeatures
{
    public static void Test(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.All);
        HPDF_SetPageMode(pdf, PdfPageMode.UseOutline);
        HPDF_SetViewerPreference(pdf,
            PdfViewerPreference.HideToolbar | PdfViewerPreference.FitWindow | PdfViewerPreference.PrintScalingNone);
        HPDF_AddPageLabel(pdf, 0, PdfPageNumStyle.LowerRoman, 1, "preface-");
        HPDF_AddPageLabel(pdf, 1, PdfPageNumStyle.Decimal, 1, "body-");

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(page, font, 12);
        HPDF_Page_TextOut(page, 50, HPDF_Page_GetHeight(page) - 60, "Document features smoke");

        var second = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(second, font, 12);
        HPDF_Page_TextOut(second, 50, HPDF_Page_GetHeight(second) - 60, "Destination target");

        var destination = HPDF_Page_CreateDestination(second);
        HPDF_Destination_SetXYZ(destination, 0, HPDF_Page_GetHeight(second), 1);
        HPDF_SetOpenAction(pdf, destination);
        HPDF_AddNamedDestination(pdf, "target-page", destination);

        var root = HPDF_CreateOutline(pdf, null, "Document features");
        HPDF_Outline_SetOpened(root, true);
        var child = HPDF_CreateOutline(pdf, root, "Target page");
        HPDF_Outline_SetDestination(child, destination);

        var link = HPDF_Page_CreateLinkAnnot(page, new PdfRect(45, 735, 220, 760), destination);
        HPDF_LinkAnnot_SetHighlightMode(link, PdfAnnotHighlightMode.InvertBorder);
        HPDF_LinkAnnot_SetBorderStyle(link, 1, 2, 2);

        HPDF_Page_CreateURILinkAnnot(page, new PdfRect(45, 700, 260, 724), "https://libharu.org");
        var textAnnot = HPDF_Page_CreateTextAnnot(page, new PdfRect(270, 700, 310, 740), "Managed text annotation");
        HPDF_TextAnnot_SetIcon(textAnnot, PdfAnnotIcon.Note);
        HPDF_TextAnnot_SetOpened(textAnnot, true);
        HPDF_MarkupAnnot_SetTitle(textAnnot, "Managed title");
        HPDF_MarkupAnnot_SetSubject(textAnnot, "Managed subject");
        HPDF_MarkupAnnot_SetCreationDate(textAnnot, new DateTimeOffset(2026, 5, 26, 13, 0, 0, TimeSpan.Zero));
        var popup = HPDF_Page_CreatePopupAnnot(page, new PdfRect(315, 690, 430, 760), textAnnot);
        HPDF_PopupAnnot_SetOpened(popup, true);

        var highlight = HPDF_Page_CreateHighlightAnnot(page, new PdfRect(45, 660, 240, 680), "Highlight annotation");
        HPDF_TextMarkupAnnot_SetQuadPoints(highlight, new PdfPoint(45, 660), new PdfPoint(240, 660),
            new PdfPoint(240, 680), new PdfPoint(45, 680));
        var highlightPopup = HPDF_Page_CreatePopupAnnot(page, new PdfRect(45, 685, 240, 735), highlight);
        HPDF_MarkupAnnot_SetPopup(highlight, highlightPopup);

        var freeText = HPDF_Page_CreateFreeTextAnnot(page, new PdfRect(270, 645, 430, 680), "Callout annotation");
        HPDF_FreeTextAnnot_SetDefaultStyle(freeText, "font: Helvetica 10pt; color: #003366");
        HPDF_MarkupAnnot_SetTransparency(freeText, 0.4);
        HPDF_MarkupAnnot_SetIntent(freeText, PdfAnnotIntent.FreeTextCallout);
        HPDF_FreeTextAnnot_Set3PointCalloutLine(freeText, new PdfPoint(270, 645), new PdfPoint(250, 625),
            new PdfPoint(230, 650));
        HPDF_FreeTextAnnot_SetLineEndingStyle(freeText, PdfAnnotLineEndingStyle.OpenArrow,
            PdfAnnotLineEndingStyle.None);
        HPDF_Annot_SetAppearance(
            freeText,
            PdfAnnotationAppearanceState.Normal,
            "0.95 g 0 0 140 30 re f BT /Helv 10 Tf 4 11 Td (Callout AP) Tj ET",
            new PdfRect(0, 0, 140, 30),
            fonts: new Dictionary<string, PdfFont> { ["Helv"] = font });

        var freeText2 = HPDF_Page_CreateFreeTextAnnot(page, new PdfRect(445, 645, 560, 680), "2-point callout");
        HPDF_FreeTextAnnot_Set2PointCalloutLine(freeText2, new PdfPoint(445, 645), new PdfPoint(420, 625));
        HPDF_FreeTextAnnot_SetLineEndingStyle(freeText2, PdfAnnotLineEndingStyle.None,
            PdfAnnotLineEndingStyle.OpenArrow);

        var lineAnnot = HPDF_Page_CreateLineAnnot(page, new PdfRect(45, 615, 240, 640), "Line annotation", null,
            new PdfPoint(50, 620), new PdfPoint(230, 635));
        HPDF_LineAnnot_SetPosition(lineAnnot, new PdfPoint(50, 620), PdfAnnotLineEndingStyle.OpenArrow,
            new PdfPoint(230, 635), PdfAnnotLineEndingStyle.ClosedArrow);
        HPDF_LineAnnot_SetLeader(lineAnnot, 8, 4, 2);
        HPDF_LineAnnot_SetCaption(lineAnnot, true, PdfLineAnnotCapPosition.Top, 2, 3);

        var square = HPDF_Page_CreateSquareAnnot(page, new PdfRect(270, 600, 330, 630), "Square annotation");
        HPDF_MarkupAnnot_SetInteriorRGBColor(square, new PdfRgbColor(0.9, 0.95, 1));
        HPDF_MarkupAnnot_SetCloudEffect(square, 2);
        HPDF_MarkupAnnot_SetRectDiff(square, new PdfRect(2, 2, 2, 2));

        var circle = HPDF_Page_CreateCircleAnnot(page, new PdfRect(270, 560, 330, 590), "Circle annotation");
        HPDF_MarkupAnnot_SetInteriorCMYKColor(circle, new PdfCmykColor(0.1, 0.2, 0, 0));
        var grayCircle = HPDF_Page_CreateCircleAnnot(page, new PdfRect(340, 560, 400, 590), "Gray circle annotation");
        HPDF_MarkupAnnot_SetInteriorGrayColor(grayCircle, 0.75);
        var transparentCircle =
            HPDF_Page_CreateCircleAnnot(page, new PdfRect(410, 560, 470, 590), "Transparent circle annotation");
        HPDF_MarkupAnnot_SetInteriorRGBColor(transparentCircle, new PdfRgbColor(1, 0.9, 0.2));
        HPDF_MarkupAnnot_SetInteriorTransparent(transparentCircle);

        HPDF_Page_CreateStampAnnot(page, new PdfRect(350, 600, 450, 630), "Approved", "Approved stamp");
        var widget = HPDF_Page_CreateWidgetAnnot(page, new PdfRect(460, 600, 520, 630));
        var widgetAppearanceResource = HPDF_Page_CreateXObjectAsWhiteRect(pdf, page, new PdfRect(0, 0, 60, 30));
        HPDF_Annot_SetAppearance(
            widget,
            PdfAnnotationAppearanceState.Normal,
            "q /WhitePatch Do Q 0 0 1 RG 1 w 0 0 60 30 re S",
            new PdfRect(0, 0, 60, 30),
            "Off",
            new Dictionary<string, PdfXObject> { ["WhitePatch"] = widgetAppearanceResource });
        HPDF_Annot_SetAppearance(widget, PdfAnnotationAppearanceState.Normal, "0.8 0.9 1 rg 0 0 60 30 re f",
            new PdfRect(0, 0, 60, 30), "Yes");
        HPDF_Annot_SetAppearance(widget, PdfAnnotationAppearanceState.Rollover, "0.9 1 0.9 rg 0 0 60 30 re f",
            new PdfRect(0, 0, 60, 30), "Yes");
        HPDF_Annot_SetAppearance(widget, PdfAnnotationAppearanceState.Down, "0.7 0.8 1 rg 0 0 60 30 re f",
            new PdfRect(0, 0, 60, 30), "Yes");

        var javaScript = HPDF_CreateJavaScript(pdf, "app.alert('managed libharu');");
        HPDF_AddNamedJavaScript(pdf, "welcome", javaScript);
        HPDF_LinkAnnot_SetJavaScript(link, javaScript);

        var embedded = HPDF_AttachFile(pdf, Path.Combine(repoRoot, "demo", "pdf_a", "factur-x.xml"));
        HPDF_EmbeddedFile_SetName(embedded, "factur-x.xml");
        HPDF_EmbeddedFile_SetDescription(embedded, "Factur-X invoice");
        HPDF_EmbeddedFile_SetSubtype(embedded, "text/xml");
        HPDF_EmbeddedFile_SetAFRelationship(embedded, PdfAFRelationship.Data);
        HPDF_EmbeddedFile_SetCreationDate(embedded, new DateTimeOffset(2026, 5, 26, 12, 0, 0, TimeSpan.Zero));
        HPDF_EmbeddedFile_SetLastModificationDate(embedded, new DateTimeOffset(2026, 5, 26, 12, 30, 0, TimeSpan.Zero));

        var icc = File.ReadAllBytes(Path.Combine(repoRoot, "demo", "pdf_a", "device_rgb.icc"));
        HPDF_AppendOutputIntents(pdf, "sRGB", icc, "sRGB IEC61966-2.1");

        var state = HPDF_CreateExtGState(pdf);
        HPDF_ExtGState_SetAlphaStroke(state, 0.65);
        HPDF_ExtGState_SetAlphaFill(state, 0.35);
        HPDF_ExtGState_SetBlendMode(state, PdfBlendMode.Multiply);
        HPDF_Page_SetExtGState(page, state);
        HPDF_Page_Rectangle(page, 45, 600, 100, 35);
        HPDF_Page_Fill(page);

        var shading = HPDF_Shading_New(pdf, PdfShadingType.FreeFormTriangleMesh, PdfColorSpace.DeviceRgb, 0, 200, 0,
            200);
        HPDF_Shading_AddVertexRGB(shading, PdfShadingFreeFormTriangleMeshEdgeFlag.NoConnection, 0, 0, 255, 0, 0);
        HPDF_Shading_AddVertexRGB(shading, PdfShadingFreeFormTriangleMeshEdgeFlag.ConnectPrevious, 200, 0, 0, 255, 0);
        HPDF_Shading_AddVertexRGB(shading, PdfShadingFreeFormTriangleMeshEdgeFlag.ConnectPreviousSecond, 100, 180, 0, 0,
            255);
        HPDF_Page_SetShading(page, shading);
        HPDF_Page_SetShading(page,
            HPDF_Shading_NewAxial(pdf, new PdfPoint(45, 555), new PdfPoint(220, 555), new PdfRgbColor(1, 0, 0),
                new PdfRgbColor(0, 0, 1), true, true));
        HPDF_Page_SetShading(page,
            HPDF_Shading_NewRadial(pdf, new PdfPoint(325, 555), 5, new PdfPoint(385, 555), 65, new PdfRgbColor(1, 1, 0),
                new PdfRgbColor(0, 0.5, 0.8), false, true));

        var u3d = HPDF_LoadU3DFromMem(pdf, Encoding.ASCII.GetBytes("U3D managed placeholder"));
        var view = HPDF_Page_Create3DView(page, u3d, "Default");
        HPDF_3DView_SetLighting(view, "CAD");
        HPDF_3DView_SetBackgroundColor(view, 0.9, 0.9, 0.95);
        HPDF_3DView_SetPerspectiveProjection(view, 45);
        HPDF_3DView_SetCamera(view, 0, 0, 0, 0, -1, 0.25, 120, 15);
        HPDF_3DView_SetCrossSectionOn(view, new PdfPoint3D(0, 0, 0), 0, 90, 0.35, true);
        var node = HPDF_3DView_CreateNode(view, "PartA");
        HPDF_3DViewNode_SetOpacity(node, 0.8);
        HPDF_3DViewNode_SetVisibility(node, true);
        HPDF_3DViewNode_SetMatrix(node, Pdf3DMatrix.Identity);
        HPDF_3DView_AddNode(view, node);
        var measure = HPDF_Page_Create3DC3DMeasure(page, new PdfPoint3D(0, 0, 0), new PdfPoint3D(1, 1, 1));
        HPDF_3DMeasure_SetName(measure, "Managed measurement");
        HPDF_3DMeasure_SetColor(measure, new PdfRgbColor(0, 0.2, 1));
        HPDF_3DMeasure_SetTextSize(measure, 10);
        HPDF_3DC3DMeasure_SetTextBoxSize(measure, 120, 30);
        HPDF_3DC3DMeasure_SetText(measure, "3D distance");
        HPDF_3DView_Add3DC3DMeasure(view, measure);
        HPDF_U3D_SetDefault3DView(u3d, "Default");
        HPDF_U3D_AddOnInstanciate(u3d, javaScript);
        var orthographicView = HPDF_Create3DView(pdf, "Orthographic");
        HPDF_3DView_SetOrthogonalProjection(orthographicView, 2.5);
        HPDF_3DView_SetCameraByMatrix(orthographicView,
            new Pdf3DMatrix(1, 0, 0, 0, 0.5, 0.866, 0, -0.866, 0.5, 12, 24, 36), 48);
        HPDF_3DView_SetCrossSectionOff(orthographicView);
        var pd3Measure = HPDF_Page_CreatePD33DMeasure(
            page,
            new PdfPoint3D(0, 0, 1),
            new PdfPoint3D(0, 0, 0),
            new PdfPoint3D(1, 0, 0),
            new PdfPoint3D(0, 1, 0),
            new PdfPoint3D(0.5, 0.25, 0),
            new PdfPoint3D(0, 1, 0),
            12.5,
            "mm");
        HPDF_3DMeasure_SetName(pd3Measure, "Managed PD3 measurement");
        HPDF_3DView_Add3DC3DMeasure(orthographicView, pd3Measure);
        HPDF_U3D_Add3DView(u3d, orthographicView);
        var projection =
            HPDF_Page_CreateProjectionAnnot(page, new PdfRect(330, 545, 520, 570), "Projection annotation");
        HPDF_3DC3DMeasure_SetProjectionAnotation(measure, projection);
        var annot3d = HPDF_Page_Create3DAnnot(page, new PdfRect(330, 580, 520, 720), u3d);
        HPDF_Annot_Set3DView(annot3d, view);
        HPDF_3DAnnotExData_Set3DMeasurement(annot3d, measure);

        HPDF_SaveToFile(pdf, pdfPath);

        var bytes = File.ReadAllBytes(pdfPath);
        var latin1 = Encoding.Latin1.GetString(bytes);

        Require(latin1.StartsWith("%PDF-1.7", StringComparison.Ordinal), "U3D did not bump the PDF version to 1.7.");
        Require(latin1.Contains("/PageMode /UseOutlines", StringComparison.Ordinal), "Missing outline page mode.");
        Require(latin1.Contains("/ViewerPreferences", StringComparison.Ordinal), "Missing viewer preferences.");
        Require(latin1.Contains("/PrintScaling /None", StringComparison.Ordinal), "Missing print-scaling preference.");
        Require(latin1.Contains("/PageLabels", StringComparison.Ordinal), "Missing page labels.");
        Require(latin1.Contains("/Outlines", StringComparison.Ordinal), "Missing outline root.");
        Require(latin1.Contains("/Title (Document features)", StringComparison.Ordinal), "Missing outline title.");
        Require(latin1.Contains("/OpenAction", StringComparison.Ordinal), "Missing open action.");
        Require(latin1.Contains("/Names", StringComparison.Ordinal), "Missing names dictionary.");
        Require(latin1.Contains("/Limits", StringComparison.Ordinal), "Missing name tree limits.");
        Require(latin1.Contains("/Dests", StringComparison.Ordinal), "Missing named destinations tree.");
        Require(latin1.Contains("/JavaScript", StringComparison.Ordinal), "Missing JavaScript entry.");
        Require(latin1.Contains("/EmbeddedFiles", StringComparison.Ordinal), "Missing embedded files name tree.");
        Require(latin1.Contains("/Type /Filespec", StringComparison.Ordinal), "Missing file specification.");
        Require(latin1.Contains("/AFRelationship /Data", StringComparison.Ordinal), "Missing AFRelationship.");
        Require(latin1.Contains("/OutputIntents", StringComparison.Ordinal), "Missing output intents.");
        Require(latin1.Contains("/DestOutputProfile", StringComparison.Ordinal), "Missing ICC profile reference.");
        Require(latin1.Contains("/Annots [", StringComparison.Ordinal), "Missing page annotations.");
        Require(latin1.Contains("/Subtype /Link", StringComparison.Ordinal), "Missing link annotation.");
        Require(latin1.Contains("/Subtype /Text", StringComparison.Ordinal), "Missing text annotation.");
        Require(latin1.Contains("/Subtype /Popup", StringComparison.Ordinal), "Missing popup annotation.");
        Require(latin1.Contains("/Subtype /FreeText", StringComparison.Ordinal), "Missing free text annotation.");
        Require(latin1.Contains("/Subtype /Line", StringComparison.Ordinal), "Missing line annotation.");
        Require(latin1.Contains("/Subtype /Circle", StringComparison.Ordinal), "Missing circle annotation.");
        Require(latin1.Contains("/Subtype /Stamp", StringComparison.Ordinal), "Missing stamp annotation.");
        Require(latin1.Contains("/Subtype /Projection", StringComparison.Ordinal), "Missing projection annotation.");
        Require(latin1.Contains("/Subtype /Widget", StringComparison.Ordinal), "Missing widget annotation.");
        Require(latin1.Contains("/CA 0.4", StringComparison.Ordinal), "Missing annotation transparency.");
        Require(latin1.Contains("/IT /FreeTextCallout", StringComparison.Ordinal), "Missing annotation intent.");
        Require(latin1.Contains("/CL [", StringComparison.Ordinal), "Missing free-text callout line.");
        Require(latin1.Contains("/LE [/None /OpenArrow]", StringComparison.Ordinal),
            "Missing 2-point callout line ending style.");
        Require(Count(latin1, "/IC [") >= 3, "Missing interior color variants.");
        Require(latin1.Contains("/AP", StringComparison.Ordinal), "Missing annotation appearance dictionary.");
        Require(latin1.Contains("/Subtype /Form", StringComparison.Ordinal), "Missing appearance form XObject.");
        Require(latin1.Contains("/R <<", StringComparison.Ordinal), "Missing rollover appearance state.");
        Require(latin1.Contains("/D <<", StringComparison.Ordinal), "Missing down appearance state.");
        Require(latin1.Contains("/AS /Yes", StringComparison.Ordinal), "Missing selected appearance state.");
        Require(latin1.Contains("/WhitePatch", StringComparison.Ordinal), "Missing appearance XObject resource.");
        Require(latin1.Contains("/Font <<", StringComparison.Ordinal), "Missing appearance font resources.");
        Require(latin1.Contains("/Subtype /3D", StringComparison.Ordinal), "Missing 3D annotation.");
        Require(latin1.Contains("/Type /3D", StringComparison.Ordinal), "Missing U3D stream dictionary.");
        Require(latin1.Contains("/OnInstantiate", StringComparison.Ordinal), "Missing U3D JavaScript activation.");
        Require(latin1.Contains("/Type /3DView", StringComparison.Ordinal), "Missing 3D view dictionary.");
        Require(latin1.Contains("/Type /3DNode", StringComparison.Ordinal), "Missing 3D node dictionary.");
        Require(latin1.Contains("/Type /3DMeasure", StringComparison.Ordinal), "Missing 3D measure dictionary.");
        Require(latin1.Contains("/Subtype /PD3", StringComparison.Ordinal), "Missing PD3 measurement dictionary.");
        Require(latin1.Contains("/MA [", StringComparison.Ordinal), "Missing 3D view measurement array.");
        Require(latin1.Contains("/ExData", StringComparison.Ordinal), "Missing 3D annotation measurement ExData.");
        Require(latin1.Contains("/M3DREF", StringComparison.Ordinal), "Missing 3D measurement reference.");
        Require(latin1.Contains("/3DCrossSection", StringComparison.Ordinal), "Missing 3D cross section dictionary.");
        Require(latin1.Contains("/SA []", StringComparison.Ordinal), "Missing explicit disabled cross-section array.");
        Require(latin1.Contains("/C2W", StringComparison.Ordinal), "Missing 3D camera matrix.");
        Require(latin1.Contains("/Subtype /O", StringComparison.Ordinal),
            "Missing orthographic projection dictionary.");
        Require(latin1.Contains("/ExtGState", StringComparison.Ordinal), "Missing ExtGState resource.");
        Require(latin1.Contains("/BM /Multiply", StringComparison.Ordinal), "Missing blend mode.");
        Require(latin1.Contains("/Shading", StringComparison.Ordinal), "Missing shading resource.");
        Require(latin1.Contains("/ShadingType 4", StringComparison.Ordinal),
            "Missing free-form triangle mesh shading.");
        Require(latin1.Contains("/ShadingType 2", StringComparison.Ordinal), "Missing axial shading.");
        Require(latin1.Contains("/ShadingType 3", StringComparison.Ordinal), "Missing radial shading.");

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"{bytes.Length} bytes with document features");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
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
}