namespace LibHaru;

public static class HaruVersion
{
    public const int Major = 2;
    public const int Minor = 4;
    public const int Bugfix = 6;
    public const string Text = "2.4.6-managed";
}

[Flags]
public enum CompressionMode : uint
{
    None = 0x00,
    Text = 0x01,
    Image = 0x02,
    Metadata = 0x04,
    All = 0x0F
}

[Flags]
public enum Permission : uint
{
    EnableRead = 0,
    EnablePrint = 4,
    EnableEditAll = 8,
    EnableCopy = 16,
    EnableEdit = 32
}

public enum PdfPageLayout : uint
{
    Single = 0,
    OneColumn,
    TwoColumnLeft,
    TwoColumnRight
}

public enum PdfPageMode : uint
{
    UseNone = 0,
    UseOutline,
    UseThumbs,
    FullScreen,
    UseAttachments
}

public enum PdfPageSize : uint
{
    Letter = 0,
    Legal,
    A3,
    A4,
    A5,
    B4,
    B5,
    Executive,
    US4x6,
    US4x8,
    US5x7,
    Comm10
}

public enum PdfPageDirection : uint
{
    Portrait = 0,
    Landscape
}

public enum PdfTransitionStyle : uint
{
    WipeRight = 0,
    WipeUp,
    WipeLeft,
    WipeDown,
    BarnDoorsHorizontalOut,
    BarnDoorsHorizontalIn,
    BarnDoorsVerticalOut,
    BarnDoorsVerticalIn,
    BoxOut,
    BoxIn,
    BlindsHorizontal,
    BlindsVertical,
    Dissolve,
    GlitterRight,
    GlitterDown,
    GlitterTopLeftToBottomRight,
    Replace
}

public enum PdfInfoType : uint
{
    CreationDate = 0,
    ModDate,
    Author,
    Creator,
    Producer,
    Title,
    Subject,
    Keywords,
    Trapped,
    GtsPdfx
}

public enum PdfTextRenderingMode : uint
{
    Fill = 0,
    Stroke,
    FillThenStroke,
    Invisible,
    FillClipping,
    StrokeClipping,
    FillStrokeClipping,
    Clipping
}

public enum PdfEncoderType : uint
{
    SingleByte = 0,
    DoubleByte,
    Uninitialized,
    Unknown
}

public enum PdfByteType : uint
{
    Single = 0,
    Lead,
    Trail,
    Unknown
}

public enum PdfWritingMode : uint
{
    Horizontal = 0,
    Vertical
}

public enum PdfEncryptMode : uint
{
    R2 = 2,
    R3 = 3
}

public enum PdfLineCap : uint
{
    ButtEnd = 0,
    RoundEnd,
    ProjectingSquareEnd
}

public enum PdfLineJoin : uint
{
    MiterJoin = 0,
    RoundJoin,
    BevelJoin
}

public enum PdfTextAlignment : uint
{
    Left = 0,
    Right,
    Center,
    Justify
}

public enum PdfAnnotType : uint
{
    TextNotes = 0,
    Link,
    Sound,
    FreeText,
    Stamp,
    Square,
    Circle,
    StrikeOut,
    Highlight,
    Underline,
    Ink,
    FileAttachment,
    Popup,
    ThreeD,
    Squiggly,
    Line,
    Projection,
    Widget
}

public enum PdfPageBoundary : uint
{
    MediaBox = 0,
    CropBox,
    BleedBox,
    TrimBox,
    ArtBox
}

public enum PdfColorSpace : uint
{
    DeviceGray = 0,
    DeviceRgb,
    DeviceCmyk,
    CalGray,
    CalRgb,
    Lab,
    IccBased,
    Separation,
    DeviceN,
    Indexed,
    Pattern,
    Eof
}

[Flags]
public enum PdfViewerPreference : uint
{
    None = 0,
    HideToolbar = 1,
    HideMenubar = 2,
    HideWindowUI = 4,
    FitWindow = 8,
    CenterWindow = 16,
    PrintScalingNone = 32
}

public enum PdfPageNumStyle : uint
{
    Decimal = 0,
    UpperRoman,
    LowerRoman,
    UpperLetters,
    LowerLetters
}

