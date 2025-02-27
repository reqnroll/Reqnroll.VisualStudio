namespace Reqnroll.VisualStudio.Tests.Diagnostics;

[CollectionDefinition(Name, DisableParallelization = true)]
public class NonParallelTestCollectionDefinition
{
    internal const string Name = "NonParallelTestCollection";
}
