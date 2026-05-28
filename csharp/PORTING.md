# libharu C# Port

This tree is a managed source migration of libharu for .NET 9. It is not a P/Invoke binding and does not load `libhpdf.dll`.

## Build

From the repository root:

```powershell
dotnet build LibHaruSharp.sln
dotnet run --project csharp\LibHaru.SmokeTests\LibHaru.SmokeTests.csproj
```

From this `csharp` directory:

```powershell
dotnet build ..\LibHaruSharp.sln
dotnet run --project LibHaru.SmokeTests\LibHaru.SmokeTests.csproj
```

The smoke test writes `artifacts/libharu-managed-smoke.pdf`.
Compatibility demo ports write generated PDFs and `.structure.txt` regression profiles under `artifacts/compatibility-demos`.
When Poppler `pdftoppm` is available, selected first-page renders are checked against `csharp/LibHaru.SmokeTests/fixtures/visual-reference.tsv` and write pixel profiles under `artifacts/rendered`. Set `LIBHARU_PDFTOPPM` to a `pdftoppm` executable or containing directory when it is not on `PATH`; set `LIBHARU_REFRESH_VISUAL_REFERENCES=1` to refresh the TSV from current renders.
Exact upstream/reference PDF comparisons are manifest-driven through `csharp/LibHaru.SmokeTests/fixtures/reference-output.tsv`; rows can assert byte-for-byte generated matches, generated SHA-256 hashes, or checked-in upstream C-output SHA-256 hashes.

## Current Managed Slice

Status: the checked-in C headers, source modules, bundled fixtures, canonical PDF demos, reference-output hashes, and Poppler visual profiles have managed coverage. Remaining work is limited to future fixture drops that are not present in the repository yet.

Verification on 2026-05-28:

- `dotnet build ..\LibHaruSharp.sln` succeeded with 0 warnings and 0 errors.
- `dotnet run --project LibHaru.SmokeTests\LibHaru.SmokeTests.csproj` generated the managed smoke PDFs, 3 real-font fixture PDFs, 28 compatibility demo PDFs, and passed 39 generated-PDF structural reader checks.
- The smoke harness checks 20 checked-in upstream C-output PDF SHA-256 fixture rows. No byte-identical managed/upstream exact-PDF rows are configured yet because the current managed outputs differ in metadata/object serialization from the canonical demo PDFs.
- Optional Poppler visual render checks passed with portable Poppler `pdftoppm` 26.02.0 via `LIBHARU_PDFTOPPM`; 10 first-page visual reference profiles were refreshed.
- The local `include/hpdf.h` exported `HPDF_*` function names were audited against the managed `HPdf` facade; all exported function names are represented.

