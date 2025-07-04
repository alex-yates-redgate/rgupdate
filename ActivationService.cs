using System.Diagnostics;

namespace rgupdate;

/// <summary>
/// Handles version activation operations
/// </summary>
public static class ActivationService
{
    /// <summary>
    /// Sets a specific version as active
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="version">Version to activate (optional, defaults to latest installed)</param>
    public static async Task SetActiveVersionAsync(string product, string? version = null)
    {
        Console.WriteLine($"Setting active version for {product}...");
        
        // Validate product
        if (!ProductConfiguration.IsProductSupported(product))
        {
            throw new ArgumentException($"Unsupported product: {product}");
        }
        
        // Get installed versions
        var installedVersions = EnvironmentManager.GetInstalledVersions(product);
        if (installedVersions.Count == 0)
        {
            throw new InvalidOperationException($"No versions of {product} are installed. Run 'rgupdate get {product}' first.");
        }
        
        // Resolve target version
        string targetVersion;
        if (string.IsNullOrEmpty(version) || version.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            // Find the latest installed version
            var sortedVersions = installedVersions
                .Select(v => new EnvironmentManager.SemanticVersion(v))
                .OrderByDescending(v => v)
                .ToList();
            
            targetVersion = sortedVersions.First().OriginalVersion;
            Console.WriteLine($"Resolved 'latest' to version {targetVersion}");
        }
        else
        {
            // Validate the specified version is installed
            var exactMatch = installedVersions.FirstOrDefault(v => 
                string.Equals(v, version, StringComparison.OrdinalIgnoreCase));
            
            if (exactMatch == null)
            {
                throw new ArgumentException($"Version {version} of {product} is not installed. Available versions: {string.Join(", ", installedVersions)}");
            }
            
            targetVersion = exactMatch;
        }
        
        // Copy version to active directory
        var sourceDir = PathManager.GetProductVersionPath(product, targetVersion);
        var activeDir = PathManager.GetProductActivePath(product);
        
        Console.WriteLine($"Copying {product} version {targetVersion} to active directory...");
        
        // Remove existing active directory if it exists
        if (Directory.Exists(activeDir))
        {
            Directory.Delete(activeDir, recursive: true);
        }
        
        // Copy the version to active directory
        await CopyDirectoryAsync(sourceDir, activeDir);
        
        Console.WriteLine($"✓ {product} version {targetVersion} is now active");
        
        // Update PATH if needed
        await UpdatePathEnvironmentAsync(product);
        
        Console.WriteLine();
        Console.WriteLine("Verification:");
        
        // Verify the version is working
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
        if (activeVersion != null)
        {
            Console.WriteLine($"✓ Active version: {activeVersion}");
        }
        else
        {
            Console.WriteLine("⚠ Warning: Could not verify active version. You may need to restart your terminal.");
        }
    }
    
    private static async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }
        
        Directory.CreateDirectory(destinationDir);
        
        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destinationFile = Path.Combine(destinationDir, relativePath);
            
            // Ensure destination directory exists
            var destinationFileDir = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(destinationFileDir))
            {
                Directory.CreateDirectory(destinationFileDir);
            }
            
            File.Copy(file, destinationFile, overwrite: true);
        }
        
        await Task.CompletedTask;
    }
    
    private static async Task UpdatePathEnvironmentAsync(string product)
    {
        try
        {
            var activeDir = PathManager.GetProductActivePath(product);
            var binPath = product.Equals("flyway", StringComparison.OrdinalIgnoreCase) 
                ? activeDir 
                : activeDir; // For rgsubset/rganonymize, the executables are in the root of the active directory
            
            if (OperatingSystem.IsWindows())
            {
                await UpdateWindowsPathAsync(binPath);
            }
            else
            {
                await UpdateUnixPathAsync(binPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Could not update PATH environment variable: {ex.Message}");
            Console.WriteLine($"  You may need to manually add the active directory to your PATH or restart your terminal.");
        }
    }
    
    private static async Task UpdateWindowsPathAsync(string pathToAdd)
    {
        try
        {
            // Try to update machine-level PATH first
            var machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
            
            if (!machinePath.Contains(pathToAdd, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var newMachinePath = string.IsNullOrEmpty(machinePath) ? pathToAdd : $"{machinePath};{pathToAdd}";
                    Environment.SetEnvironmentVariable("PATH", newMachinePath, EnvironmentVariableTarget.Machine);
                    Console.WriteLine($"✓ Added to machine-level PATH: {pathToAdd}");
                }
                catch (UnauthorizedAccessException)
                {
                    // Fallback to user-level PATH
                    var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                    if (!userPath.Contains(pathToAdd, StringComparison.OrdinalIgnoreCase))
                    {
                        var newUserPath = string.IsNullOrEmpty(userPath) ? pathToAdd : $"{userPath};{pathToAdd}";
                        Environment.SetEnvironmentVariable("PATH", newUserPath, EnvironmentVariableTarget.User);
                        Console.WriteLine($"✓ Added to user-level PATH: {pathToAdd}");
                        Console.WriteLine("  Note: Run as Administrator to set machine-level PATH for all users.");
                    }
                }
            }
            else
            {
                Console.WriteLine($"✓ PATH already contains: {pathToAdd}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update Windows PATH: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }
    
    private static async Task UpdateUnixPathAsync(string pathToAdd)
    {
        // For Unix systems, we would typically update shell profile files
        // This is a simplified implementation
        Console.WriteLine($"ℹ On Unix systems, you may need to manually add to your PATH:");
        Console.WriteLine($"  export PATH=\"{pathToAdd}:$PATH\"");
        
        await Task.CompletedTask;
    }
}
