using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;

namespace YtDlpExtension
{
    public static class ResourceExtensions
    {
        private static readonly ResourceLoader _resourceLoader = new();

        public static string ToLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);
        public static string ToLocalized(this string resourceKey, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, _resourceLoader.GetString(resourceKey), args);
    }
}