- `hpdf_objects.c`, `hpdf_array.c`, `hpdf_dict.c`, `hpdf_name.c`, `hpdf_number.c`, `hpdf_real.c`, `hpdf_string.c`: migrated in `Internal/PdfObjects.cs`, including direct/proxy ownership semantics, exact typed array/dictionary base/subclass lookup validation for all migrated object wrappers, managed stream-vs-dictionary type guards, direct raw objects, and encrypted string/binary writing.
- `hpdf_streams.c`, `hpdf_xref.c`: migrated through `Internal/PdfWriter.cs` and the writer in `Public/PdfDocument.cs`, with generated-PDF reader checks covering xref/trailer/reference behavior.
- `hpdf_doc.c`: migrated for `New`/`NewEx`, current document manager helpers (`NewDoc`, `FreeDoc`, `HasDoc`, `FreeDocAll`, document/page memory-manager accessors), page tree configuration, `AddPage`, `InsertPage`, `GetPageByIndex`, `GetFont`, info attribute getters/setters including date attributes, stream/file saving, stream chunk reads, and layout/mode/viewer metadata.
- `hpdf_pages.c`, `hpdf_page_operator.c`: migrated for page sizing, boundary/rotate/zoom metadata, text operators including text-rectangle helpers, path painting including even-odd aliases, clipping aliases, color, line style, graphics-state save/restore, ellipse/arc/circle helpers, shared content-stream arrays, and page graphics-state getters.
- `hpdf_fontdef_base14.c`, `hpdf_font.c`: migrated for built-in Base14 Type1 fonts through the shared managed font-program and single-byte encoder path, with generated upstream width tables/descriptors and FontSpecific defaults for Symbol/ZapfDingbats.
- `hpdf_fontdef_type1.c`, `hpdf_font_type1.c`: migrated for Type1 AFM parsing, PFB/PFA font program loading, font descriptors, widths arrays, and `/FontFile` embedding.
- `hpdf_fontdef_tt.c`, `hpdf_font_tt.c`: migrated for TrueType `head`, `hhea`, `maxp`, `hmtx`, `cmap` formats 0/4/6/10/12/13, `name`, `OS/2`, optional `post`, TTC face selection, embedding-permission checks, lower-level TT font-def handles, simple-font widths, descriptors, and `/FontFile2` embedding. Embedded TrueType font programs emit deterministic dense glyph subsets from actual simple/composite text use, including composite-glyph component closure/remapping, rebuilt subset `cmap`/`hmtx`/`loca` tables, explicit CIDToGIDMap streams, Type0 `/DW2` vertical metrics from the font bbox, and save-time ToUnicode CMaps for Identity and predefined CMap Type0 paths. Simple OpenType/CFF `OTTO` fonts share the SFNT metric parser and embed as `/FontFile3` `/Subtype /OpenType`; checked-in real-font fixture coverage now includes Aguafina Script, Akronim, and Alfa Slab One TTFs with manifest validation.
- `hpdf_encoder.c`, `hpdf_encoder_utf.c`, `hpdf_encoder_jp.c`, `hpdf_encoder_kr.c`, `hpdf_encoder_cns.c`, `hpdf_encoder_cnt.c`: migrated for Standard, WinAnsi/CP1252, FontSpecific, ISO8859 aliases, CP125x aliases, MacRoman, KOI8-R, UTF/Identity, and predefined Japanese/Korean/Simplified Chinese/Traditional Chinese CMap entry points. Generated upstream single-byte Unicode maps, CJK Unicode maps, CMap CID ranges, exact lead/trail byte classifiers, JP EUC-H/EUC-V entries, public encoder handles/introspection, and save-time ToUnicode CMaps are present.
- `hpdf_encrypt.c`, `hpdf_encryptdict.c`: migrated for Standard Security revision 2/3 password handling, permissions, encryption dictionary preparation, file IDs, encryption dictionary validation, user/owner password authentication, and per-object RC4 encryption/decryption for strings, binary values, and streams. Upstream libharu `v2.4.6`/HEAD was audited on 2026-05-28 for later security revisions, AES, and crypt-filter support; no newer security-handler surface was present.
- `hpdf_error.c`, `hpdf_error.h`: migrated for the full status-code table, document error state, detail codes, reset/check behavior, callback dispatch, and document-owned error paths across the migrated modules and resource validators.
- `hpdf_streams.c`, stream filter portions of `hpdf_dict.c`: migrated for libharu-style `/Filter` arrays and array-shaped `/DecodeParms`, with Flate encoding for page content, raw image streams, metadata streams, embedded files, JavaScript, ICC streams, generated form XObjects, DCT filters for JPEG streams, and CCITT Group 4 image streams.
- `hpdf_image.c`, `hpdf_image_png.c`, `hpdf_image_ccitt.c`: migrated for JPEG memory/file streams, PNG memory/file loading, raw memory/file images, 1-bit raw images with CCITT Group 4 encoding, image masks, soft masks, color-key masks, DeviceGray/RGB/CMYK raw color spaces, Indexed PNG color spaces, PNG CRC/order validation, PNG gAMA/cHRM/sRGB/iCCP color metadata mapping, non-color PNG ancillary chunk validation, delayed file-backed PNG data loading for `LoadPngImageFromFile2`, image validators, page image XObject resources, form XObject construction helpers from images/white rectangles, and an optional external image fixture harness for future checked-in JPEG/PNG/raw/1-bit CCITT samples.
- `hpdf_catalog.c`, `hpdf_destination.c`, `hpdf_outline.c`, `hpdf_page_label.c`: migrated for destinations, open actions, outlines, page labels, viewer preferences, slide-show page transitions, and catalog names dictionaries.
- `hpdf_annotation.c`: migrated for link, URI link, text, free text, line, square/circle, text-markup, popup, stamp, projection, widget, and basic 3D annotations, including border, highlight, icon, color, opened, contents, title, JavaScript action, markup/callout/interior-color variants, generic text-markup, widget appearance, projection ExData setters, specialized widget white-while-print appearances, and named normal/rollover/down appearance streams with appearance-local Font/XObject resources.
- `hpdf_namedict.c`, `hpdf_pdfa.c`: migrated for embedded file streams/filespecs, embedded-file name trees, catalog associated files, PDF/A conformance metadata, output intent dictionaries with ICC profile streams, ICC memory/file loading aliases, typed validators, save-time PDF/A output-intent/XMP/conformance guardrails, PDF/A embedded-file relationship defaults/restrictions, and large multi-leaf name-tree fixtures.
- `hpdf_u3d.c`, `hpdf_ext_gstate.c`, `hpdf_shading.c`: migrated for U3D streams, 3D views with perspective/orthographic projection, matrix cameras, cross-section toggles, nodes, C3D/PD3 measures, ExData, extended graphics states, typed validators, and axial/radial/free-form triangle mesh shadings.
- Upstream libharu `v2.4.6`/HEAD was audited on 2026-05-28 for annotation and 3D API/fixture additions beyond the local headers; no additional annotation or 3D variants were present.
- `demo/arc_demo.c`, `demo/attach.c`, `demo/character_map.c`, `demo/chfont_demo.c`, `demo/encoding_list.c`, `demo/encryption.c`, `demo/ext_gstate_demo.c`, `demo/font_demo.c`, `demo/grid_sheet.c`, `demo/image_demo.c`, `demo/jpeg_demo.c`, `demo/jpfont_demo.c`, `demo/line_demo.c`, `demo/link_annotation.c`, `demo/outline_demo.c`, `demo/outline_demo_jp.c`, `demo/pdf_a_conformance.c`, `demo/permission.c`, `demo/png_demo.c`, `demo/raw_image_demo.c`, `demo/slide_show_demo.c`, `demo/text_annotation.c`, `demo/text_demo.c`, `demo/text_demo2.c`, `demo/ttfont_demo.c`, and `demo/ttfont_demo_jp.c`: migrated as managed compatibility demo ports that run from the smoke app and generate structural PDF profiles for regression coverage. `demo/make_rawimage.c` is tracked in the generated inventory as a utility fixture generator rather than a PDF demo.
- `bindings/c#`: intentionally not used as an implementation source. The managed `HPdf` facade has callable coverage for the exported function names in the local `include/hpdf.h` audit, with low-level handles represented by managed equivalents where appropriate.

