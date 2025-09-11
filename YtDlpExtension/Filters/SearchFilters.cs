using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace YtDlpExtension
{
    internal sealed partial class SearchFilters : Filters
    {
        public SearchFilters()
        {
            CurrentFilterId = "no-cookies";
        }


        public override IFilterItem[] GetFilters()
        {
            CurrentFilterId = "no-cookies";

            return [
                new Filter() { Id = "no-cookies", Name = "Search", Icon = new IconInfo("🔎") },
                new Filter() { Id = "with-cookies", Name = "With Cookies", Icon = new IconInfo("🍪")},
            ];
        }
    }
}
