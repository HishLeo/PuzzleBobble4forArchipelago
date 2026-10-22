# PuzzleBobble4ArchipelagoClient

C# / .NET 8 external client for Puzzle Bobble 4 (PS1 NTSC-U). This experimental branch accesses DuckStation's emulated RAM directly on Windows x64 and does not use the GDB server.

## Startup and console behavior

- Waits for DuckStation RAM access. If DuckStation cannot be found, prints `Could not find DuckStation.` and retries every five seconds with `Retrying in 5 seconds...`.
- Once memory is accessible, prints `Connected to DuckStation.` and asks for `Enter AP domain:`, `Enter port:`, `Slot name:`, and `Room password:`. Leave the password blank for password-less rooms.
- Attempts an Archipelago login using the game name `Puzzle Bobble 4` and the `Archipelago.MultiClient.Net` library. If login fails, it shows a concise error and asks for the connection details again.
- After connecting, the console accepts Archipelago server commands. Type `!help` and press Enter to list server commands; `/help` lists the client's local command help. Console input runs on a separate thread and is not consumed by the game polling loop. Menu controls remain mapped from the game's controller-input memory byte.
- Internal memory diagnostics and local check spam are suppressed from the player's console.

## Current integration status

This build connects to Archipelago, sends a location check when the existing local detector sees a level transition, and queues received items for safe processing on the game-polling thread. The paired prototype APWorld assigns `Level Skip` at every normal level check. The client applies received `Level Skip` items to its counter/HUD. Other item effects and complete multiworld item-sending messages are not implemented yet.

## Run

1. Open DuckStation and load Puzzle Bobble 4. Leave its GDB server disabled for this experiment.
2. On the developer machine, install the .NET 8 SDK.
3. Run `build-windows-x64.ps1` to publish a self-contained Windows x64 executable, or run from this folder with `dotnet run --project .\src\PuzzleBobble4ArchipelagoClient.csproj`.
4. If direct memory access is denied, make sure the client and DuckStation run at the same privilege level; if needed, test running the client as administrator.

No menu coordinates are written at startup. Memory reads begin after finding the DuckStation process and its exported `RAM` pointer. This method is experimental and may fail on DuckStation builds that do not expose that export. A check transition that happens while DuckStation is closed or memory access is unavailable may be missed; test this behavior before relying on it for a full playthrough.

## Level letter ID mapping used by check detection

The check logger interprets the game's raw `0x00167A8F` value as:

- `00`-`13`: A-T
- `14`: U
- `15`: V
- `16`: W
- `17`: ?
- `18`: X
- `19`: Y
- `1A`: Z

Unknown values are logged as `ID-XX`. This mapping affects check names only; it does not change the selected-menu value written by keyboard navigation.

## Automatic reset to Menu0

When the client detects entry into the letter-selection screen (`0x00167A0C = 01`), it resets the logical position and camera coordinates to `Menu0`. It also does this if the client starts while the game is already on that screen. It does not continuously rewrite Menu0; it waits until it detects entry into that state.

## Automatic correction to Menu0 and manual navigation

When entering letter selection, the client watches for changes in `0x00167A8F` and can correct the first non-zero change to `00`. If a letter is selected manually using the program's arrow keys, this automatic correction is disabled so that it does not overwrite the player's selection.

## Full automatic reset to Menu0

When entering the letter-selection screen, the client watches `0x00167A8F`. If that byte changes to `00`, it applies `Menu0` through the same `ApplyMenu` routine used by Backspace, including the letter byte and camera coordinates. If another letter is selected manually with the arrow keys, automatic monitoring is disabled so that the manual selection is not overwritten.

## Letter locks and local unlocks

