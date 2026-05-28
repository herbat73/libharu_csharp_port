using System.Buffers.Binary;
using System.Text;
using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfJavaScript
{
    internal PdfJavaScript(PdfDocument owner, PdfIndirectObject scriptObject)
    {
        Owner = owner;
        ScriptObject = scriptObject;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject ScriptObject { get; }

    internal PdfDictionary CreateActionDictionary()
    {
        ValidateOrThrow();

        var action = new PdfDictionary();
        action.SetName("Type", "Action");
        action.SetName("S", "JavaScript");
        action.Set("JS", ScriptObject.Reference);
        return action;
    }

    internal void ValidateOrThrow()
    {
        if (ScriptObject.Value is not PdfStreamObject)
            throw Owner.CreateException(HaruStatus.InvalidObject, "JavaScript object must be a stream.");
    }
}

public sealed class PdfEmbeddedFile
{
    private readonly PdfDictionary _fileSpec;
    private readonly PdfDictionary _fileStreamDictionary;
    private readonly PdfDictionary _params;
    private string _name;

    internal PdfEmbeddedFile(PdfDocument owner, string name, PdfIndirectObject fileSpecObject, PdfIndirectObject fileStreamObject)
    {
        Owner = owner;
        _name = name;
        FileSpecObject = fileSpecObject;
        FileStreamObject = fileStreamObject;
        _fileSpec = (PdfDictionary)fileSpecObject.Value;
        _fileStreamDictionary = ((PdfStreamObject)fileStreamObject.Value).Dictionary;
        _params = new PdfDictionary();
        _fileStreamDictionary.Set("Params", _params);
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject FileSpecObject { get; }

    internal PdfIndirectObject FileStreamObject { get; }

    internal string Name => _name;

    internal bool HasAFRelationship { get; private set; }

    public void SetName(string name)
    {
        ValidateOrThrow();

        if (string.IsNullOrWhiteSpace(name))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Embedded file name cannot be empty.");

        _name = name;
        _fileSpec.Set("F", PdfString.FromText(name));
        _fileSpec.Set("UF", PdfString.FromText(name));
    }

    public void SetDescription(string description)
    {
        ValidateOrThrow();
        _fileSpec.Set("Desc", PdfString.FromText(description ?? string.Empty));
    }

    public void SetSubtype(string subtype)
    {
        ValidateOrThrow();

        if (string.IsNullOrWhiteSpace(subtype))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Embedded file subtype cannot be empty.");

        _fileStreamDictionary.SetName("Subtype", subtype);
    }

    public void SetAFRelationship(PdfAFRelationship relationship)
    {
        ValidateOrThrow();

        if (!Enum.IsDefined(relationship))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Unknown AFRelationship value.");

        _fileSpec.SetName("AFRelationship", relationship switch
        {
            PdfAFRelationship.Source => "Source",
            PdfAFRelationship.Data => "Data",
            PdfAFRelationship.Alternative => "Alternative",
            PdfAFRelationship.Supplement => "Supplement",
            PdfAFRelationship.EncryptedPayload => "EncryptedPayload",
            PdfAFRelationship.FormData => "FormData",
            PdfAFRelationship.Schema => "Schema",
            _ => "Unspecified"
        });
        HasAFRelationship = true;
    }

    public void SetSize(long size)
    {
        ValidateOrThrow();

        if (size < 0 || size > int.MaxValue)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Embedded file size is out of range.");

        _params.Set("Size", new PdfInteger((int)size));
    }

    public void SetCreationDate(DateTimeOffset value)
    {
        ValidateOrThrow();
        _params.Set("CreationDate", PdfString.FromText(PdfDocument.FormatPdfDate(value)));
    }

    public void SetLastModificationDate(DateTimeOffset value)
    {
        ValidateOrThrow();
        _params.Set("ModDate", PdfString.FromText(PdfDocument.FormatPdfDate(value)));
    }

    internal void ValidateOrThrow()
    {
        if (!ReferenceEquals(FileSpecObject.Value, _fileSpec))
            throw Owner.CreateException(HaruStatus.InvalidObject, "Embedded file filespec object is invalid.");

        if (FileStreamObject.Value is not PdfStreamObject stream || !ReferenceEquals(stream.Dictionary, _fileStreamDictionary))
            throw Owner.CreateException(HaruStatus.InvalidObject, "Embedded file stream object is invalid.");

        try
        {
            var type = _fileSpec.Get<PdfName>("Type");
            var ef = _fileSpec.Get<PdfDictionary>("EF");
            var fileStream = ef?.GetItem("F", PdfObjectClass.Dictionary);
            var streamType = _fileStreamDictionary.Get<PdfName>("Type");
            var parameters = _fileStreamDictionary.Get<PdfDictionary>("Params");

            if (type?.Value != "Filespec"
                || ef is null
                || !ReferenceEquals(fileStream, stream)
                || streamType?.Value != "EmbeddedFile"
                || !ReferenceEquals(parameters, _params))
            {
                throw Owner.CreateException(HaruStatus.InvalidObject, "Embedded file dictionary entries are invalid.");
            }
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidObject)
        {
            throw Owner.CreateException(HaruStatus.InvalidObject, "Embedded file dictionary entries are invalid.", ex.Status);
        }
    }
}

public sealed class PdfExtGState
{
    internal PdfExtGState(PdfDocument owner, string resourceName, PdfIndirectObject graphicsStateObject)
    {
        Owner = owner;
        ResourceName = resourceName;
        GraphicsStateObject = graphicsStateObject;
        Dictionary = (PdfDictionary)graphicsStateObject.Value;
    }

    internal PdfDocument Owner { get; }

    internal string ResourceName { get; }

    internal PdfIndirectObject GraphicsStateObject { get; }

    private PdfDictionary Dictionary { get; }

    public void SetAlphaStroke(double value) => SetUnit("CA", value);

    public void SetAlphaFill(double value) => SetUnit("ca", value);

    public void SetBlendMode(PdfBlendMode mode)
    {
        ValidateOrThrow();

        if (!Enum.IsDefined(mode))
            throw Owner.CreateException(HaruStatus.ExtGStateOutOfRange, "Blend mode is out of range.");

        Dictionary.SetName("BM", mode switch
        {
            PdfBlendMode.Normal => "Normal",
            PdfBlendMode.Multiply => "Multiply",
            PdfBlendMode.Screen => "Screen",
            PdfBlendMode.Overlay => "Overlay",
            PdfBlendMode.Darken => "Darken",
            PdfBlendMode.Lighten => "Lighten",
            PdfBlendMode.ColorDodge => "ColorDodge",
            PdfBlendMode.ColorBurn => "ColorBurn",
            PdfBlendMode.HardLight => "HardLight",
            PdfBlendMode.SoftLight => "SoftLight",
            PdfBlendMode.Difference => "Difference",
            _ => "Exclusion"
        });
    }

    public void SetStrokeAdjustment(bool value)
    {
        ValidateOrThrow();
        Dictionary.Set("SA", new PdfBoolean(value));
    }

    internal void ValidateOrThrow(bool writable = true)
    {
        if (GraphicsStateObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidExtGState, "Extended graphics state object must be a dictionary.");

        var isWritable = dictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.ExtGState);
        var isReadOnly = dictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.ExtGStateReadOnly);

        if (!isWritable && !isReadOnly)
            throw Owner.CreateException(HaruStatus.InvalidExtGState, "Extended graphics state object must be an ExtGState dictionary.");

        if (writable && isReadOnly)
            throw Owner.CreateException(HaruStatus.ExtGStateReadOnly, "Extended graphics state is read-only.");
    }

    private void SetUnit(string key, double value)
    {
        ValidateOrThrow();

        if (value is < 0 or > 1 || double.IsNaN(value) || double.IsInfinity(value))
            throw Owner.CreateException(HaruStatus.ExtGStateOutOfRange, "Extended graphics state alpha must be between 0 and 1.");

        Dictionary.Set(key, new PdfReal(value));
    }
}

