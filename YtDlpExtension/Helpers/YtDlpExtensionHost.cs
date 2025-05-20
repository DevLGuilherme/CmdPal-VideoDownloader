using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
