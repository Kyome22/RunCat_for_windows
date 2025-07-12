namespace RunCat365
{
    enum Theme
    {
        System,
        Light,
        Dark,
    }

    static class ThemeExtensions
    {
        internal static string GetString(this Theme theme)
        {
            switch (theme)
            {
                case Theme.System:
                    return "System";
                case Theme.Light:
                    return "Light";
                case Theme.Dark:
                    return "Dark";
                default:
                    return "";
            }
        }
    }
}
