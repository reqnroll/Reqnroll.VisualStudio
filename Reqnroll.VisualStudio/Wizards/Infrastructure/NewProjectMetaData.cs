using System.Linq;

namespace Reqnroll.VisualStudio.Wizards.Infrastructure
{
    public class NewProjectMetaData
    {
        public bool IsFallback;
        public readonly DotNetFrameworkInfo[] DotNetFrameworksMetadata;
        public readonly string TestFrameworkDefault;
        public readonly string DotNetFrameworkDefault;
        public readonly IDictionary<string, FrameworkInfo> TestFrameworkMetaData;

        public NewProjectMetaData(NewProjectMetaRecord retrievedData, bool isFallback = false)
        {
            TestFrameworkDefault = retrievedData.TestFrameworks.First().Tag;
            DotNetFrameworkDefault = retrievedData.DotNetFrameworks.First(dn => dn.Default == true).Tag;
            TestFrameworkMetaData = retrievedData.TestFrameworks
                .ToDictionary(tf => tf.Tag, tf => tf);
            DotNetFrameworksMetadata = retrievedData.DotNetFrameworks.ToArray();
            IsFallback = isFallback;
        }
    }
}