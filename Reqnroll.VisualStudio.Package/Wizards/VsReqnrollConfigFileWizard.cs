using Reqnroll.VisualStudio.Wizards.Infrastructure;

namespace Reqnroll.VisualStudio.Wizards;

public class VsReqnrollConfigFileWizard : VsProjectScopeWizard<ReqnrollConfigFileWizard>
{
    protected override ReqnrollConfigFileWizard ResolveWizard(DTE dte) => new();
}
