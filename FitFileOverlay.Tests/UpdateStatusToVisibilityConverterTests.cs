using FitFileOverlay.Converters;
using FitFileOverlay.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace FitFileOverlay.Tests;

public class UpdateStatusToVisibilityConverterTests
{
    [Test]
    [Arguments(UpdateStatus.InstallError , "InstallError")]
    [Arguments(UpdateStatus.Updating, "Updating")]
    public async Task Convert_ReturnsVisible_WhenEnumStringEqualsParamString(UpdateStatus updateStatus, string param)
    {
        // Arrange
        UpdateStatusToVisibilityConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(updateStatus, typeof(Visibility), param, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(Visibility.Visible);
    }

    [Test]
    [Arguments(UpdateStatus.InstallError, "Updating")]
    [Arguments(UpdateStatus.Updating, "InstallError")]
    public async Task Convert_ReturnsCollapsed_WhenEnumStringDiffersParamString(UpdateStatus updateStatus, string param)
    {
        // Arrange
        UpdateStatusToVisibilityConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(updateStatus, typeof(Visibility), param, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(Visibility.Collapsed);
    }
}