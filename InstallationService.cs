namespace rgupdate;

/// <summary>
/// Handles product installation operations
/// </summary>
public static class InstallationService
{
    /// <summary>
    /// Installs a product version
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="version">Version to install (optional, defaults to latest)</param>
    public static async Task InstallProductAsync(string product, string? version = null)
    {
        Console.WriteLine($"Installing {product}...");
        
        // Validate product
        if (!ProductConfiguration.IsProductSupported(product))
        {
            throw new ArgumentException($"Unsupported product: {product}");
        }
        
        // Download and install
        var installedVersion = await EnvironmentManager.DownloadAndInstallAsync(product, version);
        
        Console.WriteLine($"✓ {product} version {installedVersion} installed successfully");
        Console.WriteLine($"  Installation path: {PathManager.GetProductVersionPath(product, installedVersion)}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine($"  - Run 'rgupdate use {product} --version {installedVersion}' to make this version active");
        Console.WriteLine($"  - Run 'rgupdate validate {product}' to verify the installation");
    }
}
