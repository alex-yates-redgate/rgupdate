using System.CommandLine;
using System.Globalization;
using System.Diagnostics;

namespace rgupdate;

/// <summary>
/// Extension methods for System.Diagnostics.Process
/// </summary>
public static class ProcessExtensions
{
    /// <summary>
    /// Waits for the process to exit with a timeout
    /// </summary>
    public static async Task<bool> WaitForExitAsync(this Process process, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}

class Program
{
    private static readonly string[] SupportedProducts = { "flyway", "rgsubset", "rganonymize" };
    
    /// <summary>
    /// Maps products to their product families and CLI folder structures
    /// </summary>
    private static readonly Dictionary<string, ProductInfo> ProductMapping = new()
    {
        ["flyway"] = new ProductInfo("Flyway", "CLI"),
        ["rgsubset"] = new ProductInfo("Test Data Manager", "rgsubset"),
        ["rganonymize"] = new ProductInfo("Test Data Manager", "rganonymize")
    };
    
    /// <summary>
    /// Gets the installation path for a specific product and version
    /// </summary>
    public static string GetProductVersionPath(string product, string version)
    {
        var installLocation = EnvironmentManager.GetInstallLocation();
        var productInfo = ProductMapping[product.ToLower()];
        return Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder, version);
    }
    
    /// <summary>
    /// Gets the active installation path for a specific product
    /// </summary>
    public static string GetProductActivePath(string product)
    {
        var installLocation = EnvironmentManager.GetInstallLocation();
        var productInfo = ProductMapping[product.ToLower()];
        return Path.Combine(installLocation, productInfo.Family, productInfo.CliFolder, "active");
    }
    
