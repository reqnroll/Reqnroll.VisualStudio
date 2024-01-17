#nullable disable

namespace Reqnroll.SampleProjectGenerator;

public abstract class ProjectGenerator : IProjectGenerator
{
    protected readonly Action<string> _consoleWriteLine;
    protected readonly GeneratorOptions _options;

    protected ProjectGenerator(GeneratorOptions options, Action<string> consoleWriteLine)
    {
        _options = options;
        _consoleWriteLine = consoleWriteLine ?? Console.WriteLine;
    }

    public string AssemblyFileName => AssemblyName + ".dll";

    public string TargetFolder => _options.TargetFolder;
    public string TargetFramework => _options.TargetFramework;
    public virtual string PackagesFolder => "packages";
    public string AssemblyName => "DeveroomSample";
    public abstract string GetOutputAssemblyPath(string config = "Debug");
    public List<string> FeatureFiles { get; } = new();
    public List<NuGetPackageData> InstalledNuGetPackages { get; } = new();

    public void Generate()
    {
        _consoleWriteLine(_options.TargetFolder);
        if (!_options.Force &&
            Directory.Exists(Path.Combine(_options.TargetFolder, ".git")) &&
            File.Exists(Path.Combine(_options.TargetFolder, "DeveroomSample.csproj")))
            if (GitReset())
            {
                ScanExistingProjectFolder();
                return;
            }

        EnsureEmptyFolder(_options.TargetFolder);

        var templatesFolder = GetTemplatesFolder();
        string projectFilePath = null;
        foreach (var template in Directory.GetFiles(templatesFolder, "*.txt"))
        {
            var destFileName = Path.Combine(_options.TargetFolder, Path.GetFileNameWithoutExtension(template));
            File.Copy(template, destFileName, true);
            if (destFileName.EndsWith("proj"))
                projectFilePath = destFileName;
        }

        var packagesFolder = GetPackagesFolder();

        if (_options.PlatformTarget != null)
            SetPlatformTarget(projectFilePath);

        if (_options.TargetFramework != GeneratorOptions.DefaultTargetFramework)
            SetTargetFramework(projectFilePath);

        switch (_options.UnitTestProvider.ToLowerInvariant())
        {
            case "nunit":
                InstallNUnit(packagesFolder, projectFilePath);
                break;
            case "xunit":
                InstallXUnit(packagesFolder, projectFilePath);
                break;
            case "mstest":
                InstallMsTest(packagesFolder, projectFilePath);
                break;
            default:
                throw new NotSupportedException(_options.UnitTestProvider);
        }

        InstallReqnroll(packagesFolder, projectFilePath);

        GenerateTestArtifacts(projectFilePath);

        if (_options.AddGeneratorPlugin || _options.AddRuntimePlugin)
            InstallReqnrollPlugin(packagesFolder, projectFilePath);

        if (_options.AddExternalBindingPackage)
            InstallExternalBindingPackage(packagesFolder, projectFilePath);

        if (_options.IsBuilt)
        {
            EnsureEnvironmentVariables();

            BuildProject();

            File.WriteAllText(Path.Combine(_options.TargetFolder, ".gitignore"), "");
        }

        GitInit();
    }

    private void EnsureEnvironmentVariables()
    {
        if (_options.ReqnrollVersion.Major == 3 && _options.ReqnrollVersion.Minor < 2)
            //MSBUILDSINGLELOADCONTEXT is required for some Reqnroll versions.
            //See https://stackoverflow.com/questions/60755395/reqnroll-generatefeaturefilecodebehindtask-has-failed-unexpectedly
            Environment.SetEnvironmentVariable("MSBUILDSINGLELOADCONTEXT", "1");
    }

    protected virtual void SetTargetFramework(string projectFilePath)
    {
        var projectChanger = CreateProjectChanger(projectFilePath);
        projectChanger.SetTargetFramework(_options.TargetFramework);
        projectChanger.Save();
    }

