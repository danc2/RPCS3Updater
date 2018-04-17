using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
            String appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            String confFolder = "RPCS3Updater";
            String confFile = "options.conf";
            String configPath = appdata + "/" + confFolder + "/" + confFile;
            String[] configOptions;
            String currentDirectory = System.IO.Directory.GetCurrentDirectory();
            String[] files = System.IO.Directory.GetFiles(currentDirectory);
            Boolean installed = false;
            Boolean updateNeeded = false;
            String currentVersion;

            long currentVerInt = 0;
            long newVerInt;

            String downloadUrl = getDownloadUrl();
            int firstDash = downloadUrl.IndexOf("-");
            int lastDash = downloadUrl.LastIndexOf("-");

            String newVersion = downloadUrl.Substring(firstDash + 2, (lastDash - firstDash) - 2);

            newVerInt = Convert.ToInt64(newVersion.Replace(".", "").Replace("-", ""));

            //+2 to get rid of dash and 'v'


            Console.WriteLine("DEBUG:\n");
            Console.WriteLine("configPath: " + configPath);
            Console.WriteLine("currentDirectory: " + currentDirectory);

            /*Console.WriteLine("files: \n");
            //printing files in dir
            foreach(var item in files)
            {
                Console.WriteLine(item.ToString());
            }
            */


            foreach (var t in files)
            {
                if (t.Contains("rpcs3.exe"))
                {
                    installed = true;
                    if (File.Exists("RPCS3.log"))
                    {
                        currentVersion = File.ReadLines("RPCS3.log").First().Trim();
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

                    break;
                }
            }

            if (!installed)
            {
                Console.WriteLine("RPCS3 Not detected. Run from inside RPCS3's directory");
                Environment.Exit(0);
            }

            if (currentVerInt < newVerInt)
            {
                Console.WriteLine("Would you like to update? \ny or n");
                String ans=Console.ReadLine();
                if (ans.Contains("y"))
                {
                    updateNeeded = true;
                }
            }

            if (!File.Exists((configPath)))
            {
                //File.Create(configPath);
                //file not found
                Directory.CreateDirectory(appdata + "/" + confFolder);
                Console.WriteLine("You need to configure RPCS3 Updater.");
                Console.WriteLine("Would you like to use development versions of RPCS3?\n y or n");
                Console.WriteLine("Note: this currently doesn't apply");
                String[] writeMe = new String[2];
                String ans = Console.ReadLine();

                if (ans.Equals("y"))
                {
                    writeMe[0] = "dev=true";
                }
                else
                {
                    writeMe[0] = "dev=false";
                }

                File.WriteAllLines(configPath, writeMe);
            }
            else
            {
                configOptions = File.ReadAllLines(configPath);
                if (configOptions[0].Equals("dev=true") && updateNeeded)
                {
                    //do dev update
                    doUpdate(currentDirectory, downloadUrl);
                }
                else if (updateNeeded)
                {
                    doUpdate(currentDirectory, downloadUrl);
                    //do regular update
                }
                else
                {
                    Console.WriteLine("No update needed!");
                    Environment.Exit(0);
                }
            }
        }

        public static String getDownloadUrl()
        {
            var handler = new System.Net.Http.HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var client = new System.Net.Http.HttpClient(handler);

            client.BaseAddress = new Uri("https://rpcs3.net/download");
            System.Net.Http.HttpResponseMessage response = client.GetAsync("").Result;
            response.EnsureSuccessStatusCode();
            string result = response.Content.ReadAsStringAsync().Result;
            //Console.WriteLine(result);

            int start = result.IndexOf("https://ci.");
            int end = result.IndexOf("7z");

            result = result.Substring(start, (end - start) + 2);
            return result;
        }


        public static void doUpdate(String currentDirectory, String result)
        {
            int start = result.IndexOf("https://ci.");
            int end = result.IndexOf("7z");

            result = result.Substring(start, (end - start) + 2);
            WebClient clientDL = new WebClient();

            int s1 = result.IndexOf("rpcs3");
            int e1 = result.IndexOf("7z");

            String file = currentDirectory + "/" + result.Substring(s1, (e1 - s1) + 2);
            //file to download to (install directory + file name extracted from html)
            clientDL.DownloadFile(result, file);
            Console.WriteLine("Downloading...");


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
           Console.WriteLine("Starting RPCS3...");
            
           ProcessStartInfo rpcs3 = new ProcessStartInfo(@"rpcs3.exe");
           Process.Start(rpcs3);
           
           System.IO.File.Delete(file);
           Console.WriteLine("Done!");
        }
    }
}