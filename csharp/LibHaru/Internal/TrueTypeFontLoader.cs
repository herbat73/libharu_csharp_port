using System.Text;

namespace LibHaru.Internal;

internal static class TrueTypeFontLoader
{
    private const int FontFixedWidth = 1;
    private const int FontSerif = 2;
    private const int FontSymbolic = 4;
    private const int FontScript = 8;
    private const int FontStdCharset = 32;
    private const int FontItalic = 64;
    private const uint SfntVersionTrueType = 0x00010000;
    private const uint SfntVersionAppleTrueType = 0x74727565;
    private const uint SfntVersionOpenTypeCff = 0x4F54544F;

    internal static PdfFontProgram Load(byte[] data, bool embedding, int collectionIndex = 0)
    {
        if (data.Length < 12)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType font data is too short.");

        var faceOffset = ResolveFaceOffset(data, collectionIndex);
        var sfntVersion = ReadUInt32(data, faceOffset);
        var tables = ReadTableDirectory(data, faceOffset, sfntVersion);
        var isOpenTypeCff = sfntVersion == SfntVersionOpenTypeCff;
        if (isOpenTypeCff && !tables.ContainsKey("CFF "))
            throw new HaruException(HaruStatus.TtfMissingTable, "OpenType/CFF font is missing the CFF table.");

        var head = Required(tables, "head", HaruStatus.TtfMissingTable);
        var hhea = Required(tables, "hhea", HaruStatus.TtfMissingTable);
        var maxp = Required(tables, "maxp", HaruStatus.TtfMissingTable);
        var hmtx = Required(tables, "hmtx", HaruStatus.TtfMissingTable);
        var cmapTable = Required(tables, "cmap", HaruStatus.TtfMissingTable);
        var name = Required(tables, "name", HaruStatus.TtfMissingTable);
        var os2 = Required(tables, "OS/2", HaruStatus.TtfMissingTable);

        var unitsPerEm = ReadUInt16(data, head.Offset + 18);
        if (unitsPerEm == 0)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType unitsPerEm is zero.");

        var xMin = Scale(ReadInt16(data, head.Offset + 36), unitsPerEm);
        var yMin = Scale(ReadInt16(data, head.Offset + 38), unitsPerEm);
        var xMax = Scale(ReadInt16(data, head.Offset + 40), unitsPerEm);
        var yMax = Scale(ReadInt16(data, head.Offset + 42), unitsPerEm);
        var macStyle = ReadUInt16(data, head.Offset + 44);

        var numGlyphs = ReadUInt16(data, maxp.Offset + 4);
        var ascent = Scale(ReadInt16(data, hhea.Offset + 4), unitsPerEm);
        var descent = Scale(ReadInt16(data, hhea.Offset + 6), unitsPerEm);
        var numHMetrics = ReadUInt16(data, hhea.Offset + 34);
        if (numGlyphs == 0 || numHMetrics == 0)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType horizontal metrics are empty.");

        var advances = ReadAdvanceWidths(data, hmtx.Offset, numGlyphs, numHMetrics);
        var cmap = ReadCMap(data, cmapTable);
        var cff = isOpenTypeCff ? ReadCffMetadata(data, Required(tables, "CFF ", HaruStatus.TtfMissingTable), numGlyphs) : null;
        var names = ReadNames(data, name);
        var baseFont = names.PostScriptName ?? BuildBaseFontName(names.FamilyName, names.SubfamilyName);
        if (string.IsNullOrWhiteSpace(baseFont))
            throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType name table does not contain a usable font name.");

        var os2Version = ReadUInt16(data, os2.Offset);
        var averageWidth = Scale(ReadInt16(data, os2.Offset + 2), unitsPerEm);
        var fsType = ReadUInt16(data, os2.Offset + 8);
        if (embedding && (fsType & (0x0002 | 0x0100 | 0x0200)) != 0)
            throw new HaruException(HaruStatus.TtfCannotEmbeddingFont, "TrueType font embedding is restricted by the OS/2 fsType flags.");

        var familyClass = ReadInt16(data, os2.Offset + 30);
        var flags = FontStdCharset;
        var classId = (familyClass >> 8) & 0xFF;
        if ((classId is > 0 and < 6) || classId == 7)
            flags |= FontSerif;
        else if (classId == 10)
            flags |= FontScript;
        else if (classId == 12)
            flags |= FontSymbolic;

        if ((macStyle & 0x02) != 0)
            flags |= FontItalic;

        if (tables.TryGetValue("post", out var post) && post.Length >= 16 && ReadUInt32(data, post.Offset + 12) != 0)
            flags |= FontFixedWidth;

        var capHeight = os2Version >= 2 && os2.Length >= 90 ? Scale(ReadInt16(data, os2.Offset + 88), unitsPerEm) : 0;
        var xHeight = os2Version >= 2 && os2.Length >= 88 ? Scale(ReadInt16(data, os2.Offset + 86), unitsPerEm) : 0;
        if (capHeight == 0)
            capHeight = WidthTop(cmap, data, tables, unitsPerEm, 'H');
        if (xHeight == 0)
            xHeight = WidthTop(cmap, data, tables, unitsPerEm, 'x');

        var missingWidth = WidthForGlyph(0, advances, unitsPerEm);
        if (averageWidth == 0)
            averageWidth = missingWidth;

        int UnicodeWidth(int unicode)
        {
            var gid = cmap.GlyphId(unicode);
            return WidthForGlyph(gid, advances, unitsPerEm);
        }

        int GlyphId(int unicode) => cmap.GlyphId(unicode);

        int GlyphWidth(int glyphId) => WidthForGlyph(glyphId, advances, unitsPerEm);

        var descriptor = new PdfFontDescriptor(
            baseFont,
            flags,
            new PdfRect(xMin, yMin, xMax, yMax),
            (flags & FontItalic) != 0 ? -12 : 0,
            ascent,
            descent,
            capHeight,
            xHeight,
            Math.Max(50, averageWidth / 5),
            missingWidth);

        Func<PdfFontSubsetRequest, PdfFontSubsetData>? fontFileSubsetBuilder = embedding && !isOpenTypeCff
            ? request => CreateSubsetFontFile(data, faceOffset, tables, numGlyphs, numHMetrics, request)
            : null;
        var fontFile = CreateFontFile(data, faceOffset, tables, embedding, isOpenTypeCff, fontFileSubsetBuilder);

        var kind = isOpenTypeCff
            ? (cff is { IsCidKeyed: true } ? PdfFontProgramKind.OpenTypeCffCidKeyed : PdfFontProgramKind.OpenTypeCff)
            : PdfFontProgramKind.TrueType;
        var supportsComposite = !isOpenTypeCff || cff is { IsCidKeyed: true };

        return new PdfFontProgram(
            kind,
            baseFont,
            descriptor,
            unicodeWidthResolver: UnicodeWidth,
            glyphIdResolver: supportsComposite ? GlyphId : null,
            glyphWidthResolver: supportsComposite ? GlyphWidth : null,
            glyphCidResolver: cff is null ? null : cff.CidOfGlyph,
            fontFile: fontFile,
            fontFileSubsetBuilder: fontFileSubsetBuilder,
            cidOrdering: cff is { IsCidKeyed: true } ? cff.Ordering : null,
            cidSupplement: cff?.Supplement ?? 0,
            cidVerticalPosition: yMin,
            cidVerticalDisplacement: yMin - yMax);
    }

