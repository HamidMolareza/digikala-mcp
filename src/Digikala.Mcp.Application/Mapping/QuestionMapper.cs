using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class QuestionMapper
{
    public QuestionListDto MapToQuestionList(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        return new QuestionListDto
        {
            Questions = data.EnumerateArrayOrEmpty("questions")
                .Select(question => new QuestionDto
                {
                    Id = question.GetInt64OrNull("id") ?? 0,
                    Body = question.GetStringOrNull("body") ?? string.Empty,
                    UserName = question.GetStringOrNull("user_name"),
                    CreatedAt = JsonHelpers.ParseDateTimeOffset(question.GetStringOrNull("created_at")),
                    Answers = question.EnumerateArrayOrEmpty("answers")
                        .Select(answer => new AnswerDto
                        {
                            Id = answer.GetInt64OrNull("id") ?? 0,
                            Body = answer.GetStringOrNull("body") ?? string.Empty,
                            UserName = answer.GetStringOrNull("user_name"),
                            CreatedAt = JsonHelpers.ParseDateTimeOffset(answer.GetStringOrNull("created_at")),
                            LikeCount = answer.GetInt32OrNull("reactions", "likes") ?? 0,
                        })
                        .ToList(),
                })
                .ToList(),
            CurrentPage = data.GetInt32OrNull("pager", "current_page") ?? 1,
            TotalPages = data.GetInt32OrNull("pager", "total_pages") ?? 1,
            TotalCount = data.GetInt32OrNull("pager", "total_items") ?? 0,
        };
    }
}
