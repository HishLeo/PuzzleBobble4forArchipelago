# Puzzle Bobble 4 Archipelago Client

The Puzzle Bobble 4 Archipelago Client connects Puzzle Bobble 4 to an Archipelago multiworld session. It communicates with the game, tracks player progress, sends location checks, receives and applies items, and provides additional quality-of-life features.

The client supports four game modes:

- Puzzle/Arcade
- Puzzle/Story
- VS/Story
- VS/Contest

---

# User Guide

## Requirements

- **Game:** Puzzle Bobble 4 NTSC-U/C (Serial: `SLUS-00754`).
- **Emulator:** DuckStation.
- **Archipelago:** A generated multiworld session containing Puzzle Bobble 4.
- **Client:** Puzzle Bobble 4 Archipelago Client.

## Installation

1. Download the latest release of the Puzzle Bobble 4 Archipelago Client.
2. Extract the downloaded archive.
3. Open DuckStation and load Puzzle Bobble 4 (NTSC-U/C).
4. Run the Archipelago Client.
5. Enter your Archipelago connection information.
6. Connect to your multiworld session.

## Connecting to Archipelago

When starting the client, enter the following information:

- **AP Host:** The Archipelago server address (for example, `archipelago.gg`).
- **Port:** The port assigned to your Archipelago session.
- **Slot Name:** Your player slot name.
- **Password:** The room password, if one is required.

Once connected, the client receives the session data and activates the corresponding game modes and options.

## Supported Game Modes

### Puzzle/Arcade

Play through randomized Puzzle/Arcade worlds. Completing levels sends location checks to Archipelago, while received items can affect your gameplay and progression.

### Puzzle/Story

Progress through randomized Story mode, including Tarot Cards and their corresponding unlocks. Complete levels and meet progression requirements to send location checks to Archipelago.

### VS/Story

Fight your way through the enemy progression. Unlock characters and opponents as you advance, with victories sending location checks to Archipelago.

### VS/Contest

Compete in consecutive victories. The required number of playable characters, consecutive wins, and other difficulty settings depend on the generated world options.

## Quality-of-Life Features

The client adds several features and control improvements across the supported game modes.

### General Features

- All characters are unlocked in Puzzle modes.
- **R2** can be used to activate a Level Skip while playing.
- Added informative on-screen text in certain game modes.
- Improved item handling, including delayed item delivery when receiving items outside an unfinished level.

### Puzzle/Arcade

- Change your character in World Selection using **Up / Down**.
- Switch between unlocked worlds using **L1 / R1**.
- Press **R2** to return to the beginning of the world selection.
- Press **L2** to return to the main menu. When playing a level, **L2** takes you back to World Selection.

### Puzzle/Story

- Switch between unlocked Tarot Cards using **L1 / R1**.
- **Note:** Using the regular Left / Right controls may take you back to the previous card.
- Press **L2** to return to the main menu. When playing a level, **L2** takes you back to Card Selection.

### VS/Story

- Switch between unlocked enemies using **L1 / R1** or **Left / Right**.
- Change your character using **Up / Down**.
- **Note:** Your character's sprite does not change on the enemy selection screen. Check the on-screen text to see which character you are playing as.
- Press **L2** to return to the main menu. When playing a level, **L2** takes you back to Enemy Selection.

### VS/Contest

- Updated the on-screen text to display your consecutive wins instead of the total round number.

## Client Commands

The client includes the following commands:

| Command | Description |
|---|---|
| `/skips` | Displays the number of currently available Level Skips. |
| `/controls` | Displays the controls available in the current game mode. |

## Level Skips

Level Skips are received as Archipelago items and can be used during gameplay with **R2**.

Their behavior depends on the active game mode:

- **Puzzle modes:** A Level Skip allows you to skip the current level.
- **VS modes:** A Level Skip deals massive damage to the opponent. However, victory is not guaranteed.

Use `/skips` to check how many uses are currently available.

## Troubleshooting

If you encounter an issue:

1. Make sure DuckStation is running Puzzle Bobble 4 NTSC-U/C.
2. Make sure the Archipelago Client is running.
3. Check that your AP Host, port, slot name, and password are correct.
4. Make sure your Archipelago session was generated with the appropriate Puzzle Bobble 4 options.
5. If the issue persists, report it in the project's GitHub Issues section. Include the client log and a description of what happened.

---

# Technical Documentation

This section documents the behavior implemented by the released client: the memory values it reads and writes, the state transitions it monitors, and the conditions required before an Archipelago check is submitted.

### Memory access architecture

The client reads and writes DuckStation's emulated PlayStation RAM directly on Windows. It locates the running DuckStation process, resolves the exported `RAM` pointer from the process module, and accesses game addresses as offsets from that RAM base. It does **not** use DuckStation's GDB server and does not pause emulation for each read/write. The monitor polls memory approximately every 75 ms.

The addresses below are offsets within emulated RAM, not Windows process virtual addresses. They refer to the NTSC-U/C build listed in the User Guide and may not apply to other game revisions or emulators.

The client reads the game's mode bytes separately from its screen/level-state byte. A mode value identifies the game mode; the state value identifies the current phase, such as selection, loading, active gameplay, or completion. Checks rely on mode-specific state transitions rather than a single byte in isolation.

In a live session, generated settings and progression data come from Archipelago slot data. Local JSON configuration is not authoritative for a connected seed. The client also keeps session-level state to prevent duplicate checks and repeated actions.

The technical documentation is organized by game mode because each mode has its own progression system, selection screens, checks, and gameplay behavior.

## 1. Puzzle/Arcade

### 1.1 Game Mode Detection

Puzzle/Arcade is identified by two mode-classification bytes:

| Address | Required value | Meaning |
|---|---:|---|
| `0x00167D5B` | `00` | Puzzle mode (not VS) |
| `0x00167D5A` | `00` | Arcade rather than Story/Contest |

Both conditions must be true. The client does not classify the mode from `0x00167A0C` alone, because that byte is also used by other modes and screens.

Within Puzzle/Arcade, the letter/world-selection screen uses `0x00167A0C = 01`. Active level gameplay requires `0x00167A0C = 03` **and** `0x00167A10 = 02`. The latter is a phase/state byte, not a mode identifier.

### 1.2 World Selection

#### World Detection

The client tracks the world-selection menu while `0x00167A0C = 01`. It reads `0x00167A8F` as the current base-menu/letter identifier and `0x00167D3A` to determine Bub's facing direction:

- `00`: facing left.
- `01`: facing right.

The menu node and facing direction together identify the outgoing world being viewed. This distinction matters because the menu byte can identify a junction such as `Menu0`, rather than the destination letter. In particular, `Menu0` uses `00`, so that value must not automatically be interpreted as world A while the selection screen is active.

