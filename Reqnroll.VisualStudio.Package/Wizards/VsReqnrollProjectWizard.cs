using System;
using System.Linq;
using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Wizards;

public class VsReqnrollProjectWizard : VsProjectScopeWizard<ReqnrollProjectWizard>
{

    public override bool ShouldAddProjectItem(string filePath)
    {
        if (!base.ShouldAddProjectItem(filePath))
            return false;

        // Exclude ImplicitUsings.cs for .NET Framework projects
        if (filePath.EndsWith("ImplicitUsings.cs", StringComparison.OrdinalIgnoreCase) && ShouldExcludeImplicitUsings())
        {
            return false;
        }

        return true;
    }

    private bool ShouldExcludeImplicitUsings()
    {
        if (_wizardRunParameters?.ReplacementsDictionary == null)
            return false;

        var isNetFramework = _wizardRunParameters.ReplacementsDictionary.TryGetValue(WizardRunParameters.IsNetFrameworkKey, out var isNetFx) 
            && bool.Parse(isNetFx);

        // ImplicitUsings only makes sense for C# 10+ (.NET 6+)
        // For .NET Framework or older SDKs, remove it
        return isNetFramework;
    }
}
