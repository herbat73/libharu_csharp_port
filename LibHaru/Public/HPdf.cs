namespace LibHaru;

public static class HPdf
{
    public static string HPDF_GetVersion() => HaruVersion.Text;

    public static PdfDocument HPDF_New(HaruErrorHandler? errorHandler = null, object? userData = null) =>
        PdfDocument.New(errorHandler, userData);

    public static PdfDocument HPDF_NewEx(
        HaruErrorHandler? errorHandler = null,
        object? userAllocFunc = null,
        object? userFreeFunc = null,
        uint memPoolBufSize = 0,
        object? userData = null) =>
        PdfDocument.New(errorHandler, userData);

    public static void HPDF_Free(PdfDocument? pdf) => pdf?.Dispose();

    public static PdfDocument HPDF_GetDocMMgr(PdfDocument pdf) => pdf;

    public static uint HPDF_NewDoc(PdfDocument pdf)
    {
        pdf.NewDoc();
        return HaruStatus.OK;
    }

    public static void HPDF_FreeDoc(PdfDocument? pdf) => pdf?.FreeDoc();

    public static bool HPDF_HasDoc(PdfDocument? pdf) => pdf?.HasDoc() == true;

    public static void HPDF_FreeDocAll(PdfDocument? pdf) => pdf?.FreeDocAll();

