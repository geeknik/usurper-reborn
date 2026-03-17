# Usurper Reborn v0.52.10 Release Notes

**Release Date:** March 16, 2026
**Version Name:** The Hook

## Combat Equipment Safety Fix

A player reported intermittent near-zero damage in online mode despite having good gear. Investigation of combat event logs revealed that in-combat stats (STR 169, ArmPow 269) were significantly lower than the player's saved stats (STR 202, ArmPow 334), indicating equipment stat bonuses were not fully applied after login.

The root cause: `RecalculateStats()` depends on all equipped items being resolvable in the `EquipmentDatabase`. In MUD mode with concurrent players, equipment ID remapping during login could leave some items temporarily unresolvable, causing their stat bonuses to be silently missing.

A safety `RecalculateStats()` call is now made at the start of every combat encounter to ensure equipment bonuses are always current, regardless of any transient issues during the login flow.

Debug logging has also been added to the damage formula — when attack power is suspiciously low (< 50), the system logs the full damage calculation breakdown (STR, WeapPow, proficiency multiplier, damage modifier, target armor) to help diagnose any future occurrences.

## Admin Dashboard Equipment Display Fix

The admin dashboard's Equipment tab showed "No equipment data" for all players. The dashboard was reading `player.equipment` (a legacy JSON path that no longer exists) but the game stores equipment as `player.equippedItems` (slot-to-ID mapping) and `player.dynamicEquipment` (full item objects with all stats).

The dashboard now reads from the correct JSON paths and displays each equipped item in a table with its slot name, item name, weapon power, armor class, and all stat bonuses (STR, DEX, AGI, CON, INT, WIS, CHA, DEF, STA).

## Grief System Localization & Dungeon Context Fix

All hardcoded English strings in the grief system have been converted to `Loc.Get()` calls with translations in all 4 languages (en/es/hu/it). 37 new localization keys added: 20 post-combat flashback messages, 12 combat start grief messages, 5 stage labels for `/health`, and 2 header keys.

Additionally, grief flashback messages have been rewritten for dungeon context — they previously referenced "crowds," "strangers walking past," and "faces in the crowd" which made no sense in a dungeon post-combat setting. All messages now use dungeon-appropriate imagery (torchlight shadows, dungeon walls, echoing corridors, dripping water).

## Companion CriticalStrike Damage Fix

Companions were taking massively disproportionate damage from monster `DamageMultiplier` abilities (CriticalStrike, CrushingBlow, LifeDrain, etc.) — for example, Aldric taking 717 damage from CriticalStrike vs 56 from a normal attack. The `DamageMultiplier` path in `MonsterAttacksCompanion()` was using the magical defense formula (`sqrt(defence) * 3`) instead of the full physical defense calculation that normal attacks use. Fixed to apply full companion defense (Defence + ArmPow + MagicACBonus + TempDefenseBonus) for melee-type DamageMultiplier abilities.

## Fame System Persistence & Expansion

Fame was only stored in memory and reset to 0 on every logout/login. Added Fame to the save/load pipeline (SaveDataStructures, SaveSystem serialization, GameEngine restore). Fame now persists across sessions.

New Fame gain and loss sources added across 6 game systems:
- **Combat**: +1 per monster kill, +3 for boss kills, +1 for unique kills
- **Old God defeats**: +50 Fame
- **World boss**: +25 for killing blow, +15 for participants
- **PvP Arena**: +10 on victory, -5 on defeat
- **Achievements**: Bronze +2, Silver +5, Gold +10, Platinum +20, Diamond +50
- **Level milestones**: +5 every 10 levels

## Knighthood System Overhaul

Knighthood now provides tangible combat benefits and visible prestige:

**Combat Bonuses** — Knights receive a permanent +5% damage and +5% defense bonus in all combat (single-monster, multi-monster, and defense paths). Displayed in `/health` Active Buffs section.

**Knight Title in All Who Lists** — "Sir" or "Dame" prefix now appears in:
- MUD `/who` command
- Main Street online player list (`[3]`)
- Main Street citizen list
- Fame board rankings
- Website online players and leaderboard

