using CommunityToolkit.Mvvm.ComponentModel;
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
    internal partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private UserControl currentPage;

        private bool isEnabled = true;
        private bool useFileLTHR;
        private string customLthr;
        private uint fps;
        private double snapshotActivityPercent = 0.5;

        public delegate void SnapshotActivityPercentChangedEventHandler(double newValue);
        public event SnapshotActivityPercentChangedEventHandler? SnapshotActivityPercentChanged;

        public MainWindowViewModel()
        {
            CurrentPage = new Pages.HomePage();

            useFileLTHR = Settings.Get<bool>("UseFileLTHR");
            fps = Settings.Get<uint>("FPS");
            int customLthrValue = Settings.Get<int>("CustomLTHR");
            customLthr = customLthrValue.ToString();
        }

        public bool IsEnabled { get => isEnabled; set { isEnabled = value; OnPropertyChanged("IsEnabled"); } }
        public double SnapshotActivityPercent 
        { 
            get => snapshotActivityPercent; 
            set 
            { 
                snapshotActivityPercent = value; 
                OnPropertyChanged("SnapshotActivityPercent");
                OnSnapshotActivityPercentChanged(value);
            } 
        }

        public bool UseFileLTHR
        {
            get => useFileLTHR;
            set
            {
                useFileLTHR = value;
                Settings.Set("UseFileLTHR", value);
                OnPropertyChanged("UseFileLTHR");
            }
        }

        public string CustomLTHR
        {
            get => customLthr;
            set
            {
                if (int.TryParse(value, out int parsedValue))
                {
                    customLthr = value;
                    Settings.Set("CustomLTHR", parsedValue);
                    OnPropertyChanged("CustomLTHR");
                }
            }
        }

        public uint FPS
        {
            get => fps;
            set
            {
                fps = value;
                Settings.Set("FPS", value);
                OnPropertyChanged("FPS");
            }
        }

        private void OnSnapshotActivityPercentChanged(double value)
        {
            SnapshotActivityPercentChanged?.Invoke(value);
        }
    }
}
