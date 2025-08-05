using MeowBrowserExtern;

namespace MeowBrowser
{
    public class PluginStatus
    {
        public string? Name { get; set; }
        public bool Loaded { get; set; }
        public bool Unloaded { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public IMeowPlugin? Instance { get; set; }
    }
}