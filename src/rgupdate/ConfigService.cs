namespace rgupdate;

/// <summary>
/// Handles configuration operations for rgupdate
/// </summary>
public static class ConfigService
{
    /// <summary>
    /// Sets the installation location for rgupdate
    /// </summary>
    /// <param name="newPath">The new installation path</param>
    /// <param name="force">Whether to force the change even if existing data would be lost</param>
    public static async Task SetInstallLocationAsync(string newPath, bool force = false)
    {
        Console.WriteLine($"Setting installation location to: {newPath}");
        
        // Validate the new path
        if (string.IsNullOrWhiteSpace(newPath))
        {
            throw new ArgumentException("Installation path cannot be empty");
        }
        
        // Expand environment variables and resolve the path
        var expandedPath = Environment.ExpandEnvironmentVariables(newPath);
        var fullPath = Path.GetFullPath(expandedPath);
        
        Console.WriteLine($"Resolved path: {fullPath}");
        
        // Get current installation location
        var currentLocation = GetCurrentInstallLocation();
        
        if (string.Equals(currentLocation, fullPath, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("✓ The specified path is already the current installation location");
            return;
        }
        
        // Check if current location has existing data
        var hasExistingData = currentLocation != null && Directory.Exists(currentLocation) && 
                             Directory.GetDirectories(currentLocation).Length > 0;
        
        if (hasExistingData && !force)
        {
            Console.WriteLine();
            Console.WriteLine("⚠ Warning: Existing installation data found!");
            Console.WriteLine($"Current location: {currentLocation}");
            Console.WriteLine("This change will not move existing installations to the new location.");
            Console.WriteLine("You may want to:");
            Console.WriteLine("1. Move existing installations manually, or");
            Console.WriteLine("2. Use --force to continue anyway");
            Console.WriteLine();
            throw new InvalidOperationException("Use --force to override this warning and continue");
        }
        
        // Create the new directory if it doesn't exist
        try
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                Console.WriteLine($"✓ Created directory: {fullPath}");
            }
            else
            {
                Console.WriteLine($"✓ Directory already exists: {fullPath}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create directory '{fullPath}': {ex.Message}", ex);
        }
        
        // Test write permissions
        try
        {
            var testFile = Path.Combine(fullPath, ".rgupdate-test");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);
            Console.WriteLine("✓ Write permissions verified");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot write to directory '{fullPath}': {ex.Message}", ex);
        }
        
        // Set the environment variable
        await SetEnvironmentVariableAsync(fullPath);
        
        // Show summary
        Console.WriteLine();
        Console.WriteLine("Configuration Summary:");
        Console.WriteLine("=====================");
        Console.WriteLine($"New installation location: {fullPath}");
        
        if (hasExistingData)
        {
            Console.WriteLine($"Previous location: {currentLocation}");
            Console.WriteLine("⚠ Note: Existing installations were not moved automatically");
            Console.WriteLine("You can move them manually if needed, or reinstall tools using 'rgupdate get'");
        }
        
        Console.WriteLine();
        Console.WriteLine("✓ Configuration updated successfully");
        Console.WriteLine("  New installations will use the new location");
        Console.WriteLine("  You may need to restart your terminal to pick up the change");
    }
    
    /// <summary>
    /// Gets the current installation location
    /// </summary>
    private static string? GetCurrentInstallLocation()
    {
        // Refresh environment variables first
        EnvironmentManager.RefreshEnvironmentVariables();
        
        return Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.Machine) ??
               Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.User);
    }
    
    /// <summary>
    /// Sets the environment variable for the installation location
    /// </summary>
    private static async Task SetEnvironmentVariableAsync(string path)
    {
        bool machineSuccess = false;
        bool userSuccess = false;
        
        try
        {
            // Try to set machine-level first
            try
            {
                Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, path, EnvironmentVariableTarget.Machine);
                Console.WriteLine($"✓ Set {Constants.InstallLocationEnvVar} = {path} (machine-level)");
                machineSuccess = true;
                
                // Also clear any user-level setting to avoid conflicts
                try
                {
                    var userValue = Environment.GetEnvironmentVariable(Constants.InstallLocationEnvVar, EnvironmentVariableTarget.User);
                    if (!string.IsNullOrEmpty(userValue))
                    {
                        Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, null, EnvironmentVariableTarget.User);
                        Console.WriteLine("✓ Cleared conflicting user-level environment variable");
                    }
                }
                catch
                {
                    // Ignore errors when clearing user-level variable
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("⚠ Warning: Insufficient permissions for machine-level environment variable");
                Console.WriteLine("  Setting user-level environment variable instead...");
                
                Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, path, EnvironmentVariableTarget.User);
                Console.WriteLine($"✓ Set {Constants.InstallLocationEnvVar} = {path} (user-level)");
                Console.WriteLine("  Note: Run as Administrator to set machine-level variable for all users");
                userSuccess = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Cannot set machine-level environment variable: {ex.Message}");
                Console.WriteLine("  Trying user-level environment variable...");
                
                try
                {
                    Environment.SetEnvironmentVariable(Constants.InstallLocationEnvVar, path, EnvironmentVariableTarget.User);
                    Console.WriteLine($"✓ Set {Constants.InstallLocationEnvVar} = {path} (user-level)");
                    userSuccess = true;
                }
                catch (Exception userEx)
                {
                    throw new InvalidOperationException($"Failed to set environment variable at both machine and user level. Machine error: {ex.Message}, User error: {userEx.Message}", userEx);
                }
            }
            
            if (!machineSuccess && !userSuccess)
            {
                throw new InvalidOperationException("Failed to set environment variable at any level");
            }
            
            // Refresh environment variables
            EnvironmentManager.RefreshEnvironmentVariables();
            
            // Verify the setting
            var currentValue = GetCurrentInstallLocation();
            if (string.Equals(currentValue, path, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("✓ Environment variable verified successfully");
            }
            else
            {
                Console.WriteLine("⚠ Warning: Could not verify environment variable was set correctly");
                Console.WriteLine($"  Expected: {path}");
                Console.WriteLine($"  Found: {currentValue ?? "null"}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set environment variable: {ex.Message}", ex);
        }
        
        await Task.CompletedTask;
    }
}
