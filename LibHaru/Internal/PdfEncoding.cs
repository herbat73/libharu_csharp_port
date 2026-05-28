using System.Text;

namespace LibHaru.Internal;

internal sealed class PdfEncoding
{
    private readonly char[] _codeToUnicode;
    private readonly Dictionary<char, byte> _reverseMap;

    private PdfEncoding(string name, char[] codeToUnicode, bool isComposite = false, string? pdfName = null,
        bool preservesInputBytes = false)
    {
        Name = name;
        PdfName = pdfName ?? name;
        IsComposite = isComposite;
        PreservesInputBytes = preservesInputBytes;
        _codeToUnicode = codeToUnicode;
        _reverseMap = new Dictionary<char, byte>();

        for (var i = 0; i < codeToUnicode.Length; i++)
        {
            var ch = codeToUnicode[i];
            _reverseMap.TryAdd(ch, (byte)i);
        }
    }

    internal string Name { get; }

    internal string PdfName { get; }

    internal bool IsComposite { get; }

    internal bool PreservesInputBytes { get; }

    internal int FirstChar => 32;

    internal int LastChar => 255;

    internal PdfEncoderType EncoderType => IsComposite ? PdfEncoderType.DoubleByte : PdfEncoderType.SingleByte;

    internal PdfWritingMode WritingMode =>
        Name.EndsWith("-V", StringComparison.Ordinal) || PdfName.EndsWith("-V", StringComparison.Ordinal)
            ? PdfWritingMode.Vertical
            : PdfWritingMode.Horizontal;

    internal static PdfEncoding Get(string name, HaruError? error = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw CreateException(error, HaruStatus.InvalidEncodingName, "Encoding name cannot be empty.");

        if (SingleByteEncodingData.TryGetMap(name, out var singleByteMap))
            return new PdfEncoding(name, singleByteMap);

        return name switch
        {
            "Identity-H" => new PdfEncoding(name, IdentityMap(), true, "Identity-H"),
            "Identity-V" => new PdfEncoding(name, IdentityMap(), true, "Identity-V"),
            "UTF-8" => new PdfEncoding(name, IdentityMap(), true, "Identity-H"),
            "UTF8" => new PdfEncoding("UTF-8", IdentityMap(), true, "Identity-H"),
            "UTF-16BE" => new PdfEncoding(name, IdentityMap(), true, "Identity-H"),
            "90ms-RKSJ-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "90ms-RKSJ-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "90msp-RKSJ-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "90msp-RKSJ-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "EUC-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "EUC-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "GB-EUC-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "GB-EUC-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "GBK-EUC-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "GBK-EUC-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "ETen-B5-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "ETen-B5-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "KSCms-UHC-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "KSCms-UHC-HW-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "KSCms-UHC-HW-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "KSC-EUC-H" => new PdfEncoding(name, IdentityMap(), true, name, true),
            "KSC-EUC-V" => new PdfEncoding(name, IdentityMap(), true, name, true),
            _ => throw CreateException(error, HaruStatus.InvalidEncodingName, $"Unsupported encoding: {name}.")
        };
    }

    internal byte[] EncodeText(string text)
    {
        var bytes = new byte[text.Length];

        for (var i = 0; i < text.Length; i++)
            bytes[i] = EncodeChar(text[i]);

        return bytes;
    }

    internal byte EncodeChar(char ch)
    {
        if (_reverseMap.TryGetValue(ch, out var code))
            return code;

        return (byte)'?';
    }

    internal int ToUnicode(byte code)
    {
        return _codeToUnicode[code];
    }

    internal ushort GetUnicode(ushort code)
    {
        if (!IsComposite)
            return code <= byte.MaxValue ? _codeToUnicode[code] : (ushort)0;

        if (PreservesInputBytes)
            return CjkCMapData.ToUnicode(Name, code);

        return code;
    }

    internal ushort ToCid(ushort code)
    {
        return PreservesInputBytes ? CjkCMapData.ToCid(Name, code) : code;
    }

