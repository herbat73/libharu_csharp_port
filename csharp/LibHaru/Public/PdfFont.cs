using LibHaru.Internal;
using System.Text;

namespace LibHaru;

public sealed class PdfFont
{
    internal PdfFont(
        PdfDocument owner,
        PdfFontProgram program,
        PdfEncoding encoding,
        string resourceName,
        PdfIndirectObject fontObject,
        PdfCompositeGlyphMap? compositeGlyphMap = null)
    {
        Owner = owner;
        Program = program;
        EncodingModel = encoding;
        ResourceName = resourceName;
        FontObject = fontObject;
        CompositeGlyphMap = compositeGlyphMap;
    }

    internal PdfDocument Owner { get; }

    internal PdfFontProgram Program { get; }

    internal PdfEncoding EncodingModel { get; }

    public string BaseFont => Program.BaseFont;

    public string Encoding => EncodingModel.Name;

    public PdfRect BBox => Program.Descriptor.FontBBox;

    public int Ascent => Program.Descriptor.Ascent;

    public int Descent => Program.Descriptor.Descent;

    public int XHeight => Program.Descriptor.XHeight;

    public int CapHeight => Program.Descriptor.CapHeight;

    internal string ResourceName { get; }

    internal PdfIndirectObject FontObject { get; }

    internal PdfCompositeGlyphMap? CompositeGlyphMap { get; }

    public double TextWidth(string text, double fontSize)
    {
        ValidateOrThrow();

        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        if (fontSize <= 0 || double.IsNaN(fontSize) || double.IsInfinity(fontSize))
            throw Owner.CreateException(HaruStatus.PageInvalidFontSize, "Font size must be a positive finite number.");

        if (EncodingModel.IsComposite)
            return CompositeTextWidth(text, fontSize);

        var units = 0;
        foreach (var code in EncodeText(text))
            units += Program.WidthOfCode(EncodingModel, code);

        return units * fontSize / 1000.0;
    }

    public int GetUnicodeWidth(char unicode)
    {
        if (EncodingModel.PreservesInputBytes)
        {
            return CjkCMapData.TryGetCodeForUnicode(EncodingModel.Name, unicode, out var code)
                ? Program.WidthOfCid(EncodingModel.ToCid(code))
                : 0;
        }

        return Program.WidthOfUnicode(unicode);
    }

    public PdfTextWidth TextWidthInfo(string text)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        var width = 0;
        var numSpace = 0u;
        if (EncodingModel.PreservesInputBytes)
            return PredefinedCMapTextWidthInfo(text);

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
                numSpace++;

