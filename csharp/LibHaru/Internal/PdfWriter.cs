using System.Globalization;
using System.Text;

namespace LibHaru.Internal;

internal sealed class PdfWriter
{
    private static readonly Encoding Ascii = Encoding.ASCII;
    private readonly Stream _stream;
    private int? _currentObjectNumber;
    private int _currentGenerationNumber;
    private int _encryptionSuppressionDepth;

    internal PdfWriter(Stream stream)
    {
        _stream = stream;
    }

    internal long Position => _stream.Position;

    internal PdfEncryption? Encryption { get; set; }

    internal HaruError? Error { get; set; }

    internal bool ShouldEncryptCurrentObject =>
        Encryption is not null && _currentObjectNumber.HasValue && _encryptionSuppressionDepth == 0;

    internal void BeginObject(int objectNumber, int generationNumber)
    {
        _currentObjectNumber = objectNumber;
        _currentGenerationNumber = generationNumber;
    }

    internal void EndObject()
    {
        _currentObjectNumber = null;
        _currentGenerationNumber = 0;
    }

    internal IDisposable SuppressEncryption()
    {
        _encryptionSuppressionDepth++;
        return new EncryptionSuppression(this);
    }

    internal byte[] EncryptCurrentObject(ReadOnlySpan<byte> data)
    {
        if (!ShouldEncryptCurrentObject || Encryption is null)
            return data.ToArray();

        return Encryption.EncryptObjectData(_currentObjectNumber!.Value, _currentGenerationNumber, data);
    }

    internal void WriteAscii(string value)
    {
        var bytes = Ascii.GetBytes(value);
        _stream.Write(bytes, 0, bytes.Length);
    }

    internal void WriteLineAscii(string value)
    {
        WriteAscii(value);
        WriteAscii("\n");
    }

    internal void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        _stream.Write(bytes);
    }

    internal void WriteHexBytes(ReadOnlySpan<byte> bytes)
    {
        const string hex = "0123456789ABCDEF";

        foreach (var b in bytes)
        {
            WriteAscii(hex[b >> 4].ToString());
            WriteAscii(hex[b & 0x0F].ToString());
        }
    }

    internal void WriteHexString(ReadOnlySpan<byte> bytes)
    {
        WriteAscii("<");
        WriteHexBytes(bytes);
        WriteAscii(">");
    }

    internal string FormatReal(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw CreateException(HaruStatus.RealOutOfRange, "PDF numbers must be finite.");

        return FormatNumber(value);
    }

    internal static string FormatNumber(double value)
    {
        if (Math.Abs(value) < 0.0000001)
            value = 0;

        return value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    internal HaruException CreateException(uint status, string message, uint detail = HaruStatus.NoError)
    {
        Error?.RaiseError(status, detail);
        return new HaruException(status, detail, message);
    }

    private sealed class EncryptionSuppression : IDisposable
    {
        private PdfWriter? _writer;

        internal EncryptionSuppression(PdfWriter writer)
        {
            _writer = writer;
        }

        public void Dispose()
        {
            if (_writer is null)
                return;

            _writer._encryptionSuppressionDepth--;
            _writer = null;
        }
    }
}
