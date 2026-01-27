#nullable disable
namespace Reqnroll.Bindings.Provider.Data;

public class BindingScopeData
{
    public string Tag { get; set; } // contains leading '@', e.g. '@mytag'
    public string FeatureTitle { get; set; }
    public string ScenarioTitle { get; set; }
    public string Error { get; set; }
}
