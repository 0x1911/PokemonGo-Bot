using System;
using bhelper.Classes;
using PokemonGo.RocketAPI.GeneratedCode;

namespace bhelper
{
    public class Game
    {
        /// <summary>
        /// Calculate pokemon perfection in percent
        /// </summary>
        /// <param name="poke"></param>
        /// <returns>30</returns>
        public static float CalculatePokemonPerfection(PokemonData poke)
        {
            return (((poke.IndividualAttack * 2 + poke.IndividualDefense + poke.IndividualStamina) / 60f) * 100f);
        }


        public static bool RefreshPokedexStatus(GetInventoryResponse inventoryResponse, GetPlayerResponse profileResponse, Hero hero)
        {


            return false;
        }

        /// <summary>
        /// Refresh Hero data with the current status of our backpack
        /// </summary>
        /// <param name="inventoryResponse"></param>
        /// <param name="profileResponse"></param>
        /// <param name="hero"></param>
        /// <returns>true if valid data has been filted and Hero got refreshed</returns>
        public static bool RefreshBackPackStatus(Hero hero, Profile profile, GetInventoryResponse inventory)
        {
            int currentItemCount = 1; // lets start with 1 as the Camera item is most likely not counted TODO: check if this is indeed the case!

            try
            {
                if (!inventory.Success)
                    return false;
                

                    foreach (var tmpItem in inventory.InventoryDelta.InventoryItems)
                    {
                        if (tmpItem.InventoryItemData.Item != null)
                        {
                            currentItemCount += tmpItem.InventoryItemData.Item.Count;
                        }
                    }

                    //did we end up with valid data?
                if (currentItemCount <= 1 || profile.ItemStorage <= 0)
                    return false;

                    //refresh our hero data
                    hero.Backpack.SlotsMax = profile.ItemStorage;
                    hero.Backpack.SlotsUsed = currentItemCount;

                return true;
            }
            catch (Exception crap)
            {
                bhelper.Main.ColoredConsoleWrite(ConsoleColor.Red, "Game.RefreshBackPackStatus Exception: " + crap.Message);
                return false;
            }
        }
        /// <summary>
        ///     returns xp needed to the next level
        /// </summary>
        /// <param name="level">15</param>
        /// <returns>15000</returns>
        public static int GetXpDiff(int level)
        {
            switch (level)
            {
                case 1:
                    return 0;
                case 2:
                    return 1000;
                case 3:
                    return 2000;
                case 4:
                    return 3000;
                case 5:
                    return 4000;
                case 6:
                    return 5000;
                case 7:
                    return 6000;
                case 8:
                    return 7000;
                case 9:
                    return 8000;
                case 10:
                    return 9000;
                case 11:
                    return 10000;
                case 12:
                    return 10000;
                case 13:
                    return 10000;
                case 14:
                    return 10000;
                case 15:
                    return 15000;
                case 16:
                    return 20000;
                case 17:
                    return 20000;
                case 18:
                    return 20000;
                case 19:
                    return 25000;
                case 20:
                    return 25000;
                case 21:
                    return 50000;
                case 22:
                    return 75000;
                case 23:
                    return 100000;
                case 24:
                    return 125000;
                case 25:
                    return 150000;
                case 26:
                    return 190000;
                case 27:
                    return 200000;
                case 28:
                    return 250000;
                case 29:
                    return 300000;
                case 30:
                    return 350000;
                case 31:
                    return 500000;
                case 32:
                    return 500000;
                case 33:
                    return 750000;
                case 34:
                    return 1000000;
                case 35:
                    return 1250000;
                case 36:
                    return 1500000;
                case 37:
                    return 2000000;
                case 38:
                    return 2500000;
                case 39:
                    return 1000000;
                case 40:
                    return 1000000;
            }
            return 0;
        }
    }
}