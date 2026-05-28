using System.IO.Compression;
using System.Text;
using LibHaru;
using LibHaru.Internal;
using static LibHaru.HPdf;

public static class CompressionFilters
{
    public static void Test(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, LibHaru.CompressionMode.All);
        HPDF_SetXmpMetadata(pdf, """
            <?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>
            <x:xmpmeta xmlns:x='adobe:ns:meta/'>
              <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
                <rdf:Description rdf:about='' xmlns:pdf='http://ns.adobe.com/pdf/1.3/'>
                  <pdf:Producer>LibHaru managed compression smoke</pdf:Producer>
                </rdf:Description>
              </rdf:RDF>
            </x:xmpmeta>
            <?xpacket end='w'?>
            """);

        var font = HPDF_GetFont(pdf, "Helvetica");
        var page = HPDF_AddPage(pdf);
        HPDF_Page_SetFontAndSize(page, font, 12);
        HPDF_Page_TextOut(page, 40, HPDF_Page_GetHeight(page) - 60, "Compression filters smoke");

        var rawRgb = new byte[]
        {
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 0
        };
        var rawImage = HPDF_LoadRawImageFromMem(pdf, rawRgb, 2, 2, PdfColorSpace.DeviceRgb, 8);
        HPDF_Page_DrawImage(page, rawImage, 40, HPDF_Page_GetHeight(page) - 140, 48, 48);

        var jpegPath = Path.Combine(repoRoot, "demo", "images", "rgb.jpg");
        var jpegImage = HPDF_LoadJpegImageFromFile(pdf, jpegPath);
        HPDF_Page_DrawImage(page, jpegImage, 110, HPDF_Page_GetHeight(page) - 140, 48, 48);

        HPDF_SaveToFile(pdf, pdfPath);

        var bytes = File.ReadAllBytes(pdfPath);
        var latin1 = Encoding.Latin1.GetString(bytes);

        Require(latin1.Contains("/Subtype /Image", StringComparison.Ordinal), "Missing image XObject.");
        Require(latin1.Contains("/Subtype /XML", StringComparison.Ordinal), "Missing metadata XML stream.");
        Require(latin1.Contains("/DCTDecode", StringComparison.Ordinal), "Missing JPEG DCTDecode filter.");
        Require(Count(latin1, "/FlateDecode") >= 3, "Expected Flate filters for page content, raw image, and metadata.");
        Require(Count(latin1, "/Filter [") >= 3, "Stream filters should be emitted as arrays.");
        Require(!latin1.Contains("LibHaru managed compression smoke", StringComparison.Ordinal), "Metadata XML was not compressed.");
        Require(!latin1.Contains("Compression filters smoke", StringComparison.Ordinal), "Page content was not compressed.");

