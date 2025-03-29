using System.Resources;
using System.Reflection;
using System.Globalization;
using System.IO;
using System;
using System.Collections.Generic;

namespace RunCat
{
    public static class RunCatLanguageHelper
    {
        private static ResourceManager resourceManager;
        private static List<string> supportedLanguagesList;

        static RunCatLanguageHelper()
        {
            resourceManager = new ResourceManager("RunCat.Languages.language", Assembly.GetExecutingAssembly());
            InitSupportedLanguages();
        }

        public static string? GetString(string name)
        {
            return resourceManager.GetString(name);
        }

        public static void ChangeLanguage(string language)
        {
            CultureInfo cultureInfo = new(language);

            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;
        }

        public static List<string> GetSupportedLanguages()
        {
            return supportedLanguagesList;
        }

        public static bool IsInSupportedLanguages(string language)
        {
            return supportedLanguagesList.Contains(language);
        }

        private static void InitSupportedLanguages()
        {
            string workingDirectory = Environment.CurrentDirectory;
            string projectDirectory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
            string languagesPath = Path.Combine(projectDirectory, "Languages");
            supportedLanguagesList = new List<string>();

            if (!Directory.Exists(languagesPath))
            {
                return;
            }

            string[] languageFiles = Directory.GetFiles(languagesPath, "language.*.resx");

            foreach (string languageFile in languageFiles)
            {
                string languageFileName = Path.GetFileNameWithoutExtension(languageFile);
                string supportedLanguageName = languageFileName.Replace("language.", "");
                supportedLanguagesList.Add(supportedLanguageName);
            }
        }
    }
}
