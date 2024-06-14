using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reqnroll.VisualStudio.SpecFlowExtensionDetection
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("gherkin")]
    public class SpecFlowExtensionDetectionClassifierProvider : IClassifierProvider
    {
        private readonly SpecFlowExtensionDetectionService _detectionService;

        public SpecFlowExtensionDetectionClassifierProvider(SpecFlowExtensionDetectionService detectionService)
        {
            _detectionService = detectionService;
        }
        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            _detectionService.CheckForSpecFlowExtension();
            return null;
        }
    }
}
