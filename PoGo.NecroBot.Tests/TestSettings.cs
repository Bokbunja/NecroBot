using System.Collections.Generic;
using PoGo.NecroBot.Logic;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;

namespace PoGo.NecroBot.Tests
{
    /// <summary>Minimal ISettings double for tests (location matches the fake client's defaults).</summary>
    internal class TestSettings : ISettings
    {
        public AuthType AuthType { get; set; } = AuthType.Google;
        public string PtcUsername { get; set; } = "user";
        public string PtcPassword { get; set; } = "pass";
        public double DefaultLatitude { get; set; } = 52.379189;
        public double DefaultLongitude { get; set; } = 4.899431;
        public double DefaultAltitude { get; set; } = 10;
        public string GoogleRefreshToken { get; set; } = "";
    }

    /// <summary>Mutable ILogicSettings double so individual tests can tweak thresholds.</summary>
    internal class TestLogicSettings : ILogicSettings
    {
        public float KeepMinIvPercentage { get; set; } = 85;
        public int KeepMinCp { get; set; } = 1000;
        public double WalkingSpeedInKilometerPerHour { get; set; } = 50;
        public bool EvolveAllPokemonWithEnoughCandy { get; set; }
        public bool KeepPokemonsThatCanEvolve { get; set; }
        public bool TransferDuplicatePokemon { get; set; } = true;
        public int DelayBetweenPokemonCatch { get; set; } = 10;
        public bool UsePokemonToNotCatchFilter { get; set; }
        public int KeepMinDuplicatePokemon { get; set; } = 1;
        public bool PrioritizeIvOverCp { get; set; }
        public int MaxTravelDistanceInMeters { get; set; } = 1000;
        public bool UseGpxPathing { get; set; }
        public string GpxFile { get; set; } = "GPXPath.GPX";
        public bool UseLuckyEggsWhileEvolving { get; set; }
        public bool EvolveAllPokemonAboveIv { get; set; }
        public float EvolveAboveIvValue { get; set; } = 95;
        public bool RenameAboveIv { get; set; }
        public int AmountOfPokemonToDisplayOnStart { get; set; } = 10;

        public ICollection<KeyValuePair<ItemId, int>> ItemRecycleFilter { get; set; } =
            new List<KeyValuePair<ItemId, int>>
            {
                new KeyValuePair<ItemId, int>(ItemId.ItemPokeBall, 25),
                new KeyValuePair<ItemId, int>(ItemId.ItemPotion, 0)
            };

        public ICollection<PokemonId> PokemonsToEvolve { get; set; } =
            new List<PokemonId> {PokemonId.Pidgey, PokemonId.Rattata, PokemonId.Zubat};

        public ICollection<PokemonId> PokemonsNotToTransfer { get; set; } = new List<PokemonId>();

        public ICollection<PokemonId> PokemonsNotToCatch { get; set; } = new List<PokemonId>();
    }
}
