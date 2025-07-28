using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Threading;
using Windows.UI.ViewManagement;
using YtDlpExtension.Helpers;
using YtDlpExtension.Metada;
namespace YtDlpExtension.Pages
{
    public sealed partial class VideoFormatListItem : ListItem, IDisposable
    {

        private readonly DownloadHelper _ytDlp;
        private CancellationTokenSource _token = new();
        private readonly SettingsManager _settings;
        private StatusMessage _downloadBanner = new();
        private StatusMessage _downloadAudioOnlyBanner = new();
        public string? VideoUrl { get; }
        private string? _videoTitle;
        private string? _thumbnail;
        private VideoData? _videoData;
        private Format? _formatData;
        public DownloadState GetDownloadState => _downloadBanner.CurrentState();
        public Format? GetFormatData => _formatData != null ? _formatData : null;

        /// <summary>
        /// This Constructor is used to create a video with a format list from yt-dlp.
        /// </summary>
        /// <param name="queryURL"></param>
        /// <param name="videoTitle"></param>
        /// <param name="thumbnail"></param>
        /// <param name="videoFormatObject"></param>
        /// <param name="ytDlp"></param>
        /// <param name="settings"></param>
        public VideoFormatListItem(string queryURL, string videoTitle, string thumbnail, Format videoFormatObject, DownloadHelper ytDlp, SettingsManager settings, float? duration = 0, bool? isLive = false)
        {
            var uiSettings = new UISettings();
            var accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            var foregroundColor = uiSettings.GetColorValue(UIColorType.Foreground);
            VideoUrl = queryURL;
            _ytDlp = ytDlp;
            _settings = settings;
            _thumbnail = thumbnail;
            _videoTitle = videoTitle;
            _formatData = videoFormatObject;
            var formatId = videoFormatObject.FormatID ?? "";
            // var resolution = (_settings.GetSelectedMode == "simple" ? GetResolutionCategory(videoFormatObject.width, videoFormatObject.height) : videoFormatObject.resolution) ?? "";
            var resolution = videoFormatObject.Resolution ?? "";
            var resolutionCategory = GetResolutionCategory(videoFormatObject.Width, videoFormatObject.Height);
            var ext = videoFormatObject.Ext ?? "";

            List<Tag> _tags = [];

            if (_settings.GetSelectedMode == ExtensionMode.ADVANCED)
            {


                _tags.Add(new Tag(formatId));


                if (videoFormatObject.Resolution!.Contains("audio only") && videoFormatObject.FormatNote != null)
                {

                    var isDubbed = (videoFormatObject.FormatNote is var format && format.Contains("dubbed")) switch
                    {
                        true => format.Contains("auto") switch
                        {
                            true => new Tag("Dubbed-Auto"),
                            _ => new Tag("Dubbed")
                        },
                        _ => format.Contains("original") switch
                        {
                            true => new Tag("original"),
                            _ => null
                        }
                    };

                    if (isDubbed != null)
                    {
                        _tags.Add(isDubbed);
                    }

                }

                _tags.Add(new Tag(ext)
                {
                    Background = new OptionalColor(true, new Color(accentColor.R, accentColor.G, accentColor.B, accentColor.A)),
                    Foreground = new OptionalColor(true, new Color(foregroundColor.R, foregroundColor.G, foregroundColor.B, foregroundColor.A))
                });
            }

            List<IContextItem> _commands = [];

            var downloadAudioTitle = "DownloadAudio".ToLocalized(_settings.GetSelectedAudioOutputFormat);
            var audioOnlyContextItem = new CommandContextItem(
                    title: downloadAudioTitle,
                    name: downloadAudioTitle,
                    subtitle: downloadAudioTitle,
                    result: CommandResult.ShowToast(new ToastArgs
                    {
                        Message = downloadAudioTitle,
                        Result = CommandResult.KeepOpen()
                    })
            )
            { Icon = new IconInfo("\uec4f") };

            audioOnlyContextItem.Command = CommandsHelper.CreateDownloadWithCancelCommand(
                async token => await _ytDlp.TryExecuteDownloadAsync(
                    queryURL,
                    _downloadAudioOnlyBanner,
                    videoTitle,
                    string.Empty,
                    audioOnly: true,
                    cancellationToken: token
                ),
                downloadAudioTitle,
                "CancelDownload".ToLocalized()
            );
            _commands.Add(audioOnlyContextItem);

            if (_settings.GetSelectedMode == ExtensionMode.SIMPLE)
            {
                Title = resolutionCategory;
            }
            else
            {
                if (videoFormatObject.Resolution!.Contains("audio only") && videoFormatObject.Language != null)
                {
                    Title = $"{resolution} ({FormatHelper.TryGetNativeName(videoFormatObject.Language)})";
                }
                else
                {
                    Title = resolution;
                }
            }

            Icon = new IconInfo("\uE896");
            Tags = _tags.ToArray();
            Details = BuildDetails(videoTitle, thumbnail, videoFormatObject);
            MoreCommands = _commands.ToArray();

            var isLiveVideo = isLive ?? false;

            Command startDownloadCommand = new NoOpCommand();


            //Once download starts, the download command is updated to "Cancel Download"
            var onDownloadStart = () =>
            {
                Subtitle = "Downloading...";
                Command = new AnonymousCommand(() =>
                {
                    _token?.Cancel();
                    Subtitle = "";
                    Command = startDownloadCommand;
                    // The token needs to be renewed after the cancel or
                    // the format will not be available to download again
                    _token = new();
                })
                {
                    Name = "CancelDownload".ToLocalized(),
                    Icon = new IconInfo("\uE711"),
                    Result = CommandResult.KeepOpen()
                };
            };

            Action<Command> onDownloadFinished = (openFileCommand) =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "Downloaded".ToLocalized();

                if (openFileCommand != null)
                {
                    Command = openFileCommand;
                }
            };