The menu is represented as a graph of junctions. The client maps the left/right exit from each junction to its destination, including terminal worlds U, V, W, ?, X, Y and Z.

#### World Navigation

The client adds menu navigation by monitoring `0x0016BCEB`. It reacts to a newly detected input instead of repeatedly executing an action while a button is held.

| Input byte | Button | Client action in world selection |
|---|---|---|
| `FB` | L1 | Previous usable menu node |
| `F7` | R1 | Next usable menu node |
| `FD` | R2 | Return to `Menu0` |
| `FE` | L2 | Return to the main menu |

L1/R1 navigation follows the client's ordered menu list, skipping nodes that have no unlocked outgoing destination in the requested direction. `Menu0` is the fallback when no usable destination is found. After selecting a node, the client writes its menu/camera coordinates and corrects Bub's facing if it points toward a locked exit.

The original in-game look-direction changes are also monitored. If Bub turns toward a locked exit, the client redirects the facing byte to the opposite direction when that exit is usable.

#### World Lock and Unlock

Worlds A and B are available from the start. Worlds C–Z (including `?`) are initially locked until their corresponding Archipelago world-unlock items are received.

The client maintains a set of received world unlocks and refreshes the effective lock state when an item arrives. Current item naming is `Unlock World <letter>`; legacy world-unlock names are also accepted for compatibility. In multiworld seeds, an unlock may remain pending until its required prerequisite world unlocks have been received. It becomes usable only after those prerequisites are satisfied.

Lock state is enforced in the added world-selection navigation. A menu node is considered usable only if at least one connected exit leads to an unlocked destination. If the currently faced exit is locked, the client redirects the facing direction to the other usable exit where possible. This prevents the added controls from deliberately selecting a world that the seed has not unlocked.

### 1.3 Level Detection

#### Level Letter Detection

During active Puzzle/Arcade gameplay, `0x00167A8F` contains the zero-based world-letter ID:

| Value | World | Value | World |
|---:|:---:|---:|:---:|
| `00` | A | `0D` | N |
| `01` | B | `0E` | O |
| `02` | C | `0F` | P |
| `03` | D | `10` | Q |
| `04` | E | `11` | R |
| `05` | F | `12` | S |
| `06` | G | `13` | T |
| `07` | H | `14` | U |
| `08` | I | `15` | V |
| `09` | J | `16` | W |
| `0A` | K | `17` | ? |
| `0B` | L | `18` | X |
| `0C` | M | `19` | Y |
|  |  | `1A` | Z |

The client captures the destination letter while the player is still in world selection, before entering level 1. This is necessary because the menu byte can refer to the base junction, and the game may reset the letter byte during a level transition. The captured destination is retained as the active gameplay letter so that a transient `00` during completion does not incorrectly identify another world as A.

#### Level Number Detection

The current level index is read from `0x00167A8D`. It is zero-based in memory, so:

`displayed level = memory value + 1`

For example, memory value `00` represents level 1 and `04` represents level 5. A location identifier is therefore `A1`, `A2`, …, `A5`, or `U1` … `Z5`.

#### Level Check Identification

The client combines the active world letter with the displayed level number to form the Archipelago location name:

- World ID `00`, level index `00` → `A1`.
- World ID `00`, level index `04` → `A5`.
- World ID `17`, level index `02` → `?3`.

The client keeps an in-session duplicate guard for each location. If Puzzle/Arcade checks are disabled in the generated seed, level completion is still detected for local state handling, but no Arcade location is sent.

### 1.4 Level Properties

#### Difficulty Detection

The generated seed assigns a mode to each Puzzle/Arcade level. The client receives this per-level plan from Archipelago slot data and applies the selected difficulty before the player enters the level.

The difficulty byte is `0x00167A91`:

- `01`: Normal.
- `02`: Expert.

The client writes the selected value for the target level and reads it back to verify that DuckStation retained the write. It prepares level 1 while the player is viewing a world destination in the selection screen, and prepares the next level in the same world after detecting completion of the current level.

#### Game Variant Detection

The Original/Another World byte is `0x00167A96`:

- `00`: Original.
- `01`: Another World.

The per-level plan combines variant and difficulty into one of four assignments: `NormalO`, `ExpertO`, `NormalAW`, or `ExpertAW`.

For each target level, the client writes both `0x00167A96` and `0x00167A91`, then reads both values back. If the game does not retain either value, the client records a diagnostic rather than assuming the requested mode was applied.

### 1.5 Level Completion Detection

The client recognizes Puzzle/Arcade completion when `0x00167A10` changes from `02` (active gameplay) to `03` (level ended), while the mode bytes still identify Puzzle/Arcade and `0x00167A0C` is `03`.

It then:

1. Resolves the completed world letter, preferring the letter captured on entry if the game temporarily reset the memory letter to `00`.
2. Converts the zero-based level index at `0x00167A8D` to a displayed level number by adding one.
3. Builds the location name, such as `A1` or `?5`.
4. Checks the in-session duplicate guard.
5. Prepares the next level's randomized variant/difficulty if the completed level was 1–4.
6. Submits the location to Archipelago if Puzzle/Arcade checks are enabled in the seed.

The `02 -> 03` transition is edge-triggered. State `03` is not treated as a permanent completion flag. The generic letter-world completion handler is gated to Puzzle/Arcade so Puzzle/Story and VS transitions cannot be misidentified as Arcade checks.

### 1.6 Level Skip

R2 activates Level Skip only during active gameplay. For Puzzle/Arcade, the client requires `0x00167A0C = 03` and `0x00167A10 = 02`.

If the current level's location has already been confirmed by Archipelago, pressing R2 advances past it without consuming a Level Skip. Otherwise, at least one available Level Skip is required. When a use is available, the client:

1. Prepares the next level's randomized variant/difficulty if the current level is not level 5.
2. Writes `03` to `0x00167A10` to request the level-ending/advance transition.
3. Decreases the available Level Skip count by one.
4. Updates the on-screen skip counter and saves the updated count.

A Level Skip does not create a location check for the skipped level. A check is submitted only when the client detects the normal completion transition. If the level was already checked, the skip is free and the item count is unchanged.

Level Skip is player-triggered; receiving the item does not automatically skip a level.## 2. Puzzle/Story

Puzzle/Story uses Tarot Cards instead of the letter-world progression used by Puzzle/Arcade. Each card contains five levels. The client tracks the selected card, enforces the unlock state for its additional navigation controls, applies the generated Normal/Expert assignment to each card level, and reports completed card levels as Archipelago checks.

### 2.1 Game Mode Detection

Puzzle/Story is identified by the same two mode-classification bytes used elsewhere in the client:

| Address | Required value | Meaning |
|---|---:|---|
| `0x00167D5B` | `00` | Puzzle mode (not VS) |
| `0x00167D5A` | `01` | Story rather than Arcade |

