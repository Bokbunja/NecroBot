#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;
using POGOProtos.Map.Pokemon;
using POGOProtos.Networking.Responses;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public static class CatchNearbyPokemonsTask
    {
        public static async Task Execute(Context ctx, StateMachine machine)
        {
            Logger.Write("Looking for pokemon..", LogLevel.Debug);

            var pokemons = await GetNearbyPokemons(ctx, machine);
            foreach (var pokemon in pokemons)
            {
                if (ctx.LogicSettings.UsePokemonToNotCatchFilter &&
                    ctx.LogicSettings.PokemonsNotToCatch.Contains(pokemon.PokemonId))
                {
                    Logger.Write("Skipped " + pokemon.PokemonId);
                    continue;
                }

                var distance = LocationUtils.CalculateDistanceInMeters(ctx.Client.CurrentLatitude,
                    ctx.Client.CurrentLongitude, pokemon.Latitude, pokemon.Longitude);
                await JitterUtils.HumanLikeDelay(distance > 100 ? 15000 : 500, 0.2, machine.CancellationToken);

                var encounter = await RetryUtils.ExecuteAsync(
                    () => ctx.Client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId),
                    "EncounterPokemon", machine.CancellationToken);

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    await CatchPokemonTask.Execute(ctx, machine, encounter, pokemon);
                }
                else
                {
                    machine.Fire(new WarnEvent {Message = $"Encounter problem: {encounter.Status}"});
                }

                // If pokemon is not last pokemon in list, create delay between catches, else keep moving.
                if (!Equals(pokemons.ElementAtOrDefault(pokemons.Count() - 1), pokemon))
                {
                    await JitterUtils.HumanLikeDelay(ctx.LogicSettings.DelayBetweenPokemonCatch, 0.3,
                        machine.CancellationToken);
                }
            }
        }

        private static async Task<IOrderedEnumerable<MapPokemon>> GetNearbyPokemons(Context ctx, StateMachine machine)
        {
            var mapObjects = await RetryUtils.ExecuteAsync(() => ctx.Client.Map.GetMapObjects(), "GetMapObjects",
                machine.CancellationToken);

            var pokemons = mapObjects.MapCells.SelectMany(i => i.CatchablePokemons)
                .OrderBy(
                    i =>
                        LocationUtils.CalculateDistanceInMeters(ctx.Client.CurrentLatitude, ctx.Client.CurrentLongitude,
                            i.Latitude, i.Longitude));

            return pokemons;
        }
    }
}