    private static PdfFontFile? CreateFontFile(
        byte[] data,
        int faceOffset,
        Dictionary<string, TtfTable> tables,
        bool embedding,
        bool isOpenTypeCff,
        Func<PdfFontSubsetRequest, PdfFontSubsetData>? fontFileSubsetBuilder)
    {
        if (!embedding)
            return null;

        if (isOpenTypeCff)
        {
            var fontData = BuildStandaloneFontFile(data, faceOffset, tables);
            return new PdfFontFile(
                "FontFile3",
                fontData,
                fontData.Length,
                0,
                0,
                subtype: "OpenType",
                writesLengthEntries: false);
        }

        var fontFileData = fontFileSubsetBuilder?.Invoke(new PdfFontSubsetRequest([0], new Dictionary<int, int>()));
        return fontFileData is not null
            ? new PdfFontFile("FontFile2", fontFileData.Data, fontFileData.Data.Length, 0, 0)
            : null;
    }

    private static int ResolveFaceOffset(byte[] data, int collectionIndex)
    {
        if (ReadTag(data, 0) != "ttcf")
        {
            if (collectionIndex != 0)
                throw new HaruException(HaruStatus.InvalidTtcIndex, "A non-zero TTC index was requested for a non-TTC font.");

            return 0;
        }

        if (data.Length < 12)
            throw new HaruException(HaruStatus.InvalidTtcFile, "TTC header is truncated.");

        var count = ReadUInt32(data, 8);
        if (collectionIndex < 0 || (uint)collectionIndex >= count)
            throw new HaruException(HaruStatus.InvalidTtcIndex, "TTC index is outside the collection.");

        var offset = checked((int)ReadUInt32(data, 12 + collectionIndex * 4));
        if (offset < 0 || offset + 12 > data.Length)
            throw new HaruException(HaruStatus.InvalidTtcFile, "TTC face offset is outside the font data.");

        return offset;
    }

    private static Dictionary<string, TtfTable> ReadTableDirectory(byte[] data, int faceOffset, uint sfntVersion)
    {
        if (sfntVersion is not (SfntVersionTrueType or SfntVersionAppleTrueType or SfntVersionOpenTypeCff))
            throw new HaruException(HaruStatus.UnsupportedFontType, "Only TrueType and OpenType/CFF SFNT fonts are supported.");

        var tableCount = ReadUInt16(data, faceOffset + 4);
        var recordsOffset = faceOffset + 12;
        var tables = new Dictionary<string, TtfTable>(StringComparer.Ordinal);

        for (var i = 0; i < tableCount; i++)
        {
            var offset = recordsOffset + i * 16;
            EnsureRange(data, offset, 16);
            var tag = ReadTag(data, offset);
            var tableOffset = checked((int)ReadUInt32(data, offset + 8));
            var length = checked((int)ReadUInt32(data, offset + 12));
            EnsureRange(data, tableOffset, length);
            tables[tag] = new TtfTable(tableOffset, length);
        }

        return tables;
    }

    private static CffMetadata ReadCffMetadata(byte[] data, TtfTable table, int numGlyphs)
    {
        EnsureRange(data, table.Offset, table.Length);
        if (table.Length < 4)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF table is truncated.");

        var cffEnd = table.Offset + table.Length;
        var headerSize = data[table.Offset + 2];
        if (headerSize < 4 || table.Offset + headerSize > cffEnd)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF header is invalid.");

        var nameIndex = ReadCffIndex(data, table.Offset + headerSize, cffEnd);
        var topDictIndex = ReadCffIndex(data, nameIndex.EndOffset, cffEnd);
        var stringIndex = ReadCffIndex(data, topDictIndex.EndOffset, cffEnd);
        if (topDictIndex.Objects.Count == 0)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF top dictionary is missing.");

        var topDict = ParseCffDict(data, topDictIndex.Objects[0]);
        if (!topDict.TryGetValue(0x0C1E, out var ros) || ros.Count < 3)
            return new CffMetadata(false, "Identity", 0, IdentityCidMap(numGlyphs));

        var ordering = ResolveCffSid(data, (int)ros[1], stringIndex, "Identity");
        var supplement = (int)ros[2];
        var charsetOffset = topDict.TryGetValue(15, out var charset) && charset.Count > 0
            ? (int)charset[^1]
            : 0;
        var gidToCid = charsetOffset > 2
            ? ReadCffCharset(data, checked(table.Offset + charsetOffset), cffEnd, numGlyphs)
            : IdentityCidMap(numGlyphs);

        return new CffMetadata(true, ordering, supplement, gidToCid);
    }

    private static CffIndex ReadCffIndex(byte[] data, int offset, int cffEnd)
    {
        EnsureRange(data, offset, 2);
        var count = ReadUInt16(data, offset);
        if (count == 0)
            return new CffIndex([], offset + 2);

        EnsureRange(data, offset + 2, 1);
        var offSize = data[offset + 2];
        if (offSize is < 1 or > 4)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF INDEX offSize is invalid.");

        var offsetsOffset = offset + 3;
        EnsureRange(data, offsetsOffset, checked((count + 1) * offSize));
        var objectDataOffset = offsetsOffset + (count + 1) * offSize;
        var offsets = new int[count + 1];
        for (var i = 0; i < offsets.Length; i++)
            offsets[i] = ReadCffOffset(data, offsetsOffset + i * offSize, offSize);

        var objects = new List<CffSlice>(count);
        for (var i = 0; i < count; i++)
        {
            var start = objectDataOffset + offsets[i] - 1;
            var end = objectDataOffset + offsets[i + 1] - 1;
            if (offsets[i] < 1 || end < start || end > cffEnd)
                throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF INDEX object range is invalid.");

            objects.Add(new CffSlice(start, end - start));
        }

        return new CffIndex(objects, objectDataOffset + offsets[^1] - 1);
    }

    private static int ReadCffOffset(byte[] data, int offset, int size)
    {
        var value = 0;
        for (var i = 0; i < size; i++)
            value = (value << 8) | data[offset + i];

        return value;
    }

    private static Dictionary<int, List<double>> ParseCffDict(byte[] data, CffSlice slice)
    {
        var dict = new Dictionary<int, List<double>>();
        var operands = new List<double>();
        var offset = slice.Offset;
        var end = slice.Offset + slice.Length;

        while (offset < end)
        {
            var b = data[offset++];
            if (b <= 21)
            {
                var op = b == 12
                    ? 0x0C00 | data[offset++]
                    : b;
                dict[op] = new List<double>(operands);
                operands.Clear();
                continue;
            }

            operands.Add(ReadCffNumber(data, ref offset, end, b));
        }

        return dict;
    }

