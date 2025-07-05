namespace rgupdate;

/// <summary>
/// Product information including family and CLI folder name
/// </summary>
public record ProductInfo(string Family, string CliFolder);

/// <summary>
/// Product configuration and mapping utilities
/// </summary>
public static class ProductConfiguration
{
    /// <summary>
    /// Maps products to their product families and CLI folder structures
    /// </summary>
    private static readonly Dictionary<string, ProductInfo> ProductMapping = new()
    {
        ["flyway"] = new ProductInfo("Flyway", "CLI"),
        ["rgsubset"] = new ProductInfo("Test Data Manager", "rgsubset"),
        ["rganonymize"] = new ProductInfo("Test Data Manager", "rganonymize")
    };
    
    /// <summary>
    /// Gets product information for a given product name
    /// </summary>
    /// <param name="product">Product name (case-insensitive)</param>
    /// <returns>Product information</returns>
    /// <exception cref="ArgumentException">Thrown when product is not supported</exception>
    public static ProductInfo GetProductInfo(string product)
    {
        if (ProductMapping.TryGetValue(product.ToLowerInvariant(), out var productInfo))
        {
            return productInfo;
        }
        
        throw new ArgumentException($"Unsupported product: {product}. Supported products: {string.Join(", ", Constants.SupportedProducts)}");
    }
    
    /// <summary>
    /// Checks if a product is supported
    /// </summary>
    /// <param name="product">Product name (case-insensitive)</param>
    /// <returns>True if product is supported</returns>
    public static bool IsProductSupported(string product)
    {
        return ProductMapping.ContainsKey(product.ToLowerInvariant());
    }
}
