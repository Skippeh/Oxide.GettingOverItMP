using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GettingOverItMP.Updater.Exceptions;
using GettingOverItMP.Updater.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ShellProgressBar;

namespace GettingOverItMP.Updater
{
    internal static class Program
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private const int MaxRetries = 10;

        private static readonly ProgressBarOptions progressBarOptions = new ProgressBarOptions
        {
            CollapseWhenFinished = false,
            DisplayTimeInRealTime = false
        };

        private static int Main(string[] args)
        {
            ModType modType;
            bool launchGame = false;

            if (args.Length == 0)
            {
                if (File.Exists("GettingOverIt.exe") && Directory.Exists("GettingOverIt_Data"))
                {
                    Console.WriteLine("client/server not specified, but found client specific files. Updating client.");
                    modType = ModType.Client;
                }
                else
                {
                    Console.WriteLine("client/server not specified, could not find client specific files. Updating server.");
                    modType = ModType.Server;
                }
            }
            else
            {
                modType = args[0] == "server"
                    ? ModType.Server
                    : args[0] == "client"
                        ? ModType.Client
                        : ModType.Invalid;

                if (modType == ModType.Client && args.Length >= 2 && args[1] == "launch")
                {
                    launchGame = true;
                }
            }

            if (modType == ModType.Invalid)
            {
                Console.WriteLine("Invalid mod type specified. Please specify 'client' or 'server'.");
                return 2;
            }

            string currentModuleFileName = Directory.GetCurrentDirectory();

            if (modType == ModType.Client)
                currentModuleFileName = Path.Combine(currentModuleFileName, "GettingOverIt.exe");
            else if (modType == ModType.Server)
                currentModuleFileName = Path.Combine(currentModuleFileName, "GettingOverIt.Server.exe");

            var waitingProcesses = FindProcesses(currentModuleFileName, "GettingOverIt", "GettingOverIt.Server");

            if (waitingProcesses.Length > 0)
            {
                Console.WriteLine("Waiting for processes to exit...");

                foreach (var process in waitingProcesses)
                {
                    process.WaitForExit();
                }
            }

            if (!LocalData.Load())
            {
                Console.Error.WriteLine("Failed to load local version info. Press any key to close the updater.");
                Console.ReadKey(true);
                return 1;
            }

            using (var client = new ApiClient())
            {
                ModVersion latestVersion;

                try
                {
                    latestVersion = client.QueryLatestVersion(modType);
                }
                catch (Exception ex) when (ex is WebException || ex is ApiRequestFailedException)
                {
                    Console.Error.WriteLine($"Failed to query latest version: {ex.Message}. Press any key to close the updater.");
                    Console.ReadKey(true);
                    return 1;
                }

                int retries = 0;
                while (retries < MaxRetries)
                {
                    bool success = DownloadVersionAsync(latestVersion).Result;

                    if (success)
                    {
                        Console.WriteLine("Update downloaded successfully.");
                        Thread.Sleep(2000);
                        break;
                    }

                    ++retries;

                    if (retries < MaxRetries)
                    {
                        Console.Error.WriteLine($"Failed to download some files, retrying update in 3 seconds (attempt {retries}/{MaxRetries})...");
                        Thread.Sleep(3000);
                        Console.Clear();
                    }
                    else
                    {
                        Console.Error.WriteLine("Failed to download all files successfully, aborting update. Press any key to close the updater.");
                        Console.ReadKey(true);
                        return 1;
                    }
                }
            }

            if (launchGame)
            {
                Console.WriteLine("Launching game...");
                Process.Start("GettingOverIt.exe");
            }

            return 0;
        }

        private static Process[] FindProcesses(string currentModuleFileName, params string[] names)
        {
            var processes = new List<Process>();
            foreach (Process process in Process.GetProcessesByName("GettingOverIt").Concat(Process.GetProcessesByName("GettingOverIt.Server")))
            {
                try
                {
                    if (process.MainModule.FileName.ToLowerInvariant() == currentModuleFileName.ToLowerInvariant())
                        processes.Add(process);
                }
                catch (Exception ex)
                {
                    continue;
                }
            }

            return processes.ToArray();
        }

