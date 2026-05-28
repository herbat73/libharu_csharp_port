# libharu C# Port

This tree is a managed source migration of libharu for .NET 9. It is not a P/Invoke binding and does not load `libhpdf.dll`.

## Build

```powershell
dotnet build LibHaruSharp.sln
dotnet run --project csharp\LibHaru.SmokeTests\LibHaru.SmokeTests.csproj
```

The smoke test writes `artifacts/libharu-managed-smoke.pdf`.
Compatibility demo ports write generated PDFs and `.structure.txt` regression profiles under `artifacts/compatibility-demos`.
When Poppler `pdftoppm` is available, selected first-page renders are checked against `csharp/LibHaru.SmokeTests/fixtures/visual-reference.tsv` and write pixel profiles under `artifacts/rendered`.
Exact upstream/reference PDF comparisons are manifest-driven through `csharp/LibHaru.SmokeTests/fixtures/reference-output.tsv`; rows become byte-for-byte or SHA-256 checks as soon as stable upstream fixtures are added.

## Current Managed Slice

- `hpdf_objects.c`, `hpdf_array.c`, `hpdf_dict.c`, `hpdf_name.c`, `hpdf_number.c`, `hpdf_real.c`, `hpdf_string.c`: represented by `Internal/PdfObjects.cs`, including direct/proxy ownership semantics, exact typed array/dictionary base/subclass lookup validation, managed stream-vs-dictionary type guards, direct raw objects, and encrypted string/binary writing.
- `hpdf_streams.c`, `hpdf_xref.c`: represented by `Internal/PdfWriter.cs` and the writer in `Public/PdfDocument.cs`.
- `hpdf_doc.c`: `New`/`NewEx`, current document manager helpers (`NewDoc`, `FreeDoc`, `HasDoc`, `FreeDocAll`, document/page memory-manager accessors), page tree configuration, `AddPage`, `InsertPage`, `GetPageByIndex`, `GetFont`, info attribute getters/setters including date attributes, stream/file saving, stream chunk reads, and layout/mode/viewer metadata are implemented.
- `hpdf_pages.c`, `hpdf_page_operator.c`: page sizing, boundary/rotate/zoom metadata, text operators including text-rectangle helpers, path painting including even-odd aliases, clipping aliases, color, line style, graphics-state save/restore, ellipse/arc/circle helpers, shared content-stream arrays, and page graphics-state getters are implemented.
- `hpdf_fontdef_base14.c`, `hpdf_font.c`: built-in Base14 Type1 font objects now flow through the shared managed font-program and single-byte encoder path with generated upstream width tables/descriptors and FontSpecific defaults for Symbol/ZapfDingbats.
- `hpdf_fontdef_type1.c`, `hpdf_font_type1.c`: Type1 AFM parsing, PFB/PFA font program loading, font descriptors, widths arrays, and `/FontFile` embedding are started.
- `hpdf_fontdef_tt.c`, `hpdf_font_tt.c`: TrueType table parsing is started for `head`, `hhea`, `maxp`, `hmtx`, `cmap` formats 0/4/6/10/12/13, `name`, `OS/2`, optional `post`, TTC face selection, embedding-permission checks, lower-level TT font-def handles, simple-font widths, descriptors, and `/FontFile2` embedding. Embedded TrueType font programs now emit deterministic dense glyph subsets from actual simple/composite text use, including composite-glyph component closure/remapping, rebuilt subset `cmap`/`hmtx`/`loca` tables, explicit CIDToGIDMap streams, and save-time ToUnicode CMaps for Identity and predefined CMap Type0 paths.
- `hpdf_encoder.c`, `hpdf_encoder_utf.c`, `hpdf_encoder_jp.c`, `hpdf_encoder_kr.c`, `hpdf_encoder_cns.c`, `hpdf_encoder_cnt.c`: single-byte encoder infrastructure is started for Standard, WinAnsi/CP1252, FontSpecific, ISO8859 aliases, CP125x aliases, MacRoman, and KOI8-R names, with generated upstream Unicode maps plus public encoder handles and introspection for type, byte type, Unicode lookup, and writing mode. UTF/Identity encoding now has a managed Type0/CID TrueType path with generated ToUnicode CMaps; Japanese/Korean/Simplified Chinese/Traditional Chinese CMap entry point names are registered for predefined CID flows. The upstream CJK Unicode maps, CMap CID ranges, exact lead/trail byte classifiers, and JP EUC-H/EUC-V entries are generated into the managed port for predefined CMap introspection.
- `hpdf_encrypt.c`, `hpdf_encryptdict.c`: Standard Security revision 2/3 password handling, permissions, encryption dictionary preparation, file IDs, encryption dictionary validation, user/owner password authentication, and per-object RC4 encryption/decryption for strings, binary values, and streams are implemented.
- `hpdf_error.c`, `hpdf_error.h`: full status-code table, document error state, detail codes, reset/check behavior, and callback dispatch are implemented.
- `hpdf_streams.c`, stream filter portions of `hpdf_dict.c`: stream filter flags now write `/Filter` arrays and array-shaped `/DecodeParms`, with Flate encoding for page content, raw image streams, and metadata streams; JPEG streams use `DCTDecode`.
- `hpdf_image.c`, `hpdf_image_png.c`, `hpdf_image_ccitt.c`: JPEG memory/file streams, PNG memory/file loading, raw memory/file images, 1-bit raw images with CCITT Group 4 encoding, image masks, soft masks, color-key masks, Indexed PNG color spaces, PNG CRC/order validation, PNG gAMA/cHRM/sRGB/iCCP color metadata mapping, image validators, page image XObject resources, and form XObject construction helpers from images/white rectangles are started.
- `hpdf_catalog.c`, `hpdf_destination.c`, `hpdf_outline.c`, `hpdf_page_label.c`: destinations, open actions, outlines, page labels, viewer preferences, and catalog names dictionaries are started.
- `hpdf_annotation.c`: link, URI link, text, free text, square/circle, text-markup, popup, stamp, projection, widget, and basic 3D annotations are started with border, highlight, icon, color, opened, contents, title, JavaScript action, generic text-markup, widget appearance, and projection ExData setters. Specialized widget white-while-print appearances now mirror the C helper's AP/MK/FT/F/T dictionary shape, and generic AP streams preserve named normal/rollover/down appearance states with appearance-local Font/XObject resources.
- `hpdf_namedict.c`, `hpdf_pdfa.c`: embedded file streams/filespecs, embedded-file name trees, catalog associated files, PDF/A conformance metadata, output intent dictionaries with ICC profile streams, ICC memory/file loading aliases, save-time PDF/A output-intent/XMP/conformance guardrails, PDF/A embedded-file relationship defaults/restrictions, and large multi-leaf name-tree fixtures are started.
- `hpdf_u3d.c`, `hpdf_ext_gstate.c`, `hpdf_shading.c`: U3D streams, 3D views with perspective/orthographic projection, matrix cameras, cross-section toggles, nodes, C3D/PD3 measures, extended graphics states, and axial/radial/free-form triangle mesh shadings are started.
- `demo/arc_demo.c`, `demo/attach.c`, `demo/character_map.c`, `demo/chfont_demo.c`, `demo/encoding_list.c`, `demo/encryption.c`, `demo/ext_gstate_demo.c`, `demo/font_demo.c`, `demo/grid_sheet.c`, `demo/image_demo.c`, `demo/jpeg_demo.c`, `demo/jpfont_demo.c`, `demo/line_demo.c`, `demo/link_annotation.c`, `demo/outline_demo.c`, `demo/outline_demo_jp.c`, `demo/pdf_a_conformance.c`, `demo/permission.c`, `demo/png_demo.c`, `demo/raw_image_demo.c`, `demo/slide_show_demo.c`, `demo/text_annotation.c`, `demo/text_demo.c`, `demo/text_demo2.c`, `demo/ttfont_demo.c`, and `demo/ttfont_demo_jp.c`: managed compatibility demo ports now run from the smoke app and generate structural PDF profiles for regression coverage. `demo/make_rawimage.c` is tracked in the generated inventory as a utility fixture generator rather than a PDF demo.
- `bindings/c#`: intentionally not used as an implementation source. The managed `HPdf` facade mirrors common `HPDF_*` entry points for migration convenience.

