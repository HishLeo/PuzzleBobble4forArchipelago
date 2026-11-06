# Changelog

All notable changes to this project will be documented in this file.

## [1.2.0] - 2026-11-06

Updated project to version 1.2.0.

### APWorld (v1.2.0)

#### General Options
- Moved the **Tamtam Damage Buff** option to the **Other Options** section.

### Archipelago Client (v1.2.0)

#### VS Improvements
- Extended the **Tamtam Damage Buff** to work in both **VS/Story** and **VS/Contest** modes.

#### Item Handling
- Added a new animation when receiving an item from another player.
- Added a separate animation when receiving a trap from another player.
- Fixed a bug that incorrectly applied a delay to certain received items.

## [1.0.0] - 2026-11-02

Updated project to version 1.0.0.

### APWorld (v1.0.0)

#### New Game Modes
- Added three new game modes:
  - **Puzzle/Story**
  - **VS/Story**
  - **VS/Contest**
- Added **Simple** and **Complete** variants across the game modes. Simple variants include a limited portion of each mode's content, while Complete variants include the full mode experience.

#### Puzzle/Story
- Added Tarot Cards and their corresponding unlocks.
- Added two new goals.
- Added a customizable randomizer difficulty option.

#### VS/Story
- Added character unlocks and enemy progression checks.
- Added a new goal.
- Added a customizable number of playable characters.
- Added an optional extra enemy.

#### VS/Contest
- Added consecutive victory tracking and checks.
- Added a new goal.
- Added a customizable number of playable characters.
- Added a customizable consecutive wins requirement.
- Added an optional difficulty increase.

#### General Options
- Added a customizable starting amount of Level Skips.
- Added an option to buff the player's damage when playing as Tamtam.
- Added an option to fix Packy's damage. In the original game, Packy cannot shoot Rainbow Bubbles. This option restores that ability, although it may occasionally mute Packy's voice or cause him to shoot one more or one fewer bubble than intended.

### Archipelago Client (v1.0.1)

- Updated the client to detect and support all new APWorld modes, options, and features.
- Added two new commands:
  - `/skips` — Displays the currently available Level Skips.
  - `/controls` — Displays the updated game controls.

#### New Controls
- **L1 / R1:** Switch between worlds, Tarot Cards, or enemies, depending on the current game mode.
- **L2:** Return to the previous menu. Available from the selection menus in Puzzle/Arcade, Puzzle/Story, and VS/Story.
- **L2 (during gameplay):** Return to the corresponding selection menu.
- **R2:** Use a Level Skip. In VS modes, this deals massive damage to the opponent, but victory is not guaranteed.
- **Up / Down:** Switch characters in Puzzle/Arcade and VS/Story.

#### VS/Story Improvements
- Added a new status message: **"You are CHARACTER vs ENEMY"**. Since the player's character sprite cannot be changed on the enemy selection screen, this message makes it easier to identify both the player's character and the selected opponent.

#### VS/Contest Improvements
- Replaced the **"Round 01"** display with **"Wins 00"**, which tracks consecutive victories.

#### Item Handling
- Improved item reception logic.
- Added an item delay when receiving items outside an unfinished level. Items received during gameplay are delivered when the level is completed.

#### Other
- Several bug fixes and general improvements.

### PopTracker (v1.0.0)

- Added **Puzzle/Story** support.
- Added **VS/Story Simple** support.
- Added **VS/Story Complete** support.
- Added **VS/Contest Complete** support.
- Added automatic check visibility based on the selected game modes.
