namespace Reqnroll.VisualStudio.Tests.Diagnostics;

[CollectionDefinition(Name, DisableParallelization = true)]
public class NonParallelTestCollectionDefinition
{
    public const string Name = "NonParallelTestCollection";
}
