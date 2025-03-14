using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;

namespace ocapps.sources
{
    public class Utils
    {
        public static readonly string OsArchitecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";

        public static readonly string WorkDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static readonly string CurrentFile = AppDomain.CurrentDomain.FriendlyName;

        public static string ExtractFileNameFromUrl(string url) =>
            url.Substring(url.LastIndexOf('/') + 1);

        public static string[] GetCommandArgs()
        {
            return Environment.GetCommandLineArgs().Skip(1).ToArray();
        }

        public static bool IsNetworkAvailable()
        {
            var ping = new Ping();
            var reply = ping.Send("www.google.com", 5000);

            return reply != null && reply.Status == IPStatus.Success;
        }
    }
}