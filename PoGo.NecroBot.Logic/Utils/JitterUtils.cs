#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace PoGo.NecroBot.Logic.Utils
{
    public static class JitterUtils
    {
        private static readonly Random RandomDevice = new Random();

        /// <summary>
        ///     Cancellation-aware delay for a random duration in [min, max).
        /// </summary>
        public static Task RandomDelay(int min, int max, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (max <= min)
                max = min + 1;
            return Task.Delay(RandomDevice.Next(min, max), cancellationToken);
        }

        /// <summary>
        ///     Cancellation-aware delay around a base value, applying +/- <paramref name="jitterFactor" />
        ///     (0..1) of random variation. e.g. base 2000ms with 0.3 jitter waits somewhere in 1400-2600ms.
        ///     Makes the bot's timing look less robotic than a fixed delay.
        /// </summary>
        public static Task HumanLikeDelay(int baseMs, double jitterFactor = 0.3,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var spread = (int) (baseMs * Math.Max(0, Math.Min(1, jitterFactor)));
            return RandomDelay(Math.Max(0, baseMs - spread), baseMs + spread + 1, cancellationToken);
        }
    }
}
