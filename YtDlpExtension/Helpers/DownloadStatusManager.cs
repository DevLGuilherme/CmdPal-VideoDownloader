using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpExtension.Helpers
{
    internal enum DownloadState
    {
        Extracting,
        Downloading,
        AlreadyDownloaded,
        Finished,
        Cancelled,
        CustomMessage
    }

    internal class DownloadStatusManager
    {
        private StatusMessage _banner = new();
        // This field is responsible to manage state updates
        // if a rapid state change occurs this should be able to handle if hide or not the banner
        private int _updateVersion = 0;
        public DownloadState CurrentState { get; private set; }

        public void UpdateState(DownloadState state, string message = "", bool isIndeterminate = false, uint progressPercent = 0)
        {
            CurrentState = state;
            (_banner.Message, _banner.State, _banner.Progress) = state switch
            {
                DownloadState.Extracting => ("Extracting".ToLocalized(), MessageState.Info, new ProgressState { IsIndeterminate = true }),
                DownloadState.Downloading => (message, MessageState.Info, new ProgressState { IsIndeterminate = false, ProgressPercent = progressPercent }),
                DownloadState.AlreadyDownloaded => ("AlreadyDownloaded".ToLocalized(message), MessageState.Warning, null),
                DownloadState.Finished => ("Downloaded".ToLocalized(message), MessageState.Success, null),
                DownloadState.Cancelled => ("Cancelled".ToLocalized(), MessageState.Error, null),
                _ => (message, MessageState.Info, new ProgressState { IsIndeterminate = isIndeterminate })
            };

            var versionAtUpdate = ++_updateVersion;

            if (state is not DownloadState.Downloading and not DownloadState.Extracting)
            {

                var cts = new CancellationTokenSource();
                var token = cts.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000).ConfigureAwait(false);

                        // Only hides if the stated doesn't change within 5 sec
                        if (versionAtUpdate == _updateVersion)
                        {
                            Hide();
                        }
                    }
                    catch (TaskCanceledException) { /* Ignored */ }
                });
            }
        }

        public void ShowStatus()
        {
            YtDlpExtensionHost.Instance?.ShowStatus(_banner, StatusContext.Extension);
        }

        public void ClearStatus()
        {
            _banner = new();

        }

        public void UpdateProgress(double percent)
        {
            _banner.Progress = new ProgressState
            {
                IsIndeterminate = false,
                ProgressPercent = (uint)Math.Floor(percent)
            };

            YtDlpExtensionHost.Instance?.ShowStatus(_banner, StatusContext.Extension);
        }

        public void Hide() => YtDlpExtensionHost.Instance?.HideStatus(_banner);
    }
}
