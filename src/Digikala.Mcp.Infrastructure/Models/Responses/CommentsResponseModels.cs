using System.Text.Json.Serialization;
using System.Text.Json;
using Digikala.Mcp.Infrastructure.Models.Common;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class CommentsResponseData
{
    [JsonPropertyName("comments")]
    public List<CommentModel>? Comments { get; set; }

    [JsonPropertyName("media_comments")]
    public List<CommentModel>? MediaComments { get; set; }

    [JsonPropertyName("sort")]
    public Dictionary<string, JsonElement>? Sort { get; set; }

    [JsonPropertyName("sort_options")]
    public List<SortOptionItem>? SortOptions { get; set; }

    [JsonPropertyName("pager")]
    public PagerModel? Pager { get; set; }
}

public sealed class CommentModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("rate")]
    public int Rate { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("is_buyer")]
    public bool IsBuyer { get; set; }

    [JsonPropertyName("recommendation")]
    public string? Recommendation { get; set; }

    [JsonPropertyName("advantages")]
    public List<string>? Advantages { get; set; }

    [JsonPropertyName("disadvantages")]
    public List<string>? Disadvantages { get; set; }

    [JsonPropertyName("reactions")]
    public ReactionModel? Reactions { get; set; }
}

public sealed class ReactionModel
{
    [JsonPropertyName("likes")]
    public int Likes { get; set; }

    [JsonPropertyName("dislikes")]
    public int Dislikes { get; set; }
}
