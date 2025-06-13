using System.Linq;

namespace Reqnroll.VisualStudio.Wizards.Infrastructure
{
    public class NewProjectMetaData
    {
        public readonly IEnumerable<string> TestFrameworks;
        public readonly IEnumerable<string> DotNetFrameworks;
        public readonly string TestFrameworkDefault;
        public readonly string DotNetFrameworkDefault;
        public readonly IDictionary<string, string> DotNetFrameworkNameToTagMap;
        public readonly IDictionary<string, FrameworkInfo> TestFrameworkMetaData;

        public NewProjectMetaData(NewProjectMetaRecord retrievedData)
        {
            TestFrameworks = retrievedData.TestFrameworks.Select(tf => tf.Label).ToList();
            DotNetFrameworks = retrievedData.DotNetFrameworks.Select(dn => dn.Label).ToList();
            TestFrameworkDefault = TestFrameworks.First();
            DotNetFrameworkDefault = retrievedData.DotNetFrameworks.First(dn => dn.Default == true).Label;
            DotNetFrameworkNameToTagMap = retrievedData.DotNetFrameworks
                .ToDictionary(d => d.Label, d => d.Tag);
            TestFrameworkMetaData = retrievedData.TestFrameworks
                .ToDictionary(tf => tf.Label, tf => tf); 
        }
    }
}