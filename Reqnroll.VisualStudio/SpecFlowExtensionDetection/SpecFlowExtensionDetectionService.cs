using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reqnroll.VisualStudio.SpecFlowExtensionDetection
{
    [Export]
    public class SpecFlowExtensionDetectionService
    {
        private readonly IIdeScope _ideScope;
        private bool _extensionExistenceChecked = false;
        private bool _compatibilityAlertShown = false;

        [ImportingConstructor]
        public SpecFlowExtensionDetectionService(IIdeScope ideScope)
        {
            _ideScope = ideScope;
        }

        public void CheckForSpecFlowExtensionOnce()
        {
            if (_extensionExistenceChecked)
                return;

            CheckForSpecFlowExtension();
        }

        public void CheckForSpecFlowExtension()
        {
            if (_compatibilityAlertShown)
                return;

            _extensionExistenceChecked = true;
            var specFlowExtensionDetected = AppDomain.CurrentDomain.GetAssemblies().Any(a =>
                a.FullName.StartsWith("SpecFlow.VisualStudio"));

            if (specFlowExtensionDetected && !_compatibilityAlertShown)
            {
                _compatibilityAlertShown = true;
                _ideScope.Actions.ShowProblem(
                                        $"We detected that both the 'Reqnroll for Visual Studio' and the 'SpecFlow for Visual Studio' extension have been installed in this Visual Studio instance.{Environment.NewLine}For the proper behavior you need to uninstall or disable one of these extensions in the 'Extensions / Manage Extensions' dialog.");

            }
        }
    }
}
