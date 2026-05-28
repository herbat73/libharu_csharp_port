using System.Text;
using LibHaru;
using LibHaru.Internal;

public static class ObjectSemantics
{
    public static void Test()
    {
        DirectObjectReuseIsRejected();
        SameDictionaryValueReplacementIsIdempotent();
        IndirectObjectsAreStoredAsProxyReferences();
        CollectionLookupsValidateObjectClasses();
        CollectionLookupsRequireExactSubclasses();
        ModuleValidatorsRejectSubclassMismatches();
        EncryptedStringsWriteAsEncryptedHex();

        Console.WriteLine("Object semantics smoke passed");
    }

    private static void DirectObjectReuseIsRejected()
    {
        var error = new HaruError();
        var value = new PdfInteger(42);
        var first = new PdfArray();
        var second = new PdfArray();
        first.AttachError(error);
        second.AttachError(error);

        first.Add(value);

        var ex = RequireThrows(() => second.Add(value));
        Require(ex.Status == HaruStatus.InvalidObject, "Direct object reuse raised the wrong status.");
        Require(error.ErrorNo == HaruStatus.InvalidObject, "Direct object reuse did not set the Haru error.");
    }

    private static void SameDictionaryValueReplacementIsIdempotent()
    {
        var error = new HaruError();
        var dictionary = new PdfDictionary();
        var child = new PdfDictionary();
        dictionary.AttachError(error);

        dictionary.Set("Child", child);
        dictionary.Set("Child", child);
        Require(ReferenceEquals(dictionary.Get<PdfDictionary>("Child"), child), "Re-setting the same dictionary value changed the child.");

        var stream = new PdfStreamObject([1, 2, 3])
        {
            Filter = PdfStreamFilter.CcittDecode
        };
        var decodeParms = new PdfDictionary();
        decodeParms.Set("Columns", new PdfInteger(8));
        stream.SetDecodeParms(decodeParms);
        stream.AttachError(error);

        WriteObjectValue(stream);
        WriteObjectValue(stream);
    }

    private static void IndirectObjectsAreStoredAsProxyReferences()
    {
        using var pdf = PdfDocument.New();
        var targetDictionary = new PdfDictionary();
        targetDictionary.SetName("Type", "Example");
        var target = pdf.AddObject(targetDictionary);

        var first = new PdfDictionary();
        var second = new PdfDictionary();
        first.Set("Target", target.Value);
        second.Set("Target", target.Value);

        var firstObject = pdf.AddObject(first);
        pdf.AddObject(second);

        Require(ReferenceEquals(first.Get<PdfDictionary>("Target"), target.Value), "Dictionary proxy lookup did not unwrap the target object.");

        var bytes = pdf.SaveToStream();
        var latin1 = Encoding.Latin1.GetString(bytes);
        Require(latin1.Contains($"{firstObject.ObjectNumber} 0 obj", StringComparison.Ordinal), "Fixture object was not written.");
        Require(latin1.Contains($"/Target {target.ObjectNumber} 0 R", StringComparison.Ordinal), "Indirect dictionary value was not written as a reference.");
    }

    private static void CollectionLookupsValidateObjectClasses()
    {
        var error = new HaruError();

        var array = new PdfArray();
        array.AttachError(error);
        array.Add(new PdfName("Example"));

        var arrayTypeError = RequireThrows(() => array.GetItem<PdfString>(0));
        Require(arrayTypeError.Status == HaruStatus.ArrayItemUnexpectedType, "Array type validation raised the wrong status.");

        var missingError = RequireThrows(() => array.GetItem<PdfName>(1));
        Require(missingError.Status == HaruStatus.ArrayItemNotFound, "Array missing-item validation raised the wrong status.");

        var dict = new PdfDictionary();
        dict.AttachError(error);
        dict.Set("Answer", new PdfInteger(42));

        var dictTypeError = RequireThrows(() => dict.Get<PdfString>("Answer"));
        Require(dictTypeError.Status == HaruStatus.DictItemUnexpectedType, "Dictionary type validation raised the wrong status.");
        Require(dict.Get<PdfString>("Missing") is null, "Missing dictionary lookup should return null.");

        var streamArray = new PdfArray();
        streamArray.AttachError(error);
        streamArray.Add(new PdfDictionary());
        var streamArrayError = RequireThrows(() => streamArray.GetItem<PdfStreamObject>(0));
        Require(streamArrayError.Status == HaruStatus.ArrayItemUnexpectedType, "Array managed-type validation raised the wrong status.");

        var streamDict = new PdfDictionary();
        streamDict.AttachError(error);
        streamDict.Set("Plain", new PdfDictionary());
        var streamDictError = RequireThrows(() => streamDict.Get<PdfStreamObject>("Plain"));
        Require(streamDictError.Status == HaruStatus.DictItemUnexpectedType, "Dictionary managed-type validation raised the wrong status.");
    }

