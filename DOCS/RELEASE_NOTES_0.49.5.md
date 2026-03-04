# Usurper Reborn v0.49.5 — The Outskirts

*The NPCs are building something of their own.*

---

## NPC Autonomous Settlement — The Outskirts

A new emergent gameplay system where NPCs autonomously migrate, pool resources, and construct buildings at a settlement beyond the city gates. The settlement grows whether you visit or not, driven by the world simulation tick cycle. The building order emerges from the personality composition of the settler population — making each playthrough's settlement different.

### How It Works

Community-minded NPCs (high Sociability or Loyalty, low Aggression) have a 2% chance per world sim tick to migrate to the settlement. Once settled, they contribute gold toward construction based on their generosity — greedy NPCs contribute less, generous ones contribute more. The formula: `(1.0 - Greed) * gold * 1%` per tick.

When no building is under construction, settlers collectively "vote" on the next project through their personality traits. A settlement full of brave fighters will prioritize the Palisade. A settlement of social merchants will build the Tavern first. Each game's settlement evolves differently based on which NPCs happen to settle there.

### Accessing the Settlement

Press `[>]` from Main Street once 5+ NPCs have settled. The settlement gate appears automatically when the population threshold is reached.

### 7 Building Types (3 Tiers Each)

| Building | Settler Trait | Foundation (5k) | Built (25k) | Upgraded (100k) |
|---|---|---|---|---|
| Palisade | Courage | Foundations laid | Defense structure | +5% defense buff (5 combats) |
| Tavern | Sociability | Foundations laid | Attracts settlers | +10% XP buff (5 combats) |
| Market Stall | Greed | Foundations laid | Browse settler goods | Trade with settlers |
| Shrine | Patience | Foundations laid | Healing aura | Free 30% HP heal (1/day) |
| Workshop | Ambition | Foundations laid | Repairs gear | Identify 1 item/day |
| Watchtower | Courage+Intel | Foundations laid | Scouts report | Scout any dungeon floor |
| Council Hall | Intelligence | Foundations laid | Governance | Claim gold share |

### Player Services

Visit completed buildings for unique benefits:
- **Tavern** (Built): +10% XP bonus for 5 combats
- **Shrine** (Upgraded): Free 30% HP heal once per day
- **Palisade** (Upgraded): +5% defense bonus for 5 combats
- **Workshop** (Upgraded): Identify one unidentified item
- **Watchtower** (Upgraded): Scout monsters and hazards on any dungeon floor
- **Council Hall** (Built): Claim your share of the communal treasury
- **Market Stall** (Built): Browse settler inventories

### Settlement Buffs in Combat

Tavern XP and Palisade defense buffs last for 5 combats and are displayed in the `/health` active buffs section. They stack with existing buffs (God Slayer, Song buffs, herbs, etc.).

### Online Multiplayer

Settlement state is shared across all players in online mode, persisted via the `world_state` SQLite table. All players see the same settlement progress, buildings, and settlers.

### News Events

The settlement generates news events as it grows:
- "A settlement has been founded beyond the city gates!"
- "{NPC} has joined the Outskirts settlement."
- "The settlement's {Building} has been completed!"
- "The settlement's {Building} has been upgraded!"

### NPC Building Proposals

Once the settlement matures (3+ original buildings at Foundation or higher), settlers begin proposing their own building ideas. Proposals are driven by personality traits not used by the original 7 buildings — Mysticism, Aggression, Impulsiveness, Vengefulness, Caution, and Trustworthiness. Different settler populations will propose different buildings, creating unique settlements each playthrough.

**12 proposable buildings**: Arena (+10% damage), Thieves' Den (+15% gold find), Mystic Circle (mana restore), Prison (trap resistance), Scouts' Lodge (scout 3 floors), Library (+5% XP, stacks with Tavern), Barracks (NPC ally), Herbalist Hut (free herb), Smugglers' Cache (shop discount), Memorial (contribution boost), Gambling Hall (wager gold), Oracle's Sanctum (lore hints).

**How proposals work**:
1. NPC with strongest matching traits proposes a building from the catalog
2. Settlers vote Support or Oppose over 5 world sim ticks based on personality alignment
3. Players can Endorse (1,000 gold, +2 support) or Oppose (+2 against) via `[P] Proposals`
4. Simple majority passes — building enters construction using same tier/cost system
5. Failed proposals go on cooldown before re-appearing

New services unlock at Built or Upgraded tier, following the same pattern as the original 7 buildings.

---

## Mana/Stamina Class Split

Non-caster classes now use **Stamina** instead of Mana as their resource identity. Classes that rely on combat abilities (Warrior, Barbarian, Paladin, Ranger, Assassin, Bard, Jester, Alchemist) no longer accumulate mana they can never use, and their status bars reflect their actual combat resource.

### What Changed

