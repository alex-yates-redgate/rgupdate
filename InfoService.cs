using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace rgupdate;

/// <summary>
/// Handles system information display
/// </summary>
public static class InfoService
{
    /// <summary>
    /// Shows comprehensive system and rgupdate configuration information
    /// </summary>
    public static async Task ShowSystemInfoAsync()
    {
        Console.WriteLine("rgupdate Configuration and Environment Information");
        Console.WriteLine("=================================================");
        Console.WriteLine();
        
        await ShowApplicationInfoAsync();
        Console.WriteLine();
        
        await ShowSystemInfoCoreAsync();
        Console.WriteLine();
        
        await ShowEnvironmentInfoAsync();
        Console.WriteLine();
        
        await ShowProductStatusAsync();
    }
    
    private static async Task ShowApplicationInfoAsync()
    {
        Console.WriteLine("Application Information:");
        Console.WriteLine("------------------------");
        
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var location = AppContext.BaseDirectory;
        
        Console.WriteLine($"Version: {version}");
        Console.WriteLine($"Location: {location}");
        Console.WriteLine($"Build Configuration: {GetBuildConfiguration()}");
        
        await Task.CompletedTask;
    }
    
    private static async Task ShowSystemInfoCoreAsync()
    {
        Console.WriteLine("System Information:");
        Console.WriteLine("-------------------");
        
        Console.WriteLine($"Operating System: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($".NET Runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"Runtime Identifier: {RuntimeInformation.RuntimeIdentifier}");
        
        await Task.CompletedTask;
    }
    
    private static async Task ShowEnvironmentInfoAsync()
    {
        Console.WriteLine("Environment Configuration:");
        Console.WriteLine("--------------------------");
        
        var installLocation = EnvironmentManager.GetInstallLocation();
        Console.WriteLine($"Install Location: {installLocation}");
        
        var envVar = Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.Machine);
        if (!string.IsNullOrEmpty(envVar))
        {
            Console.WriteLine($"Environment Variable (Machine): {Constants.InstallLocationEnvVar} = {envVar}");
        }
        else
        {
            envVar = Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(envVar))
            {
                Console.WriteLine($"Environment Variable (User): {Constants.InstallLocationEnvVar} = {envVar}");
            }
            else
            {
                Console.WriteLine($"Environment Variable: {Constants.InstallLocationEnvVar} = (not set, using default)");
            }
        }
        
        Console.WriteLine($"Directory Exists: {Directory.Exists(installLocation)}");
        
        if (Directory.Exists(installLocation))
        {
            try
            {
                var size = GetDirectorySize(installLocation);
                Console.WriteLine($"Total Size: {FormatBytes(size)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Total Size: Could not calculate ({ex.Message})");
            }
        }
        
        await Task.CompletedTask;
    }
    
    private static async Task ShowProductStatusAsync()
    {
        Console.WriteLine("Product Status:");
        Console.WriteLine("---------------");
        
        foreach (var product in Constants.SupportedProducts)
        {
            Console.WriteLine($"{product}:");
            
            try
            {
                var installedVersions = EnvironmentManager.GetInstalledVersions(product);
                Console.WriteLine($"  Installed Versions: {installedVersions.Count}");
                
                if (installedVersions.Count > 0)
                {
                    var latest = installedVersions
                        .Select(v => new EnvironmentManager.SemanticVersion(v))
                        .OrderByDescending(v => v)
                        .First()
                        .OriginalVersion;
                    Console.WriteLine($"  Latest Installed: {latest}");
                    
                    var activeVersion = await EnvironmentManager.GetActiveVersionAsync(product);
                    Console.WriteLine($"  Active Version: {activeVersion ?? "none/not in PATH"}");
                    
                    var activeDir = PathManager.GetProductActivePath(product);
                    Console.WriteLine($"  Active Directory: {(Directory.Exists(activeDir) ? "exists" : "missing")}");
                }
                else
                {
                    Console.WriteLine("  Active Version: not installed");
                    Console.WriteLine("  Active Directory: n/a");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
            
            Console.WriteLine();
        }
    }
    
    private static string GetBuildConfiguration()
    {
        #if DEBUG
        return "Debug";
        #else
        return "Release";
        #endif
    }
    
    private static long GetDirectorySize(string directory)
    {
        if (!Directory.Exists(directory))
            return 0;
        
        return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Select(file => new FileInfo(file))
            .Sum(fileInfo => fileInfo.Length);
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
