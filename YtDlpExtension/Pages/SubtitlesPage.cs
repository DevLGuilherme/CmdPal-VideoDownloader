using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using YtDlpExtension.Helpers;
using YtDlpExtension.Metada;

namespace YtDlpExtension.Pages
{
    public partial class SubtitlesPage : ListPage
    {
        private List<ListItem> _items = new();
        private readonly Subtitle _subtitles = new();
        private readonly DownloadHelper _ytDlp;
        private readonly SettingsManager _settings;
        private readonly string _videoUrl = string.Empty;
        private readonly bool _isAutoCaptions;
        public SubtitlesPage(string queryUrl, Subtitle subtitles, bool isAutoCaption, DownloadHelper ytDlp, SettingsManager settings)
        {
            _subtitles = subtitles;
            _ytDlp = ytDlp;
            _settings = settings;
            _videoUrl = queryUrl;
            _isAutoCaptions = isAutoCaption;
            Icon = new IconInfo("\uf15f");
            Name = "ListAutoCaptions".ToLocalized();
        }

        public override IListItem[] GetItems()
        {
            if (string.IsNullOrEmpty(_videoUrl) || _subtitles == null || !_subtitles.Any())
            {
                _items.Clear();
                return Array.Empty<IListItem>();
            }

            if (_items.Count == _subtitles.Count)
                return _items.ToArray();

            _items.Clear();
            foreach (var subtitle in _subtitles)
            {
                string title = FormatHelper.TryGetNativeName(subtitle.Key);

                var key = subtitle.Key;
                _items.Add(new ListItem(new AnonymousCommand(async () =>
                {
                    var downloadBanner = new StatusMessage();
                    await _ytDlp.TryExecuteSubtitleDownloadAsync(_videoUrl, key, downloadBanner, _settings.DownloadLocation, _isAutoCaptions);
                })
                {
                    Name = "Download".ToLocalized(),
                    Result = CommandResult.KeepOpen()
                })
                {
                    Title = title,
                    Icon = new IconInfo("\uf15f"),
                });
            }
            return _items.ToArray();
        }
    }
}
