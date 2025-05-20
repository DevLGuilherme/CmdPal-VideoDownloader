// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ABI.System;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.System;
using YtDlpExtension.Helpers;
using YtDlpExtension.Pages;

namespace YtDlpExtension;

internal sealed partial class YtDlpExtensionPage : DynamicListPage
{
    private readonly List<ListItem> _itens = new();
    private ListItem? _selectedIten;
    private DownloadHelper _ytDlp;
    private readonly Dictionary<string, ListItem> _activeDownloads = new();
    IconInfo _ytDlpIcon = IconHelpers.FromRelativePath("Assets\\Logo.png");
    private readonly IconInfo _testeIcon = IconHelpers.FromRelativePath("Assets\\teste.gif");
    private readonly SettingsManager _settingsManager;

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
        UpdateSearchText(string.Empty, string.Empty);
    }

    public override async void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (oldSearch == newSearch) return;

        var isPlaylist = newSearch.Contains("playlist") || newSearch.Contains("list=");
        CommandResult.GoToPage(new GoToPageArgs() { PageId = "USAGE"});

        await UpdateListAsync(newSearch);
        
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
        string jsonResult = await _ytDlp.TryExecuteQueryAsync(queryURL, isPlaylist);
        
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
            JObject videoInfo = JObject.Parse(jsonOutput);
            //Get Video Title
            string videoTitle = videoInfo["title"]?.ToString() ?? "MissingTitle".ToLocalized();
            //Get all available formats
            var formats = videoInfo["formats"] as JArray;
            //Order formats by resolution, from highest to lowest
            JToken[] formatsOrdered = [];           
            if (audioOnlyQuery)
            {
                formatsOrdered = formats?.Where(
                f => 
                    f?["vcodec"]?.ToString() == "none" && 
                    f?["resolution"]?.ToString() == "audio only"
                ).ToArray() ?? [];
            } else
            {
                formatsOrdered = formats?
                    .OrderByDescending(format => format["height"]?.ToObject<int?>() ?? 0)
                    .ToArray() ?? [];
            }
            // Get Video Thumbnail if exists
            string thumbnail = videoInfo["thumbnail"]?.ToString() ?? "MissingThumb".ToLocalized();

            if (isPlaylist)
            {
                var playlistData = new JObject()
                {
                    ["title"] = videoTitle,
                    ["thumbnail"] = thumbnail,
                    ["downloadPath"] = Path.Combine(_settingsManager.DownloadLocation, videoTitle),
                    ["videoURL"] = queryURL
                };
                Title = "FetchingPlaylistTitle".ToLocalized();
                _ytDlp._downloadBanner.UpdateState(DownloadState.CustomMessage, "FetchingPlaylistTitle".ToLocalized(), true);
                _ytDlp._downloadBanner.ShowStatus();
                IsLoading = true;
                var listItem = new ListItem(new NoOpCommand())
                {
                    Tags = [new Tag("Playlist")],
                    Icon = new IconInfo("\uE895"),
                    Title = "FetchingPlaylistTitle".ToLocalized(),
                    Subtitle = "FetchingPlaylistDescription".ToLocalized()
                };
                _itens.Insert(0,listItem);
                RaiseItemsChanged(1);
                _ = _ytDlp.ExtractPlaylistDataAsync(queryURL, onFinish: (Action<JObject>?)((playlistDetails) =>
                {
                    var playlistTitle = playlistDetails["playlistTitle"]?.ToString();
                    var playlistCount = playlistDetails["playlistCount"]?.ToString();
                    playlistData.Add("playlistTitle", playlistTitle);
                    playlistData.Add("playlistCount", "playlistCount".ToLocalized(playlistCount ?? "0"));
                    listItem.Command = new PlaylistFormPage(_settingsManager, playlistData, _ytDlp);
                    listItem.Title = "PlaylistFetchedTitle".ToLocalized();
                    listItem.Subtitle = "PlaylistFetchedDescription".ToLocalized();
                    listItem.Icon = new IconInfo("\uE90B");
                    Title = "PlaylistFetchedTitle".ToLocalized();
                    RaiseItemsChanged(1);
                    _ytDlp._downloadBanner.Hide();
                }));
                

            }

            foreach (var format in formatsOrdered)
            {
                var formatObject = format as JObject;

                if (formatObject != null)
                {
                    _itens.Add(CreateListItem(queryURL, videoTitle, thumbnail, formatObject));
                    RaiseItemsChanged();
                }
            }
            RaiseItemsChanged(_itens.Count);
            IsLoading = false;
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
        
    }

    private async Task HandlePlaylistAsync(string queryText)
    {
        _itens.Clear();
    }
 
    private ListItem CreateListItem(string queryURL, string videoTitle, string thumbnail, JObject formatObject)
    {
        var formatId = formatObject["format_id"]?.ToString() ?? "";
        var resolution = formatObject["resolution"]?.ToString() ?? "";
        var vcodec = formatObject["vcodec"]?.ToString() ?? "";
        var acodec = formatObject["acodec"]?.ToString() ?? "";
        var ext = formatObject["ext"]?.ToString() ?? "";
        var filesizeBytes = formatObject["filesize"]?.ToObject<long?>()
              ?? formatObject["filesize_approx"]?.ToObject<long?>();
        double sizeInMB = 0d;
        if (filesizeBytes != null) sizeInMB = filesizeBytes.Value / (1024.0 * 1024.0);

        List<Tag> _tags = [
            new Tag(formatId),
            new Tag(ext)
        ];

        List<IContextItem> _commands = [];
        List<IContextItem> _startDownloadCommand = [
                    new CommandContextItem(
                        title: "Download Audio",
                        name: "Download Audio",
                        subtitle: "Extracts audio",
                        result: CommandResult.KeepOpen(),
                        action: () => { /* Action code */ }
                    )
                    {
                        Icon = new IconInfo("\uEC4F"),
                        RequestedShortcut = KeyChordHelpers.FromModifiers(false, false, false, false, (int)VirtualKey.A, 0)
                    }
                ];
        var listItem = new ListItem(new NoOpCommand())
        {
            Title = resolution,
            Icon = new IconInfo("\uE896"),
            Tags = _tags.ToArray(),
            Details = new Details
            {
                Body =
                        $"""
                    ##**{videoTitle}**

                    ![Thumbnail]({thumbnail})
                  
                    ---
                    ### **{"Resolution".ToLocalized()}** 
                    ### [{resolution}](#resolution)
                    ### **{"Size".ToLocalized()}** 
                    ### [~{sizeInMB:F2}](#sizeInMB)
                    ### **{"Extension".ToLocalized()}** 
                    ### [{ext}](#ext)
                    ### **{"FormatId".ToLocalized()}**
                    ### [{formatId}](#formatId)
                    ### **{"ACodec".ToLocalized()}** 
                    ### [{acodec}](#acodec)
                    ### **{"VCodec".ToLocalized()}** 
                    ### [{vcodec}](#vcodec)
                    """,
            },
            MoreCommands = _startDownloadCommand.ToArray()
        };

        var command = new AnonymousCommand(async () =>
        {
            _selectedIten = listItem;

            var onDownloadStart = () =>
            {
                listItem.Subtitle = "Downloading...";
                listItem.MoreCommands = [
                    new CommandContextItem(
                        title: "Cancel Download",
                        name: "Cancel Download",
                        subtitle: "Cancel Download",
                        result: CommandResult.ShowToast(new ToastArgs() { Message = "Download Canceled", Result = CommandResult.KeepOpen() }),
                        action: () => {
                            _ytDlp.CancelDownload();
                            listItem.Subtitle = "";
                            listItem.MoreCommands = _startDownloadCommand.ToArray();
                        }
                    )
                    {
                        Icon = new IconInfo("\uE711"),
                        RequestedShortcut = KeyChordHelpers.FromModifiers(false, false, false, false, (int)VirtualKey.C, 0)
                    }
                ];
                RaiseItemsChanged(_itens.Count);
            };
            var onDownloadFinished = () =>
            {
                listItem.Icon = new IconInfo("\uE8FB");
                listItem.Subtitle = "Downloaded".ToLocalized();
                listItem.MoreCommands = _startDownloadCommand.ToArray();
            };
            _ = await _ytDlp.TryExecuteDownloadAsync(queryURL, formatId, onStart: onDownloadStart, onFinish: onDownloadFinished);
            
            
        })
        {
            Name = "Download",
            Icon = new IconInfo("\uE896"),
            Result = CommandResult.KeepOpen()
        };

        listItem.Command = command;
        return listItem;
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
