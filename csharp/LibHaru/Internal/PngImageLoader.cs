using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace LibHaru.Internal;

internal static class PngImageLoader
{
    private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly Adam7Pass[] Adam7Passes =
    [
        new(0, 0, 8, 8),
        new(4, 0, 8, 8),
        new(0, 4, 4, 8),
        new(2, 0, 4, 4),
        new(0, 2, 2, 4),
        new(1, 0, 2, 2),
        new(0, 1, 1, 2)
    ];

    internal static PngImageData Load(byte[] data, PdfDocument owner)
    {
        if (data.Length < Signature.Length || !data.AsSpan(0, Signature.Length).SequenceEqual(Signature))
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG data must begin with a PNG signature.");

        PngHeader? header = null;
        byte[]? palette = null;
        byte[]? transparency = null;
        PngColorManagementData? colorManagement = null;
        using var idat = new MemoryStream();
        var offset = Signature.Length;
        var sawEnd = false;
        var sawIdat = false;
        var idatClosed = false;
        var sawPalette = false;
        var sawTransparency = false;
        var sawGamma = false;
        var sawChromaticities = false;
        var sawSrgb = false;
        var sawIccProfile = false;

        while (offset < data.Length)
        {
            if (offset + 8 > data.Length)
                throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG chunk header is truncated.");

            var length = ReadInt32(data, offset);
            var type = Encoding.ASCII.GetString(data, offset + 4, 4);
            var chunkOffset = offset + 8;
            var nextOffset = checked(chunkOffset + length + 4);

            if (length < 0 || nextOffset > data.Length)
                throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG chunk data is truncated.");

            var chunk = data.AsSpan(chunkOffset, length);
            ValidateChunkCrc(data, offset, length, owner);

            switch (type)
            {
                case "IHDR":
                    if (header is not null || length != 13 || offset != Signature.Length)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG IHDR chunk is invalid.");

                    header = ReadHeader(chunk, owner);
                    break;
                case "PLTE":
                    EnsureHeader(header, owner);
                    if (sawPalette || sawIdat)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG PLTE chunk is out of order.");

                    if (length == 0 || length % 3 != 0 || length > 768)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG PLTE chunk is invalid.");

                    sawPalette = true;
                    palette = chunk.ToArray();
                    break;
                case "tRNS":
                    EnsureHeader(header, owner);
                    if (sawTransparency || sawIdat)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG tRNS chunk is out of order.");

                    sawTransparency = true;
                    transparency = chunk.ToArray();
                    break;
                case "gAMA":
                    EnsureHeader(header, owner);
                    if (sawGamma || sawPalette || sawIdat || sawSrgb || sawIccProfile || length != 4)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG gAMA chunk is invalid or out of order.");

                    sawGamma = true;
                    var gamma = ReadUInt32(chunk, 0);
                    if (gamma == 0)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG gAMA value is invalid.");

                    colorManagement = (colorManagement ?? PngColorManagementData.Empty) with
                    {
                        Gamma = gamma / 100000.0
                    };
                    break;
                case "cHRM":
                    EnsureHeader(header, owner);
                    if (sawChromaticities || sawPalette || sawIdat || sawSrgb || sawIccProfile || length != 32)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG cHRM chunk is invalid or out of order.");

                    sawChromaticities = true;
                    colorManagement = (colorManagement ?? PngColorManagementData.Empty) with
                    {
                        Chromaticities = ReadChromaticities(chunk, owner)
                    };
                    break;
                case "sRGB":
                    EnsureHeader(header, owner);
                    if (sawSrgb || sawPalette || sawIccProfile || sawIdat || length != 1)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG sRGB chunk is invalid or out of order.");

                    sawSrgb = true;
                    colorManagement = PngColorManagementData.Srgb(SrgbRenderingIntent(chunk[0], owner));
                    break;
                case "iCCP":
                    EnsureHeader(header, owner);
                    if (sawIccProfile || sawPalette || sawSrgb || sawIdat)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG iCCP chunk is out of order.");

                    sawIccProfile = true;
                    colorManagement = (colorManagement ?? PngColorManagementData.Empty) with
                    {
                        IccProfile = ReadIccProfile(chunk, owner)
                    };
                    break;
                case "IDAT":
                    EnsureHeader(header, owner);
                    if (idatClosed)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG IDAT chunks must be consecutive.");

                    sawIdat = true;
                    idat.Write(chunk);
                    break;
                case "IEND":
                    if (length != 0)
                        throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG IEND chunk is invalid.");

                    sawEnd = true;
                    offset = nextOffset;
                    goto Done;
                default:
                    if (sawIdat)
                        idatClosed = true;
                    break;
            }

            offset = nextOffset;
        }

Done:
        if (!sawEnd || offset != data.Length)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG stream does not contain a valid IEND chunk.");

        if (header is null)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG stream does not contain an IHDR chunk.");

        if (idat.Length == 0)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG stream does not contain image data.");

        ValidateFormat(header.Value, palette, transparency, owner);

        var inflated = Inflate(idat.ToArray(), owner);
        var decoded = header.Value.InterlaceMethod == 0
            ? DecodeScanlines(inflated, header.Value, owner)
            : DecodeInterlacedScanlines(inflated, header.Value, owner);

        return BuildImage(decoded, header.Value, palette, transparency, colorManagement, owner);
    }

