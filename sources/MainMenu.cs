using System;

namespace ocapps.sources
{
    public class MainMenu
    {

        public static void ParseArgs()
        {
            var args = Utils.GetCommandArgs();

            if (args.Length == 0)
            {
                Console.WriteLine("TBA...");
                return;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                switch (arg)
                {
                    case "-i":
                    case "--install":
                    {
                        if (i + 1 < args.Length)
                        {
                            var appName = args[i + 1];
                            var force = i + 2 < args.Length && args[i + 2].Equals("--force");
                            Applications.InstallApplication(appName, force);
                        }
                        else
                        {
                            Console.WriteLine("Error: Missing application name for install.");
                        }

                        return;
                    }
                    case "-h":
                    case "--help":
                        // Console.WriteLine(Help);
                        Console.WriteLine("TBA...");
                        return;
                    // case "-l":
                    // case "--list":
                    // case "--list-applications":
                    // case "--list-apps":
                    // case "--apps":
                        // Applications.GetApplications(Categories);
                        // return;
                    // case "--self-update":
                        // Updater.Update();
                        // return;
                }
            }
        }
    }
}