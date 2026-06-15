// A FAKE Pokémon GO client. It talks to nobody - every call returns canned,
// self-consistent data so the refactored NecroBot loop (login -> inventory ->
// map -> walk -> catch -> spin -> transfer/recycle) runs end-to-end on .NET 8.
// This is the "stub the API client" step from docs/MODERNIZATION.md.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using POGOProtos.Data;
using POGOProtos.Data.Player;
using POGOProtos.Enums;
using POGOProtos.Inventory;
using POGOProtos.Inventory.Item;
using POGOProtos.Map.Fort;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;
using POGOProtos.Settings.Master;

namespace PokemonGo.RocketAPI.Enums
{
    public enum AuthType
    {
        Google,
        Ptc
    }
}

namespace PokemonGo.RocketAPI.Exceptions
{
    public class PtcOfflineException : Exception { }

    public class AccountNotVerifiedException : Exception { }
}

namespace PokemonGo.RocketAPI.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToUnixTime(this DateTime value)
        {
            return (long) (value.ToUniversalTime() - Epoch).TotalMilliseconds;
        }
    }
}

namespace PokemonGo.RocketAPI
{
    using PokemonGo.RocketAPI.Enums;

    public interface ISettings
    {
        AuthType AuthType { get; }
        string PtcUsername { get; }
        string PtcPassword { get; }
        double DefaultLatitude { get; }
        double DefaultLongitude { get; }
        double DefaultAltitude { get; }
        string GoogleRefreshToken { get; set; }
    }

    public class Client
    {
        // Shared, persistent fake inventory so transfers/recycles actually shrink it.
        internal GetInventoryResponse World;

        public Client(ISettings settings)
        {
            Settings = settings;
            CurrentLatitude = settings.DefaultLatitude;
            CurrentLongitude = settings.DefaultLongitude;
            World = FakeData.BuildInventory();

            Login = new LoginService();
            Player = new PlayerService(this);
            Map = new MapService(this);
            Fort = new FortService();
            Encounter = new EncounterService();
            Inventory = new InventoryService(this);
            Download = new DownloadService();
        }

        public ISettings Settings { get; }
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }

