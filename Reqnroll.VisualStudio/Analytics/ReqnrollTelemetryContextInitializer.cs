namespace Reqnroll.VisualStudio.Analytics;

// We cannot directly use IContextInitializer as dependency (with MEF), because there might be other extensions (e.g. SpecFlow)
// that also export an implementation of IContextInitializer. We need to have a separate contract for "our" context initializer.
public interface IReqnrollContextInitializer : IContextInitializer
{
}

[System.Composition.Export(typeof(IReqnrollContextInitializer))]
public class ReqnrollTelemetryContextInitializer : IReqnrollContextInitializer
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
