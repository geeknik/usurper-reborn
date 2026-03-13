# Usurper Reborn v0.52.1 - Guild Board & Teammate Buff Fix

---

## Guild Board on Main Street (New)

A new **[R] Guild Board** option on Main Street lets online players browse all guilds at a glance. The board displays:

- **Guild Rankings** — Top 10 guilds ranked by member count, showing guild name, member count (with online count), leader name, and bank gold
- **Your Guild Status** — If you're in a guild, shows your guild's members, bank balance, XP bonus, and motto. If not, shows how to create or join one
- **Gold highlighting** — #1 guild displayed in gold, top 3 in cyan

The Guild Board makes the guild system discoverable without needing to know slash commands. Players can see which guilds are active, who's online, and how to get involved.

Available in all three display modes (visual, BBS/compact, screen reader). Online mode only — offline players see a message explaining guilds require online mode.

---

## City Control System Overhaul

Comprehensive audit and fix pass for the team-based city control system, addressing 12 bugs affecting how player-led teams and NPC teams interact when competing for city control.

### Critical Fixes
- **Dead/imprisoned NPCs no longer hold city control** — `GetControllingTeam()` and `GetCityControllers()` now filter out dead (`IsDead`), deceased (`HP <= 0`), and imprisoned (`DaysInPrison > 0`) NPCs. Previously, dead or imprisoned NPCs with the `CTurf` flag could phantom-control the city, collecting tax revenue and granting shop discounts for a ghost team.
- **Player defenders now count in challenge power** — `ChallengeForCityControl()` previously only counted NPC defender stats. If a player was on the defending team, their STR/DEF/Level contributed nothing, making player-led teams artificially weak against NPC challenges. Player stats are now included on both attacker and defender sides.
- **Imprisoned challengers lose city control** — `ImprisonChallenger()` now clears `CTurf` on imprisoned NPCs and checks if remaining teammates still hold control, removing city control from the team if nobody else qualifies.
- **New team joiners no longer get instant city control** — `TransferTeamStatus()` previously blindly copied the leader's `CTurf` flag to new members. Now validates whether the team actually controls the city before granting the flag.

### Balance Changes
- **10% defender home turf bonus** — Teams defending city control now receive a 10% power bonus, giving an advantage to the established controller and making city control more meaningful to hold.
- **Solo teams can now challenge for city control** — The automated NPC challenge system previously required teams to have 2+ members. Solo teams (including player-only teams) can now participate.

### Cleanup Fixes
- **QuitTeam properly cleans up city control** — When the last member of a controlling team quits, `RemoveCityControl()` is now called. Previously left phantom `CTurf` flags with no team behind them.
- **SackTeamMember checks remaining control** — When a member with `CTurf` is sacked, the system now checks if remaining members still have it and calls `RemoveCityControl()` if nobody does, instead of silently losing control.
- **Dead NPCs excluded from gang wars** — `GetActiveCombatMembers()` now checks `IsDead` and `DaysInPrison` to prevent permanently dead or imprisoned NPCs from being selected for team combat.
- **Auto-battle CTurf only granted to alive members** — `ProcessAutoBattleResults()` and `SetRemoveTurfFlags()` now only grant `CTurf` to alive, non-imprisoned winning team members.
- **Dead NPCs no longer receive team mail** — Victory and defeat mail notifications now skip permanently dead NPCs.
- **Automated city challenge filters improved** — `ProcessCityChallenge()` now filters dead and imprisoned NPCs from team listings when evaluating potential challengers.

---

## BBS List

- Added **A-Net Online** (sysop: StingRay) — `bbs.a-net.online` telnet :1337 / ssh -p 1338 (Synchronet). Added to both in-game BBS list and website.

---

## End-Game Party Balance Overhaul

Comprehensive rework of Old God boss fights to enforce the core design philosophy: **the end-game requires a balanced, geared party**. Seven interlocking mechanics progressively introduced across the 7 Old God bosses (floors 25-100) to ensure solo play and undergeared parties cannot brute-force late-game content.