Both values must match. The client distinguishes the card-selection screen from active card gameplay using `0x00167A0C` and `0x00167A10`:

| Screen/state | `0x00167A0C` | `0x00167A10` |
|---|---:|---:|
| Tarot Card selection | `03` | `02` or `03` |
| Active Tarot level | `05` | `02` |
| Tarot level completion transition | `05` | `02` → `03` |

The `02` → `03` transition is used for completion detection only when the Puzzle/Story mode flags and the Story gameplay value (`0x00167A0C = 05`) also match. This prevents a transition in another mode from being treated as a Tarot level check.

### 2.2 Tarot Card Detection

The selected Tarot Card ID is read from `0x00167A8F`. The client maps the game's internal IDs to card names using the following fixed selection order:

| Selection order | Tarot Card | ID |
|---:|---|---:|
| 1 | Fool | `12` |
| 2 | Star | `00` |
| 3 | Moon | `01` |
| 4 | Hermit | `08` |
| 5 | Tower | `02` |
| 6 | Chariot | `06` |
| 7 | Death | `05` |
| 8 | Devil | `04` |
| 9 | Hanged Man | `09` |
| 10 | Magician | `07` |
| 11 | Wheel of Fortune | `0A` |
| 12 | Judgement | `0B` |
| 13 | Lovers | `0D` |
| 14 | Temperance | `0E` |
| 15 | Hierophant | `14` |
| 16 | Emperor | `13` |
| 17 | Empress | `15` |
| 18 | High Priestess | `16` |
| 19 | Sun | `0F` |
| 20 | Justice | `10` |
| 21 | Strength | `11` |
| 22 | World | `17` |

The table's selection order is the order used by the client's L1/R1 navigation; it is not the numerical order of the IDs. Each card has five levels. The level index is read from `0x00167A8D` and is zero-based, so the displayed level is the memory value plus one.

The card name and displayed level form the Archipelago location name. For example, Fool at level index `00` becomes `Fool 1`, and World at level index `04` becomes `World 5`.

### 2.3 Level Difficulty

Puzzle/Story has its own Normal/Expert plan for Tarot levels. In a live Archipelago session, the plan is received from slot data under `puzzle_story_difficulty_plan`; the client does not use a local Tarot difficulty file as the authority for a connected seed. The plan contains an assignment for each of the 22 cards' five levels (110 assignments total), identified by names such as `Fool 1` and `World 5`.

Tarot gameplay difficulty uses two separate memory bytes:

| Address | Normal | Expert | Purpose |
|---|---:|---:|---|
| `0x00167A8C` | `00` | `01` | Actual Tarot gameplay difficulty |
| `0x00167A91` | `01` | `02` | Difficulty display/color value |

The client writes both values for the target card level and reads them back for diagnostics. This is separate from Puzzle/Arcade's Original/Another World setting; the Tarot difficulty handler does not apply the Arcade variant setting.

The first level of a card is prepared on the Story entry transition where `0x00167A10` changes from `04` to `05`. For levels 2–5, the client applies the next level's difficulty immediately after detecting completion of the previous level (`02` → `03`). This is necessary because the game does not provide the same first-level entry transition for each subsequent level. The client also avoids writing the same level assignment repeatedly.

If the slot data does not contain a readable Tarot plan, the client logs a diagnostic and does not invent a difficulty assignment for the missing level.

### 2.4 Tarot Card Navigation

#### Card Selection

The client considers Tarot Card selection active when the mode flags identify Puzzle/Story, `0x00167A0C = 03`, and `0x00167A10` is either `02` or `03`. State `03` can occur briefly while the game is moving between cards; the client remembers the target card and reapplies it after the selection screen settles back to state `02`.

When the client first detects the card-selection screen, it selects the first card in its configured order that is both unlocked and not already completed. It does not assume that the card ID currently present in memory is a valid unlocked destination.

#### Card Switching

The added navigation controls are:

| Input | Client action |
|---|---|
| L1 | Select the previous unlocked, unfinished Tarot Card |
| R1 | Select the next unlocked, unfinished Tarot Card |

Navigation wraps around the 22-card order. While searching in either direction, the client skips cards that have not been unlocked and cards whose fifth level has already been completed. If no eligible card is found, the input has no navigation effect.

When a card is selected, the client writes its ID to `0x00167A8F` and writes the card's stored horizontal selection coordinates to `0x00167B26` (low byte) and `0x00167B27` (high byte). The target is reapplied after the game's card-transition state when necessary. These controls are edge-triggered: holding a button does not continuously repeat the same navigation action.

R2 is ignored on the card-selection screen. It remains reserved for Level Skip during active gameplay.

#### Returning to Previous Cards

The original in-game Left/Right controls are not remapped by the client's additional L1/R1 navigation handler. They remain under the game's native card-selection behavior. As a result, using the regular directional controls can move through the game's own card sequence, including returning to a previous card, rather than applying the client's filtered list of unlocked and unfinished cards.

Use L1/R1 for the additional navigation system when the intention is to move only among cards that the client currently considers available. The client does not claim that the native Left/Right behavior enforces the same unlock/completion filtering.

#### Additional World Movement

World is a special Tarot selection entry. Its card ID is `17`, but its horizontal position is not handled like the fixed coordinates of the other cards. The client writes the World marker (`0x17`) to `0x00167A8F` and then advances the horizontal coordinate stored in `0x00167B26`/`0x00167B27`.

The coordinate is advanced in small steps on successive client polls. When it reaches `0x0639`, it wraps to `0x0000` and the movement cycle repeats. This is a special selection-screen behavior for the World card; it is not a change to the level's gameplay position or to Puzzle/Arcade world navigation.

### 2.5 Card Lock and Unlock

Fool is the only Tarot Card initially present in the client's unlocked-card set. The other cards become available when their corresponding Archipelago progression items are received. The expected item name is `Unlock Tarot <CardName>`, for example `Unlock Tarot Star` or `Unlock Tarot World`. The item handler also accepts the same name with a `FILL_` prefix.

Receiving a recognized unlock item adds that card ID to the client's unlocked-card set. The item is processed as progression state and does not need to wait for active gameplay. The client does not independently grant a card because another card was completed in a live seed; the generated Archipelago item placement is authoritative for live unlocks.

When a card's fifth level is completed, the client marks that card as completed and excludes it from the added L1/R1 navigation. On connection, the client also examines the server's already-checked locations. If a previously checked location ends in ` 5` and its card name matches the Tarot list, that card is restored to the completed-card set. This allows the navigation filter to recognize cards completed before the current client run.

The local-development test path contains additional simulated unlock behavior, but it is disabled in the released live-session configuration and is not the source of progression in an Archipelago seed.

### 2.6 Level Completion Detection

A Puzzle/Story level completion is recognized when all of the following are true:

