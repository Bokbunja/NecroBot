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
        ///     Await an asynchronous, value-returning API call, retrying transient failures with
        ///     exponential backoff.
        /// </summary>
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, string operationName,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxRetries = DefaultMaxRetries, int baseDelayMs = DefaultBaseDelayMs)
        {
            var attempt = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    attempt = await HandleFailureAsync(ex, attempt, operationName, maxRetries, baseDelayMs,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///     Await an asynchronous, void API call, retrying transient failures with exponential backoff.
        /// </summary>
        public static async Task ExecuteAsync(Func<Task> action, string operationName,
            CancellationToken cancellationToken = default(CancellationToken),
            int maxRetries = DefaultMaxRetries, int baseDelayMs = DefaultBaseDelayMs)
        {
            var attempt = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    attempt = await HandleFailureAsync(ex, attempt, operationName, maxRetries, baseDelayMs,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///     Either waits for the backoff interval and returns the incremented attempt count, or
        ///     rethrows when the retry budget is exhausted / the failure must not be swallowed.
        /// </summary>
        private static async Task<int> HandleFailureAsync(Exception ex, int attempt, string operationName,
            int maxRetries, int baseDelayMs, CancellationToken cancellationToken)
        {
            // await unwraps most exceptions, but defensively flatten anything that arrives aggregated.
            var inner = (ex as AggregateException)?.Flatten().InnerException ?? ex;

            // Cancellation is never a transient failure - propagate it so shutdown is immediate.
            if (inner is OperationCanceledException)
                throw inner;

            attempt++;
            if (attempt > maxRetries)
            {
                Logger.Write($"{operationName} failed after {maxRetries} retries: {inner.Message}", LogLevel.Error);
                throw inner;
            }

            var backoff = Math.Min((int) (baseDelayMs * Math.Pow(2, attempt - 1)), MaxBackoffMs);
            // Add up to 30% jitter so retries from multiple call sites don't synchronize.
            backoff += Jitter.Next(0, backoff / 3 + 1);

            Logger.Write(
                $"{operationName} failed (attempt {attempt}/{maxRetries}): {inner.Message}. Retrying in {backoff} ms...",
                LogLevel.Warning);

            await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
            return attempt;
        }
    }
}
