using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace YtDlpExtension.Helpers
{
    internal sealed partial class DownloadHelper
    {
        public event Action<string>? TitleUpdated;
        public event Action<bool>? LoadingChanged;
        public event Action<int>? ItemsChanged;
        public readonly DownloadStatusManager _downloadBanner = new();
        private readonly SettingsManager _settings;
        private Process? _downloadProcess;
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

        public async Task<string> TryExecuteQueryAsync(string url, bool isPlaylist = false)
        {
            SetLoading(true);
            SetTitle($"Extracting URL: {url}");
            _downloadBanner.UpdateState(DownloadState.Extracting);
            var arguments = "--dump-json";
            if(isPlaylist) arguments = $"--no-playlist --dump-json";
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
            Action? onFinish = null
        )
        {
            SetLoading(true);
            onStart?.Invoke();
            SetTitle($"Extracting URL: {url}");
            _downloadBanner.UpdateState(DownloadState.Extracting);
            var arguments = $"-P \"{downloadPath}\" --yes-playlist -f \"{videoFormat}\" --merge-output-format {GetSettingsVideoOutputFormat()}";
            if (audioOnly) arguments = $"-P \"{downloadPath}\" --yes-playlist -f \"{audioFormatId}\" --extract-audio --audio-format {GetSettingsAudioOutputFormat()}";

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"{arguments} {url}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _downloadProcess = new Process { StartInfo = psi };
            var currentDownload = string.Empty;
            var totalDownloads = string.Empty;
            _downloadProcess.OutputDataReceived += (sender, args) =>
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
                    }
                    if(args.Data.Contains("Downloading item"))
                    {
                        var stringParts = args.Data.Split("of");
                        totalDownloads = stringParts[1].Trim();
                        currentDownload = stringParts[0].Split("item")[1].Trim();
                    }
                    if (Regex.Match(args.Data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%") is var match && match.Success)
                    {
                        string percentStr = match.Groups[1].Value;
                        var downloadProgress = double.Parse(percentStr, CultureInfo.InvariantCulture);
                        _downloadBanner.UpdateState(DownloadState.Downloading, "DownloadingPlaylist".ToLocalized(currentDownload, totalDownloads)  ,progressPercent: (uint)Math.Floor(downloadProgress));
                    }

                    NotifyItemsChanged(args.Data.Length);
                }
            };

            _downloadProcess.Start();

            _downloadBanner.ShowStatus();

            _downloadProcess.BeginOutputReadLine();
            await _downloadProcess.WaitForExitAsync();


            if (_downloadProcess.ExitCode == 0 && _downloadBanner.CurrentState != DownloadState.AlreadyDownloaded)
            {
                SetTitle("✅ Download finished");
                _downloadBanner.UpdateState(DownloadState.Finished);
            }
            onFinish?.Invoke();
            SetLoading(false);
            return _downloadProcess.ExitCode.ToString();
        }

        public async Task<string> TryExecuteDownloadAsync(
            string url, 
            string videoFormatId, 
            string audioFormatId = "bestaudio",
            bool audioOnly = false,
            Action? onStart = null,
            Action? onFinish = null
        )
        {
            onStart?.Invoke();
            _downloadBanner.UpdateState(DownloadState.Extracting);
            var downloadPath = GetSettingsDownloadPath();

            var arguments = audioOnly switch
            {
                false => $"--abort-on-unavailable-fragment -P \"{downloadPath}\" -f \"{videoFormatId}+{audioFormatId}\" --merge-output-format {GetSettingsVideoOutputFormat()} {url}",
                true => $"--abort-on-unavailable-fragment -P \"{downloadPath}\" -f \"{audioFormatId}\" --extract-audio --audio-format {GetSettingsAudioOutputFormat()} \"{url}\""
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

            _downloadProcess = new Process { StartInfo = psi };

            _downloadProcess.OutputDataReceived += (sender, args) =>
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
                    }
                    if (Regex.Match(args.Data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%") is var match && match.Success)
                    {
                        string percentStr = match.Groups[1].Value;
                        var downloadProgress = double.Parse(percentStr, CultureInfo.InvariantCulture);
                        _downloadBanner.UpdateState(DownloadState.Downloading, "Downloading".ToLocalized(),progressPercent: (uint) Math.Floor(downloadProgress));
                    }

                    NotifyItemsChanged(args.Data.Length);
                }
            };

            _downloadProcess.Start();

            _downloadBanner.ShowStatus();

            _downloadProcess.BeginOutputReadLine();
            await _downloadProcess.WaitForExitAsync();


            if (_downloadProcess.ExitCode == 0 && _downloadBanner.CurrentState != DownloadState.AlreadyDownloaded)
            {
                SetTitle("✅ Download finished");
                _downloadBanner.UpdateState(DownloadState.Finished);
            }
            onFinish?.Invoke();
            return _downloadProcess.ExitCode.ToString();
        }

        public void CancelDownload()
        {
            if (_downloadProcess != null && !_downloadProcess.HasExited)
            {
                try {
                    _downloadProcess.Kill(true);
                    _downloadProcess.CancelOutputRead();
                } catch {
                    // Do nothing here
                }
                _downloadProcess.WaitForExit();

                SetTitle("⛔ Download cancelled");
                _downloadBanner.UpdateState(DownloadState.Cancelled);
                NotifyItemsChanged();
            }
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
