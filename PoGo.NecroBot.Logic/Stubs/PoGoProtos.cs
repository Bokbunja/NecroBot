// Minimal stand-ins for the POGOProtos protobuf types that NecroBot consumes.
// These are NOT the real Pokémon GO protocol types - they exist only so the bot
// compiles and runs end-to-end against the fake client in PokemonGo.RocketAPI.
// Member names/shapes match what the bot code accesses.

using System.Collections.Generic;

namespace POGOProtos.Enums
{
    // Gen-1 Pokédex. Numbering must match PokemonInfo.GetBaseStats (it casts to int).
    public enum PokemonId
    {
        Missingno = 0,
        Bulbasaur = 1, Ivysaur = 2, Venusaur = 3, Charmander = 4, Charmeleon = 5, Charizard = 6,
        Squirtle = 7, Wartortle = 8, Blastoise = 9, Caterpie = 10, Metapod = 11, Butterfree = 12,
        Weedle = 13, Kakuna = 14, Beedrill = 15, Pidgey = 16, Pidgeotto = 17, Pidgeot = 18,
        Rattata = 19, Raticate = 20, Spearow = 21, Fearow = 22, Ekans = 23, Arbok = 24,
        Pikachu = 25, Raichu = 26, Sandshrew = 27, Sandslash = 28, NidoranFemale = 29, Nidorina = 30,
        Nidoqueen = 31, NidoranMale = 32, Nidorino = 33, Nidoking = 34, Clefairy = 35, Clefable = 36,
        Vulpix = 37, Ninetales = 38, Jigglypuff = 39, Wigglytuff = 40, Zubat = 41, Golbat = 42,
        Oddish = 43, Gloom = 44, Vileplume = 45, Paras = 46, Parasect = 47, Venonat = 48,
        Venomoth = 49, Diglett = 50, Dugtrio = 51, Meowth = 52, Persian = 53, Psyduck = 54,
        Golduck = 55, Mankey = 56, Primeape = 57, Growlithe = 58, Arcanine = 59, Poliwag = 60,
        Poliwhirl = 61, Poliwrath = 62, Abra = 63, Kadabra = 64, Alakazam = 65, Machop = 66,
        Machoke = 67, Machamp = 68, Bellsprout = 69, Weepinbell = 70, Victreebel = 71, Tentacool = 72,
        Tentacruel = 73, Geodude = 74, Graveler = 75, Golem = 76, Ponyta = 77, Rapidash = 78,
        Slowpoke = 79, Slowbro = 80, Magnemite = 81, Magneton = 82, Farfetchd = 83, Doduo = 84,
        Dodrio = 85, Seel = 86, Dewgong = 87, Grimer = 88, Muk = 89, Shellder = 90,
        Cloyster = 91, Gastly = 92, Haunter = 93, Gengar = 94, Onix = 95, Drowzee = 96,
        Hypno = 97, Krabby = 98, Kingler = 99, Voltorb = 100, Electrode = 101, Exeggcute = 102,
        Exeggutor = 103, Cubone = 104, Marowak = 105, Hitmonlee = 106, Hitmonchan = 107, Lickitung = 108,
        Koffing = 109, Weezing = 110, Rhyhorn = 111, Rhydon = 112, Chansey = 113, Tangela = 114,
        Kangaskhan = 115, Horsea = 116, Seadra = 117, Goldeen = 118, Seaking = 119, Staryu = 120,
        Starmie = 121, MrMime = 122, Scyther = 123, Jynx = 124, Electabuzz = 125, Magmar = 126,
        Pinsir = 127, Tauros = 128, Magikarp = 129, Gyarados = 130, Lapras = 131, Ditto = 132,
        Eevee = 133, Vaporeon = 134, Jolteon = 135, Flareon = 136, Porygon = 137, Omanyte = 138,
        Omastar = 139, Kabuto = 140, Kabutops = 141, Aerodactyl = 142, Snorlax = 143, Articuno = 144,
        Zapdos = 145, Moltres = 146, Dratini = 147, Dragonair = 148, Dragonite = 149, Mewtwo = 150, Mew = 151
    }

