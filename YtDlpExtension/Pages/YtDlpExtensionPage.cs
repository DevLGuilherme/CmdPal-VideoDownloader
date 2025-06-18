// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YtDlpExtension.Helpers;
using YtDlpExtension.Metada;
using YtDlpExtension.Pages;

namespace YtDlpExtension;

internal sealed partial class YtDlpExtensionPage : DynamicListPage
{
    private List<VideoFormatListItem> _itens = new();
    private List<VideoFormatListItem> _fallbackItems = new();
    private DownloadHelper _ytDlp;
    IconInfo _ytDlpIcon = IconHelpers.FromRelativePath("Assets\\CmdPal-YtDlp.png");
    private readonly SettingsManager _settingsManager;

    public List<VideoFormatListItem> GetActiveDownloads()
    {
        return _itens.Where(item => item.GetDownloadState == DownloadState.Downloading).ToList();
    }

    public YtDlpExtensionPage(SettingsManager settingsManager, DownloadHelper ytDlp)
    {
        _settingsManager = settingsManager;
        _ytDlp = ytDlp;
        Icon = _ytDlpIcon;
        Title = "yt-dlp-extension";
        Name = "Open";
        PlaceholderText = "PlaceholderText".ToLocalized();
        ShowDetails = true;
        EmptyContent = new CommandItem(new NoOpCommand())
        {
            Icon = _ytDlpIcon,
            Title = "EmptyContentTiltle".ToLocalized(),
            MoreCommands = [new CommandContextItem(
                    "Open folder",
                    "Open selected download folder",
                    "Open folder",
                    () => {
                        var psi = new ProcessStartInfo();
                        psi.FileName = @"c:\windows\explorer.exe";
                        psi.Arguments = _settingsManager.DownloadLocation;
                        using var process = Process.Start(psi);
                        process?.WaitForExit();
                    },
                    CommandResult.KeepOpen()
                ){

            }]
        };
        _ytDlp.TitleUpdated += title => Title = title;
        _ytDlp.LoadingChanged += loading => IsLoading = loading;
        _ytDlp.ItemsChanged += count => RaiseItemsChanged(count);
        _ytDlp.RequestActiveDownloads += GetActiveDownloads;
        UpdateSearchText(string.Empty, string.Empty);
    }

