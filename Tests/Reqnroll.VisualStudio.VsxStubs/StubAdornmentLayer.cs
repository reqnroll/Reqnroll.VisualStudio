using System.Collections.ObjectModel;

namespace Reqnroll.VisualStudio.VsxStubs;

public class StubAdornmentLayer : IAdornmentLayer
{
    public bool AddAdornment(AdornmentPositioningBehavior behavior, SnapshotSpan? visualSpan, object tag,
        UIElement adornment,
        AdornmentRemovedCallback removedCallback) =>
        throw new NotImplementedException();

    public bool AddAdornment(SnapshotSpan visualSpan, object tag, UIElement adornment) =>
        throw new NotImplementedException();

    public void RemoveAdornment(UIElement adornment)
    {
        throw new NotImplementedException();
    }

    public void RemoveAdornmentsByTag(object tag)
    {
        throw new NotImplementedException();
    }

    public void RemoveAdornmentsByVisualSpan(SnapshotSpan visualSpan)
    {
        throw new NotImplementedException();
    }

    public void RemoveMatchingAdornments(Predicate<IAdornmentLayerElement> match)
    {
        throw new NotImplementedException();
    }

    public void RemoveMatchingAdornments(SnapshotSpan visualSpan, Predicate<IAdornmentLayerElement> match)
    {
        throw new NotImplementedException();
    }

    public void RemoveAllAdornments()
    {
        throw new NotImplementedException();
    }

    public IWpfTextView TextView => throw new NotImplementedException();

    public bool IsEmpty => throw new NotImplementedException();

    public double Opacity { get; set; } = 0;

    public ReadOnlyCollection<IAdornmentLayerElement> Elements => throw new NotImplementedException();
}
