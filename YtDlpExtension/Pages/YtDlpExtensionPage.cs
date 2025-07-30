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
    private List<VideoFormatListItem> _selectedItems = new();
    private DownloadHelper _ytDlp;
    IconInfo _ytDlpIcon = IconHelpers.FromRelativePath("Assets\\CmdPal-YtDlp.png");
    private readonly SettingsManager _settingsManager;
    //private string _currentUrl;
    //private CancellationTokenSource? _debounceCts;
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
        Title = "Video Downloader";
        Name = "Open";
        ShowDetails = true;
        ReloadExtensionState();
        _ytDlp.TitleUpdated += title => Title = title;
        _ytDlp.LoadingChanged += loading => IsLoading = loading;
        _ytDlp.ItemsChanged += count => RaiseItemsChanged(count);
        _ytDlp.RequestActiveDownloads += GetActiveDownloads;

    }


    private void ReloadExtensionState()
    {
        //var (isAvailable, ytDlpVersion) = SettingsManager.IsYtDlpBinaryAvailable();

        if (!_ytDlp.IsAvailable)
        {
            PlaceholderText = "YtDlpMissingTitle".ToLocalized();
            HandleMissingYtDlpError();
            return;
        }
        PlaceholderText = "PlaceholderText".ToLocalized();
        Title = $"Video Downloader (yt-dlp version: {_ytDlp.Version})";
        EmptyContent = new CommandItem(new NoOpCommand())
        {
            Icon = _ytDlpIcon,
            Title = "EmptyContentTiltle".ToLocalized(),
            Subtitle = _settingsManager.GetSelectedMode == ExtensionMode.ADVANCED ? $"{_settingsManager.GetSelectedMode} mode" : string.Empty,
            MoreCommands = [
                new CommandContextItem(ShowOutputDirCommand(_settingsManager.DownloadLocation)),
            new CommandContextItem(_settingsManager.Settings.SettingsPage){
                Title = "Settings".ToLocalized(),
                Subtitle = "Configure the video downloader settings",
                Icon = new IconInfo("\uE713"),
            },
        ]
        };
        UpdateSearchText(string.Empty, string.Empty);
        RaiseItemsChanged(_itens.Count);
    }

    public override async void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (!_ytDlp.IsAvailable) return;

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
                                        item.VideoUrl!,
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
        catch
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
        _selectedItems.Clear();
        _fallbackItems.Clear();
        IsLoading = true;
        if (!TryParseUrl(queryText, out var queryURL, out var audioOnlyQuery))
        {
            IsLoading = false;
            return;
        }
        var isPlaylistURL = IsPlaylistUrl(queryURL);

        var (jsonResult, bestformat, error) = await _ytDlp.TryExecuteQueryAsync(queryURL);

        if (error > 0)
        {
            var (title, message, icon) = GetError(error);
            HandleError(title, message.ToLocalized(), icon);
            return;
        }

        if (string.IsNullOrWhiteSpace(jsonResult))
        {
            HandleError("SomethingWrong".ToLocalized(), "EmptyDataYtDlp".ToLocalized());
            return;
        }

        try
        {
            var (videoData, thumbnail, videoTitle) = ParseVideoData(jsonResult);

            if (videoData == null)
            {
                HandleError("SomethingWrong".ToLocalized(), "EmptyDataYtDlp".ToLocalized());
                return;
            }

            if (videoData?.Formats == null && videoData?.Entries == null)
            {
                _itens.Add(new VideoFormatListItem(queryURL, videoTitle, thumbnail, videoData!, _ytDlp, _settingsManager));
                RaiseItemsChanged(1);
                return;
            }

            Title = $"{videoTitle}";

            if (isPlaylistURL)
            {
                ApplyPlaylistData(videoData!);
                RaiseItemsChanged(1);
            }

            var items = BuildFormatListItems(videoData, queryURL);
            if (items.Count > 0)
            {
                _itens.AddRange(items);
            }

            _fallbackItems = _itens.ToList();

            RaiseItemsChanged(_itens.Count);

        }

        catch (System.Exception ex)
        {
            HandleError("SomethingWrong".ToLocalized(), ex.Message);
            IsLoading = false;
            return;
        }

        finally
        {
            IsLoading = false;
        }

    }


    private static bool TryParseUrl(string queryText, out string parsedQueryUrl, out bool audioOnly)
    {
        audioOnly = queryText.StartsWith("@", StringComparison.OrdinalIgnoreCase);
        parsedQueryUrl = audioOnly ? queryText.Split("@")[1] : queryText;
        return DownloadHelper.IsValidUrl(parsedQueryUrl);
    }

    private static bool IsPlaylistUrl(string url)
    {
        return url.Contains("playlist") || url.Contains("list=") || url.Contains("videos");
    }

    private static (VideoData? videoData, string thumbnail, string title) ParseVideoData(string json)
    {
        var videoData = System.Text.Json.JsonSerializer.Deserialize(json, VideoDataContext.Default.VideoData);
        string videoTitle = videoData?.Title ?? "MissingTitle".ToLocalized();
        string thumbnail = videoData?.Thumbnail ?? "MissingThumb".ToLocalized();

        return (videoData, thumbnail, videoTitle);
    }

    private void ApplyPlaylistData(VideoData data)
    {
        var thumbnailFallback = data?.Thumbnails!
                                    .Where(thumb => thumb.id!.Contains('7'))
                                    .Select(thumb => thumb.url)
                                    .FirstOrDefault();

        var playlistData = new JObject()
        {
            ["title"] = data?.Title,
            ["thumbnail"] = data?.Thumbnail ?? thumbnailFallback,
            ["videoURL"] = data?.OriginalUrl
        };
        IsLoading = true;

        var downloadBanner = new StatusMessage();

        // The command to go to the playlist form is declared here
        // in order to update the command once the download start
        Command goToPlaylistDownloadForm = new NoOpCommand();


        //The form page will be set after the data from the playlist is fetched
        var listItem = new VideoFormatListItem(new NoOpCommand(), _ytDlp, _settingsManager)
        {
            Tags = [new Tag("Playlist")],
            Icon = new IconInfo("\uE895"),
            Title = "FetchingPlaylistTitle".ToLocalized() ?? "MissingTitle",
            Subtitle = "FetchingPlaylistDescription".ToLocalized() ?? "MissingDescription"
        };
        _itens.Insert(0, listItem);

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

        Action<string> onDownloadFinish = (downloadLocation) =>
        {
            listItem.Command = goToPlaylistDownloadForm;
            listItem.MoreCommands = [new CommandContextItem(ShowOutputDirCommand(downloadLocation))];
            RaiseItemsChanged(1);
        };


        /// The actual form is constructed here passing the callback declared previously
        /// This is currently needed because <see cref="CommandResult.GoToPage"/>  is not implemented yet
        /// Needs future refactoring


        //listItem.Command = goToPlaylistDownloadForm;
        //listItem.Title = "PlaylistFetchedTitle".ToLocalized();
        //listItem.Subtitle = "PlaylistFetchedDescription".ToLocalized();
        //listItem.Icon = new IconInfo("\uE90B");
        //Title = "PlaylistFetchedTitle".ToLocalized();
        if (data!.ResultType == "playlist")
        {
            var playlistTitle = data?.Title!;
            playlistData.Add("playlistTitle", playlistTitle);
            playlistData.Add("downloadPath", Path.Combine(_settingsManager.DownloadLocation, playlistTitle));
            playlistData.Add("playlistCount", data!.PlaylistCount);

            goToPlaylistDownloadForm = new PlaylistFormPage(_settingsManager, data!, playlistData, _ytDlp, onSubmit: onSubmitForm, onDownloadFinish);
            listItem.Command = goToPlaylistDownloadForm;
            listItem.Title = "PlaylistFetchedTitle".ToLocalized();
            listItem.Subtitle = "PlaylistFetchedDescription".ToLocalized();
            listItem.Icon = new IconInfo("\uE90B");
            IsLoading = false;
            return;
        }

        _ = _ytDlp.ExtractPlaylistDataAsync(
            data.OriginalUrl!,
            onFinish: (playlistDetails) =>
            {
                var playlistTitle = playlistDetails["playlistTitle"]?.ToString() ?? string.Empty;
                var playlistCount = playlistDetails["playlistCount"]?.ToString() ?? string.Empty;

                playlistData.Add("playlistTitle", playlistTitle);
                playlistData.Add("downloadPath", Path.Combine(_settingsManager.DownloadLocation, playlistTitle));
                playlistData.Add("playlistCount", playlistCount);

                goToPlaylistDownloadForm = new PlaylistFormPage(_settingsManager, data!, playlistData, _ytDlp, onSubmit: onSubmitForm, onDownloadFinish);

                listItem.Command = goToPlaylistDownloadForm;
                listItem.Title = "PlaylistFetchedTitle".ToLocalized();
                listItem.Subtitle = "PlaylistFetchedDescription".ToLocalized();
                listItem.Icon = new IconInfo("\uE90B");
            });


    }

    private List<VideoFormatListItem> BuildFormatListItems(VideoData videoData, string queryUrl)
    {
        var formatsOrdered = (_settingsManager.GetSelectedMode, videoData.IsLive) switch
        {
            ("simple", false) => FormatHelper.OrderByResolutionDistinct(videoData?.Formats),
            (_, true) => FormatHelper.OrderByResolution(videoData?.Formats),
            _ => FormatHelper.OrderByResolution(videoData?.Formats)
        };



        CommandContextItem? listSubtitlesCommand = null;
        CommandContextItem? listAutoCaptionsCommand = null;
        var showOutputDir = new CommandContextItem(ShowOutputDirCommand())
        {
            Icon = new IconInfo("\uE838"),
            Title = "ShowOutputDir".ToLocalized(),
        };

        if (videoData?.Subtitles?.Count > 0)
        {
            listSubtitlesCommand = new CommandContextItem(
                new SubtitlesPage(queryUrl, videoData.Subtitles, false, _ytDlp, _settingsManager))
            {
                Icon = new IconInfo("\uf15f"),
                Title = "ListSubtitles".ToLocalized(),
            };
        }
        if (videoData?.AutomaticCaptions?.Count > 0)
        {
            listAutoCaptionsCommand = new CommandContextItem(
                new SubtitlesPage(queryUrl, videoData.AutomaticCaptions, true, _ytDlp, _settingsManager))
            {
                Icon = new IconInfo("\ued0c"),
                Title = "ListAutoCaptions".ToLocalized(),
            };
        }


        var items = new List<VideoFormatListItem>();

        foreach (var format in formatsOrdered)
        {
            var formatObject = format;

            if (formatObject != null)
            {
                var formatListItem = new VideoFormatListItem(queryUrl, videoData?.Title!, videoData?.Thumbnail!, formatObject, _ytDlp, _settingsManager, isLive: videoData!.IsLive);
                var moreCommands = formatListItem.MoreCommands.ToList();


                if (videoData.IsLive == false)
                {
                    var trimVideoForm = new TrimVideoFormPage(queryUrl, _settingsManager, videoData!, format, _ytDlp, _selectedItems);
                    var trimVideoCommand = new CommandContextItem(trimVideoForm)
                    {
                        Icon = new IconInfo("\ue8c6"),
                        Title = "TrimVideo".ToLocalized(),
                    };
                    moreCommands.Add(trimVideoCommand);
                }

                moreCommands.Add(showOutputDir);
                if (listSubtitlesCommand != null)
                    moreCommands.Add(listSubtitlesCommand);

                if (listAutoCaptionsCommand != null)
                    moreCommands.Add(listAutoCaptionsCommand);

                if (_settingsManager.GetSelectedMode == ExtensionMode.ADVANCED)
                {
                    var selectCommand = BuildQuickMergeCommand(formatListItem);
                    moreCommands.Add(selectCommand);
                }


                formatListItem.MoreCommands = moreCommands.ToArray();

                items.Add(formatListItem);
            }
        }

        return items;
    }

    private AnonymousCommand ShowOutputDirCommand(string? downloadPath = default)
    {
        return new AnonymousCommand(() =>
        {
            Process.Start("explorer.exe", $"\"{downloadPath ?? _settingsManager.DownloadLocation}\"");
        })
        {
            Result = CommandResult.KeepOpen(),
            Icon = new IconInfo("\uE838"),
            Name = "ShowOutputDir".ToLocalized(),
        };
    }

    private CommandContextItem BuildQuickMergeCommand(VideoFormatListItem formatListItem)
    {

        var uiSettings = new UISettings();
        var accentColor = uiSettings.GetColorValue(UIColorType.AccentDark2);
        var foregroundColor = uiSettings.GetColorValue(UIColorType.Foreground);

        return CommandsHelper.CreateCyclicCommand(
                    "Select",
                    () =>
                    {
                        _selectedItems.Add(formatListItem);

                        var tags = formatListItem.Tags.ToList();
                        tags.Add(new Tag("Selected")
                        {
                            Background = new OptionalColor(true, new Color(accentColor.R, accentColor.G, accentColor.B, accentColor.A)),
                            Foreground = new OptionalColor(true, new Color(foregroundColor.R, foregroundColor.G, foregroundColor.B, foregroundColor.A))
                        });
                        formatListItem.Tags = tags.ToArray();
                        if (_selectedItems.Count > 1)
                        {
                            var videoFormats = _selectedItems
                                .Where(FormatHelper.IsVideo)
                                .ToList();

                            var audioFormats = _selectedItems
                                .Where(FormatHelper.IsAudio)
                                .ToList();

                            if (videoFormats.Count == 1 && audioFormats.Count == 1)
                            {
                                var selectedVideo = videoFormats.First();
                                var selectedAudio = audioFormats.First();
                                var quickMergeList = new List<VideoFormatListItem>
                                {
                                    selectedVideo,
                                    selectedAudio
                                };

                                var videoFormatId = selectedVideo.Tags.FirstOrDefault()?.Text;
                                var audioFormatId = selectedAudio.Tags.FirstOrDefault()?.Text;

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
                                                        item.VideoUrl!,
                                                        downloadBanner,
                                                        item.Details?.Title ?? "MissingTitle".ToLocalized(),
                                                        videoFormatId,
                                                        audioFormatId: audioFormatId,
                                                        cancellationToken: cancellationToken.Token,
                                                        onFinish: (showFileCommand) =>
                                                        {
                                                            var commands = item.MoreCommands?.ToList();
                                                            item.Command = quickMergeCommand;
                                                            item.MoreCommands = commands?.ToArray() ?? [];
                                                        }
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
                            }
                        }
                    },
                    "Unselect",
                    () =>
                    {
                        _selectedItems.Remove(formatListItem);

                        var tags = formatListItem.Tags.ToList();
                        tags.RemoveAll(t => t.Text == "Selected");
                        formatListItem.Tags = tags.ToArray();
                    },
                    new IconInfo("\ue710"),
                    new IconInfo("\ue711")
                );
    }

    public static (string, string, IconInfo?) GetError(int errorCode)
    {
        return errorCode switch
        {
            401 => ("UserRestricted".ToLocalized(), "UserRestrictedMessage".ToLocalized(), new IconInfo("\ue72e")),
            403 => ("AgeRestricted".ToLocalized(), "AgeRestrictedMessage".ToLocalized(), new IconInfo("🔞")),
            _ => ("Error".ToLocalized(), "SomethingWrong".ToLocalized(), null)
        };
    }

    private void HandleError(string title, string message, IconInfo? icon = null)
    {
        IsLoading = false;
        EmptyContent = new CommandItem(new NoOpCommand())
        {
            Icon = icon ?? new IconInfo("\uE946"),
            Title = title,
            Subtitle = message
        };
    }

    private void HandleMissingYtDlpError()
    {
        IsLoading = false;
        var downloadBanner = new StatusMessage();
        var cts = new CancellationTokenSource();
        var installYtDlpWinget = new AnonymousCommand(async () =>
        {
            await _ytDlp.TryDownloadYtDlpWingetAsync(downloadBanner, cts.Token);
            if (downloadBanner.CurrentState() == DownloadState.Finished)
            {

                var (isAvailable, version) = SettingsManager.IsYtDlpBinaryAvailable();
                _ytDlp.IsAvailable = isAvailable;
                _ytDlp.Version = version;
                ReloadExtensionState();
            }
        })
        {
            Name = "InstallYtDlpWinget".ToLocalized(),
            Icon = new IconInfo("\uE946"),
            Result = CommandResult.KeepOpen()
        };


        EmptyContent = new CommandItem()
        {
            Icon = new IconInfo("\uE946"),
            Title = "YtDlpMissingTitle".ToLocalized(),
            Subtitle = "YtDlpMissingDescription".ToLocalized(),
            Command = installYtDlpWinget,
        };
    }

    public override IListItem[] GetItems() => _itens.ToArray();
}
