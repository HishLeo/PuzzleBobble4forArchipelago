
from BaseClasses import Location


GAME_NAME = "Puzzle Bobble 4"

# Cada letra representa una etapa con cinco niveles.
# El signo ? también cuenta como una etapa.
LETTERS_BY_STEP = [
    ["A", "B"],
    ["C", "D", "E"],
    ["F", "G", "H", "I"],
    ["J", "K", "L", "M", "N"],
    ["O", "P", "Q", "R", "S", "T"],
    ["U", "V", "W", "?", "X", "Y", "Z"],
]

# Identificadores únicos para las ubicaciones del juego.
BASE_ID = 167000


def create_level_locations():
    """Devuelve los nombres e identificadores de todos los niveles."""
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
