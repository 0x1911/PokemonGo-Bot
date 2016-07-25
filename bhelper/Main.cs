using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace bhelper
{
    public static class Main
    {
        public static bool CheckVersion(AssemblyName localAssembly, bool isConsoleVersion)
        {
            try
            {
                var downloadedVersionRegex =
                    new Regex( @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]")
                        .Match(GetMainVersion(isConsoleVersion));

                if (!downloadedVersionRegex.Success)
                    return false;

                var cleanedServerAssemblyVersion =
                    new Version(
                        string.Format(
                            "{0}.{1}.{2}.{3}",
                            downloadedVersionRegex.Groups[1],
                            downloadedVersionRegex.Groups[2],
                            downloadedVersionRegex.Groups[3],
                            downloadedVersionRegex.Groups[4]));

                if (cleanedServerAssemblyVersion <= localAssembly.Version)
                {
                    //ColoredConsoleWrite(ConsoleColor.Yellow, "Awesome! You have already got the newest version! " + Assembly.GetExecutingAssembly().GetName().Version);
                    return true;
                }

                ColoredConsoleWrite(ConsoleColor.Red, "There is a new Version available: " + cleanedServerAssemblyVersion);
            }
            catch (Exception crap)
            {
                ColoredConsoleWrite(ConsoleColor.Red, "Unable to check for updates: " + crap.Message);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Get the github version string
        /// either for the Graphical or Console Client
        /// </summary>
        /// <param name="isConsoleVersion"></param>
        /// <returns></returns>
        private static string GetMainVersion(bool isConsoleVersion)
        {
            using (var wC = new WebClient())
            {
                if (isConsoleVersion)
                {
                    return
                        wC.DownloadString(
                            "https://raw.githubusercontent.com/Sen66/PokemonGo-Bot/master/ConsoleClient/Properties/AssemblyInfo.cs");
                }
                else
                {
                    return
                        wC.DownloadString(
                            "https://raw.githubusercontent.com/Sen66/PokemonGo-Bot/master/GraphicalClient/Properties/AssemblyInfo.cs");
                }
            }
        }

        public static void ColoredConsoleWrite(ConsoleColor color, string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
            File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + @"\Logs.txt", text + Environment.NewLine);
        }


        public static double GetRuntime(DateTime timeStarted)
        {
            return ((DateTime.Now - timeStarted).TotalSeconds) / 3600;
        }

    }
}
