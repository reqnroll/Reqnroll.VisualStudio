#nullable disable

namespace Reqnroll.VisualStudio.Configuration;

public class DeveroomConfiguration
{
    public DateTimeOffset ConfigurationChangeTime { get; set; } = DateTimeOffset.MinValue;

    public string ConfigurationBaseFolder { get; set; }

    public ReqnrollConfiguration Reqnroll { get; set; } = new();
    public TraceabilityConfiguration Traceability { get; set; } = new();
    public EditorConfiguration Editor { get; set; } = new();

    // old settings to be reviewed
    public ProcessorArchitectureSetting ProcessorArchitecture { get; set; } = ProcessorArchitectureSetting.AutoDetect;
    public bool DebugConnector { get; set; }
    public string DefaultFeatureLanguage { get; set; } = "en-US";
    public string ConfiguredBindingCulture { get; set; } = null;
    public string BindingCulture => ConfiguredBindingCulture ?? DefaultFeatureLanguage;

    private void FixEmptyContainers()
    {
        Reqnroll ??= new ReqnrollConfiguration();
        Traceability ??= new TraceabilityConfiguration();
        Editor ??= new EditorConfiguration();
    }

    public void CheckConfiguration()
    {
        FixEmptyContainers();

        Reqnroll.CheckConfiguration();
        Traceability.CheckConfiguration();
        Editor.CheckConfiguration();
    }

    #region Equality

    protected bool Equals(DeveroomConfiguration other) =>
        string.Equals(ConfigurationBaseFolder, other.ConfigurationBaseFolder) && Equals(Reqnroll, other.Reqnroll) &&
        Equals(Traceability, other.Traceability) && Equals(Editor, other.Editor) &&
        ProcessorArchitecture == other.ProcessorArchitecture && DebugConnector == other.DebugConnector &&
        string.Equals(DefaultFeatureLanguage, other.DefaultFeatureLanguage) &&
        string.Equals(ConfiguredBindingCulture, other.ConfiguredBindingCulture);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((DeveroomConfiguration) obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ConfigurationBaseFolder != null ? ConfigurationBaseFolder.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (Reqnroll != null ? Reqnroll.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Traceability != null ? Traceability.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Editor != null ? Editor.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int) ProcessorArchitecture;
            hashCode = (hashCode * 397) ^ DebugConnector.GetHashCode();
            hashCode = (hashCode * 397) ^ (DefaultFeatureLanguage != null ? DefaultFeatureLanguage.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^
                       (ConfiguredBindingCulture != null ? ConfiguredBindingCulture.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
