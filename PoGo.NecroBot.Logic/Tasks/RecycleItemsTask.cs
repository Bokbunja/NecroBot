#region using directives

using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class RecycleItemsTask
    {
        public static async Task Execute(Context ctx, StateMachine machine)
        {
            var items = await ctx.Inventory.GetItemsToRecycle(ctx.Settings);

            foreach (var item in items)
            {
                await RetryUtils.ExecuteAsync(() => ctx.Client.Inventory.RecycleItem(item.ItemId, item.Count),
                    "RecycleItem", machine.CancellationToken);

                machine.Fire(new ItemRecycledEvent {Id = item.ItemId, Count = item.Count});

                await JitterUtils.HumanLikeDelay(500, 0.4, machine.CancellationToken);
            }

            await ctx.Inventory.RefreshCachedInventory();
        }
    }
}
