using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpExtension.Helpers
{
    public enum DownloadState
    {
        Extracting,
        Downloading,
        AlreadyDownloaded,
        Finished,
        Cancelled,
        CustomMessage
    }

    public static class DownloadStatusManager
    {
        private static readonly ConditionalWeakTable<StatusMessage, DownloadStatusState> _states = new();

        private class DownloadStatusState
        {
            public DownloadState CurrentState { get; set; } = DownloadState.CustomMessage;
            public int UpdateVersion { get; set; }
        }

        private static DownloadStatusState GetState(this StatusMessage banner)
        {
            return _states.GetOrCreateValue(banner);
        }

        public static DownloadState CurrentState(this StatusMessage banner)
        {
            return banner.GetState().CurrentState;
        }

        public static void UpdateState(this StatusMessage banner, DownloadState state, string message = "", bool isIndeterminate = false, uint progressPercent = 0)
        {
            var stateObj = banner.GetState();
            stateObj.CurrentState = state;

            (banner.Message, banner.State, banner.Progress) = state switch
            {
                DownloadState.Extracting => ("Extracting".ToLocalized(), MessageState.Info, new ProgressState { IsIndeterminate = true }),
                DownloadState.Downloading => (message, MessageState.Info, new ProgressState { IsIndeterminate = false, ProgressPercent = progressPercent }),
                DownloadState.AlreadyDownloaded => ("AlreadyDownloaded".ToLocalized(message), MessageState.Warning, null),
                DownloadState.Finished => ("Downloaded".ToLocalized(message), MessageState.Success, null),
                DownloadState.Cancelled => ("Cancelled".ToLocalized(), MessageState.Error, null),
                _ => (message, MessageState.Info, new ProgressState { IsIndeterminate = isIndeterminate })
            };

            var versionAtUpdate = ++stateObj.UpdateVersion;

            if (state is not DownloadState.Downloading and not DownloadState.Extracting)
            {
                var cts = new CancellationTokenSource();
                var token = cts.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000, token).ConfigureAwait(false);
                        if (versionAtUpdate == stateObj.UpdateVersion)
                        {
                            banner.Hide();
                        }
                    }
                    catch (TaskCanceledException) { /* Ignored */ }
                });
            }
        }

        public static void ShowStatus(this StatusMessage banner)
        {
            YtDlpExtensionHost.Instance?.ShowStatus(banner, StatusContext.Extension);
        }

        public static void ClearStatus(this StatusMessage banner)
        {
            banner.Message = "";
            banner.State = new MessageState();
            banner.Progress = null;
        }

        public static void UpdateProgress(this StatusMessage banner, double percent)
        {
            banner.Progress = new ProgressState
            {
                IsIndeterminate = false,
                ProgressPercent = (uint)Math.Floor(percent)
            };

            banner.ShowStatus();
        }

        public static void Hide(this StatusMessage banner)
        {
            YtDlpExtensionHost.Instance?.HideStatus(banner);
        }
    }
}
