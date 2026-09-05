using FitFileOverlay.Models;
using FitFileOverlay.Services;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace FitFileOverlay.Tests;

public class OverlayServiceTests
{
    [Test]
    public async Task TestOverlayServiceInitialization()
    {
        // Arrange

        // Act
        IOverlayService? overlayService = App.Services.GetService<IOverlayService>();

        // Assert
        await Assert.That(overlayService).IsNotNull();
    }

    [Test]
    public async Task Load_LoadsValidFitFile()
    {
        // Arrange
        OverlayService sut = new();
        string validFitFilePath = "./Assets/valid.fit";

        // Act
        bool loadResult = sut.Load(validFitFilePath);

        // Assert
        await Assert.That(loadResult).IsTrue();
        await Assert.That(sut.File).IsNotNull();
        await Assert.That(sut.File.IsValid).IsTrue();
        await Assert.That(sut.File.Records).Count().IsEqualTo(187);
    }

    [Test]
    public async Task Load_LoadsInvalidFitFile()
    {
        // Arrange
        OverlayService sut = new();
        string invalidFitFilePath = "./Assets/invalid.fit";

        // Act
        bool loadResult = sut.Load(invalidFitFilePath);

        // Assert
        await Assert.That(loadResult).IsFalse();
    }

    [Test]
    [Arguments(0.5)]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(-1)]
    [Arguments(5)]
    public async Task GetSnapshot_ReturnsNonNullBitmap(double activityPercent)
    {
        // Arrange
        IOverlayService sut = App.Services.GetService<IOverlayService>()!;

        // Assert
        SKBitmap? snapshot = sut.GetSnapshot(activityPercent);

        // Assert
        await Assert.That(snapshot).IsNotNull();
    }

    [Test]
    public async Task Export_CreatesTheCorrectVideoFile()
    {
        // Arrange
        IOverlayService sut = App.Services.GetService<IOverlayService>()!;
        string fileName = Path.Combine(TestContext.ResultsDirectory, "OverlayService_Export", "output.mov");
        // Delete the file if it already exists
        if (File.Exists(fileName))
            File.Delete(fileName);

        // Assert
        await sut.Export(fileName);

        // Assert
        await Assert.That(File.Exists(fileName)).IsTrue();
        //attach artifact
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    [Before(Class)]
    public static void InitStaticOverlayService()
    {
        try
        {
            // Ensure the overlay service is initialized before each test
            IOverlayService os = App.Services.GetService<IOverlayService>()!;
            os.Load("./Assets/short.fit");
            os.Settings = new OverlaySettings();
        }
        catch { }
    }
}