        LowLevelStreamFiltersUseArrayMetadataAndDecodeInOrder();

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"{bytes.Length} bytes with stream filters");
    }

    private static void LowLevelStreamFiltersUseArrayMetadataAndDecodeInOrder()
    {
        var payload = Encoding.ASCII.GetBytes("ASCII wrapper and Flate stream parity");
        var encodedStream = new PdfStreamObject(payload)
        {
            Filter = PdfStreamFilter.FlateDecode | PdfStreamFilter.ASCIIHex | PdfStreamFilter.ASCII85
        };
        var encodedPdf = WriteObjectValue(encodedStream);
        var encodedText = Encoding.Latin1.GetString(encodedPdf);

        Require(
            encodedText.Contains("/Filter [/ASCII85Decode /ASCIIHexDecode /FlateDecode]", StringComparison.Ordinal),
            "Multi-filter stream did not emit filters in decode order.");
        Require(!encodedText.Contains(Encoding.ASCII.GetString(payload), StringComparison.Ordinal), "Filtered stream leaked plaintext.");

        var streamBytes = ExtractStreamBytes(encodedPdf);
        var decoded = Inflate(DecodeAsciiHex(DecodeAscii85(streamBytes)));
        Require(decoded.SequenceEqual(payload), "ASCII85, ASCIIHex, and Flate filters did not round trip.");

        var ccittStream = new PdfStreamObject([0x00])
        {
            Filter = PdfStreamFilter.CcittDecode
        };
        var decodeParms = new PdfDictionary();
        decodeParms.Set("K", new PdfInteger(-1));
        decodeParms.Set("Columns", new PdfInteger(8));
        ccittStream.SetDecodeParms(decodeParms);

        var ccittText = Encoding.Latin1.GetString(WriteObjectValue(ccittStream));
        Require(ccittText.Contains("/Filter [/CCITTFaxDecode]", StringComparison.Ordinal), "CCITT filter was not emitted as an array.");
        Require(ccittText.Contains("/DecodeParms [<<", StringComparison.Ordinal), "DecodeParms was not emitted as an array.");
    }

    private static byte[] WriteObjectValue(PdfObject value)
    {
        using var stream = new MemoryStream();
        var writer = new PdfWriter(stream) { Error = new HaruError() };
        writer.BeginObject(1, 0);
        value.WriteTo(writer);
        writer.EndObject();
        return stream.ToArray();
    }

    private static byte[] ExtractStreamBytes(byte[] pdfBytes)
    {
        var marker = Encoding.ASCII.GetBytes("stream\n");
        var endMarker = Encoding.ASCII.GetBytes("\nendstream");
        var start = IndexOf(pdfBytes, marker);
        Require(start >= 0, "Stream start marker was not found.");
        start += marker.Length;

        var end = IndexOf(pdfBytes, endMarker, start);
        Require(end >= start, "Stream end marker was not found.");

        return pdfBytes[start..end];
    }

    private static int IndexOf(byte[] data, byte[] pattern, int start = 0)
    {
        for (var i = start; i <= data.Length - pattern.Length; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] == pattern[j])
                    continue;

                match = false;
                break;
            }

            if (match)
                return i;
        }

        return -1;
    }

    private static byte[] DecodeAscii85(byte[] data)
    {
        using var output = new MemoryStream();
        Span<byte> group = stackalloc byte[5];
        var groupLength = 0;

        foreach (var value in data)
        {
            if (char.IsWhiteSpace((char)value))
                continue;

            if (value == '~')
                break;

            if (value == 'z' && groupLength == 0)
            {
                output.WriteByte(0);
                output.WriteByte(0);
                output.WriteByte(0);
                output.WriteByte(0);
                continue;
            }

            group[groupLength++] = value;

            if (groupLength == 5)
            {
                WriteAscii85Group(output, group, 4);
                groupLength = 0;
            }
        }

        if (groupLength > 0)
        {
            var outputCount = groupLength - 1;
            for (var i = groupLength; i < 5; i++)
                group[i] = (byte)'u';

            WriteAscii85Group(output, group, outputCount);
        }

        return output.ToArray();
    }

    private static void WriteAscii85Group(Stream output, ReadOnlySpan<byte> group, int outputCount)
    {
        uint tuple = 0;

        for (var i = 0; i < 5; i++)
            tuple = tuple * 85 + (uint)(group[i] - 33);

        Span<byte> decoded = stackalloc byte[4];
        decoded[0] = (byte)(tuple >> 24);
        decoded[1] = (byte)(tuple >> 16);
        decoded[2] = (byte)(tuple >> 8);
        decoded[3] = (byte)tuple;
        output.Write(decoded[..outputCount]);
    }

    private static byte[] DecodeAsciiHex(byte[] data)
    {
        using var output = new MemoryStream();
        int? high = null;

        foreach (var value in data)
        {
            if (value == '>')
                break;

            if (char.IsWhiteSpace((char)value))
                continue;

            var nibble = HexValue(value);
            if (high is null)
            {
                high = nibble;
                continue;
            }

            output.WriteByte((byte)((high.Value << 4) | nibble));
            high = null;
        }

        if (high is not null)
            output.WriteByte((byte)(high.Value << 4));

        return output.ToArray();
    }

    private static int HexValue(byte value)
    {
        if (value is >= (byte)'0' and <= (byte)'9')
            return value - '0';
        if (value is >= (byte)'A' and <= (byte)'F')
            return value - 'A' + 10;
        if (value is >= (byte)'a' and <= (byte)'f')
            return value - 'a' + 10;

        throw new InvalidOperationException("Invalid ASCIIHex digit.");
    }

    private static byte[] Inflate(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var zlib = new ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }

    private static int Count(string value, string needle)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
