#nullable disable
namespace SpecFlow.VisualStudio.SpecFlowConnector.Models;

public class StepDefinition
{
    public string Type { get; set; }
    public string Regex { get; set; }
    public string Method { get; set; }
    public string ParamTypes { get; set; }
    public StepScope Scope { get; set; }

    public string Expression { get; set; }
    public string Error { get; set; }

    public string SourceLocation { get; set; }

    #region Equality

    protected bool Equals(StepDefinition other) => Type == other.Type && Regex == other.Regex &&
                                                   Method == other.Method && ParamTypes == other.ParamTypes &&
                                                   Equals(Scope, other.Scope) && Expression == other.Expression &&
                                                   Error == other.Error && SourceLocation == other.SourceLocation;

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((StepDefinition) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Type != null ? Type.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (Regex != null ? Regex.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Method != null ? Method.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (ParamTypes != null ? ParamTypes.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Scope != null ? Scope.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Expression != null ? Expression.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Error != null ? Error.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (SourceLocation != null ? SourceLocation.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
