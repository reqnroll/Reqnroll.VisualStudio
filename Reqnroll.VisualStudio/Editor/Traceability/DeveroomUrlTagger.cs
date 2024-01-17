#nullable disable

namespace Reqnroll.VisualStudio.Editor.Traceability;

internal class DeveroomUrlTagger : DeveroomTagConsumer, ITagger<UrlTag>
{
    private readonly IIdeScope _ideScope;
    private readonly IProjectScope _projectScope;

    public DeveroomUrlTagger(ITextBuffer buffer, ITagAggregator<DeveroomTag> tagAggregator, IIdeScope ideScope)
        : base(buffer, tagAggregator)
    {
        _ideScope = ideScope;
        _projectScope = ideScope.GetProject(buffer);
    }

    public IEnumerable<ITagSpan<UrlTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        return GetDeveroomTags(spans, t => t.Type == DeveroomTagTypes.Tag)
            .Select(tagSpan => new {tagSpan.Key, Url = GetUrl((Tag) tagSpan.Value.Data)})
            .Where(urlSpan => urlSpan.Url != null)
            .Select(urlSpan => new TagSpan<UrlTag>(urlSpan.Key,
                new UrlTag(urlSpan.Url)));
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    private Uri GetUrl(Tag tag)
    {
        if (_projectScope == null)
            return null; // TODO: support global config?

        var configuration = _projectScope.GetDeveroomConfiguration();
        if (!configuration.Traceability.TagLinks.Any())
            return null;

        foreach (var tagLinkConfiguration in configuration.Traceability.TagLinks)
        {
            if (tagLinkConfiguration.ResolvedTagPattern == null)
                continue;
            var match = tagLinkConfiguration.ResolvedTagPattern.Match(tag.Name.TrimStart('@'));
            if (!match.Success)
                continue;

            try
            {
                var url = Regex.Replace(tagLinkConfiguration.UrlTemplate, @"\{(?<paramName>[a-zA-Z_\d]+)\}",
                    paramMatch => match.Groups[paramMatch.Groups["paramName"].Value].Value);
                return new Uri(url);
            }
            catch (Exception ex)
            {
                _ideScope.Logger.LogDebugException(ex);
            }
        }

        return null;
    }

    protected override void RaiseChanged(SnapshotSpan span)
    {
        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
    }
}
