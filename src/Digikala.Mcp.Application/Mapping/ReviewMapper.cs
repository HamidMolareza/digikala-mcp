using System.Text.Json;
using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

namespace Digikala.Mcp.Application.Mapping;

public sealed class ReviewMapper
{
    public ReviewListDto MapToReviewList(JsonDocument response)
    {
        var data = JsonHelpers.GetData(response);

        return new ReviewListDto
        {
            Reviews = data.EnumerateArrayOrEmpty("comments")
                .Select(comment => new ReviewDto
                {
                    Id = comment.GetInt64OrNull("id") ?? 0,
                    Title = comment.GetStringOrNull("title") ?? string.Empty,
                    Body = comment.GetStringOrNull("body") ?? string.Empty,
                    Rating = comment.GetInt32OrNull("rate") ?? 0,
                    UserName = comment.GetStringOrNull("user_name"),
                    CreatedAt = JsonHelpers.ParseDateTimeOffset(comment.GetStringOrNull("created_at")),
                    LikeCount = comment.GetInt32OrNull("reactions", "likes") ?? 0,
                    DislikeCount = comment.GetInt32OrNull("reactions", "dislikes") ?? 0,
                    IsVerifiedPurchase = string.Equals(comment.GetStringOrNull("recommendation"), "recommended", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(comment.GetStringOrNull("recommendation"), "no_idea", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(comment.GetStringOrNull("is_buyer"), bool.TrueString, StringComparison.OrdinalIgnoreCase),
                    Pros = comment.EnumerateArrayOrEmpty("advantages")
                        .Select(item => item.GetString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Cast<string>()
                        .ToList(),
                    Cons = comment.EnumerateArrayOrEmpty("disadvantages")
                        .Select(item => item.GetString())
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Cast<string>()
                        .ToList(),
                })
                .ToList(),
            CurrentPage = data.GetInt32OrNull("pager", "current_page") ?? 1,
            TotalPages = data.GetInt32OrNull("pager", "total_pages") ?? 1,
            TotalCount = data.GetInt32OrNull("pager", "total_items") ?? 0,
            AverageRating = data.GetDoubleOrNull("ratings", "average") ?? data.GetDoubleOrNull("average_rating"),
        };
    }
}
