using System.Text;
using System.IO.Compression;

namespace LibHaru.Internal;

[Flags]
internal enum PdfObjectClass
{
    Unknown = 0x0001,
    Null = 0x0002,
    Boolean = 0x0003,
    Number = 0x0004,
    Real = 0x0005,
    Name = 0x0006,
    String = 0x0007,
    Binary = 0x0008,
    Array = 0x0010,
    Dictionary = 0x0011,
    Proxy = 0x0012,
    Direct = 0x00A0,
    Any = 0x00FF,

    Font = 0x0100,
    Catalog = 0x0200,
    Pages = 0x0300,
    Page = 0x0400,
    XObject = 0x0500,
    Outline = 0x0600,
    Destination = 0x0700,
    Annotation = 0x0800,
    Encrypt = 0x0900,
    ExtGState = 0x0A00,
    ExtGStateReadOnly = 0x0B00,
    NameDictionary = 0x0C00,
    NameTree = 0x0D00,
    Shading = 0x0E00
}

internal static class PdfObjectLimits
{
    internal const int MaxNameLength = 127;
    internal const int MaxArrayItems = 8_388_607;
    internal const int MaxDictionaryItems = 8_388_607;
}

internal abstract class PdfObject
{
    private object? _directOwner;

    internal abstract PdfObjectClass ObjectClass { get; }

    internal PdfObjectClass BaseClass => ObjectClass & PdfObjectClass.Any;

    internal HaruError? Error { get; private set; }

    internal PdfIndirectObject? IndirectObject { get; private set; }

    internal bool IsDirectObject => _directOwner is not null;

    internal bool IsHidden { get; set; }

    internal void AttachError(HaruError? error)
    {
        Error = error;
        AttachChildErrors(error);
    }

    internal void MarkIndirect(PdfIndirectObject owner)
    {
        if (ReferenceEquals(IndirectObject, owner))
            return;

        if (IsDirectObject || IndirectObject is not null)
            throw CreateException(HaruStatus.InvalidObject, "PDF object is already owned by another container.");

        IndirectObject = owner;
    }

    internal void MarkDirectOwned(object owner)
    {
        if (IsDirectObject)
            throw CreateException(HaruStatus.InvalidObject, "PDF object is already owned by another container.");

        _directOwner = owner;
    }

    internal void WriteTo(PdfWriter writer)
    {
        if (IsHidden)
            return;

        if (!IsKnownClass(BaseClass))
            throw writer.CreateException(HaruStatus.ErrUnknownClass, "Unknown PDF object class.");

        WriteValueTo(writer);
    }

    internal PdfObject ResolveProxy()
    {
        return this is PdfIndirectReference { Target: { } target } ? target : this;
    }

    internal bool MatchesClass(PdfObjectClass expectedClass)
    {
        var resolved = ResolveProxy();
        var expectedBase = expectedClass & PdfObjectClass.Any;
        var expectedSubclass = expectedClass & ~PdfObjectClass.Any;
        var resolvedSubclass = resolved.ObjectClass & ~PdfObjectClass.Any;

        if (expectedBase != 0 && expectedBase != PdfObjectClass.Any && resolved.BaseClass != expectedBase)
            return false;

        if (expectedSubclass != 0 && resolvedSubclass != expectedSubclass)
            return false;

        return true;
    }

    internal void RequireClass(PdfObjectClass expectedClass, uint status, string message)
    {
        if (!MatchesClass(expectedClass))
            throw CreateException(status, message);
    }

    protected virtual void AttachChildErrors(HaruError? error)
    {
    }

    protected abstract void WriteValueTo(PdfWriter writer);

    protected HaruException CreateException(uint status, string message, uint detail = HaruStatus.NoError)
    {
        Error?.RaiseError(status, detail);
        return new HaruException(status, detail, message);
    }