            if (EncodingModel.IsComposite)
            {
                width += EncodingModel.WritingMode == PdfWritingMode.Horizontal
                    ? Program.WidthOfGlyph(Program.GlyphIdOfUnicode(ch))
                    : -Program.CidVerticalDisplacement;
            }
            else
            {
                width += Program.WidthOfCode(EncodingModel, EncodingModel.EncodeChar(ch));
            }
        }

        return new PdfTextWidth((uint)text.Length, numSpace, (uint)Math.Max(0, width), numSpace);
    }

    public uint MeasureText(string text, double width, double fontSize, double charSpace, double wordSpace, bool wordWrap, out double realWidth)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        if (width <= 0 || fontSize <= 0 || double.IsNaN(width) || double.IsNaN(fontSize) || double.IsInfinity(width) || double.IsInfinity(fontSize))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text measurement width and font size must be positive finite numbers.");

        var measured = 0.0;
        var lastBreak = -1;
        var lastBreakWidth = 0.0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            var charWidth = TextWidth(ch.ToString(), fontSize) + charSpace;
            if (char.IsWhiteSpace(ch))
                charWidth += wordSpace;

            if (measured + charWidth > width)
            {
                if (wordWrap && lastBreak >= 0)
                {
                    realWidth = lastBreakWidth;
                    return (uint)(lastBreak + 1);
                }

                realWidth = measured;
                return (uint)i;
            }

            measured += charWidth;

            if (char.IsWhiteSpace(ch))
            {
                lastBreak = i;
                lastBreakWidth = measured;
            }
        }

        realWidth = measured;
        return (uint)text.Length;
    }

    internal void ValidateOrThrow(uint status = HaruStatus.InvalidFont)
    {
        if (FontObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(status, "Font object must be a dictionary.");

        if (!dictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Font))
            throw Owner.CreateException(status, "Font object must be a font dictionary.");

        try
        {
            var type = dictionary.Get<PdfName>("Type");
            if (type?.Value != "Font")
                throw Owner.CreateException(status, "Font dictionary Type entry is invalid.");
        }
        catch (HaruException ex) when (ex.Status != status)
        {
            throw Owner.CreateException(status, "Font dictionary Type entry is invalid.", ex.Status);
        }
    }

    internal byte[] EncodeText(string text)
    {
        ValidateOrThrow();

        if (EncodingModel.PreservesInputBytes)
        {
            var bytes = System.Text.Encoding.Latin1.GetBytes(text);
            MarkGlyphsForPreservedInput(text, bytes);
            return bytes;
        }

        if (!EncodingModel.IsComposite)
        {
            var bytes = EncodingModel.EncodeText(text);
            foreach (var code in bytes)
                Program.MarkUnicodeUsed(EncodingModel.ToUnicode(code));

            return bytes;
        }

        using var output = new MemoryStream();
        foreach (var unicode in EnumerateUnicodeScalars(text))
        {
            var glyphId = Program.MarkUnicodeUsed(unicode);
            var cid = Program.UsesFontCidCodes
                ? Program.CidOfGlyph(glyphId)
                : CompositeGlyphMap?.GetOrCreateIdentityCid(unicode, glyphId) ?? glyphId;
            if (Program.UsesFontCidCodes)
                CompositeGlyphMap?.Register(cid, glyphId, new PdfCompositeCharCode(cid, 2), unicode);
            output.WriteByte((byte)(cid >> 8));
            output.WriteByte((byte)(cid & 0xFF));
        }

        return output.ToArray();
    }

    private void MarkGlyphsForPreservedInput(string text, byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            var byteType = EncodingModel.GetByteType(text, (uint)i);
            if (byteType == PdfByteType.Trail)
                continue;

            var code = (ushort)bytes[i];
            if (byteType == PdfByteType.Lead && i + 1 < bytes.Length && EncodingModel.GetByteType(text, (uint)(i + 1)) == PdfByteType.Trail)
            {
                code = (ushort)((bytes[i] << 8) | bytes[i + 1]);
                i++;
            }

            var unicode = EncodingModel.GetUnicode(code);
            var glyphId = unicode > 0 ? Program.MarkUnicodeUsed(unicode) : 0;
            var cid = EncodingModel.ToCid(code);
            CompositeGlyphMap?.Register(cid, glyphId, new PdfCompositeCharCode(code, code > byte.MaxValue ? 2 : 1), unicode);
        }
    }

    private double CompositeTextWidth(string text, double fontSize)
    {
        if (EncodingModel.PreservesInputBytes)
            return EstimatePredefinedCMapWidth(text, fontSize);

        var units = 0;

        foreach (var unicode in EnumerateUnicodeScalars(text))
        {
            units += EncodingModel.WritingMode == PdfWritingMode.Horizontal
                ? Program.WidthOfGlyph(Program.GlyphIdOfUnicode(unicode))
                : -Program.CidVerticalDisplacement;
        }

        return units * fontSize / 1000.0;
    }

    private double EstimatePredefinedCMapWidth(string text, double fontSize)
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes(text);
        var units = 0;

        for (var i = 0; i < bytes.Length; i++)
        {
            var byteType = EncodingModel.GetByteType(text, (uint)i);
            if (byteType == PdfByteType.Lead)
            {
                var code = (ushort)(bytes[i] << 8);
                if (i + 1 < bytes.Length && EncodingModel.GetByteType(text, (uint)(i + 1)) == PdfByteType.Trail)
                {
                    code = (ushort)(code + bytes[i + 1]);
                    i++;
                }

                units += EncodingModel.WritingMode == PdfWritingMode.Horizontal
                    ? Program.WidthOfCid(EncodingModel.ToCid(code))
                    : -Program.CidVerticalDisplacement;
            }
            else
            {
                units += EncodingModel.WritingMode == PdfWritingMode.Horizontal
                    ? Program.WidthOfCid(EncodingModel.ToCid(bytes[i]))
                    : -Program.CidVerticalDisplacement;
            }
        }

        return units * fontSize / 1000.0;
    }

    private PdfTextWidth PredefinedCMapTextWidthInfo(string text)
    {
        var bytes = System.Text.Encoding.Latin1.GetBytes(text);
        var width = 0;
        var numChars = 0u;
        var numWords = 0u;
        var numSpace = 0u;
        var lastByte = (byte)0;

        for (var i = 0; i < bytes.Length; i++)
        {
            var byteType = EncodingModel.GetByteType(text, (uint)i);
            var code = (ushort)bytes[i];
            lastByte = bytes[i];

            if (byteType == PdfByteType.Lead)
            {
                code = (ushort)(bytes[i] << 8);
                if (i + 1 < bytes.Length)
                    code = (ushort)(code + bytes[i + 1]);
            }

            if (byteType != PdfByteType.Trail)
            {
                var cid = EncodingModel.ToCid(code);
                width += EncodingModel.WritingMode == PdfWritingMode.Horizontal
                    ? Program.WidthOfCid(cid)
                    : -Program.CidVerticalDisplacement;
                numChars++;
            }

            if (IsPdfWhiteSpace(code))
            {
                numWords++;
                numSpace++;
            }
        }

        if (!IsPdfWhiteSpace(lastByte))
            numWords++;

        return new PdfTextWidth(numChars, numWords, (uint)Math.Max(0, width), numSpace);
    }

    private static bool IsPdfWhiteSpace(int code) => code is 0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;

    private static IEnumerable<int> EnumerateUnicodeScalars(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                yield return char.ConvertToUtf32(ch, text[++i]);
                continue;
            }

            if (char.IsSurrogate(ch))
            {
                yield return '?';
                continue;
            }

            yield return ch;
        }
    }
}
