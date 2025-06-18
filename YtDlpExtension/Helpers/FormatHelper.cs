using System;
using System.Linq;
using YtDlpExtension.Metada;
using YtDlpExtension.Pages;

namespace YtDlpExtension.Helpers
{
    public class FormatHelper
    {
        public static bool IsAudio(VideoFormatListItem item)
        {
            return item.Title.Contains("audio", StringComparison.OrdinalIgnoreCase)
                || item.Tags.Any(tag => tag.Text.Contains("audio", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsVideo(VideoFormatListItem item)
        {
            return !IsAudio(item);
        }

        public static Format[] OrderByResolution(Format[]? formats)
        {
            return formats?
                .OrderByDescending(format => format.height)
                .ToArray() ?? [];
        }

        public static Format[] OrderByResolutionDistinct(Format[]? formats)
        {
            return formats == null ? Array.Empty<Format>() :
                formats
                .Where(f => f.height != null)
                .OrderByDescending(f => f.height)
                .ThenByDescending(f => f.tbr)
                .GroupBy(f => f.height)
                .Select(g => g.First())
                .ToArray();
        }
    }
}
