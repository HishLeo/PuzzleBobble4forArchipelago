using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Packets;


namespace PuzzleBobble4ArchipelagoClient;

internal static class Program
{
    // Enable ANSI/VT processing in the Windows console so RGB color escape
    // sequences are rendered as colors instead of appearing as literal text.
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private static void EnableConsoleColors()
    {
        try
        {
            IntPtr handle = GetStdHandle(StdOutputHandle);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                return;

            if (GetConsoleMode(handle, out uint mode))
                SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
        }
        catch
        {
            // Keep the client usable if the output isn't a Windows console.
        }
    }

    // Keep false for live Archipelago integration. Set true only for offline debugging.
    private static readonly bool LocalDevelopmentMode = false;

    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(280);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(75);

    private const uint GameModeAddress = 0x00167A0C;
    private const uint GameResetAddress = 0x00167A04;
    private const uint LevelStateAddress = 0x00167A10;
    private const uint LetterAddress = 0x00167A8F;
    private const uint CardXLowAddress = 0x00167B26;
    private const uint CardXHighAddress = 0x00167B27;
    private const byte FoolCardId = 0x12;
    private const byte FoolCardXLow = 0x1C;
    private const byte FoolCardXHigh = 0x05;
    private const uint WorldCardMarkerAddress = LetterAddress; // World uses 0x17 at 0x00167A8F.
    private const byte WorldCardMarkerValue = 0x17;
    private const byte WorldCardXEndLow = 0x39;
    private const byte WorldCardXEndHigh = 0x06;
    private const uint LevelNumberAddress = 0x00167A8D;
    private const uint AnotherWorldAddress = 0x00167A96;
    private const uint DifficultyAddress = 0x00167A91; // Existing difficulty text/color byte; preserve letter-world behavior.
    private const uint TarotGameplayDifficultyAddress = 0x00167A8C; // Actual Normal/Expert gameplay setting for Tarot card levels.
    private const byte TarotGameplayNormalValue = 0x00;
    private const byte TarotGameplayExpertValue = 0x01;
    private const byte OriginalValue = 0x00;
    private const byte AnotherWorldValue = 0x01;
    private const byte NormalValue = 0x01;
    private const byte ExpertValue = 0x02;
    private const uint FacingAddress = 0x00167D3A;
    private const uint ControllerInputAddress = 0x0016BCEB;
    private const uint ControllerVerticalInputAddress = 0x0016BCEA;
    private const uint ActiveCharacterAddress = 0x00167A45;
    private const uint ContestEnemyCharacterAddress = 0x00167A46;
    private const uint VsStoryEnemyIdAddress = 0x00167A8A;
    private const uint VsStorySpecialEnemyReturnAddress = 0x00167A8A;
    private const uint ContestHudRoundLabelAddress = 0x00167310; // Six-byte ROUND label; VS/Contest displays " WINS".
    private const uint ContestHudEnemyMarkerAddress = 0x00167A8A; // First-opponent marker bytes are reset together to FF.
    private const uint ContestHudEnemyMarkerSecondAddress = 0x00167A8B;
    private const uint VersusResultAnimationAddress = 0x00192EBF; // 07 = victory animation; 08 = defeat animation; remote trap signal = 06.
    private const uint RemoteGoodItemSignalAddress = 0x00192F53;
    private const byte RemoteGoodItemSignalValue = 0x0A;
    private const byte RemoteTrapItemSignalValue = 0x06;
    private static readonly TimeSpan RemoteItemSignalPulseDelay = TimeSpan.FromMilliseconds(50);
    private const uint SecretVsStoryFlagAddress = 0x00167A6E;
    private const uint SecretAttackTypeAddress1 = 0x00194BFE;
    private const uint SecretAttackTypeAddress2 = 0x00194BC6;
    private const byte SecretEnemyId = 0x10;
    private const uint ContestWinsAddress = 0x00167A86; // Consecutive VS/Contest victories.
    private const uint NextBubbleAddress = 0x00188BE0;
    private const uint PointerGuideAddress = 0x00192EFC;
    private const uint BoardTypeAddress = 0x00167AE0; // 00 = Simple, 01 = Wide, 02 = Continuous, 04 = VS.
    private const uint PointerGuideCeilingSlotAddress = 0x00191CF4;
    private const uint PointerGuideWideCeilingSlotAddress = 0x00191D84;
    private const uint FallingCeilingAddress = 0x00192EBD;
    private const uint VsFallingCeilingAddress = 0x00192F76;
    private const uint VsLevelSkipAddress = 0x00192E32;
    private const uint VsLevelSkipTriggerAddress = 0x00192DBA;
    private const uint ContestEnemyDifficultyAddress = 0x00192E50;
    private const uint PackyPlayerAttackTypeAddress = 0x00192EBE;
    private const uint PackyEnemyAttackTypeAddress = 0x00192D7A;
    private const uint PackyPlayerDamageAddress = 0x00192E32;
    private const uint PackyEnemyDamageAddress = 0x00192F76;
    private const byte PackyCharacterId = 0x0D;
    private const byte PackyLowAttackType = 0x03;
    private const byte PackyHighAttackType = 0x0D;
    private const uint TimerAddress = 0x00167D62;
    private const uint CharacterUnlockVisibilityAddress = 0x00167D03; // Keep all characters visible.
    private const uint CharacterUnlockFlagsStartAddress = 0x00187D2C; // 16 character unlock flags, through 0x00187D3B.
    // These flags are recalculated from the current mode and the server-generated seed options.
    private static bool ForceUnlockAllVsCharacters = true;
    private static bool ForceUnlockAllVsStoryEnemies = true;
    // Server-confirmed completion cache for the VS/Story Simple enemy checks.
    private static bool _vsStorySimpleAllEnemyChecksConfirmed;
    // Game-mode classification discovered by memory analysis.
    private const uint PuzzleVsModeAddress = 0x00167D5B; // 00 = Puzzle, 01 = VS
    private const uint StoryContestModeAddress = 0x00167D5A; // 00 = Arcade/Story, 01 = Story/Contest
    private const byte TimerFreezeValue = 220; // Decimal 220 = 0xDC
    private static readonly TimeSpan TimerWriteInterval = TimeSpan.FromSeconds(2);
    private const byte FacingLeft = 0x00;
    private const byte FacingRight = 0x01;
    private const byte MenuLetterSelectMode = 0x01;
    private const byte InLevelMode = 0x03;
    private const byte EnteredLetter1State = 0x05;
    private const byte InputNeutral = 0xFF;
    private const byte InputL1 = 0xFB;
    private const byte InputR1 = 0xF7;
    private const byte InputL2 = 0xFE;
    private const byte InputR2 = 0xFD;
    private const byte InputX = 0xBF; // 0x0016BCEB reports BF while X is pressed in VS/Story enemy selection.
    private const byte InputLeft = 0x7F; // 0x0016BCEA reports 7F while Left is pressed.
    private const byte InputRight = 0xDF; // 0x0016BCEA reports DF while Right is pressed.
    private const byte InputUp = 0xEF;
    private const byte InputDown = 0xBF;
    private const byte InputStart = 0xF7; // 0x0016BCEA reports F7 while Start is pressed.

    private sealed record Menu(string Name, byte[] X, byte[] Y);

    // A and B are available from the start. Every other world stays locked until
    // its corresponding "Unlock <letter> world" item is received from Archipelago.
    private static readonly HashSet<string> LockedLetters = new(StringComparer.OrdinalIgnoreCase)
    {
        "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N",
        "O", "P", "Q", "R", "S", "T", "U", "V", "W", "?", "X", "Y", "Z"
    };

    private static readonly Menu[] Menus =
    [
        new("Menu0", [0xFF,0x00,0x00], [0x00,0x00]),
        new("A",     [0xFE,0xFF,0xC0], [0x00,0x38]),
        new("B",     [0xFE,0x00,0x40], [0x00,0x38]),
        new("C",     [0xFE,0xFF,0x80], [0x00,0x70]),
        new("D",     [0xFF,0x00,0x00], [0x00,0x70]),
        new("E",     [0xFF,0x80,0x80], [0x00,0x70]),
        new("F",     [0xFE,0xFF,0x40], [0xA0,0x00]),
        new("G",     [0xFE,0xFF,0xC0], [0xA0,0x00]),
        new("H",     [0xFE,0x00,0x40], [0xA0,0x00]),
        new("I",     [0xFE,0x00,0xC0], [0xA0,0x00]),
        new("J",     [0xFE,0xFE,0x00], [0xDD,0x00]),
        new("K",     [0xFE,0xFF,0x80], [0xDD,0x00]),
        new("L",     [0xFF,0x00,0x00], [0xDD,0x00]),
        new("M",     [0xFF,0x80,0x80], [0xDD,0x00]),
        new("N",     [0xFF,0xFF,0xFF], [0xDD,0x00]),
        new("O",     [0xFE,0xFE,0xC0], [0x18,0x01]),
        new("P",     [0xFE,0xFF,0x40], [0x18,0x01]),
        new("Q",     [0xFE,0xFF,0xC0], [0x18,0x01]),
        new("R",     [0xFE,0x00,0x40], [0x18,0x01]),
        new("S",     [0xFE,0x00,0xC0], [0x18,0x01]),
        new("T",     [0xFE,0x01,0x40], [0x18,0x01]),
    ];

    private static int _menuIndex;
    private static DuckstationMemory? _gdb;
    private static ArchipelagoSession? _apSession;
    private static DeathLinkService? _deathLinkService;
    private static readonly ConcurrentQueue<DeathLink> _incomingDeathLinks = new();
    private static bool _deathLinkEnabled;
    private static string _slotName = string.Empty;
    private static int? _localPlayerSlot;
    private sealed record QueuedReceivedItem(string Name, string? StableKey, int? SenderSlot);
    private static readonly ConcurrentQueue<QueuedReceivedItem> _receivedItemQueue = new();
    private sealed class LevelSkipSaveData
    {
        public int AvailableUses { get; set; }
        public List<string> ProcessedItemKeys { get; set; } = new();
    }
    private static readonly HashSet<string> _processedLevelSkipItemKeys = new(StringComparer.Ordinal);
    private static string? _levelSkipSavePath;
    private static string? _legacyLevelSkipSavePath;
    private static bool _levelSkipSaveLoaded;
    private static readonly byte[] LevelSkipSaveEntropy = System.Text.Encoding.UTF8.GetBytes("PuzzleBobble4ArchipelagoClient.LevelSkipState.v1");
    // One-slot gameplay item delay: the newest gameplay-effect item replaces any older pending one.
    private static string? _pendingDelayedGameplayItem;
    // Pointer Guide remains pending while result animations or invalid level states prevent application.
    private static bool _pendingPointerGuide;
    private static readonly TimeSpan DuckstationRetryInterval = TimeSpan.FromSeconds(5);
    private static LevelModePlan? _modePlan;
    private static Dictionary<string, string> _tarotDifficultyPlan = new(StringComparer.OrdinalIgnoreCase);
    private static string? _lastTarotGameplayDifficultyKey;
    private static Dictionary<string, string> _localItemPlan = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _nextReconnectAttempt = DateTime.MinValue;
    private static bool _sentInitialContinue;
    private static DateTime _lastAction = DateTime.MinValue;
    private enum ContestEnemyRandomizerState
    {
        NeedInitialEnemy,
        Fighting,
        WaitingForNextEnemyAfterVictory,
        HoldAfterLoss
    }

    private static ContestEnemyRandomizerState _contestEnemyRandomizerState = ContestEnemyRandomizerState.NeedInitialEnemy;
    private static readonly List<byte> _contestEnemyPool = Enumerable.Range(0, 16).Select(id => (byte)id).ToList();
    private static byte? _contestEnemyIdBeingFought;
    private static bool _contestRandomizerModeActive;
    private static bool _contestFirstOpponentMarkersPending = true;
    private static byte? _previousLevelState;
    private static bool _deathLinkSentForCurrentDefeat;
    private static bool _suppressDeathLinkUntilGameplayResumes;
    private static bool _deathLinkSuppressionSawGameplayExit;
    private static byte? _previousGameMode;
    private static string? _previousDetectedGameModeLabel;
    private static byte? _previousLetterValue;
    private static byte? _previousFacing;
    private static byte? _previousControllerInput;
    private static byte? _previousVerticalControllerInput;
    private static bool _puzzleStoryCardSelectionActive;
    // VS/Story enemy selection is activated only by the observed 06 -> 08 transition.
    private static bool _vsStoryEnemySelectionActive;
    private static bool _vsStoryXConfirmSeen;
    // Secret-specific VS/Story fix. Restore the flag only if this client changed it.
    private static bool _secretFlagChangedByClient;
    private static bool _vsStoryHudLayoutActive;
    // Exact original RAM snapshot supplied by the user: 0x0011A000-0x0011A10F.
    // The two skip counters are overwritten with the live count after restoration.
    private static readonly byte[] VsStoryHudResetSnapshot =
    {
        0x20, 0x53, 0x6B, 0x69, 0x70, 0x73, 0x20, 0x31, 0x31, 0x00, 0x00, 0x00, 0x00, 0x20, 0x20, 0x20,
        0x20, 0x53, 0x6B, 0x69, 0x70, 0x73, 0x20, 0x31, 0x31, 0x00, 0x00, 0x00, 0x20, 0x20, 0x20, 0x20,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x50, 0x55, 0x53,
        0x48, 0x20, 0x53, 0x54, 0x41, 0x52, 0x54, 0x20, 0x42, 0x55, 0x54, 0x54, 0x4F, 0x4E, 0x20, 0x20,
        0x00, 0x00, 0x00, 0x00, 0x20, 0x20, 0x20, 0x20, 0x20, 0x44, 0x45, 0x4D, 0x4F, 0x20, 0x50, 0x4C,
        0x41, 0x59, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x00, 0xB8,
        0x10, 0x20, 0x00, 0x1F, 0x70, 0x01, 0x60, 0x2F, 0x00, 0x1F, 0x70, 0x30, 0x60, 0x30, 0x00, 0x1F,
        0x70, 0x60, 0x60, 0x30, 0x00, 0x1F, 0x08, 0x20, 0x60, 0x30, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00,
        0xFF, 0x00, 0x00, 0x09, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06, 0x04, 0x00, 0x00, 0x00,
        0xFF, 0x00, 0x02, 0x06, 0x00, 0x04, 0x03, 0x00, 0x00, 0x02, 0x01, 0x08, 0x08, 0x05, 0x03, 0x00,
        0xFF, 0x00, 0x03, 0x01, 0x00, 0x05, 0x02, 0x00, 0x00, 0x00, 0x03, 0x06, 0x04, 0x02, 0x00, 0x00,
        0xFF, 0x00, 0x02, 0x06, 0x08, 0x04, 0x03, 0x00, 0x00, 0x02, 0x01, 0x00, 0x00, 0x05, 0x03, 0x00,
        0xFF, 0x00, 0x00, 0x01, 0x00, 0x05, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0xFF, 0x00, 0x09, 0x00,
        0x00, 0x00, 0x09, 0x00, 0x02, 0x02, 0x07, 0x08, 0x07, 0x08, 0x02, 0x02, 0xFF, 0x02, 0x00, 0x07,
        0x00, 0x08, 0x00, 0x02, 0x00, 0x02, 0x08, 0x00, 0x00, 0x07, 0x02, 0x00, 0xFF, 0x08, 0x07, 0x00,
        0x00, 0x00, 0x08, 0x07, 0x07, 0x02, 0x08, 0x00, 0x00, 0x07, 0x02, 0x08, 0xFF, 0x08, 0x07, 0x02,
        0x00, 0x02, 0x08, 0x07, 0x00, 0x02, 0x08, 0x00, 0x00, 0x07, 0x02, 0x00, 0x05, 0x0C, 0x01, 0x59,
    };
    // Tracks newly dealt damage only while the player is Tamtam in an active VS/Story fight.
    private static byte? _previousTamtamOutgoingDamageValue;
    private static bool? _lastTamtamBuffActiveForDebug;
    private static VsStoryOptions _vsStoryOptions = new();
    private static GameModesOptions _gameModesOptions = new();
    private static ContestOptions _contestOptions = new();
    private static OtherOptions _otherOptions = new();
    private static bool _contestDifficultyTracking;
    private static byte _contestDifficultyBaseValue;
    private static byte _contestDifficultyLastAppliedValue;
    private static bool? _previousContestModeActive;
    private static byte? _previousContestWins;
    private static byte? _previousContestCharacter;
    private static bool _contestWinConfirmationPending;
    private static bool _initialLevelSkipsApplied;
    // Keys are either "single:N" or "characterId:N" so checks remain unique per track.
    private static readonly HashSet<string> _contestWinChecksSent = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isVsModeActive;
    private static bool? _previousVersusModeActive;
    private static readonly HashSet<byte> _receivedVsStoryCharacterUnlockIds = new();
    private static readonly HashSet<byte> _receivedVsContestCharacterUnlockIds = new();
    private static bool _receivedVsStoryGlobalUnlock;
    private static bool _receivedVsContestGlobalUnlock;
    private static readonly HashSet<byte> _serverSelectedVsStoryCharacterIds = new();
    private static readonly HashSet<byte> _serverSelectedContestCharacterIds = new();
    private static readonly HashSet<string> _requiredGoalVictoryItems = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _receivedGoalVictoryItems = new(StringComparer.OrdinalIgnoreCase);
    private static string _serverGoalType = string.Empty;
    private static readonly HashSet<string> _requiredGoalCheckLocations = new(StringComparer.OrdinalIgnoreCase);
    private static bool _currentVsStoryMode;
    private static bool _currentVsContestMode;
    private sealed record VsSelectableCharacter(string Name, byte Id);
    private static readonly VsSelectableCharacter[] VsSelectableCharacters =
    {
        new("Bob", 0x00),
        new("Alkanet", 0x01),
        new("Marino", 0x02),
        new("Kurol", 0x03),
        new("Tamtam", 0x04),
        new("Cleon", 0x05),
        new("G", 0x06),
        new("Bramb", 0x07),
        new("Develon", 0x08),
        new("Gigant", 0x09),
        new("Madam Luna", 0x0A),
        new("Dreg", 0x0B),
        new("Woolen", 0x0C),
        new("Packy", 0x0D),
        new("Monsta", 0x0E),
        new("Maita", 0x0F),
    };
    private static readonly Dictionary<string, byte> VsCharacterIdsByName = VsSelectableCharacters
        .Concat(new[] { new VsSelectableCharacter("Bub", 0x00) })
        .ToDictionary(character => character.Name, character => character.Id, StringComparer.OrdinalIgnoreCase);
    private static bool? _previousPuzzleArcadeMode;
    // Prevent repeated reset writes while VS/Story credits remain on screen.
    private static bool _vsStoryExtraCharacterCreditsResetTriggered;
    private static int _vsStorySelectedEnemyIndex;
    // After a confirmed VS/Story victory, restore the defeated opponent when the selection screen returns.
    private static byte? _vsStoryReturnEnemyId;
    // CanFight-style enemy progression for VS/Story. The temporary force-unlock
    // setting below bypasses this set without deleting the future locking mechanic.
    private static readonly HashSet<byte> _unlockedVsStoryEnemyIds = new() { 0x00 };
    private sealed record VsStoryEnemy(string Name, byte EnemyId, byte PlayerXLow, byte PlayerXHigh, byte PlayerYLow, byte PlayerYHigh, byte CameraXLow, byte CameraXHigh, byte CameraYLow, byte CameraYHigh);
    private static readonly VsStoryEnemy[] VsStoryEnemies =
    {
        new("Bob",        0x00, 0x50, 0x00, 0xB0, 0x01, 0x00, 0x00, 0xF8, 0x00),
        new("Alkanet",    0x01, 0x20, 0x00, 0x22, 0x01, 0x00, 0x00, 0xB1, 0x00),
        new("Marino",     0x02, 0x37, 0x00, 0xA2, 0x00, 0x0B, 0x00, 0x71, 0x00),
        new("Kurol",      0x03, 0x86, 0x00, 0x1E, 0x01, 0x27, 0x00, 0xAF, 0x00),
        new("Tamtam",     0x04, 0xFE, 0x00, 0x20, 0x01, 0x3B, 0x00, 0xF8, 0x00),
        new("Cleon",      0x05, 0xC8, 0x00, 0x9E, 0x01, 0x8C, 0x00, 0xE7, 0x00),
        new("G",          0x06, 0x46, 0x01, 0xA0, 0x01, 0x4F, 0x00, 0xF8, 0x00),
        new("Bramb",      0x07, 0x8E, 0x01, 0x68, 0x01, 0x93, 0x00, 0xD4, 0x00),
        new("Develon",    0x08, 0x9F, 0x01, 0x21, 0x01, 0xA7, 0x00, 0xD4, 0x00),
        new("Gigant",     0x09, 0x12, 0x01, 0xC1, 0x00, 0x59, 0x00, 0x90, 0x00),
        new("Secret",     0x10, 0x10, 0x01, 0x70, 0x00, 0x70, 0x00, 0x30, 0x00),
        new("Madam Luna", 0x0A, 0x7E, 0x01, 0xB0, 0x00, 0x97, 0x00, 0x78, 0x00),
    };
    // The Extra enemy option controls whether Secret appears in the selectable enemy list.
    private static VsStoryEnemy[] _activeVsStoryEnemies = VsStoryEnemies;
    // Dreg (enemy ID 0x0B) is fought automatically after Madam Luna. Keep him
    // out of VsStoryEnemies so he cannot appear in the selectable enemy list.
    private static readonly Dictionary<byte, string> VsStoryEnemyNamesById =
        VsStoryEnemies.ToDictionary(enemy => enemy.EnemyId, enemy => enemy.Name)
            .Concat(new[] { new KeyValuePair<byte, string>(0x0B, "Dreg") })
            .ToDictionary(pair => pair.Key, pair => pair.Value);
    private static bool _puzzleStoryCardMovePending;
    private static int _puzzleStorySelectedCardIndex;
    private static readonly HashSet<string> _puzzleStoryChecksLogged = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<byte> _puzzleStoryCompletedCards = new();
    private static readonly HashSet<string> _puzzleStoryCompletedLevels = new(StringComparer.OrdinalIgnoreCase);
    // Puzzle/Arcade letters are removed from selection after their Letter-5 check; A and B remain selectable.
    private static readonly HashSet<string> _puzzleArcadeCompletedLetters = new(StringComparer.OrdinalIgnoreCase);
    private static string _localGoal = "One_u-z";
    private static readonly HashSet<string> _localGoalLetters = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _localCompletedGoalLetters = new(StringComparer.OrdinalIgnoreCase);
    // Local development: Fool is always unlocked; every other Puzzle/Story card
    // remains locked until explicit unlock-item handling is added with Archipelago.
    private static readonly HashSet<byte> _puzzleStoryUnlockedCards = new() { FoolCardId };
    private static bool _puzzleStoryWorldXInitialized;
    private static bool _resetLetterOnSelectionChange;
    private static bool _letterResetPending;
    private static readonly ConcurrentDictionary<string, byte> _checksLoggedThisRun = new(StringComparer.OrdinalIgnoreCase);
    // Server-confirmed checks only. Used to allow manual, free R2 skips for completed levels.
    private static readonly ConcurrentDictionary<string, byte> _confirmedChecks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _letter1ModesApplied = new(StringComparer.OrdinalIgnoreCase);
    private static string? _pendingLetter1Target;
    // Keep the selected gameplay letter because the game resets LetterAddress to 00 during the completion transition.
    private static string? _currentGameplayLetter;
    private static bool _skipHudInitialized;
    private static DateTime _nextTimerWriteAt = DateTime.MinValue;
    private static int _levelSkipCount;
    private static readonly HashSet<string> _goalLetters = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _receivedVictoryLetters = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> _receivedUnlockLetters = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, HashSet<string>> _unlockPrerequisites = new(StringComparer.OrdinalIgnoreCase);
    private static bool _goalCompletionHandled;

    private static void ValidateBinaryOption(string name, int value)
    {
        if (value != 0 && value != 1)
            throw new InvalidDataException($"{name} must be 0 (off) or 1 (on).");
    }

    private static void Main()
    {
        // Remove a stale diagnostic log from older versions; this version never writes one.
        try { File.Delete(Path.Combine(AppContext.BaseDirectory, "debug.log")); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        try { LevelModeGenerator.ClearExportedPlan(AppContext.BaseDirectory); }
        catch (IOException ex) { WriteClientLine($"WARNING: Could not clear previous level-mode plan files: {ex.Message}"); }
        EnableConsoleColors();
        Console.Title = LocalDevelopmentMode ? "Puzzle Bobble 4 Local Client" : "Puzzle Bobble 4 Archipelago Client";

        if (LocalDevelopmentMode)
        {
            try
            {
                _vsStoryOptions = LocalRandomizer.LoadOrCreate("vs-story-options.json", new VsStoryOptions());
                _gameModesOptions = LocalRandomizer.LoadOrCreate("game-modes.json", new GameModesOptions());
                _contestOptions = LocalRandomizer.LoadOrCreate("contest-options.json", new ContestOptions());
                _otherOptions = LocalRandomizer.LoadOrCreate("other.json", new OtherOptions());
                ValidateBinaryOption("extra_enemy", _vsStoryOptions.ExtraEnemy);
                ValidateBinaryOption("buff_tamtam", _vsStoryOptions.BuffTamtam);
                if (_vsStoryOptions.VsStoryCharacters < 3 || _vsStoryOptions.VsStoryCharacters > 16)
                    throw new InvalidDataException("vs_story_characters must be between 3 and 16.");
                ValidateBinaryOption("puzzle_arcade", _gameModesOptions.PuzzleArcade);
                ValidateBinaryOption("puzzle_arcade_simple", _gameModesOptions.PuzzleArcadeSimple);
                if (_gameModesOptions.PuzzleArcade == 1 && _gameModesOptions.PuzzleArcadeSimple == 1)
                    throw new InvalidDataException("Cannot enable both Puzzle/Arcade Complete and Puzzle/Arcade Simple.");
                ValidateBinaryOption("puzzle_story", _gameModesOptions.PuzzleStory);
                ValidateBinaryOption("puzzle_story_complete", _gameModesOptions.PuzzleStoryComplete);
                ValidateBinaryOption("puzzle_story_simple", _gameModesOptions.PuzzleStorySimple);
                if (_gameModesOptions.PuzzleStoryComplete == 1 && _gameModesOptions.PuzzleStorySimple == 1)
                    throw new InvalidDataException("Cannot enable both Puzzle/Story Complete and Puzzle/Story Simple.");
                ValidateBinaryOption("vs_story_simple", _gameModesOptions.VsStorySimple);
                ValidateBinaryOption("vs_story_complete", _gameModesOptions.VsStoryComplete);
                ValidateBinaryOption("vs_contest_simple", _gameModesOptions.VsContestSimple);
                ValidateBinaryOption("vs_contest_complete", _gameModesOptions.VsContestComplete);
                if (_gameModesOptions.VsStorySimple == 1 && _gameModesOptions.VsStoryComplete == 1)
                    throw new InvalidDataException("Cannot enable both vs_story_simple and vs_story_complete.");
                if (_gameModesOptions.VsContestSimple == 1 && _gameModesOptions.VsContestComplete == 1)
                    throw new InvalidDataException("Cannot enable both vs_contest_simple and vs_contest_complete.");
                if (_contestOptions.CustomContestWins < 3 || _contestOptions.CustomContestWins > 30)
                    throw new InvalidDataException("Custom_contest_wins must be between 3 and 30.");
                if (_contestOptions.ContestCharacters < 3 || _contestOptions.ContestCharacters > 16)
                    throw new InvalidDataException("contest_characters must be between 3 and 16.");
                if (_contestOptions.IncreaseDifficultyContest < 0 || _contestOptions.IncreaseDifficultyContest > 200)
                    throw new InvalidDataException("increase_difficulty_contest must be between 0 and 200.");
                if (_otherOptions.FixPacky != 0 && _otherOptions.FixPacky != 1)
                    throw new InvalidDataException("fix_packy must be 0 (disabled) or 1 (enabled).");
            }
            catch (Exception ex)
            {
                WriteClientLine($"Local development configuration error: {ex.GetBaseException().Message}");
                return;
            }
        }
        else
        {
            // No local configuration is loaded in live mode. Zero/neutral values are
            // replaced by authoritative slot_data immediately after AP login.
            _vsStoryOptions = new VsStoryOptions { ExtraEnemy = 0, BuffTamtam = 0, VsStoryCharacters = 3 };
            _gameModesOptions = new GameModesOptions
            {
                PuzzleArcade = 0, PuzzleArcadeSimple = 0, PuzzleStory = 0,
                PuzzleStoryComplete = 0, PuzzleStorySimple = 0,
                VsStorySimple = 0, VsStoryComplete = 0,
                VsContestSimple = 0, VsContestComplete = 0
            };
            _contestOptions = new ContestOptions
            {
                CustomContestWins = 10, ContestCharacters = 3, IncreaseDifficultyContest = 0
            };
            _otherOptions = new OtherOptions { FixPacky = 0 };
        }
        _activeVsStoryEnemies = _vsStoryOptions.ExtraEnemy == 1
            ? VsStoryEnemies
            : VsStoryEnemies.Where(enemy => enemy.EnemyId != SecretEnemyId).ToArray();

        if (LocalDevelopmentMode)
        {
            try
            {
                var randomizer = new LocalRandomizer();
                ItemWeightSettings itemWeights = LocalRandomizer.LoadOrCreate("item-weights.json", new ItemWeightSettings());
                LevelModeWeightSettings modeWeights = LocalRandomizer.LoadOrCreate("level-mode-weights.json", new LevelModeWeightSettings());
                GoalWeightSettings goalWeights = LocalRandomizer.LoadOrCreate("goal-weights.json", new GoalWeightSettings());
                TarotDifficultyWeightSettings tarotDifficultyWeights = LocalRandomizer.LoadOrCreate("tarot-difficulty-weights.json", new TarotDifficultyWeightSettings());
                _localGoal = randomizer.PickGoal(goalWeights);
                _localItemPlan = randomizer.CreateItemPlan(itemWeights);
                _modePlan = randomizer.CreateModePlan(modeWeights);
                string[] tarotLevelIds = PuzzleStoryCards
                    .SelectMany(card => Enumerable.Range(1, 5).Select(level => $"{card.Name} {level}"))
                    .ToArray();
                _tarotDifficultyPlan = randomizer.CreateTarotDifficultyPlan(tarotLevelIds, tarotDifficultyWeights);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "tarot-difficulty-plan.json"),
                    System.Text.Json.JsonSerializer.Serialize(_tarotDifficultyPlan, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                LockedLetters.Clear(); // All worlds are available while testing without Archipelago.
                WriteClientLine("LOCAL DEVELOPMENT MODE: Archipelago is temporarily disabled.");
                WriteClientLine($"Generated local item plan for {_localItemPlan.Count} levels.");
                WriteClientLine($"Generated local level-mode plan for {_modePlan.Assignments.Count} levels.");
                WriteClientLine($"Generated Tarot difficulty plan for {_tarotDifficultyPlan.Count} card levels (Normal/Expert).");
                WriteClientLine("Edit item-weights.json, level-mode-weights.json, goal-weights.json, tarot-difficulty-weights.json, and vs-story-options.json to change settings.");
                if (_localGoal == "One_u-z")
                {
                    string[] candidates = ["U", "V", "W", "?", "X", "Y", "Z"];
                    _localGoalLetters.Add(candidates[Random.Shared.Next(candidates.Length)]);
                    WriteClientLine($"LOCAL GOAL: One_u-z — complete {_localGoalLetters.First()}5 (U-Z includes ?).");
                }
                else if (_localGoal == "All_u-z")
                {
                    foreach (string letter in new[] { "U", "V", "W", "?", "X", "Y", "Z" }) _localGoalLetters.Add(letter);
                    WriteClientLine("LOCAL GOAL: All_u-z — complete U5, V5, W5, ?5, X5, Y5, and Z5.");
                }
                else if (_localGoal == "Only_World") WriteClientLine("LOCAL GOAL: Only_World — complete World 5 in Puzzle/Story.");
                else if (_localGoal == "All_Tarot") WriteClientLine("LOCAL GOAL: All_Tarot — complete all 5 levels of every Tarot card; cards unlock through progression.");
                else if (_localGoal == "vs_story") WriteClientLine("LOCAL GOAL: vs_story — placeholder goal; intended to require VS/Story Complete and the configured number of characters.");
                else if (_localGoal == "vs_contest") WriteClientLine("LOCAL GOAL: vs_contest — complete the final Contest win check with every selected character.");
                WriteClientLine("Type /help to see available local commands.");
            }
            catch (Exception ex)
            {
                WriteClientLine($"Configuration error: {ex.GetBaseException().Message}");
                return;
            }
            StartLocalConsoleCommandReader();
        }

        // Do not ask for Archipelago credentials until DuckStation's RAM is accessible.
        while (_gdb is null)
        {
            try
            {
                _gdb = new DuckstationMemory();
                _previousLevelState = null;
                WriteClientLine("Connected to DuckStation.");
            }
            catch (Exception)
            {
                WriteClientLine("Could not find DuckStation.");
                WriteClientLine("Retrying in 5 seconds...");
                Thread.Sleep(DuckstationRetryInterval);
            }
        }

        if (!LocalDevelopmentMode)
        {
            _apSession = ConnectToArchipelago();
            if (_apSession is null)
            {
                DisconnectMemory();
                return;
            }
        }

        try
        {
            while (true)
            {
                if (_gdb is null)
                {
                    TryReconnect();
                }
                else
                {
                    try
                    {
                        PollGameState();
                    }
                    catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException or UnauthorizedAccessException)
                    {
                        WriteClientLine("Could not find DuckStation.");
                        DisconnectMemory();
                        _nextReconnectAttempt = DateTime.UtcNow.Add(DuckstationRetryInterval);
                        WriteClientLine("Retrying in 5 seconds...");
                    }
                }
                Thread.Sleep(PollInterval);
            }
        }
        finally
        {
            DisconnectMemory();
        }
    }


