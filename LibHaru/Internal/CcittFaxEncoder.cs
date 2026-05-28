namespace LibHaru.Internal;

internal static class CcittFaxEncoder
{
    private const int Eol = 0x001;
    private static readonly FaxCode HorizontalCode = new(3, 0x1, 0);
    private static readonly FaxCode PassCode = new(4, 0x1, 0);

    private static readonly FaxCode[] VerticalCodes =
    [
        new(7, 0x03, 0),
        new(6, 0x03, 0),
        new(3, 0x03, 0),
        new(1, 0x1, 0),
        new(3, 0x2, 0),
        new(6, 0x02, 0),
        new(7, 0x02, 0)
    ];

    private static readonly FaxCode[] WhiteCodes =
    [
        new(8, 0x35, 0),
        new(6, 0x7, 1),
        new(4, 0x7, 2),
        new(4, 0x8, 3),
        new(4, 0xB, 4),
        new(4, 0xC, 5),
        new(4, 0xE, 6),
        new(4, 0xF, 7),
        new(5, 0x13, 8),
        new(5, 0x14, 9),
        new(5, 0x7, 10),
        new(5, 0x8, 11),
        new(6, 0x8, 12),
        new(6, 0x3, 13),
        new(6, 0x34, 14),
        new(6, 0x35, 15),
        new(6, 0x2A, 16),
        new(6, 0x2B, 17),
        new(7, 0x27, 18),
        new(7, 0xC, 19),
        new(7, 0x8, 20),
        new(7, 0x17, 21),
        new(7, 0x3, 22),
        new(7, 0x4, 23),
        new(7, 0x28, 24),
        new(7, 0x2B, 25),
        new(7, 0x13, 26),
        new(7, 0x24, 27),
        new(7, 0x18, 28),
        new(8, 0x2, 29),
        new(8, 0x3, 30),
        new(8, 0x1A, 31),
        new(8, 0x1B, 32),
        new(8, 0x12, 33),
        new(8, 0x13, 34),
        new(8, 0x14, 35),
        new(8, 0x15, 36),
        new(8, 0x16, 37),
        new(8, 0x17, 38),
        new(8, 0x28, 39),
        new(8, 0x29, 40),
        new(8, 0x2A, 41),
        new(8, 0x2B, 42),
        new(8, 0x2C, 43),
        new(8, 0x2D, 44),
        new(8, 0x4, 45),
        new(8, 0x5, 46),
        new(8, 0xA, 47),
        new(8, 0xB, 48),
        new(8, 0x52, 49),
        new(8, 0x53, 50),
        new(8, 0x54, 51),
        new(8, 0x55, 52),
        new(8, 0x24, 53),
        new(8, 0x25, 54),
        new(8, 0x58, 55),
        new(8, 0x59, 56),
        new(8, 0x5A, 57),
        new(8, 0x5B, 58),
        new(8, 0x4A, 59),
        new(8, 0x4B, 60),
        new(8, 0x32, 61),
        new(8, 0x33, 62),
        new(8, 0x34, 63),
        new(5, 0x1B, 64),
        new(5, 0x12, 128),
        new(6, 0x17, 192),
        new(7, 0x37, 256),
        new(8, 0x36, 320),
        new(8, 0x37, 384),
        new(8, 0x64, 448),
        new(8, 0x65, 512),
        new(8, 0x68, 576),
        new(8, 0x67, 640),
        new(9, 0xCC, 704),
        new(9, 0xCD, 768),
        new(9, 0xD2, 832),
        new(9, 0xD3, 896),
        new(9, 0xD4, 960),
        new(9, 0xD5, 1024),
        new(9, 0xD6, 1088),
        new(9, 0xD7, 1152),
        new(9, 0xD8, 1216),
        new(9, 0xD9, 1280),
        new(9, 0xDA, 1344),
        new(9, 0xDB, 1408),
        new(9, 0x98, 1472),
        new(9, 0x99, 1536),
        new(9, 0x9A, 1600),
        new(6, 0x18, 1664),
        new(9, 0x9B, 1728),
        new(11, 0x8, 1792),
        new(11, 0xC, 1856),
        new(11, 0xD, 1920),
        new(12, 0x12, 1984),
        new(12, 0x13, 2048),
        new(12, 0x14, 2112),
        new(12, 0x15, 2176),
        new(12, 0x16, 2240),
        new(12, 0x17, 2304),
        new(12, 0x1C, 2368),
        new(12, 0x1D, 2432),
        new(12, 0x1E, 2496),
        new(12, 0x1F, 2560)
    ];

