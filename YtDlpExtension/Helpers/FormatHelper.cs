using System;
using System.Globalization;
using System.Linq;
using YtDlpExtension.Metada;
using YtDlpExtension.Pages;

namespace YtDlpExtension.Helpers
{
    public class FormatHelper
    {
        public static bool IsAudio(VideoFormatListItem item)
        {
            return item.Title.Contains("audio", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVideo(VideoFormatListItem item)
        {
            return !IsAudio(item);
        }

        public static Format[] OrderByResolution(Format[]? formats)
        {
            return formats?
                .OrderByDescending(format => format.Height)
                .ToArray() ?? [];
        }

        public static Format[] OrderByResolutionDistinct(Format[]? formats)
        {
            return formats == null ? Array.Empty<Format>() :
                formats
                .Where(f => f.Height != null)
                .OrderByDescending(f => f.Height)
                .ThenByDescending(f => f.TBR)
                .GroupBy(f => f.Height)
                .Select(g => g.First())
                .ToArray();
        }


        public static string TryGetNativeName(string code)
        {
            string Fallback(string c) =>
                c.Equals("live_chat", StringComparison.OrdinalIgnoreCase) ? "Live Chat Replay" : c;
            try
            {
                if (CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Any(c => c.Name.Equals(code, StringComparison.OrdinalIgnoreCase)))
                {
                    return new CultureInfo(code).NativeName;
                }
                else
                {
                    return code.Equals("live_chat", StringComparison.OrdinalIgnoreCase)
                        ? "Live Chat Replay"
                        : code;
                }
            }
            catch
            {
                // Ignored
            }

            return Fallback(code);
        }
    }
}
