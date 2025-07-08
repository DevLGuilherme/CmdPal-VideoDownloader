using Microsoft.CommandPalette.Extensions.Toolkit;
using System.IO;

namespace YtDlpExtension.Helpers
{

    static class ExtensionMode
    {
        public const string SIMPLE = "simple";
        public const string ADVANCED = "advanced";
    }


    public class SettingsManager : JsonSettingsManager
    {
        private readonly TextSetting _downloadLocation = new("downloadLocation", DownloadHelper.GetDefaultDownloadPath())
        {
            Label = "DownloadDirectory".ToLocalized(),
            Description = "DownloadDirectory".ToLocalized(),
            IsRequired = true,
        };
        private readonly ToggleSetting _recodeVideo = new("recodeVideo", false)
        {
            Description = "Always recode video (Increases time but ensures compatibility)"
        };


        private readonly ChoiceSetSetting _mode = new("mode", [
                new ChoiceSetSetting.Choice("Simple", ExtensionMode.SIMPLE),
                new ChoiceSetSetting.Choice("Advanced", ExtensionMode.ADVANCED),
            ])
        {
            Value = ExtensionMode.SIMPLE,
            Description = "Modes".ToLocalized(),
            IsRequired = true,
            Label = "AppMode".ToLocalized()
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
            IsRequired = true,
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
            IsRequired = true,
            Value = "mp3"
        };

        private readonly TextSetting _customFormatSelector = new("customFormatSelector", string.Empty)
        {
            Label = "CustomFormatSelector".ToLocalized(),
            Description = "CustomFormatSelector".ToLocalized(),

        };

        private readonly ToggleSetting _downloadOnPaste = new("downloadOnPaste", false)
        {
            Label = "AutoDownload".ToLocalized(),
            Description = "AutoDownloadDescription".ToLocalized(),
            ErrorMessage = "Custom Format Selector can't be empty",
        };

        private readonly TextSetting _cookiesFileLocation = new("cookiesFileLocation", string.Empty)
        {
            Label = "CookiesFile".ToLocalized(),
            Description = "cookies.txt file location"
        };


        public string DownloadLocation => _downloadLocation.Value ?? DownloadHelper.GetDefaultDownloadPath();
        public string GetSelectedVideoOutputFormat => _videoOutputFormats.Value ?? "mp4";
        public string GetSelectedAudioOutputFormat => _audioOutputFormats.Value ?? "mp3";
        public string GetCookiesFile => _cookiesFileLocation.Value ?? string.Empty;
        public string GetSelectedMode => _mode.Value ?? ExtensionMode.SIMPLE;
        public bool GetDownloadOnPaste => _downloadOnPaste.Value;
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
            Settings.Add(_mode);
            Settings.Add(_downloadLocation);
            Settings.Add(_videoOutputFormats);
            Settings.Add(_audioOutputFormats);
            Settings.Add(_cookiesFileLocation);
            Settings.Add(_customFormatSelector);
            Settings.Add(_downloadOnPaste);
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
