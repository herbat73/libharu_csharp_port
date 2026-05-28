using LibHaru.Internal;

namespace LibHaru;

public sealed class PdfOutline
{
    private readonly PdfDictionary _dictionary;

    internal PdfOutline(PdfDocument owner, PdfOutline? parent, string title, PdfIndirectObject outlineObject)
    {
        Owner = owner;
        Parent = parent;
        Title = title;
        OutlineObject = outlineObject;
        _dictionary = (PdfDictionary)outlineObject.Value;
        _dictionary.Set("Title", PdfString.FromText(title));
    }

    internal PdfDocument Owner { get; }

    internal PdfOutline? Parent { get; }

    internal PdfIndirectObject OutlineObject { get; }

    internal List<PdfOutline> Children { get; } = [];

    public string Title { get; }

    public bool Opened { get; private set; }

    public void SetDestination(PdfDestination destination)
    {
        ValidateOrThrow();

        if (destination is null || !ReferenceEquals(destination.Owner, Owner))
            throw Owner.CreateException(HaruStatus.InvalidDestination,
                "Outline destination does not belong to this document.");

        destination.ValidateOrThrow();
        _dictionary.Set("Dest", destination.DestinationObject.Reference);
    }

    public void SetOpened(bool opened)
    {
        ValidateOrThrow();
        Opened = opened;
    }

    internal int Prepare(PdfIndirectReference parentReference, PdfOutline? previous, PdfOutline? next)
    {
        ValidateOrThrow();
        _dictionary.Set("Parent", parentReference);
        _dictionary.SetName("Type", "Outline");

        if (previous is not null)
            _dictionary.Set("Prev", previous.OutlineObject.Reference);
        else
            _dictionary.Remove("Prev");

        if (next is not null)
            _dictionary.Set("Next", next.OutlineObject.Reference);
        else
            _dictionary.Remove("Next");

        if (Children.Count == 0)
        {
            _dictionary.Remove("First");
            _dictionary.Remove("Last");
            _dictionary.Remove("Count");
            return 1;
        }

        _dictionary.Set("First", Children[0].OutlineObject.Reference);
        _dictionary.Set("Last", Children[^1].OutlineObject.Reference);

        var descendantCount = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            var prev = i == 0 ? null : Children[i - 1];
            var nextChild = i == Children.Count - 1 ? null : Children[i + 1];
            descendantCount += Children[i].Prepare(OutlineObject.Reference, prev, nextChild);
        }

        _dictionary.Set("Count", new PdfInteger(Opened ? descendantCount : -descendantCount));
        return descendantCount + 1;
    }

    private void ValidateOrThrow()
    {
        if (OutlineObject.Value is not PdfDictionary dictionary)
            throw Owner.CreateException(HaruStatus.InvalidOutline, "Outline object must be a dictionary.");

        if (!dictionary.MatchesClass(PdfObjectClass.Dictionary | PdfObjectClass.Outline))
            throw Owner.CreateException(HaruStatus.InvalidOutline, "Outline object must be an outline dictionary.");
    }
}