- **Status bar**: Non-caster classes show `Stamina: X/Y` (yellow) instead of `Mana: X/Y` (blue) in all display modes (full-screen, BBS, MUD prompt, `/health`)
- **Combat display**: Non-caster classes no longer see the MP bar in combat — only the Stamina bar
- **Level-up**: Paladin (+5/level), Bard (+5/level), and Alchemist (+10/level) no longer gain BaseMaxMana on level-up
- **Migration**: Existing characters with accumulated mana get it zeroed automatically via `RecalculateStats()`

### Mana Classes vs Stamina Classes

| Mana (show Mana bar) | Stamina (show Stamina bar) |
|---|---|
| Cleric, Magician, Sage | Warrior, Barbarian, Paladin, Ranger |
| Tidesworn, Wavecaller, Cyclebreaker | Assassin, Bard, Jester, Alchemist |
| Abysswarden, Voidreaver | |

---

## Prestige Class Spell Casting Fix

The 5 prestige classes (Tidesworn, Wavecaller, Cyclebreaker, Abysswarden, Voidreaver) have 25 defined spells but were blocked from casting by hardcoded class checks that only recognized Cleric/Magician/Sage. This has been fixed:

- **SpellSystem**: `HasSpells()` and `CanCastSpell()` now recognize prestige classes
- **Spell Learning**: Prestige classes can learn spells at the Level Master via a new hybrid menu (`[A] Abilities` / `[S] Spells`)
- **Combat Quickbar**: Prestige classes now get both abilities AND spells on their 1-9 quickbar — abilities fill first, then spells in remaining slots

---

## Bug Fixes & Improvements

- **NPC Notification Wrong Locations** — NPC story notifications (e.g., "Greta has been spotted pacing near the Healer's") now use the NPC's actual location instead of hardcoded wrong locations. Greta's notification pointed to the Healer when she was at the Weapon Shop; Pip's pointed to the Auction House when he was at Main Street
- **Username Overwrite in Interactive Auth** — Fixed a critical bug in `MudServer.InteractiveAuthAsync()` where the login username was overwritten with the display name, causing all subsequent saves to write to a new database row keyed by the display name instead of the account name. This could cause apparent "account name changes" for affected players
- **WeaponType Lost on Inventory Equip** — Equipment converted from inventory items (via Inn, Home, Team Corner, or inventory management) now correctly infers `WeaponType` using `ShopItemGenerator.InferWeaponType()`. Previously, weapons equipped from inventory always had `WeaponType.None`, causing ability weapon requirements to fail (e.g., Bard instrument abilities, Ranger bow abilities, Assassin dagger abilities)
- **Party Removal Shows Members** — When removing an ally from the dungeon party via `[R]`, the list of removable members is now displayed before the number prompt. Previously only showed "Enter party number to remove (2-N)" without listing who was at each position, making it impossible to identify members (especially in MUD streaming mode where the original party list may have scrolled off)
- **Royal Bodyguard Stat Rebalance** — King's dungeon bodyguards (royal mercenaries) now scale using the same stat formulas as normal NPCs from `NPCSpawnSystem`. Previously had inflated multipliers and a quadratic HP bonus that made them significantly overpowered at high levels, trivializing end-game dungeon content. Existing bodyguards will retain their old stats until dismissed and re-hired
- **Combat Event Pruning** — The `combat_events` telemetry table is now automatically pruned during the world sim save cycle, keeping only the last 7 days or 1,000 rows (whichever is fewer). Prevents unbounded database growth from combat logging
- **Orphaned Player Data Cleanup** — When player accounts are deleted, orphaned rows in `sleeping_players`, `online_players`, and `combat_events` are now automatically cleaned up each world sim save cycle
- **Immigrant & Child NPCs Now Join Factions** — NPCs created via immigration (race extinction prevention) and children who come of age now get faction assignments based on their class, alignment, and personality traits. Previously these NPCs were always factionless, diluting faction distributions over time
- **Dashboard Excludes Dead NPCs** — The observation dashboard's distribution graphs (class, race, faction, location, level, age) now only count living NPCs. Permadead NPCs no longer skew the statistics

---

## Files Changed

