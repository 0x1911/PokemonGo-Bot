using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AllEnum;
using bhelper;
using bhelper.Classes;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.GeneratedCode;

namespace bLogic
{
    public class Pokemon
    {
        /// <summary>
        /// iterating through a given list of pokemon we want to evolve
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="pokemonToEvolve"></param>
        /// <returns></returns>

        public static int TotalExperience = 0;
        public static int TotalPokemon = 0;

        

        


        public static async Task EvolveAllGivenPokemons(Hero hero, IEnumerable<PokemonData> pokemonToEvolve)
        {
            foreach (var pokemon in pokemonToEvolve)
            {
                /*
                enum Holoholo.Rpc.Types.EvolvePokemonOutProto.Result {
	                UNSET = 0;
	                SUCCESS = 1;
	                FAILED_POKEMON_MISSING = 2;
	                FAILED_INSUFFICIENT_RESOURCES = 3;
	                FAILED_POKEMON_CANNOT_EVOLVE = 4;
	                FAILED_POKEMON_IS_DEPLOYED = 5;
                }
                }*/

                var countOfEvolvedUnits = 0;
                var xpCount = 0;

                EvolvePokemonOut evolvePokemonOutProto;
                do
                {
                    evolvePokemonOutProto = await hero.Client.EvolvePokemon(pokemon.Id);
                    //todo: someone check whether this still works

                    if (evolvePokemonOutProto.Result == 1)
                    {
                        Main.ColoredConsoleWrite(ConsoleColor.Cyan,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] Evolved {pokemon.PokemonId} successfully for {evolvePokemonOutProto.ExpAwarded}xp");

                        countOfEvolvedUnits++;
                        xpCount += evolvePokemonOutProto.ExpAwarded;
                    }
                    else
                    {
                        var result = evolvePokemonOutProto.Result;
                        /*
                        ColoredConsoleWrite(ConsoleColor.White, $"Failed to evolve {pokemon.PokemonId}. " +
                                                 $"EvolvePokemonOutProto.Result was {result}");

                        ColoredConsoleWrite(ConsoleColor.White, $"Due to above error, stopping evolving {pokemon.PokemonId}");
                        */
                    }
                } while (evolvePokemonOutProto.Result == 1);
                if (countOfEvolvedUnits > 0)
                    Main.ColoredConsoleWrite(ConsoleColor.Cyan,
                        $"[{DateTime.Now.ToString("HH:mm:ss")}] Evolved {countOfEvolvedUnits} pieces of {pokemon.PokemonId} for {xpCount}xp");

                await Task.Delay(3000);
            }
        }

        

        /// <summary>
        /// Catch all nearby pokemon
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        public static async Task ExecuteCatchAllNearbyPokemons(Hero hero)
        {
            var mapObjects = await hero.Client.GetMapObjects();
            
            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons);

            var inventory2 = await hero.Client.GetInventory();
            var pokemons2 = inventory2.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            PokemonId[] CatchOnlyThesePokemon = new[]
            {
                PokemonId.Rattata
            };

            foreach (var pokemon in pokemons)
            {
                string pokemonName;
                if (hero.ClientSettings.Language == "german")
                    pokemonName = Convert.ToString((PokemonId_german)(int)pokemon.PokemonId);
                else
                    pokemonName = Convert.ToString(pokemon.PokemonId);

                if (!CatchOnlyThesePokemon.Contains(pokemon.PokemonId) && hero.ClientSettings.CatchOnlySpecific)
                {
                    Main.ColoredConsoleWrite(ConsoleColor.DarkYellow, $"[{DateTime.Now.ToString("HH:mm:ss")}] We didnt try to catch {pokemonName} because it is filtered");
                    return;
                }
                var update = await hero.Client.UpdatePlayerLocation(pokemon.Latitude, pokemon.Longitude);
                var encounterPokemonResponse = await hero.Client.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnpointId);
                var pokemonCP = encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp;
                CatchPokemonResponse caughtPokemonResponse;
                do
                {
                    if (hero.ClientSettings.RazzBerryMode == "cp")
                        if (pokemonCP > hero.ClientSettings.RazzBerrySetting)
                            await hero.Client.UseRazzBerry(hero.Client, pokemon.EncounterId, pokemon.SpawnpointId);
                    if (hero.ClientSettings.RazzBerryMode == "probability")
                        if (encounterPokemonResponse.CaptureProbability.CaptureProbability_.First() < hero.ClientSettings.RazzBerrySetting)
                            await hero.Client.UseRazzBerry(hero.Client, pokemon.EncounterId, pokemon.SpawnpointId);
                    caughtPokemonResponse = await hero.Client.CatchPokemon(pokemon.EncounterId, pokemon.SpawnpointId, pokemon.Latitude, pokemon.Longitude, MiscEnums.Item.ITEM_POKE_BALL, pokemonCP); ; //note: reverted from settings because this should not be part of settings but part of logic
                } while (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchMissed || caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchEscape);
                if (caughtPokemonResponse.Status == CatchPokemonResponse.Types.CatchStatus.CatchSuccess)
                {
                    Main.ColoredConsoleWrite(ConsoleColor.Green, $"[{DateTime.Now.ToString("HH:mm:ss")}] We caught a {pokemonName} with {encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp} CP");
                    foreach (int xp in caughtPokemonResponse.Scores.Xp)
                        TotalExperience += xp;
                    TotalPokemon += 1;
                }
                else
                    Main.ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] {pokemonName} with {encounterPokemonResponse?.WildPokemon?.PokemonData?.Cp} CP got away..");
                