- The mode bytes identify Puzzle/Story (`0x00167D5B = 00` and `0x00167D5A = 01`).
- The Story gameplay mode byte is `0x00167A0C = 05`.
- The level-state byte changes from `0x00167A10 = 02` to `03`.

The client reads the Tarot Card ID from `0x00167A8F` and the zero-based level index from `0x00167A8D`, converts them to a card name and displayed level, and builds the corresponding location name, such as `Fool 1` or `World 5`.

For levels 1–4, the client applies the next level's Normal/Expert assignment immediately after recognizing the completion transition. It then records the completed card-level identifier in its in-session tracking set.

If Puzzle/Story checks are enabled in the generated seed, the client submits the location to Archipelago. It uses duplicate guards to avoid submitting the same card-level check repeatedly during the session. If Puzzle/Story is disabled in the seed, completion is still logged locally, but no Tarot location check is sent.

Completing level 5 marks the card as completed, removes it from the client's filtered L1/R1 navigation, and updates the progress message when the active goal is `All_Tarot`. The server's confirmed checked-location list is used to restore this completed-card status after reconnecting.

### 2.7 Level Skip

R2 activates Level Skip during active Puzzle/Story gameplay (`0x00167A0C = 05` and `0x00167A10 = 02`). R2 is deliberately ignored while the player is on the Tarot Card selection screen.

For Puzzle/Story, the client identifies the current level using the Tarot Card name and displayed level. If that card-level check has already been recorded by the client during the current run, R2 advances the game without consuming a Level Skip. Otherwise, an available Level Skip is required and one use is consumed.

The client requests the level-ending transition by writing `03` to `0x00167A10`. Unlike Puzzle/Arcade, it does not prepare a letter-world difficulty/variant for the next level; Tarot progression uses the separate card-based difficulty plan. The skip itself is not a normal level-completion event and does not independently award the skipped card-level check.

Receiving a Level Skip does not activate it automatically. The player must press R2 during an active Tarot level.
---

## 3. VS/Story

### 3.1 Game Mode Detection

The client identifies VS/Story by reading the two game-mode bytes:

| Address | Value | Meaning |
|---|---:|---|
| `0x00167D5B` | `01` | VS mode |
| `0x00167D5A` | `00` | Story mode |

Both conditions must be true. This distinguishes VS/Story from VS/Contest, which uses `0x00167D5A = 01`.

The game-state byte at `0x00167A0C` provides an additional state check:

- `0x00167A0C = 03`: VS/Story selection flow.
- `0x00167A0C = 07` and `0x00167A10 = 02`: active VS/Story fight.

The client also watches the transition from `0x00167A10 = 06` to `08` while `0x00167A0C = 03`. It changes the state to `08` and activates its custom enemy-selection handling.

### 3.2 Character Lock and Unlock

The game has 16 playable-character unlock flags. They occupy the following memory range:

| Address range | Meaning |
|---|---|
| `0x00187D2C`–`0x00187D3B` | One unlock flag per character ID, `00` locked and `01` unlocked |
| `0x00167D03` | Character visibility setting; the client writes `00` to keep characters visible |

The character ID is read from `0x00167A45`. ID `00` is the player character displayed as **Bub** by the client HUD; the Archipelago character name for that ID is **Bob**. IDs `01`–`0F` correspond to the remaining playable characters.

Character unlock behavior depends on the selected VS/Story mode:

- **Simple:** all playable characters start locked. Receiving `Unlock-VS/Story` unlocks all 16 characters.
- **Complete:** the seed supplies a selected character pool. A character becomes selectable when it belongs to that pool and its corresponding `Unlock VS/Story Character <Name>` item has been received.
- VS/Story and VS/Contest unlocks are independent. An unlock item for one mode does not unlock the same character in the other mode.

The client reapplies the appropriate flags while polling game state, because the game may rewrite them during initialization or when returning to character selection.

### 3.3 Enemy Lock and Unlock

The selected opponent's ID is stored at `0x00167A8A`. The client maintains an ordered opponent list and updates the game's enemy ID, character position, camera position, and selection HUD when the player changes opponents.

The selectable opponent order is:

| Opponent | ID |
|---|---:|
| Bob | `00` |
| Alkanet | `01` |
| Marino | `02` |
| Kurol | `03` |
| Tamtam | `04` |
| Cleon | `05` |
| G | `06` |
| Bramb | `07` |
| Develon | `08` |
| Gigant | `09` |
| Secret | `10` |
| Madam Luna | `0A` |

Secret is included in this list only when the `extra_enemy` option is enabled. Dreg (`0B`) is not part of the selectable list; the game handles the Dreg encounter as a special opponent after the regular sequence.

The enemy progression rules are:

- Bob is always the starting opponent.
- In **Simple**, defeating an opponent unlocks the next opponent in the configured list.
- In **Complete**, the client accepts `Progressive Enemy` and individual `Unlock <Name> enemy` items.
- When every applicable Simple-mode enemy victory check has been confirmed by Archipelago, the client marks the Simple enemy progression complete and allows navigation past the local win chain.

The client stores the unlocked enemy IDs in its own session state. The initial set contains Bob only.

### 3.4 Character and Enemy Checks

The client only submits a VS/Story victory check after it detects a confirmed victory. It watches the end-of-fight transition:

- `0x00167A10` changes from `02` to `03`.
- `0x00192EBF = 07` identifies the victory result.
- `0x00192EBF = 08` identifies a defeat; no victory check is sent.
- Any other result value is treated as unknown, and no victory check is sent.

For a confirmed VS/Story victory, the client reads:

- Opponent ID from `0x00167A8A`.
- Player-character ID from `0x00167A45`.

The location name depends on the mode:

| Mode | Location name |
|---|---|
| Simple | `vs <Opponent> win` |
| Complete | `Defeat <Opponent> as <Character>` |

In Complete mode, the character must belong to the seed's selected character pool and its VS/Story character unlock item must have been received. Otherwise, the victory is observed but no check is submitted for that character/opponent combination.

The client uses a per-session duplicate guard before sending a check. If the check cannot be sent, it removes the temporary guard entry so a later attempt is not incorrectly treated as submitted.

Dreg and Secret are treated as special opponents for the post-victory transition. The client writes `09` to `0x00167A8A` only after the corresponding special-opponent check has been queued or was already queued in the current session.

### 3.5 Simple Mode

VS/Story Simple tracks one victory check per opponent, rather than a separate check for every playable-character/opponent combination.

At the start of the session, Bob is the only locally unlocked opponent. Winning against the current opponent unlocks the next one in the active opponent list. This progression is handled locally after the victory is confirmed.

The Simple character roster has a separate global unlock: `Unlock-VS/Story`. Until that item arrives, the client keeps the playable characters locked. Once received, all 16 playable characters are unlocked.