    private static void CollectionLookupsRequireExactSubclasses()
    {
        var error = new HaruError();

        var pages = new PdfDictionary { Subclass = PdfObjectClass.Pages };
        pages.SetName("Type", "Pages");
        var array = new PdfArray();
        array.AttachError(error);
        array.Add(pages);

        var arraySubtypeError = RequireThrows(() => array.GetItem(0, PdfObjectClass.Dictionary | PdfObjectClass.Font));
        Require(arraySubtypeError.Status == HaruStatus.ArrayItemUnexpectedType, "Array subclass validation raised the wrong status.");

        var font = new PdfDictionary { Subclass = PdfObjectClass.Font };
        font.SetName("Type", "Font");
        var dict = new PdfDictionary();
        dict.AttachError(error);
        dict.Set("Font", font);
        Require(ReferenceEquals(dict.GetItem("Font", PdfObjectClass.Dictionary | PdfObjectClass.Font), font), "Dictionary subclass lookup did not return the font object.");

        var catalog = new PdfDictionary { Subclass = PdfObjectClass.Catalog };
        catalog.SetName("Type", "Catalog");
        var wrongDict = new PdfDictionary();
        wrongDict.AttachError(error);
        wrongDict.Set("Catalog", catalog);

        var dictSubtypeError = RequireThrows(() => wrongDict.GetItem("Catalog", PdfObjectClass.Dictionary | PdfObjectClass.Font));
        Require(dictSubtypeError.Status == HaruStatus.DictItemUnexpectedType, "Dictionary subclass validation raised the wrong status.");
    }

