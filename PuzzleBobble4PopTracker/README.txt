Puzzle Bobble 4 — PopTracker v0.3.0

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
