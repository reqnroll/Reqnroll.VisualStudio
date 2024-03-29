#nullable disable

namespace Reqnroll.VisualStudio.Configuration;

public class ReqnrollConfiguration
{
    public bool? IsReqnrollProject { get; set; }

    public string Version { get; set; }
    public string ConfigFilePath { get; set; }
    public ReqnrollProjectTraits[] Traits { get; set; } = new ReqnrollProjectTraits[0];

    private void FixEmptyContainers()
    {
        Traits = Traits ?? new ReqnrollProjectTraits[0];
    }

    public void CheckConfiguration()
    {
        FixEmptyContainers();

        if (Version != null && !Regex.IsMatch(Version, @"^(?:\.?[0-9]+){2,}(?:\-[\-a-z0-9]*)?$"))
            throw new DeveroomConfigurationException("'reqnroll/version' was not in a correct format");
    }

    #region Equality

    protected bool Equals(ReqnrollConfiguration other) => IsReqnrollProject == other.IsReqnrollProject &&
                                                          string.Equals(Version, other.Version) &&
                                                          string.Equals(ConfigFilePath, other.ConfigFilePath) &&
                                                          Equals(Traits, other.Traits);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ReqnrollConfiguration) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = IsReqnrollProject.GetHashCode();
            hashCode = (hashCode * 397) ^ (Version != null ? Version.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ConfigFilePath != null ? ConfigFilePath.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Traits != null ? Traits.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
