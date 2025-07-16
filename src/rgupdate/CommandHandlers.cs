using System.CommandLine;

namespace rgupdate;

/// <summary>
/// Handles CLI command creation and argument validation
/// </summary>
public static class CommandHandlers
{
    /// <summary>
    /// Creates a product argument with validation
    /// </summary>
    /// <returns>Configured product argument</returns>
    public static Argument<string> CreateProductArgument()
    {
        var productArgument = new Argument<string>(
            name: "product",
            description: $"The product to work with ({string.Join(", ", Constants.SupportedProducts)})"
        );
        
        productArgument.AddValidator(result =>
        {
            var product = result.GetValueForArgument(productArgument);
            if (!ProductConfiguration.IsProductSupported(product))
            {
                result.ErrorMessage = $"Unsupported product '{product}'. Supported products: {string.Join(", ", Constants.SupportedProducts)}";
            }
        });
        
        return productArgument;
    }
    
    /// <summary>
    /// Creates a version option
    /// </summary>
    /// <returns>Configured version option</returns>
    public static Option<string?> CreateVersionOption()
    {
        return new Option<string?>(
            name: "--version",
            description: "Specific version to target (e.g., 1.2.3, or 'latest')"
        );
    }
    
    /// <summary>
    /// Creates a force option
    /// </summary>
    /// <returns>Configured force option</returns>
    public static Option<bool> CreateForceOption()
    {
        return new Option<bool>(
            name: "--force",
            description: "Force the operation without confirmation prompts"
        );
    }
    
    /// <summary>
    /// Creates an all option
    /// </summary>
    /// <returns>Configured all option</returns>
    public static Option<bool> CreateAllOption()
    {
        return new Option<bool>(
            name: "--all",
            description: "Show all available versions instead of just the most recent"
        );
    }
    
    /// <summary>
    /// Creates a keep option for purge command
    /// </summary>
    /// <returns>Configured keep option</returns>
    public static Option<int> CreateKeepOption()
    {
        var option = new Option<int>(
            name: "--keep",
            description: "Number of most recent versions to keep (default: 3)"
        );
        option.SetDefaultValue(3);
        return option;
    }
    
    /// <summary>
    /// Creates an output format option for commands that support structured output
    /// </summary>
    /// <returns>Configured output format option</returns>
    public static Option<string?> CreateOutputOption()
    {
        return new Option<string?>(
            name: "--output",
            description: "Output format: 'json' or 'yaml' (default: table)"
        )
        {
            ArgumentHelpName = "format"
        };
    }
    
    /// <summary>
    /// Creates a local-copy option
    /// </summary>
    /// <returns>Configured local-copy option</returns>
    public static Option<bool> CreateLocalCopyOption()
    {
        return new Option<bool>(
            name: "--local-copy",
            description: "Also create a local copy in the current directory"
        );
    }
    
    /// <summary>
    /// Creates a local-only option
    /// </summary>
    /// <returns>Configured local-only option</returns>
    public static Option<bool> CreateLocalOnlyOption()
    {
        return new Option<bool>(
            name: "--local-only",
            description: "Create only a local copy, skip PATH management"
        );
    }
}
