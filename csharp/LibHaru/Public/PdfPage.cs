using System.Diagnostics;
using System.Text;
using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfPage
{
    private static readonly Encoding Ascii = Encoding.ASCII;
    private MemoryStream _contents = new();
    private readonly List<PdfContentStream> _contentStreams = [];
    private readonly Dictionary<string, PdfFont> _fonts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PdfImage> _xObjects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PdfXObject> _formXObjects = new(StringComparer.Ordinal);
    private readonly Dictionary<PdfPageBoundary, PdfRect> _boundaries = new();
    private readonly List<PdfAnnotation> _annotations = [];
    private readonly Dictionary<string, PdfExtGState> _extGStates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PdfShading> _shadings = new(StringComparer.Ordinal);
    private readonly Stack<PdfGraphicsState> _graphicsStateStack = new();
    private PdfGraphicsState _graphicsState = PdfGraphicsState.Default;
    private PdfGraphicsMode _graphicsMode = PdfGraphicsMode.PageDescription;
    private PdfPoint _currentPosition;
    private PdfPoint _currentTextPosition;
    private PdfTransMatrix _textMatrix = PdfTransMatrix.Identity;
    private PdfSlideShow? _slideShow;
    private ushort? _rotate;
    private double? _zoom;
    private bool _inText;
    private bool _hasCurrentPath;

    internal PdfPage(PdfDocument owner, PdfIndirectObject pageObject, PdfIndirectObject contentsObject)
    {
        Owner = owner;
        PageObject = pageObject;
        ContentsObject = contentsObject;
        _contentStreams.Add(new PdfContentStream(owner, contentsObject, _contents));
        SetSize(PdfPageSize.A4, PdfPageDirection.Portrait);
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject PageObject { get; }

    internal PdfIndirectObject ContentsObject { get; }

    public double Width { get; private set; }

    public double Height { get; private set; }

    public PdfFont? CurrentFont => _graphicsState.Font;

    public double CurrentFontSize => _graphicsState.Font is null ? 0 : _graphicsState.FontSize;

    public double LineWidth => _graphicsState.LineWidth;

    public PdfLineCap LineCap => _graphicsState.LineCap;

    public PdfLineJoin LineJoin => _graphicsState.LineJoin;

    public double MiterLimit => _graphicsState.MiterLimit;

    public PdfDashMode Dash => _graphicsState.DashMode;

    public double Flatness => _graphicsState.Flatness;

    public double CharSpace => _graphicsState.CharSpace;

    public double WordSpace => _graphicsState.WordSpace;

    public double HorizontalScalling => _graphicsState.HorizontalScaling;

    public double TextLeading => _graphicsState.TextLeading;

    public PdfTextRenderingMode TextRenderingMode => _graphicsState.TextRenderingMode;

    public double TextRise => _graphicsState.TextRise;

    public PdfColorSpace StrokingColorSpace => _graphicsState.StrokeColorSpace;

    public PdfColorSpace FillingColorSpace => _graphicsState.FillColorSpace;

    public PdfTransMatrix TransMatrix => _graphicsState.TransMatrix;

    public PdfTransMatrix TextMatrix => _textMatrix;

    public uint GStateDepth => _graphicsState.Depth;

    public PdfGraphicsMode GraphicsMode => _graphicsMode;

    public PdfPoint CurrentPosition => _graphicsMode == PdfGraphicsMode.PathObject ? _currentPosition : new PdfPoint(0, 0);

    public PdfPoint CurrentTextPosition => _inText ? _currentTextPosition : new PdfPoint(0, 0);

    public PdfRgbColor RgbFill => _graphicsState.FillColorSpace == PdfColorSpace.DeviceRgb ? _graphicsState.RgbFill : PdfRgbColor.Black;

    public PdfRgbColor RgbStroke => _graphicsState.StrokeColorSpace == PdfColorSpace.DeviceRgb ? _graphicsState.RgbStroke : PdfRgbColor.Black;

    public PdfCmykColor CmykFill => _graphicsState.FillColorSpace == PdfColorSpace.DeviceCmyk ? _graphicsState.CmykFill : new PdfCmykColor(0, 0, 0, 0);

    public PdfCmykColor CmykStroke => _graphicsState.StrokeColorSpace == PdfColorSpace.DeviceCmyk ? _graphicsState.CmykStroke : new PdfCmykColor(0, 0, 0, 0);

    public double GrayFill => _graphicsState.FillColorSpace == PdfColorSpace.DeviceGray ? _graphicsState.GrayFill : 0;

    public double GrayStroke => _graphicsState.StrokeColorSpace == PdfColorSpace.DeviceGray ? _graphicsState.GrayStroke : 0;

    public void SetWidth(double width)
    {
        ValidatePositive(width, nameof(width), HaruStatus.PageInvalidSize);
        Width = width;
    }

    public void SetHeight(double height)
    {
        ValidatePositive(height, nameof(height), HaruStatus.PageInvalidSize);
        Height = height;
    }

    public void SetSize(PdfPageSize size, PdfPageDirection direction)
    {
        var (width, height) = size switch
        {
            PdfPageSize.Letter => (612.0, 792.0),
            PdfPageSize.Legal => (612.0, 1008.0),
            PdfPageSize.A3 => (841.89, 1190.551),
            PdfPageSize.A4 => (595.276, 841.89),
            PdfPageSize.A5 => (419.528, 595.276),
            PdfPageSize.B4 => (708.661, 1000.63),
            PdfPageSize.B5 => (498.898, 708.661),
            PdfPageSize.Executive => (522.0, 756.0),
            PdfPageSize.US4x6 => (288.0, 432.0),
            PdfPageSize.US4x8 => (288.0, 576.0),
            PdfPageSize.US5x7 => (360.0, 504.0),
            PdfPageSize.Comm10 => (297.0, 684.0),
            _ => throw Owner.CreateException(HaruStatus.PageInvalidSize, "Unknown page size.")
        };

        if (!Enum.IsDefined(direction))
            throw Owner.CreateException(HaruStatus.PageInvalidDirection, "Page direction is out of range.");

        if (direction == PdfPageDirection.Landscape)
            (width, height) = (height, width);

        Width = width;
        Height = height;
        _boundaries.Remove(PdfPageBoundary.MediaBox);
    }

    public void SetBoundary(PdfPageBoundary boundary, PdfRect rect)
    {
        if (!Enum.IsDefined(boundary))
            throw Owner.CreateException(HaruStatus.PageInvalidBoundary, "Unknown page boundary.");

        ValidateRect(rect, HaruStatus.PageInvalidBoundary);
        _boundaries[boundary] = rect;

        if (boundary == PdfPageBoundary.MediaBox)
        {
            Width = rect.Right - rect.Left;
            Height = rect.Top - rect.Bottom;
        }
    }

    public void SetRotate(ushort angle)
    {
        if (angle % 90 != 0)
            throw Owner.CreateException(HaruStatus.PageInvalidRotateValue, "Page rotation angle must be a multiple of 90 degrees.", angle);

        _rotate = angle;
    }

    public void SetZoom(double zoom)
    {
        if (zoom < 0.08 || zoom > 32 || double.IsNaN(zoom) || double.IsInfinity(zoom))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Page zoom must be between 0.08 and 32.");

        _zoom = zoom;
    }

    public void SetSlideShow(PdfTransitionStyle style, double displayTime, double transitionTime)
    {
        if (!Enum.IsDefined(style))
            throw Owner.CreateException(HaruStatus.InvalidPageSlideshowType, "Unknown page transition style.");

        if (displayTime < 0 || double.IsNaN(displayTime) || double.IsInfinity(displayTime))
            throw Owner.CreateException(HaruStatus.PageInvalidTransitionTime, "Display time must be a non-negative finite number.");

        if (transitionTime < 0 || double.IsNaN(transitionTime) || double.IsInfinity(transitionTime))
            throw Owner.CreateException(HaruStatus.PageInvalidTransitionTime, "Transition time must be a non-negative finite number.");

        _slideShow = new PdfSlideShow(style, displayTime, transitionTime);
    }

    public void SetFontAndSize(PdfFont font, double size)
    {
        if (font is null)
            throw Owner.CreateException(HaruStatus.PageInvalidFont, "Font cannot be null.");

        if (!ReferenceEquals(font.Owner, Owner))
            throw Owner.CreateException(HaruStatus.PageInvalidFont, "Font does not belong to this document.");

        font.ValidateOrThrow(HaruStatus.PageInvalidFont);
        ValidatePositive(size, nameof(size), HaruStatus.PageInvalidFontSize);

        _graphicsState = _graphicsState with
        {
            Font = font,
            FontSize = size,
            WritingMode = font.EncodingModel.WritingMode
        };
        UseFont(font);

        if (_inText)
            WriteFontSelection();
    }

    public double TextWidth(string text)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        EnsureFont();
        return _graphicsState.Font!.TextWidth(text, _graphicsState.FontSize);
    }

    public int MeasureText(string text, double width, bool wordWrap, out double realWidth)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        ValidatePositive(width, nameof(width));
        EnsureFont();

        var measured = 0.0;
        var lastBreak = -1;
        var lastBreakWidth = 0.0;

        for (var i = 0; i < text.Length; i++)
        {
            var charWidth = _graphicsState.Font!.TextWidth(text[i].ToString(), _graphicsState.FontSize);
            if (measured + charWidth > width)
            {
                if (wordWrap && lastBreak >= 0)
                {
                    realWidth = lastBreakWidth;
                    return lastBreak + 1;
                }

                realWidth = measured;
                return i;
            }

            measured += charWidth;

            if (char.IsWhiteSpace(text[i]))
            {
                lastBreak = i;
                lastBreakWidth = measured;
            }
        }

        realWidth = measured;
        return text.Length;
    }

    public void BeginText()
    {
        if (_inText)
            throw Owner.CreateException(HaruStatus.PageInvalidGmode, "A text object is already open.");

        WriteOperator("BT");
        _inText = true;
        _graphicsMode = PdfGraphicsMode.TextObject;
        _currentTextPosition = new PdfPoint(0, 0);
        _textMatrix = PdfTransMatrix.Identity;

        if (_graphicsState.Font is not null)
            WriteFontSelection();
    }

    public void EndText()
    {
        if (!_inText)
            throw Owner.CreateException(HaruStatus.PageInvalidGmode, "No text object is open.");

        WriteOperator("ET");
        _inText = false;
        _graphicsMode = PdfGraphicsMode.PageDescription;
        _currentTextPosition = new PdfPoint(0, 0);
    }

    public void MoveTextPos(double x, double y)
    {
        EnsureTextMode();
        WriteOperator($"{N(x)} {N(y)} Td");
        MoveTextPosition(x, y);
    }

    public void MoveTextPos2(double x, double y)
    {
        EnsureTextMode();
        WriteOperator($"{N(x)} {N(y)} TD");
        MoveTextPosition(x, y);
        _graphicsState = _graphicsState with { TextLeading = -y };
    }

    public void SetTextMatrix(double a, double b, double c, double d, double x, double y)
    {
        EnsureTextMode();
        WriteOperator($"{N(a)} {N(b)} {N(c)} {N(d)} {N(x)} {N(y)} Tm");
        _textMatrix = new PdfTransMatrix(a, b, c, d, x, y);
        _currentTextPosition = new PdfPoint(x, y);
    }

    public void ShowText(string text)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        EnsureTextMode();
        EnsureFont();
        WritePdfString(text);
        WriteOperator(" Tj", leadingSpace: false);
        AdvanceTextPosition(text);
    }

    public void TextOut(double x, double y, string text)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        EnsureFont();

        if (_inText)
        {
            SetTextMatrix(1, 0, 0, 1, x, y);
            ShowText(text);
            return;
        }

        BeginText();
        SetTextMatrix(1, 0, 0, 1, x, y);
        ShowText(text);
        EndText();
    }

    public void SetTextRenderingMode(PdfTextRenderingMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Text rendering mode is out of range.", (uint)mode);

        _graphicsState = _graphicsState with { TextRenderingMode = mode };
        WriteOperator($"{(uint)mode} Tr");
    }

    public void SetCharSpace(double value)
    {
        _graphicsState = _graphicsState with { CharSpace = value };
        WriteOperator($"{N(value)} Tc");
    }

    public void SetWordSpace(double value)
    {
        _graphicsState = _graphicsState with { WordSpace = value };
        WriteOperator($"{N(value)} Tw");
    }

    public void SetHorizontalScalling(double value)
    {
        _graphicsState = _graphicsState with { HorizontalScaling = value };
        WriteOperator($"{N(value)} Tz");
    }

    public void SetTextLeading(double value)
    {
        _graphicsState = _graphicsState with { TextLeading = value };
        WriteOperator($"{N(value)} TL");
    }

    public void SetTextRise(double value)
    {
        _graphicsState = _graphicsState with { TextRise = value };
        WriteOperator($"{N(value)} Ts");
    }

    public void MoveToNextLine()
    {
        EnsureTextMode();
        WriteOperator("T*");
        MoveToNextLineState();
    }

    public uint TextRect(double left, double top, double right, double bottom, string text, PdfTextAlignment align, out uint length)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        if (!Enum.IsDefined(align))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text alignment is out of range.");

        if (!IsFinite(left) || !IsFinite(top) || !IsFinite(right) || !IsFinite(bottom) || right <= left || top <= bottom)
            throw Owner.CreateException(HaruStatus.PageInvalidBoundary, "Text rectangle must be finite and have positive width and height.");

        EnsureTextMode();
        EnsureFont();

        length = 0;
        if (text.Length == 0)
            return HaruStatus.OK;

        var lineHeight = _graphicsState.TextLeading;
        if (lineHeight <= 0)
        {
            lineHeight = _graphicsState.FontSize * 1.2;
            SetTextLeading(lineHeight);
        }

        var y = top - lineHeight;
        var remaining = text;
        while (remaining.Length > 0)
        {
            if (y < bottom)
                return HaruStatus.PageInsufficientSpace;

            var lineLength = MeasureText(remaining, right - left, wordWrap: true, out var realWidth);
            if (lineLength <= 0)
                return HaruStatus.PageInsufficientSpace;

            var visibleLength = lineLength;
            while (visibleLength > 0 && char.IsWhiteSpace(remaining[visibleLength - 1]))
                visibleLength--;

            var line = remaining[..visibleLength];
            var lineWidth = line.Length == 0 ? 0 : TextWidth(line);
            var x = align switch
            {
                PdfTextAlignment.Right => right - lineWidth,
                PdfTextAlignment.Center => left + (right - left - lineWidth) / 2,
                _ => left
            };

            SetTextMatrix(1, 0, 0, 1, x, y);
            if (line.Length > 0)
                ShowText(line);

            length += (uint)lineLength;
            remaining = remaining[lineLength..];
            while (remaining.Length > 0 && (remaining[0] == '\r' || remaining[0] == '\n'))
            {
                remaining = remaining[1..];
                length++;
                break;
            }

            y -= lineHeight;
            _ = realWidth;
        }

        return HaruStatus.OK;
    }

    public void ShowTextNextLine(string text)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        EnsureTextMode();
        EnsureFont();
        if (text.Length == 0)
        {
            MoveToNextLine();
            return;
        }

        WritePdfString(text);
        WriteOperator(" '", leadingSpace: false);
        MoveToNextLineState();
        AdvanceTextPosition(text);
    }

    public void ShowTextNextLineEx(double wordSpace, double charSpace, string text)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        EnsureTextMode();
        EnsureFont();

        if (text.Length == 0)
        {
            MoveToNextLine();
            return;
        }

        WriteAscii($"{N(wordSpace)} {N(charSpace)} ");
        WritePdfString(text);
        WriteOperator(" \"", leadingSpace: false);
        _graphicsState = _graphicsState with
        {
            WordSpace = wordSpace,
            CharSpace = charSpace
        };
        MoveToNextLineState();
        AdvanceTextPosition(text);
    }

    public void MoveTo(double x, double y)
    {
        WriteOperator($"{N(x)} {N(y)} m");
        SetCurrentPosition(x, y);
    }

    public void LineTo(double x, double y)
    {
        WriteOperator($"{N(x)} {N(y)} l");
        SetCurrentPosition(x, y);
    }

    public void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3)
    {
        WriteOperator($"{N(x1)} {N(y1)} {N(x2)} {N(y2)} {N(x3)} {N(y3)} c");
        SetCurrentPosition(x3, y3);
    }

    public void CurveTo2(double x2, double y2, double x3, double y3)
    {
        WriteOperator($"{N(x2)} {N(y2)} {N(x3)} {N(y3)} v");
        SetCurrentPosition(x3, y3);
    }

    public void CurveTo3(double x1, double y1, double x3, double y3)
    {
        WriteOperator($"{N(x1)} {N(y1)} {N(x3)} {N(y3)} y");
        SetCurrentPosition(x3, y3);
    }

    public void Rectangle(double x, double y, double width, double height)
    {
        WriteOperator($"{N(x)} {N(y)} {N(width)} {N(height)} re");
        SetCurrentPosition(x, y);
    }

    public void Arc(double x, double y, double radius, double startAngle, double endAngle)
    {
        ValidatePositive(radius, nameof(radius), HaruStatus.PageOutOfRange);

        if (Math.Abs(endAngle - startAngle) >= 360)
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Arc angle span must be less than 360 degrees.");

        while (startAngle < 0 || endAngle < 0)
        {
            startAngle += 360;
            endAngle += 360;
        }

        var continued = false;
        for (;;)
        {
            if (Math.Abs(endAngle - startAngle) <= 90)
            {
                InternalArc(x, y, radius, startAngle, endAngle, continued);
                return;
            }

            var nextAngle = endAngle > startAngle ? startAngle + 90 : startAngle - 90;
            InternalArc(x, y, radius, startAngle, nextAngle, continued);
            startAngle = nextAngle;

            if (Math.Abs(startAngle - endAngle) < 0.1)
                return;

            continued = true;
        }
    }

    public void Circle(double x, double y, double radius)
    {
        ValidatePositive(radius, nameof(radius), HaruStatus.PageOutOfRange);

        MoveTo(x - radius, y);
        QuarterCircleA(x, y, radius);
        QuarterCircleB(x, y, radius);
        QuarterCircleC(x, y, radius);
        QuarterCircleD(x, y, radius);
        SetCurrentPosition(x - radius, y);
    }

    public void Ellipse(double x, double y, double xRadius, double yRadius)
    {
        ValidatePositive(xRadius, nameof(xRadius), HaruStatus.PageOutOfRange);
        ValidatePositive(yRadius, nameof(yRadius), HaruStatus.PageOutOfRange);

        const double kappa = 0.552;
        MoveTo(x - xRadius, y);
        CurveTo(x - xRadius, y + yRadius * kappa, x - xRadius * kappa, y + yRadius, x, y + yRadius);
        CurveTo(x + xRadius * kappa, y + yRadius, x + xRadius, y + yRadius * kappa, x + xRadius, y);
        CurveTo(x + xRadius, y - yRadius * kappa, x + xRadius * kappa, y - yRadius, x, y - yRadius);
        CurveTo(x - xRadius * kappa, y - yRadius, x - xRadius, y - yRadius * kappa, x - xRadius, y);
        SetCurrentPosition(x - xRadius, y);
    }

    public void ClosePath() => WriteOperator("h");

    public void Stroke()
    {
        WriteOperator("S");
        ClearCurrentPath();
    }

    public void Fill()
    {
        WriteOperator("f");
        ClearCurrentPath();
    }

    public void Eofill()
    {
        WriteOperator("f*");
        ClearCurrentPath();
    }

    public void FillStroke()
    {
        WriteOperator("B");
        ClearCurrentPath();
    }

    public void EofillStroke()
    {
        WriteOperator("B*");
        ClearCurrentPath();
    }

    public void ClosePathStroke()
    {
        WriteOperator("s");
        ClearCurrentPath();
    }

    public void ClosePathFillStroke()
    {
        WriteOperator("b");
        ClearCurrentPath();
    }

    public void ClosePathEofillStroke()
    {
        WriteOperator("b*");
        ClearCurrentPath();
    }

    public void EndPath()
    {
        WriteOperator("n");
        ClearCurrentPath();
    }

    public void Clip()
    {
        WriteOperator("W");
        _graphicsMode = PdfGraphicsMode.ClippingPath;
    }

    public void Eoclip()
    {
        WriteOperator("W*");
        _graphicsMode = PdfGraphicsMode.ClippingPath;
    }

    public void GSave()
    {
        _graphicsStateStack.Push(_graphicsState);
        _graphicsState = _graphicsState with { Depth = _graphicsState.Depth + 1 };
        WriteOperator("q");
    }

    public void GRestore()
    {
        if (_graphicsStateStack.Count == 0)
            throw Owner.CreateException(HaruStatus.PageCannotRestoreGstate, "No saved graphics state is available.");

        _graphicsState = _graphicsStateStack.Pop();
        WriteOperator("Q");
    }

    public void SetLineWidth(double width)
    {
        if (width < 0 || double.IsNaN(width) || double.IsInfinity(width))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "width must be a non-negative finite number.");

        _graphicsState = _graphicsState with { LineWidth = width };
        WriteOperator($"{N(width)} w");
    }

    public void SetLineCap(PdfLineCap cap)
    {
        if (!Enum.IsDefined(cap))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Line cap is out of range.", (uint)cap);

        _graphicsState = _graphicsState with { LineCap = cap };
        WriteOperator($"{(uint)cap} J");
    }

    public void SetLineJoin(PdfLineJoin join)
    {
        if (!Enum.IsDefined(join))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Line join is out of range.", (uint)join);

        _graphicsState = _graphicsState with { LineJoin = join };
        WriteOperator($"{(uint)join} j");
    }

    public void SetMiterLimit(double limit)
    {
        if (limit < 1 || double.IsNaN(limit) || double.IsInfinity(limit))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Miter limit must be a finite number greater than or equal to 1.");

        _graphicsState = _graphicsState with { MiterLimit = limit };
        WriteOperator($"{N(limit)} M");
    }

    public void SetDash(IReadOnlyList<double> pattern, double phase)
    {
        if (pattern is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Dash pattern cannot be null.");

        if (pattern.Count > 8)
            throw Owner.CreateException(HaruStatus.PageInvalidParamCount, "Dash patterns can contain at most eight entries.");

        if (pattern.Count == 0 && phase > 0)
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "A solid dash pattern cannot have a positive phase.");

        foreach (var value in pattern)
        {
            if (value <= 0 || value > 100 || double.IsNaN(value) || double.IsInfinity(value))
                throw Owner.CreateException(HaruStatus.PageOutOfRange, "Dash pattern entries must be positive finite values no greater than 100.");
        }

        if (phase < 0 || double.IsNaN(phase) || double.IsInfinity(phase))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Dash phase must be a non-negative finite number.");

        _graphicsState = _graphicsState with { DashMode = new PdfDashMode(pattern, phase) };
        var values = string.Join(" ", pattern.Select(N));
        WriteOperator($"[{values}] {N(phase)} d");
    }

    public void SetFlat(double flatness)
    {
        if (flatness < 0 || flatness > 100 || double.IsNaN(flatness) || double.IsInfinity(flatness))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, "Flatness must be between 0 and 100.");

        _graphicsState = _graphicsState with { Flatness = flatness };
        WriteOperator($"{N(flatness)} i");
    }

    public void SetRGBFill(double r, double g, double b)
    {
        _graphicsState = _graphicsState with
        {
            RgbFill = ValidateRgb(r, g, b),
            FillColorSpace = PdfColorSpace.DeviceRgb
        };
        WriteOperator($"{N(r)} {N(g)} {N(b)} rg");
    }

    public void SetRGBStroke(double r, double g, double b)
    {
        _graphicsState = _graphicsState with
        {
            RgbStroke = ValidateRgb(r, g, b),
            StrokeColorSpace = PdfColorSpace.DeviceRgb
        };
        WriteOperator($"{N(r)} {N(g)} {N(b)} RG");
    }

    public void SetGrayFill(double gray)
    {
        ValidateUnit(gray, nameof(gray));
        _graphicsState = _graphicsState with
        {
            GrayFill = gray,
            FillColorSpace = PdfColorSpace.DeviceGray
        };
        WriteOperator($"{N(gray)} g");
    }

    public void SetGrayStroke(double gray)
    {
        ValidateUnit(gray, nameof(gray));
        _graphicsState = _graphicsState with
        {
            GrayStroke = gray,
            StrokeColorSpace = PdfColorSpace.DeviceGray
        };
        WriteOperator($"{N(gray)} G");
    }

    public void SetCMYKFill(double c, double m, double y, double k)
    {
        _graphicsState = _graphicsState with
        {
            CmykFill = ValidateCmyk(c, m, y, k),
            FillColorSpace = PdfColorSpace.DeviceCmyk
        };
        WriteOperator($"{N(c)} {N(m)} {N(y)} {N(k)} k");
    }

    public void SetCMYKStroke(double c, double m, double y, double k)
    {
        _graphicsState = _graphicsState with
        {
            CmykStroke = ValidateCmyk(c, m, y, k),
            StrokeColorSpace = PdfColorSpace.DeviceCmyk
        };
        WriteOperator($"{N(c)} {N(m)} {N(y)} {N(k)} K");
    }

    public void Concat(double a, double b, double c, double d, double x, double y)
    {
        var tm = _graphicsState.TransMatrix;
        _graphicsState = _graphicsState with
        {
            TransMatrix = new PdfTransMatrix(
                tm.A * a + tm.B * c,
                tm.A * b + tm.B * d,
                tm.C * a + tm.D * c,
                tm.C * b + tm.D * d,
                tm.X * a + tm.Y * c + x,
                tm.X * b + tm.Y * d + y)
        };
        WriteOperator($"{N(a)} {N(b)} {N(c)} {N(d)} {N(x)} {N(y)} cm");
    }

    public void DrawImage(PdfImage image, double x, double y, double width, double height)
    {
        ValidateXObject(image);

        ValidatePositive(width, nameof(width), HaruStatus.PageInvalidXObject);
        ValidatePositive(height, nameof(height), HaruStatus.PageInvalidXObject);

        GSave();
        Concat(width, 0, 0, height, x, y);
        WriteOperator($"/{image.ResourceName} Do");
        GRestore();
    }

    public void ExecuteXObject(PdfImage image)
    {
        ValidateXObject(image);
        WriteOperator($"/{image.ResourceName} Do");
    }

    public void ExecuteXObject(PdfXObject xObject)
    {
        ValidateXObject(xObject);
        WriteOperator($"/{xObject.ResourceName} Do");
    }

    public string GetXObjectName(PdfImage image)
    {
        ValidateXObject(image);
        return image.ResourceName;
    }

    public string GetXObjectName(PdfXObject xObject)
    {
        ValidateXObject(xObject);
        return xObject.ResourceName;
    }

    public PdfContentStream NewContentStream()
    {
        var streamObject = Owner.AddObject(new PdfStreamObject([]) { Subclass = PdfObjectClass.ContentStream });
        _contents = new MemoryStream();
        var contentStream = new PdfContentStream(Owner, streamObject, _contents);
        _contentStreams.Add(contentStream);
        return contentStream;
    }

    public void InsertSharedContentStream(PdfContentStream sharedStream)
    {
        if (sharedStream is null || !ReferenceEquals(sharedStream.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidStream, "Shared content stream does not belong to this document.");

        sharedStream.ValidateOrThrow();
        _contentStreams.Add(sharedStream);
        NewContentStream();
    }

    public PdfDestination CreateDestination()
    {
        var destinationObject = Owner.AddObject(new PdfArray { Subclass = PdfObjectClass.Destination });
        return new PdfDestination(Owner, this, destinationObject);
    }

    public PdfAnnotation CreateLinkAnnotation(PdfRect rect, PdfDestination destination)
    {
        if (destination is null || !ReferenceEquals(destination.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidDestination, "Link destination does not belong to this document.");

        destination.ValidateOrThrow();
        var annotation = CreateAnnotation(rect, "Link");
        annotation.Dictionary.Set("Dest", destination.DestinationObject.Reference);
        return annotation;
    }

    public PdfAnnotation CreateURILinkAnnotation(PdfRect rect, string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw Owner.CreateException(HaruStatus.InvalidUri, "URI annotation target cannot be empty.");

        var annotation = CreateAnnotation(rect, "Link");
        var action = new PdfDictionary();
        action.SetName("Type", "Action");
        action.SetName("S", "URI");
        action.Set("URI", PdfString.FromText(uri));
        annotation.Dictionary.Set("A", action);
        return annotation;
    }

    public PdfAnnotation CreateTextAnnotation(PdfRect rect, string text)
    {
        var annotation = CreateAnnotation(rect, "Text");
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        return annotation;
    }

    public PdfAnnotation CreateFreeTextAnnotation(PdfRect rect, string text)
    {
        var annotation = CreateAnnotation(rect, "FreeText");
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        return annotation;
    }

    public PdfAnnotation CreateLineAnnotation(PdfRect rect, string text, PdfPoint startPoint, PdfPoint endPoint)
    {
        var annotation = CreateAnnotation(rect, "Line");
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        annotation.Dictionary.Set("L", new PdfArray([new PdfReal(startPoint.X), new PdfReal(startPoint.Y), new PdfReal(endPoint.X), new PdfReal(endPoint.Y)]));
        return annotation;
    }

    public PdfAnnotation CreateWidgetAnnotation(PdfRect rect)
    {
        return CreateAnnotation(rect, "Widget");
    }

    public PdfAnnotation CreateWidgetAnnotationWhiteOnlyWhilePrint(PdfRect rect)
    {
        var annotation = CreateWidgetAnnotation(rect);
        var xObject = Owner.CreateXObjectAsWhiteRect(this, rect);
        var appearances = new PdfDictionary();
        appearances.Set("N", xObject.XObjectObject.Reference);
        annotation.Dictionary.Set("AP", appearances);

        var mk = new PdfDictionary();
        mk.Set("BG", new PdfArray([new PdfReal(1), new PdfReal(1), new PdfReal(1)]));
        annotation.Dictionary.Set("MK", mk);
        annotation.Dictionary.SetName("FT", "Btn");
        annotation.Dictionary.Set("F", new PdfInteger(36));
        annotation.Dictionary.Set("T", PdfString.FromText("Blind"));
        return annotation;
    }

    public PdfAnnotation CreateSquareAnnotation(PdfRect rect, string text)
    {
        var annotation = CreateAnnotation(rect, "Square");
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        return annotation;
    }

    public PdfAnnotation CreateCircleAnnotation(PdfRect rect, string text)
    {
        var annotation = CreateAnnotation(rect, "Circle");
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        return annotation;
    }

    public PdfAnnotation CreateHighlightAnnotation(PdfRect rect, string text) => CreateTextMarkupAnnotation(rect, text, "Highlight");

    public PdfAnnotation CreateTextMarkupAnnotation(PdfRect rect, string text, PdfAnnotType subtype)
    {
        return subtype switch
        {
            PdfAnnotType.Highlight => CreateHighlightAnnotation(rect, text),
            PdfAnnotType.Underline => CreateUnderlineAnnotation(rect, text),
            PdfAnnotType.Squiggly => CreateSquigglyAnnotation(rect, text),
            PdfAnnotType.StrikeOut => CreateStrikeOutAnnotation(rect, text),
            _ => throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Unsupported text-markup annotation subtype.")
        };
    }

    public PdfAnnotation CreateUnderlineAnnotation(PdfRect rect, string text) => CreateTextMarkupAnnotation(rect, text, "Underline");

    public PdfAnnotation CreateSquigglyAnnotation(PdfRect rect, string text) => CreateTextMarkupAnnotation(rect, text, "Squiggly");

    public PdfAnnotation CreateStrikeOutAnnotation(PdfRect rect, string text) => CreateTextMarkupAnnotation(rect, text, "StrikeOut");

    public PdfAnnotation CreatePopupAnnotation(PdfRect rect, PdfAnnotation parent)
    {
        if (parent is null || !ReferenceEquals(parent.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Popup parent annotation does not belong to this document.");

        var annotation = CreateAnnotation(rect, "Popup");
        annotation.Dictionary.Set("Parent", parent.AnnotationObject.Reference);
        parent.Dictionary.Set("Popup", annotation.AnnotationObject.Reference);
        return annotation;
    }

    public PdfAnnotation CreateStampAnnotation(PdfRect rect, string name, string text)
    {
        var annotation = CreateAnnotation(rect, "Stamp");
        annotation.Dictionary.SetName("Name", string.IsNullOrWhiteSpace(name) ? "Draft" : name);
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        return annotation;
    }

    public PdfAnnotation CreateProjectionAnnotation(PdfRect rect, string text)
    {
        var annotation = CreateAnnotation(rect, "Projection");
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        return annotation;
    }

    public PdfAnnotation Create3DAnnotation(PdfRect rect, PdfU3D u3d)
    {
        if (u3d is null || !ReferenceEquals(u3d.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "U3D object does not belong to this document.");

        u3d.ValidateOrThrow();
        var annotation = CreateAnnotation(rect, "3D");
        annotation.Dictionary.Set("Contents", PdfString.FromText("3D Model"));
        annotation.Dictionary.Set("3DD", u3d.U3DObject.Reference);

        var activation = new PdfDictionary();
        activation.SetName("A", "PO");
        activation.SetName("DIS", "I");
        annotation.Dictionary.Set("3DA", activation);
        return annotation;
    }

    public PdfExData Create3DAnnotExData()
    {
        var exData = new PdfDictionary { Subclass = PdfObjectClass.ExData };
        exData.SetName("Type", "ExData");
        exData.SetName("Subtype", "3DM");
        return new PdfExData(Owner, Owner.AddObject(exData));
    }

    public void SetExtGState(PdfExtGState extGState)
    {
        if (extGState is null || !ReferenceEquals(extGState.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidExtGState, "Extended graphics state does not belong to this document.");

        extGState.ValidateOrThrow(writable: false);
        _extGStates.TryAdd(extGState.ResourceName, extGState);
        WriteOperator($"/{extGState.ResourceName} gs");
    }

    public void SetShading(PdfShading shading)
    {
        if (shading is null || !ReferenceEquals(shading.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidShadingType, "Shading does not belong to this document.");

        shading.ValidateOrThrow();
        _shadings.TryAdd(shading.ResourceName, shading);
        WriteOperator($"/{shading.ResourceName} sh");
    }

    internal void ValidateOrThrow()
    {
        if (PageObject.Value is not PdfDictionary pageDictionary)
            throw Owner.CreateException(HaruStatus.InvalidPage, "Page object must be a dictionary.");

        if (!pageDictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Page))
            throw Owner.CreateException(HaruStatus.InvalidPage, "Page object must be a page dictionary.");
    }

    internal void PrepareForSave(PdfIndirectReference parent)
    {
        if (_inText)
            EndText();

        if (PageObject.Value is not PdfDictionary pageDictionary)
            throw Owner.CreateException(HaruStatus.InvalidPage, "Page object must be a dictionary.");

        if (!pageDictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Page))
            throw Owner.CreateException(HaruStatus.InvalidPage, "Page object must be a page dictionary.");

        var resources = new PdfDictionary();
        var fontResources = new PdfDictionary();
        var xObjectResources = new PdfDictionary();
        var extGStateResources = new PdfDictionary();
        var shadingResources = new PdfDictionary();

        foreach (var font in _fonts.Values)
            fontResources.Set(font.ResourceName, font.FontObject.Reference);

        if (fontResources.Count > 0)
            resources.Set("Font", fontResources);

        foreach (var image in _xObjects.Values)
            xObjectResources.Set(image.ResourceName, image.ImageObject.Reference);

        foreach (var xObject in _formXObjects.Values)
            xObjectResources.Set(xObject.ResourceName, xObject.XObjectObject.Reference);

        if (xObjectResources.Count > 0)
            resources.Set("XObject", xObjectResources);

        foreach (var extGState in _extGStates.Values)
            extGStateResources.Set(extGState.ResourceName, extGState.GraphicsStateObject.Reference);

        if (extGStateResources.Count > 0)
            resources.Set("ExtGState", extGStateResources);

        foreach (var shading in _shadings.Values)
            shadingResources.Set(shading.ResourceName, shading.ShadingObject.Reference);

        if (shadingResources.Count > 0)
            resources.Set("Shading", shadingResources);

        pageDictionary.SetName("Type", "Page");
        pageDictionary.Set("Parent", parent);
        pageDictionary.Set("MediaBox", RectArray(_boundaries.GetValueOrDefault(PdfPageBoundary.MediaBox, new PdfRect(0, 0, Width, Height))));

        SetOptionalBoundary(pageDictionary, PdfPageBoundary.CropBox, "CropBox");
        SetOptionalBoundary(pageDictionary, PdfPageBoundary.BleedBox, "BleedBox");
        SetOptionalBoundary(pageDictionary, PdfPageBoundary.TrimBox, "TrimBox");
        SetOptionalBoundary(pageDictionary, PdfPageBoundary.ArtBox, "ArtBox");

        if (_rotate is { } rotate)
            pageDictionary.Set("Rotate", new PdfInteger(rotate));
        else
            pageDictionary.Remove("Rotate");

        if (_zoom is { } zoom)
            pageDictionary.Set("PZ", new PdfReal(zoom));
        else
            pageDictionary.Remove("PZ");

        pageDictionary.Set("Resources", resources);
        if (_contentStreams.Count == 1)
            pageDictionary.Set("Contents", _contentStreams[0].StreamObject.Reference);
        else
            pageDictionary.Set("Contents", new PdfArray(_contentStreams.Select(static stream => stream.StreamObject.Reference)));

        if (_annotations.Count > 0)
            pageDictionary.Set("Annots", new PdfArray(_annotations.Select(static annotation => annotation.AnnotationObject.Reference)));
        else
            pageDictionary.Remove("Annots");

        if (_slideShow is { } slideShow)
        {
            pageDictionary.Set("Dur", new PdfReal(slideShow.DisplayTime));
            pageDictionary.Set("Trans", CreateTransitionDictionary(slideShow));
        }
        else
        {
            pageDictionary.Remove("Dur");
            pageDictionary.Remove("Trans");
        }

        foreach (var contentStream in _contentStreams)
        {
            contentStream.ValidateOrThrow();
            if (contentStream.StreamObject.Value is PdfStreamObject stream)
            {
                stream.Kind = PdfStreamKind.PageContent;
                stream.CompressionMode = Owner.CompressionMode;
                stream.SetData(contentStream.Buffer.ToArray());
            }
        }
    }

    private PdfAnnotation CreateTextMarkupAnnotation(PdfRect rect, string text, string subtype)
    {
        var annotation = CreateAnnotation(rect, subtype);
        annotation.Dictionary.Set("Contents", PdfString.FromText(text ?? string.Empty));
        annotation.Dictionary.Set("QuadPoints", new PdfArray([
            new PdfReal(rect.Left), new PdfReal(rect.Top),
            new PdfReal(rect.Right), new PdfReal(rect.Top),
            new PdfReal(rect.Left), new PdfReal(rect.Bottom),
            new PdfReal(rect.Right), new PdfReal(rect.Bottom)
        ]));
        return annotation;
    }

    private PdfAnnotation CreateAnnotation(PdfRect rect, string subtype)
    {
        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Annotation };
        dictionary.SetName("Type", "Annot");
        dictionary.SetName("Subtype", subtype);
        dictionary.Set("Rect", PdfFeatureHelpers.RectArray(rect));
        dictionary.Set("P", PageObject.Reference);
        var obj = Owner.AddObject(dictionary);
        var annotation = new PdfAnnotation(Owner, this, obj, subtype);
        _annotations.Add(annotation);
        return annotation;
    }

    private void UseFont(PdfFont font)
    {
        _fonts.TryAdd(font.ResourceName, font);
    }

    private void InternalArc(double x, double y, double radius, double startAngle, double endAngle, bool continued)
    {
        const double pi = Math.PI;

        var deltaAngle = (90 - (startAngle + endAngle) / 2) / 180 * pi;
        var newAngle = (endAngle - startAngle) / 2 / 180 * pi;

        var rx0 = radius * Math.Cos(newAngle);
        var ry0 = radius * Math.Sin(newAngle);
        var rx2 = (radius * 4.0 - rx0) / 3.0;
        var ry2 = ((radius - rx0) * (rx0 - radius * 3.0)) / (3.0 * ry0);
        var rx1 = rx2;
        var ry1 = -ry2;
        var rx3 = rx0;
        var ry3 = -ry0;

        var x0 = RotateX(rx0, ry0, deltaAngle) + x;
        var y0 = RotateY(rx0, ry0, deltaAngle) + y;
        var x1 = RotateX(rx1, ry1, deltaAngle) + x;
        var y1 = RotateY(rx1, ry1, deltaAngle) + y;
        var x2 = RotateX(rx2, ry2, deltaAngle) + x;
        var y2 = RotateY(rx2, ry2, deltaAngle) + y;
        var x3 = RotateX(rx3, ry3, deltaAngle) + x;
        var y3 = RotateY(rx3, ry3, deltaAngle) + y;

        if (!continued)
        {
            if (_hasCurrentPath)
                WriteOperator($"{N(x0)} {N(y0)} l");
            else
                WriteOperator($"{N(x0)} {N(y0)} m");
        }

        CurveTo(x1, y1, x2, y2, x3, y3);
    }

    private void QuarterCircleA(double x, double y, double radius)
    {
        const double kappa = 0.552;
        CurveTo(x - radius, y + radius * kappa, x - radius * kappa, y + radius, x, y + radius);
    }

    private void QuarterCircleB(double x, double y, double radius)
    {
        const double kappa = 0.552;
        CurveTo(x + radius * kappa, y + radius, x + radius, y + radius * kappa, x + radius, y);
    }

    private void QuarterCircleC(double x, double y, double radius)
    {
        const double kappa = 0.552;
        CurveTo(x + radius, y - radius * kappa, x + radius * kappa, y - radius, x, y - radius);
    }

    private void QuarterCircleD(double x, double y, double radius)
    {
        const double kappa = 0.552;
        CurveTo(x - radius * kappa, y - radius, x - radius, y - radius * kappa, x - radius, y);
    }

    private static double RotateX(double x, double y, double angle) => x * Math.Cos(angle) - y * Math.Sin(angle);

    private static double RotateY(double x, double y, double angle) => x * Math.Sin(angle) + y * Math.Cos(angle);

    private void SetCurrentPosition(double x, double y)
    {
        _currentPosition = new PdfPoint(x, y);
        _hasCurrentPath = true;
        _graphicsMode = PdfGraphicsMode.PathObject;
    }

    private void ClearCurrentPath()
    {
        _hasCurrentPath = false;
        _graphicsMode = PdfGraphicsMode.PageDescription;
    }

    private void MoveTextPosition(double x, double y)
    {
        _textMatrix = _textMatrix with
        {
            X = _textMatrix.X + x * _textMatrix.A + y * _textMatrix.C,
            Y = _textMatrix.Y + y * _textMatrix.D + x * _textMatrix.B
        };
        _currentTextPosition = new PdfPoint(_textMatrix.X, _textMatrix.Y);
    }

    private void MoveToNextLineState()
    {
        _textMatrix = _textMatrix with
        {
            X = _textMatrix.X - _graphicsState.TextLeading * _textMatrix.C,
            Y = _textMatrix.Y - _graphicsState.TextLeading * _textMatrix.D
        };
        _currentTextPosition = new PdfPoint(_textMatrix.X, _textMatrix.Y);
    }

    private void AdvanceTextPosition(string text)
    {
        var width = TextWidth(text);

        if (_graphicsState.WritingMode == PdfWritingMode.Horizontal)
        {
            _currentTextPosition = new PdfPoint(
                _currentTextPosition.X + width * _textMatrix.A,
                _currentTextPosition.Y + width * _textMatrix.B);
        }
        else
        {
            _currentTextPosition = new PdfPoint(
                _currentTextPosition.X - width * _textMatrix.B,
                _currentTextPosition.Y - width * _textMatrix.A);
        }
    }

    private void ValidateXObject(PdfImage? image)
    {
        if (image is null || !ReferenceEquals(image.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidImage, "Image does not belong to this document.");

        image.ValidateOrThrow();
        _xObjects.TryAdd(image.ResourceName, image);
    }

    private void ValidateXObject(PdfXObject? xObject)
    {
        if (xObject is null || !ReferenceEquals(xObject.Owner, Owner))
            throw Owner.CreateException(HaruStatus.PageInvalidXObject, "XObject does not belong to this document.");

        xObject.ValidateOrThrow();
        _formXObjects.TryAdd(xObject.ResourceName, xObject);
    }

    private void SetOptionalBoundary(PdfDictionary pageDictionary, PdfPageBoundary boundary, string key)
    {
        if (_boundaries.TryGetValue(boundary, out var rect))
            pageDictionary.Set(key, RectArray(rect));
        else
            pageDictionary.Remove(key);
    }

    private static PdfArray RectArray(PdfRect rect)
    {
        return new PdfArray([
            new PdfReal(rect.Left),
            new PdfReal(rect.Bottom),
            new PdfReal(rect.Right),
            new PdfReal(rect.Top)
        ]);
    }

    private void WriteFontSelection()
    {
        if (_graphicsState.Font is null)
            return;

        WriteOperator($"/{_graphicsState.Font.ResourceName} {N(_graphicsState.FontSize)} Tf");
    }

    private void WritePdfString(string text)
    {
        EnsureFont();
        using var stream = new MemoryStream();
        var writer = new PdfWriter(stream) { Error = Owner.Error };
        PdfString.FromBytes(_graphicsState.Font!.EncodeText(text)).WriteTo(writer);
        _contents.Write(stream.ToArray());
    }

    private void WriteOperator(string op, bool leadingSpace = true)
    {
        if (leadingSpace)
            WriteAscii(op);
        else
            WriteAscii(op);

        WriteAscii("\n");
    }

    private void WriteAscii(string value)
    {
        var bytes = Ascii.GetBytes(value);
        _contents.Write(bytes, 0, bytes.Length);
    }

    private void EnsureFont()
    {
        if (_graphicsState.Font is null)
            throw Owner.CreateException(HaruStatus.PageInvalidFont, "A current font must be selected before writing text.");
    }

    private void EnsureTextMode()
    {
        if (!_inText)
            throw Owner.CreateException(HaruStatus.PageInvalidGmode, "This text operator requires BeginText().");
    }

    private void ValidatePositive(double value, string name, uint status = HaruStatus.InvalidParameter)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
            throw Owner.CreateException(status, $"{name} must be a positive finite number.");
    }

    private void ValidateRect(PdfRect rect, uint status)
    {
        if (!IsFinite(rect.Left) || !IsFinite(rect.Bottom) || !IsFinite(rect.Right) || !IsFinite(rect.Top) || rect.Right <= rect.Left || rect.Top <= rect.Bottom)
            throw Owner.CreateException(status, "Rectangle coordinates must be finite and define a positive area.");
    }

    private PdfRgbColor ValidateRgb(double r, double g, double b)
    {
        if (!IsUnit(r) || !IsUnit(g) || !IsUnit(b))
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "RGB components must be between 0 and 1.");

        return new PdfRgbColor(r, g, b);
    }

    private PdfCmykColor ValidateCmyk(double c, double m, double y, double k)
    {
        if (!IsUnit(c) || !IsUnit(m) || !IsUnit(y) || !IsUnit(k))
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "CMYK components must be between 0 and 1.");

        return new PdfCmykColor(c, m, y, k);
    }

    private static PdfDictionary CreateTransitionDictionary(PdfSlideShow slideShow)
    {
        var transition = new PdfDictionary();
        transition.SetName("Type", "Trans");
        transition.Set("D", new PdfReal(slideShow.TransitionTime));

        switch (slideShow.Style)
        {
            case PdfTransitionStyle.WipeRight:
                transition.SetName("S", "Wipe");
                transition.Set("Di", new PdfInteger(0));
                break;
            case PdfTransitionStyle.WipeUp:
                transition.SetName("S", "Wipe");
                transition.Set("Di", new PdfInteger(90));
                break;
            case PdfTransitionStyle.WipeLeft:
                transition.SetName("S", "Wipe");
                transition.Set("Di", new PdfInteger(180));
                break;
            case PdfTransitionStyle.WipeDown:
                transition.SetName("S", "Wipe");
                transition.Set("Di", new PdfInteger(270));
                break;
            case PdfTransitionStyle.BarnDoorsHorizontalOut:
                transition.SetName("S", "Split");
                transition.SetName("Dm", "H");
                transition.SetName("M", "O");
                break;
            case PdfTransitionStyle.BarnDoorsHorizontalIn:
                transition.SetName("S", "Split");
                transition.SetName("Dm", "H");
                transition.SetName("M", "I");
                break;
            case PdfTransitionStyle.BarnDoorsVerticalOut:
                transition.SetName("S", "Split");
                transition.SetName("Dm", "V");
                transition.SetName("M", "O");
                break;
            case PdfTransitionStyle.BarnDoorsVerticalIn:
                transition.SetName("S", "Split");
                transition.SetName("Dm", "V");
                transition.SetName("M", "I");
                break;
            case PdfTransitionStyle.BoxOut:
                transition.SetName("S", "Box");
                transition.SetName("M", "O");
                break;
            case PdfTransitionStyle.BoxIn:
                transition.SetName("S", "Box");
                transition.SetName("M", "I");
                break;
            case PdfTransitionStyle.BlindsHorizontal:
                transition.SetName("S", "Blinds");
                transition.SetName("Dm", "H");
                break;
            case PdfTransitionStyle.BlindsVertical:
                transition.SetName("S", "Blinds");
                transition.SetName("Dm", "V");
                break;
            case PdfTransitionStyle.Dissolve:
                transition.SetName("S", "Dissolve");
                break;
            case PdfTransitionStyle.GlitterRight:
                transition.SetName("S", "Glitter");
                transition.Set("Di", new PdfInteger(0));
                break;
            case PdfTransitionStyle.GlitterDown:
                transition.SetName("S", "Glitter");
                transition.Set("Di", new PdfInteger(270));
                break;
            case PdfTransitionStyle.GlitterTopLeftToBottomRight:
                transition.SetName("S", "Glitter");
                transition.Set("Di", new PdfInteger(315));
                break;
            case PdfTransitionStyle.Replace:
                transition.SetName("S", "R");
                break;
            default:
                throw new UnreachableException();
        }

        return transition;
    }

    private void ValidateUnit(double value, string name)
    {
        if (!IsUnit(value))
            throw Owner.CreateException(HaruStatus.PageOutOfRange, $"{name} must be between 0 and 1.");
    }

    private static bool IsUnit(double value) => value is >= 0 and <= 1 && !double.IsNaN(value);

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private string N(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw Owner.CreateException(HaruStatus.RealOutOfRange, "PDF numbers must be finite.");

        return PdfWriter.FormatNumber(value);
    }

    private readonly record struct PdfGraphicsState(
        PdfTransMatrix TransMatrix,
        double LineWidth,
        PdfLineCap LineCap,
        PdfLineJoin LineJoin,
        double MiterLimit,
        PdfDashMode DashMode,
        double Flatness,
        double CharSpace,
        double WordSpace,
        double HorizontalScaling,
        double TextLeading,
        PdfTextRenderingMode TextRenderingMode,
        double TextRise,
        PdfColorSpace FillColorSpace,
        PdfColorSpace StrokeColorSpace,
        PdfRgbColor RgbFill,
        PdfRgbColor RgbStroke,
        PdfCmykColor CmykFill,
        PdfCmykColor CmykStroke,
        double GrayFill,
        double GrayStroke,
        PdfFont? Font,
        double FontSize,
        PdfWritingMode WritingMode,
        uint Depth)
    {
        public static readonly PdfGraphicsState Default = new(
            PdfTransMatrix.Identity,
            1,
            PdfLineCap.ButtEnd,
            PdfLineJoin.MiterJoin,
            10,
            PdfDashMode.Solid,
            1,
            0,
            0,
            100,
            0,
            PdfTextRenderingMode.Fill,
            0,
            PdfColorSpace.DeviceGray,
            PdfColorSpace.DeviceGray,
            PdfRgbColor.Black,
            PdfRgbColor.Black,
            new PdfCmykColor(0, 0, 0, 0),
            new PdfCmykColor(0, 0, 0, 0),
            0,
            0,
            null,
            0,
            PdfWritingMode.Horizontal,
            1);
    }

    private sealed record PdfSlideShow(PdfTransitionStyle Style, double DisplayTime, double TransitionTime);
}
