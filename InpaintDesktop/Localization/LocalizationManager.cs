using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace InpaintDesktop.Localization
{
    public class LocalizationManager
    {
        private static LocalizationManager? _instance;
        private Dictionary<string, Dictionary<string, string>> _localizations;
        private string _currentCulture;

        public static LocalizationManager Instance
        {
            get
            {
                _instance ??= new LocalizationManager();
                return _instance;
            }
        }

        private LocalizationManager()
        {
            _localizations = new Dictionary<string, Dictionary<string, string>>();
            _currentCulture = CultureInfo.CurrentUICulture.Name;
            LoadLocalizations();
        }

        private void LoadLocalizations()
        {
            string langsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "langs");
            if (!Directory.Exists(langsPath))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(langsPath, "*.xml"))
            {
                try
                {
                    XDocument doc = XDocument.Load(file);
                    var cultureName = doc.Root?.Attribute("cultureName")?.Value;
                    if (string.IsNullOrEmpty(cultureName))
                        continue;

                    var resources = new Dictionary<string, string>();
                    LoadResourcesRecursively(doc.Root, "", resources);
                    _localizations[cultureName] = resources;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading localization file {file}: {ex.Message}");
                }
            }

            // 如果没有找到当前UI文化的本地化资源，使用英语作为默认语言
            if (!_localizations.ContainsKey(_currentCulture))
            {
                _currentCulture = "en-US";
            }
        }

        private void LoadResourcesRecursively(XElement? element, string prefix, Dictionary<string, string> resources)
        {
            if (element == null) return;

            foreach (var child in element.Elements())
            {
                string key = string.IsNullOrEmpty(prefix) ? child.Name.LocalName : $"{prefix}.{child.Name.LocalName}";

                if (!child.HasElements)
                {
                    resources[key] = child.Value;
                }
                else
                {
                    LoadResourcesRecursively(child, key, resources);
                }
            }
        }

        public string GetString(string key)
        {
            if (_localizations.TryGetValue(_currentCulture, out var resources) &&
                resources.TryGetValue(key, out var value))
            {
                return value;
            }

            // 如果在当前语言中找不到，尝试使用英语
            if (_currentCulture != "en-US" &&
                _localizations.TryGetValue("en-US", out var enResources) &&
                enResources.TryGetValue(key, out var enValue))
            {
                return enValue;
            }

            return key;
        }

        public void SetCulture(string cultureName)
        {
            if (_localizations.ContainsKey(cultureName))
            {
                _currentCulture = cultureName;
            }
        }

        public IEnumerable<string> GetAvailableCultures()
        {
            return _localizations.Keys;
        }
    }
}