namespace rgupdate;

/// <summary>
/// Manages installation paths for Red Gate CLI tools
/// </summary>
public static class PathManager
{
    /// <summary>
    /// Gets the installation path for a specific product and version
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="version">Version string</param>
    /// <returns>Full installation path</returns>
    public static string GetProductVersionPath(string product, string version)
    {
        var installLocation = EnvironmentManager.GetInstallLocation();
        var productInfo = ProductConfiguration.GetProductInfo(product);
        return Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder, version);
    }
    
    /// <summary>
    /// Gets the active installation path for a specific product
    /// </summary>
    /// <param name="product">Product name</param>
    /// <returns>Path to active version directory</returns>
    public static string GetProductActivePath(string product)
    {
        var installLocation = EnvironmentManager.GetInstallLocation();
        var productInfo = ProductConfiguration.GetProductInfo(product);
        return Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder, Constants.ActiveVersionDirectoryName);
    }
    
    /// <summary>
    /// Gets the base product path (without version)
    /// </summary>
    /// <param name="product">Product name</param>
    /// <returns>Base product installation path</returns>
    public static string GetProductBasePath(string product)
    {
        var installLocation = EnvironmentManager.GetInstallLocation();
        var productInfo = ProductConfiguration.GetProductInfo(product);
        return Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder);
    }
}
