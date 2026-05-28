namespace LibHaru.Internal;

internal static class Base14Fonts
{
    private const int FontFixedWidth = 1;
    private const int FontSerif = 2;
    private const int FontSymbolic = 4;
    private const int FontStdCharset = 32;
    private const int FontItalic = 64;

    private static readonly HashSet<string> SupportedNames = new(Base14FontData.Names, StringComparer.Ordinal);

    internal static bool IsSupported(string name) => SupportedNames.Contains(name);

    internal static bool IsFontSpecific(string name) => IsSupported(name) && Base14FontData.Get(name).IsFontSpecific;

    internal static PdfFontProgram CreateProgram(string name)
    {
        var metric = Base14FontData.Get(name);
        var flags = FontStdCharset;

        if (name.StartsWith("Courier", StringComparison.Ordinal))
            flags |= FontFixedWidth;

        if (name.StartsWith("Times", StringComparison.Ordinal))
            flags |= FontSerif;

        if (name.Contains("Oblique", StringComparison.Ordinal) || name.Contains("Italic", StringComparison.Ordinal))
            flags |= FontItalic;

        if (name is "Symbol" or "ZapfDingbats")
            flags = FontSymbolic;

        var descriptor = new PdfFontDescriptor(
            name,
            flags,
            metric.FontBBox,
            (flags & FontItalic) != 0 ? -12 : 0,
            metric.Ascent,
            metric.Descent,
            metric.CapHeight,
            metric.XHeight,
            80,
            metric.MissingWidth);

        return new PdfFontProgram(
            PdfFontProgramKind.Type1,
            name,
            descriptor,
            unicodeWidthResolver: unicode => Base14FontData.WidthOfUnicode(name, unicode),
            isBase14: true);
    }

    internal static double TextWidth(string fontName, string text, double fontSize)
    {
        var units = 0;

        foreach (var ch in text)
            units += GlyphWidth(fontName, ch);

        return units * fontSize / 1000.0;
    }

    private static int GlyphWidth(string fontName, char ch) => Base14FontData.WidthOfUnicode(fontName, ch);
}
