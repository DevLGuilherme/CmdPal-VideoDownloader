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
using YtDlpExtension.Pages;

namespace YtDlpExtension;

internal sealed partial class YtDlpExtensionPage : DynamicListPage
{
    private List<ListItem> _itens = new();
    private List<ListItem> _fallbackItems = new();
    private DownloadHelper _ytDlp;
    private readonly Dictionary<string, ListItem> _activeDownloads = new();
    IconInfo _ytDlpIcon = IconHelpers.FromRelativePath("Assets\\Logo.png");
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

        var oldAudioOnly = oldSearch.StartsWith('@');
        var newAudioOnly = newSearch.StartsWith('@');

        if (_fallbackItems == null || _fallbackItems.Count == 0)
        {
            // First Searh or empty list do a full search
            await UpdateListAsync(newSearch);
            return;
        }

        if (oldAudioOnly != newAudioOnly && (newSearch == oldSearch.TrimStart('@') || newSearch == ""))
        {
            // If an @ is typed or removed at the start of the string but the url remains the same
            if (newAudioOnly)
            {
                // Apply local filter for "audio only"
                _itens = _fallbackItems.Where(item => item.Title == "audio only").ToList();
            }
            else
            {
                // Removes the filter and restores the list
                _itens = _fallbackItems.ToList();
            }
            RaiseItemsChanged(_itens.Count);
            return;
        }

        if (newSearch.StartsWith('@'))
        {
            var trimmedSearch = newSearch.TrimStart('@');

            // If the base text remains the same, apply local filter
            if (trimmedSearch == oldSearch.TrimStart('@'))
            {
                _itens = _fallbackItems.Where(item => item.Title == "audio only").ToList();
                RaiseItemsChanged(_itens.Count);
                return;
            }
        }
        // If the url is completely different the do a new search
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
            JToken[] formatsOrdered = OrderByResolution(formats);
            //if (audioOnlyQuery)
            //{
            //    formatsOrdered = FilterAudioOnlyFormats(formats) ?? [];
            //}
            //else
            //{
            //    formatsOrdered =;
            //}
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
                //The form page will be set after the data from the playlist is fetched
                var listItem = new ListItem(new NoOpCommand())
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
                    var playlistTitle = playlistDetails["playlistTitle"]?.ToString();
                    var playlistCount = playlistDetails["playlistCount"]?.ToString();
                    playlistData.Add("playlistTitle", playlistTitle);
                    playlistData.Add("playlistCount", "playlistCount".ToLocalized(playlistCount ?? "0"));
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
                    Title = "PlaylistFetchedTitle".ToLocalized();
                    RaiseItemsChanged(1);
                    _ytDlp._downloadBanner.Hide();
                });


            }

            foreach (var format in formatsOrdered)
            {
                var formatObject = format as JObject;

                if (formatObject != null)
                {
                    _itens.Add(new VideoFormatListItem(queryURL, videoTitle, thumbnail, formatObject, _ytDlp, _settingsManager));
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

    private static JToken[] OrderByResolution(JArray? formats)
    {
        return formats?
            .OrderByDescending(format => format["height"]?.ToObject<int?>() ?? 0)
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
