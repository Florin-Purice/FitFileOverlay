using FitFileOverlay.Helpers;
using System.Windows;

namespace FitFileOverlay.Tests;

public class CollectionToVisibilityConverterTests
{
    [Test]
    public async Task Convert_ReturnsVisible_WhenCollectionHasItems()
    {
        // Arrange
        CollectionToVisibilityConverter sut = new();
        List<int> collection = [1, 2, 3];

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(collection, typeof(Visibility), default, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(Visibility.Visible);
    }

    [Test]
    public async Task Convert_ReturnsCollapsed_WhenCollectionIsEmpty()
    {
        // Arrange
        CollectionToVisibilityConverter sut = new();
        List<int> collection = [];

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(collection, typeof(Visibility), default, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(Visibility.Collapsed);
    }

    [Test]
    public async Task Convert_ReturnsCollapsed_WhenCollectionIsNull()
    {
        // Arrange
        CollectionToVisibilityConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(null, typeof(Visibility), default, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(Visibility.Collapsed);
    }
}
