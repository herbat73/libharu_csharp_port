using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using LibHaru;
using static LibHaru.HPdf;
using CompressionMode = LibHaru.CompressionMode;

public static class ImageFeatures
{
    public static void Test(string repoRoot, string pdfPath)
    {
        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.All);

        var page = HPDF_AddPage(pdf);
        var font = HPDF_GetFont(pdf, "Helvetica");
        HPDF_Page_SetFontAndSize(page, font, 12);
        HPDF_Page_TextOut(page, 40, HPDF_Page_GetHeight(page) - 50, "Image feature smoke");

        var rawRgb = new byte[]
        {
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
            255, 255, 255
        };
        var rawImage = HPDF_LoadRawImageFromMem(pdf, rawRgb, 2, 2, PdfColorSpace.DeviceRgb);
        Require(HPDF_Image_Validate(rawImage), "Raw image validator failed.");
        HPDF_Image_SetColorMask(rawImage, 255, 255, 255, 255, 255, 255);
        HPDF_Page_DrawImage(page, rawImage, 40, HPDF_Page_GetHeight(page) - 120, 48, 48);

        var rawFile = Path.Combine(Path.GetDirectoryName(pdfPath)!, "image-raw-rgb.bin");
        File.WriteAllBytes(rawFile, rawRgb);
        var rawFileImage = HPDF_LoadRawImageFromFile(pdf, rawFile, 2, 2, PdfColorSpace.DeviceRgb);
        Require(HPDF_Image_GetColorSpace(rawFileImage) == "DeviceRGB", "Raw image color space mismatch.");
        var rawFileName = HPDF_Page_GetXObjectName(page, rawFileImage);
        Require(rawFileName.StartsWith("Im", StringComparison.Ordinal), "Missing image XObject name.");
        HPDF_Page_DrawImage(page, rawFileImage, 100, HPDF_Page_GetHeight(page) - 120, 48, 48);

        var maskData = new byte[] { 0b1000_0000, 0b0100_0000 };
        var maskImage = HPDF_Image_LoadRaw1BitImageFromMem(pdf, maskData, 2, 2, 1, true, true);
        var maskedImage = HPDF_LoadRawImageFromMem(pdf, rawRgb, 2, 2, PdfColorSpace.DeviceRgb);
        HPDF_Image_SetMaskImage(maskedImage, maskImage);
        HPDF_Page_DrawImage(page, maskedImage, 160, HPDF_Page_GetHeight(page) - 120, 48, 48);

        var softMask = HPDF_LoadRawImageFromMem(pdf, [255, 160, 80, 0], 2, 2, PdfColorSpace.DeviceGray);
        var softMaskedImage = HPDF_LoadRawImageFromMem(pdf, rawRgb, 2, 2, PdfColorSpace.DeviceRgb);
        HPDF_Image_AddSMask(softMaskedImage, softMask);
        HPDF_Page_DrawImage(page, softMaskedImage, 220, HPDF_Page_GetHeight(page) - 120, 48, 48);

        var ccittImage = HPDF_Image_LoadRaw1BitImageFromMem(pdf, [0b1010_1010, 0b0101_0101], 8, 2, 1, true, true);
        HPDF_Page_DrawImage(page, ccittImage, 280, HPDF_Page_GetHeight(page) - 120, 48, 48);

        var rgbPngPath = Path.Combine(Path.GetDirectoryName(pdfPath)!, "image-rgb.png");
        File.WriteAllBytes(rgbPngPath, CreatePng(2, 2, 8, 2, [
            255, 0, 0, 0, 255, 0,
            0, 0, 255, 255, 255, 0
        ]));
        var pngFileImage = HPDF_LoadPngImageFromFile(pdf, rgbPngPath);
        HPDF_Page_DrawImage(page, pngFileImage, 40, HPDF_Page_GetHeight(page) - 190, 48, 48);

        VerifyDelayedPngFileLoading(Path.GetDirectoryName(pdfPath)!);

        var rgbaPng = CreatePng(2, 2, 8, 6, [
            255, 0, 0, 255, 0, 255, 0, 160,
            0, 0, 255, 80, 255, 255, 0, 0
        ]);
        var rgbaImage = HPDF_LoadPngImageFromMem(pdf, rgbaPng);
        HPDF_Page_DrawImage(page, rgbaImage, 100, HPDF_Page_GetHeight(page) - 190, 48, 48);

        var indexedPng = CreatePng(
            2,
            2,
            8,
            3,
            [0, 1, 2, 0],
            [255, 0, 0, 0, 255, 0, 0, 0, 255],
            [255, 128, 0]);
        var indexedImage = HPDF_LoadPngImageFromMem(pdf, indexedPng);
        Require(HPDF_Image_GetColorSpace(indexedImage) == "Indexed", "Indexed PNG color space mismatch.");
        HPDF_Page_DrawImage(page, indexedImage, 160, HPDF_Page_GetHeight(page) - 190, 48, 48);

