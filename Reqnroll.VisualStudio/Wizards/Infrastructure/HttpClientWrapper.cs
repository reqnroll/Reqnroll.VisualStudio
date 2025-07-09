namespace Reqnroll.VisualStudio.Wizards.Infrastructure;

[Export(typeof(IHttpClient))]
public class HttpClientWrapper : IHttpClient
{
    public async Task<string> GetStringAsync(string url, CancellationTokenSource cts)
    {
        using (var client = new System.Net.Http.HttpClient())
        using (var response = await client.GetAsync(url, cts.Token))
        {
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}