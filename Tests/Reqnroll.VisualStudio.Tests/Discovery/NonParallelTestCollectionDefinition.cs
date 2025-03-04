namespace Reqnroll.VisualStudio.Tests.Discovery;

[CollectionDefinition(Name, DisableParallelization = true)]
public class NonParallelTestCollectionDefinition
{
    internal const string Name = "NonParallelTestCollection";
}