    private static double ReadCffNumber(byte[] data, ref int offset, int end, byte first)
    {
        if (first == 28)
        {
            EnsureRange(data, offset, 2);
            var value = ReadInt16(data, offset);
            offset += 2;
            return value;
        }

        if (first == 29)
        {
            EnsureRange(data, offset, 4);
            var value = (int)ReadUInt32(data, offset);
            offset += 4;
            return value;
        }

        if (first == 30)
        {
            var builder = new StringBuilder();
            while (offset < end)
            {
                var packed = data[offset++];
                if (!AppendCffRealNibble(builder, packed >> 4) || !AppendCffRealNibble(builder, packed & 0x0F))
                    break;
            }

            return double.TryParse(builder.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        if (first is >= 32 and <= 246)
            return first - 139;

        if (first is >= 247 and <= 250)
        {
            EnsureRange(data, offset, 1);
            return (first - 247) * 256 + data[offset++] + 108;
        }

        if (first is >= 251 and <= 254)
        {
            EnsureRange(data, offset, 1);
            return -((first - 251) * 256) - data[offset++] - 108;
        }

        if (first == 255)
        {
            EnsureRange(data, offset, 4);
            var raw = (int)ReadUInt32(data, offset);
            offset += 4;
            return raw / 65536.0;
        }

        throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF dictionary number is invalid.");
    }

    private static bool AppendCffRealNibble(StringBuilder builder, int nibble)
    {
        switch (nibble)
        {
            case <= 9:
                builder.Append((char)('0' + nibble));
                return true;
            case 0xA:
                builder.Append('.');
                return true;
            case 0xB:
                builder.Append('E');
                return true;
            case 0xC:
                builder.Append("E-");
                return true;
            case 0xE:
                builder.Append('-');
                return true;
            case 0xF:
                return false;
            default:
                return true;
        }
    }

    private static string ResolveCffSid(byte[] data, int sid, CffIndex stringIndex, string fallback)
    {
        const int standardStringCount = 391;
        if (sid < standardStringCount)
            return fallback;

        var customIndex = sid - standardStringCount;
        if ((uint)customIndex >= (uint)stringIndex.Objects.Count)
            return fallback;

        var slice = stringIndex.Objects[customIndex];
        return slice.Length == 0 ? fallback : Encoding.ASCII.GetString(data, slice.Offset, slice.Length);
    }

    private static int[] ReadCffCharset(byte[] data, int offset, int cffEnd, int numGlyphs)
    {
        EnsureRange(data, offset, 1);
        if (offset >= cffEnd)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF charset offset is outside the CFF table.");

        var gidToCid = IdentityCidMap(numGlyphs);
        var format = data[offset++];
        var glyphId = 1;
        switch (format)
        {
            case 0:
                while (glyphId < numGlyphs)
                {
                    EnsureRange(data, offset, 2);
                    gidToCid[glyphId++] = ReadUInt16(data, offset);
                    offset += 2;
                }
                break;
            case 1:
                while (glyphId < numGlyphs)
                {
                    EnsureRange(data, offset, 3);
                    var first = ReadUInt16(data, offset);
                    var left = data[offset + 2];
                    offset += 3;
                    for (var i = 0; i <= left && glyphId < numGlyphs; i++)
                        gidToCid[glyphId++] = first + i;
                }
                break;
            case 2:
                while (glyphId < numGlyphs)
                {
                    EnsureRange(data, offset, 4);
                    var first = ReadUInt16(data, offset);
                    var left = ReadUInt16(data, offset + 2);
                    offset += 4;
                    for (var i = 0; i <= left && glyphId < numGlyphs; i++)
                        gidToCid[glyphId++] = first + i;
                }
                break;
            default:
                throw new HaruException(HaruStatus.TtfInvalidFormat, "CFF charset format is unsupported.");
        }

        return gidToCid;
    }

    private static int[] IdentityCidMap(int numGlyphs)
    {
        var gidToCid = new int[numGlyphs];
        for (var i = 0; i < gidToCid.Length; i++)
            gidToCid[i] = i;

        return gidToCid;
    }

    private static ushort[] ReadAdvanceWidths(byte[] data, int offset, int numGlyphs, int numHMetrics)
    {
        EnsureRange(data, offset, numHMetrics * 4);
        var advances = new ushort[numGlyphs];
        ushort lastAdvance = 0;

        for (var i = 0; i < numGlyphs; i++)
        {
            if (i < numHMetrics)
            {
                lastAdvance = ReadUInt16(data, offset + i * 4);
                advances[i] = lastAdvance;
            }
            else
            {
                advances[i] = lastAdvance;
            }
        }

        return advances;
    }

    private static CMap ReadCMap(byte[] data, TtfTable table)
    {
        var count = ReadUInt16(data, table.Offset + 2);
        CMap? best = null;
        var bestScore = int.MinValue;

        for (var i = 0; i < count; i++)
        {
            var recordOffset = table.Offset + 4 + i * 8;
            EnsureRange(data, recordOffset, 8);
            var platformId = ReadUInt16(data, recordOffset);
            var encodingId = ReadUInt16(data, recordOffset + 2);
            var subtableOffset = table.Offset + checked((int)ReadUInt32(data, recordOffset + 4));
            EnsureRange(data, subtableOffset, 2);
            var format = ReadUInt16(data, subtableOffset);

            CMap? cmap = format switch
            {
                0 => ReadFormat0CMap(data, subtableOffset),
                4 => ReadFormat4CMap(data, subtableOffset),
                6 => ReadFormat6CMap(data, subtableOffset),
                10 => ReadFormat10CMap(data, subtableOffset),
                12 => ReadFormat12CMap(data, subtableOffset),
                13 => ReadFormat13CMap(data, subtableOffset),
                _ => null
            };

            if (cmap is null)
                continue;

            var score = CMapScore(platformId, encodingId, format);
            if (score > bestScore)
            {
                best = cmap;
                bestScore = score;
            }
        }

        return best ?? throw new HaruException(HaruStatus.TtfInvalidCmap, "No supported cmap format was found.");
    }

    private static int CMapScore(int platformId, int encodingId, int format)
    {
        var platformScore = (platformId, encodingId) switch
        {
            (3, 10) => 600,
            (0, _) => 500,
            (3, 1) => 400,
            (3, 0) => 300,
            (1, 0) => 100,
            _ => 10
        };

        var formatScore = format switch
        {
            12 => 60,
            10 => 50,
            4 => 40,
            6 => 30,
            0 => 20,
            13 => 10,
            _ => 0
        };

        return platformScore + formatScore;
    }

    private static CMap ReadFormat0CMap(byte[] data, int offset)
    {
        EnsureRange(data, offset, 262);
        var glyphs = new ushort[256];
        for (var i = 0; i < glyphs.Length; i++)
            glyphs[i] = data[offset + 6 + i];

        return new Format0CMap(glyphs);
    }

    private static CMap ReadFormat4CMap(byte[] data, int offset)
    {
        var length = ReadUInt16(data, offset + 2);
        EnsureRange(data, offset, length);
        var segCount = ReadUInt16(data, offset + 6) / 2;
        var endCountOffset = offset + 14;
        var startCountOffset = endCountOffset + segCount * 2 + 2;
        var idDeltaOffset = startCountOffset + segCount * 2;
        var idRangeOffsetOffset = idDeltaOffset + segCount * 2;
        var glyphArrayOffset = idRangeOffsetOffset + segCount * 2;
        var glyphArrayCount = (offset + length - glyphArrayOffset) / 2;

        var endCount = new ushort[segCount];
        var startCount = new ushort[segCount];
        var idDelta = new short[segCount];
        var idRangeOffset = new ushort[segCount];
        var glyphArray = new ushort[Math.Max(0, glyphArrayCount)];

        for (var i = 0; i < segCount; i++)
        {
            endCount[i] = ReadUInt16(data, endCountOffset + i * 2);
            startCount[i] = ReadUInt16(data, startCountOffset + i * 2);
            idDelta[i] = ReadInt16(data, idDeltaOffset + i * 2);
            idRangeOffset[i] = ReadUInt16(data, idRangeOffsetOffset + i * 2);
        }

        for (var i = 0; i < glyphArray.Length; i++)
            glyphArray[i] = ReadUInt16(data, glyphArrayOffset + i * 2);

        return new Format4CMap(endCount, startCount, idDelta, idRangeOffset, glyphArray);
    }

    private static CMap ReadFormat6CMap(byte[] data, int offset)
    {
        var length = ReadUInt16(data, offset + 2);
        EnsureRange(data, offset, length);
        var firstCode = ReadUInt16(data, offset + 6);
        var entryCount = ReadUInt16(data, offset + 8);
        EnsureRange(data, offset + 10, entryCount * 2);

        var glyphs = new ushort[entryCount];
        for (var i = 0; i < glyphs.Length; i++)
            glyphs[i] = ReadUInt16(data, offset + 10 + i * 2);

        return new TrimmedCMap(firstCode, glyphs);
    }

    private static CMap ReadFormat10CMap(byte[] data, int offset)
    {
        var length = checked((int)ReadUInt32(data, offset + 4));
        EnsureRange(data, offset, length);
        var startCharCode = ReadUInt32(data, offset + 12);
        var numChars = ReadUInt32(data, offset + 16);
        if (numChars > int.MaxValue)
            throw new HaruException(HaruStatus.TtfInvalidCmap, "TrueType cmap format 10 is too large.");

        EnsureRange(data, offset + 20, checked((int)numChars * 2));
        var glyphs = new ushort[(int)numChars];
        for (var i = 0; i < glyphs.Length; i++)
            glyphs[i] = ReadUInt16(data, offset + 20 + i * 2);

        return new TrimmedCMap(checked((int)startCharCode), glyphs);
    }

    private static CMap ReadFormat12CMap(byte[] data, int offset)
    {
        var length = checked((int)ReadUInt32(data, offset + 4));
        EnsureRange(data, offset, length);
        var groupCount = ReadUInt32(data, offset + 12);
        if (groupCount > int.MaxValue)
            throw new HaruException(HaruStatus.TtfInvalidCmap, "TrueType cmap format 12 is too large.");

        EnsureRange(data, offset + 16, checked((int)groupCount * 12));
        var groups = new SequentialMapGroup[(int)groupCount];
        for (var i = 0; i < groups.Length; i++)
        {
            var groupOffset = offset + 16 + i * 12;
            groups[i] = new SequentialMapGroup(
                ReadUInt32(data, groupOffset),
                ReadUInt32(data, groupOffset + 4),
                ReadUInt32(data, groupOffset + 8),
                IsConstantGlyph: false);
        }

        return new GroupedCMap(groups);
    }

    private static CMap ReadFormat13CMap(byte[] data, int offset)
    {
        var length = checked((int)ReadUInt32(data, offset + 4));
        EnsureRange(data, offset, length);
        var groupCount = ReadUInt32(data, offset + 12);
        if (groupCount > int.MaxValue)
            throw new HaruException(HaruStatus.TtfInvalidCmap, "TrueType cmap format 13 is too large.");

        EnsureRange(data, offset + 16, checked((int)groupCount * 12));
        var groups = new SequentialMapGroup[(int)groupCount];
        for (var i = 0; i < groups.Length; i++)
        {
            var groupOffset = offset + 16 + i * 12;
            groups[i] = new SequentialMapGroup(
                ReadUInt32(data, groupOffset),
                ReadUInt32(data, groupOffset + 4),
                ReadUInt32(data, groupOffset + 8),
                IsConstantGlyph: true);
        }

        return new GroupedCMap(groups);
    }

    private static TtfNames ReadNames(byte[] data, TtfTable table)
    {
        var count = ReadUInt16(data, table.Offset + 2);
        var stringOffset = table.Offset + ReadUInt16(data, table.Offset + 4);
        string? postScriptName = null;
        string? familyName = null;
        string? subfamilyName = null;

        for (var i = 0; i < count; i++)
        {
            var recordOffset = table.Offset + 6 + i * 12;
            var platformId = ReadUInt16(data, recordOffset);
            var encodingId = ReadUInt16(data, recordOffset + 2);
            var languageId = ReadUInt16(data, recordOffset + 4);
            var nameId = ReadUInt16(data, recordOffset + 6);
            var length = ReadUInt16(data, recordOffset + 8);
            var offset = stringOffset + ReadUInt16(data, recordOffset + 10);
            EnsureRange(data, offset, length);

            var value = ReadNameString(data, offset, length, platformId, encodingId);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var preferred = languageId == 0x0409 || platformId == 1;
            if (nameId == 6 && (postScriptName is null || preferred))
                postScriptName = value;
            else if (nameId == 1 && (familyName is null || preferred))
                familyName = value;
            else if (nameId == 2 && (subfamilyName is null || preferred))
                subfamilyName = value;
        }

        return new TtfNames(SanitizeName(postScriptName), SanitizeName(familyName), SanitizeName(subfamilyName));
    }

    private static string ReadNameString(byte[] data, int offset, int length, int platformId, int encodingId)
    {
        if (platformId == 3 || platformId == 0 || encodingId is 1 or 10)
        {
            var chars = new char[length / 2];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = (char)ReadUInt16(data, offset + i * 2);

            return new string(chars);
        }

        return Encoding.Latin1.GetString(data, offset, length);
    }

    private static string? BuildBaseFontName(string? familyName, string? subfamilyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
            return null;

        if (string.IsNullOrWhiteSpace(subfamilyName) ||
            subfamilyName.Equals("Regular", StringComparison.OrdinalIgnoreCase))
        {
            return familyName;
        }

        return $"{familyName},{subfamilyName.Replace(" ", string.Empty, StringComparison.Ordinal)}";
    }

    private static string? SanitizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
                continue;

            builder.Append(ch);
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static int WidthForGlyph(int glyphId, ushort[] advances, int unitsPerEm)
    {
        if (glyphId < 0 || glyphId >= advances.Length)
            glyphId = 0;

        return Scale(advances[glyphId], unitsPerEm);
    }

    private static int WidthTop(CMap cmap, byte[] data, Dictionary<string, TtfTable> tables, int unitsPerEm, char ch)
    {
        if (!tables.TryGetValue("glyf", out var glyf) || !tables.TryGetValue("loca", out var loca) || !tables.TryGetValue("head", out var head) || !tables.TryGetValue("maxp", out var maxp))
            return 0;

        var indexToLocFormat = ReadInt16(data, head.Offset + 50);
        var numGlyphs = ReadUInt16(data, maxp.Offset + 4);
        var gid = cmap.GlyphId(ch);
        if (gid <= 0 || gid >= numGlyphs)
            return 0;

        var glyphOffset = ReadGlyphOffset(data, loca.Offset, indexToLocFormat, gid);
        var nextGlyphOffset = ReadGlyphOffset(data, loca.Offset, indexToLocFormat, gid + 1);
        if (nextGlyphOffset <= glyphOffset)
            return 0;

        var offset = glyf.Offset + glyphOffset;
        EnsureRange(data, offset, 10);
        return Scale(ReadInt16(data, offset + 8), unitsPerEm);
    }

    private static int ReadGlyphOffset(byte[] data, int locaOffset, int format, int glyphId)
    {
        if (format == 0)
            return ReadUInt16(data, locaOffset + glyphId * 2) * 2;

        return checked((int)ReadUInt32(data, locaOffset + glyphId * 4));
    }

    private static byte[] BuildStandaloneFontFile(
        byte[] data,
        int faceOffset,
        Dictionary<string, TtfTable> tables)
    {
        var tableData = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (tag, table) in tables)
            tableData[tag] = CopyTable(data, table);

        return BuildSfnt(data, faceOffset, tableData);
    }

    private static PdfFontSubsetData CreateSubsetFontFile(
        byte[] data,
        int faceOffset,
        Dictionary<string, TtfTable> tables,
        int numGlyphs,
        int numHMetrics,
        PdfFontSubsetRequest request)
    {
        if (!tables.TryGetValue("glyf", out var glyf) ||
            !tables.TryGetValue("loca", out var loca) ||
            !tables.TryGetValue("head", out var head) ||
            !tables.TryGetValue("hhea", out var hhea) ||
            !tables.TryGetValue("hmtx", out var hmtx) ||
            !tables.TryGetValue("maxp", out var maxp))
        {
            return new PdfFontSubsetData(data.ToArray(), IdentityGlyphMap(numGlyphs));
        }

        var indexToLocFormat = ReadInt16(data, head.Offset + 50);
        var glyphs = CollectSubsetGlyphs(data, glyf, loca, indexToLocFormat, numGlyphs, request.GlyphIds);
        var glyphIdMap = BuildDenseGlyphMap(glyphs);
        var (glyfData, locaData, subsetLocaFormat) = BuildSubsetGlyphTables(data, glyf, loca, indexToLocFormat, glyphIdMap);
        var glyphCount = glyphIdMap.Count;
        var tableData = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var (tag, table) in tables)
        {
            if (ShouldDropForSubset(tag))
                continue;

            tableData[tag] = CopyTable(data, table);
        }

        var headData = CopyTable(data, head);
        WriteUInt32(headData, 8, 0);
        WriteInt16(headData, 50, subsetLocaFormat);

        var hheaData = CopyTable(data, hhea);
        WriteUInt16(hheaData, 34, checked((ushort)glyphCount));

        var maxpData = CopyTable(data, maxp);
        WriteUInt16(maxpData, 4, checked((ushort)glyphCount));

        tableData["glyf"] = glyfData;
        tableData["loca"] = locaData;
        tableData["head"] = headData;
        tableData["hhea"] = hheaData;
        tableData["hmtx"] = BuildSubsetHmtxTable(data, hmtx, glyphIdMap, numGlyphs, numHMetrics);
        tableData["cmap"] = BuildSubsetCMapTable(request.UnicodeToGlyphId, glyphIdMap);
        tableData["maxp"] = maxpData;

        var subset = BuildSfnt(data, faceOffset, tableData);
        return new PdfFontSubsetData(subset, glyphIdMap);
    }

