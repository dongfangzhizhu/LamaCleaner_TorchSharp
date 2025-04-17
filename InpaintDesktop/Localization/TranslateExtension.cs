using System;
using System.Windows.Markup;

namespace InpaintDesktop.Localization
{
    /// <summary>
    /// Custom Markup Extension to provide localized strings in XAML.
    /// Usage: Content="{local:Translate MyResourceKey}"
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TranslateExtension : MarkupExtension
    {
        [ConstructorArgument("key")]
        public string Key { get; set; }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return string.Empty; // Or return a default value like $"[{Key ?? "null"}]"
            }
            // Use the static helper to get the string
            return LocalizationHelper.GetString(Key, $"[{Key}]"); // Provide key itself as fallback
        }
    }
}