using System.Globalization;
using YamlDotNet.Serialization;

namespace rgupdate;

/// <summary>
/// Handles version listing operations
/// </summary>
public static class ListingService
{
    /// <summary>
    /// Model for structured output of version information
    /// </summary>
    public class VersionOutput
    {
        public string Version { get; set; } = string.Empty;
        public string? ReleaseDate { get; set; }
        public long SizeBytes { get; set; }
        public string SizeFormatted { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsLocalOnly { get; set; }
        public bool IsInstalled { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Model for structured output of complete listing information
    /// </summary>
    public class ListingOutput
    {
        public string Product { get; set; } = string.Empty;
        public string? ActiveVersion { get; set; }
        public int TotalVersions { get; set; }
        public int DisplayedVersions { get; set; }
        public bool ShowingAll { get; set; }
        public List<VersionOutput> Versions { get; set; } = new();
    }

    /// <summary>
    /// Lists available versions for a product
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="showAll">Whether to show all versions or just recent ones</param>
    /// <param name="outputFormat">Output format: null/empty for table, "json", or "yaml"</param>
    public static async Task ListVersionsAsync(string product, bool showAll = false, string? outputFormat = null)
    {
        // Don't show progress messages for structured output
        var isStructuredOutput = !string.IsNullOrEmpty(outputFormat);
        
        if (!isStructuredOutput)
        {
            Console.WriteLine($"Listing versions for {product}...");
        }
        
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
                // For structured output, we should still throw the exception
                // since the list command is not supported for this product
                if (isStructuredOutput)
                {
                    throw;
                }
                
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                await ListInstalledVersionsOnlyAsync(product, outputFormat);
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

            // Get active version
            var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
            
            // Handle structured output
            if (isStructuredOutput)
            {
                await OutputStructuredAsync(product, versionsToShow, sortedVersions.Count, showAll, installedVersionsSet, activeVersion, outputFormat!);
            }
            else
            {
                await DisplayVersionTableAsync(product, versionsToShow, installedVersionsSet);
                
                if (!showAll && sortedVersions.Count > Constants.DefaultVersionDisplayLimit)
                {
                    var hiddenCount = sortedVersions.Count - Constants.DefaultVersionDisplayLimit;
                    Console.WriteLine($"{hiddenCount} older versions (To see all versions, run: rgupdate list {product} --all)");
                }
                
                // Show active version info
                await DisplayActiveVersionInfoAsync(product);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to list versions for {product}: {ex.Message}", ex);
        }
    }
    
    private static async Task ListInstalledVersionsOnlyAsync(string product, string? outputFormat = null)
    {
        var installedVersions = EnvironmentManager.GetInstalledVersions(product);
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
        
        if (installedVersions.Count == 0)
        {
            if (string.IsNullOrEmpty(outputFormat))
            {
                Console.WriteLine($"No versions of {product} are currently installed.");
                Console.WriteLine($"Run 'rgupdate get {product} --version <version>' to install a specific version.");
            }
            else
            {
                // Empty structured output
                var emptyOutput = new ListingOutput
                {
                    Product = product,
                    ActiveVersion = null,
                    TotalVersions = 0,
                    DisplayedVersions = 0,
                    ShowingAll = true,
                    Versions = new List<VersionOutput>()
                };
                await OutputStructuredDataAsync(emptyOutput, outputFormat);
            }
            return;
        }

        var sortedVersions = installedVersions.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)).ToList();
        
        if (string.IsNullOrEmpty(outputFormat))
        {
            Console.WriteLine("Installed versions:");
            Console.WriteLine("Version         | Status");
            Console.WriteLine("----------------|--------");
            
            foreach (var version in sortedVersions)
            {
                var status = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "installed";
                Console.WriteLine($"{version,-15} | {status}");
            }
            
            await DisplayActiveVersionInfoAsync(product);
        }
        else
        {
            // Create structured output for installed-only versions
            var versions = sortedVersions.Select(version => new VersionOutput
            {
                Version = version,
                ReleaseDate = null, // Unknown for installed-only
                SizeBytes = 0, // Unknown for installed-only  
                SizeFormatted = "unknown",
                Status = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase) ? "ACTIVE" : "installed",
                IsLocalOnly = true,
                IsInstalled = true,
                IsActive = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase)
            }).ToList();

