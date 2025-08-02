using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;

namespace YtDlpExtension
{
    public static class ResourceExtensions
    {
        private static readonly ResourceLoader? _resourceLoader = TryCreateLoader();

        private static ResourceLoader? TryCreateLoader()
        {
            try { return new ResourceLoader(); } catch { return null; }
        }

        public static string ToLocalized(this string resourceKey) =>
            _resourceLoader?.GetString(resourceKey) ?? resourceKey;

        public static string ToLocalized(this string resourceKey, params object[] args) =>
            string.Format(CultureInfo.CurrentCulture, resourceKey.ToLocalized(), args);
    }
}
