using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpExtension.Helpers
{
    internal sealed partial class DownloadHelper
    {
        public event Action<string>? TitleUpdated;
        public event Action<bool>? LoadingChanged;
        public event Action<int>? ItemsChanged;
        public readonly DownloadStatusManager _downloadBanner = new();
        private readonly SettingsManager _settings;
        private void SetTitle(string title) => TitleUpdated?.Invoke(title);
        private void SetLoading(bool isLoading) => LoadingChanged?.Invoke(isLoading);
        private void NotifyItemsChanged(int delta = 0) => ItemsChanged?.Invoke(delta);

        public DownloadHelper(SettingsManager settingsManager)
        {
            _settings = settingsManager;
        }
        public static string GetDefaultDownloadPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        public string GetSettingsDownloadPath() => _settings.DownloadLocation;
        public string GetSettingsVideoOutputFormat() => _settings.GetSelectedVideoOutputFormat;
        public string GetSettingsAudioOutputFormat() => _settings.GetSelectedAudioOutputFormat;
        public static bool IsValidUrl(string url)
        {
            var regexPattern = @"^(https?|ftp):\/\/[a-zA-Z0-9-]+(\.[a-zA-Z]{2,})+(\/[^\s]*)?$";
            return Regex.IsMatch(url.Trim(), regexPattern, RegexOptions.IgnoreCase);
        }

        private async Task<string> ExecuteDownloadProcessAsync(
            ProcessStartInfo psi,
            Action<string>? onOutputData = null,
            Action? onStart = null,
            Action? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {

            using var downloadProcess = new Process { StartInfo = psi };

            downloadProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    SetTitle(args.Data);
                    if (args.Data.Contains("has already been downloaded"))
                    {
                        var fullLogMessage = args.Data.Split("has")[0];
                        var videoTitle = fullLogMessage.Split("[download]")[1].Trim();
                        SetTitle("⚠️ The video has already been downloaded");
                        _downloadBanner.UpdateState(DownloadState.AlreadyDownloaded, videoTitle);
                        onAlreadyDownloaded?.Invoke();
                    }

                    onOutputData?.Invoke(args.Data);
                }
            };

            bool isCancelled = false;

            try
            {
                onStart?.Invoke();
                _downloadBanner.UpdateState(DownloadState.Extracting);

                downloadProcess.Start();
                SetLoading(true);
                _downloadBanner.ShowStatus();
                downloadProcess.BeginOutputReadLine();

                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!downloadProcess.HasExited)
                        {
                            downloadProcess.Kill(entireProcessTree: true);
                            isCancelled = true;
                        }
                    }
                    catch { /* Ignored */ }
                }))
                {
                    await downloadProcess.WaitForExitAsync(cancellationToken);
                }

                if (!isCancelled && downloadProcess.ExitCode == 0 && _downloadBanner.CurrentState != DownloadState.AlreadyDownloaded)
                {
                    SetTitle("✅ Download finished");
                    _downloadBanner.UpdateState(DownloadState.Finished);
                    SetLoading(false);
                    onFinish?.Invoke();
                }
                else if (isCancelled)
                {
                    SetTitle("⛔ Download cancelled");
                    _downloadBanner.UpdateState(DownloadState.Cancelled);
                    SetLoading(false);
                }
            }
            catch (OperationCanceledException)
            {
                SetTitle("⛔ Download cancelled");
                _downloadBanner.UpdateState(DownloadState.Cancelled);
                SetLoading(false);
            }
            SetLoading(false);
            return downloadProcess.HasExited ? downloadProcess.ExitCode.ToString(CultureInfo.InvariantCulture) : "-1";
        }

        public async Task<string> TryExecuteQueryAsync(string url, bool isPlaylist = false)
        {
            SetLoading(true);
            SetTitle($"Extracting URL: {url}");
            _downloadBanner.UpdateState(DownloadState.Extracting);
            var arguments = "--dump-json";
            if (isPlaylist) arguments = $"--no-playlist --dump-json";
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{arguments} {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            SetTitle(!string.IsNullOrEmpty(output)
                ? $"✅ Available Formats for: {url}"
                : "yt-dlp-extension");

            NotifyItemsChanged();
            SetLoading(false);

            return output;
        }

        public async Task<string> TryExecutePlaylistDownloadAsync(
            string url,
            string downloadPath,
            string videoFormat,
            string audioFormatId = "bestaudio",
            bool audioOnly = false,
            Action? onStart = null,
            Action? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {
            onStart?.Invoke();
            SetTitle($"Extracting URL: {url}");
            _downloadBanner.UpdateState(DownloadState.Extracting);
            var arguments = $"-P \"{downloadPath}\" --no-mtime --yes-playlist -f \"{videoFormat}\" --merge-output-format {GetSettingsVideoOutputFormat()}";
            if (audioOnly) arguments = $"-P \"{downloadPath}\" --no-mtime --yes-playlist -f \"{audioFormatId}\" --extract-audio --audio-format {GetSettingsAudioOutputFormat()}";

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{arguments} {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var downloadProcess = new Process { StartInfo = psi };
            var currentDownload = string.Empty;
            var totalDownloads = string.Empty;
            return await ExecuteDownloadProcessAsync(
                psi,
                onOutputData: (data) =>
                {
                    if (data.Contains("Downloading item"))
                    {
                        var stringParts = data.Split("of");
                        totalDownloads = stringParts[1].Trim();
                        currentDownload = stringParts[0].Split("item")[1].Trim();
                    }
                    if (Regex.Match(data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%") is var match && match.Success)
                    {
                        var downloadProgress = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        _downloadBanner.UpdateState(DownloadState.Downloading, "DownloadingPlaylist".ToLocalized(currentDownload, totalDownloads), progressPercent: (uint)Math.Floor(downloadProgress));
                    }
                },
                onStart,
                onFinish,
                onAlreadyDownloaded,
                cancellationToken
            );
        }

        public async Task<string> TryExecuteDownloadAsync(
            string url,
            string videoTitle,
            string videoFormatId,
            string audioFormatId = "bestaudio",
            bool audioOnly = false,
            Action? onStart = null,
            Action? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {
            onStart?.Invoke();
            _downloadBanner.UpdateState(DownloadState.Extracting);
            var downloadPath = GetSettingsDownloadPath();

            var arguments = audioOnly switch
            {
                false => $"--abort-on-unavailable-fragment --no-mtime --no-playlist -P \"{downloadPath}\" -f \"{videoFormatId}+{audioFormatId}\" --merge-output-format {GetSettingsVideoOutputFormat()} {url}",
                true => $"--abort-on-unavailable-fragment --no-mtime --no-playlist -P \"{downloadPath}\" -f \"{audioFormatId}\" --extract-audio --audio-format {GetSettingsAudioOutputFormat()} \"{url}\""
            };

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var downloadProcess = new Process { StartInfo = psi };

            return await ExecuteDownloadProcessAsync(
                psi,
                onOutputData: (data) =>
                {
                    if (Regex.Match(data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%") is var match && match.Success)
                    {
                        var downloadProgress = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        _downloadBanner.UpdateState(DownloadState.Downloading, "Downloading".ToLocalized(videoTitle), progressPercent: (uint)Math.Floor(downloadProgress));
                    }

                    NotifyItemsChanged(data.Length);
                },
                onStart,
                onFinish,
                onAlreadyDownloaded,
                cancellationToken
            );
        }

        public async Task<JObject> ExtractPlaylistDataAsync(string url, Action<JObject>? onFinish = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--dump-single-json --flat-playlist {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            try
            {
                var json = JObject.Parse(output);
                var title = json["title"]?.ToString() ?? "(no title)";
                var id = json["id"]?.ToString();
                var playlistCount = json["playlist_count"]?.ToString();
                var item = new JObject
                {
                    ["playlistTitle"] = title,
                    ["playlistCount"] = playlistCount
                };
                onFinish?.Invoke(item);
                return item;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro de JSON: {ex.Message}");
            }
            await process.WaitForExitAsync();
            return new();
        }

    }
}
