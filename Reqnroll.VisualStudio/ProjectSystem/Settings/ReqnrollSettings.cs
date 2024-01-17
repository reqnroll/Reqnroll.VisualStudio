#nullable disable
using System;
using System.Linq;

namespace Reqnroll.VisualStudio.ProjectSystem.Settings;

public class ReqnrollSettings
{
    public ReqnrollSettings()
    {
        Traits = ReqnrollProjectTraits.None;
    }

    public ReqnrollSettings(NuGetVersion version, ReqnrollProjectTraits traits, string generatorFolder,
        string configFilePath)
    {
        Version = version;
        Traits = traits;
        GeneratorFolder = generatorFolder;
        ConfigFilePath = configFilePath;
    }

    public NuGetVersion Version { get; set; }
    public ReqnrollProjectTraits Traits { get; set; }
    public string GeneratorFolder { get; set; }
    public string ConfigFilePath { get; set; }
}