    private static bool IsKnownClass(PdfObjectClass objectClass)
    {
        return objectClass is PdfObjectClass.Null
            or PdfObjectClass.Boolean
            or PdfObjectClass.Number
            or PdfObjectClass.Real
            or PdfObjectClass.Name
            or PdfObjectClass.String
            or PdfObjectClass.Binary
            or PdfObjectClass.Array
            or PdfObjectClass.Dictionary
            or PdfObjectClass.Proxy
            or PdfObjectClass.Direct;
    }
}

internal sealed class PdfNull : PdfObject
{
    internal static readonly PdfNull Value = new();

    private PdfNull()
    {
    }

    internal static PdfNull New() => new();

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Null;

    protected override void WriteValueTo(PdfWriter writer) => writer.WriteAscii("null");
}

internal sealed class PdfBoolean : PdfObject
{
    internal PdfBoolean(bool value)
    {
        Value = value;
    }

    internal bool Value { get; }

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Boolean;

    protected override void WriteValueTo(PdfWriter writer) => writer.WriteAscii(Value ? "true" : "false");
}

internal sealed class PdfInteger : PdfObject
{
    internal PdfInteger(int value)
    {
        Value = value;
    }

    internal int Value { get; }

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Number;

    protected override void WriteValueTo(PdfWriter writer) => writer.WriteAscii(Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
}

internal sealed class PdfReal : PdfObject
{
    internal PdfReal(double value)
    {
        Value = value;
    }

    internal double Value { get; }

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Real;

    protected override void WriteValueTo(PdfWriter writer) => writer.WriteAscii(writer.FormatReal(Value));
}

internal sealed class PdfName : PdfObject
{
    internal PdfName(string value)
        : this(value, null)
    {
    }

    private PdfName(string value, HaruError? error)
    {
        if (string.IsNullOrEmpty(value))
            throw CreateNameException(error, HaruStatus.NameInvalidValue, "PDF name cannot be empty.");

        Value = value[0] == '/' ? value[1..] : value;

        if (string.IsNullOrEmpty(Value))
            throw CreateNameException(error, HaruStatus.NameInvalidValue, "PDF name cannot be empty.");

        if (Encoding.ASCII.GetByteCount(Value) > PdfObjectLimits.MaxNameLength)
            throw CreateNameException(error, HaruStatus.NameOutOfRange, "PDF name is too long.");
    }

    internal string Value { get; }

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Name;

    internal static PdfName Create(string value, HaruError? error) => new(value, error);

    protected override void WriteValueTo(PdfWriter writer)
    {
        writer.WriteAscii("/");
        WriteEscapedName(writer, Value);
    }

