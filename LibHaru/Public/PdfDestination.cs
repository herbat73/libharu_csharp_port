using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfDestination
{
    private bool _initialized;

    internal PdfDestination(PdfDocument owner, PdfPage targetPage, PdfIndirectObject destinationObject)
    {
        Owner = owner;
        TargetPage = targetPage;
        DestinationObject = destinationObject;
        SetDestination("Fit");
        _initialized = true;
    }

    internal PdfDocument Owner { get; }

    internal PdfPage TargetPage { get; }

    internal PdfIndirectObject DestinationObject { get; }

    public void SetXYZ(double left, double top, double zoom)
    {
        ValidateOrThrow();

        if (left < 0 || top < 0 || zoom is < 0.08 or > 32 || !IsFinite(left) || !IsFinite(top) || !IsFinite(zoom))
            throw Owner.CreateException(HaruStatus.InvalidParameter, "Destination XYZ values are out of range.");

        SetDestination("XYZ", new PdfReal(left), new PdfReal(top), new PdfReal(zoom));
    }

    public void SetFit()
    {
        if (_initialized)
            ValidateOrThrow();

        SetDestination("Fit");
        _initialized = true;
    }

    public void SetFitH(double top)
    {
        ValidateOrThrow();
        ValidateFinite(top);
        SetDestination("FitH", new PdfReal(top));
    }

    public void SetFitV(double left)
    {
        ValidateOrThrow();
        ValidateFinite(left);
        SetDestination("FitV", new PdfReal(left));
    }

    public void SetFitR(double left, double bottom, double right, double top)
    {
        ValidateOrThrow();
        ValidateFinite(left);
        ValidateFinite(bottom);
        ValidateFinite(right);
        ValidateFinite(top);
        SetDestination("FitR", new PdfReal(left), new PdfReal(bottom), new PdfReal(right), new PdfReal(top));
    }

    public void SetFitB()
    {
        ValidateOrThrow();
        SetDestination("FitB");
    }

    public void SetFitBH(double top)
    {
        ValidateOrThrow();
        ValidateFinite(top);
        SetDestination("FitBH", new PdfReal(top));
    }

    public void SetFitBV(double left)
    {
        ValidateOrThrow();
        ValidateFinite(left);
        SetDestination("FitBV", new PdfReal(left));
    }

    internal void ValidateOrThrow()
    {
        if (DestinationObject.Value is not PdfArray destination)
            throw Owner.CreateException(HaruStatus.InvalidDestination, "Destination object must be an array.");

        if (!destination.MatchesClass(PdfObjectClass.Array | PdfObjectClass.Destination))
            throw Owner.CreateException(HaruStatus.InvalidDestination, "Destination object must be a destination array.");

        try
        {
            destination.GetItem(0, PdfObjectClass.Dictionary | PdfObjectClass.Page);
        }
        catch (HaruException ex)
        {
            throw Owner.CreateException(HaruStatus.InvalidDestination, "Destination target page is invalid.", ex.Status);
        }
    }

    private void SetDestination(string name, params PdfObject[] args)
    {
        TargetPage.ValidateOrThrow();
        var items = new List<PdfObject> { TargetPage.PageObject.Reference, new PdfName(name) };
        items.AddRange(args);
        DestinationObject.Value = new PdfArray(items) { Subclass = PdfObjectClass.Destination };
    }

    private void ValidateFinite(double value)
    {
        if (!IsFinite(value))
            throw Owner.CreateException(HaruStatus.RealOutOfRange, "Destination values must be finite.");
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