    private static bool ShouldDropForSubset(string tag)
    {
        return tag is "DSIG"
            or "BASE"
            or "CBDT"
            or "CBLC"
            or "COLR"
            or "CPAL"
            or "GDEF"
            or "GPOS"
            or "GSUB"
            or "JSTF"
            or "LTSH"
            or "MATH"
            or "SVG "
            or "VDMX"
            or "VORG"
            or "hdmx"
            or "kern"
            or "post"
            or "sbix"
            or "vhea"
            or "vmtx";
    }

    private static Dictionary<int, int> IdentityGlyphMap(int numGlyphs)
    {
        var map = new Dictionary<int, int>(numGlyphs);
        for (var glyphId = 0; glyphId < numGlyphs; glyphId++)
            map[glyphId] = glyphId;

        return map;
    }

    private static Dictionary<int, int> BuildDenseGlyphMap(SortedSet<int> glyphs)
    {
        var map = new Dictionary<int, int>(glyphs.Count);
        foreach (var glyphId in glyphs)
            map[glyphId] = map.Count;

        return map;
    }

    private static SortedSet<int> CollectSubsetGlyphs(
        byte[] data,
        TtfTable glyf,
        TtfTable loca,
        int indexToLocFormat,
        int numGlyphs,
        IEnumerable<int> seedGlyphIds)
    {
        var glyphs = new SortedSet<int> { 0 };

        foreach (var glyphId in seedGlyphIds)
            AddGlyphWithComponents(data, glyf, loca, indexToLocFormat, numGlyphs, glyphId, glyphs);

        return glyphs;
    }

