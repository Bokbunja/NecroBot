using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Tasks;
using POGOProtos.Enums;
using Xunit;

namespace PoGo.NecroBot.Tests
{
    public class TaskTests
    {
        private static (Context ctx, StateMachine machine, List<IEvent> events) BuildHarness(
            TestLogicSettings logic = null)
        {
            var ctx = new Context(new TestSettings(), logic ?? new TestLogicSettings());
            var machine = new StateMachine();
            var events = new List<IEvent>();
            machine.EventListener += (evt, c) => events.Add(evt);
            return (ctx, machine, events);
        }

        [Fact]
        public async Task TransferDuplicatePokemonTask_FiresTransferEventForTheExtraPidgey()
        {
            var (ctx, machine, events) = BuildHarness();

            await TransferDuplicatePokemonTask.Execute(ctx, machine);

            var transfers = events.OfType<TransferPokemonEvent>().ToList();
            var transfer = Assert.Single(transfers);
            Assert.Equal(PokemonId.Pidgey, transfer.Id);
        }

        [Fact]
        public async Task RecycleItemsTask_FiresRecycleEvents()
        {
            var (ctx, machine, events) = BuildHarness();

            await RecycleItemsTask.Execute(ctx, machine);

            var recycled = events.OfType<ItemRecycledEvent>().ToList();
            Assert.NotEmpty(recycled);
            Assert.All(recycled, e => Assert.True(e.Count > 0));
        }

        [Fact]
        public async Task CatchNearbyPokemonsTask_CatchesTheStubSpawns()
        {
            var (ctx, machine, events) = BuildHarness();

            await CatchNearbyPokemonsTask.Execute(ctx, machine);

            // The fake map has two catchable Pokémon; both should produce a successful capture.
            var captures = events.OfType<PokemonCaptureEvent>().ToList();
            Assert.Equal(2, captures.Count);
            Assert.All(captures, c =>
                Assert.Equal(POGOProtos.Networking.Responses.CatchPokemonResponse.Types.CatchStatus.CatchSuccess,
                    c.Status));
        }
    }
}