public sealed class PdfXObject
{
    internal PdfXObject(PdfDocument owner, string resourceName, PdfIndirectObject xObjectObject, string subtype)
    {
        Owner = owner;
        ResourceName = resourceName;
        XObjectObject = xObjectObject;
        Subtype = subtype;
    }

    internal PdfDocument Owner { get; }

    internal string ResourceName { get; }

    internal PdfIndirectObject XObjectObject { get; }

    public string Subtype { get; }

    internal void ValidateOrThrow()
    {
        if (XObjectObject.Value is not PdfStreamObject stream)
            throw Owner.CreateException(HaruStatus.PageInvalidXObject, "XObject must be a stream.");

        if (!stream.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.XObject))
            throw Owner.CreateException(HaruStatus.PageInvalidXObject, "Object must be an XObject stream.");

        try
        {
            var type = stream.Dictionary.Get<PdfName>("Type");
            var subtype = stream.Dictionary.Get<PdfName>("Subtype");

            if (type?.Value != "XObject" || string.IsNullOrEmpty(subtype?.Value))
                throw Owner.CreateException(HaruStatus.PageInvalidXObject, "XObject Type/Subtype entries are invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.PageInvalidXObject)
        {
            throw Owner.CreateException(HaruStatus.PageInvalidXObject, "XObject Type/Subtype entries are invalid.", ex.Status);
        }
    }
}

public sealed class PdfShading
{
    private readonly List<byte> _data = [];
    private readonly double? _xMin;
    private readonly double? _xMax;
    private readonly double? _yMin;
    private readonly double? _yMax;
    private readonly PdfStreamObject? _meshStream;

