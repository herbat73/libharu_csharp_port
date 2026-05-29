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
}
