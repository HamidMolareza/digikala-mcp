using Digikala.Mcp.Application.Dtos;

namespace Digikala.Mcp.Application.Services;

public sealed class GetProductOverviewService(
    GetProductDetailService detailService,
    GetProductPriceChartService priceChartService,
    GetProductReviewsService reviewsService,
    GetProductQuestionsService questionsService,
    GetProductRecommendationsService recommendationsService)
{
    public async Task<ProductOverviewDto> ExecuteAsync(
        long productId,
        int? warehouseId = null,
        int topReviewsCount = 3,
        int topQuestionsCount = 3,
        int topRecommendationsCount = 6,
        CancellationToken ct = default)
    {
        var detailTask = detailService.ExecuteAsync(productId, warehouseId, ct);
        var priceChartTask = priceChartService.ExecuteAsync(productId, ct);
        var reviewsTask = reviewsService.ExecuteAsync(productId, 1, ct);
        var questionsTask = questionsService.ExecuteAsync(productId, 1, ct);
        var recommendationsTask = recommendationsService.ExecuteAsync(productId, 0, ct);

        await Task.WhenAll(detailTask, priceChartTask, reviewsTask, questionsTask, recommendationsTask);

        var detail = await detailTask;
        var priceChart = await priceChartTask;
        var reviews = await reviewsTask;
        var questions = await questionsTask;
        var recommendations = await recommendationsTask;

        var warning = detail.Warning ??
                      priceChart.Warning ??
                      reviews.Warning ??
                      questions.Warning ??
                      recommendations.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Warning))?.Warning;

        return new ProductOverviewDto
        {
            IsStale = detail.IsStale || priceChart.IsStale || reviews.IsStale || questions.IsStale || recommendations.Any(item => item.IsStale),
            Warning = warning,
            Detail = detail,
            PriceChart = priceChart,
            TopReviews = reviews.Reviews
                .OrderByDescending(item => item.Rating)
                .ThenByDescending(item => item.LikeCount)
                .ThenByDescending(item => item.CreatedAt)
                .Take(Math.Max(1, topReviewsCount))
                .ToList(),
            TopQuestions = questions.Questions
                .OrderByDescending(item => item.Answers.Count)
                .ThenByDescending(item => item.Answers.Sum(answer => answer.LikeCount))
                .ThenByDescending(item => item.CreatedAt)
                .Take(Math.Max(1, topQuestionsCount))
                .ToList(),
            Recommendations = recommendations
                .Take(Math.Max(1, topRecommendationsCount))
                .ToList(),
        };
    }
}
