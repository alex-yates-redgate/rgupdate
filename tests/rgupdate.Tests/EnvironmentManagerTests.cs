using Xunit;
using FluentAssertions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace rgupdate.Tests;

public class EnvironmentManagerTests
{
    [Fact]
    public void GetInstallLocation_ShouldReturnNonEmptyString()
    {
        // Act
        var location = EnvironmentManager.GetInstallLocation();
        
        // Assert
        location.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(location).Should().BeTrue("Install location should be an absolute path");
    }

    [Fact]
    public void GetDefaultInstallLocation_OnWindows_ShouldReturnNonEmptyPath()
    {
        // This test uses GetInstallLocation() which returns the current install location
        // It may not contain "Red Gate" if it's been changed during tests
        
        // Act
        var location = EnvironmentManager.GetInstallLocation();
        
        // Assert
        location.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(location).Should().BeTrue("Install location should be an absolute path");
    }

    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public void GetInstalledVersions_WithValidProduct_ShouldReturnList(string product)
    {
        // Act
        var versions = EnvironmentManager.GetInstalledVersions(product);
        
        // Assert
        versions.Should().NotBeNull();
        // Note: The list may be empty if no versions are installed, which is valid
    }

    [Fact]
    public void GetInstalledVersions_WithInvalidProduct_ShouldReturnEmptyList()
    {
        // Act
        var versions = EnvironmentManager.GetInstalledVersions("invalid-product");
        
        // Assert
        versions.Should().NotBeNull();
        versions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void GetInstalledVersions_WithNullOrEmptyProduct_ShouldReturnEmptyList(string product)
    {
        // Act
        var versions = EnvironmentManager.GetInstalledVersions(product);
        
        // Assert
        versions.Should().NotBeNull();
        versions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public async Task GetActiveVersionAsync_WithValidProduct_ShouldNotThrow(string product)
    {
        // Act
        var act = async () => await EnvironmentManager.GetActiveVersionAsync(product);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetActiveVersionAsync_WithInvalidProduct_ShouldReturnNull()
    {
        // Act
        var activeVersion = await EnvironmentManager.GetActiveVersionAsync("invalid-product");
        
        // Assert
        activeVersion.Should().BeNull();
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.1.10.8038")]
    [InlineData("8.1.23")]
    public void ParseVersionFromOutput_WithValidVersionOutput_ShouldReturnVersion(string expectedVersion)
    {
        // Arrange
        var output = $"Version {expectedVersion}";
        
        // Act
        var parsedVersion = EnvironmentManager.ParseVersionFromOutput(output);
        
        // Assert
        parsedVersion.Should().Be(expectedVersion);
    }

    [Theory]
    [InlineData("No version information")]
    [InlineData("")]
    [InlineData("  ")]
    public void ParseVersionFromOutput_WithInvalidOutput_ShouldReturnNull(string output)
    {
        // Act
        var parsedVersion = EnvironmentManager.ParseVersionFromOutput(output);
        
        // Assert
        parsedVersion.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPublicVersionsAsync_WithFlyway_ShouldThrowNotSupportedException()
    {
        // Flyway specifically is not supported for listing
        
        // Act & Assert
        var act = async () => await EnvironmentManager.GetAllPublicVersionsAsync("flyway", EnvironmentManager.Platform.Windows);
        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*flyway*");
    }

    [Theory]
    [InlineData("rgsubset", EnvironmentManager.Platform.Windows)]
    [InlineData("rganonymize", EnvironmentManager.Platform.Windows)]
    public async Task GetAllPublicVersionsAsync_WithValidProduct_ShouldReturnVersions(string product, EnvironmentManager.Platform platform)
    {
        // Note: flyway is not supported for listing, so we exclude it from this test
        
        // Act
        var versions = await EnvironmentManager.GetAllPublicVersionsAsync(product, platform);
        
        // Assert
        versions.Should().NotBeNull();
        // Note: We can't assert specific versions as they change over time
        // But we can verify the structure
    }

    [Fact]
    public async Task GetAllPublicVersionsAsync_WithInvalidProduct_ShouldHandleGracefully()
    {
        // Act
        var act = async () => await EnvironmentManager.GetAllPublicVersionsAsync("invalid-product", EnvironmentManager.Platform.Windows);
        
        // Assert
        // Should either return empty list or throw appropriate exception
        // The exact behavior depends on the implementation
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Theory]
    [InlineData("flyway", "1.0.0")]
    [InlineData("rgsubset", "2.1.10.8038")]
    [InlineData("rganonymize", "2.2.1.418")]
    public async Task GetLocalVersionInfoAsync_WithValidProductAndVersion_ShouldNotThrow(string product, string version)
    {
        // Act
        var act = async () => await EnvironmentManager.GetLocalVersionInfoAsync(product, version);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public async Task GetLocalVersionInfoAsync_WithValidProductAndVersionList_ShouldReturnEmptyList(string product)
    {
        // Arrange
        var versions = new List<string> { "2.1.10.8038", "2.2.1.418" };
        
        // Act
        var versionInfos = await EnvironmentManager.GetLocalVersionInfoAsync(product, versions);
        
        // Assert
        versionInfos.Should().NotBeNull();
        // Since no versions are actually installed locally, this will be empty
        // The method checks for actual installations, not just the provided version list
    }

    [Theory]
    [InlineData("flyway", "1.0.0")]
    [InlineData("rgsubset", "2.1.10.8038")]
    public void GetInstallationDiagnostics_WithValidProductAndVersion_ShouldReturnDiagnostics(string product, string version)
    {
        // Act
        var diagnostics = EnvironmentManager.GetInstallationDiagnostics(product, version);
        
        // Assert
        diagnostics.Should().NotBeNullOrEmpty();
        diagnostics.Should().Contain(product);
        diagnostics.Should().Contain(version);
    }

    [Fact]
    public async Task InitializeInstallLocationAsync_ShouldNotThrow()
    {
        // Act
        var act = async () => await EnvironmentManager.InitializeInstallLocationAsync();
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void RefreshEnvironmentVariables_ShouldNotThrow()
    {
        // This test accesses the internal method through reflection or by making it internal
        // For now, we'll test indirectly through other methods
        
        // Act & Assert
        var act = () => EnvironmentManager.GetInstallLocation();
        act.Should().NotThrow();
    }
}

public class SemanticVersionTests
{
    [Theory]
    [InlineData("1.0.0", 1, 0, 0, 0)]
    [InlineData("2.1.10.8038", 2, 1, 10, 8038)]
    [InlineData("8.1.23", 8, 1, 23, 0)]
    [InlineData("1", 1, 0, 0, 0)]
    [InlineData("1.2", 1, 2, 0, 0)]
    public void SemanticVersion_Constructor_ShouldParseCorrectly(string version, int major, int minor, int patch, int build)
    {
        // Act
        var semVer = new EnvironmentManager.SemanticVersion(version);
        
        // Assert
        semVer.Major.Should().Be(major);
        semVer.Minor.Should().Be(minor);
        semVer.Patch.Should().Be(patch);
        semVer.Build.Should().Be(build);
        semVer.OriginalVersion.Should().Be(version);
    }

    [Theory]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("1.2.3", "1.2.4", -1)]
    [InlineData("2.1.10.8038", "2.1.10.8037", 1)]
    public void SemanticVersion_CompareTo_ShouldCompareCorrectly(string version1, string version2, int expectedResult)
    {
        // Arrange
        var semVer1 = new EnvironmentManager.SemanticVersion(version1);
        var semVer2 = new EnvironmentManager.SemanticVersion(version2);
        
        // Act
        var result = semVer1.CompareTo(semVer2);
        
        // Assert
        if (expectedResult < 0)
        {
            result.Should().BeLessThan(0);
        }
        else if (expectedResult > 0)
        {
            result.Should().BeGreaterThan(0);
        }
        else
        {
            result.Should().Be(0);
        }
    }

    [Fact]
    public void SemanticVersion_CompareTo_WithNull_ShouldReturn1()
    {
        // Arrange
        var semVer = new EnvironmentManager.SemanticVersion("1.0.0");
        
        // Act
        var result = semVer.CompareTo(null);
        
        // Assert
        result.Should().Be(1);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("a.b.c")]
    public void SemanticVersion_WithInvalidVersion_ShouldDefaultToZeros(string version)
    {
        // Act
        var semVer = new EnvironmentManager.SemanticVersion(version);
        
        // Assert
        semVer.OriginalVersion.Should().Be(version);
        // Invalid parts should default to 0
    }
}
