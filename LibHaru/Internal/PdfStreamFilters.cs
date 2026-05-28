namespace LibHaru.Internal;

[Flags]
internal enum PdfStreamFilter
{
    None = 0x0000,
    ASCIIHex = 0x0100,
    ASCII85 = 0x0200,
    FlateDecode = 0x0400,
    DctDecode = 0x0800,
    CcittDecode = 0x1000
}

internal enum PdfStreamKind
{
    Generic,
    PageContent,
    Image,
    Metadata,
    Font,
    EmbeddedFile,
    JavaScript,
    U3D,
    Shading,
    IccProfile
}