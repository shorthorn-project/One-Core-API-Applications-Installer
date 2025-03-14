using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ocapps.sources
{
    public abstract class CurlWrapper
    {
        private const int ProgressBarWidth = 30;
        private static readonly string Curl = GetCurl(Utils.GetCommandArgs());

        private static string GetArgValue(string[] args, string key)
        {
            var arg = args.FirstOrDefault(a => a.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            return arg?.Substring(key.Length + 1);
        }

        private static string GetCurl(string[] args)
        {
            // method 1 - search in program arguments
            var curlFromArgs = GetArgValue(args, "--curl");
            if (!string.IsNullOrEmpty(curlFromArgs))
            {
                Logger.LogManager.Info("Picked up curl from command-line arguments");
                return curlFromArgs;
            }

            // method 2 - search in environment variable
            var curlPath = Environment.GetEnvironmentVariable("CURL_PATH");

            if (string.IsNullOrEmpty(curlPath) || !File.Exists(curlPath))
                Logger.LogManager.Info(
                    "Could not find curl in the CURL_PATH environment variable or the file does not exist.");
            else
            {
                Logger.LogManager.Info("Picked up curl from the CURL_PATH environment");
                return curlPath;
            }

            // method 3 - search in curl/curl.exe folder
            if (Directory.Exists(Utils.WorkDir + "/curl"))
            {
                var curlFolderPath = Path.Combine(Utils.WorkDir, "curl", "curl.exe");

                if (File.Exists(curlFolderPath)) return curlFolderPath;
            }

            Console.WriteLine("Could not find curl. Exiting");
            Logger.LogManager.Info("Could not find curl. Exiting");
            Environment.Exit(0);

            return null;
        }

        public static string GetFileContent(string url)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Curl,
                Arguments = $"-Ls \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string content = null;
                string error = null;

                try
                {
                    content = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading process output: {ex.Message}");
                }

                process.WaitForExit();

                if (process.ExitCode == 0) return content;

                Console.WriteLine($"Error: {error}");
                return null;
            }
        }

        public static string GetFileSize(string url)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Curl,
                Arguments = $"-IL \"{url}\" --silent",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string content = null;
                string error = null;

                try
                {
                    content = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading process output: {ex.Message}");
                }

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Error: {error}");
                    return null;
                }

                var responses = content?.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                var lastResponse = responses?.LastOrDefault();

                var contentLengthLine = lastResponse?.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));

                var contentLength = contentLengthLine?.Split(':')[1].Trim();
                return contentLength;
            }
        }


        public static void DownloadFile(string url, string filename)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Curl,
                Arguments = $"-Lo \"{filename}\" \"{url}\" --progress-bar",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                var errorReader = process.StandardError;
                var outputReader = process.StandardOutput;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                string lastProgress = null;

                while (!process.HasExited)
                {
                    var line = errorReader.ReadLine();
                    if (line == null) continue;

                    var progress = ParseProgress(line);
                    if (progress >= 0 && lastProgress != line)
                    {
                        lastProgress = line;
                        DisplayProgress(progress, stopwatch);
                    }
                    else if (line.Contains("Could not resolve host"))
                    {
                        Console.WriteLine(
                            $"Could not download {filename}\nUnable to resolve host. Are you sure you entered the correct URL?");
                        Logger.LogManager.Info(
                            $"Could not download {filename}\n    -> Unable to resolve host. Are you sure you entered the correct URL?\n");
                    }
                    else if (line.Contains("timeout"))
                    {
                        Console.WriteLine($"Could not download {filename}\nRequest timed out. Is the website alive?\n");
                        Logger.LogManager.Info(
                            $"Could not download {filename}\n    -> Request timed out. Is the website alive?");
                    }
                }

                stopwatch.Stop();
                Thread.Sleep(1000);

                string completionLine = null;
                try
                {
                    completionLine = outputReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading completion output: {ex.Message}");
                }

                if (!string.IsNullOrEmpty(completionLine))
                {
                    Console.WriteLine($"\nError: {completionLine}\n");
                }

                Console.WriteLine($"\n\n{filename} download completed.\n");
            }
        }

        private static float ParseProgress(string line)
        {
            var match = Regex.Match(line, @"(\d+[\.,]?\d*)%\s*");

            if (match.Success && float.TryParse(match.Groups[1].Value.Replace(",", "."), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var progress)) return progress;

            return -1;
        }

        private static void DisplayProgress(float progress, Stopwatch stopwatch)
        {
            if (progress <= 0 || stopwatch.Elapsed.TotalSeconds <= 0) return;

            var speed = progress / stopwatch.Elapsed.TotalSeconds;
            var remainingPercent = 100 - progress;
            var remainingSeconds = remainingPercent / speed;
            var remainingTime = remainingSeconds < TimeSpan.MaxValue.TotalSeconds
                ? TimeSpan.FromSeconds(remainingSeconds)
                : TimeSpan.MaxValue;


            var progressChars = (int)(ProgressBarWidth * (progress / 100.0));
            var progressBar = new string('#', progressChars) + new string('-', ProgressBarWidth - progressChars);

            var progressText =
                $"\r[{progressBar}] {progress:0.00}% | {speed:0.00} MB/s | {remainingTime:hh\\:mm\\:ss} remaining";

            Console.Write(progressText);
        }
    }
}