        private static async Task<bool> DownloadVersionAsync(ModVersion modVersion)
        {
            Console.WriteLine($"Downloading update: {modVersion.Version}...");

            List<string> filesToDeleteInScript = new List<string>();
            var currentVersion = LocalData.Version;

            if (currentVersion != null)
            {
                var filesToDelete = currentVersion.Checksums.Where(checksum => !modVersion.Checksums.Select(c => c.FilePath)
                                                            .Contains(checksum.FilePath))
                                                            .Select(checksum => checksum.FilePath)
                                                            .Where(File.Exists).ToList();

                if (filesToDelete.Count > 0)
                    Console.WriteLine($"Deleting {filesToDelete.Count} old file(s)...");

                var allDirectories = new List<string>();

                foreach (string filePath in filesToDelete)
                {
                    if (Utility.FileInUse(filePath))
                    {
                        filesToDeleteInScript.Add(filePath);
                        continue;
                    }

                    try
                    {
                        File.Delete(filePath);

                        string directory = Path.GetDirectoryName(filePath);

                        if (directory != string.Empty && !allDirectories.Contains(directory))
                        {
                            List<string> directories = GetDirectories(directory).ToList();
                            allDirectories.AddRange(directories.Where(d => !allDirectories.Contains(d)));
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.Error.WriteLine($"Failed to delete file: {filePath} ({ex.Message}");
                    }
                }

                foreach (string directory in allDirectories)
                {
                    try
                    {
                        if (Directory.Exists(directory) && Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length == 0)
                        {
                            Console.WriteLine($"Deleting empty directory: {directory}");
                            Directory.Delete(directory, true);
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.Error.WriteLine($"Failed to delete directory: {directory} ({ex.Message})");
                    }
                }
            }

            List<string> filesToDownload = modVersion.Checksums.Where(checksum => !checksum.MatchesExistingFile()).Select(checksum => checksum.FilePath).ToList();
            bool failed = false;

            if (filesToDownload.Count > 0)
            {
                List<string> filesToReplace = new List<string>();

                using (var progressBar = new ProgressBar(filesToDownload.Count, $"Downloading files", progressBarOptions))
                {
                    var runningTasks = new Task[filesToDownload.Count];

                    for (int i = 0; i < filesToDownload.Count; ++i)
                    {
                        string filePath = filesToDownload[i];
                        runningTasks[i] = DownloadFile(filePath, modVersion.Type, modVersion.Version, progressBar, filesToReplace, (success) =>
                        {
                            if (success)
                                progressBar.Tick();
                            else
                                failed = true;
                        });
                    }

                    await Task.WhenAll(runningTasks);
                }

                if (filesToReplace.Count > 0 || filesToDeleteInScript.Count > 0)
                {
                    var scriptGenerator = new ScriptGenerator();
                    scriptGenerator.WriteLine("Waiting for updater to exit...").SleepSeconds(1);
                    filesToReplace.ForEach(filePath => scriptGenerator.MoveFile(filePath + ".new", filePath));
                    filesToDeleteInScript.ForEach(filePath => scriptGenerator.DeleteFile(filePath));

                    string scriptFileName = scriptGenerator.GetFileName("update-finish");

                    File.WriteAllText("version.json.new", JsonConvert.SerializeObject(modVersion, Formatting.None, SerializerSettings));
                    scriptGenerator.MoveFile("version.json.new", "version.json");

                    scriptGenerator.DeleteFile(scriptFileName);
                    scriptGenerator.LaunchFile(Assembly.GetEntryAssembly().Location, string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
                    var script = scriptGenerator.Generate();
                    File.WriteAllText(scriptFileName, script);

                    Console.WriteLine("Exiting updater to replace files in use...");

                    await Task.Delay(1000);

                    Process.Start(scriptFileName);
                    Environment.Exit(3);
                }
            }

            if (failed)
                return false;

            File.WriteAllText("version.json", JsonConvert.SerializeObject(modVersion, Formatting.None, SerializerSettings));

            return true;
        }

        private static IEnumerable<string> GetDirectories(string directory)
        {
            string[] directories = directory.Split('/', '\\');

            string currentDirectory = directories[0];
            yield return currentDirectory;

            for (int i = 1; i < directories.Length; ++i)
            {
                currentDirectory += Path.DirectorySeparatorChar + directories[i];
                yield return currentDirectory;
            }
        }

        private static Task DownloadFile(string filePath, ModType modType, string version, ProgressBar parentProgressBar, List<string> filesToReplace, Action<bool> doneCallback)
        {
            return Task.Run(async () =>
            {
                using (var progressBar = parentProgressBar.Spawn(100, filePath))
                {
                    using (var apiClient = new ApiClient())
                    {
                        try
                        {
                            string targetFilePath = filePath;

                            if (Utility.FileInUse(targetFilePath))
                            {
                                targetFilePath += ".new";
                                filesToReplace.Add(filePath);
                            }

                            await apiClient.DownloadFileAsync(filePath, targetFilePath, modType, version, args =>
                            {
                                var headers = apiClient.WebClient.ResponseHeaders;
                                long fileLength = long.Parse(headers["X-File-Length"]);
                                int newTick = (int) (((double) args.BytesReceived / fileLength) * 100d);
                                progressBar.Tick(newTick);
                            });

                            doneCallback(true);
                        }
                        catch (Exception exception)
                        {
                            progressBar.Options.CollapseWhenFinished = false;
                            progressBar.Options.ForegroundColor = ConsoleColor.Red;
                            progressBar.Tick(progressBar.CurrentTick, $"Failed: {exception.Message} ({filePath})");
                            doneCallback(false);
                        }
                    }
                }
            });
        }
    }
}
