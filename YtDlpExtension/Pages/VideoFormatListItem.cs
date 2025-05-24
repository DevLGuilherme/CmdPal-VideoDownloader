using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public DownloadState GetDownloadState => _downloadBanner.CurrentState();

        public VideoFormatListItem(string queryURL, string videoTitle, string thumbnail, Format videoFormatObject, DownloadHelper ytDlp, SettingsManager settings)
        {
            var uiSettings = new UISettings();
            var accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            var foregroundColor = uiSettings.GetColorValue(UIColorType.Foreground);
            _ytDlp = ytDlp;
            _settings = settings;
            var formatId = videoFormatObject.format_id ?? "";
            var resolution = videoFormatObject.resolution ?? "";
            var ext = videoFormatObject.ext ?? "";
            List<Tag> _tags = [new Tag(formatId),
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

            audioOnlyContextItem.Command = CreateDownloadWithCancelCommand(
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
            Details = BuildDetails(videoTitle, thumbnail, videoFormatObject);
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

            var onDownloadFinished = () =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "Downloaded".ToLocalized();
                Command = startDownloadCommand;
            };

            var onAlreadyDownloaded = () =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "AlreadyDownloaded".ToLocalized(videoTitle);
                Command = null;
            };

            startDownloadCommand = new AnonymousCommand(async () =>
            {
                await _ytDlp.TryExecuteDownloadAsync(
                        queryURL,
                        _downloadBanner,
                        videoTitle,
                        formatId,
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

        public VideoFormatListItem(ICommand command)
        {
            Command = command;
        }

        public VideoFormatListItem(string queryURL, string videoTitle, string thumbnail, VideoData videoSingleFormat, DownloadHelper ytDlp, SettingsManager settings, bool isBestFormat = false)
        {
            var uiSettings = new UISettings();
            var accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            var foregroundColor = uiSettings.GetColorValue(UIColorType.Foreground);
            _ytDlp = ytDlp;
            _settings = settings;
            var formatId = videoSingleFormat.FormatID ?? "";
            var resolution = videoSingleFormat.resolution ?? "";
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

            audioOnlyContextItem.Command = CreateDownloadWithCancelCommand(
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

            var onDownloadFinished = () =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "Downloaded".ToLocalized();
                Command = startDownloadCommand;
            };

            var onAlreadyDownloaded = () =>
            {
                Icon = new IconInfo("\uE930");
                Subtitle = "AlreadyDownloaded".ToLocalized(videoTitle);
                Command = null;
            };

            startDownloadCommand = new AnonymousCommand(async () =>
            {
                await _ytDlp.TryExecuteDownloadAsync(
                        queryURL,
                        _downloadBanner,
                        videoTitle,
                        formatId,
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

        private Details? BuildDetails(string videoTitle, string thumbnail, VideoData videoFormatObject)
        {
            var formatId = videoFormatObject.FormatID ?? "";
            var resolution = videoFormatObject.resolution ?? "";
            var vcodec = videoFormatObject.vcodec ?? "";
            var acodec = videoFormatObject.acodec ?? "";
            var ext = videoFormatObject.Extension ?? "";
            var filesizeBytes = videoFormatObject.filesize
                  ?? videoFormatObject.filesize_approx;
            double sizeInMB = 0d;
            if (filesizeBytes != null) sizeInMB = filesizeBytes.Value / (1024.0 * 1024.0);

            List<IDetailsElement> detailsElements = [];
            Dictionary<string, string> formatData = new()
            {
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
        private Details? BuildDetails(string videoTitle, string thumbnail, Format videoFormatObject = null, VideoData videoSingleFormat = null)
        {
            string formatId, resolution, vcodec, acodec, ext = string.Empty;
            long? filesizeBytes = null;
            if (videoFormatObject == null)
            {
                formatId = videoSingleFormat.FormatID ?? "";
                resolution = videoSingleFormat.resolution ?? "";
                vcodec = videoSingleFormat.vcodec ?? "";
                acodec = videoSingleFormat.acodec ?? "";
                ext = videoSingleFormat.Extension ?? "";
                filesizeBytes = videoSingleFormat.filesize
                      ?? videoSingleFormat.filesize_approx;
            }

            formatId = videoFormatObject.format_id ?? "";
            resolution = videoFormatObject.resolution ?? "";
            vcodec = videoFormatObject.vcodec ?? "";
            acodec = videoFormatObject.acodec ?? "";
            ext = videoFormatObject.ext ?? "";
            filesizeBytes = videoFormatObject.filesize
                  ?? videoFormatObject.filesize_approx;

            double sizeInMB = 0d;
            if (filesizeBytes != null) sizeInMB = filesizeBytes.Value / (1024.0 * 1024.0);

            List<IDetailsElement> detailsElements = [];
            Dictionary<string, string> formatData = new()
            {
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


        /// <summary>
        /// This method is responsible to create a cyclic interaction of commands
        /// <example>
        /// <code>
        /// Donwload -> Cancel -> Download -> ...
        /// </code>
        /// </example>
        /// </summary>
        public static ICommand CreateDownloadWithCancelCommand(
            Func<CancellationToken, Task> downloadFunc,
            string commandDownloadName,
            string commandCancelName
        )
        {
            var cancellationToken = new CancellationTokenSource();
            Action execute = () => { };

            var command = new AnonymousCommand(() => execute())
            {
                Name = commandDownloadName,
                Result = CommandResult.KeepOpen()
            };

            void SetDownloadCommand()
            {
                command.Name = commandDownloadName;
                execute = () =>
                {
                    _ = ExecuteDownload();
                    SetCancelCommand();
                };
            }

            void SetCancelCommand()
            {
                command.Name = commandCancelName;
                execute = () =>
                {
                    command.Result = CommandResult.Confirm(new ConfirmationArgs()
                    {
                        Title = "CancelDownload".ToLocalized(),
                        Description = "CancelDialogDescription".ToLocalized(),
                        IsPrimaryCommandCritical = true,
                        PrimaryCommand = new AnonymousCommand(() =>
                        {
                            cancellationToken.Cancel();
                            cancellationToken = new CancellationTokenSource();
                            SetDownloadCommand();
                        })
                        {
                            Name = "Confirm".ToLocalized(),
                            Result = CommandResult.KeepOpen(),
                        }
                    });
                };

            }

            async Task ExecuteDownload()
            {
                SetCancelCommand();
                try
                {
                    await downloadFunc(cancellationToken.Token);
                }
                finally
                {
                    SetDownloadCommand();
                }
            }


            SetDownloadCommand();

            return command;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
