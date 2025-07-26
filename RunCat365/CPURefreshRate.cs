using System.Diagnostics.CodeAnalysis;

namespace RunCat365
{
    enum CPURefreshRate
    {
        REFRESH1,
        REFRESH2,
        REFRESH3,
        REFRESH4,
        REFRESH5,
        REFRESH10,
    }
    internal static class CPURefreshRateExtension
    {
        internal static string GetString(this CPURefreshRate refreshRate)
        {
            return refreshRate switch
            {
                CPURefreshRate.REFRESH1 => "1 sec",
                CPURefreshRate.REFRESH2 => "2 secs",
                CPURefreshRate.REFRESH3 => "3 secs",
                CPURefreshRate.REFRESH4 => "4 secs",
                CPURefreshRate.REFRESH5 => "5 secs",
                CPURefreshRate.REFRESH10 => "10 secs",
                _ => "",
            };
        }

        internal static int GetRate(this CPURefreshRate refreshRate)
        {
            return refreshRate switch
            {
                CPURefreshRate.REFRESH1 => 1,
                CPURefreshRate.REFRESH2 => 2,
                CPURefreshRate.REFRESH3 => 3,
                CPURefreshRate.REFRESH4 => 4,
                CPURefreshRate.REFRESH5 => 5,
                CPURefreshRate.REFRESH10 => 10,
                _ => 1,
            };
        }

        internal static bool TryParse([NotNullWhen(true)] string? value, out CPURefreshRate result)
        {
            CPURefreshRate? nullableResult = value switch
            {
                "1 sec" => CPURefreshRate.REFRESH1,
                "2 secs" => CPURefreshRate.REFRESH2,
                "3 secs" => CPURefreshRate.REFRESH3,
                "4 secs" => CPURefreshRate.REFRESH4,
                "5 secs" => CPURefreshRate.REFRESH5,
                "10 secs" => CPURefreshRate.REFRESH10,
                _ => null,
            };

            if (nullableResult is CPURefreshRate nonNullableResult)
            {
                result = nonNullableResult;
                return true;
            }
            else
            {
                result = CPURefreshRate.REFRESH5;
                return false;
            }
        }

    }
}