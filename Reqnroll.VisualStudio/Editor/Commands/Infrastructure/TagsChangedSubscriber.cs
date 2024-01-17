namespace Reqnroll.VisualStudio.Editor.Commands.Infrastructure;

internal class TagsChangedSubscriber : IDisposable
{
    private readonly IDeveroomLogger _logger;
    private readonly AutoResetEvent _parsed = new(false);
    private readonly FeatureFileTagger _tagger;

    public TagsChangedSubscriber(FeatureFileTagger tagger, IDeveroomLogger logger)
    {
        _tagger = tagger;
        _logger = logger;
        _tagger.TagsChanged += OnTagsChanged;
    }

    public void Dispose()
    {
        _tagger.TagsChanged -= OnTagsChanged;
        _parsed.Close();
    }

    public void Wait(int version)
    {
        while (_tagger.ParsedSnapshotVersionNumber < version)
        {
            _logger.LogVerbose(
                $"Waiting for snapshot version to be parsed. {_tagger.ParsedSnapshotVersionNumber}<{version}");
            if (!_parsed.WaitOne(TimeSpan.FromSeconds(1)))
                _logger.LogWarning($"{version} not parsed in time");
        }
    }

    private void OnTagsChanged(object sender, SnapshotSpanEventArgs args) => _parsed.Set();
}
