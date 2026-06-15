# NecroBot Modernization Plan (.NET Framework 4.5 → .NET 8)

This document is a concrete, ordered checklist for porting NecroBot from its
current **.NET Framework 4.5 / Windows-only** form to a modern, cross-platform
**.NET 8** build. It is written against the code as it actually exists in this
repo, calling out the specific files and dependencies that block the port.

> **Reality check.** This is a *modernization / learning* exercise. The bot will
> still not connect to live Pokémon GO — that depends on the un-maintained
> `FeroxRev/Pokemon-Go-Rocket-API` submodule and a request-signing ("hashing")
> service that no longer exists in this era of the code. See
> [Why it won't connect](#appendix-why-it-wont-connect-to-live-pokémon-go).

---

## ✅ STATUS: the stub build is implemented and verified

Both projects are now **SDK-style, target `net8.0`, build with 0 errors, and run
end-to-end** against a fake client. The reverse-engineered `FeroxRev` submodule
and the `POGOProtos` protobufs are replaced by hand-written stand-ins under
`PoGo.NecroBot.Logic/Stubs/` (`PoGoProtos.cs`, `RocketApi.cs`, `GeoCoordinate.cs`).
The fake `PokemonGo.RocketAPI.Client` returns canned, self-consistent data so the
full loop exercises real code paths.

### Build & run

```bash
dotnet build NecroBot.sln
dotnet run --project PoGo.NecroBot.CLI      # Ctrl+C to stop gracefully
```

Expected: version check → login (`Playing as StubTrainer`) → display best Pokémon
→ recycle → walk to a stub Pokéstop → catch Pokémon while walking → spin the stop
→ transfer duplicates → loop. Ctrl+C prints `Bot stopped.` and exits 0.

### What the stub step replaced (matches §1 / §3 below)

- `System.Windows.Forms` clipboard → removed (`Program.LoginWithGoogle` prints only).
- `System.Device.Location.GeoCoordinate` → `Stubs/GeoCoordinate.cs` (Haversine).
- `SuperSocket` WebSocket → `WebSocketInterface` is a no-op stub.
- `log4net` / `App.config` / `packages.config` → removed; console logging only.
- `ConsoleLogger` now uses UTF-8 and tolerates redirected stdout.
- The game API + protos → `Stubs/RocketApi.cs` + `Stubs/PoGoProtos.cs`.

### Done (polish)

- ✅ Windows-style `\\config\\…` / `\\Logs\\…` / `\\Configs\\…` / `\\auth.json`
  paths replaced with `Path.Combine` for clean cross-platform output.
- ✅ Obsolete `WebClient` in `VersionCheckState` replaced with a static `HttpClient`
  (async, 10s timeout).
- ✅ xUnit project (`PoGo.NecroBot.Tests`, 16 tests) covers the catch / transfer /
  evolve / recycle decision logic against the stub client.

---

## 0. Current state (what you're starting from)

| Project | Target | Output |
| --- | --- | --- |
| `PoGo.NecroBot.Logic` | `net45` (classic `.csproj`) | class library |
| `PoGo.NecroBot.CLI` | `net45` (classic `.csproj`) | console exe |
| `FeroxRev/` (submodule) | `net45` | the actual game API client (`PokemonGo.RocketAPI`) — **not checked out** |

Already done in this branch (prerequisites that make the port easier):

- The core loop is now **async/await** end-to-end (no `.Result`/`.Wait()` in the
  state/task layer). .NET 8 is async-first, so this removes the biggest source
  of friction and deadlock risk.
- Network calls are wrapped with retry/backoff (`RetryUtils`) and graceful
  cancellation (`CancellationToken`), which map cleanly onto modern hosting.

---

## 1. Blocking dependencies and their replacements

These are the Windows/.NET-Framework-only APIs in the codebase. Each must be
replaced or abstracted before the projects will compile on .NET 8.

| Dependency | Used in | .NET 8 replacement |
| --- | --- | --- |
| `System.Windows.Forms` (`Clipboard`, STA thread) | `Program.cs` (`LoginWithGoogle`) | Drop the clipboard copy; just print the device code + URL. Clipboard isn't meaningful headless/cross-platform. |
| `System.Device.Location.GeoCoordinate` | `Navigation.cs`, `FarmPokestops*Task.cs` | Add a tiny `GeoCoordinate` struct (Lat/Lng/Alt) in `Utils/`, or use `GeoCoordinatePortable` NuGet. Only `CalculateDistanceInMeters`/bearing helpers are used. |
| `log4net` | `PoGo.NecroBot.CLI/Config/*.config`, `ConsoleLogger` | `Microsoft.Extensions.Logging` (+ `Microsoft.Extensions.Logging.Console`). The existing `ILogger`/`Logger` wrapper already abstracts this — swap the implementation behind it. |
| `SuperSocket` / `WebSocket4Net` | `WebSocketInterface.cs` | `System.Net.WebSockets` (built in) or `Microsoft.AspNetCore` minimal host. Optional feature — can be stubbed/removed for first build. |
| `App.config` / `app.config` | both projects | Delete; use `appsettings.json` + `Microsoft.Extensions.Configuration`, or keep the existing hand-rolled `GlobalSettings.Load(json)`. |
| `Microsoft.CSharp` (`RuntimeBinder`) | `LogBestPokemonTask.cs` (unused `using`) | Remove the stray using; not needed. |
| `protobuf` / `POGOProtos` (via submodule) | everywhere | Reference a `net8.0`-targeted protobuf build, or multi-target the protos. This is the hard one — see §3. |

---

## 2. Mechanical project-file conversion

1. **Convert both `.csproj` to SDK-style.** Replace the verbose classic project
   files with:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>net8.0</TargetFramework>
       <LangVersion>latest</LangVersion>
       <Nullable>enable</Nullable>   <!-- optional; expect many warnings at first -->
     </PropertyGroup>
   </Project>
   ```
   SDK-style projects glob `**/*.cs` automatically, so the explicit
   `<Compile Include=...>` lists (including the `Utils\RetryUtils.cs` entry) all
   disappear.
2. **Drop `AssemblyInfo.cs`** attributes that the SDK now generates
   (`AssemblyVersion`, `AssemblyTitle`, …) to avoid duplicate-attribute errors,
   or set `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`.
3. **Replace `packages.config`** with `<PackageReference>` items.
4. **`CLI` → `<OutputType>Exe</OutputType>`**, `Logic` stays a library.

---

## 3. The API client (the real work)

The game client lives in the `FeroxRev` submodule and is what every
`ctx.Client.*` / `ctx.Inventory.*` call ultimately hits. Options, in order of
effort:

1. **Stub it (recommended first step).** Define the `Client`, `Inventory`,
   `ISettings`, and the `POGOProtos` response types as interfaces/DTOs and a
   no-op/fake implementation. This lets the **Logic + CLI** projects compile and
   run on .NET 8 end-to-end (state machine, tasks, logging, events, graceful
   shutdown) against a simulated game — ideal as a learning/portfolio artifact
   and unit-testable.
2. **Port the submodule.** Multi-target `PokemonGo.RocketAPI` to `net8.0`,
   swap its protobuf to `Google.Protobuf` (net8-compatible), and replace its
   `HttpClient`/handler plumbing. Large, and still non-functional against live
   servers without a hashing service.
3. **Swap in a maintained fork.** Out of scope here and a permanent treadmill —
   Niantic actively breaks these.

> For a buildable, testable modernization, **stop at option 1.** It exercises
> 100% of the code we refactored without chasing a dead protocol.

---

## 4. Suggested order of operations

1. Branch. Convert `Logic.csproj` to SDK-style targeting `net8.0`; expect it to
   fail only on the §1 dependencies.
2. Add the `GeoCoordinate` struct (or NuGet) → fixes `Navigation` + farm tasks.
3. Swap `log4net` → `Microsoft.Extensions.Logging` behind the existing `ILogger`.
4. Stub the API client + protos (§3 option 1) so `Logic` compiles.
5. Convert `CLI.csproj`; remove `System.Windows.Forms`, simplify
   `LoginWithGoogle` to print-only; wire up console logging + config.
6. Stub or port `WebSocketInterface` (or compile it out behind a feature flag).
7. `dotnet build`, then add a small **xUnit** project: the now-async, guarded
   `TransferDuplicatePokemonTask` / `EvolvePokemonTask` / `GetBestBall` logic is
   very testable against the stub client.
8. `dotnet run` — verify the state machine drives `Login → Info → PositionCheck
   → Farm` against the fake client and that Ctrl+C shuts down gracefully.

---

## 5. Definition of done

- [ ] `dotnet build` succeeds on Linux/macOS/Windows with no Framework-only refs.
- [ ] `dotnet run` starts, logs, loops, and shuts down cleanly on Ctrl+C.
- [ ] A unit-test project covers the catch/transfer/evolve decision logic.
- [ ] No `System.Windows.Forms` / `System.Device.Location` / `log4net` /
      `packages.config` / `App.config` remaining.
- [ ] README updated to state it runs against a simulated client only.

---

## Appendix: why it won't connect to live Pokémon GO

There is no official Pokémon GO API or API key. This code talks to the game by
reverse-engineering its private protocol via the `FeroxRev` submodule. Shortly
after this code's era (mid-2016), Niantic began requiring **signed requests**
("unknown6"), which bots satisfied via paid third-party **hashing services** —
the thing people colloquially called an "API key." This version predates that
enforcement and has no hashing support, those services are defunct, the protocol
has changed many times since, and Niantic bans automation. Modernizing the
*codebase* is worthwhile as an engineering exercise; reconnecting it to the live
game is not a key swap and is out of scope.
