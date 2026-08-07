namespace GarminFitFilePaceOverlay.Navigation
{
    public partial class NavigationStore
    {
        private ViewModelBase? currentViewModel;

        public event Action? CurrentViewModelChanged;

        public ViewModelBase? CurrentViewModel 
        { 
            get => currentViewModel; 
            set 
            {
                currentViewModel = value;
                OnCurrentViewModelChanged();
            } 
        }

        public void ChangeViewModel(ViewModelBase? viewModel)
        {
            CurrentViewModel = viewModel;
        }

        private void OnCurrentViewModelChanged()
        {
            CurrentViewModelChanged?.Invoke();
        }
    }
}
