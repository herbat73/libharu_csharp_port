using LibHaru;

namespace LibHaru.Tests;

public sealed class PdfResourceTests
{
    [Fact]
    public void RawImages_ExposeMetadataAndSupportMasks()
    {
        using var document = new PdfDocument();
        var image = document.LoadRawImageFromMem([255, 0, 0, 0, 255, 0], 2, 1, PdfColorSpace.DeviceRgb);
        var softMask = document.LoadRawImageFromMem([0, 255], 2, 1, PdfColorSpace.DeviceGray);
        var imageMask = document.LoadRaw1BitImageFromMem([0b1000_0000], 1, 1, 1, true, true);

        image.SetColorMask(0, 255, 0, 255, 0, 255);
        image.AddSMask(softMask);
        image.SetMaskImage(imageMask);

        Assert.True(image.Validate());
        Assert.Equal(2, image.Width);
        Assert.Equal(1, image.Height);
        Assert.Equal(8, image.BitsPerComponent);
        Assert.Equal(PdfColorSpace.DeviceRgb, image.ColorSpace);
        Assert.Equal("DeviceRGB", image.ColorSpaceName);
        Assert.Equal(new PdfPoint(2, 1), image.Size);
    }

    [Fact]
    public void RawImages_RejectInvalidDataAndPeerDocumentMasks()
    {
        using var document = new PdfDocument();
        using var otherDocument = new PdfDocument();
        var image = document.LoadRawImageFromMem([255, 0, 0], 1, 1, PdfColorSpace.DeviceRgb);
        var maskFromOtherDocument = otherDocument.LoadRawImageFromMem([0], 1, 1, PdfColorSpace.DeviceGray);

        TestHelpers.AssertHaruException(HaruStatus.InvalidImage,
            () => document.LoadRawImageFromMem([255], 1, 1, PdfColorSpace.DeviceRgb));
        TestHelpers.AssertHaruException(HaruStatus.InvalidImage, () => image.SetMaskImage(maskFromOtherDocument));
        TestHelpers.AssertHaruException(HaruStatus.InvalidColorSpace, () => image.AddSMask(image));
    }

    [Fact]
    public void PageCanDrawImagesAndFormXObjects()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        var image = document.LoadRawImageFromMem([255, 255, 255], 1, 1, PdfColorSpace.DeviceRgb);
        var xObject = document.CreateXObjectFromImage(page, new PdfRect(0, 0, 20, 20), image, true);
        var whiteRect = document.CreateXObjectAsWhiteRect(page, new PdfRect(0, 0, 10, 10));

        page.DrawImage(image, 10, 20, 30, 40);
        page.ExecuteXObject(xObject);
        page.ExecuteXObject(whiteRect);