### Boss Enrage Timers
Every Old God now has an enrage timer. At 50% and 75% of the round limit, warning messages appear. When the timer expires, the boss doubles its damage, gains 1.5x defense, and gets 2 extra attacks per round. This creates a hard DPS check — parties that can't deal enough damage will be overwhelmed. Enrage timers get tighter on harder bosses: Maelketh (30 rounds) down to Manwe (15 rounds).

### Party-Wide AoE Attacks
Bosses from Veloura (Floor 40) onward periodically unleash AoE abilities that hit the entire party. If a **tank** (Warrior, Paladin, Barbarian, Tidesworn, Abysswarden) is present and taunting, they absorb 60% of the AoE damage meant for other party members. Without a tank, every party member takes full damage — healers and casters will fall quickly. AoE frequency increases from every 5 rounds (Veloura) to every 2 rounds (Manwe), and damage scales from 150 to 700.

### Boss Channeling & Interrupts
From Thorgrim (Floor 55) onward, bosses begin channeling devastating abilities over 2 rounds. If not interrupted, the channel completes for massive damage (600-1500). **Fast characters** (Assassins and Rangers get +20% interrupt chance, Voidreavers +15%) can attempt to interrupt based on an Agility + Dexterity/2 speed check against a threshold. Companion AI now prioritizes interrupting channels before casting spells. Without an interrupter in the party, channeled abilities will devastate the group.

### Stacking Corruption DoT
From Noctura (Floor 70) onward, bosses apply corruption stacks to random party members (30% chance per round in phase 2+). Each stack deals escalating damage per round (15-40 per stack depending on the boss), stacking up to 10 times. **Only healer classes** (Cleric, Paladin, Bard, Wavecaller, Abysswarden) can cleanse corruption — they automatically prioritize cleansing when a party member has 3+ stacks. Without a healer, corruption will inevitably overwhelm the party.

### Doom Debuff
From Aurelion (Floor 85) onward, bosses can apply Doom to a party member (15% chance per round in phase 3). Doom is a countdown — when it reaches 0, the target is **instantly killed**. Only healer classes can dispel Doom, and companion AI prioritizes dispelling Doom above all other actions. Doom countdown ranges from 3 rounds (most bosses) to 2 rounds (Manwe).

### Boss Phase Immunities
When bosses transition to phase 2, they gain temporary immunity to either physical or magical damage for 4 rounds. Physical immunity means only spells deal damage; magical immunity means only weapon attacks work. During immunity, 10% of damage still gets through. This forces parties to have **both physical and magical damage dealers** — pure melee or pure caster parties will have dead rounds during phase transitions. Boss immunity type is thematic: Noctura and Terravok have physical immunity (requiring magic), Aurelion has magical immunity (requiring melee), and Manwe has both (alternating).

### Potion Cooldown in Boss Fights
During Old God encounters, using a potion triggers a 2-round cooldown before another can be used. This prevents unlimited potion-tanking that previously made solo play viable. The game displays a message directing players to rely on their healer instead.

### Divine Armor Rework
Divine armor (Old God damage reduction) now has a proper gear check:
- **Artifact weapons** (Sunforged Blade, Godforged, Voidtouched): Fully bypass divine armor (100%)
- **Enchanted weapons**: Bypass 50% of divine armor (was 100% — any enchantment previously gave full bypass)
- **Unenchanted weapons**: Full divine armor penalty applies

### Per-Boss Difficulty Progression
| Boss | Floor | Enrage | AoE | Channel | Corruption | Doom | Immunity |
|------|-------|--------|-----|---------|------------|------|----------|
| Maelketh | 25 | 30 rds | — | — | — | — | — |
| Veloura | 40 | 28 rds | 150 dmg/5 rds | — | — | — | — |
| Thorgrim | 55 | 25 rds | 250 dmg/4 rds | 600 dmg/6 rds | — | — | — |
| Noctura | 70 | 22 rds | 350 dmg/4 rds | 800 dmg/5 rds | 20/stack | — | Physical |
| Aurelion | 85 | 20 rds | 450 dmg/3 rds | 1000 dmg/5 rds | 25/stack | 3 rds | Magical |
| Terravok | 95 | 18 rds | 550 dmg/3 rds | 1200 dmg/4 rds | 30/stack | 3 rds | Physical |
| Manwe | 100 | 15 rds | 700 dmg/2 rds | 1500 dmg/3 rds | 40/stack | 2 rds | Both |

