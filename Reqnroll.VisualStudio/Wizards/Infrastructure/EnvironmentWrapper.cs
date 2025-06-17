namespace Reqnroll.VisualStudio.Wizards.Infrastructure
{
    public interface IEnvironmentWrapper
    {
        string? GetEnvironmentVariable(string name);
    }

    [Export(typeof(IEnvironmentWrapper))]
    public class EnvironmentWrapper : IEnvironmentWrapper
    {
        public string? GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }
    }
}
