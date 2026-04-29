# HUD Clock — temperature-mod integration contract

HUD Clock 4.3+ can display extended body-temperature and apparent-temperature
information when another mod populates a small set of well-known
`WatchedAttribute` keys on the player entity. This document defines that
contract.

The contract is **vendor-neutral** — HUD Clock doesn't check for any specific
mod ID, only for the presence of these attributes. Any temperature mod that
writes them gets the integration for free.

## Where the keys live

All keys live under the existing `bodyTemp` `TreeAttribute` on the player
entity's `WatchedAttributes`. That's the same subtree vanilla uses for
`bodytemp`, so any mod patching `EntityBehaviorBodyTemperature` already has a
handle to it.

```csharp
ITreeAttribute? tempTree = entity.WatchedAttributes?.GetTreeAttribute("bodyTemp");
```

After writing, mark the path dirty so the change replicates to clients:

```csharp
entity.WatchedAttributes.MarkPathDirty("bodyTemp");
```

## Keys

| Key | Type | Unit / domain | Required? | Semantics |
|---|---|---|---|---|
| `bodytemp` | float | °C, normally `[normal − 6, normal + 6]` | inherited from vanilla | Current body temperature. Already populated by vanilla; mods that extend the temperature system into the warm half should keep using this key for the body's resulting temperature. |
| `apparentTemp` | string | enum name | Recommended | Categorical "feels like" bucket. Suggested values: `Comfy`, `Cold`, `Freezing`, `Warm`, `Hot`. **Presence of this key is HUD Clock's signal that the bidirectional/immersive temperature system is active** — when present, HUD Clock shows warm/HOT body-temperature states above normal in addition to the existing cool/FREEZING below normal. |
| `apparentTempC` | float | °C | Optional | Numeric apparent ("felt") temperature in Celsius — environment temperature plus modifiers (wind, wetness, humidity, sun, etc.). HUD Clock displays a dedicated Apparent-temperature line when this key is present. |

All three keys are read on the client side. Use synchronous tree reads with
`GetFloat` / `GetString`. Re-fetch the tree on each read, since
`WatchedAttributes` may replace the subtree reference on resync.

## Categorical labels

When `apparentTemp` is set to one of `Comfy`, `Cold`, `Freezing`, `Warm`, or
`Hot`, HUD Clock looks up a localized label using lang key
`hudclock:apparent-temperature-state-<lowercase>` (e.g.
`hudclock:apparent-temperature-state-warm`). HUD Clock ships translations for
those five values in English. Custom strings outside that set will display
verbatim — fine for development, but pick one of the five if you want
localization to work.

## Top-level effect-strength keys (informational)

HUD Clock does not currently consume these, but other HUDs / mods may. If your
mod already sets them, HUD Clock will read them in a future version:

| Key | Type | Domain | Source |
|---|---|---|---|
| `heatstrokeEffectStrength` | float (top-level) | `[0, 1]` | written by Immersive Body Temperature today |
| `freezingEffectStrength` | float (top-level) | `[0, 1]` | written by Immersive Body Temperature today |

## Minimal implementation example

The mod patching `EntityBehaviorBodyTemperature` only needs to add `apparentTempC`
on the server side wherever `apparentTemp` (string) is already being written:

```csharp
ITreeAttribute? tempTree = entity.WatchedAttributes?.GetTreeAttribute("bodyTemp");
if (tempTree is not null)
{
    tempTree.SetString("apparentTemp", apparentEnum.ToString());   // already done
    tempTree.SetFloat("apparentTempC", apparentTemperatureCelsius); // new
    entity.WatchedAttributes.MarkPathDirty("bodyTemp");             // already done
}
```

That's the entire change.

## What HUD Clock does with this data

- **Body-temperature line.** Visible whenever the player is at or below normal
  body temperature (existing behavior). When `apparentTemp` is present, the
  visibility predicate extends symmetrically above normal — the line shows
  `warm` or `HOT` with the deviation from normal in the player's chosen unit.
  Threshold for `HOT` is `normal + 4` °C (mirrors the vanilla `FREEZING`
  threshold).
- **Apparent-temperature line.** New, off by default. Visible when the user
  enables it in settings *and* `apparentTempC` is present on the entity.
  Format: `Apparent: 32.5 °C (Hot)` — numeric value with the categorical
  label in parentheses when `apparentTemp` is also set.
- **Unit handling.** Both lines respect HUD Clock's existing Fahrenheit
  toggle; mods always provide values in °C.

## Versioning

Adding new keys to this contract is non-breaking. If we ever need to change
the meaning of an existing key we'll introduce a new key alongside it rather
than reinterpret the old one. Any mod implementing the contract above will
continue to work across HUD Clock 4.x.

## Questions

Open an issue or start a thread on the HUD Clock mod page. Happy to
collaborate on edge cases or extensions.
