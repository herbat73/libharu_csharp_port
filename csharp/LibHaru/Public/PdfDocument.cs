using LibHaru.Internal;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LibHaru;

public sealed class PdfDocument : IDisposable
{
    private static readonly HashSet<string> PdfAProhibitedActionTypes = new(StringComparer.Ordinal)
    {
        "JavaScript",
        "Launch",
        "Sound",
        "Movie",
        "ResetForm",
        "ImportData",
        "Rendition",
        "SubmitForm"
    };

    private static readonly HashSet<string> PdfAProhibitedMediaAnnotationSubtypes = new(StringComparer.Ordinal)
    {
        "Movie",
        "RichMedia",
        "Screen",
        "Sound"
    };

    private static readonly string[] PdfAdditionalActionKeys =
    [
        "E",
        "X",
        "D",
        "U",
        "Fo",
        "Bl",
        "PO",
        "PC",
        "PV",
        "PI",
        "O",
        "C",
        "K",
        "F",
        "V"
    ];

    private readonly List<PdfIndirectObject> _objects = [];
    private readonly List<PdfPage> _pages = [];
    private readonly List<PdfImage> _images = [];
    private readonly List<PdfIndirectObject> _fontFileObjects = [];
    private readonly List<PdfEmbeddedFile> _embeddedFiles = [];
    private readonly List<PdfOutputIntent> _outputIntents = [];
    private readonly List<PdfOutline> _rootOutlines = [];
    private readonly List<string> _pdfAXmpExtensions = [];
    private readonly Dictionary<string, PdfFont> _fonts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PdfFontProgram> _fontPrograms = new(StringComparer.Ordinal);
    private readonly List<PdfCompositeFontBinding> _compositeFontBindings = [];
    private readonly SortedDictionary<string, PdfDestination> _namedDestinations = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, PdfJavaScript> _namedJavaScripts = new(StringComparer.Ordinal);
    private readonly Dictionary<PdfInfoType, string> _infoValues = new();
    private readonly Dictionary<string, PdfEncoder> _encoders = new(StringComparer.Ordinal);
    private readonly PdfDictionary _catalogDictionary;
    private readonly PdfDictionary _pagesDictionary;
    private readonly PdfDictionary _infoDictionary;
    private readonly PdfIndirectObject _catalogObject;
    private readonly PdfIndirectObject _pagesObject;
    private readonly PdfIndirectObject _infoObject;
    private PdfEncryption? _encryption;
    private PdfIndirectObject? _encryptionObject;
    private PdfIndirectObject? _metadataObject;
    private PdfIndirectObject? _outlineRootObject;
    private byte[]? _fileId;
    private string? _metadataXml;
    private byte[]? _lastSavedStream;
    private int _streamPosition;
    private string _pdfVersion = "1.4";
    private CompressionMode _compressionMode = CompressionMode.None;
    private PdfViewerPreference _viewerPreference = PdfViewerPreference.None;
    private PdfPdfAType _pdfAType = PdfPdfAType.NonPdfA;
    private int _extGStateCount;
    private int _shadingCount;
    private int _xObjectCount;
    private uint _pagePerPages;
    private bool _hasDoc = true;
    private readonly List<(int PageNumber, PdfPageNumStyle Style, int FirstPage, string Prefix)> _pageLabels = [];

    public PdfDocument(HaruErrorHandler? errorHandler = null, object? userData = null)
    {
        Error = new HaruError(errorHandler, userData);

        _pagesDictionary = new PdfDictionary { Subclass = PdfObjectClass.Pages };
        _pagesDictionary.SetName("Type", "Pages");
        _pagesDictionary.Set("Kids", new PdfArray());
        _pagesDictionary.Set("Count", new PdfInteger(0));
        _pagesObject = AddObject(_pagesDictionary);

        _catalogDictionary = new PdfDictionary { Subclass = PdfObjectClass.Catalog };
        _catalogDictionary.SetName("Type", "Catalog");
        _catalogDictionary.Set("Pages", _pagesObject.Reference);
        _catalogObject = AddObject(_catalogDictionary);

        _infoDictionary = new PdfDictionary();
        var producer = $"Haru Free PDF Library {HaruVersion.Text}";
        var creationDate = PdfDate(DateTimeOffset.Now);
        _infoValues[PdfInfoType.Producer] = producer;
        _infoValues[PdfInfoType.CreationDate] = creationDate;
        _infoDictionary.Set("Producer", PdfString.FromText(producer));
        _infoDictionary.Set("CreationDate", PdfString.FromText(creationDate));
        _infoObject = AddObject(_infoDictionary);
    }

    public static PdfDocument New(HaruErrorHandler? errorHandler = null, object? userData = null) => new(errorHandler, userData);

    public IReadOnlyList<PdfPage> Pages => _pages;

    public PdfPage? CurrentPage { get; private set; }

    public PdfEncoder? CurrentEncoder { get; private set; }

    public uint PagePerPages => _pagePerPages;

    public CompressionMode CompressionMode => _compressionMode;

    public HaruError Error { get; }

    public PdfPageLayout PageLayout { get; private set; } = PdfPageLayout.Single;

    public PdfPageMode PageMode { get; private set; } = PdfPageMode.UseNone;

    public bool IsEncrypted => _encryption is not null;

    public PdfViewerPreference ViewerPreference => _viewerPreference;

    public PdfPdfAType PdfAType => _pdfAType;

    public uint GetError() => Error.ErrorNo;

    public uint GetErrorDetail() => Error.DetailNo;

    public uint CheckError() => Error.CheckError();

    public void ResetError() => Error.Reset();

    public bool HasDoc()
    {
        if (!_hasDoc || Error.ErrorNo != HaruStatus.NoError)
        {
            Error.RaiseError(HaruStatus.InvalidDocument);
            return false;
        }

        return true;
    }

    public void NewDoc() => ResetDocumentState(resetCompression: false, hasDoc: true);

    public void FreeDoc() => ResetDocumentState(resetCompression: false, hasDoc: false);

    public void FreeDocAll() => ResetDocumentState(resetCompression: true, hasDoc: false);

    public void SetPagesConfiguration(uint pagePerPages)
    {
        EnsureHasDoc();

        if (CurrentPage is not null)
            Throw(HaruStatus.InvalidDocumentState, "Pages configuration must be set before adding pages.");

        if (pagePerPages > PdfObjectLimits.MaxArrayItems)
            Throw(HaruStatus.InvalidParameter, "Pages configuration exceeds the maximum array size.");

        _pagePerPages = pagePerPages;
    }

    public void SetErrorHandler(HaruErrorHandler? errorHandler, object? userData = null) =>
        Error.SetHandler(errorHandler, userData);

    public PdfPage AddPage()
    {
        EnsureHasDoc();

        var contents = AddObject(new PdfStreamObject([]) { Subclass = PdfObjectClass.ContentStream });
        var pageObject = AddObject(new PdfDictionary { Subclass = PdfObjectClass.Page });
        var page = new PdfPage(this, pageObject, contents);

        _pages.Add(page);
        CurrentPage = page;
        return page;
    }

    public PdfPage InsertPage(PdfPage beforePage)
    {
        EnsureHasDoc();

        if (beforePage is null)
            Throw(HaruStatus.InvalidPage, "The target page cannot be null.");

        var index = _pages.IndexOf(beforePage);
        if (index < 0)
            Throw(HaruStatus.InvalidPage, "The target page does not belong to this document.");

        var contents = AddObject(new PdfStreamObject([]) { Subclass = PdfObjectClass.ContentStream });
        var pageObject = AddObject(new PdfDictionary { Subclass = PdfObjectClass.Page });
        var page = new PdfPage(this, pageObject, contents);

        _pages.Insert(index, page);
        CurrentPage = page;
        return page;
    }

    public PdfPage GetPageByIndex(int index)
    {
        EnsureHasDoc();

        if (index < 0 || index >= _pages.Count)
            Throw(HaruStatus.InvalidPageIndex, "Page index is outside the document page list.");

        return _pages[index];
    }

    public PdfFont GetFont(string fontName, string? encoding = null)
    {
        EnsureHasDoc();

        if (string.IsNullOrWhiteSpace(fontName))
            Throw(HaruStatus.InvalidFontName, "Font name cannot be empty.");

        encoding ??= Base14Fonts.IsFontSpecific(fontName) ? "FontSpecific" : "StandardEncoding";

        PdfEncoding encodingModel;
        try
        {
            encodingModel = GetEncoder(encoding).EncodingModel;
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }

        var program = ResolveFontProgram(fontName);

        var key = $"{program.BaseFont}|{encodingModel.Name}";
        if (_fonts.TryGetValue(key, out var font))
            return font;

        var dictionary = CreateFontDictionary(program, encodingModel, out var compositeObjects);
        var fontObject = AddObject(dictionary);
        var compositeGlyphMap = compositeObjects is not null ? new PdfCompositeGlyphMap() : null;
        font = new PdfFont(this, program, encodingModel, $"F{_fonts.Count + 1}", fontObject, compositeGlyphMap);
        if (compositeObjects is not null && compositeGlyphMap is not null)
            _compositeFontBindings.Add(new PdfCompositeFontBinding(font, compositeGlyphMap, compositeObjects));

        _fonts.Add(key, font);
        return font;
    }

    public PdfEncoder GetEncoder(string encodingName)
    {
        EnsureHasDoc();

        PdfEncoding encodingModel;
        try
        {
            encodingModel = PdfEncoding.Get(encodingName, Error);
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }

        if (_encoders.TryGetValue(encodingModel.Name, out var encoder))
            return encoder;

        encoder = new PdfEncoder(this, encodingModel);
        _encoders.Add(encodingModel.Name, encoder);
        return encoder;
    }

    public void SetCurrentEncoder(string encodingName)
    {
        CurrentEncoder = GetEncoder(encodingName);
    }

