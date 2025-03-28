using System.Resources;
using System.Reflection;
using System.Globalization;

namespace RunCat
{
    public static class RunCatLanguageHelper
    {
        private static ResourceManager resource_manager;

        static RunCatLanguageHelper()
        {
            resource_manager = new ResourceManager("RunCat.Languages.language", Assembly.GetExecutingAssembly());
        }

        public static string? GetString(string name)
        {
            return resource_manager.GetString(name);
        }

        public static void ChangeLanguage(string language)
        {
            CultureInfo culture_info = new CultureInfo(language);

            CultureInfo.CurrentCulture = culture_info;
            CultureInfo.CurrentUICulture = culture_info;
        }
    }
}
