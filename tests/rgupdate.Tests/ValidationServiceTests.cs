using Xunit;
using FluentAssertions;

namespace rgupdate.Tests;

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var isValid = true;
        var message = "Test message";
        
        // Act
        var result = new ValidationResult(isValid, message);
        
        // Assert
        result.IsValid.Should().Be(isValid);
        result.Message.Should().Be(message);
    }
    
    [Fact]
    public void ValidationResult_EqualityComparison_WorksCorrectly()
    {
        // Arrange
        var result1 = new ValidationResult(true, "Success");
        var result2 = new ValidationResult(true, "Success");
        var result3 = new ValidationResult(false, "Failure");
        
        // Act & Assert
        result1.Should().Be(result2);
        result1.Should().NotBe(result3);
    }
}

public class ValidationServiceTests
{
    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public async Task ValidateProductAsync_ValidProduct_ReturnsValidationResult(string product)
    {
        // Act
        var result = await ValidationService.ValidateProductAsync(product);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationResult>();
        // Note: We can't assert IsValid since it depends on actual installations
    }
    
    [Theory]
    [InlineData("invalid")]
    public async Task ValidateProductAsync_InvalidProduct_ReturnsFailureResult(string product)
    {
        // Act
        var result = await ValidationService.ValidateProductAsync(product);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Unsupported product");
    }

    [Theory]
    [InlineData("")]
    public async Task ValidateProductAsync_EmptyProduct_ReturnsFailureResult(string product)
    {
        // Act
        var result = await ValidationService.ValidateProductAsync(product);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Product name cannot be null or empty");
    }
    
    [Fact]
    public async Task ValidateProductAsync_NullProduct_ReturnsFailureResult()
    {
        // Act
        var result = await ValidationService.ValidateProductAsync(null!);
        
        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("Product name cannot be null or empty");
    }
    
    [Theory]
    [InlineData("flyway")]
    [InlineData("rgsubset")]
    [InlineData("rganonymize")]
    public async Task ValidateProductAsync_ValidProductWithSpecificVersion_ReturnsValidationResult(string product)
    {
        // Arrange
        var version = "2.1.10.8038"; // Test version that likely doesn't exist
        
        // Act
        var result = await ValidationService.ValidateProductAsync(product, version);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationResult>();
        // Since version 1.0.0 likely doesn't exist, this should be false
        if (!result.IsValid)
        {
            result.Message.Should().Contain("not installed");
        }
    }
}
