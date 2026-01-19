using Reqnroll.VisualStudio.ProjectSystem;

namespace Reqnroll.VisualStudio.Editor.Services
{
    public abstract class StepFinderBase
    {
        private readonly IIdeScope _ideScope;

        protected StepFinderBase(IIdeScope ideScope)
        {
            _ideScope = ideScope;
        }
        protected bool LoadContent(string featureFilePath, out string content)
        {
            if (LoadAlreadyOpenedContent(featureFilePath, out string openedContent))
            {
                content = openedContent;
                return true;
            }

            if (LoadContentFromFile(featureFilePath, out string fileContent))
            {
                content = fileContent;
                return true;
            }

            content = string.Empty;
            return false;
        }

        private bool LoadContentFromFile(string featureFilePath, out string content)
        {
            try
            {
                content = _ideScope.FileSystem.File.ReadAllText(featureFilePath);
                return true;
            }
            catch (Exception ex)
            {
                _ideScope.Logger.LogDebugException(ex);
                content = string.Empty;
                return false;
            }
        }

        private bool LoadAlreadyOpenedContent(string featureFilePath, out string content)
        {
            var sl = new SourceLocation(featureFilePath, 1, 1);
            if (!_ideScope.GetTextBuffer(sl, out ITextBuffer tb))
            {
                content = string.Empty;
                return false;
            }

            content = tb.CurrentSnapshot.GetText();
            return true;
        }

        protected class StepFinderContext : IGherkinDocumentContext
        {
            public StepFinderContext(object node, IGherkinDocumentContext? parent = null)
            {
                Node = node;
                Parent = parent;
            }

            public IGherkinDocumentContext? Parent { get; }
            public object Node { get; }
        }
    }
}