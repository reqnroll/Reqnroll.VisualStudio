namespace Reqnroll.VisualStudio.VsxStubs;

public class StubViewScroller : IViewScroller
{
    public void ScrollViewportVerticallyByPixels(double distanceToScroll)
    {
    }

    public void ScrollViewportVerticallyByLine(ScrollDirection direction)
    {
    }

    public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count)
    {
    }

    public bool ScrollViewportVerticallyByPage(ScrollDirection direction) => false;

    public void ScrollViewportHorizontallyByPixels(double distanceToScroll)
    {
    }

    public void EnsureSpanVisible(SnapshotSpan span)
    {
    }

    public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options)
    {
    }

    public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options)
    {
    }
}