        var colorManagedPng = CreatePng(
            1,
            1,
            8,
            2,
            [32, 96, 224],
            gamma: 45455,
            chromaticities: SrgbChromaticities());
        var colorManagedImage = HPDF_LoadPngImageFromMem(pdf, colorManagedPng);
        Require(HPDF_Image_GetColorSpace(colorManagedImage) == "CalRGB", "gAMA/cHRM PNG did not produce CalRGB.");

        var grayGammaPng = CreatePng(1, 1, 8, 0, [128], gamma: 50000);
        var grayGammaImage = HPDF_LoadPngImageFromMem(pdf, grayGammaPng);
        Require(HPDF_Image_GetColorSpace(grayGammaImage) == "CalGray", "gAMA grayscale PNG did not produce CalGray.");

        var srgbPng = CreatePng(1, 1, 8, 2, [200, 160, 120], srgbIntent: 0);
        var srgbImage = HPDF_LoadPngImageFromMem(pdf, srgbPng);
        Require(HPDF_Image_GetColorSpace(srgbImage) == "CalRGB", "sRGB PNG did not produce CalRGB.");

        var ancillaryPng = CreatePng(
            1,
            1,
            8,
            2,
            [64, 128, 192],
            ancillaryChunks:
            [
                ("sBIT", [8, 8, 8]),
                ("pHYs", PhysicalPixels(2_835, 2_835, 1)),
                ("bKGD", TrueColorBackground(64, 128, 192)),
                ("tEXt", Latin1Text("Title", "Ancillary smoke")),
                ("zTXt", CompressedLatin1Text("Comment", "compressed ancillary")),
                ("iTXt", InternationalText("Description", "international ancillary"))
            ]);
        var ancillaryImage = HPDF_LoadPngImageFromMem(pdf, ancillaryPng);
        Require(HPDF_Image_Validate(ancillaryImage), "PNG ancillary chunk image failed validation.");

        var jpegPath = Path.Combine(repoRoot, "demo", "images", "rgb.jpg");
        var jpegImage = HPDF_LoadJpegImageFromFile(pdf, jpegPath);
        HPDF_Page_DrawImage(page, jpegImage, 220, HPDF_Page_GetHeight(page) - 190, 48, 48);

        var grayJpegPath = Path.Combine(repoRoot, "demo", "images", "gray.jpg");
        var grayJpegImage = HPDF_LoadJpegImageFromFile(pdf, grayJpegPath);
        Require(HPDF_Image_GetColorSpace(grayJpegImage) == "DeviceGray",
            "Grayscale JPEG fixture color space mismatch.");
        HPDF_Page_DrawImage(page, grayJpegImage, 280, HPDF_Page_GetHeight(page) - 190, 48, 48);

        var maskFixturePath = Path.Combine(repoRoot, "demo", "pngsuite", "maskimage.png");
        var maskFixtureImage = HPDF_LoadPngImageFromFile(pdf, maskFixturePath);
        Require(HPDF_Image_Validate(maskFixtureImage), "PngSuite mask fixture failed validation.");
        HPDF_Page_DrawImage(page, maskFixtureImage, 340, HPDF_Page_GetHeight(page) - 190, 48, 48);

        HPDF_Page_ExecuteXObject(page, jpegImage);

        var pngSuiteDir = Path.Combine(repoRoot, "demo", "pngsuite");
        var suiteX = 40.0;
        var suiteY = HPDF_Page_GetHeight(page) - 260;
        foreach (var suiteFile in Directory.GetFiles(pngSuiteDir, "basn*.png")
                     .OrderBy(static path => path, StringComparer.Ordinal))
        {
            var suiteImage = HPDF_LoadPngImageFromFile(pdf, suiteFile);
            Require(HPDF_Image_Validate(suiteImage),
                $"PngSuite fixture failed validation: {Path.GetFileName(suiteFile)}.");
            HPDF_Page_DrawImage(page, suiteImage, suiteX, suiteY, 20, 20);
            suiteX += 24;
            if (suiteX > 340)
            {
                suiteX = 40;
                suiteY -= 24;
            }
        }

        var invalidPng = CreatePng(1, 1, 8, 2, [1, 2, 3]);
        invalidPng[^1] ^= 0xFF;
        var invalidPngError = RequireHaruException(() => HPDF_LoadPngImageFromMem(pdf, invalidPng));
        Require(invalidPngError.Status == HaruStatus.InvalidPngImage, "Invalid PNG CRC raised the wrong status.");

