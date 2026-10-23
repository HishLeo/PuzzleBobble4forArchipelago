from dataclasses import dataclass

from BaseClasses import Region, ItemClassification
from Options import PerGameCommonOptions
from worlds.AutoWorld import World, WebWorld
from BaseClasses import Tutorial
from .Items import GAME_NAME, PuzzleBobble4Item
from .Locations import LEVEL_LOCATIONS, PuzzleBobble4Location
from .Rules import LETTERS_BY_STEP
from .Options import Goal, LevelModeWeights, FillerWeight, TrapWeight, TrapTypes, PuzzleBobble4DeathLink


GOAL_LETTERS = ["U", "V", "W", "?", "X", "Y", "Z"]
ALL_LETTERS = [letter for step in LETTERS_BY_STEP for letter in step]
UNLOCK_LETTERS = [letter for letter in ALL_LETTERS if letter not in ("A", "B")]

FILLER_ITEM_NAMES = ["Pointer-Guide", "Star-Bubble", "Metal-Bubble"]
TRAP_ITEM_NAMES = ["Rainbow-Bubble", "Obstacle-Block", "Random-Color-Bubble", "Falling-Ceiling"]

# Preserve the existing Level Skip and victory item IDs. New items use unused IDs.
ITEM_IDS = {
    "Level Skip": 200001,
    "Pointer-Guide": 200011,
    "Star-Bubble": 200012,
    "Metal-Bubble": 200013,
    "Rainbow-Bubble": 200014,
    "Obstacle-Block": 200015,
    "Random-Color-Bubble": 200016,
    "Falling-Ceiling": 200017,
}


def victory_item_name(letter):
    return f"Victory - Complete World {letter}"


def unlock_item_name(letter):
    return f"Unlock {letter} world"


