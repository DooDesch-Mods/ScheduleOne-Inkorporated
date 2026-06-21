# Changelog

All notable changes to Inkorporated are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-06-21

Initial release.

### Added
- Custom tattoo framework for the in-game tattoo shop, built on S1API. Tattoos register through the game's
  own `Resources.Load` pipeline, so they render, persist in saves and sync in multiplayer like vanilla.
- Drop-in **pack format**: `UserData/Inkorporated/Packs/<Pack>/manifest.json` + PNGs. Placements `chest`,
  `leftarm`, `rightarm`, `face`; optional per-tattoo price; transparent PNGs at any size.
- Public **API** (`Inkorporated.API.RegisterTattoo` / `RegisterTattooFromFile`) for code mods, with a
  `TattooPlacement` enum. Lazy registration so PNG/texture work happens when the shop first opens.
- **Shop integration**: custom tattoos are injected into the matching tattoo-shop category by cloning an
  existing option, with the game's own selection/price/persistence handling reused.
- Bundled **example pack** (8 UV-aligned designs across all placements) gated behind the `LoadExamplePack`
  preference (default off); extracted to disk as a copyable template, never overwriting an existing folder.
- Correct face-vs-body routing (face layers register under a `/Face/` path so the game applies them to the
  face mesh).
- Debug build only: a built-in-tattoo **UV template exporter** (`UserData/Inkorporated/Templates/`) and a
  shop layout diagnostic; neither ships in the release.
