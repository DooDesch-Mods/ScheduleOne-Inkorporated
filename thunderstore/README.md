# Inkorporated - Custom Tattoos for Schedule I

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de/inkorporated](https://support.doodesch.de/inkorporated).

> **The tattoo framework for Schedule I.** Mods add their own PNG tattoos to the in-game tattoo shop -
> via a drop-in pack folder or an API - and the game treats them like vanilla: they render on the
> avatar, persist in saves and sync in co-op. On its own this mod adds no tattoos; it's the dependency
> tattoo packs are built on.

![Version](https://img.shields.io/badge/version-1.1.0-blue)
![Game](https://img.shields.io/badge/game-Schedule%20I-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![S1API](https://img.shields.io/badge/S1API-required-orange)

## What it does

- Adds custom tattoos to the tattoo shop under their placement category (Chest, Left/Right Arm, Face).
- Real avatar layers via the game's own pipeline - they **render, save and sync in multiplayer** like
  built-in tattoos (everyone needs the same mod + pack to see them; others just don't render them).
- Tattoo packs are plain folders of PNGs + a tiny `manifest.json` - no code required. Code mods can also
  register tattoos through the API.

## Requirements

- **Schedule I** (IL2CPP) with **MelonLoader 0.7.3+**.
- **S1API** (pulled in as a dependency).
- Optional: **Mod Manager & Phone App** for the in-game settings UI.

## Using it

Install Inkorporated plus any tattoo pack that depends on it; the new tattoos show up in the tattoo shop.
Packs live in `UserData/Inkorporated/Packs/<PackName>/`.

Want a quick look at the format (and a few free tattoos)? Set **`LoadExamplePack`** to `true` (in the Mod
Manager & Phone App UI or `UserData/MelonPreferences.cfg` under `Inkorporated_01_Main`) and restart - a
bundled example pack with a working folder/manifest template is dropped into `Packs/Examples` to copy.

## Making a pack

A `manifest.json` lists tattoos with a `placement` (`chest` | `leftarm` | `rightarm` | `face`), an optional
`price`, and a transparent PNG. The design's opaque pixels must sit at the correct UV region of that body
part (a tattoo is a full skin-texture layer, not a centered sticker - so you can do anything from a small
mark to a full sleeve). See the example pack and the full guide on
[GitHub](https://github.com/DooDesch-Mods/ScheduleOne-Inkorporated).

## Settings

`LoadExamplePack` (default `false`) - drop the bundled example pack on disk as a template. Editable in the
Mod Manager & Phone App UI or `UserData/MelonPreferences.cfg`.

## License

MIT. See the included LICENSE.md.
