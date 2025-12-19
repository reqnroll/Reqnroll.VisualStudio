# Connector Assembly Dependencies

## Overview

The Reqnroll and SpecFlow connectors are out-of-process executables that load user test assemblies to discover step definitions. When user test projects reference certain Reqnroll/SpecFlow plugins, those plugins require specific dependencies that may not be available on end-user machines.

## Microsoft.Extensions.DependencyInjection Dependencies

### Why These Are Needed

Many users install the `Reqnroll.Microsoft.Extensions.DependencyInjection` plugin in their test projects. This plugin enables using Microsoft's dependency injection container for test dependencies.

**The Problem:**
- The plugin depends on `Microsoft.Extensions.DependencyInjection` v6.0.0
- Which transitively depends on `Microsoft.Extensions.DependencyInjection.Abstractions` v6.0.0
- When the connector loads a user's test assembly, Reqnroll initializes and tries to load the plugin
- On **development machines**, these assemblies might be available in:
  - Global Assembly Cache (GAC)
  - .NET SDK folders
  - System-wide NuGet cache
- On **end-user machines** (without full SDK), these assemblies are NOT available globally
- Even if the user's project references them, they may not be copied to the test project's bin folder

**The Solution:**
- Bundle these assemblies WITH the connectors themselves
- The connectors' assembly resolution will find them in the connector's directory
- For .NET Framework (V1 connector): `AssemblyHelper` searches the connector directory
- For .NET Core (Generic connector): `TestAssemblyLoadContext` uses multiple resolution strategies including NuGet cache

### Assemblies Included