    public string LoadType1FontFromFile(string afmFileName, string? dataFileName)
    {
        if (string.IsNullOrWhiteSpace(afmFileName))
            Throw(HaruStatus.MissingFileNameEntry, "AFM file name cannot be empty.");

        try
        {
            var program = Type1FontLoader.Load(afmFileName, dataFileName);
            RegisterFontProgram(program);
            return program.BaseFont;
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public string LoadTTFontFromFile(string fileName, bool embedding)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "TrueType file name cannot be empty.");

        try
        {
            return LoadTTFontFromMemory(File.ReadAllBytes(fileName), embedding);
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfFontDef GetTTFontDefFromFile(string fileName, bool embedding)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "TrueType file name cannot be empty.");

        try
        {
            var program = TrueTypeFontLoader.Load(File.ReadAllBytes(fileName), embedding);
            if (!_fontPrograms.TryGetValue(program.BaseFont, out var registered))
            {
                RegisterFontProgram(program);
                registered = program;
            }

            return new PdfFontDef(this, registered);
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public string LoadTTFontFromFile2(string fileName, int index, bool embedding)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "TrueType collection file name cannot be empty.");

        if (index < 0)
            Throw(HaruStatus.InvalidTtcIndex, "TrueType collection index cannot be negative.");

        try
        {
            var program = TrueTypeFontLoader.Load(File.ReadAllBytes(fileName), embedding, index);
            RegisterFontProgram(program);
            return program.BaseFont;
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public string LoadTTFontFromMemory(byte[] buffer, bool embedding)
    {
        if (buffer is null)
            Throw(HaruStatus.InvalidParameter, "TrueType font buffer cannot be null.");

        try
        {
            var program = TrueTypeFontLoader.Load(buffer, embedding);
            RegisterFontProgram(program);
            return program.BaseFont;
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
    }

    public void SetCompressionMode(CompressionMode mode)
    {
        if ((mode & ~CompressionMode.All) != 0)
            Throw(HaruStatus.InvalidCompressionMode, "Compression mode contains unsupported flags.");

        _compressionMode = mode;
    }

    public void SetPageLayout(PdfPageLayout layout)
    {
        if (!Enum.IsDefined(layout))
            Throw(HaruStatus.PageLayoutOutOfRange, "Page layout is out of range.");

        PageLayout = layout;
    }

    public void SetPageMode(PdfPageMode mode)
    {
        if (!Enum.IsDefined(mode))
            Throw(HaruStatus.PageModeOutOfRange, "Page mode is out of range.");

        PageMode = mode;
    }

    public void SetViewerPreference(PdfViewerPreference preference)
    {
        if ((preference & ~(PdfViewerPreference.HideToolbar
            | PdfViewerPreference.HideMenubar
            | PdfViewerPreference.HideWindowUI
            | PdfViewerPreference.FitWindow
            | PdfViewerPreference.CenterWindow
            | PdfViewerPreference.PrintScalingNone)) != 0)
        {
            Throw(HaruStatus.InvalidParameter, "Viewer preference contains unsupported flags.");
        }

        _viewerPreference = preference;
    }

    public void SetOpenAction(PdfDestination? destination)
    {
        if (destination is null)
        {
            _catalogDictionary.Remove("OpenAction");
            return;
        }

        if (!ReferenceEquals(destination.Owner, this))
            Throw(HaruStatus.InvalidDestination, "Open action destination does not belong to this document.");

        destination.ValidateOrThrow();
        _catalogDictionary.Set("OpenAction", destination.DestinationObject.Reference);
    }

    public void SetOpenAction(PdfJavaScript? javaScript)
    {
        if (javaScript is null)
        {
            _catalogDictionary.Remove("OpenAction");
            return;
        }

        if (!ReferenceEquals(javaScript.Owner, this))
            Throw(HaruStatus.InvalidObject, "Open action JavaScript does not belong to this document.");

        _catalogDictionary.Set("OpenAction", javaScript.CreateActionDictionary());
    }

    public void AddPageLabel(int pageNumber, PdfPageNumStyle style, int firstPage = 1, string prefix = "")
    {
        if (pageNumber < 0)
            Throw(HaruStatus.InvalidPageIndex, "Page label index cannot be negative.");

        if (!Enum.IsDefined(style))
            Throw(HaruStatus.PageNumStyleOutOfRange, "Page number style is out of range.");

        if (firstPage <= 0)
            Throw(HaruStatus.InvalidParameter, "First page number must be positive.");

        _pageLabels.Add((pageNumber, style, firstPage, prefix ?? string.Empty));
    }

    public PdfOutline CreateOutline(PdfOutline? parent, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            Throw(HaruStatus.InvalidOutline, "Outline title cannot be empty.");

        if (parent is not null && !ReferenceEquals(parent.Owner, this))
            Throw(HaruStatus.InvalidOutline, "Parent outline does not belong to this document.");

        var root = EnsureOutlineRoot();
        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Outline };
        var obj = AddObject(dictionary);
        var outline = new PdfOutline(this, parent, title, obj);

        if (parent is null)
            RootOutlines(root).Add(outline);
        else
            parent.Children.Add(outline);

        return outline;
    }

    public void AddNamedDestination(string name, PdfDestination destination)
    {
        if (string.IsNullOrWhiteSpace(name))
            Throw(HaruStatus.NameInvalidValue, "Named destination cannot have an empty name.");

        if (destination is null || !ReferenceEquals(destination.Owner, this))
            Throw(HaruStatus.InvalidDestination, "Named destination does not belong to this document.");

        destination.ValidateOrThrow();
        _namedDestinations[name] = destination;
    }

    public PdfJavaScript CreateJavaScript(string code)
    {
        if (code is null)
            Throw(HaruStatus.InvalidParameter, "JavaScript code cannot be null.");

        var stream = new PdfStreamObject(Encoding.UTF8.GetBytes(code))
        {
            Kind = PdfStreamKind.JavaScript,
            CompressionMode = _compressionMode,
            Subclass = PdfObjectClass.JavaScript
        };
        var obj = AddObject(stream);
        return new PdfJavaScript(this, obj);
    }

    public PdfJavaScript LoadJavaScriptFromFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "JavaScript file name cannot be empty.");

        try
        {
            return CreateJavaScript(File.ReadAllText(fileName, Encoding.UTF8));
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public void AddNamedJavaScript(string name, PdfJavaScript javaScript)
    {
        if (string.IsNullOrWhiteSpace(name))
            Throw(HaruStatus.NameInvalidValue, "Named JavaScript cannot have an empty name.");

        if (javaScript is null || !ReferenceEquals(javaScript.Owner, this))
            Throw(HaruStatus.InvalidObject, "Named JavaScript does not belong to this document.");

        _namedJavaScripts[name] = javaScript;
    }

    public PdfEmbeddedFile AttachFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "Embedded file name cannot be empty.");

        try
        {
            var data = File.ReadAllBytes(fileName);
            var name = Path.GetFileName(fileName);
            var stream = new PdfStreamObject(data)
            {
                Kind = PdfStreamKind.EmbeddedFile,
                CompressionMode = _compressionMode,
                Subclass = PdfObjectClass.EmbeddedFile
            };
            stream.Dictionary.SetName("Type", "EmbeddedFile");
            var streamObject = AddObject(stream);

            var ef = new PdfDictionary();
            ef.Set("F", streamObject.Reference);

            var fileSpec = new PdfDictionary { Subclass = PdfObjectClass.FileSpec };
            fileSpec.SetName("Type", "Filespec");
            fileSpec.Set("F", PdfString.FromText(name));
            fileSpec.Set("UF", PdfString.FromText(name));
            fileSpec.Set("EF", ef);
            var fileSpecObject = AddObject(fileSpec);

            var embeddedFile = new PdfEmbeddedFile(this, name, fileSpecObject, streamObject);
            embeddedFile.SetSize(data.Length);
            _embeddedFiles.Add(embeddedFile);
            return embeddedFile;
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfExtGState CreateExtGState()
    {
        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.ExtGState };
        dictionary.SetName("Type", "ExtGState");
        var obj = AddObject(dictionary);
        return new PdfExtGState(this, $"E{++_extGStateCount}", obj);
    }

    public PdfShading CreateShading(PdfShadingType type, PdfColorSpace colorSpace, double xMin, double xMax, double yMin, double yMax)
    {
        if (type != PdfShadingType.FreeFormTriangleMesh)
            Throw(HaruStatus.InvalidShadingType, "Use the typed axial/radial shading helpers for non-mesh shading types.");

        if (colorSpace != PdfColorSpace.DeviceRgb)
            Throw(HaruStatus.InvalidColorSpace, "Only DeviceRGB shadings are implemented.");

        if (!IsFinite(xMin) || !IsFinite(xMax) || !IsFinite(yMin) || !IsFinite(yMax) || xMax <= xMin || yMax <= yMin)
            Throw(HaruStatus.InvalidParameter, "Shading decode bounds are invalid.");

        var stream = new PdfStreamObject([])
        {
            Kind = PdfStreamKind.Shading,
            CompressionMode = _compressionMode,
            Subclass = PdfObjectClass.Shading
        };
        stream.Dictionary.Set("ShadingType", new PdfInteger((int)type));
        stream.Dictionary.SetName("ColorSpace", "DeviceRGB");
        stream.Dictionary.Set("BitsPerCoordinate", new PdfInteger(32));
        stream.Dictionary.Set("BitsPerComponent", new PdfInteger(8));
        stream.Dictionary.Set("BitsPerFlag", new PdfInteger(8));
        stream.Dictionary.Set("Decode", new PdfArray([
            new PdfReal(xMin), new PdfReal(xMax),
            new PdfReal(yMin), new PdfReal(yMax),
            new PdfReal(0), new PdfReal(1),
            new PdfReal(0), new PdfReal(1),
            new PdfReal(0), new PdfReal(1)
        ]));

        var obj = AddObject(stream);
        return new PdfShading(this, $"Sh{_shadingCount++}", obj, type, xMin, xMax, yMin, yMax);
    }

    public PdfShading CreateAxialShading(PdfPoint startPoint, PdfPoint endPoint, PdfRgbColor startColor, PdfRgbColor endColor, bool extendStart = false, bool extendEnd = false)
    {
        ValidatePoint(startPoint, "Axial shading start point");
        ValidatePoint(endPoint, "Axial shading end point");
        ValidateRgbColor(startColor);
        ValidateRgbColor(endColor);

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Shading };
        dictionary.Set("ShadingType", new PdfInteger((int)PdfShadingType.Axial));
        dictionary.SetName("ColorSpace", "DeviceRGB");
        dictionary.Set("Coords", new PdfArray([
            new PdfReal(startPoint.X), new PdfReal(startPoint.Y),
            new PdfReal(endPoint.X), new PdfReal(endPoint.Y)
        ]));
        dictionary.Set("Function", CreateExponentialInterpolationFunction(startColor, endColor));
        dictionary.Set("Extend", new PdfArray([new PdfBoolean(extendStart), new PdfBoolean(extendEnd)]));

        var obj = AddObject(dictionary);
        return new PdfShading(this, $"Sh{_shadingCount++}", obj, PdfShadingType.Axial);
    }

    public PdfShading CreateRadialShading(PdfPoint startCenter, double startRadius, PdfPoint endCenter, double endRadius, PdfRgbColor startColor, PdfRgbColor endColor, bool extendStart = false, bool extendEnd = false)
    {
        ValidatePoint(startCenter, "Radial shading start center");
        ValidatePoint(endCenter, "Radial shading end center");
        ValidateNonNegative(startRadius, "Radial shading start radius");
        ValidateNonNegative(endRadius, "Radial shading end radius");
        ValidateRgbColor(startColor);
        ValidateRgbColor(endColor);

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Shading };
        dictionary.Set("ShadingType", new PdfInteger((int)PdfShadingType.Radial));
        dictionary.SetName("ColorSpace", "DeviceRGB");
        dictionary.Set("Coords", new PdfArray([
            new PdfReal(startCenter.X), new PdfReal(startCenter.Y), new PdfReal(startRadius),
            new PdfReal(endCenter.X), new PdfReal(endCenter.Y), new PdfReal(endRadius)
        ]));
        dictionary.Set("Function", CreateExponentialInterpolationFunction(startColor, endColor));
        dictionary.Set("Extend", new PdfArray([new PdfBoolean(extendStart), new PdfBoolean(extendEnd)]));

