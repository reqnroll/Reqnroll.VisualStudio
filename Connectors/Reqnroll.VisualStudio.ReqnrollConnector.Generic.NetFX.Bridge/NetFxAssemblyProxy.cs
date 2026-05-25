using ReqnrollConnector.AssemblyLoading.netFXAppDomainInterfaces;
using ReqnrollConnector.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace ReqnrollConnector.AssemblyLoading.netFx;

/// <summary>
/// Runs inside the remote AppDomain; all calls from the host domain are
/// marshalled transparently because this class inherits MarshalByRefObject.
/// Keep return types to primitives/strings where possible to avoid
/// accidental cross-domain type-identity issues.
/// </summary>
public class NetFxAssemblyProxy : MarshalByRefObject, INetFxAssemblyProxy
{
    private Assembly _testAssembly;

    // .NET Framework TFM folder names to probe in the NuGet cache, in preference order.
    // Matches the same fallback intent as NugetCacheAssemblyResolver's NetStandard20 fallback.
    private static readonly string[] NuGetLibFolderCandidates =
    {
        "net481", "net48", "net472", "net471", "net47",
        "net462", "net461", "net46", "net45",
        "netstandard2.0", "netstandard1.6", "netstandard1.0"
    };

    public NetFxAssemblyProxy()
    {
        // Subscribe before any loads — covers the window between CreateInstanceAndUnwrap
        // and Initialize(), and any implicit dependency loads triggered by LoadFrom().
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    public void Initialize(string assemblyPath)
    {
        _testAssembly = Assembly.LoadFrom(assemblyPath);
    }

    private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        try
        {
            var requestedName = new AssemblyName(args.Name);
            var simpleName = requestedName.Name;

            // 1. Never intercept BCL / GAC-owned assemblies — mirrors the System.
            //    guard in TestAssemblyLoadContext.Load() and the System.Runtime.dll
            //    skip in RuntimeCompositeCompilationAssemblyResolver.
            if (IsBclOrGacOwned(simpleName))
                return null;

            // 2. Probe AppBase and immediate subdirectories — mirrors AppBaseCompilationAssemblyResolver.
            //    CLR native probing already covers ApplicationBase itself, but this catches
            //    subdirectories that were not listed in AppDomainSetup.PrivateBinPath.
            var probeRoot = _testAssembly != null
                ? Path.GetDirectoryName(_testAssembly.Location)
                : AppDomain.CurrentDomain.BaseDirectory;

            if (!string.IsNullOrEmpty(probeRoot))
            {
                var found = ProbeDirectory(probeRoot, simpleName);
                if (found != null) return found;
            }

            // 3. NuGet cache fallback — mirrors NugetCacheAssemblyResolver.
            //    Valuable for SDK-style .NET Framework projects whose transitive
            //    packages are not always copied to the output directory.
            var version = requestedName.Version;
            if (version != null)
            {
                var nugetAssembly = ProbeNuGetCache(simpleName, version);
                if (nugetAssembly != null) return nugetAssembly;
            }
        }
        catch
        {
            // Mirrors the exception-swallowing in RuntimeCompositeCompilationAssemblyResolver —
            // return null and let the CLR continue with its own fallbacks.
        }

        return null;
    }

