using System;
using System.Collections.Generic;
using PokemonGo.RocketAPI;

namespace bhelper.Classes
{
    public struct Hero
    {
        public ISettings ClientSettings { get; set; }
        public Client Client { get; set; }
        public int Currentlevel { get; set; }
        public int TotalExperience { get; set; }
        public double TotalKmWalked { get; set; }
        public DateTime TimeStarted { get; set; }
        public bool AllowedToRun { get; set; }
        public List<bhelper.Classes.Pokemon> OwnedPokemons { get; set; }

        public Hero(Client client)
        {
            Client = client;
            ClientSettings = new Settings();

            Currentlevel = -1;
            TotalExperience = 0;
            TotalKmWalked = 0;
            TimeStarted = DateTime.Now;
            AllowedToRun = true;

            OwnedPokemons = new List<Pokemon>();

        }
    }
}