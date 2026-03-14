# Usurper Reborn v0.52.4 - Localization: Main Street & Dungeon

---

## Localization

### Main Street Menu Localized

The classic visual Main Street menu now uses the localization system for all menu labels. Previously, the visual menu (`ShowClassicMenu()`) had hardcoded English labels like `[D]ungeons`, `[W]eapon Shop` that displayed in English regardless of the player's language setting. The BBS compact and screen reader menus were already localized.

Menu labels now use `Loc.Get("menu.action.*")` calls, matching the pattern established in BBS and SR menus. The format was changed from split-word `[D]ungeons` to full-label `[D] Dungeons` to enable proper localization where the hotkey letter is independent of the translated word.

6 new `menu.action.*` localization keys added across all 4 languages (English, Spanish, Hungarian, Italian).

### Dungeon Content Localized (427 Keys)

All dungeon room flavor text has been localized. Previously, room names, descriptions, atmosphere text, feature names, exit descriptions, boss rooms, and theme short names in `DungeonGenerator.cs` were all hardcoded English strings -- approximately 427 strings total.

Changes:
- **Room flavor text**: All 8 dungeon themes (Catacombs, Sewers, Caverns, Ancient Ruins, Demon Lair, Frozen Depths, Volcanic Pit, Abyssal Void) across 6 room types (corridor, chamber, hall, shrine, crypt, alcove) with name/description/atmosphere for each
- **Special room types**: Merchant rooms, mystery rooms (5 themes), guardian rooms (4 themes), danger rooms (3 themes), vault rooms (9 themes), lore rooms (9 themes), meditation rooms (9 themes), boss antechambers (9 themes), arena rooms (3 themes), memory rooms (9 themes)
- **Room features**: 33 interactable objects across 6 theme sets (bone piles, crystal clusters, demonic altars, etc.)
- **Exit descriptions**: Per-theme exit templates with directional placeholders, plus 4 cardinal direction names
- **Hidden exits**: 7 theme-specific hidden passage discovery texts
- **Boss rooms**: 9 theme-specific boss chamber names and descriptions
- **Treasure room**: Guarded treasury name and description
- **Theme short names**: 8 display names used in dungeon headers (previously raw enum `.ToString()` like "Sewers")

`DungeonGenerator.cs` was refactored with a `GetThemeKey()` helper that maps `DungeonTheme` enum values to short key abbreviations (cat, sew, cav, ruin, demon, ice, fire, void) for constructing localization keys dynamically.

`DungeonLocation.cs` received a new `GetThemeShortName()` method that wraps theme enum display with `Loc.Get()` calls, fixing 5 locations where `currentFloor.Theme.ToString()` was used directly in player-visible text.

All 427 keys translated into Spanish, Hungarian, and Italian.

### Italian Dungeon Support

Italian (`it.json`) received the 8 `dungeon.theme_short.*` keys and 6 `menu.action.*` keys that were previously missing, in addition to the 427 dungeon content keys.

---

## Files Changed

- `Scripts/Core/GameConfig.cs` -- Version 0.52.4
- `Scripts/Systems/DungeonGenerator.cs` -- `GetThemeKey()` helper; `GenerateRoomFlavor()` rewritten to construct localization keys dynamically; all 10 special room flavor methods replaced with `Loc.Get()` lookups; `GetThemeFeatures()` replaced with localized feature names/descriptions; `GetExitDescription()` replaced with localized direction names and per-theme templates; `GetHiddenExitDescription()`, `GetBossRoomName()`, `GetBossRoomDescription()`, treasure room strings all localized; 427 total `Loc.Get()` replacements
- `Scripts/Locations/DungeonLocation.cs` -- New `GetThemeShortName()` static method mapping `DungeonTheme` to localized short names; 5 display locations updated from `currentFloor.Theme.ToString()` to `GetThemeShortName()`
- `Scripts/Locations/MainStreetLocation.cs` -- `ShowClassicMenu()` replaced all ~25 hardcoded menu labels with `Loc.Get("menu.action.*")` calls; column width adjusted from 15 to 16; BBS and SR menu "Guilds" hardcoded strings replaced with `Loc.Get()`
- `Localization/en.json` -- 6 new `menu.action.*` keys; 427 `dg.*` dungeon localization keys; 8 `dungeon.theme_short.*` keys
- `Localization/es.json` -- 6 new `menu.action.*` keys; 427 `dg.*` dungeon translations (Spanish); 8 `dungeon.theme_short.*` keys
- `Localization/hu.json` -- 6 new `menu.action.*` keys; 427 `dg.*` dungeon translations (Hungarian); 8 `dungeon.theme_short.*` keys
- `Localization/it.json` -- 6 new `menu.action.*` keys; 427 `dg.*` dungeon translations (Italian); 8 `dungeon.theme_short.*` keys
