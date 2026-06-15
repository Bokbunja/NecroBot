#region using directives

using System;
using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using PokemonGo.RocketAPI;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class EvolvePokemonTask
    {
        private static DateTime _lastLuckyEggTime;

        public static async Task Execute(Context ctx, StateMachine machine)
        {
            if (ctx.LogicSettings.UseLuckyEggsWhileEvolving)
            {
                await UseLuckyEgg(ctx.Client, ctx.Inventory, machine);
            }

            var pokemonToEvolve = await ctx.Inventory.GetPokemonToEvolve(ctx.LogicSettings.PokemonsToEvolve);

            foreach (var pokemon in pokemonToEvolve)
            {
                var evolvePokemonOutProto = await RetryUtils.ExecuteAsync(
                    () => ctx.Client.Inventory.EvolvePokemon(pokemon.Id), "EvolvePokemon", machine.CancellationToken);

                machine.Fire(new PokemonEvolveEvent
                {
                    Id = pokemon.PokemonId,
                    Exp = evolvePokemonOutProto.ExperienceAwarded,
                    Result = evolvePokemonOutProto.Result
                });

                await JitterUtils.HumanLikeDelay(3000, 0.3, machine.CancellationToken);
            }
        }

        public static async Task UseLuckyEgg(Client client, Inventory inventory, StateMachine machine)
        {
            var inventoryContent = await inventory.GetItems();

            var luckyEggs = inventoryContent.Where(p => p.ItemId == ItemId.ItemLuckyEgg);
            var luckyEgg = luckyEggs.FirstOrDefault();

            if (luckyEgg == null || luckyEgg.Count <= 0 || _lastLuckyEggTime.AddMinutes(30).Ticks > DateTime.Now.Ticks)
                return;

            _lastLuckyEggTime = DateTime.Now;
            await RetryUtils.ExecuteAsync(() => client.Inventory.UseItemXpBoost(), "UseLuckyEgg",
                machine.CancellationToken);
            await inventory.RefreshCachedInventory();
            machine.Fire(new UseLuckyEggEvent {Count = luckyEgg.Count});
            await JitterUtils.HumanLikeDelay(2000, 0.3, machine.CancellationToken);
        }
    }
}