        public LoginService Login { get; }
        public PlayerService Player { get; }
        public MapService Map { get; }
        public FortService Fort { get; }
        public EncounterService Encounter { get; }
        public InventoryService Inventory { get; }
        public DownloadService Download { get; }
    }

    public class LoginService
    {
        // Real client raised this for Google device-code auth; never fired by the stub.
        public event Action<string, string> GoogleDeviceCodeEvent;

        public Task DoPtcLogin(string username, string password) => Task.CompletedTask;

        public Task DoGoogleLogin() => Task.CompletedTask;
    }

    public class PlayerService
    {
        private readonly Client _client;
        public PlayerService(Client client) { _client = client; }

        public Task<GetPlayerResponse> GetPlayer()
        {
            return Task.FromResult(new GetPlayerResponse
            {
                PlayerData = new PlayerData
                {
                    Username = "StubTrainer",
                    Currencies =
                    {
                        new Currency {Name = "POKECOIN", Amount = 0},
                        new Currency {Name = "STARDUST", Amount = 5000}
                    }
                }
            });
        }

        public Task<PlayerUpdateResponse> UpdatePlayerLocation(double latitude, double longitude, double altitude)
        {
            // Move the avatar so HumanLikeWalking actually converges on its target.
            _client.CurrentLatitude = latitude;
            _client.CurrentLongitude = longitude;
            return Task.FromResult(new PlayerUpdateResponse());
        }
    }

    public class MapService
    {
        private readonly Client _client;
        public MapService(Client client) { _client = client; }

        public Task<GetMapObjectsResponse> GetMapObjects()
        {
            var lat = _client.Settings.DefaultLatitude;
            var lng = _client.Settings.DefaultLongitude;

            var cell = new MapCell
            {
                Forts =
                {
                    new FortData {Id = "stop_1", Latitude = lat + 0.00020, Longitude = lng, Type = FortType.Checkpoint, CooldownCompleteTimestampMs = 0},
                    new FortData {Id = "stop_2", Latitude = lat + 0.00030, Longitude = lng + 0.00010, Type = FortType.Checkpoint, CooldownCompleteTimestampMs = 0}
                },
                CatchablePokemons =
                {
                    new MapPokemon {PokemonId = PokemonId.Pidgey, EncounterId = 5001, SpawnPointId = "spawn_1", Latitude = lat + 0.00010, Longitude = lng},
                    new MapPokemon {PokemonId = PokemonId.Rattata, EncounterId = 5002, SpawnPointId = "spawn_2", Latitude = lat + 0.00012, Longitude = lng + 0.00005}
                }
            };

            return Task.FromResult(new GetMapObjectsResponse {MapCells = {cell}});
        }
    }

    public class FortService
    {
        public Task<FortDetailsResponse> GetFort(string fortId, double latitude, double longitude)
        {
            return Task.FromResult(new FortDetailsResponse {Name = "Stub Pokéstop " + fortId});
        }

        public Task<FortSearchResponse> SearchFort(string fortId, double latitude, double longitude)
        {
            return Task.FromResult(new FortSearchResponse
            {
                ExperienceAwarded = 50,
                GemsAwarded = 0,
                ItemsAwarded =
                {
                    new ItemAward {ItemId = ItemId.ItemPokeBall, ItemCount = 2},
                    new ItemAward {ItemId = ItemId.ItemRazzBerry, ItemCount = 1}
                }
            });
        }
    }

    public class EncounterService
    {
        public Task<EncounterResponse> EncounterPokemon(ulong encounterId, string spawnPointId)
        {
            var species = encounterId % 2 == 0 ? PokemonId.Rattata : PokemonId.Pidgey;
            return Task.FromResult(new EncounterResponse
            {
                Status = EncounterResponse.Types.Status.EncounterSuccess,
                WildPokemon = new WildPokemon
                {
                    PokemonData = FakeData.MakePokemon(encounterId, species, 110, 10, 11, 12)
                },
                CaptureProbability = new CaptureProbability {CaptureProbability_ = {0.6}}
            });
        }

        public Task<CatchPokemonResponse> CatchPokemon(ulong encounterId, string spawnPointId, ItemId pokeball)
        {
            return Task.FromResult(new CatchPokemonResponse
            {
                Status = CatchPokemonResponse.Types.CatchStatus.CatchSuccess,
                CaptureAward = new CaptureAward {Xp = {100}, Candy = {3}, Stardust = {100}}
            });
        }

        public Task<UseItemCaptureResponse> UseCaptureItem(ulong encounterId, ItemId item, string spawnPointId)
        {
            return Task.FromResult(new UseItemCaptureResponse {Success = true});
        }
    }

    public class InventoryService
    {
        private readonly Client _client;
        public InventoryService(Client client) { _client = client; }

        public Task<GetInventoryResponse> GetInventory() => Task.FromResult(_client.World);

        public Task<ReleasePokemonResponse> TransferPokemon(ulong pokemonId)
        {
            // Inventory.DeletePokemonFromInvById already removes it from World; nothing to do.
            return Task.FromResult(new ReleasePokemonResponse());
        }

        public Task<EvolvePokemonResponse> EvolvePokemon(ulong pokemonId)
        {
            return Task.FromResult(new EvolvePokemonResponse
            {
                ExperienceAwarded = 500,
                Result = EvolvePokemonResponse.Types.Result.Success
            });
        }

        public Task<RecycleInventoryItemResponse> RecycleItem(ItemId itemId, int count)
        {
            var item = _client.World.InventoryDelta.InventoryItems
                .Select(i => i.InventoryItemData?.Item)
                .FirstOrDefault(i => i != null && i.ItemId == itemId);
            if (item != null)
                item.Count = Math.Max(0, item.Count - count);
            return Task.FromResult(new RecycleInventoryItemResponse());
        }

        public Task<NicknamePokemonResponse> NicknamePokemon(ulong pokemonId, string nickname)
            => Task.FromResult(new NicknamePokemonResponse());

        public Task<UseItemXpBoostResponse> UseItemXpBoost()
            => Task.FromResult(new UseItemXpBoostResponse());
    }

    public class DownloadService
    {
        public Task<DownloadItemTemplatesResponse> GetItemTemplates()
            => Task.FromResult(FakeData.BuildTemplates());
    }

    internal static class FakeData
    {
        public static PokemonData MakePokemon(ulong id, PokemonId species, int cp, int atk, int def, int sta)
        {
            return new PokemonData
            {
                Id = id,
                PokemonId = species,
                Cp = cp,
                CpMultiplier = 0.5f,
                AdditionalCpMultiplier = 0f,
                IndividualAttack = atk,
                IndividualDefense = def,
                IndividualStamina = sta,
                StaminaMax = 40,
                Favorite = 0,
                DeployedFortId = string.Empty,
                Nickname = string.Empty
            };
        }

        public static GetInventoryResponse BuildInventory()
        {
            var items = new List<InventoryItem>();

            void AddPokemon(PokemonData p) =>
                items.Add(new InventoryItem {InventoryItemData = new InventoryItemData {PokemonData = p}});
            void AddItem(ItemId id, int count) =>
                items.Add(new InventoryItem {InventoryItemData = new InventoryItemData {Item = new ItemData {ItemId = id, Count = count}}});
            void AddFamily(PokemonFamilyId fam, int candy) =>
                items.Add(new InventoryItem {InventoryItemData = new InventoryItemData {PokemonFamily = new PokemonFamily {FamilyId = fam, Candy = candy}}});

            AddPokemon(MakePokemon(1001, PokemonId.Pidgey, 120, 5, 6, 7));
            AddPokemon(MakePokemon(1002, PokemonId.Pidgey, 80, 2, 3, 4));   // low duplicate -> transfer
            AddPokemon(MakePokemon(1003, PokemonId.Rattata, 150, 10, 10, 10));
            AddPokemon(MakePokemon(1004, PokemonId.Zubat, 90, 1, 2, 3));
            AddPokemon(MakePokemon(1005, PokemonId.Caterpie, 60, 8, 8, 8));
            AddPokemon(MakePokemon(1006, PokemonId.Charmander, 300, 15, 15, 14)); // high IV -> keep

            AddItem(ItemId.ItemPokeBall, 50);
            AddItem(ItemId.ItemGreatBall, 20);
            AddItem(ItemId.ItemRazzBerry, 10);
            AddItem(ItemId.ItemPotion, 30);
            AddItem(ItemId.ItemLuckyEgg, 1);

            AddFamily(PokemonFamilyId.FamilyPidgey, 50);
            AddFamily(PokemonFamilyId.FamilyRattata, 30);
            AddFamily(PokemonFamilyId.FamilyZubat, 20);
            AddFamily(PokemonFamilyId.FamilyCaterpie, 15);
            AddFamily(PokemonFamilyId.FamilyCharmander, 40);

            items.Add(new InventoryItem
            {
                InventoryItemData = new InventoryItemData
                {
                    PlayerStats = new PlayerStats {Level = 5, Experience = 12000, PrevLevelXp = 10000, NextLevelXp = 20000}
                }
            });

            return new GetInventoryResponse {InventoryDelta = new InventoryDelta {InventoryItems = items}};
        }

        public static DownloadItemTemplatesResponse BuildTemplates()
        {
            PokemonSettings Setting(PokemonId id, PokemonFamilyId fam, int candy, PokemonId evolvesTo) =>
                new PokemonSettings {PokemonId = id, FamilyId = fam, CandyToEvolve = candy, EvolutionIds = {evolvesTo}};

            return new DownloadItemTemplatesResponse
            {
                ItemTemplates =
                {
                    new ItemTemplate {PokemonSettings = Setting(PokemonId.Pidgey, PokemonFamilyId.FamilyPidgey, 12, PokemonId.Pidgeotto)},
                    new ItemTemplate {PokemonSettings = Setting(PokemonId.Rattata, PokemonFamilyId.FamilyRattata, 25, PokemonId.Raticate)},
                    new ItemTemplate {PokemonSettings = Setting(PokemonId.Zubat, PokemonFamilyId.FamilyZubat, 50, PokemonId.Golbat)},
                    new ItemTemplate {PokemonSettings = Setting(PokemonId.Caterpie, PokemonFamilyId.FamilyCaterpie, 12, PokemonId.Metapod)},
                    new ItemTemplate {PokemonSettings = Setting(PokemonId.Charmander, PokemonFamilyId.FamilyCharmander, 25, PokemonId.Charmeleon)}
                }
            };
        }
    }
}
