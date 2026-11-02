Puzzle Bobble 4 — PopTracker v1.0.0

This version restores the documented PopTracker location schema ("sections") so the map squares and popup sections can render.
Markers were repositioned lower than the letter glyphs based on the screenshot.

Archipelago:
- The pack uses the exact location IDs from the supplied .apworld (167000 + index, five levels per letter).
- It registers Clear, Location, and Item handlers, and imports existing CheckedLocations when the connection is established.
- Unlock items are tracked using their actual IDs (200020 onward, in the .apworld's Unlock-letter order).

Install:
1. Remove/replace the old Puzzle Bobble 4 pack so PopTracker does not load an older copy.
2. Drag this ZIP into PopTracker.
3. Load the pack, click AP, and connect to the same Archipelago session.

If map markers still do not render, send the PopTracker log after loading this version; the schema has been restored to the version that rendered squares in v0.1.0.
The custom black-node-after-six-completions behavior is not included yet.

v0.4.0 change:
- Added parent-level access_rules to every world C–Z and ? so the map marker is red until its corresponding Unlock item is received.
- A and B have no access rule and remain green/accessible from the start.
- This relies on the Unlock toggle item becoming active when the matching AP item ID is received.


Version 0.3.5: adds all 208 VS/Story Complete checks (16 characters x 13 enemies), mapped to the supplied matrix background and APWorld location IDs.


Version 0.3.12:
- Added the third map tab: VS/Contest complete.
- Added 16 character portrait markers in the requested order.
- Each marker contains Win Contest 1..N sections, with N read from AP slot data (Custom Contest Wins).
- Markers are green only for selected unlocked characters; locked or unselected characters remain red.
- Added AP tracking for VS/Contest Complete checks and character unlock items.


Version 1.0.0 marker visibility fix:
- Removed per-section visibility filters that could hide the parent map marker when the slot option was not read.
- Kept all Win Contest sections visible in each character popup.
- Simplified map placement fields to the same format used by the working VS/Story map.
- Reduced marker size slightly to keep the square aligned over each portrait.


Version 0.3.19
- Puzzle/Arcade map visibility now follows seed slot_data: Simple shows A/B plus the five selected letters; Complete shows all 27 worlds; Disabled hides all Arcade worlds.
- Puzzle/Story map visibility now follows seed slot_data: Simple shows Fool plus the five selected Tarot cards; Complete shows all 22 cards; Disabled hides all Tarot cards.
- Visibility flags are internal and are not displayed in the tracker layout. Existing AP location IDs and checks are unchanged.

Version 0.3.20
- VS/Story Simple and VS/Story Complete locations follow the selected mode in slot_data.
- The VS/Contest Simple square is independently controlled by the VS/Contest mode.
- VS/Contest Complete locations follow the selected mode in slot_data.
- Contest sections are limited to the seed's Custom Contest Wins value in both Simple and Complete maps.
- Existing Archipelago location names and IDs are unchanged.

Version 0.3.22
- VS/Contest Simple square remains red until the Unlock VS/Contest item is received, then becomes available/green.
- No Archipelago checks, items, or location IDs were added or changed.


Version 1.0.0:
- Package version standardized to 1.0.0.
- Includes the final map artwork and marker positions supplied for VS/Story Complete and VS/Contest Complete.
