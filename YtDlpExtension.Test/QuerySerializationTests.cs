using Newtonsoft.Json.Linq;
using Xunit;
using YtDlpExtension.Helpers;
using YtDlpExtension.Metada;

namespace YtDlpExtension.Test
{

    public class QuerySerializationTests
    {
        private static SettingsManager _settings = new();
        private DownloadHelper _ytDlp = new(_settings);

        [Fact]
        public async Task AbcNewsQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("http://abcnews.go.com/ThisWeek/video/week-exclusive-irans-foreign-minister-zarif-20411932");
            JObject videoInfo = JObject.Parse(jsonResult);
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());

                Console.WriteLine("\n\nABC NEWS Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }

        [Fact]
        public async Task PlaylistQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("https://www.youtube.com/watch?v=D-h6MoF7HLA&list=PLXqeB_d1wZEAP6QXjEeHaPbUWvp0EpdqT");
            JObject videoInfo = JObject.Parse(jsonResult);
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());
                Console.WriteLine("\n\nPlaylist Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }
        [Fact]
        public async Task YoutubeLiveQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("https://www.youtube.com/watch?v=WsDyRAPFBC8");
            JObject videoInfo = JObject.Parse(jsonResult);
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());
                Console.WriteLine("\nYoutube Live Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }

        [Fact]
        public async Task XQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("https://x.com/TumultoBRacervo/status/1923115324771041604");
            JObject videoInfo = JObject.Parse(jsonResult);
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());
                Console.WriteLine("\n\nX Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }

        [Fact]
        public async Task OkRuQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("https://ok.ru/video/230863342321");
            JObject videoInfo = JObject.Parse(jsonResult);
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());
                Console.WriteLine("\n\nOk.RU Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }

        [Fact]
        public async Task InstagramReelsQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("https://about.instagram.com/pt-br/features/reels");
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());
                Console.WriteLine("\n\nInstagram Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }
        [Fact]
        public async Task TwitchQuery()
        {
            var (jsonResult, bestformat) = await _ytDlp.TryExecuteQueryAsync("https://www.twitch.tv/gaules");
            VideoData? videoData;
            try
            {
                videoData = System.Text.Json.JsonSerializer.Deserialize(jsonResult, VideoDataContext.Default.VideoData);
                if (videoData == null)
                {
                    Assert.Fail("Video data is null.");
                    return;
                }
                Assert.False(string.IsNullOrEmpty(videoData?.Title));
                Assert.False(string.IsNullOrEmpty(videoData?.Thumbnail));
                Assert.True(videoData.Formats?.Length > 0, videoData.Formats.ToString());

                Console.WriteLine("\n\nTwitch Formats: " + string.Join(", ", videoData.Formats?.Select(f => f?.format_id)));
            }
            catch { }
        }
    }
}