The client also checks whether all enemy victory locations that exist in the current seed have been confirmed by Archipelago. Once they are all confirmed, it records that Simple enemy progression is complete and allows the player to navigate through the remaining opponents without relying on the local win chain.

### 3.6 Character and Enemy Selection

#### Character Selection

During the VS/Story enemy-selection screen, the client reads the current playable character from `0x00167A45`.

- **Up:** selects the next unlocked playable character.
- **Down:** selects the previous unlocked playable character.

The client searches the character unlock-flag range `0x00187D2C`–`0x00187D3B` and skips locked characters. If only one character is unlocked, the selection remains on that character.

#### Enemy Selection

The client activates its enemy-selection handler when it observes the `06` → `08` state transition while `0x00167A0C = 03`.

- **R1:** selects the next available opponent.
- **L1:** selects the previous available opponent.
- **Right / Left:** provide the corresponding next/previous opponent navigation through the directional-input byte.
- **X:** confirms the currently selected opponent.
- **L2:** returns to the main menu.

The client only allows the game to leave enemy-selection state `08` for state `09` after it has observed X being pressed (`0x0016BCEB = BF`). If the game changes to `09` without that input, the client restores `08`.

When an opponent is selected, the client writes the opponent ID to `0x00167A8A` and updates the associated player and camera coordinates. This allows the selection screen to display the chosen opponent in the correct position.

During an active fight, **L2** returns to enemy selection. The client writes `03` to `0x00167A0C` and then `00` to `0x00167A10`.

### 3.7 Character vs. Enemy Display

On the enemy-selection screen, the client writes a custom banner to the HUD memory range `0x0011A000`–`0x0011A10F`.

The banner is assembled from the current player-character ID at `0x00167A45` and the selected opponent in the client’s enemy list. Its text follows this format:

`you are <Character> vs <Opponent>`

The player-character name is refreshed when the character changes, and the opponent name is refreshed whenever the selected enemy changes.

The client also places a small check-status marker in the HUD:

- `CA`: the corresponding check has not been sent in the current client session.
- `F6`: the corresponding check has already been sent in the current client session.

The check name used for this marker follows the active mode's naming convention: Complete uses the character/opponent combination; Simple uses the opponent victory check.

During an active fight, the selection banner is hidden and the second HUD row is restored to the `Skips` label. The Level Skip counter remains in its dedicated HUD positions.

### 3.8 Secret Character

Secret is an optional additional opponent controlled by the `extra_enemy` option. When enabled, Secret is inserted into the selectable opponent list immediately before Madam Luna. Its enemy ID is `0x10`.

Secret is separate from the 16 playable-character IDs. Selecting Secret causes the client to apply a game-specific compatibility fix when the fight begins:

- It sets the two attack-type bytes at `0x00194BFE` and `0x00194BC6` to `02`.
- If `0x00167A6E` is `00`, the client temporarily changes it to `01`.
- On the fight's `02` → `03` completion transition, the client restores that flag to `00` only if it was the client that changed it and the value is still `01`.

This avoids overwriting a value that the game has changed independently.

The client recognizes Secret as a special opponent and can construct the corresponding victory-check name. However, the source code comments identify Secret as a client-side optional opponent that is not yet part of the current APWorld version. Therefore, enabling the client option does not by itself guarantee that the matching Archipelago location exists in a generated seed.

### 3.9 Level Skip

During an active VS/Story fight, pressing **R2** uses one Level Skip if at least one use is available.

Unlike Puzzle modes, a VS Level Skip does not advance a level or change the `0x00167A10` level-state transition. Instead, the client writes `FF` to:

`0x00192E32`

The client then decrements the available Level Skip count, saves the updated count, and refreshes the HUD. If no uses remain, the input is ignored.

The effect is a large damage event against the opponent; it does not guarantee an immediate victory. The normal victory/defeat detection still determines the result of the fight.

Pressing **L2** during the fight has a different function: it returns the player to enemy selection rather than consuming a Level Skip.

## 4. VS/Contest

### 4.1 Game Mode Detection

The client identifies VS/Contest by reading the two game-mode bytes:

| Address | Value | Meaning |
|---|---:|---|
| `0x00167D5B` | `01` | VS mode |
| `0x00167D5A` | `01` | Contest mode |

Both conditions must be true. This distinguishes VS/Contest from VS/Story, which uses `0x00167D5A = 00`.

The game-state byte at `0x00167A0C` is used to distinguish the Contest opponent-selection screen from active gameplay:

- `0x00167A0C = 02`: opponent-selection screen.
- `0x00167A0C = 04` and `0x00167A10 = 02`: active VS/Contest match.

The client uses the mode flags and game-state byte together. A state value by itself is not sufficient to identify an active Contest match.

### 4.2 Character Lock and Unlock

VS/Contest uses the same 16 playable-character unlock flags as VS/Story:

| Address range | Meaning |
|---|---|
| `0x00187D2C`–`0x00187D3B` | One unlock flag per playable-character ID |
| `0x00167D03` | Character visibility setting; the client keeps this at `00` so locked characters remain visible |

Each character flag uses `00` for locked and `01` for unlocked. The player-character ID is read from `0x00167A45`.

The unlock policy depends on the selected mode:

- **Simple:** all 16 characters begin locked. Receiving `Unlock-VS/Contest` unlocks all 16 characters.
- **Complete:** only characters included in the seed's `contest_selected_characters` pool and whose corresponding `Unlock VS/Contest Character <Name>` item has been received are unlocked.
- Contest unlocks are separate from VS/Story unlocks. Receiving a character unlock for one mode does not unlock that character in the other mode.

The client reapplies the mode's unlock policy while polling game state, because the game can rewrite the flags during initialization or when returning to character selection.

### 4.3 Simple and Complete Modes

The APWorld selects either `vs_contest_simple` or `vs_contest_complete`; both cannot be enabled in the same configuration.

**Simple** uses one shared progression track, regardless of which playable character is used. The seed provides one check for each consecutive-win milestone. The playable-character roster remains locked until the global `Unlock-VS/Contest` item is received.

**Complete** creates a separate progression track for each character selected in the seed. A character can generate checks only if:

1. Its ID is included in `contest_selected_characters`.
2. Its `Unlock VS/Contest Character <Name>` item has been received.

A character outside the selected pool, or one whose unlock item has not arrived, cannot generate Complete-mode Contest checks.

The configured number of selected characters is controlled by `contest_characters` and is constrained to 3–16. The actual selected names are supplied by slot data in `contest_selected_characters`.

### 4.4 Consecutive Wins

The game stores the current consecutive-win count at:

`0x00167A86`

The target number of wins is supplied by slot data as `custom_contest_wins`. The client accepts values from 3 to 30; the default used when no value is supplied is 10.

The client does not infer a victory merely from the counter changing. It first requires the VS result transition:

- `0x00167A0C = 04` identifies the VS/Contest gameplay state.
- `0x00167A10` transitions from `02` to `03` at the end of the match.
- `0x00192EBF = 07` confirms a victory.
- `0x00192EBF = 08` confirms a defeat.

Only after the victory animation is confirmed does the client accept an increase in the consecutive-win counter as eligible for location checks.

If the counter decreases, the client treats this as a lost streak. No new check is generated for the loss, and previously completed milestone checks remain completed in Archipelago.

When the selected playable character changes, the client resets its local comparison baseline to zero for the new character's streak.

### 4.5 Wins Display

The game normally labels the consecutive-win counter as `ROUND`. In VS/Contest only, the client replaces this label with ` WINS`.

The label occupies six bytes beginning at:

`0x00167310`

The client writes the ASCII text ` WINS` followed by a null terminator. When leaving VS/Contest, it restores `ROUND` followed by a null terminator.

The numeric wins counter itself remains the game's own counter at `0x00167A86`; the client changes the label, not the underlying counter.

### 4.6 Victory Checks

Each newly reached consecutive-win milestone corresponds to an Archipelago location.

| Mode | Location name |
|---|---|
| Simple | `Contest Win <Number>` |
| Complete | `Contest Win <Number> as <Character>` |

For example, the third milestone is `Contest Win 3` in Simple, or `Contest Win 3 as Bob` in Complete.

After a confirmed victory, the client compares the current value of `0x00167A86` with its previous value. It submits every newly crossed milestone, up to the configured `custom_contest_wins` target. This also allows the client to account for more than one newly crossed milestone if the counter advances by multiple values between polls.

In Complete mode, checks are submitted only for a character in the seed's selected pool whose individual Contest unlock item has been received. In Simple mode, character identity does not change the location name or create a separate track.

The client keeps a duplicate guard for each milestone. A loss resets the in-game streak, but it does not erase checks already sent to Archipelago. Reaching those milestones again does not create duplicate checks.

### 4.7 Enemy Randomizer

VS/Contest uses a client-side enemy randomizer. It chooses from all 16 playable-character IDs (`00`–`0F`) and stores the opponent ID at:

`0x00167A46`

At the beginning of a new VS/Contest session, the client creates a pool containing all 16 IDs. It selects one at random for the first opponent and removes that ID from the pool. After each confirmed victory, the next selection is randomized when the game returns to the opponent-selection screen (`0x00167A0C = 02`).

The randomizer does not repeat an opponent until all 16 IDs have been selected. Once the pool is empty, it refills the pool and begins another cycle.

The randomizer uses these states:

- **NeedInitialEnemy:** waiting to choose the first opponent of the session.
- **Fighting:** an opponent has been selected and remains fixed during the match.
- **WaitingForNextEnemyAfterVictory:** a victory was confirmed; wait until the game returns to opponent selection before choosing again.
- **HoldAfterLoss:** a defeat or unknown result occurred; do not reroll the opponent.

Leaving VS/Contest resets the randomizer. A failed memory write does not remove the attempted ID from the pool; the client verifies the written value before accepting the selection.

After the first randomized opponent is selected, the client also writes `FF` to `0x00167A8A` and `0x00167A8B` to reset the game's first-opponent markers.

### 4.8 Contest Enemy Difficulty

The `increase_difficulty_contest` option adds a configured amount to the game's Contest enemy-difficulty value at:

`0x00192E50`

The option is read from `vs_contest_options.increase_difficulty_contest` (or the supported top-level fallback). Its accepted range is 0–200; the default is 0.

The client applies the increase only during active VS/Contest gameplay (`0x00167A0C = 04` and `0x00167A10 = 02`). It captures the original value once per match, adds the configured amount, and clamps the result to the maximum byte value (`FF`). It does not add the increase repeatedly on every polling cycle.

When the match ends or the player leaves active gameplay, the client restores the captured value if the memory still contains the value that the client applied. This avoids overwriting a different value written by the game.

### 4.9 Packy Fix

When the `fix_packy` option is enabled, the client corrects Packy's attack-type selection during active VS matches, including VS/Contest.

The player character ID is read from `0x00167A45`; the Contest opponent ID is read from `0x00167A46`. When either side is Packy (`0D`), the client selects the appropriate attack type based on the corresponding damage value.

This correction is limited to active VS gameplay. It does not change character unlocks or victory-check logic.

### 4.10 Level Skip

During an active VS/Contest match, pressing **R2** uses one Level Skip if at least one use is available.

VS modes use a dedicated memory signal rather than the Puzzle/Arcade level-state transition. The client writes `FF` to:

`0x00192E32`

It then decrements the available Level Skip count, saves the updated count, and refreshes the HUD. If no uses remain, the input is ignored.

The VS Level Skip is a damage effect; it does not automatically count as a victory. The normal result-animation and consecutive-win checks still determine whether a Contest milestone is completed.

## 5. General Client Systems

This section documents systems shared by multiple game modes. Mode-specific behavior is described in the corresponding mode sections above.

### 5.1 Archipelago Connection and Slot Data

The client connects to an Archipelago server using the server address, port, slot name, and optional room password entered by the player. It identifies itself to the server as `Puzzle Bobble 4` and requests all received items.

After a successful login, the client reads the slot data supplied by the APWorld. Slot data configures several client systems, including:

- Enabled game modes and their options.
- The selected goal and its required letters, locations, or victory items.
- World-unlock prerequisites.
- The level-mode plan used by Puzzle/Arcade.
- Initial Level Skip inventory (`extra_level_skips`).
- The seed identifier used to isolate saved Level Skip inventory.
- Whether DeathLink is enabled (`death_link`).
- The selected character pool and other mode-specific settings.

The client treats the APWorld's slot data as the authoritative configuration for the connected seed. It does not generate a replacement local level-mode plan when the server has not supplied the required plan.

The received-item event is registered before login, so the client can receive both the initial item backlog and later items. The callback extracts the resolved item name and a stable item identity, then places the item in a thread-safe queue. Actual item effects are applied by the main game-monitoring loop, not directly inside the network callback.

### 5.2 Received Item Processing

Received items pass through a common processing pipeline:

1. Archipelago raises the item-received event.
2. The callback dequeues the item information and adds its name and stable identity to the client's incoming-item queue.
3. The main polling loop checks whether the game is currently showing a victory or defeat result animation.
4. If the result-animation byte is `07` or `08`, item processing pauses and the queued items remain pending.
5. Otherwise, the client processes queued items in order and routes each item to its corresponding handler.

The client uses the item name to identify the effect. It also normalizes supported naming variations, including the `FILL_` prefix used by some item delivery paths.

Progression items are generally applied immediately because they change client-side unlock or goal state rather than the active game board. Examples include world unlocks, Tarot Card unlocks, VS character/enemy unlocks, and victory-progress items.

