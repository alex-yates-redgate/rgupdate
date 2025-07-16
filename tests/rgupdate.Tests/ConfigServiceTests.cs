using Xunit;
using FluentAssertions;
using System.IO;
using System.Threading.Tasks;
using System;

namespace rgupdate.Tests;

public class ConfigServiceTests
{
    [Fact]
    public async Task SetInstallLocationAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await ConfigService.SetInstallLocationAsync("");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Installation path cannot be empty");
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await ConfigService.SetInstallLocationAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Installation path cannot be empty");
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithWhitespacePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = async () => await ConfigService.SetInstallLocationAsync("   ");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Installation path cannot be empty");
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithValidPath_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "rgupdate-test-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            // Ensure directory doesn't exist initially
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            // Act
            await ConfigService.SetInstallLocationAsync(tempPath, force: true);

            // Assert
            Directory.Exists(tempPath).Should().BeTrue();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithEnvironmentVariables_ShouldExpandThem()
    {
        // Arrange
        var pathWithEnvVar = Path.Combine("%TEMP%", "rgupdate-test-env");
        var expectedPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP")!, "rgupdate-test-env");
        var fullExpectedPath = Path.GetFullPath(expectedPath);

        try
        {
            // Act
            await ConfigService.SetInstallLocationAsync(pathWithEnvVar, force: true);

            // Assert
            Directory.Exists(fullExpectedPath).Should().BeTrue();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(fullExpectedPath))
            {
                try
                {
                    Directory.Delete(fullExpectedPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithExistingDataAndNoForce_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "rgupdate-test-existing-" + Guid.NewGuid().ToString("N")[..8]);
        
        try
        {
            // Create directory structure that simulates existing data
            Directory.CreateDirectory(tempPath);
            var subDir = Path.Combine(tempPath, "ExistingProduct");
            Directory.CreateDirectory(subDir);

            // First, set this as the current location with force
            await ConfigService.SetInstallLocationAsync(tempPath, force: true);

            // Now try to change to a different location without force
            var newTempPath = Path.Combine(Path.GetTempPath(), "rgupdate-test-new-" + Guid.NewGuid().ToString("N")[..8]);

            // Act & Assert
            var act = async () => await ConfigService.SetInstallLocationAsync(newTempPath, force: false);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*force*");

            // Clean up new path if it was created
            if (Directory.Exists(newTempPath))
            {
                try
                {
                    Directory.Delete(newTempPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithSamePathAsCurrent_ShouldNotThrow()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "rgupdate-test-same-" + Guid.NewGuid().ToString("N")[..8]);
        
        try
        {
            // First set the location
            await ConfigService.SetInstallLocationAsync(tempPath, force: true);

            // Act - try to set the same location again
            var act = async () => await ConfigService.SetInstallLocationAsync(tempPath, force: false);

            // Assert
            await act.Should().NotThrowAsync();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Theory]
    [InlineData("D:\\MyTools\\RedGate")]
    [InlineData("C:\\CustomPath\\RedGate")]
    public async Task SetInstallLocationAsync_WithAbsolutePath_ShouldWork(string absolutePath)
    {
        // Only run this test if the drive exists (to avoid issues on different systems)
        var drive = Path.GetPathRoot(absolutePath);
        if (!Directory.Exists(drive!))
        {
            return; // Skip test if drive doesn't exist
        }

        try
        {
            // Act
            await ConfigService.SetInstallLocationAsync(absolutePath, force: true);

            // Assert
            Directory.Exists(absolutePath).Should().BeTrue();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(absolutePath))
            {
                try
                {
                    Directory.Delete(absolutePath, true);
                }
                catch
                {
                    // Ignore cleanup errors - might be permission issues
                }
            }
        }
    }

    [Fact]
    public async Task SetInstallLocationAsync_WithRelativePath_ShouldResolveToAbsolute()
    {
        // Arrange
        var relativePath = Path.Combine(".", "rgupdate-test-relative");
        var expectedAbsolutePath = Path.GetFullPath(relativePath);

        try
        {
            // Act
            await ConfigService.SetInstallLocationAsync(relativePath, force: true);

            // Assert
            Directory.Exists(expectedAbsolutePath).Should().BeTrue();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(expectedAbsolutePath))
            {
                try
                {
                    Directory.Delete(expectedAbsolutePath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task SetInstallLocationAsync_ShouldTestWritePermissions()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), "rgupdate-test-permissions-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            // Act
            await ConfigService.SetInstallLocationAsync(tempPath, force: true);

            // Assert
            // If no exception was thrown, write permissions were verified
            Directory.Exists(tempPath).Should().BeTrue();
        }
        finally
        {
            // Clean up
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
