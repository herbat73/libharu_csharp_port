namespace LibHaru.Internal;

internal enum PdfFontProgramKind
{
    Type1,
    TrueType,
    OpenTypeCff,
    OpenTypeCffCidKeyed,
    CidType0
}

internal sealed class PdfFontProgram
{
    private readonly IReadOnlyDictionary<int, int> _codeWidths;
    private readonly IReadOnlyDictionary<int, int> _unicodeWidths;
    private readonly Func<int, int>? _unicodeWidthResolver;
    private readonly Func<int, int>? _glyphIdResolver;
    private readonly Func<int, int>? _glyphWidthResolver;
    private readonly Func<int, int>? _glyphCidResolver;
    private readonly IReadOnlyDictionary<int, int> _cidWidthLookup;
    private readonly Func<PdfFontSubsetRequest, PdfFontSubsetData>? _fontFileSubsetBuilder;
    private readonly SortedSet<int> _usedGlyphIds = [0];
    private readonly SortedDictionary<int, int> _usedUnicodeGlyphIds = [];
    private IReadOnlyDictionary<int, int>? _subsetGlyphIds;

    internal PdfFontProgram(
        PdfFontProgramKind kind,
        string baseFont,
        PdfFontDescriptor descriptor,
        IReadOnlyDictionary<int, int>? codeWidths = null,
        IReadOnlyDictionary<int, int>? unicodeWidths = null,
        Func<int, int>? unicodeWidthResolver = null,
        Func<int, int>? glyphIdResolver = null,
        Func<int, int>? glyphWidthResolver = null,
        Func<int, int>? glyphCidResolver = null,
        PdfFontFile? fontFile = null,
        Func<PdfFontSubsetRequest, PdfFontSubsetData>? fontFileSubsetBuilder = null,
        bool isBase14 = false,
        string? cidOrdering = null,
        int cidSupplement = 0,
        IReadOnlyList<CjkCidWidth>? cidWidths = null,
        int cidDefaultWidth = 0,
        int cidVerticalPosition = 880,
        int cidVerticalDisplacement = -1000)
    {
        Kind = kind;
        BaseFont = baseFont;
        Descriptor = descriptor;
        _codeWidths = codeWidths ?? new Dictionary<int, int>();
        _unicodeWidths = unicodeWidths ?? new Dictionary<int, int>();
        _unicodeWidthResolver = unicodeWidthResolver;
        _glyphIdResolver = glyphIdResolver;
        _glyphWidthResolver = glyphWidthResolver;
        _glyphCidResolver = glyphCidResolver;
        FontFile = fontFile;
        _fontFileSubsetBuilder = fontFileSubsetBuilder;
        IsBase14 = isBase14;
        CidOrdering = cidOrdering;
        CidSupplement = cidSupplement;
        CidWidths = cidWidths ?? Array.Empty<CjkCidWidth>();
        CidDefaultWidth = cidDefaultWidth != 0 ? cidDefaultWidth : (descriptor.MissingWidth != 0 ? descriptor.MissingWidth : 1000);
        CidVerticalPosition = cidVerticalPosition;
        CidVerticalDisplacement = cidVerticalDisplacement;
        _cidWidthLookup = CidWidths.Count == 0
            ? new Dictionary<int, int>()
            : CidWidths.ToDictionary(static width => (int)width.Cid, static width => (int)width.Width);
    }

    internal PdfFontProgramKind Kind { get; }

    internal string BaseFont { get; }

    internal PdfFontDescriptor Descriptor { get; }

    internal PdfFontFile? FontFile { get; }

    internal bool IsBase14 { get; }

    internal string? CidOrdering { get; }

    internal int CidSupplement { get; }

    internal IReadOnlyList<CjkCidWidth> CidWidths { get; }

    internal int CidDefaultWidth { get; }

    internal int CidVerticalPosition { get; }

    internal int CidVerticalDisplacement { get; }

    internal bool SupportsCompositeEncoding => _glyphIdResolver is not null && _glyphWidthResolver is not null;

    internal bool UsesFontCidCodes => Kind == PdfFontProgramKind.OpenTypeCffCidKeyed;

    internal PdfIndirectObject? DescriptorObject { get; set; }

    internal PdfIndirectObject? FontFileObject { get; set; }

    internal void MarkGlyphUsed(int glyphId)
    {
        if (_fontFileSubsetBuilder is not null && glyphId >= 0)
            _usedGlyphIds.Add(glyphId);
    }

    internal int MarkUnicodeUsed(int unicode)
    {
        var glyphId = GlyphIdOfUnicode(unicode);
        if (glyphId > 0)
        {
            _usedUnicodeGlyphIds.TryAdd(unicode, glyphId);
            MarkGlyphUsed(glyphId);
        }

        return glyphId;
    }

    internal PdfFontSubsetData? BuildFontFileSubset()
    {
        if (_fontFileSubsetBuilder is null)
            return null;

        var subset = _fontFileSubsetBuilder(new PdfFontSubsetRequest(
            _usedGlyphIds.ToArray(),
            new SortedDictionary<int, int>(_usedUnicodeGlyphIds)));
        _subsetGlyphIds = subset.GlyphIdMap;
        return subset;
    }

