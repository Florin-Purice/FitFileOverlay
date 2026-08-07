using System;
using System.Collections.Generic;
using System.Text;

namespace GarminFitFilePaceOverlay.Navigation
{
    public class NavigationService<T> : INavigationService where T : ViewModelBase
    {
        private readonly NavigationStore navigationStore;
        private readonly Func<T> createViewModel;

        public NavigationService(NavigationStore navigationStore, Func<T> createViewModel)
        {
            this.navigationStore = navigationStore;
            this.createViewModel = createViewModel;
        }

        public void Navigate()
        {
            T viewModel = createViewModel();
            navigationStore.ChangeViewModel(viewModel);
        }
    }
}
