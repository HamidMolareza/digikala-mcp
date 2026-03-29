using Digikala.Mcp.Application.Dtos;
using Digikala.Mcp.Application.Internal;

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
        var detailTask = FetchSection(
            "detail",
            () => detailService.ExecuteAsync(productId, warehouseId, ct),
            "Product detail is unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["warehouseId"] = warehouseId?.ToString() });
        var priceChartTask = FetchSection(
            "priceChart",
            () => priceChartService.ExecuteAsync(productId, ct),
            "Price chart is unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString() });
        var reviewsTask = FetchSection(
            "reviews",
            () => reviewsService.ExecuteAsync(productId, 1, ct),
            "Reviews are unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["page"] = "1" });
        var questionsTask = FetchSection(
            "questions",
            () => questionsService.ExecuteAsync(productId, 1, ct),
            "Questions are unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["page"] = "1" });
        var recommendationsTask = FetchSection(
            "recommendations",
            () => recommendationsService.ExecuteAsync(productId, 0, ct),
            "Recommendations are unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["offset"] = "0" });

        await Task.WhenAll(detailTask, priceChartTask, reviewsTask, questionsTask, recommendationsTask);

        var detail = await detailTask;
        var priceChart = await priceChartTask;
        var reviews = await reviewsTask;
        var questions = await questionsTask;
        var recommendations = await recommendationsTask;

        var sectionWarnings = new List<string>();
        var sectionStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sectionIssues = new Dictionary<string, ToolIssueDto>(StringComparer.OrdinalIgnoreCase);

        Register(sectionStatus, sectionWarnings, sectionIssues, detail);
        Register(sectionStatus, sectionWarnings, sectionIssues, priceChart);
        Register(sectionStatus, sectionWarnings, sectionIssues, reviews);
        Register(sectionStatus, sectionWarnings, sectionIssues, questions);
        Register(sectionStatus, sectionWarnings, sectionIssues, recommendations);

        return new ProductOverviewDto
        {
            IsStale = detail.IsStale || priceChart.IsStale || reviews.IsStale || questions.IsStale || recommendations.IsStale,
            HasError = detail.Issue is not null,
            Warning = sectionWarnings.Count > 0 ? string.Join(" ", sectionWarnings) : null,
            SectionWarnings = sectionWarnings,
            SectionStatus = sectionStatus,
            SectionIssues = sectionIssues,
            Detail = detail.Value ?? new ProductDetailDto
            {
                Warning = detail.Issue?.Message,
                IsStale = detail.IsStale,
            },
            PriceChart = priceChart.Value,
            TopReviews = (reviews.Value?.Reviews ?? [])
                .OrderByDescending(item => item.Rating)
                .ThenByDescending(item => item.LikeCount)
                .ThenByDescending(item => item.CreatedAt)
                .Take(Math.Max(1, topReviewsCount))
                .ToList(),
            TopQuestions = (questions.Value?.Questions ?? [])
                .OrderByDescending(item => item.Answers.Count)
                .ThenByDescending(item => item.Answers.Sum(answer => answer.LikeCount))
                .ThenByDescending(item => item.CreatedAt)
                .Take(Math.Max(1, topQuestionsCount))
                .ToList(),
            Recommendations = (recommendations.Value ?? [])
                .Take(Math.Max(1, topRecommendationsCount))
                .ToList(),
        };
    }

    private static void Register<T>(
        IDictionary<string, string> sectionStatus,
        ICollection<string> sectionWarnings,
        IDictionary<string, ToolIssueDto> sectionIssues,
        SectionResult<T> result)
    {
        sectionStatus[result.Name] = result.Issue is null ? "ok" : "failed";

        if (result.Issue is not null)
        {
            sectionWarnings.Add(result.Issue.Message);
            sectionIssues[result.Name] = result.Issue;
        }
    }

    private static async Task<SectionResult<T>> FetchSection<T>(
        string name,
        Func<Task<T>> factory,
        string failureMessage,
        Dictionary<string, string?> request)
    {
        try
        {
            var value = await factory();
            return new SectionResult<T>(name, value, IsStale(value), null);
        }
        catch (Exception exception)
        {
            return new SectionResult<T>(
                name,
                default,
                false,
                ToolIssueFactory.Create("get_product_overview", failureMessage, exception, request));
        }
    }

    private static bool IsStale<T>(T? value)
    {
        if (value is null)
        {
            return false;
        }

        var property = value.GetType().GetProperty("IsStale");
        return property?.PropertyType == typeof(bool) &&
               property.GetValue(value) is bool isStale &&
               isStale;
    }

    private sealed record SectionResult<T>(string Name, T? Value, bool IsStale, ToolIssueDto? Issue);
}
