using Xunit;
using FluentAssertions;

namespace rgupdate.Tests;

public class ProductInfoTests
{
    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public void IsProductSupported_WithValidProducts_ShouldReturnTrue(string product)
    {
        // Act
        var isSupported = ProductConfiguration.IsProductSupported(product);
        
        // Assert
        isSupported.Should().BeTrue();
    }

    [Theory]
    [InlineData("FLYWAY")]
    [InlineData("RgSubset")]
    [InlineData("RGANONYMIZE")]
    public void IsProductSupported_WithValidProductsDifferentCase_ShouldReturnTrue(string product)
    {
        // Act
        var isSupported = ProductConfiguration.IsProductSupported(product);
        
        // Assert
        isSupported.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("sqlcompare")]
    [InlineData("")]
    public void IsProductSupported_WithInvalidProducts_ShouldReturnFalse(string product)
    {
        // Act
        var isSupported = ProductConfiguration.IsProductSupported(product);
        
        // Assert
        isSupported.Should().BeFalse();
    }

    [Fact]
    public void GetProductInfo_WithFlyway_ShouldReturnCorrectInfo()
    {
        // Act
        var productInfo = ProductConfiguration.GetProductInfo("flyway");
        
        // Assert
        productInfo.Should().NotBeNull();
        productInfo.Family.Should().Be("Flyway");
        productInfo.CliFolder.Should().Be("CLI");
    }

    [Fact]
    public void GetProductInfo_WithRgsubset_ShouldReturnCorrectInfo()
    {
        // Act
        var productInfo = ProductConfiguration.GetProductInfo("rgsubset");
        
        // Assert
        productInfo.Should().NotBeNull();
        productInfo.Family.Should().Be("Test Data Manager");
        productInfo.CliFolder.Should().Be("rgsubset");
    }

    [Fact]
    public void GetProductInfo_WithRganonymize_ShouldReturnCorrectInfo()
    {
        // Act
        var productInfo = ProductConfiguration.GetProductInfo("rganonymize");
        
        // Assert
        productInfo.Should().NotBeNull();
        productInfo.Family.Should().Be("Test Data Manager");
        productInfo.CliFolder.Should().Be("rganonymize");
    }

    [Fact]
    public void GetProductInfo_WithInvalidProduct_ShouldThrowException()
    {
        // Act & Assert
        var act = () => ProductConfiguration.GetProductInfo("invalid");
        act.Should().Throw<ArgumentException>()
           .WithMessage("Unsupported product: invalid*");
    }

    [Theory]
    [InlineData("FLYWAY", "Flyway")]
    [InlineData("RgSubset", "Test Data Manager")]
    public void GetProductInfo_WithDifferentCase_ShouldWork(string product, string expectedFamily)
    {
        // Act
        var productInfo = ProductConfiguration.GetProductInfo(product);
        
        // Assert
        productInfo.Family.Should().Be(expectedFamily);
    }
}
