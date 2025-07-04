using System.Xml.Linq;
using System.Diagnostics;

namespace rgupdate;

public static class EnvironmentManager
{
    private const string InstallLocationEnvVar = "RGUPDATE_INSTALL_LOCATION";
    
    /// <summary>
    /// Initializes the RGUPDATE_INSTALL_LOCATION environment variable if it doesn't exist
    /// </summary>
    public static async Task InitializeInstallLocationAsync()
    {
        try
        {
            // Refresh environment variables to get the latest system state
            RefreshEnvironmentVariables();
            
            // Check both machine and user level environment variables
            var installLocation = Environment.GetEnvironmentVariable(InstallLocationEnvVar, EnvironmentVariableTarget.Machine) ??
                                Environment.GetEnvironmentVariable(InstallLocationEnvVar, EnvironmentVariableTarget.User);
            
            if (string.IsNullOrEmpty(installLocation))
            {
                Console.WriteLine($"Environment variable {InstallLocationEnvVar} not found. Setting up default location...");
                
                var defaultLocation = GetDefaultInstallLocation();
                
                try
                {
                    // Try to set the machine-level environment variable
                    Environment.SetEnvironmentVariable(InstallLocationEnvVar, defaultLocation, EnvironmentVariableTarget.Machine);
                    Console.WriteLine($"✓ Set {InstallLocationEnvVar} = {defaultLocation} (machine-level)");
                    
                    // Refresh variables again to pick up the new setting
                    RefreshEnvironmentVariables();
                    
                    // Verify the variable was set correctly
                    var verifyLocation = Environment.GetEnvironmentVariable(InstallLocationEnvVar, EnvironmentVariableTarget.Machine);
                    if (verifyLocation == defaultLocation)
                    {
                        Console.WriteLine("✓ Environment variable verified successfully");
                    }
                    else
                    {
                        Console.WriteLine("⚠ Warning: Could not verify environment variable was set correctly");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("⚠ Warning: Insufficient permissions to set machine-level environment variables.");
                    Console.WriteLine("  Falling back to user-level environment variable.");
                    
                    try
                    {
                        // Fallback to user-level environment variable
                        Environment.SetEnvironmentVariable(InstallLocationEnvVar, defaultLocation, EnvironmentVariableTarget.User);
                        Console.WriteLine($"✓ Set {InstallLocationEnvVar} = {defaultLocation} (user-level)");
                        Console.WriteLine("  Note: Run as Administrator to set machine-level variable for all users.");
                    }
                    catch (Exception userEx)
                    {
                        Console.WriteLine($"⚠ Warning: Could not set user-level environment variable: {userEx.Message}");
                        Console.WriteLine("  rgupdate will use default path for this session.");
                    }
                }
                catch (Exception machineEx)
                {
                    Console.WriteLine($"⚠ Warning: Could not set machine-level environment variable: {machineEx.Message}");
                    Console.WriteLine("  Trying user-level environment variable...");
                    
                    try
                    {
                        Environment.SetEnvironmentVariable(InstallLocationEnvVar, defaultLocation, EnvironmentVariableTarget.User);
                        Console.WriteLine($"✓ Set {InstallLocationEnvVar} = {defaultLocation} (user-level)");
                    }
                    catch (Exception userEx)
                    {
                        Console.WriteLine($"⚠ Warning: Could not set user-level environment variable: {userEx.Message}");
                        Console.WriteLine("  rgupdate will use default path for this session.");
                    }
                }
            }
            else
            {
                Console.WriteLine($"Using install location: {installLocation}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error initializing environment variables: {ex.Message}");
            Console.WriteLine("  rgupdate will continue with default installation path.");
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets the default installation location based on the operating system
    /// </summary>
    private static string GetDefaultInstallLocation()
    {
        if (OperatingSystem.IsWindows())
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(programFiles, "Red Gate");
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Use /opt for Linux/macOS as it's the standard location for optional software
            return "/opt/Red Gate";
        }
        else
        {
            // Fallback to user's home directory for other platforms
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDir, ".redgate");
        }
    }
    
    /// <summary>
    /// Refreshes the current process's environment variables from the system
    /// </summary>
    private static void RefreshEnvironmentVariables()
    {
        // This is a simplified approach - in practice, you might need platform-specific code
        // to fully refresh environment variables in the current process
        
        if (OperatingSystem.IsWindows())
        {
            // On Windows, we can refresh by reading from registry or using Win32 API
            // For now, we'll rely on the Environment.GetEnvironmentVariable calls
            // which read directly from the system
        }
        // On Linux/macOS, environment variables are typically inherited from parent process
        // and don't need explicit refresh for machine-level variables
    }
    
    /// <summary>
    /// Gets the current install location from environment variable
    /// </summary>
    public static string GetInstallLocation()
    {
        // Try machine-level first, then user-level
        var location = Environment.GetEnvironmentVariable(InstallLocationEnvVar, EnvironmentVariableTarget.Machine) ??
                      Environment.GetEnvironmentVariable(InstallLocationEnvVar, EnvironmentVariableTarget.User);
        
        if (string.IsNullOrEmpty(location))
        {
            // Fallback to default if somehow the variable is not set
            location = GetDefaultInstallLocation();
        }
        
        return location;
    }
    
    /// <summary>
    /// Platform enumeration for version queries
    /// </summary>
    public enum Platform
    {
        Windows,
        Linux
    }
    
    /// <summary>
    /// Version information for a product release
    /// </summary>
    public record VersionInfo(string Version, DateTime LastModified, long Size);
    
    /// <summary>
    /// Represents a semantic version for comparison purposes
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int Build { get; }
        public string OriginalVersion { get; }

        public SemanticVersion(string version)
        {
            OriginalVersion = version;
            var parts = version.Split('.');
            
            Major = parts.Length > 0 && int.TryParse(parts[0], out var major) ? major : 0;
            Minor = parts.Length > 1 && int.TryParse(parts[1], out var minor) ? minor : 0;
            Patch = parts.Length > 2 && int.TryParse(parts[2], out var patch) ? patch : 0;
            Build = parts.Length > 3 && int.TryParse(parts[3], out var build) ? build : 0;
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other == null) return 1;

            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            return Build.CompareTo(other.Build);
        }

        public override string ToString() => OriginalVersion;
    }