    public override async void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch)
            return;

        var oldAudioOnly = oldSearch is { Length: > 0 } oldQuery && oldQuery.StartsWith('@');
        var newAudioOnly = newSearch is { Length: > 0 } newQuery && newQuery.StartsWith('@');

        var oldTrimmed = oldSearch.TrimStart('@');
        var newTrimmed = newSearch.TrimStart('@');

        if (_fallbackItems == null || _fallbackItems.Count == 0)
        {
            // First Search or empty list — do a full search
            await UpdateListAsync(newSearch);
            return;
        }

        var quickMergeRegex = new Regex(@"^([\w\-]+)(?:\+([\w\-]+))?@(.+)$");

        var match = quickMergeRegex.Match(newSearch);
        if (match.Success)
        {
            var videoFormat = match.Groups[1].Value;
            var audioFormat = match.Groups[2].Value;
            var url = match.Groups[3].Value;

            ApplyLocalFormatFilter(videoFormat, audioFormat);
            return;
        }

        if (oldAudioOnly != newAudioOnly && (newTrimmed == oldTrimmed || newTrimmed == ""))
        {
            // If an @ is typed or removed at the start of the string but the url remains the same
            ApplyLocalFilter(newAudioOnly);
            return;
        }

        if (newAudioOnly && newTrimmed == oldTrimmed)
        {
            // If the base text remains the same, apply local filter
            ApplyLocalFilter(true);
            return;
        }



        // If the url is completely different then do a new search
        await UpdateListAsync(newSearch);
    }

    private void ApplyLocalFilter(bool audioOnly)
    {
        if (audioOnly)
        {
            _itens = _fallbackItems.Where(item => item.Title == "audio only").ToList();
        }
        else
        {
            _itens = _fallbackItems.ToList();
        }
        RaiseItemsChanged(_itens.Count);
    }

    private void ApplyLocalFormatFilter(string videoFormat, string audioFormat)
    {
        List<VideoFormatListItem> FilterFormatCandidates(string format, bool audioOnly)
        {
            var candidates = _fallbackItems
                .Where(item => (audioOnly ? item.Title == "audio only" : item.Title != "audio only") &&
                               item.Tags.Any(tag => tag.Text.Contains(format, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Se houver exatamente 2 candidatos e um deles for exato, retorna só o exato
            if (candidates.Count == 2)
            {
                var exact = candidates.FirstOrDefault(item =>
                    item.Tags.Any(tag => tag.Text.Equals(format, StringComparison.OrdinalIgnoreCase)));
                if (exact != null)
                    return new List<VideoFormatListItem> { exact };
            }

            return candidates;
        }

        if (string.IsNullOrEmpty(audioFormat))
        {
            var videoCandidates = FilterFormatCandidates(videoFormat, audioOnly: false);
            var audioCandidates = _fallbackItems
                .Where(item => item.Title == "audio only")
                .ToList();
            _itens = videoCandidates.Concat(audioCandidates).ToList();
        }
        else
        {
            var videoCandidates = FilterFormatCandidates(videoFormat, audioOnly: false);
            var audioCandidates = FilterFormatCandidates(audioFormat, audioOnly: true);

            var result = new List<VideoFormatListItem>();
            if (videoCandidates.Count > 0)
                result.AddRange(videoCandidates);
            if (audioCandidates.Count > 0)
                result.AddRange(audioCandidates);
            _itens = result;
        }
        IContextItem[] _fallbackcommands;
        if (_itens.Count > 0 && _itens.Count <= 2)
        {
            foreach (var item in _itens)
            {
                _fallbackcommands = item.MoreCommands;
                var commands = _fallbackcommands.ToList();
                commands.Insert(0, new CommandContextItem(
                    "QuickMerge",
                    "QuickMerge",
                    "QuickMerge",
                    action: null,
                    result: CommandResult.KeepOpen()
                )
                {
                    RequestedShortcut = KeyChordHelpers.FromModifiers(true, false, false, false, Windows.System.VirtualKey.M, 0),
                    Icon = new IconInfo("\uE71B"),
                });
                item.MoreCommands = commands.ToArray();
            }
            RaiseItemsChanged(2);
        }
        RaiseItemsChanged(_itens.Count);
    }


    private async Task UpdateListAsync(string queryText)
    {
        _itens.Clear();
        var audioOnlyQuery = queryText.StartsWith("@", StringComparison.OrdinalIgnoreCase);
        var queryURL = audioOnlyQuery ? queryText.Split("@")[1] : queryText;
        if (!DownloadHelper.IsValidUrl(queryURL))
        {
            IsLoading = false;
            return;
        }

        IsLoading = true;
        var isPlaylist = queryText.Contains("playlist") || queryText.Contains("list=");
        var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync(queryURL);

        if (string.IsNullOrEmpty(jsonResult))
        {
            HandleError("EmptyDataYtDlp".ToLocalized());
            return;
        }

        try
        {
            //JSON Cleaning
            var jsonOutput = Regex.Replace(jsonResult, @"\""\s\""\s*:\s*""[^""]*"",?", "");
            //JSON Parse

            VideoData videoData;

            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                //Get Video Title
                string videoTitle = videoData?.Title ?? "MissingTitle".ToLocalized();
                //Order formats by resolution, from highest to lowest
                var formatsOrdered = _settingsManager.GetSelectedMode == "simple" ?
                                        FormatHelper.OrderByResolutionDistinct(videoData?.Formats) :
                                        FormatHelper.OrderByResolution(videoData?.Formats);
                string thumbnail = videoData?.Thumbnail ?? "MissingThumb".ToLocalized();
                if (videoData?.Formats == null && videoData != null)
                {
                    JObject videoInfo = JObject.Parse(jsonOutput);
                    _itens.Add(new VideoFormatListItem(queryURL, videoTitle, thumbnail, videoData, _ytDlp, _settingsManager));
                    RaiseItemsChanged(1);
                    return;
                }


                if (isPlaylist)
                {
                    var playlistData = new JObject()
                    {
                        ["title"] = videoTitle,
                        ["thumbnail"] = thumbnail,
                        ["videoURL"] = queryURL
                    };
                    //Title = "FetchingPlaylistTitle".ToLocalized();
                    //_ytDlp._downloadBanner.UpdateState(DownloadState.CustomMessage, "FetchingPlaylistTitle".ToLocalized(), true);
                    //_ytDlp._downloadBanner.ShowStatus();
                    IsLoading = true;
                    //The form page will be set after the data from the playlist is fetched
                    var listItem = new VideoFormatListItem(new NoOpCommand())
                    {
                        Tags = [new Tag("Playlist")],
                        Icon = new IconInfo("\uE895"),
                        Title = "FetchingPlaylistTitle".ToLocalized(),
                        Subtitle = "FetchingPlaylistDescription".ToLocalized()
                    };
                    _itens.Insert(0, listItem);
                    RaiseItemsChanged(1);
                    _ = _ytDlp.ExtractPlaylistDataAsync(queryURL, onFinish: (playlistDetails) =>
                    {
                        var playlistTitle = playlistDetails["playlistTitle"]?.ToString() ?? string.Empty;
                        var playlistCount = playlistDetails["playlistCount"]?.ToString() ?? string.Empty;
                        playlistData.Add("playlistTitle", playlistTitle);
                        playlistData.Add("downloadPath", Path.Combine(_settingsManager.DownloadLocation, playlistTitle));
                        playlistData.Add("playlistCount", playlistCount);
                        // The command to go to the playlist form is declared here
                        // in order to update the command once the download starts
                        Command goToPlaylistDownloadForm = new NoOpCommand();

                        // This callback will be invoked once the download starts
                        // And will update the command to cancel the playlist download
                        Action<CancellationTokenSource> onSubmitForm = (cancellationToken) =>
                        {
                            listItem.Command = new AnonymousCommand(() =>
                            {
                                // Once the download is cancelled
                                // The previous command should take place of the cancel command 
                                // The form needs to be available again
                                cancellationToken.Cancel();
                                listItem.Command = goToPlaylistDownloadForm;
                                listItem.Title = "PlaylistFetchedTitle".ToLocalized();
                                listItem.Subtitle = "PlaylistFetchedDescription".ToLocalized();
                                listItem.Icon = new IconInfo("\uE90B");
                            })
                            {
                                Name = "CancelDownload".ToLocalized(),
                                Icon = new IconInfo("\uE711"),
                                Result = CommandResult.KeepOpen()
                            };
                        };

                        // The actual form is constructed here passing the callback declared previously
                        /// This is currently needed because <see cref="CommandResult.GoToPage"/>  is not implemented yet
                        // Needs future refactoring
                        goToPlaylistDownloadForm = new PlaylistFormPage(_settingsManager, playlistData, _ytDlp, onSubmit: onSubmitForm);

                        listItem.Command = goToPlaylistDownloadForm;
                        listItem.Title = "PlaylistFetchedTitle".ToLocalized();
                        listItem.Subtitle = "PlaylistFetchedDescription".ToLocalized();
                        listItem.Icon = new IconInfo("\uE90B");
                        //Title = "PlaylistFetchedTitle".ToLocalized();
                        RaiseItemsChanged(1);
                        //_ytDlp._downloadBanner.Hide();
                    });


                }

                CommandContextItem? listSubtitlesCommand = null;
                CommandContextItem? listAutoCaptionsCommand = null;


                if (videoData?.Subtitles?.Count > 0)
                {
                    listSubtitlesCommand = new CommandContextItem(
                        new SubtitlesPage(queryURL, videoData.Subtitles, false, _ytDlp, _settingsManager))
                    {
                        Icon = new IconInfo("\uf15f"),
                        Title = "ListSubtitles".ToLocalized(),
                    };
                }
                if (videoData?.AutomaticCaptions?.Count > 0)
                {
                    listAutoCaptionsCommand = new CommandContextItem(
                        new SubtitlesPage(queryURL, videoData.AutomaticCaptions, true, _ytDlp, _settingsManager))
                    {
                        Icon = new IconInfo("\ued0c"),
                        Title = "ListAutoCaptions".ToLocalized(),
                    };
                }

                var uiSettings = new UISettings();
                var accentColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
                var foregroundColor = uiSettings.GetColorValue(UIColorType.Foreground);

                foreach (var format in formatsOrdered)
                {
                    var formatObject = format;

                    if (formatObject != null)
                    {
                        var formatListItem = new VideoFormatListItem(queryURL, videoTitle, thumbnail, formatObject, _ytDlp, _settingsManager);
                        var moreCommands = formatListItem.MoreCommands.ToList();

                        var trimVideoForm = new TrimVideoFormPage(queryURL, _settingsManager, videoData, format, _ytDlp);
                        var trimVideoCommand = new CommandContextItem(trimVideoForm)
                        {
                            Icon = new IconInfo("\ue8c6"),
                            Title = "TrimVideo".ToLocalized(),
                        };
                        moreCommands.Add(trimVideoCommand);

                        if (listSubtitlesCommand != null)
                            moreCommands.Add(listSubtitlesCommand);

                        if (listAutoCaptionsCommand != null)
                            moreCommands.Add(listAutoCaptionsCommand);

                        formatListItem.MoreCommands = moreCommands.ToArray();

                        _itens.Add(formatListItem);
                    }
                }
                _fallbackItems = _itens.ToList();
                if (audioOnlyQuery)
                {
                    _itens = _fallbackItems.Where(item => item.Title == "audio only").ToList();
                }
                RaiseItemsChanged(_itens.Count);
                IsLoading = false;
            }
            catch (System.Exception ex)
            {
                HandleError(ex.Message);
                IsLoading = false;
                return;
            }

        }
        catch (System.Exception ex)
        {
            IsLoading = false;
            EmptyContent = new CommandItem(new NoOpCommand())
            {
                Icon = new IconInfo("\uE946"),
                Title = "Error",
                Subtitle = ex.Message
            };
        }
        IsLoading = false;
        RaiseItemsChanged(_itens.Count);
    }

    private static JToken[]? FilterAudioOnlyFormats(JArray? formats)
    {
        return formats?.Where(
                        f =>
                            f?["vcodec"]?.ToString() == "none" &&
                            f?["resolution"]?.ToString() == "audio only"
                        ).ToArray();
    }

    private static Format[] OrderByResolution(Format[]? formats)
    {
        return formats?
            .OrderByDescending(format => format.height)
            .ToArray() ?? [];
    }

    private void HandleError(string message)
    {
        IsLoading = false;
        EmptyContent = new CommandItem(new NoOpCommand())
        {
            Icon = new IconInfo("\uE946"),
            Title = "Error",
            Subtitle = message
        };
    }

    public override IListItem[] GetItems() => _itens.ToArray();
}
