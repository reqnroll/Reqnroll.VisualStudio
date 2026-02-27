namespace Reqnroll.VisualStudio.Connector.Protocol
{
    /// <summary>
    /// Well-known capability identifiers reported during service registration.
    /// </summary>
    public static class Capabilities
    {
        public const string BindingDiscovery = "discovery/discoverBindings";
        public const string Reload = "lifecycle/reload";
        public const string Shutdown = "lifecycle/shutdown";
        // Future capabilities:
        // public const string StepGeneration = "generation/stepSnippet";
        // public const string Formatting     = "formatting/gherkin";
    }
}
