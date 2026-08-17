using GarminFitFilePaceOverlay.Navigation;

namespace GarminFitFilePaceOverlay
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly NavigationStore navigationStore;

        public MainWindowViewModel(NavigationStore navigationStore)
        {
            this.navigationStore = navigationStore;
            navigationStore.CurrentViewModelChanged += Navigation_CurrentViewModelChanged;
        }

        public ViewModelBase? CurrentViewModel => navigationStore.CurrentViewModel;

        private void Navigation_CurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }
    }
}
