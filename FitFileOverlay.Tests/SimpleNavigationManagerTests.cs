using FitFileOverlay.Navigation;

namespace FitFileOverlay.Tests;

public class SimpleNavigationManagerTests
{
    [Test]
    public async Task NavigateTo_ChangesValueOfCurrentViewModel()
    {
        //Arrange
        SimpleNavigationManager sut = new();
        sut.RegisterViewModelFactory(NavigationTarget.Home, CreateViewModelA);

        //Act
        bool returnValue = sut.NavigateTo(NavigationTarget.Home);

        //Assert
        await Assert.That(returnValue).IsTrue();
        await Assert.That(sut.CurrentViewModel is ViewModelA).IsTrue();
    }

    [Test]
    public async Task NavigateTo_UnregisteredTargetReturnsFalse()
    {
        //Arrange
        SimpleNavigationManager sut = new();

        //Act
        bool returnValue = sut.NavigateTo(NavigationTarget.Home);

        //Assert
        await Assert.That(returnValue).IsFalse();
        await Assert.That(sut.CurrentViewModel is null).IsTrue();
    }

    [Test]
    public async Task NavigateTo_MultipleRegistrationsOnSameTargetOverwrite()
    {
        //Arrange
        SimpleNavigationManager sut = new();
        sut.RegisterViewModelFactory(NavigationTarget.Home, CreateViewModelA);
        sut.RegisterViewModelFactory(NavigationTarget.Home, CreateViewModelB);

        //Act
        bool returnValue = sut.NavigateTo(NavigationTarget.Home);

        //Assert
        await Assert.That(returnValue).IsTrue();
        await Assert.That(sut.CurrentViewModel is ViewModelB).IsTrue();
    }


    #region SIMULATOR
    private static ViewModelA CreateViewModelA() => new();
    private static ViewModelB CreateViewModelB() => new();

    public class ViewModelA : INavigableViewModel { }
    public class ViewModelB : INavigableViewModel { }
    #endregion
}