Gameplay-effect items use the delay rules described below. Level Skip is handled separately: receiving one increments the available-use counter immediately, even when the player is between levels.

If an item arrives during a result animation, it is not discarded. It remains queued until the result animation ends, preventing memory writes from interfering with the game's end-of-level or end-of-match sequence.

### 5.3 Gameplay Item Delay

The regular gameplay-item delay protects the active game board from receiving certain effects at an unsafe time.

The following items use a single pending-item slot:

- Star Bubble
- Metal Bubble
- Rainbow Bubble
- Obstacle Block
- Random Color Bubble
- Falling Ceiling

If one of these items arrives while the player is not in active gameplay, the client stores it until a valid active-gameplay state is detected.

The active-gameplay test requires both:

- `0x00167A10 = 02`, indicating active gameplay.
- `0x00167A0C` matching the current mode:
  - Puzzle/Arcade: `03`
  - Puzzle/Story: `05`
  - VS/Story: `07`
  - VS/Contest: `04`

The pending gameplay-item slot holds only one item. If another delayed gameplay-effect item arrives before the pending item can be applied, the newer item replaces the older pending item. This is a deliberate replace-latest behavior, not a queue of multiple gameplay effects.

Once the correct mode and active-gameplay state are confirmed, the pending item is applied before the client processes newly queued network items in that polling cycle.

Level Skip and Pointer Guide do not use this regular one-slot delay. They have their own handling because their effects and safe application conditions are different.

### 5.4 Pointer Guide Special Delay

Pointer Guide uses a separate state-aware delay because applying it while the Falling Ceiling effect is still in its initial state can interfere with the board.

The Pointer Guide memory flag is:

`0x00192EFC`

The client writes `80` to this address when applying Pointer Guide.

Its handling depends on the game state:

- **During level initialization (`0x00167A10 = 01`):** Pointer Guide is applied immediately. The Falling Ceiling state is ignored.
- **During active gameplay (`0x00167A10 = 02`):** if the Falling Ceiling byte at `0x00192EBD` is nonzero, Pointer Guide is applied immediately.
- **During active gameplay while `0x00192EBD = 00`:** Pointer Guide remains pending until the Falling Ceiling byte changes from zero.
- **Outside a recognized mode or in another game state:** Pointer Guide remains pending until a valid application phase is reached.

The secondary delay is checked independently from the regular gameplay-item delay. A pending Pointer Guide can therefore be applied during the next level's initialization phase, even if the Falling Ceiling byte has not changed.

### 5.5 Level Skip Inventory and Persistence

Level Skip is a consumable item. The client tracks the number of available uses in `_levelSkipCount`, with a supported range of `00`–`99`.

The initial amount comes from the APWorld slot-data field `extra_level_skips`. If a saved inventory already exists for the current seed and slot, the saved amount takes precedence over the initial amount. Each received Level Skip adds one use, up to the maximum of 99.

The client also records stable identities for received Level Skip items. If Archipelago replays an item that the client has already processed, its identity is recognized and the use is not added a second time.

The inventory is saved outside the game directory under the user's local Archipelago data directory. Its filename is scoped by server domain, seed ID, and slot name, so separate seeds and slots do not share the same counter. The save data contains:

- Available Level Skip uses.
- The stable identities of Level Skip items already processed.

Before being written to disk, the save data is protected with Windows DPAPI using the current Windows user account. The client also includes a migration path for save files created by earlier versions.

The counter is saved when it changes, including when a Level Skip is received or used. If a save cannot be read or written, the client logs the problem; the game session can continue, but persistence may not be reliable.

### 5.6 Level Skip Use

Level Skip is always player-triggered with **R2**. Receiving the item does not automatically skip a level or trigger its effect.

The effect depends on the active mode:

- **Puzzle/Arcade and Puzzle/Story:** the client uses the Puzzle level-state transition. It changes `0x00167A10` to `03`. Puzzle/Arcade prepares the next level where applicable; Puzzle/Story follows its Tarot Card progression rules.
- **VS/Story and VS/Contest:** the client writes `FF` to the VS effect byte at `0x00192E32`. It does not advance a level-state counter.

If the current Puzzle level has already been confirmed as checked by Archipelago, pressing R2 can advance without consuming a Level Skip. Otherwise, one available use is required and consumed.

A skipped Puzzle level does not create a location check merely because it was skipped. The normal check-detection logic remains responsible for completing locations.

After a paid use, the client decrements the inventory, saves it, updates the HUD, and reports the remaining count. If no uses are available and the current level is not eligible for a free skip, the input is ignored.

The local `/skips` command displays the current available-use count. It does not send a command to the Archipelago server.

### 5.7 DeathLink

DeathLink is enabled only when the connected seed's slot data explicitly sets `death_link` to true. If the option is disabled or absent, the client does not create or enable the DeathLink service.

When enabled, incoming DeathLinks are placed in a thread-safe queue. A signal attributed to the client's own slot is ignored. If multiple incoming signals accumulate while the player is outside active gameplay, the client collapses the queued signals and applies one reset when a valid active-gameplay state is reached.

The client only applies a received DeathLink while the current mode is actively being played:

| Mode | Required `0x00167A0C` | Required `0x00167A10` |
|---|---:|---:|
| Puzzle/Arcade | `03` | `02` |
| Puzzle/Story | `05` | `02` |
| VS/Story | `07` | `02` |
| VS/Contest | `04` | `02` |

To apply the received DeathLink, the client writes `00` to `0x00167A10`. It deliberately does not write `04`, because that direct state change can cause game problems.

The client suppresses sending a new DeathLink for the loss caused by an incoming DeathLink. Suppression is released only after the game exits the affected gameplay state and a fresh active gameplay state resumes.

For outgoing DeathLinks, the client watches the result-animation byte:

`0x00192EBF`

- `07`: victory animation.
- `08`: defeat animation.

A defeat sends one DeathLink only if the player is in active gameplay and DeathLink is enabled. The client rearms detection after the result byte leaves `08`, preventing the same defeat animation from being sent repeatedly.

In VS modes, the cause identifies the opponent involved in the defeat. In Puzzle modes, the cause describes the player's bubble crossing the deadline.

### 5.8 Trap and Special Item Effects

The client recognizes the following gameplay items and traps:

| Item | Effect |
|---|---|
| Star Bubble | Writes `0A` to the next-bubble byte |
| Metal Bubble | Writes `0B` to the next-bubble byte |
| Rainbow Bubble | Writes `0C` to the next-bubble byte |
| Obstacle Block | Writes `0D` to the next-bubble byte |
| Random Color Bubble | Chooses a random color code from `01`–`08` and writes it to the next-bubble byte |
| Falling Ceiling | Applies the mode-specific Falling Ceiling effect |

