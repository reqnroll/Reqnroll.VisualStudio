using System;

namespace Reqnroll.VisualStudio.Configuration;

public class EditorConfiguration
{
    public bool ShowStepCompletionAfterStepKeywords { get; set; } = true;
    public GherkinFormatConfiguration GherkinFormat { get; set; } = new();

    private void FixEmptyContainers()
    {
        GherkinFormat ??= new GherkinFormatConfiguration();
    }

    public void CheckConfiguration()
    {
        FixEmptyContainers();

        GherkinFormat.CheckConfiguration();
    }

    #region Equality

    protected bool Equals(EditorConfiguration other) =>
        ShowStepCompletionAfterStepKeywords == other.ShowStepCompletionAfterStepKeywords &&
        Equals(GherkinFormat, other.GherkinFormat);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((EditorConfiguration) obj);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode()
    {
        unchecked
        {
            return (ShowStepCompletionAfterStepKeywords.GetHashCode() * 397) ^
                   (GherkinFormat != null ? GherkinFormat.GetHashCode() : 0);
        }
    }
    // ReSharper restore NonReadonlyMemberInGetHashCode

    #endregion
}