    private static void ModuleValidatorsRejectSubclassMismatches()
    {
        using (var pdf = PdfDocument.New())
        {
            var page = pdf.AddPage();
            var font = pdf.GetFont("Helvetica");
            ((PdfDictionary)font.FontObject.Value).Subclass = PdfObjectClass.Pages;

            var ex = RequireThrows(() => page.SetFontAndSize(font, 12));
            Require(ex.Status == HaruStatus.PageInvalidFont, "Font subclass validation raised the wrong status.");
        }

        using (var pdf = PdfDocument.New())
        {
            var page = pdf.AddPage();
            var destination = page.CreateDestination();
            ((PdfArray)destination.DestinationObject.Value).Subclass = PdfObjectClass.Outline;

            var ex = RequireThrows(() => pdf.SetOpenAction(destination));
            Require(ex.Status == HaruStatus.InvalidDestination, "Destination subclass validation raised the wrong status.");
        }

        using (var pdf = PdfDocument.New())
        {
            var image = pdf.LoadRawImageFromMem([0, 0, 0], 1, 1, PdfColorSpace.DeviceRgb);
            ((PdfStreamObject)image.ImageObject.Value).Subclass = PdfObjectClass.Shading;

            var ex = RequireThrows(image.ValidateOrThrow);
            Require(ex.Status == HaruStatus.InvalidImage, "Image subclass validation raised the wrong status.");
        }

        using (var pdf = PdfDocument.New())
        {
            var page = pdf.AddPage();
            var annotation = page.CreateTextAnnotation(new PdfRect(0, 0, 10, 10), "note");
            ((PdfDictionary)annotation.AnnotationObject.Value).Subclass = PdfObjectClass.Page;

            var ex = RequireThrows(() => annotation.SetIcon(PdfAnnotIcon.Note));
            Require(ex.Status == HaruStatus.InvalidAnnotation, "Annotation subclass validation raised the wrong status.");
        }

        using (var pdf = PdfDocument.New())
        {
            var extGState = pdf.CreateExtGState();
            ((PdfDictionary)extGState.GraphicsStateObject.Value).Subclass = PdfObjectClass.ExtGStateReadOnly;

            var ex = RequireThrows(() => extGState.SetAlphaFill(0.5));
            Require(ex.Status == HaruStatus.ExtGStateReadOnly, "Read-only ExtGState validation raised the wrong status.");
        }

        using (var pdf = PdfDocument.New())
        {
            var shading = pdf.CreateShading(PdfShadingType.FreeFormTriangleMesh, PdfColorSpace.DeviceRgb, 0, 10, 0, 10);
            ((PdfStreamObject)shading.ShadingObject.Value).Subclass = PdfObjectClass.XObject;

            var ex = RequireThrows(() => shading.AddVertexRGB(PdfShadingFreeFormTriangleMeshEdgeFlag.NoConnection, 1, 1, 0, 0, 0));
            Require(ex.Status == HaruStatus.InvalidShadingType, "Shading subclass validation raised the wrong status.");
        }

        using (var pdf = PdfDocument.New())
        {
            var page = pdf.AddPage();
            ((PdfDictionary)page.PageObject.Value).Subclass = PdfObjectClass.Pages;

            var ex = RequireThrows(() => pdf.SaveToStream());
            Require(ex.Status == HaruStatus.InvalidPage, "Page subclass validation raised the wrong status.");
        }
    }

    private static void EncryptedStringsWriteAsEncryptedHex()
    {
        const string secret = "secret (needs escaping)";
        var error = new HaruError();
        var encryption = new PdfEncryption(error);
        encryption.SetPassword("owner", "user");
        encryption.Prepare(Enumerable.Range(0, 16).Select(static i => (byte)i).ToArray());

        using var plainStream = new MemoryStream();
        var plainWriter = new PdfWriter(plainStream) { Error = error };
        PdfString.FromText(secret).WriteTo(plainWriter);
        var plain = Encoding.Latin1.GetString(plainStream.ToArray());
        Require(plain.Contains("secret", StringComparison.Ordinal), "Plain string fixture did not write literal text.");
        Require(plain.Contains("\\(", StringComparison.Ordinal), "Plain string fixture did not escape parentheses.");

        using var encryptedStream = new MemoryStream();
        var encryptedWriter = new PdfWriter(encryptedStream)
        {
            Error = error,
            Encryption = encryption
        };
        encryptedWriter.BeginObject(9, 0);
        PdfString.FromText(secret).WriteTo(encryptedWriter);
        encryptedWriter.EndObject();

        var encrypted = Encoding.Latin1.GetString(encryptedStream.ToArray());
        Require(encrypted.StartsWith("<", StringComparison.Ordinal), "Encrypted string should be emitted as a hex string.");
        Require(encrypted.EndsWith(">", StringComparison.Ordinal), "Encrypted string should be emitted as a hex string.");
        Require(!encrypted.Contains(secret, StringComparison.Ordinal), "Encrypted string leaked plaintext.");
        Require(!encrypted.Contains("\\(", StringComparison.Ordinal), "Encrypted string was literal-escaped instead of encrypted.");
    }

    private static HaruException RequireThrows(Action action)
    {
        try
        {
            action();
        }
        catch (HaruException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected a HaruException.");
    }

    private static void WriteObjectValue(PdfObject value)
    {
        using var stream = new MemoryStream();
        var writer = new PdfWriter(stream) { Error = new HaruError() };
        writer.BeginObject(1, 0);
        value.WriteTo(writer);
        writer.EndObject();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
