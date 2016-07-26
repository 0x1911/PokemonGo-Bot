using System;
using System.Linq;
using System.Threading.Tasks;
using AllEnum;
using bhelper;
using bhelper.Classes;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.GeneratedCode;

namespace bLogic
{ 
    /// <summary>
    /// Methods in here are in general printing some kind of information either to a text object or the console
    /// </summary>
    public static class Info
    {
        public static void AddToTotalExeperience(Hero hero, Int32 xp)
        {
            hero.TotalExperience += xp;
        }

        public static Int32 GetTotalExperience(Hero hero)
        {
            return hero.TotalExperience;
        }
        
        public static bool PrintStartUp(Hero hero, GetPlayerResponse profileResponse)
        {
            try
            {
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "+-------------- account info -----------------+");
                if (hero.ClientSettings.AuthType == AuthType.Ptc)
                {
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " PTC Name: " + hero.ClientSettings.PtcUsername);
                }
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " User Name: " + profileResponse.Profile.Username);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Team: " + profileResponse.Profile.Team);
                if (profileResponse.Profile.Currency.ToArray()[0].Amount > 0)
                    bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Pokecoins: " + profileResponse.Profile.Currency.ToArray()[0].Amount);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Stardust: " + profileResponse.Profile.Currency.ToArray()[1].Amount);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Registered since: " + hero.TimeStarted);
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Distance traveled: " + String.Format("{0:0.00} km", hero.TotalKmWalked));
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Latitude: " + String.Format("{0:0.00} degree", hero.ClientSettings.DefaultLatitude));
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, " Longitude: " + String.Format("{0:0.00} degree", hero.ClientSettings.DefaultLongitude));
            }
            catch (Exception crap)
            {
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "Info.StartUpPrint Exception: " + crap.Message);
                return false;
            }

            return true;
        }

        public static bool PrintInventory(GetInventoryResponse inventoryResponse, GetPlayerResponse profileResponse)
        {
            int currentItemCount = 1; // lets start with 1 as the Camera item is most likely not counted TODO: check if this is indeed the case!
            int pokemonOwned = 0;
            int eggsOwned = 0;

            try
            {
                if (inventoryResponse.Success)
                {
                    bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "+-------------- inventory info ---------------+");
                    foreach (var tmpItem in inventoryResponse.InventoryDelta.InventoryItems)
                    {
                        if (tmpItem.InventoryItemData.Item != null)
                        {
                            bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, "  " + tmpItem.InventoryItemData.Item.Count + "x " + tmpItem.InventoryItemData.Item.Item_.ToString());
                            currentItemCount += tmpItem.InventoryItemData.Item.Count;
                        }
                    }
                    bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, "  " + currentItemCount + "/ " + profileResponse.Profile.ItemStorage + " items in our backpack.");
                    foreach (var tmpItem in inventoryResponse.InventoryDelta.InventoryItems)
                    {
                        if (tmpItem.InventoryItemData.Pokemon != null)
                        {
                            if (tmpItem.InventoryItemData.Pokemon.IsEgg)
                            {
                                eggsOwned++;
                            }

                            pokemonOwned++;
                        }
                    }
                    bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkGray, "  We own " + pokemonOwned + " pokemon, " + eggsOwned + " of them are eggs. " + profileResponse.Profile.PokeStorage + " total pokemon storage");
                }
            }
            catch (Exception crap)
            {
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, "Info.StartUpPrint Exception: " + crap.Message);
                return false;
            }

            return true;
        }

        public static async Task PrintMostValueablePokemonsOwned(Hero hero)
        {
            var inventory = await hero.Client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            //clean up so we dont end up with dupes
            hero.OwnedPokemons.Clear();
            //readd our knowledge
            foreach (var tmpPokemon in pokemons)
            {
                bhelper.Classes.Pokemon tmpOwnedPokemon = new bhelper.Classes.Pokemon(tmpPokemon, Game.CalculatePokemonPerfection(tmpPokemon));

                hero.OwnedPokemons.Add(tmpOwnedPokemon);
            }
            //iterate through it
            var sortedList = hero.OwnedPokemons.OrderByDescending(q => q.PerfectionPercent).ToList();
            Console.WriteLine("+-----------------+-----------+---------------+");
            Console.WriteLine("| Name            | Perfect % | Combat Points |");
            Console.WriteLine("+-----------------+-----------+---------------+");
            foreach (var pokemon in sortedList)
            {
                Console.WriteLine("| {0} | {1} | {2} |", pokemon.Pokemondata.PokemonId.ToString().PadRight(15), String.Format("{0:0.00} %", pokemon.PerfectionPercent).PadRight(9), (pokemon.Pokemondata.Cp + " CP").ToString().PadRight(13));
            }
        }
        /// <summary>
        /// Print a level related event to RichTextBox or console log
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task PrintLevel(Hero hero, GetInventoryResponse inventory)
        {
            var stats = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.PlayerStats).ToArray();
            foreach (var v in stats)
                if (v != null)
                {
                    int XpDiff = bhelper.Game.GetXpDiff(v.Level);
                    if (hero.ClientSettings.LevelOutput == "time")
                        bhelper.Main.ColoredConsoleWrite(ConsoleColor.Yellow, $"[{DateTime.Now.ToString("HH:mm:ss")}] Current Level: " + v.Level + " (" + (v.Experience - XpDiff) + "/" + (v.NextLevelXp - XpDiff) + ")");
                    else if (hero.ClientSettings.LevelOutput == "levelup")
                        if (hero.Currentlevel != v.Level)
                        {
                            hero.Currentlevel = v.Level;
                            bhelper.Main.ColoredConsoleWrite(ConsoleColor.Magenta, $"[{DateTime.Now.ToString("HH:mm:ss")}] Current Level: " + v.Level + ". XP needed for next Level: " + (v.NextLevelXp - v.Experience));
                        }
                }
            if (hero.ClientSettings.LevelOutput == "levelup")
                await Task.Delay(1000);
            else
                await Task.Delay(hero.ClientSettings.LevelTimeInterval * 1000);
        }
    }
}