            var output = new ListingOutput
            {
                Product = product,
                ActiveVersion = activeVersion,
                TotalVersions = installedVersions.Count,
                DisplayedVersions = installedVersions.Count,
                ShowingAll = true,
                Versions = versions
            };
            
            await OutputStructuredDataAsync(output, outputFormat);
        }
    }
    
    private static async Task OutputStructuredAsync(string product, List<EnvironmentManager.DisplayVersionInfo> versionsToShow, 
        int totalVersions, bool showAll, HashSet<string> installedVersionsSet, string? activeVersion, string outputFormat)
    {
        var versions = versionsToShow.Select(version =>
        {
            var isInstalled = installedVersionsSet.Contains(version.Version);
            var isActive = string.Equals(version.Version, activeVersion, StringComparison.OrdinalIgnoreCase);
            
            var status = isActive ? "ACTIVE" : (isInstalled ? "installed" : "-");
            
            return new VersionOutput
            {
                Version = version.Version,
                ReleaseDate = version.IsLocalOnly ? null : version.LastModified?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                SizeBytes = version.Size,
                SizeFormatted = FormatBytes(version.Size),
                Status = status,
                IsLocalOnly = version.IsLocalOnly,
                IsInstalled = isInstalled,
                IsActive = isActive
            };
        }).ToList();

        var output = new ListingOutput
        {
            Product = product,
            ActiveVersion = activeVersion,
            TotalVersions = totalVersions,
            DisplayedVersions = versionsToShow.Count,
            ShowingAll = showAll,
            Versions = versions
        };
        
        await OutputStructuredDataAsync(output, outputFormat);
    }
    
    private static Task OutputStructuredDataAsync(ListingOutput output, string outputFormat)
    {
        switch (outputFormat.ToLowerInvariant())
        {
            case "json":
                // Simple manual JSON formatting to avoid trimming issues
                var jsonOutput = FormatAsJson(output);
                Console.WriteLine(jsonOutput);
                break;
                
            case "yaml":
                var yamlSerializer = new SerializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                    .Build();
                var yaml = yamlSerializer.Serialize(output);
                Console.WriteLine(yaml);
                break;
                
            default:
                throw new ArgumentException($"Unsupported output format: {outputFormat}. Supported formats: json, yaml");
        }
        
        return Task.CompletedTask;
    }
    
    private static string FormatAsJson(ListingOutput output)
    {
        var versions = string.Join(",\n    ", output.Versions.Select(v => 
            $@"{{
      ""version"": ""{EscapeJson(v.Version)}"",
      ""releaseDate"": {(v.ReleaseDate != null ? $@"""{v.ReleaseDate}""" : "null")},
      ""sizeBytes"": {v.SizeBytes},
      ""sizeFormatted"": ""{EscapeJson(v.SizeFormatted)}"",
      ""status"": ""{EscapeJson(v.Status)}"",
      ""isLocalOnly"": {v.IsLocalOnly.ToString().ToLower()},
      ""isInstalled"": {v.IsInstalled.ToString().ToLower()},
      ""isActive"": {v.IsActive.ToString().ToLower()}
    }}"));

        return $@"{{
  ""product"": ""{EscapeJson(output.Product)}"",
  ""activeVersion"": {(output.ActiveVersion != null ? $@"""{EscapeJson(output.ActiveVersion)}""" : "null")},
  ""totalVersions"": {output.TotalVersions},
  ""displayedVersions"": {output.DisplayedVersions},
  ""showingAll"": {output.ShowingAll.ToString().ToLower()},
  ""versions"": [
    {versions}
  ]
}}";
    }
    
    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
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
