# Usurper Reborn v0.50.4 — Mana Potions, Gold Audit & World Boss Fix

Teammates can now share mana potions in combat, comprehensive gold audit logging catches exploits, and a world boss double-reward race condition is fixed.

## Improvements

### Mana Potion Support for Teammates
The [H] Aid Ally combat action (formerly "Heal Ally") now offers three options:
- **Give healing potion(s)** — restore an ally's HP (unchanged)
- **Give mana potion(s)** — restore an ally's MP from your own supply, with 1-potion or Full options
- **Cast healing spell** — cast a heal spell on an ally (unchanged)

The target selection screen shows MP status when giving mana potions, including which allies have no mana pool. The [H] option now appears whenever any teammate needs HP *or* MP and you have the means to help.

### NPC & Companion Mana Potion AI
- **Companions** now stock mana potions on recruit, rest, and daily reset. Healer, Hybrid, and Bard companions receive mana potions scaled to their level.
- **NPC team members** (spellcaster classes) now spawn with mana potions proportional to their level.
- **Combat AI**: Teammates automatically drink a mana potion when their mana drops below 30%, displayed with a blue combat message. This happens before offensive actions, so casters stay effective throughout longer fights.

### Comprehensive Gold Audit Logging
Added debug logging to all major gold-changing operations to detect and trace exploits:
- **Login snapshot**: Logs gold, bank, and total wealth on every login (`GOLD_LOGIN`)
- **Save audit**: Logs gold totals on every save; flags suspicious wealth-to-earnings ratios >5x (`GOLD_AUDIT`)
- **Combat**: Logs gold gained from single and multi-monster victories
- **Bank**: Logs all deposits, withdrawals, transfers, robbery, loans, daily interest, and guard wages
- **Shops**: Logs weapon and armor sales (single and bulk Sell All)
- **Quests**: Logs quest gold rewards
- **Castle**: Logs treasury withdrawals
- **Arena**: Logs PvP gold theft
- **Dark Alley**: Logs loan shark loans

All entries use the `GOLD`, `GOLD_AUDIT`, or `GOLD_LOGIN` categories for easy filtering in debug logs.

## Bug Fixes

### World Boss Double Reward Race Condition
Online players who participated in (but didn't land the killing blow on) a world boss received gold and XP twice — once via direct SQL update and again via in-memory addition to their active session. When the session saved, both additions could persist depending on timing. Fixed by checking if the player has an active online session before choosing the reward path: online players receive rewards in-memory only (persisted on next session save), offline players receive rewards via SQL only (loaded on next login).

## Files Changed
- `Scripts/Core/GameConfig.cs` — Version 0.50.4
- `Scripts/Core/GameEngine.cs` — `GOLD_LOGIN` snapshot on player load
- `Scripts/Systems/CombatEngine.cs` — `HandleHealAlly()` expanded with mana potion option; `canHealAlly` checks broadened to include mana-needing teammates; menu labels updated from "Heal Ally" to "Aid Ally"; `TryTeammateHealAction()` adds mana potion self-use at <30% MP; new `TeammateUseManaPotion()` method; GOLD logging on combat victory (single and multi-monster)
- `Scripts/Systems/CompanionSystem.cs` — `Companion.ManaPotions` and `MaxManaPotions` properties; `RefillCompanionPotions()` stocks mana potions for caster companions; companion-to-Character wrapper copies `ManaPotions`; `SyncCompanionPotions()` syncs mana potions back after combat
- `Scripts/Systems/NPCSpawnSystem.cs` — Spellcaster NPCs now receive mana potions on equip/initialization
- `Scripts/Systems/WorldBossSystem.cs` — Fixed double gold/XP for online non-killing players; online players get in-memory rewards only, offline players get SQL rewards only
- `Scripts/Systems/SaveSystem.cs` — `GOLD_AUDIT` on every save with suspicious wealth detection
- `Scripts/Systems/QuestSystem.cs` — GOLD logging on quest rewards
- `Scripts/Locations/BankLocation.cs` — GOLD logging on all bank operations (deposit, withdraw, transfer, robbery, loan, interest, wages)
- `Scripts/Locations/WeaponShopLocation.cs` — GOLD logging on weapon sales
- `Scripts/Locations/ArmorShopLocation.cs` — GOLD logging on armor sales
- `Scripts/Locations/ArenaLocation.cs` — GOLD logging on PvP gold theft
- `Scripts/Locations/DarkAlleyLocation.cs` — GOLD logging on loan shark loans
- `Scripts/Locations/CastleLocation.cs` — GOLD logging on treasury withdrawals
