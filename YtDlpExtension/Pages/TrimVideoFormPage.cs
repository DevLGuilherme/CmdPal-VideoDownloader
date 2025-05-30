using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Threading;
using YtDlpExtension.Helpers;
using YtDlpExtension.Metada;

namespace YtDlpExtension.Pages
{
    public class TrimVideoFormPage : ContentPage
    {
        private readonly SettingsManager _settings;
        private readonly VideoData _videoData;
        private readonly DownloadHelper _ytDlp;
        private readonly Format _formatData;
        private readonly string _url;
        private readonly Action<CancellationTokenSource> _onSubmit;

        public TrimVideoFormPage(string queryUrl, SettingsManager settings, VideoData videoData, Format formatData, DownloadHelper ytDlp, Action<CancellationTokenSource> onSubmit = null)
        {
            _settings = settings;
            _videoData = videoData;
            _ytDlp = ytDlp;
            _formatData = formatData;
            _url = queryUrl;
            _onSubmit = onSubmit;
            Icon = new IconInfo("\uf0c7");

        }

        public override IContent[] GetContent()
        {
            return [
                new TreeContent
                {
                    RootContent = new TrimVideoFormContent(_url, _settings, _videoData, _formatData, _ytDlp, _onSubmit),
                    Children = []
                }
            ];
        }
    }

    public class TrimVideoFormContent : FormContent
    {
        private readonly SettingsManager _settings;
        private VideoData _videoData = new();
        private readonly string _url;
        private Format _formatData = new();
        private readonly DownloadHelper _ytDlp;
        private readonly Action<CancellationTokenSource> _onSubmit;

        public TrimVideoFormContent(string queryUrl, SettingsManager settings, VideoData videoData, Format formatData, DownloadHelper ytDlp, Action<CancellationTokenSource> onSubmit = null)
        {
            _settings = settings;
            _ytDlp = ytDlp;
            _onSubmit = onSubmit;
            _videoData = videoData;
            _formatData = formatData;
            _url = queryUrl;
            long filesize = formatData.filesize ?? 0;
            float bitrate = formatData.tbr ?? 0;
            var duration = videoData.Duration != null ? TimeSpan.FromSeconds(videoData.Duration ?? 0) : GetDurationFromSizeAndBitrate(filesize, bitrate);
            var formattedDuration = duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
            var endTime = formattedDuration;
            var dataJson = $$"""
                    {
                        "videoTitle": "{{videoData.Title}}",
                        "thumbnail": "{{videoData.Thumbnail}}",
                        "resolution": "{{formatData.resolution}}",
                        "formatId": "{{_formatData.format_id}}"
                    }
                """;
            var templateJson = $$"""
                {
                "type": "AdaptiveCard",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "${videoTItle}",
                        "wrap": true,
                        "weight": "Bolder",
                        "size": "Large",
                        "fontType": "Default"
                    },
                    {
                        "type": "TextBlock",
                        "text": "${resolution} ${formatId}",
                        "wrap": true,
                        "weight": "Bolder",
                        "size": "Default",
                        "fontType": "Default"
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
                                        "type": "ColumnSet",
                                        "columns": [
                                            {
                                                "type": "Column",
                                                "width": "stretch",
                                                "items": [
                                                    {
                                                      "type": "Input.Text",
                                                      "id": "startTime",
                                                      "label": "{{"Start".ToLocalized()}} (HH:mm:ss)",
                                                      "value": "00:00:00",
                                                      "placeholder": "00:00:00",
                                                      "regex": "^([0-1]?\\d|2[0-3]):[0-5]\\d:[0-5]\\d$",
                                                      "errorMessage": "Invalid Format. Use HH:mm:ss"
                                                    },
                                                    {
                                                      "type": "Input.Text",
                                                      "id": "endTime",
                                                      "label": "{{"End".ToLocalized()}} (HH:mm:ss)",
                                                      "value": "{{endTime}}",
                                                      "placeholder": "00:00:10",
                                                      "regex": "^([0-1]?\\d|2[0-3]):[0-5]\\d:[0-5]\\d$",
                                                      "errorMessage": "Invalid Format. Use HH:mm:ss"
                                                    }
                                                ]
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                ],
                "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
                "version": "1.6",
                "actions": [
                    {
                        "type": "Action.Submit",
                        "title": "Download"
                    }
                ],
                "verticalContentAlignment": "Center"
            }


            """;
            DataJson = dataJson;
            TemplateJson = templateJson;
        }


        public static TimeSpan GetDurationFromSizeAndBitrate(long filesize, float bitrate)
        {
            var duration = (filesize * 8) / (bitrate * 1000);
            return TimeSpan.FromSeconds(duration);
        }

        public override CommandResult SubmitForm(string inputs)
        {
            var formInput = JObject.Parse(inputs);
            if (formInput == null)
            {
                return CommandResult.GoHome();
            }
            var startTime = formInput["startTime"]?.ToString() ?? "00:00:00";
            var duration = TimeSpan.FromSeconds(_videoData.Duration ?? 0);
            var formattedDuration = duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

            var endTime = formInput["endTime"]?.ToString() ?? formattedDuration;

            if (TimeSpan.TryParse(startTime, out var startTs) && TimeSpan.TryParse(endTime, out var endTs))
            {
                if (startTs >= endTs)
                {
                    return CommandResult.Confirm(new ConfirmationArgs
                    {
                        Title = "Error".ToLocalized(),
                        Description = "TrimTimeError".ToLocalized(),
                    });
                }
            }

            var videoFormatId = _formatData.format_id ?? string.Empty; // Ensure videoFormatId is not null to avoid warnings

            if (string.IsNullOrEmpty(videoFormatId))
            {
                return CommandResult.Confirm(new ConfirmationArgs
                {
                    Title = "Error".ToLocalized(),
                    Description = "The video format ID is missing.",
                });
            }

            var downloadBanner = new StatusMessage();
            _ = _ytDlp.TryExecuteDownloadAsync(
                _url,
                downloadBanner,
                _videoData.Title ?? "MissingTitle".ToLocalized(),
                videoFormatId,
                startTime,
                endTime
            );

            return CommandResult.GoBack();
        }
    }
}
