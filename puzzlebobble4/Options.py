from Options import Choice, DeathLink, OptionDict, Range, Toggle


class PuzzleArcadeMode(OptionDict):
    """Classic puzzle mode. Unlock and play worlds from A to Z.

simple: Includes 5 worlds to unlock and play.
complete: Includes all worlds to unlock and play.
disable: Puzzle/Arcade is not included."""
    display_name = "Puzzle/Arcade Mode"
    default = {"complete": 0, "disable": 0, "simple": 50}


class PuzzleStoryMode(OptionDict):
    """Unlock Tarot cards and play their levels.

simple: Includes 6 Tarot cards to unlock and play.
complete: Includes all 22 Tarot cards to unlock and play.
disable: Puzzle/Story is not included."""
    display_name = "Puzzle/Story Mode"
    default = {"complete": 0, "disable": 0, "simple": 50}


class VsStoryMode(OptionDict):
    """Fight 12 enemies in VS mode.

simple: One check per defeated enemy, using any character. Find the unlock to start playing!
complete: Includes character unlocks and progressive enemies, with a separate check for each character.
disable: VS/Story is not included."""
    display_name = "VS/Story Mode"
    default = {"complete": 0, "disable": 0, "simple": 50}


class VsContestMode(OptionDict):
    """Win consecutive matches to earn checks.

simple: Reach a set number of consecutive wins using any character. Find the unlock to start playing!
complete: Includes character unlocks, with a separate check for each character.
disable: VS/Contest is not included."""
    display_name = "VS/Contest Mode"
    default = {"complete": 0, "disable": 0, "simple": 50}


class Goal(Choice):
    """Choose the victory condition for Puzzle Bobble 4.

Puzzle/Arcade Complete:
- one_u_z: Complete one randomly selected world from U, V, W, ?, X, Y or Z.
- all_u_z: Complete every final world: U, V, W, ?, X, Y and Z.

Puzzle/Story Complete:
- only_world: Complete the final Tarot card World.
- all_tarot: Complete every Tarot card.

VS/Story:
- vs_story: Defeat Dreg using different characters.

VS/Contest:
- vs_contest: Win consecutive matches using different characters."""
    display_name = "Goal"
    option_one_u_z = 0
    option_all_u_z = 1
    option_only_world = 2
    option_all_tarot = 3
    option_vs_story = 4
    option_vs_contest = 5
    default = option_one_u_z


class LevelModeWeights(OptionDict):
    """Choose which level variants the randomizer can assign to Puzzle/Arcade levels. At least one number must be greater than 0.

normal_o: Normal difficulty of the original levels.
expert_o: Expert difficulty of the original levels.
normal_aw: Normal difficulty of Another World levels.
expert_aw: Expert difficulty of Another World levels."""
    display_name = "Level Mode Weights"
    default = {
        "expert_aw": 0,
        "expert_o": 0,
        "normal_aw": 0,
        "normal_o": 50,
    }


class TarotDifficultyWeights(OptionDict):
    """Choose which level difficulty the randomizer can assign to Puzzle/Story levels. At least one number must be greater than 0."""
    display_name = "Tarot Difficulty Weights"
    default = {
        "expert": 0,
        "normal": 50,
    }


class VsStoryCharacters(Range):
    """Number of randomly selected characters available to unlock and use for checks. Only applies to VS/Story Complete."""
    display_name = "VS/Story Characters"
    range_start = 3
    range_end = 16
    default = 5


class ExtraEnemy(Toggle):
    """Add an extra powerful enemy before Madam Luna."""
    display_name = "Extra Enemy"
    default = 0


class CustomContestWins(Range):
    """Number of consecutive wins required to earn checks."""
    display_name = "Custom Contest Wins"
    range_start = 3
    range_end = 30
    default = 10


class ContestCharacters(Range):
    """Number of randomly selected characters available to unlock and use for checks. Only applies to VS/Contest Complete."""
    display_name = "Contest Characters"
    range_start = 3
    range_end = 16
    default = 5


class IncreaseDifficultyContest(Range):
    """Additional difficulty for VS/Contest. 0 keeps the original difficulty."""
    display_name = "Increase Difficulty Contest"
    range_start = 0
    range_end = 200
    default = 0


class FillerWeight(OptionDict):
    """Items that can appear in the item pool. Set an item's weight to 0 to disable it.

Pointer-Guide: Enables the pointer guide for the current level.
Star-Bubble: Replaces your next bubble with a Star Bubble.
Metal-Bubble: Replaces your next bubble with a Metal Bubble.
Level-skip: Lets you skip the current level. Press R2 during a level to use it. In VS mode, deals massive damage to the enemy."""
    display_name = "Filler Weight"
    default = {
        "Level-skip": 5,
        "Metal-Bubble": 10,
        "Pointer-Guide": 25,
        "Star-Bubble": 10,
    }


class TrapWeight(Range):
    """Set the percentage of trap items in the item pool. Set to 0 to disable traps."""
    display_name = "Trap Weight"
    range_start = 0
    range_end = 100
    default = 10


class TrapTypes(OptionDict):
    """Individual trap types that can appear in the item pool. Set a trap weight to 0 to disable it.

Rainbow-Bubble: Replaces your next bubble with a Rainbow Bubble.
Obstacle-Block: Replaces your next bubble with an Obstacle Block.
Random-Color-Bubble: Replaces your next bubble with a random-color bubble.
Falling-Ceiling: Makes the ceiling start falling on your next shot. In VS mode, deals damage to the player."""
    display_name = "Trap Types"
    default = {
        "Falling-Ceiling": 5,
        "Obstacle-Block": 15,
        "Rainbow-Bubble": 20,
        "Random-Color-Bubble": 10,
    }


class ExtraLevelSkips(Range):
    """Number of Level Skips available at the start of the game."""
    display_name = "Extra Level Skips"
    range_start = 0
    range_end = 25
    default = 3


class BuffTamtam(Toggle):
    """Increase Player Tamtam's damage in VS mode."""
    display_name = "Buff Tamtam"
    default = 0


class FixPacky(Toggle):
    """Fix Packy's damage in VS mode so he can attack with Rainbow Bubbles."""
    display_name = "Packy Fix"
    default = 0


class PuzzleBobble4DeathLink(DeathLink):
    """When you die, everyone who enabled Death Link dies. Of course, the reverse is true too.

Please remember that this game can be heavily RNG-dependent. With difficulty changes and traps, you may die frequently, which can make the session frustrating.
You have been warned. Have fun!"""
