#region using directives

using System;
using System.IO;
using PoGo.NecroBot.Logic;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.CLI
{
    internal class Program
    {
        // Cross-platform: just print the Google device code + URL. (The original copied the
        // code to the Windows clipboard and launched a browser; neither is portable, and the
        // stub client never triggers Google device-code auth anyway.)
        public static void LoginWithGoogle(string usercode, string uri)
        {
            Logger.Write($"Google login required - go to: {uri} and enter code: {usercode}", LogLevel.Warning);
        }

        private static void Main()
        {
            Logger.SetLogger(new ConsoleLogger(LogLevel.Info));

            GlobalSettings settings = GlobalSettings.Load(Path.Combine("config", "config.json"));

            var machine = new StateMachine();
            var stats = new Statistics();
            stats.DirtyEvent += () =>
            {
                try { Console.Title = stats.ToString(); }
                catch { /* Console.Title is a no-op / throws when output is redirected */ }
            };

            var aggregator = new StatisticsAggregator(stats);
            var listener = new ConsoleEventListener();

            machine.EventListener += listener.Listen;
            machine.EventListener += aggregator.Listen;

            if (settings.EnableWebSocket)
            {
                var websocket = new WebSocketInterface(settings.WebSocketPort);
                machine.EventListener += websocket.Listen;
            }

            machine.SetFailureState(new LoginState());

            var context = new Context(new ClientSettings(settings), new LogicSettings(settings));
            context.Client.Login.GoogleDeviceCodeEvent += LoginWithGoogle;

            var botTask = machine.AsyncStart(new VersionCheckState(), context);

            // Graceful shutdown: Ctrl+C (or Ctrl+Break) asks the state machine to stop after its
            // current step instead of hard-killing the process in the middle of an API call.
            var shutdownRequested = false;
            Console.CancelKeyPress += (sender, e) =>
            {
                if (shutdownRequested)
                    return; // second Ctrl+C: let the default handler kill the process
                shutdownRequested = true;
                e.Cancel = true; // keep the process alive so we can shut down cleanly
                Logger.Write("Shutdown requested - stopping bot gracefully (press Ctrl+C again to force quit)...",
                    LogLevel.Warning);
                machine.Stop();
            };

            // Block until the bot loop exits (either via graceful shutdown or completion).
            botTask.Wait();
        }
    }
}
