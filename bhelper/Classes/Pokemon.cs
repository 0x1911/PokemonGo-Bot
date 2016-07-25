using PokemonGo.RocketAPI.GeneratedCode;

namespace bhelper.Classes
{
    public class Pokemon
    {
        public PokemonData Pokemondata { get; set; }
        public float PerfectionPercent { get; set; }
        public Pokemon(PokemonData pokemondata, float perfectionPercent)
        {
            Pokemondata = pokemondata;
            PerfectionPercent = perfectionPercent;
        }
    }
}