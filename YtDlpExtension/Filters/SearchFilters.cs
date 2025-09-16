using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace YtDlpExtension
{
    internal sealed partial class SearchFilters : Filters
    {

        internal const string DefaultCookiesFilterId = "no-cookies";
        private readonly IFilterItem[] _filterItems = [
                new Filter() { Id = "no-cookies", Name = "Search", Icon = new IconInfo("🔎") },
                new Filter() { Id = "with-cookies", Name = "With Cookies", Icon = new IconInfo("🍪")},
            ];

        public SearchFilters(string filterId = DefaultCookiesFilterId)
        {
            CurrentFilterId = filterId;
        }

        public override IFilterItem[] GetFilters() => _filterItems;
    }
}