    public static uint HPDF_SetPagesConfiguration(PdfDocument pdf, uint pagePerPages)
    {
        pdf.SetPagesConfiguration(pagePerPages);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetErrorHandler(PdfDocument pdf, HaruErrorHandler? errorHandler, object? userData = null)
    {
        pdf.SetErrorHandler(errorHandler, userData);
        return HaruStatus.OK;
    }

    public static uint HPDF_GetError(PdfDocument pdf) => pdf.GetError();

    public static uint HPDF_GetErrorDetail(PdfDocument pdf) => pdf.GetErrorDetail();

    public static void HPDF_ResetError(PdfDocument pdf) => pdf.ResetError();

    public static uint HPDF_CheckError(HaruError error) => error.CheckError();

    public static uint HPDF_CheckError(PdfDocument pdf) => pdf.CheckError();

    public static uint HPDF_SaveToFile(PdfDocument pdf, string fileName)
    {
        pdf.SaveToFile(fileName);
        return HaruStatus.OK;
    }

    public static byte[] HPDF_SaveToStream(PdfDocument pdf) => pdf.SaveToStream();

    public static uint HPDF_GetStreamSize(PdfDocument pdf) => pdf.GetStreamSize();

    public static byte[] HPDF_ReadFromStream(PdfDocument pdf, uint size) => pdf.ReadFromStream(size);

    public static uint HPDF_ReadFromStream(PdfDocument pdf, byte[] buffer, ref uint size)
    {
        if (buffer is null)
            throw pdf.CreateException(HaruStatus.InvalidParameter, "Read buffer cannot be null.");

        var data = pdf.ReadFromStream(size);
        if (data.Length > buffer.Length)
            throw pdf.CreateException(HaruStatus.InvalidParameter, "Read buffer is smaller than requested stream data.");

        Array.Copy(data, buffer, data.Length);
        size = (uint)data.Length;
        return HaruStatus.OK;
    }

    public static byte[] HPDF_GetContents(PdfDocument pdf) => pdf.GetContents();

    public static void HPDF_ResetStream(PdfDocument pdf) => pdf.ResetStream();

    public static PdfPage HPDF_AddPage(PdfDocument pdf) => pdf.AddPage();

    public static PdfPage HPDF_InsertPage(PdfDocument pdf, PdfPage beforePage) => pdf.InsertPage(beforePage);

    public static PdfPage HPDF_GetPageByIndex(PdfDocument pdf, uint index) => pdf.GetPageByIndex((int)index);

    public static PdfPage? HPDF_GetCurrentPage(PdfDocument pdf) => pdf.CurrentPage;

    public static PdfDocument HPDF_GetPageMMgr(PdfPage page) => page.Owner;

    public static PdfFont HPDF_GetFont(PdfDocument pdf, string fontName, string? encoding = null) => pdf.GetFont(fontName, encoding);

    public static PdfEncoder HPDF_GetEncoder(PdfDocument pdf, string encodingName) => pdf.GetEncoder(encodingName);

    public static PdfEncoder? HPDF_GetCurrentEncoder(PdfDocument pdf) => pdf.CurrentEncoder;

    public static uint HPDF_SetCurrentEncoder(PdfDocument pdf, string encodingName)
    {
        pdf.SetCurrentEncoder(encodingName);
        return HaruStatus.OK;
    }

    public static PdfEncoderType HPDF_Encoder_GetType(PdfEncoder encoder) => encoder.Type;

    public static PdfByteType HPDF_Encoder_GetByteType(PdfEncoder encoder, string text, uint index) =>
        encoder.GetByteType(text, index);

    public static ushort HPDF_Encoder_GetUnicode(PdfEncoder encoder, ushort code) => encoder.GetUnicode(code);

    public static PdfWritingMode HPDF_Encoder_GetWritingMode(PdfEncoder encoder) => encoder.WritingMode;

    public static string HPDF_LoadType1FontFromFile(PdfDocument pdf, string afmFileName, string? dataFileName = null) =>
        pdf.LoadType1FontFromFile(afmFileName, dataFileName);

    public static string HPDF_LoadTTFontFromFile(PdfDocument pdf, string fileName, bool embedding) =>
        pdf.LoadTTFontFromFile(fileName, embedding);

    public static PdfFontDef HPDF_GetTTFontDefFromFile(PdfDocument pdf, string fileName, bool embedding) =>
        pdf.GetTTFontDefFromFile(fileName, embedding);

    public static string HPDF_LoadTTFontFromFile2(PdfDocument pdf, string fileName, uint index, bool embedding)
    {
        if (index > int.MaxValue)
            throw pdf.CreateException(HaruStatus.InvalidTtcIndex, "TrueType collection index is too large.");

        return pdf.LoadTTFontFromFile2(fileName, (int)index, embedding);
    }

    public static string HPDF_LoadTTFontFromMemory(PdfDocument pdf, byte[] buffer, bool embedding) =>
        pdf.LoadTTFontFromMemory(buffer, embedding);

    public static string HPDF_Font_GetFontName(PdfFont font) => font.BaseFont;

    public static string HPDF_Font_GetEncodingName(PdfFont font) => font.Encoding;

    public static int HPDF_Font_GetUnicodeWidth(PdfFont font, char code) => font.GetUnicodeWidth(code);

    public static PdfRect HPDF_Font_GetBBox(PdfFont font) => font.BBox;

    public static int HPDF_Font_GetAscent(PdfFont font) => font.Ascent;

    public static int HPDF_Font_GetDescent(PdfFont font) => font.Descent;

    public static int HPDF_Font_GetXHeight(PdfFont font) => font.XHeight;

    public static int HPDF_Font_GetCapHeight(PdfFont font) => font.CapHeight;

    public static PdfTextWidth HPDF_Font_TextWidth(PdfFont font, string text) => font.TextWidthInfo(text);

    public static uint HPDF_Font_MeasureText(
        PdfFont font,
        string text,
        double width,
        double fontSize,
        double charSpace,
        double wordSpace,
        bool wordWrap,
        out double realWidth) =>
        font.MeasureText(text, width, fontSize, charSpace, wordSpace, wordWrap, out realWidth);

    public static uint HPDF_SetCompressionMode(PdfDocument pdf, CompressionMode mode)
    {
        pdf.SetCompressionMode(mode);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetPageLayout(PdfDocument pdf, PdfPageLayout layout)
    {
        pdf.SetPageLayout(layout);
        return HaruStatus.OK;
    }

    public static PdfPageLayout HPDF_GetPageLayout(PdfDocument pdf) => pdf.PageLayout;

    public static uint HPDF_SetPageMode(PdfDocument pdf, PdfPageMode mode)
    {
        pdf.SetPageMode(mode);
        return HaruStatus.OK;
    }

    public static PdfPageMode HPDF_GetPageMode(PdfDocument pdf) => pdf.PageMode;

    public static uint HPDF_SetViewerPreference(PdfDocument pdf, PdfViewerPreference preference)
    {
        pdf.SetViewerPreference(preference);
        return HaruStatus.OK;
    }

    public static PdfViewerPreference HPDF_GetViewerPreference(PdfDocument pdf) => pdf.ViewerPreference;

    public static uint HPDF_SetOpenAction(PdfDocument pdf, PdfDestination destination)
    {
        pdf.SetOpenAction(destination);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetOpenAction(PdfDocument pdf, PdfJavaScript javaScript)
    {
        pdf.SetOpenAction(javaScript);
        return HaruStatus.OK;
    }

    public static uint HPDF_AddPageLabel(PdfDocument pdf, uint pageNum, PdfPageNumStyle style, uint firstPage = 1, string prefix = "")
    {
        if (pageNum > int.MaxValue || firstPage > int.MaxValue)
            throw pdf.CreateException(HaruStatus.InvalidParameter, "Page label values are too large.");

        pdf.AddPageLabel((int)pageNum, style, (int)firstPage, prefix);
        return HaruStatus.OK;
    }

    public static PdfOutline HPDF_CreateOutline(PdfDocument pdf, PdfOutline? parent, string title, object? encoder = null) =>
        pdf.CreateOutline(parent, title);

    public static uint HPDF_Outline_SetOpened(PdfOutline outline, bool opened)
    {
        outline.SetOpened(opened);
        return HaruStatus.OK;
    }

    public static uint HPDF_Outline_SetDestination(PdfOutline outline, PdfDestination destination)
    {
        outline.SetDestination(destination);
        return HaruStatus.OK;
    }

    public static uint HPDF_AddNamedDestination(PdfDocument pdf, string name, PdfDestination destination)
    {
        pdf.AddNamedDestination(name, destination);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetInfoAttr(PdfDocument pdf, PdfInfoType type, string value)
    {
        pdf.SetInfoAttr(type, value);
        return HaruStatus.OK;
    }

    public static string? HPDF_GetInfoAttr(PdfDocument pdf, PdfInfoType type) => pdf.GetInfoAttr(type);

    public static uint HPDF_SetInfoDateAttr(PdfDocument pdf, PdfInfoType type, DateTimeOffset value)
    {
        pdf.SetInfoDateAttr(type, value);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetXmpMetadata(PdfDocument pdf, string xml)
    {
        pdf.SetXmpMetadata(xml);
        return HaruStatus.OK;
    }

    public static PdfJavaScript HPDF_CreateJavaScript(PdfDocument pdf, string code) => pdf.CreateJavaScript(code);

    public static PdfJavaScript HPDF_LoadJSFromFile(PdfDocument pdf, string fileName) => pdf.LoadJavaScriptFromFile(fileName);

    public static uint HPDF_AddNamedJavaScript(PdfDocument pdf, string name, PdfJavaScript javaScript)
    {
        pdf.AddNamedJavaScript(name, javaScript);
        return HaruStatus.OK;
    }

    public static PdfEmbeddedFile HPDF_AttachFile(PdfDocument pdf, string fileName) => pdf.AttachFile(fileName);

    public static uint HPDF_EmbeddedFile_SetName(PdfEmbeddedFile embeddedFile, string name)
    {
        embeddedFile.SetName(name);
        return HaruStatus.OK;
    }

    public static uint HPDF_EmbeddedFile_SetDescription(PdfEmbeddedFile embeddedFile, string description)
    {
        embeddedFile.SetDescription(description);
        return HaruStatus.OK;
    }

    public static uint HPDF_EmbeddedFile_SetSubtype(PdfEmbeddedFile embeddedFile, string subtype)
    {
        embeddedFile.SetSubtype(subtype);
        return HaruStatus.OK;
    }

    public static uint HPDF_EmbeddedFile_SetAFRelationship(PdfEmbeddedFile embeddedFile, PdfAFRelationship relationship)
    {
        embeddedFile.SetAFRelationship(relationship);
        return HaruStatus.OK;
    }

    public static uint HPDF_EmbeddedFile_SetSize(PdfEmbeddedFile embeddedFile, long size)
    {
        embeddedFile.SetSize(size);
        return HaruStatus.OK;
    }

    public static uint HPDF_EmbeddedFile_SetCreationDate(PdfEmbeddedFile embeddedFile, DateTimeOffset date)
    {
        embeddedFile.SetCreationDate(date);
        return HaruStatus.OK;
    }

    public static uint HPDF_EmbeddedFile_SetLastModificationDate(PdfEmbeddedFile embeddedFile, DateTimeOffset date)
    {
        embeddedFile.SetLastModificationDate(date);
        return HaruStatus.OK;
    }

    public static PdfOutputIntent HPDF_AppendOutputIntents(PdfDocument pdf, string outputConditionIdentifier, byte[] iccProfile, string? info = null) =>
        pdf.AppendOutputIntent(outputConditionIdentifier, iccProfile, info);

    public static PdfOutputIntent HPDF_AppendOutputIntents(PdfDocument pdf, string outputConditionIdentifier, PdfIccProfile iccProfile, string? info = null) =>
        pdf.AppendOutputIntent(outputConditionIdentifier, iccProfile, info);

    public static PdfIccProfile HPDF_ICC_LoadIccFromMem(PdfDocument pdf, byte[] iccProfile, int numcomponent) =>
        pdf.LoadIccProfileFromMem(iccProfile, numcomponent);

    public static PdfIccProfile HPDF_LoadIccProfileFromFile(PdfDocument pdf, string iccFileName, int numcomponent) =>
        pdf.LoadIccProfileFromFile(iccFileName, numcomponent);

    public static uint HPDF_PDFA_SetPDFAConformance(PdfDocument pdf, PdfPdfAType pdfAType)
    {
        pdf.SetPdfAConformance(pdfAType);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetPDFAConformance(PdfDocument pdf, PdfPdfAType pdfAType) =>
        HPDF_PDFA_SetPDFAConformance(pdf, pdfAType);

    public static uint HPDF_AddPDFAXmpExtension(PdfDocument pdf, string xmpExtension)
    {
        pdf.AddPdfAXmpExtension(xmpExtension);
        return HaruStatus.OK;
    }

    public static PdfExtGState HPDF_CreateExtGState(PdfDocument pdf) => pdf.CreateExtGState();

    public static uint HPDF_ExtGState_SetAlphaStroke(PdfExtGState extGState, double value)
    {
        extGState.SetAlphaStroke(value);
        return HaruStatus.OK;
    }

    public static uint HPDF_ExtGState_SetAlphaFill(PdfExtGState extGState, double value)
    {
        extGState.SetAlphaFill(value);
        return HaruStatus.OK;
    }

    public static uint HPDF_ExtGState_SetBlendMode(PdfExtGState extGState, PdfBlendMode mode)
    {
        extGState.SetBlendMode(mode);
        return HaruStatus.OK;
    }

    public static PdfShading HPDF_Shading_New(PdfDocument pdf, PdfShadingType type, PdfColorSpace colorSpace, double xMin, double xMax, double yMin, double yMax) =>
        pdf.CreateShading(type, colorSpace, xMin, xMax, yMin, yMax);

    public static PdfShading HPDF_Shading_NewAxial(PdfDocument pdf, PdfPoint startPoint, PdfPoint endPoint, PdfRgbColor startColor, PdfRgbColor endColor, bool extendStart = false, bool extendEnd = false) =>
        pdf.CreateAxialShading(startPoint, endPoint, startColor, endColor, extendStart, extendEnd);

    public static PdfShading HPDF_Shading_NewRadial(PdfDocument pdf, PdfPoint startCenter, double startRadius, PdfPoint endCenter, double endRadius, PdfRgbColor startColor, PdfRgbColor endColor, bool extendStart = false, bool extendEnd = false) =>
        pdf.CreateRadialShading(startCenter, startRadius, endCenter, endRadius, startColor, endColor, extendStart, extendEnd);

    public static uint HPDF_Shading_AddVertexRGB(PdfShading shading, PdfShadingFreeFormTriangleMeshEdgeFlag edgeFlag, double x, double y, byte r, byte g, byte b)
    {
        shading.AddVertexRGB(edgeFlag, x, y, r, g, b);
        return HaruStatus.OK;
    }

    public static PdfU3D HPDF_LoadU3DFromFile(PdfDocument pdf, string fileName) => pdf.LoadU3DFromFile(fileName);

    public static PdfU3D HPDF_LoadU3DFromMem(PdfDocument pdf, byte[] data) => pdf.LoadU3DFromMem(data);

    public static Pdf3DView HPDF_Create3DView(PdfDocument pdf, string name) => pdf.Create3DView(name);

    public static uint HPDF_U3D_Add3DView(PdfU3D u3d, Pdf3DView view)
    {
        u3d.Add3DView(view);
        return HaruStatus.OK;
    }

    public static uint HPDF_U3D_SetDefault3DView(PdfU3D u3d, string name)
    {
        u3d.SetDefault3DView(name);
        return HaruStatus.OK;
    }

    public static uint HPDF_U3D_AddOnInstanciate(PdfU3D u3d, PdfJavaScript javaScript)
    {
        u3d.AddOnInstantiate(javaScript);
        return HaruStatus.OK;
    }

    public static Pdf3DView HPDF_Page_Create3DView(PdfPage page, PdfU3D u3d, string name)
    {
        var view = page.Owner.Create3DView(name);
        u3d.Add3DView(view);
        return view;
    }

    public static Pdf3DNode HPDF_3DView_CreateNode(Pdf3DView view, string name) => view.CreateNode(name);

    public static uint HPDF_3DView_AddNode(Pdf3DView view, Pdf3DNode node)
    {
        view.AddNode(node);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DViewNode_SetOpacity(Pdf3DNode node, double opacity)
    {
        node.SetOpacity(opacity);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DViewNode_SetVisibility(Pdf3DNode node, bool visible)
    {
        node.SetVisibility(visible);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DViewNode_SetMatrix(Pdf3DNode node, Pdf3DMatrix matrix)
    {
        node.SetMatrix(matrix);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetLighting(Pdf3DView view, string scheme)
    {
        view.SetLighting(scheme);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetBackgroundColor(Pdf3DView view, double r, double g, double b)
    {
        view.SetBackgroundColor(r, g, b);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetPerspectiveProjection(Pdf3DView view, double fieldOfView)
    {
        view.SetPerspectiveProjection(fieldOfView);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetOrthogonalProjection(Pdf3DView view, double magnification)
    {
        view.SetOrthogonalProjection(magnification);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetCamera(Pdf3DView view, double centerX, double centerY, double centerZ, double cameraDirectionX, double cameraDirectionY, double cameraDirectionZ, double orbitRadius, double roll)
    {
        view.SetCamera(centerX, centerY, centerZ, cameraDirectionX, cameraDirectionY, cameraDirectionZ, orbitRadius, roll);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetCameraByMatrix(Pdf3DView view, Pdf3DMatrix matrix, double cameraOrbit)
    {
        view.SetCameraByMatrix(matrix, cameraOrbit);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetCrossSectionOn(Pdf3DView view, PdfPoint3D center, double roll, double pitch, double opacity, bool showIntersection)
    {
        view.SetCrossSectionOn(center, roll, pitch, opacity, showIntersection);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_SetCrossSectionOff(Pdf3DView view)
    {
        view.SetCrossSectionOff();
        return HaruStatus.OK;
    }

    public static Pdf3DMeasure HPDF_Page_Create3DC3DMeasure(PdfPage page, PdfPoint3D firstAnchorPoint, PdfPoint3D textAnchorPoint) =>
        page.Owner.Create3DC3DMeasure(firstAnchorPoint, textAnchorPoint);

    public static Pdf3DMeasure HPDF_Page_CreatePD33DMeasure(PdfPage page, PdfPoint3D annotationPlaneNormal, PdfPoint3D firstAnchorPoint, PdfPoint3D secondAnchorPoint, PdfPoint3D leaderLinesDirection, PdfPoint3D measurementValuePoint, PdfPoint3D textYDirection, double value, string units) =>
        page.Owner.CreatePD33DMeasure(annotationPlaneNormal, firstAnchorPoint, secondAnchorPoint, leaderLinesDirection, measurementValuePoint, textYDirection, value, units);

    public static uint HPDF_3DMeasure_SetName(Pdf3DMeasure measure, string name)
    {
        measure.SetName(name);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DMeasure_SetColor(Pdf3DMeasure measure, PdfRgbColor color)
    {
        measure.SetColor(color);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DMeasure_SetTextSize(Pdf3DMeasure measure, double textSize)
    {
        measure.SetTextSize(textSize);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DC3DMeasure_SetTextBoxSize(Pdf3DMeasure measure, int x, int y)
    {
        measure.SetTextBoxSize(x, y);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DC3DMeasure_SetText(Pdf3DMeasure measure, string text)
    {
        measure.SetText(text);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DC3DMeasure_SetProjectionAnotation(Pdf3DMeasure measure, PdfAnnotation projectionAnnotation)
    {
        measure.SetProjectionAnnotation(projectionAnnotation);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DView_Add3DC3DMeasure(Pdf3DView view, Pdf3DMeasure measure)
    {
        view.AddMeasure(measure);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetPassword(PdfDocument pdf, string ownerPassword, string? userPassword)
    {
        pdf.SetPassword(ownerPassword, userPassword);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetPermission(PdfDocument pdf, Permission permission)
    {
        pdf.SetPermission(permission);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetPermission(PdfDocument pdf, uint permission)
    {
        pdf.SetPermission(permission);
        return HaruStatus.OK;
    }

    public static uint HPDF_SetEncryptionMode(PdfDocument pdf, PdfEncryptMode mode, uint keyLength)
    {
        pdf.SetEncryptionMode(mode, keyLength);
        return HaruStatus.OK;
    }

    public static uint HPDF_UseJPEncodings(PdfDocument pdf)
    {
        pdf.UseJPEncodings();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseKREncodings(PdfDocument pdf)
    {
        pdf.UseKREncodings();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseCNSEncodings(PdfDocument pdf)
    {
        pdf.UseCNSEncodings();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseCNTEncodings(PdfDocument pdf)
    {
        pdf.UseCNTEncodings();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseUTFEncodings(PdfDocument pdf)
    {
        pdf.UseUTFEncodings();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseJPFonts(PdfDocument pdf)
    {
        pdf.UseJPFonts();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseKRFonts(PdfDocument pdf)
    {
        pdf.UseKRFonts();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseCNSFonts(PdfDocument pdf)
    {
        pdf.UseCNSFonts();
        return HaruStatus.OK;
    }

    public static uint HPDF_UseCNTFonts(PdfDocument pdf)
    {
        pdf.UseCNTFonts();
        return HaruStatus.OK;
    }

    public static PdfImage HPDF_LoadRawImageFromMem(
        PdfDocument pdf,
        byte[] data,
        uint width,
        uint height,
        PdfColorSpace colorSpace,
        uint bitsPerComponent = 8)
    {
        if (width > int.MaxValue || height > int.MaxValue || bitsPerComponent > int.MaxValue)
            throw pdf.CreateException(HaruStatus.InvalidImage, "Image dimensions are too large.");

        return pdf.LoadRawImageFromMem(data, (int)width, (int)height, colorSpace, (int)bitsPerComponent);
    }

    public static PdfImage HPDF_LoadRawImageFromFile(
        PdfDocument pdf,
        string fileName,
        uint width,
        uint height,
        PdfColorSpace colorSpace)
    {
        if (width > int.MaxValue || height > int.MaxValue)
            throw pdf.CreateException(HaruStatus.InvalidImage, "Image dimensions are too large.");

        return pdf.LoadRawImageFromFile(fileName, (int)width, (int)height, colorSpace);
    }

    public static PdfImage HPDF_Image_LoadRaw1BitImageFromMem(
        PdfDocument pdf,
        byte[] data,
        uint width,
        uint height,
        uint lineWidth,
        bool blackIs1,
        bool topIsFirst)
    {
        if (width > int.MaxValue || height > int.MaxValue || lineWidth > int.MaxValue)
            throw pdf.CreateException(HaruStatus.InvalidImage, "Image dimensions are too large.");

        return pdf.LoadRaw1BitImageFromMem(data, (int)width, (int)height, (int)lineWidth, blackIs1, topIsFirst);
    }

    public static PdfImage HPDF_LoadPngImageFromFile(PdfDocument pdf, string fileName) =>
        pdf.LoadPngImageFromFile(fileName);

    public static PdfImage HPDF_LoadPngImageFromFile2(PdfDocument pdf, string fileName) =>
        pdf.LoadPngImageFromFile2(fileName);

    public static PdfImage HPDF_LoadPngImageFromMem(PdfDocument pdf, byte[] data) =>
        pdf.LoadPngImageFromMem(data);

    public static PdfImage HPDF_LoadJpegImageFromFile(PdfDocument pdf, string fileName) =>
        pdf.LoadJpegImageFromFile(fileName);

    public static PdfImage HPDF_LoadJpegImageFromMem(PdfDocument pdf, byte[] data) =>
        pdf.LoadJpegImageFromMem(data);

    public static bool HPDF_Image_Validate(PdfImage? image) => image?.Validate() == true;

    public static PdfPoint HPDF_Image_GetSize(PdfImage image)
    {
        image.ValidateOrThrow();
        return image.Size;
    }

    public static uint HPDF_Image_GetWidth(PdfImage image)
    {
        image.ValidateOrThrow();
        return checked((uint)image.Width);
    }

    public static uint HPDF_Image_GetHeight(PdfImage image)
    {
        image.ValidateOrThrow();
        return checked((uint)image.Height);
    }

    public static uint HPDF_Image_GetBitsPerComponent(PdfImage image)
    {
        image.ValidateOrThrow();
        return checked((uint)image.BitsPerComponent);
    }

    public static string HPDF_Image_GetColorSpace(PdfImage image)
    {
        return image.GetColorSpaceName();
    }

    public static uint HPDF_Image_GetSize2(PdfImage image, out PdfPoint size)
    {
        image.ValidateOrThrow();
        size = image.Size;
        return HaruStatus.OK;
    }

    public static uint HPDF_Image_SetColorMask(PdfImage image, uint rMin, uint rMax, uint gMin, uint gMax, uint bMin, uint bMax)
    {
        image.SetColorMask(rMin, rMax, gMin, gMax, bMin, bMax);
        return HaruStatus.OK;
    }

    public static uint HPDF_Image_SetMaskImage(PdfImage image, PdfImage maskImage)
    {
        image.SetMaskImage(maskImage);
        return HaruStatus.OK;
    }

    public static uint HPDF_Image_AddSMask(PdfImage image, PdfImage softMask)
    {
        image.AddSMask(softMask);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetSize(PdfPage page, PdfPageSize size, PdfPageDirection direction)
    {
        page.SetSize(size, direction);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetBoundary(PdfPage page, PdfPageBoundary boundary, double left, double bottom, double right, double top)
    {
        page.SetBoundary(boundary, new PdfRect(left, bottom, right, top));
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetRotate(PdfPage page, ushort angle)
    {
        page.SetRotate(angle);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetZoom(PdfPage page, double zoom)
    {
        page.SetZoom(zoom);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetWidth(PdfPage page) => page.Width;

    public static double HPDF_Page_GetHeight(PdfPage page) => page.Height;

    public static uint HPDF_Page_SetWidth(PdfPage page, double width)
    {
        page.SetWidth(width);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetHeight(PdfPage page, double height)
    {
        page.SetHeight(height);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetSlideShow(PdfPage page, PdfTransitionStyle style, double displayTime, double transitionTime)
    {
        page.SetSlideShow(style, displayTime, transitionTime);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetFontAndSize(PdfPage page, PdfFont font, double size)
    {
        page.SetFontAndSize(font, size);
        return HaruStatus.OK;
    }

    public static PdfFont? HPDF_Page_GetCurrentFont(PdfPage page) => page.CurrentFont;

    public static double HPDF_Page_GetCurrentFontSize(PdfPage page) => page.CurrentFontSize;

    public static ushort HPDF_Page_GetGMode(PdfPage page) => (ushort)page.GraphicsMode;

    public static PdfTransMatrix HPDF_Page_GetTransMatrix(PdfPage page) => page.TransMatrix;

    public static double HPDF_Page_TextWidth(PdfPage page, string text) => page.TextWidth(text);

    public static int HPDF_Page_MeasureText(PdfPage page, string text, double width, bool wordWrap, out double realWidth) =>
        page.MeasureText(text, width, wordWrap, out realWidth);

    public static uint HPDF_Page_BeginText(PdfPage page)
    {
        page.BeginText();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_EndText(PdfPage page)
    {
        page.EndText();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_MoveTextPos(PdfPage page, double x, double y)
    {
        page.MoveTextPos(x, y);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_MoveTextPos2(PdfPage page, double x, double y)
    {
        page.MoveTextPos2(x, y);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_TextOut(PdfPage page, double x, double y, string text)
    {
        page.TextOut(x, y, text);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_TextRect(
        PdfPage page,
        double left,
        double top,
        double right,
        double bottom,
        string text,
        PdfTextAlignment align,
        out uint length)
    {
        return page.TextRect(left, top, right, bottom, text, align, out length);
    }

    public static uint HPDF_Page_ShowText(PdfPage page, string text)
    {
        page.ShowText(text);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ShowTextNextLine(PdfPage page, string text)
    {
        page.ShowTextNextLine(text);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ShowTextNextLineEx(PdfPage page, double wordSpace, double charSpace, string text)
    {
        page.ShowTextNextLineEx(wordSpace, charSpace, text);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_MoveToNextLine(PdfPage page)
    {
        page.MoveToNextLine();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetTextMatrix(PdfPage page, double a, double b, double c, double d, double x, double y)
    {
        page.SetTextMatrix(a, b, c, d, x, y);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetTextRenderingMode(PdfPage page, PdfTextRenderingMode mode)
    {
        page.SetTextRenderingMode(mode);
        return HaruStatus.OK;
    }

    public static PdfTextRenderingMode HPDF_Page_GetTextRenderingMode(PdfPage page) => page.TextRenderingMode;

    public static uint HPDF_Page_SetCharSpace(PdfPage page, double value)
    {
        page.SetCharSpace(value);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetCharSpace(PdfPage page) => page.CharSpace;

    public static uint HPDF_Page_SetWordSpace(PdfPage page, double value)
    {
        page.SetWordSpace(value);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetWordSpace(PdfPage page) => page.WordSpace;

    public static uint HPDF_Page_SetHorizontalScalling(PdfPage page, double value)
    {
        page.SetHorizontalScalling(value);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetHorizontalScalling(PdfPage page) => page.HorizontalScalling;

    public static uint HPDF_Page_SetTextLeading(PdfPage page, double value)
    {
        page.SetTextLeading(value);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetTextLeading(PdfPage page) => page.TextLeading;

    public static uint HPDF_Page_SetTextRise(PdfPage page, double value)
    {
        page.SetTextRise(value);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetTextRaise(PdfPage page, double value) => HPDF_Page_SetTextRise(page, value);

    public static double HPDF_Page_GetTextRise(PdfPage page) => page.TextRise;

    public static double HPDF_Page_GetTextRaise(PdfPage page) => page.TextRise;

    public static uint HPDF_Page_MoveTo(PdfPage page, double x, double y)
    {
        page.MoveTo(x, y);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_LineTo(PdfPage page, double x, double y)
    {
        page.LineTo(x, y);
        return HaruStatus.OK;
    }

    public static PdfPoint HPDF_Page_GetCurrentPos(PdfPage page) => page.CurrentPosition;

    public static uint HPDF_Page_GetCurrentPos2(PdfPage page, out PdfPoint pos)
    {
        pos = page.CurrentPosition;
        return HaruStatus.OK;
    }

    public static PdfPoint HPDF_Page_GetCurrentTextPos(PdfPage page) => page.CurrentTextPosition;

    public static uint HPDF_Page_GetCurrentTextPos2(PdfPage page, out PdfPoint pos)
    {
        pos = page.CurrentTextPosition;
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_CurveTo(PdfPage page, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        page.CurveTo(x1, y1, x2, y2, x3, y3);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_CurveTo2(PdfPage page, double x2, double y2, double x3, double y3)
    {
        page.CurveTo2(x2, y2, x3, y3);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_CurveTo3(PdfPage page, double x1, double y1, double x3, double y3)
    {
        page.CurveTo3(x1, y1, x3, y3);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Rectangle(PdfPage page, double x, double y, double width, double height)
    {
        page.Rectangle(x, y, width, height);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Arc(PdfPage page, double x, double y, double radius, double startAngle, double endAngle)
    {
        page.Arc(x, y, radius, startAngle, endAngle);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Circle(PdfPage page, double x, double y, double radius)
    {
        page.Circle(x, y, radius);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Ellipse(PdfPage page, double x, double y, double xRadius, double yRadius)
    {
        page.Ellipse(x, y, xRadius, yRadius);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Stroke(PdfPage page)
    {
        page.Stroke();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Fill(PdfPage page)
    {
        page.Fill();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Eofill(PdfPage page)
    {
        page.Eofill();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_FillStroke(PdfPage page)
    {
        page.FillStroke();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_EofillStroke(PdfPage page)
    {
        page.EofillStroke();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ClosePath(PdfPage page)
    {
        page.ClosePath();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ClosePathStroke(PdfPage page)
    {
        page.ClosePathStroke();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ClosePathFillStroke(PdfPage page)
    {
        page.ClosePathFillStroke();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ClosePathEofillStroke(PdfPage page)
    {
        page.ClosePathEofillStroke();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_EndPath(PdfPage page)
    {
        page.EndPath();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Clip(PdfPage page)
    {
        page.Clip();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Eoclip(PdfPage page)
    {
        page.Eoclip();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_GSave(PdfPage page)
    {
        page.GSave();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_GRestore(PdfPage page)
    {
        page.GRestore();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetLineWidth(PdfPage page, double width)
    {
        page.SetLineWidth(width);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetLineWidth(PdfPage page) => page.LineWidth;

    public static uint HPDF_Page_SetLineCap(PdfPage page, PdfLineCap cap)
    {
        page.SetLineCap(cap);
        return HaruStatus.OK;
    }

    public static PdfLineCap HPDF_Page_GetLineCap(PdfPage page) => page.LineCap;

    public static uint HPDF_Page_SetLineJoin(PdfPage page, PdfLineJoin join)
    {
        page.SetLineJoin(join);
        return HaruStatus.OK;
    }

    public static PdfLineJoin HPDF_Page_GetLineJoin(PdfPage page) => page.LineJoin;

    public static uint HPDF_Page_SetMiterLimit(PdfPage page, double limit)
    {
        page.SetMiterLimit(limit);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetMiterLimit(PdfPage page) => page.MiterLimit;

    public static uint HPDF_Page_SetDash(PdfPage page, IReadOnlyList<double>? pattern, uint count, double phase)
    {
        if (pattern is null)
        {
            if (count != 0)
                throw page.Owner.CreateException(HaruStatus.InvalidParameter, "Dash pattern cannot be null when count is non-zero.");

            page.SetDash(Array.Empty<double>(), phase);
            return HaruStatus.OK;
        }

        if (count > pattern.Count || count > int.MaxValue)
            throw page.Owner.CreateException(HaruStatus.InvalidParameter, "Dash pattern count is outside the supplied array.");

        page.SetDash(pattern.Take((int)count).ToArray(), phase);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetDash(PdfPage page, IReadOnlyList<double> pattern, double phase)
    {
        page.SetDash(pattern, phase);
        return HaruStatus.OK;
    }

    public static PdfDashMode HPDF_Page_GetDash(PdfPage page) => page.Dash;

    public static uint HPDF_Page_SetFlat(PdfPage page, double flatness)
    {
        page.SetFlat(flatness);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetFlat(PdfPage page) => page.Flatness;

    public static uint HPDF_Page_SetRGBFill(PdfPage page, double r, double g, double b)
    {
        page.SetRGBFill(r, g, b);
        return HaruStatus.OK;
    }

    public static PdfRgbColor HPDF_Page_GetRGBFill(PdfPage page) => page.RgbFill;

    public static uint HPDF_Page_SetRGBStroke(PdfPage page, double r, double g, double b)
    {
        page.SetRGBStroke(r, g, b);
        return HaruStatus.OK;
    }

    public static PdfRgbColor HPDF_Page_GetRGBStroke(PdfPage page) => page.RgbStroke;

    public static uint HPDF_Page_SetGrayFill(PdfPage page, double gray)
    {
        page.SetGrayFill(gray);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetGrayFill(PdfPage page) => page.GrayFill;

    public static uint HPDF_Page_SetGrayStroke(PdfPage page, double gray)
    {
        page.SetGrayStroke(gray);
        return HaruStatus.OK;
    }

    public static double HPDF_Page_GetGrayStroke(PdfPage page) => page.GrayStroke;

    public static uint HPDF_Page_SetCMYKFill(PdfPage page, double c, double m, double y, double k)
    {
        page.SetCMYKFill(c, m, y, k);
        return HaruStatus.OK;
    }

    public static PdfCmykColor HPDF_Page_GetCMYKFill(PdfPage page) => page.CmykFill;

    public static uint HPDF_Page_SetCMYKStroke(PdfPage page, double c, double m, double y, double k)
    {
        page.SetCMYKStroke(c, m, y, k);
        return HaruStatus.OK;
    }

    public static PdfCmykColor HPDF_Page_GetCMYKStroke(PdfPage page) => page.CmykStroke;

    public static PdfColorSpace HPDF_Page_GetStrokingColorSpace(PdfPage page) => page.StrokingColorSpace;

    public static PdfColorSpace HPDF_Page_GetFillingColorSpace(PdfPage page) => page.FillingColorSpace;

    public static PdfTransMatrix HPDF_Page_GetTextMatrix(PdfPage page) => page.TextMatrix;

    public static uint HPDF_Page_GetGStateDepth(PdfPage page) => page.GStateDepth;

    public static uint HPDF_Page_Concat(PdfPage page, double a, double b, double c, double d, double x, double y)
    {
        page.Concat(a, b, c, d, x, y);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_DrawImage(PdfPage page, PdfImage image, double x, double y, double width, double height)
    {
        page.DrawImage(image, x, y, width, height);
        return HaruStatus.OK;
    }

    public static PdfXObject HPDF_Page_CreateXObjectFromImage(PdfDocument pdf, PdfPage page, PdfRect rect, PdfImage image, bool zoom) =>
        pdf.CreateXObjectFromImage(page, rect, image, zoom);

    public static PdfXObject HPDF_Page_CreateXObjectAsWhiteRect(PdfDocument pdf, PdfPage page, PdfRect rect) =>
        pdf.CreateXObjectAsWhiteRect(page, rect);

    public static uint HPDF_Page_ExecuteXObject(PdfPage page, PdfImage image)
    {
        page.ExecuteXObject(image);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_ExecuteXObject(PdfPage page, PdfXObject xObject)
    {
        page.ExecuteXObject(xObject);
        return HaruStatus.OK;
    }

    public static string HPDF_Page_GetXObjectName(PdfPage page, PdfImage image) => page.GetXObjectName(image);

    public static string HPDF_Page_GetXObjectName(PdfPage page, PdfXObject xObject) => page.GetXObjectName(xObject);

    public static PdfContentStream HPDF_Page_New_Content_Stream(PdfPage page) => page.NewContentStream();

    public static uint HPDF_Page_New_Content_Stream(PdfPage page, out PdfContentStream newStream)
    {
        newStream = page.NewContentStream();
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_Insert_Shared_Content_Stream(PdfPage page, PdfContentStream sharedStream)
    {
        page.InsertSharedContentStream(sharedStream);
        return HaruStatus.OK;
    }

    public static PdfDestination HPDF_Page_CreateDestination(PdfPage page) => page.CreateDestination();

    public static uint HPDF_Destination_SetXYZ(PdfDestination destination, double left, double top, double zoom)
    {
        destination.SetXYZ(left, top, zoom);
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFit(PdfDestination destination)
    {
        destination.SetFit();
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFitH(PdfDestination destination, double top)
    {
        destination.SetFitH(top);
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFitV(PdfDestination destination, double left)
    {
        destination.SetFitV(left);
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFitR(PdfDestination destination, double left, double bottom, double right, double top)
    {
        destination.SetFitR(left, bottom, right, top);
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFitB(PdfDestination destination)
    {
        destination.SetFitB();
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFitBH(PdfDestination destination, double top)
    {
        destination.SetFitBH(top);
        return HaruStatus.OK;
    }

    public static uint HPDF_Destination_SetFitBV(PdfDestination destination, double left)
    {
        destination.SetFitBV(left);
        return HaruStatus.OK;
    }

    public static PdfAnnotation HPDF_Page_CreateLinkAnnot(PdfPage page, PdfRect rect, PdfDestination destination) =>
        page.CreateLinkAnnotation(rect, destination);

    public static PdfAnnotation HPDF_Page_CreateURILinkAnnot(PdfPage page, PdfRect rect, string uri) =>
        page.CreateURILinkAnnotation(rect, uri);

    public static PdfAnnotation HPDF_Page_CreateTextAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateTextAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateFreeTextAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateFreeTextAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateLineAnnot(PdfPage page, PdfRect rect, string text, object? encoder, PdfPoint startPoint, PdfPoint endPoint) =>
        page.CreateLineAnnotation(rect, text, startPoint, endPoint);

    public static PdfAnnotation HPDF_Page_CreateWidgetAnnot(PdfPage page, PdfRect rect) =>
        page.CreateWidgetAnnotation(rect);

    public static PdfAnnotation HPDF_Page_CreateWidgetAnnot_WhiteOnlyWhilePrint(PdfDocument pdf, PdfPage page, PdfRect rect) =>
        page.CreateWidgetAnnotationWhiteOnlyWhilePrint(rect);

    public static PdfAnnotation HPDF_Page_CreateSquareAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateSquareAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateCircleAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateCircleAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateHighlightAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateHighlightAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateTextMarkupAnnot(PdfPage page, PdfRect rect, string text, object? encoder, PdfAnnotType subType) =>
        page.CreateTextMarkupAnnotation(rect, text, subType);

    public static PdfAnnotation HPDF_Page_CreateUnderlineAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateUnderlineAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateSquigglyAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateSquigglyAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreateStrikeOutAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateStrikeOutAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_CreatePopupAnnot(PdfPage page, PdfRect rect, PdfAnnotation parent) =>
        page.CreatePopupAnnotation(rect, parent);

    public static PdfAnnotation HPDF_Page_CreateStampAnnot(PdfPage page, PdfRect rect, string name, string text, object? encoder = null) =>
        page.CreateStampAnnotation(rect, name, text);

    public static PdfAnnotation HPDF_Page_CreateProjectionAnnot(PdfPage page, PdfRect rect, string text, object? encoder = null) =>
        page.CreateProjectionAnnotation(rect, text);

    public static PdfAnnotation HPDF_Page_Create3DAnnot(PdfPage page, PdfRect rect, PdfU3D u3d) =>
        page.Create3DAnnotation(rect, u3d);

    public static PdfExData HPDF_Page_Create3DAnnotExData(PdfPage page) => page.Create3DAnnotExData();

    public static uint HPDF_Annotation_SetBorderStyle(PdfAnnotation annotation, PdfAnnotBorderStyle style, double width, ushort dashOn = 0, ushort dashOff = 0, ushort dashPhase = 0)
    {
        annotation.SetBorderStyle(style, width, dashOn, dashOff, dashPhase);
        return HaruStatus.OK;
    }

    public static uint HPDF_LinkAnnot_SetBorderStyle(PdfAnnotation annotation, double width, ushort dashOn, ushort dashOff)
    {
        annotation.SetBorderStyle(dashOn == 0 && dashOff == 0 ? PdfAnnotBorderStyle.Solid : PdfAnnotBorderStyle.Dashed, width, dashOn, dashOff);
        return HaruStatus.OK;
    }

    public static uint HPDF_LinkAnnot_SetHighlightMode(PdfAnnotation annotation, PdfAnnotHighlightMode mode)
    {
        annotation.SetHighlightMode(mode);
        return HaruStatus.OK;
    }

    public static uint HPDF_LinkAnnot_SetJavaScript(PdfAnnotation annotation, PdfJavaScript javaScript)
    {
        annotation.SetJavaScript(javaScript);
        return HaruStatus.OK;
    }

    public static uint HPDF_TextAnnot_SetIcon(PdfAnnotation annotation, PdfAnnotIcon icon)
    {
        annotation.SetIcon(icon);
        return HaruStatus.OK;
    }

    public static uint HPDF_TextAnnot_SetOpened(PdfAnnotation annotation, bool opened)
    {
        annotation.SetOpened(opened);
        return HaruStatus.OK;
    }

    public static uint HPDF_PopupAnnot_SetOpened(PdfAnnotation annotation, bool opened)
    {
        annotation.SetOpened(opened);
        return HaruStatus.OK;
    }

    public static uint HPDF_Annot_SetRGBColor(PdfAnnotation annotation, PdfRgbColor color)
    {
        annotation.SetRGBColor(color.R, color.G, color.B);
        return HaruStatus.OK;
    }

    public static uint HPDF_Annot_SetCMYKColor(PdfAnnotation annotation, PdfCmykColor color)
    {
        annotation.SetCMYKColor(color.C, color.M, color.Y, color.K);
        return HaruStatus.OK;
    }

    public static uint HPDF_Annot_SetGrayColor(PdfAnnotation annotation, double color)
    {
        annotation.SetGrayColor(color);
        return HaruStatus.OK;
    }

    public static uint HPDF_Annot_SetNoColor(PdfAnnotation annotation)
    {
        annotation.SetNoColor();
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetTitle(PdfAnnotation annotation, string name)
    {
        annotation.SetTitle(name);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetSubject(PdfAnnotation annotation, string name)
    {
        annotation.SetSubject(name);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetCreationDate(PdfAnnotation annotation, DateTimeOffset value)
    {
        annotation.SetCreationDate(value);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetTransparency(PdfAnnotation annotation, double value)
    {
        annotation.SetTransparency(value);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetIntent(PdfAnnotation annotation, PdfAnnotIntent intent)
    {
        annotation.SetIntent(intent);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetPopup(PdfAnnotation annotation, PdfAnnotation popup)
    {
        annotation.SetPopup(popup);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetRectDiff(PdfAnnotation annotation, PdfRect rect)
    {
        annotation.SetRectDiff(rect);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetCloudEffect(PdfAnnotation annotation, int cloudIntensity)
    {
        annotation.SetCloudEffect(cloudIntensity);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetInteriorRGBColor(PdfAnnotation annotation, PdfRgbColor color)
    {
        annotation.SetInteriorRGBColor(color);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetInteriorCMYKColor(PdfAnnotation annotation, PdfCmykColor color)
    {
        annotation.SetInteriorCMYKColor(color);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetInteriorGrayColor(PdfAnnotation annotation, double color)
    {
        annotation.SetInteriorGrayColor(color);
        return HaruStatus.OK;
    }

    public static uint HPDF_MarkupAnnot_SetInteriorTransparent(PdfAnnotation annotation)
    {
        annotation.SetInteriorTransparent();
        return HaruStatus.OK;
    }

    public static uint HPDF_TextMarkupAnnot_SetQuadPoints(PdfAnnotation annotation, PdfPoint leftBottom, PdfPoint rightBottom, PdfPoint rightTop, PdfPoint leftTop)
    {
        annotation.SetQuadPoints(leftBottom, rightBottom, rightTop, leftTop);
        return HaruStatus.OK;
    }

    public static uint HPDF_FreeTextAnnot_SetLineEndingStyle(PdfAnnotation annotation, PdfAnnotLineEndingStyle startStyle, PdfAnnotLineEndingStyle endStyle)
    {
        annotation.SetLineEndingStyle(startStyle, endStyle);
        return HaruStatus.OK;
    }

    public static uint HPDF_FreeTextAnnot_Set2PointCalloutLine(PdfAnnotation annotation, PdfPoint startPoint, PdfPoint endPoint)
    {
        annotation.SetCalloutLine(startPoint, endPoint);
        return HaruStatus.OK;
    }

    public static uint HPDF_FreeTextAnnot_Set3PointCalloutLine(PdfAnnotation annotation, PdfPoint startPoint, PdfPoint kneePoint, PdfPoint endPoint)
    {
        annotation.SetCalloutLine(startPoint, kneePoint, endPoint);
        return HaruStatus.OK;
    }

    public static uint HPDF_FreeTextAnnot_SetDefaultStyle(PdfAnnotation annotation, string style)
    {
        annotation.SetDefaultStyle(style);
        return HaruStatus.OK;
    }

    public static uint HPDF_LineAnnot_SetPosition(PdfAnnotation annotation, PdfPoint startPoint, PdfAnnotLineEndingStyle startStyle, PdfPoint endPoint, PdfAnnotLineEndingStyle endStyle)
    {
        annotation.SetLinePosition(startPoint, startStyle, endPoint, endStyle);
        return HaruStatus.OK;
    }

    public static uint HPDF_LineAnnot_SetLeader(PdfAnnotation annotation, int leaderLength, int leaderExtensionLength, int leaderOffsetLength)
    {
        annotation.SetLineLeader(leaderLength, leaderExtensionLength, leaderOffsetLength);
        return HaruStatus.OK;
    }

    public static uint HPDF_LineAnnot_SetCaption(PdfAnnotation annotation, bool showCaption, PdfLineAnnotCapPosition position, int horizontalOffset, int verticalOffset)
    {
        annotation.SetLineCaption(showCaption, position, horizontalOffset, verticalOffset);
        return HaruStatus.OK;
    }

    public static uint HPDF_Annot_Set3DView(PdfAnnotation annotation, Pdf3DView view)
    {
        annotation.Set3DView(view);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DAnnotExData_Set3DMeasurement(PdfAnnotation annotation, Pdf3DMeasure measure)
    {
        annotation.Set3DMeasure(measure);
        return HaruStatus.OK;
    }

    public static uint HPDF_3DAnnotExData_Set3DMeasurement(PdfExData exData, Pdf3DMeasure measure)
    {
        exData.Set3DMeasurement(measure);
        return HaruStatus.OK;
    }

    public static uint HPDF_ProjectionAnnot_SetExData(PdfAnnotation annotation, PdfExData exData)
    {
        annotation.SetExData(exData);
        return HaruStatus.OK;
    }

    public static uint HPDF_Annot_SetAppearance(
        PdfAnnotation annotation,
        PdfAnnotationAppearanceState state,
        string contentStream,
        PdfRect boundingBox,
        string? appearanceName = null,
        IReadOnlyDictionary<string, PdfXObject>? xObjects = null,
        IReadOnlyDictionary<string, PdfFont>? fonts = null)
    {
        annotation.SetAppearance(state, contentStream, boundingBox, appearanceName, xObjects, fonts);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetExtGState(PdfPage page, PdfExtGState extGState)
    {
        page.SetExtGState(extGState);
        return HaruStatus.OK;
    }

    public static uint HPDF_Page_SetShading(PdfPage page, PdfShading shading)
    {
        page.SetShading(shading);
        return HaruStatus.OK;
    }
}
