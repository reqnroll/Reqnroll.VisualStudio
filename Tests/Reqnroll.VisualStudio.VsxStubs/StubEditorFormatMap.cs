using Microsoft.VisualStudio.Text.Classification;

namespace Reqnroll.VisualStudio.VsxStubs;

public class StubEditorFormatMap : IEditorFormatMap
{
    public ResourceDictionary GetProperties(string key) => new();

    public void AddProperties(string key, ResourceDictionary properties)
    {
        throw new NotImplementedException();
    }

    public void SetProperties(string key, ResourceDictionary properties)
    {
        throw new NotImplementedException();
    }

    public void BeginBatchUpdate()
    {
        throw new NotImplementedException();
    }

    public void EndBatchUpdate()
    {
        throw new NotImplementedException();
    }

    public bool IsInBatchUpdate => throw new NotImplementedException();

    public event EventHandler<FormatItemsEventArgs>? FormatMappingChanged;
}
