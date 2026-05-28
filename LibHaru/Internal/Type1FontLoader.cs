using System.Globalization;
using System.Text;

namespace LibHaru.Internal;

internal static class Type1FontLoader
{
    private const int FontFixedWidth = 1;
    private const int FontStdCharset = 32;
    private const int FontItalic = 64;

    private static readonly Dictionary<string, int> GlyphUnicode = new(StringComparer.Ordinal)
    {
        ["space"] = ' ',
        ["exclam"] = '!',
        ["quotedbl"] = '"',
        ["numbersign"] = '#',
        ["dollar"] = '$',
        ["percent"] = '%',
        ["ampersand"] = '&',
        ["quotesingle"] = '\'',
        ["quoteright"] = '\'',
        ["parenleft"] = '(',
        ["parenright"] = ')',
        ["asterisk"] = '*',
        ["plus"] = '+',
        ["comma"] = ',',
        ["hyphen"] = '-',
        ["period"] = '.',
        ["slash"] = '/',
        ["zero"] = '0',
        ["one"] = '1',
        ["two"] = '2',
        ["three"] = '3',
        ["four"] = '4',
        ["five"] = '5',
        ["six"] = '6',
        ["seven"] = '7',
        ["eight"] = '8',
        ["nine"] = '9',
        ["colon"] = ':',
        ["semicolon"] = ';',
        ["less"] = '<',
        ["equal"] = '=',
        ["greater"] = '>',
        ["question"] = '?',
        ["at"] = '@',
        ["bracketleft"] = '[',
        ["backslash"] = '\\',
        ["bracketright"] = ']',
        ["asciicircum"] = '^',
        ["underscore"] = '_',
        ["grave"] = '`',
        ["braceleft"] = '{',
        ["bar"] = '|',
        ["braceright"] = '}',
        ["asciitilde"] = '~',
        ["exclamdown"] = 0x00A1,
        ["cent"] = 0x00A2,
        ["sterling"] = 0x00A3,
        ["currency"] = 0x00A4,
        ["yen"] = 0x00A5,
        ["brokenbar"] = 0x00A6,
        ["section"] = 0x00A7,
        ["dieresis"] = 0x00A8,
        ["copyright"] = 0x00A9,
        ["ordfeminine"] = 0x00AA,
        ["guillemotleft"] = 0x00AB,
        ["logicalnot"] = 0x00AC,
        ["registered"] = 0x00AE,
        ["macron"] = 0x00AF,
        ["degree"] = 0x00B0,
        ["plusminus"] = 0x00B1,
        ["twosuperior"] = 0x00B2,
        ["threesuperior"] = 0x00B3,
        ["acute"] = 0x00B4,
        ["mu"] = 0x00B5,
        ["paragraph"] = 0x00B6,
        ["periodcentered"] = 0x00B7,
        ["cedilla"] = 0x00B8,
        ["onesuperior"] = 0x00B9,
        ["ordmasculine"] = 0x00BA,
        ["guillemotright"] = 0x00BB,
        ["onequarter"] = 0x00BC,
        ["onehalf"] = 0x00BD,
        ["threequarters"] = 0x00BE,
        ["questiondown"] = 0x00BF,
        ["Agrave"] = 0x00C0,
        ["Aacute"] = 0x00C1,
        ["Acircumflex"] = 0x00C2,
        ["Atilde"] = 0x00C3,
        ["Adieresis"] = 0x00C4,
        ["Aring"] = 0x00C5,
        ["AE"] = 0x00C6,
        ["Ccedilla"] = 0x00C7,
        ["Egrave"] = 0x00C8,
        ["Eacute"] = 0x00C9,
        ["Ecircumflex"] = 0x00CA,
        ["Edieresis"] = 0x00CB,
        ["Igrave"] = 0x00CC,
        ["Iacute"] = 0x00CD,
        ["Icircumflex"] = 0x00CE,
        ["Idieresis"] = 0x00CF,
        ["Eth"] = 0x00D0,
        ["Ntilde"] = 0x00D1,
        ["Ograve"] = 0x00D2,
        ["Oacute"] = 0x00D3,
        ["Ocircumflex"] = 0x00D4,
        ["Otilde"] = 0x00D5,
        ["Odieresis"] = 0x00D6,
        ["multiply"] = 0x00D7,
        ["Oslash"] = 0x00D8,
        ["Ugrave"] = 0x00D9,
        ["Uacute"] = 0x00DA,
        ["Ucircumflex"] = 0x00DB,
        ["Udieresis"] = 0x00DC,
        ["Yacute"] = 0x00DD,
        ["Thorn"] = 0x00DE,
        ["germandbls"] = 0x00DF,
        ["agrave"] = 0x00E0,
        ["aacute"] = 0x00E1,
        ["acircumflex"] = 0x00E2,
        ["atilde"] = 0x00E3,
        ["adieresis"] = 0x00E4,
        ["aring"] = 0x00E5,
        ["ae"] = 0x00E6,
        ["ccedilla"] = 0x00E7,
        ["egrave"] = 0x00E8,
        ["eacute"] = 0x00E9,
        ["ecircumflex"] = 0x00EA,
        ["edieresis"] = 0x00EB,
        ["igrave"] = 0x00EC,
        ["iacute"] = 0x00ED,
        ["icircumflex"] = 0x00EE,
        ["idieresis"] = 0x00EF,
        ["eth"] = 0x00F0,
        ["ntilde"] = 0x00F1,
        ["ograve"] = 0x00F2,
        ["oacute"] = 0x00F3,
        ["ocircumflex"] = 0x00F4,
        ["otilde"] = 0x00F5,
        ["odieresis"] = 0x00F6,
        ["divide"] = 0x00F7,
        ["oslash"] = 0x00F8,
        ["ugrave"] = 0x00F9,
        ["uacute"] = 0x00FA,
        ["ucircumflex"] = 0x00FB,
        ["udieresis"] = 0x00FC,
        ["yacute"] = 0x00FD,
        ["thorn"] = 0x00FE,
        ["ydieresis"] = 0x00FF,
        ["Euro"] = 0x20AC,
        ["quotesinglbase"] = 0x201A,
        ["florin"] = 0x0192,
        ["quotedblbase"] = 0x201E,
        ["ellipsis"] = 0x2026,
        ["dagger"] = 0x2020,
        ["daggerdbl"] = 0x2021,
        ["circumflex"] = 0x02C6,
        ["perthousand"] = 0x2030,
        ["Scaron"] = 0x0160,
        ["guilsinglleft"] = 0x2039,
        ["OE"] = 0x0152,
        ["Zcaron"] = 0x017D,
        ["quoteleft"] = 0x2018,
        ["quoteright"] = 0x2019,
        ["quotedblleft"] = 0x201C,
        ["quotedblright"] = 0x201D,
        ["bullet"] = 0x2022,
        ["endash"] = 0x2013,
        ["emdash"] = 0x2014,
        ["tilde"] = 0x02DC,
        ["trademark"] = 0x2122,
        ["scaron"] = 0x0161,
        ["guilsinglright"] = 0x203A,
        ["oe"] = 0x0153,
        ["zcaron"] = 0x017E,
        ["Ydieresis"] = 0x0178
    };

