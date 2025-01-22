namespace Reqnroll.VisualStudio.Configuration;

public class BindingDiscoveryConfiguration
{
    public string? ConnectorPath { get; set; } = null;

    private void FixEmptyContainers()
    {
    }

    public void CheckConfiguration()
    {
        FixEmptyContainers();
    }

    #region Equality

    protected bool Equals(BindingDiscoveryConfiguration other)
    {
        return ConnectorPath == other.ConnectorPath;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((BindingDiscoveryConfiguration)obj);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode()
    {
        return (ConnectorPath != null ? ConnectorPath.GetHashCode() : 0);
    }
    // ReSharper restore NonReadonlyMemberInGetHashCode

    #endregion
}