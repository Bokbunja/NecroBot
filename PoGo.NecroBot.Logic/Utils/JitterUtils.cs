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

        public static Task RandomDelay(int min, int max)
        {
            return Task.Delay(RandomDevice.Next(min, max));
        }

        /// <summary>
        ///     Cancellation-aware blocking sleep for a random duration in [min, max). Used to make the
        ///     bot's timing look less robotic than a fixed <c>Thread.Sleep</c> and to let a requested
        ///     shutdown interrupt the wait instead of blocking for the full interval.
        /// </summary>
        public static void RandomSleep(int min, int max, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (max <= min)
                max = min + 1;

            var delay = RandomDevice.Next(min, max);
            if (cancellationToken.WaitHandle.WaitOne(delay))
                cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        ///     Cancellation-aware blocking sleep around a base value, applying +/- <paramref name="jitterFactor" />
        ///     (0..1) of random variation. e.g. base 2000ms with 0.3 jitter sleeps somewhere in 1400-2600ms.
        /// </summary>
        public static void HumanLikeSleep(int baseMs, double jitterFactor = 0.3,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var spread = (int) (baseMs * Math.Max(0, Math.Min(1, jitterFactor)));
            RandomSleep(Math.Max(0, baseMs - spread), baseMs + spread + 1, cancellationToken);
        }
    }
}
