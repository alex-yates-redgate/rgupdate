namespace rgupdate;

/// <summary>
/// Handles version removal operations
/// </summary>
public static class RemovalService
{
    /// <summary>
    /// Removes specific versions or all versions of a product
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="versionSpec">Version specification (optional, prompts user if not provided)</param>
    /// <param name="force">Skip confirmation prompts</param>
    public static async Task RemoveVersionsAsync(string product, string? versionSpec = null, bool force = false)
    {
        Console.WriteLine($"Removing versions for {product}...");
        
        // Validate product
        if (!ProductConfiguration.IsProductSupported(product))
        {
            throw new ArgumentException($"Unsupported product: {product}");
        }
        
        var installedVersions = EnvironmentManager.GetInstalledVersions(product);
        if (installedVersions.Count == 0)
        {
            Console.WriteLine($"No versions of {product} are installed.");
            return;
        }
        
        // Resolve versions to remove
        var versionsToRemove = await ResolveVersionsForRemoval(product, versionSpec, installedVersions);
        if (versionsToRemove.Count == 0)
        {
            Console.WriteLine("No versions selected for removal.");
            return;
        }
        
        // Check if active version is being removed
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
        var removingActiveVersion = versionsToRemove.Any(v => 
            string.Equals(v, activeVersion, StringComparison.OrdinalIgnoreCase));
        
        // Show what will be removed
        Console.WriteLine();
        Console.WriteLine("The following versions will be removed:");
        foreach (var version in versionsToRemove)
        {
            var isActive = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"  - {version}{(isActive ? " (ACTIVE)" : "")}");
        }
        
        if (removingActiveVersion)
        {
            Console.WriteLine();
            Console.WriteLine("⚠ Warning: This will remove the currently active version!");
            Console.WriteLine("  The active directory will also be removed.");
        }
        
        // Confirm removal
        if (!force)
        {
            Console.WriteLine();
            Console.Write("Continue with removal? (y/N): ");
            var confirmation = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (confirmation != "y" && confirmation != "yes")
            {
                Console.WriteLine("Removal cancelled.");
                return;
            }
        }
        
        // Perform removal
        await PerformRemovalAsync(product, versionsToRemove, removingActiveVersion);
        
        Console.WriteLine($"✓ Successfully removed {versionsToRemove.Count} version(s) of {product}");
        
        // Show remaining versions
        var remainingVersions = EnvironmentManager.GetInstalledVersions(product);
        if (remainingVersions.Count > 0)
        {
            Console.WriteLine($"Remaining versions: {string.Join(", ", remainingVersions)}");
        }
        else
        {
            Console.WriteLine($"No versions of {product} remain installed.");
        }
    }
    
    private static async Task<List<string>> ResolveVersionsForRemoval(string product, string? versionSpec, List<string> installedVersions)
    {
        if (string.IsNullOrEmpty(versionSpec))
        {
            // Interactive selection
            return await PromptForVersionSelection(product, installedVersions);
        }
        
        if (versionSpec.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return installedVersions;
        }
        
        // Specific version
        var exactMatch = installedVersions.FirstOrDefault(v => 
            string.Equals(v, versionSpec, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
        {
            return new List<string> { exactMatch };
        }
        
        // Partial match
        var partialMatches = installedVersions
            .Where(v => v.StartsWith(versionSpec, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (partialMatches.Count == 1)
        {
            Console.WriteLine($"Resolved '{versionSpec}' to version {partialMatches[0]}");
            return partialMatches;
        }
        
        if (partialMatches.Count > 1)
        {
            Console.WriteLine($"Multiple versions match '{versionSpec}': {string.Join(", ", partialMatches)}");
            Console.WriteLine("Please specify a more specific version.");
            return new List<string>();
        }
        
        throw new ArgumentException($"Version '{versionSpec}' not found. Installed versions: {string.Join(", ", installedVersions)}");
    }
    
    private static async Task<List<string>> PromptForVersionSelection(string product, List<string> installedVersions)
    {
        Console.WriteLine();
        Console.WriteLine("Select versions to remove:");
        Console.WriteLine("0. All versions");
        
        for (int i = 0; i < installedVersions.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {installedVersions[i]}");
        }
        
        Console.WriteLine();
        Console.Write("Enter selection (number or comma-separated numbers): ");
        var input = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(input))
        {
            return new List<string>();
        }
        
        var selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
        
        var selectedVersions = new List<string>();
        
        foreach (var selection in selections)
        {
            if (int.TryParse(selection, out var index))
            {
                if (index == 0)
                {
                    return installedVersions; // All versions
                }
                else if (index >= 1 && index <= installedVersions.Count)
                {
                    selectedVersions.Add(installedVersions[index - 1]);
                }
                else
                {
                    Console.WriteLine($"Invalid selection: {index}");
                }
            }
            else
            {
                Console.WriteLine($"Invalid selection: {selection}");
            }
        }
        
        await Task.CompletedTask;
        return selectedVersions.Distinct().ToList();
    }
    
    private static async Task PerformRemovalAsync(string product, List<string> versionsToRemove, bool removeActiveDirectory)
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
        
        // Remove active directory if needed
        if (removeActiveDirectory)
        {
            try
            {
                var activeDir = PathManager.GetProductActivePath(product);
                if (Directory.Exists(activeDir))
                {
                    Directory.Delete(activeDir, recursive: true);
                    Console.WriteLine("  Removed active directory");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed to remove active directory: {ex.Message}");
            }
        }
        
        await Task.CompletedTask;
    }
}
