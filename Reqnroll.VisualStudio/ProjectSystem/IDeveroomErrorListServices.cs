#nullable disable

namespace Reqnroll.VisualStudio.ProjectSystem;

public interface IDeveroomErrorListServices
{
    void ClearErrors(DeveroomUserErrorCategory category);
    void AddErrors(IEnumerable<DeveroomUserError> errors);
}

public enum DeveroomUserErrorCategory
{
    Discovery
}

public class DeveroomUserError
{
    public string Message { get; set; }
    public TaskErrorCategory Type { get; set; }
    public SourceLocation SourceLocation { get; set; }
    public DeveroomUserErrorCategory Category { get; set; }
}
