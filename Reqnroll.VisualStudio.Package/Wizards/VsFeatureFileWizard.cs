using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Wizards;

public class VsFeatureFileWizard : VsSimulatedItemAddProjectScopeWizard<FeatureFileWizard>
{
    protected override FeatureFileWizard ResolveWizard(DTE dte) => new();
}
