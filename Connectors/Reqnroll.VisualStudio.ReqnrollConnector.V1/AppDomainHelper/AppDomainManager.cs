#nullable disable
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Permissions;

// source: xUnit project

namespace Reqnroll.VisualStudio.ReqnrollConnector.AppDomainHelper;

internal class AppDomainManager : IDisposable
{
    public AppDomainManager(string assemblyFileName, string configFileName, bool shadowCopy, string shadowCopyFolder)
    {
        if (string.IsNullOrEmpty(assemblyFileName))
            throw new ArgumentException(nameof(assemblyFileName));

        assemblyFileName = Path.GetFullPath(assemblyFileName);

        if (configFileName == null)
            configFileName = GetDefaultConfigFile(assemblyFileName);

        if (configFileName != null)
            configFileName = Path.GetFullPath(configFileName);

        AssemblyFileName = assemblyFileName;
        ConfigFileName = configFileName;
        AppDomain = CreateAppDomain(assemblyFileName, configFileName, shadowCopy, shadowCopyFolder);
    }

    public AppDomain AppDomain { get; }

    public string AssemblyFileName { get; }

    public string ConfigFileName { get; }

    public virtual void Dispose()
    {
        if (AppDomain != null)
        {
            string cachePath = AppDomain.SetupInformation.CachePath;

            try
            {
                AppDomain.Unload(AppDomain);

                if (cachePath != null)
                    Directory.Delete(cachePath, true);
            }
            catch
            {
            }
        }
    }

    private static AppDomain CreateAppDomain(string assemblyFilename, string configFilename, bool shadowCopy,
        string shadowCopyFolder)
    {
        var setup = new AppDomainSetup();
        setup.ApplicationBase = Path.GetDirectoryName(assemblyFilename);
        setup.ApplicationName = Guid.NewGuid().ToString();

        if (shadowCopy)
        {
            setup.ShadowCopyFiles = "true";
            setup.ShadowCopyDirectories = setup.ApplicationBase;
            setup.CachePath = shadowCopyFolder ?? Path.Combine(Path.GetTempPath(), setup.ApplicationName);
        }

        setup.ConfigurationFile = configFilename;

        return AppDomain.CreateDomain(Path.GetFileNameWithoutExtension(assemblyFilename),
            AppDomain.CurrentDomain.Evidence, setup, new PermissionSet(PermissionState.Unrestricted));
    }

    public TObject CreateObjectFrom<TObject>(string assemblyLocation, string typeName, params object[] args)
    {
        try
        {
#pragma warning disable CS0618
            var unwrappedObject =
                AppDomain.CreateInstanceFromAndUnwrap(assemblyLocation, typeName, false, 0, null, args, null, null,
                    null);
#pragma warning restore CS0618
            return (TObject) unwrappedObject;
        }
        catch (TargetInvocationException ex)
        {
            RethrowExceptionWithNoStackTraceLoss(ex.InnerException);
            return default;
        }
    }

    public TObject CreateObject<TObject>(AssemblyName assemblyName, string typeName, params object[] args)
    {
        try
        {
#pragma warning disable CS0618
            var unwrappedObject = AppDomain.CreateInstanceAndUnwrap(assemblyName.FullName, typeName, false, 0, null,
                args, null, null, null);
#pragma warning restore CS0618
            return (TObject) unwrappedObject;
        }
        catch (TargetInvocationException ex)
        {
            RethrowExceptionWithNoStackTraceLoss(ex.InnerException);
            return default;
        }
    }

    public static void RethrowExceptionWithNoStackTraceLoss(Exception ex)
    {
        ExceptionDispatchInfo.Capture(ex).Throw();
    }

    private static string GetDefaultConfigFile(string assemblyFile)
    {
        string configFilename = assemblyFile + ".config";

        if (File.Exists(configFilename))
            return configFilename;

        return null;
    }
}
