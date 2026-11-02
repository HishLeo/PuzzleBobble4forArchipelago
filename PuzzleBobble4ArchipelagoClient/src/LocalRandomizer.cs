using System.Text.Json;
using System.Text.Json.Serialization;

namespace PuzzleBobble4ArchipelagoClient;


internal sealed class VsStoryOptions
{
    // 0 = off; 1 = on.
    [JsonPropertyName("extra_enemy")]
    public int ExtraEnemy { get; set; } = 0;

    [JsonPropertyName("buff_tamtam")]
    public int BuffTamtam { get; set; } = 0;

    // Number of randomly selected VS/Story characters required in Complete mode.
    [JsonPropertyName("vs_story_characters")]
    public int VsStoryCharacters { get; set; } = 3;
}

internal sealed class GameModesOptions
{
    // 0 = off; 1 = on. In a live seed, values from APWorld slot_data override this local fallback.
    [JsonPropertyName("puzzle_arcade")]
    public int PuzzleArcade { get; set; } = 1;

    [JsonPropertyName("puzzle_arcade_simple")]
    public int PuzzleArcadeSimple { get; set; } = 0;

    [JsonPropertyName("puzzle_story")]
    public int PuzzleStory { get; set; } = 1;

    [JsonPropertyName("puzzle_story_complete")]
    public int PuzzleStoryComplete { get; set; } = 1;

    [JsonPropertyName("puzzle_story_simple")]
    public int PuzzleStorySimple { get; set; } = 0;

    [JsonPropertyName("vs_story_simple")]
    public int VsStorySimple { get; set; } = 1;

    [JsonPropertyName("vs_story_complete")]
    public int VsStoryComplete { get; set; } = 1;

    [JsonPropertyName("vs_contest_simple")]
    public int VsContestSimple { get; set; } = 1;

    [JsonPropertyName("vs_contest_complete")]
    public int VsContestComplete { get; set; } = 1;
}

internal sealed class ContestOptions
{
    [JsonPropertyName("Custom_contest_wins")]
    public int CustomContestWins { get; set; } = 10;

    // Number of randomly selected VS/Contest characters required in Complete mode.
    [JsonPropertyName("contest_characters")]
    public int ContestCharacters { get; set; } = 3;

    // Additive enemy difficulty for VS/Contest only; 0 preserves vanilla.
    [JsonPropertyName("increase_difficulty_contest")]
    public int IncreaseDifficultyContest { get; set; } = 0;
}

internal sealed class OtherOptions
{
    // 0 = vanilla Packy attacks; 1 = correct Packy's attack selection in both VS modes.
    [JsonPropertyName("fix_packy")]
    public int FixPacky { get; set; } = 0;
}

internal sealed class ItemWeightSettings
{
    [JsonPropertyName("filler_weight")]
    public Dictionary<string, int> FillerWeight { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Level Skip"] = 10,
        ["Pointer-Guide"] = 20,
        ["Star-Bubble"] = 10,
        ["Metal-Bubble"] = 10,
    };

    [JsonPropertyName("trap_weight_percent")]
    public int TrapWeightPercent { get; set; } = 10;

    [JsonPropertyName("trap_types")]
    public Dictionary<string, int> TrapTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rainbow-Bubble"] = 15,
        ["Obstacle-Block"] = 10,
        ["Random-Color-Bubble"] = 10,
        ["Falling-Ceiling"] = 10,
    };
}

internal sealed class LevelModeWeightSettings
{
    [JsonPropertyName("normal_o")] public int NormalO { get; set; } = 100;
    [JsonPropertyName("expert_o")] public int ExpertO { get; set; } = 0;
    [JsonPropertyName("normal_aw")] public int NormalAW { get; set; } = 0;
    [JsonPropertyName("expert_aw")] public int ExpertAW { get; set; } = 0;
}


internal sealed class TarotDifficultyWeightSettings
{
    [JsonPropertyName("normal")] public int Normal { get; set; } = 25;
    [JsonPropertyName("expert")] public int Expert { get; set; } = 25;
}

internal sealed class GoalWeightSettings
{
    [JsonPropertyName("one_u_z")] public int OneUZ { get; set; } = 25;
    [JsonPropertyName("all_u_z")] public int AllUZ { get; set; } = 10;
    [JsonPropertyName("only_world")] public int OnlyWorld { get; set; } = 10;
    [JsonPropertyName("all_tarot")] public int AllTarot { get; set; } = 5;
    [JsonPropertyName("vs_story")] public int VsStory { get; set; } = 0;
    [JsonPropertyName("vs_contest")] public int VsContest { get; set; } = 0;
}

