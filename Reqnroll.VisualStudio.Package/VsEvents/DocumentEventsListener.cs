#nullable disable

namespace Reqnroll.VisualStudio.VsEvents;

public class DocumentEventsListener : IDisposable
{
    private readonly IDeveroomLogger _logger;
    private DocumentEvents _documentEvents;

    public DocumentEventsListener(IDeveroomLogger logger, DTE dte)
    {
        _logger = logger;
        _documentEvents = dte.Events.DocumentEvents;
        _documentEvents.DocumentOpened += DocumentEventsOnDocumentOpened;
    }

    public void Dispose()
    {
        if (_documentEvents != null)
        {
            _documentEvents.DocumentOpened -= DocumentEventsOnDocumentOpened;
            _documentEvents = null;
        }
    }

    public event Action<Document> DocumentOpened;

    private void DocumentEventsOnDocumentOpened(Document document)
    {
        _logger.LogVerbose($"{document.FullName}, {document.Type}");
        DocumentOpened?.Invoke(document);
    }
}
