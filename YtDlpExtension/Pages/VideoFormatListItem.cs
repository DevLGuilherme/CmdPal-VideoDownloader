using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using YtDlpExtension.Helpers;
namespace YtDlpExtension.Pages
{
    internal class VideoFormatListItem : ListItem
    {

        private readonly DownloadHelper _ytDlp;
        private CancellationTokenSource token = new();

        public VideoFormatListItem(string queryURL, string videoTitle, string thumbnail, JObject videoFormatObject, DownloadHelper ytDlp)
        {
            _ytDlp = ytDlp;
            var formatId = videoFormatObject["format_id"]?.ToString() ?? "";
            var resolution = videoFormatObject["resolution"]?.ToString() ?? "";
            var ext = videoFormatObject["ext"]?.ToString() ?? "";

            List<Tag> _tags = [new Tag(formatId), new Tag(ext)];

            List<IContextItem> _commands = [];
            var downloadAudioOnlyCommand = new CommandContextItem(
                        title: "DownloadAudio".ToLocalized(),
                        name: "DownloadAudio".ToLocalized(),
                        subtitle: "DownloadAudio".ToLocalized(),
                        result: CommandResult.ShowToast(new ToastArgs() { Message = "DownloadAudio".ToLocalized(), Result = CommandResult.KeepOpen() }),
                        action: () => { }
                    );
            _commands.Add(downloadAudioOnlyCommand);
            //The command will be set at the end 
            //in order to update the command once the download starts
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
                    token?.Cancel();
                    Subtitle = "";
                    Command = startDownloadCommand;
                    // The token needs to be renewed after the cancel or
                    // the format will not be available to download again
                    token = new();
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
                Subtitle = "AlreadyDownloaded".ToLocalized();
                Command = null;
            };

            startDownloadCommand = new AnonymousCommand(async () =>
            {
                _ = await _ytDlp.TryExecuteDownloadAsync(
                        queryURL,
                        videoTitle,
                        formatId,
                        onStart: onDownloadStart,
                        onFinish: onDownloadFinished,
                        onAlreadyDownloaded: onAlreadyDownloaded,
                        cancellationToken: token.Token
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
    }
}
