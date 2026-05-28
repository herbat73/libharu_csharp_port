using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text;

public static class VisualRegression
{
    private const string PdftoppmPathEnvVar = "LIBHARU_PDFTOPPM";
    private const string RefreshReferencesEnvVar = "LIBHARU_REFRESH_VISUAL_REFERENCES";

    public static void TryRenderSmokePdfs(string artifactsRoot, string fixturePath, params string[] pdfPaths)
    {
        var pdftoppm = FindPdftoppm();
        if (pdftoppm is null)
        {
            Console.WriteLine(
                $"Skipped visual render checks; pdftoppm was not found on PATH. Set {PdftoppmPathEnvVar} to a pdftoppm executable or containing directory to enable them.");
            return;
        }

        var refreshReferences = IsEnabled(Environment.GetEnvironmentVariable(RefreshReferencesEnvVar));
        var fixtures = File.Exists(fixturePath)
            ? LoadFixtures(fixturePath)
            : new Dictionary<string, VisualFixture>(StringComparer.OrdinalIgnoreCase);
        if (!refreshReferences && fixtures.Count == 0)
            throw new FileNotFoundException("Cannot load visual reference fixtures.", fixturePath);

        var renderDir = Path.Combine(artifactsRoot, "rendered");
        Directory.CreateDirectory(renderDir);
        var preferredOrder = new List<string>();
        var seenPdfNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pdfPath in pdfPaths)
        {
            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("Cannot render missing PDF.", pdfPath);

            var pdfName = Path.GetFileName(pdfPath);
            if (seenPdfNames.Add(pdfName))
                preferredOrder.Add(pdfName);

            var outputPrefix = Path.Combine(renderDir, Path.GetFileNameWithoutExtension(pdfPath));
            Run(pdftoppm, "-png", "-singlefile", "-f", "1", "-l", "1", pdfPath, outputPrefix);
            var pngPath = outputPrefix + ".png";
            var pngBytes = File.ReadAllBytes(pngPath);
            var profile = ReadPngProfile(pngBytes, pngPath);

            if (refreshReferences)
                fixtures[pdfName] = CreateRefreshedFixture(pdfName, profile);

            if (!fixtures.TryGetValue(pdfName, out var fixture))
                throw new InvalidOperationException(
                    $"Missing visual fixture for {pdfName}. Set {RefreshReferencesEnvVar}=1 to refresh {fixturePath} from the current renders.");

            File.WriteAllText(outputPrefix + ".render-profile.txt", BuildProfile(pdfName, fixture, profile));
            CheckProfile(pdfName, fixture, profile);
        }

        if (refreshReferences)
        {
            WriteFixtures(fixturePath, fixtures, preferredOrder);
            Console.WriteLine($"Refreshed {fixtures.Count} visual reference fixture(s) in {fixturePath}");
        }

