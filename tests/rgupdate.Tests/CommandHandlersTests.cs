using Xunit;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace rgupdate.Tests;

public class CommandHandlersTests
{
    [Fact]
    public void CreateProductArgument_ReturnsConfiguredArgument()
    {
        // Act
        var argument = CommandHandlers.CreateProductArgument();
        
        // Assert
        Assert.NotNull(argument);
        Assert.Equal("product", argument.Name);
        Assert.Contains("rgsubset", argument.Description);
        Assert.Contains("rganonymize", argument.Description);
        Assert.Contains("flyway", argument.Description);
    }
    
    [Theory]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    [InlineData("flyway")]
    public void CreateProductArgument_WithSupportedProduct_PassesValidation(string product)
    {
        // Arrange
        var argument = CommandHandlers.CreateProductArgument();
        var command = new Command("test");
        command.AddArgument(argument);
        
        // Act
        var parseResult = command.Parse(new[] { product });
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(product, parseResult.GetValueForArgument(argument));
    }
    
    [Theory]
    [InlineData("unsupported")]
    [InlineData("invalid-product")]
    [InlineData("")]
    public void CreateProductArgument_WithUnsupportedProduct_FailsValidation(string product)
    {
        // Arrange
        var argument = CommandHandlers.CreateProductArgument();
        var command = new Command("test");
        command.AddArgument(argument);
        
        // Act
        var parseResult = command.Parse(new[] { product });
        
        // Assert
        Assert.NotEmpty(parseResult.Errors);
        var error = parseResult.Errors[0];
        Assert.Contains("Unsupported product", error.Message);
        Assert.Contains("rgsubset", error.Message);
        Assert.Contains("rganonymize", error.Message);
        Assert.Contains("flyway", error.Message);
    }
    
    [Fact]
    public void CreateVersionOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateVersionOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("version", option.Name);
        Assert.Contains("Specific version", option.Description);
        Assert.Contains("latest", option.Description);
    }
    
    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.1.3")]
    [InlineData("latest")]
    [InlineData(null)]
    public void CreateVersionOption_WithValidValues_AcceptsInput(string? version)
    {
        // Arrange
        var option = CommandHandlers.CreateVersionOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var args = version != null ? new[] { "--version", version } : Array.Empty<string>();
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(version, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void CreateForceOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateForceOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("force", option.Name);
        Assert.Contains("Force the operation", option.Description);
        Assert.Contains("confirmation", option.Description);
    }
    
    [Theory]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--force" }, true)]
    public void CreateForceOption_WithVariousInputs_ReturnsExpectedValue(string[] args, bool expected)
    {
        // Arrange
        var option = CommandHandlers.CreateForceOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(expected, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void CreateAllOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateAllOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("all", option.Name);
        Assert.Contains("Show all available versions", option.Description);
        Assert.Contains("most recent", option.Description);
    }
    
    [Theory]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--all" }, true)]
    public void CreateAllOption_WithVariousInputs_ReturnsExpectedValue(string[] args, bool expected)
    {
        // Arrange
        var option = CommandHandlers.CreateAllOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(expected, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void CreateKeepOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateKeepOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("keep", option.Name);
        Assert.Contains("Number of most recent versions", option.Description);
        Assert.Contains("default: 3", option.Description);
    }
    
    [Theory]
    [InlineData(new string[0], 3)] // Default value
    [InlineData(new[] { "--keep", "5" }, 5)]
    [InlineData(new[] { "--keep", "1" }, 1)]
    [InlineData(new[] { "--keep", "10" }, 10)]
    public void CreateKeepOption_WithVariousInputs_ReturnsExpectedValue(string[] args, int expected)
    {
        // Arrange
        var option = CommandHandlers.CreateKeepOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(expected, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void CreateOutputOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateOutputOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("output", option.Name);
        Assert.Contains("Output format", option.Description);
        Assert.Contains("json", option.Description);
        Assert.Contains("yaml", option.Description);
        Assert.Equal("format", option.ArgumentHelpName);
    }
    
    [Theory]
    [InlineData(new string[0], null)] // Default value
    [InlineData(new[] { "--output", "json" }, "json")]
    [InlineData(new[] { "--output", "yaml" }, "yaml")]
    [InlineData(new[] { "--output", "table" }, "table")]
    public void CreateOutputOption_WithVariousInputs_ReturnsExpectedValue(string[] args, string? expected)
    {
        // Arrange
        var option = CommandHandlers.CreateOutputOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(expected, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void CreateLocalCopyOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateLocalCopyOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("local-copy", option.Name);
        Assert.Contains("create a local copy", option.Description);
        Assert.Contains("current directory", option.Description);
    }
    
    [Theory]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--local-copy" }, true)]
    public void CreateLocalCopyOption_WithVariousInputs_ReturnsExpectedValue(string[] args, bool expected)
    {
        // Arrange
        var option = CommandHandlers.CreateLocalCopyOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(expected, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void CreateLocalOnlyOption_ReturnsConfiguredOption()
    {
        // Act
        var option = CommandHandlers.CreateLocalOnlyOption();
        
        // Assert
        Assert.NotNull(option);
        Assert.Equal("local-only", option.Name);
        Assert.Contains("only a local copy", option.Description);
        Assert.Contains("skip PATH management", option.Description);
    }
    
    [Theory]
    [InlineData(new string[0], false)]
    [InlineData(new[] { "--local-only" }, true)]
    public void CreateLocalOnlyOption_WithVariousInputs_ReturnsExpectedValue(string[] args, bool expected)
    {
        // Arrange
        var option = CommandHandlers.CreateLocalOnlyOption();
        var command = new Command("test");
        command.AddOption(option);
        
        // Act
        var parseResult = command.Parse(args);
        
        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(expected, parseResult.GetValueForOption(option));
    }
    
    [Fact]
    public void AllOptions_HaveUniqueNames()
    {
        // Arrange
        var options = new Option[]
        {
            CommandHandlers.CreateVersionOption(),
            CommandHandlers.CreateForceOption(),
            CommandHandlers.CreateAllOption(),
            CommandHandlers.CreateKeepOption(),
            CommandHandlers.CreateOutputOption(),
            CommandHandlers.CreateLocalCopyOption(),
            CommandHandlers.CreateLocalOnlyOption()
        };
        
        // Act
        var names = options.Select(o => o.Name).ToList();
        var uniqueNames = names.Distinct().ToList();
        
        // Assert
        Assert.Equal(names.Count, uniqueNames.Count);
    }
    
    [Fact]
    public void AllOptions_HaveDescriptions()
    {
        // Arrange
        var options = new Option[]
        {
            CommandHandlers.CreateVersionOption(),
            CommandHandlers.CreateForceOption(),
            CommandHandlers.CreateAllOption(),
            CommandHandlers.CreateKeepOption(),
            CommandHandlers.CreateOutputOption(),
            CommandHandlers.CreateLocalCopyOption(),
            CommandHandlers.CreateLocalOnlyOption()
        };
        
        // Act & Assert
        foreach (var option in options)
        {
            Assert.NotNull(option.Description);
            Assert.NotEmpty(option.Description);
        }
    }
}
