namespace Reqnroll.VisualStudio.ProjectSystem;

public interface IDeveroomOutputPaneServices
{
    void WriteLine(string text);
    void SendWriteLine(string text);
    void Activate();
}
