#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.PoGoUtils;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class TransferDuplicatePokemonTask
    {
        public static async Task Execute(Context ctx, StateMachine machine)
        {
            var duplicatePokemons =
                await ctx.Inventory.GetDuplicatePokemonToTransfer(ctx.LogicSettings.KeepPokemonsThatCanEvolve,
                    ctx.LogicSettings.PrioritizeIvOverCp, ctx.LogicSettings.PokemonsNotToTransfer);

            var pokemonSettings = await ctx.Inventory.GetPokemonSettings();
            var pokemonFamilies = await ctx.Inventory.GetPokemonFamilies();

            foreach (var duplicatePokemon in duplicatePokemons)
            {
                if (duplicatePokemon.Cp >= ctx.LogicSettings.KeepMinCp ||
                    PokemonInfo.CalculatePokemonPerfection(duplicatePokemon) >= ctx.LogicSettings.KeepMinIvPercentage)
                {
                    continue;
                }

                await RetryUtils.ExecuteAsync(() => ctx.Client.Inventory.TransferPokemon(duplicatePokemon.Id),
                    "TransferPokemon", machine.CancellationToken);
                ctx.Inventory.DeletePokemonFromInvById(duplicatePokemon.Id);

                var bestPokemonOfType = ctx.LogicSettings.PrioritizeIvOverCp
                    ? await ctx.Inventory.GetHighestPokemonOfTypeByIv(duplicatePokemon)
                    : await ctx.Inventory.GetHighestPokemonOfTypeByCp(duplicatePokemon);

                if (bestPokemonOfType == null)
                    bestPokemonOfType = duplicatePokemon;

                // Look-ups can legitimately miss (e.g. a PokemonId not present in the cached
                // settings/families); guard against it instead of crashing the whole transfer loop.
                var setting = pokemonSettings.FirstOrDefault(q => q.PokemonId == duplicatePokemon.PokemonId);
                var family = setting == null
                    ? null
                    : pokemonFamilies.FirstOrDefault(q => q.FamilyId == setting.FamilyId);

                if (family != null)
                    family.Candy++;

                machine.Fire(new TransferPokemonEvent
                {
                    Id = duplicatePokemon.PokemonId,
                    Perfection = PokemonInfo.CalculatePokemonPerfection(duplicatePokemon),
                    Cp = duplicatePokemon.Cp,
                    BestCp = bestPokemonOfType.Cp,
                    BestPerfection = PokemonInfo.CalculatePokemonPerfection(bestPokemonOfType),
                    FamilyCandies = family?.Candy ?? 0
                });
            }
        }
    }
}
