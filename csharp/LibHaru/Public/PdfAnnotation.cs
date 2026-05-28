using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfAnnotation
{
    internal PdfAnnotation(PdfDocument owner, PdfPage page, PdfIndirectObject annotationObject, string subtype)
    {
        Owner = owner;
        Page = page;
        AnnotationObject = annotationObject;
        Subtype = subtype;
        Dictionary = (PdfDictionary)annotationObject.Value;
    }

    internal PdfDocument Owner { get; }

    internal PdfPage Page { get; }

    internal PdfIndirectObject AnnotationObject { get; }

    internal PdfDictionary Dictionary { get; }

    public string Subtype { get; }

    public void SetBorderStyle(PdfAnnotBorderStyle style, double width, ushort dashOn = 0, ushort dashOff = 0, ushort dashPhase = 0)
    {
        ValidateOrThrow();

        if (width < 0 || double.IsNaN(width) || double.IsInfinity(width))
            throw Owner.CreateException(HaruStatus.AnnotInvalidBorderStyle, "Annotation border width must be non-negative.");

        var border = new PdfDictionary();
        border.SetName("Type", "Border");
        border.SetName("S", style switch
        {
            PdfAnnotBorderStyle.Solid => "S",
            PdfAnnotBorderStyle.Dashed => "D",
            PdfAnnotBorderStyle.Beveled => "B",
            PdfAnnotBorderStyle.Inset => "I",
            PdfAnnotBorderStyle.Underlined => "U",
            _ => throw Owner.CreateException(HaruStatus.AnnotInvalidBorderStyle, "Unknown annotation border style.")
        });
        border.Set("W", new PdfReal(width));

        if (style == PdfAnnotBorderStyle.Dashed)
        {
            var dash = new PdfArray([new PdfInteger(dashOn), new PdfInteger(dashOff)]);
            if (dashPhase != 0)
                dash.Add(new PdfInteger(dashPhase));

            border.Set("D", dash);
        }

        Dictionary.Set("BS", border);
    }

    public void SetHighlightMode(PdfAnnotHighlightMode mode)
    {
        EnsureSubtype("Link");
        Dictionary.SetName("H", mode switch
        {
            PdfAnnotHighlightMode.NoHighlight => "N",
            PdfAnnotHighlightMode.InvertBox => "I",
            PdfAnnotHighlightMode.InvertBorder => "O",
            PdfAnnotHighlightMode.DownAppearance => "P",
            _ => throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Unknown link annotation highlight mode.")
        });
    }

    public void SetJavaScript(PdfJavaScript javaScript)
    {
        EnsureSubtype("Link");
        if (javaScript is null || !ReferenceEquals(javaScript.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "JavaScript action does not belong to this document.");

        Dictionary.Set("A", javaScript.CreateActionDictionary());
    }

    public void SetRGBColor(double r, double g, double b)
    {
        ValidateOrThrow();
        Dictionary.Set("C", ColorArray(r, g, b));
    }

    public void SetCMYKColor(double c, double m, double y, double k)
    {
        ValidateOrThrow();
        Dictionary.Set("C", new PdfArray([new PdfReal(Unit(c)), new PdfReal(Unit(m)), new PdfReal(Unit(y)), new PdfReal(Unit(k))]));
    }

    public void SetGrayColor(double gray)
    {
        ValidateOrThrow();
        Dictionary.Set("C", new PdfArray([new PdfReal(Unit(gray))]));
    }

    public void SetNoColor()
    {
        ValidateOrThrow();
        Dictionary.Remove("C");
    }

    public void SetOpened(bool opened)
    {
        ValidateOrThrow();

        if (Subtype != "Text" && Subtype != "Popup")
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Only Text and Popup annotations support the Open flag.");

        Dictionary.Set("Open", new PdfBoolean(opened));
    }

    public void SetIcon(PdfAnnotIcon icon)
    {
        EnsureSubtype("Text");
        Dictionary.SetName("Name", icon switch
        {
            PdfAnnotIcon.Comment => "Comment",
            PdfAnnotIcon.Key => "Key",
            PdfAnnotIcon.Note => "Note",
            PdfAnnotIcon.Help => "Help",
            PdfAnnotIcon.NewParagraph => "NewParagraph",
            PdfAnnotIcon.Paragraph => "Paragraph",
            PdfAnnotIcon.Insert => "Insert",
            _ => throw Owner.CreateException(HaruStatus.AnnotInvalidIcon, "Unknown text annotation icon.")
        });
    }

    public void SetTitle(string title)
    {
        ValidateOrThrow();
        Dictionary.Set("T", PdfString.FromText(title ?? string.Empty));
    }

    public void SetSubject(string subject)
    {
        ValidateOrThrow();
        Dictionary.Set("Subj", PdfString.FromText(subject ?? string.Empty));
    }

    public void SetContents(string contents)
    {
        ValidateOrThrow();
        Dictionary.Set("Contents", PdfString.FromText(contents ?? string.Empty));
    }

    public void SetCreationDate(DateTimeOffset value)
    {
        ValidateOrThrow();
        Dictionary.Set("CreationDate", PdfString.FromText(PdfDocument.FormatPdfDate(value)));
    }

    public void SetTransparency(double value)
    {
        ValidateOrThrow();
        Dictionary.Set("CA", new PdfReal(Unit(value)));
    }

    public void SetIntent(PdfAnnotIntent intent)
    {
        ValidateOrThrow();

        if (!Enum.IsDefined(intent))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Unknown annotation intent.");

        Dictionary.SetName("IT", intent switch
        {
            PdfAnnotIntent.FreeTextCallout => "FreeTextCallout",
            PdfAnnotIntent.FreeTextTypeWriter => "FreeTextTypeWriter",
            PdfAnnotIntent.LineArrow => "LineArrow",
            PdfAnnotIntent.LineDimension => "LineDimension",
            PdfAnnotIntent.PolygonCloud => "PolygonCloud",
            PdfAnnotIntent.PolyLineDimension => "PolyLineDimension",
            PdfAnnotIntent.PolygonDimension => "PolygonDimension",
            PdfAnnotIntent.StampImage => "StampImage",
            _ => "StampSnapshot"
        });
    }

    public void SetPopup(PdfAnnotation popup)
    {
        ValidateOrThrow();

        if (popup is null || !ReferenceEquals(popup.Owner, Owner) || popup.Subtype != "Popup")
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Popup annotation does not belong to this document.");

        Dictionary.Set("Popup", popup.AnnotationObject.Reference);
        popup.Dictionary.Set("Parent", AnnotationObject.Reference);
    }

    public void SetRectDiff(PdfRect rect)
    {
        ValidateOrThrow();
        Dictionary.Set("RD", PdfFeatureHelpers.RectArray(rect));
    }

    public void SetCloudEffect(int cloudIntensity)
    {
        ValidateOrThrow();

        if (cloudIntensity < 0)
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Cloud intensity must be non-negative.");

        var borderEffect = new PdfDictionary();
        borderEffect.SetName("S", "C");
        borderEffect.Set("I", new PdfInteger(cloudIntensity));
        Dictionary.Set("BE", borderEffect);
    }

    public void SetInteriorRGBColor(PdfRgbColor color)
    {
        ValidateOrThrow();
        Dictionary.Set("IC", ColorArray(color.R, color.G, color.B));
    }

    public void SetInteriorCMYKColor(PdfCmykColor color)
    {
        ValidateOrThrow();
        Dictionary.Set("IC", new PdfArray([new PdfReal(Unit(color.C)), new PdfReal(Unit(color.M)), new PdfReal(Unit(color.Y)), new PdfReal(Unit(color.K))]));
    }

    public void SetInteriorGrayColor(double gray)
    {
        ValidateOrThrow();
        Dictionary.Set("IC", new PdfArray([new PdfReal(Unit(gray))]));
    }

    public void SetInteriorTransparent()
    {
        ValidateOrThrow();
        Dictionary.Remove("IC");
    }

    public void SetQuadPoints(PdfPoint leftBottom, PdfPoint rightBottom, PdfPoint rightTop, PdfPoint leftTop)
    {
        ValidateOrThrow();
        Dictionary.Set("QuadPoints", new PdfArray([
            new PdfReal(leftBottom.X), new PdfReal(leftBottom.Y),
            new PdfReal(rightBottom.X), new PdfReal(rightBottom.Y),
            new PdfReal(rightTop.X), new PdfReal(rightTop.Y),
            new PdfReal(leftTop.X), new PdfReal(leftTop.Y)
        ]));
    }

    public void SetLineEndingStyle(PdfAnnotLineEndingStyle startStyle, PdfAnnotLineEndingStyle endStyle)
    {
        ValidateOrThrow();
        Dictionary.Set("LE", new PdfArray([new PdfName(LineEndingName(startStyle)), new PdfName(LineEndingName(endStyle))]));
    }

    public void SetCalloutLine(PdfPoint startPoint, PdfPoint endPoint)
    {
        EnsureSubtype("FreeText");
        Dictionary.Set("CL", new PdfArray([new PdfReal(startPoint.X), new PdfReal(startPoint.Y), new PdfReal(endPoint.X), new PdfReal(endPoint.Y)]));
    }

    public void SetCalloutLine(PdfPoint startPoint, PdfPoint kneePoint, PdfPoint endPoint)
    {
        EnsureSubtype("FreeText");
        Dictionary.Set("CL", new PdfArray([
            new PdfReal(startPoint.X), new PdfReal(startPoint.Y),
            new PdfReal(kneePoint.X), new PdfReal(kneePoint.Y),
            new PdfReal(endPoint.X), new PdfReal(endPoint.Y)
        ]));
    }

    public void SetDefaultStyle(string style)
    {
        EnsureSubtype("FreeText");
        Dictionary.Set("DS", PdfString.FromText(style ?? string.Empty));
    }

    public void SetLinePosition(PdfPoint startPoint, PdfAnnotLineEndingStyle startStyle, PdfPoint endPoint, PdfAnnotLineEndingStyle endStyle)
    {
        EnsureSubtype("Line");
        Dictionary.Set("L", new PdfArray([new PdfReal(startPoint.X), new PdfReal(startPoint.Y), new PdfReal(endPoint.X), new PdfReal(endPoint.Y)]));
        SetLineEndingStyle(startStyle, endStyle);
    }

    public void SetLineLeader(int leaderLength, int leaderExtensionLength, int leaderOffsetLength)
    {
        EnsureSubtype("Line");
        Dictionary.Set("LL", new PdfInteger(leaderLength));
        Dictionary.Set("LLE", new PdfInteger(leaderExtensionLength));
        Dictionary.Set("LLO", new PdfInteger(leaderOffsetLength));
    }

    public void SetLineCaption(bool showCaption, PdfLineAnnotCapPosition position, int horizontalOffset, int verticalOffset)
    {
        EnsureSubtype("Line");

        if (!Enum.IsDefined(position))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Unknown line annotation caption position.");

        Dictionary.Set("Cap", new PdfBoolean(showCaption));
        Dictionary.SetName("CP", position == PdfLineAnnotCapPosition.Top ? "Top" : "Inline");
        Dictionary.Set("CO", new PdfArray([new PdfInteger(horizontalOffset), new PdfInteger(verticalOffset)]));
    }

    public void Set3DView(Pdf3DView view)
    {
        EnsureSubtype("3D");

        if (view is null || !ReferenceEquals(view.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D view does not belong to this document.");

        view.ValidateOrThrow();
        Dictionary.Set("3DV", view.ViewObject.Reference);
    }

    public void Set3DMeasure(Pdf3DMeasure measure)
    {
        EnsureSubtype("3D");

        if (measure is null || !ReferenceEquals(measure.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D measure does not belong to this document.");

        measure.ValidateOrThrow();
        var exData = new PdfDictionary();
        exData.SetName("Type", "ExData");
        exData.SetName("Subtype", "3DM");
        exData.Set("M3DREF", measure.MeasureObject.Reference);
        Dictionary.Set("ExData", Owner.AddObject(exData).Reference);
    }

    public void SetExData(PdfExData exData)
    {
        EnsureSubtype("Projection");

        if (exData is null || !ReferenceEquals(exData.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidObject, "External data does not belong to this document.");

        Dictionary.Set("ExData", exData.ExDataObject.Reference);
    }

    public void SetAppearance(
        PdfAnnotationAppearanceState state,
        string contentStream,
        PdfRect boundingBox,
        string? appearanceName = null,
        IReadOnlyDictionary<string, PdfXObject>? xObjects = null,
        IReadOnlyDictionary<string, PdfFont>? fonts = null)
    {
        ValidateOrThrow();

        if (!Enum.IsDefined(state))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Unknown annotation appearance state.");

        var resources = CreateAppearanceResources(xObjects, fonts);
        var stream = new PdfStreamObject(PdfFeatureHelpers.Utf8(contentStream ?? string.Empty))
        {
            CompressionMode = Owner.CompressionMode,
            Subclass = PdfObjectClass.XObject
        };
        stream.Dictionary.SetName("Type", "XObject");
        stream.Dictionary.SetName("Subtype", "Form");
        stream.Dictionary.Set("FormType", new PdfInteger(1));
        stream.Dictionary.Set("BBox", PdfFeatureHelpers.RectArray(boundingBox));
        stream.Dictionary.Set("Matrix", new PdfArray([
            new PdfInteger(1),
            new PdfInteger(0),
            new PdfInteger(0),
            new PdfInteger(1),
            new PdfInteger(0),
            new PdfInteger(0)
        ]));
        stream.Dictionary.Set("Resources", resources);
        var appearanceObject = Owner.AddObject(stream);

        var appearances = Dictionary.GetItem("AP", PdfObjectClass.Dictionary) as PdfDictionary ?? new PdfDictionary();
        var key = state switch
        {
            PdfAnnotationAppearanceState.Normal => "N",
            PdfAnnotationAppearanceState.Rollover => "R",
            _ => "D"
        };

        if (string.IsNullOrWhiteSpace(appearanceName))
        {
            appearances.Set(key, appearanceObject.Reference);
        }
        else
        {
            var named = appearances.GetItem(key, PdfObjectClass.Dictionary) as PdfDictionary ?? new PdfDictionary();
            named.Set(appearanceName, appearanceObject.Reference);
            appearances.Set(key, named);
            Dictionary.SetName("AS", appearanceName);
        }

        Dictionary.Set("AP", appearances);
    }

    private PdfDictionary CreateAppearanceResources(
        IReadOnlyDictionary<string, PdfXObject>? xObjects,
        IReadOnlyDictionary<string, PdfFont>? fonts)
    {
        var procSet = new List<PdfObject> { new PdfName("PDF") };
        var resources = new PdfDictionary();

        if (fonts is { Count: > 0 })
        {
            var fontResources = new PdfDictionary();
            foreach (var (name, font) in fonts)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw Owner.CreateException(HaruStatus.InvalidParameter, "Appearance font resource name cannot be empty.");

                if (font is null || !ReferenceEquals(font.Owner, Owner))
                    throw Owner.CreateException(HaruStatus.InvalidFont, "Appearance font resource does not belong to this document.");

                font.ValidateOrThrow();
                fontResources.Set(name, font.FontObject.Reference);
            }

            resources.Set("Font", fontResources);
            procSet.Add(new PdfName("Text"));
        }

        if (xObjects is { Count: > 0 })
        {
            var xObjectResources = new PdfDictionary();
            foreach (var (name, xObject) in xObjects)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw Owner.CreateException(HaruStatus.InvalidParameter, "Appearance XObject resource name cannot be empty.");

                if (xObject is null || !ReferenceEquals(xObject.Owner, Owner))
                    throw Owner.CreateException(HaruStatus.PageInvalidXObject, "Appearance XObject resource does not belong to this document.");

                xObject.ValidateOrThrow();
                xObjectResources.Set(name, xObject.XObjectObject.Reference);
            }

            resources.Set("XObject", xObjectResources);
            procSet.Add(new PdfName("ImageB"));
            procSet.Add(new PdfName("ImageC"));
            procSet.Add(new PdfName("ImageI"));
        }

        resources.Set("ProcSet", new PdfArray(procSet));
        return resources;
    }

    private void EnsureSubtype(string expected)
    {
        ValidateOrThrow();

        if (!string.Equals(Subtype, expected, StringComparison.Ordinal))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, $"Annotation subtype must be {expected}.");
    }

    private void ValidateOrThrow()
    {
        if (AnnotationObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Annotation object must be a dictionary.");

        if (!dictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Annotation))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Annotation object must be an annotation dictionary.");

        try
        {
            var type = dictionary.Get<PdfName>("Type");
            var subtype = dictionary.Get<PdfName>("Subtype");

            if (type?.Value != "Annot" || subtype?.Value != Subtype)
                throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Annotation Type/Subtype entries are invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidAnnotation)
        {
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Annotation Type/Subtype entries are invalid.", ex.Status);
        }
    }

    private PdfArray ColorArray(double r, double g, double b)
    {
        if (!IsUnit(r) || !IsUnit(g) || !IsUnit(b))
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "Annotation RGB color components must be between 0 and 1.");

        return new PdfArray([new PdfReal(r), new PdfReal(g), new PdfReal(b)]);
    }

    private double Unit(double value)
    {
        if (!IsUnit(value))
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "Annotation color components must be between 0 and 1.");

        return value;
    }

    private string LineEndingName(PdfAnnotLineEndingStyle style) => style switch
    {
        PdfAnnotLineEndingStyle.None => "None",
        PdfAnnotLineEndingStyle.Square => "Square",
        PdfAnnotLineEndingStyle.Circle => "Circle",
        PdfAnnotLineEndingStyle.Diamond => "Diamond",
        PdfAnnotLineEndingStyle.OpenArrow => "OpenArrow",
        PdfAnnotLineEndingStyle.ClosedArrow => "ClosedArrow",
        PdfAnnotLineEndingStyle.Butt => "Butt",
        PdfAnnotLineEndingStyle.ReversedOpenArrow => "ROpenArrow",
        PdfAnnotLineEndingStyle.ReversedClosedArrow => "RClosedArrow",
        PdfAnnotLineEndingStyle.Slash => "Slash",
        _ => throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Unknown annotation line ending style.")
    };

    private static bool IsUnit(double value) => value is >= 0 and <= 1 && !double.IsNaN(value) && !double.IsInfinity(value);
}