    internal PdfByteType GetByteType(string text, uint index)
    {
        if (EncoderType != PdfEncoderType.DoubleByte)
            return PdfByteType.Single;

        var bytes = BytesForByteTypeInspection(text);
        if (index >= bytes.Length)
            return PdfByteType.Unknown;

        var offset = (int)index;
        if (Name is "UTF-8" or "UTF8")
            return GetUtf8ByteType(bytes, offset);

        if (Name is "Identity-H" or "Identity-V" or "UTF-16BE")
            return offset % 2 == 0 ? PdfByteType.Lead : PdfByteType.Trail;

        return GetCMapByteType(bytes, offset);
    }

    private byte[] BytesForByteTypeInspection(string text)
    {
        if (Name is "UTF-8" or "UTF8")
            return Encoding.UTF8.GetBytes(text);

        return Encoding.Latin1.GetBytes(text);
    }

    private static PdfByteType GetUtf8ByteType(byte[] bytes, int offset)
    {
        for (var i = 0; i < bytes.Length;)
        {
            var count = Utf8SequenceLength(bytes[i]);
            if (count <= 1 || i + count > bytes.Length)
            {
                if (i == offset)
                    return PdfByteType.Single;

                i++;
                continue;
            }

            if (offset >= i && offset < i + count)
                return offset == i + count - 1 ? PdfByteType.Single : PdfByteType.Trail;

            i += count;
        }

        return PdfByteType.Unknown;
    }

    private static int Utf8SequenceLength(byte value)
    {
        if ((value & 0x80) == 0)
            return 1;

        if ((value & 0xF8) == 0xF0)
            return 4;

        if ((value & 0xF0) == 0xE0)
            return 3;

        if ((value & 0xE0) == 0xC0)
            return 2;

        return 1;
    }

    private PdfByteType GetCMapByteType(byte[] bytes, int offset)
    {
        var byteType = PdfByteType.Single;

        for (var i = 0; i <= offset; i++)
        {
            var value = bytes[i];
            if (byteType == PdfByteType.Lead)
            {
                byteType = IsTrailByte(value) ? PdfByteType.Trail : PdfByteType.Unknown;
                continue;
            }

            byteType = IsLeadByte(value) ? PdfByteType.Lead : PdfByteType.Single;
        }

        return byteType;
    }

    private bool IsLeadByte(byte value)
    {
        if (Name.Contains("RKSJ", StringComparison.Ordinal))
            return value is >= 0x81 and <= 0x9F or >= 0xE0 and <= 0xFC;

        if (Name is "EUC-H" or "EUC-V")
            return value is >= 0xA1 and <= 0xFE or 0x8E;

        if (Name.StartsWith("KSCms-UHC", StringComparison.Ordinal)
            || Name.StartsWith("GBK-EUC", StringComparison.Ordinal)
            || Name.StartsWith("ETen-B5", StringComparison.Ordinal))
            return value is >= 0x81 and <= 0xFE;

        if (Name.StartsWith("KSC-EUC", StringComparison.Ordinal)
            || Name.StartsWith("GB-EUC", StringComparison.Ordinal))
            return value is >= 0xA1 and <= 0xFE;

        return false;
    }

    private bool IsTrailByte(byte value)
    {
        if (Name.Contains("RKSJ", StringComparison.Ordinal))
            return value is >= 0x40 and <= 0x7E or >= 0x80 and <= 0xFC;

        if (Name is "EUC-H" or "EUC-V" or "KSC-EUC-H" or "KSC-EUC-V")
            return value is >= 0xA0 and <= 0xFE;

        if (Name.StartsWith("GB-EUC", StringComparison.Ordinal))
            return value is >= 0xA1 and <= 0xFE;

        if (Name.StartsWith("KSCms-UHC", StringComparison.Ordinal))
            return value is >= 0x41 and <= 0xFE;

        if (Name.StartsWith("GBK-EUC", StringComparison.Ordinal)
            || Name.StartsWith("ETen-B5", StringComparison.Ordinal))
            return value is >= 0x40 and <= 0xFE;

        return false;
    }

    private static char[] IdentityMap()
    {
        var map = new char[256];

        for (var i = 0; i < map.Length; i++)
            map[i] = (char)i;

        return map;
    }

    private static HaruException CreateException(HaruError? error, uint status, string message)
    {
        error?.RaiseError(status);
        return new HaruException(status, message);
    }
}