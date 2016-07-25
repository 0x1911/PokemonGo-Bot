﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bhelper;
using PokemonGo.RocketAPI.GeneratedCode;

namespace bLogic
{
    public class Item
    {
        /// <summary>
        /// Check for hatchet eggs
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        public static async Task CheckEggsHatched(Hero hero)
        {
            try
            {
                var inventory = await hero.Client.GetInventory();
                var eggkmwalked = inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.EggIncubators.EggIncubator).ToArray();
                foreach (var v in eggkmwalked)
                    if (v != null)
                        if (v.TargetKmWalked > hero.TotalKmWalked)
                            bhelper.Main.ColoredConsoleWrite(ConsoleColor.DarkYellow, "One of your eggs is hatched");
            }
            catch (Exception) { }
        }

        public static string GetFriendlyItemsString(IEnumerable<FortSearchResponse.Types.ItemAward> items)
        {
            var enumerable = items as IList<FortSearchResponse.Types.ItemAward> ?? items.ToList();

            if (!enumerable.Any())
                return string.Empty;

            return enumerable.GroupBy(i => i.ItemId)
                    .Select(kvp => new { ItemName = kvp.Key.ToString().Substring(4), Amount = kvp.Sum(x => x.ItemCount) })
                    .Select(y => $"{y.Amount}x {y.ItemName}")
                    .Aggregate((a, b) => $"{a}, {b}");
        }
    }
}