#nullable enable

namespace Reqnroll.VisualStudio.VsxStubs.ProjectSystem;

public class StubErrorListServices : IDeveroomErrorListServices
{
    public ConcurrentBag<DeveroomUserError> Errors { get; private set; } = new();

    public void ClearErrors(DeveroomUserErrorCategory category) =>
        Errors = new ConcurrentBag<DeveroomUserError>(Errors.Where(e => e.Category == category));

    public void AddErrors(IEnumerable<DeveroomUserError> errors)
    {
        foreach (var error in errors) Errors.Add(error);
    }
}
