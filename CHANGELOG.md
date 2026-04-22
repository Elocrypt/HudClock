# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [4.0.0] - TBD

Complete rewrite of the mod on a new architecture. Feature parity with 3.x is preserved; internal structure is not. Settings and keybinds do not migrate from earlier versions — reconfigure via the settings dialog (default `Shift+A`) on first launch.

### Changed

- Rebuilt on Vintage Story 1.22.0 and .NET 10.
- New three-layer architecture (Domain / Infrastructure / Presentation) with MVVM dialogs.
- Unified settings file — keybinds and options now live in a single config under `hudclock/`.
- HUD no longer recomposes its GUI every tick; only text contents update.
- Centralized texture/icon ownership — no more scattered `BitmapRef` disposal.
- Reflection into `SystemTemporalStability` and `ModSystemRiftWeather` now uses compiled accessors cached at startup.

### Fixed

- Storm HUD no longer auto-closes during an active storm when `ShowStormDialogState` is `TRIGGER_ONLY`.
- Season detection no longer performs a dummy zero-volume block search.
- HUD-reposition-below-other-dialogs no longer runs every 100 ms while the HUD is open.

### Removed

- Settings migration from 3.x is not supported; existing settings reset on first run.
- Internal `SharedLibrary` helpers that were no longer used.
- Harmony reference (no patches required).

[Unreleased]: https://github.com/Elocrypt/HudClock/compare/v4.0.0...HEAD
[4.0.0]: https://github.com/Elocrypt/HudClock/releases/tag/v4.0.0
