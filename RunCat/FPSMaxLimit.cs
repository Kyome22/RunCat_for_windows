namespace RunCat365
{
    enum FPSMaxLimit
    {
        FPS40,
        FPS30,
        FPS20,
        FPS10,
    }

    static class FPSMaxLimitExtensions
    {
        internal static string GetString(this FPSMaxLimit fpsMaxLimit)
        {
            switch (fpsMaxLimit)
            {
                case FPSMaxLimit.FPS40:
                    return "40fps";
                case FPSMaxLimit.FPS30:
                    return "30fps";
                case FPSMaxLimit.FPS20:
                    return "20fps";
                case FPSMaxLimit.FPS10:
                    return "10fps";
                default:
                    return "";
            }
        }

        internal static float GetRate(this FPSMaxLimit fPSMaxLimit)
        {
            switch (fPSMaxLimit)
            {
                case FPSMaxLimit.FPS40:
                    return 1f;
                case FPSMaxLimit.FPS30:
                    return 0.75f;
                case FPSMaxLimit.FPS20:
                    return 0.5f;
                case FPSMaxLimit.FPS10:
                    return 0.25f;
                default:
                    return 1f;
            }
        }
    }

    static class _FPSMaxLimit
    {
        internal static FPSMaxLimit Parse(string value)
        {
            switch (value)
            {
                case "40fps":
                    return FPSMaxLimit.FPS40;
                case "30fps":
                    return FPSMaxLimit.FPS30;
                case "20fps":
                    return FPSMaxLimit.FPS20;
                case "10fps":
                    return FPSMaxLimit.FPS10;
                default:
                    return FPSMaxLimit.FPS40;
            }
        }
    }
}
