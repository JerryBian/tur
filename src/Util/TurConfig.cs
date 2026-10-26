using System.Text.Json.Serialization;

namespace Tur.Util;

/// <summary>
/// User defaults loaded from ~/.tur.json (or the file pointed to by TUR_CONFIG).
/// Every value is optional; explicit CLI arguments always win.
/// </summary>
public class TurConfig
{
    [JsonPropertyName("includes")]
    public string[] Includes { get; set; }

    [JsonPropertyName("excludes")]
    public string[] Excludes { get; set; }

    [JsonPropertyName("output")]
    public string Output { get; set; }

    [JsonPropertyName("minSize")]
    public string MinSize { get; set; }

    [JsonPropertyName("maxSize")]
    public string MaxSize { get; set; }

    [JsonPropertyName("maxDepth")]
    public int? MaxDepth { get; set; }

    [JsonPropertyName("threads")]
    public int? Threads { get; set; }

    [JsonPropertyName("bwlimit")]
    public string BwLimit { get; set; }

    [JsonPropertyName("ignoreError")]
    public bool? IgnoreError { get; set; }

    [JsonPropertyName("verbose")]
    public bool? Verbose { get; set; }

    [JsonPropertyName("json")]
    public bool? Json { get; set; }

    [JsonPropertyName("progress")]
    public bool? Progress { get; set; }

    [JsonPropertyName("followLinks")]
    public bool? FollowLinks { get; set; }

    [JsonPropertyName("noUserInteraction")]
    public bool? NoUserInteraction { get; set; }

    [JsonPropertyName("yes")]
    public bool? Yes { get; set; }

    [JsonPropertyName("trash")]
    public bool? Trash { get; set; }

    [JsonPropertyName("keep")]
    public string Keep { get; set; }

    [JsonPropertyName("deleteExcluded")]
    public bool? DeleteExcluded { get; set; }
}
