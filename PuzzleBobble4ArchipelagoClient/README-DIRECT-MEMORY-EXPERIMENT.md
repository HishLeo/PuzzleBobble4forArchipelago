# Experiment: Direct DuckStation RAM Access (Without GDB)

This is an experimental version based on the current client. Instead of connecting to GDB port 2345, it locates the DuckStation process, finds the PE export named `RAM`, and uses `ReadProcessMemory` / `WriteProcessMemory` to access emulated RAM.

## Testing on Windows

1. Close DuckStation and any previous instance of the client.
2. Build the project with the .NET 8 SDK installed by running `build-windows-x64.ps1`.
3. Open DuckStation and load Puzzle Bobble 4. Leave the GDB server disabled.
4. Run `publish\\PuzzleBobble4ArchipelagoClient.exe`.
5. Confirm that the console displays `DuckStation RAM located...` and reads the game state without errors.
6. Test navigation, mode changes, the HUD, and the timer. If an `OpenProcess` error appears, try running the client with the same privilege level as DuckStation. If the error persists, save the error message.

## Important limitations

- This experimental implementation requires Windows x64.
- It depends on the DuckStation executable exporting a PE symbol named `RAM`, as observed in the MediEvil client library. If your build does not expose this symbol, detection will fail and RAM discovery will need to be adapted.
- This version could not be compiled or run in the original development environment because the .NET SDK was not installed and internet access was unavailable to install it. The publish script can build it on a Windows machine with the .NET 8 SDK.
- This project retains the base version's functional scope. Real connection to an Archipelago server and sending/receiving items are not implemented by this memory-access change; the application messages indicate which parts remain prototypes.
- Keep the previous ZIP as a backup. This version has not yet been tested with a real DuckStation instance.

## Recent changes

- Bytes `0x0011A000` and `0x0011A010` are initialized to `0x20` (ASCII space).
- When using a Level Skip, the client prepares the mode assigned to the next level of the same letter before writing `0x03` to `0x00167A10`. This avoids depending on the polling loop observing the `0x02 -> 0x03` transition while DuckStation is still running.
