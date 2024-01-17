using System;
using System.Linq;

namespace Reqnroll.VisualStudio;

[Export(typeof(IVersionProvider))]
public class VersionProvider : IVersionProvider
{
    private readonly Lazy<string> _lazyExtensionVersion;

    private readonly Lazy<string> _lazyVsVersion;
    private readonly IServiceProvider _serviceProvider;

    [ImportingConstructor]
    public VersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _lazyVsVersion = new Lazy<string>(GetVsProductDisplayVersion);
        _lazyExtensionVersion = new Lazy<string>(ReadExtensionVersion);
    }

    public string GetVsVersion() => _lazyVsVersion.Value;

    public string GetExtensionVersion() => _lazyExtensionVersion.Value;

    private string GetVsProductDisplayVersion() => VsUtils.GetVsProductDisplayVersionSafe(_serviceProvider);

    private string ReadExtensionVersion()
    {
        var assembly = Assembly.GetAssembly(typeof(VersionProvider));
        var versionAttr = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
            .OfType<AssemblyInformationalVersionAttribute>().FirstOrDefault();
        if (versionAttr != null) return versionAttr.InformationalVersion.Split('+', '-')[0];
        return assembly.GetName().Version.ToString(3);
    }
}