                switch (hero.ClientSettings.TransferType)
                {
                    case "leaveStrongest":
                        await TransferAllButStrongestUnwantedPokemon(hero);
                        break;
                    case "all":
                        await TransferAllGivenPokemons(hero, pokemons2);
                        break;
                    case "duplicate":
                        await TransferDuplicatePokemon(hero);
                        break;
                    case "cp":
                        await TransferAllWeakPokemon(hero);
                        break;
                } 
                
                await Task.Delay(3000);
            }
        }

        
        /// <summary>
        /// Transfer duplicate weak pokemon to the doctor
        /// </summary>
        /// <param name="hero"></param>
        /// <returns></returns>
        public static async Task TransferAllButStrongestUnwantedPokemon(Hero hero)
        {
            PokemonId[] unwantedPokemonTypes = new[]
            {
                PokemonId.Pidgey,
                PokemonId.Rattata,
                PokemonId.Weedle,
                PokemonId.Zubat,
                PokemonId.Caterpie,
                PokemonId.Pidgeotto,
                PokemonId.Paras,
                PokemonId.Venonat,
                PokemonId.Psyduck,
                PokemonId.Poliwag,
                PokemonId.Slowpoke,
                PokemonId.Drowzee,
                PokemonId.Gastly,
                PokemonId.Goldeen,
                PokemonId.Staryu,
                PokemonId.Magikarp,
                PokemonId.Clefairy,
                PokemonId.Eevee,
                PokemonId.Tentacool,
                PokemonId.Dratini,
                PokemonId.Ekans,
                PokemonId.Jynx,
                PokemonId.Lickitung,
                PokemonId.Spearow,
                PokemonId.NidoranFemale,
                PokemonId.NidoranMale
            };

            var inventory = await hero.Client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Pokemon)
                .Where(p => p != null && p?.PokemonId > 0)
                .ToArray();

            foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                var pokemonOfDesiredType = pokemons.Where(p => p.PokemonId == unwantedPokemonType)
                    .OrderByDescending(p => p.Cp)
                    .ToList();

                var unwantedPokemon =
                    pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                        .ToList();

                //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Grinding {unwantedPokemon.Count} pokemons of type {unwantedPokemonType}");
                await Pokemon.TransferAllGivenPokemons(hero, unwantedPokemon);
            }

            //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Finished grinding all the meat");
        }

        public static async Task ExecuteFarmingPokestopsAndPokemons(Hero _hero)
        {
            var mapObjects = await _hero.Client.GetMapObjects();

            var pokeStops = mapObjects.MapCells.SelectMany(i => i.Forts).Where(i => i.Type == FortType.Checkpoint && i.CooldownCompleteTimestampMs < DateTime.UtcNow.ToUnixTime());

            foreach (var pokeStop in pokeStops)
            {
                if (!pokeStop.Enabled)
                    continue;

                var update = await _hero.Client.UpdatePlayerLocation(pokeStop.Latitude, pokeStop.Longitude);
                var fortInfo = await _hero.Client.GetFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);
                var fortSearch = await _hero.Client.SearchFort(pokeStop.Id, pokeStop.Latitude, pokeStop.Longitude);

                StringWriter PokeStopOutput = new StringWriter();
                PokeStopOutput.Write($"[{DateTime.Now.ToString("HH:mm:ss")}] ");

                if (fortInfo == null)
                {
                    continue;
                }

                if (fortInfo.Name != string.Empty)
                {

                    PokeStopOutput.Write("PokeStop: {0}", fortInfo.Name);
                }
                    

                if (fortSearch.ExperienceAwarded > 0)
                {
                    PokeStopOutput.Write(" | {0} XP", fortSearch.ExperienceAwarded);
                }

                if (fortSearch.GemsAwarded > 0)
                {
                    PokeStopOutput.Write(" | {0} Gems", fortSearch.GemsAwarded);
                }
                    
                if (fortSearch.PokemonDataEgg != null)
                {
                    PokeStopOutput.Write(" | Egg received!");
                }
                   
                string FriendlyItems = Item.GetFriendlyItemsString(fortSearch.ItemsAwarded, _hero);
                if (FriendlyItems != string.Empty)
                {
                    PokeStopOutput.Write(" | Items: " + FriendlyItems);
                }

                Main.ColoredConsoleWrite(ConsoleColor.Cyan, PokeStopOutput.ToString());

                if (_hero.Backpack.SlotsUsed >= _hero.Backpack.SlotsMax)
                {
                    await _hero.Client.RecycleItems(_hero.Client);

                    //refresh our bp knowledge
                    var profile = await _hero.Client.GetProfile();
                    var inventory = await _hero.Client.GetInventory();
                    bhelper.Game.RefreshBackPackStatus(_hero, profile.Profile, inventory);
                }
               

                if (fortSearch.ExperienceAwarded != 0)
                    TotalExperience += (fortSearch.ExperienceAwarded);
                await Task.Delay(15000);
                await ExecuteCatchAllNearbyPokemons(_hero);
            }
        }

        

        public static async Task TransferAllWeakPokemon(Hero hero)
        {
            //ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Firing up the meat grinder");

            PokemonId[] doNotTransfer = new[] //these will not be transferred even when below the CP threshold
            { // DO NOT EMPTY THIS ARRAY
                //PokemonId.Pidgey,
                //PokemonId.Rattata,
                //PokemonId.Weedle,
                //PokemonId.Zubat,
                //PokemonId.Caterpie,
                //PokemonId.Pidgeotto,
                //PokemonId.NidoranFemale,
                //PokemonId.Paras,
                //PokemonId.Venonat,
                //PokemonId.Psyduck,
                //PokemonId.Poliwag,
                //PokemonId.Slowpoke,
                //PokemonId.Drowzee,
                //PokemonId.Gastly,
                //PokemonId.Goldeen,
                //PokemonId.Staryu,
                //PokemonId.Dratini
                PokemonId.Magikarp,
                PokemonId.Eevee
            };

            var inventory = await hero.Client.GetInventory();
            var pokemons = inventory.InventoryDelta.InventoryItems
                                .Select(i => i.InventoryItemData?.Pokemon)
                                .Where(p => p != null && p?.PokemonId > 0)
                                .ToArray();

            //foreach (var unwantedPokemonType in unwantedPokemonTypes)
            {
                List<PokemonData> pokemonToDiscard;
                if (doNotTransfer.Count() != 0)
                    pokemonToDiscard = pokemons.Where(p => !doNotTransfer.Contains(p.PokemonId) && p.Cp < hero.ClientSettings.TransferCPThreshold).OrderByDescending(p => p.Cp).ToList();
                else
                    pokemonToDiscard = pokemons.Where(p => p.Cp < hero.ClientSettings.TransferCPThreshold).OrderByDescending(p => p.Cp).ToList();


                //var unwantedPokemon = pokemonOfDesiredType.Skip(1) // keep the strongest one for potential battle-evolving
                //                                          .ToList();
                Main.ColoredConsoleWrite(ConsoleColor.Gray, $"[{DateTime.Now.ToString("HH:mm:ss")}] Grinding {pokemonToDiscard.Count} pokemon below {hero.ClientSettings.TransferCPThreshold} CP.");
                await TransferAllGivenPokemons(hero, pokemonToDiscard);

            }

            Main.ColoredConsoleWrite(ConsoleColor.Gray, $"[{DateTime.Now.ToString("HH:mm:ss")}] Finished grinding all the meat");
        }

        public static async Task TransferAllGivenPokemons(Hero hero, IEnumerable<PokemonData> unwantedPokemons, float keepPerfectPokemonLimit = 80.0f)
        {
            foreach (var pokemon in unwantedPokemons)
            {
                if (Game.CalculatePokemonPerfection(pokemon) >= keepPerfectPokemonLimit) continue;
                Main.ColoredConsoleWrite(ConsoleColor.White, $"[{DateTime.Now.ToString("HH:mm:ss")}] Pokemon {pokemon.PokemonId} with {pokemon.Cp} CP has IV percent less than {keepPerfectPokemonLimit}%");

                if (pokemon.Favorite == 0)
                {
                    var transferPokemonResponse = await hero.Client.TransferPokemon(pokemon.Id);

                    /*
                    ReleasePokemonOutProto.Status {
                        UNSET = 0;
                        SUCCESS = 1;
                        POKEMON_DEPLOYED = 2;
                        FAILED = 3;
                        ERROR_POKEMON_IS_EGG = 4;
                    }*/
                    string pokemonName;
                    if (hero.ClientSettings.Language == "german")
                        pokemonName = Convert.ToString((PokemonId_german)(int)pokemon.PokemonId);
                    else
                        pokemonName = Convert.ToString(pokemon.PokemonId);
                    if (transferPokemonResponse.Status == 1)
                    {
                        Main.ColoredConsoleWrite(ConsoleColor.Magenta, $"[{DateTime.Now.ToString("HH:mm:ss")}] Transferred {pokemonName} with {pokemon.Cp} CP");
                    }
                    else
                    {
                        var status = transferPokemonResponse.Status;

                        Main.ColoredConsoleWrite(ConsoleColor.Red, $"[{DateTime.Now.ToString("HH:mm:ss")}] Somehow failed to transfer {pokemonName} with {pokemon.Cp} CP. " +
                                                 $"ReleasePokemonOutProto.Status was {status}");
                    }

                    await Task.Delay(3000);
                }
            }
        }

        /// <summary>
        /// transfer duplicate pokemons to the doctor.
        /// For what ever he is doing with them (?)
        /// </summary>
        /// <param name="hero"></param>
        public static async Task TransferDuplicatePokemon(Hero hero)
        {
            var inventory = await hero.Client.GetInventory();
            var allpokemons =
                inventory.InventoryDelta.InventoryItems.Select(i => i.InventoryItemData?.Pokemon)
                    .Where(p => p != null && p?.PokemonId > 0);

            var DupedPokemon = allpokemons.OrderBy(x => x.Cp).Select((x, i) => new { index = i, value = x })
                .GroupBy(x => x.value.PokemonId)
                .Where(x => x.Skip(1).Any());

            for (var i = 0; i < DupedPokemon.Count(); i++)
            {
                for (var j = 0; j < DupedPokemon.ElementAt(i).Count() - 1; j++)
                {
                    var tmpDupePokemon = DupedPokemon.ElementAt(i).ElementAt(j).value;
                    if (tmpDupePokemon.Favorite == 0)
                    {
                        var transfer = await hero.Client.TransferPokemon(tmpDupePokemon.Id);
                        string pokemonName;
                        if (hero.ClientSettings.Language == "german")
                            pokemonName = Convert.ToString((PokemonId_german)(int)tmpDupePokemon.PokemonId);
                        else
                            pokemonName = Convert.ToString(tmpDupePokemon.PokemonId);
                        Main.ColoredConsoleWrite(ConsoleColor.DarkGreen,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] Transferred " + pokemonName + ": " + (tmpDupePokemon.Cp + " CP ").ToString().PadRight(7) + String.Format("| {0:0.00}% perfection", Game.CalculatePokemonPerfection(tmpDupePokemon)));
                        Main.ColoredConsoleWrite(ConsoleColor.DarkGreen,
                            $"[{DateTime.Now.ToString("HH:mm:ss")}] -We possess " + pokemonName + ": " + (DupedPokemon.ElementAt(i).Last().value.Cp + " CP").ToString().PadRight(7) + String.Format("| {0:0.00}% perfection", Game.CalculatePokemonPerfection(DupedPokemon.ElementAt(i).Last().value)) );
                    }
                }
            }
        }

        
    }
}
