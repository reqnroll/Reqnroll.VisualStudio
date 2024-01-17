#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.Models;

public class StepScope
{
    public string Tag { get; set; }
    public string FeatureTitle { get; set; }
    public string ScenarioTitle { get; set; }

    #region Equality

    protected bool Equals(StepScope other) => string.Equals(Tag, other.Tag) &&
                                              string.Equals(FeatureTitle, other.FeatureTitle) &&
                                              string.Equals(ScenarioTitle, other.ScenarioTitle);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((StepScope) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Tag != null ? Tag.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (FeatureTitle != null ? FeatureTitle.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ScenarioTitle != null ? ScenarioTitle.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
