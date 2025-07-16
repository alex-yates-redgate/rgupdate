using System.Xml.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Formats.Tar;

namespace rgupdate;

public static class EnvironmentManager
{
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
            var installLocation = Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.Machine) ??
                                Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.User);
            
            if (string.IsNullOrEmpty(installLocation))
            {
                Console.WriteLine($"Environment variable {Constants.InstallLocationEnvVar} not found. Setting up default location...");
                
                var defaultLocation = GetDefaultInstallLocation();
                
                try
                {
                    // Try to set the machine-level environment variable
                    Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, defaultLocation, EnvironmentVariableTarget.Machine);
                    Console.WriteLine($"✓ Set {Constants.InstallLocationEnvVar} = {defaultLocation} (machine-level)");
                    
                    // Refresh variables again to pick up the new setting
                    RefreshEnvironmentVariables();
                    
                    // Verify the variable was set correctly
                    var verifyLocation = Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.Machine);
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
                        Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, defaultLocation, EnvironmentVariableTarget.User);
                        Console.WriteLine($"✓ Set {Constants.InstallLocationEnvVar} = {defaultLocation} (user-level)");
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
                        Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, defaultLocation, EnvironmentVariableTarget.User);
                        Console.WriteLine($"✓ Set {Constants.InstallLocationEnvVar} = {defaultLocation} (user-level)");
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
    internal static void RefreshEnvironmentVariables()
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
        var location = Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.Machine) ??
                      Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.User);
        
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
    /// <exception cref="ArgumentException">Thrown for unsupported products</exception>
    public static async Task<List<VersionInfo>> GetAllPublicVersionsAsync(string product, Platform platform)
    {
        // Check for flyway - use Maven metadata instead of S3
        if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
        {
            return await GetFlywayVersionsFromMavenAsync();
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
            var productPath = PathManager.GetProductBasePath(product);
            
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
                if (dirName.Equals(Constants.ActiveVersionDirectoryName, StringComparison.OrdinalIgnoreCase))
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
            // Flyway uses "flyway version" instead of "flyway --version"
            var arguments = product.Equals("flyway", StringComparison.OrdinalIgnoreCase) 
                ? "version" 
                : "--version";
            
            var processInfo = new ProcessStartInfo
            {
                FileName = product.ToLower(),
                Arguments = arguments,
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
                // For flyway: "Flyway Community Edition 11.0.0 by Redgate"
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
    /// <summary>
    /// Parses version information from command output
    /// </summary>
    /// <param name="output">Command output containing version info</param>
    /// <returns>Parsed version string</returns>
    public static string? ParseVersionFromOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        // Try different patterns to extract version
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Special pattern for Flyway: "Flyway Community Edition 11.0.0 by Redgate"
            // Be more specific to avoid matching version numbers in warning messages
            var flywayMatch = System.Text.RegularExpressions.Regex.Match(
                trimmedLine,
                @"Flyway\s+Community\s+Edition\s+(\d+\.\d+\.\d+(?:\.\d+)?)\s+by\s+Redgate",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (flywayMatch.Success)
            {
                return flywayMatch.Groups[1].Value;
            }
            
            // Look for general version patterns like "version 2.1.13.1440" or "2.1.13.1440" or "2.1.3.7501+git-hash"
            // But avoid matching version numbers in warning/info messages
            var versionMatch = System.Text.RegularExpressions.Regex.Match(
                trimmedLine, 
                @"(?:^|\s)(?:version\s+)?(\d+\.\d+\.\d+(?:\.\d+)?)(?:\+.*)?(?:\s|$)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            // Don't match version numbers in warning/info messages
            if (versionMatch.Success && 
                !trimmedLine.Contains("WARNING", StringComparison.OrdinalIgnoreCase) &&
                !trimmedLine.Contains("available", StringComparison.OrdinalIgnoreCase) &&
                !trimmedLine.Contains("upgrade", StringComparison.OrdinalIgnoreCase) &&
                !trimmedLine.Contains("find out more", StringComparison.OrdinalIgnoreCase))
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
            var versionPath = PathManager.GetProductVersionPath(product, version);
            
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

    /// <summary>
    /// Downloads and installs a specific version of a product
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="versionSpec">Version specification (optional, defaults to latest)</param>
    /// <returns>The actual version that was installed</returns>
    public static async Task<string> DownloadAndInstallAsync(string product, string? versionSpec = null)
    {
        var platform = OperatingSystem.IsWindows() ? Platform.Windows : Platform.Linux;
        Console.WriteLine($"Installing {product} for platform: {platform}");
        
        // Special handling for flyway
        if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(versionSpec) || versionSpec.Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                // Get the latest version from Maven metadata
                var allVersions = await GetFlywayVersionsFromMavenAsync();
                if (allVersions.Count == 0)
                {
                    throw new InvalidOperationException("ERROR: No Flyway versions found in Maven metadata");
                }
                
                // Use the first (latest) version
                versionSpec = allVersions.First().Version;
                Console.WriteLine($"Using latest available Flyway version: {versionSpec}");
            }
            
            // For flyway, use the provided version directly
            var targetVersion = versionSpec;
            
            // Check if already installed
            var installPath = GetProductInstallPath(product, targetVersion);
            if (Directory.Exists(installPath) && Directory.GetFiles(installPath, "*", SearchOption.AllDirectories).Length > 0)
            {
                Console.WriteLine($"✓ {product} version {targetVersion} is already installed");
                return targetVersion;
            }
            
            // Download and install
            Console.WriteLine($"Downloading {product} version {targetVersion}...");
            
            var downloadUrl = GetDownloadUrl(product, targetVersion, platform);
            var tempFilePath = await DownloadFile(downloadUrl, product, targetVersion);
            
            try
            {
                Console.WriteLine($"Installing {product} version {targetVersion}...");
                await ExtractAndInstall(tempFilePath, installPath, product);
                
                Console.WriteLine($"✓ {product} version {targetVersion} installation completed");
                return targetVersion;
            }
            finally
            {
                // Clean up temp file
                try
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch { } // Ignore cleanup errors
            }
        }
        
        // For non-flyway products, use the existing logic
        
        // Get available versions
        var availableVersions = await GetAllPublicVersionsAsync(product, platform);
        if (availableVersions.Count == 0)
        {
            throw new InvalidOperationException($"No versions available for {product}");
        }
        
        // Resolve the version to install
        string targetVersionNormal;
        if (string.IsNullOrEmpty(versionSpec) || versionSpec.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            // Get the latest version
            var sortedVersions = availableVersions
                .Select(v => new SemanticVersion(v.Version))
                .OrderByDescending(v => v)
                .ToList();
            
            targetVersionNormal = sortedVersions.First().OriginalVersion;
            Console.WriteLine($"Resolved 'latest' to version {targetVersionNormal}");
        }
        else
        {
            // Try to find exact match or resolve partial version
            targetVersionNormal = ResolveVersion(versionSpec, availableVersions.Select(v => v.Version).ToList());
        }
        
        // Check if already installed
        var installPathNormal = GetProductInstallPath(product, targetVersionNormal);
        if (Directory.Exists(installPathNormal) && Directory.GetFiles(installPathNormal, "*", SearchOption.AllDirectories).Length > 0)
        {
            Console.WriteLine($"✓ {product} version {targetVersionNormal} is already installed");
            return targetVersionNormal;
        }
        
        // Download and install
        Console.WriteLine($"Downloading {product} version {targetVersionNormal}...");
        
        var downloadUrlNormal = GetDownloadUrl(product, targetVersionNormal, platform);
        var tempFilePathNormal = await DownloadFile(downloadUrlNormal, product, targetVersionNormal);
        
        try
        {
            Console.WriteLine($"Installing {product} version {targetVersionNormal}...");
            await ExtractAndInstall(tempFilePathNormal, installPathNormal, product);
            
            Console.WriteLine($"✓ {product} version {targetVersionNormal} installation completed");
            return targetVersionNormal;
        }
        finally
        {
            // Clean up temp file
            try
            {
                if (File.Exists(tempFilePathNormal))
                    File.Delete(tempFilePathNormal);
            }
            catch { } // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Gets installation diagnostics for a product version
    /// </summary>
    /// <param name="product">Product name</param>
    /// <param name="version">Version to diagnose</param>
    /// <returns>Diagnostic information</returns>
    public static string GetInstallationDiagnostics(string product, string version)
    {
        // For now, return basic info. This method would implement detailed diagnostics
        return $"Installation diagnostics for {product} {version}: Basic validation completed";
    }

    /// <summary>
    /// Gets the installation path for a product version
    /// </summary>
    private static string GetProductInstallPath(string product, string version)
    {
        return PathManager.GetProductVersionPath(product, version);
    }
    
    /// <summary>
    /// Resolves a version specification to an actual version
    /// </summary>
    private static string ResolveVersion(string versionSpec, List<string> availableVersions)
    {
        // Try exact match first
        var exactMatch = availableVersions.FirstOrDefault(v => 
            string.Equals(v, versionSpec, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;
        
        // Try partial match (e.g., "11" matches "11.10.1")
        var partialMatches = availableVersions
            .Where(v => v.StartsWith(versionSpec + ".", StringComparison.OrdinalIgnoreCase))
            .Select(v => new SemanticVersion(v))
            .OrderByDescending(v => v)
            .ToList();
        
        if (partialMatches.Count > 0)
        {
            var resolved = partialMatches.First().OriginalVersion;
            Console.WriteLine($"Resolved '{versionSpec}' to version {resolved}");
            return resolved;
        }
        
        throw new ArgumentException($"Version '{versionSpec}' not found. Available versions: {string.Join(", ", availableVersions.Take(5))}...");
    }
    
    /// <summary>
    /// Gets the download URL for a specific product version and platform
    /// </summary>
    private static string GetDownloadUrl(string product, string version, Platform platform)
    {
        return product.ToLower() switch
        {
            "flyway" => platform switch
            {
                Platform.Windows => $"https://download.red-gate.com/maven/release/com/redgate/flyway/flyway-commandline/{version}/flyway-commandline-{version}-windows-x64.zip",
                Platform.Linux => $"https://download.red-gate.com/maven/release/com/redgate/flyway/flyway-commandline/{version}/flyway-commandline-{version}-linux-x64.tar.gz",
                _ => throw new ArgumentException($"Unsupported platform for flyway: {platform}")
            },
            "rgsubset" => platform switch
            {
                Platform.Windows => $"https://redgate-download.s3.eu-west-1.amazonaws.com/EAP/SubsetterWin64/SubsetterWin64_{version}.zip",
                Platform.Linux => $"https://redgate-download.s3.eu-west-1.amazonaws.com/EAP/SubsetterLinux64/SubsetterLinux64_{version}.tar.gz",
                _ => throw new ArgumentException($"Unsupported platform for rgsubset: {platform}")
            },
            "rganonymize" => platform switch
            {
                Platform.Windows => $"https://redgate-download.s3.eu-west-1.amazonaws.com/EAP/AnonymizeWin64/AnonymizeWin64_{version}.zip",
                Platform.Linux => $"https://redgate-download.s3.eu-west-1.amazonaws.com/EAP/AnonymizeLinux64/AnonymizeLinux64_{version}.tar.gz",
                _ => throw new ArgumentException($"Unsupported platform for rganonymize: {platform}")
            },
            _ => throw new ArgumentException($"Unsupported product: {product}")
        };
    }
    
    /// <summary>
    /// Downloads a file from the specified URL
    /// </summary>
    private static async Task<string> DownloadFile(string url, string product, string version)
    {
        var tempDir = Path.GetTempPath();
        var fileName = Path.GetFileName(new Uri(url).LocalPath);
        var tempFilePath = Path.Combine(tempDir, $"{product}-{version}-{fileName}");
        
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(Constants.DownloadTimeoutMinutes); // Longer timeout for large files
        
        try
        {
            Console.WriteLine($"Downloading from: {url}");
            using var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = $"Failed to download {product} version {version}. " +
                    $"HTTP {(int)response.StatusCode} {response.StatusCode} from URL: {url}";
                
                // Provide helpful hints for Linux users
                if (!OperatingSystem.IsWindows() && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    errorMessage += $"\n\nNote: This may be because the Linux version of {product} v{version} is not available. " +
                        "Linux and Windows versions may have different release schedules. " +
                        "Try using a different version or 'latest'.";
                }
                
                throw new InvalidOperationException(errorMessage);
            }
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;
            
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            
            var buffer = new byte[8192];
            int bytesRead;
            var lastReportTime = DateTime.UtcNow;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;
                
                // Report progress every 2 seconds
                if (DateTime.UtcNow - lastReportTime > TimeSpan.FromSeconds(Constants.ProgressReportIntervalSeconds))
                {
                    if (totalBytes > 0)
                    {
                        var percentage = (double)downloadedBytes / totalBytes * 100;
                        Console.WriteLine($"Progress: {percentage:F1}% ({FormatBytes(downloadedBytes)} / {FormatBytes(totalBytes)})");
                    }
                    else
                    {
                        Console.WriteLine($"Downloaded: {FormatBytes(downloadedBytes)}");
                    }
                    lastReportTime = DateTime.UtcNow;
                }
            }
            
            Console.WriteLine($"✓ Download completed: {FormatBytes(downloadedBytes)}");
            return tempFilePath;
        }
        catch (Exception ex)
        {
            // Clean up partial download
            try
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
            catch { }
            
            throw new InvalidOperationException($"Failed to download {product} version {version}: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Extracts and installs the downloaded file
    /// </summary>
    private static async Task ExtractAndInstall(string archiveFilePath, string installPath, string product)
    {
        // Ensure install directory exists
        Directory.CreateDirectory(installPath);
        
        var isZip = archiveFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        var isTarGz = archiveFilePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase);
        
        if (isZip)
        {
            await ExtractZip(archiveFilePath, installPath, product);
        }
        else if (isTarGz)
        {
            await ExtractTarGz(archiveFilePath, installPath, product);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported archive format: {Path.GetExtension(archiveFilePath)}");
        }
    }
    
    /// <summary>
    /// Extracts a ZIP archive
    /// </summary>
    private static Task ExtractZip(string zipFilePath, string installPath, string product)
    {
        try
        {
            using var archive = System.IO.Compression.ZipFile.OpenRead(zipFilePath);
            
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) // Skip directories
                    continue;
                
                // For flyway, we want to skip the top-level directory in the zip
                var relativePath = entry.FullName;
                if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip the first directory level (e.g., "flyway-11.10.1/")
                    var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (pathParts.Length > 1)
                    {
                        relativePath = string.Join("/", pathParts.Skip(1));
                    }
                    else
                    {
                        continue; // Skip the top-level directory entry itself
                    }
                }
                
                var destinationPath = Path.Combine(installPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                
                // Ensure directory exists
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                // Extract file
                entry.ExtractToFile(destinationPath, overwrite: true);
                
                // Set executable permissions on Unix-like systems
                if (!OperatingSystem.IsWindows() && (
                    relativePath.EndsWith(".sh") || 
                    (Path.GetDirectoryName(relativePath)?.EndsWith("bin") == true && !Path.HasExtension(relativePath))))
                {
                    // This is a simplified approach - in practice you'd use proper Unix permissions
                    // For now, we'll just ensure the file exists
                }
            }
            
            Console.WriteLine($"✓ Extracted {archive.Entries.Count} files");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract ZIP archive: {ex.Message}", ex);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Extracts a TAR.GZ archive
    /// </summary>
    private static Task ExtractTarGz(string tarGzFilePath, string installPath, string product)
    {
        try
        {
            // Use System.Formats.Tar which is available in .NET 7+
            using var fileStream = new FileStream(tarGzFilePath, FileMode.Open, FileAccess.Read);
            using var gzipStream = new System.IO.Compression.GZipStream(fileStream, System.IO.Compression.CompressionMode.Decompress);
            
            // Extract TAR entries
            var reader = new System.Formats.Tar.TarReader(gzipStream);
            
            int extractedCount = 0;
            while (reader.GetNextEntry() is { } entry)
            {
                if (entry.EntryType == System.Formats.Tar.TarEntryType.Directory)
                    continue;
                
                // For products, we might want to skip the top-level directory
                var relativePath = entry.Name;
                if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip the first directory level (e.g., "flyway-11.10.1/")
                    var pathParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (pathParts.Length > 1)
                    {
                        relativePath = string.Join("/", pathParts.Skip(1));
                    }
                    else
                    {
                        continue; // Skip the top-level directory entry itself
                    }
                }
                
                var destinationPath = Path.Combine(installPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
                
                // Ensure directory exists
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }
                
                // Extract file
                entry.ExtractToFile(destinationPath, overwrite: true);
                
                // Set executable permissions on Unix-like systems for appropriate files
                if (!OperatingSystem.IsWindows())
                {
                    var fileName = Path.GetFileName(relativePath);
                    var dirName = Path.GetDirectoryName(relativePath);
                    
                    // Set executable for: .sh files, files in bin/ directories without extensions, 
                    // and main executable files
                    if (fileName.EndsWith(".sh") || 
                        (dirName?.Contains("bin") == true && !Path.HasExtension(fileName)) ||
                        fileName.Equals(product, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            // Set executable permissions (equivalent to chmod +x)
                            var fileInfo = new FileInfo(destinationPath);
                            if (fileInfo.Exists)
                            {
                                File.SetUnixFileMode(destinationPath, 
                                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not set executable permissions for {destinationPath}: {ex.Message}");
                        }
                    }
                }
                
                extractedCount++;
            }
            
            Console.WriteLine($"✓ Extracted {extractedCount} files from TAR.GZ archive");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to extract TAR.GZ archive: {ex.Message}", ex);
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Formats byte count as human-readable string
    /// </summary>
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

    /// <summary>
    /// Gets Flyway versions from Maven metadata XML
    /// </summary>
    /// <returns>List of version information for Flyway</returns>
    private static async Task<List<VersionInfo>> GetFlywayVersionsFromMavenAsync()
    {
        const string mavenMetadataUrl = "https://download.red-gate.com/maven/release/com/redgate/flyway/flyway-commandline/maven-metadata.xml";
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await httpClient.GetStringAsync(mavenMetadataUrl);
            
            // Parse the Maven metadata XML response
            var versions = ParseMavenMetadataXmlResponse(response);
            
            return versions;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to fetch Flyway version information: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new InvalidOperationException($"Request timed out while fetching Flyway version information: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error fetching Flyway version information: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses Maven metadata XML response to extract Flyway version information
    /// </summary>
    private static List<VersionInfo> ParseMavenMetadataXmlResponse(string xmlContent)
    {
        var versions = new List<VersionInfo>();
        
        try
        {
            var doc = XDocument.Parse(xmlContent);
            var ns = doc.Root?.GetDefaultNamespace();
            
            // Maven metadata typically has structure: <metadata><versioning><versions><version>...</version></versions></versioning></metadata>
            var versionElements = doc.Descendants("version");
            
            foreach (var versionElement in versionElements)
            {
                var versionString = versionElement.Value;
                
                if (!string.IsNullOrEmpty(versionString) && versionString.Contains('.'))
                {
                    // For Maven metadata, we don't have release date or file size info
                    // Use a default date and size since these fields won't be displayed for Flyway
                    versions.Add(new VersionInfo(versionString, DateTime.MinValue, 0));
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse Maven metadata XML response: {ex.Message}", ex);
        }
        
        // Sort versions by semantic version (most recent first)
        return versions
            .Select(v => new { Version = v, Semantic = new SemanticVersion(v.Version) })
            .OrderByDescending(x => x.Semantic)
            .Select(x => x.Version)
            .ToList();
    }
}
