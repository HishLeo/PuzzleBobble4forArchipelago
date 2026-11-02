from BaseClasses import Location

GAME_NAME = "Puzzle Bobble 4"

# Each letter represents a Puzzle/Arcade world with five levels.
LETTERS_BY_STEP = [
    ["A", "B"],
    ["C", "D", "E"],
    ["F", "G", "H", "I"],
    ["J", "K", "L", "M", "N"],
    ["O", "P", "Q", "R", "S", "T"],
    ["U", "V", "W", "?", "X", "Y", "Z"],
]

# Keep these names and this order in sync with the DuckStation client.
TAROT_CARDS = [
    "Fool", "Star", "Moon", "Hermit", "Tower", "Chariot", "Death", "Devil",
    "Hanged Man", "Magician", "Wheel of Fortune", "Judgement", "Lovers",
    "Temperance", "Hierophant", "Emperor", "Empress", "High Priestess",
    "Sun", "Justice", "Strength", "World",
]

BASE_ID = 167000
TAROT_BASE_ID = 167200


def create_level_locations():
    locations = {}
    for step_letters in LETTERS_BY_STEP:
        for letter in step_letters:
            for level_number in range(1, 6):
                name = f"{letter}{level_number}"
                locations[name] = BASE_ID + len(locations)
    return locations


def create_tarot_locations():
    return {
        f"{card} {level}": TAROT_BASE_ID + card_index * 5 + level - 1
        for card_index, card in enumerate(TAROT_CARDS)
        for level in range(1, 6)
    }


LEVEL_LOCATIONS = create_level_locations()
TAROT_LEVEL_LOCATIONS = create_tarot_locations()

VS_STORY_CHARACTERS = [
    "Bob", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Madam Luna", "Dreg", "Woolen", "Packy", "Monsta", "Maita",
]
VS_STORY_ENEMIES = [
    "Bob", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Secret", "Madam Luna", "Dreg",
]
VS_STORY_LOCATION_BASE_ID = 167400
VS_CONTEST_LOCATION_BASE_ID = 168000


def vs_story_simple_location_name(enemy):
    return f"vs {enemy} win"


def vs_story_complete_location_name(enemy, character):
    # Player ID 00 is named Bub when playable; Bob remains the enemy name.
    player_name = "Bub" if character == "Bob" else character
    return f"Defeat {enemy} as {player_name}"


def create_vs_story_simple_locations():
    return {
        vs_story_simple_location_name(enemy): VS_STORY_LOCATION_BASE_ID + index
        for index, enemy in enumerate(VS_STORY_ENEMIES)
    }


def create_vs_story_complete_locations():
    base = VS_STORY_LOCATION_BASE_ID + len(VS_STORY_ENEMIES)
    return {
        vs_story_complete_location_name(enemy, character): base + character_index * len(VS_STORY_ENEMIES) + enemy_index
        for character_index, character in enumerate(VS_STORY_CHARACTERS)
        for enemy_index, enemy in enumerate(VS_STORY_ENEMIES)
    }


VS_STORY_SIMPLE_LOCATIONS = create_vs_story_simple_locations()
VS_STORY_COMPLETE_LOCATIONS = create_vs_story_complete_locations()


class PuzzleBobble4Location(Location):
    game = GAME_NAME


# VS/Contest milestone checks. These names are intentionally distinct between
# the mutually exclusive Simple and Complete modes.
def vs_contest_simple_location_name(number):
    return f"Contest Win {number}"


def vs_contest_complete_location_name(number, character):
    return f"Contest Win {number} as {character}"


def create_vs_contest_simple_locations(max_wins=30):
    return {vs_contest_simple_location_name(number): VS_CONTEST_LOCATION_BASE_ID + number - 1
            for number in range(1, max_wins + 1)}


def create_vs_contest_complete_locations(max_wins=30):
    base = VS_CONTEST_LOCATION_BASE_ID + max_wins
    return {vs_contest_complete_location_name(number, character):
            base + character_index * max_wins + number - 1
            for character_index, character in enumerate(VS_STORY_CHARACTERS)
            for number in range(1, max_wins + 1)}


VS_CONTEST_SIMPLE_LOCATIONS = create_vs_contest_simple_locations()
VS_CONTEST_COMPLETE_LOCATIONS = create_vs_contest_complete_locations()
