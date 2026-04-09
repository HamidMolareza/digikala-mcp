using System.Text.Json.Serialization;
using System.Text.Json;
using Digikala.Mcp.Infrastructure.Models.Common;

namespace Digikala.Mcp.Infrastructure.Models.Responses;

public sealed class QuestionsResponseData
{
    [JsonPropertyName("questions")]
    public List<QuestionModel>? Questions { get; set; }

    [JsonPropertyName("sort")]
    public Dictionary<string, JsonElement>? Sort { get; set; }

    [JsonPropertyName("sort_options")]
    public List<SortOptionItem>? SortOptions { get; set; }

    [JsonPropertyName("pager")]
    public PagerModel? Pager { get; set; }
}

public sealed class QuestionModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("answers")]
    public List<AnswerModel>? Answers { get; set; }
}

public sealed class AnswerModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("reactions")]
    public ReactionModel? Reactions { get; set; }
}