            var onAlreadyDownloaded = () =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "AlreadyDownloaded".ToLocalized(videoTitle);
                Command = null;
            };

            //var downloadCommandResult = isLiveResult ? CommandResult.Confirm(new ConfirmationArgs()
            //{
            //    IsPrimaryCommandCritical = true,
            //    PrimaryCommand = new AnonymousCommand(async () =>
            //    {
            //        await _ytDlp.TryExecuteDownloadAsync(
            //                queryURL,
            //                _downloadBanner,
            //                videoTitle,
            //                formatId,
            //                isLive: isLive ?? false,
            //                onStart: onDownloadStart,
            //                onFinish: onDownloadFinished,
            //                onAlreadyDownloaded: onAlreadyDownloaded,
            //                cancellationToken: _token.Token
            //            );
            //    })
            //    {
            //        Name = $"Download ({settings.GetSelectedVideoOutputFormat})",
            //        Icon = new IconInfo("\uE896"),
            //    },
            //    Title = "Warning",
            //    Description = "Live streaming is not fully supported, a new window will open and begin the download. YOU HAVE TO INTERRUPT THE PROCESS YOURSELF (With Ctrl+C) in order to stop the download."
            //}) : CommandResult.KeepOpen();

            if (isLiveVideo)
            {
                Command = new AnonymousCommand(() =>
                {
                    // Apenas exibe a confirmação, não inicia o download direto
                })
                {
                    Name = "Download (Live)",
                    Icon = new IconInfo("\uE896"),
                    Result = CommandResult.Confirm(new ConfirmationArgs
                    {
                        Title = "Aviso",
                        Description = "O download de livestreams não é totalmente suportado. Uma nova janela será aberta e você deverá encerrá-la manualmente (Ctrl+C) para finalizar o download.",
                        IsPrimaryCommandCritical = true,
                        PrimaryCommand = new AnonymousCommand(async () =>
                        {
                            await _ytDlp.TryExecuteDownloadAsync(
                                queryURL,
                                _downloadBanner,
                                videoTitle,
                                videoFormatObject.FormatID!,
                                isLive: true,
                                onStart: onDownloadStart,
                                onFinish: onDownloadFinished,
                                onAlreadyDownloaded: onAlreadyDownloaded,
                                cancellationToken: _token.Token
                            );
                        })
                        {
                            Name = $"Iniciar Download ({settings.GetSelectedVideoOutputFormat})",
                            Icon = new IconInfo("\uE896"),
                            Result = CommandResult.KeepOpen()
                        }
                    })
                };
            }
            else
            {
                startDownloadCommand = new AnonymousCommand(async () =>
                {
                    await _ytDlp.TryExecuteDownloadAsync(
                            queryURL,
                            _downloadBanner,
                            videoTitle,
                            formatId,
                            isLive: isLive ?? false,
                            onStart: onDownloadStart,
                            onFinish: onDownloadFinished,
                            onAlreadyDownloaded: onAlreadyDownloaded,
                            cancellationToken: _token.Token
                        );
                })
                {
                    Name = $"Download ({settings.GetSelectedVideoOutputFormat})",
                    Icon = new IconInfo("\uE896"),
                    Result = CommandResult.KeepOpen()
                };

                Command = startDownloadCommand;
            }

        }

        public VideoFormatListItem(ICommand command, DownloadHelper ytDlp, SettingsManager settings)
        {
            Command = command;
            _ytDlp = ytDlp;
            _settings = settings;
        }

        /// <summary>
        /// This Constructor handles the case where a video does not have a format list, but only a single format.
        /// </summary>
        /// <param name="queryURL"></param>
        /// <param name="videoTitle"></param>
        /// <param name="thumbnail"></param>
        /// <param name="videoSingleFormat"></param>
        /// <param name="ytDlp"></param>
        /// <param name="settings"></param>
        public VideoFormatListItem(string queryURL, string videoTitle, string thumbnail, VideoData videoSingleFormat, DownloadHelper ytDlp, SettingsManager settings, bool isBestFormat = false)
        {
            var uiSettings = new UISettings();
            var accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            var foregroundColor = uiSettings.GetColorValue(UIColorType.Foreground);
            VideoUrl = queryURL;
            _videoData = videoSingleFormat;
            _ytDlp = ytDlp;
            _settings = settings;
            _videoTitle = videoTitle;
            _thumbnail = thumbnail;
            var formatId = videoSingleFormat.FormatID ?? "";
            var resolution = videoSingleFormat.Resolution ?? "";
            var ext = videoSingleFormat.Extension ?? "";

            List<Tag> _tags = [
                new Tag(formatId),
                new Tag(ext) {
                    Background = new OptionalColor(true, new Color(accentColor.R, accentColor.G, accentColor.B, accentColor.A)),
                    Foreground = new OptionalColor(true, new Color(foregroundColor.R, foregroundColor.G, foregroundColor.B, foregroundColor.A))
                }
            ];

            List<IContextItem> _commands = [];

            var downloadAudioTitle = "DownloadAudio".ToLocalized(_settings.GetSelectedAudioOutputFormat);
            var audioOnlyContextItem = new CommandContextItem(
                    title: downloadAudioTitle,
                    name: downloadAudioTitle,
                    subtitle: downloadAudioTitle,
                    result: CommandResult.ShowToast(new ToastArgs
                    {
                        Message = downloadAudioTitle,
                        Result = CommandResult.KeepOpen()
                    })
            )
            { Icon = new IconInfo("\uec4f") };

            audioOnlyContextItem.Command = CommandsHelper.CreateDownloadWithCancelCommand(
                async token => await _ytDlp.TryExecuteDownloadAsync(
                    queryURL,
                    _downloadAudioOnlyBanner,
                    videoTitle,
                    string.Empty,
                    audioOnly: true,
                    cancellationToken: token
                ),
                downloadAudioTitle,
                "CancelDownload".ToLocalized()
            );

            _commands.Add(audioOnlyContextItem);

            Title = resolution;
            Icon = new IconInfo("\uE896");
            Tags = _tags.ToArray();
            Details = BuildDetails(videoTitle, thumbnail, videoSingleFormat);
            MoreCommands = _commands.ToArray();
            Command startDownloadCommand = new NoOpCommand();


            //Once download starts, the download command is updated to "Cancel Download"
            var onDownloadStart = () =>
            {
                Subtitle = "Downloading...";
                Command = new AnonymousCommand(() =>
                {
                    _token?.Cancel();
                    Subtitle = "";
                    Command = startDownloadCommand;
                    // The token needs to be renewed after the cancel or
                    // the format will not be available to download again
                    _token = new();
                })
                {
                    Name = "CancelDownload".ToLocalized(),
                    Icon = new IconInfo("\uE711"),
                    Result = CommandResult.KeepOpen()
                };
            };


            Action<Command> onDownloadFinished = (_) =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "Downloaded".ToLocalized();

            };

            var onAlreadyDownloaded = () =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "AlreadyDownloaded".ToLocalized(videoTitle);
                Command = null;
            };

            startDownloadCommand = new AnonymousCommand(async () =>
            {
                // Command to show the file in explorer after download finishes
                var showFileInExplorer = await _ytDlp.TryExecuteDownloadAsync(
                        queryURL,
                        _downloadBanner,
                        videoTitle,
                        formatId,
                        isLive: videoSingleFormat.IsLive ?? false,
                        onStart: onDownloadStart,
                        onFinish: onDownloadFinished,
                        onAlreadyDownloaded: onAlreadyDownloaded,
                        cancellationToken: _token.Token
                 );
            })
            {
                Name = "Download",
                Icon = new IconInfo("\uE896"),
                Result = CommandResult.KeepOpen()
            };

            Command = startDownloadCommand;


        }

        public static string GetResolutionCategory(int? width, int? height)
        {
            return width switch
            {
                >= 3840 => "2160p (4K)",
                >= 2560 => "1440p (2K)",
                >= 1920 => "1080p",
                >= 1280 => "720p",
                >= 854 => "480p",
                >= 640 => "360p",
                >= 426 => "240p",
                >= 256 => "144p",
                _ => width.HasValue && height.HasValue ? $"{width}x{height}" : string.Empty
            };
        }

        public static long? EstimateFilesize(double? tbr, double? durationSeconds)
        {
            if (tbr.HasValue && durationSeconds.HasValue)
            {
                double sizeInBytes = (tbr.Value * 1000 / 8) * durationSeconds.Value;
                return (long)Math.Round(sizeInBytes);
            }

            return null;
        }

        private static Details? BuildDetails(string videoTitle, string thumbnail, VideoData videoFormatObject)
        {
            var formatId = videoFormatObject.FormatID ?? "";
            var resolution = videoFormatObject.Resolution ?? "";
            var formatNote = videoFormatObject.FormatNote ?? "";
            var vcodec = videoFormatObject.VCodec ?? "";
            var acodec = videoFormatObject.ACodec ?? "";
            var ext = videoFormatObject.Extension ?? "";
            var filesizeBytes = videoFormatObject.Filesize
                  ?? videoFormatObject.FilesizeApprox;
            double sizeInMB = 0d;
            if (filesizeBytes != null) sizeInMB = filesizeBytes.Value / (1024.0 * 1024.0);

            List<IDetailsElement> detailsElements = [];
            Dictionary<string, string> formatData = new()
            {
                { "Format".ToLocalized(), formatNote },
                { "Resolution".ToLocalized(), resolution },
                { "Size".ToLocalized(), $"{sizeInMB:F2}" },
                { "Extension".ToLocalized(), ext },
                { "FormatId".ToLocalized(), formatId },
                { "VCodec".ToLocalized(), vcodec},
                { "ACodec".ToLocalized(), acodec },
            };

            foreach (var data in formatData)
            {
                var pair = new DetailsElement()
                {
                    Data = new DetailsLink() { Link = null, Text = data.Value },
                    Key = data.Key,
                };
                detailsElements.Add(pair);
            }

            return new Details()
            {
                Title = videoTitle,
                Body = $"""![Thumbnail]({thumbnail})""",
                Metadata = detailsElements.ToArray(),
            };
        }
        private static Details? BuildDetails(string videoTitle, string thumbnail, Format? videoFormatObject = null, VideoData? videoSingleFormat = null)
        {
            string formatId, formatNote, resolution, vcodec, acodec, ext = string.Empty;
            long? filesizeBytes = null;
            //if (videoFormatObject == null)
            //{
            //    formatId = videoSingleFormat?.FormatID ?? "";
            //    formatNote = videoSingleFormat?.FormatNote ?? "";
            //    resolution = videoSingleFormat?.Resolution ?? "";
            //    vcodec = videoSingleFormat?.VCodec ?? "";
            //    acodec = videoSingleFormat?.ACodec ?? "";
            //    ext = videoSingleFormat?.Extension ?? "";
            //    filesizeBytes = videoSingleFormat?.Filesize
            //          ?? videoSingleFormat?.FilesizeApprox;
            //}

            formatId = videoFormatObject?.FormatID ?? "";
            formatNote = videoFormatObject?.FormatNote ?? "";
            resolution = videoFormatObject?.Resolution ?? "";
            vcodec = videoFormatObject?.VCodec ?? "";
            acodec = videoFormatObject?.ACodec ?? "";
            ext = videoFormatObject?.Ext ?? "";
            filesizeBytes = videoFormatObject?.Filesize
                  ?? videoFormatObject?.FilesizeApprox;

            double sizeInMB = 0d;
            if (filesizeBytes != null) sizeInMB = filesizeBytes.Value / (1024.0 * 1024.0);

            List<IDetailsElement> detailsElements = [];
            Dictionary<string, string> formatData = new()
            {
                { "Format".ToLocalized(), formatNote },
                { "Resolution".ToLocalized(), resolution },
                { "VCodec".ToLocalized(), vcodec},
                { "ACodec".ToLocalized(), acodec },
                { "Size".ToLocalized(), $"{sizeInMB:F2}" },
                { "Extension".ToLocalized(), ext },
                { "FormatId".ToLocalized(), formatId },
            };

            foreach (var data in formatData)
            {
                var pair = new DetailsElement()
                {
                    Data = new DetailsLink() { Link = null, Text = data.Value },
                    Key = data.Key,
                };
                detailsElements.Add(pair);
            }

            return new Details()
            {
                Title = videoTitle,
                Body = $"""![Thumbnail]({thumbnail})""",
                Metadata = detailsElements.ToArray(),
            };
        }


        public void Dispose()
        {
            ((IDisposable)_token).Dispose();
        }
    }
}
