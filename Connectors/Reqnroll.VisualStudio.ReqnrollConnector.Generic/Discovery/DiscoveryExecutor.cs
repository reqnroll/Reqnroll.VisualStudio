using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using Reqnroll.Bindings.Provider.Data;
using Reqnroll.VisualStudio.ReqnrollConnector.Models;
using ReqnrollConnector.AssemblyLoading;
using ReqnrollConnector.CommandLineOptions;
using ReqnrollConnector.Logging;
using ReqnrollConnector.SourceDiscovery;
using ReqnrollConnector.Utils;

namespace ReqnrollConnector.Discovery;

public class DiscoveryExecutor
{
    public static DiscoveryResult Execute(DiscoveryOptions options,
        Func<AssemblyLoadContext, string, Assembly> testAssemblyFactory, ILogger log, IAnalyticsContainer analytics)
    {
        log.Info($"Loading {options.AssemblyFile}");
        var testAssemblyContext = new TestAssemblyLoadContext(options.AssemblyFile, testAssemblyFactory, log);
        analytics.AddAnalyticsProperty("ImageRuntimeVersion", testAssemblyContext.TestAssembly.ImageRuntimeVersion);

        var targetFramework = GetTargetFramework(testAssemblyContext.TestAssembly);
        if (targetFramework != null)
            analytics.AddAnalyticsProperty("TargetFramework", targetFramework);

        var reqnrollVersion = GetReqnrollVersion(testAssemblyContext.TestAssembly, log);
        if (reqnrollVersion != null)
        {
            analytics.AddAnalyticsProperty("SFFile", reqnrollVersion.InternalName ?? reqnrollVersion.FileName);
            analytics.AddAnalyticsProperty("SFFileVersion", reqnrollVersion.FileVersion ?? "Unknown");
            analytics.AddAnalyticsProperty("SFProductVersion", reqnrollVersion.ProductVersion ?? "Unknown");
        }

        var configFileContent = LoadConfigFileContent(options.ConfigFile);

        var bindingProvider = GetBindingProvider(targetFramework, reqnrollVersion, log);

        BindingData bindingData;
        try
        {
            bindingData = bindingProvider.DiscoverBindings(testAssemblyContext, testAssemblyContext.TestAssembly, configFileContent, log);
        }
        catch (Exception ex)
        {
            return CreateErrorResult(analytics, $"Could discover bindings via: {bindingProvider}", ex);
        }

        var transformer = new DiscoveryResultTransformer();
        var sourceLocationProvider = new SourceLocationProvider(testAssemblyContext, testAssemblyContext.TestAssembly, log);
        var discoveryResult = transformer.Transform(bindingData, sourceLocationProvider, analytics);

        return new DiscoveryResult
        {
            StepDefinitions = discoveryResult.StepDefinitions,
            Hooks = discoveryResult.Hooks,
            SourceFiles = new Dictionary<string, string>(discoveryResult.SourceFiles),
            TypeNames = new Dictionary<string, string>(discoveryResult.TypeNames),
            AnalyticsProperties = analytics.ToDictionary()
        };
    }

    private static IBindingProvider GetBindingProvider(string? targetFramework, FileVersionInfo? reqnrollVersion, ILogger log)
    {
        // we could choose a version-specific binding provider here if needed
        var bindingProvider = new DefaultBindingProvider();
        log.Info($"Using binding provider: {bindingProvider} (target framework: {targetFramework}, Reqnroll version: {reqnrollVersion}");
        return bindingProvider;
    }

    private static string? GetTargetFramework(Assembly testAssembly)
    {
        var targetFrameworkAttribute = testAssembly.CustomAttributes
            .FirstOrDefault(a => a.AttributeType == typeof(TargetFrameworkAttribute));
        return targetFrameworkAttribute?.ConstructorArguments.First().ToString().Trim('\"');
    }

    private static string? LoadConfigFileContent(string? configFilePath)
    {
        if (string.IsNullOrEmpty(configFilePath))
            return null;

        var configFile = FileDetails.FromPath(configFilePath);
        if (configFile.Extension.Equals(".config", StringComparison.InvariantCultureIgnoreCase))
            return LegacyAppConfigLoader.LoadConfiguration(configFile);

        return File.ReadAllText(configFile.FullName);
    }

    private static FileVersionInfo? GetReqnrollVersion(Assembly testAssembly, ILogger log)
    {
        var reqnrollAssemblyPath =
            Path.Combine(Path.GetDirectoryName(testAssembly.Location) ?? ".", "Reqnroll.dll");
        if (File.Exists(reqnrollAssemblyPath))
            return GetReqnrollVersionInfo(reqnrollAssemblyPath, log);

        foreach (var otherReqnrollFile in Directory.EnumerateFiles(
                     Path.GetDirectoryName(reqnrollAssemblyPath)!, "Reqnroll*.dll"))
        {
            return GetReqnrollVersionInfo(otherReqnrollFile, log);
        }

        log.Info($"Not found {reqnrollAssemblyPath}");
        return null;
    }

    private static FileVersionInfo GetReqnrollVersionInfo(string reqnrollAssemblyPath, ILogger log)
    {
        var reqnrollVersion = FileVersionInfo.GetVersionInfo(reqnrollAssemblyPath);
        log.Info($"Found V{reqnrollVersion.FileVersion} at {reqnrollAssemblyPath}");
        return reqnrollVersion;
    }


    private static DiscoveryResult CreateErrorResult(IAnalyticsContainer analytics, string errorMessage, Exception? exception = null)
    {
        return new DiscoveryResult
        {
            AnalyticsProperties = analytics.ToDictionary(),
            ErrorMessage = exception != null ? $"{errorMessage}: {exception}" : errorMessage
        };
    }
}