    internal int SubsetGlyphIdOfOriginal(int glyphId)
    {
        if (_subsetGlyphIds is not null && _subsetGlyphIds.TryGetValue(glyphId, out var subsetGlyphId))
            return subsetGlyphId;

        return glyphId;
    }

    internal int WidthOfCode(PdfEncoding encoding, byte code)
    {
        var unicode = encoding.ToUnicode(code);

        if (_unicodeWidthResolver is not null)
            return _unicodeWidthResolver(unicode);

        if (_unicodeWidths.TryGetValue(unicode, out var unicodeWidth))
            return unicodeWidth;

        if (_codeWidths.TryGetValue(code, out var codeWidth))
            return codeWidth;

        return Descriptor.MissingWidth != 0 ? Descriptor.MissingWidth : 600;
    }

    internal int WidthOfUnicode(int unicode)
    {
        if (_unicodeWidthResolver is not null)
            return _unicodeWidthResolver(unicode);

        if (_unicodeWidths.TryGetValue(unicode, out var width))
            return width;

        return Descriptor.MissingWidth;
    }

    internal int GlyphIdOfUnicode(int unicode) => _glyphIdResolver?.Invoke(unicode) ?? 0;

    internal int CidOfGlyph(int glyphId) => _glyphCidResolver?.Invoke(glyphId) ?? glyphId;

    internal int WidthOfGlyph(int glyphId)
    {
        if (_glyphWidthResolver is not null)
            return _glyphWidthResolver(glyphId);

        return Descriptor.MissingWidth != 0 ? Descriptor.MissingWidth : 600;
    }

    internal int WidthOfCid(int cid)
    {
        return _cidWidthLookup.TryGetValue(cid, out var width) ? width : CidDefaultWidth;
    }
}

internal sealed record PdfFontSubsetRequest(
    IReadOnlyCollection<int> GlyphIds,
    IReadOnlyDictionary<int, int> UnicodeToGlyphId);

internal sealed record PdfFontSubsetData(
    byte[] Data,
    IReadOnlyDictionary<int, int> GlyphIdMap);

internal sealed class PdfCompositeGlyphMap
{
    private readonly Dictionary<int, int> _cidByUnicode = [];
    private readonly SortedDictionary<int, int> _cidToGlyphId = [];
    private readonly SortedDictionary<PdfCompositeCharCode, int> _codeToUnicode = [];
    private int _nextCid = 1;

    internal IReadOnlyDictionary<int, int> CidToGlyphId => _cidToGlyphId;

    internal IReadOnlyDictionary<PdfCompositeCharCode, int> CodeToUnicode => _codeToUnicode;

    internal int GetOrCreateIdentityCid(int unicode, int glyphId)
    {
        if (glyphId <= 0)
            return 0;

        if (_cidByUnicode.TryGetValue(unicode, out var cid))
            return cid;

        if (_nextCid > ushort.MaxValue)
            return 0;

        cid = _nextCid++;
        _cidByUnicode.Add(unicode, cid);
        Register(cid, glyphId, new PdfCompositeCharCode(cid, 2), unicode);
        return cid;
    }

    internal void Register(int cid, int glyphId, PdfCompositeCharCode code, int unicode)
    {
        if (cid < 0 || cid > ushort.MaxValue)
            return;

        if (cid > 0 && glyphId > 0)
            _cidToGlyphId.TryAdd(cid, glyphId);

        if (unicode > 0)
            _codeToUnicode.TryAdd(code, unicode);
    }
}

internal readonly record struct PdfCompositeCharCode(int Code, int ByteLength) : IComparable<PdfCompositeCharCode>
{
    public int CompareTo(PdfCompositeCharCode other)
    {
        var lengthComparison = ByteLength.CompareTo(other.ByteLength);
        return lengthComparison != 0 ? lengthComparison : Code.CompareTo(other.Code);
    }
}

internal sealed record PdfFontDescriptor(
    string FontName,
    int Flags,
    PdfRect FontBBox,
    int ItalicAngle,
    int Ascent,
    int Descent,
    int CapHeight,
    int XHeight,
    int StemV,
    int MissingWidth);

internal sealed class PdfFontFile
{
    internal PdfFontFile(
        string descriptorKey,
        byte[] data,
        int length1,
        int length2,
        int length3,
        string? subtype = null,
        bool writesLengthEntries = true)
    {
        DescriptorKey = descriptorKey;
        Data = data;
        Length1 = length1;
        Length2 = length2;
        Length3 = length3;
        Subtype = subtype;
        WritesLengthEntries = writesLengthEntries;
    }

    internal string DescriptorKey { get; }

    internal string? Subtype { get; }

    internal bool WritesLengthEntries { get; }

    internal byte[] Data { get; private set; }

    internal int Length1 { get; private set; }

    internal int Length2 { get; private set; }

    internal int Length3 { get; private set; }

    internal void ReplaceData(byte[] data, int length1, int length2, int length3)
    {
        Data = data;
        Length1 = length1;
        Length2 = length2;
        Length3 = length3;
    }
}
