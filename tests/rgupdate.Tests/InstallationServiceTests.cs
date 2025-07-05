using Xunit;
using System;
using System.Threading.Tasks;

namespace rgupdate.Tests;

public class InstallationServiceTests
{
    [Fact]
    public async Task InstallProductAsync_WithUnsupportedProduct_ThrowsArgumentException()
    {
        // Arrange
        var product = "unsupported-product";
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            InstallationService.InstallProductAsync(product));
        Assert.Equal($"Unsupported product: {product}", exception.Message);
    }
    
    [Fact]
    public async Task InstallProductAsync_WithNullProduct_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            InstallationService.InstallProductAsync(null!));
        Assert.Equal("Unsupported product: ", exception.Message);
    }
    
    [Fact]
    public async Task InstallProductAsync_WithEmptyProduct_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            InstallationService.InstallProductAsync(""));
        Assert.Equal("Unsupported product: ", exception.Message);
    }
    
    [Theory]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    // Skip flyway as it may require specific setup
    public async Task InstallProductAsync_WithSupportedProduct_CompletesSuccessfully(string product)
    {
        // Note: This test actually installs the product since the environment supports it
        // In a real test environment, we might want to mock the EnvironmentManager
        
        // Arrange & Act
        await InstallationService.InstallProductAsync(product);
        
        // Assert
        // If we reach here, the installation completed without throwing
        Assert.True(true);
    }
    
    [Theory]
    [InlineData("rgsubset", "1.0.0")]
    [InlineData("rganonymize", "2.1.0")]
    // Skip flyway as it may require specific setup  
    public async Task InstallProductAsync_WithSupportedProductAndVersion_CompletesSuccessfully(string product, string version)
    {
        // Note: This test actually installs the product since the environment supports it
        
        // Arrange & Act
        await InstallationService.InstallProductAsync(product, version);
        
        // Assert
        // If we reach here, the installation completed without throwing
        Assert.True(true);
    }
    
    [Fact]
    public async Task InstallProductAsync_WithNullVersion_CompletesSuccessfully()
    {
        // Arrange
        var product = "rgsubset";
        
        // Act
        await InstallationService.InstallProductAsync(product, null);
        
        // Assert
        // The method should accept null version (defaults to latest) and complete successfully
        Assert.True(true);
    }
}
