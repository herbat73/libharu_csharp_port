using System.Security.Cryptography;

public static class ReferenceOutputRegression
{
    public static void TryCompareExactPdfs(string repoRoot, string artifactsRoot, string fixturePath)
    {
        if (!File.Exists(fixturePath))
            throw new FileNotFoundException("Cannot load exact reference-output fixtures.", fixturePath);

        var fixtures = LoadFixtures(fixturePath);
        if (fixtures.Count == 0)
        {
            Console.WriteLine("No upstream reference-output fixtures configured.");
            return;
        }

        var exactCount = 0;
        var hashOnlyCount = 0;
        var generatedHashCount = 0;
        var skippedCount = 0;
        foreach (var fixture in fixtures)
        {
            var referencePath = string.IsNullOrWhiteSpace(fixture.ReferencePath) || fixture.ReferencePath == "-"
                ? null
                : ResolvePath(repoRoot, fixture.ReferencePath);

            if (fixture.GeneratedPath == "-")
            {
                if (referencePath is null)
                    throw new InvalidOperationException($"{fixturePath}: reference hash-only rows must include a checked-in reference PDF path.");

                if (!File.Exists(referencePath))
                    throw new FileNotFoundException("Cannot load checked-in reference PDF for hash validation.", referencePath);

                if (string.IsNullOrEmpty(fixture.ReferenceSha256))
                    throw new InvalidOperationException($"{fixture.ReferencePath}: reference hash-only rows must include an expected SHA-256.");

                ValidateReferenceHash(fixture, File.ReadAllBytes(referencePath));
                hashOnlyCount++;
                continue;
            }

            var generatedPath = ResolvePath(artifactsRoot, fixture.GeneratedPath);
            if (!File.Exists(generatedPath))
                throw new FileNotFoundException("Cannot compare missing generated PDF.", generatedPath);

            var generatedBytes = File.ReadAllBytes(generatedPath);
            var generatedSha256 = Sha256(generatedBytes);

            if (referencePath is not null && File.Exists(referencePath))
            {
                var referenceBytes = File.ReadAllBytes(referencePath);
                var referenceSha256 = ValidateReferenceHash(fixture, referenceBytes);

                if (!generatedBytes.SequenceEqual(referenceBytes))
                    throw new InvalidOperationException($"{fixture.GeneratedPath}: exact upstream fixture mismatch. Generated {generatedBytes.Length} bytes/{generatedSha256}; reference {referenceBytes.Length} bytes/{referenceSha256}.");

                exactCount++;
                continue;
            }

            if (!string.IsNullOrEmpty(fixture.ReferenceSha256))
            {
                if (!string.Equals(generatedSha256, fixture.ReferenceSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"{fixture.GeneratedPath}: exact reference hash mismatch. Generated {generatedSha256}, expected {fixture.ReferenceSha256}.");

                generatedHashCount++;
                continue;
            }

            skippedCount++;
        }

        Console.WriteLine($"Checked {exactCount} exact upstream reference PDF fixture(s), {generatedHashCount} generated SHA-256 fixture(s), and {hashOnlyCount} checked-in upstream SHA-256 fixture(s); skipped {skippedCount} unavailable fixture(s).");
    }

    private static List<ReferenceFixture> LoadFixtures(string fixturePath)
    {
        var fixtures = new List<ReferenceFixture>();
        var lineNumber = 0;
        foreach (var rawLine in File.ReadLines(fixturePath))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var parts = line.Split('\t');
            if (parts.Length is < 2 or > 3)
                throw new InvalidOperationException($"{fixturePath}:{lineNumber}: expected two or three tab-separated fields.");

            fixtures.Add(new ReferenceFixture(
                parts[0],
                parts[1],
                parts.Length == 3 && parts[2] != "-" ? parts[2] : string.Empty));
        }

        return fixtures;
    }

    private static string ResolvePath(string root, string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(root, path);

    private static string ValidateReferenceHash(ReferenceFixture fixture, byte[] referenceBytes)
    {
        var referenceSha256 = Sha256(referenceBytes);
        if (!string.IsNullOrEmpty(fixture.ReferenceSha256) &&
            !string.Equals(referenceSha256, fixture.ReferenceSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{fixture.ReferencePath}: reference fixture hash changed. Expected {fixture.ReferenceSha256}, actual {referenceSha256}.");
        }

        return referenceSha256;
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed record ReferenceFixture(string GeneratedPath, string ReferencePath, string ReferenceSha256);
}
