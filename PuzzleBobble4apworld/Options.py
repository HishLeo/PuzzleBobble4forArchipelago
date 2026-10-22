from Options import Choice, DeathLink, OptionDict, Range


class PuzzleBobble4DeathLink(DeathLink):
    """When you die, everyone who enabled death link dies. Of course, the reverse is true too.

    Please remember that this game can be heavily RNG-dependent. With the difficulty
    changes and traps, you may die frequently, which can make the session frustrating.
    You have been warned—have fun!"""


class Goal(Choice):
    """Choose the victory condition for Puzzle Bobble 4.

    one_random_u_z: Complete level 5 of one randomly selected world from U, V, W,
    ?, X, Y, or Z to win.
    all_u_z: Complete level 5 in every world from U, V, W, ?, X, Y, and Z to win.
    """

    display_name = "Goal"
    option_one_random_u_z = 0
    option_all_u_z = 1
    default = option_one_random_u_z


class LevelModeWeights(OptionDict):
    """Choose which level variants the randomizer can assign throughout the game.

    normal_o: Original levels on Normal difficulty.
    expert_o: Original levels on Expert difficulty.
    normal_aw: Another World levels on Normal difficulty.
    expert_aw: Another World levels on Expert difficulty.
    """

    display_name = "Level Mode Weights"
    default = {
        "normal_o": 50,
        "expert_o": 0,
        "normal_aw": 0,
        "expert_aw": 0,
    }


class FillerWeight(OptionDict):
    """Configure the regular filler items that can appear in the item pool.
    Set an item's weight to 0 to disable it.

    Pointer-Guide: Enables the pointer guide for the current level.
    Star-Bubble: Replaces your next bubble with a Star Bubble.
    Metal-Bubble: Replaces your next bubble with a Metal Bubble.
    Level-skip: Lets you skip the current level. Press R2 during a level to use it.

    Single-player recommendation: leave only Level-skip enabled. Checks are sent
    when a level is completed, so other filler items cannot affect that level because
    it has already ended.
    """

    display_name = "Filler Weight"
    default = {
        "Pointer-Guide": 20,
        "Star-Bubble": 10,
        "Metal-Bubble": 10,
        "Level-skip": 10,
    }


class TrapWeight(Range):
    """Set the overall weight of trap items in the item pool. Set to 0 to disable traps.

    Single-player recommendation: disable traps. Checks are sent when a level is
    completed, so traps received from those checks cannot affect the level that has
    already ended.
    """

    display_name = "Trap Weight"
    range_start = 0
    range_end = 100
    default = 10


class TrapTypes(OptionDict):
    """Configure the individual trap types that can appear in the item pool.
    Set a trap type's weight to 0 to disable it.

    Falling-Ceiling: Makes the ceiling start falling on your next shot.
    Obstacle-Block: Replaces your next bubble with an Obstacle Block.
    Rainbow-Bubble: Replaces your next bubble with a Rainbow Bubble.
    Random-Color-Bubble: Replaces your next bubble with a random-color bubble.
    """

    display_name = "Trap Types"
    default = {
        "Rainbow-Bubble": 10,
        "Obstacle-Block": 10,
        "Random-Color-Bubble": 15,
        "Falling-Ceiling": 15,
    }