    internal static PdfFontProgram Load(string afmPath, string? dataPath)
    {
        if (string.IsNullOrWhiteSpace(afmPath))
            throw new HaruException(HaruStatus.MissingFileNameEntry, "AFM file name cannot be empty.");

        var lines = File.ReadAllLines(afmPath, Encoding.ASCII);
        if (lines.Length == 0 || !lines[0].StartsWith("StartFontMetrics", StringComparison.Ordinal))
            throw new HaruException(HaruStatus.InvalidAfmHeader, "AFM file does not start with StartFontMetrics.");

        var parser = new AfmParser();
        parser.Parse(lines);

        var fontFile = string.IsNullOrWhiteSpace(dataPath)
            ? null
            : LoadFontFile(File.ReadAllBytes(dataPath));

        var descriptor = new PdfFontDescriptor(
            parser.FontName,
            parser.Flags,
            new PdfRect(parser.BBox[0], parser.BBox[1], parser.BBox[2], parser.BBox[3]),
            parser.ItalicAngle,
            parser.Ascent,
            parser.Descent,
            parser.CapHeight,
            parser.XHeight,
            parser.StemV,
            parser.MissingWidth);

        return new PdfFontProgram(
            PdfFontProgramKind.Type1,
            parser.FontName,
            descriptor,
            parser.CodeWidths,
            parser.UnicodeWidths,
            fontFile: fontFile);
    }

    private static PdfFontFile LoadFontFile(byte[] bytes)
    {
        if (bytes.Length >= 6 && bytes[0] == 0x80)
            return LoadPfb(bytes);

        return LoadPfa(bytes);
    }

    private static PdfFontFile LoadPfb(byte[] bytes)
    {
        using var data = new MemoryStream();
        var offset = 0;
        var length1 = 0;
        var length2 = 0;
        var length3 = 0;
        var sawBinary = false;

        while (offset + 6 <= bytes.Length && bytes[offset] == 0x80)
        {
            var segmentType = bytes[offset + 1];
            offset += 2;

            if (segmentType == 3)
                break;

            var length = bytes[offset]
                         | (bytes[offset + 1] << 8)
                         | (bytes[offset + 2] << 16)
                         | (bytes[offset + 3] << 24);
            offset += 4;

            if (length < 0 || offset + length > bytes.Length)
                throw new HaruException(HaruStatus.UnsupportedType1Font, "PFB segment length is invalid.");

            data.Write(bytes, offset, length);

            if (segmentType == 1 && !sawBinary)
            {
                length1 += length;
            }
            else if (segmentType == 2)
            {
                sawBinary = true;
                length2 += length;
            }
            else
            {
                length3 += length;
            }

            offset += length;
        }

        return new PdfFontFile("FontFile", data.ToArray(), length1, length2, length3);
    }