    private static bool IsBclOrGacOwned(string simpleName)
    {
        return simpleName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
               simpleName.Equals("System", StringComparison.OrdinalIgnoreCase) ||
               simpleName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
               simpleName.Equals("netstandard", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Probes <paramref name="directory"/> and its immediate subdirectories for
    /// <c>{simpleName}.dll</c>, skipping any path that is under a <c>refs/</c>
    /// folder — mirrors the <c>IsRefsPath</c> filter shared by both
    /// <c>RuntimeCompositeCompilationAssemblyResolver</c> and
    /// <c>TestAssemblyLoadContext</c>.
    /// </summary>
    private static Assembly ProbeDirectory(string directory, string simpleName)
    {
        foreach (var candidate in CandidatePaths(directory, simpleName))
        {
            if (File.Exists(candidate) && !IsRefsPath(candidate))
                return Assembly.LoadFile(candidate); // LoadFile does not re-raise AssemblyResolve
        }
        return null;
    }

    private static IEnumerable<string> CandidatePaths(string directory, string simpleName)
    {
        var fileName = simpleName + ".dll";
        yield return Path.Combine(directory, fileName);

        // One level of subdirectories (e.g. x64/, x86/, runtimes/)
        foreach (var sub in Directory.GetDirectories(directory))
            yield return Path.Combine(sub, fileName);
    }

    /// <summary>
    /// Mirrors <c>NugetCacheAssemblyResolver</c>: probes
    /// <c>%NUGET_PACKAGES%\{name}\{major}.{minor}.{build}\lib\{tfm}\{name}.dll</c>
    /// using the same environment-variable resolution order.
    /// Version path uses major.minor.patch to match NuGet folder naming conventions.
    /// </summary>
    private static Assembly ProbeNuGetCache(string simpleName, Version version)
    {
        var cacheRoot = NuGetCachePath();
        if (string.IsNullOrEmpty(cacheRoot)) return null;

        // NuGet cache folders use the full SemVer string as the version segment.
        var versionFolder = $"{version.Major}.{version.Minor}.{version.Build}";
        var libRoot = Path.Combine(cacheRoot, simpleName.ToLowerInvariant(), versionFolder, "lib");
        if (!Directory.Exists(libRoot)) return null;

        var fileName = simpleName + ".dll";
        foreach (var tfm in NuGetLibFolderCandidates)
        {
            var candidate = Path.Combine(libRoot, tfm, fileName);
            if (File.Exists(candidate) && !IsRefsPath(candidate))
                return Assembly.LoadFile(candidate);
        }
        return null;
    }

    /// <summary>Mirrors <c>NugetCacheAssemblyResolver.NugetCachePath()</c>.</summary>
    private static string NuGetCachePath()
    {
        var path = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                   ?? Environment.GetEnvironmentVariable("NuGetCachePath")
                   ?? Path.Combine(
                       Environment.GetEnvironmentVariable("USERPROFILE") ?? string.Empty,
                       ".nuget", "packages");
        return Environment.ExpandEnvironmentVariables(path);
    }

    /// <summary>
    /// Mirrors the <c>IsRefsPath</c> helper present in both
    /// <c>RuntimeCompositeCompilationAssemblyResolver</c> and <c>TestAssemblyLoadContext</c>.
    /// Rejects reference-only assemblies that live under a <c>refs/</c> directory.
    /// </summary>
    private static bool IsRefsPath(string path)
    {
        var dir = Path.GetDirectoryName(path);
        return !string.IsNullOrEmpty(dir) &&
               "refs".Equals(Path.GetFileName(dir), StringComparison.OrdinalIgnoreCase);
    }

    // Return strings so callers never hold a cross-domain Assembly proxy
    // just for metadata — they can re-resolve if they need the object.
    public string ImageRuntimeVersion => _testAssembly.ImageRuntimeVersion;

    public string TargetFrameworkName
    {
        get
        {
            var attr = (TargetFrameworkAttribute)Attribute.GetCustomAttribute(
                _testAssembly, typeof(TargetFrameworkAttribute));
            return attr?.FrameworkName ?? string.Empty;
        }
    }

    public string TestAssemblyLocation => _testAssembly.Location;
    public string TestAssemblyFullName => _testAssembly.FullName;

    private Assembly LoadAssemblyByName(string fullAssemblyName) =>
            Assembly.Load(fullAssemblyName);

    private Type GetType(string assemblyName, string typeName)
    {
        var asm = Assembly.Load(assemblyName);
        return asm.GetType(typeName, throwOnError: true);
    }

    public string InvokeReqnrollBindingDiscoveryMethod(string? configFileContent)
    {
        var bindingProviderServiceType = GetType("Reqnroll", "Reqnroll.Bindings.Provider.BindingProviderService");
        var bindingJson = bindingProviderServiceType.ReflectionCallStaticMethod<string>("DiscoverBindings", new[] { typeof(Assembly), typeof(string) }, _testAssembly, configFileContent);
        return bindingJson;
    }

    public string AssemblyLocationFromAssemblyName(string assemblyName)
    {
        var asm = Assembly.Load(assemblyName);
        return asm.Location;
    }

    // Ensure the lease never expires while the AppDomain is alive.
    public override object InitializeLifetimeService() => null;
}
