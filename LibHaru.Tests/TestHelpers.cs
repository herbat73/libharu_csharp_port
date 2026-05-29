using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using LibHaru;

namespace LibHaru.Tests;

internal static class TestHelpers
{
    public static HaruException AssertHaruException(uint expectedStatus, Action action)
    {
        var exception = Assert.Throws<HaruException>(action);
        Assert.Equal(expectedStatus, exception.Status);
        return exception;
    }

    public static void AssertPdf(byte[] bytes)
    {
        Assert.NotEmpty(bytes);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(bytes, 0, Math.Min(5, bytes.Length)));
        Assert.Contains("%%EOF", PdfText(bytes));
    }

    public static string PdfText(byte[] bytes)
    {
        return Encoding.Latin1.GetString(bytes);
    }

    public static string NewArtifactPath(string fileName)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "TestArtifacts", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, fileName);
    }

    public static string RepoPath(params string[] segments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var parts = new string[segments.Length + 1];
        parts[0] = root;
        segments.CopyTo(parts, 1);
        return Path.Combine(parts);
    }

    public static byte[] MinimalJpeg(byte components = 3, ushort width = 1, ushort height = 1)
    {
        return
        [
            0xFF, 0xD8,
            0xFF, 0xE0, 0x00, 0x02,
            0xFF, 0xC0, 0x00, 0x08, 0x08,
            (byte)(height >> 8), (byte)height,
            (byte)(width >> 8), (byte)width,
            components,
            0xFF, 0xD9
        ];
    }

    public static byte[] MinimalPng(bool gamma = false, bool chromaticities = false, bool srgb = false,
        byte[]? iccProfile = null)
    {
        using var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4, 4), 1);
        ihdr[8] = 8;
        ihdr[9] = 2;
        WritePngChunk(png, "IHDR", ihdr);

        if (gamma)
        {
            var data = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(data, 45455);
            WritePngChunk(png, "gAMA", data);
        }

        if (chromaticities)
        {
            var data = new byte[32];
            uint[] values = [31270, 32900, 64000, 33000, 30000, 60000, 15000, 6000];
            for (var i = 0; i < values.Length; i++)
                BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(i * 4, 4), values[i]);
            WritePngChunk(png, "cHRM", data);
        }

        if (srgb)
            WritePngChunk(png, "sRGB", [0]);

        if (iccProfile is not null)
        {
            var name = Encoding.ASCII.GetBytes("unit");
            var compressedProfile = ZLibCompress(iccProfile);
            var data = new byte[name.Length + 2 + compressedProfile.Length];
            name.CopyTo(data, 0);
            data[name.Length] = 0;
            data[name.Length + 1] = 0;
            compressedProfile.CopyTo(data, name.Length + 2);
            WritePngChunk(png, "iCCP", data);
        }

        WritePngChunk(png, "IDAT", ZLibCompress([0, 255, 0, 0]));
        WritePngChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static byte[] ZLibCompress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(data);
        return output.ToArray();
    }

    private static void WritePngChunk(Stream output, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        var crc = UpdateCrc(0xFFFFFFFF, typeBytes);
        crc = UpdateCrc(crc, data) ^ 0xFFFFFFFF;

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) == 1 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }

        return crc;
    }
}