        Assert.Equal("Form", xObject.Subtype);
        Assert.StartsWith("Im", page.GetXObjectName(image));
        Assert.StartsWith("X", page.GetXObjectName(xObject));
        TestHelpers.AssertPdf(document.SaveToStream());
    }

    [Fact]
    public void DestinationsOutlinesAndAnnotations_CanBeConfiguredAndSaved()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        var rect = new PdfRect(10, 10, 60, 40);
        var destination = page.CreateDestination();
        destination.SetXYZ(0, 100, 1);
        destination.SetFit();
        destination.SetFitH(100);
        destination.SetFitV(5);
        destination.SetFitR(0, 0, 100, 100);
        destination.SetFitB();
        destination.SetFitBH(50);
        destination.SetFitBV(5);
        document.AddNamedDestination("top", destination);

        var outline = document.CreateOutline(null, "Top");
        outline.SetDestination(destination);
        outline.SetOpened(true);
        Assert.Equal("Top", outline.Title);
        Assert.True(outline.Opened);

        var javaScript = document.CreateJavaScript("app.alert('test');");
        var link = page.CreateLinkAnnotation(rect, destination);
        link.SetBorderStyle(PdfAnnotBorderStyle.Dashed, 1, 2, 3);
        link.SetHighlightMode(PdfAnnotHighlightMode.InvertBorder);
        link.SetJavaScript(javaScript);
        link.SetRGBColor(0.1, 0.2, 0.3);

        var text = page.CreateTextAnnotation(rect, "note");
        text.SetIcon(PdfAnnotIcon.Help);
        text.SetOpened(true);
        text.SetTitle("title");
        text.SetSubject("subject");
        text.SetContents("contents");
        text.SetCreationDate(new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.Zero));
        text.SetTransparency(0.5);

        var popup = page.CreatePopupAnnotation(rect, text);
        popup.SetOpened(false);
        text.SetPopup(popup);

        var freeText = page.CreateFreeTextAnnotation(rect, "free");
        freeText.SetCalloutLine(new PdfPoint(0, 0), new PdfPoint(10, 10));
        freeText.SetCalloutLine(new PdfPoint(0, 0), new PdfPoint(5, 5), new PdfPoint(10, 10));
        freeText.SetDefaultStyle("font: 12pt Helvetica");
        freeText.SetLineEndingStyle(PdfAnnotLineEndingStyle.OpenArrow, PdfAnnotLineEndingStyle.ClosedArrow);

        var line = page.CreateLineAnnotation(rect, "line", new PdfPoint(0, 0), new PdfPoint(10, 10));
        line.SetLinePosition(new PdfPoint(0, 0), PdfAnnotLineEndingStyle.None, new PdfPoint(10, 10),
            PdfAnnotLineEndingStyle.Square);
        line.SetLineLeader(1, 2, 3);
        line.SetLineCaption(true, PdfLineAnnotCapPosition.Top, 4, 5);

        var square = page.CreateSquareAnnotation(rect, "square");
        square.SetInteriorRGBColor(new PdfRgbColor(0.2, 0.3, 0.4));
        square.SetInteriorCMYKColor(new PdfCmykColor(0.1, 0.2, 0.3, 0.4));
        square.SetInteriorGrayColor(0.6);
        square.SetInteriorTransparent();
        square.SetRectDiff(new PdfRect(1, 1, 2, 2));
        square.SetCloudEffect(2);
        square.SetIntent(PdfAnnotIntent.PolygonCloud);

        page.CreateCircleAnnotation(rect, "circle").SetGrayColor(0.5);
        page.CreateHighlightAnnotation(rect, "highlight").SetQuadPoints(
            new PdfPoint(10, 10), new PdfPoint(60, 10), new PdfPoint(60, 40), new PdfPoint(10, 40));
        page.CreateUnderlineAnnotation(rect, "underline").SetCMYKColor(0.1, 0.2, 0.3, 0.4);
        page.CreateSquigglyAnnotation(rect, "squiggly").SetNoColor();
        page.CreateStrikeOutAnnotation(rect, "strikeout");
        page.CreateStampAnnotation(rect, "Approved", "stamp");
        page.CreateURILinkAnnotation(rect, "https://example.com");
        page.CreateWidgetAnnotationWhiteOnlyWhilePrint(rect);

        var projection = page.CreateProjectionAnnotation(rect, "projection");
        var exData = page.Create3DAnnotExData();
        projection.SetExData(exData);

        TestHelpers.AssertPdf(document.SaveToStream());
    }

    [Fact]
    public void Annotations_CoverAppearanceIntentAndLineEndingVariants()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        var rect = new PdfRect(10, 10, 60, 40);
        var font = document.GetFont("Helvetica");
        var image = document.LoadRawImageFromMem([255, 255, 255], 1, 1, PdfColorSpace.DeviceRgb);
        var xObject = document.CreateXObjectFromImage(page, rect, image, true);

        var square = page.CreateSquareAnnotation(rect, "square");
        foreach (var intent in new[]
                 {
                     PdfAnnotIntent.FreeTextCallout,
                     PdfAnnotIntent.FreeTextTypeWriter,
                     PdfAnnotIntent.LineArrow,
                     PdfAnnotIntent.LineDimension,
                     PdfAnnotIntent.PolygonCloud,
                     PdfAnnotIntent.PolyLineDimension,
                     PdfAnnotIntent.PolygonDimension,
                     PdfAnnotIntent.StampImage,
                     PdfAnnotIntent.StampSnapshot
                 })
        {
            square.SetIntent(intent);
        }

        var link = page.CreateURILinkAnnotation(rect, "https://example.com");
        link.SetBorderStyle(PdfAnnotBorderStyle.Beveled, 2);
        link.SetBorderStyle(PdfAnnotBorderStyle.Inset, 2);
        link.SetBorderStyle(PdfAnnotBorderStyle.Underlined, 2);

        var line = page.CreateLineAnnotation(rect, "line", new PdfPoint(0, 0), new PdfPoint(10, 10));
        line.SetLineEndingStyle(PdfAnnotLineEndingStyle.Circle, PdfAnnotLineEndingStyle.Diamond);
        line.SetLineEndingStyle(PdfAnnotLineEndingStyle.Butt, PdfAnnotLineEndingStyle.ReversedOpenArrow);
        line.SetLineEndingStyle(PdfAnnotLineEndingStyle.ReversedClosedArrow, PdfAnnotLineEndingStyle.Slash);

        square.SetAppearance(
            PdfAnnotationAppearanceState.Normal,
            "q /Im1 Do BT /F1 10 Tf ET Q",
            rect,
            xObjects: new Dictionary<string, PdfXObject> { ["Im1"] = xObject },
            fonts: new Dictionary<string, PdfFont> { ["F1"] = font });

        TestHelpers.AssertPdf(document.SaveToStream());
    }

    [Fact]
    public void Images_RejectColorMaskMisuseCases()
    {
        using var document = new PdfDocument();
        var rgb = document.LoadRawImageFromMem([255, 0, 0], 1, 1, PdfColorSpace.DeviceRgb);
        var gray = document.LoadRawImageFromMem([128], 1, 1, PdfColorSpace.DeviceGray);
        var oneBit = document.LoadRaw1BitImageFromMem([0b1000_0000], 1, 1, 1, true, true);
        var maskTarget = document.LoadRawImageFromMem([0, 0, 0], 1, 1, PdfColorSpace.DeviceRgb);

        TestHelpers.AssertHaruException(HaruStatus.InvalidColorSpace,
            () => gray.SetColorMask(0, 255, 0, 255, 0, 255));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidBitPerComponent,
            () => oneBit.SetColorMask(0, 255, 0, 255, 0, 255));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter,
            () => rgb.SetColorMask(255, 0, 0, 255, 0, 255));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidBitPerComponent,
            () => maskTarget.SetMaskImage(rgb));
        document.ResetError();

        rgb.SetMaskImage(oneBit);

        TestHelpers.AssertHaruException(HaruStatus.InvalidOperation,
            () => oneBit.SetColorMask(0, 255, 0, 255, 0, 255));
    }

    [Fact]
    public void Annotations_RejectUnsupportedSubtypeOperations()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        var text = page.CreateTextAnnotation(new PdfRect(0, 0, 10, 10), "note");

        TestHelpers.AssertHaruException(HaruStatus.InvalidAnnotation,
            () => text.SetHighlightMode(PdfAnnotHighlightMode.InvertBox));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidAnnotation,
            () => text.SetCalloutLine(new PdfPoint(0, 0), new PdfPoint(1, 1)));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidAnnotation,
            () => text.SetJavaScript(document.CreateJavaScript("app.alert('wrong subtype');")));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidAnnotation,
            () => text.SetPopup(null!));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidAnnotation,
            () => page.CreateURILinkAnnotation(new PdfRect(0, 0, 10, 10), "https://example.com").SetOpened(true));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidColorSpace, () => text.SetGrayColor(2));
        document.ResetError();
        TestHelpers.AssertHaruException(HaruStatus.InvalidColorSpace, () => text.SetRGBColor(2, 0, 0));
    }

    [Fact]
    public void ExtGStateAndShadings_CanBeAppliedToPage()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();

        var extGState = document.CreateExtGState();
        extGState.SetAlphaStroke(0.5);
        extGState.SetAlphaFill(0.25);
        extGState.SetBlendMode(PdfBlendMode.Multiply);
        extGState.SetStrokeAdjustment(true);
        page.SetExtGState(extGState);

        var mesh = document.CreateShading(PdfShadingType.FreeFormTriangleMesh, PdfColorSpace.DeviceRgb, 0, 100, 0, 100);
        mesh.AddVertexRGB(PdfShadingFreeFormTriangleMeshEdgeFlag.NoConnection, 0, 0, 255, 0, 0);
        page.SetShading(mesh);

        var axial = document.CreateAxialShading(new PdfPoint(0, 0), new PdfPoint(100, 0), PdfRgbColor.Black,
            new PdfRgbColor(1, 1, 1));
        var radial = document.CreateRadialShading(new PdfPoint(10, 10), 0, new PdfPoint(30, 30), 10,
            PdfRgbColor.Black, new PdfRgbColor(1, 0, 0));

        Assert.Equal(PdfShadingType.FreeFormTriangleMesh, mesh.Type);
        Assert.Equal(PdfShadingType.Axial, axial.Type);
        Assert.Equal(PdfShadingType.Radial, radial.Type);
        TestHelpers.AssertHaruException(HaruStatus.InvalidShadingType,
            () => document.CreateShading(PdfShadingType.Axial, PdfColorSpace.DeviceRgb, 0, 1, 0, 1));
        TestHelpers.AssertHaruException(HaruStatus.InvalidParameter,
            () => mesh.AddVertexRGB(PdfShadingFreeFormTriangleMeshEdgeFlag.NoConnection, 200, 0, 0, 0, 0));

        document.ResetError();

        TestHelpers.AssertPdf(document.SaveToStream());
    }

    [Fact]
    public void IccProfilesAndOutputIntents_ExposeComponentCount()
    {
        using var document = new PdfDocument();

        var profile = document.LoadIccProfileFromMem([1, 2, 3], 3);
        var intent = document.AppendOutputIntent("sRGB", profile, "Test profile");

        Assert.Equal(3, profile.ComponentCount);
        Assert.NotNull(intent);
        TestHelpers.AssertHaruException(HaruStatus.InvalidIccComponentNum,
            () => document.LoadIccProfileFromMem([1, 2, 3], 2));
    }

    [Fact]
    public void U3DViewsNodesMeasuresAndAnnotations_CanBeConfiguredAndSaved()
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        var u3d = document.LoadU3DFromMem([1, 2, 3, 4]);
        var view = document.Create3DView("Default");
        var node = view.CreateNode("Part");
        var measure = document.Create3DC3DMeasure(new PdfPoint3D(0, 0, 0), new PdfPoint3D(1, 1, 1));
        var projectionMeasure = document.CreatePD33DMeasure(
            new PdfPoint3D(0, 0, 1),
            new PdfPoint3D(0, 0, 0),
            new PdfPoint3D(1, 0, 0),
            new PdfPoint3D(0, 1, 0),
            new PdfPoint3D(1, 1, 0),
            new PdfPoint3D(0, 1, 0),
            12.5,
            "mm");

        node.SetOpacity(0.5);
        node.SetVisibility(true);
        node.SetMatrix(Pdf3DMatrix.Identity);
        view.SetLighting("Artwork");
        view.SetBackgroundColor(0.1, 0.2, 0.3);
        view.SetPerspectiveProjection(45);
        view.SetOrthogonalProjection(2);
        view.SetCamera(0, 0, 0, 0, 0, -1, 10, 0);
        view.SetCameraByMatrix(Pdf3DMatrix.Identity, 5);
        view.AddNode(node);
        view.AddMeasure(measure);
        view.AddMeasure(projectionMeasure);
        view.SetCrossSectionOn(new PdfPoint3D(0, 0, 0), 0, 0, 0.5, true);
        view.SetCrossSectionOff();
        u3d.Add3DView(view);
        u3d.SetDefault3DView("Default");
        u3d.AddOnInstantiate(document.CreateJavaScript("app.alert('3d');"));

        measure.SetName("measure");
        measure.SetColor(new PdfRgbColor(1, 0, 0));
        measure.SetTextSize(12);
        measure.SetTextBoxSize(20, 10);
        measure.SetText("distance");

        var annotation = page.Create3DAnnotation(new PdfRect(10, 10, 100, 100), u3d);
        annotation.Set3DView(view);
        annotation.Set3DMeasure(measure);
        measure.SetProjectionAnnotation(page.CreateProjectionAnnotation(new PdfRect(10, 10, 100, 100), "projection"));
        page.Create3DAnnotExData().Set3DMeasurement(projectionMeasure);

        TestHelpers.AssertHaruException(HaruStatus.InvalidU3DData, () => view.SetLighting("Unknown"));

        document.ResetError();

        TestHelpers.AssertPdf(document.SaveToStream());
    }
}