    private static void AddGlyphWithComponents(
        byte[] data,
        TtfTable glyf,
        TtfTable loca,
        int indexToLocFormat,
        int numGlyphs,
        int glyphId,
        SortedSet<int> glyphs)
    {
        if (glyphId < 0 || glyphId >= numGlyphs || !glyphs.Add(glyphId))
            return;

        var glyphOffset = ReadGlyphOffset(data, loca.Offset, indexToLocFormat, glyphId);
        var nextGlyphOffset = ReadGlyphOffset(data, loca.Offset, indexToLocFormat, glyphId + 1);
        if (nextGlyphOffset <= glyphOffset)
            return;

        var offset = glyf.Offset + glyphOffset;
        var length = nextGlyphOffset - glyphOffset;
        EnsureRange(data, offset, length);
        if (length < 10 || ReadInt16(data, offset) >= 0)
            return;

        var cursor = offset + 10;
        var end = offset + length;
        while (cursor + 4 <= end)
        {
            var flags = ReadUInt16(data, cursor);
            var componentGlyphId = ReadUInt16(data, cursor + 2);
            cursor += 4;

            cursor += (flags & 0x0001) != 0 ? 4 : 2;
            if ((flags & 0x0008) != 0)
                cursor += 2;
            else if ((flags & 0x0040) != 0)
                cursor += 4;
            else if ((flags & 0x0080) != 0)
                cursor += 8;

            if (cursor > end)
                throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType composite glyph data is truncated.");

            AddGlyphWithComponents(data, glyf, loca, indexToLocFormat, numGlyphs, componentGlyphId, glyphs);

            if ((flags & 0x0020) == 0)
                break;
        }
    }

