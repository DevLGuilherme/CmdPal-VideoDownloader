using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace YtDlpExtension.Helpers
{
    public sealed partial class DownloadHelper
    {
        public event Action<string>? TitleUpdated;
        public event Action<bool>? LoadingChanged;
        public event Action<int>? ItemsChanged;
        //        public readonly DownloadStatusManager _downloadBanner = new();
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
            StatusMessage downloadBanner,
            Action<string>? onOutputData = null,
            Action? onStart = null,
            Action? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {

            using var downloadProcess = new Process { StartInfo = psi };

            bool isCancelled = false;
            bool alreadyDownloaded = false;

            downloadProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    SetTitle(args.Data);
                    if (args.Data.Contains("has already been downloaded"))
                    {
                        alreadyDownloaded = true;
                        var fullLogMessage = args.Data.Split("has")[0];
                        var videoTitle = fullLogMessage.Split("[download]")[1].Trim();
                        SetTitle("⚠️ The video has already been downloaded");
                        downloadBanner.UpdateState(DownloadState.AlreadyDownloaded, videoTitle);
                        onAlreadyDownloaded?.Invoke();
                    }

                    onOutputData?.Invoke(args.Data);
                }
            };



            try
            {
                onStart?.Invoke();
                downloadBanner.UpdateState(DownloadState.Extracting);

                downloadProcess.Start();
                SetLoading(true);
                downloadBanner.ShowStatus();
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

                if (!isCancelled && downloadProcess.ExitCode == 0 && !alreadyDownloaded)
                {
                    SetTitle("✅ Download finished");
                    downloadBanner.UpdateState(DownloadState.Finished);
                    SetLoading(false);
                    onFinish?.Invoke();
                }
                else if (isCancelled)
                {
                    SetTitle("⛔ Download cancelled");
                    downloadBanner.UpdateState(DownloadState.Cancelled);
                    SetLoading(false);
                }
            }
            catch (OperationCanceledException)
            {
                SetTitle("⛔ Download cancelled");
                downloadBanner.UpdateState(DownloadState.Cancelled);
                SetLoading(false);
            }
            SetLoading(false);
            return downloadProcess.HasExited ? downloadProcess.ExitCode.ToString(CultureInfo.InvariantCulture) : "-1";
        }

        public async Task<string> TryExecuteQueryAsync(string url)
        {
            SetLoading(true);
            var arguments = "--dump-json  --no-playlist --no-check-formats --ignore-no-formats-error --verbose";

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{arguments} {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = psi };

            var outputBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data))
                    return;

                var line = args.Data.Trim();

                if (line.StartsWith('{') || line.StartsWith('['))
                {
                    outputBuilder.AppendLine(line);
                }
            };

            process.Start();
            process.BeginOutputReadLine();


            await process.WaitForExitAsync();

            var output = outputBuilder.ToString();

            SetTitle(!string.IsNullOrEmpty(output)
                ? $"✅ Available Formats for: {url}"
                : "yt-dlp-extension");

            NotifyItemsChanged();
            SetLoading(false);

            return output;
        }

        public async Task<string> TryExecutePlaylistDownloadAsync(
            string url,
            StatusMessage downloadBanner,
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
            downloadBanner.UpdateState(DownloadState.Extracting);
            Directory.CreateDirectory(downloadPath);
            var arguments = $"-P \"{downloadPath}\" --yes-playlist -f \"{videoFormat}\" --merge-output-format {GetSettingsVideoOutputFormat()}";
            if (audioOnly) arguments = $"-P \"{downloadPath}\" --yes-playlist -f \"{audioFormatId}\" --extract-audio --audio-format {GetSettingsAudioOutputFormat()}";

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{arguments} {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var downloadProcess = new Process { StartInfo = psi };
            var currentDownload = string.Empty;
            var totalDownloads = string.Empty;
            var progress = 0d;
            return await ExecuteDownloadProcessAsync(
                psi,
                downloadBanner,
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
                        var downloadProgressParse = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        progress = downloadProgressParse > 0 ? downloadProgressParse : progress;
                        downloadBanner.UpdateState(DownloadState.Downloading, "DownloadingPlaylist".ToLocalized(currentDownload, totalDownloads), progressPercent: (uint)Math.Floor(progress));
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
            StatusMessage downloadBanner,
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
            downloadBanner.UpdateState(DownloadState.Extracting);
            var downloadPath = GetSettingsDownloadPath();

            var arguments = audioOnly switch
            {
                false => $"--verbose --no-mtime --no-playlist  -P \"{downloadPath}\" -f \"{videoFormatId}+{audioFormatId}/{videoFormatId}+ba/{videoFormatId}/best\" --merge-output-format {GetSettingsVideoOutputFormat()} \"{url}\"",
                true => $"--verbose --no-mtime --no-playlist -P \"{downloadPath}\" -f \"{audioFormatId}/bestaudio/ba/best\" --extract-audio --audio-format {GetSettingsAudioOutputFormat()} \"{url}\""
            };

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var downloadProcess = new Process { StartInfo = psi };
            var progress = 0d;
            return await ExecuteDownloadProcessAsync(
                psi,
                downloadBanner,
                onOutputData: (data) =>
                {
                    if (Regex.Match(data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%") is var match && match.Success)
                    {
                        var downloadProgressParse = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                        progress = downloadProgressParse > 0 ? downloadProgressParse : progress;
                        downloadBanner.UpdateState(DownloadState.Downloading, "Downloading".ToLocalized(videoTitle), progressPercent: (uint)Math.Floor(progress));
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
