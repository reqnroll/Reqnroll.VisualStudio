#nullable disable
namespace Reqnroll.VisualStudio.ReqnrollConnector.Models;

public class Hook
{
    public string Type { get; set; }
    public int? HookOrder { get; set; }
    public string Method { get; set; }
    //public string ParamTypes { get; set; }
    public StepScope Scope { get; set; }

    public string Error { get; set; }

    public string SourceLocation { get; set; }

    #region Equality

    protected bool Equals(Hook other)
    {
        return Type == other.Type && HookOrder == other.HookOrder && Method == other.Method && Equals(Scope, other.Scope) && Error == other.Error && SourceLocation == other.SourceLocation;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Hook)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Type != null ? Type.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ HookOrder.GetHashCode();
            hashCode = (hashCode * 397) ^ (Method != null ? Method.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Scope != null ? Scope.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (SourceLocation != null ? SourceLocation.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
