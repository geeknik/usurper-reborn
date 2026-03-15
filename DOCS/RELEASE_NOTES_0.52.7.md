# Usurper Reborn v0.52.7 Release Notes

**Release Date:** March 15, 2026
**Version Name:** The Hook

## Windows ANSI Rendering Fix

Running the game exe directly in Windows Command Prompt or PowerShell showed garbled escape codes (`[1;31m`, `[0;30m`) instead of colors — the splash screen, ANSI art portraits, and all colored text were unreadable. The game now enables Virtual Terminal Processing via the Win32 API at startup, so ANSI escape codes render correctly in any Windows terminal. WezTerm users were unaffected (it handles ANSI natively).

## Companion/NPC Equipment Stat Loss Fix

Unequipping items from companions (Inn), NPC teammates (Team Corner), or spouses (Home) was silently stripping Intelligence, Constitution, all enchantments (Fire, Frost, Lightning, Poison, Holy, Shadow), and all proc effects (Lifesteal, Mana Steal, Critical Strike, Critical Damage, Armor Piercing, Thorns, Regeneration, Mana Regen, Magic Resist) from items. Only basic stats (STR, DEX, HP, Mana, Defense) survived the transfer.

Each of the three location files had its own copy of the equipment-to-item conversion that was missing the LootEffects preservation logic. All three now delegate to `Character.ConvertEquipmentToLegacyItem()` which is the canonical implementation that preserves everything.

**Example:** A "Grounded Ring of the Mage" with +41 INT and +6 DEF would lose the INT entirely when unequipped from a companion, showing only DEF:+6 afterward.

## Inventory Backpack Pagination

The inventory backpack no longer truncates at 20 items with "... and X more". Items are now paginated at 15 per page with `[<]` / `[>]` navigation. Page indicator shows in the header (e.g., "BACKPACK (2/3)"). Item numbers (`B1`, `B2`, etc.) remain absolute across pages so they always reference the correct item.

## Inventory Menu Text Fix

The inventory menu showed `[E]quip item` and `[M]anage slot` but neither key actually did anything in the input handler. The menu now accurately shows the real controls: `[1-9/F/C/N/L/R] Manage slot  [B#] Equip backpack item  [D]rop  [U]nequip all`.

## Website Paladin Stat Bar Fix

The Paladin class card on the website showed STR/DEF stat bars, but the Paladin's DEF growth (+1/level) is actually lower than the Warrior's (+2/level). The Paladin's tankiness comes from holy magic and high constitution, not raw defense. Changed the stat bars to STR/WIS and updated the description to accurately explain the class fantasy.

## Accessible Launcher Terminal Priority Fix

The Linux accessible launcher (`play-accessible.sh`) had xterm first in its terminal search order, so it would always pick xterm even when better screen-reader-compatible terminals (gnome-terminal, konsole) were available. Reordered the list so xterm is last resort. gnome-terminal is now preferred first since it has the best Orca screen reader support.

## Tank Companion AI: Immediate Taunt

Tank companions (Aldric, and any Warrior/Paladin/Barbarian teammate) now open combat with Thundering Roar (or single-target taunt) immediately on round 1 instead of basic attacking for several turns first. Previously, ability usage was gated behind a 50% random roll each round, then picked randomly from all available abilities — so a tank might spend 3-4 rounds basic attacking before finally taunting. Tanks now bypass the random gate entirely when no monsters are taunted, guaranteeing they establish aggro before the party takes unnecessary damage. Once monsters are taunted, the tank returns to normal AI for ability selection.

## Party Ambush Detection

Ambush detection now uses the best Agility, Dexterity, and Level from anyone in the party (leader, companions, NPC teammates) instead of only the leader's stats. A high-DEX Ranger or Assassin companion helps the whole party spot ambushes.

## Group Combat: Full Menu for Followers

Players in a dungeon group as followers now see the full visual combat menu with the boxed quickbar showing numbered skill names, instead of the compact BBS-style one-liner. BBS/compact mode and screen reader users still get their appropriate menu formats. AUTO and SPD options are hidden for followers across all three menu styles (visual, BBS, screen reader).

## Credential Save Prompt: Don't Ask Again