        var unknownCriticalPng = CreatePng(1, 1, 8, 2, [1, 2, 3], ancillaryChunks: [("VpAg", [0])]);
        var unknownCriticalError = RequireHaruException(() => HPDF_LoadPngImageFromMem(pdf, unknownCriticalPng));
        Require(unknownCriticalError.Status == HaruStatus.InvalidPngImage,
            "Unknown critical PNG chunk raised the wrong status.");

        var invalidPhysicalPixelsPng = CreatePng(1, 1, 8, 2, [1, 2, 3], ancillaryChunks: [("pHYs", [0])]);
        var invalidPhysicalPixelsError =
            RequireHaruException(() => HPDF_LoadPngImageFromMem(pdf, invalidPhysicalPixelsPng));
        Require(invalidPhysicalPixelsError.Status == HaruStatus.InvalidPngImage,
            "Invalid PNG pHYs chunk raised the wrong status.");

        HPDF_SaveToFile(pdf, pdfPath);

        var bytes = File.ReadAllBytes(pdfPath);
        var latin1 = Encoding.Latin1.GetString(bytes);

        Require(latin1.Contains("/Subtype /Image", StringComparison.Ordinal), "Missing image XObject.");
        Require(latin1.Contains("/ColorSpace [/Indexed /DeviceRGB", StringComparison.Ordinal),
            "Missing Indexed color space.");
        Require(latin1.Contains("/DCTDecode", StringComparison.Ordinal), "Missing JPEG DCTDecode filter.");
        Require(latin1.Contains("/CCITTFaxDecode", StringComparison.Ordinal), "Missing CCITT filter.");
        Require(latin1.Contains("/DecodeParms [", StringComparison.Ordinal), "Missing CCITT DecodeParms.");
        Require(latin1.Contains("/BlackIs1 true", StringComparison.Ordinal), "Missing CCITT BlackIs1 parameter.");
        Require(latin1.Contains("/SMask", StringComparison.Ordinal), "Missing soft mask.");
        Require(latin1.Contains("/ImageMask true", StringComparison.Ordinal), "Missing image mask.");
        Require(latin1.Contains("/Mask [", StringComparison.Ordinal), "Missing color-key mask.");
        Require(latin1.Contains("/XObject", StringComparison.Ordinal), "Missing page XObject resources.");
        Require(latin1.Contains("/CalRGB", StringComparison.Ordinal), "Missing CalRGB color-managed PNG color space.");
        Require(latin1.Contains("/CalGray", StringComparison.Ordinal),
            "Missing CalGray color-managed PNG color space.");
        Require(latin1.Contains("/Gamma", StringComparison.Ordinal), "Missing PNG gamma mapping.");
        Require(latin1.Contains("/Matrix", StringComparison.Ordinal), "Missing PNG chromaticity matrix.");
        Require(latin1.Contains("/Intent /Perceptual", StringComparison.Ordinal), "Missing PNG sRGB rendering intent.");

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"{bytes.Length} bytes with image features");
    }

    private static void VerifyDelayedPngFileLoading(string outputDirectory)
    {
        using var pdf = HPDF_New();
        var page = HPDF_AddPage(pdf);
        var pngPath = Path.Combine(outputDirectory, "image-delayed-file2.png");
        var pdfPath = Path.Combine(outputDirectory, "image-delayed-file2.pdf");
        byte[] initialPixel = [231, 17, 29];
        byte[] updatedPixel = [37, 149, 213];

        File.WriteAllBytes(pngPath, CreatePng(1, 1, 8, 2, initialPixel));
        var image = HPDF_LoadPngImageFromFile2(pdf, pngPath);
        Require(HPDF_Image_Validate(image), "Delayed PNG image validator failed.");

        File.WriteAllBytes(pngPath, CreatePng(1, 1, 8, 2, updatedPixel));
        HPDF_Page_DrawImage(page, image, 40, 40, 32, 32);
        HPDF_SaveToFile(pdf, pdfPath);

        var bytes = File.ReadAllBytes(pdfPath);
        Require(ContainsSequence(bytes, updatedPixel), "Delayed PNG did not reload updated file data at save time.");
        Require(!ContainsSequence(bytes, initialPixel), "Delayed PNG retained eagerly loaded image data.");

        var latin1 = Encoding.Latin1.GetString(bytes);
        Require(!latin1.Contains("_FILE_NAME", StringComparison.Ordinal),
            "Delayed PNG file marker leaked into PDF output.");
    }

    private static byte[] CreatePng(
        int width,
        int height,
        byte bitDepth,
        byte colorType,
        byte[] pixels,
        byte[]? palette = null,
        byte[]? transparency = null,
        uint? gamma = null,
        byte[]? chromaticities = null,
        byte? srgbIntent = null,
        IReadOnlyList<(string Type, byte[] Data)>? ancillaryChunks = null)
    {
        var bitsPerPixel = colorType switch
        {
            0 => bitDepth,
            2 => 3 * bitDepth,
            3 => bitDepth,
            4 => 2 * bitDepth,
            6 => 4 * bitDepth,
            _ => throw new ArgumentOutOfRangeException(nameof(colorType))
        };
        var rowBytes = (width * bitsPerPixel + 7) / 8;
        var scanlines = new byte[(rowBytes + 1) * height];

        for (var y = 0; y < height; y++)
            Buffer.BlockCopy(pixels, y * rowBytes, scanlines, y * (rowBytes + 1) + 1, rowBytes);

        using var output = new MemoryStream();
        output.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr[4..], (uint)height);
        ihdr[8] = bitDepth;
        ihdr[9] = colorType;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WriteChunk(output, "IHDR", ihdr);

        if (chromaticities is not null)
            WriteChunk(output, "cHRM", chromaticities);

        if (gamma is not null)
        {
            Span<byte> gama = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(gama, gamma.Value);
            WriteChunk(output, "gAMA", gama);
        }

        if (srgbIntent is not null)
            WriteChunk(output, "sRGB", [srgbIntent.Value]);

        if (palette is not null)
            WriteChunk(output, "PLTE", palette);

        if (transparency is not null)
            WriteChunk(output, "tRNS", transparency);

        if (ancillaryChunks is not null)
            foreach (var (type, chunkData) in ancillaryChunks)
                WriteChunk(output, type, chunkData);

        WriteChunk(output, "IDAT", Zlib(scanlines));
        WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
        return output.ToArray();
    }

    private static byte[] SrgbChromaticities()
    {
        using var output = new MemoryStream();
        WriteUInt32(output, 31270);
        WriteUInt32(output, 32900);
        WriteUInt32(output, 64000);
        WriteUInt32(output, 33000);
        WriteUInt32(output, 30000);
        WriteUInt32(output, 60000);
        WriteUInt32(output, 15000);
        WriteUInt32(output, 6000);
        return output.ToArray();
    }

    private static byte[] PhysicalPixels(uint xPixelsPerUnit, uint yPixelsPerUnit, byte unit)
    {
        using var output = new MemoryStream();
        WriteUInt32(output, xPixelsPerUnit);
        WriteUInt32(output, yPixelsPerUnit);
        output.WriteByte(unit);
        return output.ToArray();
    }

    private static byte[] TrueColorBackground(ushort red, ushort green, ushort blue)
    {
        using var output = new MemoryStream();
        WriteUInt16(output, red);
        WriteUInt16(output, green);
        WriteUInt16(output, blue);
        return output.ToArray();
    }

    private static byte[] Latin1Text(string keyword, string text)
    {
        using var output = new MemoryStream();
        output.Write(Encoding.Latin1.GetBytes(keyword));
        output.WriteByte(0);
        output.Write(Encoding.Latin1.GetBytes(text));
        return output.ToArray();
    }

    private static byte[] CompressedLatin1Text(string keyword, string text)
    {
        using var output = new MemoryStream();
        output.Write(Encoding.Latin1.GetBytes(keyword));
        output.WriteByte(0);
        output.WriteByte(0);
        output.Write(Zlib(Encoding.Latin1.GetBytes(text)));
        return output.ToArray();
    }

    private static byte[] InternationalText(string keyword, string text)
    {
        using var output = new MemoryStream();
        output.Write(Encoding.Latin1.GetBytes(keyword));
        output.WriteByte(0);
        output.WriteByte(0);
        output.WriteByte(0);
        output.WriteByte(0);
        output.WriteByte(0);
        output.Write(Encoding.UTF8.GetBytes(text));
        return output.ToArray();
    }

    private static void WriteUInt32(Stream output, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        output.Write(buffer);
    }

    private static void WriteUInt16(Stream output, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        output.Write(buffer);
    }

    private static byte[] Zlib(byte[] data)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, true))
        {
            zlib.Write(data);
        }

        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)data.Length);
        output.Write(buffer);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        using var crcInput = new MemoryStream();
        crcInput.Write(typeBytes);
        crcInput.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, Crc32(crcInput.ToArray()));
        output.Write(buffer);
    }

    private static uint Crc32(byte[] data)
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

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0)
            return true;

        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var found = true;
            for (var j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j])
                {
                    found = false;
                    break;
                }

            if (found)
                return true;
        }

        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static HaruException RequireHaruException(Action action)
    {
        try
        {
            action();
        }
        catch (HaruException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected a HaruException.");
    }
}