    private static PngHeader ReadHeader(ReadOnlySpan<byte> chunk, PdfDocument owner)
    {
        var width = ReadUInt32(chunk, 0);
        var height = ReadUInt32(chunk, 4);

        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG image dimensions are invalid.");

        var bitDepth = chunk[8];
        var colorType = chunk[9];
        var compressionMethod = chunk[10];
        var filterMethod = chunk[11];
        var interlaceMethod = chunk[12];

        if (compressionMethod != 0 || filterMethod != 0 || interlaceMethod is not (0 or 1))
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG compression, filter, or interlace method is unsupported.");

        return new PngHeader((int)width, (int)height, bitDepth, colorType, interlaceMethod);
    }

    private static void ValidateFormat(PngHeader header, byte[]? palette, byte[]? transparency, PdfDocument owner)
    {
        var valid = header.ColorType switch
        {
            0 => header.BitDepth is 1 or 2 or 4 or 8 or 16,
            2 => header.BitDepth is 8 or 16,
            3 => (header.BitDepth is 1 or 2 or 4 or 8) && palette is not null,
            4 => header.BitDepth is 8 or 16,
            6 => header.BitDepth is 8 or 16,
            _ => false
        };

        if (!valid)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG color type or bit depth is unsupported.");

        if (palette is not null)
        {
            if (header.ColorType is 0 or 4)
                throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG PLTE chunk is not valid for grayscale images.");

            if (header.ColorType == 3 && palette.Length / 3 > 1 << header.BitDepth)
                throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG palette has too many entries for the bit depth.");
        }

        if (transparency is not null)
        {
            var transparencyValid = header.ColorType switch
            {
                0 => transparency.Length == 2 && ReadUInt16(transparency, 0) < 1 << header.BitDepth,
                2 => transparency.Length == 6,
                3 => palette is not null && transparency.Length <= palette.Length / 3,
                _ => false
            };

            if (!transparencyValid)
                throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG tRNS chunk is not valid for this image format.");
        }
    }

    private static void EnsureHeader(PngHeader? header, PdfDocument owner)
    {
        if (header is null)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG IHDR chunk must appear before image data chunks.");
    }

    private static void ValidateChunkCrc(byte[] data, int chunkStart, int length, PdfDocument owner)
    {
        var expected = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(chunkStart + 8 + length, 4));
        var actual = Crc32(data.AsSpan(chunkStart + 4, length + 4));