        var obj = AddObject(dictionary);
        return new PdfShading(this, $"Sh{_shadingCount++}", obj, PdfShadingType.Radial);
    }

    public PdfU3D LoadU3DFromFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "U3D file name cannot be empty.");

        try
        {
            return LoadU3DFromMem(File.ReadAllBytes(fileName));
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfU3D LoadU3DFromMem(byte[] data)
    {
        if (data is null)
            Throw(HaruStatus.InvalidU3DData, "U3D data cannot be null.");

        _pdfVersion = "1.7";
        var stream = new PdfStreamObject(data)
        {
            Kind = PdfStreamKind.U3D,
            CompressionMode = _compressionMode,
            Subclass = PdfObjectClass.U3D
        };
        stream.Dictionary.SetName("Type", "3D");
        stream.Dictionary.SetName("Subtype", "U3D");
        var obj = AddObject(stream);
        return new PdfU3D(this, obj);
    }

    public Pdf3DView Create3DView(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            Throw(HaruStatus.InvalidParameter, "3D view name cannot be empty.");

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.View3D };
        var obj = AddObject(dictionary);
        return new Pdf3DView(this, name, obj);
    }

    public Pdf3DNode Create3DNode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            Throw(HaruStatus.InvalidParameter, "3D node name cannot be empty.");

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Node3D };
        var obj = AddObject(dictionary);
        return new Pdf3DNode(this, obj, name);
    }

    public Pdf3DMeasure Create3DC3DMeasure(PdfPoint3D firstAnchorPoint, PdfPoint3D textAnchorPoint)
    {
        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Measure3D };
        dictionary.SetName("Type", "3DMeasure");
        dictionary.SetName("Subtype", "3DC");
        dictionary.Set("A1", Point3DArray(firstAnchorPoint));
        dictionary.Set("TP", Point3DArray(textAnchorPoint));
        return new Pdf3DMeasure(this, AddObject(dictionary));
    }

    public Pdf3DMeasure CreatePD33DMeasure(
        PdfPoint3D annotationPlaneNormal,
        PdfPoint3D firstAnchorPoint,
        PdfPoint3D secondAnchorPoint,
        PdfPoint3D leaderLinesDirection,
        PdfPoint3D measurementValuePoint,
        PdfPoint3D textYDirection,
        double value,
        string units)
    {
        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Measure3D };
        dictionary.SetName("Type", "3DMeasure");
        dictionary.SetName("Subtype", "PD3");
        dictionary.Set("AP", Point3DArray(annotationPlaneNormal));
        dictionary.Set("A1", Point3DArray(firstAnchorPoint));
        dictionary.Set("A2", Point3DArray(secondAnchorPoint));
        dictionary.Set("D1", Point3DArray(leaderLinesDirection));
        dictionary.Set("TP", Point3DArray(measurementValuePoint));
        dictionary.Set("TY", Point3DArray(textYDirection));
        dictionary.Set("V", new PdfReal(value));
        dictionary.Set("U", PdfString.FromText(units ?? string.Empty));
        return new Pdf3DMeasure(this, AddObject(dictionary));
    }

    public PdfOutputIntent AppendOutputIntent(string outputConditionIdentifier, byte[] iccProfile, string? info = null)
    {
        return AppendOutputIntent(outputConditionIdentifier, LoadIccProfileFromMem(iccProfile, 3), info);
    }

    public PdfOutputIntent AppendOutputIntent(string outputConditionIdentifier, PdfIccProfile iccProfile, string? info = null)
    {
        EnsureHasDoc();

        if (string.IsNullOrWhiteSpace(outputConditionIdentifier))
            Throw(HaruStatus.InvalidParameter, "Output condition identifier cannot be empty.");

        if (iccProfile is null || !ReferenceEquals(iccProfile.Owner, this))
            Throw(HaruStatus.InvalidObject, "ICC profile does not belong to this document.");

        iccProfile.ValidateOrThrow();
        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.OutputIntent };
        dictionary.SetName("Type", "OutputIntent");
        dictionary.SetName("S", "GTS_PDFA1");
        dictionary.Set("OutputConditionIdentifier", PdfString.FromText(outputConditionIdentifier));
        dictionary.Set("OutputCondition", PdfString.FromText(outputConditionIdentifier));
        dictionary.Set("Info", PdfString.FromText(info ?? outputConditionIdentifier));
        dictionary.Set("DestOutputProfile", iccProfile.ProfileObject.Reference);
        var intentObject = AddObject(dictionary);
        var intent = new PdfOutputIntent(this, intentObject);
        _outputIntents.Add(intent);
        return intent;
    }

    public PdfIccProfile LoadIccProfileFromFile(string fileName, int componentCount)
    {
        EnsureHasDoc();

        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "ICC profile file name cannot be empty.");

        try
        {
            return LoadIccProfileFromMem(File.ReadAllBytes(fileName), componentCount);
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfIccProfile LoadIccProfileFromMem(byte[] iccProfile, int componentCount)
    {
        EnsureHasDoc();

        if (iccProfile is null || iccProfile.Length == 0)
            Throw(HaruStatus.InvalidParameter, "ICC profile cannot be empty.");

        var alternate = componentCount switch
        {
            1 => "DeviceGray",
            3 => "DeviceRGB",
            4 => "DeviceCMYK",
            _ => throw CreateException(HaruStatus.InvalidIccComponentNum, "ICC profile component count must be 1, 3, or 4.")
        };

        var profileStream = new PdfStreamObject(iccProfile.ToArray())
        {
            Kind = PdfStreamKind.IccProfile,
            CompressionMode = _compressionMode,
            Subclass = PdfObjectClass.IccProfile
        };
        profileStream.Dictionary.Set("N", new PdfInteger(componentCount));
        profileStream.Dictionary.SetName("Alternate", alternate);
        return new PdfIccProfile(this, AddObject(profileStream), componentCount);
    }

    public PdfXObject CreateXObjectFromImage(PdfPage page, PdfRect rect, PdfImage image, bool zoom)
    {
        EnsureHasDoc();
        ValidatePagePeer(page);

        if (image is null || !ReferenceEquals(image.Owner, this))
            Throw(HaruStatus.PageInvalidXObject, "Image does not belong to this document.");

        image.ValidateOrThrow();
        var (left, bottom, right, top, width, height) = NormalizeXObjectRect(rect);
        var xobjects = new PdfDictionary();
        xobjects.Set("Im1", image.ImageObject.Reference);

        var content = zoom
            ? $"q\n{PdfWriter.FormatNumber(width)} 0 0 {PdfWriter.FormatNumber(height)} 0 0 cm\n/Im1 Do\nQ"
            : "q\n1 0 0 1 0 0 cm\n/Im1 Do\nQ";

        var stream = CreateFormXObjectStream(
            PdfFeatureHelpers.Utf8(content),
            new PdfArray([new PdfReal(left), new PdfReal(bottom), new PdfReal(right), new PdfReal(top)]),
            xobjects);

        return new PdfXObject(this, $"X{++_xObjectCount}", AddObject(stream), "Form");
    }

    public PdfXObject CreateXObjectAsWhiteRect(PdfPage page, PdfRect rect)
    {
        EnsureHasDoc();
        ValidatePagePeer(page);
        var (_, _, _, _, width, height) = NormalizeXObjectRect(rect);
        var content = $"1 g\n0 0 {PdfWriter.FormatNumber(width)} {PdfWriter.FormatNumber(height)} re\nf";
        var bbox = new PdfArray([new PdfInteger(0), new PdfInteger(0), new PdfReal(width), new PdfReal(height)]);
        var stream = CreateFormXObjectStream(PdfFeatureHelpers.Utf8(content), bbox, new PdfDictionary());
        return new PdfXObject(this, $"X{++_xObjectCount}", AddObject(stream), "Form");
    }

    public void SetPdfAConformance(PdfPdfAType pdfAType)
    {
        if (!Enum.IsDefined(pdfAType))
            Throw(HaruStatus.InvalidParameter, "PDF/A type is out of range.");

        _pdfAType = pdfAType;

        if (pdfAType != PdfPdfAType.NonPdfA && _metadataObject is null)
            SetXmpMetadata(CreatePdfAXmp(pdfAType));
    }

    public void AddPdfAXmpExtension(string xmpExtension)
    {
        if (string.IsNullOrWhiteSpace(xmpExtension))
            return;

        _pdfAXmpExtensions.Add(xmpExtension);

        if (_pdfAType != PdfPdfAType.NonPdfA)
            SetXmpMetadata(CreatePdfAXmp(_pdfAType));
    }

    public void ClearPdfAXmpExtensions()
    {
        _pdfAXmpExtensions.Clear();

        if (_pdfAType != PdfPdfAType.NonPdfA)
            SetXmpMetadata(CreatePdfAXmp(_pdfAType));
    }

    public void SetInfoAttr(PdfInfoType type, string value)
    {
        if (value is null)
            Throw(HaruStatus.InvalidParameter, "Info attribute value cannot be null.");

        var key = type switch
        {
            PdfInfoType.CreationDate => "CreationDate",
            PdfInfoType.ModDate => "ModDate",
            PdfInfoType.Author => "Author",
            PdfInfoType.Creator => "Creator",
            PdfInfoType.Producer => "Producer",
            PdfInfoType.Title => "Title",
            PdfInfoType.Subject => "Subject",
            PdfInfoType.Keywords => "Keywords",
            PdfInfoType.Trapped => "Trapped",
            PdfInfoType.GtsPdfx => "GTS_PDFX",
            _ => throw CreateException(HaruStatus.InvalidParameter, "Unknown info attribute.")
        };

        _infoValues[type] = value;
        _infoDictionary.Set(key, PdfString.FromText(value));
    }

    public string? GetInfoAttr(PdfInfoType type)
    {
        return _infoValues.TryGetValue(type, out var value) ? value : null;
    }

    public void SetInfoDateAttr(PdfInfoType type, DateTimeOffset value)
    {
        SetInfoAttr(type, PdfDate(value));
    }

    public void SetXmpMetadata(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            Throw(HaruStatus.InvalidStream, "Metadata XML cannot be empty.");

        _metadataXml = xml;
        var stream = new PdfStreamObject(Encoding.UTF8.GetBytes(xml))
        {
            Kind = PdfStreamKind.Metadata,
            CompressionMode = _compressionMode
        };
        stream.Dictionary.SetName("Type", "Metadata");
        stream.Dictionary.SetName("Subtype", "XML");

        if (_metadataObject is null)
        {
            _metadataObject = AddObject(stream);
            _catalogDictionary.Set("Metadata", _metadataObject.Reference);
        }
        else
        {
            _metadataObject.Value = stream;
        }
    }

    public PdfImage LoadRawImageFromMem(byte[] data, int width, int height, PdfColorSpace colorSpace, int bitsPerComponent = 8)
    {
        if (data is null)
            Throw(HaruStatus.InvalidImage, "Image data cannot be null.");

        ValidateImageDimensions(width, height);

        if (bitsPerComponent is not (1 or 2 or 4 or 8))
            Throw(HaruStatus.InvalidImage, "Bits per component must be 1, 2, 4, or 8.");

        var componentCount = ComponentCount(colorSpace);
        var expectedSize = CheckedImageByteCount(width, height, componentCount, bitsPerComponent);

        if (data.Length != expectedSize)
            Throw(HaruStatus.InvalidImage, $"Raw image data length was {data.Length}; expected {expectedSize} bytes.");

        if (colorSpace == PdfColorSpace.DeviceGray && bitsPerComponent == 1)
            return LoadRaw1BitImageFromMem(data, width, height, (width + 7) / 8, blackIs1: true, topIsFirst: true);

        var stream = CreateImageStream(data, width, height, colorSpace, bitsPerComponent);
        return RegisterImage(stream, width, height, bitsPerComponent, colorSpace);
    }

    public PdfImage LoadRawImageFromFile(string fileName, int width, int height, PdfColorSpace colorSpace)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "File name cannot be empty.");

        try
        {
            return LoadRawImageFromMem(File.ReadAllBytes(fileName), width, height, colorSpace, 8);
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfImage LoadRaw1BitImageFromMem(byte[] data, int width, int height, int lineWidth, bool blackIs1, bool topIsFirst)
    {
        if (data is null)
            Throw(HaruStatus.InvalidImage, "Image data cannot be null.");

        ValidateImageDimensions(width, height);

        var stride = (width + 7) / 8;
        if (lineWidth < stride)
            Throw(HaruStatus.InvalidImage, "Raw 1-bit image line width is too small.");

        var expectedSize = CheckedImageByteCount(lineWidth, height, 1, 8);
        if (data.Length < expectedSize)
            Throw(HaruStatus.InvalidImage, $"Raw 1-bit image data length was {data.Length}; expected at least {expectedSize} bytes.");

        var rows = new byte[CheckedImageByteCount(stride, height, 1, 8)];

        for (var y = 0; y < height; y++)
        {
            var sourceY = topIsFirst ? y : height - 1 - y;
            Buffer.BlockCopy(data, sourceY * lineWidth, rows, y * stride, stride);
        }

        var streamData = rows;
        var stream = CreateImageStream(streamData, width, height, PdfColorSpace.DeviceGray, 1);

        if (_compressionMode.HasFlag(CompressionMode.Image))
        {
            stream.SetData(CcittFaxEncoder.EncodeGroup4(rows, width, height, stride));
            stream.Filter = PdfStreamFilter.CcittDecode;
            stream.CompressionMode = CompressionMode.None;

            var decodeParms = new PdfDictionary();
            decodeParms.Set("K", new PdfInteger(-1));
            decodeParms.Set("Columns", new PdfInteger(width));
            decodeParms.Set("Rows", new PdfInteger(height));
            decodeParms.Set("BlackIs1", new PdfBoolean(blackIs1));
            stream.SetDecodeParms(decodeParms);
        }

        return RegisterImage(stream, width, height, 1, PdfColorSpace.DeviceGray);
    }

    public PdfImage LoadPngImageFromFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "File name cannot be empty.");

        try
        {
            return LoadPngImageFromMem(File.ReadAllBytes(fileName));
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfImage LoadPngImageFromFile2(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "File name cannot be empty.");

        try
        {
            var fullPath = Path.GetFullPath(fileName);
            var pngBytes = File.ReadAllBytes(fullPath);
            var png = PngImageLoader.LoadMetadata(pngBytes, this);

            if (png.RequiresImmediateImageData)
                return RegisterPngImage(PngImageLoader.Load(pngBytes, this));

            return RegisterPngImage(png, () => LoadDelayedPngImageData(fullPath, png), fullPath);
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfImage LoadPngImageFromMem(byte[] data)
    {
        if (data is null)
            Throw(HaruStatus.InvalidPngImage, "PNG data cannot be null.");

        var png = PngImageLoader.Load(data, this);
        return RegisterPngImage(png);
    }

    private PdfImage RegisterPngImage(PngImageData png, Func<byte[]>? delayedDataProvider = null, string? delayedFileName = null)
    {
        var colorSpaceObject = CreatePngColorSpaceObject(png);
        var stream = CreateImageStream(
            delayedDataProvider is null ? png.ImageData : [],
            png.Width,
            png.Height,
            png.ColorSpace,
            png.BitsPerComponent,
            colorSpaceObject);

        if (delayedDataProvider is not null)
        {
            stream.SetDelayedData(delayedDataProvider);
            var fileName = PdfString.FromText(delayedFileName ?? string.Empty);
            fileName.IsHidden = true;
            stream.Dictionary.Set("_FILE_NAME", fileName);
        }

        if (!string.IsNullOrEmpty(png.ColorManagement?.RenderingIntent))
            stream.Dictionary.SetName("Intent", png.ColorManagement.RenderingIntent);

        if (png.ColorMask is not null)
            stream.Dictionary.Set("Mask", new PdfArray(png.ColorMask.Select(static value => new PdfInteger(value))));

        if (png.SoftMaskData is not null)
        {
            var softMaskStream = CreateImageStream(png.SoftMaskData, png.Width, png.Height, PdfColorSpace.DeviceGray, 8);
            var softMaskObject = AddObject(softMaskStream);
            stream.Dictionary.Set("SMask", softMaskObject.Reference);
        }

        return RegisterImage(stream, png.Width, png.Height, png.BitsPerComponent, png.ColorSpace);
    }

    private byte[] LoadDelayedPngImageData(string fileName, PngImageData expected)
    {
        try
        {
            var actual = PngImageLoader.Load(File.ReadAllBytes(fileName), this);
            ValidateDelayedPngCompatibility(expected, actual);
            return actual.ImageData;
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    private void ValidateDelayedPngCompatibility(PngImageData expected, PngImageData actual)
    {
        if (actual.SoftMaskData is not null
            || actual.Width != expected.Width
            || actual.Height != expected.Height
            || actual.BitsPerComponent != expected.BitsPerComponent
            || actual.ColorSpace != expected.ColorSpace
            || actual.IndexedHighValue != expected.IndexedHighValue
            || !SameSequence(actual.ColorMask, expected.ColorMask)
            || !SameSequence(actual.IndexedPalette, expected.IndexedPalette)
            || !SamePngColorManagement(actual.ColorManagement, expected.ColorManagement))
        {
            Throw(HaruStatus.InvalidPngImage, "Delayed PNG file changed to an incompatible image format before write.");
        }
    }

    private static bool SamePngColorManagement(PngColorManagementData? left, PngColorManagementData? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Gamma == right.Gamma
            && left.Chromaticities == right.Chromaticities
            && string.Equals(left.RenderingIntent, right.RenderingIntent, StringComparison.Ordinal)
            && SameSequence(left.IccProfile, right.IccProfile);
    }

    private static bool SameSequence<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null || left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(left[i], right[i]))
                return false;
        }

        return true;
    }

    public PdfImage LoadJpegImageFromFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "File name cannot be empty.");

        try
        {
            return LoadJpegImageFromMem(File.ReadAllBytes(fileName));
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
            throw;
        }
    }

    public PdfImage LoadJpegImageFromMem(byte[] data)
    {
        if (data is null)
            Throw(HaruStatus.InvalidJpegData, "JPEG data cannot be null.");

        var header = ReadJpegHeader(data);
        var stream = CreateImageStream(data, header.Width, header.Height, header.ColorSpace, header.BitsPerComponent);
        stream.Filter = PdfStreamFilter.DctDecode;

        if (header.ColorSpace == PdfColorSpace.DeviceCmyk)
        {
            stream.Dictionary.Set("Decode", new PdfArray([
                new PdfInteger(1), new PdfInteger(0),
                new PdfInteger(1), new PdfInteger(0),
                new PdfInteger(1), new PdfInteger(0),
                new PdfInteger(1), new PdfInteger(0)
            ]));
        }

        return RegisterImage(stream, header.Width, header.Height, header.BitsPerComponent, header.ColorSpace);
    }

    public void SetPassword(string ownerPassword, string? userPassword)
    {
        if (ownerPassword is null)
            Throw(HaruStatus.EncryptInvalidPassword, "Owner password cannot be null.");

        try
        {
            EnsureEncryptionDictionary().SetPassword(ownerPassword, userPassword);
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
    }

    public void SetPermission(Permission permission) => SetPermission((uint)permission);

    public void SetPermission(uint permission)
    {
        var encryption = _encryption;
        if (encryption is null)
        {
            Throw(HaruStatus.DocEncryptDictNotFound, "Encryption dictionary has not been created. Call SetPassword first.");
            return;
        }

        encryption.SetPermission(permission);
    }

    public void SetEncryptionMode(PdfEncryptMode mode, uint keyLength = 0)
    {
        var encryption = _encryption;
        if (encryption is null)
        {
            Throw(HaruStatus.DocEncryptDictNotFound, "Encryption dictionary has not been created. Call SetPassword first.");
            return;
        }

        try
        {
            encryption.SetMode(mode, keyLength);
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
    }

    public void SetEncryptOff()
    {
        if (_encryptionObject is not null)
            _encryptionObject.Value = PdfNull.New();

        _encryption = null;
        _encryptionObject = null;
    }

    public void UseJPEncodings()
    {
        _ = GetEncoder("90ms-RKSJ-H");
        _ = GetEncoder("90ms-RKSJ-V");
        _ = GetEncoder("90msp-RKSJ-H");
        _ = GetEncoder("90msp-RKSJ-V");
        _ = GetEncoder("EUC-H");
        _ = GetEncoder("EUC-V");
    }

    public void UseKREncodings()
    {
        _ = GetEncoder("KSCms-UHC-H");
        _ = GetEncoder("KSCms-UHC-HW-H");
        _ = GetEncoder("KSCms-UHC-HW-V");
        _ = GetEncoder("KSC-EUC-H");
        _ = GetEncoder("KSC-EUC-V");
    }

    public void UseCNSEncodings()
    {
        _ = GetEncoder("GBK-EUC-H");
        _ = GetEncoder("GBK-EUC-V");
        _ = GetEncoder("GB-EUC-H");
        _ = GetEncoder("GB-EUC-V");
    }

    public void UseCNTEncodings()
    {
        _ = GetEncoder("ETen-B5-H");
        _ = GetEncoder("ETen-B5-V");
    }

    public void UseUTFEncodings()
    {
        _ = GetEncoder("UTF-8");
    }

    public void UseJPFonts()
    {
        _ = PredefinedCidFonts.CreateProgram("MS-Mincho");
    }

    public void UseKRFonts()
    {
        _ = PredefinedCidFonts.CreateProgram("Dotum");
    }

    public void UseCNSFonts()
    {
        _ = PredefinedCidFonts.CreateProgram("SimSun");
    }

    public void UseCNTFonts()
    {
        _ = PredefinedCidFonts.CreateProgram("MingLiU");
    }

    public byte[] SaveToStream()
    {
        using var stream = new MemoryStream();
        Save(stream);
        _lastSavedStream = stream.ToArray();
        _streamPosition = 0;
        return _lastSavedStream.ToArray();
    }

    public uint GetStreamSize() => (uint)(_lastSavedStream?.Length ?? 0);

    public byte[] ReadFromStream(uint size)
    {
        if (_lastSavedStream is null)
            SaveToStream();

        var available = _lastSavedStream!.Length - _streamPosition;
        var count = (int)Math.Min(size, (uint)Math.Max(0, available));
        var buffer = new byte[count];
        Array.Copy(_lastSavedStream, _streamPosition, buffer, 0, count);
        _streamPosition += count;
        return buffer;
    }

    public byte[] GetContents()
    {
        if (_lastSavedStream is null)
            SaveToStream();

        return _lastSavedStream!.ToArray();
    }

    public void ResetStream()
    {
        _lastSavedStream = null;
        _streamPosition = 0;
    }

    public void SaveToFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            Throw(HaruStatus.MissingFileNameEntry, "File name cannot be empty.");

        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(fileName));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var stream = File.Create(fileName);
            Save(stream);
        }
        catch (HaruException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            Throw(HaruStatus.FileOpenError, ex.Message, unchecked((uint)ex.HResult));
        }
    }

    public void Save(Stream output)
    {
        EnsureHasDoc();

        if (output is null || !output.CanWrite)
            Throw(HaruStatus.InvalidStream, "Output stream is not writable.");

        if (!output.CanSeek)
        {
            using var temp = new MemoryStream();
            Save(temp);
            temp.Position = 0;
            temp.CopyTo(output);
            return;
        }

        try
        {
            PrepareForSave();
            PrepareEncryption();
            WritePdf(output);
        }
        catch (HaruException ex)
        {
            throw Propagate(ex);
        }
    }

    public void Dispose()
    {
        _lastSavedStream = null;
    }

    internal PdfIndirectObject AddObject(PdfObject value)
    {
        EnsureHasDoc();

        var obj = new PdfIndirectObject(_objects.Count + 1, value);
        obj.AttachError(Error);
        _objects.Add(obj);
        return obj;
    }

    private void EnsureHasDoc()
    {
        if (!_hasDoc)
            Throw(HaruStatus.InvalidDocument, "Document has no active content. Call NewDoc before using it.");
    }

    private void ResetDocumentState(bool resetCompression, bool hasDoc)
    {
        _objects.Clear();
        _pages.Clear();
        _images.Clear();
        _fontFileObjects.Clear();
        _embeddedFiles.Clear();
        _outputIntents.Clear();
        _rootOutlines.Clear();
        _pdfAXmpExtensions.Clear();
        _fonts.Clear();
        _fontPrograms.Clear();
        _namedDestinations.Clear();
        _namedJavaScripts.Clear();
        _infoValues.Clear();
        _encoders.Clear();
        _pageLabels.Clear();

        _encryption = null;
        _encryptionObject = null;
        _metadataObject = null;
        _outlineRootObject = null;
        _fileId = null;
        _metadataXml = null;
        _lastSavedStream = null;
        _streamPosition = 0;
        _pdfVersion = "1.4";
        _viewerPreference = PdfViewerPreference.None;
        _pdfAType = PdfPdfAType.NonPdfA;
        _extGStateCount = 0;
        _shadingCount = 0;
        _xObjectCount = 0;
        _pagePerPages = 0;
        CurrentPage = null;
        CurrentEncoder = null;
        PageLayout = PdfPageLayout.Single;
        PageMode = PdfPageMode.UseNone;

        if (resetCompression)
            _compressionMode = CompressionMode.None;

        _pagesDictionary.Clear();
        _catalogDictionary.Clear();
        _infoDictionary.Clear();

        _hasDoc = hasDoc;

        if (hasDoc)
        {
            _objects.Add(_pagesObject);
            _objects.Add(_catalogObject);
            _objects.Add(_infoObject);

            _pagesDictionary.Subclass = PdfObjectClass.Pages;
            _pagesDictionary.SetName("Type", "Pages");
            _pagesDictionary.Set("Kids", new PdfArray());
            _pagesDictionary.Set("Count", new PdfInteger(0));

            _catalogDictionary.Subclass = PdfObjectClass.Catalog;
            _catalogDictionary.SetName("Type", "Catalog");
            _catalogDictionary.Set("Pages", _pagesObject.Reference);

            var producer = $"Haru Free PDF Library {HaruVersion.Text}";
            var creationDate = PdfDate(DateTimeOffset.Now);
            _infoValues[PdfInfoType.Producer] = producer;
            _infoValues[PdfInfoType.CreationDate] = creationDate;
            _infoDictionary.Set("Producer", PdfString.FromText(producer));
            _infoDictionary.Set("CreationDate", PdfString.FromText(creationDate));
        }
        else
        {
            _pagesDictionary.Subclass = 0;
            _catalogDictionary.Subclass = 0;
            _infoDictionary.Subclass = 0;
        }

        Error.Reset();
    }

    private PdfIndirectObject EnsureOutlineRoot()
    {
        if (_outlineRootObject is not null)
            return _outlineRootObject;

        var root = new PdfDictionary { Subclass = PdfObjectClass.Outline };
        root.SetName("Type", "Outlines");
        root.Set("Count", new PdfInteger(0));
        _outlineRootObject = AddObject(root);
        _catalogDictionary.Set("Outlines", _outlineRootObject.Reference);
        return _outlineRootObject;
    }

    private List<PdfOutline> RootOutlines(PdfIndirectObject _) => _rootOutlines;

    private PdfFontProgram ResolveFontProgram(string fontName)
    {
        if (_fontPrograms.TryGetValue(fontName, out var program))
            return program;

        if (Base14Fonts.IsSupported(fontName))
            return Base14Fonts.CreateProgram(fontName);

        if (PredefinedCidFonts.IsSupported(fontName))
            return PredefinedCidFonts.CreateProgram(fontName);

        Throw(HaruStatus.InvalidFontName, $"Font is not registered: {fontName}.");
        throw new UnreachableException();
    }

    private void RegisterFontProgram(PdfFontProgram program)
    {
        if (_fontPrograms.ContainsKey(program.BaseFont) || Base14Fonts.IsSupported(program.BaseFont))
            Throw(HaruStatus.FontExists, $"Font is already registered: {program.BaseFont}.");

        _fontPrograms.Add(program.BaseFont, program);
    }

    private PdfDictionary CreateFontDictionary(PdfFontProgram program, PdfEncoding encoding, out PdfCompositeFontObjects? compositeObjects)
    {
        compositeObjects = null;
        if (encoding.IsComposite)
            return CreateCompositeFontDictionary(program, encoding, out compositeObjects);

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Font };
        dictionary.SetName("Type", "Font");
        dictionary.SetName("BaseFont", program.BaseFont);
        dictionary.SetName("Subtype", program.Kind == PdfFontProgramKind.TrueType ? "TrueType" : "Type1");

        if (!string.Equals(encoding.PdfName, "StandardEncoding", StringComparison.Ordinal))
            dictionary.SetName("Encoding", encoding.PdfName);

        if (!program.IsBase14)
        {
            dictionary.Set("FirstChar", new PdfInteger(encoding.FirstChar));
            dictionary.Set("LastChar", new PdfInteger(encoding.LastChar));
            dictionary.Set("Widths", BuildWidthsArray(program, encoding));

            if (program.Descriptor.MissingWidth != 0)
                dictionary.Set("MissingWidth", new PdfInteger(program.Descriptor.MissingWidth));

            dictionary.Set("FontDescriptor", CreateFontDescriptor(program).Reference);
        }

        return dictionary;
    }

    private PdfDictionary CreateCompositeFontDictionary(PdfFontProgram program, PdfEncoding encoding, out PdfCompositeFontObjects? compositeObjects)
    {
        compositeObjects = null;
        if (program.Kind == PdfFontProgramKind.CidType0)
            return CreatePredefinedCidFontDictionary(program, encoding);

        if (program.Kind == PdfFontProgramKind.OpenTypeCffCidKeyed && program.SupportsCompositeEncoding)
            return CreateOpenTypeCffCidFontDictionary(program, encoding, out compositeObjects);

        if (program.Kind != PdfFontProgramKind.TrueType || !program.SupportsCompositeEncoding)
            Throw(HaruStatus.InvalidEncoderType, "Composite UTF/Identity encodings require an embedded TrueType font.");

        var descendant = new PdfDictionary { Subclass = PdfObjectClass.Font };
        descendant.SetName("Type", "Font");
        descendant.SetName("Subtype", "CIDFontType2");
        descendant.SetName("BaseFont", program.BaseFont);
        descendant.Set("CIDSystemInfo", CreateCidSystemInfo());
        descendant.Set("FontDescriptor", CreateFontDescriptor(program).Reference);
        var cidToGidMapStream = new PdfStreamObject([]);
        var cidToGidMapObject = AddObject(cidToGidMapStream);
        descendant.Set("CIDToGIDMap", cidToGidMapObject.Reference);

        if (program.Descriptor.MissingWidth != 0)
            descendant.Set("DW", new PdfInteger(program.Descriptor.MissingWidth));

        descendant.Set("DW2", new PdfArray([new PdfInteger(program.CidVerticalPosition), new PdfInteger(program.CidVerticalDisplacement)]));
        descendant.Set("W", new PdfArray());
        var descendantObject = AddObject(descendant);
        var toUnicodeStream = new PdfStreamObject([]);
        var toUnicodeObject = AddObject(toUnicodeStream);

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Font };
        dictionary.SetName("Type", "Font");
        dictionary.SetName("Subtype", "Type0");
        dictionary.SetName("BaseFont", program.BaseFont);
        dictionary.SetName("Encoding", encoding.PdfName);
        dictionary.Set("DescendantFonts", new PdfArray([descendantObject.Reference]));
        dictionary.Set("ToUnicode", toUnicodeObject.Reference);
        compositeObjects = new PdfCompositeFontObjects(descendant, cidToGidMapStream, toUnicodeStream);
        return dictionary;
    }

    private PdfDictionary CreateOpenTypeCffCidFontDictionary(PdfFontProgram program, PdfEncoding encoding, out PdfCompositeFontObjects compositeObjects)
    {
        var descendant = new PdfDictionary { Subclass = PdfObjectClass.Font };
        descendant.SetName("Type", "Font");
        descendant.SetName("Subtype", "CIDFontType0");
        descendant.SetName("BaseFont", program.BaseFont);
        descendant.Set("CIDSystemInfo", CreateCidSystemInfo(program.CidOrdering ?? "Identity", program.CidSupplement));
        descendant.Set("FontDescriptor", CreateFontDescriptor(program).Reference);

        if (program.Descriptor.MissingWidth != 0)
            descendant.Set("DW", new PdfInteger(program.Descriptor.MissingWidth));

        descendant.Set("DW2", new PdfArray([new PdfInteger(program.CidVerticalPosition), new PdfInteger(program.CidVerticalDisplacement)]));
        descendant.Set("W", new PdfArray());
        var descendantObject = AddObject(descendant);
        var toUnicodeStream = new PdfStreamObject([]);
        var toUnicodeObject = AddObject(toUnicodeStream);

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Font };
        dictionary.SetName("Type", "Font");
        dictionary.SetName("Subtype", "Type0");
        dictionary.SetName("BaseFont", program.BaseFont);
        dictionary.SetName("Encoding", encoding.PdfName);
        dictionary.Set("DescendantFonts", new PdfArray([descendantObject.Reference]));
        dictionary.Set("ToUnicode", toUnicodeObject.Reference);
        compositeObjects = new PdfCompositeFontObjects(descendant, null, toUnicodeStream);
        return dictionary;
    }

    private PdfDictionary CreatePredefinedCidFontDictionary(PdfFontProgram program, PdfEncoding encoding)
    {
        if (!encoding.PreservesInputBytes)
            Throw(HaruStatus.InvalidEncoderType, "Predefined CID fonts require a CMap encoding.");

        var descendant = new PdfDictionary { Subclass = PdfObjectClass.Font };
        descendant.SetName("Type", "Font");
        descendant.SetName("Subtype", "CIDFontType0");
        descendant.SetName("BaseFont", program.BaseFont);
        descendant.Set("CIDSystemInfo", CreateCidSystemInfo(program.CidOrdering ?? "Identity", program.CidSupplement));
        descendant.Set("FontDescriptor", CreateFontDescriptor(program).Reference);
        descendant.Set("DW", new PdfInteger(program.CidDefaultWidth));
        descendant.Set("DW2", new PdfArray([new PdfInteger(program.CidVerticalPosition), new PdfInteger(program.CidVerticalDisplacement)]));
        descendant.Set("W", BuildPredefinedCidWidthsArray(program));
        var descendantObject = AddObject(descendant);

        var dictionary = new PdfDictionary { Subclass = PdfObjectClass.Font };
        dictionary.SetName("Type", "Font");
        dictionary.SetName("Subtype", "Type0");
        dictionary.SetName("BaseFont", program.BaseFont);
        dictionary.SetName("Encoding", encoding.PdfName);
        dictionary.Set("DescendantFonts", new PdfArray([descendantObject.Reference]));
        return dictionary;
    }

    private PdfArray BuildWidthsArray(PdfFontProgram program, PdfEncoding encoding)
    {
        var widths = new List<PdfObject>(encoding.LastChar - encoding.FirstChar + 1);

        for (var code = encoding.FirstChar; code <= encoding.LastChar; code++)
            widths.Add(new PdfInteger(program.WidthOfCode(encoding, (byte)code)));

        return new PdfArray(widths);
    }

    private static PdfDictionary CreateCidSystemInfo(string ordering = "Identity", int supplement = 0)
    {
        var cidSystemInfo = new PdfDictionary();
        cidSystemInfo.Set("Registry", PdfString.FromText("Adobe"));
        cidSystemInfo.Set("Ordering", PdfString.FromText(ordering));
        cidSystemInfo.Set("Supplement", new PdfInteger(supplement));
        return cidSystemInfo;
    }

    private static PdfArray BuildCompositeCidWidthsArray(PdfFontProgram program, PdfCompositeGlyphMap glyphMap)
    {
        var array = new PdfArray();
        PdfArray? subArray = null;
        var previousCid = -2;

        foreach (var (cid, glyphId) in glyphMap.CidToGlyphId)
        {
            if (cid <= 0)
                continue;

            if (subArray is null || cid != previousCid + 1)
            {
                subArray = new PdfArray();
                array.AddNumber(cid);
                array.Add(subArray);
            }

            subArray.AddNumber(program.WidthOfGlyph(glyphId));
            previousCid = cid;
        }

        return array;
    }

    private static PdfArray BuildPredefinedCidWidthsArray(PdfFontProgram program)
    {
        var array = new PdfArray();
        PdfArray? subArray = null;
        var saveCid = 0;

        foreach (var width in program.CidWidths)
        {
            if (subArray is null || width.Cid != saveCid + 1)
            {
                subArray = new PdfArray();
                array.AddNumber(width.Cid);
                array.Add(subArray);
            }

            subArray.AddNumber(width.Width);
            saveCid = width.Cid;
        }

        return array;
    }

    private static byte[] CreateToUnicodeCMapData(PdfCompositeGlyphMap glyphMap)
    {
        var builder = new StringBuilder();
        builder.AppendLine("/CIDInit /ProcSet findresource begin");
        builder.AppendLine("12 dict begin");
        builder.AppendLine("begincmap");
        builder.AppendLine("/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> def");
        builder.AppendLine("/CMapName /Adobe-Identity-UCS def");
        builder.AppendLine("/CMapType 2 def");
        var hasOneByteCodes = glyphMap.CodeToUnicode.Keys.Any(static code => code.ByteLength == 1);
        var hasTwoByteCodes = glyphMap.CodeToUnicode.Keys.Any(static code => code.ByteLength == 2);
        var codeSpaceCount = (hasOneByteCodes ? 1 : 0) + (hasTwoByteCodes ? 1 : 0);
        if (codeSpaceCount == 0)
        {
            codeSpaceCount = 1;
            hasTwoByteCodes = true;
        }

        builder.AppendLine($"{codeSpaceCount} begincodespacerange");
        if (hasOneByteCodes)
            builder.AppendLine("<00> <FF>");
        if (hasTwoByteCodes)
            builder.AppendLine("<0000> <FFFF>");
        builder.AppendLine("endcodespacerange");

        foreach (var chunk in glyphMap.CodeToUnicode.Chunk(100))
        {
            builder.AppendLine($"{chunk.Length} beginbfchar");
            foreach (var item in chunk)
                builder.AppendLine($"<{FormatCompositeCode(item.Key)}> <{FormatUnicodeScalar(item.Value)}>");
            builder.AppendLine("endbfchar");
        }

        builder.AppendLine("endcmap");
        builder.AppendLine("CMapName currentdict /CMap defineresource pop");
        builder.AppendLine("end");
        builder.AppendLine("end");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static byte[] CreateCidToGidMapData(PdfFontProgram program, PdfCompositeGlyphMap glyphMap)
    {
        var maxCid = glyphMap.CidToGlyphId.Count == 0 ? 0 : glyphMap.CidToGlyphId.Keys.Max();
        var data = new byte[(maxCid + 1) * 2];

        foreach (var (cid, glyphId) in glyphMap.CidToGlyphId)
        {
            if (cid < 0 || cid > maxCid)
                continue;

            var subsetGlyphId = program.SubsetGlyphIdOfOriginal(glyphId);
            if (subsetGlyphId < 0 || subsetGlyphId > ushort.MaxValue)
                subsetGlyphId = 0;

            data[cid * 2] = (byte)(subsetGlyphId >> 8);
            data[cid * 2 + 1] = (byte)subsetGlyphId;
        }

        return data;
    }

    private static string FormatCompositeCode(PdfCompositeCharCode code)
    {
        return code.ByteLength == 1
            ? (code.Code & 0xFF).ToString("X2", System.Globalization.CultureInfo.InvariantCulture)
            : (code.Code & 0xFFFF).ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string FormatUnicodeScalar(int unicode)
    {
        if (unicode <= 0xFFFF)
            return unicode.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);

        var scalar = unicode - 0x10000;
        var high = 0xD800 + (scalar >> 10);
        var low = 0xDC00 + (scalar & 0x3FF);
        return high.ToString("X4", System.Globalization.CultureInfo.InvariantCulture)
            + low.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    private PdfIndirectObject CreateFontDescriptor(PdfFontProgram program)
    {
        if (program.DescriptorObject is not null)
            return program.DescriptorObject;

        var descriptor = new PdfDictionary();
        var metrics = program.Descriptor;
        descriptor.SetName("Type", "FontDescriptor");
        descriptor.SetName("FontName", metrics.FontName);
        descriptor.Set("Flags", new PdfInteger(metrics.Flags));
        descriptor.Set("FontBBox", new PdfArray([
            new PdfReal(metrics.FontBBox.Left),
            new PdfReal(metrics.FontBBox.Bottom),
            new PdfReal(metrics.FontBBox.Right),
            new PdfReal(metrics.FontBBox.Top)
        ]));
        descriptor.Set("ItalicAngle", new PdfInteger(metrics.ItalicAngle));
        descriptor.Set("Ascent", new PdfInteger(metrics.Ascent));
        descriptor.Set("Descent", new PdfInteger(metrics.Descent));
        descriptor.Set("StemV", new PdfInteger(metrics.StemV));

        if (metrics.CapHeight != 0)
            descriptor.Set("CapHeight", new PdfInteger(metrics.CapHeight));

        if (metrics.XHeight != 0)
            descriptor.Set("XHeight", new PdfInteger(metrics.XHeight));

        if (metrics.MissingWidth != 0)
            descriptor.Set("MissingWidth", new PdfInteger(metrics.MissingWidth));

        if (program.FontFile is not null)
        {
            if (string.Equals(program.FontFile.Subtype, "OpenType", StringComparison.Ordinal))
                EnsurePdfVersion("1.6");

            var fontFileStream = new PdfStreamObject(program.FontFile.Data)
            {
                Kind = PdfStreamKind.Font,
                CompressionMode = _compressionMode
            };

            if (program.FontFile.Subtype is not null)
                fontFileStream.Dictionary.SetName("Subtype", program.FontFile.Subtype);

            if (program.FontFile.WritesLengthEntries)
            {
                fontFileStream.Dictionary.Set("Length1", new PdfInteger(program.FontFile.Length1));
                fontFileStream.Dictionary.Set("Length2", new PdfInteger(program.FontFile.Length2));
                fontFileStream.Dictionary.Set("Length3", new PdfInteger(program.FontFile.Length3));
            }

            program.FontFileObject = AddObject(fontFileStream);
            _fontFileObjects.Add(program.FontFileObject);
            descriptor.Set(program.FontFile.DescriptorKey, program.FontFileObject.Reference);
        }

        program.DescriptorObject = AddObject(descriptor);
        return program.DescriptorObject;
    }

    private void PrepareForSave()
    {
        var kids = new PdfArray(_pages.Select(page => page.PageObject.Reference));
        _pagesDictionary.Set("Kids", kids);
        _pagesDictionary.Set("Count", new PdfInteger(_pages.Count));

        _catalogDictionary.SetName("PageLayout", PageLayoutName(PageLayout));
        _catalogDictionary.SetName("PageMode", PageModeName(PageMode));
        PrepareViewerPreferences();
        PreparePageLabels();
        PrepareOutlines();
        PrepareNameTrees();
        PrepareAssociatedFiles();
        PrepareOutputIntents();
        PreparePdfA();

        foreach (var page in _pages)
            page.PrepareForSave(_pagesObject.Reference);

        PrepareFontFiles();

        foreach (var image in _images)
        {
            if (image.ImageObject.Value is PdfStreamObject imageStream)
                imageStream.CompressionMode = _compressionMode;
        }

        foreach (var fontFile in _fontFileObjects)
        {
            if (fontFile.Value is PdfStreamObject fontFileStream)
                fontFileStream.CompressionMode = _compressionMode;
        }

        if (_metadataObject?.Value is PdfStreamObject metadataStream)
            metadataStream.CompressionMode = _compressionMode;
    }

    private void PrepareFontFiles()
    {
        var seen = new HashSet<PdfFontProgram>();
        foreach (var font in _fonts.Values)
        {
            var program = font.Program;
            if (!seen.Add(program) || program.FontFile is null)
                continue;

            var subset = program.BuildFontFileSubset();
            if (subset is null)
                continue;

            var subsetData = subset.Data;
            program.FontFile.ReplaceData(subsetData, subsetData.Length, 0, 0);
            if (program.FontFileObject?.Value is PdfStreamObject fontFileStream)
            {
                fontFileStream.SetData(subsetData);
                if (program.FontFile.WritesLengthEntries)
                {
                    fontFileStream.Dictionary.Set("Length1", new PdfInteger(program.FontFile.Length1));
                    fontFileStream.Dictionary.Set("Length2", new PdfInteger(program.FontFile.Length2));
                    fontFileStream.Dictionary.Set("Length3", new PdfInteger(program.FontFile.Length3));
                }
            }
        }

        foreach (var binding in _compositeFontBindings)
        {
            binding.Objects.Descendant.Set("W", BuildCompositeCidWidthsArray(binding.Font.Program, binding.GlyphMap));
            binding.Objects.CidToGidMapStream?.SetData(CreateCidToGidMapData(binding.Font.Program, binding.GlyphMap));
            binding.Objects.ToUnicodeStream.SetData(CreateToUnicodeCMapData(binding.GlyphMap));
        }
    }

    private void PrepareViewerPreferences()
    {
        if (_viewerPreference == PdfViewerPreference.None)
        {
            _catalogDictionary.Remove("ViewerPreferences");
            return;
        }

        var preferences = new PdfDictionary();
        if (_viewerPreference.HasFlag(PdfViewerPreference.HideToolbar))
            preferences.Set("HideToolbar", new PdfBoolean(true));
        if (_viewerPreference.HasFlag(PdfViewerPreference.HideMenubar))
            preferences.Set("HideMenubar", new PdfBoolean(true));
        if (_viewerPreference.HasFlag(PdfViewerPreference.HideWindowUI))
            preferences.Set("HideWindowUI", new PdfBoolean(true));
        if (_viewerPreference.HasFlag(PdfViewerPreference.FitWindow))
            preferences.Set("FitWindow", new PdfBoolean(true));
        if (_viewerPreference.HasFlag(PdfViewerPreference.CenterWindow))
            preferences.Set("CenterWindow", new PdfBoolean(true));
        if (_viewerPreference.HasFlag(PdfViewerPreference.PrintScalingNone))
            preferences.SetName("PrintScaling", "None");

        _catalogDictionary.Set("ViewerPreferences", preferences);
    }

    private void PreparePageLabels()
    {
        if (_pageLabels.Count == 0)
        {
            _catalogDictionary.Remove("PageLabels");
            return;
        }

        var nums = new PdfArray();
        foreach (var label in _pageLabels.OrderBy(static item => item.PageNumber))
        {
            var dict = new PdfDictionary();
            dict.SetName("S", label.Style switch
            {
                PdfPageNumStyle.Decimal => "D",
                PdfPageNumStyle.UpperRoman => "R",
                PdfPageNumStyle.LowerRoman => "r",
                PdfPageNumStyle.UpperLetters => "A",
                PdfPageNumStyle.LowerLetters => "a",
                _ => "D"
            });

            if (!string.IsNullOrEmpty(label.Prefix))
                dict.Set("P", PdfString.FromText(label.Prefix));

            if (label.FirstPage != 1)
                dict.Set("St", new PdfInteger(label.FirstPage));

            nums.Add(new PdfInteger(label.PageNumber));
            nums.Add(dict);
        }

        var labels = new PdfDictionary();
        labels.Set("Nums", nums);
        _catalogDictionary.Set("PageLabels", labels);
    }

    private void PrepareOutlines()
    {
        if (_outlineRootObject is null)
            return;

        if (_outlineRootObject.Value is not PdfDictionary root)
        {
            Throw(HaruStatus.InvalidOutline, "Outline root is invalid.");
            throw new UnreachableException();
        }

        if (!root.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Outline))
            Throw(HaruStatus.InvalidOutline, "Outline root is invalid.");

        if (_rootOutlines.Count == 0)
        {
            root.Set("Count", new PdfInteger(0));
            root.Remove("First");
            root.Remove("Last");
            return;
        }

        root.Set("First", _rootOutlines[0].OutlineObject.Reference);
        root.Set("Last", _rootOutlines[^1].OutlineObject.Reference);

        var count = 0;
        for (var i = 0; i < _rootOutlines.Count; i++)
        {
            var previous = i == 0 ? null : _rootOutlines[i - 1];
            var next = i == _rootOutlines.Count - 1 ? null : _rootOutlines[i + 1];
            count += _rootOutlines[i].Prepare(_outlineRootObject.Reference, previous, next);
        }

        root.Set("Count", new PdfInteger(count));
    }

    private void PrepareNameTrees()
    {
        var names = new PdfDictionary { Subclass = PdfObjectClass.NameDictionary };
        var hasNames = false;

        if (_namedDestinations.Count > 0)
        {
            names.Set("Dests", BuildNameTree(_namedDestinations.Select(static item =>
                new KeyValuePair<string, PdfObject>(item.Key, item.Value.DestinationObject.Reference))));
            hasNames = true;
        }

        if (_embeddedFiles.Count > 0)
        {
            foreach (var embeddedFile in _embeddedFiles)
                embeddedFile.ValidateOrThrow();

            names.Set("EmbeddedFiles", BuildNameTree(_embeddedFiles
                .OrderBy(static file => file.Name, StringComparer.Ordinal)
                .Select(static file => new KeyValuePair<string, PdfObject>(file.Name, file.FileSpecObject.Reference))));
            hasNames = true;
        }

        if (_namedJavaScripts.Count > 0)
        {
            foreach (var javaScript in _namedJavaScripts.Values)
                javaScript.ValidateOrThrow();

            names.Set("JavaScript", BuildNameTree(_namedJavaScripts.Select(static item =>
                new KeyValuePair<string, PdfObject>(item.Key, item.Value.ScriptObject.Reference))));
            hasNames = true;
        }

        if (hasNames)
            _catalogDictionary.Set("Names", names);
        else
            _catalogDictionary.Remove("Names");
    }

    private PdfDictionary BuildNameTree(IEnumerable<KeyValuePair<string, PdfObject>> entries)
    {
        const int leafSize = 64;
        var sorted = entries.OrderBy(static item => item.Key, StringComparer.Ordinal).ToArray();
        var tree = new PdfDictionary { Subclass = PdfObjectClass.NameTree };

        if (sorted.Length == 0)
            return tree;

        tree.Set("Limits", new PdfArray([PdfString.FromText(sorted[0].Key), PdfString.FromText(sorted[^1].Key)]));

        if (sorted.Length <= leafSize)
        {
            tree.Set("Names", BuildNameTreeLeafNames(sorted));
            return tree;
        }

        var kids = new PdfArray();
        for (var offset = 0; offset < sorted.Length; offset += leafSize)
        {
            var leafEntries = sorted.Skip(offset).Take(leafSize).ToArray();
            var leaf = new PdfDictionary { Subclass = PdfObjectClass.NameTree };
            leaf.Set("Limits", new PdfArray([PdfString.FromText(leafEntries[0].Key), PdfString.FromText(leafEntries[^1].Key)]));
            leaf.Set("Names", BuildNameTreeLeafNames(leafEntries));
            kids.Add(AddObject(leaf).Reference);
        }

        tree.Set("Kids", kids);
        return tree;
    }

    private static PdfArray BuildNameTreeLeafNames(IEnumerable<KeyValuePair<string, PdfObject>> entries)
    {
        var array = new PdfArray();
        foreach (var entry in entries)
        {
            array.Add(PdfString.FromText(entry.Key));
            array.Add(entry.Value);
        }

        return array;
    }

    private void PrepareAssociatedFiles()
    {
        if (_embeddedFiles.Count == 0)
        {
            _catalogDictionary.Remove("AF");
            return;
        }

        foreach (var embeddedFile in _embeddedFiles)
            embeddedFile.ValidateOrThrow();

        _catalogDictionary.Set("AF", new PdfArray(_embeddedFiles.Select(static file => file.FileSpecObject.Reference)));
    }

    private void PrepareOutputIntents()
    {
        if (_outputIntents.Count == 0)
        {
            _catalogDictionary.Remove("OutputIntents");
            return;
        }

        foreach (var outputIntent in _outputIntents)
            outputIntent.ValidateOrThrow();

        _catalogDictionary.Set("OutputIntents", new PdfArray(_outputIntents.Select(static intent => intent.IntentObject.Reference)));
    }

    private void PreparePdfA()
    {
        if (_pdfAType == PdfPdfAType.NonPdfA)
            return;

        if (_encryption is not null)
            Throw(HaruStatus.InvalidDocumentState, "PDF/A documents cannot be encrypted.");

        if (_outputIntents.Count == 0)
            Throw(HaruStatus.InvalidDocumentState, "PDF/A documents require at least one output intent.");

        ValidatePdfAOutputIntents();
        ValidatePdfAActionAndMediaRestrictions();

        if (_embeddedFiles.Count > 0 && !PdfAAllowsAssociatedFiles(_pdfAType))
            Throw(HaruStatus.InvalidDocumentState, "Embedded files require PDF/A-3, PDF/A-4F, or PDF/A-4E conformance.");

        foreach (var embeddedFile in _embeddedFiles)
        {
            if (!embeddedFile.HasAFRelationship)
                embeddedFile.SetAFRelationship(PdfAFRelationship.Unspecified);
        }

        if (_metadataObject is null || _metadataXml is null || !_metadataXml.Contains("pdfaid:part", StringComparison.Ordinal))
        {
            SetXmpMetadata(CreatePdfAXmp(_pdfAType));
        }
        else
        {
            ValidatePdfAXmpIdentification(_metadataXml, _pdfAType);
        }

        EnsurePdfVersion(MinPdfVersionForPdfA(_pdfAType));

        var markInfo = new PdfDictionary();
        markInfo.Set("Marked", new PdfBoolean(true));
        _catalogDictionary.Set("MarkInfo", markInfo);

        if (_catalogDictionary.GetItem("StructTreeRoot", PdfObjectClass.Any) is null)
        {
            var structTreeRoot = new PdfDictionary();
            structTreeRoot.SetName("Type", "StructTreeRoot");
            structTreeRoot.Set("K", new PdfArray());
            _catalogDictionary.Set("StructTreeRoot", structTreeRoot);
        }

        _fileId ??= PdfEncryption.CreateFileId(_infoValues, _objects.Count, Error);
    }

    private static bool PdfAAllowsAssociatedFiles(PdfPdfAType type) =>
        type is PdfPdfAType.PdfA3A
            or PdfPdfAType.PdfA3B
            or PdfPdfAType.PdfA3U
            or PdfPdfAType.PdfA4E
            or PdfPdfAType.PdfA4F;

    private void ValidatePdfAActionAndMediaRestrictions()
    {
        if (_namedJavaScripts.Count > 0)
            Throw(HaruStatus.InvalidDocumentState, "PDF/A documents cannot contain named JavaScript.");

        foreach (var obj in _objects)
            ValidatePdfAObjectActionAndMediaRestrictions(obj.Value);
    }

    private void ValidatePdfAObjectActionAndMediaRestrictions(PdfObject obj)
    {
        var dictionary = obj switch
        {
            PdfStreamObject stream => stream.Dictionary,
            PdfDictionary dict => dict,
            _ => null
        };

        if (dictionary is null)
            return;

        var type = dictionary.Get<PdfName>("Type");
        var subtype = dictionary.Get<PdfName>("Subtype");

        if (type?.Value == "Annot" && subtype is not null && PdfAProhibitsAnnotationSubtype(subtype.Value, _pdfAType))
            Throw(HaruStatus.InvalidDocumentState, $"PDF/A documents cannot contain {subtype.Value} annotations.");

        ValidatePdfAActionObject(dictionary.GetItem("A", PdfObjectClass.Any), allowActionArray: false);
        ValidatePdfAActionObject(dictionary.GetItem("OpenAction", PdfObjectClass.Any), allowActionArray: false);

        if (dictionary.GetItem("AA", PdfObjectClass.Any) is PdfDictionary additionalActions)
            ValidatePdfAAdditionalActions(additionalActions);
    }

    private void ValidatePdfAAdditionalActions(PdfDictionary additionalActions)
    {
        foreach (var key in PdfAdditionalActionKeys)
            ValidatePdfAActionObject(additionalActions.GetItem(key, PdfObjectClass.Any), allowActionArray: false);
    }

    private void ValidatePdfAActionObject(PdfObject? action, bool allowActionArray)
    {
        switch (action)
        {
            case null:
                return;
            case PdfArray actions when allowActionArray:
                for (var i = 0; i < actions.Count; i++)
                    ValidatePdfAActionObject(actions.GetItem(i, PdfObjectClass.Any), allowActionArray: false);
                return;
            case PdfDictionary dictionary:
                var actionType = dictionary.Get<PdfName>("S");
                if (actionType is not null && PdfAProhibitedActionTypes.Contains(actionType.Value))
                    Throw(HaruStatus.InvalidDocumentState, $"PDF/A documents cannot contain {actionType.Value} actions.");

                ValidatePdfAActionObject(dictionary.GetItem("Next", PdfObjectClass.Any), allowActionArray: true);
                return;
        }
    }

    private static bool PdfAProhibitsAnnotationSubtype(string subtype, PdfPdfAType type)
    {
        if (subtype == "3D")
            return type != PdfPdfAType.PdfA4E;

        return PdfAProhibitedMediaAnnotationSubtypes.Contains(subtype);
    }

    private void ValidatePdfAOutputIntents()
    {
        foreach (var outputIntent in _outputIntents)
        {
            outputIntent.ValidateOrThrow();

            if (outputIntent.IntentObject.Value is not PdfDictionary dictionary)
            {
                Throw(HaruStatus.InvalidObject, "PDF/A output intent must be a dictionary.");
                throw new UnreachableException();
            }

            var type = dictionary.Get<PdfName>("Type");
            var subtype = dictionary.Get<PdfName>("S");
            if (type?.Value != "OutputIntent" || subtype?.Value != "GTS_PDFA1")
                Throw(HaruStatus.InvalidDocumentState, "PDF/A output intents must use /Type /OutputIntent and /S /GTS_PDFA1.");

            var profile = dictionary.GetItem("DestOutputProfile", PdfObjectClass.Dictionary);
            if (profile is not PdfStreamObject profileStream)
            {
                Throw(HaruStatus.InvalidDocumentState, "PDF/A output intents require an ICC destination profile stream.");
                throw new UnreachableException();
            }

            var components = profileStream.Dictionary.Get<PdfInteger>("N");
            if (components is null || components.Value is not (1 or 3 or 4))
                Throw(HaruStatus.InvalidIccComponentNum, "PDF/A ICC destination profile must declare 1, 3, or 4 components.");
        }
    }

    private void ValidatePdfAXmpIdentification(string metadataXml, PdfPdfAType pdfAType)
    {
        var (part, conformance, requiresRevision) = PdfAIdentification(pdfAType);
        if (!ContainsXmlScalar(metadataXml, "pdfaid:part", part))
            Throw(HaruStatus.InvalidDocumentState, $"PDF/A metadata pdfaid:part must be {part}.");

        if (!ContainsXmlScalar(metadataXml, "pdfaid:conformance", conformance))
            Throw(HaruStatus.InvalidDocumentState, $"PDF/A metadata pdfaid:conformance must be {conformance}.");

        if (requiresRevision && !ContainsXmlScalar(metadataXml, "pdfaid:rev", "2020"))
            Throw(HaruStatus.InvalidDocumentState, "PDF/A-4 metadata requires pdfaid:rev 2020.");
    }

    private static (string Part, string Conformance, bool RequiresRevision) PdfAIdentification(PdfPdfAType type) => type switch
    {
        PdfPdfAType.PdfA1A => ("1", "A", false),
        PdfPdfAType.PdfA1B => ("1", "B", false),
        PdfPdfAType.PdfA2A => ("2", "A", false),
        PdfPdfAType.PdfA2B => ("2", "B", false),
        PdfPdfAType.PdfA2U => ("2", "U", false),
        PdfPdfAType.PdfA3A => ("3", "A", false),
        PdfPdfAType.PdfA3B => ("3", "B", false),
        PdfPdfAType.PdfA3U => ("3", "U", false),
        PdfPdfAType.PdfA4 => ("4", string.Empty, true),
        PdfPdfAType.PdfA4E => ("4", "E", true),
        PdfPdfAType.PdfA4F => ("4", "F", true),
        _ => (string.Empty, string.Empty, false)
    };

    private static bool ContainsXmlScalar(string xml, string name, string value)
    {
        return xml.Contains($"{name}='{value}'", StringComparison.Ordinal)
            || xml.Contains($"{name}=\"{value}\"", StringComparison.Ordinal)
            || xml.Contains($"<{name}>{value}</{name}>", StringComparison.Ordinal);
    }

    private static string MinPdfVersionForPdfA(PdfPdfAType type) => type switch
    {
        PdfPdfAType.PdfA1A or PdfPdfAType.PdfA1B => "1.4",
        PdfPdfAType.PdfA2A or PdfPdfAType.PdfA2B or PdfPdfAType.PdfA2U
            or PdfPdfAType.PdfA3A or PdfPdfAType.PdfA3B or PdfPdfAType.PdfA3U => "1.7",
        PdfPdfAType.PdfA4 or PdfPdfAType.PdfA4E or PdfPdfAType.PdfA4F => "2.0",
        _ => "1.4"
    };

    private void EnsurePdfVersion(string minimumVersion)
    {
        if (Version.Parse(_pdfVersion) < Version.Parse(minimumVersion))
            _pdfVersion = minimumVersion;
    }

    private PdfEncryption EnsureEncryptionDictionary()
    {
        if (_encryption is not null)
            return _encryption;

        _encryption = new PdfEncryption(Error);
        var encryptionDictionary = new PdfDictionary { Subclass = PdfObjectClass.Encrypt };
        _encryptionObject = AddObject(encryptionDictionary);
        return _encryption;
    }

    private void PrepareEncryption()
    {
        if (_encryption is null)
            return;

        var encryptionDictionary = _encryptionObject?.Value as PdfDictionary;
        if (encryptionDictionary is null || !PdfEncryption.ValidateDictionary(encryptionDictionary))
        {
            Throw(HaruStatus.DocEncryptDictNotFound, "Encryption dictionary is missing.");
            return;
        }

        var fileId = PdfEncryption.CreateFileId(_infoValues, _objects.Count, Error);
        _encryption.Prepare(fileId);

        encryptionDictionary.Set("O", PdfBinary.FromBytes(_encryption.OwnerKey));
        encryptionDictionary.Set("U", PdfBinary.FromBytes(_encryption.UserKey));
        encryptionDictionary.SetName("Filter", "Standard");
        encryptionDictionary.Set("P", new PdfInteger(_encryption.PermissionValue));

        if (_encryption.Mode == PdfEncryptMode.R2)
        {
            encryptionDictionary.Set("V", new PdfInteger(1));
            encryptionDictionary.Set("R", new PdfInteger(2));
            encryptionDictionary.Remove("Length");
        }
        else
        {
            encryptionDictionary.Set("V", new PdfInteger(2));
            encryptionDictionary.Set("R", new PdfInteger(3));
            encryptionDictionary.Set("Length", new PdfInteger(_encryption.KeyLengthBytes * 8));
        }
    }

    private void WritePdf(Stream output)
    {
        var writer = new PdfWriter(output)
        {
            Encryption = _encryption,
            Error = Error
        };
        writer.WriteAscii($"%PDF-{_pdfVersion}\n%");
        writer.WriteBytes(stackalloc byte[] { 0xB7, 0xBE, 0xAD, 0xAA });
        writer.WriteAscii("\n");

        var offsets = new long[_objects.Count + 1];

        foreach (var obj in _objects)
        {
            offsets[obj.ObjectNumber] = writer.Position;
            writer.WriteLineAscii($"{obj.ObjectNumber} 0 obj");
            writer.BeginObject(obj.ObjectNumber, 0);
            try
            {
                obj.Value.WriteTo(writer);
            }
            finally
            {
                writer.EndObject();
            }

            writer.WriteAscii("\nendobj\n");
        }

        var xrefOffset = writer.Position;
        writer.WriteLineAscii("xref");
        writer.WriteLineAscii($"0 {_objects.Count + 1}");
        writer.WriteLineAscii("0000000000 65535 f ");

        for (var i = 1; i < offsets.Length; i++)
            writer.WriteLineAscii($"{offsets[i]:D10} 00000 n ");

        writer.WriteLineAscii("trailer");
        var trailer = new PdfDictionary();
        trailer.Set("Size", new PdfInteger(_objects.Count + 1));
        trailer.Set("Root", _catalogObject.Reference);
        trailer.Set("Info", _infoObject.Reference);

        if (_encryption is not null && _encryptionObject is not null)
        {
            trailer.Set("Encrypt", _encryptionObject.Reference);
            trailer.Set("ID", new PdfArray([
                PdfBinary.FromBytes(_encryption.FileId),
                PdfBinary.FromBytes(_encryption.FileId)
            ]));
        }
        else if (_fileId is not null)
        {
            trailer.Set("ID", new PdfArray([
                PdfBinary.FromBytes(_fileId),
                PdfBinary.FromBytes(_fileId)
            ]));
        }

        trailer.WriteTo(writer);
        writer.WriteAscii("\nstartxref\n");
        writer.WriteLineAscii(xrefOffset.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteAscii("%%EOF\n");
    }

    private PdfStreamObject CreateImageStream(byte[] data, int width, int height, PdfColorSpace colorSpace, int bitsPerComponent, PdfObject? colorSpaceObject = null)
    {
        var stream = new PdfStreamObject(data)
        {
            Kind = PdfStreamKind.Image,
            CompressionMode = _compressionMode,
            Subclass = PdfObjectClass.XObject
        };
        stream.Dictionary.SetName("Type", "XObject");
        stream.Dictionary.SetName("Subtype", "Image");
        if (colorSpaceObject is null)
            stream.Dictionary.SetName("ColorSpace", ColorSpaceName(colorSpace));
        else
            stream.Dictionary.Set("ColorSpace", colorSpaceObject);
        stream.Dictionary.Set("Width", new PdfInteger(width));
        stream.Dictionary.Set("Height", new PdfInteger(height));
        stream.Dictionary.Set("BitsPerComponent", new PdfInteger(bitsPerComponent));
        return stream;
    }

    private PdfStreamObject CreateFormXObjectStream(byte[] data, PdfArray bbox, PdfDictionary xobjects)
    {
        var resources = new PdfDictionary();
        resources.Set("ProcSet", new PdfArray([
            new PdfName("PDF"),
            new PdfName("ImageC")
        ]));
        resources.Set("XObject", xobjects);

        var stream = new PdfStreamObject(data)
        {
            Filter = PdfStreamFilter.FlateDecode,
            Subclass = PdfObjectClass.XObject
        };
        stream.Dictionary.SetName("Type", "XObject");
        stream.Dictionary.SetName("Subtype", "Form");
        stream.Dictionary.Set("BBox", bbox);
        stream.Dictionary.Set("Matrix", new PdfArray([
            new PdfInteger(1),
            new PdfInteger(0),
            new PdfInteger(0),
            new PdfInteger(1),
            new PdfInteger(0),
            new PdfInteger(0)
        ]));
        stream.Dictionary.Set("Resources", resources);
        stream.Dictionary.Set("FormType", new PdfInteger(1));
        return stream;
    }

    private void ValidatePagePeer(PdfPage? page)
    {
        if (page is null || !ReferenceEquals(page.Owner, this))
            Throw(HaruStatus.InvalidPage, "Page does not belong to this document.");

        page.ValidateOrThrow();
    }

    private (double Left, double Bottom, double Right, double Top, double Width, double Height) NormalizeXObjectRect(PdfRect rect)
    {
        if (!IsFinite(rect.Left) || !IsFinite(rect.Bottom) || !IsFinite(rect.Right) || !IsFinite(rect.Top))
            Throw(HaruStatus.PageInvalidBoundary, "XObject rectangle coordinates must be finite.");

        var bottom = Math.Min(rect.Bottom, rect.Top);
        var top = Math.Max(rect.Bottom, rect.Top);
        var width = rect.Right - rect.Left;
        var height = top - bottom;

        if (width <= 0 || height <= 0)
            Throw(HaruStatus.PageInvalidBoundary, "XObject rectangle must have positive width and height.");

        return (rect.Left, bottom, rect.Right, top, width, height);
    }

    private PdfObject? CreatePngColorSpaceObject(PngImageData png)
    {
        if (png.ColorSpace == PdfColorSpace.Indexed)
        {
            if (png.IndexedPalette is null)
                return png.ColorSpaceObject;

            return new PdfArray([
                new PdfName("Indexed"),
                CreatePngBaseColorSpace(PdfColorSpace.DeviceRgb, png.ColorManagement),
                new PdfInteger(png.IndexedHighValue),
                PdfBinary.FromBytes(png.IndexedPalette)
            ]);
        }

        return CreatePngBaseColorSpace(png.ColorSpace, png.ColorManagement, allowDeviceFallback: true);
    }

    private PdfObject CreatePngBaseColorSpace(PdfColorSpace colorSpace, PngColorManagementData? colorManagement, bool allowDeviceFallback = false)
    {
        var managed = CreatePngManagedColorSpace(colorSpace, colorManagement);
        if (managed is not null)
            return managed;

        if (allowDeviceFallback)
            return new PdfName(ColorSpaceName(colorSpace));

        return new PdfName("DeviceRGB");
    }

    private PdfObject? CreatePngManagedColorSpace(PdfColorSpace colorSpace, PngColorManagementData? colorManagement)
    {
        if (colorManagement is null)
            return null;

        if (colorManagement.IccProfile is { Length: > 0 } profile)
        {
            var profileStream = new PdfStreamObject(profile)
            {
                Kind = PdfStreamKind.IccProfile,
                CompressionMode = _compressionMode,
                Subclass = PdfObjectClass.IccProfile
            };
            profileStream.Dictionary.Set("N", new PdfInteger(colorSpace == PdfColorSpace.DeviceGray ? 1 : 3));
            var profileObject = AddObject(profileStream);
            return new PdfArray([new PdfName("ICCBased"), profileObject.Reference]);
        }

        if (colorManagement.Gamma is null && colorManagement.Chromaticities is null)
            return null;

        return colorSpace switch
        {
            PdfColorSpace.DeviceGray => CreateCalGrayColorSpace(colorManagement),
            PdfColorSpace.DeviceRgb => CreateCalRgbColorSpace(colorManagement),
            _ => null
        };
    }

    private static PdfArray CreateCalGrayColorSpace(PngColorManagementData colorManagement)
    {
        var dictionary = new PdfDictionary();
        dictionary.Set("WhitePoint", WhitePointArray(colorManagement.Chromaticities));

        if (colorManagement.Gamma is { } gamma)
            dictionary.Set("Gamma", new PdfReal(ToPdfGamma(gamma)));

        return new PdfArray([new PdfName("CalGray"), dictionary]);
    }

    private static PdfArray CreateCalRgbColorSpace(PngColorManagementData colorManagement)
    {
        var dictionary = new PdfDictionary();
        dictionary.Set("WhitePoint", WhitePointArray(colorManagement.Chromaticities));

        if (colorManagement.Gamma is { } gamma)
        {
            var pdfGamma = ToPdfGamma(gamma);
            dictionary.Set("Gamma", new PdfArray([new PdfReal(pdfGamma), new PdfReal(pdfGamma), new PdfReal(pdfGamma)]));
        }

        if (colorManagement.Chromaticities is { } chromaticities)
            dictionary.Set("Matrix", new PdfArray(RgbToXyzMatrixValues(chromaticities).Select(static value => new PdfReal(value))));

        return new PdfArray([new PdfName("CalRGB"), dictionary]);
    }

    private static PdfArray WhitePointArray(PngChromaticities? chromaticities)
    {
        var (x, y) = chromaticities is { } c ? (c.WhiteX, c.WhiteY) : (0.3127, 0.3290);
        return new PdfArray([
            new PdfReal(x / y),
            new PdfReal(1),
            new PdfReal((1 - x - y) / y)
        ]);
    }

    private static double[] RgbToXyzMatrixValues(PngChromaticities chromaticities)
    {
        var xr = chromaticities.RedX / chromaticities.RedY;
        var yr = 1.0;
        var zr = (1 - chromaticities.RedX - chromaticities.RedY) / chromaticities.RedY;
        var xg = chromaticities.GreenX / chromaticities.GreenY;
        var yg = 1.0;
        var zg = (1 - chromaticities.GreenX - chromaticities.GreenY) / chromaticities.GreenY;
        var xb = chromaticities.BlueX / chromaticities.BlueY;
        var yb = 1.0;
        var zb = (1 - chromaticities.BlueX - chromaticities.BlueY) / chromaticities.BlueY;
        var xw = chromaticities.WhiteX / chromaticities.WhiteY;
        var yw = 1.0;
        var zw = (1 - chromaticities.WhiteX - chromaticities.WhiteY) / chromaticities.WhiteY;

        var scales = Solve3x3(
            xr, xg, xb,
            yr, yg, yb,
            zr, zg, zb,
            xw, yw, zw);

        return
        [
            scales[0] * xr, scales[0] * yr, scales[0] * zr,
            scales[1] * xg, scales[1] * yg, scales[1] * zg,
            scales[2] * xb, scales[2] * yb, scales[2] * zb
        ];
    }

    private static double[] Solve3x3(
        double a00, double a01, double a02,
        double a10, double a11, double a12,
        double a20, double a21, double a22,
        double b0, double b1, double b2)
    {
        var determinant = Determinant3x3(a00, a01, a02, a10, a11, a12, a20, a21, a22);
        if (Math.Abs(determinant) < 0.0000001)
            return [1, 1, 1];

        return
        [
            Determinant3x3(b0, a01, a02, b1, a11, a12, b2, a21, a22) / determinant,
            Determinant3x3(a00, b0, a02, a10, b1, a12, a20, b2, a22) / determinant,
            Determinant3x3(a00, a01, b0, a10, a11, b1, a20, a21, b2) / determinant
        ];
    }

    private static double Determinant3x3(
        double a00, double a01, double a02,
        double a10, double a11, double a12,
        double a20, double a21, double a22)
    {
        return a00 * (a11 * a22 - a12 * a21)
            - a01 * (a10 * a22 - a12 * a20)
            + a02 * (a10 * a21 - a11 * a20);
    }

    private static double ToPdfGamma(double pngGamma) => pngGamma <= 0 ? 1 : 1 / pngGamma;

    private PdfImage RegisterImage(PdfStreamObject stream, int width, int height, int bitsPerComponent, PdfColorSpace colorSpace)
    {
        var imageObject = AddObject(stream);
        var image = new PdfImage(this, $"Im{_images.Count + 1}", imageObject, width, height, bitsPerComponent, colorSpace, ColorSpaceName(colorSpace));
        _images.Add(image);
        return image;
    }

    private PdfDictionary CreateExponentialInterpolationFunction(PdfRgbColor startColor, PdfRgbColor endColor)
    {
        var function = new PdfDictionary();
        function.Set("FunctionType", new PdfInteger(2));
        function.Set("Domain", new PdfArray([new PdfInteger(0), new PdfInteger(1)]));
        function.Set("C0", new PdfArray([new PdfReal(startColor.R), new PdfReal(startColor.G), new PdfReal(startColor.B)]));
        function.Set("C1", new PdfArray([new PdfReal(endColor.R), new PdfReal(endColor.G), new PdfReal(endColor.B)]));
        function.Set("N", new PdfReal(1));
        return function;
    }

    private void ValidatePoint(PdfPoint point, string name)
    {
        if (!IsFinite(point.X) || !IsFinite(point.Y))
            Throw(HaruStatus.InvalidParameter, $"{name} coordinates must be finite.");
    }

    private void ValidateNonNegative(double value, string name)
    {
        if (value < 0 || !IsFinite(value))
            Throw(HaruStatus.InvalidParameter, $"{name} must be a non-negative finite number.");
    }

    private void ValidateRgbColor(PdfRgbColor color)
    {
        if (!IsUnit(color.R) || !IsUnit(color.G) || !IsUnit(color.B))
            Throw(HaruStatus.InvalidColorSpace, "RGB color components must be between 0 and 1.");
    }

    private static PdfArray Point3DArray(PdfPoint3D point) =>
        new([new PdfReal(point.X), new PdfReal(point.Y), new PdfReal(point.Z)]);

    private static bool IsUnit(double value) => value is >= 0 and <= 1 && !double.IsNaN(value) && !double.IsInfinity(value);

    private string ColorSpaceName(PdfColorSpace colorSpace) => colorSpace switch
    {
        PdfColorSpace.DeviceGray => "DeviceGray",
        PdfColorSpace.DeviceRgb => "DeviceRGB",
        PdfColorSpace.DeviceCmyk => "DeviceCMYK",
        PdfColorSpace.CalGray => "CalGray",
        PdfColorSpace.CalRgb => "CalRGB",
        PdfColorSpace.Lab => "Lab",
        PdfColorSpace.IccBased => "ICCBased",
        PdfColorSpace.Separation => "Separation",
        PdfColorSpace.DeviceN => "DeviceN",
        PdfColorSpace.Indexed => "Indexed",
        PdfColorSpace.Pattern => "Pattern",
        _ => throw CreateException(HaruStatus.InvalidColorSpace, "Invalid image color space.")
    };

    private void ValidateImageDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            Throw(HaruStatus.InvalidImage, "Image dimensions must be positive.");
    }

    private void ValidateColorSpace(PdfColorSpace colorSpace)
    {
        if (!Enum.IsDefined(colorSpace))
            Throw(HaruStatus.InvalidColorSpace, "Invalid image color space.");
    }

    private int ComponentCount(PdfColorSpace colorSpace)
    {
        return colorSpace switch
        {
            PdfColorSpace.DeviceGray => 1,
            PdfColorSpace.DeviceRgb => 3,
            PdfColorSpace.DeviceCmyk => 4,
            _ => throw CreateException(HaruStatus.InvalidColorSpace, "Raw images require DeviceGray, DeviceRGB, or DeviceCMYK color spaces.")
        };
    }

    private int CheckedImageByteCount(int width, int height, int componentCount, int bitsPerComponent)
    {
        try
        {
            checked
            {
                var samples = width * height * componentCount;
                return (samples * bitsPerComponent + 7) / 8;
            }
        }
        catch (OverflowException)
        {
            throw CreateException(HaruStatus.InvalidImage, "Image dimensions are too large.");
        }
    }

    private JpegHeader ReadJpegHeader(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
            Throw(HaruStatus.InvalidJpegData, "JPEG data must begin with SOI marker.");

        var offset = 2;

        while (offset + 3 < data.Length)
        {
            while (offset < data.Length && data[offset] != 0xFF)
                offset++;

            while (offset < data.Length && data[offset] == 0xFF)
                offset++;

            if (offset >= data.Length)
                break;

            var marker = data[offset++];

            if (marker is 0xD8 or 0xD9)
                continue;

            if (offset + 2 > data.Length)
                Throw(HaruStatus.InvalidJpegData, "JPEG marker length is truncated.");

            var segmentLength = (data[offset] << 8) | data[offset + 1];
            offset += 2;

            if (segmentLength < 2 || offset + segmentLength - 2 > data.Length)
                Throw(HaruStatus.InvalidJpegData, "JPEG segment length is invalid.");

            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC9)
            {
                if (segmentLength < 8)
                    Throw(HaruStatus.InvalidJpegData, "JPEG SOF segment is too short.");

                var precision = data[offset];
                var height = (data[offset + 1] << 8) | data[offset + 2];
                var width = (data[offset + 3] << 8) | data[offset + 4];
                var components = data[offset + 5];
                var colorSpace = components switch
                {
                    1 => PdfColorSpace.DeviceGray,
                    3 => PdfColorSpace.DeviceRgb,
                    4 => PdfColorSpace.DeviceCmyk,
                    _ => throw CreateException(HaruStatus.UnsupportedJpegFormat, "Unsupported JPEG component count.")
                };

                ValidateImageDimensions(width, height);
                return new JpegHeader(width, height, precision, colorSpace);
            }

            offset += segmentLength - 2;
        }

        throw CreateException(HaruStatus.UnsupportedJpegFormat, "JPEG SOF marker was not found.");
    }

    private sealed record PdfCompositeFontBinding(
        PdfFont Font,
        PdfCompositeGlyphMap GlyphMap,
        PdfCompositeFontObjects Objects);

    private sealed record PdfCompositeFontObjects(
        PdfDictionary Descendant,
        PdfStreamObject? CidToGidMapStream,
        PdfStreamObject ToUnicodeStream);

    private readonly record struct JpegHeader(int Width, int Height, int BitsPerComponent, PdfColorSpace ColorSpace);

    internal HaruException CreateException(uint status, string message, uint detail = HaruStatus.NoError)
    {
        Error.RaiseError(status, detail);
        return new HaruException(status, detail, message);
    }

    internal HaruException Propagate(HaruException exception)
    {
        if (Error.ErrorNo != exception.Status || Error.DetailNo != exception.DetailStatus)
            Error.RaiseError(exception.Status, exception.DetailStatus);

        return exception;
    }

    [DoesNotReturn]
    private void Throw(uint status, string message, uint detail = HaruStatus.NoError)
    {
        throw CreateException(status, message, detail);
    }

    private static string PageLayoutName(PdfPageLayout layout) => layout switch
    {
        PdfPageLayout.Single => "SinglePage",
        PdfPageLayout.OneColumn => "OneColumn",
        PdfPageLayout.TwoColumnLeft => "TwoColumnLeft",
        PdfPageLayout.TwoColumnRight => "TwoColumnRight",
        _ => "SinglePage"
    };

    private static string PageModeName(PdfPageMode mode) => mode switch
    {
        PdfPageMode.UseNone => "UseNone",
        PdfPageMode.UseOutline => "UseOutlines",
        PdfPageMode.UseThumbs => "UseThumbs",
        PdfPageMode.FullScreen => "FullScreen",
        PdfPageMode.UseAttachments => "UseAttachments",
        _ => "UseNone"
    };

    internal static string FormatPdfDate(DateTimeOffset value) => PdfDate(value);

    private static string PdfDate(DateTimeOffset value)
    {
        var offset = value.Offset;
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        offset = offset.Duration();
        return $"D:{value:yyyyMMddHHmmss}{sign}{offset.Hours:00}'{offset.Minutes:00}'";
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private string CreatePdfAXmp(PdfPdfAType pdfAType)
    {
        var (part, conformance, revision) = pdfAType switch
        {
            PdfPdfAType.PdfA1A => ("1", "A", string.Empty),
            PdfPdfAType.PdfA1B => ("1", "B", string.Empty),
            PdfPdfAType.PdfA2A => ("2", "A", string.Empty),
            PdfPdfAType.PdfA2B => ("2", "B", string.Empty),
            PdfPdfAType.PdfA2U => ("2", "U", string.Empty),
            PdfPdfAType.PdfA3A => ("3", "A", string.Empty),
            PdfPdfAType.PdfA3B => ("3", "B", string.Empty),
            PdfPdfAType.PdfA3U => ("3", "U", string.Empty),
            PdfPdfAType.PdfA4 => ("4", string.Empty, "2020"),
            PdfPdfAType.PdfA4E => ("4", "E", "2020"),
            PdfPdfAType.PdfA4F => ("4", "F", "2020"),
            _ => ("", "", "")
        };
        var extensions = _pdfAXmpExtensions.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, _pdfAXmpExtensions);
        var revisionElement = string.IsNullOrEmpty(revision)
            ? string.Empty
            : $"<pdfaid:rev>{revision}</pdfaid:rev>";

        return $"""
            <?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>
            <x:xmpmeta xmlns:x='adobe:ns:meta/'>
              <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
                <rdf:Description rdf:about='' xmlns:pdfaid='http://www.aiim.org/pdfa/ns/id/'>
                  <pdfaid:part>{part}</pdfaid:part>
                  <pdfaid:conformance>{conformance}</pdfaid:conformance>
                  {revisionElement}
                </rdf:Description>
                {extensions}
              </rdf:RDF>
            </x:xmpmeta>
            <?xpacket end='w'?>
            """;
    }
}