### Settlement Feature
- `Scripts/Core/GameConfig.cs` — `Settlement = 505` in GameLocation enum; settlement constants; proposal constants (deliberation ticks, cooldown, endorsement cost, new buff values)
- `Scripts/Systems/SettlementSystem.cs` — **NEW** — Core singleton with enums, state classes, ProcessTick, migration/contributions/building selection/construction; NPC proposal catalog (12 templates), ProposalTemplate/ActiveProposal/ProposedBuildingState classes, proposal engine (GenerateProposal, ProcessDeliberation, ResolveProposal), extended save/load
- `Scripts/Locations/SettlementLocation.cs` — **NEW** — Player location with dynamic descriptions, building progress, contribute gold, `[P] Proposals` menu with voting, 9 NPC-proposed building services (Arena, Thieves' Den, Mystic Circle, Prison, Scouts' Lodge, Library, Herbalist Hut, Gambling Hall, Oracle's Sanctum)
- `Scripts/Systems/WorldSimulator.cs` — ProcessTick call in SimulateStep(); settlement activity in ProcessNPCActivities()
- `Scripts/Systems/LocationManager.cs` — Register SettlementLocation; MainStreet ↔ Settlement navigation
- `Scripts/Locations/MainStreetLocation.cs` — `[>] The Outskirts` menu option (all 3 display methods), gated on 5+ settlers
- `Scripts/Core/Character.cs` — SettlementBuffType, SettlementBuffCombats, SettlementBuffValue, HasSettlementBuff properties
- `Scripts/Systems/SaveDataStructures.cs` — Settlement buff fields in player save data; SettlementSaveData in WorldStateData
- `Scripts/Systems/SaveSystem.cs` — Settlement buff serialization; world state settlement serialization
- `Scripts/Core/GameEngine.cs` — Restore settlement buff fields and world state settlement on load
- `Scripts/Systems/WorldSimService.cs` — SaveSettlementState/LoadSettlementState methods; persistence in init/save cycle
- `Scripts/Systems/CombatEngine.cs` — Settlement buff decrement; defense/damage/XP/gold bonus application across all combat paths
- `Scripts/Locations/BaseLocation.cs` — Extended buff display for all 6 settlement buff types in `/health`
- `Scripts/Locations/DungeonLocation.cs` — TrapResist buff applied to riddle and puzzle trap damage

### Mana/Stamina Class Split
- `Scripts/Core/Character.cs` — `IsManaClass` computed property; `RecalculateStats()` zeroes mana for non-caster classes
- `Scripts/Locations/LevelMasterLocation.cs` — Removed `BaseMaxMana +=` from Paladin (+5), Bard (+5), Alchemist (+10) level-up paths
- `Scripts/Locations/BaseLocation.cs` — `ShowStatusLine()`, `ShowBBSStatusLine()`, `GetUserChoice()` MUD prompt, `ShowHealthStatus()` all show Stamina for non-mana classes
- `Scripts/Systems/CombatEngine.cs` — `DisplayCombatStatus()` skips MP bar for non-mana classes; `DisplayCombatStatusBBS()` uses `IsManaClass` check

### Prestige Spell Fix
- `Scripts/Systems/SpellSystem.cs` — `HasSpells()` and `CanCastSpell()` recognize prestige classes (Tidesworn, Wavecaller, Cyclebreaker, Abysswarden, Voidreaver)
- `Scripts/Systems/SpellLearningSystem.cs` — Uses `SpellSystem.HasSpells()` instead of hardcoded 3-class check
- `Scripts/Locations/LevelMasterLocation.cs` — Hybrid `[A] Abilities` / `[S] Spells` menu for prestige classes at Level Master
- `Scripts/Systems/CombatEngine.cs` — Prestige quickbar: abilities fill first, then spells in remaining 1-9 slots

### Bug Fixes
- `Scripts/Systems/TownNPCStorySystem.cs` — NPC notifications use actual NPC location instead of hardcoded wrong locations
- `Scripts/Server/MudServer.cs` — `InteractiveAuthAsync()` no longer overwrites login username with display name
- `Scripts/Locations/BaseLocation.cs` — `ConvertInventoryItemToEquipment()` infers `WeaponType` via `ShopItemGenerator.InferWeaponType()` for weapons and `InferShieldType()` for shields
- `Scripts/Systems/ShopItemGenerator.cs` — `InferShieldType()` made public for use by inventory equip conversion
- `Scripts/Locations/DungeonLocation.cs` — Party member list displayed in `RemoveTeammateFromParty()` before the number prompt
- `Scripts/Locations/CastleLocation.cs` — Royal bodyguard stat scaling aligned with NPC norms; removed quadratic HP bonus; reduced WeapPow/ArmPow/Defence multipliers across all 4 roles
- `Scripts/Systems/SqlSaveBackend.cs` — New `PruneCombatEvents()` and `PruneOrphanedPlayerData()` methods
- `Scripts/Systems/WorldSimService.cs` — Calls `PruneCombatEvents()` and `PruneOrphanedPlayerData()` in world state save cycle
- `Scripts/Systems/NPCSpawnSystem.cs` — New `DetermineFactionForNPC(NPC)` overload using class/alignment/personality floats; called in `GenerateImmigrantNPC()`
- `Scripts/Systems/FamilySystem.cs` — `ConvertChildToNPC()` now assigns faction via `DetermineFactionForNPC()`
- `web/ssh-proxy.js` — Dashboard `getDashSummary()` distributions use `alive` filter instead of all NPCs