    private static readonly FaxCode[] BlackCodes =
    [
        new(10, 0x37, 0),
        new(3, 0x2, 1),
        new(2, 0x3, 2),
        new(2, 0x2, 3),
        new(3, 0x3, 4),
        new(4, 0x3, 5),
        new(4, 0x2, 6),
        new(5, 0x3, 7),
        new(6, 0x5, 8),
        new(6, 0x4, 9),
        new(7, 0x4, 10),
        new(7, 0x5, 11),
        new(7, 0x7, 12),
        new(8, 0x4, 13),
        new(8, 0x7, 14),
        new(9, 0x18, 15),
        new(10, 0x17, 16),
        new(10, 0x18, 17),
        new(10, 0x8, 18),
        new(11, 0x67, 19),
        new(11, 0x68, 20),
        new(11, 0x6C, 21),
        new(11, 0x37, 22),
        new(11, 0x28, 23),
        new(11, 0x17, 24),
        new(11, 0x18, 25),
        new(12, 0xCA, 26),
        new(12, 0xCB, 27),
        new(12, 0xCC, 28),
        new(12, 0xCD, 29),
        new(12, 0x68, 30),
        new(12, 0x69, 31),
        new(12, 0x6A, 32),
        new(12, 0x6B, 33),
        new(12, 0xD2, 34),
        new(12, 0xD3, 35),
        new(12, 0xD4, 36),
        new(12, 0xD5, 37),
        new(12, 0xD6, 38),
        new(12, 0xD7, 39),
        new(12, 0x6C, 40),
        new(12, 0x6D, 41),
        new(12, 0xDA, 42),
        new(12, 0xDB, 43),
        new(12, 0x54, 44),
        new(12, 0x55, 45),
        new(12, 0x56, 46),
        new(12, 0x57, 47),
        new(12, 0x64, 48),
        new(12, 0x65, 49),
        new(12, 0x52, 50),
        new(12, 0x53, 51),
        new(12, 0x24, 52),
        new(12, 0x37, 53),
        new(12, 0x38, 54),
        new(12, 0x27, 55),
        new(12, 0x28, 56),
        new(12, 0x58, 57),
        new(12, 0x59, 58),
        new(12, 0x2B, 59),
        new(12, 0x2C, 60),
        new(12, 0x5A, 61),
        new(12, 0x66, 62),
        new(12, 0x67, 63),
        new(10, 0xF, 64),
        new(12, 0xC8, 128),
        new(12, 0xC9, 192),
        new(12, 0x5B, 256),
        new(12, 0x33, 320),
        new(12, 0x34, 384),
        new(12, 0x35, 448),
        new(13, 0x6C, 512),
        new(13, 0x6D, 576),
        new(13, 0x4A, 640),
        new(13, 0x4B, 704),
        new(13, 0x4C, 768),
        new(13, 0x4D, 832),
        new(13, 0x72, 896),
        new(13, 0x73, 960),
        new(13, 0x74, 1024),
        new(13, 0x75, 1088),
        new(13, 0x76, 1152),
        new(13, 0x77, 1216),
        new(13, 0x52, 1280),
        new(13, 0x53, 1344),
        new(13, 0x54, 1408),
        new(13, 0x55, 1472),
        new(13, 0x5A, 1536),
        new(13, 0x5B, 1600),
        new(13, 0x64, 1664),
        new(13, 0x65, 1728),
        new(11, 0x8, 1792),
        new(11, 0xC, 1856),
        new(11, 0xD, 1920),
        new(12, 0x12, 1984),
        new(12, 0x13, 2048),
        new(12, 0x14, 2112),
        new(12, 0x15, 2176),
        new(12, 0x16, 2240),
        new(12, 0x17, 2304),
        new(12, 0x1C, 2368),
        new(12, 0x1D, 2432),
        new(12, 0x1E, 2496),
        new(12, 0x1F, 2560)
    ];

