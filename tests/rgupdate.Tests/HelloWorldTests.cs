using Xunit;

namespace rgupdate.Tests;

public class HelloWorldTests
{
    [Fact]
    public void HelloWorld_ShouldPass()
    {
        // Arrange
        var expected = "Hello World";
        
        // Act
        var actual = "Hello World";
        
        // Assert
        Assert.Equal(expected, actual);
    }
    
    [Fact]
    public void BasicMath_ShouldWork()
    {
        // Arrange & Act
        var result = 2 + 2;
        
        // Assert
        Assert.Equal(4, result);
    }
}
