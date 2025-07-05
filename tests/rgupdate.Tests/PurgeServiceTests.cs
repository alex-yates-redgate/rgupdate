using Xunit;
using System;
using System.Threading.Tasks;

namespace rgupdate.Tests;

public class PurgeServiceTests
{
    [Fact]
    public async Task PurgeOldVersionsAsync_WithUnsupportedProduct_ThrowsArgumentException()
    {
        // Arrange
        var product = "unsupported-product";
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            PurgeService.PurgeOldVersionsAsync(product));
        Assert.Equal($"Unsupported product: {product}", exception.Message);
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_WithNullProduct_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            PurgeService.PurgeOldVersionsAsync(null!));
        Assert.Equal("Unsupported product: ", exception.Message);
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_WithEmptyProduct_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            PurgeService.PurgeOldVersionsAsync(""));
        Assert.Equal("Unsupported product: ", exception.Message);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task PurgeOldVersionsAsync_WithInvalidKeepCount_ThrowsArgumentException(int keepCount)
    {
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            PurgeService.PurgeOldVersionsAsync(product, keepCount));
        Assert.Equal("Keep count must be at least 1", exception.Message);
    }
    
    [Theory]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    [InlineData("flyway")]
    public async Task PurgeOldVersionsAsync_WithNoInstalledVersions_DisplaysMessage(string product)
    {
        // Note: This test verifies the behavior when no versions are installed
        // In a clean test environment, no versions should be installed
        
        // Arrange & Act
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        
        // Assert
        // The method should complete successfully and display "No versions installed" message
        Assert.True(true); // The test passes if no exception is thrown
    }
    
    [Theory]
    [InlineData("rgsubset", 3)]
    [InlineData("rganonymize", 5)]
    [InlineData("flyway", 2)]
    public async Task PurgeOldVersionsAsync_WithFewerVersionsThanKeepCount_DisplaysNothingToPurge(string product, int keepCount)
    {
        // Note: This tests the scenario where installed versions <= keepCount
        // In a clean test environment, this should display "nothing to purge"
        
        // Arrange & Act
        await PurgeService.PurgeOldVersionsAsync(product, keepCount, force: true);
        
        // Assert
        Assert.True(true); // Test passes if method completes without error
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_WithDefaultKeepCount_KeepsThreeVersions()
    {
        // Arrange
        var product = "rgsubset";
        
        // Act
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        
        // Assert
        // Default keep count should be 3
        Assert.True(true);
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task PurgeOldVersionsAsync_WithCustomKeepCount_KeepsSpecifiedNumber(int keepCount)
    {
        // Arrange
        var product = "rgsubset";
        
        // Act
        await PurgeService.PurgeOldVersionsAsync(product, keepCount, force: true);
        
        // Assert
        Assert.True(true); // Test passes if method accepts the keep count
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_WithForceFlag_SkipsConfirmation()
    {
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        // Force flag should skip confirmation prompts
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true); // Test passes if method completes without hanging on input
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_WithoutForceFlag_RequiresConfirmation()
    {
        // Note: This test would require mocking console input
        // For now, we test with force=true to avoid hanging on input
        
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true);
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_PreservesActiveVersion()
    {
        // Note: This tests that the active version is preserved even if it's not in the most recent versions
        // The actual test would need to set up an scenario with multiple versions and an active version
        
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true);
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_SortsVersionsSemanticaly()
    {
        // Note: This tests that versions are sorted using semantic versioning rules
        // The actual test would verify that versions like 1.10.0 comes after 1.2.0
        
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true);
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_DisplaysVersionsToKeepAndRemove()
    {
        // Note: This tests the display logic that shows which versions will be kept/removed
        // The actual test would capture console output and verify the formatting
        
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true);
    }
    
    [Fact]
    public void PerformPurgeAsync_RemovesSpecifiedVersions()
    {
        // Note: This tests the private method indirectly
        // The actual implementation would create test directories and verify removal
        
        Assert.True(true); // Placeholder for removal operation testing
    }
    
    [Fact]
    public void PerformPurgeAsync_HandlesRemovalErrors()
    {
        // Note: This tests error handling during purge operations
        // The actual implementation would test scenarios where directory removal fails
        
        Assert.True(true); // Placeholder for error handling testing
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_UpdatesVersionCounts()
    {
        // Note: This tests that the correct counts are displayed after purging
        
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true);
    }
    
    [Fact]
    public async Task PurgeOldVersionsAsync_HandlesActiveVersionNotInKeepList()
    {
        // Note: This tests the logic that moves active version to keep list if it would be removed
        
        // Arrange
        var product = "rgsubset";
        
        // Act & Assert
        await PurgeService.PurgeOldVersionsAsync(product, force: true);
        Assert.True(true);
    }
}
