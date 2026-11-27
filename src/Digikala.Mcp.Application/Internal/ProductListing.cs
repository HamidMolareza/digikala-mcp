using System.Text.Json;

namespace Digikala.Mcp.Application.Internal;

internal static class ProductListing
{
    /// <summary>
    /// Returns the element that holds pager/filters/category and the product elements of a listing response.
    /// Discovery and amazing endpoints wrap the listing in <c>widgets[type=vertical_product_listing].data</c>
    /// and each product in <c>widgets[type=product].data</c>; older endpoints use a flat <c>products</c> array.
    /// </summary>
    public static (JsonElement Listing, IReadOnlyList<JsonElement> Products) Unwrap(JsonElement data)
    {
        var listingWidget = data.EnumerateArrayOrEmpty("widgets")
            .FirstOrDefault(widget => widget.GetStringOrNull("type") == "vertical_product_listing");

        if (listingWidget.ValueKind == JsonValueKind.Object && listingWidget.TryGetNested(out var listing, "data"))
        {
            var products = listing.EnumerateArrayOrEmpty("widgets")
                .Where(widget => widget.GetStringOrNull("type") == "product")
                .Select(widget => widget.TryGetNested(out var product, "data") ? product : default)
                .Where(product => product.ValueKind == JsonValueKind.Object)
                .ToList();
            return (listing, products);
        }

        return (data, data.EnumerateArrayOrEmpty("products").ToList());
    }
}
