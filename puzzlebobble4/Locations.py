
from BaseClasses import Location


GAME_NAME = "Puzzle Bobble 4"

# Each letter represents a world with five levels.
# The ? symbol also counts as a world.
LETTERS_BY_STEP = [
    ["A", "B"],
    ["C", "D", "E"],
    ["F", "G", "H", "I"],
    ["J", "K", "L", "M", "N"],
    ["O", "P", "Q", "R", "S", "T"],
    ["U", "V", "W", "?", "X", "Y", "Z"],
]

# Unique identifiers for the game locations.
BASE_ID = 167000


def create_level_locations():
    """Return the names and identifiers of all level locations."""
    locations = {}

    for step_letters in LETTERS_BY_STEP:
        for letter in step_letters:
            for level_number in range(1, 6):
                name = f"{letter}{level_number}"
                locations[name] = BASE_ID + len(locations)

    return locations


LEVEL_LOCATIONS = create_level_locations()


class PuzzleBobble4Location(Location):
    game = GAME_NAME
