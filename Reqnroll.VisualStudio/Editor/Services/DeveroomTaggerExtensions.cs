namespace Reqnroll.VisualStudio.Editor.Services;

public static class DeveroomTaggerExtensions
{
    public static IEnumerable<ITagSpan<DeveroomTag>> GetUpToDateDeveroomTagsForSpan(
        this ITagger<DeveroomTag> tagger, SnapshotSpan span) =>
        ((FeatureFileTagger) tagger).GetUpToDateTags(span);
}
