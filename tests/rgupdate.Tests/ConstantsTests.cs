using Xunit;
using FluentAssertions;

namespace rgupdate.Tests;

public class ConstantsTests
{
    [Fact]
    public void SupportedProducts_ShouldContainExpectedProducts()
    {
        // Arrange & Act
        var products = Constants.SupportedProducts;
        
        // Assert
        products.Should().NotBeEmpty();
        products.Should().Contain("flyway");
        products.Should().Contain("rgsubset");
        products.Should().Contain("rganonymize");
    }

    [Fact]
    public void InstallLocationEnvVar_ShouldBeCorrectValue()
    {
        // Arrange & Act
        var envVar = Constants.InstallLocationEnvVar;
        
        // Assert
        envVar.Should().Be("RGUPDATE_INSTALL_LOCATION");
        envVar.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DefaultVersionDisplayLimit_ShouldBePositive()
    {
        // Arrange & Act
        var limit = Constants.DefaultVersionDisplayLimit;
        
        // Assert
        limit.Should().BePositive();
        limit.Should().Be(10);
    }

    [Fact]
    public void ActiveVersionDirectoryName_ShouldBeActive()
    {
        // Arrange & Act
        var dirName = Constants.ActiveVersionDirectoryName;
        
        // Assert
        dirName.Should().Be("active");
        dirName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DownloadTimeoutMinutes_ShouldBeReasonable()
    {
        // Arrange & Act
        var timeout = Constants.DownloadTimeoutMinutes;
        
        // Assert
        timeout.Should().BePositive();
        timeout.Should().BeGreaterThan(0);
        timeout.Should().BeLessThan(60); // Less than 1 hour
    }

    [Fact]
    public void ProgressReportIntervalSeconds_ShouldBeReasonable()
    {
        // Arrange & Act
        var interval = Constants.ProgressReportIntervalSeconds;
        
        // Assert
        interval.Should().BePositive();
        interval.Should().BeGreaterThan(0);
        interval.Should().BeLessThan(10); // Less than 10 seconds
    }
}