    protected virtual void ScanExistingProjectFolder()
    {
        FeatureFiles.AddRange(Directory.GetFiles(_options.TargetFolder, "*.feature", SearchOption.AllDirectories));
        var projectFilePath = Directory.GetFiles(TargetFolder, "*.csproj").FirstOrDefault();
        if (projectFilePath == null)
            throw new Exception("Unable to detect project file");
        var projectChanger = CreateProjectChanger(projectFilePath);
        InstalledNuGetPackages.AddRange(projectChanger.GetInstalledNuGetPackages(GetPackagesFolder()));
    }

    protected abstract string GetTemplatesFolder();
    protected abstract string GetPackagesFolder();
    protected abstract ProjectChanger CreateProjectChanger(string projectFilePath);

    protected virtual void BuildProject()
    {
        var exitCode = ExecBuild();

        if (exitCode != 0)
        {
            _consoleWriteLine($"Build exit code: {exitCode}");
            throw new Exception($"Build failed with exit code {exitCode}");
        }
    }

    protected abstract int ExecBuild();

    private void SetPlatformTarget(string projectFilePath)
    {
        var projectChanger = CreateProjectChanger(projectFilePath);
        projectChanger.SetPlatformTarget(_options.PlatformTarget);
        projectChanger.Save();
    }

    private void GenerateTestArtifacts(string projectFilePath)
    {
        var customTool = _options.ReqnrollVersion >= new Version("3.0") ? null : "ReqnrollSingleFileGenerator";

        var featuresFolder = Path.Combine(_options.TargetFolder, "Features");
        var stepDefsFolder = Path.Combine(_options.TargetFolder, "StepDefinitions");
        EnsureEmptyFolder(featuresFolder);
        EnsureEmptyFolder(stepDefsFolder);

        var stepCount = _options.FeatureFileCount * _options.ScenarioPerFeatureFileCount * 4;
        var assetGenerator =
            new ReqnrollAssetGenerator(Math.Max(3, stepCount * _options.StepDefinitionPerStepPercent / 100));
        if (_options.AddUnicodeBinding)
            assetGenerator.AddUnicodeSteps();
        if (_options.AddAsyncStep)
            assetGenerator.AddAsyncStep();
        var projectChanger = CreateProjectChanger(projectFilePath);
        for (int i = 0; i < _options.FeatureFileCount; i++)
        {
            var scenarioOutlineCount =
                _options.ScenarioPerFeatureFileCount * _options.ScenarioOutlinePerScenarioPercent / 100;
            var scenarioCount = _options.ScenarioPerFeatureFileCount - scenarioOutlineCount;
            var filePath = assetGenerator.GenerateFeatureFile(featuresFolder, scenarioCount, scenarioOutlineCount);
            projectChanger.AddFile(filePath, "None", customTool);
            FeatureFiles.Add(filePath);
        }

        var stepDefClasses = assetGenerator.GenerateStepDefClasses(stepDefsFolder, _options.StepDefPerClassCount);
        foreach (var stepDefClass in stepDefClasses) projectChanger.AddFile(stepDefClass, "Compile");
        projectChanger.Save();

        _consoleWriteLine(
            $"Generated {assetGenerator.StepDefCount} step definitions, {_options.FeatureFileCount * _options.ScenarioPerFeatureFileCount} scenarios, {assetGenerator.StepCount} steps, {stepDefClasses.Count} step definition classes, {_options.FeatureFileCount} feature files");
    }

    private void ExecNuGetInstall(string packageName, string packagesFolder, params string[] otherArgs)
    {
        var args = new[]
        {
            "install", packageName, "-OutputDirectory", packagesFolder,
            "-Source", "https://api.nuget.org/v3/index.json"
        }.AsEnumerable();
        if (otherArgs != null)
            args = args.Concat(otherArgs);
        if (_options.FallbackNuGetPackageSource != null)
            args = args.Concat(new[] { "-FallbackSource", _options.FallbackNuGetPackageSource });
        ExecNuGet(args.ToArray());
    }