The next-bubble byte is:

`0x00188BE0`

The random color range corresponds to the eight standard bubble colors. The special bubble codes are handled directly by the client.

Falling Ceiling uses different memory behavior in Puzzle and VS modes:

- **Puzzle modes:** writes `10` to `0x00192EBD`.
- **VS modes:** reads `0x00192F76` and adds `07` to its current byte value.

The VS behavior is additive; it does not simply overwrite the value with `07`. The byte operation wraps according to its 8-bit representation.

The client classifies traps by their known item names, not only by whether the name contains the word `Trap`. This ensures items such as Rainbow Bubble, Obstacle Block, Random Color Bubble, and Falling Ceiling are displayed as traps in the console.

### 5.9 HUD and Level Skip Counter

The client initializes a custom Level Skip display in the game's HUD memory area. It writes the ASCII label `Skips ` and two decimal digits representing the current inventory.

The primary counter digits are stored at:

- `0x0011A007`: tens digit.
- `0x0011A008`: units digit.

A mirrored copy is maintained at:

- `0x0011A017`: tens digit.
- `0x0011A018`: units digit.

These are ASCII characters, not a binary integer. For example, an inventory of 05 is displayed using the ASCII digits `0` and `5`.

The HUD is initialized once after the memory connection is established, and its digits are refreshed when the inventory changes. The mirror is synchronized during the polling loop.

VS/Story uses a custom first HUD row for the selected player and opponent, so the client does not overwrite that row with the normal mirrored counter. Its Level Skip digits remain on the second row. When leaving the VS/Story HUD layout, the client restores the original captured HUD bytes before reinstating the normal Level Skip display.

### 5.10 Game State and Mode Detection

The client does not rely on a single memory byte to decide which mode is active. It combines the two mode flags with the game-state byte.

| Mode | `0x00167D5B` | `0x00167D5A` | Active gameplay: `0x00167A0C` |
|---|---:|---:|---:|
| Puzzle/Arcade | `00` | `00` | `03` |
| Puzzle/Story | `00` | `01` | `05` |
| VS/Story | `01` | `00` | `07` |
| VS/Contest | `01` | `01` | `04` |

For all four modes, `0x00167A10 = 02` is also required to classify the game as actively playing a level or match.

This combined test is reused by the item-delay system, DeathLink, and other shared gameplay effects. It prevents a state value belonging to one mode from being mistaken for active gameplay in another mode.

The client also tracks previous values of important state and input bytes. Comparing the current value with the previous polling cycle lets it detect transitions, such as entering gameplay, finishing a level, or pressing a menu button, rather than repeatedly reacting to a state that remains unchanged.

### 5.11 Location Check Submission and Confirmation

When a mode-specific event qualifies as a location check, the client converts the location name into an Archipelago location ID using the `Puzzle Bobble 4` game package. It then calls `CompleteLocationChecks` for that ID.

The client distinguishes between a check being detected locally and a check being confirmed by the server:

- **Detected/submitted:** the client found a matching location name and sent its ID to Archipelago.
- **Confirmed:** Archipelago later included the location in its checked-location update.

A local duplicate guard prevents the same check from being repeatedly submitted during one run. At login, the client seeds its local checked-state caches from the server's authoritative list of already checked locations. It also listens for `CheckedLocationsUpdated` and records each confirmed location.

If a location name is not present in the APWorld datapackage, the client reports an error and does not successfully submit that check. A message saying that a check was detected is therefore not proof that Archipelago accepted it.

Confirmed checks are used by other client systems, including completed Tarot Card detection, VS/Story Simple enemy progression, and goal completion.

### 5.12 Goal Progress and Completion

The APWorld supplies the goal configuration through slot data. Depending on the goal, the client can track required letters, required location checks, or required victory items.

When a relevant progression item is received, the client updates its local goal-progress state. When a required location is confirmed by Archipelago, the client updates its confirmed-check cache and reevaluates the goal.

Once every required condition is satisfied, the client calls `SetGoalAchieved()` on the active Archipelago session.

The client also has a legacy Arcade-goal behavior: for the applicable goal type, it can submit remaining Arcade world checks when completing the goal. This special handling is not used as a general-purpose way to submit checks from unrelated modes.

If goal completion fails because of a connection or server error, the client clears its local completion latch so a later attempt can retry.

### 5.13 Console Commands and Logging

The client accepts both Archipelago server commands and local client commands.

- Server commands are entered in the console and sent through the Archipelago session. The server's `!help` command provides the server-side command list.
- Local commands are handled by the client and are not sent to the server.

The available local commands are:

| Command | Function |
|---|---|
| `/help` | Displays the local command list and server-command guidance |
| `/skips` | Displays the current Level Skip inventory |
| `/arcade` | Lists unlocked Arcade worlds |
| `/tarot` | Lists unlocked Tarot Cards |

The client also writes diagnostic information to its log. Debug entries include state transitions, item-delay decisions, memory writes, received-item handling, and check-submission results.

Console messages use color categories for readability. For example, received Level Skip messages use the Useful-item color, trap items use red, goal and victory messages use green, and connection or configuration messages use their corresponding status colors.

### 5.14 Polling Loop and Shared Runtime State

The client runs a recurring game-monitoring loop that reads the relevant DuckStation memory values and updates its internal state. The loop is responsible for coordinating shared systems, including:

- Game-mode and game-state detection.
- Controller-input observation.
- HUD initialization and counter synchronization.
- Received-item processing and delayed effects.
- DeathLink handling.
- Mode-specific state transitions and check detection.
- Periodic timer-memory maintenance.

The main loop performs the memory-dependent work so that network callbacks do not independently write to DuckStation memory. Network events enqueue received items or DeathLinks; the polling loop later processes them when the game is in a safe state.

The timer byte at `0x00167D62` is periodically written with decimal value `220` (`DC` in hexadecimal), approximately once every two seconds. This is intentionally not written on every polling iteration.

The client maintains previous-state values and per-session guards to avoid repeating actions while a state remains unchanged. These guards are reset or re-armed when the relevant game state changes, allowing the same event to be detected again during a later level or match.

# Credits and Disclaimer

Puzzle Bobble 4 for Archipelago is an unofficial fan-made project. It is not affiliated with, endorsed by, or sponsored by Taito or the original game's developers.

Puzzle Bobble 4 is the property of its respective rights holders.

### AI Assistance

ChatGPT (developed by OpenAI) was used as an AI-assisted development tool throughout this project. It provided assistance with programming, debugging, technical documentation, problem-solving, and development ideas.

All AI-generated suggestions and code were reviewed, tested, and adapted as part of the development process. The project design, implementation decisions, testing, and final integration remain the responsibility of the project author.

AI assistance is acknowledged as a development aid and does not imply any official affiliation, endorsement, or partnership with OpenAI.