- For testing, all letters start unlocked (`LockedLetters` is empty).
- The locking mechanism remains implemented: exits are checked against the graph, navigation skips menus whose two exits are blocked, and the code preserves the local A1-A5 unlocks for C, D, E, Y, and Z. This allows locks to be re-enabled for future Archipelago logic without reimplementing the system.
- The neighbor table preserves the graph's exits: S has X to the left and Y to the right; T has Y to the left and Z to the right. U-Z do not have menu coordinates in this version, but their exits are still evaluated to determine whether the entry direction is blocked or allowed.
- Checks are detected and processed locally in the console; they are not yet sent to or received from an Archipelago server.

## Linear navigation filtered by available exits

The arrow keys traverse menus in linear order, but they do not stop at a letter merely because its name is not locked. For each candidate menu, the client checks its two exits in the defined graph and verifies whether either leads to a letter whose entry is unlocked. If neither exit is available, that menu is skipped and navigation continues in the pressed direction. If no other valid letter is found, navigation returns to or remains at Menu0. When the selected menu is reached, the client corrects `0x00167D3A` to face an unlocked exit if the current direction is blocked.

For testing, all letters are unlocked at startup. The logic for blocking destinations and unlocking them through A1-A5 is preserved for future Archipelago integration.

## Locking the special ? letter

The locking logic also covers entry to `?`, through Q on the right and R on the left. Currently, `?` starts unlocked for testing; the mechanism is preserved for future configuration through Archipelago.

## Level-mode plan generated by Archipelago

Modes are no longer randomized or read from local client files. The world's YAML configures `level_mode_weights`; during generation, the APWorld uses the seed's deterministic RNG to assign a mode to each of the 135 levels and sends the complete map in `slot_data` as `level_mode_plan`.

Example YAML for an initial test (all Normal Original):

```yaml
level_mode_weights:
  normal_o: 100
  expert_o: 0
  normal_aw: 0
  expert_aw: 0
```

When the client starts an Archipelago session, it reads `level_mode_plan`, validates that it contains all 135 levels and that every mode is recognized, and uses only that plan to configure DuckStation. If the plan is missing or incomplete, the client displays a warning and does not change the modes; it does not generate a local fallback plan. To change the distribution, edit the weights in the YAML and generate a new seed/multidata.

### Applying modes in DuckStation

The client applies the plan's modes using the confirmed memory addresses: `0x00167A96` (`00` = Original, `01` = Another World) and `0x00167A91` (`01` = Normal, `02` = Expert). During selection, it calculates the destination letter from the base menu and facing direction, then prepares that letter's level 1 mode. It keeps that destination and applies the mode when entering the level, rather than inferring it from `0x00167A8F`, which identifies the base menu and uses `00` for Menu0. When a completion check is detected for levels 1-4, it immediately prepares the next level's mode for the same letter. The console displays the values written and read back.

### Detecting Letter1 levels

The client detects the transition into `0x00167A10 = 05` while `0x00167A0C = 03`. It uses the destination captured while the player was in the menu (the letter the player was facing) to apply the `Letter1` assignment. This avoids confusing Menu0 (`00`) with A and fixes A1/B1 and other destinations. Letter ID decoding also follows the mapping Menu0=`00`, A=`01`, B=`02`, …, T=`14`, U=`15`, V=`16`, W=`17`, ?=`18`, X=`19`, Y=`1A`, Z=`1B`. Levels 2-5 continue to use completion-check detection.

### Displaying the focused letter in the console

While `0x00167A0C=01` (letter selection), the client prints `LETTER YOU ARE LOOKING AT` when entering the menu and whenever `0x00167A8F` changes. It also displays the letter read back after navigation initiated from the client.

### Diagnostics for the focused letter and entry into Letter1

The console reports the target letter based on the base menu and facing direction (`0x00167D3A`: `00` = left, `01` = right). It also reports every transition of `0x00167A10` to `05` and whether the level 1 mode was written and verified during the session. “Written and verified” means both mode addresses returned the expected values after writing; it does not guarantee that the game will not change them later while loading the level.

### Item weights from the Archipelago YAML (v12)

