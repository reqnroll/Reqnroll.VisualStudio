using System.Linq;

namespace Reqnroll.VisualStudio.Wizards.Infrastructure
{
    public class NewProjectMetaData
    {
        public bool IsFallback;
        public readonly IEnumerable<string> TestFrameworks;
        public readonly IEnumerable<string> DotNetFrameworks;
        public readonly DotNetFrameworkInfo[] DotNetFrameworksMetadata;
        public readonly string TestFrameworkDefault;
        public readonly string DotNetFrameworkDefault;
        public readonly IDictionary<string, string> DotNetFrameworkNameToTagMap;
        public readonly IDictionary<string, FrameworkInfo> TestFrameworkMetaData;

        public NewProjectMetaData(NewProjectMetaRecord retrievedData, bool isFallback = false)
        {
            TestFrameworks = retrievedData.TestFrameworks.Select(tf => tf.Label).ToList();
            DotNetFrameworks = retrievedData.DotNetFrameworks.Select(dn => dn.Label).ToList();
            TestFrameworkDefault = retrievedData.TestFrameworks.First().Tag;
            DotNetFrameworkDefault = retrievedData.DotNetFrameworks.First(dn => dn.Default == true).Tag;
            DotNetFrameworkNameToTagMap = retrievedData.DotNetFrameworks
                .ToDictionary(d => d.Label, d => d.Tag);
            TestFrameworkMetaData = retrievedData.TestFrameworks
                .ToDictionary(tf => tf.Tag, tf => tf);
            DotNetFrameworksMetadata = retrievedData.DotNetFrameworks.ToArray();
            IsFallback = isFallback;
        }
    }
}