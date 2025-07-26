using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using YtDlpExtension.Converters;

namespace YtDlpExtension.Metada
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

    public record VideoData
    {
        [JsonPropertyName("_type")]
        public string? ResultType { get; set; }
        [JsonPropertyName("extractor")]
        public string? Extractor { get; set; }
        [JsonPropertyName("extractor_key")]
        public string? ExtractorKey { get; set; }
        [JsonPropertyName("entries")]
        public VideoData[]? Entries { get; set; }
        [JsonPropertyName("id")]
        public string? ID { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("formats")]
        public Format[]? Formats { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("original_url")]
        public string? OriginalUrl { get; set; }
        [JsonPropertyName("ext")]
        public string? Extension { get; set; }
        [JsonPropertyName("format")]
        public string? Format { get; set; }
        [JsonPropertyName("format_id")]
        public string? FormatID { get; set; }
        [JsonPropertyName("player_url")]
        public string? PlayerUrl { get; set; }
        [JsonPropertyName("direct")]
        public bool? Direct { get; set; }
        [JsonPropertyName("alt_title")]
        public string? AltTitle { get; set; }
        [JsonPropertyName("display_id")]
        public string? DisplayID { get; set; }
        [JsonPropertyName("thumbnails")]
        public Thumbnail[]? Thumbnails { get; set; }
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("uploader")]
        public string? Uploader { get; set; }
        [JsonPropertyName("license")]
        public string? License { get; set; }
        [JsonPropertyName("creator")]
        public string? Creator { get; set; }
        [JsonPropertyName("release_timestamp")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? ReleaseTimestamp { get; set; }
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }
        [JsonPropertyName("timestamp")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? Timestamp { get; set; }
        [JsonPropertyName("upload_date")]
        public string? UploadDate { get; set; }
        [JsonPropertyName("modified_timestemp")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? ModifiedTimestamp { get; set; }
        [JsonPropertyName("modified_date")]
        public string? ModifiedDate { get; set; }
        [JsonPropertyName("uploader_id")]
        public string? UploaderID { get; set; }
        [JsonPropertyName("uploader_url")]
        public string? UploaderUrl { get; set; }
        [JsonPropertyName("channel")]
        public string? Channel { get; set; }
        [JsonPropertyName("channel_id")]
        public string? ChannelID { get; set; }
        [JsonPropertyName("channel_url")]
        public string? ChannelUrl { get; set; }
        [JsonPropertyName("channel_follower_count")]
        public long? ChannelFollowerCount { get; set; }
        [JsonPropertyName("location")]
        public string? Location { get; set; }
        [JsonPropertyName("subtitles")]
        public Subtitle? Subtitles { get; set; }
        [JsonPropertyName("automatic_captions")]
        public Subtitle? AutomaticCaptions { get; set; }
        [JsonPropertyName("duration")]
        public float? Duration { get; set; }
        [JsonPropertyName("view_count")]
        public long? ViewCount { get; set; }
        [JsonPropertyName("playlist_count")]
        public int? PlaylistCount { get; set; }

        [JsonPropertyName("concurrent_view_count")]
        public long? ConcurrentViewCount { get; set; }
        [JsonPropertyName("like_count")]
        public long? LikeCount { get; set; }
        [JsonPropertyName("dislike_count")]
        public long? DislikeCount { get; set; }
        [JsonPropertyName("repost_count")]
        public long? RepostCount { get; set; }
        [JsonPropertyName("average_rating")]
        public double? AverageRating { get; set; }
        [JsonPropertyName("comment_count")]
        public long? CommentCount { get; set; }
        [JsonPropertyName("comments")]
        public CommentData[]? Comments { get; set; }
        [JsonPropertyName("age_limit")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? AgeLimit { get; set; }
        [JsonPropertyName("webpage_url")]
        public string? WebpageUrl { get; set; }
        [JsonPropertyName("categories")]
        public string[]? Categories { get; set; }
        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }
        [JsonPropertyName("cast")]
        public string[]? Cast { get; set; }
        [JsonPropertyName("is_live")]
        public bool? IsLive { get; set; }
        [JsonPropertyName("was_live")]
        public bool? WasLive { get; set; }
        [JsonPropertyName("live_status")]
        public string? LiveStatus { get; set; }
        [JsonPropertyName("start_time")]
        public float? StartTime { get; set; }
        [JsonPropertyName("end_time")]
        public float? EndTime { get; set; }
        [JsonPropertyName("playable_in_embed")]
        public bool? PlayableInEmbed { get; set; }
        [JsonPropertyName("availability")]
        public string? Availability { get; set; }
        [JsonPropertyName("chapters")]
        public ChapterData[]? Chapters { get; set; }
        [JsonPropertyName("chapter")]
        public string? Chapter { get; set; }
        [JsonPropertyName("chapter_number")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? ChapterNumber { get; set; }
        [JsonPropertyName("chapter_id")]
        public string? ChapterId { get; set; }
        [JsonPropertyName("series")]
        public string? Series { get; set; }
        [JsonPropertyName("series_id")]
        public string? SeriesId { get; set; }
        [JsonPropertyName("season")]
        public string? Season { get; set; }
        [JsonPropertyName("season_number")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? SeasonNumber { get; set; }
        [JsonPropertyName("season_id")]
        public string? SeasonId { get; set; }
        [JsonPropertyName("episode")]
        public string? Episode { get; set; }
        [JsonPropertyName("episode_number")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? EpisodeNumber { get; set; }
        [JsonPropertyName("episode_id")]
        public string? EpisodeId { get; set; }
        [JsonPropertyName("track")]
        public string? Track { get; set; }
        [JsonPropertyName("track_number")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? TrackNumber { get; set; }
        [JsonPropertyName("track_id")]
        public string? TrackId { get; set; }
        [JsonPropertyName("artist")]
        public string? Artist { get; set; }
        [JsonPropertyName("genre")]
        public string? Genre { get; set; }
        [JsonPropertyName("album")]
        public string? Album { get; set; }
        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; }
        [JsonPropertyName("album_artist")]
        public string? AlbumArtist { get; set; }
        [JsonPropertyName("disc_number")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? DiscNumber { get; set; }
        [JsonPropertyName("release_year")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? ReleaseYear { get; set; }
        [JsonPropertyName("composer")]
        public string? Composer { get; set; }
        [JsonPropertyName("section_start")]
        public long? SectionStart { get; set; }
        [JsonPropertyName("section_end")]
        public long? SectionEnd { get; set; }
        [JsonPropertyName("rows")]
        public long? StoryboardFragmentRows { get; set; }
        [JsonPropertyName("columns")]
        public long? StoryboardFragmentColumns { get; set; }



        // The same properties from the Format Class needs to be repeated here
        // YT-DLP has extractors that support only single format and it returns just a single object
        [JsonPropertyName("asr")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? asr { get; set; }
        [JsonPropertyName("filesize")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? Filesize { get; set; }
        [JsonPropertyName("format_note")]
        public string? FormatNote { get; set; }
        [JsonPropertyName("source_preference")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? SourcePreference { get; set; }
        [JsonPropertyName("fps")]
        public object? FPS { get; set; }
        [JsonPropertyName("audio_channels")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? AudioChannels { get; set; }
        [JsonPropertyName("height")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? Height { get; set; }
        [JsonPropertyName("quality")]
        public float? Quality { get; set; }
        [JsonPropertyName("has_drm")]
        public bool? HasDRM { get; set; }
        [JsonPropertyName("tbr")]
        public float? TBR { get; set; }
        [JsonPropertyName("filesize_approx")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? FilesizeApprox { get; set; }
        [JsonPropertyName("width")]
        [JsonConverter(typeof(FlexibleIntConverter))]

        public int? Width { get; set; }
        [JsonPropertyName("language")]
        public object? Language { get; set; }
        [JsonPropertyName("language_preference")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? LanguagePreference { get; set; }
        [JsonPropertyName("preference")]
        public object? Preference { get; set; }
        [JsonPropertyName("vcodec")]
        public string? VCodec { get; set; }
        [JsonPropertyName("acodec")]
        public string? ACodec { get; set; }
        [JsonPropertyName("dynamic_range")]
        public object? DynamicRange { get; set; }
        [JsonPropertyName("container")]
        public string? Container { get; set; }
        [JsonPropertyName("downloader_options")]
        public DownloaderOptions? DownloaderOptions { get; set; }
        [JsonPropertyName("protocol")]
        public string? Protocol { get; set; }
        [JsonPropertyName("audio_ext")]
        public string? AudioExt { get; set; }
        [JsonPropertyName("video_ext")]
        public string? VideoExt { get; set; }
        [JsonPropertyName("vbr")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? VBR { get; set; }
        [JsonPropertyName("abr")]
        public float? ABR { get; set; }
        [JsonPropertyName("resolution")]
        public string? Resolution { get; set; }
        [JsonPropertyName("aspect_ratio")]
        public object? AspectRatio { get; set; }
        [JsonPropertyName("http_headers")]
        public HttpHeaders? HttpHeaders { get; set; }
    }

    public class CommentData
    {
        [JsonPropertyName("id")]
        public string? ID { get; set; }
        [JsonPropertyName("author")]
        public string? Author { get; set; }
        [JsonPropertyName("author_id")]
        public string? AuthorID { get; set; }
        [JsonPropertyName("author_thumbnail")]
        public string? AuthorThumbnail { get; set; }
        [JsonPropertyName("html")]
        public string? Html { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("timestamp")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? Timestamp { get; set; }
        [JsonPropertyName("parent")]
        public string? Parent { get; set; }
        [JsonPropertyName("like_count")]
        public int? LikeCount { get; set; }
        [JsonPropertyName("dislike_count")]
        public int? DislikeCount { get; set; }
        [JsonPropertyName("is_favorited")]
        public bool? IsFavorited { get; set; }
        [JsonPropertyName("author_is_uploader")]
        public bool? AuthorIsUploader { get; set; }
    }

    public class ChapterData
    {
        [JsonPropertyName("start_time")]
        public float? StartTime { get; set; }
        [JsonPropertyName("end_time")]
        public float? EndTime { get; set; }
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class Thumbnail
    {
        public string? url { get; set; }
        public int? preference { get; set; }
        public string? id { get; set; }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]

    public class Format
    {
        [JsonPropertyName("asr")]
        public int? ASR { get; set; }
        [JsonPropertyName("filesize")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? Filesize { get; set; }
        [JsonPropertyName("format_id")]
        public string? FormatID { get; set; }
        [JsonPropertyName("format_note")]
        public string? FormatNote { get; set; }
        [JsonPropertyName("source_preference")]
        public int? SourcePreference { get; set; }
        [JsonPropertyName("fps")]
        public object? FPS { get; set; }
        [JsonPropertyName("audio_channels")]
        public int? AudioChannels { get; set; }
        [JsonPropertyName("height")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? Height { get; set; }
        [JsonPropertyName("quality")]
        public float? Quality { get; set; }
        [JsonPropertyName("has_drm")]
        public bool? HasDRM { get; set; }
        [JsonPropertyName("tbr")]
        public float? TBR { get; set; }
        [JsonPropertyName("filesize_approx")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? FilesizeApprox { get; set; }
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("width")]
        [JsonConverter(typeof(FlexibleIntConverter))]

        public int? Width { get; set; }
        [JsonPropertyName("language")]
        public string? Language { get; set; }
        [JsonPropertyName("language_preference")]
        public int? LanguagePreference { get; set; }
        [JsonPropertyName("preference")]
        public object? Preference { get; set; }
        [JsonPropertyName("ext")]
        public string? Ext { get; set; }
        [JsonPropertyName("vcodec")]
        public string? VCodec { get; set; }
        [JsonPropertyName("acodec")]
        public string? ACodec { get; set; }
        [JsonPropertyName("dynamic_range")]
        public object? DynamicRange { get; set; }
        [JsonPropertyName("container")]
        public string? Container { get; set; }
        [JsonPropertyName("downloader_options")]
        public DownloaderOptions? DownloaderOptions { get; set; }
        [JsonPropertyName("protocol")]
        public string? Protocol { get; set; }
        [JsonPropertyName("audio_ext")]
        public string? AudioExt { get; set; }
        [JsonPropertyName("video_ext")]
        public string? VideoExt { get; set; }
        [JsonPropertyName("vbr")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? VBR { get; set; }
        [JsonPropertyName("abr")]
        public float? ABR { get; set; }
        [JsonPropertyName("resolution")]
        public string? Resolution { get; set; }
        [JsonPropertyName("aspect_ratio")]
        public object? AspectRatio { get; set; }
        [JsonPropertyName("http_headers")]
        public HttpHeaders? HttpHeaders { get; set; }
        [JsonPropertyName("format")]
        public string? FormatInfo { get; set; }
    }

    public class DownloaderOptions
    {
        [JsonPropertyName("http_chunk_size")]
        public int? HttpChunkSize { get; set; }
    }

    public class HttpHeaders
    {
        public string? UserAgent { get; set; }
        public string? Accept { get; set; }
        public string? AcceptLanguage { get; set; }
        public string? SecFetchMode { get; set; }
    }

    public class SubtitleData
    {
        public string? Ext { get; set; }
        public string? Url { get; set; }
        public string? Format { get; set; }
        [JsonPropertyName("__yt_dlp_client")]
        public string? YtDlpClient { get; set; }
    }

    public partial class Subtitle : Dictionary<string, SubtitleData[]>
    {
    }

    public partial class AutomaticCaptions : Dictionary<string, SubtitleData[]>
    {
    }

    public class VersionInfo
    {
        public string? Version { get; set; }
        public object? CurrentGitHead { get; set; }
        public string? ReleaseGitHead { get; set; }
        public string? Repository { get; set; }
    }

    public enum MetadataType
    {
        [EnumMember(Value = "video")]
        Video,
        [EnumMember(Value = "playlist")]
        Playlist,
        [EnumMember(Value = "multi_video")]
        MultiVideo,
        [EnumMember(Value = "url")]
        Url,
        [EnumMember(Value = "url_transparent")]
        UrlTransparent
    }

}
