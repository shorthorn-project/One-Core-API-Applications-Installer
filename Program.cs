using System;
using ocapps.sources;

namespace ocapps
{
    class Program
    {
        static void Main()
        {
            Logger.SetupLog("xp-apps");

#if DEBUG
            Logger.LogManager.Debug(
                $"Current architecture: {Utils.OsArchitecture} | Current OS: {Environment.OSVersion}");
            var args = Utils.GetCommandArgs()?.Length > 0
                ? string.Join(" ", Utils.GetCommandArgs())
                : "No additional arguments";
            Logger.LogManager.Debug($"Used command-line arguments: {args}");
#endif

            MainMenu.ParseArgs();
        }
    }
}