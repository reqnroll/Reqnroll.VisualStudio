using System;

namespace Reqnroll.SampleProjectGenerator;

public class NewProjectFormatForNetFrameworkProjectGenerator : NewProjectFormatProjectGenerator
{
    public NewProjectFormatForNetFrameworkProjectGenerator(GeneratorOptions options, Action<string> consoleWriteLine) :
        base(options, consoleWriteLine)
    {
    }

    protected override int ExecBuild() => Exec(_options.TargetFolder,
        ToolLocator.GetToolPath(ExternalTools.MsBuild, _consoleWriteLine));
}