**Cinematic Knighting Ceremony** — The knighting scene has been expanded from a few lines to a multi-screen cinematic experience: the throne room falls silent, the king addresses the court, the player approaches and kneels, the ceremonial blade touches each shoulder, and the court erupts in applause. Benefits are clearly displayed afterward.

**Server-Wide Broadcast** — When a player is knighted in online mode, all online players see a golden announcement.

**Increased Requirements** — Fame requirement raised from 100 to 150, level requirement from 10 to 15, to keep knighthood challenging with the expanded Fame sources.

## Auction House Cleanup

The "My Listings" screen now hides fully-resolved listings. Previously, items marked "SOLD - COLLECTED" and "EXPIRED - COLLECTED" stayed in the list forever, cluttering it with stale entries. The query now filters out sold+collected, expired+collected, and cancelled listings, showing only active, sold (uncollected), and expired (uncollected) items.

## Guild System Overhaul — Ranks, Item Bank & Gold Withdrawal

The guild system now has a proper rank hierarchy and a full item bank.

**Rank System** — Three ranks with distinct permissions:
- **Leader** — Full control: invite, kick, promote/demote, withdraw gold/items, set ranks
- **Officer** — Can invite members, withdraw items, withdraw gold (up to 50,000 per transaction)
- **Member** — Can deposit gold and items only, no withdrawals

The guild leader can assign ranks with `/grank <player> <rank>`. Rank tags now show next to member names in `/guild` and `/ginfo`.

**Leadership Transfer** — `/gtransfer <player>` lets the guild leader hand off leadership to another member. The current leader is demoted to Officer. Both players are notified, and the full guild sees a broadcast.

**Guild Item Bank** — Members can deposit equipment from their inventory into a shared guild bank (up to 50 items). Leaders and Officers can withdraw items. New commands:
- `/gdeposit` — Shows numbered inventory list, deposit an item by number
- `/gwithdraw` — Shows guild bank items with IDs, withdraw by number
- `/gbank items` — View all items in the guild bank

**Gold Withdrawal** — The guild bank now supports withdrawals as well as deposits:
- `/gbank deposit <amount>` — Deposit gold (all members)
- `/gbank withdraw <amount>` — Withdraw gold (Leader/Officer only)
- Legacy `/gbank <amount>` still works for deposits

All bank transactions (gold and items) are broadcast to online guild members.

**Bug Fixes** — Guild disband now cleans up orphaned bank items. Member list sorts correctly by rank (Leader > Officer > Member). Leader auto-promotion on leave now prefers Officers over oldest member. Leader name shown as character display name instead of login username in `/guild` and `/ginfo`. Guild Board online member count lookup fixed. Help menus updated with all new commands.

## Guild System Audit & Localization

Comprehensive audit of the guild system found and fixed 6 additional bugs:

- **Guild Board leader display**: Showed raw login username (e.g. "player123") instead of display name. Guild Board now joins against the players table to resolve display names.
- **Item bank withdraw race condition**: Two players could withdraw the same item simultaneously — the SELECT and DELETE were separate queries. Now wrapped in a SQLite transaction for atomicity.
- **Kick not broadcasting**: Kicking a member only notified the kicked player, not remaining guild members. Now broadcasts to all members.
- **Invite showing DB key**: Guild invites showed the lowercased database key (e.g. "my guild") instead of the display name ("My Guild"). Invite flow now passes the display name.
- **Transfer double-notifying**: `/gtransfer` sent both a generic "leadership transferred" broadcast AND a personalized message to the new leader, so the new leader saw two messages. Generic broadcast now excludes the target.
- **`/gtransfer` missing from help**: The command was functional but not listed in the `/` quick commands help menu.

All ~90 hardcoded English strings across guild chat commands and the Guild Board display are now localized via `Loc.Get()`. Translations added to Spanish, Hungarian, and Italian.

## Mana Potion Purchase Display Fix

Buying mana potions from the Magic Shop showed raw format placeholders (`{0} hands you {1} mana potion(s)`) instead of the merchant's name. The `magic_shop.mana_deal` localization key expected two arguments (merchant name, quantity) but only the quantity was being passed.

## Wavecaller Siren's Lament & Weaken Debuff Fix

