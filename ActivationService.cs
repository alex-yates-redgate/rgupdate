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
    /// <param name="localCopy">Also create a local copy in current directory</param>
    /// <param name="localOnly">Create only local copy, skip PATH management</param>
    public static async Task SetActiveVersionAsync(string product, string? version = null, bool localCopy = false, bool localOnly = false)
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
        
        // Validate mutually exclusive options
        if (localCopy && localOnly)
        {
            throw new ArgumentException("Cannot specify both --local-copy and --local-only options");
        }
        
        // Handle local-only mode
        if (localOnly)
        {
            var versionPath = PathManager.GetProductVersionPath(product, targetVersion);
            var localPath = Directory.GetCurrentDirectory();
            await CreateLocalCopyAsync(product, versionPath, localPath);
            
            Console.WriteLine($"✓ {product} version {targetVersion} local copy created");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"• Run './{GetExecutableName(product)}' to use this version");
            Console.WriteLine($"• No PATH changes made - system version unchanged");
            
            // Detect environment context and provide tips
            var contextTips = DetectEnvironmentContext();
            if (!string.IsNullOrEmpty(contextTips))
            {
                Console.WriteLine();
                Console.WriteLine(contextTips);
            }
            
            return;
        }
        
        // Handle standard active installation setup (always for normal use, also for --local-copy)
        var pathWasUpdated = false;
        
        // Copy version to active installation directory
        var sourceDir = PathManager.GetProductVersionPath(product, targetVersion);
        var activeDir = PathManager.GetProductActivePath(product);
        
        Console.WriteLine($"Setting {product} version {targetVersion} as active installation...");
        
        // Remove existing active installation directory if it exists
        if (Directory.Exists(activeDir))
        {
            Directory.Delete(activeDir, recursive: true);
        }
        
        // Copy the version to active installation directory
        await CopyDirectoryAsync(sourceDir, activeDir);
        
        Console.WriteLine($"✓ {product} version {targetVersion} is now active");
        
        // Update PATH if needed and track if it was updated
        pathWasUpdated = await UpdatePathEnvironmentAsync(product);
        
        // Create local copy if requested
        if (localCopy)
        {
            var versionPath = PathManager.GetProductVersionPath(product, targetVersion);
            var localPath = Directory.GetCurrentDirectory();
            await CreateLocalCopyAsync(product, versionPath, localPath);
            Console.WriteLine($"✓ Local copy created: ./{GetExecutableName(product)}");
        }
        
        Console.WriteLine();
        
        // Show PATH update warning and usage guidance
        ShowUsageGuidance(product, pathWasUpdated, localCopy, localCopy ? Directory.GetCurrentDirectory() : null);
        
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
    
    private static async Task<bool> UpdatePathEnvironmentAsync(string product)
    {
        try
        {
            var activeDir = PathManager.GetProductActivePath(product);
            var binPath = product.Equals("flyway", StringComparison.OrdinalIgnoreCase) 
                ? activeDir 
                : activeDir; // For rgsubset/rganonymize, the executables are in the root of the active installation directory
            
            if (OperatingSystem.IsWindows())
            {
                return await UpdateWindowsPathAsync(binPath);
            }
            else
            {
                return await UpdateUnixPathAsync(binPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning: Could not update PATH environment variable: {ex.Message}");
            Console.WriteLine($"  You may need to manually add the active installation to your PATH or restart your terminal.");
            return false;
        }
    }
    
    private static async Task<bool> UpdateWindowsPathAsync(string pathToAdd)
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
                    await Task.CompletedTask;
                    return true;
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
                        await Task.CompletedTask;
                        return true;
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
        return false;
    }
    
    private static async Task<bool> UpdateUnixPathAsync(string pathToAdd)
    {
        // For Unix systems, we would typically update shell profile files
        // This is a simplified implementation
        Console.WriteLine($"ℹ On Unix systems, you may need to manually add to your PATH:");
        Console.WriteLine($"  export PATH=\"{pathToAdd}:$PATH\"");
        
        await Task.CompletedTask;
        return true; // Assume we provided guidance to update PATH
    }

    private static async Task CreateLocalCopyAsync(string product, string versionPath, string localPath)
    {
        try
        {
            // Ensure the local directory exists
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            // Get the executable name for the current platform
            var executableName = GetExecutableName(product);
            var sourceExe = Path.Combine(versionPath, executableName);
            var targetExe = Path.Combine(localPath, executableName);

            if (!File.Exists(sourceExe))
            {
                throw new FileNotFoundException($"Source executable not found: {sourceExe}");
            }

            // Copy the main executable
            File.Copy(sourceExe, targetExe, overwrite: true);

            // Copy any required dependencies (DLLs, etc.)
            var sourceFiles = Directory.GetFiles(versionPath, "*", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileName(f).Equals(executableName, StringComparison.OrdinalIgnoreCase));

            foreach (var sourceFile in sourceFiles)
            {
                var fileName = Path.GetFileName(sourceFile);
                var targetFile = Path.Combine(localPath, fileName);
                File.Copy(sourceFile, targetFile, overwrite: true);
            }

            // Copy subdirectories (for localization resources, etc.)
            var sourceDirs = Directory.GetDirectories(versionPath);
            foreach (var sourceDir in sourceDirs)
            {
                var dirName = Path.GetFileName(sourceDir);
                var targetDir = Path.Combine(localPath, dirName);
                
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, recursive: true);
                }
                
                CopyDirectory(sourceDir, targetDir);
            }

            Console.WriteLine($"✓ Created local copy at: {localPath}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create local copy: {ex.Message}", ex);
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, overwrite: true);
        }

        // Copy subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            CopyDirectory(subDir, targetSubDir);
        }
    }

    private static string GetExecutableName(string product)
    {
        return OperatingSystem.IsWindows() ? $"{product}.exe" : product;
    }

    private static void ShowUsageGuidance(string product, bool pathUpdated, bool localCopyCreated, string? localCopyPath)
    {
        Console.WriteLine();
        Console.WriteLine("📋 Next Steps:");

        if (pathUpdated)
        {
            Console.WriteLine("⚠️  PATH has been updated. To use the new version:");
            Console.WriteLine("   • Open a new terminal/command prompt, OR");
            Console.WriteLine("   • Restart your current terminal session");
            Console.WriteLine();
        }

        if (localCopyCreated && !string.IsNullOrEmpty(localCopyPath))
        {
            var executableName = GetExecutableName(product);
            var fullPath = Path.Combine(localCopyPath, executableName);
            
            Console.WriteLine("💡 Local copy created. You can now use:");
            Console.WriteLine($"   {fullPath} [command] [options]");
            Console.WriteLine();
            
            if (pathUpdated)
            {
                Console.WriteLine("   This local copy works immediately without PATH changes.");
            }
        }

        var envContext = DetectEnvironmentContext();
        if (!string.IsNullOrEmpty(envContext))
        {
            Console.WriteLine($"🔧 Environment: {envContext}");
        }
    }

    private static string DetectEnvironmentContext()
    {
        var context = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            context.Add("Windows");
            
            // Detect shell context
            var comSpec = Environment.GetEnvironmentVariable("COMSPEC");
            if (!string.IsNullOrEmpty(comSpec))
            {
                var shell = Path.GetFileNameWithoutExtension(comSpec);
                context.Add(shell);
            }

            // Check if running in elevated context
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    context.Add("Administrator");
                }
            }
            catch
            {
                // Ignore errors in privilege detection
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            context.Add("Linux");
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (!string.IsNullOrEmpty(shell))
            {
                context.Add(Path.GetFileName(shell));
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            context.Add("macOS");
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (!string.IsNullOrEmpty(shell))
            {
                context.Add(Path.GetFileName(shell));
            }
        }

        return string.Join(", ", context);
    }
}
