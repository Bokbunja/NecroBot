using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic;
using PokemonGo.RocketAPI;
using POGOProtos.Enums;
using POGOProtos.Inventory.Item;
using Xunit;

namespace PoGo.NecroBot.Tests
{
    public class InventoryTests
    {
        private static Inventory BuildInventory(TestLogicSettings logic = null)
        {
            var client = new Client(new TestSettings());
            var logicClient = new LogicClient(logic ?? new TestLogicSettings());
            return new Inventory(client, logicClient);
        }

        [Fact]
        public async Task GetItemAmountByType_ReturnsStockedCount()
        {
            var inv = BuildInventory();
            Assert.Equal(50, await inv.GetItemAmountByType(ItemId.ItemPokeBall));
            Assert.Equal(0, await inv.GetItemAmountByType(ItemId.ItemMasterBall));
        }

        [Fact]
        public async Task GetPokemons_ReturnsAllStockedPokemon()
        {
            var inv = BuildInventory();
            var pokemon = (await inv.GetPokemons()).ToList();
            Assert.Equal(6, pokemon.Count);
        }

        [Fact]
        public async Task GetHighestsCp_IsOrderedDescendingByCp()
        {
            var inv = BuildInventory();
            var top = (await inv.GetHighestsCp(3)).ToList();
            Assert.Equal(PokemonId.Charmander, top[0].PokemonId); // CP 300
            Assert.True(top[0].Cp >= top[1].Cp && top[1].Cp >= top[2].Cp);
        }

        [Fact]
        public async Task GetPokemonFamilies_ExposesCandyTotals()
        {
            var inv = BuildInventory();
            var families = await inv.GetPokemonFamilies();
            var pidgey = families.Single(f => f.FamilyId == PokemonFamilyId.FamilyPidgey);
            Assert.Equal(50, pidgey.Candy);
        }

        [Fact]
        public async Task GetDuplicatePokemonToTransfer_KeepsBestAndFlagsTheExtra()
        {
            var inv = BuildInventory();
            var toTransfer = (await inv.GetDuplicatePokemonToTransfer()).ToList();

            // Only the two Pidgey are duplicates (KeepMinDuplicatePokemon = 1), so exactly the
            // lower-CP one (Id 1002, CP 80) should be flagged; everything else is a singleton.
            var single = Assert.Single(toTransfer);
            Assert.Equal(PokemonId.Pidgey, single.PokemonId);
            Assert.Equal(1002ul, single.Id);
        }

        [Fact]
        public async Task GetPokemonToEvolve_RespectsCandyAndFilter()
        {
            var logic = new TestLogicSettings {EvolveAllPokemonWithEnoughCandy = true};
            var inv = BuildInventory(logic);

            var toEvolve = (await inv.GetPokemonToEvolve(logic.PokemonsToEvolve)).ToList();

            // 2 Pidgey (50 candy, 12 each) + 1 Rattata (30 candy, 25). Zubat has only 20/50 candy.
            Assert.Equal(3, toEvolve.Count);
            Assert.Equal(2, toEvolve.Count(p => p.PokemonId == PokemonId.Pidgey));
            Assert.Contains(toEvolve, p => p.PokemonId == PokemonId.Rattata);
            Assert.DoesNotContain(toEvolve, p => p.PokemonId == PokemonId.Zubat);
        }

        [Fact]
        public async Task GetItemsToRecycle_RecyclesAmountAboveThreshold()
        {
            var inv = BuildInventory();
            var recycle = (await inv.GetItemsToRecycle(new TestSettings())).ToList();

            // Have 50 Pokéballs, keep threshold 25 -> recycle 25.
            var pokeballs = recycle.Single(i => i.ItemId == ItemId.ItemPokeBall);
            Assert.Equal(25, pokeballs.Count);
        }
    }
}
