# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [4.2.2] - TBD

### Added

- **Custom icon theme.** A third option in the icon-theme dropdown alongside Modern and Classic. Selects textures from `assets/hudclock/textures/hud/custom/` (named `spring.png`, `summer.png`, `fall.png`, `winter.png`, `tempstorm.png`) and `assets/hudclock/textures/room/custom/` (named `room.png`, `cellar.png`, `greenhouse.png`). Mod ships transparent placeholders so the HUD layout still renders cleanly when no override is present; users override individual textures by either dropping PNGs directly into the mod's asset folders or — recommended — shipping a standard Vintage Story texture pack that overrides the same paths. No external dependencies, no extra libraries.
- Custom theme reserves the same 200×200 season-icon column as Modern. Texture-pack authors can ship art at any resolution and Vintage Story scales it to fit.

## [4.2.1] - 2026/04/25

### Fixed

- HUD layout now dynamically adjusts the season icon size based on the selected theme. The layout builder correctly allocates 200px for the Modern theme and 100px for the Classic theme, resolving an issue where the Classic theme was forced into a hardcoded 200px bound.

## [4.2.0] - 2026/04/25

### Added

- **Body temperature comfort indicator.** Optional new HUD line that warns when the player is getting cold. Hidden under comfortable conditions (matches Status HUD Continued's convention). Shows `cool` with the deviation from normal when below 37 °C raw, and `FREEZING` when at or below 33 °C raw — the same threshold the survival mod uses to start freezing damage. Displays the deviation in the player's chosen unit (°C or °F via the existing Weather toggle). The HUD deliberately does not show an absolute body temperature: the raw watched-attribute value differs from the character GUI's "Body Temperature" display by design (the GUI assembles its number client-side from clothing, wetness, and climate inputs that aren't a single attribute), and a comfort indicator is more actionable than a number that disagrees with vanilla.
- **Intoxication line.** Optional new HUD line showing the player's current intoxication as a percentage. Hidden while sober (matches the Status HUD Continued convention) so the line doesn't sit at "0%" most of a playthrough.
- **Rainfall line.** Optional new HUD line showing current rainfall using the same labels as the vanilla Environment dialog (Rare, Light, Moderate, High, Very high). Off by default; toggle in the Weather section.
- New **Player stats** section in the settings dialog containing the body-temp and intoxication toggles. Both default off — they're survival-mode features that opt in.
- Spanish localization (`es-es` and `es-419`).

### Changed

- Internal `TemperatureString` formatter now takes the value as a parameter so world temp and body temp can share the unit-conversion path.

### Fixed

- Intoxication line no longer requires the settings dialog to be opened and closed before showing or hiding when intoxication crosses zero. The HUD now detects when the set of visible lines changes between ticks and triggers a rebuild automatically.

## [4.1.0] - 2026/04/24

### Added

- **Icon theme selector.** A new Appearance section in the settings dialog lets you choose between the refreshed 4.x art (*Modern*, default) and the original 3.x art (*Classic*). Switching themes updates the HUD immediately and affects the season background, the storm dialog icon, and the room-indicator icons together.

### Changed

- Asset layout reorganized from flat `textures/hud/*.png` and `textures/room/*.png` into theme subfolders (`modern/` and `classic/`). Users don't see this; mod authors referencing these paths directly will need to update.

## [4.0.0] - 2026/04/24

Complete rewrite of the mod on a new architecture. Feature parity with 3.x is preserved; internal structure is not. Settings and keybinds do not migrate from earlier versions — reconfigure via the settings dialog (default `Shift+O`) on first launch.

### Added

- Separate HUD position anchor for the storm dialog, independent of the main HUD position. Either can be placed in any of the six screen corners (top/bottom × left/center/right).
- Per-minute countdown updates on the approaching-storm timer. The text re-renders at in-game-minute resolution (every second in real-time at default world speed) instead of the previous 5-second cadence.
- Automatic icon refresh when the season or room-status changes. Icons update within one HUD tick (≤2.5 s) without needing to reopen the settings dialog.
- New large season icons (200 × 100) with refreshed art.

### Changed

- Rebuilt on Vintage Story 1.22.0 and .NET 10.
- New three-layer architecture (Domain / Infrastructure / Presentation) with MVVM dialogs and 133 unit tests covering the formatting, state-machine, and bounds-builder logic.
- Unified settings file — keybinds and options now live in a single JSON document under `ModConfig/hudclock/`.
- HUD no longer recomposes its GUI every tick; text content updates in place via `SetNewText`, and a full rebuild happens only on settings changes or icon-state changes.
- Centralized texture and icon ownership — the icon cache owns all `BitmapRef` instances and disposes them on world-leave.
- Reflection into `SystemTemporalStability` and `ModSystemRiftWeather` now uses compiled accessors cached at startup rather than repeated `GetField` lookups.
- HUD positioning now uses fixed per-anchor offsets applied via `WithFixedAlignmentOffset` (the documented VS API) instead of walking the dialog list and writing to engine-internal `absOffsetY` fields. Offsets are tunable in a single constants file.
- Storm HUD stacks below the main HUD when both share an anchor; when they sit in different corners, each uses its solo anchor offset.

### Fixed

- Storm HUD no longer auto-closes during an active storm when storm visibility is set to *Trigger only*.
- Season detection no longer performs a dummy zero-volume block search.
- HUD-reposition-below-other-dialogs no longer runs every 100 ms while the HUD is open.
- Main HUD no longer disappears permanently when all visible lines are toggled off and then one is toggled back on.
- Storm countdown no longer jumps by 5-minute increments on slow polls; it updates per in-game minute.
- Season icon and room-status icon no longer stay frozen until the settings dialog is reopened.
- HUD and storm dialog no longer overlap the minimap, hotbar, hover tooltip, or claim banner when placed in their respective corners at the default GUI scale.

### Removed

- Settings migration from 3.x original and 1.x patch is not supported; existing settings reset on first run.
- Internal `SharedLibrary` helpers that were no longer used.
- Harmony reference (no patches required).

[Unreleased]: https://github.com/Elocrypt/HudClock/compare/v4.2.2...HEAD
[4.2.2]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.2
[4.2.1]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.1
[4.2.0]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.0
[4.1.0]: https://github.com/Elocrypt/HudClock/releases/tag/v4.1.0
[4.0.0]: https://github.com/Elocrypt/HudClock/releases/tag/v4.0.0
