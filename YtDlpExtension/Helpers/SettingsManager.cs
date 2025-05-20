using Microsoft.CommandPalette.Extensions.Toolkit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YtDlpExtension.Helpers
{
    internal class SettingsManager : JsonSettingsManager
    {
        private readonly TextSetting _downloadLocation = new("downloadLocation", DownloadHelper.GetDefaultDownloadPath())
        {
            Label = "Download Directory",
            Description = "Download Directory"
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
            Label = "Video Output Format",
            Description = "Video Output Format",
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
            Label = "Audio Only Output Format",
            Description = "Audio Only Output Format",
            Value = "mp3"
        };

        public string DownloadLocation => _downloadLocation.Value ?? DownloadHelper.GetDefaultDownloadPath();
        public string GetSelectedVideoOutputFormat => _videoOutputFormats.Value ?? "mp4";
        public string GetSelectedAudioOutputFormat => _audioOutputFormats.Value ?? "mp3";
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
            try
            {
                LoadSettings();
            }
            catch (FileNotFoundException)
            {
                SaveSettings(); // cria o arquivo com os valores padrões
            }

            Settings.SettingsChanged += (s, a) => this.SaveSettings();
        }
    }
}