        Console.WriteLine(
            $"Rendered and checked {pdfPaths.Length} PDF reference page(s) in {renderDir} using {pdftoppm}");
    }

    private static Dictionary<string, VisualFixture> LoadFixtures(string fixturePath)
    {
        if (!File.Exists(fixturePath))
            throw new FileNotFoundException("Cannot load visual reference fixtures.", fixturePath);

        var fixtures = new Dictionary<string, VisualFixture>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(fixturePath))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var parts = line.Split('\t');
            if (parts.Length != 5)
                throw new InvalidOperationException($"{fixturePath}:{lineNumber}: expected five tab-separated fields.");

            fixtures[parts[0]] = new VisualFixture(
                parts[0],
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture),
                int.Parse(parts[3], CultureInfo.InvariantCulture),
                int.Parse(parts[4], CultureInfo.InvariantCulture));
        }

        return fixtures;
    }

    private static void CheckProfile(string pdfName, VisualFixture fixture, PngProfile profile)
    {
        var failures = new List<string>();
        if (profile.Width < fixture.MinWidth)
            failures.Add($"width {profile.Width} < {fixture.MinWidth}");
        if (profile.Height < fixture.MinHeight)
            failures.Add($"height {profile.Height} < {fixture.MinHeight}");
        if (profile.NonWhitePixels < fixture.MinNonWhitePixels)
            failures.Add($"non-white pixels {profile.NonWhitePixels} < {fixture.MinNonWhitePixels}");
        if (profile.ColorCount < fixture.MinColorCount)
            failures.Add($"color count {profile.ColorCount} < {fixture.MinColorCount}");

        if (failures.Count > 0)
            throw new InvalidOperationException($"{pdfName} visual fixture mismatch: {string.Join("; ", failures)}");
    }

    private static string BuildProfile(string pdfName, VisualFixture fixture, PngProfile profile)
    {
        return string.Join(Environment.NewLine, $"pdf: {pdfName}", $"width: {profile.Width} (min {fixture.MinWidth})",
            $"height: {profile.Height} (min {fixture.MinHeight})",
            $"nonWhitePixels: {profile.NonWhitePixels} (min {fixture.MinNonWhitePixels})",
            $"colorCount: {profile.ColorCount} (min {fixture.MinColorCount})", $"colorType: {profile.ColorType}",
            string.Empty);
    }

    private static VisualFixture CreateRefreshedFixture(string pdfName, PngProfile profile)
    {
        var minNonWhitePixels = profile.NonWhitePixels == 0
            ? 0
            : Math.Max(1, (int)Math.Floor(profile.NonWhitePixels * 0.90));

        var minColorCount = profile.ColorCount <= 2
            ? profile.ColorCount
            : Math.Max(2, (int)Math.Floor(profile.ColorCount * 0.50));

        return new VisualFixture(
            pdfName,
            profile.Width,
            profile.Height,
            minNonWhitePixels,
            minColorCount);
    }

    private static void WriteFixtures(string fixturePath, Dictionary<string, VisualFixture> fixtures,
        IReadOnlyList<string> preferredOrder)
    {
        var directory = Path.GetDirectoryName(fixturePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var writer = new StreamWriter(fixturePath, false, new UTF8Encoding(false));
        writer.WriteLine("# pdf\tmin_width\tmin_height\tmin_non_white_pixels\tmin_color_count");
        writer.WriteLine($"# Refresh with {RefreshReferencesEnvVar}=1 when pdftoppm is available.");

        var written = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pdfName in preferredOrder)
            if (fixtures.TryGetValue(pdfName, out var fixture) && written.Add(pdfName))
                WriteFixtureLine(writer, fixture);

        foreach (var fixture in fixtures.Values.OrderBy(static fixture => fixture.PdfName,
                     StringComparer.OrdinalIgnoreCase))
            if (written.Add(fixture.PdfName))
                WriteFixtureLine(writer, fixture);
    }

    private static void WriteFixtureLine(TextWriter writer, VisualFixture fixture)
    {
        writer.Write(fixture.PdfName);
        writer.Write('\t');
        writer.Write(fixture.MinWidth.ToString(CultureInfo.InvariantCulture));
        writer.Write('\t');
        writer.Write(fixture.MinHeight.ToString(CultureInfo.InvariantCulture));
        writer.Write('\t');
        writer.Write(fixture.MinNonWhitePixels.ToString(CultureInfo.InvariantCulture));
        writer.Write('\t');
        writer.WriteLine(fixture.MinColorCount.ToString(CultureInfo.InvariantCulture));
    }

    private static PngProfile ReadPngProfile(byte[] bytes, string path)
    {
        RequirePng(bytes, path);

        var offset = 8;
        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        var interlaceMethod = 0;
        using var idat = new MemoryStream();

        while (offset < bytes.Length)
        {
            if (offset > bytes.Length - 12)
                throw new InvalidOperationException($"PNG chunk is truncated: {path}");

            var length = ReadBigEndianInt32(bytes, offset);
            var chunkType = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var chunkDataOffset = offset + 8;
            if (length < 0 || chunkDataOffset > bytes.Length - length - 4)
                throw new InvalidOperationException($"PNG chunk length is invalid: {path}");

            if (chunkType == "IHDR")
            {
                width = ReadBigEndianInt32(bytes, chunkDataOffset);
                height = ReadBigEndianInt32(bytes, chunkDataOffset + 4);
                bitDepth = bytes[chunkDataOffset + 8];
                colorType = bytes[chunkDataOffset + 9];
                interlaceMethod = bytes[chunkDataOffset + 12];
            }
            else if (chunkType == "IDAT")
            {
                idat.Write(bytes, chunkDataOffset, length);
            }
            else if (chunkType == "IEND")
            {
                break;
            }

            offset = chunkDataOffset + length + 4;
        }

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException($"Rendered PNG has invalid dimensions: {path}");
        if (bitDepth != 8 || interlaceMethod != 0)
            throw new InvalidOperationException($"Rendered PNG uses an unsupported pixel format: {path}");

        var bytesPerPixel = colorType switch
        {
            0 => 1,
            2 => 3,
            4 => 2,
            6 => 4,
            _ => throw new InvalidOperationException($"Rendered PNG color type {colorType} is unsupported: {path}")
        };

        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var rawStream = new MemoryStream();
        zlib.CopyTo(rawStream);
        var raw = rawStream.ToArray();
        var stride = checked(width * bytesPerPixel);
        var expectedRawLength = checked((stride + 1) * height);
        if (raw.Length < expectedRawLength)
            throw new InvalidOperationException($"Rendered PNG pixel data is truncated: {path}");

        var previous = new byte[stride];
        var current = new byte[stride];
        var nonWhitePixels = 0;
        var colors = new HashSet<int>();
        var rawOffset = 0;

        for (var y = 0; y < height; y++)
        {
            var filter = raw[rawOffset++];
            Array.Clear(current);

            for (var x = 0; x < stride; x++)
            {
                var encoded = raw[rawOffset++];
                var left = x >= bytesPerPixel ? current[x - bytesPerPixel] : 0;
                var up = previous[x];
                var upperLeft = x >= bytesPerPixel ? previous[x - bytesPerPixel] : 0;

                current[x] = filter switch
                {
                    0 => encoded,
                    1 => unchecked((byte)(encoded + left)),
                    2 => unchecked((byte)(encoded + up)),
                    3 => unchecked((byte)(encoded + (left + up) / 2)),
                    4 => unchecked((byte)(encoded + Paeth(left, up, upperLeft))),
                    _ => throw new InvalidOperationException($"Rendered PNG has unsupported filter {filter}: {path}")
                };
            }

            for (var x = 0; x < stride; x += bytesPerPixel)
            {
                var (red, green, blue, alpha) = ReadPixel(current, x, colorType);
                if (alpha > 0 && (red < 250 || green < 250 || blue < 250))
                    nonWhitePixels++;

                colors.Add((red << 24) | (green << 16) | (blue << 8) | alpha);
            }

            (previous, current) = (current, previous);
        }

        return new PngProfile(width, height, colorType, nonWhitePixels, colors.Count);
    }

    private static (int Red, int Green, int Blue, int Alpha) ReadPixel(byte[] scanline, int offset, int colorType)
    {
        return colorType switch
        {
            0 => (scanline[offset], scanline[offset], scanline[offset], 255),
            2 => (scanline[offset], scanline[offset + 1], scanline[offset + 2], 255),
            4 => (scanline[offset], scanline[offset], scanline[offset], scanline[offset + 1]),
            6 => (scanline[offset], scanline[offset + 1], scanline[offset + 2], scanline[offset + 3]),
            _ => (0, 0, 0, 0)
        };
    }

    private static int Paeth(int left, int up, int upperLeft)
    {
        var p = left + up - upperLeft;
        var pa = Math.Abs(p - left);
        var pb = Math.Abs(p - up);
        var pc = Math.Abs(p - upperLeft);

        if (pa <= pb && pa <= pc)
            return left;
        return pb <= pc ? up : upperLeft;
    }

    private static string? FindPdftoppm()
    {
        var configured = Environment.GetEnvironmentVariable(PdftoppmPathEnvVar);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var resolved = ResolveConfiguredPdftoppm(configured);
            if (resolved is not null)
                return resolved;

            throw new FileNotFoundException($"The {PdftoppmPathEnvVar} value does not point to pdftoppm.", configured);
        }

        return FindOnPath("pdftoppm");
    }

    private static string? ResolveConfiguredPdftoppm(string configured)
    {
        var candidate = configured.Trim().Trim('"');
        if (File.Exists(candidate))
            return candidate;

        if (Directory.Exists(candidate))
            return FindInDirectory(candidate, "pdftoppm");

        return null;
    }

    private static string? FindOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        foreach (var directory in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
                continue;

            var candidate = FindInDirectory(directory, fileName);
            if (candidate is not null)
                return candidate;
        }

        return null;
    }

    private static string? FindInDirectory(string directory, string fileName)
    {
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD").Split(';',
                StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];

        foreach (var extension in extensions)
        {
            var candidate = Path.Combine(directory, fileName + extension);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsEnabled(string? value)
    {
        var normalized = value?.Trim();
        return normalized is not null
               && (normalized == "1"
                   || normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
                   || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    private static void Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo);

        if (process is null)
            throw new InvalidOperationException($"Could not start {fileName}.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{Path.GetFileName(fileName)} failed with exit code {process.ExitCode}: {stderr}{stdout}");
    }

    private static void RequirePng(byte[] bytes, string path)
    {
        if (bytes.Length < 33)
            throw new InvalidOperationException($"Rendered PNG is unexpectedly small: {path}");

        if (bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47)
            throw new InvalidOperationException($"Rendered output is not a PNG: {path}");
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private sealed record VisualFixture(
        string PdfName,
        int MinWidth,
        int MinHeight,
        int MinNonWhitePixels,
        int MinColorCount);

    private sealed record PngProfile(int Width, int Height, int ColorType, int NonWhitePixels, int ColorCount);
}