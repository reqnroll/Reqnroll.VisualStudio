namespace Reqnroll.VisualStudio.Editor.Services;

public interface IDeveroomTagParser
{
    IReadOnlyCollection<DeveroomTag> Parse(ITextSnapshot fileSnapshot);
}