@dataclass
class PuzzleBobble4Options(PerGameCommonOptions):
    goal: Goal
    level_mode_weights: LevelModeWeights
    filler_weight: FillerWeight
    trap_weight: TrapWeight
    trap_types: TrapTypes
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

    location_name_to_id = LEVEL_LOCATIONS

    def generate_early(self):
        # Pick the random goal once per seed, using Archipelago's seeded RNG.
        if self.options.goal == Goal.option_one_random_u_z:
            self.goal_letters = [self.random.choice(GOAL_LETTERS)]
        else:
            self.goal_letters = list(GOAL_LETTERS)

        # Select two Unlocks relevant to the goal route. These items are excluded
        # from the locally pre-filled route. Archipelago's normal fill decides
        # their final placement, which may be local or belong to another player.
        self.remote_unlock_letters = []
        self.remote_goal_letter = None
        self.remote_prerequisite_letters = []
        if self.multiworld.players > 1:
            self.remote_goal_letter = self.random.choice(self.goal_letters)
            eligible = [letter for letter in UNLOCK_LETTERS
                        if letter not in self.goal_letters]
            if not eligible:
                raise ValueError("No non-goal Unlock is available to serve as the additional prerequisite.")
            prerequisite = self.random.choice(eligible)
            self.remote_unlock_letters = [self.remote_goal_letter, prerequisite]
            self.remote_prerequisite_letters = [prerequisite]

        # Generate one weighted mode for every level using Archipelago's seeded
        # RNG. This plan is sent to the external client through slot_data.
        mode_names = ["NormalO", "ExpertO", "NormalAW", "ExpertAW"]
        weight_keys = ["normal_o", "expert_o", "normal_aw", "expert_aw"]
        configured_weights = self.options.level_mode_weights.value
        weights = [int(configured_weights.get(key, 0)) for key in weight_keys]
        if any(weight < 0 for weight in weights):
            raise ValueError("Level Mode Weights cannot be negative.")
        if sum(weights) <= 0:
            raise ValueError("At least one Level Mode Weight must be greater than zero.")
        level_ids = [f"{letter}{number}" for step in LETTERS_BY_STEP for letter in step for number in range(1, 6)]
        self.level_mode_plan = {
            level_id: self.random.choices(mode_names, weights=weights, k=1)[0]
            for level_id in level_ids
        }

    def create_regions(self):
        menu = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu)

        level_regions = {}
        level_locations = {}

        # Every game level remains a normal check with its stable ID.
        for step in LETTERS_BY_STEP:
            for letter in step:
                for number in range(1, 6):
                    level_name = f"{letter}{number}"
                    region = Region(level_name, self.player, self.multiworld)
                    location = PuzzleBobble4Location(
                        self.player,
                        level_name,
                        LEVEL_LOCATIONS[level_name],
                        region,
                    )
                    region.locations.append(location)
                    self.multiworld.regions.append(region)
                    level_regions[level_name] = region
                    level_locations[level_name] = location

        # In the actual client, the player can select any unlocked letter
        # directly. Therefore, every world's first level is connected to Menu;
        # the only restriction is owning that world's Unlock item. The old
        # Pascal-triangle letter connections do not describe client navigation.
        for step in LETTERS_BY_STEP:
            for letter in step:
                first_level = f"{letter}1"
                # Explicit entrance names keep spoiler Paths readable instead
                # of repeating the source region name (Menu -> Menu -> A1).
                menu.connect(level_regions[first_level], f"Select {first_level}")

        # Levels within a letter are sequential. Name each transition so paths
        # read as A1 -> Advance to A2 -> A2, rather than repeating region names.
        for step in LETTERS_BY_STEP:
            for letter in step:
                for number in range(1, 5):
                    current_level = f"{letter}{number}"
                    next_level = f"{letter}{number + 1}"
                    level_regions[current_level].connect(
                        level_regions[next_level], f"Advance to {next_level}"
                    )

        # Gate each letter's Menu entrance by its own Unlock item. Each goal
        # world also requires the selected prerequisite Unlock. The selected goal
        # Archipelago's normal fill determines each Unlock's recipient, which may
        # be this player or another player. The client applies the same prerequisite
        # before unlocking goal letters in-game.
        for letter in UNLOCK_LETTERS:
            required_item = unlock_item_name(letter)
            prerequisites = list(self.remote_prerequisite_letters) if letter in self.goal_letters else []
            first_level_region = level_regions[f"{letter}1"]
            for entrance in first_level_region.entrances:
                entrance.access_rule = (
                    lambda state, item_name=required_item, prereq_letters=prerequisites:
                    state.has(item_name, self.player) and all(
                        state.has(unlock_item_name(prereq), self.player)
                        for prereq in prereq_letters
                    )
                )

        # Goal locations keep their normal check names/IDs, but contain locked
        # Victory items instead of ordinary randomized items.
        for letter in self.goal_letters:
            item_name = victory_item_name(letter)
            goal_location = level_locations[f"{letter}5"]
            victory_item = PuzzleBobble4Item(
                item_name,
                ItemClassification.progression,
                self.item_name_to_id[item_name],
                self.player,
            )
            goal_location.place_locked_item(victory_item)

        if len(self.goal_letters) == 1:
            target_item = victory_item_name(self.goal_letters[0])
            self.multiworld.completion_condition[self.player] = (
                lambda state, item_name=target_item: state.has(item_name, self.player)
            )
        else:
            required_items = [victory_item_name(letter) for letter in GOAL_LETTERS]
            self.multiworld.completion_condition[self.player] = (
                lambda state, names=required_items: all(
                    state.has(item_name, self.player) for item_name in names
                )
            )

    def get_filler_item_name(self) -> str:
        """Return a repeatable filler item, respecting the configured weights."""
        configured_weights = self.options.filler_weight.value or {}
        option_to_item = {
            "Pointer-Guide": "Pointer-Guide",
            "Star-Bubble": "Star-Bubble",
            "Metal-Bubble": "Metal-Bubble",
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
        elif name in ("Pointer-Guide", "Star-Bubble", "Metal-Bubble"):
            classification = ItemClassification.filler
        elif name in TRAP_ITEM_NAMES:
            classification = ItemClassification.trap
        elif name in {unlock_item_name(letter) for letter in UNLOCK_LETTERS}:
            classification = ItemClassification.progression
        elif name in {victory_item_name(letter) for letter in GOAL_LETTERS}:
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
        # Goal locations already contain locked Victory items. Lock a small,
        # deliberate branching route locally. Unlock items not used in these
        # fixed placements remain in the ordinary item pool for normal fill.
        item_count = len(LEVEL_LOCATIONS) - len(self.goal_letters)
        fixed_placements = self._make_fixed_unlock_plan()
        self.fixed_unlock_targets = {target for _, target in fixed_placements}

        for location_name, target in fixed_placements:
            location = self.multiworld.get_location(location_name, self.player)
            if location.locked:
                raise ValueError(f"Cannot place Unlock {target} world at locked location {location_name}.")
            item = self.create_item(unlock_item_name(target))
            item.classification = ItemClassification.progression
            location.place_locked_item(item)

        for letter in UNLOCK_LETTERS:
            if letter not in self.fixed_unlock_targets:
                self.multiworld.itempool.append(self.create_item(unlock_item_name(letter)))

        # Locking k Unlocks removes k locations and k items from the regular
        # fill, so the usual filler count remains item_count - total Unlocks.
        filler_pool_count = item_count - len(UNLOCK_LETTERS)
        if filler_pool_count < 0:
            raise ValueError("Not enough locations to place all letter-unlock items.")

        configured_trap_weights = self.options.trap_types.value or {}
        trap_names = [
            "Rainbow-Bubble",
            "Obstacle-Block",
            "Random-Color-Bubble",
            "Falling-Ceiling",
        ]
        enabled_traps = []
        trap_weights = []
        for trap_name in trap_names:
            try:
                weight = max(0, int(configured_trap_weights.get(trap_name, 0)))
            except (TypeError, ValueError, AttributeError):
                weight = 0
            enabled_traps.append(trap_name)
            trap_weights.append(weight)
        has_enabled_traps = sum(trap_weights) > 0
        trap_percentage = int(self.options.trap_weight.value)
        trap_count = int(filler_pool_count * trap_percentage / 100 + 0.5) if has_enabled_traps else 0
        trap_count = min(max(trap_count, 0), filler_pool_count)
        normal_filler_count = filler_pool_count - trap_count

        # FillerWeight is a dictionary in the YAML, with one relative weight per
        # filler item. Level-skip maps to the existing client-compatible item name.
        configured_weights = self.options.filler_weight.value or {}
        option_to_item = {
            "Pointer-Guide": "Pointer-Guide",
            "Star-Bubble": "Star-Bubble",
            "Metal-Bubble": "Metal-Bubble",
            "Level-skip": "Level Skip",
        }
        filler_names = []
        filler_weights = []
        for option_name, item_name in option_to_item.items():
            try:
                weight = max(0, int(configured_weights.get(option_name, 0)))
            except (TypeError, ValueError, AttributeError):
                weight = 0
            filler_names.append(item_name)
            filler_weights.append(weight)

        if sum(filler_weights) == 0:
            raise ValueError("At least one filler item type must have a weight greater than 0.")

        normal_fillers = self.random.choices(filler_names, weights=filler_weights, k=normal_filler_count)
        self.multiworld.itempool.extend(self.create_item(name) for name in normal_fillers)

        # Trap weight is a percentage of the filler pool, not an extra relative
        # weight against normal fillers. Enabled trap types use their YAML weights.
        if trap_count:
            selected_traps = self.random.choices(enabled_traps, weights=trap_weights, k=trap_count)
            self.multiworld.itempool.extend(self.create_item(name) for name in selected_traps)

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
        goals = list(self.goal_letters)
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
        # Called for the generated seed after item placement. This is the final
        # location -> item map, not a second client-side randomization. The client
        # may use it for inspection/diagnostics; Archipelago remains authoritative
        # for item delivery when a location check is collected.
        level_item_plan = {}
        for level_name in LEVEL_LOCATIONS:
            location = self.multiworld.get_location(level_name, self.player)
            item = location.item
            if item is None:
                continue
            level_item_plan[level_name] = {
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
            # Full seed-generated mode plan. No local mode randomization.
            "level_mode_plan": dict(self.level_mode_plan),
            # Explicitly expose the YAML toggle so the external client only
            # participates in DeathLink when this slot enabled it.
            "death_link": bool(self.options.death_link.value),
            # Final item placements from this generated seed. These entries are
            # informational only; checks and item delivery still use AP protocol.
            "level_item_plan": level_item_plan,
            # Echo the effective YAML settings so the client can confirm which
            # weights were used for the generated seed.
            "item_weight_config": weight_config,
            "goal_letters": list(self.goal_letters),
            "unlock_letters": list(UNLOCK_LETTERS),
            "unlock_prerequisites": {
                letter: list(self.remote_prerequisite_letters)
                for letter in self.goal_letters
            },
            "remote_unlock_letters": list(self.remote_unlock_letters),
            "remote_goal_letter": self.remote_goal_letter,
        }