    // ANSI RGB colors; modern Windows Terminal / PowerShell supports these sequences.
    private const string AnsiReset = "\u001b[0m";
    private const string SelfPlayerColor = "\u001b[38;2;155;45;255m";      // strong violet
    private const string OtherPlayerColor = "\u001b[38;2;235;145;220m";    // soft pink-violet
    private const string FillerColor = "\u001b[38;2;70;220;105m";          // green
    private const string UsefulColor = "\u001b[38;2;50;220;210m";           // turquoise
    private const string ProgressionColor = "\u001b[38;2;255;190;45m";     // gold
    private const string TrapColor = "\u001b[38;2;255;75;75m";             // red
    private const string LocationColor = "\u001b[38;2;80;205;255m";        // cyan
    private const string DeadLinkColor = "\u001b[38;2;255;0;0m";            // intense red
    private const string DeadLinkDisabledColor = "\u001b[38;2;65;105;225m"; // calm royal blue
    private const string LightBlueColor = "\u001b[38;2;100;220;255m";       // light cyan
    private const string VictoryColor = "\u001b[38;2;80;255;95m";           // bright green
    private const string ErrorColor = "\u001b[38;2;255;155;55m";            // orange
    private const string NeutralColor = "\u001b[38;2;255;255;255m";         // white

    private static void WriteColored(string text, string color)
    {
        Console.Write(color);
        Console.Write(text);
        Console.Write(AnsiReset);
    }

    private static void WriteReceivedDataSummary(System.Collections.Generic.IDictionary<string, object> slotData)
    {
        var received = new List<string>();

        if (_gameModesOptions.PuzzleArcade == 1)
        {
            received.Add($"{_modePlan?.Assignments.Count ?? 0} Puzzle/Arcade levels");
        }

        if (_gameModesOptions.PuzzleStory == 1)
            received.Add($"{_tarotDifficultyPlan.Count} Puzzle/Story levels");

        bool vsStoryEnabled = _gameModesOptions.VsStorySimple == 1 || _gameModesOptions.VsStoryComplete == 1;
        if (vsStoryEnabled)
        {
            int enemyCount = 0;
            if (slotData.TryGetValue("vs_story_enemy_names", out object? rawEnemyNames) &&
                EnumerateSlotDataArray(rawEnemyNames) is IEnumerable<object?> enemyNames)
                enemyCount = enemyNames.Count(value => !string.IsNullOrWhiteSpace(Convert.ToString(value)));
            if (enemyCount == 0)
                enemyCount = 13 - (_vsStoryOptions.ExtraEnemy == 1 ? 0 : 1); // Includes the final Dreg match.

            int characterTracks = _gameModesOptions.VsStoryComplete == 1
                ? Math.Max(1, _serverSelectedVsStoryCharacterIds.Count)
                : 1;
            received.Add($"{enemyCount * characterTracks} VS/Story matches");
        }

        bool vsContestEnabled = _gameModesOptions.VsContestSimple == 1 || _gameModesOptions.VsContestComplete == 1;
        if (vsContestEnabled)
        {
            int characterTracks = _gameModesOptions.VsContestComplete == 1
                ? Math.Max(1, _serverSelectedContestCharacterIds.Count)
                : 1;
            received.Add($"{_contestOptions.CustomContestWins * characterTracks} VS/Contest matches");
        }

        if (received.Count > 0)
            WriteClientLine($"Data received successfully from Archipelago: {string.Join(", ", received)}.");
        else
            WriteClientLine("Data received successfully from Archipelago. No gameplay modes are enabled.");
    }

    private static int GoalLetterOrder(string letter) => letter.ToUpperInvariant() switch
    {
        "U" => 0, "V" => 1, "W" => 2, "?" => 3, "X" => 4, "Y" => 5, "Z" => 6,
        _ => 100 + (letter.Length > 0 ? char.ToUpperInvariant(letter[0]) : '\0')
    };

    private static IEnumerable<string> OrderedGoalLetters(IEnumerable<string> letters) =>
        letters.OrderBy(GoalLetterOrder).ThenBy(x => x, StringComparer.OrdinalIgnoreCase);

