using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AllEnum;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;
using System.Net.Http;
using System.Text;
using Google.Protobuf;
using PokemonGo.RocketAPI.Helpers;
using System.IO;
using bhelper;


namespace PokemonGo.RocketAPI.GUI
{
    internal class Program
    {
        public static bhelper.Hero _hero;
        
        public static async void Execute()
        {
            if (!_hero.AllowedToRun)
            {
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "Stopping bot cyclus now!");
                return;
            }

            try
            {
                bhelper.Main.CheckVersion(Assembly.GetExecutingAssembly().GetName());
                if (_hero.ClientSettings.AuthType == AuthType.Ptc)
                    await _hero.Client.DoPtcLogin(_hero.ClientSettings.PtcUsername, _hero.ClientSettings.PtcPassword);
                else if (_hero.ClientSettings.AuthType == AuthType.Google)
                    await _hero.Client.DoGoogleLogin();

                await _hero.Client.SetServer();
                var profile = await _hero.Client.GetProfile();
                var settings = await _hero.Client.GetSettings();
                var mapObjects = await _hero.Client.GetMapObjects();
                var inventory = await _hero.Client.GetInventory();
                var pokemons =
                    inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                        .Where(p => p != null && p?.PokemonId > 0);
                var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData.PlayerStats).ToArray();
                foreach (var v in stats)
                    if (v != null)
                        _hero.TotalKmWalked = v.KmWalked;

                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "+-------------- account info ---------------+");
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Account Name: " + _hero.ClientSettings.PtcUsername);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Hero Name: " + profile.Profile.Username);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Team: " + profile.Profile.Team);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Stardust: " + profile.Profile.Currency.ToArray()[1].Amount);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " distance traveled: " + String.Format("{0:0.00} km", _hero.TotalKmWalked));
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Latitude: " + _hero.ClientSettings.DefaultLatitude);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Longitude: " + _hero.ClientSettings.DefaultLongitude);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "+--------------------------------------------+");
                if (_hero.ClientSettings.TransferType == "leaveStrongest")
                    await bLogic.Pokemon.TransferAllButStrongestUnwantedPokemon(_hero);
                else if (_hero.ClientSettings.TransferType == "all")
                    await bLogic.Pokemon.TransferAllGivenPokemons(_hero, pokemons);
                else if (_hero.ClientSettings.TransferType == "duplicate")
                    await bLogic.Pokemon.TransferDuplicatePokemon(_hero);
                else if (_hero.ClientSettings.TransferType == "cp")
                    await bLogic.Pokemon.TransferAllWeakPokemon(_hero);
                else
                    bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, $"[{DateTime.Now.ToString("HH:mm:ss")}] Transfering pokemon disabled");
                if (_hero.ClientSettings.EvolveAllGivenPokemons)
                    await bLogic.Pokemon.EvolveAllGivenPokemons(_hero, pokemons);
                if (_hero.ClientSettings.Recycler)
                    _hero.Client.RecycleItems(_hero.Client);
                
                await Task.Delay(5000);

                PrintLevel(_hero.Client);
                UpdateFormTitle(_hero.Client);


                if (_hero.ClientSettings.EggHatchedOutput)
                    await bLogic.Pokemon.CheckEggsHatched(_hero);
                if (_hero.ClientSettings.UseLuckyEggMode == "always")
                    await _hero.Client.UseLuckyEgg(_hero.Client);

                await bLogic.Pokemon.ExecuteFarmingPokestopsAndPokemons(_hero);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] No nearby usefull locations found. Please wait 10 seconds.");
                await Task.Delay(10000);
                Execute();
            }
            catch (TaskCanceledException) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.White, "Task Canceled Exception - Restarting"); Execute(); }
            catch (UriFormatException) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.White, "System URI Format Exception - Restarting"); Execute(); }
            catch (ArgumentOutOfRangeException) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.White, "ArgumentOutOfRangeException - Restarting"); Execute(); }
            catch (ArgumentNullException) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.White, "Argument Null Refference - Restarting"); Execute(); }
            catch (NullReferenceException) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.White, "Null Refference - Restarting"); Execute(); }
            catch (AccountNotVerifiedException) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.Red, "ACCOUNT NOT VERIFIED - WONT WORK"); Execute(); }
            catch (Exception ex) { bhelper.Main.ColoredConsoleWrite(ConsoleColor.White, ex.ToString()); Execute(); }
        }

        public static async Task UpdateFormTitle(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            var profile = await client.GetProfile();
            foreach (var playerStatistic in stats)
                if (playerStatistic != null)
                {
                    MainWindow.main.SetMainFormTitle = string.Format(_hero.ClientSettings.PtcUsername + " :: L{0:0} | {1:0} exp/h | {2:0} pok/h", playerStatistic.Level, Math.Round(_hero.TotalExperience / bhelper.Main.GetRuntime(_hero.TimeStarted)), Math.Round(_hero.TotalPokemon / bhelper.Main.GetRuntime(_hero.TimeStarted)));
                    
                }
            await Task.Delay(1000);
        }
        

        public static async Task PrintLevel(Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = bhelper.Game.GetXpDiff(v.Level);
                    if (_hero.ClientSettings.LevelOutput == "time")
                        bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] Current Level: " + v.Level + " (" +
                            (v.Experience - XpDiff) + "/" + (v.NextLevelXp - XpDiff) + ")");
                    else if (_hero.ClientSettings.LevelOutput == "levelup")
                        if (_hero.Currentlevel != v.Level)
                        {
                            _hero.Currentlevel = v.Level;
                            bhelper.Main.ColoredConsoleWrite(ConsoleColor.Magenta,
                                $"[{DateTime.Now.ToString("HH:mm:ss")}] Current Level: " + v.Level +
                                ". XP needed for next Level: " + (v.NextLevelXp - v.Experience));
                        }
                }
            if (_hero.ClientSettings.LevelOutput == "levelup")
                await Task.Delay(1000);
            else
                await Task.Delay(_hero.ClientSettings.LevelTimeInterval*1000);

            PrintLevel(client);
        }
        

        public static async Task ConsoleLevelTitle(string Username, Client client)
        {
            var inventory = await client.GetInventory();
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            var profile = await client.GetProfile();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = bhelper.Game.GetXpDiff(v.Level);
                    System.Console.Title = string.Format(Username + " | Level: {0:0} - ({1:0} / {2:0}) | Stardust: {3:0}", v.Level, (v.Experience - v.PrevLevelXp - XpDiff), (v.NextLevelXp - v.PrevLevelXp - XpDiff), profile.Profile.Currency.ToArray()[1].Amount) + " | XP/Hour: " + Math.Round(_hero.TotalExperience / bhelper.Main.GetRuntime(_hero.TimeStarted)) + " | Pokemon/Hour: " + Math.Round(_hero.TotalPokemon / bhelper.Main.GetRuntime(_hero.TimeStarted));
                }
            await Task.Delay(1000);
            ConsoleLevelTitle(Username, client);
        }
        
    }
}
