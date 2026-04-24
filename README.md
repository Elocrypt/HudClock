<div align="center">

# HUD Clock

**A compact, configurable heads-up display for [Vintage Story](https://www.vintagestory.at/).**

In-game time · date · season · temperature · wind · rift activity · temporal storms · shelter detection · land-claim ownership · online players.

[![CI](https://github.com/Elocrypt/HudClock/actions/workflows/ci.yml/badge.svg)](https://github.com/Elocrypt/HudClock/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Elocrypt/HudClock?include_prereleases)](https://github.com/Elocrypt/HudClock/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![VS 1.22.0](https://img.shields.io/badge/Vintage%20Story-1.22.0-purple)](https://www.vintagestory.at/)

</div>

---

> **4.0.0 is a complete rewrite.** The mod now targets Vintage Story 1.22.0 on .NET 10 and uses a fresh MVVM architecture. Settings and keybinds from earlier versions will not carry over — reconfigure on first launch.

## Features

<table>
<tr>
<td width="50%" valign="top">

### Time & calendar
- In-game **date**, **time**, and **season**, with a large seasonal illustration that tracks the current season
- 12-hour or 24-hour format toggle
- Optional **real-time clock** line for quick glances at real-world time

### Weather
- Current **temperature** at the player's position, Celsius or Fahrenheit
- **Wind speed** as either a Beaufort-scale descriptor or a percentage
- Clean, textured icons for spring, summer, fall, and winter

### Temporal storms
- Separate storm dialog with countdown to the next storm, rendered as `hh:mm` of in-game time
- **Per-in-game-minute** countdown updates
- Visibility modes: *always shown*, *only when a storm is imminent or active*, or *hidden*
- Independent screen anchor so you can park it in a different corner than the main HUD

</td>
<td width="50%" valign="top">

### Temporal rifts
- Current rift activity level, read from the world's rift system
- Toggles: always shown, shown only when the world has rifts enabled, or hidden

### Shelter detection
- Small badge appears next to the HUD when the player is standing in a **valid closed room**
- Recognizes generic rooms, **cellars**, and **greenhouses** with separate icons

### Land claims
- Discreet banner that names the claim owner whenever the player steps into a claimed area
- Banner auto-hides when the player leaves the claim

### Multiplayer
- Optional **online player count** line — automatically hidden in single-player

### Layout
- Any of six screen anchors: top-left, top-center, top-right, bottom-left, bottom-center, bottom-right
- Offsets tuned to avoid the minimap, hotbar, hover tooltip, and coordinate readout
- Individual lines can be toggled off one at a time

### Appearance
- **Icon theme selector** — pick between *Modern* (refreshed 4.x art) or *Classic* (the original 3.x art) from settings
- Season, storm, and room-indicator icons all swap together when the theme changes

</td>
</tr>
</table>

## Install

1. Download the latest `HudClock_<version>.zip` from the [Releases](https://github.com/Elocrypt/HudClock/releases) page.
2. Drop the zip (don't extract it) into your Vintage Story `Mods/` folder:
   - **Windows:** `%AppData%\VintagestoryData\Mods`
   - **Linux:** `~/.config/VintagestoryData/Mods`
   - **macOS:** `~/Library/Application Support/VintagestoryData/Mods`
3. Launch Vintage Story. The HUD appears in the top-left by default.

Client-side only — no server install required.

## Using it

- **Settings dialog:** <kbd>Shift</kbd>+<kbd>A</kbd> (rebindable in the game's control settings).
- **Toggle the main HUD:** <kbd>Ctrl</kbd>+<kbd>G</kbd> (rebindable).
- **Toggle the storm dialog:** <kbd>Ctrl</kbd>+<kbd>[</kbd> (rebindable).

Everything else lives in the settings dialog — line visibility, HUD position, storm/rift policy, temperature unit, wind format, time format. Changes save automatically and persist across worlds.

Settings are stored at:
- **Windows:** `%AppData%\VintagestoryData\ModConfig\hudclock\settings.json`
- **Linux:** `~/.config/VintagestoryData/ModConfig/hudclock/settings.json`

Editing the JSON directly while the game is running works but a settings-dialog action will overwrite your changes. Prefer the dialog unless you're scripting configurations.

## Compatibility

- **Vintage Story 1.22.0** or later. Earlier versions aren't supported — the [3.x line](https://github.com/Elocrypt/HudClock/tree/v3) runs on 1.21 and below.
- Client-side only. Safe to use on multiplayer servers the mod isn't installed on.
- No known conflicts. The mod reads from (but never writes to) VS's temporal-storm and rift systems via reflection, which is why each VS version is pinned explicitly — an API shape change will gracefully degrade those two lines rather than crash the HUD.

## Languages

English, German, French, Italian, Japanese, Polish, Portuguese (Brazil), Portuguese (Portugal), Russian, Ukrainian. The game picks the right language automatically based on your client locale. Contributions welcome.

---

<details>
<summary><b>Building from source</b></summary>

### Requirements

- Vintage Story 1.22.0 or later (for the referenced game DLLs)
- .NET 10 SDK

### Setup

1. Install the Vintage Story client at a known location.
2. Set environment variables pointing at your install and data directories:
   - `VINTAGE_STORY` — the game install directory (contains `Vintagestory.exe`).
   - `VINTAGE_STORY_DATA` — the data directory VS creates/uses (contains `Mods/`, `ModConfig/`, `Saves/`). If your shortcut launches with `--datapath .\v`, this is `<install>\v`.

   ```powershell
   # Windows (PowerShell)
   [Environment]::SetEnvironmentVariable("VINTAGE_STORY",      "F:\VintageStory\Client_v1.22.0\Vintagestory",   "User")
   [Environment]::SetEnvironmentVariable("VINTAGE_STORY_DATA", "F:\VintageStory\Client_v1.22.0\Vintagestory\v", "User")
   ```

3. Restart your IDE so it picks up the new variables.
4. Open `HudClock.sln`.

If the variables are not set, `Directory.Build.props` falls back to `F:\VintageStory\Client_v1.22.0\Vintagestory` (install) and `<install>\v` (data) on Windows, so the default developer machine needs no configuration.

### Build

```powershell
dotnet build HudClock.sln -c Release
```

Build output at `src/HudClock/bin/Release/net10.0/` is a complete, loadable mod folder (DLL + `modinfo.json` + assets). By default the build also deploys the mod to `$(VINTAGE_STORY_DATA)\Mods\HudClock` (the user-mods folder under the active data path) so it's picked up by the game on next launch. Disable with `/p:DeployMod=false`.

### Debug

Press <kbd>F5</kbd> in Visual Studio. The `Vintage Story` launch profile runs `%VINTAGE_STORY%\Vintagestory.exe --datapath .\v` with the debugger attached; the post-build deploy puts the freshly-built mod into the matching data path's `Mods` folder first. If you use a different datapath, edit `src/HudClock/Properties/launchSettings.json`.

### Test

```powershell
dotnet test HudClock.sln -c Release
```

133 tests across calendar formatting, the storm dialog state machine, settings defaults, lang-key enum coverage, reflective field access, and bounds building.

### Package a release

```powershell
./build/package.ps1 -Configuration Release -Version 4.0.0
```

Produces `build/dist/HudClock_4.0.0.zip`, ready to upload to the Vintage Story mod portal. On push of a tag matching `v*.*.*`, GitHub Actions runs the same script and publishes a release automatically — see `.github/workflows/release.yml`.

### Architecture

The codebase is layered:

- **`Domain/`** — pure, framework-free models and services: calendar snapshots, storm status, weather readings, room status. Each service sits behind an interface so the presentation layer can be unit-tested against fakes.
- **`Infrastructure/`** — glue to Vintage Story: icon caching, keybind registration, reflective field access, settings persistence.
- **`Presentation/`** — view + viewmodel + controller triples for each dialog. Views own the `GuiComposer`; viewmodels hold strings; controllers wire ticks and events. See `Presentation/Shared/` for the anchor-offset table and bounds builder.
- **`Configuration/`** — the single settings document, with one `*Options` class per feature group.

Adding a new HUD line: add a property to the right `*Options`, expose a string property on `MainHudViewModel`, register the line key in `MainHudLineKey`, and add a row in the settings dialog.

</details>

## License

MIT — see [LICENSE](LICENSE). Icons and textures are original work and ship under the same license.

## Credits

- Original **Simple HUD Clock** by [Daniel Kellerdt](https://github.com/dakellerdt) (3.x and earlier), maintained across multiple Vintage Story versions.
- 4.0 rewrite by [Elocrypt](https://github.com/Elocrypt).
