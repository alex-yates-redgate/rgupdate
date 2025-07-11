using System.CommandLine;
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

/// <summary>
/// Main program entry point and CLI setup
/// </summary>
class Program
{
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
            CreateInfoCommand(),
            CreateConfigCommand()
        };

        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate that the correct version is installed and runs properly");
        var productArgument = CommandHandlers.CreateProductArgument();
        var versionOption = CommandHandlers.CreateVersionOption();
        
        command.AddArgument(productArgument);
        command.AddOption(versionOption);

        command.SetHandler(async (string product, string? version) =>
        {
            var result = await ValidationService.ValidateProductAsync(product, version);
            Environment.Exit(result.IsValid ? 0 : 1);
        }, productArgument, versionOption);

        return command;
    }

    private static Command CreateGetCommand()
    {
        var command = new Command("get", "Download and install the specified product version");
        var productArgument = CommandHandlers.CreateProductArgument();
        var versionOption = CommandHandlers.CreateVersionOption();
        
        command.AddArgument(productArgument);
        command.AddOption(versionOption);

        command.SetHandler(async (string product, string? version) =>
        {
            try
            {
                await InstallationService.InstallProductAsync(product, version);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Installation failed: {ex.Message}");
                Environment.Exit(1);
            }
        }, productArgument, versionOption);

        return command;
    }

    private static Command CreateUseCommand()
    {
        var command = new Command("use", "Set a specific version as active and ensure it's in PATH");
        var productArgument = CommandHandlers.CreateProductArgument();
        var versionOption = CommandHandlers.CreateVersionOption();
        var localCopyOption = CommandHandlers.CreateLocalCopyOption();
        var localOnlyOption = CommandHandlers.CreateLocalOnlyOption();
        
        command.AddArgument(productArgument);
        command.AddOption(versionOption);
        command.AddOption(localCopyOption);
        command.AddOption(localOnlyOption);

        command.SetHandler(async (string product, string? version, bool localCopy, bool localOnly) =>
        {
            try
            {
                await ActivationService.SetActiveVersionAsync(product, version, localCopy, localOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Activation failed: {ex.Message}");
                Environment.Exit(1);
            }
        }, productArgument, versionOption, localCopyOption, localOnlyOption);

        return command;
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List available versions of a product");
        var productArgument = CommandHandlers.CreateProductArgument();
        var allOption = CommandHandlers.CreateAllOption();
        var outputOption = CommandHandlers.CreateOutputOption();
        
        command.AddArgument(productArgument);
        command.AddOption(allOption);
        command.AddOption(outputOption);

        command.SetHandler(async (string product, bool showAll, string? output) =>
        {
            try
            {
                await ListingService.ListVersionsAsync(product, showAll, output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Listing failed: {ex.Message}");
                Environment.Exit(1);
            }
        }, productArgument, allOption, outputOption);

        return command;
    }

    private static Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove a specific version or all versions of a product");
        var productArgument = CommandHandlers.CreateProductArgument();
        var versionOption = CommandHandlers.CreateVersionOption();
        var forceOption = CommandHandlers.CreateForceOption();
        
        command.AddArgument(productArgument);
        command.AddOption(versionOption);
        command.AddOption(forceOption);

        command.SetHandler(async (string product, string? version, bool force) =>
        {
            try
            {
                await RemovalService.RemoveVersionsAsync(product, version, force);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Removal failed: {ex.Message}");
                Environment.Exit(1);
            }
        }, productArgument, versionOption, forceOption);

        return command;
    }

    private static Command CreatePurgeCommand()
    {
        var command = new Command("purge", "Remove old versions keeping only the specified number of most recent versions");
        var productArgument = CommandHandlers.CreateProductArgument();
        var keepOption = CommandHandlers.CreateKeepOption();
        var forceOption = CommandHandlers.CreateForceOption();
        
        command.AddArgument(productArgument);
        command.AddOption(keepOption);
        command.AddOption(forceOption);

        command.SetHandler(async (string product, int keep, bool force) =>
        {
            try
            {
                await PurgeService.PurgeOldVersionsAsync(product, keep, force);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Purge failed: {ex.Message}");
                Environment.Exit(1);
            }
        }, productArgument, keepOption, forceOption);

        return command;
    }

    private static Command CreateConfigCommand()
    {
        var command = new Command("config", "Configure rgupdate settings");
        
        // Add set-location subcommand
        var setLocationCommand = new Command("set-location", "Set the installation location for rgupdate");
        var pathArgument = new Argument<string>("path", "The new installation path");
        var forceOption = CommandHandlers.CreateForceOption();
        
        setLocationCommand.AddArgument(pathArgument);
        setLocationCommand.AddOption(forceOption);
        
        setLocationCommand.SetHandler(async (string path, bool force) =>
        {
            try
            {
                await ConfigService.SetInstallLocationAsync(path, force);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration failed: {ex.Message}");
                Environment.Exit(1);
            }
        }, pathArgument, forceOption);
        
        command.AddCommand(setLocationCommand);
        
        return command;
    }

    private static Command CreateInfoCommand()
    {
        var command = new Command("info", "Show rgupdate configuration and environment information");

        command.SetHandler(async () =>
        {
            try
            {
                await InfoService.ShowSystemInfoAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Info display failed: {ex.Message}");
                Environment.Exit(1);
            }
        });

        return command;
    }
}
