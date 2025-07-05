using Xunit;
using FluentAssertions;
using System.IO;

namespace rgupdate.Tests;

public class PathManagerTests
{
    [Theory]
    [InlineData("flyway", "8.1.23")]
    [InlineData("rgsubset", "2.1.15.1477")]
    [InlineData("rganonymize", "2.2.2.448")]
    public void GetProductVersionPath_WithValidProductAndVersion_ShouldReturnCorrectPath(string product, string version)
    {
        // Act
        var path = PathManager.GetProductVersionPath(product, version);
        
        // Assert
        path.Should().NotBeNullOrEmpty();
        path.Should().EndWith(version);
        
        // Verify the path contains the expected components
        var productInfo = ProductConfiguration.GetProductInfo(product);
        path.Should().Contain(productInfo.Family);
        path.Should().Contain(productInfo.CliFolder);
    }

    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public void GetProductActivePath_WithValidProduct_ShouldReturnActiveDirectoryPath(string product)
    {
        // Act
        var path = PathManager.GetProductActivePath(product);
        
        // Assert
        path.Should().NotBeNullOrEmpty();
        path.Should().EndWith(Constants.ActiveVersionDirectoryName);
        
        // Verify the path contains the expected components
        var productInfo = ProductConfiguration.GetProductInfo(product);
        path.Should().Contain(productInfo.Family);
        path.Should().Contain(productInfo.CliFolder);
    }

    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public void GetProductBasePath_WithValidProduct_ShouldReturnBaseProductPath(string product)
    {
        // Act
        var path = PathManager.GetProductBasePath(product);
        
        // Assert
        path.Should().NotBeNullOrEmpty();
        
        // Verify the path contains the expected components
        var productInfo = ProductConfiguration.GetProductInfo(product);
        path.Should().Contain(productInfo.Family);
        path.Should().EndWith(productInfo.CliFolder);
    }

    [Fact]
    public void GetProductVersionPath_WithInvalidProduct_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => PathManager.GetProductVersionPath("invalid", "1.0.0");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetProductActivePath_WithInvalidProduct_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => PathManager.GetProductActivePath("invalid");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetProductBasePath_WithInvalidProduct_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => PathManager.GetProductBasePath("invalid");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("flyway", "8.1.23", "Flyway", "CLI")]
    [InlineData("rgsubset", "2.1.15.1477", "Test Data Manager", "rgsubset")]
    [InlineData("rganonymize", "2.2.2.448", "Test Data Manager", "rganonymize")]
    public void GetProductVersionPath_ShouldFollowExpectedPathStructure(string product, string version, string expectedFamily, string expectedCliFolder)
    {
        // Act
        var path = PathManager.GetProductVersionPath(product, version);
        
        // Assert
        // Path should be: [InstallLocation]\[Family]\[CliFolder]\[Version]
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        parts.Should().Contain(expectedFamily);
        parts.Should().Contain(expectedCliFolder);
        parts.Should().Contain(version);
        
        // Version should be the last component
        parts[parts.Length - 1].Should().Be(version);
    }

    [Theory]
    [InlineData("FLYWAY", "8.1.23")]
    [InlineData("RgSubset", "2.1.15.1477")]
    [InlineData("RGANONYMIZE", "2.2.2.448")]
    public void GetProductVersionPath_WithDifferentCase_ShouldWork(string product, string version)
    {
        // Act
        var path = PathManager.GetProductVersionPath(product, version);
        
        // Assert
        path.Should().NotBeNullOrEmpty();
        path.Should().EndWith(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GetProductVersionPath_WithNullOrEmptyProduct_ShouldThrowException(string product)
    {
        // Act & Assert
        var act = () => PathManager.GetProductVersionPath(product, "1.0.0");
        act.Should().Throw<Exception>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GetProductVersionPath_WithNullOrEmptyVersion_ShouldStillWork(string version)
    {
        // Act
        var path = PathManager.GetProductVersionPath("flyway", version);
        
        // Assert
        path.Should().NotBeNullOrEmpty();
        // Path should still be constructed, just with empty/null version at the end
    }
}
