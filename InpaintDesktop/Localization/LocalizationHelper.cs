using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace InpaintDesktop.Localization
{
    public static class LocalizationHelper
    {
        private static Dictionary<string, string> _localizedStrings = new Dictionary<string, string>();

        public static void LoadLanguage(string cultureName)
        {
            string langDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "langs");
            string langFile = Path.Combine(langDir, cultureName + ".xml");
            if (!File.Exists(langFile))
            {
                // Fallback to en-US
                langFile = Path.Combine(langDir, "en-US.xml");
            }
            if (!File.Exists(langFile)) return;
            _localizedStrings.Clear();
            var doc = XDocument.Load(langFile);
            // Add null check for doc.Root to address CS8602 warning
            if (doc.Root != null)
            {
                var mainWindow = doc.Root.Element("MainWindow");
                if (mainWindow != null)
                {
                    foreach (var el in mainWindow.Elements())
                    {
                        _localizedStrings[el.Name.LocalName] = el.Value;
                    }
                }
            }
        }

        public static string GetString(string key, string defaultValue = "")
        {
            if (_localizedStrings.TryGetValue(key, out var value))
                return value;
            return defaultValue;
        }
    }
}