The client no longer reads or generates assignments from `item-weights.json`. The APWorld uses the YAML options to create the seed's item pool:

- `filler_weight`: relative weights for `Pointer-Guide`, `Star-Bubble`, `Metal-Bubble`, and `Level-skip`.
- `trap_weight`: the percentage of the filler pool reserved for traps.
- `trap_types`: relative weights for `Rainbow-Bubble`, `Obstacle-Block`, `Random-Color-Bubble`, and `Falling-Ceiling`.

Archipelago's RNG applies these weights during generation. When the session starts, the client receives `item_weight_config` with the effective values and `level_item_plan` with the final item assignment for all 135 locations. It saves an informational copy to `level-item-plan-received.json`; it does not randomize items again or replace Archipelago's actual item delivery.

## Level Skip mechanic (local prototype)

- The counter is stored internally as an integer from `0` to `99` and displayed as ASCII digits in `Skips 00`.
- `FILL_Level Skip` is handled by `Program.HandleReceivedItem(string itemName)`: each received item increments the counter by one, up to `99`, and updates the primary and mirrored HUD digits. This handler is prepared for the future Archipelago item-received callback; live item reception is not connected yet.
- To use a skip, the player must be in a level with `0x00167A0C = 03` and `0x00167A10 = 02`, then press R2 (`0x0016BCEB = FD`). The input is edge-triggered, so holding R2 does not consume multiple skips.
- If the counter is `00`, R2 does nothing. If a skip is available, the client writes `03` to `0x00167A10`, decrements the counter, updates `0x0011A007`-`0x0011A008`, and mirrors the digits to `0x0011A017`-`0x0011A018`.
- A skip-triggered `02 -> 03` transition is excluded from the local completed-level check detector, so using a skip does not incorrectly log a normal completion check.

## Initial Archipelago checks and Level Skip test

- Each existing local level-completion detection (`A1`, `A2`, etc.) is sent using the location ID resolved from the connected Archipelago session.
- The client prints `YOU sent A1 to Archipelago.` when it submits a check.
- The paired `puzzle_bobble_4_level_skip.apworld` uses `Level Skip` as the item in the item pool for every normal level location.
- Received `Level Skip` items are queued from the Archipelago callback and applied on the main polling loop, increasing the local skip counter/HUD.
- This is an initial test integration, not the complete item system. Confirm the check name mapping in-game and verify that each completion is marked on the server before relying on it.

## Goal-critical remote Unlock prerequisites (v7 source)

The matching Puzzle Bobble 4 world can provide `unlock_prerequisites` in slot data. When present, the client stores a received goal-world `Unlock` item as pending until all listed prerequisite Unlocks have themselves unlocked. This allows a multiplayer seed to require remote Unlock items before the goal world can be entered. Solo seeds send an empty prerequisite list and keep the usual behavior.

This source archive has not been compiled in this environment; build it on Windows with the included `build-windows-x64.ps1` and the .NET 8 SDK before replacing your existing client.

## Console commands

After a successful Archipelago login, type a server command such as `!help` and press Enter. The client forwards the line to Archipelago. `/help` is handled locally and explains how to access server commands. Console input is read on a dedicated background task; the game polling loop does not consume keyboard input.

## DeathLink

The client supports Archipelago DeathLink when the generated Puzzle Bobble 4 slot has `death_link: true` in its YAML. On a local Game Over, the client watches RAM address `0x00167A10` and sends one DeathLink when the state enters `04` while the game is in level mode (`0x00167A0C = 03`). When a DeathLink is received from another slot, the client waits until gameplay is active (`state 02`) and writes `00` to `0x00167A10` to use the game's tested reset transition. It deliberately does not write `04` for received deaths.

DeathLink is opt-in and disabled by default unless enabled in the generated YAML. Both the APWorld and this client must be updated, and a new seed must be generated so the slot data contains the option.
