using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.State;

namespace PoGo.NecroBot.CLI
{
    /// <summary>
    ///     No-op stand-in for the original SuperSocket-based WebSocket server (SuperSocket is a
    ///     Windows/.NET-Framework dependency). The hook is preserved so the event wiring in
    ///     Program.cs still compiles; broadcasting can be reimplemented on System.Net.WebSockets
    ///     when needed. Disabled by default (GlobalSettings.EnableWebSocket = false).
    /// </summary>
    public class WebSocketInterface
    {
        public WebSocketInterface(int port)
        {
            Logic.Logging.Logger.Write(
                $"WebSocket interface is stubbed out in this .NET 8 build (requested port {port}).",
                Logic.Logging.LogLevel.Warning);
        }

        public void Listen(IEvent evt, Context ctx)
        {
            // Intentionally empty - no clients to broadcast to in the stub build.
        }
    }
}
