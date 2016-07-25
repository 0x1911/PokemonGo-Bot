﻿using System;
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
        public static bool CheckVersion(AssemblyName localAssembly)
        {
            try
            {
                var downloadedVersionRegex =
                    new Regex( @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]")
                        .Match(GetMainVersion());

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
            catch (Exception)
            {
                ColoredConsoleWrite(ConsoleColor.Red, "Unable to check for updates now...");
                return false;
            }

            return false;
        }

        private static string GetMainVersion()
        {
            using (var wC = new WebClient())
                return
                    wC.DownloadString(
                        "https://raw.githubusercontent.com/Sen66/PokemonGo-Bot/master/PokemonGo/RocketAPI/Console/Properties/AssemblyInfo.cs");
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
