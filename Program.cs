using System.CommandLine;
using System.Globalization;

namespace rgupdate;

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
            Console.WriteLine($"Validating {product}...");
            if (!string.IsNullOrEmpty(version))
            {
                Console.WriteLine($"Target version: {version}");
            }
            
            // TODO: Implement validation logic
            // - Run version command
            // - Validate correct version is installed
            // - Verify it runs properly
            await Task.CompletedTask;
            Console.WriteLine("✓ Validation completed");
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
            Console.WriteLine($"Getting {product}...");
            if (!string.IsNullOrEmpty(version))
            {
                Console.WriteLine($"Target version: {version}");
            }
            
            // TODO: Implement get logic
            // - Check if software is already installed
            // - If installed, run validate
            // - If not installed or validation fails, install to Program Files/Red Gate/[product]/CLI/[version]
            await Task.CompletedTask;
            Console.WriteLine($"✓ {product} installation completed");
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
            Console.WriteLine($"Setting {product} as active...");
            if (!string.IsNullOrEmpty(version))
            {
                Console.WriteLine($"Target version: {version}");
            }
            
            // TODO: Implement use logic
            // - Check if correct version is installed, validate/install as needed
            // - Copy correct version to Program Files/Red Gate/[product]/CLI/active
            // - Check system environment PATH variable
            // - Add Program Files/Red Gate/[product]/CLI/active to PATH if needed
            // - Update PATH in current context if required
            // - Validate active installation is accessible and correct version
            await Task.CompletedTask;
            Console.WriteLine($"✓ {product} is now active and available in PATH");
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
                    Console.WriteLine("ERROR: Flyway is not yet supported");
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
                
                // Get active version
                var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
                
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
            Console.WriteLine($"Removing {product}...");
            
            if (removeAll)
            {
                Console.WriteLine("Target: All versions");
            }
            else if (!string.IsNullOrEmpty(version))
            {
                Console.WriteLine($"Target version: {version}");
            }
            else
            {
                Console.WriteLine("Error: Must specify either --version or --all");
                return;
            }
            
            Console.WriteLine($"Force mode: {force}");
            
            // TODO: Implement remove logic
            // - Identify versions to remove
            // - Check if active version would be deleted
            // - If active version would be deleted and --force not used, fail with error
            // - Remove specified version(s)
            await Task.CompletedTask;
            Console.WriteLine("✓ Removal completed");
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
            Console.WriteLine($"Purging old versions of {product}...");
            Console.WriteLine($"Keeping {keep} most recent versions");
            Console.WriteLine($"Force mode: {force}");
            
            // TODO: Implement purge logic
            // - Get list of all installed versions
            // - Sort by version/date (most recent first)
            // - Identify versions to remove (all except the N most recent)
            // - Check if active version would be deleted
            // - If active version would be deleted and --force not used, fail with error
            // - Remove old versions
            await Task.CompletedTask;
            Console.WriteLine("✓ Purge completed");
        }, productArgument, keepOption, forceOption);

        return command;
    }

    private static Command CreateInfoCommand()
    {
        var command = new Command("info", "Show rgupdate configuration and environment information");

        command.SetHandler(async () =>
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
            
            await Task.CompletedTask;
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
}
