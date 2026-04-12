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
        bool skipCache = false,
        CancellationToken ct = default)
    {
        var validationDiagnostics = ValidateInputs(productId, topReviewsCount, topQuestionsCount, topRecommendationsCount);
        if (validationDiagnostics.Count > 0)
        {
            return new ProductOverviewDto
            {
                HasError = true,
                Warning = "Structured validation blocked an invalid product overview request.",
                SectionWarnings = validationDiagnostics,
                SectionStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["detail"] = "failed",
                    ["priceChart"] = "failed",
                    ["reviews"] = "failed",
                    ["questions"] = "failed",
                    ["recommendations"] = "failed",
                },
                SectionIssues = new Dictionary<string, ToolIssueDto>(StringComparer.OrdinalIgnoreCase)
                {
                    ["detail"] = new()
                    {
                        Code = "InvalidInput",
                        Message = "get_product_overview received invalid inputs.",
                        Tool = "get_product_overview",
                        Request = new Dictionary<string, string?>
                        {
                            ["productId"] = productId.ToString(),
                            ["warehouseId"] = warehouseId?.ToString(),
                            ["topReviewsCount"] = topReviewsCount.ToString(),
                            ["topQuestionsCount"] = topQuestionsCount.ToString(),
                            ["topRecommendationsCount"] = topRecommendationsCount.ToString(),
                            ["skipCache"] = skipCache.ToString(),
                        },
                        Retryable = false,
                        Diagnostics = validationDiagnostics,
                    },
                },
                Detail = new ProductDetailDto
                {
                    Warning = "Product overview request was rejected before upstream calls.",
                },
            };
        }

        var detailTask = FetchSection(
            "detail",
            () => detailService.ExecuteAsync(productId, warehouseId, skipCache, ct),
            "Product detail is unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["warehouseId"] = warehouseId?.ToString() });
        var priceChartTask = FetchSection(
            "priceChart",
            () => priceChartService.ExecuteAsync(productId, skipCache, ct),
            "Price chart is unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString() });
        var reviewsTask = FetchSection(
            "reviews",
            () => reviewsService.ExecuteAsync(productId, 1, skipCache, ct),
            "Reviews are unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["page"] = "1" });
        var questionsTask = FetchSection(
            "questions",
            () => questionsService.ExecuteAsync(productId, 1, skipCache, ct),
            "Questions are unavailable.",
            new Dictionary<string, string?> { ["productId"] = productId.ToString(), ["page"] = "1" });
        var recommendationsTask = FetchSection(
            "recommendations",
            () => recommendationsService.ExecuteAsync(productId, 0, skipCache, ct),
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
            IsFromCache = detail.IsFromCache || priceChart.IsFromCache || reviews.IsFromCache || questions.IsFromCache || recommendations.IsFromCache,
            CacheStatus = BuildOverviewCacheStatus(detail.CacheStatus, priceChart.CacheStatus, reviews.CacheStatus, questions.CacheStatus, recommendations.CacheStatus),
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

    private static string BuildOverviewCacheStatus(params string[] statuses)
    {
        var distinct = statuses
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return distinct.Length switch
        {
            0 => "unknown",
            1 => distinct[0],
            _ => string.Join('+', distinct.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)),
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
            return new SectionResult<T>(name, value, IsStale(value), IsFromCache(value), GetCacheStatus(value), GetToolIssue(value));
        }
        catch (Exception exception)
        {
            return new SectionResult<T>(
                name,
                default,
                false,
                false,
                "unknown",
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

    private static bool IsFromCache<T>(T? value)
    {
        if (value is null)
        {
            return false;
        }

        var property = value.GetType().GetProperty("IsFromCache");
        return property?.PropertyType == typeof(bool) &&
               property.GetValue(value) is bool isFromCache &&
               isFromCache;
    }

    private static string GetCacheStatus<T>(T? value)
    {
        if (value is null)
        {
            return "unknown";
        }

        var property = value.GetType().GetProperty("CacheStatus");
        return property?.PropertyType == typeof(string) &&
               property.GetValue(value) is string cacheStatus &&
               !string.IsNullOrWhiteSpace(cacheStatus)
            ? cacheStatus
            : "unknown";
    }

    private static ToolIssueDto? GetToolIssue<T>(T? value)
    {
        if (value is null)
        {
            return null;
        }

        var property = value.GetType().GetProperty("Error");
        return property?.PropertyType == typeof(ToolIssueDto)
            ? property.GetValue(value) as ToolIssueDto
            : null;
    }

    private static List<string> ValidateInputs(long productId, int topReviewsCount, int topQuestionsCount, int topRecommendationsCount)
    {
        var diagnostics = new List<string>();

        if (productId <= 0)
        {
            diagnostics.Add("`productId` must be greater than 0.");
        }

        if (topReviewsCount < 1)
        {
            diagnostics.Add("`topReviewsCount` must be greater than or equal to 1.");
        }

        if (topQuestionsCount < 1)
        {
            diagnostics.Add("`topQuestionsCount` must be greater than or equal to 1.");
        }

        if (topRecommendationsCount < 1)
        {
            diagnostics.Add("`topRecommendationsCount` must be greater than or equal to 1.");
        }

        return diagnostics;
    }

    private sealed record SectionResult<T>(string Name, T? Value, bool IsStale, bool IsFromCache, string CacheStatus, ToolIssueDto? Issue);
}
