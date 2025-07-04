namespace rgupdate;

/// <summary>
/// Application-wide constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// Supported Red Gate CLI products
    /// </summary>
    public static readonly string[] SupportedProducts = { "flyway", "rgsubset", "rganonymize" };
    
    /// <summary>
    /// Environment variable for custom install location
    /// </summary>
    public const string InstallLocationEnvVar = "RGUPDATE_INSTALL_LOCATION";
    
    /// <summary>
    /// Default number of versions to display in list command
    /// </summary>
    public const int DefaultVersionDisplayLimit = 10;
    
    /// <summary>
    /// Active version directory name
    /// </summary>
    public const string ActiveVersionDirectoryName = "active";
    
    /// <summary>
    /// Download timeout in minutes
    /// </summary>
    public const int DownloadTimeoutMinutes = 10;
    
    /// <summary>
    /// Progress reporting interval in seconds
    /// </summary>
    public const int ProgressReportIntervalSeconds = 2;
}
