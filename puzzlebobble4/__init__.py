from dataclasses import dataclass

from BaseClasses import Region, ItemClassification
from Options import PerGameCommonOptions
from worlds.AutoWorld import World, WebWorld
from BaseClasses import Tutorial
from .Items import GAME_NAME, PuzzleBobble4Item
from .Locations import (LEVEL_LOCATIONS, TAROT_LEVEL_LOCATIONS, TAROT_CARDS, PuzzleBobble4Location,
                        VS_STORY_SIMPLE_LOCATIONS, VS_STORY_COMPLETE_LOCATIONS,
                        vs_story_simple_location_name, vs_story_complete_location_name,
                        VS_CONTEST_SIMPLE_LOCATIONS, VS_CONTEST_COMPLETE_LOCATIONS,
                        vs_contest_simple_location_name, vs_contest_complete_location_name)
from .Rules import LETTERS_BY_STEP
from .Options import (
    Goal, PuzzleArcadeMode, PuzzleStoryMode, VsStoryMode, VsContestMode,
    LevelModeWeights, TarotDifficultyWeights,
    ExtraEnemy, VsStoryCharacters,
    CustomContestWins, ContestCharacters, IncreaseDifficultyContest, ExtraLevelSkips, BuffTamtam, FixPacky,
    FillerWeight, TrapWeight, TrapTypes, PuzzleBobble4DeathLink,
)


GOAL_LETTERS = ["U", "V", "W", "?", "X", "Y", "Z"]
ALL_LETTERS = [letter for step in LETTERS_BY_STEP for letter in step]
UNLOCK_LETTERS = [letter for letter in ALL_LETTERS if letter not in ("A", "B")]
TAROT_UNLOCK_CARDS = [card for card in TAROT_CARDS if card != "Fool"]
TAROT_SIMPLE_GROUPS = [
    ["Chariot", "Magician", "Lovers"],
    ["Hierophant", "Emperor", "Empress", "High Priestess"],
    ["Wheel of Fortune", "Judgement", "Temperance", "Justice", "Strength"],
    ["Hermit", "Tower", "Death", "Devil", "Hanged Man"],
    ["Star", "Moon", "Sun", "World"],
]

VS_STORY_CHARACTERS = [
    "Bob", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Madam Luna", "Dreg", "Woolen", "Packy", "Monsta", "Maita",
]
VS_STORY_BASE_ENEMIES = [
    "Bob", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Madam Luna", "Dreg",
]
VS_STORY_ENEMIES_WITH_SECRET = [
    "Bob", "Alkanet", "Marino", "Kurol", "Tamtam", "Cleon", "G", "Bramb",
    "Develon", "Gigant", "Secret", "Madam Luna", "Dreg",
]


def vs_story_simple_location_name(enemy):
    return f"vs {enemy} win"


def vs_story_complete_location_name(enemy, character):
    # Player ID 00 is named Bub when playable; Bob remains the enemy name.
    player_name = "Bub" if character == "Bob" else character
    return f"Defeat {enemy} as {player_name}"


def vs_story_character_unlock_item_name(character):
    return f"Unlock VS/Story Character {character}"


def vs_contest_character_unlock_item_name(character):
    return f"Unlock VS/Contest Character {character}"



FILLER_ITEM_NAMES = ["Pointer Guide", "Star Bubble", "Metal Bubble"]
TRAP_ITEM_NAMES = ["Rainbow Bubble", "Obstacle Block", "Random Color Bubble", "Falling Ceiling"]

# Preserve the existing Level Skip and victory item IDs. New items use unused IDs.
ITEM_IDS = {
    "Level Skip": 200001,
    "Pointer Guide": 200011,
    "Star Bubble": 200012,
    "Metal Bubble": 200013,
    "Rainbow Bubble": 200014,
    "Obstacle Block": 200015,
    "Random Color Bubble": 200016,
    "Falling Ceiling": 200017,
    "Progressive Enemy": 200220,
}
VS_STORY_CHARACTER_ITEM_IDS = {
    vs_story_character_unlock_item_name(character): 200200 + index
    for index, character in enumerate(VS_STORY_CHARACTERS)
}
VS_CONTEST_CHARACTER_ITEM_IDS = {
    vs_contest_character_unlock_item_name(character): 200230 + index
    for index, character in enumerate(VS_STORY_CHARACTERS)
}


def victory_item_name(letter):
    return f"Victory - Complete World {letter}"


def unlock_item_name(letter):
    return f"Unlock World {letter}"


def tarot_unlock_item_name(card):
    return f"Unlock Tarot {card}"


def tarot_victory_item_name(card):
    return f"Victory - Complete Tarot {card}"


def tarot_world_victory_item_name():
    return "Victory - Complete Tarot World"


@dataclass
class PuzzleBobble4Options(PerGameCommonOptions):
    # Keep these fields in the same order as the requested YAML sections.
    puzzle_arcade_mode: PuzzleArcadeMode
    puzzle_story_mode: PuzzleStoryMode
    vs_story_mode: VsStoryMode
    vs_contest_mode: VsContestMode
    goal: Goal
    level_mode_weights: LevelModeWeights
    tarot_difficulty_weights: TarotDifficultyWeights
    vs_story_characters: VsStoryCharacters
    extra_enemy: ExtraEnemy
    Custom_contest_wins: CustomContestWins
    contest_characters: ContestCharacters
    increase_difficulty_contest: IncreaseDifficultyContest
    filler_weight: FillerWeight
    trap_weight: TrapWeight
    trap_types: TrapTypes
    Extra_level_skips: ExtraLevelSkips
    buff_tamtam: BuffTamtam
    fix_packy: FixPacky
    death_link: PuzzleBobble4DeathLink


class PuzzleBobble4Web(WebWorld):
    """Webhost configuration for Puzzle Bobble 4."""
    tutorials = [
        Tutorial(
            "Setup Guide",
            "How to install and connect the Puzzle Bobble 4 client.",
            "English",
            "setup_en.md",
            "setup/en",
            ["Hish"],
        ),
    ]


