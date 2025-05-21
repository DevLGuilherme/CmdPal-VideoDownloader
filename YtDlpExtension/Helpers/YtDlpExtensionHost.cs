using Microsoft.CommandPalette.Extensions;

namespace YtDlpExtension.Helpers
{
    public static class YtDlpExtensionHost
    {
        public static IExtensionHost? Instance { get; private set; }

        public static void Register(IExtensionHost host)
        {
            Instance = host;
        }
    }
}
