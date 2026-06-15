#region using directives

using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using PoGo.NecroBot.Logic;
using PoGo.NecroBot.Logic.Logging;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.CLI
{
    internal class Program
    {
        public static void LoginWithGoogle(string usercode, string uri)
        {
            try
            {
                Logger.Write("Google Device Code copied to clipboard");
                Thread.Sleep(2000);
                Process.Start(uri);
                var thread = new Thread(() => Clipboard.SetText(usercode)); //Copy device code
                thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                thread.Start();
                thread.Join();
            }
            catch (Exception)
            {
                Logger.Write("Couldnt copy to clipboard, do it manually", LogLevel.Error);
                Logger.Write($"Goto: {uri} & enter {usercode}", LogLevel.Error);
            }
        }

        private static void Main()
        {
            Logger.SetLogger(new ConsoleLogger(LogLevel.Info));

            GlobalSettings settings = GlobalSettings.Load("\\config\\config.json");

            var machine = new StateMachine();
            var stats = new Statistics();
            stats.DirtyEvent += () => Console.Title = stats.ToString();

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