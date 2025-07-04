namespace rgupdate;

/// <summary>
/// Handles version purging operations (keeping only recent versions)
/// </summary>
public static class PurgeService
{
    /// <summary>
    /// Removes old versions, keeping only the specified number of most recent versions
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="keepCount">Number of recent versions to keep</param>
    /// <param name="force">Skip confirmation prompts</param>
    public static async Task PurgeOldVersionsAsync(string product, int keepCount = 3, bool force = false)
    {
        Console.WriteLine($"Purging old versions of {product} (keeping {keepCount} most recent)...");
        
        // Validate product
        if (!ProductConfiguration.IsProductSupported(product))
        {
            throw new ArgumentException($"Unsupported product: {product}");
        }
        
        if (keepCount < 1)
        {
            throw new ArgumentException("Keep count must be at least 1");
        }
        
        var installedVersions = EnvironmentManager.GetInstalledVersions(product);
        if (installedVersions.Count == 0)
        {
            Console.WriteLine($"No versions of {product} are installed.");
            return;
        }
        
        if (installedVersions.Count <= keepCount)
        {
            Console.WriteLine($"Only {installedVersions.Count} version(s) installed, nothing to purge.");
            return;
        }
        
        // Sort versions by semantic version (most recent first)
        var sortedVersions = installedVersions
            .Select(v => new EnvironmentManager.SemanticVersion(v))
            .OrderByDescending(v => v)
            .ToList();
        
        var versionsToKeep = sortedVersions.Take(keepCount).Select(v => v.OriginalVersion).ToList();
        var versionsToRemove = sortedVersions.Skip(keepCount).Select(v => v.OriginalVersion).ToList();
        
        // Check if active version is being removed
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
        var removingActiveVersion = versionsToRemove.Any(v => 
            string.Equals(v, activeVersion, StringComparison.OrdinalIgnoreCase));
        
        if (removingActiveVersion)
        {
            // Move active version to keep list if it's not already there
            if (!versionsToKeep.Any(v => string.Equals(v, activeVersion, StringComparison.OrdinalIgnoreCase)))
            {
                // Remove the oldest version from keep list and add active version
                if (versionsToKeep.Count >= keepCount)
                {
                    var oldestToKeep = versionsToKeep.Last();
                    versionsToKeep.Remove(oldestToKeep);
                    versionsToRemove.Add(oldestToKeep);
                }
                
                versionsToKeep.Add(activeVersion!);
                versionsToRemove.Remove(activeVersion!);
                
                Console.WriteLine($"ℹ Active version {activeVersion} will be preserved");
            }
        }
        
        if (versionsToRemove.Count == 0)
        {
            Console.WriteLine("No versions need to be removed.");
            return;
        }
        
        // Show what will be removed/kept
        Console.WriteLine();
        Console.WriteLine($"Versions to keep ({versionsToKeep.Count}):");
        foreach (var version in versionsToKeep.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)))
        {
            var isActive = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"  ✓ {version}{(isActive ? " (ACTIVE)" : "")}");
        }
        
        Console.WriteLine();
        Console.WriteLine($"Versions to remove ({versionsToRemove.Count}):");
        foreach (var version in versionsToRemove.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)))
        {
            Console.WriteLine($"  ❌ {version}");
        }
        
        // Confirm removal
        if (!force)
        {
            Console.WriteLine();
            Console.Write($"Remove {versionsToRemove.Count} old version(s)? (y/N): ");
            var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (confirmation != "y" && confirmation != "yes")
            {
                Console.WriteLine("Purge cancelled.");
                return;
            }
        }
        
        // Perform removal
        await PerformPurgeAsync(product, versionsToRemove);
        
        Console.WriteLine($"✓ Successfully purged {versionsToRemove.Count} old version(s) of {product}");
        Console.WriteLine($"Kept {versionsToKeep.Count} most recent version(s)");
    }
    
    private static async Task PerformPurgeAsync(string product, List<string> versionsToRemove)
    {
        foreach (var version in versionsToRemove)
        {
            try
            {
                var versionPath = PathManager.GetProductVersionPath(product, version);
                if (Directory.Exists(versionPath))
                {
                    Directory.Delete(versionPath, recursive: true);
                    Console.WriteLine($"  Removed {version}");
                }
                else
                {
                    Console.WriteLine($"  Version {version} directory not found (already removed?)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to remove {version}: {ex.Message}");
            }
        }
        
        await Task.CompletedTask;
    }
}