**V1 Connector (.NET Framework 4.8):**
- `Microsoft.Extensions.DependencyInjection.dll` (6.0.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions.dll` (6.0.0)
- `Microsoft.Bcl.AsyncInterfaces.dll` (6.0.0 - transitive dependency)

**Generic Connector (.NET 6+):**
- `Microsoft.Extensions.DependencyInjection.dll` (6.0.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions.dll` (6.0.0)

**Note:** The Generic connector also includes `Microsoft.Extensions.DependencyModel.dll` (8.0.1), but this was already present before this fix - it's used for reading deps.json files, not for DI plugin support.

### Version Selection

**Why version 6.0.0?**
- All versions of `Reqnroll.Microsoft.Extensions.DependencyInjection` plugin (v2.x and v3.x) depend on v6.0.0
- This version is compatible with all .NET Framework and .NET Core versions the connectors support
- Using the exact version the plugin expects avoids assembly binding redirect issues

### Adding New Assembly Dependencies

If you need to add support for other plugins or dependencies:

1. **Identify the dependency chain:**
   - Check the plugin's `.nuspec` file for its dependencies
   - Check transitive dependencies recursively
   - Document which plugin requires which assemblies

2. **Add to the appropriate connector project(s):**
   - Update `Reqnroll.VisualStudio.ReqnrollConnector.V1.csproj` for .NET Framework
   - Update `Reqnroll.VisualStudio.ReqnrollConnector.Generic.csproj` for .NET Core
   - Add as `<PackageReference>` with specific version

3. **Add documentation:**
   - Add XML comments in the `.csproj` file explaining why the package is needed
   - Update this README with the new dependency information

4. **Verify deployment:**
   - Build connectors: `cd Connectors && pwsh build.ps1`
   - Check assemblies in `Connectors/bin/Debug/Reqnroll-V1/` and `Reqnroll-Generic-net6.0/`
   - Build VSIX: `dotnet build Reqnroll.VisualStudio.Package/Reqnroll.VisualStudio.Package.csproj`
   - Check assemblies in `Reqnroll.VisualStudio.Package/bin/Debug/net481/Connectors/`

## Verification Instructions

### Verify Connector Build Output

After building connectors with `pwsh Connectors/build.ps1`:

**PowerShell:**
```powershell
# Check V1 Connector (.NET Framework)
Get-ChildItem Connectors/bin/Debug/Reqnroll-V1/ | Where-Object Name -like "Microsoft.Extensions*"

# Expected output:
# Microsoft.Bcl.AsyncInterfaces.dll
# Microsoft.Extensions.DependencyInjection.Abstractions.dll
# Microsoft.Extensions.DependencyInjection.dll

# Check Generic Connector (.NET 6+)
Get-ChildItem Connectors/bin/Debug/Reqnroll-Generic-net6.0/ | Where-Object Name -like "Microsoft.Extensions*"

# Expected output:
# Microsoft.Extensions.DependencyInjection.Abstractions.dll
# Microsoft.Extensions.DependencyInjection.dll
# Microsoft.Extensions.DependencyModel.dll (was already present before this fix)
```

**Bash/Linux:**
```bash
# Check V1 Connector (.NET Framework)
ls Connectors/bin/Debug/Reqnroll-V1/ | grep "Microsoft.Extensions"

# Check Generic Connector (.NET 6+)
ls Connectors/bin/Debug/Reqnroll-Generic-net6.0/ | grep "Microsoft.Extensions"
```

### Verify VSIX Package Contents

After building the VSIX package:

**PowerShell:**
```powershell
# Build the VSIX
dotnet build Reqnroll.VisualStudio.Package/Reqnroll.VisualStudio.Package.csproj

# Check V1 connector in package output
Get-ChildItem Reqnroll.VisualStudio.Package/bin/Debug/net481/Connectors/Reqnroll-V1/ | Where-Object Name -like "Microsoft.Extensions*"

# Check Generic connector in package output  
Get-ChildItem Reqnroll.VisualStudio.Package/bin/Debug/net481/Connectors/Reqnroll-Generic-net6.0/ | Where-Object Name -like "Microsoft.Extensions*"
```

**Bash/Linux:**
```bash
# Build the VSIX
dotnet build Reqnroll.VisualStudio.Package/Reqnroll.VisualStudio.Package.csproj

# Check V1 connector in package output
ls Reqnroll.VisualStudio.Package/bin/Debug/net481/Connectors/Reqnroll-V1/ | grep "Microsoft.Extensions"

# Check Generic connector in package output  
ls Reqnroll.VisualStudio.Package/bin/Debug/net481/Connectors/Reqnroll-Generic-net6.0/ | grep "Microsoft.Extensions"
```

### Test on Clean Machine

To verify the fix works on end-user machines without SDK:

1. Install the VSIX on a clean Windows machine (or VM) without .NET SDK
2. Create a test project that uses `Reqnroll.Microsoft.Extensions.DependencyInjection` plugin:
   ```xml
   <PackageReference Include="Reqnroll" Version="3.1.1" />
   <PackageReference Include="Reqnroll.Microsoft.Extensions.DependencyInjection" Version="3.1.1" />
   ```
3. Open a feature file in Visual Studio
4. Verify step definitions are discovered without `FileNotFoundException` errors
5. Check the VS output window and extension logs for any assembly loading errors

### Check Extension Logs

If issues occur, check the extension logs:

- Location: `%LOCALAPPDATA%\Reqnroll\reqnroll-vs-{date}.log`
- Look for: `FileNotFoundException` or assembly loading errors
- Connector invocation logs show which assemblies are being loaded

## Common Issues

### Assembly Version Mismatches

**Symptom:** User's project has a different version of Microsoft.Extensions.* assemblies

**Solution:** The connectors include v6.0.0 which should work for most scenarios. If users have newer versions (7.x, 8.x), .NET's assembly binding should redirect to the available version. For .NET Framework projects, this might require binding redirects in the user's app.config, but that's the user's responsibility, not the extension's.

### Missing Transitive Dependencies

**Symptom:** After adding a new package, its transitive dependencies are missing

**Solution:** Check the package's dependencies with:
```bash
dotnet list package --include-transitive
```

Add any missing dependencies explicitly to the `.csproj` file.

### Size Concerns

**Impact:** Each connector adds ~150KB for these assemblies

**Mitigation:** This is acceptable given the alternative is the extension not working for many users.

## Related Files

- `Connectors/Reqnroll.VisualStudio.ReqnrollConnector.V1/Reqnroll.VisualStudio.ReqnrollConnector.V1.csproj` - V1 connector package references
- `Connectors/Reqnroll.VisualStudio.ReqnrollConnector.Generic/Reqnroll.VisualStudio.ReqnrollConnector.Generic.csproj` - Generic connector package references
- `Connectors/Reqnroll.VisualStudio.ReqnrollConnector.V1/AppDomainHelper/AssemblyHelper.cs` - V1 assembly resolution
- `Connectors/Reqnroll.VisualStudio.ReqnrollConnector.Generic/AssemblyLoading/TestAssemblyLoadContext.cs` - Generic assembly resolution
- `Connectors/build.ps1` - Connector build script
- `Connectors/DeploymentAssets.props` - Connector deployment configuration

## References

- [Reqnroll.Microsoft.Extensions.DependencyInjection on NuGet](https://www.nuget.org/packages/Reqnroll.Microsoft.Extensions.DependencyInjection)
- [Microsoft.Extensions.DependencyInjection on NuGet](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection)
- [GitHub Issue: Assembly Loading](https://github.com/reqnroll/Reqnroll.VisualStudio/issues) (link to specific issue once created)
