#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;

#endregion

namespace PoGo.NecroBot.Logic.State
{
    public delegate void StateMachineEventDeletate(IEvent evt, Context ctx);

    public class StateMachine
    {
        private const int MaxFailureBackoffMs = 5 * 60 * 1000; // cap repeated-failure backoff at 5 minutes

        private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private Context _ctx;
        private int _delay;
        private IState _initialState;

        /// <summary>
        ///     Token that is cancelled when <see cref="Stop" /> is called. Tasks should pass this to
        ///     <see cref="Utils.RetryUtils" /> and <see cref="Utils.JitterUtils" /> so that long waits
        ///     and retries abort promptly on shutdown.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationSource.Token;

        public Task AsyncStart(IState initialState, Context ctx)
        {
            return Task.Run(async () => await Start(initialState, ctx));
        }

        public event StateMachineEventDeletate EventListener;

        public void Fire(IEvent evt)
        {
            EventListener?.Invoke(evt, _ctx);
        }

        public void RequestDelay(int delay)
        {
            _delay = delay;
        }

        public void SetFailureState(IState state)
        {
            _initialState = state;
        }

        /// <summary>
        ///     Requests a graceful shutdown. The run loop finishes its current step (or aborts the
        ///     current wait) and then exits cleanly.
        /// </summary>
        public void Stop()
        {
            _cancellationSource.Cancel();
        }

        public async Task Start(IState initialState, Context ctx)
        {
            _ctx = ctx;
            var state = initialState;
            var consecutiveFailures = 0;
            var token = _cancellationSource.Token;

            do
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    state = await state.Execute(ctx, this);
                    consecutiveFailures = 0;

                    if (await WaitOrCancelled(_delay, token))
                        break;
                    _delay = 0;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // A cancellation may surface wrapped in an AggregateException (e.g. from a
                    // walking callback). Treat that as a clean shutdown.
                    if (token.IsCancellationRequested)
                        break;

                    Fire(new ErrorEvent {Message = ex.ToString()});

                    // Back off exponentially on repeated failures so we don't hammer the servers
                    // (or spin in a tight loop) when the network or account is having problems.
                    consecutiveFailures++;
                    var backoff = Math.Min((int) (1000 * Math.Pow(2, consecutiveFailures - 1)), MaxFailureBackoffMs);
                    Fire(new NoticeEvent
                    {
                        Message = $"Recovering from error (failure #{consecutiveFailures}). Retrying in {backoff / 1000}s..."
                    });

                    if (await WaitOrCancelled(backoff, token))
                        break;

                    state = _initialState;
                }
            } while (state != null);

            Fire(new NoticeEvent {Message = "Bot stopped."});
        }

        /// <summary>
        ///     Awaits for <paramref name="milliseconds" /> unless cancellation is requested first.
        ///     Returns true if a shutdown was requested (caller should stop looping).
        /// </summary>
        private static async Task<bool> WaitOrCancelled(int milliseconds, CancellationToken token)
        {
            if (milliseconds <= 0)
                return token.IsCancellationRequested;
            try
            {
                await Task.Delay(milliseconds, token);
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }
    }
}