    internal static void WriteEscapedName(PdfWriter writer, string value)
    {
        foreach (var ch in Encoding.ASCII.GetBytes(value))
        {
            if (IsRegularNameByte(ch))
            {
                writer.WriteBytes(stackalloc[] { ch });
            }
            else
            {
                writer.WriteAscii("#");
                writer.WriteAscii(ch.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    private static bool IsRegularNameByte(byte value)
    {
        return value is >= 33 and <= 126
            && value is not (byte)'#'
            && value is not (byte)'%'
            && value is not (byte)'('
            && value is not (byte)')'
            && value is not (byte)'<'
            && value is not (byte)'>'
            && value is not (byte)'['
            && value is not (byte)']'
            && value is not (byte)'{'
            && value is not (byte)'}'
            && value is not (byte)'/'
            && value is not (byte)' ';
    }

    private static HaruException CreateNameException(HaruError? error, uint status, string message)
    {
        error?.RaiseError(status);
        return new HaruException(status, message);
    }
}

internal sealed class PdfString : PdfObject
{
    private static readonly Encoding Latin1 = Encoding.Latin1;
    private readonly byte[] _bytes;

    private PdfString(byte[] bytes)
    {
        _bytes = bytes;
    }

    internal static PdfString FromText(string value) => new(Latin1.GetBytes(value));

    internal static PdfString FromBytes(ReadOnlySpan<byte> value) => new(value.ToArray());

    internal override PdfObjectClass ObjectClass => PdfObjectClass.String;

    protected override void WriteValueTo(PdfWriter writer)
    {
        if (writer.ShouldEncryptCurrentObject)
        {
            writer.WriteHexString(writer.EncryptCurrentObject(_bytes));
            return;
        }

        writer.WriteAscii("(");

        foreach (var b in _bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                case (byte)'(':
                case (byte)')':
                    writer.WriteAscii("\\");
                    writer.WriteBytes(stackalloc[] { b });
                    break;
                case 0x08:
                    writer.WriteAscii("\\b");
                    break;
                case 0x09:
                    writer.WriteAscii("\\t");
                    break;
                case 0x0A:
                    writer.WriteAscii("\\n");
                    break;
                case 0x0C:
                    writer.WriteAscii("\\f");
                    break;
                case 0x0D:
                    writer.WriteAscii("\\r");
                    break;
                default:
                    if (b < 0x20 || b > 0x7E)
                    {
                        writer.WriteAscii("\\");
                        writer.WriteAscii(Convert.ToString(b, 8).PadLeft(3, '0'));
                    }
                    else
                    {
                        writer.WriteBytes(stackalloc[] { b });
                    }

                    break;
            }
        }

        writer.WriteAscii(")");
    }
}

internal sealed class PdfBinary : PdfObject
{
    private readonly byte[] _bytes;

    private PdfBinary(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes.ToArray();
    }

    internal static PdfBinary FromBytes(ReadOnlySpan<byte> bytes) => new(bytes);

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Binary;

    protected override void WriteValueTo(PdfWriter writer)
    {
        var bytes = writer.ShouldEncryptCurrentObject ? writer.EncryptCurrentObject(_bytes) : _bytes;

        writer.WriteHexString(bytes);
    }
}

internal sealed class PdfDirectObject : PdfObject
{
    private readonly byte[] _bytes;

    private PdfDirectObject(ReadOnlySpan<byte> bytes)
    {
        _bytes = bytes.ToArray();
    }

    internal static PdfDirectObject FromAscii(string value) => new(Encoding.ASCII.GetBytes(value));

    internal static PdfDirectObject FromBytes(ReadOnlySpan<byte> value) => new(value);

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Direct;

    protected override void WriteValueTo(PdfWriter writer) => writer.WriteBytes(_bytes);
}

internal sealed class PdfArray : PdfObject
{
    private readonly List<PdfObject> _items = [];

    internal PdfArray()
    {
    }

    internal PdfArray(IEnumerable<PdfObject> items)
    {
        foreach (var item in items)
            Add(item);
    }

    internal int Count => _items.Count;

    internal PdfObjectClass Subclass { get; set; }

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Array | Subclass;

    internal void Add(PdfObject value)
    {
        if (_items.Count >= PdfObjectLimits.MaxArrayItems)
            throw CreateException(HaruStatus.ArrayCountErr, "PDF array item count exceeded the Haru limit.");

        _items.Add(PrepareCollectionValue(value));
    }

    internal void Add(PdfIndirectObject value)
    {
        if (value is null)
            throw CreateException(HaruStatus.InvalidObject, "PDF array item cannot be null.");

        Add(value.Value);
    }

    internal void AddNumber(int value) => Add(new PdfInteger(value));

    internal void AddReal(double value) => Add(new PdfReal(value));

    internal void AddNull() => Add(PdfNull.New());

    internal void AddName(string value) => Add(PdfName.Create(value, Error));

    internal void Insert(int index, PdfObject value)
    {
        if ((uint)index > (uint)_items.Count)
            throw CreateException(HaruStatus.ArrayItemNotFound, "PDF array insertion index is out of range.");

        if (_items.Count >= PdfObjectLimits.MaxArrayItems)
            throw CreateException(HaruStatus.ArrayCountErr, "PDF array item count exceeded the Haru limit.");

        _items.Insert(index, PrepareCollectionValue(value));
    }

    internal T GetItem<T>(int index)
        where T : PdfObject
    {
        var expectedClass = PdfObjectClassOf<T>();
        var item = GetItem(index, expectedClass);

        if (item is T typed)
            return typed;

        throw CreateException(HaruStatus.ArrayItemUnexpectedType, "PDF array item has an unexpected object type.");
    }

    internal PdfObject GetItem(int index, PdfObjectClass expectedClass)
    {
        if ((uint)index >= (uint)_items.Count)
            throw CreateException(HaruStatus.ArrayItemNotFound, "PDF array item was not found.");

        var item = _items[index].ResolveProxy();

        if (!item.MatchesClass(expectedClass))
            throw CreateException(HaruStatus.ArrayItemUnexpectedType, "PDF array item has an unexpected object class.");

        return item;
    }

    internal void Clear() => _items.Clear();

    protected override void AttachChildErrors(HaruError? error)
    {
        foreach (var item in _items)
            item.AttachError(error);
    }

    protected override void WriteValueTo(PdfWriter writer)
    {
        writer.WriteAscii("[");

        for (var i = 0; i < _items.Count; i++)
        {
            if (i > 0)
                writer.WriteAscii(" ");

            _items[i].WriteTo(writer);
        }

        writer.WriteAscii("]");
    }

    private PdfObject PrepareCollectionValue(PdfObject? value)
    {
        if (value is null)
            throw CreateException(HaruStatus.InvalidObject, "PDF array item cannot be null.");

        if (value.IsDirectObject)
            throw CreateException(HaruStatus.InvalidObject, "PDF object is already owned by another container.");

        if (value.IndirectObject is { } indirectObject)
        {
            var proxy = indirectObject.Reference;
            proxy.AttachError(Error);
            proxy.MarkDirectOwned(this);
            return proxy;
        }

        value.AttachError(Error);
        value.MarkDirectOwned(this);
        return value;
    }

    internal static PdfObjectClass PdfObjectClassOf<T>()
        where T : PdfObject
    {
        if (typeof(T) == typeof(PdfArray))
            return PdfObjectClass.Array;
        if (typeof(T) == typeof(PdfDictionary) || typeof(T) == typeof(PdfStreamObject))
            return PdfObjectClass.Dictionary;
        if (typeof(T) == typeof(PdfName))
            return PdfObjectClass.Name;
        if (typeof(T) == typeof(PdfString))
            return PdfObjectClass.String;
        if (typeof(T) == typeof(PdfBinary))
            return PdfObjectClass.Binary;
        if (typeof(T) == typeof(PdfInteger))
            return PdfObjectClass.Number;
        if (typeof(T) == typeof(PdfReal))
            return PdfObjectClass.Real;
        if (typeof(T) == typeof(PdfBoolean))
            return PdfObjectClass.Boolean;
        if (typeof(T) == typeof(PdfNull))
            return PdfObjectClass.Null;
        if (typeof(T) == typeof(PdfDirectObject))
            return PdfObjectClass.Direct;

        return PdfObjectClass.Any;
    }
}

internal sealed class PdfDictionary : PdfObject
{
    private readonly List<KeyValuePair<string, PdfObject>> _items = [];
    private PdfObjectClass _subclass;

    internal int Count => _items.Count;

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Dictionary | _subclass;

    internal PdfObjectClass Subclass
    {
        get => _subclass;
        set => _subclass = value;
    }

    internal bool IsEncryptionDictionary
    {
        get => _subclass == PdfObjectClass.Encrypt;
        set
        {
            if (value)
                _subclass = PdfObjectClass.Encrypt;
            else if (_subclass == PdfObjectClass.Encrypt)
                _subclass = 0;
        }
    }

    internal void Set(string key, PdfObject value)
    {
        ValidateKey(key);

        for (var i = 0; i < _items.Count; i++)
        {
            if (StringComparer.Ordinal.Equals(_items[i].Key, key))
            {
                if (ReferenceEquals(_items[i].Value.ResolveProxy(), value.ResolveProxy()))
                    return;

                _items[i] = new KeyValuePair<string, PdfObject>(key, PrepareCollectionValue(value));
                return;
            }
        }

        if (_items.Count >= PdfObjectLimits.MaxDictionaryItems)
            throw CreateException(HaruStatus.DictCountErr, "PDF dictionary item count exceeded the Haru limit.");

        _items.Add(new KeyValuePair<string, PdfObject>(key, PrepareCollectionValue(value)));
    }

    internal void Set(string key, PdfIndirectObject value)
    {
        if (value is null)
            throw CreateException(HaruStatus.InvalidObject, "PDF dictionary value cannot be null.");

        Set(key, value.Value);
    }

    internal bool Remove(string key)
    {
        ValidateKey(key);

        for (var i = 0; i < _items.Count; i++)
        {
            if (StringComparer.Ordinal.Equals(_items[i].Key, key))
            {
                _items.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    internal void Clear() => _items.Clear();

    internal void SetName(string key, string value) => Set(key, PdfName.Create(value, Error));

    internal T? Get<T>(string key)
        where T : PdfObject
    {
        var value = GetItem(key, PdfArray.PdfObjectClassOf<T>());

        if (value is null)
            return null;

        if (value is T typed)
            return typed;

        throw CreateException(HaruStatus.DictItemUnexpectedType, "PDF dictionary item has an unexpected object type.");
    }

    internal PdfObject? GetItem(string key, PdfObjectClass expectedClass)
    {
        ValidateKey(key);

        foreach (var item in _items)
        {
            if (!StringComparer.Ordinal.Equals(item.Key, key))
                continue;

            var value = item.Value.ResolveProxy();
            if (!value.MatchesClass(expectedClass))
                throw CreateException(HaruStatus.DictItemUnexpectedType, "PDF dictionary item has an unexpected object class.");

            return value;
        }

        return null;
    }

    internal string? GetKeyByValue(PdfObject value)
    {
        var target = value.ResolveProxy();

        foreach (var item in _items)
        {
            if (ReferenceEquals(item.Value.ResolveProxy(), target))
                return item.Key;
        }

        return null;
    }

    protected override void AttachChildErrors(HaruError? error)
    {
        foreach (var item in _items)
            item.Value.AttachError(error);
    }

    protected override void WriteValueTo(PdfWriter writer)
    {
        if (!IsEncryptionDictionary)
        {
            WriteDictionaryBody(writer);
            return;
        }

        using (writer.SuppressEncryption())
            WriteDictionaryBody(writer);
    }

    private void WriteDictionaryBody(PdfWriter writer)
    {
        writer.WriteAscii("<<");

        if (_items.Count > 0)
            writer.WriteAscii("\n");

        foreach (var item in _items)
        {
            if (item.Value.IsHidden)
                continue;

            writer.WriteAscii("/");
            PdfName.WriteEscapedName(writer, item.Key);
            writer.WriteAscii(" ");
            item.Value.WriteTo(writer);
            writer.WriteAscii("\n");
        }

        writer.WriteAscii(">>");
    }

    private PdfObject PrepareCollectionValue(PdfObject? value)
    {
        if (value is null)
            throw CreateException(HaruStatus.InvalidObject, "PDF dictionary value cannot be null.");

        if (value.IsDirectObject)
            throw CreateException(HaruStatus.InvalidObject, "PDF object is already owned by another container.");

        if (value.IndirectObject is { } indirectObject)
        {
            var proxy = indirectObject.Reference;
            proxy.AttachError(Error);
            proxy.MarkDirectOwned(this);
            return proxy;
        }

        value.AttachError(Error);
        value.MarkDirectOwned(this);
        return value;
    }

    private void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw CreateException(HaruStatus.InvalidObject, "PDF dictionary key cannot be empty.");

        if (Encoding.ASCII.GetByteCount(key) > PdfObjectLimits.MaxNameLength)
            throw CreateException(HaruStatus.NameOutOfRange, "PDF dictionary key is too long.");
    }
}

internal sealed class PdfStreamObject : PdfObject
{
    private byte[] _data;

    internal PdfStreamObject(byte[] data)
    {
        _data = data;
        Dictionary = new PdfDictionary();
    }

    internal PdfDictionary Dictionary { get; }

    internal PdfStreamFilter Filter { get; set; }

    internal PdfObject? DecodeParms { get; private set; }

    internal PdfStreamKind Kind { get; set; }

    internal CompressionMode CompressionMode { get; set; }

    internal PdfObjectClass Subclass { get; set; }

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Dictionary | Subclass;

    internal void SetData(byte[] data) => _data = data;

    internal void SetDecodeParms(PdfObject? decodeParms)
    {
        DecodeParms = decodeParms is null or PdfArray
            ? decodeParms
            : new PdfArray([decodeParms]);
    }

    protected override void AttachChildErrors(HaruError? error) => Dictionary.AttachError(error);

    protected override void WriteValueTo(PdfWriter writer)
    {
        var filter = ResolveFilter();
        var data = ApplyEncodingFilters(filter, _data);
        data = writer.ShouldEncryptCurrentObject ? writer.EncryptCurrentObject(data) : data;

        WriteFilterDictionaryEntries(filter);

        Dictionary.Set("Length", new PdfInteger(data.Length));
        Dictionary.WriteTo(writer);
        writer.WriteAscii("\nstream\n");
        writer.WriteBytes(data);
        writer.WriteAscii("\nendstream");
    }

    private PdfStreamFilter ResolveFilter()
    {
        var filter = Filter;

        if (Kind == PdfStreamKind.PageContent && CompressionMode.HasFlag(CompressionMode.Text))
            filter |= PdfStreamFilter.FlateDecode;

        if (Kind == PdfStreamKind.Image && CompressionMode.HasFlag(CompressionMode.Image))
            filter |= PdfStreamFilter.FlateDecode;

        if (Kind == PdfStreamKind.Metadata && CompressionMode.HasFlag(CompressionMode.Metadata))
            filter |= PdfStreamFilter.FlateDecode;

        if (Kind == PdfStreamKind.Font && CompressionMode.HasFlag(CompressionMode.Metadata))
            filter |= PdfStreamFilter.FlateDecode;

        if (Kind is PdfStreamKind.EmbeddedFile or PdfStreamKind.JavaScript or PdfStreamKind.IccProfile)
            filter |= PdfStreamFilter.FlateDecode;

        if ((filter & (PdfStreamFilter.DctDecode | PdfStreamFilter.CcittDecode)) != 0)
            filter &= ~PdfStreamFilter.FlateDecode;

        return filter;
    }

    private byte[] ApplyEncodingFilters(PdfStreamFilter filter, byte[] data)
    {
        if (data.Length == 0)
            return data;

        if (filter.HasFlag(PdfStreamFilter.FlateDecode))
            data = Compress(data);

        if (filter.HasFlag(PdfStreamFilter.ASCIIHex))
            data = EncodeAsciiHex(data);

        if (filter.HasFlag(PdfStreamFilter.ASCII85))
            data = EncodeAscii85(data);

        return data;
    }

    private void WriteFilterDictionaryEntries(PdfStreamFilter filter)
    {
        var filters = new List<string>();

        if (filter.HasFlag(PdfStreamFilter.ASCII85))
            filters.Add("ASCII85Decode");

        if (filter.HasFlag(PdfStreamFilter.ASCIIHex))
            filters.Add("ASCIIHexDecode");

        if (filter.HasFlag(PdfStreamFilter.DctDecode))
            filters.Add("DCTDecode");

        if (filter.HasFlag(PdfStreamFilter.CcittDecode))
            filters.Add("CCITTFaxDecode");

        if (filter.HasFlag(PdfStreamFilter.FlateDecode))
            filters.Add("FlateDecode");

        if (filters.Count == 0)
        {
            Dictionary.Remove("Filter");
            Dictionary.Remove("DecodeParms");
            return;
        }

        Dictionary.Set("Filter", new PdfArray(filters.Select(static name => new PdfName(name))));

        if (DecodeParms is not null)
            Dictionary.Set("DecodeParms", DecodeParms);
        else
            Dictionary.Remove("DecodeParms");
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();

        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(data, 0, data.Length);

        return output.ToArray();
    }

    private static byte[] EncodeAsciiHex(byte[] data)
    {
        const string hex = "0123456789ABCDEF";
        var output = new byte[data.Length * 2 + 1];
        var index = 0;

        foreach (var b in data)
        {
            output[index++] = (byte)hex[b >> 4];
            output[index++] = (byte)hex[b & 0x0F];
        }

        output[index] = (byte)'>';
        return output;
    }

    private static byte[] EncodeAscii85(byte[] data)
    {
        using var output = new MemoryStream();
        Span<byte> encoded = stackalloc byte[5];

        for (var i = 0; i < data.Length; i += 4)
        {
            var remaining = Math.Min(4, data.Length - i);
            uint tuple = 0;

            for (var j = 0; j < 4; j++)
            {
                tuple <<= 8;
                if (j < remaining)
                    tuple |= data[i + j];
            }

            if (remaining == 4 && tuple == 0)
            {
                output.WriteByte((byte)'z');
                continue;
            }

            for (var j = 4; j >= 0; j--)
            {
                encoded[j] = (byte)(tuple % 85 + 33);
                tuple /= 85;
            }

            output.Write(encoded[..(remaining + 1)]);
        }

        output.WriteByte((byte)'~');
        output.WriteByte((byte)'>');
        return output.ToArray();
    }
}

internal sealed class PdfIndirectReference : PdfObject
{
    private readonly PdfIndirectObject? _targetObject;

    internal PdfIndirectReference(int objectNumber, int generationNumber = 0)
    {
        ObjectNumber = objectNumber;
        GenerationNumber = generationNumber;
    }

    internal PdfIndirectReference(PdfIndirectObject targetObject)
        : this(targetObject.ObjectNumber, targetObject.GenerationNumber)
    {
        _targetObject = targetObject;
        AttachError(targetObject.Error);
    }

    internal int ObjectNumber { get; }

    internal int GenerationNumber { get; }

    internal PdfObject? Target => _targetObject?.Value;

    internal override PdfObjectClass ObjectClass => PdfObjectClass.Proxy;

    protected override void WriteValueTo(PdfWriter writer)
    {
        writer.WriteAscii(ObjectNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteAscii(" ");
        writer.WriteAscii(GenerationNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteAscii(" R");
    }
}

internal sealed class PdfIndirectObject
{
    private PdfObject _value;

    internal PdfIndirectObject(int objectNumber, PdfObject value)
    {
        ObjectNumber = objectNumber;
        GenerationNumber = 0;
        _value = value;
        Value = value;
    }

    internal int ObjectNumber { get; }

    internal int GenerationNumber { get; }

    internal HaruError? Error { get; private set; }

    internal PdfObject Value
    {
        get => _value;
        set
        {
            if (value is null)
                throw CreateException(HaruStatus.InvalidObject, "Indirect object value cannot be null.");

            value.AttachError(Error);
            value.MarkIndirect(this);
            _value = value;
        }
    }

    internal PdfIndirectReference Reference => new(this);

    internal void AttachError(HaruError? error)
    {
        Error = error;
        _value.AttachError(error);
    }

    private HaruException CreateException(uint status, string message, uint detail = HaruStatus.NoError)
    {
        Error?.RaiseError(status, detail);
        return new HaruException(status, detail, message);
    }
}