## Remaining Migration Work

- [ ] Add exact upstream C-output PDF or SHA-256 fixture rows to `csharp/LibHaru.SmokeTests/fixtures/reference-output.tsv` when stable upstream outputs are checked in.
- [ ] Enable and refresh optional Poppler visual regression profiles when `pdftoppm` is available on the verification machine.
- [ ] Add OpenType/CFF font coverage, deeper vertical TrueType metrics, and wider real-font compatibility fixtures when those assets are checked in.
- [ ] Broaden image compatibility coverage when new external JPEG/PNG/raw/CCITT fixtures are added.
- [ ] Add document-owned error paths and typed validators for any newly migrated C modules or object subclasses.
- [ ] Add further annotation and 3D variants only if upstream introduces APIs or fixtures outside the current local headers.
- [ ] Revisit security-handler support only if a newer upstream source exposes later security revisions; the checked-in C source only exposes revision 2/3 Standard Security.

## Source Module Map

| C module group | Managed destination | Current migration status |
| --- | --- | --- |
| Core objects, dictionary, array, primitive values | `csharp/LibHaru/Internal` | Migrated for the local object model; add validators only for new object subclasses. |
| Streams, xref, trailer writer | `csharp/LibHaru/Internal`, `PdfDocument` | Migrated and covered by generated-PDF reader checks. |
| Document/catalog/pages/page operators | `csharp/LibHaru/Public` | Migrated for the local `include/hpdf.h` audit and compatibility demos. |
| Error, memory manager, list utilities | `HaruError`, `HaruStatus`, managed collections | Error parity migrated; memory/list behavior is represented by managed ownership and collections. |
| Base14 fonts | `PdfFont`, `Base14Fonts`, `PdfFontProgram`, generated Base14 data | Migrated with generated upstream widths and descriptors. |
| Type1 fonts | `Type1FontLoader`, `PdfFontProgram`, `PdfDocument` | Migrated for AFM parsing, PFB/PFA loading, descriptors, widths, and embedding. |
| TrueType fonts | `TrueTypeFontLoader`, `PdfFontProgram`, `PdfDocument` | Migrated for simple/Type0 usage, TTC selection, embedding checks, dense subsetting, composite remapping, and ToUnicode output; OpenType/CFF and deeper vertical metrics remain future fixture-driven work. |
| CID fonts | `PdfDocument`, `PdfFontProgram`, `PredefinedCidFonts`, generated CJK metrics | Migrated for predefined CJK CID names/metrics, Type0 TrueType CID maps, and Identity/predefined-CMap ToUnicode output. |
| Encoders and CMaps | `PdfEncoding`, generated single-byte maps, generated CJK CMap data, generated ToUnicode streams | Migrated for public encoder handles/introspection, single-byte maps, UTF/Identity, CJK byte classifiers, JP EUC-H/EUC-V, predefined CJK CMaps, and save-time ToUnicode maps. |
| Images and color spaces | `PdfDocument`, `PdfImage`, `PngImageLoader`, `CcittFaxEncoder`, `PdfPage` | Migrated for JPEG, PNG, raw images, CCITT Group 4, masks, Indexed PNG color spaces, PNG metadata/validation, delayed file-backed PNG data, and image/form XObject resources. |
| Stream filters and compression | `PdfStreamObject`, `PdfStreamFilter` | Migrated for Flate, DCT, CCITT, ASCIIHex, ASCII85, and DecodeParms dictionary output. |
| Encryption | `PdfEncryption`, `PdfDocument`, `PdfWriter` | Migrated for revision 2/3 Standard Security validation, authentication, dictionary generation, and object encryption/decryption. |
| PDF/A, metadata, output intents | `PdfDocument`, `PdfIccProfile`, `PdfOutputIntent` | Migrated for metadata/output-intent generation, ICC loading aliases, PDF/A conformance metadata, and save-time guardrails. |
| Outlines, destinations, annotations | `PdfDestination`, `PdfOutline`, `PdfAnnotation`, `PdfPage`, `PdfDocument` | Migrated for destinations, outlines, page labels, link/URI/text/free-text/shape/markup/popup/stamp/projection/widget/basic 3D annotations, appearance streams, and annotation validation covered by smoke tests. |
| Names, attachments, JavaScript, PDF/A, output intents | `PdfEmbeddedFile`, `PdfJavaScript`, `PdfOutputIntent`, `PdfDocument` | Migrated for embedded-file streams/filespecs, name trees, catalog associated files, JavaScript actions, PDF/A restrictions, and output-intent resources. |
| U3D, 3D measure, shadings, ext graphics state | `PdfU3D`, `Pdf3DView`, `PdfShading`, `PdfExtGState`, `PdfPage` | Migrated for U3D streams, 3D views/nodes/camera/cross-sections, C3D/PD3 measures, ExData, axial/radial/free-form triangle mesh shadings, and extended graphics state. |
