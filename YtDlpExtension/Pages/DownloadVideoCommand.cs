using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading;
using YtDlpExtension.Helpers;

namespace YtDlpExtension.Pages
{
    internal class DownloadVideoCommand : InvokableCommand
    {
        private readonly string _url;
        private readonly StatusMessage _downloadBanner;
        private readonly string _videoTitle;
        private readonly string _videoFormatId;
        private readonly string _audioFormatId;
        private readonly bool _audioOnly;
        private readonly Action _onStart;
        private readonly Action _onFinish;
        private readonly Action _onAlreadyDownloaded;
        private readonly CancellationToken _cancellationToken;
        private readonly DownloadHelper _ytDlp;

        public DownloadVideoCommand(
            string url,
            StatusMessage downloadBanner,
            string videoTitle,
            string videoFormatId,
            DownloadHelper ytDlp,
            string audioFormatId = "bestaudio",
            bool audioOnly = false,
            Action? onStart = null,
            Action? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {
            _ytDlp = ytDlp;
            _downloadBanner = downloadBanner;
            _url = url;
            _videoTitle = videoTitle;
            _videoFormatId = videoFormatId;
            _audioFormatId = audioFormatId;
            _audioOnly = audioOnly;
            _onStart = onStart;
            _onFinish = onFinish;
            _onAlreadyDownloaded = onAlreadyDownloaded;
            _cancellationToken = cancellationToken;


        }

        public override ICommandResult Invoke()
        {
            _ = _ytDlp.TryExecuteDownloadAsync(
                        _url,
                        _downloadBanner,
                        _videoTitle,
                        _videoFormatId,
                        _audioFormatId,
                        _audioOnly,
                        _onStart,
                        _onFinish,
                        _onAlreadyDownloaded,
                        _cancellationToken
                    );


            return CommandResult.KeepOpen();
        }

        public override ICommandResult Invoke(object? sender)
        {
            return base.Invoke(sender);
        }
    }
}
