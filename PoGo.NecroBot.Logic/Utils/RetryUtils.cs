#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Logging;

#endregion

namespace PoGo.NecroBot.Logic.Utils
{
    /// <summary>
    ///     Helpers that wrap network/API calls so that transient failures (timeouts, dropped
    ///     connections, server hiccups) are retried with exponential backoff instead of bubbling
    ///     up and forcing a full re-login. All overloads are cancellation aware so a requested
    ///     shutdown aborts the wait immediately.
    /// </summary>
    public static class RetryUtils
    {
        private const int DefaultMaxRetries = 3;
        private const int DefaultBaseDelayMs = 1000;
        private const int MaxBackoffMs = 30000;

        private static readonly Random Jitter = new Random();

        /// <summary>
        ///     Run an asynchronous, value-returning API call synchronously, retrying transient
        ///     failures with exponential backoff. Mirrors the existing <c>.Result</c> style used
        ///     throughout the bot, but resilient to flaky networks.
        /// </summary>
        public static T Execute<T>(Func<Task<T>> action, string operationName,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxRetries = DefaultMaxRetries, int baseDelayMs = DefaultBaseDelayMs)
        {
            var attempt = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return action().Result;
                }
                catch (Exception ex)
                {
                    if (!HandleFailure(ex, ref attempt, operationName, maxRetries, baseDelayMs, cancellationToken))
                        throw;
                }
            }
        }

        /// <summary>
        ///     Run an asynchronous, void API call synchronously, retrying transient failures with
        ///     exponential backoff. Replacement for the existing <c>.Wait()</c> call sites.
        /// </summary>
        public static void Execute(Func<Task> action, string operationName,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxRetries = DefaultMaxRetries, int baseDelayMs = DefaultBaseDelayMs)
        {
            var attempt = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    action().Wait();
                    return;
                }
                catch (Exception ex)
                {
                    if (!HandleFailure(ex, ref attempt, operationName, maxRetries, baseDelayMs, cancellationToken))
                        throw;
                }
            }
        }

        /// <summary>
        ///     Decides whether another attempt should be made. Returns false (caller rethrows) when
        ///     the retry budget is exhausted or the failure is one we must not swallow. Otherwise it
        ///     sleeps for the backoff interval and returns true.
        /// </summary>
        private static bool HandleFailure(Exception ex, ref int attempt, string operationName,
            int maxRetries, int baseDelayMs, CancellationToken cancellationToken)
        {
            // .Result/.Wait() wrap the real exception in an AggregateException.
            var inner = (ex as AggregateException)?.Flatten().InnerException ?? ex;

            // Cancellation is never a transient failure - propagate it so shutdown is immediate.
            if (inner is OperationCanceledException)
                throw inner;

            attempt++;
            if (attempt > maxRetries)
            {
                Logger.Write($"{operationName} failed after {maxRetries} retries: {inner.Message}", LogLevel.Error);
                return false;
            }

            var backoff = Math.Min((int) (baseDelayMs * Math.Pow(2, attempt - 1)), MaxBackoffMs);
            // Add up to 30% jitter so retries from multiple call sites don't synchronize.
            backoff += Jitter.Next(0, backoff / 3 + 1);

            Logger.Write(
                $"{operationName} failed (attempt {attempt}/{maxRetries}): {inner.Message}. Retrying in {backoff} ms...",
                LogLevel.Warning);

            // Cancellation-aware sleep: returns early (true) if shutdown was requested while waiting.
            if (cancellationToken.WaitHandle.WaitOne(backoff))
                cancellationToken.ThrowIfCancellationRequested();

            return true;
        }
    }
}
