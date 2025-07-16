using System.Diagnostics;

namespace rgupdate;

/// <summary>
/// Handles product validation operations
/// </summary>
public static class ValidationService
{
    /// <summary>
    /// Validates a product installation
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="targetVersion">Optional specific version to validate</param>
    /// <returns>Validation result</returns>
    public static async Task<ValidationResult> ValidateProductAsync(string product, string? targetVersion = null)
    {
        try
        {
            // Check for null or empty product first
            if (string.IsNullOrEmpty(product))
            {
                var message = "Product name cannot be null or empty";
                Console.WriteLine($"❌ {message}");
                return new ValidationResult(false, message);
            }
            
            // Check if product is supported
            if (!ProductConfiguration.IsProductSupported(product))
            {
                var message = $"Unsupported product: {product}. Supported products: {string.Join(", ", Constants.SupportedProducts)}";
                Console.WriteLine($"❌ {message}");
                return new ValidationResult(false, message);
            }
            
            Console.WriteLine($"Validating {product}...");
            
            // Get installed versions
            var installedVersions = EnvironmentManager.GetInstalledVersions(product);
            
            if (targetVersion != null)
            {
                return await ValidateSpecificVersionAsync(product, targetVersion, installedVersions);
            }
            else
            {
                return await ValidateAllVersionsAsync(product, installedVersions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Validation failed: {ex.Message}");
            return new ValidationResult(false, ex.Message);
        }
    }
    
    private static async Task<ValidationResult> ValidateSpecificVersionAsync(string product, string targetVersion, List<string> installedVersions)
    {
        // Check if version is installed
        if (!installedVersions.Any(v => string.Equals(v, targetVersion, StringComparison.OrdinalIgnoreCase)))
        {
            var message = $"Version {targetVersion} of {product} is not installed.";
            Console.WriteLine($"❌ {message}");
            return new ValidationResult(false, message);
        }
        
        // Test execution
        var executablePath = GetExecutablePath(product, targetVersion);
        if (!File.Exists(executablePath))
        {
            var message = $"Executable not found at expected location: {executablePath}";
            Console.WriteLine($"❌ {message}");
            return new ValidationResult(false, message);
        }
        
        // Test version command
        var versionOutput = await TestVersionCommandAsync(product, executablePath);
        if (versionOutput == null)
        {
            var message = $"Version command failed for {product} {targetVersion}";
            Console.WriteLine($"❌ {message}");
            return new ValidationResult(false, message);
        }
        
        Console.WriteLine($"✓ {product} version {targetVersion} is installed and working correctly");
        Console.WriteLine($"  Executable: {executablePath}");
        Console.WriteLine($"  Version output: {versionOutput}");
        
        return new ValidationResult(true, $"{product} {targetVersion} validation successful");
    }
    
    private static async Task<ValidationResult> ValidateAllVersionsAsync(string product, List<string> installedVersions)
    {
        if (installedVersions.Count == 0)
        {
            var message = $"No versions of {product} are installed.";
            Console.WriteLine($"❌ {message}");
            return new ValidationResult(false, message);
        }
        
        Console.WriteLine($"Found {installedVersions.Count} installed version(s) of {product}:");
        
        var allValid = true;
        var issues = new List<string>();
        
        foreach (var version in installedVersions)
        {
            var result = await ValidateSpecificVersionAsync(product, version, installedVersions);
            if (!result.IsValid)
            {
                allValid = false;
                issues.Add($"Version {version}: {result.Message}");
            }
        }
        
        if (allValid)
        {
            Console.WriteLine($"✓ All {installedVersions.Count} versions of {product} are valid");
            return new ValidationResult(true, $"All versions validated successfully");
        }
        else
        {
            Console.WriteLine($"❌ Some versions have issues:");
            foreach (var issue in issues)
            {
                Console.WriteLine($"  - {issue}");
            }
            return new ValidationResult(false, $"{issues.Count} validation issues found");
        }
    }
    
    private static string GetExecutablePath(string product, string version)
    {
        var versionPath = PathManager.GetProductVersionPath(product, version);
        
        return product.ToLowerInvariant() switch
        {
            "flyway" => Path.Combine(versionPath, OperatingSystem.IsWindows() ? "flyway.cmd" : "flyway"),
            "rgsubset" => Path.Combine(versionPath, OperatingSystem.IsWindows() ? "rgsubset.exe" : "rgsubset"),
            "rganonymize" => Path.Combine(versionPath, OperatingSystem.IsWindows() ? "rganonymize.exe" : "rganonymize"),
            _ => throw new ArgumentException($"Unknown executable path for product: {product}")
        };
    }
    
    private static async Task<string?> TestVersionCommandAsync(string product, string executablePath)
    {
        try
        {
            var arguments = product.Equals("flyway", StringComparison.OrdinalIgnoreCase) ? "version" : "--version";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) 
                ? EnvironmentManager.ParseVersionFromOutput(output.Trim()) 
                : null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult(bool IsValid, string Message);