    /// <summary>
    /// Combined version information for display
    /// </summary>
    public record DisplayVersionInfo(string Version, DateTime? LastModified, long Size, bool IsLocalOnly);
    
    /// <summary>
    /// Gets all available public versions for a product and platform from Red Gate's S3 bucket
    /// </summary>
    /// <param name="product">The product name (flyway, rgsubset, rganonymize)</param>
    /// <param name="platform">The target platform (Windows, Linux)</param>
    /// <returns>List of version information</returns>
    /// <exception cref="NotSupportedException">Thrown when flyway is requested</exception>
    /// <exception cref="ArgumentException">Thrown for unsupported products</exception>
    public static async Task<List<VersionInfo>> GetAllPublicVersionsAsync(string product, Platform platform)
    {
        // Check for flyway - not yet supported
        if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("ERROR: Flyway is not yet supported");
        }
        
        // Get the appropriate S3 URL for the product and platform
        var url = GetS3UrlForProduct(product, platform);
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await httpClient.GetStringAsync(url);
            
            // Parse the XML response from S3
            var versions = ParseS3XmlResponse(response);
            
            return versions;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to fetch version information for {product}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException($"Request timed out while fetching version information for {product}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error fetching version information for {product}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Gets the S3 URL for a specific product and platform
    /// </summary>
    private static string GetS3UrlForProduct(string product, Platform platform)
    {
        var baseUrl = "https://redgate-download.s3.eu-west-1.amazonaws.com/?delimiter=/&prefix=EAP/";
        
        return product.ToLower() switch
        {
            "rganonymize" => platform switch
            {
                Platform.Windows => $"{baseUrl}AnonymizeWin64/",
                Platform.Linux => $"{baseUrl}AnonymizeLinux64/",
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            },
            "rgsubset" => platform switch
            {
                Platform.Windows => $"{baseUrl}SubsetterWin64/",
                Platform.Linux => $"{baseUrl}SubsetterLinux64/",
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            },
            _ => throw new ArgumentException($"Unsupported product: {product}")
        };
    }
    
    /// <summary>
    /// Parses the S3 XML response to extract version information
    /// </summary>
    private static List<VersionInfo> ParseS3XmlResponse(string xmlContent)
    {
        var versions = new List<VersionInfo>();
        
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var ns = doc.Root?.GetDefaultNamespace();
            
            if (ns == null)
            {
                throw new InvalidOperationException("Could not determine XML namespace");
            }
            
            var contents = doc.Descendants(ns + "Contents");
            
            foreach (var content in contents)
            {
                var key = content.Element(ns + "Key")?.Value;
                var lastModified = content.Element(ns + "LastModified")?.Value;
                var size = content.Element(ns + "Size")?.Value;
                
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(lastModified) || string.IsNullOrEmpty(size))
                    continue;
                
                // Extract version from the key (assuming it's in the path)
                // Example: EAP/AnonymizeWin64/1.2.3/file.zip -> version is 1.2.3
                // But actually the structure seems to be: EAP/SubsetterWin64/SubsetterWin64_2.1.15.1477.zip
                var pathParts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
                string? version = null;
                
                if (pathParts.Length >= 3)
                {
                    // Try the third part first (directory-based structure)
                    var potentialVersion = pathParts[2];
                    if (potentialVersion.Contains('.') && !potentialVersion.Contains("zip"))
                    {
                        version = potentialVersion;
                    }
                }
                
                // If no version found in directory structure, try to extract from filename
                if (version == null && pathParts.Length >= 2)
                {
                    var filename = pathParts[pathParts.Length - 1]; // Last part is the filename
                    
                    // Skip checksum files (.sha256)
                    if (filename.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                        continue;
                    
                    // Extract version from filename pattern like "SubsetterWin64_2.1.15.1477.zip"
                    var underscoreIndex = filename.IndexOf('_');
                    var dotZipIndex = filename.LastIndexOf(".zip", StringComparison.OrdinalIgnoreCase);
                    
                    if (underscoreIndex > 0 && dotZipIndex > underscoreIndex)
                    {
                        version = filename.Substring(underscoreIndex + 1, dotZipIndex - underscoreIndex - 1);
                    }
                }
                
                // Skip if we couldn't extract a valid version
                if (string.IsNullOrEmpty(version) || !version.Contains('.'))
                    continue;
                    
                if (DateTime.TryParse(lastModified, out var parsedDate) && 
                    long.TryParse(size, out var parsedSize))
                {
                    // Only add if we don't already have this version
                    if (!versions.Any(v => v.Version == version))
                    {
                        versions.Add(new VersionInfo(version, parsedDate, parsedSize));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse S3 XML response: {ex.Message}", ex);
        }
        
        // Sort versions by date (most recent first)
        return versions.OrderByDescending(v => v.LastModified).ToList();
    }
    
    /// <summary>
    /// Gets all installed versions for a product by scanning the filesystem
    /// </summary>
    /// <param name="product">The product name</param>
    /// <returns>List of installed version strings</returns>
    public static List<string> GetInstalledVersions(string product)
    {
        var installedVersions = new List<string>();
        
        try
        {
            var installLocation = GetInstallLocation();
            var productMapping = new Dictionary<string, (string Family, string CliFolder)>
            {
                ["flyway"] = ("Flyway", "CLI"),
                ["rgsubset"] = ("Test Data Manager", "rgsubset"),
                ["rganonymize"] = ("Test Data Manager", "rganonymize")
            };
            
            if (!productMapping.TryGetValue(product.ToLower(), out var productInfo))
            {
                return installedVersions; // Return empty list for unknown products
            }
            
            var productPath = Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder);
            
            if (!Directory.Exists(productPath))
            {
                return installedVersions; // Return empty list if product directory doesn't exist
            }
            
            // Get all subdirectories that look like version numbers
            var directories = Directory.GetDirectories(productPath);
            foreach (var directory in directories)
            {
                var dirName = Path.GetFileName(directory);
                
                // Skip the "active" directory - this is a special symlink/copy, not a version
                if (dirName.Equals("active", StringComparison.OrdinalIgnoreCase))
                    continue;
                
                // Check if the directory name looks like a version (contains dots)
                if (dirName.Contains('.') && IsValidVersionDirectory(directory))
                {
                    installedVersions.Add(dirName);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but return empty list rather than throwing
            Console.WriteLine($"Warning: Could not scan for installed versions of {product}: {ex.Message}");
        }
        
        return installedVersions;
    }
    
    /// <summary>
    /// Checks if a directory appears to be a valid version installation
    /// </summary>
    private static bool IsValidVersionDirectory(string directory)
    {
        try
        {
            // A valid version directory should contain some files (not be empty)
            return Directory.Exists(directory) && 
                   (Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length > 0);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets the active version of a product by running its --version command
    /// </summary>
    /// <param name="product">The product name</param>
    /// <returns>The active version string, or null if not found or command failed</returns>
    public static async Task<string?> GetActiveVersionAsync(string product)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = product.ToLower(),
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Parse version from output
                // Expected format might be like "rgsubset version 2.1.13.1440" or just "2.1.13.1440"
                return ParseVersionFromOutput(output.Trim());
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - this is expected if the tool isn't in PATH or installed
            Console.WriteLine($"Debug: Could not get active version for {product}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Parses version string from command output
    /// </summary>
    private static string? ParseVersionFromOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        // Try different patterns to extract version
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Look for version patterns like "version 2.1.13.1440" or "2.1.13.1440" or "2.1.3.7501+git-hash"
            var versionMatch = System.Text.RegularExpressions.Regex.Match(
                trimmedLine, 
                @"(?:version\s+)?(\d+\.\d+\.\d+(?:\.\d+)?)(?:\+.*)?", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (versionMatch.Success)
            {
                return versionMatch.Groups[1].Value;
            }
        }

        return null;
    }
    
    /// <summary>
    /// Gets version information for a locally installed version that may not be available online
    /// </summary>
    /// <param name="product">The product name</param>
    /// <param name="version">The version string</param>
    /// <returns>VersionInfo for the local version, or null if not found</returns>
    public static Task<VersionInfo?> GetLocalVersionInfoAsync(string product, string version)
    {
        try
        {
            var installLocation = GetInstallLocation();
            var productMapping = new Dictionary<string, (string Family, string CliFolder)>
            {
                ["flyway"] = ("Flyway", "CLI"),
                ["rgsubset"] = ("Test Data Manager", "rgsubset"),
                ["rganonymize"] = ("Test Data Manager", "rganonymize")
            };
            
            if (!productMapping.TryGetValue(product.ToLower(), out var productInfo))
            {
                return Task.FromResult<VersionInfo?>(null);
            }
            
            var versionPath = Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder, version);
            
            if (!Directory.Exists(versionPath))
            {
                return Task.FromResult<VersionInfo?>(null);
            }
            
            // Calculate total size of all files in the version directory
            long totalSize = 0;
            try
            {
                var files = Directory.GetFiles(versionPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                }
            }
            catch
            {
                // If we can't calculate size, use 0
                totalSize = 0;
            }
            
            // Use a sentinel date to indicate this is a local-only version
            var unknownDate = new DateTime(1900, 1, 1);
            
            return Task.FromResult<VersionInfo?>(new VersionInfo(version, unknownDate, totalSize));
        }
        catch
        {
            return Task.FromResult<VersionInfo?>(null);
        }
    }

    /// <summary>
    /// Gets all local-only versions (installed but not available online)
    /// </summary>
    /// <param name="product">The product name</param>
    /// <param name="onlineVersions">List of versions available online</param>
    /// <returns>List of local-only version information</returns>
    public static async Task<List<VersionInfo>> GetLocalVersionInfoAsync(string product, List<string> onlineVersions)
    {
        var localOnlyVersions = new List<VersionInfo>();
        
        try
        {
            var installedVersions = GetInstalledVersions(product);
            var onlineVersionsSet = new HashSet<string>(onlineVersions, StringComparer.OrdinalIgnoreCase);
            
            // Find versions that are installed locally but not available online
            var localOnlyVersionNames = installedVersions
                .Where(v => !onlineVersionsSet.Contains(v))
                .ToList();
            
            foreach (var version in localOnlyVersionNames)
            {
                var versionInfo = await GetLocalVersionInfoAsync(product, version);
                if (versionInfo != null)
                {
                    localOnlyVersions.Add(versionInfo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not scan for local-only versions of {product}: {ex.Message}");
        }
        
        return localOnlyVersions;
    }
}
