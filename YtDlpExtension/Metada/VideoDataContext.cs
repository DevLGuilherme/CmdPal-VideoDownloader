using System.Text.Json.Serialization;
using YtDlpExtension.Metada;

namespace YtDlpExtension.Helpers
{
    [JsonSerializable(typeof(VideoData))]
    [JsonSerializable(typeof(string))]
    public partial class VideoDataContext : JsonSerializerContext
    {
    }
}
