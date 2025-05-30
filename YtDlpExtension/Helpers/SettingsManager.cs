using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace YtDlpExtension.Helpers
{
    public class SettingsManager : JsonSettingsManager
    {
        private readonly TextSetting _downloadLocation = new("downloadLocation", DownloadHelper.GetDefaultDownloadPath())
        {
            Label = "DownloadDirectory".ToLocalized(),
            Description = "DownloadDirectory".ToLocalized()
        };
        private readonly ToggleSetting _recodeVideo = new("recodeVideo", false)
        {
            Description = "Always recode video (Increases time but ensures compatibility)"
        };

        private readonly ChoiceSetSetting _videoOutputFormats = new("videoOutputFormats", [
            new ChoiceSetSetting.Choice("mp4", "mp4"),
            new ChoiceSetSetting.Choice("mkv", "mkv"),
            new ChoiceSetSetting.Choice("webm", "webm"),
            new ChoiceSetSetting.Choice("flv", "flv"),
            new ChoiceSetSetting.Choice("ogg", "ogg"),
            new ChoiceSetSetting.Choice("avi", "avi"),
            new ChoiceSetSetting.Choice("mov", "mov"),
            new ChoiceSetSetting.Choice("ts", "ts"),
        ])
        {
            Label = "VideoOutputFormat".ToLocalized(),
            Description = "VideoOutputFormat".ToLocalized(),
            Value = "mp4"
        };

        private readonly ChoiceSetSetting _audioOutputFormats = new("audioOutputFormats", [
            new ChoiceSetSetting.Choice("aac", "aac"),
            new ChoiceSetSetting.Choice("flac", "flac"),
            new ChoiceSetSetting.Choice("webm", "webm"),
            new ChoiceSetSetting.Choice("mp3", "mp3"),
            new ChoiceSetSetting.Choice("m4a", "m4a"),
            new ChoiceSetSetting.Choice("opus", "opus"),
            new ChoiceSetSetting.Choice("vorbis", "vorbis"),
            new ChoiceSetSetting.Choice("wav", "wav"),
        ])
        {
            Label = "AudioOutputFormat".ToLocalized(),
            Description = "AudioOutputFormat".ToLocalized(),
            Value = "mp3"
        };

        private readonly TextSetting _customFormatSelector = new("customFormatSelector", string.Empty)
        {
            Label = "CustomFormatSelector".ToLocalized(),
            Description = "CustomFormatSelector".ToLocalized()
        };

        public string DownloadLocation => _downloadLocation.Value ?? DownloadHelper.GetDefaultDownloadPath();
        public string GetSelectedVideoOutputFormat => _videoOutputFormats.Value ?? "mp4";
        public string GetSelectedAudioOutputFormat => _audioOutputFormats.Value ?? "mp3";
        public string GetCustomFormatSelector => _customFormatSelector.Value ?? string.Empty;
        internal static string SettingsJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("YtDlp");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "settings.json");
        }

        public SettingsManager()
        {
            FilePath = SettingsJsonPath();
            Settings.Add(_downloadLocation);
            Settings.Add(_videoOutputFormats);
            Settings.Add(_audioOutputFormats);
            Settings.Add(_customFormatSelector);
            try
            {
                LoadSettings();
            }
            catch (FileNotFoundException)
            {
                SaveSettings();
            }

            Settings.SettingsChanged += (s, a) => this.SaveSettings();
        }
    }
}
