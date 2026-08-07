using GarminFitFilePaceOverlay.Navigation;
using GarminFitFilePaceOverlay.Pages;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;
using System.Xml;

namespace GarminFitFilePaceOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly string settingsFileName = "Settings.xaml";
        private readonly IServiceProvider serviceProvider;

        public App()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<NavigationStore>();
            services.AddSingleton<HomePageViewModel>(s => new HomePageViewModel(CreateSettingsNavigationService(s)));
            services.AddSingleton<SettingsPageViewModel>(s => new SettingsPageViewModel(CreateHomeNavigationService(s)));
            services.AddSingleton<INavigationService>(CreateHomeNavigationService);
            services.AddSingleton<MainWindowViewModel>(s => new MainWindowViewModel(s.GetRequiredService<NavigationStore>()));
            services.AddSingleton<MainWindow>(s => new MainWindow(s.GetRequiredService<MainWindowViewModel>()));

            serviceProvider = services.BuildServiceProvider();
        }

        public ResourceDictionary SettingsDictionary
        {
            get { return Resources.MergedDictionaries[0]; }
        }

        public void RestoreDefaultSettings()
        {
            ResourceDictionary defaultTemplate = new ResourceDictionary() { Source = new Uri("/Templates/DefaultTemplate.xaml", UriKind.Relative) };
            ResourceDictionary defaultSettingsBase = new ResourceDictionary() { Source = new Uri("/Templates/DefaultSettingsBase.xaml", UriKind.Relative) };
            //add all base settings that are missing from template
            foreach (string key in defaultSettingsBase.Keys)
                if (!defaultTemplate.Contains(key))
                    defaultTemplate.Add(key, defaultSettingsBase[key]);
            SettingsDictionary.MergedDictionaries.Clear();
            SettingsDictionary.MergedDictionaries.Add(defaultTemplate);
        }

        public void ChangeUserSetting(string settingName, object value)
        {
            SettingsDictionary.MergedDictionaries[0][settingName] = value;
        }

        public void ChangeTemplate(string templateFileName)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Templates", templateFileName);
            if (File.Exists(filePath))
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    try
                    {
                        ResourceDictionary templateDictionary = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
                        //replace settings with values from template
                        foreach (string key in templateDictionary.Keys)
                            SettingsDictionary.MergedDictionaries[0][key] = templateDictionary[key];
                    }
                    catch (Exception) { }
                }
            }
        }

        public void ChangeTemplateEmbedded(string templateFileName)
        {
            try
            {
                ResourceDictionary templateDictionary = new ResourceDictionary() { Source = new Uri($"/Templates/{templateFileName}", UriKind.Relative) };
                //replace settings with values from template
                foreach (string key in templateDictionary.Keys)
                    SettingsDictionary.MergedDictionaries[0][key] = templateDictionary[key];
            }
            catch (Exception) { }
        }

        public void SaveSettings()
        {
            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            writerSettings.IndentChars = "\t";
            writerSettings.NewLineOnAttributes = true;
            string saveLocation = (string)Resources["SettingsFileLocation"];
            string settingsFilePath = Path.Combine(saveLocation, settingsFileName);
            Directory.CreateDirectory(saveLocation);
            using (FileStream stream = File.Create(settingsFilePath))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stream, writerSettings))
                {
                    ResourceDictionary resourceDictionary = SettingsDictionary.MergedDictionaries[0];
                    XamlServices.Save(xmlWriter, resourceDictionary);
                }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //loading save data
            string saveLocation = AppContext.BaseDirectory;
            Resources["SettingsFileLocation"] = saveLocation;
            string settingsFilePath = Path.Combine(saveLocation, settingsFileName);
            if (File.Exists(settingsFilePath))
            {
                using (FileStream stream = File.OpenRead(settingsFilePath))
                {
                    try
                    {
                        ResourceDictionary resourceDictionary = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
                        //add missing settings from default template
                        ResourceDictionary defaultTemplate = new ResourceDictionary() { Source = new Uri("/Templates/DefaultTemplate.xaml", UriKind.Relative) };
                        foreach (string key in defaultTemplate.Keys)
                            if (!resourceDictionary.Contains(key))
                                resourceDictionary.Add(key, defaultTemplate[key]);
                        //add missing settings from default base settings
                        ResourceDictionary defaultSettingsBase = new ResourceDictionary() { Source = new Uri("/Templates/DefaultSettingsBase.xaml", UriKind.Relative) };
                        foreach (string key in defaultSettingsBase.Keys)
                            if (!resourceDictionary.Contains(key))
                                resourceDictionary.Add(key, defaultSettingsBase[key]);
                        SettingsDictionary.MergedDictionaries.Clear();
                        SettingsDictionary.MergedDictionaries.Add(resourceDictionary);
                    }
                    catch (Exception) { }
                }
            }
            //opening main window
            INavigationService initialNav = serviceProvider.GetRequiredService<INavigationService>();
            initialNav.Navigate();
            MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SaveSettings();
        }

        private INavigationService CreateHomeNavigationService(IServiceProvider serviceProvider)
        {
            return new NavigationService<HomePageViewModel>(
                serviceProvider.GetRequiredService<NavigationStore>(),
                () => serviceProvider.GetRequiredService<HomePageViewModel>());
        }

        private INavigationService CreateSettingsNavigationService(IServiceProvider serviceProvider)
        {
            return new NavigationService<SettingsPageViewModel>(
                serviceProvider.GetRequiredService<NavigationStore>(),
                () => serviceProvider.GetRequiredService<SettingsPageViewModel>());
        }
    }
}
