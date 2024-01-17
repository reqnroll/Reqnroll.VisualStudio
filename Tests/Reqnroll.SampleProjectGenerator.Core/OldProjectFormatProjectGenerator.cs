using System;

namespace Reqnroll.SampleProjectGenerator;

public class OldProjectFormatProjectGenerator : ProjectGenerator
{
    public OldProjectFormatProjectGenerator(GeneratorOptions options, Action<string> consoleWriteLine) : base(options,
        consoleWriteLine)
    {
    }

    public override string GetOutputAssemblyPath(string config = "Debug")
        => Path.Combine("bin", config, AssemblyFileName);

    protected override ProjectChanger CreateProjectChanger(string projectFilePath) =>
        new OldProjectFormatProjectChanger(projectFilePath);

    protected override string GetTemplatesFolder() => @"Templates\CS-OLD";

    protected override string GetPackagesFolder() => Path.Combine(_options.TargetFolder, "packages");

    protected override int ExecBuild() => Exec(_options.TargetFolder,
        ToolLocator.GetToolPath(ExternalTools.MsBuild, _consoleWriteLine));
}
