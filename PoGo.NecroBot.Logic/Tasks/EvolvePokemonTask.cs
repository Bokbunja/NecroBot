#region using directives

using System;
using System.Linq;
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
        public static void Execute(Context ctx, StateMachine machine)
        {
            if (ctx.LogicSettings.UseLuckyEggsWhileEvolving)
            {
                UseLuckyEgg(ctx.Client, ctx.Inventory, machine);
            }

            var pokemonToEvolveTask = ctx.Inventory.GetPokemonToEvolve(ctx.LogicSettings.PokemonsToEvolve);
            pokemonToEvolveTask.Wait();

            var pokemonToEvolve = pokemonToEvolveTask.Result;
            foreach (var pokemon in pokemonToEvolve)
            {
                var evolvePokemonOutProto = RetryUtils.Execute(
                    () => ctx.Client.Inventory.EvolvePokemon(pokemon.Id), "EvolvePokemon", machine.CancellationToken);

                machine.Fire(new PokemonEvolveEvent
                {
                    Id = pokemon.PokemonId,
                    Exp = evolvePokemonOutProto.ExperienceAwarded,
                    Result = evolvePokemonOutProto.Result
                });

                JitterUtils.HumanLikeSleep(3000, 0.3, machine.CancellationToken);
            }
        }

        public static void UseLuckyEgg(Client client, Inventory inventory, StateMachine machine)
        {

            var inventoryContent = inventory.GetItems().Result;

            var luckyEggs = inventoryContent.Where(p => p.ItemId == ItemId.ItemLuckyEgg);
            var luckyEgg = luckyEggs.FirstOrDefault();

            if (luckyEgg == null || luckyEgg.Count <= 0 || _lastLuckyEggTime.AddMinutes(30).Ticks > DateTime.Now.Ticks)
                return;

            _lastLuckyEggTime = DateTime.Now;
            RetryUtils.Execute(() => client.Inventory.UseItemXpBoost(), "UseLuckyEgg", machine.CancellationToken);
            var refreshCachedInventory = inventory.RefreshCachedInventory();
            machine.Fire(new UseLuckyEggEvent {Count = luckyEgg.Count});
            JitterUtils.HumanLikeSleep(2000, 0.3, machine.CancellationToken);
        }
    }
}