# HUD Clock

A client-side HUD mod for [Vintage Story](https://www.vintagestory.at/) 1.22.0. Displays in-game time, weather, temporal storms, temporal rift activity, room/shelter status, and land claim ownership.

> **4.0.0 is a complete rewrite.** Settings and keybinds from earlier versions will not carry over.

## Requirements

- Vintage Story 1.22.0 or later
- .NET 10 SDK (for building from source)

## Development setup

1. Install the Vintage Story client at a known location.
2. Set the `VINTAGE_STORY` environment variable to your install path:
   - Windows (PowerShell): `[Environment]::SetEnvironmentVariable("VINTAGE_STORY", "F:\VintageStory\Client_v1.22.0\Vintagestory", "User")`
   - Linux/macOS: `export VINTAGE_STORY=~/.local/share/Vintagestory`
3. Restart your IDE so it picks up the new variable.
4. Open `HudClock.sln`.

If `VINTAGE_STORY` is not set, `Directory.Build.props` falls back to `F:\VintageStory\Client_v1.22.0\Vintagestory` on Windows, so the default developer machine needs no configuration.

## Building

```powershell
dotnet build HudClock.sln -c Release
```

Build output at `src/HudClock/bin/Release/net10.0/` is a complete, loadable mod folder (DLL + `modinfo.json` + assets). By default the build also deploys the mod to `$(VINTAGE_STORY)\Mods\HudClock` so it's picked up by the game on next launch. Disable with `/p:DeployMod=false`.

## Debugging

Press **F5** in Visual Studio. The `Vintage Story` launch profile runs `%VINTAGE_STORY%\Vintagestory.exe` with the debugger attached; the post-build deploy puts the freshly-built mod into the game's Mods folder first.

## Packaging a release

```powershell
./build/package.ps1 -Configuration Release -Version 4.0.0
```

Produces `build/dist/HudClock_4.0.0.zip`, ready to upload to the Vintage Story mod portal.

## Tests

```powershell
dotnet test HudClock.sln -c Release
```

## License

MIT — see [LICENSE](LICENSE).