    private static (byte[] Glyf, byte[] Loca, short LocaFormat) BuildSubsetGlyphTables(
        byte[] data,
        TtfTable glyf,
        TtfTable loca,
        int indexToLocFormat,
        IReadOnlyDictionary<int, int> glyphIdMap)
    {
        using var glyfStream = new MemoryStream();
        var glyphCount = glyphIdMap.Count;
        var offsets = new uint[glyphCount + 1];
        var originalGlyphIds = new int[glyphCount];
        foreach (var (originalGlyphId, subsetGlyphId) in glyphIdMap)
            originalGlyphIds[subsetGlyphId] = originalGlyphId;

        for (var subsetGlyphId = 0; subsetGlyphId < glyphCount; subsetGlyphId++)
        {
            offsets[subsetGlyphId] = checked((uint)glyfStream.Length);
            var glyphId = originalGlyphIds[subsetGlyphId];
            var glyphOffset = ReadGlyphOffset(data, loca.Offset, indexToLocFormat, glyphId);
            var nextGlyphOffset = ReadGlyphOffset(data, loca.Offset, indexToLocFormat, glyphId + 1);
            if (nextGlyphOffset <= glyphOffset)
                continue;

            var length = nextGlyphOffset - glyphOffset;
            EnsureRange(data, glyf.Offset + glyphOffset, length);
            var glyphData = new byte[length];
            Array.Copy(data, glyf.Offset + glyphOffset, glyphData, 0, length);
            RemapCompositeGlyph(glyphData, glyphIdMap);
            glyfStream.Write(glyphData, 0, glyphData.Length);
            PadStream(glyfStream);
        }

        offsets[glyphCount] = checked((uint)glyfStream.Length);

        var canUseShortLoca = true;
        foreach (var offset in offsets)
        {
            if ((offset & 1) != 0 || offset / 2 > ushort.MaxValue)
            {
                canUseShortLoca = false;
                break;
            }
        }

        if (canUseShortLoca)
        {
            var shortLocaData = new byte[(glyphCount + 1) * 2];
            for (var i = 0; i < offsets.Length; i++)
                WriteUInt16(shortLocaData, i * 2, checked((ushort)(offsets[i] / 2)));

            return (glyfStream.ToArray(), shortLocaData, 0);
        }

        var longLocaData = new byte[(glyphCount + 1) * 4];
        for (var i = 0; i < offsets.Length; i++)
            WriteUInt32(longLocaData, i * 4, offsets[i]);

        return (glyfStream.ToArray(), longLocaData, 1);
    }

    private static void RemapCompositeGlyph(byte[] glyphData, IReadOnlyDictionary<int, int> glyphIdMap)
    {
        if (glyphData.Length < 10 || ReadInt16(glyphData, 0) >= 0)
            return;

        var cursor = 10;
        while (cursor + 4 <= glyphData.Length)
        {
            var flags = ReadUInt16(glyphData, cursor);
            var originalComponentGlyphId = ReadUInt16(glyphData, cursor + 2);
            if (glyphIdMap.TryGetValue(originalComponentGlyphId, out var subsetComponentGlyphId))
                WriteUInt16(glyphData, cursor + 2, checked((ushort)subsetComponentGlyphId));

            cursor += 4;
            cursor += (flags & 0x0001) != 0 ? 4 : 2;
            if ((flags & 0x0008) != 0)
                cursor += 2;
            else if ((flags & 0x0040) != 0)
                cursor += 4;
            else if ((flags & 0x0080) != 0)
                cursor += 8;

            if ((flags & 0x0020) == 0)
                break;
        }
    }

    private static byte[] BuildSubsetHmtxTable(
        byte[] data,
        TtfTable hmtx,
        IReadOnlyDictionary<int, int> glyphIdMap,
        int numGlyphs,
        int numHMetrics)
    {
        var glyphCount = glyphIdMap.Count;
        var hmtxData = new byte[glyphCount * 4];
        var originalGlyphIds = new int[glyphCount];
        foreach (var (originalGlyphId, subsetGlyphId) in glyphIdMap)
            originalGlyphIds[subsetGlyphId] = originalGlyphId;

        for (var subsetGlyphId = 0; subsetGlyphId < glyphCount; subsetGlyphId++)
        {
            var originalGlyphId = originalGlyphIds[subsetGlyphId];
            WriteUInt16(hmtxData, subsetGlyphId * 4, ReadGlyphAdvance(data, hmtx, originalGlyphId, numGlyphs, numHMetrics));
            WriteInt16(hmtxData, subsetGlyphId * 4 + 2, ReadGlyphLeftSideBearing(data, hmtx, originalGlyphId, numGlyphs, numHMetrics));
        }

        return hmtxData;
    }

    private static ushort ReadGlyphAdvance(byte[] data, TtfTable hmtx, int glyphId, int numGlyphs, int numHMetrics)
    {
        if (glyphId < 0 || glyphId >= numGlyphs)
            glyphId = 0;

        var metricGlyphId = Math.Min(glyphId, numHMetrics - 1);
        return ReadUInt16(data, hmtx.Offset + metricGlyphId * 4);
    }

    private static short ReadGlyphLeftSideBearing(byte[] data, TtfTable hmtx, int glyphId, int numGlyphs, int numHMetrics)
    {
        if (glyphId < 0 || glyphId >= numGlyphs)
            glyphId = 0;

        if (glyphId < numHMetrics)
            return ReadInt16(data, hmtx.Offset + glyphId * 4 + 2);

        return ReadInt16(data, hmtx.Offset + numHMetrics * 4 + (glyphId - numHMetrics) * 2);
    }