    internal PdfShading(PdfDocument owner, string resourceName, PdfIndirectObject shadingObject, PdfShadingType type, double? xMin = null, double? xMax = null, double? yMin = null, double? yMax = null)
    {
        Owner = owner;
        ResourceName = resourceName;
        ShadingObject = shadingObject;
        Type = type;
        _meshStream = shadingObject.Value as PdfStreamObject;
        _xMin = xMin;
        _xMax = xMax;
        _yMin = yMin;
        _yMax = yMax;
    }

    internal PdfDocument Owner { get; }

    internal string ResourceName { get; }

    internal PdfIndirectObject ShadingObject { get; }

    public PdfShadingType Type { get; }

    public void AddVertexRGB(PdfShadingFreeFormTriangleMeshEdgeFlag edgeFlag, double x, double y, byte r, byte g, byte b)
    {
        ValidateOrThrow();

        if (Type != PdfShadingType.FreeFormTriangleMesh || _meshStream is null || _xMin is null || _xMax is null || _yMin is null || _yMax is null)
            throw Owner.CreateException(HaruStatus.InvalidShadingType, "Vertices can only be added to free-form triangle mesh shadings.");

        if (!Enum.IsDefined(edgeFlag))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Unknown shading edge flag.");

        _data.Add((byte)edgeFlag);
        WriteUInt32(_data, EncodeCoordinate(x, _xMin.Value, _xMax.Value));
        WriteUInt32(_data, EncodeCoordinate(y, _yMin.Value, _yMax.Value));
        _data.Add(r);
        _data.Add(g);
        _data.Add(b);
        _meshStream.SetData(_data.ToArray());
    }

    internal void ValidateOrThrow()
    {
        if (ShadingObject.Value is not PdfStreamObject and not PdfDictionary)
            throw Owner.CreateException(HaruStatus.InvalidShadingType, "Shading object must be a stream or dictionary.");

        if (!ShadingObject.Value.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Shading))
            throw Owner.CreateException(HaruStatus.InvalidShadingType, "Shading object must be a shading stream.");

        try
        {
            var dictionary = ShadingObject.Value is PdfStreamObject stream
                ? stream.Dictionary
                : (PdfDictionary)ShadingObject.Value;

            var shadingType = dictionary.Get<PdfInteger>("ShadingType");
            if (shadingType is null || shadingType.Value != (int)Type)
                throw Owner.CreateException(HaruStatus.InvalidShadingType, "ShadingType entry is invalid.");

            if (Type != PdfShadingType.FreeFormTriangleMesh)
                return;

            var decode = dictionary.Get<PdfArray>("Decode");
            if (decode is null || decode.Count < 10)
                throw Owner.CreateException(HaruStatus.InvalidShadingType, "Shading Decode array is invalid.");

            for (var i = 0; i < 10; i++)
                decode.GetItem<PdfReal>(i);
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidShadingType)
        {
            throw Owner.CreateException(HaruStatus.InvalidShadingType, "Shading dictionary entries are invalid.", ex.Status);
        }
    }