    private void InstallExternalBindingPackage(string packagesFolder, string projectFilePath)
    {
        ExecNuGetInstall(_options.ExternalBindingPackageName, packagesFolder);
        var projectChanger = CreateProjectChanger(projectFilePath);
        InstallNuGetPackage(projectChanger, packagesFolder, _options.ExternalBindingPackageName, packageVersion: "1.0.0", sourcePlatform: "net45");
        projectChanger.SetReqnrollConfig("stepAssemblies/stepAssembly", "assembly",
            _options.ExternalBindingPackageName);
        projectChanger.Save();
    }

    private void InstallReqnrollPlugin(string packagesFolder, string projectFilePath)
    {
        ExecNuGetInstall(_options.PluginName, packagesFolder);
        var projectChanger = CreateProjectChanger(projectFilePath);
        InstallNuGetPackage(projectChanger, packagesFolder, _options.PluginName, packageVersion: "1.0.0", sourcePlatform: "net45");
        if (_options.ReqnrollVersion.Major < 3)
        {
            projectChanger.SetReqnrollConfig("plugins/add", "name", _options.PluginName.Replace(".ReqnrollPlugin", ""));
            projectChanger.SetReqnrollConfig("plugins/add", "type",
                _options.AddRuntimePlugin && _options.AddGeneratorPlugin ? "GeneratorAndRuntime" :
                _options.AddGeneratorPlugin ? "Generator" : "Runtime");
        }

        projectChanger.Save();
    }

    private bool GitReset()
    {
        var exitCode = ExecGit("reset", "--hard");
        ExecGit("clean", "-fdx", "-e", "packages");
        if (exitCode != 0)
            _consoleWriteLine($"Git status exit code: {exitCode}");
        return exitCode == 0;
    }

    private void GitInit()
    {
        ExecGit("init");
        GitCommitAll();
    }

    private void GitCommitAll()
    {
        ExecGit("add", ".");
        ExecGit("-c user.name='Reqnroll'", "-c user.email='reqnroll@reqnroll.net'", "commit", "-q", "-m", "init");
    }