    public enum PokemonFamilyId
    {
        FamilyUnset = 0,
        FamilyCharmander = 4, FamilyCaterpie = 10, FamilyWeedle = 13, FamilyPidgey = 16,
        FamilyRattata = 19, FamilyZubat = 41, FamilyEevee = 133
    }
}

namespace POGOProtos.Inventory.Item
{
    public enum ItemId
    {
        ItemUnknown = 0,
        ItemPokeBall = 1, ItemGreatBall = 2, ItemUltraBall = 3, ItemMasterBall = 4,
        ItemPotion = 101, ItemSuperPotion = 102, ItemHyperPotion = 103, ItemMaxPotion = 104,
        ItemRevive = 201, ItemMaxRevive = 202,
        ItemLuckyEgg = 301,
        ItemIncenseOrdinary = 401, ItemIncenseSpicy = 402, ItemIncenseCool = 403, ItemIncenseFloral = 404,
        ItemTroyDisk = 501,
        ItemXAttack = 602, ItemXDefense = 603, ItemXMiracle = 604,
        ItemRazzBerry = 701, ItemBlukBerry = 702, ItemNanabBerry = 703, ItemWeparBerry = 704, ItemPinapBerry = 705,
        ItemSpecialCamera = 801,
        ItemIncubatorBasicUnlimited = 901, ItemIncubatorBasic = 902,
        ItemPokemonStorageUpgrade = 1001, ItemItemStorageUpgrade = 1002
    }

    public class ItemData
    {
        public ItemId ItemId { get; set; }
        public int Count { get; set; }
        public int Unseen { get; set; }
    }

    public class ItemAward
    {
        public ItemId ItemId { get; set; }
        public int ItemCount { get; set; }
    }
}

namespace POGOProtos.Data
{
    using POGOProtos.Enums;

    public class PokemonData
    {
        public ulong Id { get; set; }
        public PokemonId PokemonId { get; set; }
        public int Cp { get; set; }
        public float CpMultiplier { get; set; }
        public float AdditionalCpMultiplier { get; set; }
        public int IndividualAttack { get; set; }
        public int IndividualDefense { get; set; }
        public int IndividualStamina { get; set; }
        public int StaminaMax { get; set; }
        public int Favorite { get; set; }
        public string DeployedFortId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
    }

    public class PlayerData
    {
        public string Username { get; set; }
        public List<Currency> Currencies { get; set; } = new List<Currency>();
    }

    public class Currency
    {
        public string Name { get; set; }
        public int Amount { get; set; }
    }
}

namespace POGOProtos.Data.Player
{
    public class PlayerStats
    {
        public int Level { get; set; }
        public long Experience { get; set; }
        public long PrevLevelXp { get; set; }
        public long NextLevelXp { get; set; }
    }
}

namespace POGOProtos.Inventory
{
    using System.Collections.Generic;
    using POGOProtos.Enums;

    public class PokemonFamily
    {
        public PokemonFamilyId FamilyId { get; set; }
        public int Candy { get; set; }
    }

    public class InventoryItemData
    {
        public POGOProtos.Data.PokemonData PokemonData { get; set; }
        public POGOProtos.Inventory.Item.ItemData Item { get; set; }
        public POGOProtos.Data.Player.PlayerStats PlayerStats { get; set; }
        public PokemonFamily PokemonFamily { get; set; }
    }

    public class InventoryItem
    {
        public InventoryItemData InventoryItemData { get; set; }
    }

    public class InventoryDelta
    {
        public List<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
    }
}

namespace POGOProtos.Settings.Master
{
    using System.Collections.Generic;
    using POGOProtos.Enums;

    public class PokemonSettings
    {
        public PokemonId PokemonId { get; set; }
        public PokemonFamilyId FamilyId { get; set; }
        public int CandyToEvolve { get; set; }
        public List<PokemonId> EvolutionIds { get; set; } = new List<PokemonId>();
    }

