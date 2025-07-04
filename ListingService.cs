using System.Globalization;

namespace rgupdate;

/// <summary>
/// Handles version listing operations
/// </summary>
public static class ListingService
{
    /// <summary>
    /// Lists available versions for a product
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="showAll">Whether to show all versions or just recent ones</param>
    public static async Task ListVersionsAsync(string product, bool showAll = false)
    {
        Console.WriteLine($"Listing versions for {product}...");
        
        // Validate product
        if (!ProductConfiguration.IsProductSupported(product))
        {
            throw new ArgumentException($"Unsupported product: {product}");
        }
        
        try
        {
            var platform = OperatingSystem.IsWindows() ? EnvironmentManager.Platform.Windows : EnvironmentManager.Platform.Linux;
            
            // Get online versions (may fail for flyway)
            List<EnvironmentManager.VersionInfo> onlineVersions;
            try
            {
                onlineVersions = await EnvironmentManager.GetAllPublicVersionsAsync(product, platform);
            }
            catch (NotSupportedException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                await ListInstalledVersionsOnlyAsync(product);
                return;
            }
            
            // Get installed versions
            var installedVersions = EnvironmentManager.GetInstalledVersions(product);
            var installedVersionsSet = new HashSet<string>(installedVersions, StringComparer.OrdinalIgnoreCase);
            
            // Get local-only versions
            var localOnlyVersions = await EnvironmentManager.GetLocalVersionInfoAsync(product, 
                onlineVersions.Select(v => v.Version).ToList());
            
            // Combine and prepare display data
            var allVersions = new List<EnvironmentManager.DisplayVersionInfo>();
            
            // Add online versions
            foreach (var version in onlineVersions)
            {
                var isInstalled = installedVersionsSet.Contains(version.Version);
                allVersions.Add(new EnvironmentManager.DisplayVersionInfo(
                    version.Version, 
                    version.LastModified, 
                    version.Size, 
                    false
                ));
            }
            
            // Add local-only versions
            foreach (var version in localOnlyVersions)
            {
                allVersions.Add(new EnvironmentManager.DisplayVersionInfo(
                    version.Version,
                    version.LastModified,
                    version.Size,
                    true
                ));
            }
            
            // Sort by semantic version (descending)
            var sortedVersions = allVersions
                .Select(v => new { Version = v, Semantic = new EnvironmentManager.SemanticVersion(v.Version) })
                .OrderByDescending(x => x.Semantic)
                .Select(x => x.Version)
                .ToList();
            
            // Limit display unless --all is specified
            var versionsToShow = showAll ? sortedVersions : sortedVersions.Take(Constants.DefaultVersionDisplayLimit).ToList();
            
            await DisplayVersionTableAsync(product, versionsToShow, installedVersionsSet);
            
            if (!showAll && sortedVersions.Count > Constants.DefaultVersionDisplayLimit)
            {
                var hiddenCount = sortedVersions.Count - Constants.DefaultVersionDisplayLimit;
                Console.WriteLine($"{hiddenCount} older versions (To see all versions, run: rgupdate list {product} --all)");
            }
            
            // Show active version info
            await DisplayActiveVersionInfoAsync(product);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list versions for {product}: {ex.Message}", ex);
        }
    }
    
    private static async Task ListInstalledVersionsOnlyAsync(string product)
    {
        var installedVersions = EnvironmentManager.GetInstalledVersions(product);
        
        if (installedVersions.Count == 0)
        {
            Console.WriteLine($"No versions of {product} are currently installed.");
            Console.WriteLine($"Run 'rgupdate get {product} --version <version>' to install a specific version.");
            return;
        }
        
        Console.WriteLine("Installed versions:");
        Console.WriteLine("Version         | Status");
        Console.WriteLine("----------------|--------");
        
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
        
        foreach (var version in installedVersions.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)))
        {
            var status = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "installed";
            Console.WriteLine($"{version,-15} | {status}");
        }
        
        await DisplayActiveVersionInfoAsync(product);
    }
    
    private static async Task DisplayVersionTableAsync(string product, List<EnvironmentManager.DisplayVersionInfo> versions, HashSet<string> installedVersions)
    {
        Console.WriteLine("Version         | Release Date  | Size      | Status");
        Console.WriteLine("----------------|---------------|-----------|--------");
        
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
        
        foreach (var version in versions)
        {
            var releaseDateStr = version.IsLocalOnly ? "local-only" : version.LastModified?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "unknown";
            var sizeStr = FormatBytes(version.Size);
            
            var status = "";
            if (string.Equals(version.Version, activeVersion, StringComparison.OrdinalIgnoreCase))
            {
                status = "ACTIVE";
            }
            else if (installedVersions.Contains(version.Version))
            {
                status = "installed";
            }
            else
            {
                status = "-";
            }
            
            Console.WriteLine($"{version.Version,-15} | {releaseDateStr,-13} | {sizeStr,-9} | {status}");
        }
    }
    
    private static async Task DisplayActiveVersionInfoAsync(string product)
    {
        Console.WriteLine();
        Console.WriteLine("Active Version Status:");
        Console.WriteLine("======================");
        
        // Check active directory version
        var activeDir = PathManager.GetProductActivePath(product);
        if (Directory.Exists(activeDir))
        {
            // Try to determine version from active directory contents
            var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
            if (activeVersion != null)
            {
                Console.WriteLine($"✓ Active directory version: {activeVersion}");
            }
            else
            {
                Console.WriteLine("⚠ Active directory exists but version could not be determined");
            }
        }
        else
        {
            Console.WriteLine("❌ No active version set");
        }
        
        // Check PATH version
        try
        {
            var pathVersion = await EnvironmentManager.GetActiveVersionAsync(product);
            if (pathVersion != null)
            {
                Console.WriteLine($"✓ PATH version: {pathVersion}");
            }
            else
            {
                Console.WriteLine("❌ Tool not found in PATH");
                Console.WriteLine("   The tool is not accessible from PATH - you may need to:");
                Console.WriteLine("   - Restart your terminal to pick up PATH changes");
                Console.WriteLine("   - Or run the PATH update command shown when you used 'rgupdate use'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error checking PATH version: {ex.Message}");
            Console.WriteLine("   The tool is not accessible from PATH - you may need to:");
            Console.WriteLine("   - Restart your terminal to pick up PATH changes");
            Console.WriteLine("   - Or run the PATH update command shown when you used 'rgupdate use'");
        }
    }
    
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size = size / 1024;
        }
        
        return $"{size:0.#} {sizes[order]}";
    }
}