    private void InstallNUnit(string packagesFolder, string projectFilePath)
    {
        if (IsTargetFrameworkTooOldForNUnit4(_options.TargetFramework) || _options.ReqnrollVersion < new Version("2.0"))
        {
            ExecNuGetInstall("NUnit", packagesFolder, "-Version", "3.14.0"); //Latest major 3 version
        }
        else
        {
            ExecNuGetInstall("NUnit", packagesFolder);
        }

        ExecNuGetInstall("NUnit3TestAdapter", packagesFolder);

        var projectChanger = CreateProjectChanger(projectFilePath);

        if (IsTargetFrameworkTooOldForNUnit4(_options.TargetFramework) || _options.ReqnrollVersion < new Version("2.0"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "NUnit", packageVersion: "3.14.0", sourcePlatform: "netstandard2.0");
        }
        else
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "NUnit");
        }

        InstallNuGetPackage(projectChanger, packagesFolder, "NUnit3TestAdapter", "net35");
        projectChanger.Save();
    }

    /// <summary>
    /// NUnit 4 only supports >= .net6.0  & >= net462
    /// </summary>
    private bool IsTargetFrameworkTooOldForNUnit4(string framework)
    {
        return framework switch
        {
            "netcoreapp20" => true,
            "netcoreapp2.0" => true,
            "netcoreapp2.1" => true,
            "netcoreapp21" => true,
            "netcoreapp2.2" => true,
            "netcoreapp22" => true,
            "netcoreapp3.0" => true,
            "netcoreapp30" => true,
            "netcoreapp3.1" => true,
            "netcoreapp31" => true,
            "net5.0" => true,
            _ => false
        };
    }

    private void InstallXUnit(string packagesFolder, string projectFilePath)
    {
        ExecNuGetInstall("xUnit", packagesFolder);
        ExecNuGetInstall("xunit.runner.visualstudio", packagesFolder);

        var projectChanger = CreateProjectChanger(projectFilePath);
        InstallNuGetPackage(projectChanger, packagesFolder, "xunit.core");
        InstallNuGetPackage(projectChanger, packagesFolder, "xunit.abstractions", "net35");
        InstallNuGetPackage(projectChanger, packagesFolder, "xunit.assert", "netstandard1.1");
        InstallNuGetPackage(projectChanger, packagesFolder, "xunit.extensibility.core", "netstandard1.1");
        InstallNuGetPackage(projectChanger, packagesFolder, "xunit.extensibility.execution", "netstandard1.1");
        InstallNuGetPackage(projectChanger, packagesFolder, "xunit.runner.visualstudio", "net462");
        projectChanger.Save();
    }

    private void InstallMsTest(string packagesFolder, string projectFilePath)
    {
        ExecNuGetInstall("MSTest.TestFramework", packagesFolder);
        ExecNuGetInstall("MSTest.TestAdapter", packagesFolder);

        var projectChanger = CreateProjectChanger(projectFilePath);
        InstallNuGetPackage(projectChanger, packagesFolder, "MSTest.TestFramework", "net462");
        InstallNuGetPackage(projectChanger, packagesFolder, "MSTest.TestAdapter");
        projectChanger.Save();
    }

    private void InstallReqnroll(string packagesFolder, string projectFilePath)
    {
        ExecNuGetInstall("Reqnroll", packagesFolder, "-Version", _options.ReqnrollPackageVersion);

        var projectChanger = CreateProjectChanger(projectFilePath);
        InstallReqnrollPackages(packagesFolder, projectChanger);
        SetReqnrollUnitTestProvider(projectChanger, packagesFolder);
        projectChanger.Save();
    }

    protected virtual void SetReqnrollUnitTestProvider(ProjectChanger projectChanger, string packagesFolder)
    {
        if (_options.ReqnrollVersion >= new Version("3.0"))
        {
            var sourcePlatform = GetReqnrollSourcePlatform();
            ExecNuGetInstall("Reqnroll.Tools.MsBuild.Generation", packagesFolder, "-Version",
                _options.ReqnrollPackageVersion);
            InstallNuGetPackage(projectChanger, packagesFolder, "Reqnroll.Tools.MsBuild.Generation", sourcePlatform,
                _options.ReqnrollPackageVersion);

            ExecNuGetInstall("Reqnroll." + _options.UnitTestProvider, packagesFolder, "-Version",
                _options.ReqnrollPackageVersion);
            InstallNuGetPackage(projectChanger, packagesFolder, "Reqnroll." + _options.UnitTestProvider, sourcePlatform,
                _options.ReqnrollPackageVersion);
            return;
        }

        projectChanger.SetReqnrollConfig("unitTestProvider", "name", _options.UnitTestProvider);
    }

    protected virtual void InstallReqnrollPackages(string packagesFolder, ProjectChanger projectChanger)
    {
        var sourcePlatform = GetReqnrollSourcePlatform();
        InstallNuGetPackage(projectChanger, packagesFolder, "Reqnroll", sourcePlatform,
            _options.ReqnrollPackageVersion);

        if (_options.ReqnrollVersion >= new Version("3.1"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "Cucumber.Messages", dependency: true,
                packageVersion: "6.0.1", sourcePlatform: "netstandard2.0");
            InstallNuGetPackage(projectChanger, packagesFolder, "Google.Protobuf", dependency: true,
                packageVersion: "3.7.0", sourcePlatform: "netstandard1.0");
        }

        if (_options.ReqnrollVersion >= new Version("3.7"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "BoDi", dependency: true, packageVersion: "1.5.0", sourcePlatform: "netstandard2.0");
            InstallNuGetPackage(projectChanger, packagesFolder, "Gherkin", dependency: true, packageVersion: "6.0.0", sourcePlatform: "netstandard2.0");
            InstallNuGetPackage(projectChanger, packagesFolder, "Utf8Json", "net45", dependency: true,
                packageVersion: "1.3.7");
            InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0",
                dependency: true);
        }
        else if (_options.ReqnrollVersion >= new Version("3.0.188"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "BoDi", dependency: true, packageVersion: "1.4.1", sourcePlatform: "netstandard2.0");
            InstallNuGetPackage(projectChanger, packagesFolder, "Gherkin", dependency: true, packageVersion: "6.0.0", sourcePlatform: "netstandard2.0");
            InstallNuGetPackage(projectChanger, packagesFolder, "Utf8Json", "net45", dependency: true,
                packageVersion: "1.3.7");
            InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0",
                dependency: true);
        }
        else if (_options.ReqnrollVersion >= new Version("3.0"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "BoDi", dependency: true,
                packageVersion: "1.4.0-alpha1");
            InstallNuGetPackage(projectChanger, packagesFolder, "Gherkin", dependency: true,
                packageVersion: "6.0.0-beta1");
            InstallNuGetPackage(projectChanger, packagesFolder, "Utf8Json", "net45", dependency: true,
                packageVersion: "1.3.7");
            InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0",
                dependency: true);
        }
        else if (_options.ReqnrollVersion >= new Version("2.3"))
        {
            InstallNuGetPackage(projectChanger, packagesFolder, "Newtonsoft.Json", dependency: true, sourcePlatform: "netstandard1.0");
            InstallNuGetPackage(projectChanger, packagesFolder, "System.ValueTuple", "netstandard1.0",
                dependency: true);
        }
    }

    private string GetReqnrollSourcePlatform()
    {
        var sourcePlatform =
            _options.ReqnrollVersion >= new Version("3.3") ? "net461" :
            _options.ReqnrollVersion >= new Version("2.0") ? "net45" :
            "net35";
        return sourcePlatform;
    }

    protected void InstallNuGetPackage(ProjectChanger projectChanger, string packagesFolder, string packageName,
        string sourcePlatform = "net462", string packageVersion = null, bool dependency = false)
    {
        var package =
            projectChanger.InstallNuGetPackage(packagesFolder, packageName, sourcePlatform, packageVersion, dependency);
        if (package != null)
            InstalledNuGetPackages.Add(package);
    }

    private void EnsureEmptyFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            return;
        }

        if (!Directory.GetFileSystemEntries(folder).Any())
            return;

        foreach (var subFolder in Directory.GetDirectories(folder))
            try
            {
                Directory.Delete(subFolder, true);
            }
            catch (Exception)
            {
            }

        foreach (var file in Directory.GetFiles(folder))
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
            }

        Thread.Sleep(200);

        if (!Directory.GetFileSystemEntries(folder).Any())
            return;

        for (int i = 0; i < 3; i++)
            if (Directory.Exists(folder))
            {
                Exec(Path.Combine(folder, ".."), Environment.GetEnvironmentVariable("ComSpec"), "/C", "rmdir",
                    "/S", "/Q", folder);
                Thread.Sleep(500);
            }

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            Thread.Sleep(500);
        }
    }

    private void ExecNuGet(params string[] args)
    {
        Exec(_options.TargetFolder, ToolLocator.GetToolPath(ExternalTools.NuGet, _consoleWriteLine), args);
    }

    private int ExecGit(params string[] args) => Exec(_options.TargetFolder,
        ToolLocator.GetToolPath(ExternalTools.Git, _consoleWriteLine), args);


    protected int ExecDotNet(params string[] args) => Exec(_options.TargetFolder,
        Environment.ExpandEnvironmentVariables(@"%ProgramW6432%\dotnet\dotnet.exe"), args);

    protected int Exec(string workingDirectory, string tool, params string[] args)
    {
        var arguments = string.Join(" ", args);
        _consoleWriteLine($"{tool} {arguments}");

        var psi = new ProcessStartInfoEx(workingDirectory, tool, arguments);
        var ph = new ProcessHelper();
        ProcessResult result = ph.RunProcess(psi, _consoleWriteLine);
        return result.ExitCode;
    }
}