    private static void WriteGoalSummary()
    {
        string normalizedGoal = _serverGoalType.Trim().Replace('-', '_').Replace(' ', '_');
        if (normalizedGoal.Equals("all_tarot", StringComparison.OrdinalIgnoreCase))
        {
            WriteClientLine($"Your goal is to finish all Tarot cards ({_puzzleStoryCompletedCards.Count}/22).");
        }
        else if (normalizedGoal.Equals("only_world", StringComparison.OrdinalIgnoreCase))
        {
            WriteClientLine("Your goal is to find and finish the Tarot card World.");
        }
        else if (normalizedGoal.Equals("all_u_z", StringComparison.OrdinalIgnoreCase) && _goalLetters.Count > 0)
        {
            WriteClientLine($"Your goal is to finish Worlds {string.Join(", ", OrderedGoalLetters(_goalLetters))}.");
            var remaining = OrderedGoalLetters(_goalLetters.Where(letter => !_receivedVictoryLetters.Contains(letter))).ToArray();
            WriteClientLine($"Worlds remaining: {(remaining.Length > 0 ? string.Join(", ", remaining) : "none")}.");
        }
        else if (_goalLetters.Count == 1)
            WriteClientLine($"Your goal is to finish World {_goalLetters.First()}.");
        else if (_goalLetters.Count > 1)
            WriteClientLine($"Your goal is to finish Worlds {string.Join(", ", OrderedGoalLetters(_goalLetters))}.");
        else if (normalizedGoal.Equals("vs_story", StringComparison.OrdinalIgnoreCase) && _requiredGoalCheckLocations.Count > 0)
        {
            // VS/Story goal progress mirrors the VS/Contest summary: list the
            // selected characters whose required win checks are not confirmed yet.
            var requiredCharacterNames = _requiredGoalCheckLocations
                .Select(name => Regex.Match(name, @"^(?:Defeat .+ as |vs_.+_win_as_)(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .Where(match => match.Success)
                .Select(match => match.Groups[1].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var selectedCharacterNames = _serverSelectedVsStoryCharacterIds
                .Select(GetVsStoryPlayerCharacterName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
            var allCharacterNames = selectedCharacterNames.Length > 0 ? selectedCharacterNames! : requiredCharacterNames;
            var remainingCharacterNames = allCharacterNames
                .Where(characterName => !_requiredGoalCheckLocations.Any(locationName =>
                {
                    Match match = Regex.Match(locationName, @"^(?:Defeat .+ as |vs_.+_win_as_)(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    return match.Success && string.Equals(match.Groups[1].Value.Trim(), characterName, StringComparison.OrdinalIgnoreCase)
                        && _confirmedChecks.ContainsKey(locationName);
                }))
                .ToArray();
            WriteClientLine($"Your goal is to complete VS/Story with: {(remainingCharacterNames.Length > 0 ? string.Join(", ", remainingCharacterNames) : "none (complete)") }.");
        }
        else if (normalizedGoal.Equals("vs_contest", StringComparison.OrdinalIgnoreCase) && _requiredGoalCheckLocations.Count > 0)
        {
            var requiredCharacterNames = _requiredGoalCheckLocations
                .Select(name => Regex.Match(name, @"^Contest Win \d+ as (.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .Where(match => match.Success)
                .Select(match => match.Groups[1].Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var selectedCharacterNames = _serverSelectedContestCharacterIds
                .Select(id => VsSelectableCharacters.FirstOrDefault(character => character.Id == id)?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
            var allCharacterNames = selectedCharacterNames.Length > 0 ? selectedCharacterNames! : requiredCharacterNames;
            var remainingCharacterNames = allCharacterNames
                .Where(characterName => !_requiredGoalCheckLocations.Any(locationName =>
                {
                    Match match = Regex.Match(locationName, @"^Contest Win \d+ as (.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    return match.Success && string.Equals(match.Groups[1].Value.Trim(), characterName, StringComparison.OrdinalIgnoreCase)
                        && _confirmedChecks.ContainsKey(locationName);
                }))
                .ToArray();
            WriteClientLine($"Your goal is to complete VS/Contest with: {(remainingCharacterNames.Length > 0 ? string.Join(", ", remainingCharacterNames) : "none (complete)")}.");
        }
        else if (_requiredGoalCheckLocations.Count > 0)
            WriteClientLine($"Your goal: {_serverGoalType}; required checks: {_requiredGoalCheckLocations.Count}.");
        else if (_requiredGoalVictoryItems.Count > 0)
            WriteClientLine($"Your goal: {_serverGoalType}; required completion items: {_requiredGoalVictoryItems.Count}.");
        else if (normalizedGoal.Equals("none", StringComparison.OrdinalIgnoreCase))
            WriteClientLine("No victory goal is configured for this seed.");
        else
            WriteClientLine("WARNING: slot_data contains neither goal_letters nor goal_victory_items. Automatic goal completion is disabled.");
    }

    private static void WriteClientLine(string text)
    {
        string lower = text.ToLowerInvariant();

        if (lower.StartsWith("received item: level skip."))
        {
            // Level Skip is a Useful item, so use the same turquoise as its category.
            WriteColored(text, UsefulColor);
        }
        else if (lower.StartsWith("level skip used. remaining uses:"))
        {
            WriteColored(text, LightBlueColor);
        }
        else if (lower.StartsWith("death link enabled for this seed."))
        {
            WriteColored(text, DeadLinkColor);
        }
        else if (lower.StartsWith("death link is disabled for this seed."))
        {
            WriteColored(text, DeadLinkDisabledColor);
        }
        else if (lower.StartsWith("level skips available:"))
        {
            WriteColored(text, LightBlueColor);
        }
        else if (lower.StartsWith("level mode plan received from archipelago:"))
        {
            WriteColored(text, LocationColor);
        }
        else if (lower.StartsWith("world unlocked:"))
        {
            WriteColored(text, VictoryColor);
        }
        else if (lower.Contains("dead link"))
        {
            WriteColored(text, DeadLinkColor);
        }
        else if (lower.StartsWith("your goal") || lower.StartsWith("local goal:") ||
                 lower.StartsWith("goal of this run:") || lower.Contains("completed their goal") ||
                 lower.Contains("goal complete") || lower.Contains("game completed") ||
                 lower.StartsWith("victory:"))
        {
            // Every goal-related status uses green, including goals that do not
            // contain the word "victory" (for example, the Worlds U-Z goal).
            WriteColored(text, VictoryColor);
        }
        else if (lower.StartsWith("data received successfully from archipelago:") ||
                 lower.StartsWith("data received successfully from archipelago.") ||
                 lower.StartsWith("server game modes:") ||
                 lower.StartsWith("server options applied:") ||
                 lower.StartsWith("enabled modes:") ||
                 lower.StartsWith("enabled options:") ||
                 lower.StartsWith("level mode plan received from archipelago:"))
        {
            // Archipelago-provided configuration and received-data summaries use cyan.
            WriteColored(text, LightBlueColor);
        }
        else if (lower.Contains("error") || lower.Contains("warning") ||
                 lower.Contains("could not") || lower.Contains("invalid") ||
                 lower.Contains("failed") || lower.Contains("exception") ||
                 lower.Contains("cannot be empty") || lower.Contains("retrying"))
        {
            WriteColored(text, ErrorColor);
        }
        else if (text.StartsWith("Archipelago confirmed check: ", StringComparison.OrdinalIgnoreCase))
        {
            const string prefix = "Archipelago confirmed check: ";
            WriteColored(text[..prefix.Length], NeutralColor);
            WriteColored(text[prefix.Length..], LocationColor);
        }
        else if (lower.Contains("connected") || lower.Contains("disconnected") ||
                 lower.Contains("synced") || lower.Contains("reconnect"))
        {
            // Only connection/status messages are forced to white. Item and player
            // categories in ordinary Archipelago messages keep their own colors.
            WriteColored(text, NeutralColor);
        }
        else
        {
            Console.Write(text);
        }

        Console.WriteLine();
    }

    private static bool IsTrapItemName(string lowerItem)
    {
        // Archipelago trap items in Puzzle Bobble 4 do not all contain the word "trap".
        return lowerItem.Contains("trap") ||
               lowerItem.Contains("rainbow bubble") ||
               lowerItem.Contains("obstacle block") ||
               lowerItem.Contains("random next color") ||
               lowerItem.Contains("random color bubble") ||
               lowerItem.Contains("falling ceiling");
    }

    private static void WriteArchipelagoMessage(string text)
    {
        // Color outgoing item transfers too, including items sent to other games
        // through multiworld. Example: "Player1 sent T Tetromino to HishTETRIS (L3)".
        Match sent = Regex.Match(text,
            @"^(?<player>.+?) sent (?<item>.+?) to (?<recipient>.+?)(?: \((?<location>[^()]*)\))?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (sent.Success)
        {
            string senderName = sent.Groups["player"].Value;
            string senderColor = string.Equals(senderName, _slotName, StringComparison.OrdinalIgnoreCase)
                ? SelfPlayerColor : OtherPlayerColor;
            WriteColored(senderName, senderColor);
            Console.Write(" sent ");

            string itemName = sent.Groups["item"].Value;
            string lowerItem = itemName.ToLowerInvariant();
            string itemColor = IsTrapItemName(lowerItem) ? TrapColor
                : lowerItem.Contains("progressive enemy") || lowerItem.Contains("unlock") || lowerItem.Contains("victory") ? ProgressionColor
                : lowerItem.Contains("level skip") || lowerItem.Contains("pointer-guide") ||
                  lowerItem.Contains("star-bubble") || lowerItem.Contains("metal-bubble") ? UsefulColor
                : FillerColor;
            WriteColored(itemName, itemColor);
            Console.Write(" to ");
            WriteColored(sent.Groups["recipient"].Value, OtherPlayerColor);
            if (sent.Groups["location"].Success)
            {
                Console.Write(" (");
                WriteColored(sent.Groups["location"].Value, LocationColor);
                Console.Write(")");
            }
            Console.WriteLine();
            return;
        }

        // Keep Archipelago's category colors for item-found messages while never
        // printing the private level_item_plan or its weights.
        // Example: "Player1 found their Unlock S world (A2)"
        Match found = Regex.Match(text,
            @"^(?<player>.+?) found their (?<item>.+?) \((?<location>[^()]*)\)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (found.Success)
        {
            string foundPlayer = found.Groups["player"].Value;
            string foundPlayerColor = string.Equals(foundPlayer, _slotName, StringComparison.OrdinalIgnoreCase)
                ? SelfPlayerColor : OtherPlayerColor;
            WriteColored(foundPlayer, foundPlayerColor);
            Console.Write(" found their ");
            string itemName = found.Groups["item"].Value;
            string lowerItem = itemName.ToLowerInvariant();
            string itemColor = IsTrapItemName(lowerItem) ? TrapColor
                : lowerItem.Contains("progressive enemy") || lowerItem.Contains("unlock") || lowerItem.Contains("victory") ? ProgressionColor
                : lowerItem.Contains("level skip") || lowerItem.Contains("pointer-guide") ||
                  lowerItem.Contains("star-bubble") || lowerItem.Contains("metal-bubble") ? UsefulColor
                : FillerColor;
            WriteColored(itemName, itemColor);
            Console.Write(" (");
            WriteColored(found.Groups["location"].Value, LocationColor);
            Console.WriteLine(")");
            return;
        }

        // Some server versions phrase the same event as "Player found X at Y".
        Match foundAt = Regex.Match(text,
            @"^(?<player>.+?) found (?<item>.+?) at (?<location>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (foundAt.Success)
        {
            string foundAtPlayer = foundAt.Groups["player"].Value;
            string foundAtPlayerColor = string.Equals(foundAtPlayer, _slotName, StringComparison.OrdinalIgnoreCase)
                ? SelfPlayerColor : OtherPlayerColor;
            WriteColored(foundAtPlayer, foundAtPlayerColor);
            Console.Write(" found ");
            string itemName = foundAt.Groups["item"].Value;
            string lowerItem = itemName.ToLowerInvariant();
            string itemColor = IsTrapItemName(lowerItem) ? TrapColor
                : lowerItem.Contains("progressive enemy") || lowerItem.Contains("unlock") || lowerItem.Contains("victory") ? ProgressionColor
                : lowerItem.Contains("level skip") || lowerItem.Contains("pointer-guide") ||
                  lowerItem.Contains("star-bubble") || lowerItem.Contains("metal-bubble") ? UsefulColor
                : FillerColor;
            WriteColored(itemName, itemColor);
            Console.Write(" at ");
            WriteColored(foundAt.Groups["location"].Value, LocationColor);
            Console.WriteLine();
            return;
        }

        // Fall back to normal message handling; connection messages alone are white.
        WriteClientLine(text);
    }

    private static bool TryReadSlotDataBoolean(System.Collections.Generic.IDictionary<string, object> slotData, string key, out bool value)
    {
        value = false;
        if (!slotData.TryGetValue(key, out object? raw) || raw is null)
            return false;
        if (raw is bool boolean)
        {
            value = boolean;
            return true;
        }
        string text = Convert.ToString(raw)?.Trim() ?? string.Empty;
        if (bool.TryParse(text, out boolean))
        {
            value = boolean;
            return true;
        }
        if (int.TryParse(text, out int numeric))
        {
            value = numeric != 0;
            return true;
        }
        return false;
    }

    private static bool TryGetSlotDataValue(object? source, string key, out object? value)
    {
        // Archipelago clients may deserialize nested slot_data maps with different
        // dictionary implementations and key casing. Treat option names
        // case-insensitively so nested values cannot silently fall back to zero.
        if (source is System.Collections.IDictionary map)
        {
            foreach (System.Collections.DictionaryEntry entry in map)
            {
                if (string.Equals(Convert.ToString(entry.Key), key, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.Value;
                    return true;
                }
            }
        }
        if (source is System.Text.Json.JsonElement json && json.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (System.Text.Json.JsonProperty property in json.EnumerateObject())
            {
                if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }
        value = null;
        return false;
    }

    private static int ReadSlotDataInt(object? raw, int fallback)
    {
        if (raw is null) return fallback;
        try
        {
            if (raw is System.Text.Json.JsonElement json)
            {
                if (json.ValueKind == System.Text.Json.JsonValueKind.Number && json.TryGetInt32(out int number)) return number;
                if (json.ValueKind == System.Text.Json.JsonValueKind.True) return 1;
                if (json.ValueKind == System.Text.Json.JsonValueKind.False) return 0;
                if (json.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(json.GetString(), out number)) return number;
            }
            return Convert.ToInt32(raw);
        }
        catch { return fallback; }
    }

    private static IEnumerable<object?>? EnumerateSlotDataArray(object? raw)
    {
        if (raw is System.Text.Json.JsonElement json && json.ValueKind == System.Text.Json.JsonValueKind.Array)
            return json.EnumerateArray().Select(value => (object?)value).ToArray();
        if (raw is System.Collections.IEnumerable values && raw is not string)
            return values.Cast<object?>().ToArray();
        return null;
    }


    private static IEnumerable<KeyValuePair<string, object?>>? EnumerateSlotDataMap(object? raw)
    {
        if (raw is System.Collections.IDictionary map)
        {
            var entries = new List<KeyValuePair<string, object?>>();
            foreach (System.Collections.DictionaryEntry entry in map)
            {
                string? key = Convert.ToString(entry.Key);
                if (!string.IsNullOrWhiteSpace(key)) entries.Add(new KeyValuePair<string, object?>(key, entry.Value));
            }
            return entries;
        }

        if (raw is System.Text.Json.JsonElement json && json.ValueKind == System.Text.Json.JsonValueKind.Object)
            return json.EnumerateObject()
                .Select(property => new KeyValuePair<string, object?>(property.Name, property.Value))
                .ToArray();

        // Some client-library versions may expose nested slot_data maps through
        // generic KeyValuePair enumerables that do not implement non-generic IDictionary.
        if (raw is System.Collections.IEnumerable sequence && raw is not string)
        {
            var entries = new List<KeyValuePair<string, object?>>();
            foreach (object? item in sequence)
            {
                if (item is null) continue;
                var type = item.GetType();
                var keyProperty = type.GetProperty("Key") ?? type.GetProperty("Name");
                var valueProperty = type.GetProperty("Value");
                if (keyProperty is null || valueProperty is null) continue;
                string? key = Convert.ToString(keyProperty.GetValue(item));
                if (!string.IsNullOrWhiteSpace(key))
                    entries.Add(new KeyValuePair<string, object?>(key, valueProperty.GetValue(item)));
            }
            if (entries.Count > 0) return entries;
        }

        return null;
    }

    private static void LoadLevelSkipSave(string domain, int port, string slotName, string seedId)
    {
        // Keep Level Skip state outside the game folder and protect it with Windows DPAPI.
        // The identity uses the Archipelago seed name, not the port, so a restarted server
        // keeps the same state while a newly generated seed gets a fresh counter.
        string safeDomain = string.Concat(domain.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_'));
        string safeSlot = string.Concat(slotName.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
        string safeSeed = string.Concat(seedId.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // Keep this game-specific state directly in Archipelago's existing LocalAppData folder,
        // separate from Cache\datapackage. The filename is seed-specific and port-independent.
        string archipelagoData = Path.Combine(localAppData, "Archipelago");
        Directory.CreateDirectory(archipelagoData);
        _levelSkipSavePath = Path.Combine(archipelagoData, $"level-skips-{safeDomain}-{safeSeed}-{safeSlot}.dat");
        _legacyLevelSkipSavePath = Path.Combine(AppContext.BaseDirectory, $"level-skips-{safeDomain}-{port}-{safeSlot}.json");
        string previousStateDirectory = Path.Combine(localAppData, "PuzzleBobble4ArchipelagoClient", "State");
        _levelSkipSaveLoaded = false;
        _processedLevelSkipItemKeys.Clear();
        string? migratedProtectedPath = null;
        string? migratedLegacyPath = null;
        try
        {
            LevelSkipSaveData? data = null;
            if (File.Exists(_levelSkipSavePath))
            {
                byte[] protectedBytes = File.ReadAllBytes(_levelSkipSavePath);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, LevelSkipSaveEntropy, DataProtectionScope.CurrentUser);
                data = JsonSerializer.Deserialize<LevelSkipSaveData>(plainBytes);
            }
            else
            {
                // One-time migration from the previous port-independent filename. Do not scan
                // all seed-specific saves: doing so could import a different seed's inventory.
                string oldUnseededPath = Path.Combine(archipelagoData, $"level-skips-{safeDomain}-{safeSlot}.dat");
                string previousUnseededPath = Path.Combine(previousStateDirectory, $"level-skips-{safeDomain}-{safeSlot}.dat");
                string? oldProtectedPath = File.Exists(oldUnseededPath) ? oldUnseededPath
                    : File.Exists(previousUnseededPath) ? previousUnseededPath
                    : null;
                if (oldProtectedPath is not null)
                {
                    byte[] protectedBytes = File.ReadAllBytes(oldProtectedPath);
                    byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, LevelSkipSaveEntropy, DataProtectionScope.CurrentUser);
                    data = JsonSerializer.Deserialize<LevelSkipSaveData>(plainBytes);
                    migratedProtectedPath = oldProtectedPath;
                }
                else
                {
                    // Migrate one legacy plaintext JSON matching the current connection. This is
                    // only a compatibility path for old builds; it is removed after successful save.
                    if (!string.IsNullOrWhiteSpace(_legacyLevelSkipSavePath) && File.Exists(_legacyLevelSkipSavePath))
                    {
                        data = JsonSerializer.Deserialize<LevelSkipSaveData>(File.ReadAllText(_legacyLevelSkipSavePath));
                        migratedLegacyPath = _legacyLevelSkipSavePath;
                    }
                }
            }

            if (data is null) return;
            _levelSkipCount = Math.Clamp(data.AvailableUses, 0, 99);
            foreach (string key in data.ProcessedItemKeys ?? new List<string>())
                _processedLevelSkipItemKeys.Add(key);
            _levelSkipSaveLoaded = true;

            // Write the port-independent protected state before removing any migrated old file.
            if (migratedProtectedPath is not null || migratedLegacyPath is not null)
            {
                SaveLevelSkipState();
                try { if (migratedProtectedPath is not null) File.Delete(migratedProtectedPath); } catch { }
                try { if (migratedLegacyPath is not null) File.Delete(migratedLegacyPath); } catch { }
            }
            else if (!string.IsNullOrWhiteSpace(_legacyLevelSkipSavePath) && File.Exists(_legacyLevelSkipSavePath))
            {
                SaveLevelSkipState();
                try { File.Delete(_legacyLevelSkipSavePath); } catch { }
            }
            ClientLog.Debug($"Restored Level Skip state: {_levelSkipCount} available uses; {_processedLevelSkipItemKeys.Count} received skip items already processed.");
        }
        catch (Exception ex)
        {
            ClientLog.Debug($"Could not restore Level Skip state: {ex.Message}");
        }
    }


    private static void SaveLevelSkipState()
    {
        if (string.IsNullOrWhiteSpace(_levelSkipSavePath)) return;
        try
        {
            var data = new LevelSkipSaveData
            {
                AvailableUses = Math.Clamp(_levelSkipCount, 0, 99),
                ProcessedItemKeys = _processedLevelSkipItemKeys.ToList()
            };
            byte[] plainBytes = JsonSerializer.SerializeToUtf8Bytes(data);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, LevelSkipSaveEntropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_levelSkipSavePath, protectedBytes);
            _levelSkipSaveLoaded = true;
        }
        catch (Exception ex)
        {
            ClientLog.Debug($"Could not save Level Skip state: {ex.Message}");
        }
    }

    private static string? BuildReceivedItemKey(object receivedItem)
    {
        // Reflection keeps compatibility with MultiClient.Net ItemInfo versions while using
        // the source location/player/item identity to deduplicate replayed inventory entries.
        string? ReadProperty(string name)
        {
            object? value = receivedItem.GetType().GetProperty(name)?.GetValue(receivedItem);
            return value is null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        string? location = ReadProperty("LocationId");
        string? player = ReadProperty("Player");
        string? item = ReadProperty("ItemId");
        if (string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(item)) return null;
        return $"location:{location}|player:{player ?? "?"}|item:{item}";
    }

    // Read slot identifiers through reflection to remain compatible with MultiClient.Net
    // versions while distinguishing the sending player from this client's local slot.
    private static int? TryReadSlotId(object? source, string propertyName)
    {
        if (source is null)
            return null;

        try
        {
            object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is null)
                return null;
            if (value is int slot)
                return slot;
            return int.TryParse(
                Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int parsed)
                ? parsed
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyServerSlotData(System.Collections.Generic.IDictionary<string, object> slotData)
    {
        if (!_initialLevelSkipsApplied && slotData.TryGetValue("extra_level_skips", out object? rawInitialSkips))
        {
            int initialSkips = Math.Clamp(ReadSlotDataInt(rawInitialSkips, 0), 0, 25);
            if (!_levelSkipSaveLoaded)
            {
                _levelSkipCount = initialSkips;
                SaveLevelSkipState();
            }
            _initialLevelSkipsApplied = true;
            if (_gdb is not null && _skipHudInitialized)
                UpdateSkipCounterHud(_gdb);
            ClientLog.Debug($"Initial Level Skips received from slot data: {initialSkips}; current available uses: {_levelSkipCount}.");
            WriteClientLine($"Level skips available: {_levelSkipCount:00}.");
        }

        object? rawModes = slotData.TryGetValue("game_modes", out object? modesValue) ? modesValue : null;
        int ReadGameMode(string key)
        {
            // Prefer the structured game_modes object, but also accept flat aliases.
            // This keeps the client compatible with slot_data from older APWorld builds.
            if (rawModes is not null && TryGetSlotDataValue(rawModes, key, out object? nestedValue))
                return ReadSlotDataInt(nestedValue, 0);

            if (slotData.TryGetValue(key, out object? flatValue))
                return ReadSlotDataInt(flatValue, 0);

            return 0;
        }

        int puzzleArcadeComplete = slotData.ContainsKey("puzzle_arcade_complete")
            ? ReadGameMode("puzzle_arcade_complete")
            : ReadGameMode("puzzle_arcade");
        _gameModesOptions.PuzzleArcadeSimple = ReadGameMode("puzzle_arcade_simple");
        _gameModesOptions.PuzzleArcade = puzzleArcadeComplete == 1 || _gameModesOptions.PuzzleArcadeSimple == 1 ? 1 : 0;
        int puzzleStoryComplete = slotData.ContainsKey("puzzle_story_complete")
            ? ReadGameMode("puzzle_story_complete")
            : ReadGameMode("puzzle_story"); // Legacy slot_data treated Puzzle/Story as Complete.
        _gameModesOptions.PuzzleStoryComplete = puzzleStoryComplete;
        _gameModesOptions.PuzzleStorySimple = ReadGameMode("puzzle_story_simple");
        _gameModesOptions.PuzzleStory = puzzleStoryComplete == 1 || _gameModesOptions.PuzzleStorySimple == 1 ? 1 : 0;
        _gameModesOptions.VsStorySimple = ReadGameMode("vs_story_simple");
        _gameModesOptions.VsStoryComplete = ReadGameMode("vs_story_complete");
        _gameModesOptions.VsContestSimple = ReadGameMode("vs_contest_simple");
        _gameModesOptions.VsContestComplete = ReadGameMode("vs_contest_complete");

        if (rawModes is null &&
            !slotData.ContainsKey("puzzle_arcade") &&
            !slotData.ContainsKey("puzzle_arcade_complete") &&
            !slotData.ContainsKey("puzzle_arcade_simple") &&
            !slotData.ContainsKey("puzzle_story") &&
            !slotData.ContainsKey("puzzle_story_complete") &&
            !slotData.ContainsKey("puzzle_story_simple") &&
            !slotData.ContainsKey("vs_story_simple") &&
            !slotData.ContainsKey("vs_story_complete") &&
            !slotData.ContainsKey("vs_contest_simple") &&
            !slotData.ContainsKey("vs_contest_complete"))
        {
            WriteClientLine("WARNING: Archipelago slot data contains no recognized game-mode options. Checks may be disabled; update the APWorld and regenerate the seed.");
        }
        else
        {
            var enabledModes = new List<string>();
            if (_gameModesOptions.PuzzleArcade == 1 && _gameModesOptions.PuzzleArcadeSimple == 0) enabledModes.Add("Puzzle/Arcade Complete");
            if (_gameModesOptions.PuzzleArcadeSimple == 1) enabledModes.Add("Puzzle/Arcade Simple");
            if (_gameModesOptions.PuzzleStoryComplete == 1) enabledModes.Add("Puzzle/Story Complete");
            if (_gameModesOptions.PuzzleStorySimple == 1) enabledModes.Add("Puzzle/Story Simple");
            if (_gameModesOptions.VsStorySimple == 1) enabledModes.Add("VS/Story simple");
            if (_gameModesOptions.VsStoryComplete == 1) enabledModes.Add("VS/Story complete");
            if (_gameModesOptions.VsContestSimple == 1) enabledModes.Add("VS/Contest simple");
            if (_gameModesOptions.VsContestComplete == 1) enabledModes.Add("VS/Contest complete");
            WriteClientLine($"Enabled modes: {(enabledModes.Count > 0 ? string.Join(", ", enabledModes) : "none")}.");
        }

        // The APWorld supplies these options in slot_data. Never use local JSON fallbacks in live mode.
        object? rawStoryOptions = slotData.TryGetValue("vs_story_options", out object? storyOptions) ? storyOptions : null;
        _vsStoryOptions.BuffTamtam = ReadSlotDataInt(
            TryGetSlotDataValue(rawStoryOptions, "buff_tamtam", out object? nestedBuff) ? nestedBuff :
            (TryGetSlotDataValue(slotData, "buff_tamtam", out object? flatBuff) ? flatBuff : null), 0);
        _vsStoryOptions.ExtraEnemy = ReadSlotDataInt(
            TryGetSlotDataValue(rawStoryOptions, "extra_enemy", out object? nestedSecret) ? nestedSecret :
            (TryGetSlotDataValue(slotData, "extra_enemy", out object? flatSecret) ? flatSecret : null), 0);
        _activeVsStoryEnemies = _vsStoryOptions.ExtraEnemy == 1
            ? VsStoryEnemies
            : VsStoryEnemies.Where(enemy => enemy.EnemyId != SecretEnemyId).ToArray();

        if (slotData.TryGetValue("vs_story_characters", out object? rawStoryCount))
            _vsStoryOptions.VsStoryCharacters = Math.Clamp(ReadSlotDataInt(rawStoryCount, 3), 3, 16);

        object? rawContestOptions = slotData.TryGetValue("vs_contest_options", out object? contestOptions) ? contestOptions : null;
        _contestOptions.IncreaseDifficultyContest = ReadSlotDataInt(
            TryGetSlotDataValue(rawContestOptions, "increase_difficulty_contest", out object? nestedIncrease) ? nestedIncrease :
            (TryGetSlotDataValue(slotData, "increase_difficulty_contest", out object? flatIncrease) ? flatIncrease : null), 0);
        if (slotData.TryGetValue("contest_characters", out object? rawContestCount))
            _contestOptions.ContestCharacters = Math.Clamp(ReadSlotDataInt(rawContestCount, 3), 3, 16);
        if (slotData.TryGetValue("custom_contest_wins", out object? rawWins))
            _contestOptions.CustomContestWins = Math.Clamp(ReadSlotDataInt(rawWins, 10), 3, 30);

        object? rawOtherOptions = slotData.TryGetValue("other_options", out object? otherOptions) ? otherOptions : null;
        _otherOptions.FixPacky = ReadSlotDataInt(
            TryGetSlotDataValue(rawOtherOptions, "fix_packy", out object? nestedPacky) ? nestedPacky :
            (TryGetSlotDataValue(slotData, "fix_packy", out object? flatPacky) ? flatPacky : null), 0);
        var enabledOptions = new List<string>();
        if (_vsStoryOptions.ExtraEnemy == 1) enabledOptions.Add("Extra enemy");
        if (_vsStoryOptions.BuffTamtam == 1) enabledOptions.Add("Tamtam buff");
        if (_contestOptions.IncreaseDifficultyContest > 0)
            enabledOptions.Add($"Increased VS/Contest difficulty by {_contestOptions.IncreaseDifficultyContest}");
        if (_otherOptions.FixPacky == 1) enabledOptions.Add("Fixed Packy's damage");
        WriteClientLine($"Enabled options: {(enabledOptions.Count > 0 ? string.Join(", ", enabledOptions) : "none")}.");

        // The APWorld is authoritative for Tarot difficulty in a live seed.
        // Do not read a local Tarot difficulty plan in live mode.
        if (TryGetSlotDataValue(slotData, "puzzle_story_difficulty_plan", out object? rawTarotPlan) &&
            EnumerateSlotDataMap(rawTarotPlan) is IEnumerable<KeyValuePair<string, object?>> tarotPlanEntries)
        {
            var serverPlan = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> entry in tarotPlanEntries)
            {
                string key = entry.Key.Trim();
                string value = Convert.ToString(entry.Value)?.Trim() ?? string.Empty;
                if (value.Equals("Normal", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("Expert", StringComparison.OrdinalIgnoreCase))
                    serverPlan[key] = value;
            }
            if (serverPlan.Count > 0)
            {
                _tarotDifficultyPlan = serverPlan;
                ClientLog.Debug($"Received Tarot difficulty plan from Archipelago: {serverPlan.Count} card levels.");
            }
            else
            {
                ClientLog.Debug("Archipelago slot_data included a Tarot difficulty plan, but it contained no valid assignments.");
            }
        }
        else if (_gameModesOptions.PuzzleStory == 1)
        {
            ClientLog.Debug("WARNING: Puzzle/Story is enabled, but slot_data did not provide a readable puzzle_story_difficulty_plan. Expected 110 Tarot level assignments. Check the installed APWorld version and regenerate the seed if needed.");
        }

        _serverGoalType = slotData.TryGetValue("goal_type", out object? rawGoalType)
            ? Convert.ToString(rawGoalType)?.Trim() ?? string.Empty
            : string.Empty;
        _requiredGoalCheckLocations.Clear();
        if (slotData.TryGetValue("goal_check_locations", out object? rawGoalChecks) &&
            EnumerateSlotDataArray(rawGoalChecks) is IEnumerable<object?> goalChecks)
        {
            foreach (object? value in goalChecks)
            {
                string? name = Convert.ToString(value)?.Trim();
                if (!string.IsNullOrWhiteSpace(name)) _requiredGoalCheckLocations.Add(name);
            }
        }
        _requiredGoalVictoryItems.Clear();
        _receivedGoalVictoryItems.Clear();
        if (slotData.TryGetValue("goal_victory_items", out object? rawVictoryItems) && EnumerateSlotDataArray(rawVictoryItems) is IEnumerable<object?> victoryItems)
        {
            foreach (object? value in victoryItems)
            {
                string? name = Convert.ToString(value)?.Trim();
                if (!string.IsNullOrWhiteSpace(name)) _requiredGoalVictoryItems.Add(name);
            }
        }

        _serverSelectedVsStoryCharacterIds.Clear();
        _serverSelectedContestCharacterIds.Clear();
        _receivedVsStoryCharacterUnlockIds.Clear();
        _receivedVsContestCharacterUnlockIds.Clear();
        _receivedVsStoryGlobalUnlock = false;
        _receivedVsContestGlobalUnlock = false;
        _unlockedVsStoryEnemyIds.Clear();
        _unlockedVsStoryEnemyIds.Add(0x00); // Bob is always the first opponent.
        _vsStorySimpleAllEnemyChecksConfirmed = false;
        ReadCharacterPool(slotData, "vs_story_selected_characters", _serverSelectedVsStoryCharacterIds);
        ReadCharacterPool(slotData, "contest_selected_characters", _serverSelectedContestCharacterIds);
        AddStartingCharacter(slotData, "vs_story_start_character", _receivedVsStoryCharacterUnlockIds);
        AddStartingCharacter(slotData, "contest_start_character", _receivedVsContestCharacterUnlockIds);

        // A mode's local memory flags are configured when entering that mode; simple
        // modes unlock all characters, while Complete modes restrict them to the seed pool.
        _currentVsStoryMode = false;
        _currentVsContestMode = false;
    }

    private static void ReadCharacterPool(System.Collections.Generic.IDictionary<string, object> slotData, string key, HashSet<byte> destination)
    {
        if (!slotData.TryGetValue(key, out object? raw) || EnumerateSlotDataArray(raw) is not IEnumerable<object?> values) return;
        foreach (object? value in values)
        {
            string? name = Convert.ToString(value)?.Trim();
            if (!string.IsNullOrWhiteSpace(name) && VsCharacterIdsByName.TryGetValue(name, out byte id))
                destination.Add(id);
        }
    }

    private static void AddStartingCharacter(System.Collections.Generic.IDictionary<string, object> slotData, string key, HashSet<byte> destination)
    {
        if (!slotData.TryGetValue(key, out object? raw)) return;
        string? name = Convert.ToString(raw)?.Trim();
        if (!string.IsNullOrWhiteSpace(name) && VsCharacterIdsByName.TryGetValue(name, out byte id))
            destination.Add(id);
    }

    private static ArchipelagoSession? ConnectToArchipelago()
    {
        while (true)
        {
            Console.Write("Enter AP host (leave blank for archipelago.gg): ");
            string domain = (Console.ReadLine() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(domain))
                domain = "archipelago.gg";
            if (domain.Length == 0)
            {
                WriteClientLine("Host cannot be empty.");
                continue;
            }

            Console.Write("Enter port: ");
            string portText = (Console.ReadLine() ?? string.Empty).Trim();
            if (!int.TryParse(portText, out int port) || port is < 1 or > 65535)
            {
                WriteClientLine("Invalid port. Enter a number between 1 and 65535.");
                continue;
            }

            Console.Write("Slot name: ");
            string slotName = (Console.ReadLine() ?? string.Empty).Trim();
            if (slotName.Length == 0)
            {
                WriteClientLine("Slot name cannot be empty.");
                continue;
            }

            Console.Write("Room password: ");
            string passwordInput = Console.ReadLine() ?? string.Empty;
            string? password = string.IsNullOrWhiteSpace(passwordInput) ? null : passwordInput;

            _slotName = slotName;
            WriteClientLine("Connecting to Archipelago...");
            try
            {
                var session = ArchipelagoSessionFactory.CreateSession(domain, port);

                // Keep the server's original text, but color the known message parts.
                session.MessageLog.OnMessageReceived += message =>
                    WriteArchipelagoMessage(message.ToString());

                // Register before login so initial and future received items are not missed.
                session.Items.ItemReceived += receivedItemsHelper =>
                {
                    // In Archipelago.MultiClient.Net 6.7.1, DequeueItem returns ItemInfo.
                    // Use the resolved item name supplied by ItemInfo.
                    var receivedItem = receivedItemsHelper.DequeueItem();
                    string? itemName = receivedItem.ItemName ?? receivedItem.ItemDisplayName;
                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        object? rawPlayer = receivedItem.GetType().GetProperty("Player")?.GetValue(receivedItem);
                        // ItemInfo.Player is a PlayerInfo object, not the numeric slot itself.
                        // PlayerInfo exposes its numeric slot through the Slot property.
                        int? senderSlot = TryReadSlotId(rawPlayer, "Slot");
                        string? stableKey = BuildReceivedItemKey(receivedItem);
                        string rawPlayerDescription = rawPlayer is null
                            ? "null"
                            : $"{Convert.ToString(rawPlayer, System.Globalization.CultureInfo.InvariantCulture) ?? rawPlayer.GetType().Name} " +
                              $"(type={rawPlayer.GetType().FullName}, slot={((senderSlot.HasValue) ? senderSlot.Value.ToString() : "NULL")})";

                        ClientLog.Debug(
                            $"REMOTE ITEM DEBUG: Archipelago event received '{itemName}'; " +
                            $"PlayerInfo={rawPlayerDescription}; parsed senderSlot={(senderSlot.HasValue ? senderSlot.Value.ToString() : "NULL")}; " +
                            $"localSlot={(_localPlayerSlot.HasValue ? _localPlayerSlot.Value.ToString() : "NULL")}; stableKey={stableKey ?? "NULL"}.");

                        _receivedItemQueue.Enqueue(new QueuedReceivedItem(itemName, stableKey, senderSlot));
                    }
                    else
                    {
                        ClientLog.Debug("REMOTE ITEM DEBUG: Archipelago ItemReceived event had no resolved item name; item was not queued.");
                    }
                };

                var result = session.TryConnectAndLogin(
                    "Puzzle Bobble 4",
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(0, 6, 0),
                    password: password);

                if (result.Successful)
                {
                    if (result is LoginSuccessful loginSuccessful)
                    {
                        _localPlayerSlot = TryReadSlotId(loginSuccessful, "Slot");
                        if (_localPlayerSlot.HasValue)
                            ClientLog.Debug($"ARCHIPELAGO: local player slot is {_localPlayerSlot.Value}.");
                        else
                            WriteClientLine("WARNING: Could not determine the local Archipelago slot; remote-item gameplay signals are disabled.");

                        // DeathLink is opt-in through this seed's YAML option.
                        // The client does not participate unless the APWorld explicitly
                        // reports that the slot enabled it.
                        _deathLinkEnabled = TryReadSlotDataBoolean(loginSuccessful.SlotData, "death_link", out bool deathLinkOption) && deathLinkOption;
                        string seedId = loginSuccessful.SlotData.TryGetValue("level_skip_seed_id", out object? rawSeedId)
                            ? Convert.ToString(rawSeedId)?.Trim() ?? string.Empty
                            : string.Empty;
                        if (string.IsNullOrWhiteSpace(seedId))
                        {
                            WriteClientLine("WARNING: This APWorld did not provide a seed ID for Level Skip persistence. Update the APWorld and regenerate the seed.");
                            seedId = "unknown-seed";
                        }
                        LoadLevelSkipSave(domain, port, slotName, seedId);
                        ApplyServerSlotData(loginSuccessful.SlotData);
                        _deathLinkService = null;
                        while (_incomingDeathLinks.TryDequeue(out _)) { }
                        if (_deathLinkEnabled)
                        {
                            _deathLinkService = session.CreateDeathLinkService();
                            _deathLinkService.OnDeathLinkReceived += deathLink =>
                            {
                                // Ignore a bounce attributed to our own slot to avoid
                                // re-applying a local death if the server echoes it back.
                                if (string.Equals(deathLink.Source, _slotName, StringComparison.OrdinalIgnoreCase))
                                    return;
                                _incomingDeathLinks.Enqueue(deathLink);
                            };
                            _deathLinkService.EnableDeathLink();
                            WriteClientLine("Death Link enabled for this seed.");
                        }
                        else
                        {
                            WriteClientLine("Death Link is disabled for this seed.");
                        }

                        // Remove diagnostic plan exports left by older client versions.
                        // The player does not need a local copy of the hidden level assignments.
                        try
                        {
                            LevelModeGenerator.ClearExportedPlan(AppContext.BaseDirectory);
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                        {
                            WriteClientLine($"WARNING: Could not remove an old level-mode diagnostic export: {ex.Message}");
                        }

                        // The APWorld generates the weighted mode for every level
                        // from the seed RNG and sends the complete map via slot_data.
                        // Never fall back to a local random plan: without this data,
                        // level modes stay untouched so we cannot apply the wrong seed.
                        _modePlan = null;
                        if (_gameModesOptions.PuzzleArcade == 1)
                        {
                            string modePlanError = "slot_data does not contain 'level_mode_plan'.";
                            bool hasModePlan = loginSuccessful.SlotData.TryGetValue("level_mode_plan", out object? rawModePlan);
                            LevelModePlan? receivedPlan = null;
                            string[] expectedArcadeLevels;
                            if (_gameModesOptions.PuzzleArcadeSimple == 1)
                            {
                                var simpleLetters = new List<string> { "A", "B" };
                                if (loginSuccessful.SlotData.TryGetValue("puzzle_arcade_simple_letters", out object? rawSimpleLetters) &&
                                    EnumerateSlotDataArray(rawSimpleLetters) is IEnumerable<object?> selectedLetters)
                                {
                                    simpleLetters.AddRange(selectedLetters
                                        .Select(value => Convert.ToString(value)?.Trim())
                                        .Where(value => !string.IsNullOrWhiteSpace(value))!
                                        .Select(value => value!));
                                }
                                expectedArcadeLevels = LevelModeGenerator.BuildLevelIds(simpleLetters);
                            }
                            else
                            {
                                expectedArcadeLevels = LevelModeGenerator.BuildLevelIds();
                            }

                            bool validModePlan = hasModePlan &&
                                LevelModeGenerator.TryCreatePlan(rawModePlan, expectedArcadeLevels, out receivedPlan, out modePlanError);
                            if (validModePlan)
                            {
                                _modePlan = receivedPlan;
                                ClientLog.Debug($"Level mode plan received from Archipelago: {receivedPlan!.Assignments.Count} levels.");
                            }
                            else
                            {
                                WriteClientLine($"WARNING: Could not read the level mode plan from Archipelago: {modePlanError} Level modes will not be changed.");
                            }
                        }

                        WriteReceivedDataSummary(loginSuccessful.SlotData);

                        // Item placements and item weights are intentionally not displayed or written
                        // to local files. Items are secret; the server remains authoritative for delivery.

                        // Read any extra Unlock prerequisites supplied by the world.
                        // Goal-world Unlock items stay pending until all required remote
                        // Unlocks have actually arrived and unlocked.
                        _unlockPrerequisites.Clear();
                        if (loginSuccessful.SlotData.TryGetValue("unlock_prerequisites", out object? rawPrerequisites) &&
                            EnumerateSlotDataMap(rawPrerequisites) is IEnumerable<KeyValuePair<string, object?>> prerequisiteEntries)
                        {
                            foreach (KeyValuePair<string, object?> entry in prerequisiteEntries)
                            {
                                string? targetLetter = entry.Key.Trim().ToUpperInvariant();
                                if (!IsKnownLetterName(targetLetter) || EnumerateSlotDataArray(entry.Value) is not IEnumerable<object?> prerequisiteValues)
                                    continue;

                                var requiredLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                foreach (object? value in prerequisiteValues)
                                {
                                    string? requiredLetter = Convert.ToString(value)?.Trim().ToUpperInvariant();
                                    if (IsKnownLetterName(requiredLetter)) requiredLetters.Add(requiredLetter!);
                                }
                                _unlockPrerequisites[targetLetter!] = requiredLetters;
                            }
                        }

                        // The world supplies the selected goal letter(s) in slot_data.
                        // This supports both a single random goal and the "all U-Z" goal.
                        if (loginSuccessful.SlotData.TryGetValue("goal_letters", out object? rawGoalLetters) &&
                            EnumerateSlotDataArray(rawGoalLetters) is IEnumerable<object?> goalLetterValues)
                        {
                            foreach (object? value in goalLetterValues)
                            {
                                string? goalLetter = Convert.ToString(value)?.Trim();
                                if (IsKnownLetterName(goalLetter))
                                    _goalLetters.Add(goalLetter!);
                            }
                        }
                    }

                    // Seed the local duplicate guard from the server's authoritative checked list.
                    foreach (long checkedId in session.Locations.AllLocationsChecked)
                    {
                        string? checkedName = session.Locations.GetLocationNameFromId(
                            checkedId, "Puzzle Bobble 4");
                        if (!string.IsNullOrWhiteSpace(checkedName))
                        {
                            _checksLoggedThisRun.TryAdd(checkedName, 0);
                            _confirmedChecks.TryAdd(checkedName, 0);
                            RegisterPreviouslyCompletedTarotCard(checkedName);
                            RegisterPreviouslyCompletedArcadeLetter(checkedName);
                        }
                    }

                    // Evaluate the server's already-checked list once after login.
                    UpdateVsStorySimpleEnemyUnlocksFromConfirmedChecks(session);
                    WriteGoalSummary();

                    // This event is the server-side confirmation, unlike a local "sent" message.
                    session.Locations.CheckedLocationsUpdated += checkedIds =>
                    {
                        bool vsContestGoalProgressChanged = false;
                        foreach (long checkedId in checkedIds)
                        {
                            string? checkedName = session.Locations.GetLocationNameFromId(
                                checkedId, "Puzzle Bobble 4");
                            if (!string.IsNullOrWhiteSpace(checkedName))
                            {
                                _checksLoggedThisRun.TryAdd(checkedName, 0);
                                _confirmedChecks.TryAdd(checkedName, 0);
                                ClientLog.Debug($"Archipelago confirmed check: {checkedName}");
                                RegisterPreviouslyCompletedArcadeLetter(checkedName);
                                string normalizedServerGoal = _serverGoalType.Trim().Replace('-', '_').Replace(' ', '_');
                                if ((normalizedServerGoal.Equals("vs_contest", StringComparison.OrdinalIgnoreCase) ||
                                     normalizedServerGoal.Equals("vs_story", StringComparison.OrdinalIgnoreCase)) &&
                                    _requiredGoalCheckLocations.Contains(checkedName))
                                    vsContestGoalProgressChanged = true;
                            }
                        }
                        UpdateVsStorySimpleEnemyUnlocksFromConfirmedChecks(session);
                        if (vsContestGoalProgressChanged)
                            WriteGoalSummary();
                        CheckGoalFromConfirmedLocations();
                    };

                    WriteClientLine("Connected to Archipelago.");
                    WriteClientLine("Type server commands such as !help and press Enter. Type /help for local command help.");
                    StartConsoleCommandReader(session);
                    CheckGoalFromConfirmedLocations();
                    WriteClientLine($"Synced {session.Locations.AllLocationsChecked.Count} checks already registered by the server.");
                    return session;
                }

                WriteClientLine("Could not connect to Archipelago. Check the host, port, slot name, and room password.");
            }
            catch (Exception ex)
            {
                WriteClientLine($"Could not connect to Archipelago: {ex.GetBaseException().Message}");
            }

            WriteClientLine("Please enter the connection details again.");
        }
    }

    private static void WriteUnlockedArcadeWorlds()
    {
        // Include every Puzzle/Arcade world, including U-Z and the special "?" world.
        // Menus only contains the navigable menu entries and is not the complete world list.
        string[] allWorlds = "ABCDEFGHIJKLMNOPQRSTUVW?XYZ".Select(letter => letter.ToString()).ToArray();
        string[] unlocked = allWorlds
            .Where(world => !LockedLetters.Contains(world))
            .ToArray();
        string[] completed = allWorlds
            .Where(world => _puzzleArcadeCompletedLetters.Contains(world))
            .ToArray();
        string[] notCompleted = unlocked
            .Where(world => !_puzzleArcadeCompletedLetters.Contains(world))
            .ToArray();

        WriteClientLine($"Worlds unlocked: {(unlocked.Length > 0 ? string.Join(", ", unlocked) : "none")}.");
        WriteClientLine($"Worlds completed: {(completed.Length > 0 ? string.Join(", ", completed) : "none")}.");
        WriteClientLine($"Worlds not completed: {(notCompleted.Length > 0 ? string.Join(", ", notCompleted) : "none")}.");
    }

    private static void WriteUnlockedTarotCards()
    {
        string[] unlocked = PuzzleStoryCards
            .Where(card => _puzzleStoryUnlockedCards.Contains(card.Id))
            .Select(card => card.Name)
            .ToArray();
        string[] completed = PuzzleStoryCards
            .Where(card => _puzzleStoryCompletedCards.Contains(card.Id))
            .Select(card => card.Name)
            .ToArray();
        string[] notCompleted = PuzzleStoryCards
            .Where(card => _puzzleStoryUnlockedCards.Contains(card.Id) &&
                !_puzzleStoryCompletedCards.Contains(card.Id))
            .Select(card => card.Name)
            .ToArray();

        WriteClientLine($"Tarot cards unlocked: {(unlocked.Length > 0 ? string.Join(", ", unlocked) : "none")}.");
        WriteClientLine($"Tarot cards completed: {(completed.Length > 0 ? string.Join(", ", completed) : "none")}.");
        WriteClientLine($"Tarot cards not completed: {(notCompleted.Length > 0 ? string.Join(", ", notCompleted) : "none")}.");
    }

    private static void WriteLocalCommandHelp()
    {
        WriteClientLine("Local commands:");
        WriteClientLine("  /skips - Show current level skips");
        WriteClientLine("  /arcade - Show worlds unlocked");
        WriteClientLine("  /tarot - Show tarot cards unlocked");
    }


    private static void StartLocalConsoleCommandReader()
    {
        _ = Task.Run(() =>
        {
            while (true)
            {
                string? line;
                try
                {
                    Console.Write("> ");
                    line = Console.ReadLine();
                }
                catch { return; }
                if (line is null) return;
                line = line.Trim();
                if (line.Length == 0) continue;
                if (line.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    WriteLocalCommandHelp();
                }
                else if (line.Equals("/skips", StringComparison.OrdinalIgnoreCase))
                {
                    WriteClientLine($"You have {Volatile.Read(ref _levelSkipCount)} level skips");
                }
                else if (line.Equals("/arcade", StringComparison.OrdinalIgnoreCase))
                {
                    WriteUnlockedArcadeWorlds();
                }
                else if (line.Equals("/tarot", StringComparison.OrdinalIgnoreCase))
                {
                    WriteUnlockedTarotCards();
                }
                else
                {
                    WriteClientLine("Unknown local command.");
                }
            }
        });
    }

    private static void ProcessCompletedLevelLocally(DuckstationMemory gdb, string levelId)
    {
        if (!_localItemPlan.TryGetValue(levelId, out string? itemName))
        {
            WriteClientLine($"LOCAL CHECK: {levelId} completed; no item was assigned.");
            return;
        }

        WriteClientLine($"LOCAL CHECK: {levelId} completed; local item: {itemName}.");
        // Route simulated rewards through the same delay and application path as network items.
        HandleReceivedItem(itemName);
    }

    private static void ApplyFallingCeiling(DuckstationMemory gdb, string message)
    {
        // Both VS modes use their own effect byte; Puzzle modes retain the original trap.
        if (gdb.ReadByte(PuzzleVsModeAddress) == 0x01)
        {
            byte current = gdb.ReadByte(VsFallingCeilingAddress);
            byte updated = unchecked((byte)(current + 0x07));
            gdb.WriteByte(VsFallingCeilingAddress, updated);
            if (message.StartsWith("RECEIVED TRAP:", StringComparison.OrdinalIgnoreCase))
                ClientLog.Debug($"{message} VS effect: 0x{VsFallingCeilingAddress:X8} {current:X2} -> {updated:X2} (+07).");
            else
                WriteClientLine($"{message} VS effect: 0x{VsFallingCeilingAddress:X8} {current:X2} -> {updated:X2} (+07).");
        }
        else
        {
            gdb.WriteByte(FallingCeilingAddress, 0x10);
            if (message.StartsWith("RECEIVED TRAP:", StringComparison.OrdinalIgnoreCase))
                ClientLog.Debug($"{message} value=0x10 written to 0x{FallingCeilingAddress:X8}.");
            else
                WriteClientLine(message);
        }
    }

    private static void StartConsoleCommandReader(ArchipelagoSession session)
    {
        // Console input is handled on its own thread. The game polling loop must
        // never consume stdin, otherwise the player cannot type server commands.
        _ = Task.Run(() =>
        {
            while (true)
            {
                string? line;
                try
                {
                    Console.Write("> ");
                    line = Console.ReadLine();
                }
                catch (Exception ex)
                {
                    WriteClientLine($"Console input stopped: {ex.GetBaseException().Message}");
                    return;
                }

                if (line is null)
                    return;

                line = line.Trim();
                if (line.Length == 0)
                    continue;

                if (line.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    WriteClientLine("Server commands: type !help and press Enter.");
                    WriteLocalCommandHelp();
                    continue;
                }

                // /skips is a local query in every mode and does not require gameplay.
                if (line.Equals("/skips", StringComparison.OrdinalIgnoreCase))
                {
                    WriteClientLine($"You have {Volatile.Read(ref _levelSkipCount)} level skips");
                    continue;
                }
                if (line.Equals("/arcade", StringComparison.OrdinalIgnoreCase))
                {
                    WriteUnlockedArcadeWorlds();
                    continue;
                }
                if (line.Equals("/tarot", StringComparison.OrdinalIgnoreCase))
                {
                    WriteUnlockedTarotCards();
                    continue;
                }
                if (line.StartsWith("/", StringComparison.Ordinal))
                {
                    WriteClientLine("Unknown local command.");
                    continue;
                }

                try
                {
                    session.Socket.SendPacket(new SayPacket { Text = line });
                }
                catch (Exception ex)
                {
                    WriteClientLine($"Could not send command to Archipelago: {ex.GetBaseException().Message}");
                }
            }
        });
    }

    // Local testing commands are queued by the console reader and applied on the
    // game polling thread, so DuckStation memory access remains single-threaded.
    private static void DisconnectMemory()
    {
        // Release the DuckStation process handle and allow the reconnect loop
        // to locate the emulator again. Safe to call more than once.
        try
        {
            _gdb?.Dispose();
        }
        catch (Exception ex)
        {
            ClientLog.Debug($"Error while disconnecting from DuckStation: {ex.Message}");
        }
        finally
        {
            _gdb = null;
            _previousLevelState = null;
            _previousGameMode = null;
            _previousLetterValue = null;
            _previousFacing = null;
            _previousControllerInput = null;
            _previousVerticalControllerInput = null;
            _previousPuzzleArcadeMode = null;
            _previousVersusModeActive = null;
            _isVsModeActive = false;
        }
    }

    private static void HandleKey(ConsoleKey key)
    {
        if (DateTime.UtcNow - _lastAction < Debounce) return;

        int direction = key switch
        {
            ConsoleKey.LeftArrow => -1,
            ConsoleKey.RightArrow => 1,
            _ => 0
        };
        bool reset = key == ConsoleKey.Backspace;
        if (direction == 0 && !reset) return;

        try
        {
            byte mode = _gdb!.ReadByte(GameModeAddress);
            if (mode != MenuLetterSelectMode)
            {
                ClientLog.Debug($"Movement ignored: 0x{GameModeAddress:X8}={mode:X2}; letter selection only works in mode 01.");
                _lastAction = DateTime.UtcNow;
                return;
            }

            // Navigation through the menu list stays linear, but a menu is a
            // valid stop only if at least one of its graph exits leads to an
            // unlocked destination. Skip invalid menus in the pressed direction.
            // Menu0 is the fallback and is always considered valid.
            if (reset)
            {
                _menuIndex = 0;
            }
            else
            {
                int previousIndex = _menuIndex;
                _menuIndex = FindNextUsableMenu(_menuIndex, direction);
                if (_menuIndex == 0 && previousIndex != 0)
                    ClientLog.Debug("No more menus with unlocked exits in that direction; returning to Menu0.");
                else if (_menuIndex == 0 && previousIndex == 0)
                    ClientLog.Debug("No letters have unlocked exits; staying in Menu0.");
                else if (_menuIndex != previousIndex + direction)
                    ClientLog.Debug($"Skipped menus with no unlocked exits; selected menu: {Menus[_menuIndex].Name}.");
            }

            if (_menuIndex != 0 && _resetLetterOnSelectionChange)
            {
                _resetLetterOnSelectionChange = false;
                ClientLog.Debug("Automatic correction to Menu0 disabled: you selected a world manually.");
            }

            ApplyMenu(_gdb, _menuIndex);
            CorrectFacingForCurrentMenu(_gdb);
            byte currentFacing = _gdb.ReadByte(FacingAddress);
            PrepareLookedAtLetterMode(_gdb, currentFacing, "world selection");
            byte focusedLetter = _gdb.ReadByte(LetterAddress);
            ClientLog.Debug($"Base letter applied: {Menus[_menuIndex].Name}; menu ID=0x{focusedLetter:X2}.");
            PrintLookTargetStatus(_gdb, currentFacing, "manual selection/navigation");
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException or UnauthorizedAccessException)
        {
            ClientLog.Debug($"Could not change the letter due to a memory access error: {ex.Message}");
            DisconnectMemory();
            _nextReconnectAttempt = DateTime.UtcNow.AddSeconds(1);
        }
        _lastAction = DateTime.UtcNow;
    }

    private static int FindNextUsableMenu(int currentIndex, int direction)
    {
        int index = currentIndex;
        // Walk the linear menu list in the requested direction. The first
        // menu with at least one unlocked outgoing edge is selected. Menu0 is
        // the guaranteed fallback when no letter qualifies.
        for (int steps = 0; steps < Menus.Length; steps++)
        {
            index = (index + direction + Menus.Length) % Menus.Length;
            if (index == 0 || HasUsableExit(index))
                return index;
        }
        return 0;
    }

    private static bool HasUsableExit(int menuIndex) =>
        IsUsableDestination(GetNeighbor(menuIndex, -1)) ||
        IsUsableDestination(GetNeighbor(menuIndex, 1));

    private static int GetNeighbor(int menuIndex, int direction)
    {
        // Indices: Menu0=0, A=1, B=2, ... T=20. Terminal letters U-Z are
        // represented in the graph but have no coordinate entries in this client.
        return (menuIndex, direction) switch
        {
            (0, -1) => 1, (0, 1) => 2,
            (1, -1) => 3, (1, 1) => 4,
            (2, -1) => 4, (2, 1) => 5,
            (3, -1) => 6, (3, 1) => 7,
            (4, -1) => 7, (4, 1) => 8,
            (5, -1) => 8, (5, 1) => 9,
            (6, -1) => 10, (6, 1) => 11,
            (7, -1) => 11, (7, 1) => 12,
            (8, -1) => 12, (8, 1) => 13,
            (9, -1) => 13, (9, 1) => 14,
            (10, -1) => 15, (10, 1) => 16,
            (11, -1) => 16, (11, 1) => 17,
            (12, -1) => 17, (12, 1) => 18,
            (13, -1) => 18, (13, 1) => 19,
            (14, -1) => 19, (14, 1) => 20,
            (15, -1) => 21, (15, 1) => 22,
            (16, -1) => 22, (16, 1) => 23,
            (17, -1) => 23, (17, 1) => 24,
            (18, -1) => 24, (18, 1) => 25,
            (19, -1) => 25, (19, 1) => 26,
            (20, -1) => 26, (20, 1) => 27,
            _ => -1
        };
    }

    private static string? GetDestinationName(int destinationIndex)
    {
        if (destinationIndex >= 0 && destinationIndex < Menus.Length)
            return destinationIndex == 0 ? null : Menus[destinationIndex].Name;

        return destinationIndex switch
        {
            21 => "U", 22 => "V", 23 => "W", 24 => "?", 25 => "X", 26 => "Y", 27 => "Z",
            _ => null
        };
    }

    private static bool IsLocked(int destinationIndex)
    {
        string? destination = GetDestinationName(destinationIndex);
        return destination is not null && LockedLetters.Contains(destination);
    }

    // Once at a menu, ensure the character is facing an unlocked exit.
    // 00 means left and 01 means right. If the current exit is locked, try the other.
    private static void CorrectFacingForCurrentMenu(DuckstationMemory gdb)
    {
        byte facing = gdb.ReadByte(FacingAddress);
        int direction = facing == FacingLeft ? -1 : facing == FacingRight ? 1 : 0;
        if (direction == 0) return;

        int target = GetNeighbor(_menuIndex, direction);
        if (IsUsableDestination(target)) return;

        int otherDirection = direction == -1 ? 1 : -1;
        int otherTarget = GetNeighbor(_menuIndex, otherDirection);
        if (IsUsableDestination(otherTarget))
        {
            byte corrected = otherDirection == -1 ? FacingLeft : FacingRight;
            gdb.WriteByte(FacingAddress, corrected);
            ClientLog.Debug($"Look direction corrected in {Menus[_menuIndex].Name}: {facing:X2} -> {corrected:X2} because the original exit is locked.");
        }
    }

    private static bool IsUsableDestination(int target)
    {
        string? destination = GetDestinationName(target);
        if (destination is null || IsLocked(target))
            return false;

        // A and B are the permanent starting choices in Menu0 and must remain
        // selectable even after their fifth-level checks have been completed.
        if (destination.Equals("A", StringComparison.OrdinalIgnoreCase) ||
            destination.Equals("B", StringComparison.OrdinalIgnoreCase))
            return true;

        return !_puzzleArcadeCompletedLetters.Contains(destination);
    }

    private static void TryReconnect()
    {
        if (DateTime.UtcNow < _nextReconnectAttempt) return;
        _nextReconnectAttempt = DateTime.UtcNow.Add(DuckstationRetryInterval);
        try
        {
            _gdb = new DuckstationMemory();
            _previousLevelState = null; // Avoid a false 02 -> 03 check across a reconnect.
            WriteClientLine("Connected to DuckStation.");
        }
        catch (Exception)
        {
            WriteClientLine("Could not find DuckStation.");
            WriteClientLine("Retrying in 5 seconds...");
        }
    }

    private static void InitializeSkipHud(DuckstationMemory gdb)
    {
        // Hide the original "To start" string.
        for (uint address = 0x0011A020; address <= 0x0011A027; address++)
            gdb.WriteByte(address, 0x00);

        // "Skips 00" in ASCII. Keep the last two bytes as the primary counter.
        byte[] label = [0x53, 0x6B, 0x69, 0x70, 0x73, 0x20]; // "Skips "
        for (int i = 0; i < label.Length; i++)
        {
            gdb.WriteByte(0x0011A001u + (uint)i, label[i]);
            gdb.WriteByte(0x0011A011u + (uint)i, label[i]);
        }
        gdb.WriteByte(0x0011A007, 0x30); // ASCII '0', tens digit
        gdb.WriteByte(0x0011A008, 0x30); // ASCII '0', units digit
        gdb.WriteByte(0x0011A000, 0x20); // ASCII space
        gdb.WriteByte(0x0011A010, 0x20); // ASCII space
        for (uint address = 0x0011A009; address <= 0x0011A00C; address++)
            gdb.WriteByte(address, 0x00);
        for (uint address = 0x0011A019; address <= 0x0011A01B; address++)
            gdb.WriteByte(address, 0x00);
    }

    private static void RestorePuzzleArcadeSkipHud(DuckstationMemory gdb)
    {
        // Restore every byte from the user's original Reset.bin snapshot. This is
        // important because 0x0011A020-0x0011A10F contains non-text game data;
        // filling the region with spaces would corrupt the original HUD/state.
        for (int i = 0; i < VsStoryHudResetSnapshot.Length; i++)
            gdb.WriteByte(0x0011A000u + (uint)i, VsStoryHudResetSnapshot[i]);

        // Reset.bin was captured with 11 skips. Replace only the four ASCII
        // counter digits with the live Level Skip count after restoring the bytes.
        _vsStoryHudLayoutActive = false;
        UpdateSkipCounterHud(gdb);

        ClientLog.Debug($"VS/Story HUD reset snapshot restored byte-for-byte at 0x0011A000-0x0011A10F; live Level Skip count {_levelSkipCount:00} reapplied.");
    }

    private static void UpdateSkipCounterHud(DuckstationMemory gdb)
    {
        // The HUD stores ASCII digits, not a binary integer. VS/Story repurposes
        // the first line for the enemy name, so its only counter copy is at A017/A018.
        int clamped = Math.Clamp(_levelSkipCount, 0, 99);
        _levelSkipCount = clamped;
        byte tens = (byte)('0' + (clamped / 10));
        byte units = (byte)('0' + (clamped % 10));
        if (!_vsStoryHudLayoutActive)
        {
            gdb.WriteByte(0x0011A007, tens);
            gdb.WriteByte(0x0011A008, units);
        }
        gdb.WriteByte(0x0011A017, tens);
        gdb.WriteByte(0x0011A018, units);
    }

    private static void SynchronizeSkipCounterMirror(DuckstationMemory gdb)
    {
        // In VS/Story, A000-A00F is the enemy-name line and must not be overwritten
        // with mirrored counter digits. Other modes retain the existing two-copy HUD.
        if (_vsStoryHudLayoutActive) return;
        byte tens = gdb.ReadByte(0x0011A007);
        byte units = gdb.ReadByte(0x0011A008);
        if (gdb.ReadByte(0x0011A017) != tens) gdb.WriteByte(0x0011A017, tens);
        if (gdb.ReadByte(0x0011A018) != units) gdb.WriteByte(0x0011A018, units);
    }

    private static bool SendLocationCheck(string checkName)
    {
        var session = _apSession;
        if (session is null)
        {
            WriteClientLine($"WARNING: check {checkName} detected, but there is no Archipelago session.");
            return false;
        }

        try
        {
            long locationId = session.Locations.GetLocationIdFromName("Puzzle Bobble 4", checkName);
            if (locationId < 0)
                throw new InvalidOperationException(
                    $"Location '{checkName}' was not found in the Archipelago datapackage for Puzzle Bobble 4.");

            session.Locations.CompleteLocationChecks(locationId);
            ClientLog.Debug($"Sent check {checkName} to Archipelago.");
            return true;
        }
        catch (Exception ex)
        {
            WriteClientLine($"Could not send check {checkName} to Archipelago: {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static void CheckGoalFromConfirmedLocations()
    {
        if (_goalCompletionHandled || _requiredGoalCheckLocations.Count == 0) return;
        if (_requiredGoalCheckLocations.All(name => _confirmedChecks.ContainsKey(name)))
            HandleGoalReached();
    }

    private static void HandleGoalReached()
    {
        if (_goalCompletionHandled)
            return;

        var session = _apSession;
        if (session is null)
        {
            WriteClientLine("WARNING: A progression event was received, but there is no active Archipelago session.");
            return;
        }

        _goalCompletionHandled = true;
        try
        {
            // The legacy Arcade goal flushes remaining world checks after victory.
            // Other goals must never submit checks from unrelated modes.
            if (_serverGoalType is "one_random_u_z" or "")
            {
                var alreadyChecked = new HashSet<long>(session.Locations.AllLocationsChecked);
                var remainingLocationIds = new List<long>();
                foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVW?XYZ")
                {
                    for (int level = 1; level <= 5; level++)
                    {
                        long locationId = session.Locations.GetLocationIdFromName("Puzzle Bobble 4", $"{letter}{level}");
                        if (locationId >= 0 && !alreadyChecked.Contains(locationId)) remainingLocationIds.Add(locationId);
                    }
                }
                if (remainingLocationIds.Count > 0)
                {
                    session.Locations.CompleteLocationChecks(remainingLocationIds.ToArray());
                    WriteClientLine($"GOAL COMPLETE: Sent {remainingLocationIds.Count} remaining Arcade checks to Archipelago.");
                }
            }
            session.SetGoalAchieved();
            WriteClientLine("GOAL COMPLETE: Victory status sent to the server.");
        }
        catch (Exception ex)
        {
            // Permit a later repeated victory item/reconnect to retry if the operation failed.
            _goalCompletionHandled = false;
            WriteClientLine($"ERROR while completing the goal or sending remaining checks: {ex.GetBaseException().Message}");
        }
    }

    private static void ProcessReceivedItems()
    {
        // Always drain the queue. Progression and Level Skip items are immediate;
        // gameplay-effect Fill/Trap items apply their own animation/gameplay delay.
        while (_receivedItemQueue.TryDequeue(out QueuedReceivedItem? received))
        {
            ClientLog.Debug(
                $"REMOTE ITEM DEBUG: processing queued item '{received.Name}'; senderSlot={(received.SenderSlot.HasValue ? received.SenderSlot.Value.ToString() : "NULL")}; " +
                $"localSlot={(_localPlayerSlot.HasValue ? _localPlayerSlot.Value.ToString() : "NULL")}; stableKey={received.StableKey ?? "NULL"}.");
            HandleReceivedItem(received.Name, received.StableKey, received.SenderSlot);
        }
    }

    // A goal-world Unlock can be received early, but it does not open that world
    // until all prerequisites have themselves become available. Re-evaluate every
    // received Unlock whenever a new one arrives, so out-of-order delivery works.
    private static void RefreshUnlockedLetters()
    {
        bool changed;
        do
        {
            changed = false;
            foreach (string letter in _receivedUnlockLetters.ToArray())
            {
                if (_unlockPrerequisites.TryGetValue(letter, out HashSet<string>? prerequisites) &&
                    !prerequisites.All(required => !LockedLetters.Contains(required)))
                    continue;

                if (LockedLetters.Remove(letter))
                {
                    // Unlock notifications are intentionally summarized by /arcade
                    // to avoid flooding the console as progression items arrive.
                    changed = true;
                }
            }
        } while (changed);
    }

    // Received items are queued by the Archipelago callback and applied on the
    // main polling thread, where DuckStation memory access is already managed.
    internal static void HandleReceivedItem(string itemName) => HandleReceivedItem(itemName, null, null);

    private static void HandleReceivedItem(string itemName, string? stableItemKey) =>
        HandleReceivedItem(itemName, stableItemKey, null);

    private static void HandleReceivedItem(string itemName, string? stableItemKey, int? senderSlot)
    {
        string normalizedItemName = itemName.Trim()
            .Replace("FILL_", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "-");

        // A visual alert is only emitted for items sent by another Archipelago slot.
        // It is deliberately not queued: if the player is in a menu, no RAM signal is written.
        ClientLog.Debug(
            $"REMOTE ITEM DEBUG: HandleReceivedItem('{normalizedItemName}'); senderSlot={(senderSlot.HasValue ? senderSlot.Value.ToString() : "NULL")}; " +
            $"localSlot={(_localPlayerSlot.HasValue ? _localPlayerSlot.Value.ToString() : "NULL")}; DuckStation connected={_gdb is not null}.");
        ApplyRemoteItemReceivedSignal(normalizedItemName, senderSlot);

        // Pointer Guide has its own board-specific gameplay check, but it must also wait
        // until the result animation (07/08) has finished before entering that logic.
        if (string.Equals(normalizedItemName, "Pointer-Guide", StringComparison.OrdinalIgnoreCase) &&
            IsResultAnimationBlockingItemReceipt())
        {
            _pendingPointerGuide = true;
            ClientLog.Debug("ITEM DELAY: Pointer Guide saved until the result animation (0x00192EBF=07/08) ends.");
            return;
        }

        // Only gameplay-effect Fill/Trap items are delayed. Progression and Level Skip
        // items continue immediately, even while a VS result animation is playing.
        if (IsGameplayEffectItem(itemName) &&
            (IsResultAnimationBlockingItemReceipt() || !IsActivelyPlayingLevel()))
        {
            string? replaced = _pendingDelayedGameplayItem;
            _pendingDelayedGameplayItem = itemName;
            if (replaced is null)
                ClientLog.Debug($"ITEM DELAY: {itemName} saved until active gameplay begins.");
            else
                ClientLog.Debug($"ITEM DELAY: {itemName} replaced pending item {replaced}; only the newest item is kept.");
            return;
        }

        ApplyReceivedItemNow(itemName, stableItemKey);
    }

    private static void ApplyRemoteItemReceivedSignal(string normalizedItemName, int? senderSlot)
    {
        if (_gdb is null)
        {
            ClientLog.Debug($"REMOTE ITEM SIGNAL SKIPPED: '{normalizedItemName}'; DuckStation memory connection is unavailable.");
            return;
        }

        if (!senderSlot.HasValue)
        {
            ClientLog.Debug($"REMOTE ITEM SIGNAL SKIPPED: '{normalizedItemName}'; sender slot could not be read from ItemInfo.Player.");
            return;
        }

        if (!_localPlayerSlot.HasValue)
        {
            ClientLog.Debug($"REMOTE ITEM SIGNAL SKIPPED: '{normalizedItemName}'; local Archipelago slot is unknown.");
            return;
        }

        if (senderSlot.Value == _localPlayerSlot.Value)
        {
            ClientLog.Debug($"REMOTE ITEM SIGNAL SKIPPED: '{normalizedItemName}'; item is from the local slot {senderSlot.Value}, not another player.");
            return;
        }

        byte gameMode = _gdb.ReadByte(GameModeAddress);
        byte levelState = _gdb.ReadByte(LevelStateAddress);
        byte puzzleVsMode = _gdb.ReadByte(PuzzleVsModeAddress);
        byte storyContestMode = _gdb.ReadByte(StoryContestModeAddress);
        bool activeLevel = IsActivelyPlayingLevel(gameMode, levelState, puzzleVsMode, storyContestMode);

        string normalizedLower = normalizedItemName.Replace('-', ' ').ToLowerInvariant();
        bool isGoodItem = normalizedItemName.Equals("Star-Bubble", StringComparison.OrdinalIgnoreCase)
            || normalizedItemName.Equals("Metal-Bubble", StringComparison.OrdinalIgnoreCase)
            || normalizedItemName.Equals("Level-Skip", StringComparison.OrdinalIgnoreCase)
            || normalizedItemName.Equals("Pointer-Guide", StringComparison.OrdinalIgnoreCase);
        bool isTrap = IsTrapItemName(normalizedLower);

        if (!isGoodItem && !isTrap)
        {
            ClientLog.Debug(
                $"REMOTE ITEM SIGNAL SKIPPED: '{normalizedItemName}' from slot {senderSlot.Value}; item is not in the good-item or trap signal list. " +
                $"isGoodItem={isGoodItem}; isTrap={isTrap}.");
            return;
        }

        if (!activeLevel)
        {
            ClientLog.Debug(
                $"REMOTE ITEM SIGNAL SKIPPED: '{normalizedItemName}' from slot {senderSlot.Value}; player is not in an active level. " +
                $"0x{GameModeAddress:X8}={gameMode:X2}; 0x{LevelStateAddress:X8}={levelState:X2}; " +
                $"0x{PuzzleVsModeAddress:X8}={puzzleVsMode:X2}; 0x{StoryContestModeAddress:X8}={storyContestMode:X2}. " +
                "No signal address was modified.");
            return;
        }

        uint address = isGoodItem ? RemoteGoodItemSignalAddress : VersusResultAnimationAddress;
        byte targetValue = isGoodItem ? RemoteGoodItemSignalValue : RemoteTrapItemSignalValue;
        byte currentValue = _gdb.ReadByte(address);

        if (currentValue == targetValue)
        {
            _gdb.WriteByte(address, 0x00);
            ClientLog.Debug(
                $"REMOTE ITEM SIGNAL PULSE: '{normalizedItemName}' from slot {senderSlot.Value}; " +
                $"0x{address:X8} already contained {targetValue:X2}; wrote 00 and waiting {RemoteItemSignalPulseDelay.TotalMilliseconds:0} ms.");
            Thread.Sleep(RemoteItemSignalPulseDelay);
        }

        _gdb.WriteByte(address, targetValue);
        ClientLog.Debug(
            $"REMOTE ITEM SIGNAL APPLIED: '{normalizedItemName}' from slot {senderSlot.Value} to local slot {_localPlayerSlot.Value}; " +
            $"mode=0x{gameMode:X2}, levelState=0x{levelState:X2}, Puzzle/VS=0x{puzzleVsMode:X2}, Story/Contest=0x{storyContestMode:X2}; " +
            $"0x{address:X8} {currentValue:X2} -> " +
            (currentValue == targetValue ? $"00 -> {targetValue:X2}" : $"{targetValue:X2}") + ".");
    }

    private static bool IsGameplayEffectItem(string itemName)
    {
        string normalized = itemName.Trim()
            .Replace("FILL_", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "-");
        // Level Skip is deliberately excluded: it increments the skip counter immediately,
        // even when the player is between levels. Only the seven gameplay-effect items below
        // share the one-slot replace-latest delay.
        // Pointer Guide has a separate result-animation gate and state-aware logic
        // for level initialization (01) and board-specific gameplay application (02).
        return normalized.Equals("Star-Bubble", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Metal-Bubble", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Rainbow-Bubble", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Obstacle-Block", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Random-Color-Bubble", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Falling-Ceiling", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResultAnimationBlockingItemReceipt()
    {
        if (_gdb is null)
            return false;

        byte resultAnimation = _gdb.ReadByte(VersusResultAnimationAddress);
        return resultAnimation is 0x07 or 0x08;
    }

    // Shared mode identification: 0x00167A0C must agree with the mode flags.
    private static bool IsExpectedActiveMode(byte gameMode, byte puzzleVsMode, byte storyContestMode)
    {
        int expectedGameMode = (puzzleVsMode, storyContestMode) switch
        {
            (0x00, 0x00) => InLevelMode,           // Puzzle/Arcade: 03
            (0x00, 0x01) => EnteredLetter1State,  // Puzzle/Story: 05
            (0x01, 0x00) => 0x07,                 // VS/Story: 07
            (0x01, 0x01) => 0x04,                 // VS/Contest: 04
            _ => -1
        };
        return gameMode == expectedGameMode;
    }

    private static bool IsActivelyPlayingLevel()
    {
        if (_gdb is null)
            return false;

        byte puzzleVsMode = _gdb.ReadByte(PuzzleVsModeAddress);
        byte storyContestMode = _gdb.ReadByte(StoryContestModeAddress);
        byte gameMode = _gdb.ReadByte(GameModeAddress);
        byte levelState = _gdb.ReadByte(LevelStateAddress);
        return IsActivelyPlayingLevel(gameMode, levelState, puzzleVsMode, storyContestMode);
    }

    private static bool IsActivelyPlayingLevel(byte gameMode, byte levelState, byte puzzleVsMode, byte storyContestMode)
    {
        return levelState == 0x02 && IsExpectedActiveMode(gameMode, puzzleVsMode, storyContestMode);
    }

    private static void ProcessPendingDelayedGameplayItem(
        byte gameMode, byte levelState, byte puzzleVsMode, byte storyContestMode)
    {
        if (IsResultAnimationBlockingItemReceipt() ||
            _pendingDelayedGameplayItem is null ||
            !IsActivelyPlayingLevel(gameMode, levelState, puzzleVsMode, storyContestMode))
            return;

        string itemName = _pendingDelayedGameplayItem;
        _pendingDelayedGameplayItem = null;
        ClientLog.Debug($"ITEM DELAY: applying pending item {itemName} in active gameplay.");
        ApplyReceivedItemNow(itemName);
    }

    // Pointer Guide's secondary check selects a ceiling-slot address by board type.
    // During level initialization (state 01), it is applied immediately as 80.
    // During active gameplay (state 02), FF in the selected slot applies it as 82;
    // any other value applies it as 80.
    private static void ApplyPointerGuideForActiveGameplay()
    {
        if (_gdb is null)
            return;

        byte boardType = _gdb.ReadByte(BoardTypeAddress);
        uint ceilingSlotAddress = boardType == 0x01
            ? PointerGuideWideCeilingSlotAddress
            : PointerGuideCeilingSlotAddress;
        byte ceilingSlotValue = _gdb.ReadByte(ceilingSlotAddress);
        byte pointerGuideValue = ceilingSlotValue == 0xFF ? (byte)0x82 : (byte)0x80;

        _gdb.WriteByte(PointerGuideAddress, pointerGuideValue);
        _pendingPointerGuide = false;

        ClientLog.Debug(
            $"Pointer Guide applied during gameplay: boardType 0x{BoardTypeAddress:X8}={boardType:X2}; " +
            $"checked 0x{ceilingSlotAddress:X8}={ceilingSlotValue:X2}; " +
            $"wrote 0x{PointerGuideAddress:X8}={pointerGuideValue:X2}.");
    }

    private static void ProcessPendingPointerGuideDelay(
        byte gameMode, byte levelState, byte puzzleVsMode, byte storyContestMode)
    {
        if (!_pendingPointerGuide || _gdb is null ||
            IsResultAnimationBlockingItemReceipt() ||
            !IsExpectedActiveMode(gameMode, puzzleVsMode, storyContestMode))
            return;

        // State 01 is the next level loading phase; apply immediately without checking board type or ceiling slots.
        // State 00/03 is not a valid application phase, so keep the guide pending.
        if (levelState == 0x01)
        {
            _gdb.WriteByte(PointerGuideAddress, 0x80);
            _pendingPointerGuide = false;
            ClientLog.Debug("ITEM DELAY: pending Pointer Guide applied immediately during level initialization (0x00167A10=01); board/ceiling-slot checks bypassed.");
            return;
        }

        if (!IsActivelyPlayingLevel(gameMode, levelState, puzzleVsMode, storyContestMode))
            return;

        ApplyPointerGuideForActiveGameplay();
    }

    private static void ApplyReceivedItemNow(string itemName, string? stableItemKey = null)
    {
        // Gameplay-effect Fill/Trap items reach this method only after their
        // animation and active-gameplay gates have cleared. Pointer Guide has its own
        // result-animation gate plus its board-specific ceiling-slot application rule.
        // Progression items (world/character unlocks and goal progress) remain immediate.
        string normalizedItemName = itemName.Trim()
            .Replace("FILL_", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "-");

        if (string.Equals(normalizedItemName, "Pointer-Guide", StringComparison.OrdinalIgnoreCase))
        {
            if (_gdb is null)
            {
                WriteClientLine("WARNING: Pointer Guide received, but DuckStation memory is not connected.");
                return;
            }

            // Defensive second check: if the result animation began after the item
            // passed HandleReceivedItem, keep it pending instead of applying it now.
            if (IsResultAnimationBlockingItemReceipt())
            {
                _pendingPointerGuide = true;
                ClientLog.Debug("ITEM DELAY: Pointer Guide retained because the result animation (0x00192EBF=07/08) is active.");
                return;
            }

            byte gameMode = _gdb.ReadByte(GameModeAddress);
            byte puzzleVsMode = _gdb.ReadByte(PuzzleVsModeAddress);
            byte storyContestMode = _gdb.ReadByte(StoryContestModeAddress);
            byte currentLevelState = _gdb.ReadByte(LevelStateAddress);

            if (!IsExpectedActiveMode(gameMode, puzzleVsMode, storyContestMode))
            {
                _pendingPointerGuide = true;
                ClientLog.Debug($"ITEM DELAY: Pointer Guide retained; mode not recognized (0x{GameModeAddress:X8}={gameMode:X2}, Puzzle/VS={puzzleVsMode:X2}, Story/Contest={storyContestMode:X2}).");
            }
            else if (currentLevelState == 0x01)
            {
                _gdb.WriteByte(PointerGuideAddress, 0x80);
                _pendingPointerGuide = false;
                ClientLog.Debug("Received item: Pointer Guide applied during next-level loading (0x00167A10=01); board/ceiling-slot checks bypassed.");
            }
            else if (currentLevelState == 0x02)
            {
                ApplyPointerGuideForActiveGameplay();
            }
            else
            {
                _pendingPointerGuide = true;
                ClientLog.Debug($"ITEM DELAY: Pointer Guide retained until next level loading; 0x00167A10={currentLevelState:X2}.");
            }
            return;
        }
        if (string.Equals(normalizedItemName, "Star-Bubble", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedItemName, "Metal-Bubble", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedItemName, "Rainbow-Bubble", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedItemName, "Obstacle-Block", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedItemName, "Random-Color-Bubble", StringComparison.OrdinalIgnoreCase))
        {
            if (_gdb is null)
            {
                WriteClientLine($"WARNING: {itemName} received, but DuckStation memory is not connected.");
                return;
            }

            byte bubbleCode = normalizedItemName switch
            {
                "Star-Bubble" => 0x0A,
                "Metal-Bubble" => 0x0B,
                "Rainbow-Bubble" => 0x0C,
                "Obstacle-Block" => 0x0D,
                _ => (byte)Random.Shared.Next(0x01, 0x09)
            };
            _gdb.WriteByte(NextBubbleAddress, bubbleCode);
            ClientLog.Debug($"Received item: {normalizedItemName.Replace("-", " ")} applied (value=0x{bubbleCode:X2}).");
            return;
        }

        // Falling Ceiling is a trap with mode-specific memory behavior. In VS modes,
        // increment the dedicated byte by 07; Puzzle modes retain the original trigger.
        if (string.Equals(normalizedItemName, "Falling-Ceiling", StringComparison.OrdinalIgnoreCase))
        {
            if (_gdb is not null)
                ApplyFallingCeiling(_gdb, "RECEIVED TRAP: Falling Ceiling applied.");
            else
                WriteClientLine("WARNING: Falling Ceiling received, but DuckStation memory is not connected.");
            return;
        }

        // Unlock items must change the client's real navigation lock state, not
        // merely appear in the Archipelago item log. Accept the normal item name
        // and the FILL_ prefix some client/server paths may expose.
        Match unlockMatch = Regex.Match(
            itemName,
            @"^(?:FILL[_ ]*)?Unlock\s+(?:World\s+([A-Z?])|([A-Z?])\s+world)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (unlockMatch.Success)
        {
            string letter = (unlockMatch.Groups[1].Success ? unlockMatch.Groups[1].Value : unlockMatch.Groups[2].Value).ToUpperInvariant();
            _receivedUnlockLetters.Add(letter);
            RefreshUnlockedLetters();
            return;
        }

        // Progressive Enemy opens the next VS/Story opponent in the configured order.
        // Bob is always available; Secret is inserted before Madam Luna when enabled.
        string progressiveEnemyItem = itemName.Trim();
        if (progressiveEnemyItem.StartsWith("FILL_", StringComparison.OrdinalIgnoreCase))
            progressiveEnemyItem = progressiveEnemyItem[5..].Trim();
        if (string.Equals(progressiveEnemyItem, "Progressive Enemy", StringComparison.OrdinalIgnoreCase))
        {
            UnlockNextVsStoryEnemy();
            return;
        }

        // VS/Story enemy unlock items are progression items in Complete mode.
        string enemyItemName = itemName.Trim();
        if (enemyItemName.StartsWith("FILL_", StringComparison.OrdinalIgnoreCase)) enemyItemName = enemyItemName[5..].Trim();
        Match enemyUnlockMatch = Regex.Match(enemyItemName, @"^Unlock\s+(.+?)\s+enemy$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (enemyUnlockMatch.Success)
        {
            string enemyName = enemyUnlockMatch.Groups[1].Value.Trim();
            var match = VsStoryEnemyNamesById.FirstOrDefault(pair => string.Equals(pair.Value, enemyName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Value) && match.Key != 0x00)
            {
                if (_unlockedVsStoryEnemyIds.Add(match.Key))
                    ClientLog.Debug($"VS STORY ENEMY UNLOCK RECEIVED: {enemyName} (ID=0x{match.Key:X2}).");
            }
            return;
        }

        // Tarot-card unlocks are AP progression items in Puzzle/Story mode.
        string tarotItemName = itemName.Trim();
        if (tarotItemName.StartsWith("FILL_", StringComparison.OrdinalIgnoreCase)) tarotItemName = tarotItemName[5..].Trim();
        Match tarotUnlockMatch = Regex.Match(tarotItemName, @"^Unlock Tarot (.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (tarotUnlockMatch.Success)
        {
            string cardName = tarotUnlockMatch.Groups[1].Value.Trim();
            PuzzleStoryCard? card = PuzzleStoryCards.FirstOrDefault(candidate => string.Equals(candidate.Name, cardName, StringComparison.OrdinalIgnoreCase));
            if (card is not null && _puzzleStoryUnlockedCards.Add(card.Id))
                ClientLog.Debug($"PUZZLE/STORY CARD UNLOCK RECEIVED: {card.Name} (ID=0x{card.Id:X2}).");
            return;
        }

        // Simple-mode global unlocks are separate progression items. Each one
        // unlocks all 16 playable characters in its own VS mode only.
        if (string.Equals(normalizedItemName, "Unlock-VS/Story", StringComparison.OrdinalIgnoreCase))
        {
            if (!_receivedVsStoryGlobalUnlock)
            {
                _receivedVsStoryGlobalUnlock = true;
                ClientLog.Debug("VS/STORY GLOBAL UNLOCK RECEIVED: all 16 playable characters are now unlocked in VS/Story Simple.");
                if (_currentVsStoryMode && _isVsModeActive && _gameModesOptions.VsStorySimple == 1 && _gdb is not null)
                {
                    ForceUnlockAllVsCharacters = true;
                    ApplyCharacterUnlockFlags(_gdb, isVsMode: true);
                }
            }
            return;
        }

        if (string.Equals(normalizedItemName, "Unlock-VS/Contest", StringComparison.OrdinalIgnoreCase))
        {
            if (!_receivedVsContestGlobalUnlock)
            {
                _receivedVsContestGlobalUnlock = true;
                ClientLog.Debug("VS/CONTEST GLOBAL UNLOCK RECEIVED: all 16 playable characters are now unlocked in VS/Contest Simple.");
                if (_currentVsContestMode && _isVsModeActive && _gameModesOptions.VsContestSimple == 1 && _gdb is not null)
                {
                    ForceUnlockAllVsCharacters = true;
                    ApplyCharacterUnlockFlags(_gdb, isVsMode: true);
                }
            }
            return;
        }

        // Character unlock tracks are deliberately mode-specific. A Contest unlock
        // must never unlock the same character in VS/Story, or vice versa.
        string characterItemName = itemName.Trim();
        if (characterItemName.StartsWith("FILL_", StringComparison.OrdinalIgnoreCase))
            characterItemName = characterItemName[5..].Trim();
        Match storyCharacterMatch = Regex.Match(characterItemName, @"^Unlock VS/Story Character (.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        Match contestCharacterMatch = Regex.Match(characterItemName, @"^Unlock VS/Contest Character (.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (storyCharacterMatch.Success || contestCharacterMatch.Success)
        {
            bool isStoryUnlock = storyCharacterMatch.Success;
            string characterName = (isStoryUnlock ? storyCharacterMatch.Groups[1].Value : contestCharacterMatch.Groups[1].Value).Trim();
            HashSet<byte> destination = isStoryUnlock ? _receivedVsStoryCharacterUnlockIds : _receivedVsContestCharacterUnlockIds;
            if (VsCharacterIdsByName.TryGetValue(characterName, out byte characterId))
            {
                bool newlyUnlocked = destination.Add(characterId);
                if (newlyUnlocked)
                {
                    string modeLabel = isStoryUnlock ? "VS/Story" : "VS/Contest";
                    ClientLog.Debug($"{modeLabel.ToUpperInvariant()} CHARACTER UNLOCK RECEIVED: {characterName} (ID=0x{characterId:X2}).");
                    bool unlockAppliesToActiveMode = isStoryUnlock ? _currentVsStoryMode : _currentVsContestMode;
                    if (unlockAppliesToActiveMode && _isVsModeActive && _gdb is not null)
                        ApplyCharacterUnlockFlags(_gdb, isVsMode: true);
                }
                return;
            }
        }

        if (normalizedItemName.StartsWith("Victory-", StringComparison.OrdinalIgnoreCase))
        {
            string canonicalVictoryItemName = itemName.Trim();
            if (canonicalVictoryItemName.StartsWith("FILL_", StringComparison.OrdinalIgnoreCase))
                canonicalVictoryItemName = canonicalVictoryItemName[5..].Trim();
            _receivedGoalVictoryItems.Add(canonicalVictoryItemName);
            if (_requiredGoalVictoryItems.Count > 0 && !_goalCompletionHandled &&
                _requiredGoalVictoryItems.All(_receivedGoalVictoryItems.Contains))
            {
                HandleGoalReached();
            }

            Match victoryMatch = Regex.Match(
                itemName,
                @"^Victory\s*-\s*Complete\s+World\s+([A-Z?])$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (victoryMatch.Success)
            {
                string victoryLetter = victoryMatch.Groups[1].Value.ToUpperInvariant();
                _receivedVictoryLetters.Add(victoryLetter);
                ClientLog.Debug($"Victory progress received for world {victoryLetter}. Goal progress: {string.Join(", ", _receivedVictoryLetters)}.");
                if (_serverGoalType.Trim().Replace('-', '_').Replace(' ', '_').Equals("all_u_z", StringComparison.OrdinalIgnoreCase))
                    WriteGoalSummary();
                if (_requiredGoalVictoryItems.Count == 0 && !_goalCompletionHandled && _goalLetters.Count > 0 &&
                    _goalLetters.All(_receivedVictoryLetters.Contains))
                    HandleGoalReached();
            }
            return;
        }

        if (!string.Equals(normalizedItemName, "Level-Skip", StringComparison.OrdinalIgnoreCase)) return;
        if (!string.IsNullOrWhiteSpace(stableItemKey) && !_processedLevelSkipItemKeys.Add(stableItemKey))
        {
            ClientLog.Debug($"Duplicate Level Skip replay ignored (item key {stableItemKey}). Current uses: {_levelSkipCount:00}.");
            return;
        }
        if (_levelSkipCount >= 99)
        {
            ClientLog.Debug("A hidden usable item arrived, but the counter is already at its maximum (99). Nothing was added.");
            return;
        }

        _levelSkipCount++;
        SaveLevelSkipState();
        if (_gdb is not null && _skipHudInitialized)
            UpdateSkipCounterHud(_gdb);
        WriteClientLine($"Received item: Level Skip. Available uses: {_levelSkipCount:00}.");
    }

    private static bool TryUseLevelSkip(DuckstationMemory gdb, ref byte levelState, byte letter, byte levelNumber)
    {
        // Level Skip is always player-triggered with R2. If the server has already
        // confirmed this level's check, the same input advances for free; otherwise
        // a normal Level Skip item is required and consumed.
        string letterName = GetLetterName(letter);
        int displayedLevel = levelNumber + 1;
        string currentLevelId = $"{letterName}{displayedLevel}";
        byte puzzleVsMode = gdb.ReadByte(PuzzleVsModeAddress);
        bool isVsMode = puzzleVsMode == 0x01;
        bool isPuzzleStory = puzzleVsMode == 0x00 &&
            gdb.ReadByte(StoryContestModeAddress) == 0x01;
        if (isVsMode)
        {
            // VS/Story and VS/Contest consume a Level Skip through the VS-specific byte,
            // rather than the Puzzle/Arcade 0x00167A10 transition.
            if (_levelSkipCount <= 0)
            {
                ClientLog.Debug("VS Level Skip ignored: no Level Skip uses are available (00).");
                return false;
            }
            gdb.WriteByte(VsLevelSkipTriggerAddress, 0x01);
            _levelSkipCount--;
            SaveLevelSkipState();
            UpdateSkipCounterHud(gdb);
            ClientLog.Debug($"VS LEVEL SKIP USED: 0x{VsLevelSkipTriggerAddress:X8} set to 01; remaining={_levelSkipCount:00}.");
            WriteClientLine($"Level Skip used. Remaining uses: {_levelSkipCount:00}.");
            return true;
        }
        bool alreadyChecked = IsKnownLetterName(letterName) &&
            _confirmedChecks.ContainsKey(currentLevelId);
        string puzzleStoryLevelId = $"{GetPuzzleStoryCardName(gdb.ReadByte(LetterAddress))} {displayedLevel}";
        if (isPuzzleStory && _puzzleStoryChecksLogged.Contains(puzzleStoryLevelId))
            alreadyChecked = true;

        if (!alreadyChecked && _levelSkipCount <= 0)
        {
            ClientLog.Debug("Level Skip input ignored: this level is not checked and no uses are available (00).");
            return false;
        }

        // Puzzle/Story uses cards rather than the letter-world progression. A skip
        // must still advance the current card level and consume one use, but must
        // never prepare a letter-based next-level mode (for example S2/S3).
        // Prepare the next letter-level mode BEFORE requesting the state transition
        // only in modes that use the letter-world progression. DuckStation keeps
        // running between polls, so the game can consume 02 -> 03 before the next poll.
        if (isPuzzleStory)
        {
            ClientLog.Debug($"  Puzzle/Story Level Skip from {currentLevelId}: ignored letter-based next-level preparation; card progression only.");
        }
        else if (IsKnownLetterName(letterName) && displayedLevel < 5)
        {
            string nextLevelId = $"{letterName}{displayedLevel + 1}";
            ApplyModeForLevel(gdb, nextLevelId,
                alreadyChecked ? $"free replay skip from completed level {currentLevelId}" : $"Level Skip usado desde {currentLevelId}");
        }
        else
        {
            ClientLog.Debug($"  Skip input from {currentLevelId}: no next level in the same world will be prepared (final level or unrecognized world).");
        }

        // The player explicitly pressed R2. Never auto-skip on level entry.
        gdb.WriteByte(LevelStateAddress, 0x03);
        levelState = 0x03;

        if (alreadyChecked)
        {
            WriteClientLine($"Free skip: {currentLevelId} was already checked. No Level Skip consumed.");
            ClientLog.Debug($"FREE LEVEL SKIP: R2; {currentLevelId} already confirmed by Archipelago; remaining={_levelSkipCount:00}.");
        }
        else
        {
            _levelSkipCount--;
            SaveLevelSkipState();
            UpdateSkipCounterHud(gdb);
            ClientLog.Debug($"HIDDEN ITEM USED: R2; 0x{LevelStateAddress:X8} set to 03; remaining={_levelSkipCount:00}.");
            WriteClientLine($"Level Skip used. Remaining uses: {_levelSkipCount:00}.");
        }
        return true;
    }

    private static void RegisterPreviouslyCompletedTarotCard(string locationName)
    {
        if (!locationName.EndsWith(" 5", StringComparison.Ordinal))
            return;

        string cardName = locationName[..^2].Trim();
        PuzzleStoryCard? card = PuzzleStoryCards.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, cardName, StringComparison.OrdinalIgnoreCase));
        if (card is not null)
            _puzzleStoryCompletedCards.Add(card.Id);
    }

    private static void RegisterPreviouslyCompletedArcadeLetter(string locationName)
    {
        // Puzzle/Arcade locations use compact names such as A5, B5, or ?5.
        // Only the fifth-level check marks a letter as completed.
        if (locationName.Length != 2 || locationName[1] != '5')
            return;

        string letterName = locationName[..1].ToUpperInvariant();
        if (IsKnownLetterName(letterName) && _puzzleArcadeCompletedLetters.Add(letterName))
            ClientLog.Debug($"Puzzle/Arcade letter restored as completed from Archipelago check: {letterName}5.");
    }

    private sealed record PuzzleStoryCard(string Name, byte Id, byte XLow, byte XHigh, bool IsWorld = false);

    // This order is specific to Puzzle/Story card selection. World uses ID 0x17
    // at 0x00167A8F and its X coordinate is animated separately.
    private static readonly PuzzleStoryCard[] PuzzleStoryCards =
    [
        new("Fool", 0x12, 0x1C, 0x05),
        new("Star", 0x00, 0x0C, 0x06),
        new("Moon", 0x01, 0x14, 0x00),
        new("Hermit", 0x08, 0x76, 0x00),
        new("Tower", 0x02, 0x78, 0x00),
        new("Chariot", 0x06, 0xD0, 0x00),
        new("Death", 0x05, 0x3A, 0x01),
        new("Devil", 0x04, 0x70, 0x01),
        new("Hanged Man", 0x09, 0xB0, 0x01),
        new("Magician", 0x07, 0x20, 0x02),
        new("Wheel of Fortune", 0x0A, 0x58, 0x02),
        new("Judgement", 0x0B, 0x98, 0x02),
        new("Lovers", 0x0D, 0xE8, 0x02),
        new("Temperance", 0x0E, 0xA8, 0x03),
        new("Hierophant", 0x14, 0xF0, 0x03),
        new("Emperor", 0x13, 0xFC, 0x03),
        new("Empress", 0x15, 0x3C, 0x04),
        new("High Priestess", 0x16, 0x44, 0x04),
        new("Sun", 0x0F, 0x98, 0x04),
        new("Justice", 0x10, 0xD0, 0x04),
        new("Strength", 0x11, 0x04, 0x05),
        new("World", 0x17, 0x00, 0x00, true),
    ];

    private static void ApplyTarotDifficultyForGameplay(DuckstationMemory gdb, byte cardId, byte zeroBasedLevel)
    {
        if (zeroBasedLevel > 0x04)
            return; // There is no Tarot level after level 5.

        string cardName = GetPuzzleStoryCardName(cardId);
        string levelId = $"{cardName} {zeroBasedLevel + 1}";
        if (string.Equals(_lastTarotGameplayDifficultyKey, levelId, StringComparison.OrdinalIgnoreCase))
            return;

        if (!_tarotDifficultyPlan.TryGetValue(levelId, out string? difficultyName))
        {
            ClientLog.Debug($"TAROT DIFFICULTY: no assignment for {levelId}; memory was not modified.");
            _lastTarotGameplayDifficultyKey = levelId;
            return;
        }

        bool isExpert = string.Equals(difficultyName, "Expert", StringComparison.OrdinalIgnoreCase);

        // Tarot card levels need both bytes: 0x00167A8C controls the actual
        // gameplay difficulty, while 0x00167A91 controls the displayed text/color.
        // Keep this isolated to Puzzle/Story Tarot logic; letter-world logic below
        // continues using its existing DifficultyAddress behavior unchanged.
        byte gameplayDifficulty = isExpert ? TarotGameplayExpertValue : TarotGameplayNormalValue;
        byte displayedDifficulty = isExpert ? ExpertValue : NormalValue;
        gdb.WriteByte(TarotGameplayDifficultyAddress, gameplayDifficulty);
        gdb.WriteByte(DifficultyAddress, displayedDifficulty);

        byte actualGameplayDifficulty = gdb.ReadByte(TarotGameplayDifficultyAddress);
        byte actualDisplayedDifficulty = gdb.ReadByte(DifficultyAddress);
        ClientLog.Debug(
            $"TAROT DIFFICULTY (gameplay): {levelId} -> {difficultyName}; " +
            $"0x{TarotGameplayDifficultyAddress:X8}={actualGameplayDifficulty:X2}, " +
            $"0x{DifficultyAddress:X8}={actualDisplayedDifficulty:X2} (display/color).");
        _lastTarotGameplayDifficultyKey = levelId;
    }

    private static string GetPuzzleStoryCardName(byte cardId)
    {
        PuzzleStoryCard? card = PuzzleStoryCards.FirstOrDefault(candidate => candidate.Id == cardId);
        return card?.Name ?? $"Carta desconocida (ID 0x{cardId:X2})";
    }

    private static int FindNextUncompletedPuzzleStoryCard(int startIndex, int direction)
    {
        for (int step = 1; step <= PuzzleStoryCards.Length; step++)
        {
            int candidateIndex = (startIndex + direction * step) % PuzzleStoryCards.Length;
            if (candidateIndex < 0)
                candidateIndex += PuzzleStoryCards.Length;

            byte candidateCardId = PuzzleStoryCards[candidateIndex].Id;
            if (_puzzleStoryUnlockedCards.Contains(candidateCardId) &&
                !_puzzleStoryCompletedCards.Contains(candidateCardId))
                return candidateIndex;
        }

        return -1;
    }

    private static void ForcePuzzleStoryCardSelection(DuckstationMemory gdb, int index)
    {
        index = (index % PuzzleStoryCards.Length + PuzzleStoryCards.Length) % PuzzleStoryCards.Length;
        _puzzleStorySelectedCardIndex = index;
        PuzzleStoryCard card = PuzzleStoryCards[index];
        if (card.IsWorld)
        {
            gdb.WriteByte(WorldCardMarkerAddress, WorldCardMarkerValue);
            _puzzleStoryWorldXInitialized = true;
            ClientLog.Debug("Puzzle/Story card selected: World (0x00167A8F=17); starting X-coordinate loop.");
        }
        else
        {
            gdb.WriteByte(LetterAddress, card.Id);
            gdb.WriteByte(CardXLowAddress, card.XLow);
            gdb.WriteByte(CardXHighAddress, card.XHigh);
            _puzzleStoryWorldXInitialized = false;
            ClientLog.Debug($"Puzzle/Story card selected: {card.Name} (0x00167A8F={card.Id:X2}, X={card.XHigh:X2}{card.XLow:X2}).");
        }
    }

    private static void AdvancePuzzleStoryWorldX(DuckstationMemory gdb)
    {
        if (!_puzzleStoryWorldXInitialized)
        {
            _puzzleStoryWorldXInitialized = true;
            return;
        }

        byte low = gdb.ReadByte(CardXLowAddress);
        byte high = gdb.ReadByte(CardXHighAddress);

        // Advance ten coordinate steps per client poll (10x the previous speed).
        // Stop at 0x0639, then wrap both bytes to 0000 and repeat.
        for (int step = 0; step < 10; step++)
        {
            if (low == WorldCardXEndLow && high == WorldCardXEndHigh)
            {
                low = 0x00;
                high = 0x00;
                ClientLog.Debug("Puzzle/Story World X loop reached 0x0639; wrapped X to 0x0000.");
                break;
            }

            if (low == 0xFF)
            {
                low = 0x00;
                high = unchecked((byte)(high + 1));
            }
            else
            {
                low = unchecked((byte)(low + 1));
            }
        }

        gdb.WriteByte(CardXLowAddress, low);
        gdb.WriteByte(CardXHighAddress, high);
    }

    private static byte? FindNextUnlockedVsStoryCharacterId(DuckstationMemory gdb, byte currentCharacterId, int direction)
    {
        for (int step = 1; step <= 16; step++)
        {
            int candidate = (currentCharacterId + (direction * step) + 16) % 16;
            uint unlockAddress = CharacterUnlockFlagsStartAddress + (uint)candidate;
            if (gdb.ReadByte(unlockAddress) == 0x01)
                return (byte)candidate;
        }

        return null;
    }

    private static string GetVsStoryPlayerCharacterName(byte characterId)
    {
        // Player ID 00 is Bub. IDs 01-0F follow the existing VS character ID list.
        if (characterId == 0x00)
            return "Bub";

        return VsSelectableCharacters.FirstOrDefault(character => character.Id == characterId)?.Name
            ?? $"ID {characterId:X2}";
    }

    private static void WriteVsStorySelectionHud(DuckstationMemory gdb, string enemyName)
    {
        // Clear the complete requested HUD window first, then write the fixed labels
        // and the current player/enemy names into their dedicated fields.
        const uint start = 0x0011A000;
        const int length = 0x110; // 0x0011A000 through 0x0011A10F, inclusive.
        byte[] hud = Enumerable.Repeat((byte)0x20, length).ToArray();

        static void PutText(byte[] target, int offset, int fieldLength, string value)
        {
            byte[] encoded = System.Text.Encoding.ASCII.GetBytes(value);
            Array.Copy(encoded, 0, target, offset, Math.Min(encoded.Length, fieldLength));
        }

        byte playerCharacterId = gdb.ReadByte(ActiveCharacterAddress);
        string playerName = GetVsStoryPlayerCharacterName(playerCharacterId);
        PutText(hud, 0x0E9, 7, "you are");
        PutText(hud, 0x0F1, 10, playerName);
        PutText(hud, 0x100, 2, "vs");
        PutText(hud, 0x103, 10, enemyName);

        // The AP world uses "Bob" for player ID 00, although the selection HUD
        // intentionally displays that playable character as "Bub".
        string checkCharacterName = GetVsStoryPlayerCharacterName(playerCharacterId);
        string checkName = _gameModesOptions.VsStoryComplete == 1
            ? $"Defeat {enemyName} as {checkCharacterName}"
            : $"vs {enemyName} win";
        bool checkAlreadySent = _checksLoggedThisRun.ContainsKey(checkName);
        hud[0x10D] = checkAlreadySent ? (byte)0xF6 : (byte)0xCA;

        for (int i = 0; i < hud.Length; i++)
            gdb.WriteByte(start + (uint)i, hud[i]);

        ClientLog.Debug($"VS/Story selection HUD updated: player={playerName} (ID=0x{playerCharacterId:X2}); enemy={enemyName}; check='{checkName}'; 0x0011A10D={(checkAlreadySent ? "F6 (already sent)" : "CA (not sent)")}; range 0x0011A000-0x0011A10F cleared and formatted.");
    }

    private static void WriteVsStoryInLevelHud(DuckstationMemory gdb)
    {
        // During a VS/Story fight, the selection-only "You are Character vs Enemy"
        // banner is hidden. Keep the second-row label as "Skips" and clear the
        // selection text area without touching the skip counter at A017/A018.
        byte[] skips = System.Text.Encoding.ASCII.GetBytes("Skips");
        for (int i = 0; i < skips.Length; i++)
            gdb.WriteByte(0x0011A011u + (uint)i, skips[i]);

        for (uint address = 0x0011A0E9; address <= 0x0011A110; address++)
            gdb.WriteByte(address, 0x20);
    }

    private static void ApplyVsStoryEnemyPosition(DuckstationMemory gdb, int enemyIndex)
    {
        VsStoryEnemy enemy = _activeVsStoryEnemies[enemyIndex];
        // Enemy identity is separate from the character/camera position.
        gdb.WriteByte(0x00167A8A, enemy.EnemyId);
        WriteVsStorySelectionHud(gdb, enemy.Name);
        // Character coordinates are 16-bit little-endian pairs.
        gdb.WriteByte(0x001A4F3A, enemy.PlayerXLow);
        gdb.WriteByte(0x001A4F3B, enemy.PlayerXHigh);
        gdb.WriteByte(0x001A4F3E, enemy.PlayerYLow);
        gdb.WriteByte(0x001A4F3F, enemy.PlayerYHigh);
        // Camera coordinates are likewise written as little-endian byte pairs.
        gdb.WriteByte(0x001A4F5E, enemy.CameraXLow);
        gdb.WriteByte(0x001A4F5F, enemy.CameraXHigh);
        gdb.WriteByte(0x001A4F62, enemy.CameraYLow);
        gdb.WriteByte(0x001A4F63, enemy.CameraYHigh);
    }

    private static void UnlockNextVsStoryEnemy()
    {
        // AP item delivery advances the shared enemy chain by one step.
        // This makes repeated delivery idempotent and respects Secret's position.
        foreach (VsStoryEnemy enemy in _activeVsStoryEnemies)
        {
            if (enemy.EnemyId == 0x00) continue; // Bob is the permanent starting enemy.
            if (_unlockedVsStoryEnemyIds.Contains(enemy.EnemyId)) continue;
            _unlockedVsStoryEnemyIds.Add(enemy.EnemyId);
            ClientLog.Debug($"VS STORY PROGRESSIVE ENEMY: unlocked {enemy.Name} (ID=0x{enemy.EnemyId:X2}).");
            return;
        }
        ClientLog.Debug("VS STORY PROGRESSIVE ENEMY: all selectable enemies are already unlocked.");
    }

    private static void UnlockNextVsStoryEnemyAfterVictory(byte defeatedEnemyId)
    {
        // Simple mode advances locally only when the currently defeated opponent
        // is the last unlocked opponent in the chain. Replaying Bob cannot skip ahead.
        int defeatedIndex = Array.FindIndex(_activeVsStoryEnemies, enemy => enemy.EnemyId == defeatedEnemyId);
        if (defeatedIndex < 0 || defeatedIndex + 1 >= _activeVsStoryEnemies.Length) return;
        VsStoryEnemy nextEnemy = _activeVsStoryEnemies[defeatedIndex + 1];
        bool previousEnemyUnlocked = _unlockedVsStoryEnemyIds.Contains(defeatedEnemyId);
        if (!previousEnemyUnlocked || !_unlockedVsStoryEnemyIds.Add(nextEnemy.EnemyId)) return;
        ClientLog.Debug($"VS STORY SIMPLE: winning against {_activeVsStoryEnemies[defeatedIndex].Name} unlocked {nextEnemy.Name} (ID=0x{nextEnemy.EnemyId:X2}).");
    }

    private static void UpdateVsStorySimpleEnemyUnlocksFromConfirmedChecks(ArchipelagoSession session)
    {
        if (_gameModesOptions.VsStorySimple != 1 || _activeVsStoryEnemies.Length == 0)
            return;

        // Rebuild the Simple-mode enemy progression from Archipelago's authoritative
        // checked locations. The local unlock set is reset on login, so without this
        // reconstruction a reconnect would leave only Bob available even when later
        // enemy victories had already been confirmed by the server.
        int restoredUnlockCount = 0;
        for (int i = 0; i < _activeVsStoryEnemies.Length - 1; i++)
        {
            VsStoryEnemy defeatedEnemy = _activeVsStoryEnemies[i];
            VsStoryEnemy nextEnemy = _activeVsStoryEnemies[i + 1];
            string checkName = $"vs {defeatedEnemy.Name} win";

            // Progression is sequential: a confirmed victory unlocks the next
            // opponent, and reconstruction stops at the first unconfirmed victory.
            if (!_confirmedChecks.ContainsKey(checkName))
                break;

            if (_unlockedVsStoryEnemyIds.Add(nextEnemy.EnemyId))
            {
                restoredUnlockCount++;
                ClientLog.Debug($"VS STORY SIMPLE RESTORE: confirmed victory against {defeatedEnemy.Name} unlocks {nextEnemy.Name} (ID=0x{nextEnemy.EnemyId:X2}).");
            }
        }

        if (restoredUnlockCount > 0)
            WriteClientLine($"VS/Story Simple progression restored: {restoredUnlockCount} opponent(s) unlocked from confirmed Archipelago checks.");

        if (_vsStorySimpleAllEnemyChecksConfirmed)
            return;

        // Preserve the existing all-complete shortcut, including the optional Dreg check.
        // Only include victory locations that exist in this seed and active roster.
        var enemyChecks = new List<string>();
        foreach (VsStoryEnemy enemy in _activeVsStoryEnemies)
        {
            string checkName = $"vs {enemy.Name} win";
            if (session.Locations.GetLocationIdFromName("Puzzle Bobble 4", checkName) >= 0)
                enemyChecks.Add(checkName);
        }

        const string dregCheckName = "vs Dreg win";
        if (session.Locations.GetLocationIdFromName("Puzzle Bobble 4", dregCheckName) >= 0)
            enemyChecks.Add(dregCheckName);

        if (enemyChecks.Count == 0 || !enemyChecks.All(name => _confirmedChecks.ContainsKey(name)))
            return;

        _vsStorySimpleAllEnemyChecksConfirmed = true;
        ClientLog.Debug($"VS/STORY SIMPLE COMPLETE: all {enemyChecks.Count} enemy checks are confirmed by Archipelago; enemy selection may bypass the local win chain.");
        WriteClientLine("VS/Story Simple complete: all opponents are available because every enemy check is already completed.");
    }

    private static int FindNextUnlockedVsStoryEnemyIndex(int startIndex, int direction)
    {
        if (_activeVsStoryEnemies.Length == 0) return -1;
        int index = startIndex;
        for (int checkedCount = 0; checkedCount < _activeVsStoryEnemies.Length; checkedCount++)
        {
            index = (index + direction + _activeVsStoryEnemies.Length) % _activeVsStoryEnemies.Length;
            if (ForceUnlockAllVsStoryEnemies ||
                (_gameModesOptions.VsStorySimple == 1 && _vsStorySimpleAllEnemyChecksConfirmed) ||
                _unlockedVsStoryEnemyIds.Contains(_activeVsStoryEnemies[index].EnemyId))
                return index;
        }
        return -1;
    }

    private static void ApplyCharacterUnlockFlags(DuckstationMemory gdb, bool isVsMode)
    {
        var changedFlags = new List<string>();
        for (byte characterId = 0x00; characterId <= 0x0F; characterId++)
        {
            byte desiredValue;
            if (!isVsMode)
            {
                // Preserve the existing Puzzle-mode behavior: all character flags
                // are restored to unlocked when leaving VS modes.
                desiredValue = 0x01;
            }
            else if (ForceUnlockAllVsCharacters)
            {
                desiredValue = 0x01;
            }
            else
            {
                HashSet<byte> selectedPool = _currentVsStoryMode
                    ? _serverSelectedVsStoryCharacterIds
                    : _serverSelectedContestCharacterIds;
                HashSet<byte> unlockedPool = _currentVsStoryMode
                    ? _receivedVsStoryCharacterUnlockIds
                    : _receivedVsContestCharacterUnlockIds;
                bool inPool = selectedPool.Contains(characterId);
                bool unlocked = unlockedPool.Contains(characterId);
                desiredValue = inPool && unlocked ? (byte)0x01 : (byte)0x00;
            }

            uint address = CharacterUnlockFlagsStartAddress + characterId;
            byte currentValue = gdb.ReadByte(address);
            if (currentValue != desiredValue)
            {
                gdb.WriteByte(address, desiredValue);
                string name = VsSelectableCharacters.First(character => character.Id == characterId).Name;
                changedFlags.Add($"{name}=0x{desiredValue:X2} at 0x{address:X8}");
            }
        }

        if (changedFlags.Count > 0)
        {
            string scope = isVsMode ? (ForceUnlockAllVsCharacters ? "VS all playable characters unlocked" : "VS character locks applied") : "Puzzle-mode character flags restored";
            ClientLog.Debug($"{scope}: {string.Join(", ", changedFlags)}.");
        }
    }

    private static void PollGameState()
    {
        var gdb = _gdb!;

        // Global behavior, independent of the active game mode: keep this byte at
        // 00 so character selection does not hide locked characters. Only write
        // when the game changes it, avoiding unnecessary writes on every poll.
        byte characterUnlockVisibility = gdb.ReadByte(CharacterUnlockVisibilityAddress);
        if (characterUnlockVisibility != 0x00)
        {
            gdb.WriteByte(CharacterUnlockVisibilityAddress, 0x00);
            ClientLog.Debug($"CHARACTER VISIBILITY: 0x{CharacterUnlockVisibilityAddress:X8} changed from {characterUnlockVisibility:X2} to 00.");
        }

        // Read all relevant bytes each poll. State at 0x00167A10 is deliberately
        // treated as a transition, not as a persistent victory flag.
        byte gameMode = gdb.ReadByte(GameModeAddress);
        byte levelState = gdb.ReadByte(LevelStateAddress);
        byte letter = gdb.ReadByte(LetterAddress);
        byte levelNumber = gdb.ReadByte(LevelNumberAddress);
        byte facing = gdb.ReadByte(FacingAddress);
        byte controllerInput = gdb.ReadByte(ControllerInputAddress);
        byte verticalControllerInput = gdb.ReadByte(ControllerVerticalInputAddress);
        byte puzzleVsMode = gdb.ReadByte(PuzzleVsModeAddress);
        byte storyContestMode = gdb.ReadByte(StoryContestModeAddress);

        // Character unlock flags are shared game memory, so scope the lock system
        // strictly to VS modes. Simple modes keep every character locked until their
        // own global unlock item is received; Complete modes use per-character unlocks.
        // Secret (enemy ID 0x10) is separate from the 16 playable characters.
        bool isVsMode = puzzleVsMode == 0x01;
        bool isVsStory = isVsMode && storyContestMode == 0x00;
        bool isVsContest = isVsMode && storyContestMode == 0x01;
        bool isPuzzleArcade = puzzleVsMode == 0x00 && storyContestMode == 0x00;
        _isVsModeActive = isVsMode;

        // The in-level VS/Story HUD reset used to live inside the local test-command
        // handler. Keep this gameplay behavior in the normal poll loop instead.
        if (isVsStory && gameMode == 0x07)
            WriteVsStoryInLevelHud(gdb);
        bool leavingVsStory = _currentVsStoryMode == true && !isVsStory;
        bool modeChanged = _previousVersusModeActive is null || _previousVersusModeActive.Value != isVsMode ||
            _currentVsStoryMode != isVsStory || _currentVsContestMode != isVsContest;
        if (modeChanged)
        {
            if (leavingVsStory)
            {
                // VS/Story owns a custom HUD layout. When switching to any other
                // mode, restore the normal two-row Level Skip display immediately
                // so the selection banner/blanked text cannot leak into that mode.
                _vsStoryHudLayoutActive = false;
                RestorePuzzleArcadeSkipHud(gdb);
                ClientLog.Debug("VS/Story HUD restored: left VS/Story; normal ' Skips NN' rows reinstated for the new mode.");
            }

            _currentVsStoryMode = isVsStory;
            _currentVsContestMode = isVsContest;
            ForceUnlockAllVsCharacters = !isVsMode ||
                (isVsStory && _gameModesOptions.VsStorySimple == 1 && _receivedVsStoryGlobalUnlock) ||
                (isVsContest && _gameModesOptions.VsContestSimple == 1 && _receivedVsContestGlobalUnlock);
            ForceUnlockAllVsStoryEnemies = !isVsStory;
            _previousVersusModeActive = isVsMode;
            ClientLog.Debug($"MODE STATE: 0x{PuzzleVsModeAddress:X8}={puzzleVsMode:X2}; 0x{StoryContestModeAddress:X8}={storyContestMode:X2}; gameMode=0x{gameMode:X2}; levelState=0x{levelState:X2}; VS/Story={isVsStory}; VS/Contest={isVsContest}; forceUnlockAllCharacters={ForceUnlockAllVsCharacters}.");
            if (isVsContest)
            {
                ClientLog.Debug($"VS/CONTEST CHARACTER POOL: selected IDs=[{string.Join(", ", _serverSelectedContestCharacterIds.OrderBy(id => id).Select(id => $"0x{id:X2}"))}]; received unlock IDs=[{string.Join(", ", _receivedVsContestCharacterUnlockIds.OrderBy(id => id).Select(id => $"0x{id:X2}"))}]; mode={( _gameModesOptions.VsContestComplete == 1 ? "Complete" : "Simple")}.");
            }
        }

        // The ROUND/WINS label belongs exclusively to VS/Contest. Restore ROUND in every other mode.
        ApplyContestHudRoundLabel(gdb, isVsContest);

        // The game can rewrite character-unlock flags while initializing or returning
        // to a VS character-selection screen. Reapply the authoritative mode policy on
        // every poll (the helper writes only bytes whose values actually differ).
        // This reapplies the authoritative mode policy if the game resets flags after
        // initialization: Simple remains globally locked until its item arrives, while
        // Complete continues to enforce the selected character pool and individual items.
        ApplyCharacterUnlockFlags(gdb, isVsMode);

        // VS/Contest checks are real Archipelago milestone locations based on the current
        // consecutive-win counter. A lost streak resets the in-game counter, but already
        // checked milestones remain checked; only newly reached milestones are submitted.
        ProcessContestWinChecks(gdb, isVsContest, confirmedWin: false);
        ProcessVersusWinResult(gdb, isVsStory, isVsContest, gameMode, levelState);
        ProcessContestEnemyRandomizer(gdb, isVsContest, gameMode, levelState);
        ApplyContestEnemyDifficulty(gdb, isVsContest, gameMode, levelState);
        ApplyPackyFix(gdb, isVsStory, isVsContest, gameMode, levelState);
        ApplySecretVsStoryFix(gdb, isVsStory, gameMode, levelState);

        // Optional Tamtam buff: increase only newly dealt damage by 30% during active
        // VS/Story or VS/Contest gameplay when the selected player character is Tamtam (ID 04).
        // Round the bonus up using integer arithmetic. The address is also used as the VS
        // Level Skip signal, so reset the baseline on R2 input.
        bool isActiveTamtamFight = _vsStoryOptions.BuffTamtam == 1 &&
                                   ((isVsStory && gameMode == 0x07) ||
                                    (isVsContest && gameMode == 0x04)) &&
                                   levelState == 0x02 &&
                                   gdb.ReadByte(ActiveCharacterAddress) == 0x04;
        if (_lastTamtamBuffActiveForDebug != isActiveTamtamFight)
        {
            byte debugCharacter = gdb.ReadByte(ActiveCharacterAddress);
            byte debugDamage = gdb.ReadByte(VsLevelSkipAddress);
            ClientLog.Debug(
                $"TAMTAM BUFF DEBUG: active={isActiveTamtamFight}; option BuffTamtam={_vsStoryOptions.BuffTamtam}; " +
                $"isVsStory={isVsStory}; isVsContest={isVsContest}; gameMode=0x{gameMode:X2}; levelState=0x{levelState:X2}; " +
                $"character 0x{ActiveCharacterAddress:X8}=0x{debugCharacter:X2}; damage 0x{VsLevelSkipAddress:X8}=0x{debugDamage:X2}.");
            _lastTamtamBuffActiveForDebug = isActiveTamtamFight;
        }

        if (isActiveTamtamFight)
        {
            if (controllerInput == InputR2)
            {
                ClientLog.Debug(
                    $"TAMTAM BUFF DEBUG: R2 detected; damage baseline reset. " +
                    $"0x{VsLevelSkipAddress:X8}={gdb.ReadByte(VsLevelSkipAddress):X2}.");
                _previousTamtamOutgoingDamageValue = null;
            }
            else
            {
                byte currentDamage = gdb.ReadByte(VsLevelSkipAddress);
                if (_previousTamtamOutgoingDamageValue is null)
                {
                    // Establish a baseline on entry; don't retroactively buff old damage.
                    _previousTamtamOutgoingDamageValue = currentDamage;
                    ClientLog.Debug($"TAMTAM BUFF DEBUG: baseline established at 0x{VsLevelSkipAddress:X8}={currentDamage:X2}.");
                }
                else
                {
                    byte previousDamage = _previousTamtamOutgoingDamageValue.Value;
                    if (currentDamage > previousDamage)
                    {
                        int newlyDealtDamage = currentDamage - previousDamage;
                        // ceil(30% of the new damage), using integer arithmetic only:
                        // ceil(newlyDealtDamage * 3 / 10) == (newlyDealtDamage * 3 + 9) / 10.
                        int bonusDamage = (newlyDealtDamage * 3 + 9) / 10;
                        int increasedDamage = Math.Min(0xFF, currentDamage + bonusDamage);
                        byte increasedDamageValue = (byte)increasedDamage;
                        gdb.WriteByte(VsLevelSkipAddress, increasedDamageValue);
                        ClientLog.Debug(
                            $"TAMTAM BUFF APPLIED: character=04; mode={(isVsStory ? "VS/Story" : "VS/Contest")}; " +
                            $"damage 0x{VsLevelSkipAddress:X8}: previous={previousDamage:X2}, gameValue={currentDamage:X2}, " +
                            $"newDamage=+{newlyDealtDamage}, bonus=+{bonusDamage}, written={increasedDamageValue:X2}.");
                        // Track our post-write value so this same increase is not buffed twice.
                        _previousTamtamOutgoingDamageValue = increasedDamageValue;
                    }
                    else
                    {
                        if (currentDamage < previousDamage)
                        {
                            ClientLog.Debug(
                                $"TAMTAM BUFF DEBUG: damage counter decreased/reset at 0x{VsLevelSkipAddress:X8}: " +
                                $"{previousDamage:X2} -> {currentDamage:X2}; new baseline established.");
                        }
                        // Equal = no new damage; lower = reset/other game-side change. Don't write.
                        _previousTamtamOutgoingDamageValue = currentDamage;
                    }
                }
            }
        }
        else
        {
            _previousTamtamOutgoingDamageValue = null;
        }
        _vsStoryHudLayoutActive = isVsStory;
        // VS/Story can clear the two ASCII counter bytes when a fight ends and
        // the player returns through character selection. Reassert only the
        // dedicated second-row digits every poll; the first row belongs to the
        // selected enemy name and must never be overwritten here.
        if (isVsStory && _skipHudInitialized)
            UpdateSkipCounterHud(gdb);

        if (isPuzzleArcade && _previousPuzzleArcadeMode != true)
        {
            RestorePuzzleArcadeSkipHud(gdb);
        }
        _previousPuzzleArcadeMode = isPuzzleArcade;

        // Some unlocked extra characters (IDs 0A-0F) are not valid for VS/Story.
        // If one reaches VS/Story state 09, reset the game once to avoid entering an
        // invalid continuation with an extra character. Keep this VS/Story-only.
        if (!isVsStory || gameMode != 0x09)
        {
            _vsStoryExtraCharacterCreditsResetTriggered = false;
        }
        else if (!_vsStoryExtraCharacterCreditsResetTriggered)
        {
            byte creditsCharacter = gdb.ReadByte(ActiveCharacterAddress);
            if (creditsCharacter >= 0x0A && creditsCharacter <= 0x0F)
            {
                gdb.WriteByte(GameResetAddress, 0x00);
                _vsStoryExtraCharacterCreditsResetTriggered = true;
                ClientLog.Debug($"VS/Story extra-character safeguard: 0x{GameModeAddress:X8}=09 and 0x{ActiveCharacterAddress:X8}={creditsCharacter:X2}; wrote 00 to 0x{GameResetAddress:X8}.");
            }
        }

        if (isVsStory && gameMode == 0x03 && levelState == 0x06)
        {
            gdb.WriteByte(LevelStateAddress, 0x08);
            levelState = 0x08;
            _vsStoryEnemySelectionActive = true;
            _vsStoryXConfirmSeen = false;

            // After a confirmed victory, return the cursor to the opponent just defeated.
            // On a fresh VS/Story entry (or without a stored victory), preserve the default Bob selection.
            int returnEnemyIndex = _vsStoryReturnEnemyId.HasValue
                ? Array.FindIndex(_activeVsStoryEnemies, enemy => enemy.EnemyId == _vsStoryReturnEnemyId.Value)
                : -1;
            _vsStorySelectedEnemyIndex = returnEnemyIndex >= 0 ? returnEnemyIndex : 0;
            string selectedEnemyName = _activeVsStoryEnemies[_vsStorySelectedEnemyIndex].Name;
            ApplyVsStoryEnemyPosition(gdb, _vsStorySelectedEnemyIndex);
            _vsStoryReturnEnemyId = null;
            ClientLog.Debug($"VS/Story enemy selection activated: {selectedEnemyName} (ID=0x{_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].EnemyId:X2}); restored the previously defeated opponent when available (0x00167A0C=03, 0x00167A10 changed 06 -> 08). R1=next enemy; L1=previous enemy; X confirms.");
        }
        // While selecting an enemy, the game may spontaneously change state 08 -> 09.
        // Hold it at 08 unless X (0x0016BCEB == BF) has been observed; after X,
        // allow 09 and leave enemy-selection mode. This latch survives X release
        // until the game completes the 08 -> 09 transition.
        if (_vsStoryEnemySelectionActive && isVsStory && gameMode == 0x03)
        {
            if (controllerInput == InputX)
                _vsStoryXConfirmSeen = true;

            if (levelState == 0x09 && !_vsStoryXConfirmSeen)
            {
                gdb.WriteByte(LevelStateAddress, 0x08);
                levelState = 0x08;
                ClientLog.Debug("VS/Story enemy selection: blocked unexpected 0x00167A10=09; restored 08 because X (0x0016BCEB=BF) was not detected.");
            }
            else if (levelState == 0x09 && _vsStoryXConfirmSeen)
            {
                _vsStoryEnemySelectionActive = false;
                ClientLog.Debug("VS/Story enemy selection confirmed with X (0x0016BCEB=BF); allowing 0x00167A10=09.");
            }
            else if (levelState != 0x08)
            {
                _vsStoryEnemySelectionActive = false;
            }
        }
        else if (!isVsStory || gameMode != 0x03)
        {
            _vsStoryEnemySelectionActive = false;
            _vsStoryXConfirmSeen = false;
        }

        bool isPuzzleStory = puzzleVsMode == 0x00 && storyContestMode == 0x01;
        byte activeGameplayMode = isPuzzleStory ? EnteredLetter1State : InLevelMode;
        // Puzzle/Story first-level entry is the ONLY place where 04 -> 05 applies
        // a card's difficulty. Subsequent levels are applied immediately on the
        // previous level's 02 -> 03 completion transition below; do not wait for
        // another entry transition or a selection-screen state.
        if (isPuzzleStory && _previousLevelState == 0x04 && levelState == EnteredLetter1State)
        {
            ClientLog.Debug($"TAROT FIRST-LEVEL ENTRY: {GetPuzzleStoryCardName(letter)} 1; 0x00167A10 changed 04 -> 05.");
            ApplyTarotDifficultyForGameplay(gdb, letter, 0x00);
        }
        if (!isPuzzleStory)
        if (!isPuzzleStory || gameMode != EnteredLetter1State || levelState != 0x02)
            _lastTarotGameplayDifficultyKey = null;

        // Report the four game modes only when the detected mode changes, avoiding
        // repeated console output while the player remains in the same mode.
        string detectedGameModeLabel = (puzzleVsMode, storyContestMode) switch
        {
            (0x00, 0x00) => "Puzzle/Arcade",
            (0x00, 0x01) => "Puzzle/Story",
            (0x01, 0x00) => "VS/Story",
            (0x01, 0x01) => "VS/Contest",
            _ => $"Unknown mode (0x00167D5B={puzzleVsMode:X2}, 0x00167D5A={storyContestMode:X2})"
        };
        if (!string.Equals(detectedGameModeLabel, _previousDetectedGameModeLabel, StringComparison.Ordinal))
        {
            ClientLog.Debug($"GAME MODE: {detectedGameModeLabel} [0x00167D5B={puzzleVsMode:X2}, 0x00167D5A={storyContestMode:X2}].");
            _previousDetectedGameModeLabel = detectedGameModeLabel;
        }

        // Puzzle/Story card selection: 0x00167A0C=03 and 0x00167A10=02.
        // The game changes state to 03 while moving between cards, then returns to 02
        // when it settles. Always restore Fool after entering selection and after each move.
        bool isPuzzleStoryMode = puzzleVsMode == 0x00 && storyContestMode == 0x01;
        bool wasPuzzleStoryCardSelectionActive = _puzzleStoryCardSelectionActive;
        if (!isPuzzleStoryMode || gameMode != 0x03 || (levelState != 0x02 && levelState != 0x03))
        {
            _puzzleStoryCardSelectionActive = false;
            _puzzleStoryCardMovePending = false;
            _puzzleStoryWorldXInitialized = false;
        }
        else if (levelState == 0x02)
        {
            _puzzleStoryCardSelectionActive = true;
            if (!wasPuzzleStoryCardSelectionActive)
            {
                int firstAvailable = FindNextUncompletedPuzzleStoryCard(-1, 1);
                if (firstAvailable >= 0)
                {
                    ForcePuzzleStoryCardSelection(gdb, firstAvailable);
                    ClientLog.Debug($"Puzzle/Story card selection detected: selected first uncompleted card {PuzzleStoryCards[firstAvailable].Name}.");
                }
                else
                {
                    ClientLog.Debug("Puzzle/Story card selection: all cards are completed; no selectable card remains.");
                }
            }
            else if (_puzzleStoryCardMovePending)
            {
                // Keep the target card after the transition settles, skipping cards
                // whose fifth level has already been completed.
                ForcePuzzleStoryCardSelection(gdb, _puzzleStorySelectedCardIndex);
                _puzzleStoryCardMovePending = false;
                ClientLog.Debug($"Puzzle/Story card movement settled on {PuzzleStoryCards[_puzzleStorySelectedCardIndex].Name}.");
            }

            PuzzleStoryCard selectedTarotCard = PuzzleStoryCards[_puzzleStorySelectedCardIndex];
            if (selectedTarotCard.IsWorld)
                AdvancePuzzleStoryWorldX(gdb);
        }
        else if (levelState == 0x03 && _puzzleStoryCardSelectionActive)
        {
            _puzzleStoryCardMovePending = true;
        }

        if (!_sentInitialContinue)
        {
            // Direct process-memory access does not pause the emulator.
            _sentInitialContinue = true;
            ClientLog.Debug("Direct memory reading is active; DuckStation is not paused.");
        }

        // Maintain the timer globally, but write only once every two seconds so
        // the game can use this address freely between writes (including menus).
        DateTime now = DateTime.UtcNow;
        if (now >= _nextTimerWriteAt)
        {
            gdb.WriteByte(TimerAddress, TimerFreezeValue);
            _nextTimerWriteAt = now + TimerWriteInterval;
        }

        // Initialize the skip-counter HUD once, independently of the timer address.
        // The two primary ASCII digits remain writable for future Level Skip logic;
        // their mirror bytes are synchronized on every poll.
        if (!_skipHudInitialized)
        {
            InitializeSkipHud(gdb);
            _skipHudInitialized = true;
            // InitializeSkipHud writes placeholder zeroes; immediately render the
            // actual count, including Extra_level_skips received from slot data.
            UpdateSkipCounterHud(gdb);
        }
        // Release the one-slot delayed gameplay item only after a mode-specific active-level
        // state is confirmed. Process it before newly queued network items so newer arrivals win.
        ProcessPendingDelayedGameplayItem(gameMode, levelState, puzzleVsMode, storyContestMode);
        ProcessReceivedItems();
        ProcessPendingPointerGuideDelay(gameMode, levelState, puzzleVsMode, storyContestMode);
        SynchronizeSkipCounterMirror(gdb);

        // A received DeathLink is applied only while actively playing a level.
        // Puzzle Bobble 4's safe reset transition is 00; writing 04 directly
        // can trigger game bugs, so never use 04 to force a received death.
        if (ProcessPendingDeathLink(gdb, gameMode, levelState, isPuzzleStory, isVsStory, isVsContest))
        {
            _previousLevelState = 0x00;
            _previousGameMode = gameMode;
            _previousLetterValue = letter;
            _previousFacing = facing;
            _previousControllerInput = controllerInput;
            return;
        }

        // A defeat is identified by the dedicated result-animation byte, not by
        // 0x00167A10. The animation remains at 08 until the level/mode is reset,
        // so send only once and re-arm after the byte leaves 08.
        byte resultAnimation = gdb.ReadByte(VersusResultAnimationAddress);
        // Require an active level in the current mode and active gameplay state.
        // Puzzle/Arcade=03, Puzzle/Story=05, VS/Story=07, VS/Contest=04.
        byte expectedDeathLinkGameMode = isVsStory ? (byte)0x07 : isVsContest ? (byte)0x04 : isPuzzleStory ? EnteredLetter1State : InLevelMode;
        bool isActivelyPlayingLevel = gameMode == expectedDeathLinkGameMode && levelState == 0x02;

        if (_suppressDeathLinkUntilGameplayResumes)
        {
            if (!isActivelyPlayingLevel)
                _deathLinkSuppressionSawGameplayExit = true;
            else if (_deathLinkSuppressionSawGameplayExit && resultAnimation != 0x08)
            {
                // The received DeathLink's reset has completed and a fresh level
                // is active; future defeat animations are genuine local losses.
                _suppressDeathLinkUntilGameplayResumes = false;
                _deathLinkSuppressionSawGameplayExit = false;
                _deathLinkSentForCurrentDefeat = false;
            }
        }
        else if (resultAnimation != 0x08)
        {
            _deathLinkSentForCurrentDefeat = false;
        }
        if (_deathLinkEnabled && _deathLinkService is not null && isActivelyPlayingLevel &&
            resultAnimation == 0x08 && !_deathLinkSentForCurrentDefeat && !_suppressDeathLinkUntilGameplayResumes)
        {
            try
            {
                string cause;
                if (isVsStory || isVsContest)
                {
                    // Read the opponent at the exact moment the defeat is detected.
                    // VS/Story stores the opponent ID at 0x00167A8A;
                    // VS/Contest stores the same ID scheme at 0x00167A46.
                    uint enemyAddress = isVsStory ? VsStoryEnemyIdAddress : ContestEnemyCharacterAddress;
                    byte enemyId = gdb.ReadByte(enemyAddress);
                    string enemyName = GetDeathLinkOpponentName(enemyId);
                    cause = $"{_slotName} lost against {enemyName} in Puzzle Bobble 4";
                }
                else
                {
                    cause = $"{_slotName}'s bubble crossed the deadline in Puzzle Bobble 4";
                }
                _deathLinkService.SendDeathLink(new DeathLink(_slotName, cause));
                _deathLinkSentForCurrentDefeat = true;
                WriteColoredLine($"DeathLink sent: {cause}", DeadLinkColor);
            }
            catch (Exception ex)
            {
                WriteClientLine($"WARNING: Could not send DeathLink: {ex.GetBaseException().Message}");
            }
        }

        // Watch the separate vertical-input byte. Character switching is allowed
        // only in the letter-selection menu and is edge-triggered to avoid repeating
        // while Up/Down is held.
        if (!_previousVerticalControllerInput.HasValue)
        {
            _previousVerticalControllerInput = verticalControllerInput;
        }
        else if (verticalControllerInput != _previousVerticalControllerInput.Value)
        {
            _previousVerticalControllerInput = verticalControllerInput;
            if (verticalControllerInput == InputStart)
            {
                bool skipPuzzleArcadeCredits = isPuzzleArcade && gameMode == 0x06;
                bool skipPuzzleStoryCredits = isPuzzleStory && gameMode == 0x08;
                bool skipVsStoryCredits = isVsStory && gameMode is 0x09 or 0x0A;
                if (skipPuzzleArcadeCredits || skipPuzzleStoryCredits || skipVsStoryCredits)
                {
                    gdb.WriteByte(GameResetAddress, 0x00);
                    string creditsMode = skipPuzzleArcadeCredits ? "Puzzle/Arcade" :
                        skipPuzzleStoryCredits ? "Puzzle/Story" : "VS/Story";
                    ClientLog.Debug($"CREDITS SKIP: Start detected at 0x{ControllerVerticalInputAddress:X8}=F7 in {creditsMode} credits (0x{GameModeAddress:X8}={gameMode:X2}); wrote 00 to 0x{GameResetAddress:X8}.");
                }
            }
            if (gameMode == MenuLetterSelectMode &&
                verticalControllerInput is InputUp or InputDown)
            {
                byte character = gdb.ReadByte(ActiveCharacterAddress);
                byte nextCharacter = verticalControllerInput == InputUp
                    ? (byte)(character == 0x0F ? 0x00 : (character + 1) & 0x0F)
                    : (byte)(character == 0x00 ? 0x0F : (character - 1) & 0x0F);
                gdb.WriteByte(ActiveCharacterAddress, nextCharacter);
                ClientLog.Debug($"Character selection: vertical input 0x{ControllerVerticalInputAddress:X8}={verticalControllerInput:X2}; 0x{ActiveCharacterAddress:X8} {character:X2} -> {nextCharacter:X2}.");
            }
            // In VS/Story enemy selection, the directional byte at 0x0016BCEA
            // mirrors the R1/L1 navigation actions. Trigger only on a new direction
            // value, so holding Left/Right does not repeatedly skip opponents.
            if (_vsStoryEnemySelectionActive && isVsStory && gameMode == 0x03 &&
                levelState == 0x08 && verticalControllerInput is InputUp or InputDown)
            {
                byte currentCharacterId = gdb.ReadByte(ActiveCharacterAddress);
                int direction = verticalControllerInput == InputUp ? 1 : -1;
                byte? nextCharacterId = FindNextUnlockedVsStoryCharacterId(gdb, currentCharacterId, direction);
                string directionLabel = verticalControllerInput == InputUp ? "Up (next character)" : "Down (previous character)";

                if (!nextCharacterId.HasValue)
                {
                    ClientLog.Debug($"VS/Story player-character selection: {directionLabel} ignored; no unlocked characters were found in 0x00187D2C-0x00187D3B.");
                }
                else if (nextCharacterId.Value == currentCharacterId)
                {
                    ClientLog.Debug($"VS/Story player-character selection: {directionLabel} ignored; character 0x{currentCharacterId:X2} is the only unlocked character.");
                }
                else
                {
                    gdb.WriteByte(ActiveCharacterAddress, nextCharacterId.Value);
                    string nextCharacterName = GetVsStoryPlayerCharacterName(nextCharacterId.Value);
                    WriteVsStorySelectionHud(gdb, _activeVsStoryEnemies[_vsStorySelectedEnemyIndex].Name);
                    ClientLog.Debug($"VS/Story player-character selection: {directionLabel} via 0x{ControllerVerticalInputAddress:X8}={verticalControllerInput:X2}; 0x{ActiveCharacterAddress:X8} {currentCharacterId:X2} -> {nextCharacterId.Value:X2} ({nextCharacterName}). Only unlocked VS/Story characters are selectable.");
                }
            }

            if (_vsStoryEnemySelectionActive && isVsStory && gameMode == 0x03 &&
                levelState == 0x08 && verticalControllerInput is InputLeft or InputRight)
            {
                int delta = verticalControllerInput == InputLeft ? -1 : 1;
                int nextEnemyIndex = FindNextUnlockedVsStoryEnemyIndex(_vsStorySelectedEnemyIndex, delta);
                string directionLabel = verticalControllerInput == InputLeft ? "Left (L1 equivalent)" : "Right (R1 equivalent)";
                if (nextEnemyIndex < 0)
                {
                    ClientLog.Debug("VS/Story enemy selection: directional navigation ignored; no available opponents were found by the current enemy-unlock rules.");
                }
                else if (nextEnemyIndex == _vsStorySelectedEnemyIndex)
                {
                    ClientLog.Debug($"VS/Story enemy selection: {directionLabel} ignored; {_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].Name} is the only available opponent under the current enemy-unlock rules.");
                }
                else
                {
                    _vsStorySelectedEnemyIndex = nextEnemyIndex;
                    ApplyVsStoryEnemyPosition(gdb, _vsStorySelectedEnemyIndex);
                    ClientLog.Debug($"VS/Story enemy selection: {directionLabel} via 0x{ControllerVerticalInputAddress:X8}={verticalControllerInput:X2} -> {_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].Name} (ID=0x{_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].EnemyId:X2}; index {_vsStorySelectedEnemyIndex + 1}/{_activeVsStoryEnemies.Length}); enemy position, name and camera updated.");
                }
            }
        }

        // Watch the controller-input byte continuously. Trigger only on a transition
        // into a known button value, so holding a button does not repeat the action.
        if (!_previousControllerInput.HasValue)
        {
            _previousControllerInput = controllerInput;
        }
        else if (controllerInput != _previousControllerInput.Value)
        {
            _previousControllerInput = controllerInput;
            if (_vsStoryEnemySelectionActive && controllerInput is InputL1 or InputR1)
            {
                int delta = controllerInput == InputL1 ? -1 : 1;
                int nextEnemyIndex = FindNextUnlockedVsStoryEnemyIndex(_vsStorySelectedEnemyIndex, delta);
                string directionLabel = controllerInput == InputL1 ? "L1 previous" : "R1 next";
                if (nextEnemyIndex < 0)
                {
                    ClientLog.Debug("VS/Story enemy selection: no available opponents were found by the current enemy-unlock rules.");
                }
                else if (nextEnemyIndex == _vsStorySelectedEnemyIndex)
                {
                    ClientLog.Debug($"VS/Story enemy selection: {_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].Name} is the only available opponent under the current enemy-unlock rules.");
                }
                else
                {
                    _vsStorySelectedEnemyIndex = nextEnemyIndex;
                    ApplyVsStoryEnemyPosition(gdb, _vsStorySelectedEnemyIndex);
                    ClientLog.Debug($"VS/Story enemy selection: {directionLabel} -> {_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].Name} (ID=0x{_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].EnemyId:X2}; enemy unlocked={ForceUnlockAllVsStoryEnemies || _unlockedVsStoryEnemyIds.Contains(_activeVsStoryEnemies[_vsStorySelectedEnemyIndex].EnemyId)}; force-all={ForceUnlockAllVsStoryEnemies}; index {_vsStorySelectedEnemyIndex + 1}/{_activeVsStoryEnemies.Length}); 0x00167A8A, selection HUD 0x0011A000-0x0011A10F, player and camera coordinates updated.");
                }
            }
            else if (_puzzleStoryCardSelectionActive && controllerInput is InputL1 or InputR1)
            {
                int delta = controllerInput == InputL1 ? -1 : 1;
                int nextIndex = FindNextUncompletedPuzzleStoryCard(_puzzleStorySelectedCardIndex, delta);
                if (nextIndex < 0)
                {
                    ClientLog.Debug("Puzzle/Story card navigation ignored: no unlocked, unfinished cards are available.");
                }
                else
                {
                    PuzzleStoryCard nextCard = PuzzleStoryCards[nextIndex];
                    ForcePuzzleStoryCardSelection(gdb, nextIndex);
                    // If the game animates between cards (state 03), re-apply our target
                    // when it settles back to 02. If it stays at 02, the direct write suffices.
                    _puzzleStoryCardMovePending = levelState == 0x03;
                    string directionLabel = controllerInput == InputL1 ? "L1 previous" : "R1 next";
                    ClientLog.Debug($"Puzzle/Story card navigation: {directionLabel} -> {nextCard.Name} (index {nextIndex + 1}/{PuzzleStoryCards.Length}; locked and completed cards skipped).");
                }
            }
            else if (_puzzleStoryCardSelectionActive && controllerInput == InputR2)
            {
                // R2 is reserved for Level Skip during gameplay. In Puzzle/Story card
                // selection it must not navigate or trigger the letter-menu reset.
                ClientLog.Debug($"Puzzle/Story card selection: ignored R2 input (0x{ControllerInputAddress:X8}={controllerInput:X2}).");
            }
            else if (_vsStoryEnemySelectionActive && isVsStory && gameMode == 0x03 && levelState == 0x08 && controllerInput == InputL2)
            {
                // In VS/Story enemy selection, L2 returns to the main menu.
                // Write the three state bytes in the requested order.
                gdb.WriteByte(LevelStateAddress, 0x00);
                gdb.WriteByte(GameResetAddress, 0x02);
                gdb.WriteByte(GameModeAddress, 0x00);
                _vsStoryEnemySelectionActive = false;
                _vsStoryXConfirmSeen = false;
                _vsStorySelectedEnemyIndex = 0;
                _vsStoryReturnEnemyId = null;
                ClientLog.Debug($"VS/Story enemy selection: L2 returned to main menu; cleared the remembered defeated opponent and wrote 00 to 0x{LevelStateAddress:X8}, 02 to 0x{GameResetAddress:X8}, then 00 to 0x{GameModeAddress:X8}.");
                levelState = 0x00;
                gameMode = 0x00;
            }
            else if (_puzzleStoryCardSelectionActive && controllerInput == InputL2)
            {
                gdb.WriteByte(GameResetAddress, 0x00);
                ClientLog.Debug($"Puzzle/Story card selection: L2 requested game reset; 0x{GameResetAddress:X8} set to 00.");
            }
            else if (gameMode == MenuLetterSelectMode && controllerInput == InputL2)
            {
                gdb.WriteByte(GameResetAddress, 0x00);
                ClientLog.Debug($"Letter selection: L2 requested game reset; 0x{GameResetAddress:X8} set to 00.");
            }
            else if (gameMode == MenuLetterSelectMode)
            {
                bool isPuzzleStoryMenu = puzzleVsMode == 0x00 && storyContestMode == 0x01;
                if (isPuzzleStoryMenu && controllerInput == InputR2)
                {
                    ClientLog.Debug($"Puzzle/Story letter selection: ignored R2 input (0x{ControllerInputAddress:X8}={controllerInput:X2}); not returning to Menu0.");
                }
                else
                {
                    ConsoleKey? mappedKey = controllerInput switch
                    {
                        InputL1 => ConsoleKey.LeftArrow,
                        InputR1 => ConsoleKey.RightArrow,
                        InputR2 => ConsoleKey.Backspace,
                        _ => null
                    };
                    if (mappedKey.HasValue)
                    {
                        ClientLog.Debug($"Game input detected: 0x{ControllerInputAddress:X8}={controllerInput:X2} -> {mappedKey.Value}.");
                        HandleKey(mappedKey.Value);
                    }
                }

            }
            else if (((gameMode == activeGameplayMode) ||
                      (isVsStory && gameMode == 0x07) ||
                      (isVsContest && gameMode == 0x04)) &&
                     levelState == 0x02 && controllerInput == InputR2)
            {
                TryUseLevelSkip(gdb, ref levelState, letter, levelNumber);
            }
            else if (isVsStory && gameMode == 0x07 && levelState == 0x02 && controllerInput == InputL2)
            {
                // In VS/Story, L2 during active gameplay returns to enemy selection.
                // Write the game-mode byte first, then clear the level-state byte as requested.
                gdb.WriteByte(GameModeAddress, 0x03);
                gdb.WriteByte(LevelStateAddress, 0x00);
                ClientLog.Debug($"VS/Story level: L2 returned to enemy selection; 0x{GameModeAddress:X8}=03, then 0x{LevelStateAddress:X8}=00.");
                gameMode = 0x03;
                levelState = 0x00;
                _vsStoryEnemySelectionActive = false;
                _vsStoryXConfirmSeen = false;
            }
            else if (isPuzzleStory && gameMode == EnteredLetter1State && levelState == 0x02 && controllerInput == InputL2)
            {
                // In Puzzle/Story, L2 exits the current card level back to card selection.
                // Do not use the global game-reset address here; preserve card-mode navigation.
                gdb.WriteByte(GameModeAddress, 0x03);
                // Once 0x00167A0C has switched back to card selection (03), reset
                // the level-state byte to 00 so the card-selection screen initializes cleanly.
                gdb.WriteByte(LevelStateAddress, 0x00);
                _puzzleStoryCardSelectionActive = false;
                _puzzleStoryCardMovePending = false;
                ClientLog.Debug($"Puzzle/Story level: L2 returned to card selection; 0x{GameModeAddress:X8}=03 and 0x{LevelStateAddress:X8}=00 to reinitialize the card menu.");
                gameMode = 0x03;
                levelState = 0x00;
            }
            else if (gameMode == InLevelMode && levelState == 0x02 && controllerInput == InputL2)
            {
                // L2 returns to letter selection only during active gameplay (0x00167A10 == 02).
                // This prevents leaving after a win (state 03) before the check is processed.
                // The branch is edge-triggered, so holding L2 does not repeat writes.
                // Reset the level index so the letter selection always starts at level 1.
                gdb.WriteByte(LevelNumberAddress, 0x00);
                gdb.WriteByte(GameModeAddress, MenuLetterSelectMode);
                gdb.WriteByte(LevelStateAddress, 0x00);
                ClientLog.Debug($"L2 pressed during a level: 0x{LevelNumberAddress:X8} reset to 00; 0x{GameModeAddress:X8} changed from {InLevelMode:X2} to {MenuLetterSelectMode:X2}; then 0x{LevelStateAddress:X8} set to 00; returning to letter selection.");
                gameMode = MenuLetterSelectMode;
                levelNumber = 0x00;
            }
        }

        // Also monitor changes made by the game itself to the character's facing.
        // If the player turns toward a locked letter during letter selection,
        // immediately correct the facing byte to the opposite usable exit.
        bool facingChanged = gameMode == MenuLetterSelectMode && _previousFacing.HasValue && facing != _previousFacing.Value;
        if (facingChanged)
        {
            byte oldFacing = _previousFacing!.Value;
            CorrectFacingForCurrentMenu(gdb);
            facing = gdb.ReadByte(FacingAddress); // Track the corrected value, not the rejected one.
            ClientLog.Debug($"LOOK DIRECTION CHANGED: {FacingName(oldFacing)} -> {FacingName(facing)}.");
            PrintLookTargetStatus(gdb, facing, "cambio de mirada");
        }
        _previousFacing = facing;

        // When entering the letter-selection screen, arm a one-shot watcher for
        // 0x00167A8F. Any non-zero value makes the reset pending; then keep watching
        // 0x00167A10 until it reaches 03 before applying the full Menu0 reset.
        // Manual selection of another letter disarms this watcher in HandleKey.
        if (gameMode == MenuLetterSelectMode && _previousGameMode != MenuLetterSelectMode)
        {
            _resetLetterOnSelectionChange = true;
            _letterResetPending = false;
            ClientLog.Debug($"World selection detected. Watching 0x00167A8F (current={letter:X2}) and 0x00167A10 (current={levelState:X2}).");
            PrintLookTargetStatus(gdb, facing, "entry to the world-selection menu");
        }

        // Visible diagnostic: while the game is in the letter-selection menu,
        // print the currently focused letter whenever 0x00167A8F changes.
        // This is intentionally edge-triggered to avoid flooding the console.
        if (gameMode == MenuLetterSelectMode && _previousLetterValue.HasValue && letter != _previousLetterValue.Value)
        {
            PrintLookTargetStatus(gdb, facing, "cambio de letra base");
        }

        if (gameMode == MenuLetterSelectMode && _resetLetterOnSelectionChange &&
            !_letterResetPending && letter != 0x00)
        {
            _letterResetPending = true;
            ClientLog.Debug($"0x00167A8F is {letter:X2} (not 00). Waiting for 0x00167A10 to become 03 (current={levelState:X2}) before resetting Menu0.");
        }

        if (gameMode == MenuLetterSelectMode && _resetLetterOnSelectionChange &&
            _letterResetPending && levelState == 0x03)
        {
            _menuIndex = 0;
            ApplyMenu(gdb, 0);
            letter = 0x00;
            _resetLetterOnSelectionChange = false;
            _letterResetPending = false;
            ClientLog.Debug("0x00167A8F was not 00 and 0x00167A10 reached 03: fully reset to Menu0, including camera coordinates.");
        }

        // Stop watching when leaving the letter-selection screen or when manual
        // navigation disarms the watcher.
        if (gameMode != MenuLetterSelectMode)
        {
            _resetLetterOnSelectionChange = false;
            _letterResetPending = false;
        }

        // Revalidate the current exit on every selection-menu poll. This also
        // catches a Letter-5 completion when the game returns to Menu0 without
        // changing the facing byte; A and B remain valid permanent destinations.
        if (gameMode == MenuLetterSelectMode)
        {
            byte facingBeforeCompletionCheck = gdb.ReadByte(FacingAddress);
            CorrectFacingForCurrentMenu(gdb);
            facing = gdb.ReadByte(FacingAddress);
            if (facing != facingBeforeCompletionCheck)
            {
                _previousFacing = facing;
                ClientLog.Debug($"LOOK DIRECTION CORRECTED: {FacingName(facingBeforeCompletionCheck)} -> {FacingName(facing)} because the selected Arcade letter is locked or already completed.");
            }
        }

        _previousGameMode = gameMode;
        _previousLetterValue = letter;

        // While in the selection screen, capture the actual outgoing destination
        // (the letter being looked at), not the base menu ID stored at 0x00167A8F.
        if (gameMode == MenuLetterSelectMode)
            PrepareLookedAtLetterMode(gdb, facing, "seguimiento de destino");

        // Explicit diagnostic for every transition of the level-state byte to 05,
        // regardless of the current game-mode byte.
        if (!isPuzzleStory && _previousLevelState.HasValue && _previousLevelState.Value != EnteredLetter1State && levelState == EnteredLetter1State)
        {
            string diagnosticLetter = _pendingLetter1Target ?? GetLetterName(letter);
            if (!IsKnownLetterName(diagnosticLetter))
            {
                // State 05 can also occur in other menus. If no valid letter destination
                // was captured, ignore this transition for Letter1 mode handling.
                ClientLog.Debug($"CHANGE IGNORED: 0x{LevelStateAddress:X8} changed from {_previousLevelState.Value:X2} to 05, but pending destination={diagnosticLetter} (read menu ID=0x{letter:X2}); no level mode will be applied.");
            }
            else
            {
                ClientLog.Debug($"CHANGE DETECTED: 0x{LevelStateAddress:X8} changed from {_previousLevelState.Value:X2} to 05; 0x{GameModeAddress:X8}={gameMode:X2}; pending destination={diagnosticLetter} (read menu ID=0x{letter:X2}).");
                if (gameMode == InLevelMode)
                    ClientLog.Debug($"  Target World1: {diagnosticLetter}1; first-level mode successfully applied in this session: {WasLetter1ModeApplied(diagnosticLetter)}.");
            }
        }

        // Letter1 is edge-triggered. Prefer the destination captured in the menu
        // before entering gameplay; 0x00167A8F is the base-menu ID and is ambiguous
        // at Menu0 (00), so it must not decide whether A1/B1 or a later letter is used.
        if (!isPuzzleStory &&
            _previousLevelState != EnteredLetter1State &&
            levelState == EnteredLetter1State &&
            gameMode == InLevelMode &&
            IsKnownLetterName(_pendingLetter1Target ?? GetLetterName(letter)))
        {
            string letter1Name = _pendingLetter1Target ?? GetLetterName(letter);
            _pendingLetter1Target = null;
            if (IsKnownLetterName(letter1Name))
            {
                _currentGameplayLetter = letter1Name;
                string level1Id = $"{letter1Name}1";
                ClientLog.Debug();
                ClientLog.Debug($"ENTRY TO WORLD1 DETECTED: 0x{LevelStateAddress:X8} -> 05; destination={letter1Name} (read menu ID=0x{letter:X2}).");
                ApplyModeForLevel(gdb, level1Id, "entrada al nivel Letra1 (destino capturado)");
                ClientLog.Debug();
            }
            else
            {
                ClientLog.Debug($"ENTRY TO WORLD1 detected, but destination '{letter1Name}' is not recognized; no mode was applied.");
            }
        }

        // VS/Story and VS/Contest win checks are handled by ProcessVersusWinResult using the result animation byte.
        // A 02 -> 03 transition only means the round ended; a check is accepted only
        // if 0x00167BE7 changes FF -> 00 within the following two seconds.

        // Puzzle/Story uses a different game-mode value while a card level is active:
        // 0x00167A0C=05 and 0x00167A10=02 during gameplay, then 0x00167A10 changes
        // from 02 to 03 when the card level is completed. Report the card-based level
        // only; do not run the letter-world completion/check/preparation logic below.
        if (isPuzzleStory && _previousLevelState == 0x02 && levelState == 0x03 && gameMode == EnteredLetter1State)
        {
            string cardName = GetPuzzleStoryCardName(letter);
            int displayedCardLevel = levelNumber + 1;
            string cardLevelId = $"{cardName} {displayedCardLevel}";
            ClientLog.Debug($"Nivel terminado: {cardLevelId}");

            // Apply the next Tarot level's difficulty immediately on completion.
            // This is the progression signal for levels 1-4; no later card-entry
            // transition is expected for levels 2-5. Level 5 has no next level.
            if (levelNumber < 0x04)
            {
                byte nextLevelNumber = (byte)(levelNumber + 1);
                ApplyTarotDifficultyForGameplay(gdb, letter, nextLevelNumber);
            }

            // Keep a durable-in-session set of individual card levels for All_Tarot.
            _puzzleStoryCompletedLevels.Add(cardLevelId);

            if (_gameModesOptions.PuzzleStory == 1)
            {
                if (_puzzleStoryChecksLogged.Add(cardLevelId) && _checksLoggedThisRun.TryAdd(cardLevelId, 0))
                {
                    ClientLog.Debug($"Puzzle/Story check detected: {cardLevelId} (card ID=0x{letter:X2}, level 0x{levelNumber:X2}).");
                    SendLocationCheck(cardLevelId);
                }
            }
            else
            {
                ClientLog.Debug($"Puzzle/Story level {cardLevelId} completed, but Puzzle/Story checks are disabled in this Archipelago seed; no location check was sent.");
            }

            // Offline-only progression simulation. In a live seed, card unlocks arrive as
            // Archipelago items so the generated item placement remains authoritative.
            if (LocalDevelopmentMode && letter == FoolCardId && levelNumber == 0x00 && _puzzleStoryUnlockedCards.Add(0x00))
                ClientLog.Debug("LOCAL TEST: Puzzle/Story card unlocked: Star after Fool 1.");
            if (LocalDevelopmentMode && letter == FoolCardId && levelNumber == 0x01 && _puzzleStoryUnlockedCards.Add(0x04))
                ClientLog.Debug("LOCAL TEST: Puzzle/Story card unlocked: Devil after Fool 2.");

            if (levelNumber == 0x04)
            {
                if (_puzzleStoryCompletedCards.Add(letter))
                {
                    ClientLog.Debug($"Tarot card completed: {cardName} (level 5). It will be removed from card selection.");
                    string normalizedGoal = _serverGoalType.Trim().Replace('-', '_').Replace(' ', '_');
                    if (normalizedGoal.Equals("all_tarot", StringComparison.OrdinalIgnoreCase))
                        WriteClientLine($"Tarot card completed ({_puzzleStoryCompletedCards.Count}/22).");
                }

                // In All_Tarot, every card is eventually reachable: completing a card's
                // fifth level unlocks the next card in the game's Tarot progression.
                if (LocalDevelopmentMode && _localGoal == "All_Tarot")
                {
                    int completedIndex = Array.FindIndex(PuzzleStoryCards, card => card.Id == letter);
                    if (completedIndex >= 0 && completedIndex + 1 < PuzzleStoryCards.Length)
                    {
                        byte nextCardId = PuzzleStoryCards[completedIndex + 1].Id;
                        if (_puzzleStoryUnlockedCards.Add(nextCardId))
                            ClientLog.Debug($"All_Tarot progression: unlocked {PuzzleStoryCards[completedIndex + 1].Name} after completing {cardName} 5.");
                    }
                }

                // Offline-only test shortcut. In a live seed, World must be unlocked
                // by receiving the server's "Unlock Tarot World" item.
                if (LocalDevelopmentMode && letter == FoolCardId && _puzzleStoryUnlockedCards.Add(0x17))
                {
                    ClientLog.Debug("Puzzle/Story card unlocked: World (unlocked by completing Fool 5). World is the current local goal.");
                }

                if (LocalDevelopmentMode && _localGoal == "Only_World" && letter == 0x17)
                {
                    _goalCompletionHandled = true;
                    WriteClientLine("VICTORY: Only_World achieved — you completed World 5!");
                }
                else if (LocalDevelopmentMode && _localGoal == "All_Tarot" &&
                    PuzzleStoryCards.All(card => _puzzleStoryCompletedCards.Contains(card.Id)))
                {
                    _goalCompletionHandled = true;
                    WriteClientLine("VICTORY: All_Tarot achieved — all 22 Tarot cards have been completed!");
                }
            }
        }

        if (isPuzzleArcade && _previousLevelState == 0x02 && levelState == 0x03 &&
            gameMode == InLevelMode && !_puzzleStoryCardSelectionActive)
        {
            string memoryLetterName = GetLetterName(letter);
            // During the 02 -> 03 completion transition, the game may reset
            // LetterAddress to 00. In gameplay, 00 normally means A, but during
            // this transition it can be a transient reset even when the completed
            // level belongs to another world (notably the ? world). If a gameplay
            // letter was captured on entry, trust it over that transient zero.
            string? letterName;
            if (letter == 0x00 && IsKnownLetterName(_currentGameplayLetter))
            {
                letterName = _currentGameplayLetter;
            }
            else if (IsKnownLetterName(memoryLetterName))
            {
                _currentGameplayLetter = memoryLetterName;
                letterName = memoryLetterName;
            }
            else
            {
                letterName = _currentGameplayLetter;
            }

            int displayedLevel = levelNumber + 1;
            if (!IsKnownLetterName(letterName))
            {
                ClientLog.Debug($"CHECK DETECTED BUT NOT SENT: could not determine the world. Memory=0x{letter:X2}, level={displayedLevel}.");
                _previousLevelState = levelState;
                return;
            }

            string checkId = $"{letterName}{displayedLevel}";
            if (displayedLevel == 5 && _puzzleArcadeCompletedLetters.Add(letterName!))
                ClientLog.Debug($"Puzzle/Arcade letter completed: {letterName}5. It will be skipped during letter selection (A and B remain selectable).");

            if (LocalDevelopmentMode && displayedLevel == 5 && _localGoalLetters.Contains(letterName!) && _localCompletedGoalLetters.Add(letterName!))
            {
                WriteClientLine($"LOCAL GOAL PROGRESS: completed {letterName}5 ({_localCompletedGoalLetters.Count}/{_localGoalLetters.Count}).");
                if (_localCompletedGoalLetters.SetEquals(_localGoalLetters))
                {
                    _goalCompletionHandled = true;
                    WriteClientLine($"VICTORY: local goal {_localGoal} achieved!");
                }
            }
            ClientLog.Debug($"Level completion detected: {checkId} (state={levelState:X2}, world={letter:X2}, level={levelNumber:X2}).");
            if (_checksLoggedThisRun.TryAdd(checkId, 0))
            {
                ClientLog.Debug();
                ClientLog.Debug($"CHECK DETECTED: {checkId}");
                ClientLog.Debug($"  0x00167A0C = {gameMode:X2} (in level)");
                ClientLog.Debug($"  0x00167A10: 02 -> 03");
                ClientLog.Debug($"  World 0x00167A8F = {letter:X2}; level 0x00167A8D = {levelNumber:X2}");

                // As soon as a level check is detected, preconfigure the next
                // level in the same letter (for example A1 -> A2). Letter1 is
                // also configured on letter selection in HandleKey.
                if (displayedLevel < 5)
                {
                    string nextLevelId = $"{letterName}{displayedLevel + 1}";
                    ApplyModeForLevel(gdb, nextLevelId, $"completion of {checkId}");
                }

                if (LocalDevelopmentMode)
                    ProcessCompletedLevelLocally(gdb, checkId);
                else if (_gameModesOptions.PuzzleArcade == 1)
                    SendLocationCheck(checkId);
                else
                    ClientLog.Debug($"Arcade level {checkId} completed, but Puzzle/Arcade checks are disabled in this Archipelago seed; no location check was sent.");
                ClientLog.Debug();
            }
        }

        _previousLevelState = levelState;
    }

    private static void ApplySecretVsStoryFix(DuckstationMemory gdb, bool isVsStory, byte gameMode, byte levelState)
    {
        // Apply the Secret fix on either observed transition into state 01:
        // 00 -> 01 (usual path) or 08 -> 01 (alternate VS/Story path).
        // No enemy-selection-menu condition is required; the enemy ID is sufficient.
        byte enemyId = gdb.ReadByte(VsStoryEnemyIdAddress);
        bool secretEntryTransition = isVsStory &&
            enemyId == SecretEnemyId &&
            levelState == 0x01 &&
            (_previousLevelState == 0x00 || _previousLevelState == 0x08);

        if (secretEntryTransition)
        {
            byte attackType1 = gdb.ReadByte(SecretAttackTypeAddress1);
            byte attackType2 = gdb.ReadByte(SecretAttackTypeAddress2);
            if (attackType1 != 0x02)
                gdb.WriteByte(SecretAttackTypeAddress1, 0x02);
            if (attackType2 != 0x02)
                gdb.WriteByte(SecretAttackTypeAddress2, 0x02);
            ClientLog.Debug($"SECRET VS/STORY FIX: 0x{LevelStateAddress:X8} changed {_previousLevelState:X2} -> 01 while Secret (enemy ID 10) is active; 0x{SecretAttackTypeAddress1:X8}={attackType1:X2}->02, 0x{SecretAttackTypeAddress2:X8}={attackType2:X2}->02.");

            byte secretFlag = gdb.ReadByte(SecretVsStoryFlagAddress);
            if (secretFlag == 0x00)
            {
                gdb.WriteByte(SecretVsStoryFlagAddress, 0x01);
                _secretFlagChangedByClient = true;
                ClientLog.Debug($"SECRET VS/STORY FLAG: 0x{SecretVsStoryFlagAddress:X8} changed 00 -> 01; will restore to 00 on level completion (02 -> 03).");
            }
            else if (secretFlag == 0x01)
            {
                ClientLog.Debug($"SECRET VS/STORY FLAG: 0x{SecretVsStoryFlagAddress:X8} already 01; leaving it unchanged and will not restore it.");
            }
            else
            {
                ClientLog.Debug($"SECRET VS/STORY FLAG: 0x{SecretVsStoryFlagAddress:X8}={secretFlag:X2}; not 00 or 01, leaving unchanged.");
            }
        }

        // Restore only if this client changed the flag from 00 to 01. Do not overwrite
        // a value that another game routine has changed since then.
        if (isVsStory && _secretFlagChangedByClient &&
            _previousLevelState == 0x02 && levelState == 0x03)
        {
            byte currentFlag = gdb.ReadByte(SecretVsStoryFlagAddress);
            if (currentFlag == 0x01)
            {
                gdb.WriteByte(SecretVsStoryFlagAddress, 0x00);
                ClientLog.Debug($"SECRET VS/STORY FLAG RESTORED: 0x{SecretVsStoryFlagAddress:X8} changed 01 -> 00 after level completion (02 -> 03).");
            }
            else
            {
                ClientLog.Debug($"SECRET VS/STORY FLAG RESTORE SKIPPED: 0x{SecretVsStoryFlagAddress:X8} is now {currentFlag:X2}; preserving the game's current value.");
            }
            _secretFlagChangedByClient = false;
        }
        // Do not discard _secretFlagChangedByClient on a brief mode transition:
        // the 02 -> 03 completion edge may arrive during that transition.
    }

    private static void ApplyContestHudRoundLabel(DuckstationMemory gdb, bool isVsContest)
    {
        // The game reserves six bytes for this label. Write the five visible ASCII
        // characters and a null terminator so no stale character remains.
        byte[] desired = System.Text.Encoding.ASCII.GetBytes(isVsContest ? " WINS\0" : "ROUND\0");
        bool changed = false;
        for (int i = 0; i < desired.Length; i++)
        {
            uint address = ContestHudRoundLabelAddress + (uint)i;
            byte current = gdb.ReadByte(address);
            if (current != desired[i])
            {
                gdb.WriteByte(address, desired[i]);
                changed = true;
            }
        }
        if (changed)
            ClientLog.Debug($"HUD ROUND LABEL: VS/Contest={isVsContest}; text=\"{(isVsContest ? " WINS" : "ROUND")}\" at 0x{ContestHudRoundLabelAddress:X8}-0x{ContestHudRoundLabelAddress + 5:X8}.");
    }

    private static void ProcessContestEnemyRandomizer(DuckstationMemory gdb, bool isVsContest, byte gameMode, byte levelState)
    {
        // Reset the complete 16-character bag only when entering a new VS/Contest session.
        if (!isVsContest)
        {
            if (_contestRandomizerModeActive)
            {
                ResetContestEnemyPool("left VS/Contest");
                _contestRandomizerModeActive = false;
            }
            return;
        }

        if (!_contestRandomizerModeActive)
        {
            ResetContestEnemyPool("entered VS/Contest");
            _contestRandomizerModeActive = true;
        }

        // 0x00167A0C == 02 is the VS/Contest opponent-selection screen.
        // Only randomize on this screen, never during an active match.
        bool opponentSelectionScreen = gameMode == 0x02;
        if (!opponentSelectionScreen)
            return;

        switch (_contestEnemyRandomizerState)
        {
            case ContestEnemyRandomizerState.NeedInitialEnemy:
                RandomizeContestEnemy(gdb, "first opponent selection of this VS/Contest session");
                break;

            case ContestEnemyRandomizerState.WaitingForNextEnemyAfterVictory:
                RandomizeContestEnemy(gdb, "opponent selection after confirmed victory");
                break;

            case ContestEnemyRandomizerState.Fighting:
                // The current opponent remains unchanged until a confirmed victory.
                break;

            case ContestEnemyRandomizerState.HoldAfterLoss:
                // A loss must not reroll the opponent. The current selection is retained
                // until the player leaves VS/Contest and starts a new session.
                break;
        }
    }

    private static void ResetContestEnemyPool(string reason)
    {
        _contestEnemyPool.Clear();
        for (byte enemyId = 0x00; enemyId <= 0x0F; enemyId++)
            _contestEnemyPool.Add(enemyId);

        _contestEnemyRandomizerState = ContestEnemyRandomizerState.NeedInitialEnemy;
        _contestEnemyIdBeingFought = null;
        _contestFirstOpponentMarkersPending = true;
        ClientLog.Debug($"VS/CONTEST ENEMY POOL RESET ({reason}): 16 characters available [00-0F].");
    }

    private static void RandomizeContestEnemy(DuckstationMemory gdb, string reason)
    {
        if (_contestEnemyPool.Count == 0)
        {
            for (byte enemyId = 0x00; enemyId <= 0x0F; enemyId++)
                _contestEnemyPool.Add(enemyId);
            ClientLog.Debug("VS/CONTEST ENEMY POOL CYCLE COMPLETE: all 16 characters were selected; starting a fresh 16-character pool.");
        }

        byte originalEnemyId = gdb.ReadByte(ContestEnemyCharacterAddress);
        int selectedIndex = Random.Shared.Next(_contestEnemyPool.Count);
        byte randomizedEnemyId = _contestEnemyPool[selectedIndex];

        gdb.WriteByte(ContestEnemyCharacterAddress, randomizedEnemyId);
        byte readBackEnemyId = gdb.ReadByte(ContestEnemyCharacterAddress);
        bool writeConfirmed = readBackEnemyId == randomizedEnemyId;
        if (!writeConfirmed)
        {
            ClientLog.Debug($"VS/CONTEST ENEMY RANDOMIZATION FAILED ({reason}): original={originalEnemyId:X2}; attempted={randomizedEnemyId:X2}; read-back={readBackEnemyId:X2}; pool unchanged ({_contestEnemyPool.Count} remaining).");
            return;
        }

        _contestEnemyPool.RemoveAt(selectedIndex);
        if (_contestFirstOpponentMarkersPending)
        {
            gdb.WriteByte(ContestHudEnemyMarkerAddress, 0xFF);
            gdb.WriteByte(ContestHudEnemyMarkerSecondAddress, 0xFF);
            _contestFirstOpponentMarkersPending = false;
            ClientLog.Debug($"VS/CONTEST FIRST OPPONENT MARKERS RESET: 0x{ContestHudEnemyMarkerAddress:X8}=FF; 0x{ContestHudEnemyMarkerSecondAddress:X8}=FF after first enemy randomization ({reason}).");
        }
        _contestEnemyIdBeingFought = readBackEnemyId;
        _contestEnemyRandomizerState = ContestEnemyRandomizerState.Fighting;
        string remainingIds = string.Join(",", _contestEnemyPool.Select(id => id.ToString("X2")));
        ClientLog.Debug($"VS/CONTEST ENEMY RANDOMIZED ({reason}): address=0x{ContestEnemyCharacterAddress:X8}; original={originalEnemyId:X2}; chosen={randomizedEnemyId:X2}; remaining={_contestEnemyPool.Count}; pool=[{remainingIds}]; write=CONFIRMED.");
    }

    private static void BeginWaitingForNextContestEnemy()
    {
        // The next randomization is deferred until the game returns to opponent selection (02).
        _contestEnemyRandomizerState = ContestEnemyRandomizerState.WaitingForNextEnemyAfterVictory;
    }

    private static void ApplyPackyFix(DuckstationMemory gdb, bool isVsStory, bool isVsContest, byte gameMode, byte levelState)
    {
        if (_otherOptions.FixPacky != 1)
            return;

        // Only correct attack selection during an active VS fight. Contest has an
        // explicit enemy-character byte at 0x00167A46; VS/Story uses its enemy ID
        // at 0x00167A8A. The player character is 0x00167A45 in both modes.
        bool activeVsStory = isVsStory && gameMode == 0x07 && levelState == 0x02;
        bool activeVsContest = isVsContest && gameMode == 0x04 && levelState == 0x02;
        if (!activeVsStory && !activeVsContest)
            return;

        byte playerCharacter = gdb.ReadByte(ActiveCharacterAddress);
        uint enemyCharacterAddress = activeVsContest ? ContestEnemyCharacterAddress : VsStoryEnemyIdAddress;
        byte enemyCharacter = gdb.ReadByte(enemyCharacterAddress);

        if (playerCharacter == PackyCharacterId)
        {
            byte damage = gdb.ReadByte(PackyPlayerDamageAddress);
            byte desiredAttackType = damage >= 0x01 && damage <= 0x06 ? PackyLowAttackType : PackyHighAttackType;
            byte currentAttackType = gdb.ReadByte(PackyPlayerAttackTypeAddress);
            if (currentAttackType != desiredAttackType)
            {
                gdb.WriteByte(PackyPlayerAttackTypeAddress, desiredAttackType);
                ClientLog.Debug($"PACKY FIX (player): damage/value 0x{PackyPlayerDamageAddress:X8}={damage:X2}; 0x{PackyPlayerAttackTypeAddress:X8} {currentAttackType:X2} -> {desiredAttackType:X2}.");
            }
        }

        if (enemyCharacter == PackyCharacterId)
        {
            byte damage = gdb.ReadByte(PackyEnemyDamageAddress);
            byte desiredAttackType = damage >= 0x01 && damage <= 0x06 ? PackyLowAttackType : PackyHighAttackType;
            byte currentAttackType = gdb.ReadByte(PackyEnemyAttackTypeAddress);
            if (currentAttackType != desiredAttackType)
            {
                gdb.WriteByte(PackyEnemyAttackTypeAddress, desiredAttackType);
                ClientLog.Debug($"PACKY FIX (enemy): enemy ID address 0x{enemyCharacterAddress:X8}=0x{enemyCharacter:X2}; damage/value 0x{PackyEnemyDamageAddress:X8}={damage:X2}; 0x{PackyEnemyAttackTypeAddress:X8} {currentAttackType:X2} -> {desiredAttackType:X2}.");
            }
        }
    }

    private static void ApplyContestEnemyDifficulty(DuckstationMemory gdb, bool isVsContest, byte gameMode, byte levelState)
    {
        // Only apply the configured increase while VS/Contest is actively playing a level.
        // Capture the game's current value once per active fight so the option is additive,
        // not cumulative across polling cycles. Restore the captured value when leaving play.
        bool activeContestGameplay = isVsContest && gameMode == 0x04 && levelState == 0x02;
        if (activeContestGameplay)
        {
            if (_contestDifficultyTracking)
                return;

            _contestDifficultyBaseValue = gdb.ReadByte(ContestEnemyDifficultyAddress);
            int adjusted = Math.Min(byte.MaxValue,
                _contestDifficultyBaseValue + _contestOptions.IncreaseDifficultyContest);
            _contestDifficultyLastAppliedValue = (byte)adjusted;
            _contestDifficultyTracking = true;

            if (_contestOptions.IncreaseDifficultyContest > 0)
            {
                gdb.WriteByte(ContestEnemyDifficultyAddress, _contestDifficultyLastAppliedValue);
                ClientLog.Debug($"CONTEST ENEMY DIFFICULTY: 0x{ContestEnemyDifficultyAddress:X8} base=0x{_contestDifficultyBaseValue:X2} + increase={_contestOptions.IncreaseDifficultyContest} -> 0x{_contestDifficultyLastAppliedValue:X2}.");
            }
            else
            {
                ClientLog.Debug($"CONTEST ENEMY DIFFICULTY: vanilla unchanged at 0x{_contestDifficultyBaseValue:X2} (increase_difficulty_contest=0).");
            }
            return;
        }

        if (_contestDifficultyTracking)
        {
            byte current = gdb.ReadByte(ContestEnemyDifficultyAddress);
            if (_contestOptions.IncreaseDifficultyContest > 0 && current == _contestDifficultyLastAppliedValue)
            {
                gdb.WriteByte(ContestEnemyDifficultyAddress, _contestDifficultyBaseValue);
                ClientLog.Debug($"CONTEST ENEMY DIFFICULTY RESTORED: 0x{ContestEnemyDifficultyAddress:X8} -> 0x{_contestDifficultyBaseValue:X2} after leaving active gameplay.");
            }
            _contestDifficultyTracking = false;
        }
    }

    private static void ProcessContestWinChecks(DuckstationMemory gdb, bool isVsContest, bool confirmedWin)
    {
        if (!isVsContest || (_gameModesOptions.VsContestSimple != 1 && _gameModesOptions.VsContestComplete != 1))
        {
            _previousContestModeActive = false;
            _previousContestWins = null;
            _previousContestCharacter = null;
            _contestWinConfirmationPending = false;
            return;
        }

        byte currentWins = gdb.ReadByte(ContestWinsAddress);
        byte currentCharacter = gdb.ReadByte(ActiveCharacterAddress);
        if (_previousContestModeActive != true)
        {
            _previousContestModeActive = true;
            // Start observing from zero so a newly attached client can still
            // recognize milestones, but never emit them without a confirmed win.
            _previousContestWins = 0;
            _previousContestCharacter = currentCharacter;
            ClientLog.Debug($"CONTEST WIN CHECK TRACKING STARTED: wins={currentWins:X2}, character=0x{currentCharacter:X2}; waiting for confirmed victory signal.");
        }

        int previousWins = _previousContestWins ?? 0;
        byte previousCharacter = _previousContestCharacter ?? currentCharacter;
        if (currentCharacter != previousCharacter)
        {
            ClientLog.Debug($"CONTEST CHARACTER CHANGED: 0x{ActiveCharacterAddress:X8} {previousCharacter:X2} -> {currentCharacter:X2}.");
            previousWins = 0;
            _previousContestWins = 0;
            _contestWinConfirmationPending = false;
        }

        if (confirmedWin)
            _contestWinConfirmationPending = true;

        if (currentWins < previousWins)
        {
            ClientLog.Debug($"CONTEST STREAK RESET: 0x{ContestWinsAddress:X8} changed from {previousWins:X2} to {currentWins:X2}. No check is emitted for a loss; previously checked locations remain completed.");
            _previousContestWins = currentWins;
            _contestWinConfirmationPending = false;
        }
        else if (_contestWinConfirmationPending && currentWins > previousWins)
        {
            int lastMilestone = Math.Min(currentWins, _contestOptions.CustomContestWins);
            bool multiCharacterMode = _gameModesOptions.VsContestComplete == 1;
            bool characterCanBeUsed = !multiCharacterMode ||
                (_serverSelectedContestCharacterIds.Contains(currentCharacter) && _receivedVsContestCharacterUnlockIds.Contains(currentCharacter));
            string characterName = VsSelectableCharacters.FirstOrDefault(character => character.Id == currentCharacter)?.Name
                ?? $"Unknown 0x{currentCharacter:X2}";

            if (multiCharacterMode && !characterCanBeUsed)
            {
                ClientLog.Debug($"CONTEST CHECKS BLOCKED: {characterName} is not unlocked or not in this seed's selected character pool.");
            }
            else
            {
                for (int milestone = previousWins + 1; milestone <= lastMilestone; milestone++)
                {
                    string checkKey = multiCharacterMode ? $"{currentCharacter:X2}:{milestone}" : $"single:{milestone}";
                    if (_contestWinChecksSent.Add(checkKey))
                    {
                        string checkName = multiCharacterMode
                            ? $"Contest Win {milestone} as {characterName}"
                            : $"Contest Win {milestone}";
                        ClientLog.Debug($"CONTEST CHECK CONFIRMED: {checkName} (0x{ContestWinsAddress:X8}={currentWins:X2}; target={_contestOptions.CustomContestWins}; mode={(multiCharacterMode ? "Complete" : "Simple")}).");
                        if (_checksLoggedThisRun.TryAdd(checkName, 0))
                            SendLocationCheck(checkName);
                    }
                }
            }
            _previousContestWins = currentWins;
            _contestWinConfirmationPending = false;
        }

        // Do not advance the win baseline just because the counter rose. A check
        // is eligible only after ProcessVersusWinResult confirms the victory animation.
        _previousContestCharacter = currentCharacter;
    }

    private static void ProcessVersusWinResult(
        DuckstationMemory gdb,
        bool isVsStory,
        bool isVsContest,
        byte gameMode,
        byte levelState)
    {
        bool validVersusGameplay =
            (isVsStory && gameMode == 0x07) ||
            (isVsContest && gameMode == 0x04);
        bool completionTransition = validVersusGameplay &&
            _previousLevelState == 0x02 && levelState == 0x03;

        if (!completionTransition)
            return;

        // The 02 -> 03 transition identifies the end of the match. The result
        // animation distinguishes a victory from a defeat immediately.
        byte resultAnimation = gdb.ReadByte(VersusResultAnimationAddress);
        ClientLog.Debug($"VERSUS RESULT DETECTED: mode={(isVsStory ? "VS/Story" : "VS/Contest")}; 0x{LevelStateAddress:X8} changed 02->03; 0x{VersusResultAnimationAddress:X8}=0x{resultAnimation:X2}.");

        if (resultAnimation == 0x08)
        {
            ClientLog.Debug("VERSUS DEFEAT CONFIRMED: result animation 08. No victory check will be sent.");
            if (isVsContest)
            {
                _contestEnemyRandomizerState = ContestEnemyRandomizerState.HoldAfterLoss;
                _contestFirstOpponentMarkersPending = true;
            }
            return;
        }

        if (resultAnimation != 0x07)
        {
            ClientLog.Debug($"VERSUS RESULT UNKNOWN: animation 0x{resultAnimation:X2}; no victory check will be sent.");
            if (isVsContest)
            {
                _contestEnemyRandomizerState = ContestEnemyRandomizerState.HoldAfterLoss;
            }
            return;
        }

        byte? completedEnemyId = isVsStory ? gdb.ReadByte(VsStoryEnemyIdAddress) : null;
        ClientLog.Debug("VERSUS VICTORY CONFIRMED: result animation 07.");
        if (isVsStory && completedEnemyId.HasValue)
        {
            string completedEnemyName = VsStoryEnemyNamesById.TryGetValue(completedEnemyId.Value, out string? knownEnemyName)
                ? knownEnemyName
                : $"Unknown enemy (ID=0x{completedEnemyId.Value:X2})";
            byte characterId = gdb.ReadByte(ActiveCharacterAddress);
            string characterName = GetVsStoryPlayerCharacterName(characterId);
            bool storyModeEnabled = _gameModesOptions.VsStorySimple == 1 || _gameModesOptions.VsStoryComplete == 1;
            bool storyCompleteMode = _gameModesOptions.VsStoryComplete == 1;
            string enemyKey = completedEnemyName.Replace(" ", "_", StringComparison.Ordinal);
            string characterKey = characterName.Replace(" ", "_", StringComparison.Ordinal);
            string enemyCheckName = storyCompleteMode
                ? $"Defeat {completedEnemyName} as {characterName}"
                : $"vs {completedEnemyName} win";
            bool selectedCharacterCanCheck = !storyCompleteMode ||
                (_serverSelectedVsStoryCharacterIds.Contains(characterId) && _receivedVsStoryCharacterUnlockIds.Contains(characterId));
            // Secret is a client-side optional opponent and is not part of this APWorld version yet.
            bool secretCheckEnabled = completedEnemyId.Value != SecretEnemyId || _vsStoryOptions.ExtraEnemy == 1;
            if (storyModeEnabled && !storyCompleteMode)
                UnlockNextVsStoryEnemyAfterVictory(completedEnemyId.Value);

            // Return to the defeated opponent after victory. Dreg is fought automatically
            // after Madam Luna and is not selectable, so return to Madam Luna instead.
            byte returnEnemyId = completedEnemyId.Value == 0x0B
                ? (byte)0x0A
                : completedEnemyId.Value;
            _vsStoryReturnEnemyId = returnEnemyId;
            ClientLog.Debug($"VS/Story return selection: defeated {completedEnemyName} (ID=0x{completedEnemyId.Value:X2}); will return to {(returnEnemyId == 0x0A && completedEnemyId.Value == 0x0B ? "Madam Luna" : completedEnemyName)} (ID=0x{returnEnemyId:X2}).");

            bool specialEnemyVictory = completedEnemyId.Value == 0x0B || completedEnemyId.Value == SecretEnemyId;
            bool specialEnemyCheckSent = false;
            bool specialEnemyCheckWasAlreadyQueued = false;

            if (storyModeEnabled && secretCheckEnabled && selectedCharacterCanCheck)
            {
                if (_checksLoggedThisRun.TryAdd(enemyCheckName, 0))
                {
                    ClientLog.Debug($"VS/Story victory confirmed: {enemyCheckName} (enemy ID=0x{completedEnemyId.Value:X2}).");
                    specialEnemyCheckSent = SendLocationCheck(enemyCheckName);
                    if (!specialEnemyCheckSent)
                        _checksLoggedThisRun.TryRemove(enemyCheckName, out _);
                }
                else
                {
                    // This check was already queued earlier in this client session.
                    specialEnemyCheckWasAlreadyQueued = true;
                }
            }
            else if (!storyModeEnabled)
            {
                ClientLog.Debug("VS/Story victory observed, but VS/Story checks are disabled in this Archipelago seed; no location check was sent.");
            }
            else if (!selectedCharacterCanCheck)
            {
                ClientLog.Debug($"VS/Story check ignored: {characterName} is not an unlocked character in the seed's selected pool.");
            }

            // Special exit transition: only after the Dreg/Secret check is already queued
            // (or had previously been queued), write 09 to the VS/Story enemy ID address.
            if (specialEnemyVictory && (specialEnemyCheckSent || specialEnemyCheckWasAlreadyQueued))
            {
                gdb.WriteByte(VsStorySpecialEnemyReturnAddress, 0x09);
                ClientLog.Debug($"VS/Story special enemy victory: check '{enemyCheckName}' was queued; wrote enemy ID 09 to 0x{VsStorySpecialEnemyReturnAddress:X8}.");
            }
            else if (specialEnemyVictory)
            {
                ClientLog.Debug($"VS/Story special enemy victory: did not write 09 to 0x{VsStorySpecialEnemyReturnAddress:X8} because check '{enemyCheckName}' was not sent.");
            }
        }
        if (isVsContest)
        {
            ProcessContestWinChecks(gdb, isVsContest: true, confirmedWin: true);
            BeginWaitingForNextContestEnemy();
        }
    }

    private static string GetDeathLinkOpponentName(byte enemyId)
    {
        return enemyId switch
        {
            0x00 => "Bob",
            0x01 => "Alkanet",
            0x02 => "Marino",
            0x03 => "Kurol",
            0x04 => "Tamtam",
            0x05 => "Cleon",
            0x06 => "G",
            0x07 => "Braum",
            0x08 => "Develon",
            0x09 => "Gigant",
            0x0A => "Madam Luna",
            0x0B => "Dreg",
            0x0C => "Woolen",
            0x0D => "Packy",
            0x0E => "Monsta",
            0x0F => "Maita",
            0x10 => "Secret",
            _ => $"unknown opponent (ID {enemyId:X2})"
        };
    }

    private static bool ProcessPendingDeathLink(DuckstationMemory gdb, byte gameMode, byte levelState, bool isPuzzleStory, bool isVsStory, bool isVsContest)
    {
        // Puzzle/Arcade=03, Puzzle/Story=05, VS/Story=07, and VS/Contest=04.
        // In every case, 0x00167A10 must be 02 before applying a received DeathLink.
        byte expectedGameMode = isVsStory ? (byte)0x07 : isVsContest ? (byte)0x04 : isPuzzleStory ? EnteredLetter1State : InLevelMode;
        if (gameMode != expectedGameMode || levelState != 0x02)
            return false;

        if (!_deathLinkEnabled || _deathLinkService is null)
            return false;

        if (!_incomingDeathLinks.TryDequeue(out DeathLink? receivedDeath) || receivedDeath is null)
            return false;

        // Collapse additional signals already queued while waiting for active gameplay.
        while (_incomingDeathLinks.TryDequeue(out _)) { }

        string cause = string.IsNullOrWhiteSpace(receivedDeath.Cause)
            ? $"DeathLink received from {receivedDeath.Source}."
            : receivedDeath.Cause;
        // Do not echo the resulting loss back to Archipelago as a new DeathLink.
        _suppressDeathLinkUntilGameplayResumes = true;
        _deathLinkSuppressionSawGameplayExit = false;
        gdb.WriteByte(LevelStateAddress, 0x00);
        WriteColoredLine($"DeathLink received: {cause} Resetting level.", DeadLinkColor);
        return true;
    }

    private static void WriteColoredLine(string text, string color)
    {
        Console.Write(color);
        Console.WriteLine(text);
        Console.Write(AnsiReset);
    }

    private static void ApplyModeForLevel(DuckstationMemory gdb, string levelId, string reason)
    {
        LevelMode? selectedMode = _modePlan?.GetModeForLevel(levelId);
        if (!selectedMode.HasValue)
        {
            ClientLog.Debug($"  MODE: no assignment exists in the plan for {levelId}; memory was not modified.");
            return;
        }

        byte anotherWorld = selectedMode.Value is LevelMode.NormalAW or LevelMode.ExpertAW
            ? AnotherWorldValue : OriginalValue;
        byte difficulty = selectedMode.Value is LevelMode.ExpertO or LevelMode.ExpertAW
            ? ExpertValue : NormalValue;

        gdb.WriteByte(AnotherWorldAddress, anotherWorld);
        gdb.WriteByte(DifficultyAddress, difficulty);

        // Read back both addresses so the console confirms the values DuckStation
        // actually exposes after the writes.
        byte actualWorld = gdb.ReadByte(AnotherWorldAddress);
        byte actualDifficulty = gdb.ReadByte(DifficultyAddress);
        ClientLog.Debug($"  MODE PREPARED for {levelId} ({reason}): {selectedMode.Value}; " +
            $"0x{AnotherWorldAddress:X8}={actualWorld:X2}, 0x{DifficultyAddress:X8}={actualDifficulty:X2}.");

        bool valuesMatch = actualWorld == anotherWorld && actualDifficulty == difficulty;
        if (levelId.Length >= 2 && levelId.EndsWith("1", StringComparison.Ordinal) && IsKnownLetterName(levelId[..^1]))
        {
            string letterName = levelId[..^1];
            if (valuesMatch)
                _letter1ModesApplied.Add(letterName);
            else
                _letter1ModesApplied.Remove(letterName);
            ClientLog.Debug($"  WORLD1 STATE {letterName}: first-level mode {(valuesMatch ? "CHANGED AND VERIFIED" : "NOT VERIFIED")}; currently viewed world={(GetLookedAtLetterName(gdb.ReadByte(FacingAddress)))}.");
        }

        if (!valuesMatch)
            ClientLog.Debug("  WARNING: DuckStation did not retain one of the written values; the game may have overwritten it.");
    }

    private static bool WasLetter1ModeApplied(string letterName) => _letter1ModesApplied.Contains(letterName);

    private static bool IsKnownLetterName(string? name) =>
        name is not null &&
        (name == "?" || (name.Length == 1 && (name[0] is >= 'A' and <= 'Z')));

    private static string FacingName(byte facing) => facing switch
    {
        FacingLeft => "IZQUIERDA (00)",
        FacingRight => "DERECHA (01)",
        _ => $"DESCONOCIDA ({facing:X2})"
    };

    private static string GetLookedAtLetterName(byte facing)
    {
        int direction = facing == FacingLeft ? -1 : facing == FacingRight ? 1 : 0;
        if (direction == 0) return $"unknown direction 0x{facing:X2}";
        string? target = GetDestinationName(GetNeighbor(_menuIndex, direction));
        return target ?? "ninguna letra identificada";
    }

    private static void PrepareLookedAtLetterMode(DuckstationMemory gdb, byte facing, string reason)
    {
        string targetLetter = GetLookedAtLetterName(facing);
        if (!IsKnownLetterName(targetLetter))
        {
            _pendingLetter1Target = null;
            return;
        }

        if (!string.Equals(_pendingLetter1Target, targetLetter, StringComparison.OrdinalIgnoreCase))
        {
            _pendingLetter1Target = targetLetter;
            // Capture the destination as the active gameplay letter before the game
            // resets 0x00167A8F during the transition/completion sequence.
            _currentGameplayLetter = targetLetter;
            ClientLog.Debug($"WORLD1 DESTINATION PREPARED ({reason}): base={Menus[_menuIndex].Name}, destination={targetLetter}.");
            ClientLog.Debug($"GAME WORLD SAVED FROM DESTINATION: {targetLetter}.");
            ApplyModeForLevel(gdb, $"{targetLetter}1", $"destino mirado desde {Menus[_menuIndex].Name}");
        }
    }

    private static void PrintLookTargetStatus(DuckstationMemory gdb, byte facing, string reason)
    {
        string targetLetter = GetLookedAtLetterName(facing);
        string applied = IsKnownLetterName(targetLetter) ? (WasLetter1ModeApplied(targetLetter) ? "YES" : "NO") : "N/A";
        ClientLog.Debug($"CURRENTLY VIEWED WORLD ({reason}): {targetLetter}; base={Menus[_menuIndex].Name}; facing={FacingName(facing)}; first-level mode for this world changed and verified this session: {applied}.");
    }

    // In gameplay, 0x00167A8F is a zero-based letter ID: 00=A, 01=B, ... 13=T,
    // 14=U, 15=V, 16=W, 17=?, 18=X, 19=Y, 1A=Z. In the letter-selection menu,
    // the same byte can represent the base menu (00=Menu0); use the captured facing
    // target there instead of interpreting the byte as the gameplay letter.
    private static string GetLetterName(byte letterId) => letterId switch
    {
        >= 0x00 and <= 0x13 => ((char)('A' + letterId)).ToString(), // A-T: 00-13
        0x14 => "U",
        0x15 => "V",
        0x16 => "W",
        0x17 => "?",
        0x18 => "X",
        0x19 => "Y",
        0x1A => "Z",
        _ => $"ID-{letterId:X2}"
    };

    private static void ApplyMenu(DuckstationMemory gdb, int index)
    {
        var menu = Menus[index];

        // Required mapping in 0x00167A8F: Menu0=00, A=01, B=02, ... T=14.
        // Menu0 shares 00 with the game's original A index, but navigation is only allowed in menu-select mode.
        byte letterId = (byte)index;
        gdb.WriteByte(LetterAddress, letterId);
        Thread.Sleep(10);

        byte x1E = menu.X[0];
        byte x1F = menu.X[0];
        byte x1D = menu.X[1];
        byte y0 = menu.Y[0];
        byte y1 = menu.Y[1];

        if (index is 1 or 2) { y0 = 0x00; y1 = 0x30; }             // A, B
        else if (index is >= 3 and <= 5) { y0 = 0x70; y1 = 0x00; } // C, D, E use the requested custom sequence below
        if (index is 13 or 14) x1F = 0x00;                         // M, N: 1F=00
        if (index == 13) x1E = 0x80;                              // M: 1E=80
        if (index == 5) { x1F = 0x00; x1E = 0x80; }              // E: 1F=00, 1E=80
        if (index == 15) x1E = 0xC0;                              // O: 1E=C0

        (uint Address, byte Value)[] xWrites =
        [
            (0x00167D1E, x1E),
            (0x00167D1F, x1F),
            (0x00167D1D, x1D),
            (0x00167D2D, x1D),
            (0x00167D1C, menu.X[2]),
            (0x00167D2C, menu.X[2]),
        ];
        for (int repetition = 0; repetition < 2; repetition++)
        {
            foreach (var write in xWrites)
            {
                gdb.WriteByte(write.Address, write.Value);
                Thread.Sleep(10);
            }
        }

        (uint Address, byte Value)[] yWrites = index is 1 or 2
            ?
            [
                (0x00167D21, 0x00),
                (0x00167D2B, 0x00),
                (0x00167D20, 0x00),
                (0x00167D2A, 0x30),
            ]
            : index is >= 3 and <= 5
            ?
            [
                (0x00167D2B, 0x00),
                (0x00167D2A, 0x70),
                (0x00167D21, 0x00),
                (0x00167D20, 0x70),
            ]
            :
            [
                (0x00167D20, y0),
                (0x00167D2A, y0),
                (0x00167D21, y1),
                (0x00167D2B, y1),
            ];
        for (int repetition = 0; repetition < 2; repetition++)
        {
            foreach (var write in yWrites)
            {
                gdb.WriteByte(write.Address, write.Value);
                Thread.Sleep(10);
            }
        }
    }


}
