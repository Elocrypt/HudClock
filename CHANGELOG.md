# Changelog
<!-- markdownlint-disable MD024 -->
All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [4.5.2] - 2026/11/27

Release: [4.5.2-tag]

### Fixed

- Room indicator icon drifted to the right, outside the HUD panel, after lowering the GUI scale. It was aligned to the panel's bottom-right corner, and VS sizes an auto-sized dialog using each aligned child's position from the previous layout, so the old scale's offset widened the panel. The icon now uses fixed coordinates.

## [4.5.1] - 2026/11/26

Release: [4.5.1-tag]

### Added

- Czech translation, contributed by [DejVIIK-Original](https://github.com/DejVIIK-Original) ([#13](https://github.com/Elocrypt/HudClock/issues/13)).

## [4.5.0] - 2026/11/17

Release: [4.5.0-tag]

### Added

- **Calendar mod support.** Month names now come from the game's own `month-*` lang keys first, so calendar theme mods that rename the vanilla months (Elder Scrolls Calendar, Dwarf Fortress Calendar, and any others) show up on the HUD with no extra setup. HUD Clock's own month names remain as a fallback. Side effect: months now match the vanilla UI exactly in every locale (Russian and Polish use the genitive form, e.g. `5 сентября`; their date format dropped the dot to fit).
- **"Autumn" for non-North-American English.** The English HUD now says "Autumn" instead of "Fall" automatically when the Windows region is outside the US and Canada. The game has a single English locale, so the OS region is the only signal available. Override with `"UseAutumn": true` or `false` under `Appearance` in the settings file; leave it `null` for automatic.
- **Custom icons from ModConfig.** The Custom icon theme now loads PNGs from `ModConfig/hudclock/custom/hud/` and `ModConfig/hudclock/custom/room/`, so custom art survives mod updates instead of living inside the zip. The folder and a README listing the expected filenames are created the first time Custom is selected. Texture packs overriding `hudclock:textures/.../custom/` still work.
- Warning when the original Simple HUD Clock (mod ID `hudclock`, 3.x by Rhonen) is installed alongside this mod. That mod is unmaintained, doesn't support 1.22, and crashes with a `MissingMethodException` (`GuiComposerHelpers.AddDialogTitleBar`) when its settings dialog opens; it also shares this mod's asset domain, config folder and hotkey codes. HUD Clock now logs a warning at startup and posts one chat message on world join telling the player to remove it. Translated into all 13 locales.

### Fixed

- Settings dialog opened off-center (and pinned to the top of the screen) at most GUI scales. The centering math was correct; something outside HUD Clock moved the dialog to a fixed spot before its first frame. HUD Clock now reasserts the centered position for the first few frames after opening. Dialogs switched to "Movable" keep their saved position.
- Custom icon theme showed no season/room icon for any slot without custom art (the shipped placeholders are transparent). Missing or fully transparent custom icons now fall back to the Modern art.

### Changed

- Development builds now reference the VS 1.22.7 install by default (`Directory.Build.props` fallback path).

## [4.4.1] - 2026/07/03

Release: [4.4.1-tag]

### Added

- New optional "Rainfall in this Area" HUD line mirroring the vanilla Environment dialog (biome baseline rainfall). Reuses the game's own `freq-*` descriptors, so it localizes automatically.
- Scrollable settings dialog — long settings lists no longer run off-screen at small resolutions or large GUI scales.

### Changed

- Main HUD lines reordered and relabeled to match the vanilla Environment panel: `Date:`, `Outside Temperature:`, `Rainfall in this Area:`, `Current Rift activity:`. The existing rainfall line is now labeled `Current Rainfall`.
- Locale note: English strings shipped; the new label keys still need a translator pass across the 12 non-English locales.

## [4.4.0] - 2026/06/08

Release: [4.4.0-tag]

### Added

- **Moon phase line.** Optional new HUD line showing the current lunar phase
  (New Moon, Waxing Crescent, First Quarter, Waxing Gibbous, Full Moon, Waning
  Gibbous, Third Quarter, Waning Crescent), read directly from VS's
  `IGameCalendar.MoonPhase`. Off by default; toggle in the Time & Date section.
  Useful for tracking events tied to specific phases — especially when overcast
  weather or dense biomes hide the sky. English shipped; other locales (es-es,
  es-419, de, ko, fr, it, ja, pl, pt-br, pt-pt, ru, uk) need translator updates
  for `moon-phase-prefix`, the eight `moon-phase-*` phase names, and
  `settings-show-moon-phase`.

## [4.3.3] - 2026/06/08

Release: [4.3.3-tag]

### Added

- **Hemisphere-aware seasons** toggle (Appearance section in settings, default on).
  Restores the vanilla-accurate position-based season for players in the southern
  hemisphere (Fall in May, Spring in November, etc.), which had regressed in 4.3.2.
  Turn it off to fall back to the calendar-month derivation (Mar–May = Spring, etc.),
  which is unaffected by world-generation mods that override VS's latitude delegates —
  recommended for players using Realistic Biome Generation if the wrong season shows
  on a fresh world.

### Fixed

- Regression from 4.3.2 where the season always followed the calendar month and ignored
  the player's hemisphere, so vanilla southern-hemisphere players saw the wrong season.
  The hemisphere-aware default is restored; the month-based behavior is now opt-out via
  the new toggle.

## [4.3.2] - 2026/06/04

Release: [4.3.2-tag]

### Fixed

- Season icon and label now derive from the calendar month rather than VS's
  position-based `IGameCalendar.GetSeason` API. The API routes through
  `OnGetHemisphere` / `OnGetLatitude` delegates that world-generation mods can
  override. Realistic Biome Generation patches `GenMaps.getLatitude` and
  overrides `polarEquatorDistance` in a way that causes `GetSeason` to return
  the wrong hemisphere for the player's position — players on a new world with
  default settings saw "Fall" in May. Deriving from the month (Mar–May = Spring,
  Jun–Aug = Summer, Sep–Nov = Fall, Dec–Feb = Winter) is authoritative, mod-safe,
  and consistent with the in-game date line in all cases. Vanilla behaviour is
  unchanged.

## [4.3.1] - 2026/05/10

Release: [4.3.1-tag]

### Fixed

- **HudShelf outline placement.** The `GetBounds` callbacks registered with
  HudShelf now return the visible panel dimensions rather than the full dialog
  footprint. Both HUDs are built with `WithFixedPadding(10)`, so the previous
  `OuterWidth/Height` values included 20 px of padding on each axis, pushing
  the edit-mode outline noticeably outside the panel edge. The callbacks now
  subtract `Padding × 2 × guiScale` from each dimension, placing the outline
  at the panel boundary.

## [4.3.0] - 2026/05/03

Release: [4.3.0-tag]

### Added

- **[HudShelf](https://github.com/Elocrypt/HudShelf) integration.** When HudShelf is installed, both the main HUD and the storm HUD become drag-to-position via HudShelf's edit mode. The user presses the HudShelf hotkey to enter edit mode, drags either HUD to any of nine snap zones (corners, edge midpoints, or center), and releases. The new position persists across game sessions via HudShelf's persistence file. When HudShelf is not installed, HUD Clock behaves exactly as before — the existing anchor setting and stacking logic are unchanged.
- Soft-depend bridge files at `Infrastructure/Integration/` implement the [HudShelf bridge pattern](https://github.com/Elocrypt/HudShelf/blob/main/docs/BRIDGE.md). No HudShelf types appear in field declarations; the JIT never compiles the bridge's inner methods when HudShelf is absent, so there is zero load-time or runtime cost without HudShelf.
- `MainHudView.RepositionFromShelf` and `StormHudView.RepositionFromShelf` — mutate composed bounds in place (same technique as VS's title-bar drag) to override the HUD position without a full recompose.
- `MainHudController.HudDialog` and `StormHudController.HudDialog` — internal properties exposing the underlying `GuiDialog` for HudShelf registration.

### Changed

- After every `Rebuild` (settings change, icon change, stacking recalc), controllers now call `ReapplyShelfPosition()` so the HudShelf-managed position is never overwritten by the native anchor logic. The intermediate native-anchor state is never rendered.

## [4.2.3] - 2026/04/30

Release: [4.2.3-tag]

### Added

- **Compatibility with [Immersive Body Temperature](https://mods.vintagestory.at/immersivebodytemperature) by Cer0** and any other temperature mod following the new vendor-neutral integration contract documented in [`docs/integration.md`](docs/integration.md). When such a mod is detected, HUD Clock extends the body-temperature line into the warm half of the range — `warm` above normal, `HOT` at or above the heatstroke-damage threshold (`normal + 4`, mirroring the existing freezing threshold). Vanilla behaviour is unchanged; the warm-half display is gated on the mod's signal so vanilla players don't suddenly see a permanent "warm (+4.0 °C)" line from the spawn-default body temperature.
- **Apparent temperature line.** New optional HUD line showing the "feels like" temperature published by an immersive-temperature mod (environment + wind + wetness + humidity + sun). Off by default; silently hidden when no compatible mod is providing the value, so turning the setting on without the mod has no effect. Format: `Apparent: 32.5 °C (Hot)` — numeric value in the player's chosen unit (Celsius or Fahrenheit) with the categorical "feels like" label in parentheses when available. Toggle in the Player stats section of the settings dialog.
- **Integration contract document** at [`docs/integration.md`](docs/integration.md). Defines the small set of well-known watched-attribute keys HUD Clock reads: `bodyTemp.bodytemp` (vanilla, extended), `bodyTemp.apparentTemp` (string enum, signals immersive system active), and `bodyTemp.apparentTempC` (float, °C, optional numeric apparent temperature). The contract is vendor-neutral — any temperature mod that writes these keys gets the integration without a code change on HUD Clock's side.
- New lang keys `body-temperature-state-warm` / `body-temperature-state-hot`, `apparent-temperature-prefix`, `apparent-temperature-prefix-uncategorized`, and `apparent-temperature-state-{comfy,cold,freezing,warm,hot}`. English shipped; other locales (es-es, es-419, de, ko, fr, it, ja, pl, pt-br, pt-pt, ru, uk) need translator updates.

### Fixed

- Body-temperature line no longer flickers as "warm (+0.2 °C)" when an immersive-temperature mod is active and the player is comfortable. The mod continuously recomputes body temp from environment and clothing inputs, producing small drift around the resting value; the HUD now applies a 0.5 °C deadband on the warm side before showing the line. Cold side is unchanged — vanilla rests body temp at `normal + 4` so any drop below 37 °C remains a meaningful warning.

## [4.2.2] - 2026/04/30

Release: [4.2.2-tag]

### Added

- **Custom icon theme.** A third option in the icon-theme dropdown alongside Modern and Classic. Selects textures from `assets/hudclock/textures/hud/custom/` (named `spring.png`, `summer.png`, `fall.png`, `winter.png`, `tempstorm.png`) and `assets/hudclock/textures/room/custom/` (named `room.png`, `cellar.png`, `greenhouse.png`). Mod ships transparent placeholders so the HUD layout still renders cleanly when no override is present; users override individual textures by either dropping PNGs directly into the mod's asset folders or — recommended — shipping a standard Vintage Story texture pack that overrides the same paths. No external dependencies, no extra libraries.
- Custom theme reserves the same 200×200 season-icon column as Modern. Texture-pack authors can ship art at any resolution and Vintage Story scales it to fit.

## [4.2.1] - 2026/04/25

Release: [4.2.1-tag]

### Fixed

- HUD layout now dynamically adjusts the season icon size based on the selected theme. The layout builder correctly allocates 200px for the Modern theme and 100px for the Classic theme, resolving an issue where the Classic theme was forced into a hardcoded 200px bound.

## [4.2.0] - 2026/04/25

Release: [4.2.0-tag]

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

Release: [4.1.0-tag]

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

[4.5.2]: https://github.com/Elocrypt/HudClock/compare/v4.5.1...v4.5.2
[4.5.1]: https://github.com/Elocrypt/HudClock/compare/v4.5.0...v4.5.1
[4.5.0]: https://github.com/Elocrypt/HudClock/compare/v4.4.1...v4.5.0
[4.4.1]: https://github.com/Elocrypt/HudClock/compare/v4.4.0...v4.4.1
[4.4.0]: https://github.com/Elocrypt/HudClock/compare/v4.3.3...v4.4.0
[4.3.3]: https://github.com/Elocrypt/HudClock/compare/v4.3.2...v4.3.3
[4.3.2]: https://github.com/Elocrypt/HudClock/compare/v4.3.1...v4.3.2
[4.3.1]: https://github.com/Elocrypt/HudClock/compare/v4.3.0...v4.3.1
[4.3.0]: https://github.com/Elocrypt/HudClock/compare/v4.2.3...v4.3.0
[4.2.3]: https://github.com/Elocrypt/HudClock/compare/v4.2.2...v4.2.3
[4.2.2]: https://github.com/Elocrypt/HudClock/compare/v4.2.1...v4.2.2
[4.2.1]: https://github.com/Elocrypt/HudClock/compare/v4.2.0...v4.2.1
[4.2.0]: https://github.com/Elocrypt/HudClock/compare/v4.1.0...v4.2.0
[4.1.0]: https://github.com/Elocrypt/HudClock/compare/v4.0.0...v4.1.0
[4.5.2-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.5.2
[4.5.1-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.5.1
[4.5.0-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.5.0
[4.4.1-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.4.1
[4.4.0-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.4.0
[4.3.3-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.3.3
[4.3.2-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.3.2
[4.3.1-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.3.1
[4.2.3-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.3
[4.2.2-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.2
[4.2.1-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.1
[4.2.0-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.2.0
[4.1.0-tag]: https://github.com/Elocrypt/HudClock/releases/tag/v4.1.0
[4.0.0]: https://github.com/Elocrypt/HudClock/releases/tag/v4.0.0