class PuzzleBobble4World(World):
    game = GAME_NAME
    web = PuzzleBobble4Web()
    topology_present = True
    options_dataclass = PuzzleBobble4Options
    options: PuzzleBobble4Options

    item_name_to_id = {
        **ITEM_IDS,
        **{victory_item_name(letter): 200004 + index for index, letter in enumerate(GOAL_LETTERS)},
        **{unlock_item_name(letter): 200020 + index for index, letter in enumerate(UNLOCK_LETTERS)},
    }

    location_name_to_id = {**LEVEL_LOCATIONS, **TAROT_LEVEL_LOCATIONS,
                           **VS_STORY_SIMPLE_LOCATIONS, **VS_STORY_COMPLETE_LOCATIONS,
                           **VS_CONTEST_SIMPLE_LOCATIONS, **VS_CONTEST_COMPLETE_LOCATIONS}

    item_name_to_id = {
        **item_name_to_id,
        **{tarot_unlock_item_name(card): 200100 + index for index, card in enumerate(TAROT_UNLOCK_CARDS)},
        **{tarot_victory_item_name(card): 200130 + index for index, card in enumerate(TAROT_CARDS)},
        tarot_world_victory_item_name(): 200152,
        "Progressive Enemy": 200220,
        "Unlock VS/Story": 200246,
        "Unlock VS/Contest": 200247,
        **VS_STORY_CHARACTER_ITEM_IDS,
        **VS_CONTEST_CHARACTER_ITEM_IDS,
    }

    def generate_early(self):
        # Resolve each mode from its configured weights using Archipelago's seeded RNG.
        def choose_mode(option, option_name):
            weights = dict(option.value or {})
            keys = ("simple", "complete", "disable")
            normalized = {key: int(weights.get(key, 0)) for key in keys}
            if any(weight < 0 for weight in normalized.values()):
                raise ValueError(f"{option_name} weights cannot be negative.")
            if sum(normalized.values()) <= 0:
                raise ValueError(f"At least one {option_name} weight must be greater than zero.")
            return self.random.choices(
                list(keys), weights=[normalized[key] for key in keys], k=1
            )[0]

        self.puzzle_arcade_selected_mode = choose_mode(self.options.puzzle_arcade_mode, "Puzzle/Arcade Mode")
        self.puzzle_story_selected_mode = choose_mode(self.options.puzzle_story_mode, "Puzzle/Story Mode")
        self.vs_story_selected_mode = choose_mode(self.options.vs_story_mode, "VS/Story Mode")
        self.vs_contest_selected_mode = choose_mode(self.options.vs_contest_mode, "VS/Contest Mode")

        # A selected Goal takes priority over its mode's weighted preference.
        # The effective mode is forced to Complete when the Goal requires it.
        goal_mode_overrides = {
            "one_u_z": ("puzzle_arcade",),
            "all_u_z": ("puzzle_arcade",),
            "only_world": ("puzzle_story",),
            "all_tarot": ("puzzle_story",),
            "vs_story": ("vs_story",),
            "vs_contest": ("vs_contest",),
        }
        # Use the selected Choice key explicitly instead of relying on Choice/int
        # equality when resolving the Goal's required game mode.
        selected_goal_key = self.options.goal.current_key
        required_mode = goal_mode_overrides.get(selected_goal_key)
        if required_mode:
            mode_name = required_mode[0]
            setattr(self, f"{mode_name}_selected_mode", "complete")

        # Internal booleans preserve the existing generation logic and slot_data contract.
        self.puzzle_arcade_complete = self.puzzle_arcade_selected_mode == "complete"
        self.puzzle_arcade_simple = self.puzzle_arcade_selected_mode == "simple"
        self.puzzle_story_complete = self.puzzle_story_selected_mode == "complete"
        self.puzzle_story_simple = self.puzzle_story_selected_mode == "simple"
        self.vs_story_simple = self.vs_story_selected_mode == "simple"
        self.vs_story_complete = self.vs_story_selected_mode == "complete"
        self.vs_contest_simple = self.vs_contest_selected_mode == "simple"
        self.vs_contest_complete = self.vs_contest_selected_mode == "complete"

        # Reject invalid mode combinations before generation.
        if self.puzzle_arcade_complete and self.puzzle_arcade_simple:
            raise ValueError("Enable only one of puzzle_arcade (Complete) and puzzle_arcade_simple.")
        if self.puzzle_story_complete and self.puzzle_story_simple:
            raise ValueError("Enable only one of puzzle_story (Complete) and puzzle_story_simple.")
        if self.vs_story_simple and self.vs_story_complete:
            raise ValueError("Enable only one of vs_story_simple and vs_story_complete.")
        if self.vs_contest_simple and self.vs_contest_complete:
            raise ValueError("Enable only one of vs_contest_simple and vs_contest_complete.")
        if not any((
            self.puzzle_arcade_complete,
            self.puzzle_arcade_simple,
            self.puzzle_story_complete,
            self.puzzle_story_simple,
            self.vs_story_simple,
            self.vs_story_complete,
            self.vs_contest_simple,
            self.vs_contest_complete,
        )):
            raise ValueError("At least one game mode must be enabled.")

        # Every seed must have a real victory condition. The selected Goal
        # takes priority over weighted mode preferences and has already forced
        # its required mode to Complete above. Simple modes may coexist with
        # that goal-providing mode, but cannot neutralize the global Goal.
        self.effective_goal = self.options.goal

        # Validate that the selected goal belongs to an enabled mode.
        goal_mode_requirements = {
            Goal.option_one_u_z: (self.puzzle_arcade_complete, "puzzle_arcade"),
            Goal.option_all_u_z: (self.puzzle_arcade_complete, "puzzle_arcade"),
            Goal.option_only_world: (self.puzzle_story_complete, "puzzle_story"),
            Goal.option_all_tarot: (self.puzzle_story_complete, "puzzle_story"),
            Goal.option_vs_story: (self.vs_story_complete, "vs_story_complete"),
            Goal.option_vs_contest: (self.vs_contest_complete, "vs_contest_complete"),
        }
        enabled, required_mode = goal_mode_requirements[self.effective_goal]
        if not enabled:
            raise ValueError(f"Goal '{self.effective_goal.current_key}' requires {required_mode} = 1.")
        if self.effective_goal == Goal.option_vs_story and not self.vs_story_complete:
            raise ValueError("Goal 'vs_story' requires vs_story_complete = 1.")
        if self.effective_goal == Goal.option_vs_contest and not self.vs_contest_complete:
            raise ValueError("Goal 'vs_contest' requires vs_contest_complete = 1.")

        self.is_arcade_goal = self.effective_goal in (Goal.option_one_u_z, Goal.option_all_u_z)
        self.is_tarot_goal = self.effective_goal in (Goal.option_only_world, Goal.option_all_tarot)
        self.is_vs_story_goal = self.effective_goal == Goal.option_vs_story

        # Arcade Simple selects exactly one letter from each progression group.
        self.puzzle_arcade_simple_letters = []
        if self.puzzle_arcade_simple:
            simple_groups = [step for step in LETTERS_BY_STEP[1:]]
            self.puzzle_arcade_simple_letters = [self.random.choice(group) for group in simple_groups]
        self.arcade_active_letters = (
            list(ALL_LETTERS) if self.puzzle_arcade_complete else
            (["A", "B"] + list(self.puzzle_arcade_simple_letters)
             if self.puzzle_arcade_simple else [])
        )
        self.puzzle_story_simple_cards = []
        if self.puzzle_story_simple:
            self.puzzle_story_simple_cards = [self.random.choice(group) for group in TAROT_SIMPLE_GROUPS]
        self.tarot_active_cards = (
            list(TAROT_CARDS) if self.puzzle_story_complete else
            (["Fool"] + list(self.puzzle_story_simple_cards)
             if self.puzzle_story_simple else [])
        )
        self.vs_story_selected_characters = []
        self.vs_story_start_character = None
        self.vs_story_enemies = []
        self.vs_story_progressive_enemies = []
        self.vs_story_fixed_start_location = None
        self.contest_selected_characters = []
        self.contest_start_character = None
        self.contest_wins = int(self.options.Custom_contest_wins.value)
        if self.vs_contest_complete:
            count = int(self.options.contest_characters.value)
            if count < 3 or count > len(VS_STORY_CHARACTERS):
                raise ValueError("contest_characters must be between 3 and 16.")
            self.contest_selected_characters = self.random.sample(VS_STORY_CHARACTERS, count)
            self.contest_start_character = self.random.choice(self.contest_selected_characters)
        if self.vs_story_complete:
            count = int(self.options.vs_story_characters.value)
            if count < 3 or count > len(VS_STORY_CHARACTERS):
                raise ValueError("vs_story_characters must be between 3 and 16.")
            self.vs_story_selected_characters = self.random.sample(VS_STORY_CHARACTERS, count)
            self.vs_story_start_character = self.random.choice(self.vs_story_selected_characters)
            self.vs_story_enemies = (
                list(VS_STORY_ENEMIES_WITH_SECRET) if self.options.extra_enemy.value
                else list(VS_STORY_BASE_ENEMIES)
            )
            self.vs_story_progressive_enemies = [
                enemy for enemy in self.vs_story_enemies if enemy not in ("Bob", "Dreg")
            ]
            self.vs_story_fixed_start_location = vs_story_complete_location_name(
                "Bob", self.vs_story_start_character
            )
        elif self.vs_story_simple:
            self.vs_story_enemies = (
                list(VS_STORY_ENEMIES_WITH_SECRET) if self.options.extra_enemy.value
                else list(VS_STORY_BASE_ENEMIES)
            )

        # Arcade goal worlds are only victory targets for Arcade goals. When a Tarot
        # goal is selected but Arcade is also enabled, retain a separate route target
        # so the existing Arcade Unlock progression still has a guaranteed local path.
        if self.is_arcade_goal:
            if self.effective_goal == Goal.option_one_u_z:
                self.goal_letters = [self.random.choice(GOAL_LETTERS)]
            else:
                self.goal_letters = list(GOAL_LETTERS)
            self.arcade_route_letters = list(self.goal_letters)
        else:
            self.goal_letters = []
            self.arcade_route_letters = [self.random.choice(GOAL_LETTERS)] if self.puzzle_arcade_complete else []

        self.remote_unlock_letters = []
        self.remote_goal_letter = None
        self.remote_prerequisite_letters = []
        # Preserve the existing multiworld route behavior for actual Arcade goals.
        if self.is_arcade_goal and self.multiworld.players > 1:
            self.remote_goal_letter = self.random.choice(self.goal_letters)
            eligible = [letter for letter in UNLOCK_LETTERS if letter not in self.goal_letters]
            if not eligible:
                raise ValueError("No non-goal Unlock is available to serve as the additional prerequisite.")
            prerequisite = self.random.choice(eligible)
            self.remote_unlock_letters = [self.remote_goal_letter, prerequisite]
            self.remote_prerequisite_letters = [prerequisite]

        # Generate one weighted mode per Arcade level using Archipelago's seeded RNG.
        self.level_mode_plan = {}
        if self.puzzle_arcade_complete or self.puzzle_arcade_simple:
            mode_names = ["NormalO", "ExpertO", "NormalAW", "ExpertAW"]
            level_weights = self.options.level_mode_weights.value
            weights = [
                int(level_weights.get("normal_o", 0)),
                int(level_weights.get("expert_o", 0)),
                int(level_weights.get("normal_aw", 0)),
                int(level_weights.get("expert_aw", 0)),
            ]
            if any(weight < 0 for weight in weights):
                raise ValueError("Level Mode Weights cannot be negative.")
            if sum(weights) <= 0:
                raise ValueError("At least one Level Mode Weight must be greater than zero.")
            level_ids = (
                list(LEVEL_LOCATIONS) if self.puzzle_arcade_complete
                else [f"{letter}{level}" for letter in self.arcade_active_letters for level in range(1, 6)]
            )
            self.level_mode_plan = {
                level_id: self.random.choices(mode_names, weights=weights, k=1)[0]
                for level_id in level_ids
            }

        # Generate the Tarot difficulty plan on the server, so every client in the
        # multiworld uses the same seed-specific Normal/Expert assignment.
        self.tarot_difficulty_plan = {}
        if self.puzzle_story_complete or self.puzzle_story_simple:
            tarot_weights = self.options.tarot_difficulty_weights.value
            weights = [int(tarot_weights.get("normal", 0)), int(tarot_weights.get("expert", 0))]
            if any(weight < 0 for weight in weights):
                raise ValueError("Tarot Difficulty Weights cannot be negative.")
            if sum(weights) <= 0:
                raise ValueError("At least one Tarot Difficulty Weight must be greater than zero.")
            self.tarot_difficulty_plan = {
                f"{card} {level}": self.random.choices(["Normal", "Expert"], weights=weights, k=1)[0]
                for card in self.tarot_active_cards for level in range(1, 6)
            }

    def create_regions(self):
        menu = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu)

        level_regions = {}
        level_locations = {}
        tarot_regions = {}
        tarot_locations = {}

        if self.puzzle_arcade_complete or self.puzzle_arcade_simple:
            for letter in self.arcade_active_letters:
                for number in range(1, 6):
                    level_name = f"{letter}{number}"
                    region = Region(level_name, self.player, self.multiworld)
                    location = PuzzleBobble4Location(self.player, level_name, LEVEL_LOCATIONS[level_name], region)
                    region.locations.append(location)
                    self.multiworld.regions.append(region)
                    level_regions[level_name] = region
                    level_locations[level_name] = location

            for letter in self.arcade_active_letters:
                first_level = f"{letter}1"
                menu.connect(level_regions[first_level], f"Select {first_level}")
                for number in range(1, 5):
                    current_level = f"{letter}{number}"
                    next_level = f"{letter}{number + 1}"
                    level_regions[current_level].connect(level_regions[next_level], f"Advance to {next_level}")

            active_unlock_letters = (
                list(UNLOCK_LETTERS) if self.puzzle_arcade_complete
                else list(self.puzzle_arcade_simple_letters)
            )
            for letter in active_unlock_letters:
                required_item = unlock_item_name(letter)
                prerequisites = list(self.remote_prerequisite_letters) if letter in self.goal_letters else []
                first_level_region = level_regions[f"{letter}1"]
                for entrance in first_level_region.entrances:
                    entrance.access_rule = (
                        lambda state, item_name=required_item, prereq_letters=prerequisites:
                        state.has(item_name, self.player) and all(
                            state.has(unlock_item_name(prereq), self.player) for prereq in prereq_letters
                        )
                    )

        if self.puzzle_story_complete or self.puzzle_story_simple:
            for card in self.tarot_active_cards:
                for number in range(1, 6):
                    location_name = f"{card} {number}"
                    region = Region(location_name, self.player, self.multiworld)
                    location = PuzzleBobble4Location(
                        self.player, location_name, TAROT_LEVEL_LOCATIONS[location_name], region
                    )
                    region.locations.append(location)
                    self.multiworld.regions.append(region)
                    tarot_regions[location_name] = region
                    tarot_locations[location_name] = location

            for card in self.tarot_active_cards:
                first_name = f"{card} 1"
                menu.connect(tarot_regions[first_name], f"Select {first_name}")
                for number in range(1, 5):
                    current_name = f"{card} {number}"
                    next_name = f"{card} {number + 1}"
                    tarot_regions[current_name].connect(tarot_regions[next_name], f"Advance to {next_name}")
                if card != "Fool":
                    required_item = tarot_unlock_item_name(card)
                    for entrance in tarot_regions[first_name].entrances:
                        entrance.access_rule = lambda state, item_name=required_item: state.has(item_name, self.player)

        vs_story_locations = {}
        if self.vs_story_simple:
            for enemy in self.vs_story_enemies:
                location_name = vs_story_simple_location_name(enemy)
                region = Region(location_name, self.player, self.multiworld)
                location = PuzzleBobble4Location(
                    self.player, location_name, VS_STORY_SIMPLE_LOCATIONS[location_name], region
                )
                region.locations.append(location)
                self.multiworld.regions.append(region)
                entrance = menu.connect(region, f"VS/Story Simple: {location_name}")
                # The global unlock item must be found in another active mode;
                # VS/Story Simple checks remain inaccessible until all characters unlock.
                entrance.access_rule = lambda state: state.has("Unlock VS/Story", self.player)
                vs_story_locations[location_name] = location

        if self.vs_story_complete:
            for character in self.vs_story_selected_characters:
                for enemy in self.vs_story_enemies:
                    location_name = vs_story_complete_location_name(enemy, character)
                    region = Region(location_name, self.player, self.multiworld)
                    location = PuzzleBobble4Location(
                        self.player, location_name, VS_STORY_COMPLETE_LOCATIONS[location_name], region
                    )
                    region.locations.append(location)
                    self.multiworld.regions.append(region)
                    entrance = menu.connect(region, f"VS/Story Complete: {location_name}")
                    if character == self.vs_story_start_character:
                        character_rule = lambda state: True
                    else:
                        character_item = vs_story_character_unlock_item_name(character)
                        character_rule = lambda state, item_name=character_item: state.has(item_name, self.player)
                    enemy_index = (
                        self.vs_story_progressive_enemies.index(enemy) + 1
                        if enemy in self.vs_story_progressive_enemies
                        else len(self.vs_story_progressive_enemies) if enemy == "Dreg"
                        else 0
                    )
                    entrance.access_rule = lambda state, cr=character_rule, n=enemy_index: (
                        cr(state) and (n == 0 or state.has("Progressive Enemy", self.player, n))
                    )
                    vs_story_locations[location_name] = location

        vs_contest_locations = {}
        if self.vs_contest_simple:
            for number in range(1, self.contest_wins + 1):
                name = vs_contest_simple_location_name(number)
                region = Region(name, self.player, self.multiworld)
                location = PuzzleBobble4Location(self.player, name, VS_CONTEST_SIMPLE_LOCATIONS[name], region)
                region.locations.append(location)
                self.multiworld.regions.append(region)
                entrance = menu.connect(region, f"VS/Contest Simple: {name}")
                # Keep every VS/Contest Simple check locked until its own global unlock is found.
                entrance.access_rule = lambda state: state.has("Unlock VS/Contest", self.player)
                vs_contest_locations[name] = location

        if self.vs_contest_complete:
            for character in self.contest_selected_characters:
                for number in range(1, self.contest_wins + 1):
                    name = vs_contest_complete_location_name(number, character)
                    region = Region(name, self.player, self.multiworld)
                    location = PuzzleBobble4Location(self.player, name, VS_CONTEST_COMPLETE_LOCATIONS[name], region)
                    region.locations.append(location)
                    self.multiworld.regions.append(region)
                    entrance = menu.connect(region, f"VS/Contest Complete: {name}")
                    if character != self.contest_start_character:
                        required_item = vs_contest_character_unlock_item_name(character)
                        entrance.access_rule = lambda state, item=required_item: state.has(item, self.player)
                    vs_contest_locations[name] = location

        # Single-goal modes keep one locked victory item. Multi-goal modes
        # keep all real final checks in the regular item pool.
        if self.effective_goal == Goal.option_one_u_z:
            letter = self.goal_letters[0]
            level_locations[f"{letter}5"].place_locked_item(self.create_item(victory_item_name(letter)))
        elif self.effective_goal == Goal.option_only_world:
            tarot_locations["World 5"].place_locked_item(self.create_item(tarot_world_victory_item_name()))

        if self.effective_goal == Goal.option_one_u_z:
            target = victory_item_name(self.goal_letters[0])
            self.multiworld.completion_condition[self.player] = lambda state, name=target: state.has(name, self.player)
        elif self.effective_goal == Goal.option_only_world:
            target = tarot_world_victory_item_name()
            self.multiworld.completion_condition[self.player] = lambda state, name=target: state.has(name, self.player)
        elif self.effective_goal == Goal.option_vs_story:
            # VS/Story's goal is the set of Dreg checks, one per selected
            # character. Evaluate reachability directly for generation beatability
            # instead of routing through a synthetic progression event. The event
            # could remain unreachable during playthrough calculation even when
            # the final checks were logically reachable.
            required = tuple(
                vs_story_complete_location_name("Dreg", character)
                for character in self.vs_story_selected_characters
            )
            self.multiworld.completion_condition[self.player] = (
                lambda state, names=required: all(
                    state.can_reach_location(name, self.player) for name in names
                )
            )
        elif self.effective_goal == Goal.option_vs_contest:
            # VS/Contest Complete's goal is the final win for every selected
            # character. Evaluate the target locations directly, just like
            # VS/Story, instead of using a synthetic progression event. The event
            # could remain unreachable during playthrough calculation even when
            # all required final checks were logically reachable.
            required = tuple(
                vs_contest_complete_location_name(self.contest_wins, character)
                for character in self.contest_selected_characters
            )
            self.multiworld.completion_condition[self.player] = (
                lambda state, names=required: all(
                    state.can_reach_location(name, self.player) for name in names
                )
            )
        elif self.effective_goal == Goal.option_all_tarot:
            # Check the 22 Tarot final locations directly. A synthetic progression
            # event gated by can_reach_location can remain unreachable during
            # Archipelago playthrough calculation even when the target locations
            # themselves are reachable.
            required = tuple(f"{card} 5" for card in TAROT_CARDS)
            self.multiworld.completion_condition[self.player] = (
                lambda state, names=required: all(
                    state.can_reach_location(name, self.player) for name in names
                )
            )
        else:
            # Logical event for the multi-goal Arcade mode. It is not a real check.
            # The client reports actual victory only after all final checks have
            # been confirmed by the server.
            event_name = "Event - All Goal Locations Reachable"
            event_region = Region(event_name, self.player, self.multiworld)
            self.multiworld.regions.append(event_region)
            menu.connect(event_region, event_name)
            event_location = PuzzleBobble4Location(self.player, event_name, None, event_region)
            event_region.locations.append(event_location)
            event_location.place_locked_item(PuzzleBobble4Item(event_name, ItemClassification.progression, None, self.player))
            required = tuple(f"{letter}5" for letter in GOAL_LETTERS)
            event_region.entrances[-1].access_rule = lambda state, names=required: all(
                state.can_reach_location(name, self.player) for name in names
            )
            self.multiworld.completion_condition[self.player] = lambda state, name=event_name: state.has(name, self.player)

    def get_filler_item_name(self) -> str:
        """Return a repeatable filler item, respecting the configured weights."""
        configured_weights = self.options.filler_weight.value or {}
        option_to_item = {
            "Pointer-Guide": "Pointer Guide",
            "Star-Bubble": "Star Bubble",
            "Metal-Bubble": "Metal Bubble",
            "Level-skip": "Level Skip",
        }
        filler_names = []
        filler_weights = []
        for option_name, item_name in option_to_item.items():
            try:
                weight = max(0, int(configured_weights.get(option_name, 0)))
            except (TypeError, ValueError, AttributeError):
                weight = 0
            if weight > 0:
                filler_names.append(item_name)
                filler_weights.append(weight)

        # Keep the fallback repeatable even if all configured weights are zero.
        # create_items() reports that invalid configuration during normal generation.
        if not filler_names:
            return "Level Skip"
        return self.random.choices(filler_names, weights=filler_weights, k=1)[0]

    def create_item(self, name):
        if name == "Level Skip":
            classification = ItemClassification.useful
        elif name in ("Pointer Guide", "Star Bubble", "Metal Bubble"):
            classification = ItemClassification.filler
        elif name in TRAP_ITEM_NAMES:
            classification = ItemClassification.trap
        elif name in {unlock_item_name(letter) for letter in UNLOCK_LETTERS}:
            classification = ItemClassification.progression
        elif name in {tarot_unlock_item_name(card) for card in TAROT_UNLOCK_CARDS}:
            classification = ItemClassification.progression
        elif (name in VS_STORY_CHARACTER_ITEM_IDS or name in VS_CONTEST_CHARACTER_ITEM_IDS
              or name in ("Unlock VS/Story", "Unlock VS/Contest", "Progressive Enemy")):
            classification = ItemClassification.progression
        elif name in {victory_item_name(letter) for letter in GOAL_LETTERS}:
            classification = ItemClassification.progression
        elif name == tarot_world_victory_item_name() or name in {tarot_victory_item_name(card) for card in TAROT_CARDS}:
            classification = ItemClassification.progression
        else:
            raise KeyError(f"Unknown Puzzle Bobble 4 item: {name}")

        return PuzzleBobble4Item(
            name,
            classification,
            self.item_name_to_id[name],
            self.player,
        )

    def create_items(self):
        active_arcade_location_count = (
            len(LEVEL_LOCATIONS) if self.puzzle_arcade_complete
            else len(self.arcade_active_letters) * 5 if self.puzzle_arcade_simple
            else 0
        )
        active_location_count = active_arcade_location_count + (
            len(self.tarot_active_cards) * 5 if (self.puzzle_story_complete or self.puzzle_story_simple) else 0
        )
        if self.vs_story_simple:
            active_location_count += len(self.vs_story_enemies)
        if self.vs_story_complete:
            active_location_count += len(self.vs_story_selected_characters) * len(self.vs_story_enemies)
        if self.vs_contest_simple:
            active_location_count += self.contest_wins
        if self.vs_contest_complete:
            active_location_count += len(self.contest_selected_characters) * self.contest_wins
        locked_victory_count = 0
        if self.effective_goal == Goal.option_one_u_z:
            locked_victory_count += 1
        if self.puzzle_story_complete and self.effective_goal == Goal.option_only_world:
            locked_victory_count += 1

        # When multiple modes are enabled, let Archipelago's normal progression
        # fill distribute Arcade and Tarot unlocks across ALL active mode checks.
        # This is what makes the modes form one shared progression instead of
        # forcing the key route to stay inside Arcade or Tarot. Keep the legacy
        # guaranteed local route when only one mode is enabled.
        enabled_mode_count = sum((
            self.puzzle_arcade_complete or self.puzzle_arcade_simple,
            self.puzzle_story_complete or self.puzzle_story_simple,
            self.vs_story_simple or self.vs_story_complete,
            self.vs_contest_simple or self.vs_contest_complete,
        ))
        if enabled_mode_count > 1:
            fixed_placements = []
            fixed_tarot_placements = []
        else:
            fixed_placements = self._make_fixed_unlock_plan() if self.puzzle_arcade_complete else []
            fixed_tarot_placements = self._make_fixed_tarot_unlock_plan() if self.puzzle_story_complete else []

        self.fixed_unlock_targets = {target for _, target in fixed_placements}
        self.fixed_tarot_unlock_targets = {target for _, target in fixed_tarot_placements}
        for location_name, target in fixed_placements:
            location = self.multiworld.get_location(location_name, self.player)
            if location.locked:
                raise ValueError(f"Cannot place Unlock World {target} at locked location {location_name}.")
            location.place_locked_item(self.create_item(unlock_item_name(target)))
        for location_name, target in fixed_tarot_placements:
            location = self.multiworld.get_location(location_name, self.player)
            if location.locked:
                raise ValueError(f"Cannot place Tarot Unlock {target} at locked location {location_name}.")
            location.place_locked_item(self.create_item(tarot_unlock_item_name(target)))

        fixed_vs_story_count = 0
        if self.vs_story_complete:
            start_location = self.multiworld.get_location(self.vs_story_fixed_start_location, self.player)
            if start_location.locked:
                raise ValueError(f"Cannot place starting Progressive Enemy at {self.vs_story_fixed_start_location}.")
            start_location.place_locked_item(self.create_item("Progressive Enemy"))
            fixed_vs_story_count = 1

        unlock_item_count = 0

        # Global unlocks for the two Simple VS modes are progression items.
        # Add them to the item pool here, where the unlock count is tracked.
        if self.vs_story_simple:
            self.multiworld.itempool.append(self.create_item("Unlock VS/Story"))
            unlock_item_count += 1

        if self.vs_contest_simple:
            self.multiworld.itempool.append(self.create_item("Unlock VS/Contest"))
            unlock_item_count += 1
        if self.puzzle_arcade_complete:
            for letter in UNLOCK_LETTERS:
                if letter not in self.fixed_unlock_targets:
                    self.multiworld.itempool.append(self.create_item(unlock_item_name(letter)))
            unlock_item_count += len(UNLOCK_LETTERS)
        elif self.puzzle_arcade_simple:
            for letter in self.puzzle_arcade_simple_letters:
                self.multiworld.itempool.append(self.create_item(unlock_item_name(letter)))
            unlock_item_count += len(self.puzzle_arcade_simple_letters)
        if self.puzzle_story_complete:
            for card in TAROT_UNLOCK_CARDS:
                if card not in self.fixed_tarot_unlock_targets:
                    self.multiworld.itempool.append(self.create_item(tarot_unlock_item_name(card)))
            unlock_item_count += len(TAROT_UNLOCK_CARDS)
        elif self.puzzle_story_simple:
            for card in self.puzzle_story_simple_cards:
                self.multiworld.itempool.append(self.create_item(tarot_unlock_item_name(card)))
            unlock_item_count += len(self.puzzle_story_simple_cards)

        if self.vs_contest_complete:
            for character in self.contest_selected_characters:
                if character != self.contest_start_character:
                    self.multiworld.itempool.append(self.create_item(vs_contest_character_unlock_item_name(character)))
            unlock_item_count += max(0, len(self.contest_selected_characters) - 1)

        if self.vs_story_complete:
            for character in self.vs_story_selected_characters:
                if character != self.vs_story_start_character:
                    self.multiworld.itempool.append(self.create_item(vs_story_character_unlock_item_name(character)))
            unlock_item_count += max(0, len(self.vs_story_selected_characters) - 1)
            progressive_count = len(self.vs_story_progressive_enemies)
            for _ in range(progressive_count - fixed_vs_story_count):
                self.multiworld.itempool.append(self.create_item("Progressive Enemy"))
            unlock_item_count += progressive_count

        # Locked victory items and fixed unlock placements occupy their locations
        # directly; every remaining active location receives one item from the pool.
        filler_pool_count = active_location_count - locked_victory_count - unlock_item_count
        if filler_pool_count < 0:
            raise ValueError("Not enough active checks to place the configured progression items.")

        configured_trap_weights = self.options.trap_types.value or {}
        trap_option_to_item = [
            ("Rainbow-Bubble", "Rainbow Bubble"),
            ("Obstacle-Block", "Obstacle Block"),
            ("Random-Color-Bubble", "Random Color Bubble"),
            ("Falling-Ceiling", "Falling Ceiling"),
        ]
        trap_names = [item_name for _, item_name in trap_option_to_item]
        trap_weights = [max(0, int(configured_trap_weights.get(option_name, 0))) for option_name, _ in trap_option_to_item]
        has_enabled_traps = sum(trap_weights) > 0
        trap_percentage = int(self.options.trap_weight.value)
        trap_count = int(filler_pool_count * trap_percentage / 100 + 0.5) if has_enabled_traps else 0
        trap_count = min(max(trap_count, 0), filler_pool_count)
        normal_filler_count = filler_pool_count - trap_count

        configured_weights = self.options.filler_weight.value or {}
        option_to_item = {
            "Pointer-Guide": "Pointer Guide", "Star-Bubble": "Star Bubble",
            "Metal-Bubble": "Metal Bubble", "Level-skip": "Level Skip",
        }
        filler_names = list(option_to_item.values())
        filler_weights = [max(0, int(configured_weights.get(option, 0))) for option in option_to_item]
        if sum(filler_weights) == 0:
            raise ValueError("At least one filler item type must have a weight greater than 0.")
        normal_fillers = self.random.choices(filler_names, weights=filler_weights, k=normal_filler_count)
        self.multiworld.itempool.extend(self.create_item(name) for name in normal_fillers)
        if trap_count:
            enabled = [(name, weight) for name, weight in zip(trap_names, trap_weights) if weight > 0]
            selected_traps = self.random.choices([x[0] for x in enabled], weights=[x[1] for x in enabled], k=trap_count)
            self.multiworld.itempool.extend(self.create_item(name) for name in selected_traps)

    def _make_fixed_tarot_unlock_plan(self):
        """Create a randomized, reachable branching tree for the 21 Tarot unlocks.

        Fool is the only starting card. Two unlocks are placed at random levels
        of Fool, and one unlock is placed at a random level of each of those two
        newly reachable cards. Level 5 is eligible unless it is reserved for a
        locked victory item by the selected goal.
        """
        targets = list(TAROT_UNLOCK_CARDS)
        self.random.shuffle(targets)
        initial_targets = targets[:2]
        next_targets = targets[2:4]

        reserved_finals = set()
        if self.puzzle_story_complete and self.effective_goal == Goal.option_only_world:
            reserved_finals.add("World")

        def random_level_location(card):
            eligible_levels = [level for level in range(1, 6)
                               if not (level == 5 and card in reserved_finals)]
            level = self.random.choice(eligible_levels)
            return f"{card} {level}"

        fool_levels = self.random.sample(range(1, 6), 2)
        placements = [(f"Fool {level}", target) for level, target in zip(fool_levels, initial_targets)]
        for source, target in zip(initial_targets, next_targets):
            placements.append((random_level_location(source), target))
        return placements

    def _make_fixed_unlock_plan(self):
        """Return (location_name, target_letter) pairs for the guaranteed route.

        A1/A2 and B1/B2 always contain four local unlock branches. In the
        single-goal mode, one branch continues through a randomized chain of
        4-7 unlocks toward the goal. In all-goals mode, a shared trunk branches
        toward all seven goal worlds. Unlock items not selected for fixed local
        placements remain in the ordinary pool and are distributed by normal fill.
        Their final recipients are determined by Archipelago's normal placement rules.
        """
        remote = set(self.remote_unlock_letters)
        goals = list(self.arcade_route_letters)
        non_goal_local = [letter for letter in UNLOCK_LETTERS
                          if letter not in goals and letter not in remote]
        self.random.shuffle(non_goal_local)
        placements = []
        used_targets = set()
        used_locations = set()

        def place(source, target, level=None):
            if target in used_targets:
                raise ValueError(f"Unlock target {target} was selected more than once.")
            if level is None:
                available = [n for n in range(1, 6)
                             if f"{source}{n}" not in used_locations
                             and not self.multiworld.get_location(f"{source}{n}", self.player).locked]
                if not available:
                    raise ValueError(f"No free check remains in world {source} for an unlock.")
                level = self.random.choice(available)
            location_name = f"{source}{level}"
            if location_name in used_locations:
                raise ValueError(f"Unlock location {location_name} was selected more than once.")
            if self.multiworld.get_location(location_name, self.player).locked:
                raise ValueError(f"Unlock location {location_name} is locked.")
            placements.append((location_name, target))
            used_locations.add(location_name)
            used_targets.add(target)

        # Pick a starting root. The first chain unlock goes at level 1, and a
        # separate branch goes at level 2; the other starting world gets two
        # additional early branches. Thus A1/A2 and B1/B2 are always local.
        root = self.random.choice(["A", "B"])
        other_root = "B" if root == "A" else "A"

        if len(goals) == 1:
            path_length = self.random.randint(4, 7)
            intermediate_count = path_length - 1
            needed = intermediate_count + 3  # chain intermediates plus 3 side branches
            if len(non_goal_local) < needed:
                raise ValueError("Not enough local Unlock targets to build the guaranteed route.")
            picked = self.random.sample(non_goal_local, needed)
            intermediates = picked[:intermediate_count]
            side_branches = picked[intermediate_count:]

            place(root, intermediates[0], 1)
            place(root, side_branches[0], 2)
            place(other_root, side_branches[1], 1)
            place(other_root, side_branches[2], 2)

            previous = intermediates[0]
            for target in intermediates[1:]:
                place(previous, target)
                previous = target
            # Keep the selected goal Unlock out of this fixed local chain.
            # Its final placement is left to Archipelago's normal fill.
            if goals[0] not in remote:
                place(previous, goals[0])
        else:
            # Disperse the seven goal Unlocks across seven different source
            # worlds, instead of concentrating them in only two worlds.
            # Reachability tree:
            #   root -> d1 -> d2 -> {d3,d4,d5,d6,d7}
            #                    d3 -> {d8,d9}
            # Each of d3..d9 contains one goal Unlock. This guarantees that
            # every goal Unlock is placed in a distinct, reachable world.
            # A/B retain the same four early branches as before.
            if len(non_goal_local) < 12:
                raise ValueError("Not enough local Unlock targets for dispersed all-goals routing.")
            picked = self.random.sample(non_goal_local, 12)
            d1, d2, d3, d4, d5, d6, d7, d8, d9 = picked[:9]
            side_branches = picked[9:]

            place(root, d1, 1)
            place(root, side_branches[0], 2)
            place(other_root, side_branches[1], 1)
            place(other_root, side_branches[2], 2)
            place(d1, d2)

            # d2 has five level checks, enough to unlock d3 through d7.
            for target in (d3, d4, d5, d6, d7):
                place(d2, target)
            # d3 opens the final two source worlds. It will also hold one goal
            # Unlock, which still fits within its five level checks.
            place(d3, d8)
            place(d3, d9)

            shuffled_goals = list(goals)
            self.random.shuffle(shuffled_goals)
            for source_world, goal in zip((d3, d4, d5, d6, d7, d8, d9), shuffled_goals):
                # Keep the selected goal Unlock out of these fixed local
                # placements. Normal fill determines where that item is placed;
                # the other goal Unlocks are spread one per source world.
                if goal in remote:
                    continue
                place(source_world, goal)

        return placements

    def post_fill(self):
        # Fixed local placements are locked before regular fill. Do not reshuffle
        # locations after fill: doing so could move another player's items and
        # invalidate that player's accessibility logic.
        return

    def fill_slot_data(self):
        # Include a stable generation identity so the client can persist local state
        # across server port changes without reusing it for a different generated seed.
        level_skip_seed_id = str(self.multiworld.seed_name)
        # This is the final location -> item map, not a second client-side randomization.
        # Archipelago remains authoritative for item delivery when a location check is collected.
        level_item_plan = {}
        if self.puzzle_arcade_complete or self.puzzle_arcade_simple:
            arcade_level_names = (
                list(LEVEL_LOCATIONS) if self.puzzle_arcade_complete
                else [f"{letter}{level}" for letter in self.arcade_active_letters for level in range(1, 6)]
            )
            for level_name in arcade_level_names:
                location = self.multiworld.get_location(level_name, self.player)
                item = location.item
                if item is None:
                    continue
                level_item_plan[level_name] = {
                    "item_name": item.name,
                    "item_player": int(item.player),
                    "is_local": bool(item.player == self.player),
                }
        tarot_item_plan = {}
        if self.puzzle_story_complete or self.puzzle_story_simple:
            active_tarot_locations = [
                f"{card} {level}" for card in self.tarot_active_cards for level in range(1, 6)
            ]
            for location_name in active_tarot_locations:
                location = self.multiworld.get_location(location_name, self.player)
                item = location.item
                if item is None:
                    continue
                tarot_item_plan[location_name] = {
                    "item_name": item.name,
                    "item_player": int(item.player),
                    "is_local": bool(item.player == self.player),
                }

        filler_weights = dict(self.options.filler_weight.value or {})
        trap_type_weights = dict(self.options.trap_types.value or {})
        weight_config = {
            "filler_weight": {str(k): int(v) for k, v in filler_weights.items()},
            "trap_weight_percent": int(self.options.trap_weight.value),
            "trap_types": {str(k): int(v) for k, v in trap_type_weights.items()},
        }

        return {
            "level_skip_seed_id": level_skip_seed_id,
            "selected_modes": {
                "puzzle_arcade": self.puzzle_arcade_selected_mode,
                "puzzle_story": self.puzzle_story_selected_mode,
                "vs_story": self.vs_story_selected_mode,
                "vs_contest": self.vs_contest_selected_mode,
            },
            # Current mode settings, exposed for the external client.
            "game_modes": {
                "puzzle_arcade": int(self.puzzle_arcade_complete),
                "puzzle_arcade_complete": int(self.puzzle_arcade_complete),
                "puzzle_arcade_simple": int(self.puzzle_arcade_simple),
                "puzzle_arcade_simple_letters": list(self.puzzle_arcade_simple_letters),
                "puzzle_story": int(self.puzzle_story_complete or self.puzzle_story_simple),
                "puzzle_story_complete": int(self.puzzle_story_complete),
                "puzzle_story_simple": int(self.puzzle_story_simple),
                "puzzle_story_simple_cards": list(self.puzzle_story_simple_cards),
                "vs_story_simple": int(self.vs_story_simple),
                "vs_story_complete": int(self.vs_story_complete),
                "vs_contest_simple": int(self.vs_contest_simple),
                "vs_contest_complete": int(self.vs_contest_complete),
            },
            # Flat aliases are also emitted for clients that consume individual
            # option keys instead of the nested game_modes object.
            "puzzle_arcade": int(self.puzzle_arcade_complete),
            "puzzle_arcade_complete": int(self.puzzle_arcade_complete),
            "puzzle_arcade_simple": int(self.puzzle_arcade_simple),
            "puzzle_arcade_simple_letters": list(self.puzzle_arcade_simple_letters),
            "puzzle_story": int(self.puzzle_story_complete or self.puzzle_story_simple),
            "puzzle_story_complete": int(self.puzzle_story_complete),
            "puzzle_story_simple": int(self.puzzle_story_simple),
            "puzzle_story_simple_cards": list(self.puzzle_story_simple_cards),
            "vs_story_simple": int(self.vs_story_simple),
            "vs_story_complete": int(self.vs_story_complete),
            "vs_contest_simple": int(self.vs_contest_simple),
            "vs_contest_complete": int(self.vs_contest_complete),
            "vs_story_options": {
                "extra_enemy": int(self.options.extra_enemy.value),
                "buff_tamtam": int(self.options.buff_tamtam.value),
                "vs_story_characters": int(self.options.vs_story_characters.value),
            },
            # Flat aliases are consumed by the current C# client; keep the nested
            # options object as a structured record for other consumers.
            "extra_enemy": int(self.options.extra_enemy.value),
            "buff_tamtam": int(self.options.buff_tamtam.value),
            "vs_story_characters": int(self.options.vs_story_characters.value),
            "vs_story_selected_characters": list(self.vs_story_selected_characters),
            "vs_story_start_character": self.vs_story_start_character or "",
            "vs_story_enemy_names": list(self.vs_story_enemies),
            "vs_story_progressive_enemy_count": len(self.vs_story_progressive_enemies),
            "vs_contest_options": {
                "Custom_contest_wins": int(self.options.Custom_contest_wins.value),
                "contest_characters": int(self.options.contest_characters.value),
                "increase_difficulty_contest": int(self.options.increase_difficulty_contest.value),
            },
            "custom_contest_wins": int(self.options.Custom_contest_wins.value),
            "contest_characters": int(self.options.contest_characters.value),
            # Flat aliases are required by external clients and prevent a missing
            # nested-map decode from silently applying the default value of zero.
            "increase_difficulty_contest": int(self.options.increase_difficulty_contest.value),
            "contest_selected_characters": list(self.contest_selected_characters),
            "contest_start_character": self.contest_start_character or "",
            "other_options": {"fix_packy": int(self.options.fix_packy.value)},
            "fix_packy": int(self.options.fix_packy.value),
            "extra_level_skips": int(self.options.Extra_level_skips.value),
            "puzzle_story_weights": {
                "normal": int(self.options.tarot_difficulty_weights.value.get("normal", 0)),
                "expert": int(self.options.tarot_difficulty_weights.value.get("expert", 0)),
            },
            "puzzle_story_difficulty_plan": dict(self.tarot_difficulty_plan),
            # Full seed-generated mode plan. No local mode randomization.
            "level_mode_plan": dict(self.level_mode_plan),
            # Explicitly expose the YAML toggle so the external client only
            # participates in DeathLink when this slot enabled it.
            "death_link": bool(self.options.death_link.value),
            # Final item placements from this generated seed. These entries are
            # informational only; checks and item delivery still use AP protocol.
            "level_item_plan": level_item_plan,
            "tarot_item_plan": tarot_item_plan,
            # Echo the effective YAML settings so the client can confirm which
            # weights were used for the generated seed.
            "item_weight_config": weight_config,
            "goal_letters": list(self.goal_letters),
            "goal_type": {
                Goal.option_one_u_z: "one_random_u_z",
                Goal.option_all_u_z: "all_u_z",
                Goal.option_only_world: "only_world",
                Goal.option_all_tarot: "all_tarot",
                Goal.option_vs_story: "vs_story",
                Goal.option_vs_contest: "vs_contest",
            }[self.effective_goal],
            "goal_victory_items": (
                [tarot_world_victory_item_name()] if self.effective_goal == Goal.option_only_world else
                [] if self.effective_goal in (Goal.option_all_tarot, Goal.option_all_u_z) else
                [victory_item_name(letter) for letter in self.goal_letters]
            ),
            "goal_check_locations": (
                [f"{card} 5" for card in TAROT_CARDS] if self.effective_goal == Goal.option_all_tarot else
                [f"{letter}5" for letter in GOAL_LETTERS] if self.effective_goal == Goal.option_all_u_z else
                [vs_story_complete_location_name("Dreg", character) for character in self.vs_story_selected_characters]
                if self.effective_goal == Goal.option_vs_story else
                [vs_contest_complete_location_name(self.contest_wins, character) for character in self.contest_selected_characters]
                if self.effective_goal == Goal.option_vs_contest else []
            ),
            "puzzle_arcade_simple_letters": list(self.puzzle_arcade_simple_letters),
            "unlock_letters": (
                list(UNLOCK_LETTERS) if self.puzzle_arcade_complete
                else list(self.puzzle_arcade_simple_letters)
            ),
            "tarot_unlock_cards": list(TAROT_UNLOCK_CARDS),
            "unlock_prerequisites": {
                letter: list(self.remote_prerequisite_letters)
                for letter in self.goal_letters
            },
            "remote_unlock_letters": list(self.remote_unlock_letters),
            "remote_goal_letter": self.remote_goal_letter,
        }
