# Puzzle Bobble 4 for Archipelago

Play *Puzzle Bobble 4* in an [Archipelago](https://archipelago.gg/) multiworld!

<img width="518" height="546" alt="PB4AP" src="https://github.com/user-attachments/assets/a902068f-2720-4405-a296-4a527c09b4c9" />

## Requirements

- **Windows**.
- [Latest Archipelago release](https://github.com/ArchipelagoMW/Archipelago/releases/latest).
- [Latest Puzzle Bobble 4 client](https://github.com/HishLeo/PuzzleBobble4forArchipelago/releases/latest).
- [Puzzle Bobble 4 APWorld](https://github.com/HishLeo/PuzzleBobble4forArchipelago/releases/latest).
- [Puzzle Bobble 4 YAML file](https://github.com/HishLeo/PuzzleBobble4forArchipelago/releases/latest).
- [Latest DuckStation version](https://github.com/stenzek/duckstation/releases/latest).
- A **legally obtained NTSC-U/C copy** of *Puzzle Bobble 4* (serial: **SLUS-00754**).

## Setup

1. Install the APWorld by double-clicking the `.apworld` file or copying it into your Archipelago installation's `custom_worlds` folder.
2. Edit the YAML file to configure your preferred options.
3. Copy the YAML file into the `Players` folder in your Archipelago installation.
4. Generate the game using Archipelago.
5. Host the game by uploading the generated ZIP from your Archipelago `output` folder to the [Archipelago hosting website](https://archipelago.gg/uploads).
6. Open DuckStation and load *Puzzle Bobble 4*.
7. In the game, navigate to **Game Menu**, immediately after the **Push Start Button** screen, and wait there.
8. Launch the Puzzle Bobble 4 client and enter the connection details provided by your Archipelago room.
9. Your goal will be displayed in the client.
10. Have fun!

## Quality-of-Life Features

The client adds the following general features:

- All characters are unlocked in Puzzle modes.
- **R2** can be used to activate a Level Skip while playing.
- Added informative on-screen text in certain game modes.

### Puzzle/Arcade

- Change your character in World Selection using **Up / Down**.
- Switch between unlocked worlds using **L1 / R1**.
- Press **R2** to return to the beginning of the world selection.
- Press **L2** to return to the main menu. When playing a level, **L2** takes you back to World Selection.

### Puzzle/Story

- Switch between unlocked Tarot Cards using **L1 / R1**.
- **Note:** Using the regular Left / Right controls may take you back to the previous card. Sorry!
- Press **L2** to return to the main menu. When playing a level, **L2** takes you back to Card Selection.

### VS/Story

- Switch between unlocked enemies using **L1 / R1** or **Left / Right**.
- Change your character using **Up / Down**.
- **Note:** Your character's sprite does not change on the enemy selection screen. Check the on-screen text to see which character you are playing as.
- Press **L2** to return to the main menu. When playing a level, **L2** takes you back to Enemy Selection.

### VS/Contest

- Updated the on-screen text to display your consecutive wins instead of the total round number.

## PopTracker

You can also use [PopTracker](https://github.com/black-sliver/PopTracker) to track your progress.

1. Download and install PopTracker.
2. Download the Puzzle Bobble 4 tracker ZIP from the [latest release](https://github.com/HishLeo/PuzzleBobble4forArchipelago/releases/latest).
3. Place the ZIP in PopTracker's `packs` folder, or drag and drop it into the PopTracker window.
4. Open PopTracker and load the Puzzle Bobble 4 pack.
5. Click **AP** and enter the required information to connect to your Archipelago session.

## FAQ

### The game has too many loading screens. Can I speed them up?

In DuckStation, go to **Settings → Console → CD-ROM Emulation** and set **Read Speedup** and **Seek Speedup** to **Maximum**. This will reduce loading times, and the client will continue to work normally.

If you experience crashes, lower one of these settings and try again.

### I'm stuck in a game mode. How do I get back to the main menu?

You may have already completed all currently available content in that mode. The original game does not let you return from certain game modes, but the client adds a way to do so: press **L2** to return to the main menu.

### I found a Level Skip, but I didn't receive the item. Why?

The client checks whether the Level Skip location has already been collected in the current seed. If you create multiple rooms using the same seed, that location may already be marked as checked, so the item will not be sent again.

If possible, generate a new room with a different seed.

## Dependencies and acknowledgments

This project uses [Archipelago.MultiClient.Net](https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net), a .NET client library for communicating with Archipelago servers, distributed under the MIT License.
