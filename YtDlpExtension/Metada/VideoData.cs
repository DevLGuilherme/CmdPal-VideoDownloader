using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using YtDlpExtension.Converters;

namespace YtDlpExtension.Metada
{

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
        public long? filesize { get; set; }
        [JsonPropertyName("format_note")]
        public string? format_note { get; set; }
        [JsonPropertyName("source_preference")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? source_preference { get; set; }
        [JsonPropertyName("fps")]
        public object? fps { get; set; }
        [JsonPropertyName("audio_channels")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? audio_channels { get; set; }
        [JsonPropertyName("height")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? height { get; set; }
        [JsonPropertyName("quality")]
        public float? quality { get; set; }
        [JsonPropertyName("has_drm")]
        public bool? has_drm { get; set; }
        [JsonPropertyName("tbr")]
        public float? tbr { get; set; }
        [JsonPropertyName("filesize_approx")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? filesize_approx { get; set; }
        [JsonPropertyName("width")]
        [JsonConverter(typeof(FlexibleIntConverter))]

        public int? width { get; set; }
        [JsonPropertyName("language")]
        public object? language { get; set; }
        [JsonPropertyName("language_preference")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? language_preference { get; set; }
        [JsonPropertyName("preference")]
        public object? preference { get; set; }
        [JsonPropertyName("vcodec")]
        public string? vcodec { get; set; }
        [JsonPropertyName("acodec")]
        public string? acodec { get; set; }
        [JsonPropertyName("dynamic_range")]
        public object? dynamic_range { get; set; }
        [JsonPropertyName("container")]
        public string? container { get; set; }
        [JsonPropertyName("downloader_options")]
        public Downloader_Options? downloader_options { get; set; }
        [JsonPropertyName("protocol")]
        public string? protocol { get; set; }
        [JsonPropertyName("audio_ext")]
        public string? audio_ext { get; set; }
        [JsonPropertyName("video_ext")]
        public string? video_ext { get; set; }
        [JsonPropertyName("vbr")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? vbr { get; set; }
        [JsonPropertyName("abr")]
        public float? abr { get; set; }
        [JsonPropertyName("resolution")]
        public string? resolution { get; set; }
        [JsonPropertyName("aspect_ratio")]
        public object? aspect_ratio { get; set; }
        [JsonPropertyName("http_headers")]
        public Http_Headers? http_headers { get; set; }
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

    public class Format
    {
        [JsonPropertyName("asr")]
        public int? asr { get; set; }
        [JsonPropertyName("filesize")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? filesize { get; set; }
        [JsonPropertyName("format_id")]
        public string? format_id { get; set; }
        [JsonPropertyName("format_note")]
        public string? format_note { get; set; }
        [JsonPropertyName("source_preference")]
        public int? source_preference { get; set; }
        [JsonPropertyName("fps")]
        public object? fps { get; set; }
        [JsonPropertyName("audio_channels")]
        public int? audio_channels { get; set; }
        [JsonPropertyName("height")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? height { get; set; }
        [JsonPropertyName("quality")]
        public float? quality { get; set; }
        [JsonPropertyName("has_drm")]
        public bool? has_drm { get; set; }
        [JsonPropertyName("tbr")]
        public float? tbr { get; set; }
        [JsonPropertyName("filesize_approx")]
        [JsonConverter(typeof(FlexibleLongConverter))]
        public long? filesize_approx { get; set; }
        [JsonPropertyName("url")]
        public string? url { get; set; }
        [JsonPropertyName("width")]
        [JsonConverter(typeof(FlexibleIntConverter))]

        public int? width { get; set; }
        [JsonPropertyName("language")]
        public object? language { get; set; }
        [JsonPropertyName("language_preference")]
        public int? language_preference { get; set; }
        [JsonPropertyName("preference")]
        public object? preference { get; set; }
        [JsonPropertyName("ext")]
        public string? ext { get; set; }
        [JsonPropertyName("vcodec")]
        public string? vcodec { get; set; }
        [JsonPropertyName("acodec")]
        public string? acodec { get; set; }
        [JsonPropertyName("dynamic_range")]
        public object? dynamic_range { get; set; }
        [JsonPropertyName("container")]
        public string? container { get; set; }
        [JsonPropertyName("downloader_options")]
        public Downloader_Options? downloader_options { get; set; }
        [JsonPropertyName("protocol")]
        public string? protocol { get; set; }
        [JsonPropertyName("audio_ext")]
        public string? audio_ext { get; set; }
        [JsonPropertyName("video_ext")]
        public string? video_ext { get; set; }
        [JsonPropertyName("vbr")]
        [JsonConverter(typeof(FlexibleIntConverter))]
        public int? vbr { get; set; }
        [JsonPropertyName("abr")]
        public float? abr { get; set; }
        [JsonPropertyName("resolution")]
        public string? resolution { get; set; }
        [JsonPropertyName("aspect_ratio")]
        public object? aspect_ratio { get; set; }
        [JsonPropertyName("http_headers")]
        public Http_Headers? http_headers { get; set; }
        [JsonPropertyName("format")]
        public string? format { get; set; }
    }

    public class Downloader_Options
    {
        public int? http_chunk_size { get; set; }
    }

    public class Http_Headers
    {
        public string? UserAgent { get; set; }
        public string? Accept { get; set; }
        public string? AcceptLanguage { get; set; }
        public string? SecFetchMode { get; set; }
    }

    public class SubtitleData
    {
        public string? ext { get; set; }
        public string? url { get; set; }
        public string? format { get; set; }
        public string? __yt_dlp_client { get; set; }
    }

    public class Subtitle : Dictionary<string, SubtitleData[]>
    {
    }

    public class AutomaticCaptions : Dictionary<string, SubtitleData[]>
    {
    }

    public class _Version
    {
        public string? version { get; set; }
        public object? current_git_head { get; set; }
        public string? release_git_head { get; set; }
        public string? repository { get; set; }
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
