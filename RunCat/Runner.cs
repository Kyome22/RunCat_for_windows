namespace RunCat
{
    enum Runner
    {
        Cat,
        Parrot,
        Horse,
    }

    static class RunnerExtensions
    {
        internal static string GetString(this Runner runner)
        {
            switch (runner)
            {
                case Runner.Cat:
                    return "Cat";
                case Runner.Parrot:
                    return "Parrot";
                case Runner.Horse:
                    return "Horse";
                default:
                    return "";
            }
        }

        internal static int GetFrameNumber(this Runner runner)
        {
            switch (runner)
            {
                case Runner.Cat:
                    return 5;
                case Runner.Parrot:
                    return 10;
                case Runner.Horse:
                    return 14;
                default:
                    return 0;
            }
        }
    }
}
