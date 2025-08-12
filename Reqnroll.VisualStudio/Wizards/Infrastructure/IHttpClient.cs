namespace Reqnroll.VisualStudio.Wizards.Infrastructure;
public interface IHttpClient
{
    Task<string> GetStringAsync(string url, CancellationTokenSource cts);
}