internal sealed class LocalRandomizer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly Random _random = Random.Shared;

    internal LevelModePlan CreateModePlan(LevelModeWeightSettings weights)
    {
        var weighted = new List<(LevelMode Mode, int Weight)>
        {
            (LevelMode.NormalO, weights.NormalO),
            (LevelMode.ExpertO, weights.ExpertO),
            (LevelMode.NormalAW, weights.NormalAW),
            (LevelMode.ExpertAW, weights.ExpertAW),
        };
        ValidateWeights(weighted.Select(x => (x.Mode.ToString(), x.Weight)), "level-mode-weights.json");
        var assignments = LevelModeGenerator.BuildLevelIds()
            .Select(level => new LevelModeAssignment(level, Pick(weighted)))
            .ToList();
        return new LevelModePlan(1,
            new ModeWeights(weights.NormalO, weights.ExpertO, weights.NormalAW, weights.ExpertAW),
            DateTimeOffset.UtcNow, assignments);
    }

    internal string PickGoal(GoalWeightSettings settings)
    {
        var weighted = new List<(string Value, int Weight)>
        {
            ("One_u-z", settings.OneUZ),
            ("All_u-z", settings.AllUZ),
            ("Only_World", settings.OnlyWorld),
            ("All_Tarot", settings.AllTarot),
            ("vs_story", settings.VsStory),
            ("vs_contest", settings.VsContest),
        };
        ValidateWeights(weighted.Select(x => (x.Value, x.Weight)), "goal-weights.json");
        return Pick(weighted);
    }

    internal Dictionary<string, string> CreateTarotDifficultyPlan(IEnumerable<string> tarotLevelIds, TarotDifficultyWeightSettings settings)
    {
        var weighted = new List<(string Value, int Weight)>
        {
            ("Normal", settings.Normal),
            ("Expert", settings.Expert),
        };
        ValidateWeights(weighted.Select(x => (x.Value, x.Weight)), "tarot-difficulty-weights.json");
        return tarotLevelIds.ToDictionary(levelId => levelId, _ => Pick(weighted), StringComparer.OrdinalIgnoreCase);
    }

    internal Dictionary<string, string> CreateItemPlan(ItemWeightSettings settings)
    {
        if (settings.TrapWeightPercent is < 0 or > 100)
            throw new InvalidDataException("trap_weight_percent tiene que estar entre 0 y 100.");
        ValidateWeights(settings.FillerWeight.Select(x => (x.Key, x.Value)), "item-weights.json: filler_weight");
        ValidateWeights(settings.TrapTypes.Select(x => (x.Key, x.Value)), "item-weights.json: trap_types");

        string[] levels = LevelModeGenerator.BuildLevelIds();
        var plan = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string level in levels)
        {
            bool trap = _random.Next(100) < settings.TrapWeightPercent;
            plan[level] = trap
                ? Pick(settings.TrapTypes.Select(x => (x.Key, x.Value)).ToList())
                : Pick(settings.FillerWeight.Select(x => (x.Key, x.Value)).ToList());
        }
        return plan;
    }

    internal static T LoadOrCreate<T>(string fileName, T defaults)
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonSerializer.Serialize(defaults, JsonOptions));
            Console.WriteLine($"Created default configuration: {fileName}");
            return defaults;
        }
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException("El archivo está vacío o contiene null.");
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException)
        {
            throw new InvalidDataException($"No se pudo cargar {fileName}: {ex.Message}", ex);
        }
    }

    private T Pick<T>(IReadOnlyList<(T Value, int Weight)> choices)
    {
        int total = choices.Sum(x => Math.Max(0, x.Weight));
        if (total <= 0) throw new InvalidDataException("La suma de los pesos tiene que ser mayor que cero.");
        int roll = _random.Next(total);
        foreach (var (value, weight) in choices)
        {
            roll -= Math.Max(0, weight);
            if (roll < 0) return value;
        }
        return choices[^1].Value;
    }

    private static void ValidateWeights(IEnumerable<(string Name, int Weight)> entries, string source)
    {
        var values = entries.ToArray();
        if (values.Length == 0 || values.Any(x => x.Weight < 0) || values.All(x => x.Weight == 0))
            throw new InvalidDataException($"Pesos inválidos en {source}: deben ser no negativos y al menos uno debe ser mayor que cero.");
    }
}
