using Microsoft.CommandPalette.Extensions.Toolkit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YtDlpExtension.Pages;

namespace YtDlpExtension.Helpers
{
    public sealed partial class DownloadHelper
    {
        public event Action<string>? TitleUpdated;
        public event Action<bool>? LoadingChanged;
        public event Action<int>? ItemsChanged;
        public event Func<List<VideoFormatListItem>>? RequestActiveDownloads;

        private readonly SettingsManager _settings;

        private DateTime _lastThrottle = DateTime.MinValue;
        private readonly object _throttleLock = new();
        private void Throttle(Action<string> action, string data, int milliseconds = 500)
        {
            lock (_throttleLock)
            {
                var now = DateTime.UtcNow;
                if ((now - _lastThrottle).TotalMilliseconds >= milliseconds)
                {
                    _lastThrottle = now;
                    action(data);
                }
            }
        }

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
            var regexPattern = @"^(https?|ftp):\/\/([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}(\/[^\s]*)?$";
            return Regex.IsMatch(url.Trim(), regexPattern, RegexOptions.IgnoreCase);
        }

        private async Task<Command> ExecuteDownloadProcessAsync(
            ProcessStartInfo psi,
            StatusMessage downloadBanner,
            bool isLive,
            Action<string>? onOutputData = null,
            Action<string>? onErrorData = null,
            Action? onStart = null,
            Action<Command>? onFinish = null,
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
                    onOutputData?.Invoke(args.Data);

                    Throttle((data) =>
                    {
                        var ativos = RequestActiveDownloads?.Invoke() ?? new List<VideoFormatListItem>();


                        if (ativos.Count > 1)
                        {
                            SetTitle($"InQueue".ToLocalized());
                        }
                        else
                        {

                            SetTitle(data);
                        }
                        if (data.Contains("has already been downloaded"))
                        {
                            alreadyDownloaded = true;
                            var fullLogMessage = data.Split("has")[0];
                            var videoTitle = fullLogMessage.Split("[download]")[1].Trim();
                            SetTitle($"⚠️ {"AlreadyDownloaded".ToLocalized()}");
                            downloadBanner.UpdateState(DownloadState.AlreadyDownloaded, videoTitle);
                            onAlreadyDownloaded?.Invoke();
                        }



                    }, args.Data, 200);
                }
            };

            downloadProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data)) onErrorData?.Invoke(args.Data);

            };

            try
            {
                onStart?.Invoke();
                downloadProcess.Start();

                if (!isLive)
                {
                    downloadProcess.BeginOutputReadLine();
                    downloadProcess.BeginErrorReadLine();
                    downloadBanner.UpdateState(DownloadState.Extracting);
                }
                downloadBanner.ShowStatus();
                using (cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!downloadProcess.HasExited)
                        {
                            isCancelled = true;
                            downloadProcess.Kill(true);
                        }
                    }
                    catch { /* Ignored */ }
                }))
                {
                    await downloadProcess.WaitForExitAsync(cancellationToken);
                }

                if (!isCancelled && downloadProcess.ExitCode == 0 && !alreadyDownloaded)
                {
                    SetTitle($"✅ {"Downloaded".ToLocalized()}");
                    downloadBanner.UpdateState(DownloadState.Finished);

                    var showInExplorerCommand = new AnonymousCommand(() =>
                    {
                        var filepath = ExtractDownloadPath(psi);
                        try
                        {
                            Process.Start("explorer.exe", $"\"{filepath}\"");
                        }
                        catch
                        {
                            downloadBanner.UpdateState(DownloadState.Error, "Error creating open file command");
                        }
                    })
                    {
                        Name = "ShowOutputDir".ToLocalized(),
                        Result = CommandResult.KeepOpen()
                    };

                    onFinish?.Invoke(showInExplorerCommand);
                }
                else if (isCancelled)
                {
                    SetTitle($"⛔ {"Cancelled".ToLocalized()}");
                    downloadBanner.UpdateState(DownloadState.Cancelled);
                }

                if (downloadProcess.ExitCode > 0)
                {
                    SetTitle("Error".ToLocalized());
                    downloadBanner.UpdateState(DownloadState.Error, "EmptyDataYtDlp".ToLocalized());
                }
            }
            catch (OperationCanceledException)
            {
                SetTitle($"⛔ {"Cancelled".ToLocalized()}");
                downloadBanner.UpdateState(DownloadState.Cancelled);
                return null;
            }

            if (ExtractDownloadPath(psi) is var filePath)
            {
                return new AnonymousCommand(() =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening file: {ex.Message}");
                    }
                })
                {
                    Name = $"Open in folder",
                    Icon = new IconInfo("\uE838")
                };
            }

            return null;

        }

        public async Task<(string, string, int)> TryExecuteQueryAsync(string url)
        {
            var arguments = "--dump-single-json  --no-playlist --no-check-formats --ignore-no-formats-error --verbose --flat-playlist";

            if (_settings.GetCookiesFile is var cookies && !string.IsNullOrEmpty(cookies))
            {
                arguments = $"{arguments} --cookies \"{cookies}\"";
            }
            var errorCode = 0;
            //var debugBanner = new StatusMessage();

            //debugBanner.UpdateState(DownloadState.CustomMessage, arguments);
            //debugBanner.ShowStatus();

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
            var bestformat = string.Empty;

            process.ErrorDataReceived += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data))
                    return;

                var line = args.Data.Trim();
                if (line.Contains("[info]"))
                {
                    bestformat = line;
                }
                if (line.Contains("This video is age-restricted"))
                {
                    errorCode = 403;
                }
                if (line.Contains("This video is only available for registered users"))
                {
                    errorCode = 401;
                }
            };

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
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();



            await process.WaitForExitAsync();

            var output = outputBuilder.ToString();

            SetTitle(!string.IsNullOrEmpty(output)
                ? $"Best format {bestformat}"
                : "yt-dlp-extension");

            NotifyItemsChanged();
            SetLoading(false);

            return (output, bestformat, errorCode);
        }

        public async Task<Command?> TryExecutePlaylistDownloadAsync(
            string url,
            StatusMessage downloadBanner,
            string downloadPath,
            string videoFormat,
            string playlistStart,
            string playlistEnd,
            string customFormatSelector = "",
            string audioFormatId = "bestaudio",
            bool audioOnly = false,
            Action? onStart = null,
            Action<Command>? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {
            onStart?.Invoke();
            SetTitle($"Extracting URL: {url}");
            downloadBanner.UpdateState(DownloadState.Extracting);
            var arguments = new List<string>
            {
                "--verbose",
                "--no-mtime",
                "-P", $"\"{downloadPath}\"",
                "--yes-playlist"
            };

            if (!string.IsNullOrEmpty(playlistStart))
                arguments.Add($"--playlist-start {playlistStart}");
            if (!string.IsNullOrEmpty(playlistEnd))
                arguments.Add($"--playlist-end {playlistEnd}");

            if (audioOnly)
            {
                arguments.Add($"-f \"{audioFormatId}\"");
                arguments.Add("--extract-audio");
                arguments.Add($"--audio-format {GetSettingsAudioOutputFormat()}");
            }
            else
            {
                arguments.Add($"-f \"{(!string.IsNullOrEmpty(customFormatSelector) ? customFormatSelector : videoFormat)}\"");
                arguments.Add($"--merge-output-format {GetSettingsVideoOutputFormat()}");
            }

            arguments.Add($"\"{url}\"");

            var argumentsFinal = string.Join(" ", arguments);
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = argumentsFinal,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var downloadProcess = new Process { StartInfo = psi };
            var currentDownload = string.Empty;
            var totalDownloads = string.Empty;
            return await ExecuteDownloadProcessAsync(
                psi,
                downloadBanner,
                false,
                onOutputData: (data) =>
                {
                    SetTitle(data);
                    var itemMatch = Regex.Match(data, @"\[download\]\s+Downloading item\s+(\d+)\s+of\s+(\d+)", RegexOptions.Compiled);
                    if (itemMatch.Success)
                    {
                        currentDownload = itemMatch.Groups[1].Value;
                        totalDownloads = itemMatch.Groups[2].Value;
                    }

                    var progressMatch = Regex.Match(data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);
                    if (progressMatch.Success)
                    {
                        var progress = double.Parse(progressMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        Throttle((dataThrottled) =>
                        {
                            downloadBanner.UpdateState(
                                    DownloadState.Downloading,
                                    $"{"DownloadingPlaylist".ToLocalized(currentDownload, totalDownloads)}\n{dataThrottled}",
                                    progressPercent: (uint)Math.Floor(progress));
                        }, data, 200);
                    }

                },
                null,
                onStart,
                onFinish,
                onAlreadyDownloaded,
                cancellationToken
            );
        }

        public async Task<Command?> TryExecuteDownloadAsync(
            string url,
            StatusMessage downloadBanner,
            string videoTitle = "",
            string videoFormatId = "",
            string startTime = "",
            string endTime = "",
            string audioFormatId = "bestaudio",
            bool audioOnly = false,
            bool isLive = false,
            Action? onStart = null,
            Action<Command>? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {
            var customFormatSelector = _settings.GetCustomFormatSelector;
            onStart?.Invoke();
            if (isLive)
            {
                downloadBanner.UpdateState(DownloadState.CustomMessage, "NewWindowOpen".ToLocalized(), true);
            }

            else
            {
                downloadBanner.UpdateState(DownloadState.Extracting);
            }
            var downloadPath = GetSettingsDownloadPath();

            var arguments = new List<string>
            {
                "--verbose",
                "--no-mtime",
                "--no-playlist",
                "-o", "\"%(title)s - %(format_id)s - %(resolution)s.%(ext)s\"",
                "-P", $"\"{downloadPath}\""
            };

            if (_settings.GetCookiesFile is var cookies && !string.IsNullOrEmpty(cookies))
            {
                arguments.Add($"--cookies \"{cookies}\"");
            }

            if (audioOnly)
            {
                arguments.Add("-f");
                arguments.Add($"\"{audioFormatId}/bestaudio/ba/best\"");
                arguments.Add("--extract-audio");
                arguments.Add("--audio-format");
                arguments.Add(GetSettingsAudioOutputFormat());
            }
            else
            {
                arguments.Add("-f");
                if (!string.IsNullOrEmpty(customFormatSelector))
                {
                    arguments.Add($"\"{customFormatSelector}\"");
                }
                else if (_settings.GetSelectedMode == ExtensionMode.SIMPLE)
                {
                    arguments.Add($"\"{videoFormatId}+bestaudio[acodec!=opus]/{videoFormatId}+ba/{videoFormatId}/best\"");
                }
                else
                {
                    arguments.Add($"\"{videoFormatId}+{audioFormatId}/best\"");
                }
                arguments.Add("--merge-output-format");
                arguments.Add(GetSettingsVideoOutputFormat());
            }

            if (!string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
            {
                arguments.Add($"--download-sections \"*{startTime}-{endTime}\"");
            }

            arguments.Add($"\"{url}\"");

            var argumentsFinal = string.Join(" ", arguments);

            var shouldRedirect = isLive ? false : true;
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = argumentsFinal,
                RedirectStandardOutput = shouldRedirect,
                RedirectStandardError = shouldRedirect,
                UseShellExecute = false,
                CreateNoWindow = shouldRedirect,
            };

            using var downloadProcess = new Process { StartInfo = psi };

            var timeRegex = new Regex(@"time=(\d{2}:\d{2}:\d{2}(?:\.\d{1,3})?)", RegexOptions.Compiled);
            return await ExecuteDownloadProcessAsync(
                psi,
                downloadBanner,
                isLive,
                onOutputData: (data) =>
                {
                    //SetTitle(data);
                    var progressMatch = Regex.Match(data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%", RegexOptions.IgnoreCase);
                    if (progressMatch.Success)
                    {
                        var progress = double.Parse(progressMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        Throttle(dataThrottled =>
                        {

                            downloadBanner.UpdateState(
                                    DownloadState.Downloading,
                                    $"{"Downloading".ToLocalized(videoTitle)}\n{dataThrottled}",
                                    progressPercent: (uint)Math.Floor(progress));

                        }, data, 200);

                    }
                },
                onErrorData: (data) =>
                {
                    // This handles the ffmpeg output for trimming and live streams
                    if (!string.IsNullOrEmpty(data))
                    {
                        var match = timeRegex.Match(data);
                        if (match.Success)
                        {
                            var timeStr = match.Groups[1].Value;
                            if (TimeSpan.TryParseExact(timeStr, @"hh\:mm\:ss\.ff", CultureInfo.InvariantCulture, out var currentTime) ||
                                TimeSpan.TryParseExact(timeStr, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out currentTime) ||
                                TimeSpan.TryParseExact(timeStr, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out currentTime))
                            {
                                if (TimeSpan.TryParse(endTime, out var endTs) && TimeSpan.TryParse(startTime, out var startTs))
                                {
                                    var totalDuration = endTs - startTs;
                                    if (totalDuration.TotalSeconds > 0)
                                    {
                                        var percent = Math.Min(100, (currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100);

                                        Throttle((d) =>
                                        {
                                            downloadBanner.UpdateState(
                                                DownloadState.Downloading,
                                                $"{data.Trim()}\n({currentTime:hh\\:mm\\:ss} / {totalDuration:hh\\:mm\\:ss})",
                                                progressPercent: (uint)Math.Floor(percent)
                                            );
                                        }, data, 200);
                                    }
                                }
                            }
                        }
                    }
                },
                onStart,
                onFinish,
                onAlreadyDownloaded,
                cancellationToken
            );
        }

        public async Task<Command> TryExecuteSubtitleDownloadAsync(
            string url,
            string subtitleKey,
            StatusMessage downloadBanner,
            string downloadPath,
            bool autoSubtitle = false,
            Action? onStart = null,
            Action<Command>? onFinish = null,
            Action? onAlreadyDownloaded = null,
            CancellationToken cancellationToken = default
        )
        {
            onStart?.Invoke();
            SetTitle($"DownloadingSubtitle".ToLocalized(subtitleKey));


            var arguments = new List<string>
            {
                "-P", $"\"{downloadPath}\"",
                "--no-playlist",
                "--sub-langs", subtitleKey,
                "--skip-download",
                "--no-mtime"
            };

            if (autoSubtitle)
                arguments.Add("--write-auto-subs");
            else
                arguments.Add("--write-subs");

            arguments.Add($"\"{url}\"");
            var argumentsFinal = string.Join(" ", arguments);
            downloadBanner.UpdateState(DownloadState.Downloading, "DownloadingSubtitle".ToLocalized(subtitleKey), true);
            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = argumentsFinal,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            return await ExecuteDownloadProcessAsync(
                psi,
                downloadBanner,
                false,
                onOutputData: data =>
                {
                    var progressMatch = Regex.Match(data, @"\[download\]\s+(\d{1,3}(?:\.\d+)?)%", RegexOptions.IgnoreCase);
                    if (progressMatch.Success)
                    {
                        var progress = double.Parse(progressMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        downloadBanner.UpdateState(
                                    DownloadState.Downloading,
                                    $"{"DownloadingSubtitle".ToLocalized(subtitleKey)}\n{data}",
                                    progressPercent: (uint)Math.Floor(progress));

                    }
                },
                null,
                onStart,
                onFinish,
                onAlreadyDownloaded,
                cancellationToken
            );
        }

        private static string ExtractDownloadPath(ProcessStartInfo psi)
        {
            var match = Regex.Match(psi.Arguments, @"-P\s+(?:""([^""]+)""|([^\s]+))");
            if (!match.Success) return string.Empty;
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
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