Siren's Lament (Wavecaller spell 3) did nothing in single-monster combat — the `HandleSpecialSpellEffect` method (used by the single-monster spell path) was missing a `case "weaken"`, so the debuff silently fell through with no effect. This meant Wave's Echo's "damage doubles if target is debuffed" never triggered, since the target was never actually debuffed. Fixed by adding the missing weaken handler.

Additionally, the ability-path weaken effect (used by Cutting Words, etc.) only reduced DEF by 25% — it was missing the ATK reduction entirely. All weaken handlers now consistently apply -30% ATK and -20% DEF, matching the spell description.

## Shield Loot Generation (by maxsond)

Shields dropped as dungeon loot previously had no `ShieldBonus` or `BlockChance`, making them strictly worse than authored shield templates. Shields now generate with proper defensive stats based on shield type (Buckler, Shield, Tower Shield) and rarity, modeled after the existing authored shield values.

- `BlockChance` and `ShieldBonus` added to the `Item` class
- Dedicated `GenerateShield()` and `RollShieldStats()` methods in `LootGenerator`
- Shield stats displayed during loot encounters with comparison against currently equipped shield
- Full save/load serialization for shield properties across inventory, equipped items, and NPC equipment
- Weapon and armor generation code consolidated to reduce duplication
- Dead `Item.cs` file removed (unused duplicate of `Items.cs`)

## NPC Teammate Catch-Up XP

NPC teammates and companions were falling hopelessly behind the player's level, forcing players to spend hours grinding just to keep their team on pace. Two changes address this:

- **Catch-up XP bonus**: Underleveled teammates now receive +10% bonus XP per level they're behind the player, up to a 4x cap. A teammate 10 levels behind gets 2x XP; 20 levels behind gets 3x; 30+ levels behind hits the 4x cap. The bonus is shown in combat output (e.g. "catch-up 2.0x"). Applied to both NPC teammates and companions through the per-slot XP distribution system.
- **Auto XP distribution**: New players were unknowingly starving their teammates of XP because the default per-slot allocation is 100% to the player and 0% to all teammate slots. The system now auto-sets an even split on first combat when teammates are present but all teammate slots are at 0% (e.g. 2 teammates → 34%/33%/33%). Players can still manually adjust the split in the dungeon XP Distribution menu.

---

## Files Changed