    private uint EncodeCoordinate(double value, double min, double max)
    {
        if (value < min || value > max || max <= min || double.IsNaN(value) || double.IsInfinity(value))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Shading vertex coordinate is outside the decode range.");

        return (uint)Math.Round((value - min) / (max - min) * uint.MaxValue);
    }

    private static void WriteUInt32(List<byte> bytes, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        bytes.AddRange(buffer.ToArray());
    }
}

public sealed class PdfU3D
{
    private readonly PdfStreamObject _stream;
    private readonly List<Pdf3DView> _views = [];

    internal PdfU3D(PdfDocument owner, PdfIndirectObject u3dObject)
    {
        Owner = owner;
        U3DObject = u3dObject;
        _stream = (PdfStreamObject)u3dObject.Value;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject U3DObject { get; }

    public void Add3DView(Pdf3DView view)
    {
        ValidateOrThrow();

        if (view is null || !ReferenceEquals(view.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D view does not belong to this document.");

        view.ValidateOrThrow();
        if (!_views.Contains(view))
            _views.Add(view);

        RewriteViews();
    }

    public void SetDefault3DView(string name)
    {
        ValidateOrThrow();

        if (string.IsNullOrWhiteSpace(name))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "Default 3D view name cannot be empty.");

        _stream.Dictionary.Set("DV", PdfString.FromText(name));
    }

    public void AddOnInstantiate(PdfJavaScript javaScript)
    {
        ValidateOrThrow();

        if (javaScript is null || !ReferenceEquals(javaScript.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D JavaScript action does not belong to this document.");

        _stream.Dictionary.Set("OnInstantiate", javaScript.CreateActionDictionary());
    }

    internal void ValidateOrThrow()
    {
        if (U3DObject.Value is not PdfStreamObject stream)
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "U3D object must be a stream.");

        try
        {
            var type = stream.Dictionary.Get<PdfName>("Type");
            var subtype = stream.Dictionary.Get<PdfName>("Subtype");

            if (type?.Value != "3D" || subtype?.Value is not ("U3D" or "PRC"))
                throw Owner.CreateException(HaruStatus.InvalidU3DData, "U3D stream Type/Subtype entries are invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidU3DData)
        {
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "U3D stream Type/Subtype entries are invalid.", ex.Status);
        }
    }

    private void RewriteViews()
    {
        if (_views.Count == 0)
        {
            _stream.Dictionary.Remove("VA");
            _stream.Dictionary.Remove("DV");
            return;
        }

        _stream.Dictionary.Set("VA", new PdfArray(_views.Select(static view => view.ViewObject.Reference)));

        if (_stream.Dictionary.GetItem("DV", PdfObjectClass.Any) is null)
            _stream.Dictionary.Set("DV", new PdfInteger(0));
    }
}

public sealed class Pdf3DView
{
    private static readonly HashSet<string> LightingSchemes = new(StringComparer.Ordinal)
    {
        "Artwork",
        "None",
        "White",
        "Day",
        "Night",
        "Hard",
        "Primary",
        "Blue",
        "Red",
        "Cube",
        "CAD",
        "Headlamp"
    };

    private readonly List<Pdf3DNode> _nodes = [];
    private readonly List<Pdf3DMeasure> _measures = [];

    internal Pdf3DView(PdfDocument owner, string name, PdfIndirectObject viewObject)
    {
        Owner = owner;
        ViewObject = viewObject;
        _dictionary = (PdfDictionary)viewObject.Value;
        _dictionary.SetName("Type", "3DView");
        _dictionary.Set("XN", PdfString.FromText(name));
        _dictionary.Set("IN", PdfString.FromText(name));
    }

    private PdfDictionary _dictionary;

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject ViewObject { get; }

    internal PdfDictionary Dictionary => _dictionary;

    public void SetLighting(string scheme)
    {
        ValidateOrThrow();

        if (string.IsNullOrWhiteSpace(scheme))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "3D lighting scheme cannot be empty.");

        if (Encoding.ASCII.GetByteCount(scheme) > 127)
            throw Owner.CreateException(HaruStatus.NameOutOfRange, "3D lighting scheme name is too long.");

        if (!LightingSchemes.Contains(scheme))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "Unknown 3D lighting scheme.");

        var lighting = new PdfDictionary();
        lighting.SetName("Type", "3DLightingScheme");
        lighting.SetName("Subtype", scheme);
        _dictionary.Set("LS", lighting);
    }

    public void SetBackgroundColor(double r, double g, double b)
    {
        ValidateOrThrow();

        ValidateUnit(r, nameof(r));
        ValidateUnit(g, nameof(g));
        ValidateUnit(b, nameof(b));

        var background = new PdfDictionary();
        background.SetName("Type", "3DBG");
        background.Set("C", new PdfArray([new PdfReal(r), new PdfReal(g), new PdfReal(b)]));
        _dictionary.Set("BG", background);
    }

    public void SetPerspectiveProjection(double fieldOfView)
    {
        ValidateOrThrow();

        if (fieldOfView is < 0 or > 180 || double.IsNaN(fieldOfView) || double.IsInfinity(fieldOfView))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "Perspective field of view must be between 0 and 180.");

        var projection = new PdfDictionary();
        projection.SetName("Subtype", "P");
        projection.SetName("PS", "Min");
        projection.Set("FOV", new PdfReal(fieldOfView));
        _dictionary.Set("P", projection);
    }

    public void SetOrthogonalProjection(double magnification)
    {
        ValidateOrThrow();

        if (magnification <= 0 || double.IsNaN(magnification) || double.IsInfinity(magnification))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "Orthogonal projection magnification must be positive.");

        var projection = new PdfDictionary();
        projection.SetName("Subtype", "O");
        projection.Set("OS", new PdfReal(magnification));
        _dictionary.Set("P", projection);
    }