## Fixture-Gated Follow-up Work

The local source migration is complete for the checked-in headers and C modules. The open items below require new fixture assets that are not currently checked in.

- [ ] Add CID-keyed CFF Type0 font fixture coverage when suitable OpenType/CFF CID-keyed fonts are added under `csharp/LibHaru.SmokeTests/fixtures/fonts`.
- [ ] Add real external JPEG/PNG/raw/1-bit CCITT image fixture assets and manifest rows under `csharp/LibHaru.SmokeTests/fixtures/images`; the optional harness is present, but only placeholder examples are currently checked in.

## Source Module Map

| C module group | Managed destination | Current migration status |
| --- | --- | --- |
| Core objects, dictionary, array, primitive values | `csharp/LibHaru/Internal` | Complete for checked-in source, including object ownership, stream/dictionary guards, typed subclass validation, and encrypted string/binary output. |
| Streams, xref, trailer writer | `csharp/LibHaru/Internal`, `PdfDocument` | Complete for checked-in source; generated-PDF reader checks cover xref offsets, trailers, references, stream filters, and `startxref`. |
| Document, catalog, pages, page operators | `csharp/LibHaru/Public` | Complete for local `include/hpdf.h` exports, including document lifecycle, page tree operations, metadata, viewer state, page graphics state, text, paths, colors, shared streams, and compatibility demos. |
| Error, memory manager, list utilities | `HaruError`, `HaruStatus`, managed collections | Complete for managed parity: document-owned error state/callbacks and typed validators replace C memory/list ownership patterns. |
| Base14 fonts | `PdfFont`, `Base14Fonts`, `PdfFontProgram`, generated Base14 data | Complete with generated upstream widths/descriptors and FontSpecific handling for Symbol/ZapfDingbats. |
| Type1 fonts | `Type1FontLoader`, `PdfFontProgram`, `PdfDocument` | Complete for AFM parsing, PFB/PFA loading, descriptors, widths arrays, and `/FontFile` embedding. |
| TrueType/OpenType fonts | `TrueTypeFontLoader`, `PdfFontProgram`, `PdfDocument` | Complete for TrueType simple/Type0 use, TTC selection, embedding permissions, dense subsetting, composite glyph remapping, ToUnicode, Type0 vertical metrics, simple OpenType/CFF `/FontFile3`, and checked-in real-font fixtures. CID-keyed CFF Type0 remains fixture-gated. |
| CID fonts | `PdfDocument`, `PdfFontProgram`, `PredefinedCidFonts`, generated CJK metrics | Complete for predefined CJK CID names/metrics, Type0 TrueType CID maps, vertical metrics, and Identity/predefined-CMap ToUnicode output. |
| Encoders and CMaps | `PdfEncoding`, generated single-byte maps, generated CJK CMap data, generated ToUnicode streams | Complete for public encoder handles/introspection, single-byte aliases/maps, UTF/Identity, CJK byte classifiers, JP EUC-H/EUC-V, predefined CJK CMaps, and save-time ToUnicode maps. |
| Images and color spaces | `PdfDocument`, `PdfImage`, `PngImageLoader`, `CcittFaxEncoder`, `PdfPage` | Complete for JPEG, PNG, raw, CCITT Group 4, masks, Indexed PNG, PNG metadata/validation, delayed PNG loading, image/form XObject resources, and optional external-fixture harness. Real external image fixture assets remain fixture-gated. |
| Stream filters and compression | `PdfStreamObject`, `PdfStreamFilter` | Complete for Flate, DCT, CCITT, ASCIIHex, ASCII85, filter arrays, and DecodeParms dictionary/array output. |
| Encryption | `PdfEncryption`, `PdfDocument`, `PdfWriter` | Complete for checked-in revision 2/3 Standard Security, including passwords, permissions, file IDs, dictionary validation, authentication, and object encryption/decryption; audited upstream exposes no R4/R5/R6, AES, or crypt-filter surface. |
| PDF/A, metadata, output intents | `PdfDocument`, `PdfIccProfile`, `PdfOutputIntent` | Complete for metadata/output-intent generation, ICC loading aliases, PDF/A conformance metadata, embedded-file restrictions/defaults, and save-time guardrails. |
| Outlines, destinations, annotations | `PdfDestination`, `PdfOutline`, `PdfAnnotation`, `PdfPage`, `PdfDocument` | Complete for local headers and audited upstream: destinations, outlines, page labels, link/URI/text/free-text/shape/markup/popup/stamp/projection/widget/basic 3D annotations, appearances, and validation. |
| Names, attachments, JavaScript, PDF/A associated files | `PdfEmbeddedFile`, `PdfJavaScript`, `PdfOutputIntent`, `PdfDocument` | Complete for embedded-file streams/filespecs, name trees, catalog associated files, JavaScript actions, PDF/A restrictions, output intents, and typed resource validators. |
| U3D, 3D measure, shadings, ext graphics state | `PdfU3D`, `Pdf3DView`, `PdfShading`, `PdfExtGState`, `PdfPage` | Complete for U3D streams, 3D views/nodes/cameras/cross-sections, C3D/PD3 measures, ExData, axial/radial/free-form triangle mesh shadings, extended graphics state, and typed 3D validators. |
| Compatibility demos and regression fixtures | `csharp/LibHaru.SmokeTests` | Complete for current checked-in demos: 28 managed compatibility demo PDFs, structural profiles, 20 upstream C-output SHA-256 rows, 10 Poppler visual profiles, and 3 real-font fixture PDFs. |
