namespace Reqnroll.VisualStudio.Discovery; 
public class ProjectBindingImplementationEqualityComparer : IEqualityComparer<ProjectBindingImplementation>
{
    public bool Equals(ProjectBindingImplementation x, ProjectBindingImplementation y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Method == y.Method &&
               x.ParameterTypes.SequenceEqual(y.ParameterTypes) &&
               Equals(x.SourceLocation, y.SourceLocation);
    }

    public int GetHashCode(ProjectBindingImplementation obj)
    {
        if (obj is null) return 0;

        unchecked // Use unchecked to handle potential integer overflow
        {
            int hash = 17;
            hash = hash * 23 + (obj.Method?.GetHashCode() ?? 0);

            foreach (var paramType in obj.ParameterTypes)
                hash = hash * 23 + (paramType?.GetHashCode() ?? 0);

            hash = hash * 23 + (obj.SourceLocation?.GetHashCode() ?? 0);
            return hash;
        }
    }
}