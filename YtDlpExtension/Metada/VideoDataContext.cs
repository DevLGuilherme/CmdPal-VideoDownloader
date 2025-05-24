using System.Text.Json.Serialization;
using YtDlpExtension.Metada;

namespace YtDlpExtension.Helpers
{
    [JsonSerializable(typeof(VideoData))]
    public partial class VideoDataContext : JsonSerializerContext
    {
    }
}
