using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ocapps.sources;
using YamlDotNet.Serialization;
using ocapps.Structures;

namespace ocapps
{
    public static class Applications
    {
        private static Application.RootObject ProgramsList { get; set; }

        private const string ApplicationDb =
            "https://raw.githubusercontent.com/shorthorn-project/One-Core-API-Applications-Installer/main/apps.yml";

        private const string LocalYamlFile = "apps.yaml";


        private static Dictionary<string, List<Dictionary<string, Application.AppItem>>> Categories =>
            new Dictionary<string, List<Dictionary<string, Application.AppItem>>>
            {
                { "browsers", ProgramsList.browsers },
                { "vista_apps", ProgramsList.VistaApps },
                { "windows_7_apps", ProgramsList.Windows7Apps },
                { "Codec_video_audio", ProgramsList.CodecVideoAudio },
                { "Utilities", ProgramsList.Utilities },
                { "Other", ProgramsList.Other },
                { "Office", ProgramsList.Office },
                { "Programming", ProgramsList.Programming }
            };

        static Applications()
        {
            if (Utils.IsNetworkAvailable())
            {
                EnsureLocalYamlFile();
            }
            else if (!File.Exists(LocalYamlFile))
            {
                Logger.LogManager.Info("No internet connection and local apps.yaml not found.");
                return;
            }

            var yamlContent = File.ReadAllText(LocalYamlFile);
            var deserializer = new DeserializerBuilder().Build();
            ProgramsList = deserializer.Deserialize<Application.RootObject>(yamlContent);
        }

        private static void EnsureLocalYamlFile()
        {
            if (!File.Exists(LocalYamlFile))
            {
                Logger.LogManager.Info("Local apps.yaml not found. Downloading...");
                CurlWrapper.DownloadFile(ApplicationDb, LocalYamlFile);
            }
            else if (Utils.IsNetworkAvailable())
            {
                var remoteSizeStr = CurlWrapper.GetFileSize(ApplicationDb);
                var remoteSize = long.Parse(remoteSizeStr);
                var localSize = new FileInfo(LocalYamlFile).Length;

                if (remoteSize == localSize) return;

                Console.WriteLine("A new version of apps.yaml is available. Downloading update...");
                CurlWrapper.DownloadFile(ApplicationDb, LocalYamlFile);
            }
            else
            {
                Logger.LogManager.Info("No internet connection available, aborting.");
                Environment.Exit(0);
            }
        }

        private static IEnumerable<KeyValuePair<string, Application.AppItem>> GetProgramDetails(
            List<Dictionary<string, Application.AppItem>> category)
        {
            return category.SelectMany(dict => dict);
        }

        private static Application.AppItem FindApplication(string appName)
        {
            return Categories.Select(category =>
                    FindApplicationInCategory(appName, category.Value, category.Key))
                .FirstOrDefault(found => found != null);
        }

        private static Application.AppItem FindApplicationInCategory(string appName,
            List<Dictionary<string, Application.AppItem>> category, string _)
        {
            foreach (var kv in GetProgramDetails(category))
            {
                var programName = kv.Key;
                var programDetails = kv.Value;
                if (programName.Equals(appName, StringComparison.OrdinalIgnoreCase))
                    return programDetails;

                if (programDetails.aliases != null &&
                    programDetails.aliases.Any(alias => alias.Equals(appName, StringComparison.OrdinalIgnoreCase)))
                    return programDetails;
            }

            return null;
        }

        private static List<string> FindSimilarApplications(string appName)
        {
            var allApps = Categories.Values
                .SelectMany(GetProgramDetails)
                .SelectMany(pd => new List<string> { pd.Key }
                    .Concat(pd.Value.aliases ?? new List<string>()))
                .ToList();

            var threshold = Math.Max(appName.Length / 2, 2);

            return allApps
                .Where(name => LevenshteinDistance(appName, name) <= threshold ||
                               name.IndexOf(appName, StringComparison.OrdinalIgnoreCase) >= 0)
                .Distinct()
                .OrderBy(name => LevenshteinDistance(appName, name))
                .ThenBy(name => name)
                .Take(5)
                .ToList();
        }

        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length, m = t.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++)
                d[i, 0] = i;
            for (var j = 0; j <= m; j++)
                d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + (s[i - 1] == t[j - 1] ? 0 : 1)
                    );
                }
            }

            return d[n, m];
        }

        public static void InstallApplication(string appName, bool isForce)
        {
            Console.Clear();

            var applicationDetails = FindApplication(appName);
            if (applicationDetails == null)
            {
                Console.Write($"Could not find application {appName}.");

                var similarApps = FindSimilarApplications(appName);
                if (similarApps.Any())
                    Console.WriteLine($" Did you mean: {string.Join(", ", similarApps.Distinct())}?");
                return;
            }

            var filename = applicationDetails.filename;
            var url = applicationDetails.url;

            Logger.LogManager.Info($"Found application {appName}");

            var filesize = Convert.ToInt64(CurlWrapper.GetFileSize(url));
            var downloadedIn = Path.Combine(Utils.WorkDir, filename);
            Console.WriteLine(
                $"Application name: {applicationDetails.name}\nSize: {filesize / (1024 * 1024)} MB" +
                $"\nFilename: {applicationDetails.filename}\nWill be downloaded in {downloadedIn}");

            if (File.Exists(filename) && isForce)
                File.Delete(filename);

            if (filename != null && File.Exists(filename) && new FileInfo(filename).Length == filesize)
            {
                Console.WriteLine(
                    "File already exists and is not corrupted. Skipping download." +
                    $"\nIf you want force download, use 'xp-apps.exe --install {appName} --force'"
                );
                return;
            }

            CurlWrapper.DownloadFile(url, downloadedIn);
        }
    }
}