    static async Task<int> Main(string[] args)
    {
        // Initialize environment variables on first execution
        await EnvironmentManager.InitializeInstallLocationAsync();
        
        var rootCommand = new RootCommand("rgupdate - Red Gate CLI tool version manager for Windows and Linux")
        {
            CreateValidateCommand(),
            CreateGetCommand(),
            CreateUseCommand(),
            CreateListCommand(),
            CreateRemoveCommand(),
            CreatePurgeCommand(),
            CreateInfoCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate that the correct version is installed and runs properly");
        
        var productArgument = new Argument<string>(
            name: "product",
            description: "The product to validate (flyway, rgsubset, rganonymize)"
        );
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!SupportedProducts.Contains(product?.ToLower()))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", SupportedProducts)}";
            }
        });
        command.AddArgument(productArgument);

        var versionOption = new Option<string>(
            name: "--version",
            description: "Specific version to validate (e.g., 1.2.3, 1, latest)"
        );
        command.AddOption(versionOption);

        command.SetHandler(async (string product, string? version) =>
        {
            try
            {
                Console.WriteLine($"Validating {product}...");
                
                if (!string.IsNullOrEmpty(version))
                {
                    // Validate specific version
                    Console.WriteLine($"Target version: {version}");
                    var isValid = await ValidateSpecificVersionAsync(product, version);
                    if (isValid)
                    {
                        Console.WriteLine($"✓ {product} version {version} validation passed");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {product} version {version} validation failed");
                    }
                }
                else
                {
                    // Validate all installed versions
                    await ValidateAllInstalledVersionsAsync(product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error validating {product}: {ex.Message}");
            }
        }, productArgument, versionOption);

        return command;
    }

    private static Command CreateGetCommand()
    {
        var command = new Command("get", "Download and install the specified product version");
        
        var productArgument = new Argument<string>(
            name: "product",
            description: "The product to get (flyway, rgsubset, rganonymize)"
        );
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!SupportedProducts.Contains(product?.ToLower()))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", SupportedProducts)}";
            }
        });
        command.AddArgument(productArgument);

        var versionOption = new Option<string>(
            name: "--version",
            description: "Specific version to get (e.g., 1.2.3, 1, latest)"
        );
        command.AddOption(versionOption);

        command.SetHandler(async (string product, string? version) =>
        {
            try
            {
                Console.WriteLine($"Getting {product}...");
                if (!string.IsNullOrEmpty(version))
                {
                    Console.WriteLine($"Target version: {version}");
                }
                
                // Download and install the specified version
                var installedVersion = await EnvironmentManager.DownloadAndInstallAsync(product, version);
                
                // Validate the installation
                Console.WriteLine("Validating installation...");
                var isValid = ValidateInstallation(product, installedVersion);
                
                if (isValid)
                {
                    Console.WriteLine($"✓ {product} version {installedVersion} installation completed and validated");
                }
                else
                {
                    Console.WriteLine($"⚠ {product} version {installedVersion} was installed but validation failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting {product}: {ex.Message}");
            }
        }, productArgument, versionOption);

        return command;
    }

    private static Command CreateUseCommand()
    {
        var command = new Command("use", "Set a specific version as active and ensure it's in PATH");
        
        var productArgument = new Argument<string>(
            name: "product",
            description: "The product to use (flyway, rgsubset, rganonymize)"
        );
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!SupportedProducts.Contains(product?.ToLower()))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", SupportedProducts)}";
            }
        });
        command.AddArgument(productArgument);

        var versionOption = new Option<string>(
            name: "--version",
            description: "Specific version to use (e.g., 1.2.3, 1, latest)"
        );
        command.AddOption(versionOption);

        command.SetHandler(async (string product, string? version) =>
        {
            try
            {
                Console.WriteLine($"Setting {product} as active...");
                
                // Resolve version if not specified
                string targetVersion;
                if (string.IsNullOrEmpty(version))
                {
                    // Get the most recent installed version
                    var installedVersions = EnvironmentManager.GetInstalledVersions(product);
                    if (installedVersions.Count == 0)
                    {
                        Console.WriteLine($"❌ No versions of {product} are installed. Use 'rgupdate get {product}' to install a version first.");
                        return;
                    }
                    
                    var sortedVersions = installedVersions
                        .Select(v => new EnvironmentManager.SemanticVersion(v))
                        .OrderByDescending(v => v)
                        .ToList();
                    
                    targetVersion = sortedVersions.First().OriginalVersion;
                    Console.WriteLine($"No version specified, using most recent installed version: {targetVersion}");
                }
                else
                {
                    targetVersion = version;
                }
                
                // Check if target version is installed
                var installPath = GetProductVersionPath(product, targetVersion);
                if (!Directory.Exists(installPath))
                {
                    Console.WriteLine($"❌ Version {targetVersion} is not installed. Installing now...");
                    targetVersion = await EnvironmentManager.DownloadAndInstallAsync(product, targetVersion);
                }
                
                // Validate the installation
                var isValid = ValidateInstallation(product, targetVersion);
                if (!isValid)
                {
                    Console.WriteLine($"❌ Version {targetVersion} failed validation");
                    return;
                }
                
                // Set up the active version
                await SetActiveVersionAsync(product, targetVersion);
                
                Console.WriteLine($"✓ {product} version {targetVersion} is now active and available in PATH");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error setting {product} as active: {ex.Message}");
            }
        }, productArgument, versionOption);

        return command;
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List available versions of a product");
        
        var productArgument = new Argument<string>(
            name: "product",
            description: "The product to list versions for (flyway, rgsubset, rganonymize)"
        );
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!SupportedProducts.Contains(product?.ToLower()))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", SupportedProducts)}";
            }
        });
        command.AddArgument(productArgument);

        var allOption = new Option<bool>(
            name: "--all",
            description: "Show all available versions instead of just the most recent 10"
        );
        command.AddOption(allOption);

        command.SetHandler(async (string product, bool showAll) =>
        {
            try
            {
                Console.WriteLine($"Listing versions for {product}...");
                Console.WriteLine();
                
                // Handle flyway specifically
                if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("ERROR: The --list command is not yet supported for flyway");
                    return;
                }
                
                // Detect current platform
                var platform = OperatingSystem.IsWindows() ? EnvironmentManager.Platform.Windows : EnvironmentManager.Platform.Linux;
                
                // Fetch public versions from S3
                List<EnvironmentManager.VersionInfo> publicVersions;
                try
                {
                    publicVersions = await EnvironmentManager.GetAllPublicVersionsAsync(product, platform);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching public versions: {ex.Message}");
                    return;
                }
                
                if (publicVersions.Count == 0)
                {
                    Console.WriteLine($"No public versions found for {product}");
                    return;
                }
                
                // Get installed versions
                var installedVersions = EnvironmentManager.GetInstalledVersions(product);
                var installedVersionsSet = new HashSet<string>(installedVersions, StringComparer.OrdinalIgnoreCase);
                
                // Get active version info with PATH validation
                var activeVersionInfo = await GetActiveVersionInfoAsync(product);
                var activeVersion = activeVersionInfo.ActiveVersion;
                
                // Get local-only versions (installed but not in public list)
                var localOnlyVersions = await EnvironmentManager.GetLocalVersionInfoAsync(product, publicVersions.Select(v => v.Version).ToList());
                
                // Combine all versions for sorting
                var allVersions = new List<EnvironmentManager.DisplayVersionInfo>();
                
                // Add public versions
                foreach (var version in publicVersions)
                {
                    allVersions.Add(new EnvironmentManager.DisplayVersionInfo(
                        version.Version, 
                        version.LastModified, 
                        version.Size, 
                        false
                    ));
                }
                
                // Add local-only versions
                foreach (var localVersion in localOnlyVersions)
                {
                    allVersions.Add(new EnvironmentManager.DisplayVersionInfo(
                        localVersion.Version,
                        localVersion.LastModified,
                        localVersion.Size,
                        true
                    ));
                }
                
                // Sort all versions by semantic version (descending)
                var sortedVersions = allVersions
                    .OrderByDescending(v => new EnvironmentManager.SemanticVersion(v.Version))
                    .ToList();
                
                // Find how many versions to show
                List<EnvironmentManager.DisplayVersionInfo> versionsToShow;
                int totalVersions = sortedVersions.Count;
                
                if (showAll)
                {
                    versionsToShow = sortedVersions;
                }
                else
                {
                    // Find the minimum number of versions to include all installed/active versions
                    var minVersionsToShow = 10; // Default top 10
                    
                    // Check if we need to extend beyond top 10 to include installed/active versions
                    for (int i = 0; i < sortedVersions.Count; i++)
                    {
                        var version = sortedVersions[i];
                        var isInstalledOrActive = installedVersionsSet.Contains(version.Version) ||
                                                string.Equals(version.Version, activeVersion, StringComparison.OrdinalIgnoreCase);
                        
                        if (isInstalledOrActive && i >= minVersionsToShow)
                        {
                            minVersionsToShow = i + 1;
                        }
                    }
                    
                    versionsToShow = sortedVersions.Take(minVersionsToShow).ToList();
                }
                
                // Display table header
                Console.WriteLine("Version         | Release Date  | Size      | Status");
                Console.WriteLine("----------------|---------------|-----------|--------");
                
                // Display each version
                foreach (var version in versionsToShow)
                {
                    var status = "-";
                    
                    if (!string.IsNullOrEmpty(activeVersion) && 
                        string.Equals(version.Version, activeVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        status = "ACTIVE";
                    }
                    else if (installedVersionsSet.Contains(version.Version))
                    {
                        status = "installed";
                    }
                    
                    // Special handling for local-only versions
                    if (version.IsLocalOnly)
                    {
                        if (status == "ACTIVE")
                        {
                            status = "ACTIVE (local only)";
                        }
                        else
                        {
                            status = "Local only (pulled from online)";
                        }
                    }
                    
                    var sizeFormatted = FormatFileSize(version.Size);
                    var dateFormatted = version.LastModified?.ToString("yyyy-MM-dd") ?? "UNKNOWN";
                    
                    // Override date for local-only versions with sentinel date
                    if (version.IsLocalOnly && version.LastModified?.Year <= 1900)
                    {
                        dateFormatted = "UNKNOWN";
                    }
                    
                    Console.WriteLine($"{version.Version,-15} | {dateFormatted,-13} | {sizeFormatted,-9} | {status}");
                }
                
                // Show summary if not all versions are displayed
                if (!showAll && totalVersions > versionsToShow.Count)
                {
                    var olderCount = totalVersions - versionsToShow.Count;
                    Console.WriteLine();
                    Console.WriteLine($"{olderCount} older versions (To see all versions, run: rgupdate list {product} --all)");
                }
                
                // Show active version validation information
                Console.WriteLine();
                DisplayActiveVersionValidation(product, activeVersionInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing versions: {ex.Message}");
            }
        }, productArgument, allOption);

        return command;
    }

    private static Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove a specific version or all versions of a product");
        
        var productArgument = new Argument<string>(
            name: "product",
            description: "The product to remove versions from (flyway, rgsubset, rganonymize)"
        );
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!SupportedProducts.Contains(product?.ToLower()))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", SupportedProducts)}";
            }
        });
        command.AddArgument(productArgument);

        var versionOption = new Option<string>(
            name: "--version",
            description: "Specific version to remove (e.g., 1.2.3, 1, latest)"
        );
        command.AddOption(versionOption);

        var allOption = new Option<bool>(
            name: "--all",
            description: "Remove all versions of the product"
        );
        command.AddOption(allOption);

        var forceOption = new Option<bool>(
            name: "--force",
            description: "Force removal even if it would delete the active version"
        );
        command.AddOption(forceOption);

        command.SetHandler(async (string product, string? version, bool removeAll, bool force) =>
        {
            try
            {
                Console.WriteLine($"Removing {product}...");
                
                // Validate parameters
                if (removeAll && !string.IsNullOrEmpty(version))
                {
                    Console.WriteLine("❌ Error: Cannot specify both --all and --version");
                    return;
                }
                
                if (!removeAll && string.IsNullOrEmpty(version))
                {
                    Console.WriteLine("❌ Error: Must specify either --version or --all");
                    return;
                }
                
                // Get installed versions and active version info
                var installedVersions = EnvironmentManager.GetInstalledVersions(product);
                var activeVersionInfo = await GetActiveVersionInfoAsync(product);
                var activeVersion = activeVersionInfo.ActiveVersion;
                
                if (installedVersions.Count == 0)
                {
                    Console.WriteLine($"❌ No versions of {product} are installed");
                    return;
                }
                
                List<string> versionsToRemove;
                
                if (removeAll)
                {
                    versionsToRemove = installedVersions;
                    Console.WriteLine($"Target: All versions ({installedVersions.Count} versions)");
                }
                else
                {
                    // Resolve the version to remove
                    var resolvedVersions = await ResolveVersionForRemoval(product, version!, installedVersions);
                    if (resolvedVersions == null || resolvedVersions.Count == 0)
                    {
                        return; // Error already displayed in ResolveVersionForRemoval
                    }
                    
                    versionsToRemove = resolvedVersions;
                    Console.WriteLine($"Target version: {string.Join(", ", versionsToRemove)}");
                }
                
                // Check if active version would be removed
                var willRemoveActive = !string.IsNullOrEmpty(activeVersion) && 
                                     versionsToRemove.Any(v => string.Equals(v, activeVersion, StringComparison.OrdinalIgnoreCase));
                
                if (willRemoveActive && !force)
                {
                    Console.WriteLine($"❌ Version {activeVersion} is the active version.");
                    if (removeAll)
                    {
                        Console.WriteLine($"   To remove all versions including the active version, use: rgupdate remove {product} --all --force");
                    }
                    else
                    {
                        Console.WriteLine($"   To remove the active version, use: rgupdate remove {product} --version {activeVersion} --force");
                    }
                    return;
                }
                
                // Display removal plan
                Console.WriteLine();
                Console.WriteLine("Versions to be removed:");
                foreach (var versionToRemove in versionsToRemove.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)))
                {
                    var status = string.Equals(versionToRemove, activeVersion, StringComparison.OrdinalIgnoreCase) ? " (ACTIVE)" : "";
                    Console.WriteLine($"  - {versionToRemove}{status}");
                }
                
                if (willRemoveActive)
                {
                    Console.WriteLine("⚠ Warning: This will also remove the active directory");
                }
                
                // Perform removal
                Console.WriteLine();
                await RemoveVersionsAsync(product, versionsToRemove, willRemoveActive);
                
                Console.WriteLine($"✓ Successfully removed {versionsToRemove.Count} version(s) of {product}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error removing {product}: {ex.Message}");
            }
        }, productArgument, versionOption, allOption, forceOption);

        return command;
    }

    private static Command CreatePurgeCommand()
    {
        var command = new Command("purge", "Remove old versions keeping only the specified number of most recent versions");
        
        var productArgument = new Argument<string>(
            name: "product",
            description: "The product to purge old versions from (flyway, rgsubset, rganonymize)"
        );
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!SupportedProducts.Contains(product?.ToLower()))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", SupportedProducts)}";
            }
        });
        command.AddArgument(productArgument);

        var keepOption = new Option<int>(
            name: "--keep",
            description: "Number of most recent versions to keep"
        ) { IsRequired = true };
        command.AddOption(keepOption);

        var forceOption = new Option<bool>(
            name: "--force",
            description: "Force purge even if it would delete the active version"
        );
        command.AddOption(forceOption);

        command.SetHandler(async (string product, int keep, bool force) =>
        {
            try
            {
                Console.WriteLine($"Purging old versions of {product}...");
                Console.WriteLine($"Keeping {keep} most recent versions");
                
                if (keep < 1)
                {
                    Console.WriteLine("❌ Error: --keep must be at least 1");
                    return;
                }
                
                // Get installed versions and active version info
                var installedVersions = EnvironmentManager.GetInstalledVersions(product);
                var activeVersionInfo = await GetActiveVersionInfoAsync(product);
                var activeVersion = activeVersionInfo.ActiveVersion;
                
                if (installedVersions.Count == 0)
                {
                    Console.WriteLine($"❌ No versions of {product} are installed");
                    return;
                }
                
                if (installedVersions.Count <= keep)
                {
                    Console.WriteLine($"✓ Currently have {installedVersions.Count} version(s), which is not more than --keep {keep}");
                    Console.WriteLine("Nothing to purge.");
                    return;
                }
                
                // Sort versions by semantic version (newest first)
                var sortedVersions = installedVersions
                    .Select(v => new EnvironmentManager.SemanticVersion(v))
                    .OrderByDescending(v => v)
                    .ToList();
                
                // Identify versions to keep and remove
                var versionsToKeep = sortedVersions.Take(keep).Select(v => v.OriginalVersion).ToList();
                var versionsToRemove = sortedVersions.Skip(keep).Select(v => v.OriginalVersion).ToList();
                
                Console.WriteLine();
                Console.WriteLine($"Analysis: {installedVersions.Count} total versions, keeping {keep} most recent");
                Console.WriteLine();
                
                Console.WriteLine("Versions to KEEP:");
                foreach (var version in versionsToKeep)
                {
                    var status = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase) ? " (ACTIVE)" : "";
                    Console.WriteLine($"  ✓ {version}{status}");
                }
                
                Console.WriteLine();
                Console.WriteLine($"Versions to REMOVE ({versionsToRemove.Count}):");
                foreach (var version in versionsToRemove)
                {
                    var status = string.Equals(version, activeVersion, StringComparison.OrdinalIgnoreCase) ? " (ACTIVE)" : "";
                    Console.WriteLine($"  ❌ {version}{status}");
                }
                
                // Check if active version would be removed
                var willRemoveActive = !string.IsNullOrEmpty(activeVersion) && 
                                     versionsToRemove.Any(v => string.Equals(v, activeVersion, StringComparison.OrdinalIgnoreCase));
                
                if (willRemoveActive && !force)
                {
                    Console.WriteLine();
                    Console.WriteLine($"❌ The active version {activeVersion} would be removed by this purge operation.");
                    Console.WriteLine($"   To proceed with purging including the active version, use: rgupdate purge {product} --keep {keep} --force");
                    Console.WriteLine("   Note: This will remove the active directory and you'll need to run 'rgupdate use' to set a new active version.");
                    return;
                }
                
                if (willRemoveActive)
                {
                    Console.WriteLine();
                    Console.WriteLine("⚠ Warning: This will remove the active version and the active directory");
                }
                
                // Perform removal
                Console.WriteLine();
                Console.WriteLine("Proceeding with purge...");
                await RemoveVersionsAsync(product, versionsToRemove, willRemoveActive);
                
                Console.WriteLine($"✓ Purge completed: removed {versionsToRemove.Count} version(s), kept {versionsToKeep.Count} most recent");
                
                if (willRemoveActive)
                {
                    Console.WriteLine("⚠ Note: No active version is set. Use 'rgupdate use {product}' to set an active version.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error purging {product}: {ex.Message}");
            }
        }, productArgument, keepOption, forceOption);

        return command;
    }

    private static Command CreateInfoCommand()
    {
        var command = new Command("info", "Show rgupdate configuration and environment information");

        command.SetHandler(() =>
        {
            Console.WriteLine("rgupdate Configuration Information");
            Console.WriteLine("=================================");
            Console.WriteLine();
            
            var installLocation = EnvironmentManager.GetInstallLocation();
            Console.WriteLine($"Install Location: {installLocation}");
            
            var machineLevel = Environment.GetEnvironmentVariable("RGUPDATE_INSTALL_LOCATION", EnvironmentVariableTarget.Machine);
            var userLevel = Environment.GetEnvironmentVariable("RGUPDATE_INSTALL_LOCATION", EnvironmentVariableTarget.User);
            
            Console.WriteLine($"Machine-level env var: {machineLevel ?? "(not set)"}");
            Console.WriteLine($"User-level env var: {userLevel ?? "(not set)"}");
            Console.WriteLine();
            
            Console.WriteLine("Supported Products:");
            foreach (var product in SupportedProducts)
            {
                Console.WriteLine($"  - {product}");
                Console.WriteLine($"    Version path: {GetProductVersionPath(product, "<version>")}");
                Console.WriteLine($"    Active path:  {GetProductActivePath(product)}");
            }
            Console.WriteLine();
            
            Console.WriteLine("System Information:");
            Console.WriteLine($"  Operating System: {Environment.OSVersion}");
            Console.WriteLine($"  Platform: {Environment.OSVersion.Platform}");
            Console.WriteLine($"  .NET Version: {Environment.Version}");
            Console.WriteLine($"  Current Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"  User: {Environment.UserName}");
            Console.WriteLine($"  Machine: {Environment.MachineName}");
            Console.WriteLine($"  Current Culture: {CultureInfo.CurrentCulture.Name}");
            Console.WriteLine($"  Current UI Culture: {CultureInfo.CurrentUICulture.Name}");
        });

        return command;
    }
    
    /// <summary>
    /// Formats a file size in bytes to a human-readable string
    /// </summary>
    private static string FormatFileSize(long bytes)
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
        
        // Return size with appropriate precision
        return $"{size:0.#} {sizes[order]}";
    }
    
    /// <summary>
    /// Validates that a specific version of a product is properly installed
    /// </summary>
    /// <param name="product">The product name</param>
    /// <param name="version">The version to validate</param>
    /// <returns>True if validation passed, false otherwise</returns>
    private static bool ValidateInstallation(string product, string version)
    {
        try
        {
            var installPath = GetProductVersionPath(product, version);
            
            Console.WriteLine($"Validating installation at: {installPath}");
            
            // Check if the installation directory exists and has files
            if (!Directory.Exists(installPath))
            {
                Console.WriteLine($"❌ Installation directory not found: {installPath}");
                return false;
            }
            
            var files = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.WriteLine($"❌ Installation directory is empty: {installPath}");
                return false;
            }
            
            // Show detailed diagnostics
            var diagnostics = EnvironmentManager.GetInstallationDiagnostics(product, version);
            Console.WriteLine("Installation Details:");
            Console.WriteLine(diagnostics);
            
            // Look for executable files
            var executableExtensions = OperatingSystem.IsWindows() 
                ? new[] { ".exe", ".bat", ".cmd" }
                : new[] { "", ".sh" }; // On Linux, executables may have no extension
            
            var executables = files.Where(f => 
                executableExtensions.Any(ext => 
                    Path.GetFileName(f).EndsWith(ext, StringComparison.OrdinalIgnoreCase) ||
                    (ext == "" && !Path.HasExtension(f) && IsExecutableFile(f))
                )).ToList();
            
            if (executables.Count == 0)
            {
                Console.WriteLine($"⚠ No executable files found in installation directory");
                Console.WriteLine($"  This may be normal depending on the tool's structure");
                // This is a warning, not a failure - some tools might have different structures
            }
            else
            {
                Console.WriteLine($"✓ Found {executables.Count} potential executable files");
            }
            
            Console.WriteLine($"✓ Installation validated: {files.Length} files found in {installPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Validation failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Checks if a file appears to be executable (Unix-like systems)
    /// </summary>
    private static bool IsExecutableFile(string filePath)
    {
        if (OperatingSystem.IsWindows()) return false;
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            // This is a simplified check - in practice you'd check file permissions
            // For now, we'll just check if it's not obviously a data file
            var fileName = Path.GetFileName(filePath).ToLower();
            var nonExecutableExtensions = new[] { ".txt", ".md", ".json", ".xml", ".conf", ".cfg", ".log" };
            
            return !nonExecutableExtensions.Any(ext => fileName.EndsWith(ext));
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Sets a specific version as the active version for a product
    /// </summary>
    /// <param name="product">The product name</param>
    /// <param name="version">The version to make active</param>
    private static async Task SetActiveVersionAsync(string product, string version)
    {
        var sourcePath = GetProductVersionPath(product, version);
        var activePath = GetProductActivePath(product);
        
        // Ensure the active directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(activePath)!);
        
        // Remove existing active installation if it exists
        if (Directory.Exists(activePath))
        {
            Console.WriteLine("Removing previous active version...");
            Directory.Delete(activePath, recursive: true);
        }
        
        // Copy the target version to the active directory
        Console.WriteLine($"Setting up active version from {sourcePath}...");
        await CopyDirectoryAsync(sourcePath, activePath);
        
        // Add to PATH if needed
        await EnsureInPathAsync(activePath);
        
        Console.WriteLine($"✓ Active version set to {version}");
    }
    
    /// <summary>
    /// Recursively copies a directory and its contents
    /// </summary>
    private static async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");
        }
        
        // Create destination directory
        Directory.CreateDirectory(destinationDir);
        
        // Copy all files
        var files = Directory.GetFiles(sourceDir);
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destinationDir, fileName);
            File.Copy(file, destFile, overwrite: true);
        }
        
        // Copy all subdirectories
        var subDirs = Directory.GetDirectories(sourceDir);
        foreach (var subDir in subDirs)
        {
            var dirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destinationDir, dirName);
            await CopyDirectoryAsync(subDir, destSubDir);
        }
    }
    
    /// <summary>
    /// Ensures that a directory is in the system PATH
    /// </summary>
    private static Task EnsureInPathAsync(string directoryPath)
    {
        try
        {
            // Check current process PATH first
            var currentProcessPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process) ?? "";
            var processPathDirectories = currentProcessPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            
            var isInProcessPath = processPathDirectories.Any(dir => 
                string.Equals(dir.TrimEnd('\\', '/'), directoryPath.TrimEnd('\\', '/'), 
                StringComparison.OrdinalIgnoreCase));
            
            // Update current process PATH if needed
            if (!isInProcessPath)
            {
                var newProcessPath = $"{directoryPath}{Path.PathSeparator}{currentProcessPath}";
                Environment.SetEnvironmentVariable("PATH", newProcessPath, EnvironmentVariableTarget.Process);
                Console.WriteLine($"✓ Added to current session PATH: {directoryPath}");
            }
            
            // Check if already in system or user PATH
            var systemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            
            var systemPathDirectories = systemPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var userPathDirectories = userPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            
            var isInSystemPath = systemPathDirectories.Any(dir => 
                string.Equals(dir.TrimEnd('\\', '/'), directoryPath.TrimEnd('\\', '/'), 
                StringComparison.OrdinalIgnoreCase));
                
            var isInUserPath = userPathDirectories.Any(dir => 
                string.Equals(dir.TrimEnd('\\', '/'), directoryPath.TrimEnd('\\', '/'), 
                StringComparison.OrdinalIgnoreCase));
            
            if (isInSystemPath)
            {
                Console.WriteLine($"✓ Directory already in SYSTEM PATH: {directoryPath}");
                return Task.CompletedTask;
            }
            
            if (isInUserPath)
            {
                Console.WriteLine($"✓ Directory already in user PATH: {directoryPath}");
                return Task.CompletedTask;
            }
            
            // Try to add to SYSTEM PATH first
            bool systemPathUpdated = false;
            try
            {
                var newSystemPath = $"{directoryPath}{Path.PathSeparator}{systemPath}";
                Environment.SetEnvironmentVariable("PATH", newSystemPath, EnvironmentVariableTarget.Machine);
                Console.WriteLine($"✓ Added to SYSTEM PATH: {directoryPath}");
                Console.WriteLine("  This will be available to all users and new terminal sessions.");
                systemPathUpdated = true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"⚠ Unable to update SYSTEM PATH: insufficient privileges");
                Console.WriteLine($"  Falling back to user PATH...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Unable to update SYSTEM PATH: {ex.Message}");
                Console.WriteLine($"  Falling back to user PATH...");
            }
            
            // If system PATH update failed, try user PATH
            if (!systemPathUpdated)
            {
                try
                {
                    var newUserPath = $"{directoryPath}{Path.PathSeparator}{userPath}";
                    Environment.SetEnvironmentVariable("PATH", newUserPath, EnvironmentVariableTarget.User);
                    Console.WriteLine($"✓ Added to user PATH: {directoryPath}");
                    Console.WriteLine("  💡 TIP: Run rgupdate as Administrator to update SYSTEM PATH for all users");
                    Console.WriteLine("  Note: New terminal sessions will use the updated PATH automatically.");
                    Console.WriteLine();
                    Console.WriteLine("To use the tool in this session immediately, run:");
                    if (OperatingSystem.IsWindows())
                    {
                        Console.WriteLine("  PowerShell:");
                        Console.WriteLine($"    $env:PATH = \"{directoryPath};\" + $env:PATH");
                        Console.WriteLine("  Command Prompt:");
                        Console.WriteLine($"    set PATH={directoryPath};%PATH%");
                    }
                    else
                    {
                        Console.WriteLine("  Bash/Zsh:");
                        Console.WriteLine($"    export PATH=\"{directoryPath}:$PATH\"");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Could not update PATH environment variable: {ex.Message}");
                    Console.WriteLine($"  Please manually add this directory to your PATH: {directoryPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Error managing PATH: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Validates all installed versions of a product by running their --version command
    /// </summary>
    /// <param name="product">The product name</param>
    private static async Task ValidateAllInstalledVersionsAsync(string product)
    {
        Console.WriteLine($"Validating all installed versions of {product}...");
        Console.WriteLine();
        
        var installedVersions = EnvironmentManager.GetInstalledVersions(product);
        
        if (installedVersions.Count == 0)
        {
            Console.WriteLine($"❌ No installed versions found for {product}");
            Console.WriteLine($"   Use 'rgupdate get {product}' to install a version");
            return;
        }
        
        Console.WriteLine($"Found {installedVersions.Count} installed version(s):");
        Console.WriteLine("Version         | Status    | Validation Result");
        Console.WriteLine("----------------|-----------|------------------");
        
        var validationResults = new List<(string Version, bool IsValid, string Message)>();
        
        foreach (var version in installedVersions.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)))
        {
            try
            {
                var result = await ValidateVersionExecutableAsync(product, version);
                validationResults.Add((version, result.IsValid, result.Message));
                
                var statusIcon = result.IsValid ? "✓" : "❌";
                var status = result.IsValid ? "PASS" : "FAIL";
                Console.WriteLine($"{version,-15} | {status,-9} | {result.Message}");
            }
            catch (Exception ex)
            {
                validationResults.Add((version, false, $"Error: {ex.Message}"));
                Console.WriteLine($"{version,-15} | {"FAIL",-9} | Error: {ex.Message}");
            }
        }
        
        Console.WriteLine();
        
        var passedCount = validationResults.Count(r => r.IsValid);
        var failedCount = validationResults.Count(r => !r.IsValid);
        
        if (failedCount == 0)
        {
            Console.WriteLine($"✓ All {passedCount} version(s) passed validation");
        }
        else
        {
            Console.WriteLine($"❌ Validation failed: {passedCount} passed, {failedCount} failed");
            
            Console.WriteLine("\nFailed versions:");
            foreach (var (version, isValid, message) in validationResults.Where(r => !r.IsValid))
            {
                Console.WriteLine($"  - {version}: {message}");
            }
        }
    }
    
    /// <summary>
    /// Validates a specific version by running its executable and checking version output
    /// </summary>
    /// <param name="product">The product name</param>
    /// <param name="version">The version to validate</param>
    /// <returns>Validation result with details</returns>
    private static async Task<(bool IsValid, string Message)> ValidateVersionExecutableAsync(string product, string version)
    {
        var installPath = GetProductVersionPath(product, version);
        
        // First check if directory and files exist
        if (!Directory.Exists(installPath))
        {
            return (false, "Installation directory not found");
        }
        
        // Find the executable - different for flyway
        string executablePath;
        string arguments;
        
        if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
        {
            // For flyway, the executable is directly in the installation directory
            var flywayExeNames = new[] { "flyway.cmd", "flyway.exe", "flyway" };
            executablePath = null;
            
            foreach (var exeName in flywayExeNames)
            {
                var tryPath = Path.Combine(installPath, exeName);
                if (File.Exists(tryPath))
                {
                    executablePath = tryPath;
                    break;
                }
            }
            
            if (executablePath == null)
            {
                return (false, "Flyway executable not found");
            }
            
            arguments = "version"; // Flyway uses "version" not "--version"
        }
        else
        {
            // For other products (rgsubset, rganonymize)
            var executableName = $"{product}.exe";
            executablePath = Path.Combine(installPath, executableName);
            
            if (!File.Exists(executablePath))
            {
                // Try to find any .exe file in the directory
                var exeFiles = Directory.GetFiles(installPath, "*.exe");
                if (exeFiles.Length == 0)
                {
                    return (false, "No executable files found");
                }
                executablePath = exeFiles.First();
            }
            
            arguments = "--version"; // Standard --version argument
        }
        
        try
        {
            // Run the executable with appropriate version argument
            var processInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = installPath
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            
            // Wait for process to complete with timeout
            var completed = await process.WaitForExitAsync(TimeSpan.FromSeconds(10));
            
            if (!completed)
            {
                try { process.Kill(); } catch { }
                return (false, "Executable timed out");
            }

            if (process.ExitCode != 0)
            {
                var errorMessage = !string.IsNullOrWhiteSpace(error) ? error.Trim() : "Unknown error";
                return (false, $"Executable failed (exit code {process.ExitCode}): {errorMessage}");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return (false, "No version output received");
            }

            // Parse version from output
            var reportedVersion = EnvironmentManager.ParseVersionFromOutput(output.Trim());
            
            if (string.IsNullOrEmpty(reportedVersion))
            {
                return (false, $"Could not parse version from output: {output.Trim()}");
            }

            // Compare reported version with expected version
            if (!string.Equals(reportedVersion, version, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Version mismatch: expected {version}, got {reportedVersion}");
            }

            return (true, $"Version confirmed: {reportedVersion}");
        }
        catch (Exception ex)
        {
            return (false, $"Execution error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates a specific version (wrapper for backward compatibility)
    /// </summary>
    /// <param name="product">The product name</param>
    /// <param name="version">The version to validate</param>
    /// <returns>True if validation passed, false otherwise</returns>
    private static async Task<bool> ValidateSpecificVersionAsync(string product, string version)
    {
        var result = await ValidateVersionExecutableAsync(product, version);
        
        if (result.IsValid)
        {
            Console.WriteLine($"✓ {result.Message}");
        }
        else
        {
            Console.WriteLine($"❌ {result.Message}");
        }
        
        return result.IsValid;
    }
    
    /// <summary>
    /// Gets comprehensive active version information including PATH validation
    /// </summary>
    private static async Task<ActiveVersionInfo> GetActiveVersionInfoAsync(string product)
    {
        var info = new ActiveVersionInfo();
        var activePath = GetProductActivePath(product);
        
        // Handle different executable structures for different products
        string activeExecutablePath;
        string versionArgument;
        
        if (product.Equals("flyway", StringComparison.OrdinalIgnoreCase))
        {
            // For flyway, the executable is directly in the active directory
            var flywayExeNames = new[] { "flyway.cmd", "flyway.exe", "flyway" };
            activeExecutablePath = null;
            
            foreach (var exeName in flywayExeNames)
            {
                var tryPath = Path.Combine(activePath, exeName);
                if (File.Exists(tryPath))
                {
                    activeExecutablePath = tryPath;
                    break;
                }
            }
            
            versionArgument = "version";
        }
        else
        {
            // For other products (rgsubset, rganonymize)
            var executableName = product.ToLower() + (OperatingSystem.IsWindows() ? ".exe" : "");
            activeExecutablePath = Path.Combine(activePath, executableName);
            versionArgument = "--version";
        }
        
        // Check if active directory exists and has the executable
        if (Directory.Exists(activePath) && activeExecutablePath != null && File.Exists(activeExecutablePath))
        {
            info.ActiveDirectoryExists = true;
            
            // Get version from active directory
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = activeExecutablePath,
                    Arguments = versionArgument,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    info.ActiveDirectoryVersion = EnvironmentManager.ParseVersionFromOutput(output.Trim());
                }
            }
            catch (Exception ex)
            {
                info.ActiveDirectoryError = ex.Message;
            }
        }
        
        // Get version from PATH
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
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                info.PathVersion = EnvironmentManager.ParseVersionFromOutput(output.Trim());
                
                // Try to find the actual location of the executable
                try
                {
                    var whereCommand = OperatingSystem.IsWindows() ? "where" : "which";
                    var whereProcessInfo = new ProcessStartInfo
                    {
                        FileName = whereCommand,
                        Arguments = product.ToLower(),
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var whereProcess = new Process { StartInfo = whereProcessInfo };
                    whereProcess.Start();

                    var whereOutput = await whereProcess.StandardOutput.ReadToEndAsync();
                    await whereProcess.WaitForExitAsync();

                    if (whereProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(whereOutput))
                    {
                        info.PathExecutableLocation = whereOutput.Trim().Split('\n', '\r')[0].Trim();
                    }
                }
                catch
                {
                    // Ignore errors when trying to find executable location
                }
            }
        }
        catch (Exception ex)
        {
            info.PathError = ex.Message;
        }
        
        return info;
    }

    /// <summary>
    /// Information about the active version and PATH configuration
    /// </summary>
    private class ActiveVersionInfo
    {
        public bool ActiveDirectoryExists { get; set; }
        public string? ActiveDirectoryVersion { get; set; }
        public string? ActiveDirectoryError { get; set; }
        public string? PathVersion { get; set; }
        public string? PathError { get; set; }
        public string? PathExecutableLocation { get; set; }
        
        public bool IsConsistent => !string.IsNullOrEmpty(ActiveDirectoryVersion) && 
                                   !string.IsNullOrEmpty(PathVersion) && 
                                   ActiveDirectoryVersion.Equals(PathVersion, StringComparison.OrdinalIgnoreCase);
        
        public string? ActiveVersion => ActiveDirectoryVersion ?? PathVersion;
    }

    /// <summary>
    /// Displays active version validation information
    /// </summary>
    private static void DisplayActiveVersionValidation(string product, ActiveVersionInfo activeVersionInfo)
    {
        Console.WriteLine("Active Version Status:");
        Console.WriteLine("======================");
        
        if (!activeVersionInfo.ActiveDirectoryExists)
        {
            Console.WriteLine("❌ No active version configured (active directory does not exist)");
            Console.WriteLine($"   Use 'rgupdate use {product}' to set an active version");
            return;
        }
        
        if (!string.IsNullOrEmpty(activeVersionInfo.ActiveDirectoryError))
        {
            Console.WriteLine($"❌ Error checking active directory version: {activeVersionInfo.ActiveDirectoryError}");
        }
        else if (!string.IsNullOrEmpty(activeVersionInfo.ActiveDirectoryVersion))
        {
            Console.WriteLine($"✓ Active directory version: {activeVersionInfo.ActiveDirectoryVersion}");
        }
        else
        {
            Console.WriteLine("⚠ Active directory exists but could not determine version");
        }
        
        if (!string.IsNullOrEmpty(activeVersionInfo.PathError))
        {
            Console.WriteLine($"❌ Error checking PATH version: {activeVersionInfo.PathError}");
            Console.WriteLine("   The tool is not accessible from PATH - you may need to:");
            Console.WriteLine("   - Restart your terminal to pick up PATH changes");
            Console.WriteLine("   - Or run the PATH update command shown when you used 'rgupdate use'");
        }
        else if (!string.IsNullOrEmpty(activeVersionInfo.PathVersion))
        {
            Console.WriteLine($"✓ PATH version: {activeVersionInfo.PathVersion}");
            
            if (!string.IsNullOrEmpty(activeVersionInfo.PathExecutableLocation))
            {
                var activePath = GetProductActivePath(product);
                var expectedLocation = Path.Combine(activePath, product.ToLower() + (OperatingSystem.IsWindows() ? ".exe" : ""));
                
                if (!string.Equals(Path.GetFullPath(activeVersionInfo.PathExecutableLocation), 
                                 Path.GetFullPath(expectedLocation), 
                                 StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⚠ PATH executable location: {activeVersionInfo.PathExecutableLocation}");
                    Console.WriteLine($"   Expected location: {expectedLocation}");
                    Console.WriteLine("   The PATH is pointing to a different installation!");
                }
                else
                {
                    Console.WriteLine($"✓ PATH executable location: {activeVersionInfo.PathExecutableLocation}");
                }
            }
        }
        else
        {
            Console.WriteLine("❌ Tool not found in PATH");
            Console.WriteLine("   Run the PATH update command shown when you used 'rgupdate use'");
        }
        
        // Overall consistency check
        Console.WriteLine();
        if (activeVersionInfo.IsConsistent)
        {
            Console.WriteLine($"✅ Active configuration is consistent - version {activeVersionInfo.ActiveVersion} is properly configured");
        }
        else if (!string.IsNullOrEmpty(activeVersionInfo.ActiveDirectoryVersion) && 
                 !string.IsNullOrEmpty(activeVersionInfo.PathVersion))
        {
            Console.WriteLine($"❌ Version mismatch detected!");
            Console.WriteLine($"   Active directory has: {activeVersionInfo.ActiveDirectoryVersion}");
            Console.WriteLine($"   PATH is using: {activeVersionInfo.PathVersion}");
            Console.WriteLine($"   Run 'rgupdate use {product}' to fix this inconsistency");
        }
    }
    
    /// <summary>
    /// Resolves a version specification for removal, handling partial matches and "latest"
    /// </summary>
    private static async Task<List<string>?> ResolveVersionForRemoval(string product, string versionSpec, List<string> installedVersions)
    {
        if (string.Equals(versionSpec, "latest", StringComparison.OrdinalIgnoreCase))
        {
            // "latest" refers to the newest available version, not necessarily installed
            try
            {
                var platform = OperatingSystem.IsWindows() ? EnvironmentManager.Platform.Windows : EnvironmentManager.Platform.Linux;
                var publicVersions = await EnvironmentManager.GetAllPublicVersionsAsync(product, platform);
                
                if (publicVersions.Count == 0)
                {
                    Console.WriteLine($"❌ Could not determine latest version for {product}");
                    return null;
                }
                
                var latestVersion = publicVersions
                    .Select(v => new EnvironmentManager.SemanticVersion(v.Version))
                    .OrderByDescending(v => v)
                    .First()
                    .OriginalVersion;
                
                if (!installedVersions.Contains(latestVersion, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"❌ Latest version {latestVersion} is not installed");
                    return null;
                }
                
                return new List<string> { latestVersion };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error determining latest version: {ex.Message}");
                return null;
            }
        }
        
        // Check for exact match first
        var exactMatch = installedVersions.FirstOrDefault(v => 
            string.Equals(v, versionSpec, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
        {
            return new List<string> { exactMatch };
        }
        
        // Check for partial matches
        var partialMatches = installedVersions.Where(v => 
            v.StartsWith(versionSpec, StringComparison.OrdinalIgnoreCase)).ToList();
        
        if (partialMatches.Count == 0)
        {
            Console.WriteLine($"❌ Version {versionSpec} is not installed");
            return null;
        }
        
        if (partialMatches.Count == 1)
        {
            return partialMatches;
        }
        
        // Multiple matches - ask for more specific version
        Console.WriteLine($"❌ Version {versionSpec} is not specific enough. Multiple matches found:");
        foreach (var match in partialMatches.OrderByDescending(v => new EnvironmentManager.SemanticVersion(v)))
        {
            Console.WriteLine($"  - {match}");
        }
        Console.WriteLine("Please specify the intended version more precisely.");
        
        return null;
    }
    
    /// <summary>
    /// Removes the specified versions and optionally the active directory
    /// </summary>
    private static async Task RemoveVersionsAsync(string product, List<string> versionsToRemove, bool removeActiveDirectory)
    {
        foreach (var version in versionsToRemove)
        {
            var versionPath = GetProductVersionPath(product, version);
            
            if (Directory.Exists(versionPath))
            {
                try
                {
                    Console.WriteLine($"Removing {version}...");
                    Directory.Delete(versionPath, recursive: true);
                    Console.WriteLine($"✓ Removed version {version}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to remove version {version}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"⚠ Version {version} directory not found at {versionPath}");
            }
        }
        
        // Remove active directory if needed
        if (removeActiveDirectory)
        {
            var activePath = GetProductActivePath(product);
            if (Directory.Exists(activePath))
            {
                try
                {
                    Console.WriteLine("Removing active directory...");
                    Directory.Delete(activePath, recursive: true);
                    Console.WriteLine("✓ Removed active directory");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to remove active directory: {ex.Message}");
                }
            }
        }
        
        await Task.CompletedTask;
    }
}