    public void SetCamera(double centerX, double centerY, double centerZ, double cameraDirectionX, double cameraDirectionY, double cameraDirectionZ, double orbitRadius, double roll)
    {
        ValidateOrThrow();

        var viewX = -cameraDirectionX;
        var viewY = -cameraDirectionY;
        var viewZ = -cameraDirectionZ;

        if (viewX == 0 && viewY == 0 && viewZ == 0)
            viewY = 1;

        (viewX, viewY, viewZ) = Normalize(viewX, viewY, viewZ);

        var leftX = -1.0;
        var leftY = 0.0;
        var leftZ = 0.0;
        var upX = viewZ < 0 ? 0.0 : 0.0;
        var upY = viewZ < 0 ? 1.0 : -1.0;
        var upZ = 0.0;

        if (Math.Abs(viewX) + Math.Abs(viewY) != 0)
        {
            upX = -viewZ * viewX;
            upY = -viewZ * viewY;
            upZ = -viewZ * viewZ + 1.0;
            (upX, upY, upZ) = Normalize(upX, upY, upZ);
            leftX = viewZ * upY - viewY * upZ;
            leftY = viewX * upZ - viewZ * upX;
            leftZ = viewY * upX - viewX * upY;
            (leftX, leftY, leftZ) = Normalize(leftX, leftY, leftZ);
        }

        var sinRoll = Math.Sin(roll / 180.0 * Math.PI);
        var cosRoll = Math.Cos(roll / 180.0 * Math.PI);
        var rolledLeftX = leftX * cosRoll + upX * sinRoll;
        var rolledLeftY = leftY * cosRoll + upY * sinRoll;
        var rolledLeftZ = leftZ * cosRoll + upZ * sinRoll;
        var rolledUpX = upX * cosRoll + leftX * sinRoll;
        var rolledUpY = upY * cosRoll + leftY * sinRoll;
        var rolledUpZ = upZ * cosRoll + leftZ * sinRoll;

        orbitRadius = Math.Abs(orbitRadius);
        if (orbitRadius == 0)
            orbitRadius = double.Epsilon;

        var matrix = new Pdf3DMatrix(
            rolledLeftX,
            rolledLeftY,
            rolledLeftZ,
            rolledUpX,
            rolledUpY,
            rolledUpZ,
            viewX,
            viewY,
            viewZ,
            centerX - orbitRadius * viewX,
            centerY - orbitRadius * viewY,
            centerZ - orbitRadius * viewZ);

        SetCameraByMatrix(matrix, orbitRadius);
    }