        if (actual != expected)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG chunk CRC is invalid.");
    }

    private static PngChromaticities ReadChromaticities(ReadOnlySpan<byte> chunk, PdfDocument owner)
    {
        var values = new double[8];

        for (var i = 0; i < values.Length; i++)
            values[i] = ReadUInt32(chunk, i * 4) / 100000.0;

        var chromaticities = new PngChromaticities(
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5],
            values[6],
            values[7]);

        if (!chromaticities.IsValid)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG cHRM chromaticities are invalid.");

        return chromaticities;
    }

    private static string SrgbRenderingIntent(byte value, PdfDocument owner)
    {
        return value switch
        {
            0 => "Perceptual",
            1 => "RelativeColorimetric",
            2 => "Saturation",
            3 => "AbsoluteColorimetric",
            _ => throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG sRGB rendering intent is invalid.")
        };
    }

    private static byte[] ReadIccProfile(ReadOnlySpan<byte> chunk, PdfDocument owner)
    {
        var separator = chunk.IndexOf((byte)0);
        if (separator is <= 0 or > 79 || separator + 2 > chunk.Length)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG iCCP profile name is invalid.");

        if (chunk[separator + 1] != 0)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG iCCP compression method is unsupported.");

        var compressed = chunk[(separator + 2)..].ToArray();
        if (compressed.Length == 0)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG iCCP profile data is empty.");

        return Inflate(compressed, owner);
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFF_FFFFu;

        foreach (var value in data)
        {
            crc ^= value;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB8_8320u : crc >> 1;
        }

        return ~crc;
    }

    private static byte[] Inflate(byte[] data, PdfDocument owner)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var zlib = new ZLibStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }
        catch (InvalidDataException ex)
        {
            throw owner.CreateException(HaruStatus.LibPngError, ex.Message, unchecked((uint)ex.HResult));
        }
    }

    private static byte[] DecodeScanlines(byte[] data, PngHeader header, PdfDocument owner)
    {
        var rowBytes = RowBytes(header.Width, header.BitsPerPixel);
        var bytesPerPixel = FilterBytesPerPixel(header.BitsPerPixel);
        var expected = CheckedMultiply(rowBytes + 1, header.Height, owner);

        if (data.Length < expected)
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG image data is truncated.");

        var output = new byte[CheckedMultiply(rowBytes, header.Height, owner)];
        var previous = new byte[rowBytes];
        var source = 0;
        var destination = 0;

        for (var y = 0; y < header.Height; y++)
        {
            var filter = data[source++];
            var current = new byte[rowBytes];
            Buffer.BlockCopy(data, source, current, 0, rowBytes);
            source += rowBytes;

            Unfilter(current, previous, bytesPerPixel, filter, owner);
            Buffer.BlockCopy(current, 0, output, destination, rowBytes);
            destination += rowBytes;
            previous = current;
        }

        return output;
    }

    private static byte[] DecodeInterlacedScanlines(byte[] data, PngHeader header, PdfDocument owner)
    {
        var finalRowBytes = RowBytes(header.Width, header.BitsPerPixel);
        var output = new byte[CheckedMultiply(finalRowBytes, header.Height, owner)];
        var source = 0;
        var bytesPerPixel = FilterBytesPerPixel(header.BitsPerPixel);

        foreach (var pass in Adam7Passes)
        {
            var passWidth = PassSize(header.Width, pass.XStart, pass.XStep);
            var passHeight = PassSize(header.Height, pass.YStart, pass.YStep);

            if (passWidth == 0 || passHeight == 0)
                continue;

            var passRowBytes = RowBytes(passWidth, header.BitsPerPixel);
            var previous = new byte[passRowBytes];

            for (var py = 0; py < passHeight; py++)
            {
                if (source + 1 + passRowBytes > data.Length)
                    throw owner.CreateException(HaruStatus.InvalidPngImage, "Interlaced PNG image data is truncated.");

                var filter = data[source++];
                var current = new byte[passRowBytes];
                Buffer.BlockCopy(data, source, current, 0, passRowBytes);
                source += passRowBytes;

                Unfilter(current, previous, bytesPerPixel, filter, owner);
                ScatterPassRow(current, output, header, pass, py, finalRowBytes);
                previous = current;
            }
        }

        return output;
    }

    private static void ScatterPassRow(byte[] passRow, byte[] output, PngHeader header, Adam7Pass pass, int passY, int finalRowBytes)
    {
        var y = pass.YStart + passY * pass.YStep;
        var finalRowOffset = y * finalRowBytes;
        var passWidth = PassSize(header.Width, pass.XStart, pass.XStep);

        if (header.BitDepth < 8)
        {
            for (var px = 0; px < passWidth; px++)
            {
                var x = pass.XStart + px * pass.XStep;
                SetPackedSample(output, finalRowOffset, header.BitDepth, x, GetPackedSample(passRow, 0, header.BitDepth, px));
            }

            return;
        }

        var pixelBytes = header.BitsPerPixel / 8;

        for (var px = 0; px < passWidth; px++)
        {
            var x = pass.XStart + px * pass.XStep;
            Buffer.BlockCopy(passRow, px * pixelBytes, output, finalRowOffset + x * pixelBytes, pixelBytes);
        }
    }

    private static void Unfilter(byte[] current, byte[] previous, int bytesPerPixel, int filter, PdfDocument owner)
    {
        switch (filter)
        {
            case 0:
                return;
            case 1:
                for (var i = 0; i < current.Length; i++)
                    current[i] = unchecked((byte)(current[i] + Left(current, i, bytesPerPixel)));
                return;
            case 2:
                for (var i = 0; i < current.Length; i++)
                    current[i] = unchecked((byte)(current[i] + previous[i]));
                return;
            case 3:
                for (var i = 0; i < current.Length; i++)
                    current[i] = unchecked((byte)(current[i] + ((Left(current, i, bytesPerPixel) + previous[i]) >> 1)));
                return;
            case 4:
                for (var i = 0; i < current.Length; i++)
                {
                    var left = Left(current, i, bytesPerPixel);
                    var up = previous[i];
                    var upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
                    current[i] = unchecked((byte)(current[i] + Paeth(left, up, upLeft)));
                }

                return;
            default:
                throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG scanline filter is unsupported.");
        }
    }

    private static int Left(byte[] row, int index, int bytesPerPixel) => index >= bytesPerPixel ? row[index - bytesPerPixel] : 0;

    private static int Paeth(int left, int up, int upLeft)
    {
        var estimate = left + up - upLeft;
        var distanceLeft = Math.Abs(estimate - left);
        var distanceUp = Math.Abs(estimate - up);
        var distanceUpLeft = Math.Abs(estimate - upLeft);

        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpLeft)
            return left;

        return distanceUp <= distanceUpLeft ? up : upLeft;
    }

    private static PngImageData BuildImage(byte[] decoded, PngHeader header, byte[]? palette, byte[]? transparency, PngColorManagementData? colorManagement, PdfDocument owner)
    {
        return header.ColorType switch
        {
            0 => BuildGrayImage(decoded, header, transparency, colorManagement),
            2 => BuildRgbImage(decoded, header, transparency, colorManagement),
            3 => BuildIndexedImage(decoded, header, palette!, transparency, colorManagement),
            4 => BuildGrayAlphaImage(decoded, header, colorManagement, owner),
            6 => BuildRgbaImage(decoded, header, colorManagement, owner),
            _ => throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG color type is unsupported.")
        };
    }

    private static PngImageData BuildGrayImage(byte[] decoded, PngHeader header, byte[]? transparency, PngColorManagementData? colorManagement)
    {
        var bitsPerComponent = header.BitDepth == 16 ? 8 : header.BitDepth;
        var imageData = header.BitDepth == 16 ? Strip16BitSamples(decoded, 1) : decoded;
        int[]? colorMask = transparency is { Length: >= 2 }
            ? [ScaleTransparentSample(ReadUInt16(transparency, 0), header.BitDepth), ScaleTransparentSample(ReadUInt16(transparency, 0), header.BitDepth)]
            : null;

        return new PngImageData(imageData, header.Width, header.Height, bitsPerComponent, PdfColorSpace.DeviceGray, null, null, colorMask, null, 0, colorManagement);
    }

    private static PngImageData BuildRgbImage(byte[] decoded, PngHeader header, byte[]? transparency, PngColorManagementData? colorManagement)
    {
        var imageData = header.BitDepth == 16 ? Strip16BitSamples(decoded, 3) : decoded;
        int[]? colorMask = null;

        if (transparency is { Length: >= 6 })
        {
            var r = ScaleTransparentSample(ReadUInt16(transparency, 0), header.BitDepth);
            var g = ScaleTransparentSample(ReadUInt16(transparency, 2), header.BitDepth);
            var b = ScaleTransparentSample(ReadUInt16(transparency, 4), header.BitDepth);
            colorMask = [r, r, g, g, b, b];
        }

        return new PngImageData(imageData, header.Width, header.Height, 8, PdfColorSpace.DeviceRgb, null, null, colorMask, null, 0, colorManagement);
    }

    private static PngImageData BuildIndexedImage(byte[] decoded, PngHeader header, byte[] palette, byte[]? transparency, PngColorManagementData? colorManagement)
    {
        var paletteEntries = palette.Length / 3;
        var colorSpace = new PdfArray([
            new PdfName("Indexed"),
            new PdfName("DeviceRGB"),
            new PdfInteger(paletteEntries - 1),
            PdfBinary.FromBytes(palette)
        ]);
        var softMask = transparency is not null && transparency.Any(static value => value != 0xFF)
            ? CreatePaletteSoftMask(decoded, header.Width, header.Height, header.BitDepth, transparency)
            : null;

        return new PngImageData(decoded, header.Width, header.Height, header.BitDepth, PdfColorSpace.Indexed, colorSpace, softMask, null, palette, paletteEntries - 1, colorManagement);
    }

    private static PngImageData BuildGrayAlphaImage(byte[] decoded, PngHeader header, PngColorManagementData? colorManagement, PdfDocument owner)
    {
        var pixelCount = CheckedMultiply(header.Width, header.Height, owner);
        var image = new byte[pixelCount];
        var mask = new byte[pixelCount];
        var source = 0;

        for (var i = 0; i < pixelCount; i++)
        {
            image[i] = decoded[source];
            source += header.BitDepth == 16 ? 2 : 1;
            mask[i] = decoded[source];
            source += header.BitDepth == 16 ? 2 : 1;
        }

        return new PngImageData(image, header.Width, header.Height, 8, PdfColorSpace.DeviceGray, null, mask, null, null, 0, colorManagement);
    }

    private static PngImageData BuildRgbaImage(byte[] decoded, PngHeader header, PngColorManagementData? colorManagement, PdfDocument owner)
    {
        var pixelCount = CheckedMultiply(header.Width, header.Height, owner);
        var image = new byte[CheckedMultiply(pixelCount, 3, owner)];
        var mask = new byte[pixelCount];
        var source = 0;
        var destination = 0;

        for (var i = 0; i < pixelCount; i++)
        {
            image[destination++] = decoded[source];
            source += header.BitDepth == 16 ? 2 : 1;
            image[destination++] = decoded[source];
            source += header.BitDepth == 16 ? 2 : 1;
            image[destination++] = decoded[source];
            source += header.BitDepth == 16 ? 2 : 1;
            mask[i] = decoded[source];
            source += header.BitDepth == 16 ? 2 : 1;
        }

        return new PngImageData(image, header.Width, header.Height, 8, PdfColorSpace.DeviceRgb, null, mask, null, null, 0, colorManagement);
    }

    private static byte[] Strip16BitSamples(byte[] decoded, int channels)
    {
        var output = new byte[decoded.Length / 2];
        var source = 0;

        for (var destination = 0; destination < output.Length; destination++)
        {
            output[destination] = decoded[source];
            source += 2;
        }

        _ = channels;
        return output;
    }

    private static byte[] CreatePaletteSoftMask(byte[] indexedData, int width, int height, int bitDepth, byte[] transparency)
    {
        var rowBytes = RowBytes(width, bitDepth);
        var mask = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * rowBytes;

            for (var x = 0; x < width; x++)
            {
                var index = GetPackedSample(indexedData, rowOffset, bitDepth, x);
                mask[y * width + x] = index < transparency.Length ? transparency[index] : (byte)0xFF;
            }
        }

        return mask;
    }

    private static int GetPackedSample(byte[] data, int rowOffset, int bitDepth, int sampleIndex)
    {
        var bitIndex = sampleIndex * bitDepth;
        var value = data[rowOffset + (bitIndex >> 3)];
        var shift = 8 - bitDepth - (bitIndex & 7);
        return (value >> shift) & ((1 << bitDepth) - 1);
    }

    private static void SetPackedSample(byte[] data, int rowOffset, int bitDepth, int sampleIndex, int sample)
    {
        var bitIndex = sampleIndex * bitDepth;
        var byteIndex = rowOffset + (bitIndex >> 3);
        var shift = 8 - bitDepth - (bitIndex & 7);
        var mask = ((1 << bitDepth) - 1) << shift;
        data[byteIndex] = (byte)((data[byteIndex] & ~mask) | ((sample << shift) & mask));
    }

    private static int ScaleTransparentSample(int value, int bitDepth) => bitDepth == 16 ? value >> 8 : value;

    private static int RowBytes(int width, int bitsPerPixel) => (width * bitsPerPixel + 7) / 8;

    private static int FilterBytesPerPixel(int bitsPerPixel) => Math.Max(1, (bitsPerPixel + 7) / 8);

    private static int PassSize(int size, int start, int step) => size <= start ? 0 : (size - start + step - 1) / step;

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset) => BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);

    private static int ReadInt32(byte[] data, int offset)
    {
        var value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
        return value > int.MaxValue ? -1 : (int)value;
    }

    private static int ReadUInt16(byte[] data, int offset) => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));

    private static int CheckedMultiply(int left, int right, PdfDocument owner)
    {
        try
        {
            return checked(left * right);
        }
        catch (OverflowException)
        {
            throw owner.CreateException(HaruStatus.InvalidPngImage, "PNG image dimensions are too large.");
        }
    }

    private readonly record struct PngHeader(int Width, int Height, int BitDepth, int ColorType, int InterlaceMethod)
    {
        internal int Channels => ColorType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => 0
        };

        internal int BitsPerPixel => Channels * BitDepth;
    }

    private readonly record struct Adam7Pass(int XStart, int YStart, int XStep, int YStep);
}

