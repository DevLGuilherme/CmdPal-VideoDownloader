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
using Windows.UI.ViewManagement;
using YtDlpExtension.Helpers;
using YtDlpExtension.Metada;
using YtDlpExtension.Pages;

namespace YtDlpExtension;

internal sealed partial class YtDlpExtensionPage : DynamicListPage
{
    private List<VideoFormatListItem> _itens = new();
    private List<VideoFormatListItem> _fallbackItems = new();
    private List<VideoFormatListItem> _itemsToMerge = new();
    private DownloadHelper _ytDlp;
    IconInfo _ytDlpIcon = IconHelpers.FromRelativePath("Assets\\CmdPal-YtDlp.png");
    private readonly SettingsManager _settingsManager;
    private string _currentUrl;
    private CancellationTokenSource? _debounceCts;
    private string _lastSearch = string.Empty;

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

        try
        {
            if (_lastSearch == newSearch)
                return;

            _lastSearch = newSearch;

            if (oldSearch == newSearch)
                return;

            var trimmedSearch = newSearch.Trim();

            if (string.IsNullOrWhiteSpace(trimmedSearch))
            {
                await ApplyLocalFormatFilterAsync(null);
                return;
            }

            if (_settingsManager.GetDownloadOnPaste && !string.IsNullOrEmpty(_settingsManager.GetCustomFormatSelector))
            {
                var downloadBanner = new StatusMessage();
                _ = _ytDlp.TryExecuteDownloadAsync(trimmedSearch, downloadBanner);
                return;
            }

            var isValidFormatId = Regex.IsMatch(trimmedSearch, @"^([\w.\-]+)([+][\w.\-]+)*$");

            if (isValidFormatId)
            {
                await ApplyLocalFormatFilterAsync(trimmedSearch);
                return;
            }

            await UpdateListAsync(newSearch);
        }
        catch
        {
            //IGNORED
        }
    }

    private async Task ApplyLocalFormatFilterAsync(string? formatsSearch)
    {
        try
        {
            var sourceItems = new List<VideoFormatListItem>(_fallbackItems);
            _itens.Clear();

            if (string.IsNullOrWhiteSpace(formatsSearch))
            {
                _itens.AddRange(sourceItems);
                RaiseItemsChanged(_itens.Count);
                return;
            }

            var formatParts = formatsSearch.Split("+", StringSplitOptions.TrimEntries).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (formatParts.Length == 0 || formatParts.Length > 2)
            {
                _itens.AddRange(sourceItems);
                RaiseItemsChanged(_itens.Count);
                return;
            }

            var firstFormat = formatParts[0];
            var videoFormats = sourceItems
                .Where(FormatHelper.IsVideo)
                .Where(item => item.Tags.Any(tag => tag.Text.StartsWith(firstFormat, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var audioFormats = sourceItems
                .Where(FormatHelper.IsAudio)
                .Where(item => item.Title.Contains("audio", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (formatParts.Length > 1)
            {
                var lastFormat = formatParts[1];

                if (!string.IsNullOrWhiteSpace(lastFormat))
                {
                    audioFormats = audioFormats
                        .Where(item => item.Tags.Any(tag => tag.Text.StartsWith(lastFormat, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    var exactMatch = audioFormats
                        .Where(item => item.Tags.Any(tag => tag.Text.Equals(lastFormat, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (exactMatch.Count == 1)
                    {
                        audioFormats = exactMatch;
                    }
                }
                else
                {
                    audioFormats.Clear();
                }
            }

            // Special Case: exactly one video and one audio format
            if (videoFormats.Count == 1 && audioFormats.Count == 1)
            {
                var selectedVideo = videoFormats[0];
                var selectedAudio = audioFormats[0];
                var quickMergeList = new List<VideoFormatListItem>
                {
                    selectedVideo,
                    selectedAudio
                };

                var videoFormatId = selectedVideo.Tags.FirstOrDefault()?.Text;
                var audioFormatId = selectedAudio.Tags.FirstOrDefault()?.Text;
                Title = $"{videoFormatId?.ToString()}+{audioFormatId?.ToString()}";
                if (!string.IsNullOrWhiteSpace(videoFormatId) && !string.IsNullOrWhiteSpace(audioFormatId))
                {
                    foreach (var item in quickMergeList)
                    {
                        var commands = item.MoreCommands?.ToList() ?? new List<IContextItem>();
                        var cancellationToken = new CancellationTokenSource();
                        Command quickMergeCommand = new AnonymousCommand(() => { });

                        var cancelDownloadCommand = new AnonymousCommand(() =>
                        {
                            cancellationToken.Cancel();
                            item.Command = quickMergeCommand;
                        })
                        {
                            Name = "CancelDownload".ToLocalized(),
                            Icon = new IconInfo("\uE711"),
                            Result = CommandResult.KeepOpen(),
                        };
                        var downloadBanner = new StatusMessage();
                        quickMergeCommand = new AnonymousCommand
                        (
                          async () =>
                          {
                              item.Command = cancelDownloadCommand;
                              await _ytDlp.TryExecuteDownloadAsync(
                                        item.VideoUrl,
                                        downloadBanner,
                                        item.Details?.Title ?? "MissingTitle".ToLocalized(),
                                        videoFormatId,
                                        audioFormatId: audioFormatId,
                                        cancellationToken: cancellationToken.Token
                                    );
                          }
                        )
                        {
                            Name = "QuickMerge",
                            Result = CommandResult.KeepOpen()
                        };



                        item.Command = quickMergeCommand;
                    }
                }
                _itens.Clear();
                _itens.AddRange(quickMergeList);
                RaiseItemsChanged(_itens.Count);
                return;
            }
            else
            {
                _itens.Clear();
                _itens.AddRange(videoFormats);
                _itens.AddRange(audioFormats);
                RaiseItemsChanged(_itens.Count);
                return;
            }

        }
        catch (Exception ex)
        {
            // Resets the list in case of error
            _itens.Clear();
            _itens.AddRange(_fallbackItems);
            RaiseItemsChanged(_itens.Count);
        }
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

                        //var selectToMergeCommand = new CommandContextItem("Select");
                        var selectToMergeCommand = CommandsHelper.CreateCyclicCommand(
                            "Select",
                            () =>
                            {
                                _itemsToMerge.Add(formatListItem);

                                var tags = formatListItem.Tags.ToList();
                                tags.Add(new Tag("Selected")
                                {
                                    Background = new OptionalColor(true, new Color(accentColor.R, accentColor.G, accentColor.B, accentColor.A)),
                                    Foreground = new OptionalColor(true, new Color(foregroundColor.R, foregroundColor.G, foregroundColor.B, foregroundColor.A))
                                });
                                formatListItem.Tags = tags.ToArray();
                            },
                            "Unselect",
                            () =>
                            {
                                _itemsToMerge.Remove(formatListItem);

                                var tags = formatListItem.Tags.ToList();
                                tags.RemoveAll(t => t.Text == "Selected");
                                formatListItem.Tags = tags.ToArray();
                            },
                            new IconInfo("\ue710"),
                            new IconInfo("\ue711")
                        );

                        moreCommands.Add(selectToMergeCommand);

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