---

## Armor Thematic Bonus Fix

Dungeon loot armor with name suffixes like "of the Arcane" or "of Shadows" now correctly receives thematic stat bonuses matching the suffix. Previously, `ApplyThematicBonuses()` used the **template name** (e.g., "Leather Gloves") instead of the **final item name** (e.g., "Leather Gloves of the Arcane"), so a pair of arcane gloves would get ranger stats from the "leather" keyword instead of caster stats from the "arcane" keyword.

---

## Bug Fixes

- **Teammate attack buffs never applied** — Stimulant Brew, Battle Brew, Bard songs, and all other abilities that set `TempAttackBonus` on teammates had no effect on their damage. The teammate attack calculation in both `ProcessTeammateAction()` and `ProcessTeammateActionMultiMonster()` never added `TempAttackBonus` to attack power, despite the buff being correctly set on the Character object. Player attacks correctly used the bonus. Now adds `TempAttackBonus` to teammate attack power in both combat paths.
- **Teammate buff durations never expired** — `TempAttackBonusDuration` and `TempDefenseBonusDuration` were only decremented for the player each round. Teammate buffs persisted indefinitely once applied. Now decrements both attack and defense buff durations for all living teammates at end of each round.
- **Companions/NPCs never picked up accessory loot** — When the player passed on a dropped necklace or ring, companions with empty accessory slots would not equip it. The `itemPower` calculation for accessories only summed direct Item properties (Strength, Dexterity, etc.) but most accessory stats (Constitution, Intelligence) are stored in `LootEffects` — so accessories appeared to have zero power. A "Vitality Amulet" with all its stats in LootEffects scored `itemPower = 0`, hitting the zero-power skip and never being offered to companions. Now includes LootEffects stat values (Constitution, Intelligence, AllStats) in the accessory power calculation.
- **Royal bodyguards only attacked once per round** — Warrior bodyguards (and other classes) always got exactly 1 attack per round regardless of class. `GetAttackCount()` checked for an equipped MainHand weapon item, but bodyguards are created as Character objects with raw `WeapPow` stats and no actual weapon in `EquippedItems`. This caused an early return of 1, bypassing all class bonuses (Warrior +2 extra swings, agility attacks, artifact bonuses, etc.). A Warrior bodyguard was dealing ~67% less damage than expected. Now recognizes mercenaries with `WeapPow > 0` as armed.
- **NPC class distribution heavily skewed** — Immigrant NPCs used pure uniform random class selection across 11 classes, with no awareness of current population balance. Combined with race restrictions (Trolls, Gnolls, Orcs can't be Clerics/Alchemists) and higher attrition for squishy healer classes, servers ended up with very few Clerics and Alchemists over time. Now uses inverse-population weighting: underrepresented classes get boosted spawn chance, overrepresented classes get reduced chance. Children coming of age also had incomplete class pools — neutral-aligned children could never become Clerics, Alchemists, Jesters, Barbarians, or Paladins. All 11 base classes now available. A one-time rebalance pass runs on server startup, reassigning NPCs from overrepresented classes (e.g. Sage, Warrior, Bard) to underrepresented ones (e.g. Cleric, Alchemist), recalculating HP and mana to match the new class.
- **NPC race distribution heavily skewed toward Human** — The classic NPC templates are 52% Human (31/60) with no Trolls, Gnolls, Gnomes, Half-Elves, or Mutants at all. The immigration system only spawned new NPCs for races with fewer than 2 alive members — pure extinction prevention with no diversity balancing. Over time the population stayed ~50% Human. Immigration now has a diversity phase: each world sim tick, the most underrepresented race (below 60% of the equal-share target) gets 1 additional immigrant, gradually diversifying the population over time.
- **"Monster attacks you!" shown when monster targets teammate** — In dungeon combat with teammates, every monster attack displayed "Kobold attacks you!" before showing the actual target (e.g. "The Kobold attacks Aldric!"). The attack message was printed unconditionally before `ProcessMonsterAction()` selected the actual target via `SelectMonsterTarget()`. Moved the "attacks you!" message inside `ProcessMonsterAction()` so it only displays when the monster actually targets the player.
- **Companion death returned broken equipment** — When a companion died, `KillCompanion()` and `TriggerCompanionDeathByParadox()` manually constructed Item objects from companion Equipment with only 5-12 of 40+ properties preserved. Returned items lost all enchantments (fire, frost, lightning, etc.), proc effects (lifesteal, critical strike, thorns, etc.), Constitution, Intelligence, Agility, Stamina, MinLevel, and LootEffects. Items also lost their correct slot type, all becoming type 16 (Magic) which made them unsellable and unequippable. Now uses the existing `ConvertEquipmentToLegacyItem()` method which was also enhanced to preserve elemental enchantments and proc effects as LootEffects.
- **Boss kill summary showed 0 damage for spellcasters** — The shareable boss kill summary (`[X dmg]`) always showed 0 for Magicians and other spell-focused classes. Two damage paths never tracked `TotalDamageDealt`: AoE spell damage via `ApplyAoEDamage()` tracked stats but not the summary counter, and single-monster spell damage via `ApplySpellEffects()` had no tracking at all. A Magician using Fireball or Chain Lightning dealt 100% of their damage through these untracked paths.
- **Boss potion cooldown bypassed via Use Item** — The potion cooldown check was only in `ExecuteHeal` (quick-heal), but players could bypass it by using `[U] Use Item` to drink potions directly from inventory during boss fights. Added cooldown check at the top of `ExecuteUseItem`.
- **Boss potion cooldown off-by-one** — Potion cooldown decremented at round start before the player acts, so a "2-round cooldown" was effectively 1 round. Initial cooldown now set to `BossPotionCooldownRounds + 1` to compensate. Removed duplicate cooldown overwrite in ExecuteHeal→ExecuteUseItem path.
- **Manwe dual immunity never triggered magical phase** — Manwe is configured with both physical and magical immunity (alternating), but `if/else if` logic meant only physical immunity fired on phase 2. Magical immunity on phase 3 was dead code. Separated into independent phase 2 and phase 3 triggers so both immunity types activate.
- **Divine armor was display-only** — `GetDivineArmorReduction()` computed the damage reduction percentage for Old God bosses but the value was never applied to actual combat damage calculations. Added `DivineArmorReduction` to BossCombatContext and applied it in both physical attack and magical damage paths.
- **Boss enrage warning severity reversed** — The 50% warning (earlier, less urgent) displayed in bright red while the 75% warning (later, critical) displayed in yellow. Swapped colors so urgency matches severity.
- **Boss enrage box not screen-reader safe** — The enrage activation box used Unicode box-drawing characters without a ScreenReaderMode guard, causing garbled output for screen reader users.
- **Status effect death broke group combat** — When the party leader died from status effects (poison, plague) during boss fights, combat unconditionally ended even if grouped players survived. Now checks `anyGroupedAlive()` and allows surviving group members to fight on, matching the monster-attack death behavior.
- **Teammate death from corruption/doom skipped death handlers** — When companions or NPC teammates died from boss corruption stacks or Doom countdown, no death handlers ran — no grief system trigger, no companion equipment return, no IsDead flag, no combat channel cleanup. Added proper death handling in `ProcessBossRoundMechanics` matching the pattern used for monster-attack deaths.
- **Burst damage could skip boss phase 2 mechanics** — If massive damage pushed a boss from phase 1 directly to phase 3, phase 2 was entirely skipped — no dialogue, no immunity activation, no Maelketh minion spawns. Phase transitions now iterate through each intermediate phase sequentially.
- **City control granted to dead/imprisoned NPCs** — `TransferCityControl()` could grant the `CTurf` flag to permanently dead or imprisoned NPCs when their team won control. `GetTurfController()` could return a dead or imprisoned NPC as the active city controller. `SetRemoveTurfFlags()` granted winner `CTurf` without checking if the winner was alive or free. All three now filter on `IsAlive`, `!IsDead`, and `DaysInPrison <= 0`.
- **Artifact divine armor bypass checked collection instead of equipment** — `GetDivineArmorReduction()` used `HasSunforgedBlade()` which checks if the player has ever *collected* a Sunforged Blade artifact, not whether it's currently *equipped*. A player who collected the artifact but equipped a different weapon still bypassed divine armor. Now only checks the currently equipped weapon's name for artifact keywords.
- **Healers couldn't self-cleanse doom/corruption** — `TryHealerCleanse()` filtered out the healer itself from both doom and corruption cleanse target lists (`t != healer`). A doomed or corrupted healer would always die even though it had the ability to save itself. Removed the self-exclusion filter.
- **Boss mechanic death didn't check grouped player survival** — When the party leader died from boss corruption or doom ticks, combat ended immediately without checking if grouped players were still alive. Now checks `anyGroupedAlive()` before breaking combat, matching the pattern used for status effect and monster-attack deaths.
- **Dead player processed status effects after boss mechanic death** — If boss round mechanics (corruption/doom) killed the player, `ProcessStatusEffects()` still ran on the dead player, potentially showing misleading status messages. Now skips status effect processing for dead players.

---

## Files Changed

- `Scripts/Core/GameConfig.cs` — Version 0.52.1; 14 boss balance constants (potion cooldown, enrage multipliers, corruption/doom parameters, tank absorption, phase immunity, channel interrupt, divine armor bypass rates)
- `Scripts/Core/Character.cs` — `CorruptionStacks`, `DoomCountdown`, `PotionCooldownRounds` properties for boss fight party mechanics
- `Scripts/Core/Monster.cs` — `IsChanneling`, `ChannelingRoundsLeft`, `ChannelingAbilityName`, `IsPhysicalImmune`, `IsMagicalImmune`, `PhaseImmunityRounds`, `IsEnraged` properties for boss fight mechanics
- `Scripts/Systems/CombatEngine.cs` — 16 new BossCombatContext properties (enrage, immunity, corruption, doom, channeling, AoE config, `DivineArmorReduction`); ~400 lines of boss party mechanic helpers (`CheckBossEnrage`, `ProcessCorruptionTick`, `ApplyCorruptionStacks`, `ProcessDoomTick`, `ApplyDoom`, `StartBossChannel`, `ProcessBossChannel`, `TryInterruptBossChannel`, `ProcessBossAoE`, `IsHealerClass`, `TryHealerCleanse`, `ApplyPhaseImmunity`, `ApplyPhaseImmunityDamage`, `ProcessBossRoundMechanics`); potion cooldown check in `ExecuteHeal` and `ExecuteUseItem`; physical/magical immunity damage checks; divine armor reduction in physical and magical damage paths; companion AI priority actions (cleanse, dispel doom, interrupt channels); phase immunity triggers for phase 2 and phase 3 (Manwe dual immunity); phase transition loop for intermediate phases on burst damage; teammate death handlers in `ProcessBossRoundMechanics` for corruption/doom kills; status effect death checks `anyGroupedAlive()` for group combat; `ApplyAoEDamage` now tracks `TotalDamageDealt`; `ApplySpellEffects` and `ProcessSpellCasting` now accept and track `CombatResult` for spell damage summary; `TryHealerCleanse` allows healers to self-cleanse doom/corruption; boss mechanic death checks `anyGroupedAlive()` for group combat; dead players skip `ProcessStatusEffects()` after boss mechanic death
- `Scripts/Systems/OldGodBossSystem.cs` — `ConfigureBossPartyMechanics()` with per-boss difficulty scaling (Maelketh through Manwe); `GetDivineArmorReduction()` reworked: checks equipped weapon name only (not artifact collection status), artifact weapons fully bypass, enchanted weapons 50% bypass, unenchanted full penalty
- `Scripts/Systems/LootGenerator.cs` — `ApplyThematicBonuses()` now uses final item name instead of template name, fixing suffix-based bonuses ("of the Arcane", "of Shadows", etc.)
- `Scripts/Systems/CityControlSystem.cs` — `GetControllingTeam()` and `GetCityControllers()` now filter dead/imprisoned NPCs; `ChallengeForCityControl()` includes player stats on both attacker and defender sides, adds 10% defender home turf bonus, filters dead/imprisoned from challenger team; `TransferCityControl()` only grants CTurf to alive, non-dead, non-imprisoned NPCs
- `Scripts/Systems/TeamSystem.cs` — `TransferTeamStatus()` validates city control before granting CTurf to new joiners; `GetActiveCombatMembers()` checks `IsDead` and `DaysInPrison`; `QuitTeam()` calls `RemoveCityControl()` on team dissolution; `SackTeamMember()` checks remaining CTurf holders; `SetRemoveTurfFlags()` validates winner alive/non-dead/non-imprisoned before granting CTurf, only mails alive members; `GetTurfController()` filters dead/imprisoned NPCs; `ProcessAutoBattleResults()` filters dead/imprisoned from CTurf assignment
- `Scripts/Systems/ChallengeSystem.cs` — `ImprisonChallenger()` clears CTurf and checks remaining team control; `ProcessCityChallenge()` allows solo teams (`>= 1` member), filters dead/imprisoned NPCs from team listings
- `Scripts/Systems/GuildSystem.cs` — New `GetAllGuilds()` method returning all guilds ordered by member count and bank gold
- `Scripts/Systems/CombatEngine.cs` — Added `teammate.TempAttackBonus` to attack power in both `ProcessTeammateAction()` and `ProcessTeammateActionMultiMonster()`; added teammate buff duration decrement loop (attack and defense) in round-end processing; fixed accessory `itemPower` in `TryTeammatePickupItem()` to include LootEffects stat values (Constitution, Intelligence, AllStats) so companions correctly evaluate necklaces and rings; `GetAttackCount()` now recognizes mercenaries with `WeapPow > 0` as armed, enabling Warrior multi-attacks and all class-based attack bonuses for royal bodyguards
- `Scripts/Core/Character.cs` — `ConvertEquipmentToItem()` enhanced to preserve elemental enchantments (fire, frost, lightning, poison, holy, shadow) and proc effects (lifesteal, mana steal, critical strike, etc.) as LootEffects; added Agility, Stamina, MinLevel preservation
- `Scripts/Systems/CompanionSystem.cs` — `KillCompanion()` and `TriggerCompanionDeathByParadox()` now use `ConvertEquipmentToLegacyItem()` instead of manual lossy Item construction
- `Scripts/Locations/MainStreetLocation.cs` — `ShowGuildBoard()` method with ranked guild display and player status; `[R]` menu option added to visual, BBS/compact, and screen reader menus; ProcessChoice handler for `R` key; A-Net Online added to in-game BBS list
- `Scripts/Core/GameEngine.cs` — A-Net Online added to in-game BBS list
- `Scripts/Systems/NPCSpawnSystem.cs` — New `PickWeightedClass()` method uses inverse-population weighting for immigrant class selection; `RebalanceClassDistribution()` one-time startup pass reassigns NPCs from overrepresented to underrepresented classes with HP/mana recalculation
- `Scripts/Systems/WorldSimulator.cs` — `ProcessNPCImmigration()` rewritten with race diversity balancing; most underrepresented race gets 1 immigrant per tick; extracted `SpawnImmigrant()` helper
- `Scripts/Systems/WorldSimService.cs` — Calls `RebalanceClassDistribution()` after loading NPCs from database
- `Scripts/Systems/FamilySystem.cs` — Neutral-aligned children coming of age now have all 11 base classes available instead of 6
- `web/index.html` — A-Net Online added to website BBS list table