internal sealed record PngImageData(
    byte[] ImageData,
    int Width,
    int Height,
    int BitsPerComponent,
    PdfColorSpace ColorSpace,
    PdfObject? ColorSpaceObject,
    byte[]? SoftMaskData,
    int[]? ColorMask,
    byte[]? IndexedPalette,
    int IndexedHighValue,
    PngColorManagementData? ColorManagement);

internal sealed record PngColorManagementData(
    double? Gamma,
    PngChromaticities? Chromaticities,
    string? RenderingIntent,
    byte[]? IccProfile)
{
    internal static readonly PngColorManagementData Empty = new(null, null, null, null);

    internal static PngColorManagementData Srgb(string renderingIntent) => new(
        0.45455,
        PngChromaticities.Srgb,
        renderingIntent,
        null);
}

internal readonly record struct PngChromaticities(
    double WhiteX,
    double WhiteY,
    double RedX,
    double RedY,
    double GreenX,
    double GreenY,
    double BlueX,
    double BlueY)
{
    internal static readonly PngChromaticities Srgb = new(0.3127, 0.3290, 0.6400, 0.3300, 0.3000, 0.6000, 0.1500, 0.0600);

    internal bool IsValid =>
        IsValidPoint(WhiteX, WhiteY)
        && IsValidPoint(RedX, RedY)
        && IsValidPoint(GreenX, GreenY)
        && IsValidPoint(BlueX, BlueY);

    private static bool IsValidPoint(double x, double y)
    {
        return x > 0 && y > 0 && x + y < 1 && !double.IsNaN(x) && !double.IsNaN(y);
    }
}
