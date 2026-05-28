using System.Globalization;
using System.Text;
using LibHaru;
using static LibHaru.HPdf;

public static class ImageFixtureCoverage
{
    private static readonly string[] AutoLoadExtensions = [".jpg", ".jpeg", ".png"];

    public static void Test(string repoRoot, string artifactsDir)
    {
        var fixtureDir = Path.Combine(repoRoot, "LibHaru.SmokeTests", "fixtures", "images");
        if (!Directory.Exists(fixtureDir))
        {
            Console.WriteLine($"No optional external image fixtures found at {fixtureDir}");
            return;
        }

        var manifestPath = Path.Combine(fixtureDir, "image-fixtures.tsv");
        var fixtures = LoadManifest(fixtureDir, manifestPath);
        var manifestPaths = fixtures
            .Select(static fixture => fixture.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        fixtures.AddRange(Directory.EnumerateFiles(fixtureDir, "*", SearchOption.AllDirectories)
            .Where(static path => AutoLoadExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !manifestPaths.Contains(Path.GetFullPath(path)))
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new ImageFixture(InferKind(path), path)));

        if (fixtures.Count == 0)
        {
            Console.WriteLine($"No optional external image fixtures found at {fixtureDir}");
            return;
        }

        var outputDir = Path.Combine(artifactsDir, "image-fixtures");
        Directory.CreateDirectory(outputDir);
        var pdfPath = Path.Combine(outputDir, "external-image-fixtures.pdf");

        using var pdf = HPDF_New();
        HPDF_SetCompressionMode(pdf, CompressionMode.All);
        var font = HPDF_GetFont(pdf, "Helvetica");
        PdfPage? page = null;
        var slot = 0;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var fixture in fixtures)
        {
            if (slot % 12 == 0)
            {
                page = HPDF_AddPage(pdf);
                HPDF_Page_SetFontAndSize(page, font, 10);
                HPDF_Page_TextOut(page, 40, HPDF_Page_GetHeight(page) - 40, "External image compatibility fixtures");
            }

            var image = LoadImage(pdf, fixture);
            ValidateFixtureImage(fixture, image);
            counts[fixture.Kind] = counts.TryGetValue(fixture.Kind, out var count) ? count + 1 : 1;

            DrawFixture(page!, font, image, fixture, slot % 12);
            slot++;
        }

