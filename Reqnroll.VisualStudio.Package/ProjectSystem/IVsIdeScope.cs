namespace Reqnroll.VisualStudio.ProjectSystem;

public interface IVsIdeScope : IIdeScope, IDisposable
{
    IServiceProvider ServiceProvider { get; }
    DTE Dte { get; }
    IProjectScope GetProjectScope(Project project);
}