    private static byte[] BuildSubsetCMapTable(
        IReadOnlyDictionary<int, int> unicodeToGlyphId,
        IReadOnlyDictionary<int, int> glyphIdMap)
    {
        var unicodeToSubsetGlyphId = new SortedDictionary<int, int>();
        foreach (var (unicode, originalGlyphId) in unicodeToGlyphId)
        {
            if (unicode <= 0 || unicode > 0x10FFFF)
                continue;

            if (glyphIdMap.TryGetValue(originalGlyphId, out var subsetGlyphId) && subsetGlyphId > 0)
                unicodeToSubsetGlyphId.TryAdd(unicode, subsetGlyphId);
        }

        var format4 = BuildFormat4CMapTable(unicodeToSubsetGlyphId);
        var needsFormat12 = unicodeToSubsetGlyphId.Keys.Any(static unicode => unicode > 0xFFFF);
        var subtableCount = needsFormat12 ? 2 : 1;
        var headerLength = 4 + subtableCount * 8;
        var format12 = needsFormat12 ? BuildFormat12CMapTable(unicodeToSubsetGlyphId) : [];
        var length = headerLength + format4.Length + format12.Length;
        var cmap = new byte[length];

        WriteUInt16(cmap, 0, 0);
        WriteUInt16(cmap, 2, checked((ushort)subtableCount));
        WriteUInt16(cmap, 4, 3);
        WriteUInt16(cmap, 6, 1);
        WriteUInt32(cmap, 8, checked((uint)headerLength));
        Array.Copy(format4, 0, cmap, headerLength, format4.Length);

        if (needsFormat12)
        {
            WriteUInt16(cmap, 12, 3);
            WriteUInt16(cmap, 14, 10);
            WriteUInt32(cmap, 16, checked((uint)(headerLength + format4.Length)));
            Array.Copy(format12, 0, cmap, headerLength + format4.Length, format12.Length);
        }

        return cmap;
    }

    private static byte[] BuildFormat4CMapTable(SortedDictionary<int, int> unicodeToSubsetGlyphId)
    {
        var bmpMappings = unicodeToSubsetGlyphId
            .Where(static item => item.Key is > 0 and < 0xFFFF)
            .Select(static item => (Unicode: item.Key, GlyphId: item.Value))
            .ToArray();
        var segCount = bmpMappings.Length + 1;
        var length = 16 + segCount * 8;
        var table = new byte[length];

        WriteUInt16(table, 0, 4);
        WriteUInt16(table, 2, checked((ushort)length));
        WriteUInt16(table, 4, 0);
        WriteUInt16(table, 6, checked((ushort)(segCount * 2)));

        var maxPowerOfTwo = 1;
        var entrySelector = 0;
        while (maxPowerOfTwo * 2 <= segCount)
        {
            maxPowerOfTwo *= 2;
            entrySelector++;
        }

        var searchRange = maxPowerOfTwo * 2;
        WriteUInt16(table, 8, checked((ushort)searchRange));
        WriteUInt16(table, 10, checked((ushort)entrySelector));
        WriteUInt16(table, 12, checked((ushort)(segCount * 2 - searchRange)));

        var endCodeOffset = 14;
        var startCodeOffset = endCodeOffset + segCount * 2 + 2;
        var idDeltaOffset = startCodeOffset + segCount * 2;
        var idRangeOffsetOffset = idDeltaOffset + segCount * 2;

        for (var i = 0; i < bmpMappings.Length; i++)
        {
            var (unicode, glyphId) = bmpMappings[i];
            WriteUInt16(table, endCodeOffset + i * 2, checked((ushort)unicode));
            WriteUInt16(table, startCodeOffset + i * 2, checked((ushort)unicode));
            WriteInt16(table, idDeltaOffset + i * 2, unchecked((short)((glyphId - unicode) & 0xFFFF)));
            WriteUInt16(table, idRangeOffsetOffset + i * 2, 0);
        }

        var sentinelIndex = segCount - 1;
        WriteUInt16(table, endCodeOffset + sentinelIndex * 2, 0xFFFF);
        WriteUInt16(table, endCodeOffset + segCount * 2, 0);
        WriteUInt16(table, startCodeOffset + sentinelIndex * 2, 0xFFFF);
        WriteInt16(table, idDeltaOffset + sentinelIndex * 2, 1);
        WriteUInt16(table, idRangeOffsetOffset + sentinelIndex * 2, 0);
        return table;
    }

    private static byte[] BuildFormat12CMapTable(SortedDictionary<int, int> unicodeToSubsetGlyphId)
    {
        var groups = new List<(int StartUnicode, int EndUnicode, int StartGlyphId)>();
        foreach (var (unicode, glyphId) in unicodeToSubsetGlyphId)
        {
            if (groups.Count > 0)
            {
                var last = groups[^1];
                var expectedGlyphId = last.StartGlyphId + (unicode - last.StartUnicode);
                if (unicode == last.EndUnicode + 1 && glyphId == expectedGlyphId)
                {
                    groups[^1] = (last.StartUnicode, unicode, last.StartGlyphId);
                    continue;
                }
            }

            groups.Add((unicode, unicode, glyphId));
        }

        var length = 16 + groups.Count * 12;
        var table = new byte[length];
        WriteUInt16(table, 0, 12);
        WriteUInt16(table, 2, 0);
        WriteUInt32(table, 4, checked((uint)length));
        WriteUInt32(table, 8, 0);
        WriteUInt32(table, 12, checked((uint)groups.Count));

        for (var i = 0; i < groups.Count; i++)
        {
            var offset = 16 + i * 12;
            var group = groups[i];
            WriteUInt32(table, offset, checked((uint)group.StartUnicode));
            WriteUInt32(table, offset + 4, checked((uint)group.EndUnicode));
            WriteUInt32(table, offset + 8, checked((uint)group.StartGlyphId));
        }

        return table;
    }

    private static byte[] BuildSfnt(byte[] source, int faceOffset, SortedDictionary<string, byte[]> tableData)
    {
        var tableCount = tableData.Count;
        var directoryLength = 12 + tableCount * 16;
        var offset = directoryLength;
        var records = new List<TableBuildRecord>(tableCount);

        foreach (var (tag, bytes) in tableData)
        {
            offset = Align4(offset);
            records.Add(new TableBuildRecord(tag, CalculateChecksum(bytes, 0, bytes.Length), offset, bytes.Length, bytes));
            offset += Align4(bytes.Length);
        }

        var output = new byte[offset];
        WriteUInt32(output, 0, ReadUInt32(source, faceOffset));
        WriteUInt16(output, 4, checked((ushort)tableCount));
        var maxPowerOfTwo = 1;
        var entrySelector = 0;
        while (maxPowerOfTwo * 2 <= tableCount)
        {
            maxPowerOfTwo *= 2;
            entrySelector++;
        }

        var searchRange = maxPowerOfTwo * 16;
        WriteUInt16(output, 6, checked((ushort)searchRange));
        WriteUInt16(output, 8, checked((ushort)entrySelector));
        WriteUInt16(output, 10, checked((ushort)(tableCount * 16 - searchRange)));

        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var recordOffset = 12 + i * 16;
            WriteTag(output, recordOffset, record.Tag);
            WriteUInt32(output, recordOffset + 4, record.Checksum);
            WriteUInt32(output, recordOffset + 8, checked((uint)record.Offset));
            WriteUInt32(output, recordOffset + 12, checked((uint)record.Length));
            Array.Copy(record.Data, 0, output, record.Offset, record.Length);
        }