        HPDF_SaveToFile(pdf, pdfPath);
        var latin1 = Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));

        Require(Count(latin1, "/Subtype /Image") >= fixtures.Count, "Missing image XObjects for external fixtures.");
        Require(latin1.Contains("/XObject", StringComparison.Ordinal), "Missing page XObject resources for external fixtures.");

        if (counts.ContainsKey("jpeg"))
            Require(latin1.Contains("/DCTDecode", StringComparison.Ordinal), "External JPEG fixtures did not emit DCTDecode.");

        if (counts.ContainsKey("png") || counts.ContainsKey("raw"))
            Require(latin1.Contains("/FlateDecode", StringComparison.Ordinal), "External PNG/raw fixtures did not emit FlateDecode.");

        if (counts.ContainsKey("ccitt"))
        {
            Require(latin1.Contains("/CCITTFaxDecode", StringComparison.Ordinal), "External 1-bit fixtures did not emit CCITT compression.");
            Require(latin1.Contains("/DecodeParms [", StringComparison.Ordinal), "External 1-bit fixtures did not emit CCITT DecodeParms.");
            Require(latin1.Contains("/K -1", StringComparison.Ordinal), "External 1-bit fixtures did not emit Group 4 CCITT parameters.");
            Require(latin1.Contains("/BlackIs1 ", StringComparison.Ordinal), "External 1-bit fixtures did not emit BlackIs1.");
        }

        Console.WriteLine($"Generated {pdfPath}");
        Console.WriteLine($"Exercised {fixtures.Count} optional external image fixture(s)");
    }

    private static List<ImageFixture> LoadManifest(string fixtureDir, string manifestPath)
    {
        var fixtures = new List<ImageFixture>();
        if (!File.Exists(manifestPath))
            return fixtures;

        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(manifestPath))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var parts = rawLine.Split('\t');
            if (parts.Length < 2)
                throw new InvalidOperationException($"Image fixture manifest line {lineNumber} must include kind and path.");

            var kind = NormalizeKind(parts[0]);
            var fullPath = ResolveFixturePath(fixtureDir, parts[1]);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Image fixture manifest line {lineNumber} references a missing file.", fullPath);

            fixtures.Add(new ImageFixture(
                kind,
                fullPath,
                Width: ParseOptionalInt(parts, 2, lineNumber, "width"),
                Height: ParseOptionalInt(parts, 3, lineNumber, "height"),
                ColorSpace: ParseOptionalColorSpace(parts, 4, lineNumber),
                BitsPerComponent: ParseOptionalInt(parts, 5, lineNumber, "bitsPerComponent"),
                LineWidth: ParseOptionalInt(parts, 6, lineNumber, "lineWidth"),
                BlackIs1: ParseOptionalBool(parts, 7, defaultValue: true, lineNumber, "blackIs1"),
                TopIsFirst: ParseOptionalBool(parts, 8, defaultValue: true, lineNumber, "topIsFirst")));
        }

        return fixtures;
    }

    private static PdfImage LoadImage(PdfDocument pdf, ImageFixture fixture)
    {
        return fixture.Kind switch
        {
            "jpeg" => HPDF_LoadJpegImageFromFile(pdf, fixture.FullPath),
            "png" => HPDF_LoadPngImageFromFile(pdf, fixture.FullPath),
            "raw" => HPDF_LoadRawImageFromMem(
                pdf,
                File.ReadAllBytes(fixture.FullPath),
                (uint)Required(fixture.Width, fixture, "width"),
                (uint)Required(fixture.Height, fixture, "height"),
                fixture.ColorSpace ?? throw new InvalidOperationException($"Raw image fixture {fixture.DisplayName} must specify a color space."),
                (uint)(fixture.BitsPerComponent ?? 8)),
            "ccitt" => HPDF_Image_LoadRaw1BitImageFromMem(
                pdf,
                File.ReadAllBytes(fixture.FullPath),
                (uint)Required(fixture.Width, fixture, "width"),
                (uint)Required(fixture.Height, fixture, "height"),
                (uint)(fixture.LineWidth ?? ((Required(fixture.Width, fixture, "width") + 7) / 8)),
                fixture.BlackIs1,
                fixture.TopIsFirst),
            _ => throw new InvalidOperationException($"Unsupported image fixture kind: {fixture.Kind}.")
        };
    }

    private static void ValidateFixtureImage(ImageFixture fixture, PdfImage image)
    {
        Require(HPDF_Image_Validate(image), $"Image fixture failed validation: {fixture.DisplayName}.");
        Require(HPDF_Image_GetWidth(image) > 0 && HPDF_Image_GetHeight(image) > 0, $"Image fixture has invalid dimensions: {fixture.DisplayName}.");
        Require(HPDF_Image_GetBitsPerComponent(image) > 0, $"Image fixture has invalid bit depth: {fixture.DisplayName}.");

        if (fixture.Width is not null)
            Require(HPDF_Image_GetWidth(image) == (uint)fixture.Width.Value, $"Image fixture width mismatch: {fixture.DisplayName}.");
        if (fixture.Height is not null)
            Require(HPDF_Image_GetHeight(image) == (uint)fixture.Height.Value, $"Image fixture height mismatch: {fixture.DisplayName}.");
        if (fixture.ColorSpace is not null)
            Require(HPDF_Image_GetColorSpace(image) == ColorSpaceName(fixture.ColorSpace.Value), $"Image fixture color space mismatch: {fixture.DisplayName}.");
        if (fixture.BitsPerComponent is not null)
            Require(HPDF_Image_GetBitsPerComponent(image) == (uint)fixture.BitsPerComponent.Value, $"Image fixture bit-depth mismatch: {fixture.DisplayName}.");
    }

    private static void DrawFixture(PdfPage page, PdfFont font, PdfImage image, ImageFixture fixture, int slot)
    {
        var column = slot % 4;
        var row = slot / 4;
        var x = 40 + column * 130;
        var y = HPDF_Page_GetHeight(page) - 150 - row * 155;
        var scale = Math.Min(96.0 / Math.Max(1, image.Width), 96.0 / Math.Max(1, image.Height));
        var width = Math.Max(8, image.Width * scale);
        var height = Math.Max(8, image.Height * scale);

        HPDF_Page_DrawImage(page, image, x, y, width, height);
        Require(HPDF_Page_GetXObjectName(page, image).StartsWith("Im", StringComparison.Ordinal), $"Image fixture did not receive an XObject name: {fixture.DisplayName}.");

        HPDF_Page_SetFontAndSize(page, font, 7);
        HPDF_Page_TextOut(page, x, y - 12, Truncate($"{fixture.Kind}: {fixture.DisplayName}", 32));
        HPDF_Page_TextOut(page, x, y - 22, $"{image.Width}x{image.Height} {HPDF_Image_GetColorSpace(image)} {image.BitsPerComponent}bpc");
    }

    private static string ResolveFixturePath(string fixtureDir, string path)
    {
        var normalized = path.Trim();
        return Path.GetFullPath(Path.IsPathRooted(normalized)
            ? normalized
            : Path.Combine(fixtureDir, normalized));
    }

    private static string InferKind(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpeg";
    }

    private static string NormalizeKind(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "jpeg",
            "png" => "png",
            "raw" => "raw",
            "raw1" or "raw1bit" or "ccitt" or "ccitt-g4" => "ccitt",
            var kind => kind
        };
    }

    private static int? ParseOptionalInt(string[] parts, int index, int lineNumber, string name)
    {
        if (index >= parts.Length || string.IsNullOrWhiteSpace(parts[index]) || parts[index] == "-")
            return null;

        return int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidOperationException($"Image fixture manifest line {lineNumber} has an invalid {name} value.");
    }

    private static bool ParseOptionalBool(string[] parts, int index, bool defaultValue, int lineNumber, string name)
    {
        if (index >= parts.Length || string.IsNullOrWhiteSpace(parts[index]) || parts[index] == "-")
            return defaultValue;

        return bool.TryParse(parts[index], out var value)
            ? value
            : throw new InvalidOperationException($"Image fixture manifest line {lineNumber} has an invalid {name} value.");
    }

    private static PdfColorSpace? ParseOptionalColorSpace(string[] parts, int index, int lineNumber)
    {
        if (index >= parts.Length || string.IsNullOrWhiteSpace(parts[index]) || parts[index] == "-")
            return null;

        var value = parts[index].Trim();
        if (Enum.TryParse<PdfColorSpace>(value, ignoreCase: true, out var colorSpace))
            return colorSpace;

        return value.Equals("DeviceRGB", StringComparison.OrdinalIgnoreCase) ? PdfColorSpace.DeviceRgb
            : value.Equals("DeviceGray", StringComparison.OrdinalIgnoreCase) ? PdfColorSpace.DeviceGray
            : value.Equals("DeviceCMYK", StringComparison.OrdinalIgnoreCase) ? PdfColorSpace.DeviceCmyk
            : throw new InvalidOperationException($"Image fixture manifest line {lineNumber} has an invalid colorSpace value.");
    }

    private static int Required(int? value, ImageFixture fixture, string name)
    {
        return value ?? throw new InvalidOperationException($"{fixture.Kind} image fixture {fixture.DisplayName} must specify {name}.");
    }

    private static string ColorSpaceName(PdfColorSpace colorSpace)
    {
        return colorSpace switch
        {
            PdfColorSpace.DeviceGray => "DeviceGray",
            PdfColorSpace.DeviceRgb => "DeviceRGB",
            PdfColorSpace.DeviceCmyk => "DeviceCMYK",
            _ => throw new InvalidOperationException($"Unsupported raw fixture color space: {colorSpace}.")
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
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

    private sealed record ImageFixture(
        string Kind,
        string FullPath,
        int? Width = null,
        int? Height = null,
        PdfColorSpace? ColorSpace = null,
        int? BitsPerComponent = null,
        int? LineWidth = null,
        bool BlackIs1 = true,
        bool TopIsFirst = true)
    {
        internal string DisplayName => Path.GetFileName(FullPath);
    }
}
