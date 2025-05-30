using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using YtDlpExtension.Helpers;

namespace YtDlpExtension.Pages
{
    internal sealed partial class PlaylistFormPage : ContentPage
    {
        private readonly SettingsManager _settings;
        private readonly JObject _jsonData;
        private readonly DownloadHelper _ytDlp;
        private readonly string _url;
        private readonly Action<CancellationTokenSource> _onSubmit;
        public PlaylistFormPage(SettingsManager settings, JObject jsonData, DownloadHelper ytDlp, Action<CancellationTokenSource> onSubmit)
        {
            _settings = settings;
            _jsonData = jsonData;
            _ytDlp = ytDlp;
            _url = jsonData["videoURL"]?.ToString() ?? "";
            _onSubmit = onSubmit;
        }

        public override IContent[] GetContent()
        {
            var playlistTitle = _jsonData["playlistTitle"]?.ToString() ?? "";
            return [
                new TreeContent
                {
                    RootContent = new PlaylistFormContent(_settings, _jsonData, _ytDlp, _onSubmit),
                    Children = []
                }
            ];
        }

    }

    internal sealed partial class PlaylistFormContent : FormContent
    {
        private readonly SettingsManager _settings;
        private string _playlistTitle;
        private JObject _jsonData = new();
        private readonly DownloadHelper _ytDlp;
        private readonly Action<CancellationTokenSource> _onSubmit;
        private readonly StatusMessage _playlistDownloadBanner = new();
        public PlaylistFormContent(SettingsManager settings, JObject jsonData, DownloadHelper ytDlp, Action<CancellationTokenSource> onSubmit)
        {
            _settings = settings;
            _ytDlp = ytDlp;
            _onSubmit = onSubmit;
            var videoURL = jsonData["videoURL"]?.ToString();
            var title = jsonData["title"]?.ToString();
            var thumbnail = jsonData["thumbnail"]?.ToString();
            var downloadPath = jsonData["downloadPath"]?.ToString();
            var downloadPathJson = downloadPath?.Replace(@"\", @"\\"); ;
            var playlistCount = jsonData["playlistCount"]?.ToString() ?? "1";
            _playlistTitle = jsonData["playlistTitle"]?.ToString() ?? "";
            _jsonData?.Add("title", videoURL);
            _jsonData?.Add("videoURL", videoURL);
            _jsonData?.Add("thumbnail", thumbnail);
            _jsonData?.Add("downloadPath", downloadPath);
            var templateJson = $$"""
                {
                    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                    "type": "AdaptiveCard",
                    "version": "1.6",
                    "body": [
                        {
                            "type": "TextBlock",
                            "id": "playlistTitle",
                            "text": "Playlist: ${playlistTitle}",
                            "wrap": true,
                            "weight": "Bolder",
                            "size": "Large"
                        },
                        {
                            "type": "ColumnSet",
                            "columns": [
                                {
                                    "type": "Column",
                                    "width": "stretch",
                                    "items": [
                                        {
                                            "type": "TextBlock",
                                            "id": "playlistCount",
                                            "text": "{{"playlistCount".ToLocalized(playlistCount ?? "0")}}",
                                            "wrap": true,
                                            "weight": "Bolder",
                                            "size": "Medium"
                                        },
                                        {
                                            "type": "Image",
                                            "url": "${thumbnail}"
                                        }
                                    ]
                                },
                                {
                                    "type": "Column",
                                    "width": "stretch",
                                    "items": [
                                        {
                                            "type": "Input.Text",
                                            "id": "downloadPath",
                                            "label": "{{"DownloadDirectory".ToLocalized()}}",
                                            "placeholder": "${downloadPath}",
                                            "value": "{{downloadPathJson}}",
                                            "isRequired": true,
                                            "errorMessage": "Must have a path"
                                        },
                                        {
                                            "type": "Input.Text",
                                            "id": "customFormat",
                                            "label": "{{"CustomFormatSelector".ToLocalized()}}",
                                            "placeholder": "",
                                            "isVisible": false
                                        },
                                        {
                                            "type": "Input.ChoiceSet",
                                            "isVisible": true,
                                            "id": "resolution",
                                            "label": "{{"VideoResolution".ToLocalized()}}",
                                            "value": "bestvideo[ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[ext=mp4][vcodec^=avc1]",
                                            "choices": [
                                                {
                                                    "title": "{{"BestAvailable".ToLocalized()}}",
                                                    "value": "bestvideo[ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "2160p (4K)",
                                                    "value": "bestvideo[height<=2160][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=2160][ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "1440p (2K)",
                                                    "value": "bestvideo[height<=1440][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=1440][ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "1080p (Full HD)",
                                                    "value": "bestvideo[height<=1080][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=1080][ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "720p (HD)",
                                                    "value": "bestvideo[height<=720][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=720][ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "480p (SD)",
                                                    "value": "bestvideo[height<=480][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=480][ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "360p",
                                                    "value": "bestvideo[height<=360][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=360][ext=mp4][vcodec^=avc1]"
                                                },
                                                {
                                                    "title": "240p",
                                                    "value": "bestvideo[height<=240][ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a][acodec^=mp4a]/best[height<=240][ext=mp4][vcodec^=avc1]"
                                                }
                                            ]
                                        },
                                        {
                                            "type": "ColumnSet",
                                            "columns": [
                                                {
                                                    "type": "Column",
                                                    "verticalContentAlignment": "Bottom",
                                                    "width": "auto",
                                                    "items": [
                                                        {
                                                            "type": "Input.Number",
                                                            "placeholder": "{{"Start".ToLocalized()}}",
                                                            "label": "{{"Start".ToLocalized()}}",
                                                            "id": "playlistStart",
                                                            "min": 1,
                                                            "value": 1,
                                                            "max": {{playlistCount}},
                                                            "isRequired": true,
                                                            "errorMessage": "Must have a number between 1 and {{playlistCount}}"
                                                        }
                                                    ]
                                                },
                                                {
                                                    "type": "Column",
                                                    "verticalContentAlignment": "Bottom",
                                                    "width": "auto",
                                                    "items": [
                                                        {
                                                            "type": "Input.Number",
                                                            "label": "{{"End".ToLocalized()}}",
                                                            "id": "playlistEnd",
                                                            "value": {{playlistCount}},
                                                            "min": 1,
                                                            "max": {{playlistCount}},
                                                            "isRequired": true,
                                                            "errorMessage": "Must have a number between 1 and {{playlistCount}}"
                                                        }
                                                    ]
                                                },
                                                {
                                                    "type": "Column",
                                                    "verticalContentAlignment": "Top",
                                                    "width": "auto",
                                                    "items": [
                                                        {
                                                          "type": "TextBlock",
                                                          "text": " ",
                                                          "spacing": "None"
                                                        },
                                                        {
                                                            "type": "Input.Toggle",
                                                            "title": "{{"DownloadAudioOnly".ToLocalized()}}",
                                                            "id": "audioOnly"
                                                        }
                                                    ]
                                                }
                                            ]
                                        },
                                        {
                                            "type": "ActionSet",
                                            "actions": [
                                                {
                                                    "type": "Action.Submit",
                                                    "title": "Download",
                                                    "data": {
                                                        "actions": "downloadPlaylist"
                                                    }
                                                },
                                                {
                                                    "type": "Action.ToggleVisibility",
                                                    "title": "{{"AdvancedOptions".ToLocalized()}}",
                                                    "targetElements": [
                                                        "customFormat",
                                                        "resolution"
                                                    ]
                                                }
                                            ],
                                            "spacing": "Padding",
                                            "horizontalAlignment": "Center"
                                        }
                                    ],
                                    "rtl": false,
                                    "horizontalAlignment": "Center",
                                    "verticalContentAlignment": "Center",
                                    "roundedCorners": true
                                }
                            ]
                        }
                    ]
                }
                """;

            TemplateJson = templateJson;
            DataJson = jsonData.ToString();
        }


        public override CommandResult SubmitForm(string payload, string data)
        {
            var formInput = JObject.Parse(payload);
            if (formInput == null)
            {
                return CommandResult.GoHome();
            }
            ;
            var resolution = formInput["resolution"]?.ToString() ?? "best";
            var downloadPathFromPayload = formInput["downloadPath"]?.ToString();
            bool audioOnly = formInput["audioOnly"]?.ToString().Contains("true") ?? false;
            string playlistStart = formInput["playlistStart"]?.ToString() ?? "1";
            string playlistEnd = formInput["playlistEnd"]?.ToString() ?? "1";
            string customFormat = formInput["customFormat"]?.ToString() ?? "";

            var videoURL = _jsonData["videoURL"]?.ToString() ?? "";
            var downloadPath = string.IsNullOrEmpty(downloadPathFromPayload) ? Path.Combine(_settings.DownloadLocation, _playlistTitle) : downloadPathFromPayload;
            var token = new CancellationTokenSource();
            CommandResult.Confirm(new ConfirmationArgs()
            {
                Description = formInput.ToString(),
                Title = "Payload",
                PrimaryCommand = new NoOpCommand()
                {

                }

            });
            _ = _ytDlp.TryExecutePlaylistDownloadAsync(
                videoURL,
                _playlistDownloadBanner,
                downloadPath,
                resolution,
                playlistStart,
                playlistEnd,
                customFormat,
                audioOnly: audioOnly,
                cancellationToken: token.Token
             );
            _onSubmit.Invoke(token);
            return CommandResult.GoBack();
        }
    }
}
