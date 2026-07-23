# Inkorporated - Custom Tattoos for Schedule I

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de](https://support.doodesch.de).

> The tattoo framework for Schedule I. Inkorporated lets mods add their own PNG tattoos to the
> in-game tattoo shop - drop a pack folder, or register via the API - and the game treats them
> exactly like vanilla tattoos: they render on the avatar, persist in saves and sync in co-op.
> Built on [S1API](https://github.com/ifBars/S1API).

![Version](https://img.shields.io/badge/version-1.1.1-blue)
![Game](https://img.shields.io/badge/game-Schedule%20I-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![S1API](https://img.shields.io/badge/S1API-required-orange)
![Type](https://img.shields.io/badge/type-library%2Fdependency-lightgrey)

Inkorporated is primarily a **library / dependency**. On its own it adds no tattoos - it's the thing
tattoo packs depend on. Install it alongside a tattoo pack (or flip on the bundled example pack) and
the new tattoos show up in the tattoo shop under their placement category (Chest, Left/Right Arm, Face).

## Documentation & example

- 📖 **[Wiki](https://github.com/DooDesch-Mods/ScheduleOne-Inkorporated/wiki)** - the full guide for making
  tattoos: pack format, the API, authoring + UV alignment, multiplayer/NPCs, troubleshooting.
- 🧩 **[Example repo](https://github.com/DooDesch-Mods/ScheduleOne-InkorporatedExample)** - a working,
  copy-me template showing **both** routes side by side: a no-code content pack and a code mod using the API.

## Features

- **Real tattoos, not decals.** Each custom tattoo is registered as a proper avatar layer through the
  game's own `Resources.Load` pipeline, so it renders, **saves**, and **syncs in multiplayer** just like
  a built-in tattoo (any client with the same mod + pack sees it; others simply don't render it - no crash).
- **Two ways to ship tattoos** - a drop-in **pack folder** (PNGs + a tiny `manifest.json`, no code), or a
  C# **API** for code mods that build textures at runtime.
- **All placements:** chest, left arm, right arm and face.
- **No asset bundles, no engine patching.** Custom textures are loaded from PNG at runtime and wrapped into
  cloned avatar layers - nothing to cook in Unity.
- **Bundled example pack** (off by default) gives players a few ready-made tattoos and authors a working
  template to copy.

## Requirements

| Component | Version / Source |
|-----------|------------------|
| Schedule I | IL2CPP (current Steam public build) |
| MelonLoader | `0.7.3+` |
| S1API | [ifBars/S1API_Forked](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/) (Resources registry, avatar layer factory) |
| Mod Manager & Phone App | [Nexus mods/397](https://www.nexusmods.com/schedule1/mods/397) - optional, for the in-game settings UI |

## Installation

### Recommended: a Thunderstore mod manager
Install with r2modman / Gale from the Schedule I community; the dependencies (MelonLoader, S1API) are
pulled in automatically. Then install any tattoo pack that lists Inkorporated as a dependency.

### Manual
1. Install **MelonLoader 0.7.3** for Schedule I.
2. Install **S1API** (its DLLs go in `Mods/` and `Plugins/` per its own instructions).
3. Drop **`Inkorporated.dll`** into your Schedule I `Mods/` folder.
4. Add tattoo packs (see below), or enable the bundled example pack (see Configuration).

## For players: using tattoo packs

A tattoo pack is just a folder under:

```
<Schedule I>/UserData/Inkorporated/Packs/<PackName>/
```

containing a `manifest.json` and the PNGs it lists. Mod managers that target this path install packs for
you. To see the format, turn on the bundled example pack (Configuration below) and look in
`UserData/Inkorporated/Packs/Examples`. Custom tattoos then appear in the tattoo shop, free by default.

## For pack authors: the pack format

Create `UserData/Inkorporated/Packs/<YourPack>/manifest.json`:

```json
{
  "name": "My Pack",
  "author": "you",
  "tattoos": [
    { "id": "skull", "name": "Skull", "placement": "chest", "file": "skull.png" },
    { "id": "tear",  "name": "Teardrop", "placement": "face", "file": "tear.png", "price": 250 }
  ]
}
```

- **`placement`**: one of `chest` | `leftarm` | `rightarm` | `face`.
- **`price`**: optional; omit or `0` for "Free".
- **`file`**: PNG filename relative to the pack folder (defaults to `<id>.png` if omitted).
- **`id`**: unique within your pack.

### Authoring the PNGs (important)

A tattoo is a **full UV-space skin texture**, not a sticker placed in a box. The body shares one UV unwrap
(torso + both arms + legs), the face has its own. Your design's **opaque pixels must sit at the UV
coordinates of the body part you want** - everything else transparent. Get this wrong and the ink lands on
the wrong limb.

- Recommended canvas: **2048x2048** for body/arms, **512x512** for face (matches the built-ins). Size only
  affects sharpness, not placement; there is **no size cap** - you can paint a whole arm sleeve or a full
  back piece by covering that region's UV.
- Use the built-in tattoos as alignment references. A DEBUG build of Inkorporated exports every built-in
  tattoo texture to `UserData/Inkorporated/Templates/` - paint over those to line your art up.

## For mod developers: the API

Reference `Inkorporated.dll` and register early (e.g. in your `OnInitializeMelon`), **before** the tattoo
shop UI is built. Tattoos registered later appear the next time that UI is rebuilt.

```csharp
using Inkorporated;                 // API
using Inkorporated.Model;           // TattooPlacement

// From an already-loaded texture:
API.RegisterTattoo("skull", "Skull", TattooPlacement.Chest, myTexture2D, price: 0f, source: "MyMod");

// From a PNG on disk (loaded lazily when first needed):
API.RegisterTattooFromFile("tear", "Teardrop", TattooPlacement.Face, pngPath, source: "MyMod");

// From a PNG embedded in your own DLL (no loading boilerplate - caller assembly is auto-detected):
API.RegisterTattooFromResource("rose", "Rose", TattooPlacement.LeftArm, "MyMod.Assets.rose.png", source: "MyMod");
```

`TattooPlacement` = `Chest | LeftArm | RightArm | Face`. `source` namespaces your ids (used for
de-duplication and the registered resource path). Because tattoos register through `Resources.Load`, the
same custom path also works for S1API NPC appearances (`WithBodyLayer`/`WithFaceLayer`) - just register
before the NPC's appearance is applied. On Thunderstore, list `DooDesch-Inkorporated` as a dependency.

## Configuration

Settings live in the **Mod Manager & Phone App** UI in-game, or in `UserData/MelonPreferences.cfg` under
`Inkorporated_01_Main`.

| Setting | Default | What it does |
|---|---|---|
| `LoadExamplePack` | `false` | When on, drops the bundled example pack into `UserData/Inkorporated/Packs/Examples` on startup (if not already there) - a few ready-made tattoos plus a folder/manifest template to copy. Requires a game restart. Never overwrites an existing Examples folder. |

## How it works

For each custom tattoo, Inkorporated loads the PNG into a `Texture2D`, clones a built-in `AvatarLayer` of
the matching placement (via S1API's `AvatarLayerFactory`) with the texture swapped in, and registers it at a
custom Resources path (S1API's `RuntimeResourceRegistry` patches `Resources.Load`). The shop buttons are
added by cloning an existing option in the tattoo-shop UI. From there the game's own avatar pipeline loads,
renders, saves and networks the tattoo like any vanilla one - which is why no asset bundles or render hooks
are needed.

## Compatibility

- IL2CPP build only (current Steam public branch).
- **Multiplayer:** custom tattoos are transmitted as their resource-path string. Every player needs
  Inkorporated **and the same pack** for them to render on each other; players without it simply don't show
  that tattoo (no desync, no crash).

## Credits

- **DooDesch** - mod author.
- **[ifBars/S1API](https://github.com/ifBars/S1API)** - the modding API this is built on (texture/avatar-layer
  helpers + the Resources registry that make custom tattoos possible).
- **Prowiler** - Mod Manager & Phone App (in-game settings UI).

## License

Provided as-is under the [MIT License](LICENSE.md).
