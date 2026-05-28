using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfEncoder
{
    internal PdfEncoder(PdfDocument owner, PdfEncoding encoding)
    {
        Owner = owner;
        EncodingModel = encoding;
    }

    internal PdfDocument Owner { get; }

    internal PdfEncoding EncodingModel { get; }

    public string Name => EncodingModel.Name;

    public PdfEncoderType Type => EncodingModel.EncoderType;

    public PdfWritingMode WritingMode => EncodingModel.WritingMode;

    public PdfByteType GetByteType(string text, uint index)
    {
        if (text is null)
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Text cannot be null.");

        return EncodingModel.GetByteType(text, index);
    }

    public ushort GetUnicode(ushort code) => EncodingModel.GetUnicode(code);
}