    private static PdfFontFile LoadPfa(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var eexec = text.IndexOf("eexec", StringComparison.Ordinal);
        var clearToMark = text.IndexOf("cleartomark", StringComparison.Ordinal);

        if (eexec < 0 || clearToMark < 0 || clearToMark <= eexec)
            return new PdfFontFile("FontFile", bytes, bytes.Length, 0, 0);

        var length1 = eexec + "eexec".Length;
        while (length1 < bytes.Length && (bytes[length1] == '\r' || bytes[length1] == '\n'))
            length1++;

        var length2 = clearToMark - length1;
        var length3 = bytes.Length - clearToMark;
        return new PdfFontFile("FontFile", bytes, length1, length2, length3);
    }

    private sealed class AfmParser
    {
        internal string FontName { get; private set; } = string.Empty;

        internal int[] BBox { get; } = [0, 0, 0, 0];

        internal int ItalicAngle { get; private set; }

        internal int Ascent { get; private set; }

        internal int Descent { get; private set; }

        internal int CapHeight { get; private set; }

        internal int XHeight { get; private set; }

        internal int StemV { get; private set; } = 80;

        internal int MissingWidth { get; private set; }

        internal int Flags { get; private set; } = FontStdCharset;

        internal Dictionary<int, int> CodeWidths { get; } = new();

        internal Dictionary<int, int> UnicodeWidths { get; } = new();

        internal void Parse(string[] lines)
        {
            var inMetrics = false;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("StartCharMetrics", StringComparison.Ordinal))
                {
                    inMetrics = true;
                    continue;
                }

                if (line.StartsWith("EndCharMetrics", StringComparison.Ordinal))
                {
                    inMetrics = false;
                    continue;
                }

                if (inMetrics)
                    ParseMetricLine(line);
                else
                    ParseHeaderLine(line);
            }

            if (string.IsNullOrWhiteSpace(FontName))
                throw new HaruException(HaruStatus.InvalidFontDefData, "AFM FontName entry was not found.");

            if (Ascent == 0)
                Ascent = CapHeight;

            if (MissingWidth == 0 && CodeWidths.TryGetValue(0, out var notdefWidth))
                MissingWidth = notdefWidth;
        }

        private void ParseHeaderLine(string line)
        {
            var firstSpace = line.IndexOf(' ');
            if (firstSpace < 0)
                return;

            var key = line[..firstSpace];
            var value = line[(firstSpace + 1)..].Trim();

            switch (key)
            {
                case "FontName":
                    FontName = value;
                    break;
                case "FontBBox":
                    var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 4)
                        for (var i = 0; i < 4; i++)
                            BBox[i] = ParseInt(parts[i]);
                    break;
                case "ItalicAngle":
                    ItalicAngle = (int)Math.Round(ParseDouble(value));
                    if (ItalicAngle != 0)
                        Flags |= FontItalic;
                    break;
                case "IsFixedPitch":
                    if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
                        Flags |= FontFixedWidth;
                    break;
                case "Ascender":
                    Ascent = ParseInt(value);
                    break;
                case "Descender":
                    Descent = ParseInt(value);
                    break;
                case "CapHeight":
                    CapHeight = ParseInt(value);
                    break;
                case "XHeight":
                    XHeight = ParseInt(value);
                    break;
                case "StdVW":
                case "STDVW":
                case "StdV":
                    StemV = ParseInt(value);
                    break;
            }
        }

        private void ParseMetricLine(string line)
        {
            var fields = line.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var code = -1;
            var width = 0;
            string? glyphName = null;

            foreach (var field in fields)
                if (field.StartsWith("C ", StringComparison.Ordinal))
                    code = ParseInt(field[2..].Trim());
                else if (field.StartsWith("WX ", StringComparison.Ordinal))
                    width = ParseInt(field[3..].Trim());
                else if (field.StartsWith("N ", StringComparison.Ordinal))
                    glyphName = field[2..].Trim();

            if (width <= 0 && code != 0)
                return;

            if (code is >= 0 and <= 255)
                CodeWidths[code] = width;

            if (glyphName is not null && TryGlyphNameToUnicode(glyphName, out var unicode))
                UnicodeWidths[unicode] = width;
        }

        private static bool TryGlyphNameToUnicode(string glyphName, out int unicode)
        {
            if (glyphName.Length == 1)
            {
                unicode = glyphName[0];
                return true;
            }

            if (glyphName.StartsWith("uni", StringComparison.Ordinal) &&
                glyphName.Length >= 7 &&
                int.TryParse(glyphName.AsSpan(3, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out unicode))
                return true;

            if (glyphName.StartsWith('u') &&
                glyphName.Length is >= 5 and <= 7 &&
                int.TryParse(glyphName.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out unicode))
                return true;

            return GlyphUnicode.TryGetValue(glyphName, out unicode);
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static double ParseDouble(string value)
        {
            return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
    }
}