public enum PdfAnnotHighlightMode : uint
{
    NoHighlight = 0,
    InvertBox,
    InvertBorder,
    DownAppearance
}

public enum PdfAnnotIcon : uint
{
    Comment = 0,
    Key,
    Note,
    Help,
    NewParagraph,
    Paragraph,
    Insert
}

public enum PdfAnnotBorderStyle : uint
{
    Solid = 0,
    Dashed,
    Beveled,
    Inset,
    Underlined
}

public enum PdfAnnotIntent : uint
{
    FreeTextCallout = 0,
    FreeTextTypeWriter,
    LineArrow,
    LineDimension,
    PolygonCloud,
    PolyLineDimension,
    PolygonDimension,
    StampImage,
    StampSnapshot
}

public enum PdfAnnotLineEndingStyle : uint
{
    None = 0,
    Square,
    Circle,
    Diamond,
    OpenArrow,
    ClosedArrow,
    Butt,
    ReversedOpenArrow,
    ReversedClosedArrow,
    Slash
}

public enum PdfLineAnnotCapPosition : uint
{
    Inline = 0,
    Top
}

public enum PdfAnnotationAppearanceState : uint
{
    Normal = 0,
    Rollover,
    Down
}

public enum PdfBlendMode : uint
{
    Normal = 0,
    Multiply,
    Screen,
    Overlay,
    Darken,
    Lighten,
    ColorDodge,
    ColorBurn,
    HardLight,
    SoftLight,
    Difference,
    Exclusion
}

public enum PdfAFRelationship : uint
{
    Source = 0,
    Data,
    Alternative,
    Supplement,
    EncryptedPayload,
    FormData,
    Schema,
    Unspecified
}

public enum PdfPdfAType : int
{
    NonPdfA = -1,
    PdfA1A = 0,
    PdfA1B,
    PdfA2A,
    PdfA2B,
    PdfA2U,
    PdfA3A,
    PdfA3B,
    PdfA3U,
    PdfA4,
    PdfA4E,
    PdfA4F
}

public enum PdfShadingType : uint
{
    FunctionBased = 1,
    Axial = 2,
    Radial = 3,
    FreeFormTriangleMesh = 4
}

public enum PdfShadingFreeFormTriangleMeshEdgeFlag : byte
{
    NoConnection = 0,
    ConnectPrevious,
    ConnectPreviousSecond
}

[Flags]
public enum PdfGraphicsMode : ushort
{
    PageDescription = 0x0001,
    PathObject = 0x0002,
    TextObject = 0x0004,
    ClippingPath = 0x0008,
    Shading = 0x0010,
    InlineImage = 0x0020,
    ExternalObject = 0x0040
}

public readonly record struct PdfPoint(double X, double Y);

public readonly record struct PdfPoint3D(double X, double Y, double Z);

public readonly record struct PdfTransMatrix(double A, double B, double C, double D, double X, double Y)
{
    public static readonly PdfTransMatrix Identity = new(1, 0, 0, 1, 0, 0);
}

public readonly record struct Pdf3DMatrix(
    double A,
    double B,
    double C,
    double D,
    double E,
    double F,
    double G,
    double H,
    double I,
    double Tx,
    double Ty,
    double Tz)
{
    public static readonly Pdf3DMatrix Identity = new(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0);
}

public readonly record struct PdfRect(double Left, double Bottom, double Right, double Top);

public readonly record struct PdfRgbColor(double R, double G, double B)
{
    public static readonly PdfRgbColor Black = new(0, 0, 0);
}

public readonly record struct PdfCmykColor(double C, double M, double Y, double K)
{
    public static readonly PdfCmykColor Black = new(0, 0, 0, 1);
}

public readonly record struct PdfDashMode
{
    public PdfDashMode(IReadOnlyList<double>? pattern, double phase)
    {
        Pattern = (pattern ?? Array.Empty<double>()).ToArray();
        Phase = phase;
    }

    public static readonly PdfDashMode Solid = new(Array.Empty<double>(), 0);

    public IReadOnlyList<double> Pattern { get; }

    public uint Count => checked((uint)Pattern.Count);

    public double Phase { get; }
}

public readonly record struct PdfTextWidth(uint NumChars, uint NumWords, uint Width, uint NumSpace);