    public void SetCameraByMatrix(Pdf3DMatrix matrix, double cameraOrbit)
    {
        ValidateOrThrow();

        if (cameraOrbit < 0 || double.IsNaN(cameraOrbit) || double.IsInfinity(cameraOrbit))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "Camera orbit must be non-negative.");

        _dictionary.SetName("MS", "M");
        _dictionary.Set("C2W", MatrixArray(matrix));
        _dictionary.Set("CO", new PdfReal(cameraOrbit));
    }

    public Pdf3DNode CreateNode(string name) => Owner.Create3DNode(name);

    public void AddNode(Pdf3DNode node)
    {
        ValidateOrThrow();

        if (node is null || !ReferenceEquals(node.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D node does not belong to this document.");

        node.ValidateOrThrow();
        if (!_nodes.Contains(node))
            _nodes.Add(node);

        _dictionary.Set("NA", new PdfArray(_nodes.Select(static node => node.NodeObject.Reference)));
    }

    public void AddMeasure(Pdf3DMeasure measure)
    {
        ValidateOrThrow();

        if (measure is null || !ReferenceEquals(measure.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D measure does not belong to this document.");

        measure.ValidateOrThrow();
        if (!_measures.Contains(measure))
            _measures.Add(measure);

        _dictionary.Set("MA", new PdfArray(_measures.Select(static measure => measure.MeasureObject.Reference)));
    }

    public void SetCrossSectionOn(PdfPoint3D center, double roll, double pitch, double opacity, bool showIntersection)
    {
        ValidateOrThrow();

        ValidateUnit(opacity, nameof(opacity));

        var crossSection = new PdfDictionary();
        crossSection.SetName("Type", "3DCrossSection");
        crossSection.Set("C", Point3DArray(center));
        crossSection.Set("O", new PdfArray([PdfNull.New(), new PdfReal(roll), new PdfReal(pitch)]));
        crossSection.Set("PO", new PdfReal(opacity));
        crossSection.Set("IV", new PdfBoolean(showIntersection));
        crossSection.Set("IC", new PdfArray([new PdfName("DeviceRGB"), new PdfReal(1), new PdfReal(0), new PdfReal(0)]));
        _dictionary.Set("SA", new PdfArray([crossSection]));
    }

    public void SetCrossSectionOff()
    {
        ValidateOrThrow();
        _dictionary.Set("SA", new PdfArray());
    }

    internal void ValidateOrThrow()
    {
        if (ViewObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D view object must be a dictionary.");

        try
        {
            var type = dictionary.Get<PdfName>("Type");
            if (type?.Value != "3DView")
                throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D view Type entry is invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidU3DData)
        {
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D view Type entry is invalid.", ex.Status);
        }
    }

    private static (double X, double Y, double Z) Normalize(double x, double y, double z)
    {
        var length = Math.Sqrt(x * x + y * y + z * z);
        return length == 0 ? (x, y, z) : (x / length, y / length, z / length);
    }

    private static PdfArray MatrixArray(Pdf3DMatrix matrix) =>
        new([
            new PdfReal(matrix.A),
            new PdfReal(matrix.B),
            new PdfReal(matrix.C),
            new PdfReal(matrix.D),
            new PdfReal(matrix.E),
            new PdfReal(matrix.F),
            new PdfReal(matrix.G),
            new PdfReal(matrix.H),
            new PdfReal(matrix.I),
            new PdfReal(matrix.Tx),
            new PdfReal(matrix.Ty),
            new PdfReal(matrix.Tz)
        ]);

    private static PdfArray Point3DArray(PdfPoint3D point) =>
        new([new PdfReal(point.X), new PdfReal(point.Y), new PdfReal(point.Z)]);

    private void ValidateUnit(double value, string name)
    {
        if (value is < 0 or > 1 || double.IsNaN(value) || double.IsInfinity(value))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, $"{name} must be between 0 and 1.");
    }
}

public sealed class Pdf3DNode
{
    internal Pdf3DNode(PdfDocument owner, PdfIndirectObject nodeObject, string name)
    {
        Owner = owner;
        NodeObject = nodeObject;
        Dictionary = (PdfDictionary)nodeObject.Value;
        Dictionary.SetName("Type", "3DNode");
        Dictionary.Set("N", PdfString.FromText(name));
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject NodeObject { get; }

    internal PdfDictionary Dictionary { get; }

    public void SetOpacity(double opacity)
    {
        ValidateOrThrow();

        if (opacity is < 0 or > 1 || double.IsNaN(opacity) || double.IsInfinity(opacity))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D node opacity must be between 0 and 1.");

        Dictionary.Set("O", new PdfReal(opacity));
    }

    public void SetVisibility(bool visible)
    {
        ValidateOrThrow();
        Dictionary.Set("V", new PdfBoolean(visible));
    }

    public void SetMatrix(Pdf3DMatrix matrix)
    {
        ValidateOrThrow();

        Dictionary.Set("M", new PdfArray([
            new PdfReal(matrix.A),
            new PdfReal(matrix.B),
            new PdfReal(matrix.C),
            new PdfReal(matrix.D),
            new PdfReal(matrix.E),
            new PdfReal(matrix.F),
            new PdfReal(matrix.G),
            new PdfReal(matrix.H),
            new PdfReal(matrix.I),
            new PdfReal(matrix.Tx),
            new PdfReal(matrix.Ty),
            new PdfReal(matrix.Tz)
        ]));
    }

    internal void ValidateOrThrow()
    {
        if (NodeObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D node object must be a dictionary.");

        var type = dictionary.Get<PdfName>("Type");
        if (type?.Value != "3DNode")
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D node Type entry is invalid.");
    }
}

public sealed class Pdf3DMeasure
{
    internal Pdf3DMeasure(PdfDocument owner, PdfIndirectObject measureObject)
    {
        Owner = owner;
        MeasureObject = measureObject;
        Dictionary = (PdfDictionary)measureObject.Value;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject MeasureObject { get; }

    internal PdfDictionary Dictionary { get; }

    public void SetColor(PdfRgbColor color)
    {
        ValidateOrThrow();
        Dictionary.Set("C", new PdfArray([new PdfName("DeviceRGB"), new PdfReal(color.R), new PdfReal(color.G), new PdfReal(color.B)]));
    }

    public void SetTextSize(double textSize)
    {
        ValidateOrThrow();

        if (textSize <= 0 || double.IsNaN(textSize) || double.IsInfinity(textSize))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D measure text size must be positive.");

        Dictionary.Set("TS", new PdfReal(textSize));
    }

    public void SetName(string name)
    {
        ValidateOrThrow();
        Dictionary.Set("TRL", PdfString.FromText(name ?? string.Empty));
    }

    public void SetTextBoxSize(int x, int y)
    {
        ValidateOrThrow();
        Dictionary.Set("TB", new PdfArray([new PdfInteger(x), new PdfInteger(y)]));
    }

    public void SetText(string text)
    {
        ValidateOrThrow();
        Dictionary.Set("UT", PdfString.FromText(text ?? string.Empty));
    }

    public void SetProjectionAnnotation(PdfAnnotation projectionAnnotation)
    {
        ValidateOrThrow();

        if (projectionAnnotation is null || !ReferenceEquals(projectionAnnotation.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidAnnotation, "Projection annotation does not belong to this document.");

        Dictionary.Set("S", projectionAnnotation.AnnotationObject.Reference);
    }

    internal void ValidateOrThrow()
    {
        if (MeasureObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D measure object must be a dictionary.");

        var type = dictionary.Get<PdfName>("Type");
        if (type?.Value != "3DMeasure")
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D measure Type entry is invalid.");
    }
}

public sealed class PdfOutputIntent
{
    internal PdfOutputIntent(PdfDocument owner, PdfIndirectObject intentObject)
    {
        Owner = owner;
        IntentObject = intentObject;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject IntentObject { get; }

    internal void ValidateOrThrow()
    {
        if (IntentObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidObject, "Output intent object must be a dictionary.");

        try
        {
            var type = dictionary.Get<PdfName>("Type");
            var subtype = dictionary.Get<PdfName>("S");
            var profile = dictionary.GetItem("DestOutputProfile", PdfObjectClass.Dictionary);

            if (type?.Value != "OutputIntent" || subtype?.Value != "GTS_PDFA1" || profile is not PdfStreamObject)
                throw Owner.CreateException(HaruStatus.InvalidObject, "Output intent dictionary entries are invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidObject)
        {
            throw Owner.CreateException(HaruStatus.InvalidObject, "Output intent dictionary entries are invalid.", ex.Status);
        }
    }
}

public sealed class PdfFontDef
{
    internal PdfFontDef(PdfDocument owner, PdfFontProgram program)
    {
        Owner = owner;
        Program = program;
    }

    internal PdfDocument Owner { get; }

    internal PdfFontProgram Program { get; }

    public string BaseFont => Program.BaseFont;

    public PdfRect BBox => Program.Descriptor.FontBBox;

    public int Ascent => Program.Descriptor.Ascent;

    public int Descent => Program.Descriptor.Descent;
}

public sealed class PdfContentStream
{
    internal PdfContentStream(PdfDocument owner, PdfIndirectObject streamObject, MemoryStream buffer)
    {
        Owner = owner;
        StreamObject = streamObject;
        Buffer = buffer;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject StreamObject { get; }

    internal MemoryStream Buffer { get; }

    internal void ValidateOrThrow()
    {
        if (StreamObject.Value is not PdfStreamObject)
            throw Owner.CreateException(HaruStatus.InvalidStream, "Content stream object must be a stream.");
    }
}

public sealed class PdfExData
{
    internal PdfExData(PdfDocument owner, PdfIndirectObject exDataObject)
    {
        Owner = owner;
        ExDataObject = exDataObject;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject ExDataObject { get; }

    public void Set3DMeasurement(Pdf3DMeasure measure)
    {
        ValidateOrThrow();

        if (measure is null || !ReferenceEquals(measure.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidU3DData, "3D measure does not belong to this document.");

        measure.ValidateOrThrow();
        var dictionary = (PdfDictionary)ExDataObject.Value;

        dictionary.Set("M3DREF", measure.MeasureObject.Reference);
    }

    internal void ValidateOrThrow()
    {
        if (ExDataObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidObject, "External data object must be a dictionary.");

        try
        {
            var type = dictionary.Get<PdfName>("Type");
            var subtype = dictionary.Get<PdfName>("Subtype");

            if (type?.Value != "ExData" || subtype?.Value != "3DM")
                throw Owner.CreateException(HaruStatus.InvalidObject, "External data Type/Subtype entries are invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidObject)
        {
            throw Owner.CreateException(HaruStatus.InvalidObject, "External data Type/Subtype entries are invalid.", ex.Status);
        }
    }
}

public sealed class PdfIccProfile
{
    internal PdfIccProfile(PdfDocument owner, PdfIndirectObject profileObject, int componentCount)
    {
        Owner = owner;
        ProfileObject = profileObject;
        ComponentCount = componentCount;
    }

    internal PdfDocument Owner { get; }

    internal PdfIndirectObject ProfileObject { get; }

    public int ComponentCount { get; }

    internal void ValidateOrThrow()
    {
        if (ProfileObject.Value is not PdfStreamObject stream)
            throw Owner.CreateException(HaruStatus.InvalidObject, "ICC profile must be a stream.");

        try
        {
            var components = stream.Dictionary.Get<PdfInteger>("N");
            if (components is null || components.Value != ComponentCount)
                throw Owner.CreateException(HaruStatus.InvalidIccComponentNum, "ICC profile component count is invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidIccComponentNum)
        {
            throw Owner.CreateException(HaruStatus.InvalidIccComponentNum, "ICC profile component count is invalid.", ex.Status);
        }
    }
}

internal static class PdfFeatureHelpers
{
    internal static PdfArray RectArray(PdfRect rect)
    {
        var bottom = Math.Min(rect.Bottom, rect.Top);
        var top = Math.Max(rect.Bottom, rect.Top);
        return new PdfArray([new PdfReal(rect.Left), new PdfReal(bottom), new PdfReal(rect.Right), new PdfReal(top)]);
    }

    internal static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);
}
