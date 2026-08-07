using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminFitFilePaceOverlay.Navigation;
using GarminFitFilePaceOverlay.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GarminFitFilePaceOverlay
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private NavigationStore navigationStore;

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
