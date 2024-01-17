namespace Reqnroll.VisualStudio.Analytics;

[System.Composition.Export(typeof(IContextInitializer))]
public class ReqnrollTelemetryContextInitializer : IContextInitializer
{
    private readonly IUserUniqueIdStore _userUniqueIdStore;
    private readonly IVersionProvider _versionProvider;

    [System.Composition.ImportingConstructor]
    public ReqnrollTelemetryContextInitializer(IUserUniqueIdStore userUniqueIdStore, IVersionProvider versionProvider)
    {
        _userUniqueIdStore = userUniqueIdStore;
        _versionProvider = versionProvider;
    }

    public void Initialize(TelemetryContext context)
    {
        context.Properties.Add("Ide", "Microsoft Visual Studio");
        context.Properties.Add("UserId", _userUniqueIdStore.GetUserId());
        context.Properties.Add("IdeVersion", _versionProvider.GetVsVersion());
        context.Properties.Add("ExtensionVersion", _versionProvider.GetExtensionVersion());
    }
}
