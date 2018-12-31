using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace RPCS3Updater
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //only execute in RPCS3 Folder
            String currentDirectory = System.IO.Directory.GetCurrentDirectory();
            String[] currentFileNames = System.IO.Directory.GetFiles(currentDirectory);
            Boolean installed = false;
            Boolean updateNeeded = false;
            String[] nodashArgs = new String[args.Length];
            long currentVerInt = 0;
            Boolean updateArg = false;
            Boolean noLaunch = false;
            


            String newVersion = getDownloadVersion();
            String downloadURL = getDownloadUrl();

            long newVerInt = Convert.ToInt64(newVersion.Replace(".", "").Replace("-", ""));

            for (int i = 0; i < args.Length; i++)
            {
                nodashArgs[i] = args[i].Replace("-", "");
            }

            for (int i = 0; i < nodashArgs.Length; i++)
            {
                String sanitizedArg = nodashArgs[i].ToLower().Trim();
                switch (sanitizedArg)
                {
                    case "y":
                        updateArg = true;
                        break;
                    case "nolaunch":
                        noLaunch = true;
                        break;
                    case "h":
                        Console.WriteLine("Usage: -y to autoupdate, -h to print this statement," +
                                          " -nolaunch to disable autolaunching behavior when an update isn't needed");
                        Environment.Exit(0);
                        break;
                    default:
                        Environment.Exit(1);
                        break;
                }
            }

            //for loop ----> LINQ expression
            if (currentFileNames.Any(currentFileName => currentFileName.Contains("rpcs3.exe")))
            {
                installed = true;
                if (File.Exists("RPCS3.log"))
                {
                    //read log file to get current version information
                    String currentVersion = File.ReadLines("RPCS3.log").First().Trim();
                    int startIndex = currentVersion.IndexOf("v");
                    int lastDashIndex = currentVersion.LastIndexOf("-");
                    currentVersion = currentVersion.Substring(startIndex + 1, (lastDashIndex - startIndex) - 1);

                    currentVerInt = Convert.ToInt64(currentVersion.Replace(".", "").Replace("-", ""));

                    Console.WriteLine("Current Version: " + currentVersion);
                    Console.WriteLine("New Version: " + newVersion);
                }
                else
                {
                    Console.WriteLine("Current Version Unknown");
                    Console.WriteLine("New Version: " + newVersion);
                    currentVerInt = -1;
                }
            }

            if (!installed)
            {
                Console.WriteLine("RPCS3 Not detected. Run from inside RPCS3's directory");
                Environment.Exit(0);
            }

            if (currentVerInt < newVerInt)
            {
                String ans = "";
                if (!updateArg)
                {
                    //only prompt user if -y wasn't specified
                    Console.WriteLine("Would you like to update? \ny or n");
                    ans = Console.ReadLine();
                }

                if (ans.Contains("y") || updateArg)
                {
                    updateNeeded = true;
                }
            }


            if (updateNeeded)
            {
                //do dev update
                String fileName = newVersion + ".7z";
                doUpdate(currentDirectory, fileName, downloadURL);
            }
            else
            {
                Console.WriteLine("No update needed!");
            }

            if (!noLaunch || (currentVerInt < newVerInt))
            {
                Console.WriteLine("Starting RPCS3...");
                //to force log file to be updated and launch
                ProcessStartInfo rpcs3 = new ProcessStartInfo(@"rpcs3.exe");
                Process.Start(rpcs3);
            }

            Environment.Exit(0);
        }

        private static string getDownloadUrl()
        {
            WebClient githubAPI = new WebClient();
            githubAPI.Headers.Add ("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            Uri githubLink = new Uri("https://api.github.com/repos/rpcs3/rpcs3-binaries-win/releases/latest");
            Stream stream = githubAPI.OpenRead(githubLink);
            StreamReader reader = new StreamReader(stream);
            String response = reader.ReadToEnd();

            JObject githubResponse = JObject.Parse(response);
            String downloadURL = (string) githubResponse["assets"][0]["browser_download_url"];
            return downloadURL.Trim().Normalize();
        }

        private static string getDownloadVersion()
        {
            WebClient githubAPI = new WebClient();
            githubAPI.Headers.Add ("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            Uri githubLink = new Uri("https://api.github.com/repos/rpcs3/rpcs3-binaries-win/releases/latest");
            Stream stream = githubAPI.OpenRead(githubLink);
            StreamReader reader = new StreamReader(stream);
            String response = reader.ReadToEnd();

            JObject githubResponse = JObject.Parse(response);
            String downloadVersion = (string) githubResponse["name"];
            return downloadVersion.Trim().Normalize();
        }

        private static void doUpdate(String currentDirectory, String fileName, String downloadURL)
        {
            WebClient clientDL = new WebClient();
            String file = currentDirectory + "/" + fileName;
            //file to download to (install directory + file name extracted from html)
            Console.WriteLine("Downloading...");
            clientDL.DownloadFile(downloadURL, file);


            //extract and overwrite
            Console.WriteLine("Extracting...");
            using (var archive = SevenZipArchive.Open(file))
            {
                foreach (var entry in archive.Entries)
                {
                    entry.WriteToDirectory(currentDirectory, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
            }

            File.Delete(file);
            Console.WriteLine("Done!");
        }
    }
}