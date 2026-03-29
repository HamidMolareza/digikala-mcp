using System.ComponentModel;

namespace Digikala.Mcp.Application.Dtos;

public record ProductSummaryDto
{
    public bool IsStale { get; set; }
    public string? Warning { get; set; }
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? EnglishTitle { get; init; }
    public string? MainImageUrl { get; init; }
    public long PriceRials { get; init; }
    public long? OriginalPriceRials { get; init; }
    public int? DiscountPercent { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    public string? CategorySlug { get; init; }
    public bool IsAvailable { get; init; }
    public string Url { get; init; } = string.Empty;
    public double? SearchScore { get; set; }
    public List<string> MatchedSignals { get; set; } = [];
    public List<string> RejectedSignals { get; set; } = [];
    public List<string> AttributeHighlights { get; set; } = [];
    public List<string> UseCaseHints { get; set; } = [];
    public List<string> SuitabilityNotes { get; set; } = [];
    public List<string> SuitableForHairTypes { get; set; } = [];
    public List<string> SuitableForScalpTypes { get; set; } = [];
    public bool? IsDailyUse { get; set; }
    public bool? IsSulfateFree { get; set; }
    public bool? IsParabenFree { get; set; }
    public List<string> KeyIngredients { get; set; } = [];
    public List<string> ConcernsTargeted { get; set; } = [];
    public List<string> PotentialCautions { get; set; } = [];
}
