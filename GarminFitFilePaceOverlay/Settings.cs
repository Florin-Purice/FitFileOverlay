using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarminFitFilePaceOverlay
{
    internal static class Settings
    {
        private static App app;

        static Settings()
        {
            app = (App)(App.Current);
        }

        public static void LoadTemplate(string templateFileName, bool embedded = false)
        {
            if (embedded)
                app.ChangeTemplateEmbedded(templateFileName);
            else
                app.ChangeTemplate(templateFileName);
        }

        public static void Set(string settingName, object value)
        {
            app.ChangeUserSetting(settingName, value);
        }

        public static object Get(string settingName)
        {
            return app.SettingsDictionary.MergedDictionaries[0][settingName];
        }

        public static T Get<T>(string settingName)
        {
            return (T)app.SettingsDictionary.MergedDictionaries[0][settingName];
        }

        public static void RestoreDefault()
        {
            app.RestoreDefaultSettings();
        }

        public static void Save()
        {
            app.SaveSettings();
        }
    }
}
