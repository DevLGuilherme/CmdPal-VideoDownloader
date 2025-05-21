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
        private JObject _templateJson = JObject.Parse($$"""
                {
                    "$schema": "https://adaptivecards.io/schemas/adaptive-card.json",
                    "type": "AdaptiveCard",
                    "version": "1.5",
                    "body": [
                        {
                            "type": "TextBlock",
                            "id": "playlistTitle",
                            "text": "📺 Playlist: ${playlistTitle}",
                            "wrap": true,
                            "weight": "Bolder",
                            "size": "Large"
                        },
                        {
                            "type": "TextBlock",
                            "id": "${playlistCount}",
                            "text": "${playlistCount}",
                            "wrap": true,
                            "weight": "Bolder",
                            "size": "Medium"
                        },
                        {
                            "type": "ColumnSet",
                            "columns": [
                                {
                                    "type": "Column",
                                    "width": "stretch",
                                    "items": [
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
                                            "label": "Local de download",
                                            "placeholder": "${downloadPath}"
                                        },
                                        {
                                            "type": "Input.ChoiceSet",
                                            "id": "resolution",
                                            "label": "Resolução dos vídeos",
                                            "value": "best",
                                            "choices": [
                                                {
                                                    "title": "Melhor disponível",
                                                    "value": "best"
                                                },
                                                {
                                                    "title": "2160p (4K)",
                                                    "value": "bestvideo[height<=2160]+bestaudio/best[height<=2160]"
                                                },
                                                {
                                                    "title": "1440p (2K)",
                                                    "value": "bestvideo[height<=1440]+bestaudio/best[height<=1440]"
                                                },
                                                {
                                                    "title": "1080p (Full HD)",
                                                    "value": "bestvideo[height<=1080]+bestaudio/best[height<=1080]"
                                                },
                                                {
                                                    "title": "720p (HD)",
                                                    "value": "bestvideo[height<=720]+bestaudio/best[height<=720]"
                                                },
                                                {
                                                    "title": "480p (SD)",
                                                    "value": "bestvideo[height<=480]+bestaudio/best[height<=480]"
                                                },
                                                {
                                                    "title": "360p",
                                                    "value": "bestvideo[height<=360]+bestaudio/best[height<=360]"
                                                },
                                                {
                                                    "title": "240p",
                                                    "value": "bestvideo[height<=240]+bestaudio/best[height<=240]"
                                                }
                                            ]
                                        },
                                        {
                                            "type": "ColumnSet",
                                            "columns": [
                                                {
                                                    "type": "Column",
                                                    "width": "stretch",
                                                    "items": [
                                                        {
                                                            "type": "Input.Toggle",
                                                            "title": "Download Only Audio",
                                                            "id": "audioOnly"
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
                                                                    "type": "Action.Execute",
                                                                    "title": "Voltar",
                                                                    "data": {
                                                                        "actions": "goBack"
                                                                    }
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
                            ]
                        }
                    ]
                }
                """);
        public PlaylistFormContent(SettingsManager settings, JObject jsonData, DownloadHelper ytDlp, Action<CancellationTokenSource> onSubmit)
        {
            _settings = settings;
            _ytDlp = ytDlp;
            _onSubmit = onSubmit;
            var videoURL = jsonData["videoURL"]?.ToString();
            var title = jsonData["title"]?.ToString();
            var thumbnail = jsonData["thumbnail"]?.ToString();
            var downloadPath = jsonData["downloadPath"]?.ToString();
            _playlistTitle = jsonData["playlistTitle"]?.ToString() ?? "";
            _jsonData?.Add("title", videoURL);
            _jsonData?.Add("videoURL", videoURL);
            _jsonData?.Add("thumbnail", thumbnail);
            _jsonData?.Add("downloadPath", downloadPath);
            TemplateJson = _templateJson.ToString();
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
            var videoURL = _jsonData["videoURL"]?.ToString() ?? "";
            var downloadPath = downloadPathFromPayload;
            if (downloadPath == "") downloadPath = Path.Combine(_ytDlp.GetSettingsDownloadPath(), _playlistTitle);
            var token = new CancellationTokenSource();
            _ = _ytDlp.TryExecutePlaylistDownloadAsync(videoURL, downloadPath, resolution, audioOnly: audioOnly, cancellationToken: token.Token);
            _onSubmit.Invoke(token);
            return CommandResult.GoBack();
        }
    }
}