- `Scripts/Core/GameConfig.cs` -- Version 0.52.10; `KnightDamageBonus`, `KnightDefenseBonus`, `KnightFameDecayResistance` constants; `TeamXPConfig.CatchUpBonusPerLevel` (+10%/level) and `CatchUpMaxMultiplier` (4x cap)
- `Scripts/Core/Character.cs` -- `IsKnighted` computed property
- `Scripts/Systems/GriefSystem.cs` -- `GetPostCombatFlashback()` and `GetCombatStartGriefMessage()` converted to `Loc.Get()` with dynamic keys; dungeon-appropriate flashback text
- `Scripts/Systems/CombatEngine.cs` -- Safety `RecalculateStats()` at combat start; debug logging on low attack power; companion `DamageMultiplier` defense fix (full physical defense instead of `sqrt*3`); knight +5% damage in single and multi-monster paths; knight +5% defense; Fame +1/+3/+1 on monster kills; `GetCatchUpMultiplier()` gives underleveled teammates +10%/level bonus XP (4x cap); `AutoDistributeTeamXP()` auto-sets even XP split when teammates exist but all slots are 0%; both applied in all 3 combat victory paths; missing `case "weaken"` added to `HandleSpecialSpellEffect` (single-monster spell path) so Siren's Lament actually debuffs the target; ability-path weaken handlers now reduce both ATK (-30%) and DEF (-20%) instead of only DEF (-25%)
- `Scripts/Systems/SaveDataStructures.cs` -- `Fame` property added to player save data
- `Scripts/Systems/SaveSystem.cs` -- Fame serialization
- `Scripts/Core/GameEngine.cs` -- Fame restore on load
- `Scripts/Systems/AchievementSystem.cs` -- Tier-scaled Fame rewards on achievement unlock
- `Scripts/Systems/OldGodBossSystem.cs` -- +50 Fame on Old God defeat
- `Scripts/Systems/WorldBossSystem.cs` -- +25 Fame killing blow, +15 Fame participation
- `Scripts/Locations/ArenaLocation.cs` -- +10 Fame PvP victory, -5 Fame PvP defeat
- `Scripts/Locations/LevelMasterLocation.cs` -- +5 Fame every 10 levels
- `Scripts/Locations/CastleLocation.cs` -- Cinematic knighting ceremony; increased requirements (Fame 150, Level 15); server-wide broadcast; `IsKnighted` replaces string contains checks
- `Scripts/Locations/BaseLocation.cs` -- Knight buff in `/health` Active Buffs; `IsKnighted` in buff condition
- `Scripts/Locations/MainStreetLocation.cs` -- Knight title prefix in citizen list and Fame board; grief `/health` labels converted to `Loc.Get()`
- `Scripts/Systems/GuildSystem.cs` -- Rank system (Leader/Officer/Member) with permission helpers; `guild_bank_items` SQL table; `GetMemberRank()`, `SetMemberRank()`, `WithdrawGold()`, `DepositItem()`, `WithdrawItem()`, `GetBankItems()`; `OfficerGoldWithdrawLimit`, `MaxBankItems` constants
- `Scripts/Server/MudChatSystem.cs` -- Knight title prefix ("Sir"/"Dame") prepended to name in `/who` list; guild rank commands (`/grank`, `/gdeposit`, `/gwithdraw`); updated `/gbank` with deposit/withdraw/items subcommands; updated `/ginvite` to allow Officers; `NotifyGuildMembers()` helper; rank tags in `/guild` and `/ginfo`
- `Scripts/Systems/OnlineChatSystem.cs` -- Knight title prefix in online who list via live session lookup
- `Scripts/Systems/IOnlineSaveBackend.cs` -- `NobleTitle` added to `PlayerSummary` class
- `Scripts/Systems/SqlSaveBackend.cs` -- `nobleTitle` extracted in `GetAllPlayerSummaries()` SQL; auction `GetMyAuctionListings()` filters out collected/cancelled listings
- `web/ssh-proxy.js` -- Knight title prefix in online players and leaderboard API; admin dashboard equipment fix
- `web/admin.html` -- Equipment tab table display
- `Scripts/Systems/GuildSystem.cs` -- `WithdrawItem()` SQLite transaction; `GuildInfo.LeaderDisplayName`; `GuildInvite.GuildDisplayName`; `GetAllGuilds()` joins players table; `SendInvite()` display name parameter
- `Scripts/Server/MudChatSystem.cs` -- All guild command handlers localized; kick broadcasts to remaining members; invite display name fix; transfer excludes target from broadcast
- `Scripts/Locations/MagicShopLocation.cs` -- Mana potion `magic_shop.mana_deal` missing merchant name arg
- `Scripts/Locations/MainStreetLocation.cs` -- Guild Board localized; uses `LeaderDisplayName`
- `Scripts/Locations/BaseLocation.cs` -- `/gtransfer` added to help menus
- `Scripts/Core/Items.cs` -- `BlockChance` and `ShieldBonus` properties added to Item class
- `Scripts/Core/Item.cs` -- **DELETED** — unused duplicate Item class (dead code)
- `Scripts/Systems/LootGenerator.cs` -- `GenerateShield()`, `RollShieldStats()`, `CreateShieldFromTemplate()` methods; weapon/armor generation consolidation
- `Scripts/Systems/SaveDataStructures.cs` -- `BlockChance` and `ShieldBonus` in `InventoryItemData`
- `Scripts/Systems/SaveSystem.cs` -- Shield property serialization/deserialization
- `Tests/LootGeneratorTests.cs` -- Shield generation tests
- `Localization/en.json` -- 37 grief keys; knight requirement text updated (Fame 150, Level 15); ~90 `guild.*` localization keys
- `Localization/es.json` -- 37 grief keys (Spanish); knight requirement text updated; ~90 `guild.*` keys
- `Localization/hu.json` -- 37 grief keys (Hungarian); knight requirement text updated; ~90 `guild.*` keys
- `Localization/it.json` -- 37 grief keys (Italian); knight requirement text updated; ~90 `guild.*` keys
