using System;

namespace Reqnroll.SampleProjectGenerator;

public class NewProjectFormatProjectGenerator : ProjectGenerator
{
    public NewProjectFormatProjectGenerator(GeneratorOptions options, Action<string> consoleWriteLine) : base(options,
        consoleWriteLine)
    {
    }

    public override string PackagesFolder => GetPackagesFolder();

    public override string GetOutputAssemblyPath(string config = "Debug")
        => Path.Combine("bin", config, _options.TargetFramework, AssemblyFileName);

    protected override ProjectChanger CreateProjectChanger(string projectFilePath) =>
        new NewProjectFormatProjectChanger(projectFilePath);

    protected override string GetTemplatesFolder() => @"Templates\CS-NEW";

    protected override string GetPackagesFolder() =>
        Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\.nuget\packages");

    protected override void InstallReqnrollPackages(string packagesFolder, ProjectChanger projectChanger)
    {
        //nop
    }

    protected override void SetReqnrollUnitTestProvider(ProjectChanger projectChanger, string packagesFolder)
    {
        InstallNuGetPackage(projectChanger, packagesFolder, $"Reqnroll.{_options.UnitTestProvider}", "net462",
            _options.ReqnrollPackageVersion);
    }

    protected override void BuildProject()
    {
        var args = new List<string> { "restore" };
        if (!string.IsNullOrWhiteSpace(_options.FallbackNuGetPackageSource)) {
            args.Add("-s");
            args.Add($"\"https://api.nuget.org/v3/index.json;{_options.FallbackNuGetPackageSource}\"");
        }
    
        var exitCode = ExecDotNet(args.ToArray()).ExitCode;
        if (exitCode != 0)
        {
            _consoleWriteLine($"dotnet restore exit code: {exitCode}");
            throw new Exception($"dotnet restore failed with exit code {exitCode}");
        }

        base.BuildProject();
    }

    protected override int ExecBuild() => ExecDotNet("build", "--no-restore").ExitCode;
}
