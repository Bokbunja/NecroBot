#region using directives

using System.Linq;
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
        public static void Execute(Context ctx, StateMachine machine)
        {
            Logger.Write("Looking for pokemon..", LogLevel.Debug);

            var pokemons = GetNearbyPokemons(ctx, machine);
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
                JitterUtils.HumanLikeSleep(distance > 100 ? 15000 : 500, 0.2, machine.CancellationToken);

                var encounter = RetryUtils.Execute(
                    () => ctx.Client.Encounter.EncounterPokemon(pokemon.EncounterId, pokemon.SpawnPointId),
                    "EncounterPokemon", machine.CancellationToken);

                if (encounter.Status == EncounterResponse.Types.Status.EncounterSuccess)
                {
                    CatchPokemonTask.Execute(ctx, machine, encounter, pokemon);
                }
                else
                {
                    machine.Fire(new WarnEvent {Message = $"Encounter problem: {encounter.Status}"});
                }

                // If pokemon is not last pokemon in list, create delay between catches, else keep moving.
                if (!Equals(pokemons.ElementAtOrDefault(pokemons.Count() - 1), pokemon))
                {
                    JitterUtils.HumanLikeSleep(ctx.LogicSettings.DelayBetweenPokemonCatch, 0.3,
                        machine.CancellationToken);
                }
            }
        }

        private static IOrderedEnumerable<MapPokemon> GetNearbyPokemons(Context ctx, StateMachine machine)
        {
            var mapObjects = RetryUtils.Execute(() => ctx.Client.Map.GetMapObjects(), "GetMapObjects",
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