The "Save credentials for next time?" prompt after online login now shows three options: `[Y]es / [N]o / [D]on't ask again`. Choosing D saves the preference and the prompt never appears again. Previously saved credentials are preserved when setting this preference. Delete `online_credentials.json` from the game folder to reset.

## Group Commands in Help

The `/` help menu now includes a "Group Dungeon Commands" section showing `/group`, `/leave`, `/disband`, `/party`, `/accept`, and `/deny`.

## Screen Reader Help: /gear Command

The `/gear` command was missing from the screen reader version of the `/` help menu (present in the visual menu since v0.50.6). Now listed in both.

## Missing Person Quests Removed

The NPC petition system's "missing person" quests have been removed. These quests had multiple issues: quest objectives would auto-complete without player action, and rescued NPCs would not reappear at their town locations after quest completion due to `CurrentLocation` never being restored. Rather than patch the broken quest flow, the feature has been removed entirely. The `MissingPerson` enum value is preserved for save compatibility.

## Credits: maxsond

Added maxsond as a code contributor in all credits: in-game credits screen, ending credits scroll, and website credits section (all 4 game languages + 3 website languages).

---

## Files Changed

- `Console/Bootstrap/Program.cs` -- `EnableWindowsAnsiSupport()` calls Win32 `SetConsoleMode` with `ENABLE_VIRTUAL_TERMINAL_PROCESSING` at startup; P/Invoke declarations for `GetStdHandle`, `GetConsoleMode`, `SetConsoleMode`
- `Scripts/Core/GameConfig.cs` -- Version 0.52.7
- `Scripts/Locations/InnLocation.cs` -- `CompanionConvertEquipmentToItem()` delegates to `ConvertEquipmentToLegacyItem()` preserving all LootEffects
- `Scripts/Locations/TeamCornerLocation.cs` -- `ConvertEquipmentToItem()` delegates to `ConvertEquipmentToLegacyItem()` preserving all LootEffects
- `Scripts/Locations/HomeLocation.cs` -- `ConvertEquipmentToItem()` delegates to `ConvertEquipmentToLegacyItem()` preserving all LootEffects
- `Scripts/Systems/InventorySystem.cs` -- Backpack pagination (15 items/page); `[<]`/`[>]` page navigation; page indicator in header; corrected menu text
- `Scripts/Core/GameEngine.cs` -- maxsond added to in-game credits
- `Scripts/Systems/EndingsSystem.cs` -- maxsond added to ending credits scroll
- `launchers/play-accessible.sh` -- xterm moved to last in terminal search order; gnome-terminal first
- `web/index.html` -- Paladin stat bars STR/DEF changed to STR/WIS; description updated; maxsond credit card added
- `Scripts/Systems/CombatEngine.cs` -- Tank AI priority taunt (bypasses 50% gate when no monsters taunted); party-wide ambush detection (best AGI/DEX/Level from any party member); group follower full visual combat menu with `isFollower` parameter on `ShowDungeonCombatMenuStandard`, `ShowDungeonCombatMenuBBS`, and `ShowDungeonCombatMenuScreenReader`
- `Scripts/Systems/OnlinePlaySystem.cs` -- Credential save prompt `[D]on't ask again` option; `NeverAskToSave` flag on `OnlineCredentials`; `ShouldNeverAskToSave()` and `SaveNeverAskPreference()` methods; preference preserves existing saved credentials
- `Scripts/Locations/BaseLocation.cs` -- Group dungeon commands added to `/` help menu (visual and screen reader); `/gear` added to screen reader help menu
- `Localization/en.json` -- `inventory.prev_page`, `inventory.next_page` keys; updated `options_line1` and `options_line2`; updated `online.save_credentials_prompt`; added `online.credentials_never_ask`; maxsond credit keys
- `Localization/es.json` -- Same localization keys as en.json (Spanish translations)
- `Localization/hu.json` -- Same localization keys as en.json (Hungarian translations)
- `Localization/it.json` -- Same localization keys as en.json (Italian translations)
- `Scripts/Systems/NPCPetitionSystem.cs` -- Removed `TryMissingPerson()` and `ExecuteMissingPerson()` methods; removed MissingPerson from petition checks and ExecutePetition switch; enum value preserved for serialization
- `web/lang/en.json` -- maxsond credit keys
- `web/lang/es.json` -- maxsond credit keys
- `web/lang/hu.json` -- maxsond credit keys
