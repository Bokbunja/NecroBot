#region using directives

using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class RecycleItemsTask
    {
        public static void Execute(Context ctx, StateMachine machine)
        {
            var items = ctx.Inventory.GetItemsToRecycle(ctx.Settings).Result;

            foreach (var item in items)
            {
                RetryUtils.Execute(() => ctx.Client.Inventory.RecycleItem(item.ItemId, item.Count),
                    "RecycleItem", machine.CancellationToken);

                machine.Fire(new ItemRecycledEvent {Id = item.ItemId, Count = item.Count});

                JitterUtils.HumanLikeSleep(500, 0.4, machine.CancellationToken);
            }

            ctx.Inventory.RefreshCachedInventory().Wait();
        }
    }
}