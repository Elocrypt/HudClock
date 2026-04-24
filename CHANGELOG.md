# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [4.0.0] - 2026-04-24

Complete rewrite of the mod on a new architecture. Feature parity with 3.x is preserved; internal structure is not. Settings and keybinds do not migrate from earlier versions — reconfigure via the settings dialog (default `Shift+A`) on first launch.

### Added

- Separate HUD position anchor for the storm dialog, independent of the main HUD position. Either can be placed in any of the six screen corners (top/bottom × left/center/right).
- Per-minute countdown updates on the approaching-storm timer. The text re-renders at in-game-minute resolution (every second in real-time at default world speed) instead of the previous 5-second cadence.
- Automatic icon refresh when the season or room-status changes. Icons update within one HUD tick (≤2.5 s) without needing to reopen the settings dialog.
- New large season icons (200 × 200) with refreshed art.

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

- Settings migration from 3.x is not supported; existing settings reset on first run.
- Internal `SharedLibrary` helpers that were no longer used.
- Harmony reference (no patches required).

[Unreleased]: https://github.com/Elocrypt/HudClock/compare/v4.0.0...HEAD
[4.0.0]: https://github.com/Elocrypt/HudClock/releases/tag/v4.0.0