    // Wrapper mirroring DownloadItemTemplatesResponse.ItemTemplates[i].PokemonSettings
    public class ItemTemplate
    {
        public PokemonSettings PokemonSettings { get; set; }
    }
}

namespace POGOProtos.Map.Pokemon
{
    using POGOProtos.Enums;

    public class MapPokemon
    {
        public PokemonId PokemonId { get; set; }
        public ulong EncounterId { get; set; }
        public string SpawnPointId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

namespace POGOProtos.Map.Fort
{
    public enum FortType
    {
        Gym = 0,
        Checkpoint = 1
    }

    public class FortData
    {
        public string Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public FortType Type { get; set; }
        public long CooldownCompleteTimestampMs { get; set; }
    }
}

namespace POGOProtos.Networking.Responses
{
    using System.Collections.Generic;
    using POGOProtos.Data;
    using POGOProtos.Inventory;
    using POGOProtos.Inventory.Item;
    using POGOProtos.Map.Fort;
    using POGOProtos.Map.Pokemon;
    using POGOProtos.Settings.Master;

    public class GetPlayerResponse
    {
        public PlayerData PlayerData { get; set; } = new PlayerData();
    }

    public class PlayerUpdateResponse
    {
    }

    public class GetInventoryResponse
    {
        public InventoryDelta InventoryDelta { get; set; } = new InventoryDelta();
    }

    public class DownloadItemTemplatesResponse
    {
        public List<ItemTemplate> ItemTemplates { get; set; } = new List<ItemTemplate>();
    }

    public class MapCell
    {
        public List<FortData> Forts { get; set; } = new List<FortData>();
        public List<MapPokemon> CatchablePokemons { get; set; } = new List<MapPokemon>();
    }

    public class GetMapObjectsResponse
    {
        public List<MapCell> MapCells { get; set; } = new List<MapCell>();
    }

    public class FortDetailsResponse
    {
        public string Name { get; set; }
    }

    public class FortSearchResponse
    {
        public int ExperienceAwarded { get; set; }
        public int GemsAwarded { get; set; }
        public List<ItemAward> ItemsAwarded { get; set; } = new List<ItemAward>();
    }

    public class WildPokemon
    {
        public PokemonData PokemonData { get; set; }
    }

    public class CaptureProbability
    {
        // Trailing underscore mirrors the protobuf-generated field name the bot uses.
        public List<double> CaptureProbability_ { get; set; } = new List<double>();
    }

    public class EncounterResponse
    {
        public Types.Status Status { get; set; }
        public WildPokemon WildPokemon { get; set; }
        public CaptureProbability CaptureProbability { get; set; }

        public static class Types
        {
            public enum Status
            {
                EncounterError = 0,
                EncounterSuccess = 1,
                EncounterNotFound = 2,
                EncounterClosed = 3,
                EncounterPokemonFled = 4
            }
        }
    }

    public class CaptureAward
    {
        public List<int> Xp { get; set; } = new List<int>();
        public List<int> Candy { get; set; } = new List<int>();
        public List<int> Stardust { get; set; } = new List<int>();
    }

    public class CatchPokemonResponse
    {
        public Types.CatchStatus Status { get; set; }
        public CaptureAward CaptureAward { get; set; } = new CaptureAward();

        public static class Types
        {
            public enum CatchStatus
            {
                CatchError = 0,
                CatchSuccess = 1,
                CatchEscape = 2,
                CatchFlee = 3,
                CatchMissed = 4
            }
        }
    }

    public class UseItemCaptureResponse
    {
        public bool Success { get; set; }
    }

    public class ReleasePokemonResponse
    {
    }

    public class RecycleInventoryItemResponse
    {
    }

    public class NicknamePokemonResponse
    {
    }

    public class UseItemXpBoostResponse
    {
    }

    public class EvolvePokemonResponse
    {
        public int ExperienceAwarded { get; set; }
        public Types.Result Result { get; set; }

        public static class Types
        {
            public enum Result
            {
                Unset = 0,
                Success = 1,
                FailedPokemonMissing = 2,
                FailedInsufficientResources = 3,
                FailedPokemonCannotEvolve = 4
            }
        }
    }
}
