using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YtDlpExtension.Helpers;
namespace YtDlpExtension.Pages
{
    internal sealed partial class VideoFormatListItem : ListItem, IDisposable
    {

        private readonly DownloadHelper _ytDlp;
        private CancellationTokenSource _token = new();
        private readonly SettingsManager _settings;
        private StatusMessage _downloadBanner = new();
        private StatusMessage _downloadAudioOnlyBanner = new();
        public VideoFormatListItem(string queryURL, string videoTitle, string thumbnail, JObject videoFormatObject, DownloadHelper ytDlp, SettingsManager settings)
        {
            _ytDlp = ytDlp;
            _settings = settings;
            var formatId = videoFormatObject["format_id"]?.ToString() ?? "";
            var resolution = videoFormatObject["resolution"]?.ToString() ?? "";
            var ext = videoFormatObject["ext"]?.ToString() ?? "";
            List<Tag> _tags = [new Tag(formatId), new Tag(ext)];

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
            );

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

            //Action<CancellationToken> downloadAudioOnlyCommand = (cancellationToken) => { };
            //var audioOnlyCancellationToken = new CancellationTokenSource();

            //var onStartAudioOnly = () =>
            //{
            //    audioOnlyContextItem.Command = new AnonymousCommand(() =>
            //    {
            //        audioOnlyCancellationToken.Cancel();
            //        audioOnlyContextItem.Command = new AnonymousCommand(() => downloadAudioOnlyCommand(audioOnlyCancellationToken.Token))
            //        {
            //            Name = "DownloadAudio".ToLocalized(_settings.GetSelectedAudioOutputFormat),
            //            Result = CommandResult.KeepOpen()
            //        };
            //        audioOnlyCancellationToken = new();
            //    })
            //    {
            //        Name = "CancelDownload".ToLocalized(),
            //        Result = CommandResult.KeepOpen()
            //    }; ;
            //};

            //downloadAudioOnlyCommand = (cancellationToken) =>
            //{
            //    _ = _ytDlp.TryExecuteDownloadAsync(
            //        queryURL,
            //        videoTitle,
            //        string.Empty,
            //        audioOnly: true,
            //        onStart: onStartAudioOnly,
            //        cancellationToken: cancellationToken
            //    );
            //};
            //audioOnlyContextItem.Command = new AnonymousCommand(() => downloadAudioOnlyCommand(audioOnlyCancellationToken.Token))
            //{
            //    Name = "DownloadAudio".ToLocalized(_settings.GetSelectedAudioOutputFormat),
            //    Result = CommandResult.KeepOpen()
            //};
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
                _ = await _ytDlp.TryExecuteDownloadAsync(
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

        private Details? BuildDetails(string videoTitle, string thumbnail, JObject videoFormatObject)
        {
            var formatId = videoFormatObject["format_id"]?.ToString() ?? "";
            var resolution = videoFormatObject["resolution"]?.ToString() ?? "";
            var vcodec = videoFormatObject["vcodec"]?.ToString() ?? "";
            var acodec = videoFormatObject["acodec"]?.ToString() ?? "";
            var ext = videoFormatObject["ext"]?.ToString() ?? "";
            var filesizeBytes = videoFormatObject["filesize"]?.ToObject<long?>()
                  ?? videoFormatObject["filesize_approx"]?.ToObject<long?>();
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