        var headOffset = -1;
        foreach (var record in records)
        {
            if (record.Tag == "head")
            {
                headOffset = record.Offset;
                break;
            }
        }

        if (headOffset >= 0)
        {
            WriteUInt32(output, headOffset + 8, 0);
            var checksum = CalculateChecksum(output, 0, output.Length);
            WriteUInt32(output, headOffset + 8, unchecked(0xB1B0AFBAu - checksum));
        }

        return output;
    }

    private static byte[] CopyTable(byte[] data, TtfTable table)
    {
        var copy = new byte[table.Length];
        Array.Copy(data, table.Offset, copy, 0, table.Length);
        return copy;
    }

    private static void PadStream(MemoryStream stream)
    {
        while (stream.Length % 4 != 0)
            stream.WriteByte(0);
    }

    private static int Align4(int value) => (value + 3) & ~3;

    private static uint CalculateChecksum(byte[] data, int offset, int length)
    {
        uint checksum = 0;
        var paddedLength = Align4(length);

        for (var i = 0; i < paddedLength; i += 4)
        {
            var value = (uint)((i < length ? data[offset + i] : 0) << 24);
            value |= (uint)((i + 1 < length ? data[offset + i + 1] : 0) << 16);
            value |= (uint)((i + 2 < length ? data[offset + i + 2] : 0) << 8);
            value |= i + 3 < length ? data[offset + i + 3] : 0u;
            checksum = unchecked(checksum + value);
        }

        return checksum;
    }

    private static TtfTable Required(Dictionary<string, TtfTable> tables, string tag, uint status)
    {
        if (!tables.TryGetValue(tag, out var table))
            throw new HaruException(status, $"TrueType required table is missing: {tag}.");

        return table;
    }

    private static int Scale(int value, int unitsPerEm) => (int)Math.Round(value * 1000.0 / unitsPerEm);

    private static ushort ReadUInt16(byte[] data, int offset)
    {
        EnsureRange(data, offset, 2);
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static short ReadInt16(byte[] data, int offset) => unchecked((short)ReadUInt16(data, offset));

    private static uint ReadUInt32(byte[] data, int offset)
    {
        EnsureRange(data, offset, 4);
        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }

    private static string ReadTag(byte[] data, int offset)
    {
        EnsureRange(data, offset, 4);
        return Encoding.ASCII.GetString(data, offset, 4);
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static void WriteInt16(byte[] data, int offset, short value) => WriteUInt16(data, offset, unchecked((ushort)value));

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteTag(byte[] data, int offset, string tag)
    {
        if (tag.Length != 4)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType table tag length is invalid.");

        for (var i = 0; i < tag.Length; i++)
            data[offset + i] = (byte)tag[i];
    }

    private static void EnsureRange(byte[] data, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > data.Length - length)
            throw new HaruException(HaruStatus.TtfInvalidFormat, "TrueType table data is truncated.");
    }

    private readonly record struct TtfTable(int Offset, int Length);

    private readonly record struct TableBuildRecord(string Tag, uint Checksum, int Offset, int Length, byte[] Data);

    private readonly record struct TtfNames(string? PostScriptName, string? FamilyName, string? SubfamilyName);

    private sealed class CffMetadata(bool isCidKeyed, string ordering, int supplement, int[] gidToCid)
    {
        internal bool IsCidKeyed { get; } = isCidKeyed;

        internal string Ordering { get; } = ordering;

        internal int Supplement { get; } = supplement;

        internal int CidOfGlyph(int glyphId) =>
            glyphId >= 0 && glyphId < gidToCid.Length ? gidToCid[glyphId] : 0;
    }

    private sealed record CffIndex(IReadOnlyList<CffSlice> Objects, int EndOffset);

    private readonly record struct CffSlice(int Offset, int Length);

    private abstract class CMap
    {
        internal abstract int GlyphId(int unicode);
    }

    private sealed class Format0CMap : CMap
    {
        private readonly ushort[] _glyphs;

        internal Format0CMap(ushort[] glyphs)
        {
            _glyphs = glyphs;
        }

        internal override int GlyphId(int unicode) => unicode is >= 0 and < 256 ? _glyphs[unicode] : 0;
    }

    private sealed class Format4CMap : CMap
    {
        private readonly ushort[] _endCount;
        private readonly ushort[] _startCount;
        private readonly short[] _idDelta;
        private readonly ushort[] _idRangeOffset;
        private readonly ushort[] _glyphArray;

        internal Format4CMap(ushort[] endCount, ushort[] startCount, short[] idDelta, ushort[] idRangeOffset, ushort[] glyphArray)
        {
            _endCount = endCount;
            _startCount = startCount;
            _idDelta = idDelta;
            _idRangeOffset = idRangeOffset;
            _glyphArray = glyphArray;
        }

        internal override int GlyphId(int unicode)
        {
            for (var i = 0; i < _endCount.Length; i++)
            {
                if (unicode > _endCount[i])
                    continue;

                if (unicode < _startCount[i])
                    return 0;

                if (_idRangeOffset[i] == 0)
                    return (unicode + _idDelta[i]) & 0xFFFF;

                var index = _idRangeOffset[i] / 2 + (unicode - _startCount[i]) - (_endCount.Length - i);
                if (index < 0 || index >= _glyphArray.Length)
                    return 0;

                var glyph = _glyphArray[index];
                if (glyph == 0)
                    return 0;

                return (glyph + _idDelta[i]) & 0xFFFF;
            }

            return 0;
        }
    }

    private sealed class TrimmedCMap : CMap
    {
        private readonly int _firstCode;
        private readonly ushort[] _glyphs;

        internal TrimmedCMap(int firstCode, ushort[] glyphs)
        {
            _firstCode = firstCode;
            _glyphs = glyphs;
        }

        internal override int GlyphId(int unicode)
        {
            var index = unicode - _firstCode;
            return index >= 0 && index < _glyphs.Length ? _glyphs[index] : 0;
        }
    }

    private sealed class GroupedCMap : CMap
    {
        private readonly SequentialMapGroup[] _groups;

        internal GroupedCMap(SequentialMapGroup[] groups)
        {
            _groups = groups;
        }

        internal override int GlyphId(int unicode)
        {
            if (unicode < 0)
                return 0;

            var code = (uint)unicode;
            var low = 0;
            var high = _groups.Length - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) / 2);
                var group = _groups[mid];

                if (code < group.StartCharCode)
                {
                    high = mid - 1;
                    continue;
                }

                if (code > group.EndCharCode)
                {
                    low = mid + 1;
                    continue;
                }

                var glyphId = group.IsConstantGlyph
                    ? group.GlyphId
                    : group.GlyphId + (code - group.StartCharCode);
                return glyphId <= ushort.MaxValue ? (int)glyphId : 0;
            }

            return 0;
        }
    }

    private readonly record struct SequentialMapGroup(
        uint StartCharCode,
        uint EndCharCode,
        uint GlyphId,
        bool IsConstantGlyph);
}
