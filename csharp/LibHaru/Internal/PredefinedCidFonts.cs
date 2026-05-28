namespace LibHaru.Internal;

internal static class PredefinedCidFonts
{
    private const int FontFixedWidth = 1;
    private const int FontSerif = 2;
    private const int FontSymbolic = 4;
    private const int FontItalic = 64;
    private const int FontForceBold = 262144;

    private static readonly Dictionary<string, (string Ordering, int Supplement)> SupportedNames = new(StringComparer.Ordinal)
    {
        ["MS-Mincho"] = ("Japan1", 2),
        ["MS-Mincho,Bold"] = ("Japan1", 2),
        ["MS-Mincho,Italic"] = ("Japan1", 2),
        ["MS-Mincho,BoldItalic"] = ("Japan1", 2),
        ["MS-PMincho"] = ("Japan1", 2),
        ["MS-PMincho,Bold"] = ("Japan1", 2),
        ["MS-PMincho,Italic"] = ("Japan1", 2),
        ["MS-PMincho,BoldItalic"] = ("Japan1", 2),
        ["MS-Gothic"] = ("Japan1", 2),
        ["MS-Gothic,Bold"] = ("Japan1", 2),
        ["MS-Gothic,Italic"] = ("Japan1", 2),
        ["MS-Gothic,BoldItalic"] = ("Japan1", 2),
        ["MS-PGothic"] = ("Japan1", 2),
        ["MS-PGothic,Bold"] = ("Japan1", 2),
        ["MS-PGothic,Italic"] = ("Japan1", 2),
        ["MS-PGothic,BoldItalic"] = ("Japan1", 2),
        ["DotumChe"] = ("Korea1", 1),
        ["DotumChe,Bold"] = ("Korea1", 1),
        ["DotumChe,Italic"] = ("Korea1", 1),
        ["DotumChe,BoldItalic"] = ("Korea1", 1),
        ["Dotum"] = ("Korea1", 1),
        ["Dotum,Bold"] = ("Korea1", 1),
        ["Dotum,Italic"] = ("Korea1", 1),
        ["Dotum,BoldItalic"] = ("Korea1", 1),
        ["BatangChe"] = ("Korea1", 1),
        ["BatangChe,Bold"] = ("Korea1", 1),
        ["BatangChe,Italic"] = ("Korea1", 1),
        ["BatangChe,BoldItalic"] = ("Korea1", 1),
        ["Batang"] = ("Korea1", 1),
        ["Batang,Bold"] = ("Korea1", 1),
        ["Batang,Italic"] = ("Korea1", 1),
        ["Batang,BoldItalic"] = ("Korea1", 1),
        ["SimSun"] = ("GB1", 2),
        ["SimSun,Bold"] = ("GB1", 2),
        ["SimSun,Italic"] = ("GB1", 2),
        ["SimSun,BoldItalic"] = ("GB1", 2),
        ["SimHei"] = ("GB1", 2),
        ["SimHei,Bold"] = ("GB1", 2),
        ["SimHei,Italic"] = ("GB1", 2),
        ["SimHei,BoldItalic"] = ("GB1", 2),
        ["MingLiU"] = ("CNS1", 0),
        ["MingLiU,Bold"] = ("CNS1", 0),
        ["MingLiU,Italic"] = ("CNS1", 0),
        ["MingLiU,BoldItalic"] = ("CNS1", 0)
    };

    internal static bool IsSupported(string name) => SupportedNames.ContainsKey(name);

    internal static PdfFontProgram CreateProgram(string name)
    {
        var (ordering, supplement) = SupportedNames[name];
        var familyMetrics = MetricsForFamily(FamilyName(name));
        var flags = familyMetrics.Flags;
        var italicAngle = familyMetrics.ItalicAngle;
        var stemV = familyMetrics.StemV;

        if (name.Contains("Bold", StringComparison.Ordinal))
        {
            flags |= FontForceBold;
            stemV *= 2;
        }

        if (name.Contains("Italic", StringComparison.Ordinal))
        {
            flags |= FontItalic;
            italicAngle -= 11;
        }

        var descriptor = new PdfFontDescriptor(
            name,
            flags,
            familyMetrics.BBox,
            italicAngle,
            familyMetrics.Ascent,
            familyMetrics.Descent,
            familyMetrics.CapHeight,
            0,
            stemV,
            500);

        return new PdfFontProgram(
            PdfFontProgramKind.CidType0,
            name,
            descriptor,
            isBase14: true,
            cidOrdering: ordering,
            cidSupplement: supplement,
            cidWidths: CjkCMapData.CidWidthsForFont(name),
            cidDefaultWidth: 1000);
    }

    private static string FamilyName(string name)
    {
        var comma = name.IndexOf(',');
        return comma < 0 ? name : name[..comma];
    }

    private static CidFamilyMetrics MetricsForFamily(string family) => family switch
    {
        "MS-Gothic" => new CidFamilyMetrics(new PdfRect(0, -136, 1000, 859), 859, -140, 769, FontSymbolic | FontFixedWidth, 0, 78),
        "MS-PGothic" => new CidFamilyMetrics(new PdfRect(-121, -136, 996, 859), 859, -140, 679, FontSymbolic, 0, 78),
        "MS-Mincho" => new CidFamilyMetrics(new PdfRect(0, -136, 1000, 859), 859, -140, 769, FontSymbolic | FontFixedWidth | FontSerif, 0, 78),
        "MS-PMincho" => new CidFamilyMetrics(new PdfRect(-82, -136, 996, 859), 859, -140, 679, FontSymbolic | FontSerif, 0, 78),
        "DotumChe" => new CidFamilyMetrics(new PdfRect(0, -150, 1000, 863), 858, -141, 679, FontSymbolic | FontFixedWidth, 0, 78),
        "Dotum" => new CidFamilyMetrics(new PdfRect(0, -150, 1000, 863), 858, -141, 679, FontSymbolic, 0, 78),
        "BatangChe" => new CidFamilyMetrics(new PdfRect(0, -154, 1000, 861), 858, -141, 769, FontSymbolic | FontFixedWidth | FontSerif, 0, 78),
        "Batang" => new CidFamilyMetrics(new PdfRect(0, -154, 1000, 861), 858, -141, 679, FontSymbolic | FontSerif, 0, 78),
        "SimSun" => new CidFamilyMetrics(new PdfRect(0, -140, 996, 855), 859, -140, 683, FontSymbolic | FontFixedWidth | FontSerif, 0, 78),
        "SimHei" => new CidFamilyMetrics(new PdfRect(0, -140, 996, 855), 859, -140, 769, FontSymbolic | FontFixedWidth, 0, 78),
        "MingLiU" => new CidFamilyMetrics(new PdfRect(0, -199, 1000, 800), 800, -199, 769, FontSymbolic | FontFixedWidth | FontSerif, 0, 78),
        _ => new CidFamilyMetrics(new PdfRect(0, -136, 1000, 859), 859, -140, 769, FontSymbolic, 0, 78)
    };

    private sealed record CidFamilyMetrics(PdfRect BBox, int Ascent, int Descent, int CapHeight, int Flags, int ItalicAngle, int StemV);
}
