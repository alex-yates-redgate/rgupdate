using Xunit;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace rgupdate.Tests;

public class ListingServiceTests
{
    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public async Task ListVersionsAsync_WithValidProduct_ShouldNotThrow(string product)
    {
        // Act
        var act = async () => await ListingService.ListVersionsAsync(product, showAll: false, outputFormat: null);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListVersionsAsync_WithInvalidProduct_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await ListingService.ListVersionsAsync("invalid-product");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported product*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ListVersionsAsync_WithNullOrEmptyProduct_ShouldThrowException(string? product)
    {
        // Act & Assert
        var act = async () => await ListingService.ListVersionsAsync(product!);
        await act.Should().ThrowAsync<Exception>();
    }

    [Theory]
    [InlineData("flyway", true)]
    [InlineData("rgsubset", false)]
    [InlineData("rganonymize", true)]
    public async Task ListVersionsAsync_WithShowAllFlag_ShouldNotThrow(string product, bool showAll)
    {
        // Act
        var act = async () => await ListingService.ListVersionsAsync(product, showAll: showAll);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListVersionsAsync_WithFlyway_ShouldNotThrow()
    {
        // Flyway should now be supported for listing
        
        // Act & Assert - should not throw an exception
        await ListingService.ListVersionsAsync("flyway", showAll: false, outputFormat: "json");
        
        // If we reach here without exception, the test passes
    }

    [Theory]
    [InlineData("rgsubset", "json")]
    [InlineData("rganonymize", "yaml")]
    [InlineData("rgsubset", "yaml")]
    public async Task ListVersionsAsync_WithStructuredOutput_ShouldNotThrow(string product, string outputFormat)
    {
        // Note: flyway is excluded because it throws NotSupportedException for listing
        
        // Act
        var act = async () => await ListingService.ListVersionsAsync(product, showAll: false, outputFormat: outputFormat);
        
        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("invalid-format")]
    [InlineData("xml")]
    [InlineData("csv")]
    public async Task ListVersionsAsync_WithInvalidOutputFormat_ShouldStillWork(string outputFormat)
    {
        // The method should handle invalid output formats gracefully
        // by defaulting to a standard format or throwing a clear error
        
        // Act
        var act = async () => await ListingService.ListVersionsAsync("flyway", outputFormat: outputFormat);
        
        // Assert
        // Should either work (by defaulting) or throw a clear exception
        await act.Should().NotThrowAsync<NullReferenceException>();
    }

    [Theory]
    [InlineData("flyway", false, "")]
    [InlineData("rgsubset", true, "json")]
    [InlineData("rganonymize", false, "yaml")]
    public async Task ListVersionsAsync_WithVariousParameters_ShouldNotThrow(string product, bool showAll, string outputFormat)
    {
        // Act
        var act = async () => await ListingService.ListVersionsAsync(product, showAll, outputFormat);
        
        // Assert
        await act.Should().NotThrowAsync();
    }
}

public class VersionOutputTests
{
    [Fact]
    public void VersionOutput_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var versionOutput = new ListingService.VersionOutput();
        
        // Assert
        versionOutput.Version.Should().NotBeNull();
        versionOutput.Version.Should().Be(string.Empty);
        versionOutput.SizeFormatted.Should().NotBeNull();
        versionOutput.SizeFormatted.Should().Be(string.Empty);
        versionOutput.Status.Should().NotBeNull();
        versionOutput.Status.Should().Be(string.Empty);
        versionOutput.SizeBytes.Should().Be(0);
        versionOutput.IsLocalOnly.Should().BeFalse();
        versionOutput.IsInstalled.Should().BeFalse();
        versionOutput.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("1.0.0", 1024, "1 KB", "installed", false, true, false)]
    [InlineData("2.1.10.8038", 37400000, "35.7 MB", "ACTIVE", true, true, true)]
    [InlineData("8.1.23", 0, "0 B", "-", false, false, false)]
    public void VersionOutput_WithProperties_ShouldSetCorrectly(string version, long sizeBytes, string sizeFormatted, string status, bool isLocalOnly, bool isInstalled, bool isActive)
    {
        // Act
        var versionOutput = new ListingService.VersionOutput
        {
            Version = version,
            SizeBytes = sizeBytes,
            SizeFormatted = sizeFormatted,
            Status = status,
            IsLocalOnly = isLocalOnly,
            IsInstalled = isInstalled,
            IsActive = isActive
        };
        
        // Assert
        versionOutput.Version.Should().Be(version);
        versionOutput.SizeBytes.Should().Be(sizeBytes);
        versionOutput.SizeFormatted.Should().Be(sizeFormatted);
        versionOutput.Status.Should().Be(status);
        versionOutput.IsLocalOnly.Should().Be(isLocalOnly);
        versionOutput.IsInstalled.Should().Be(isInstalled);
        versionOutput.IsActive.Should().Be(isActive);
    }
}

public class ListingOutputTests
{
    [Fact]
    public void ListingOutput_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var listingOutput = new ListingService.ListingOutput();
        
        // Assert
        listingOutput.Product.Should().NotBeNull();
        listingOutput.Product.Should().Be(string.Empty);
        listingOutput.TotalVersions.Should().Be(0);
        listingOutput.DisplayedVersions.Should().Be(0);
        listingOutput.ShowingAll.Should().BeFalse();
        listingOutput.Versions.Should().NotBeNull();
        listingOutput.Versions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("flyway", "8.1.23", 100, 10, false)]
    [InlineData("rgsubset", "2.1.10.8038", 54, 54, true)]
    [InlineData("rganonymize", "", 25, 25, true)]
    public void ListingOutput_WithProperties_ShouldSetCorrectly(string product, string activeVersion, int totalVersions, int displayedVersions, bool showingAll)
    {
        // Arrange
        var versions = new List<ListingService.VersionOutput>
        {
            new() { Version = "1.0.0", Status = "installed" },
            new() { Version = "2.0.0", Status = "ACTIVE" }
        };
        
        // Act
        var listingOutput = new ListingService.ListingOutput
        {
            Product = product,
            ActiveVersion = activeVersion,
            TotalVersions = totalVersions,
            DisplayedVersions = displayedVersions,
            ShowingAll = showingAll,
            Versions = versions
        };
        
        // Assert
        listingOutput.Product.Should().Be(product);
        listingOutput.ActiveVersion.Should().Be(activeVersion);
        listingOutput.TotalVersions.Should().Be(totalVersions);
        listingOutput.DisplayedVersions.Should().Be(displayedVersions);
        listingOutput.ShowingAll.Should().Be(showingAll);
        listingOutput.Versions.Should().HaveCount(2);
        listingOutput.Versions.Should().Contain(v => v.Version == "1.0.0");
        listingOutput.Versions.Should().Contain(v => v.Version == "2.0.0");
    }

    [Fact]
    public void ListingOutput_WithEmptyVersionsList_ShouldWork()
    {
        // Act
        var listingOutput = new ListingService.ListingOutput
        {
            Product = "flyway",
            Versions = new List<ListingService.VersionOutput>()
        };
        
        // Assert
        listingOutput.Versions.Should().NotBeNull();
        listingOutput.Versions.Should().BeEmpty();
    }
}