    internal static byte[] EncodeGroup4(byte[] rows, int width, int height, int stride)
    {
        var writer = new BitWriter();
        var reference = new byte[stride];

        for (var y = 0; y < height; y++)
        {
            var offset = y * stride;
            Encode2DRow(writer, rows, offset, reference, 0, width);
            Buffer.BlockCopy(rows, offset, reference, 0, stride);
        }

        writer.PutBits(Eol, 12);
        writer.PutBits(Eol, 12);
        writer.FlushPartialByte();
        return writer.ToArray();
    }

    private static void Encode2DRow(BitWriter writer, byte[] row, int rowOffset, byte[] reference, int referenceOffset,
        int width)
    {
        var a0 = 0;
        var a1 = Pixel(row, rowOffset, 0, width) ? 0 : FindDiff(row, rowOffset, 0, width, false);
        var b1 = Pixel(reference, referenceOffset, 0, width)
            ? 0
            : FindDiff(reference, referenceOffset, 0, width, false);

        while (true)
        {
            var b2 = FindDiff2(reference, referenceOffset, b1, width, Pixel(reference, referenceOffset, b1, width));

            if (b2 >= a1)
            {
                var delta = b1 - a1;
                if (delta is < -3 or > 3)
                {
                    var a2 = FindDiff2(row, rowOffset, a1, width, Pixel(row, rowOffset, a1, width));
                    PutCode(writer, HorizontalCode);

                    if (a0 + a1 == 0 || !Pixel(row, rowOffset, a0, width))
                    {
                        PutSpan(writer, a1 - a0, WhiteCodes);
                        PutSpan(writer, a2 - a1, BlackCodes);
                    }
                    else
                    {
                        PutSpan(writer, a1 - a0, BlackCodes);
                        PutSpan(writer, a2 - a1, WhiteCodes);
                    }

                    a0 = a2;
                }
                else
                {
                    PutCode(writer, VerticalCodes[delta + 3]);
                    a0 = a1;
                }
            }
            else
            {
                PutCode(writer, PassCode);
                a0 = b2;
            }

            if (a0 >= width)
                break;

            var rowColor = Pixel(row, rowOffset, a0, width);
            a1 = FindDiff(row, rowOffset, a0, width, rowColor);
            b1 = FindDiff(reference, referenceOffset, a0, width, !rowColor);
            b1 = FindDiff(reference, referenceOffset, b1, width, rowColor);
        }
    }

    private static bool Pixel(byte[] data, int offset, int index, int width)
    {
        if (index < 0 || index >= width)
            return false;

        return ((data[offset + (index >> 3)] >> (7 - (index & 7))) & 1) != 0;
    }

    private static int FindDiff(byte[] data, int offset, int start, int end, bool color)
    {
        for (var index = start; index < end; index++)
            if (Pixel(data, offset, index, end) != color)
                return index;

        return end;
    }

    private static int FindDiff2(byte[] data, int offset, int start, int end, bool color)
    {
        return start < end ? FindDiff(data, offset, start, end, color) : end;
    }

    private static void PutCode(BitWriter writer, FaxCode code)
    {
        writer.PutBits(code.Code, code.Length);
    }

    private static void PutSpan(BitWriter writer, int span, FaxCode[] table)
    {
        while (span >= 2624)
        {
            var code = table[103];
            writer.PutBits(code.Code, code.Length);
            span -= code.RunLength;
        }

        if (span >= 64)
        {
            var code = table[63 + (span >> 6)];
            writer.PutBits(code.Code, code.Length);
            span -= code.RunLength;
        }

        var terminal = table[span];
        writer.PutBits(terminal.Code, terminal.Length);
    }

    private sealed class BitWriter
    {
        private readonly MemoryStream _stream = new();
        private int _current;
        private int _remaining = 8;

        internal void PutBits(int bits, int length)
        {
            while (length > _remaining)
            {
                _current |= bits >> (length - _remaining);
                length -= _remaining;
                FlushByte();
            }

            _current |= (bits & ((1 << length) - 1)) << (_remaining - length);
            _remaining -= length;

            if (_remaining == 0)
                FlushByte();
        }

        internal void FlushPartialByte()
        {
            if (_remaining != 8)
                FlushByte();
        }

        internal byte[] ToArray()
        {
            return _stream.ToArray();
        }

        private void FlushByte()
        {
            _stream.WriteByte((byte)_current);
            _current = 0;
            _remaining = 8;
        }
    }

    private readonly record struct FaxCode(int Length, int Code, int RunLength);
}