using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfImage
{
    private bool _imageMask;

    internal PdfImage(
        PdfDocument owner,
        string resourceName,
        PdfIndirectObject imageObject,
        int width,
        int height,
        int bitsPerComponent,
        PdfColorSpace colorSpace,
        string colorSpaceName)
    {
        Owner = owner;
        ResourceName = resourceName;
        ImageObject = imageObject;
        Width = width;
        Height = height;
        BitsPerComponent = bitsPerComponent;
        ColorSpace = colorSpace;
        ColorSpaceName = colorSpaceName;
    }

    internal PdfDocument Owner { get; }

    internal string ResourceName { get; }

    internal PdfIndirectObject ImageObject { get; }

    public int Width { get; }

    public int Height { get; }

    public int BitsPerComponent { get; }

    public PdfColorSpace ColorSpace { get; }

    public string ColorSpaceName { get; }

    public PdfPoint Size => new(Width, Height);

    private PdfStreamObject Stream =>
        ImageObject.Value as PdfStreamObject
        ?? throw Owner.CreateException(HaruStatus.InvalidImage, "Image object must be a stream.");

    public bool Validate()
    {
        try
        {
            ValidateOrThrow();
            return true;
        }
        catch (HaruException)
        {
            return false;
        }
    }

    public void SetColorMask(uint rMin, uint rMax, uint gMin, uint gMax, uint bMin, uint bMax)
    {
        ValidateOrThrow();

        if (_imageMask)
            throw Owner.CreateException(HaruStatus.InvalidOperation, "Image masks cannot have color-key masks.");

        if (BitsPerComponent != 8)
            throw Owner.CreateException(HaruStatus.InvalidBitPerComponent,
                "Color-key masks require 8 bits per component.");

        if (GetColorSpaceName() != "DeviceRGB")
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "Color-key masks require DeviceRGB images.");

        if (rMax > 255 || gMax > 255 || bMax > 255 || rMin > rMax || gMin > gMax || bMin > bMax)
            throw Owner.CreateException(HaruStatus.InvalidParameter,
                "Color-key mask values must be within 0..255 ranges.");

        Stream.Dictionary.Set("Mask", new PdfArray([
            new PdfInteger((int)rMin),
            new PdfInteger((int)rMax),
            new PdfInteger((int)gMin),
            new PdfInteger((int)gMax),
            new PdfInteger((int)bMin),
            new PdfInteger((int)bMax)
        ]));
    }

    public void SetMaskImage(PdfImage maskImage)
    {
        ValidatePeer(maskImage);
        ValidateOrThrow();
        maskImage.ValidateOrThrow();
        maskImage.SetImageMask(true);
        Stream.Dictionary.Set("Mask", maskImage.ImageObject.Reference);
    }

    public void AddSMask(PdfImage softMask)
    {
        ValidatePeer(softMask);
        ValidateOrThrow();
        softMask.ValidateOrThrow();

        if (softMask.ColorSpace != PdfColorSpace.DeviceGray)
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "Soft masks require DeviceGray images.");

        Stream.Dictionary.Set("SMask", softMask.ImageObject.Reference);
    }

    internal void SetImageMask(bool value)
    {
        ValidateOrThrow();

        if (value && BitsPerComponent != 1)
            throw Owner.CreateException(HaruStatus.InvalidBitPerComponent, "Image masks require 1 bit per component.");

        _imageMask = value;
        Stream.Dictionary.Set("ImageMask", new PdfBoolean(value));
    }

    internal void ValidateOrThrow()
    {
        if (ImageObject.Value is not PdfStreamObject stream)
            throw Owner.CreateException(HaruStatus.InvalidImage, "Image object must be a stream.");

        if (!stream.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.XObject))
            throw Owner.CreateException(HaruStatus.InvalidImage, "Image object must be an XObject.");

        try
        {
            var type = stream.Dictionary.Get<PdfName>("Type");
            var subtype = stream.Dictionary.Get<PdfName>("Subtype");

            if (type?.Value != "XObject" || subtype?.Value != "Image")
                throw Owner.CreateException(HaruStatus.InvalidImage,
                    "Image dictionary Type/Subtype entries are invalid.");
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidImage)
        {
            throw Owner.CreateException(HaruStatus.InvalidImage, "Image dictionary Type/Subtype entries are invalid.",
                ex.Status);
        }
    }

    internal string GetColorSpaceName()
    {
        ValidateOrThrow();

        try
        {
            var colorSpace = Stream.Dictionary.GetItem("ColorSpace", PdfObjectClass.Any);
            return colorSpace switch
            {
                PdfName name => name.Value,
                PdfArray array => array.GetItem<PdfName>(0).Value,
                _ => throw Owner.CreateException(HaruStatus.InvalidColorSpace, "Image ColorSpace entry is invalid.")
            };
        }
        catch (HaruException ex) when (ex.Status != HaruStatus.InvalidColorSpace)
        {
            throw Owner.CreateException(HaruStatus.InvalidColorSpace, "Image ColorSpace entry is invalid.", ex.Status);
        }
    }

    private void ValidatePeer(PdfImage? image)
    {
        if (image is null || !ReferenceEquals(image.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidImage, "Image does not belong to this document.");
    }
}