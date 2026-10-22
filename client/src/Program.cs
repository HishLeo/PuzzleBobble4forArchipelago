using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
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

    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(280);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(75);

    private const uint GameModeAddress = 0x00167A0C;
    private const uint LevelStateAddress = 0x00167A10;
    private const uint LetterAddress = 0x00167A8F;
    private const uint LevelNumberAddress = 0x00167A8D;
    private const uint AnotherWorldAddress = 0x00167A96;
    private const uint DifficultyAddress = 0x00167A91;
    private const byte OriginalValue = 0x00;
    private const byte AnotherWorldValue = 0x01;
    private const byte NormalValue = 0x01;
    private const byte ExpertValue = 0x02;
    private const uint FacingAddress = 0x00167D3A;
    private const uint ControllerInputAddress = 0x0016BCEB;
    private const uint ControllerVerticalInputAddress = 0x0016BCEA;
    private const uint ActiveCharacterAddress = 0x00167A45;
    private const uint NextBubbleAddress = 0x00188BE0;
    private const uint PointerGuideAddress = 0x00192EFC;
    private const uint FallingCeilingAddress = 0x00192EBD;
    private const uint TimerAddress = 0x00167D62;
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
    private const byte InputUp = 0xEF;
    private const byte InputDown = 0xBF;

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
    private static readonly ConcurrentQueue<string> _receivedItemQueue = new();
    private static readonly ConcurrentQueue<int> _localTestCommandQueue = new();
    private static int _localDeathLinkTestPending;
    private static readonly TimeSpan DuckstationRetryInterval = TimeSpan.FromSeconds(5);
    private static LevelModePlan? _modePlan;
    private static DateTime _nextReconnectAttempt = DateTime.MinValue;
    private static bool _sentInitialContinue;
    private static DateTime _lastAction = DateTime.MinValue;
    private static byte? _previousLevelState;
    private static byte? _previousGameMode;
    private static byte? _previousLetterValue;
    private static byte? _previousFacing;
    private static byte? _previousControllerInput;
    private static byte? _previousVerticalControllerInput;
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

    private static void Main()
    {
        EnableConsoleColors();
        Console.Title = "Puzzle Bobble 4 Archipelago Client";

        // Item weights and final level -> item placements come from the generated
        // Archipelago slot_data. The client never reads item-weights.json or rolls
        // its own item plan locally.

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

        _apSession = ConnectToArchipelago();
        if (_apSession is null)
        {
            DisconnectMemory();
            return;
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
    private const string VictoryColor = "\u001b[38;2;80;255;95m";           // bright green
    private const string ErrorColor = "\u001b[38;2;255;155;55m";            // orange
    private const string NeutralColor = "\u001b[38;2;255;255;255m";         // white

    private static void WriteColored(string text, string color)
    {
        Console.Write(color);
        Console.Write(text);
        Console.Write(AnsiReset);
    }

    private static void WriteClientLine(string text)
    {
        string lower = text.ToLowerInvariant();

        if (lower.StartsWith("received item: level skip."))
        {
            // Level Skip is a Useful item, so use the same turquoise as its category.
            WriteColored(text, UsefulColor);
        }
        else if (lower.StartsWith("death link enabled for this seed."))
        {
            WriteColored(text, DeadLinkColor);
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
        else if (lower.Contains("victory") || lower.Contains("completed their goal") ||
                 lower.Contains("goal complete") || lower.Contains("game completed") ||
                 lower.StartsWith("goal of this run:"))
        {
            WriteColored(text, VictoryColor);
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
            string itemColor = lowerItem.Contains("trap") ? TrapColor
                : lowerItem.Contains("level skip") || lowerItem.Contains("pointer-guide") ||
                  lowerItem.Contains("star-bubble") || lowerItem.Contains("metal-bubble") ? UsefulColor
                : lowerItem.Contains("unlock") || lowerItem.Contains("victory") ? ProgressionColor
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
            string itemColor = lowerItem.Contains("trap") ? TrapColor
                : lowerItem.Contains("level skip") || lowerItem.Contains("pointer-guide") ||
                  lowerItem.Contains("star-bubble") || lowerItem.Contains("metal-bubble") ? UsefulColor
                : lowerItem.Contains("unlock") || lowerItem.Contains("victory") ? ProgressionColor
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
            string itemColor = lowerItem.Contains("trap") ? TrapColor
                : lowerItem.Contains("level skip") || lowerItem.Contains("pointer-guide") ||
                  lowerItem.Contains("star-bubble") || lowerItem.Contains("metal-bubble") ? UsefulColor
                : lowerItem.Contains("unlock") || lowerItem.Contains("victory") ? ProgressionColor
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

    private static ArchipelagoSession? ConnectToArchipelago()
    {
        while (true)
        {
            Console.Write("Enter AP domain (leave blank for archipelago.gg): ");
            string domain = (Console.ReadLine() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(domain))
                domain = "archipelago.gg";
            if (domain.Length == 0)
            {
                WriteClientLine("Domain cannot be empty.");
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
                        _receivedItemQueue.Enqueue(itemName);
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
                        // DeathLink is opt-in through this seed's YAML option.
                        // The client does not participate unless the APWorld explicitly
                        // reports that the slot enabled it.
                        _deathLinkEnabled = TryReadSlotDataBoolean(loginSuccessful.SlotData, "death_link", out bool deathLinkOption) && deathLinkOption;
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

                        // The APWorld generates the weighted mode for every level
                        // from the seed RNG and sends the complete map via slot_data.
                        // Never fall back to a local random plan: without this data,
                        // level modes stay untouched so we cannot apply the wrong seed.
                        _modePlan = null;
                        string modePlanError = "slot_data does not contain 'level_mode_plan'.";
                        bool hasModePlan = loginSuccessful.SlotData.TryGetValue("level_mode_plan", out object? rawModePlan);
                        LevelModePlan? receivedPlan = null;
                        bool validModePlan = hasModePlan &&
                            LevelModeGenerator.TryCreatePlan(rawModePlan, out receivedPlan, out modePlanError);
                        if (validModePlan)
                        {
                            _modePlan = receivedPlan;
                            WriteClientLine($"Level mode plan received from Archipelago: {receivedPlan!.Assignments.Count} levels.");
                        }
                        else
                        {
                            WriteClientLine($"WARNING: Could not read the level mode plan from Archipelago: {modePlanError} Level modes will not be changed.");
                        }

                        // Item placements and item weights are intentionally not displayed or written
                        // to local files. Items are secret; the server remains authoritative for delivery.

                        // Read any extra Unlock prerequisites supplied by the world.
                        // Goal-world Unlock items stay pending until all required remote
                        // Unlocks have actually arrived and unlocked.
                        _unlockPrerequisites.Clear();
                        if (loginSuccessful.SlotData.TryGetValue("unlock_prerequisites", out object? rawPrerequisites) &&
                            rawPrerequisites is System.Collections.IDictionary prerequisiteMap)
                        {
                            foreach (System.Collections.DictionaryEntry entry in prerequisiteMap)
                            {
                                string? targetLetter = Convert.ToString(entry.Key)?.Trim().ToUpperInvariant();
                                if (!IsKnownLetterName(targetLetter) || entry.Value is not System.Collections.IEnumerable prerequisiteValues)
                                    continue;

                                var requiredLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                foreach (object? value in prerequisiteValues)
                                {
                                    string? requiredLetter = Convert.ToString(value)?.Trim().ToUpperInvariant();
                                    if (IsKnownLetterName(requiredLetter))
                                        requiredLetters.Add(requiredLetter!);
                                }
                                _unlockPrerequisites[targetLetter!] = requiredLetters;
                            }
                        }

                        // The world supplies the selected goal letter(s) in slot_data.
                        // This supports both a single random goal and the "all U-Z" goal.
                        if (loginSuccessful.SlotData.TryGetValue("goal_letters", out object? rawGoalLetters) &&
                            rawGoalLetters is System.Collections.IEnumerable goalLetterValues)
                        {
                            foreach (object? value in goalLetterValues)
                            {
                                string? goalLetter = Convert.ToString(value)?.Trim();
                                if (IsKnownLetterName(goalLetter))
                                    _goalLetters.Add(goalLetter!);
                            }
                        }
                    }

                    if (_goalLetters.Count == 1)
                        WriteClientLine($"Goal of this run: Finish World {_goalLetters.First()}.");
                    else if (_goalLetters.Count > 1)
                        WriteClientLine($"Goal of this run: Finish Worlds {string.Join(", ", _goalLetters.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}.");
                    else
                        WriteClientLine("WARNING: Could not read goal_letters from slot_data. Automatic goal completion is disabled to avoid reporting an incorrect victory.");

                    // Seed the local duplicate guard from the server's authoritative checked list.
                    foreach (long checkedId in session.Locations.AllLocationsChecked)
                    {
                        string? checkedName = session.Locations.GetLocationNameFromId(
                            checkedId, "Puzzle Bobble 4");
                        if (!string.IsNullOrWhiteSpace(checkedName))
                        {
                            _checksLoggedThisRun.TryAdd(checkedName, 0);
                            _confirmedChecks.TryAdd(checkedName, 0);
                        }
                    }

                    // This event is the server-side confirmation, unlike a local "sent" message.
                    session.Locations.CheckedLocationsUpdated += checkedIds =>
                    {
                        foreach (long checkedId in checkedIds)
                        {
                            string? checkedName = session.Locations.GetLocationNameFromId(
                                checkedId, "Puzzle Bobble 4");
                            if (!string.IsNullOrWhiteSpace(checkedName))
                            {
                                _checksLoggedThisRun.TryAdd(checkedName, 0);
                                _confirmedChecks.TryAdd(checkedName, 0);
                                ClientLog.Debug($"Archipelago confirmed check: {checkedName}");
                            }
                        }
                    };

                    WriteClientLine("Connected to Archipelago.");
                    WriteClientLine("Type server commands such as !help and press Enter. Type /help for local command help.");
                    StartConsoleCommandReader(session);
                    WriteClientLine($"Synced {session.Locations.AllLocationsChecked.Count} checks already registered by the server.");
                    return session;
                }

                WriteClientLine("Could not connect to Archipelago. Check the domain, port, slot name, and room password.");
            }
            catch (Exception ex)
            {
                WriteClientLine($"Could not connect to Archipelago: {ex.GetBaseException().Message}");
            }

            WriteClientLine("Please enter the connection details again.");
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
                    continue;
                }

                // Player release: swallow local test commands. Their implementation
                // remains in the source, but players cannot invoke /1 through /9.
                if (line.Length == 2 && line[0] == '/' && line[1] is >= '1' and <= '9')
                {
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
    private static void ProcessLocalTestCommands(DuckstationMemory gdb, byte gameMode, byte levelState)
    {
        while (_localTestCommandQueue.TryDequeue(out int number))
        {
            if (gameMode != InLevelMode || levelState != 0x02)
            {
                WriteClientLine($"Local command /{number} ignored: enter a level and wait until gameplay is active.");
                continue;
            }

            switch (number)
            {
                case 1:
                    gdb.WriteByte(NextBubbleAddress, 0x0A);
                    WriteClientLine("TEST: Star Bubble applied (/1).");
                    break;
                case 2:
                    gdb.WriteByte(NextBubbleAddress, 0x0B);
                    WriteClientLine("TEST: Metal Bubble applied (/2).");
                    break;
                case 3:
                    gdb.WriteByte(PointerGuideAddress, 0x80);
                    WriteClientLine("TEST: Pointer Guide activated (/3).");
                    break;
                case 4:
                    HandleReceivedItem("Level Skip");
                    WriteClientLine("TEST: added one Level Skip (/4).");
                    break;
                case 5:
                    gdb.WriteByte(NextBubbleAddress, 0x0C);
                    WriteClientLine("TEST: Rainbow Bubble applied (/5).");
                    break;
                case 6:
                    gdb.WriteByte(NextBubbleAddress, 0x0D);
                    WriteClientLine("TEST: Obstacle Block applied (/6).");
                    break;
                case 7:
                {
                    byte randomBubble = (byte)Random.Shared.Next(0x01, 0x09);
                    gdb.WriteByte(NextBubbleAddress, randomBubble);
                    WriteClientLine($"TEST: Random next color applied (/7; value=0x{randomBubble:X2}).");
                    break;
                }
                case 8:
                    gdb.WriteByte(FallingCeilingAddress, 0x10);
                    WriteClientLine("TEST: Falling Ceiling activated (/8).");
                    break;
                case 9:
                    // Exercise the same receive/reset path as a real DeathLink, but locally.
                    // This test is allowed even when DeathLink is disabled in the YAML.
                    Interlocked.Exchange(ref _localDeathLinkTestPending, 1);
                    WriteColoredLine("TEST: Simulated incoming DeathLink queued (/9).", DeadLinkColor);
                    break;
            }
        }
    }

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

    private static bool IsUsableDestination(int target) =>
        GetDestinationName(target) is not null && !IsLocked(target);

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

    private static void UpdateSkipCounterHud(DuckstationMemory gdb)
    {
        // The HUD stores ASCII digits, not a binary integer. Keep both copies aligned.
        int clamped = Math.Clamp(_levelSkipCount, 0, 99);
        _levelSkipCount = clamped;
        byte tens = (byte)('0' + (clamped / 10));
        byte units = (byte)('0' + (clamped % 10));
        gdb.WriteByte(0x0011A007, tens);
        gdb.WriteByte(0x0011A008, units);
        gdb.WriteByte(0x0011A017, tens);
        gdb.WriteByte(0x0011A018, units);
    }

    private static void SynchronizeSkipCounterMirror(DuckstationMemory gdb)
    {
        byte tens = gdb.ReadByte(0x0011A007);
        byte units = gdb.ReadByte(0x0011A008);
        if (gdb.ReadByte(0x0011A017) != tens) gdb.WriteByte(0x0011A017, tens);
        if (gdb.ReadByte(0x0011A018) != units) gdb.WriteByte(0x0011A018, units);
    }

    private static void SendLocationCheck(string checkName)
    {
        var session = _apSession;
        if (session is null)
        {
            WriteClientLine($"WARNING: check {checkName} detected, but there is no Archipelago session.");
            return;
        }

        try
        {
            long locationId = session.Locations.GetLocationIdFromName("Puzzle Bobble 4", checkName);
            if (locationId < 0)
                throw new InvalidOperationException(
                    $"Location '{checkName}' was not found in the Archipelago datapackage for Puzzle Bobble 4.");

            session.Locations.CompleteLocationChecks(locationId);
            ClientLog.Debug($"Sent check {checkName} to Archipelago.");
        }
        catch (Exception ex)
        {
            WriteClientLine($"Could not send check {checkName} to Archipelago: {ex.GetBaseException().Message}");
        }
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
            var alreadyChecked = new HashSet<long>(session.Locations.AllLocationsChecked);
            var remainingLocationIds = new List<long>();
            string allLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ?";
            foreach (char letter in allLetters)
            {
                for (int level = 1; level <= 5; level++)
                {
                    string locationName = $"{letter}{level}";
                    long locationId = session.Locations.GetLocationIdFromName("Puzzle Bobble 4", locationName);
                    if (locationId >= 0 && !alreadyChecked.Contains(locationId))
                        remainingLocationIds.Add(locationId);
                }
            }

            if (remainingLocationIds.Count > 0)
            {
                session.Locations.CompleteLocationChecks(remainingLocationIds.ToArray());
                WriteClientLine($"GOAL COMPLETE: Sent {remainingLocationIds.Count} remaining checks to Archipelago.");
            }
            else
            {
                WriteClientLine("GOAL COMPLETE: There were no pending checks left to send.");
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
        while (_receivedItemQueue.TryDequeue(out string? itemName))
        {
            HandleReceivedItem(itemName);
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
                    WriteClientLine($"WORLD UNLOCKED: {letter}. You can now access this world.");
                    changed = true;
                }
            }
        } while (changed);
    }

    // Received items are queued by the Archipelago callback and applied on the
    // main polling thread, where DuckStation memory access is already managed.
    internal static void HandleReceivedItem(string itemName)
    {
        // Unlock items must change the client's real navigation lock state, not
        // merely appear in the Archipelago item log. Accept the normal item name
        // and the FILL_ prefix some client/server paths may expose.
        Match unlockMatch = Regex.Match(
            itemName,
            @"^(?:FILL[_ ]*)?Unlock\s+([A-Z?])\s+world$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (unlockMatch.Success)
        {
            string letter = unlockMatch.Groups[1].Value.ToUpperInvariant();
            _receivedUnlockLetters.Add(letter);
            RefreshUnlockedLetters();
            return;
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

            // Victory items are placed at the required X5 locations. Wait for every
            // configured goal item, so "All U-Z" does not finish after only one world.
            if (!_goalCompletionHandled && _goalLetters.Count > 0 &&
                _goalLetters.All(_receivedVictoryLetters.Contains))
            {
                HandleGoalReached();
            }
            return;
        }

        if (!string.Equals(itemName, "Level Skip", StringComparison.Ordinal) &&
            !string.Equals(itemName, "FILL_Level Skip", StringComparison.Ordinal)) return;
        if (_levelSkipCount >= 99)
        {
            ClientLog.Debug("A hidden usable item arrived, but the counter is already at its maximum (99). Nothing was added.");
            return;
        }

        _levelSkipCount++;
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
        bool alreadyChecked = IsKnownLetterName(letterName) &&
            _confirmedChecks.ContainsKey(currentLevelId);

        if (!alreadyChecked && _levelSkipCount <= 0)
        {
            ClientLog.Debug("Level Skip input ignored: this level is not checked and no uses are available (00).");
            return false;
        }

        // Prepare the next level's mode BEFORE requesting the state transition.
        // With direct process-memory access DuckStation keeps running between polls,
        // so the game can consume 02 -> 03 before the next poll observes that edge.
        if (IsKnownLetterName(letterName) && displayedLevel < 5)
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
            UpdateSkipCounterHud(gdb);
            ClientLog.Debug($"HIDDEN ITEM USED: R2; 0x{LevelStateAddress:X8} set to 03; remaining={_levelSkipCount:00}.");
        }
        return true;
    }

    private static void PollGameState()
    {
        var gdb = _gdb!;
        // Read all relevant bytes each poll. State at 0x00167A10 is deliberately
        // treated as a transition, not as a persistent victory flag.
        byte gameMode = gdb.ReadByte(GameModeAddress);
        byte levelState = gdb.ReadByte(LevelStateAddress);
        byte letter = gdb.ReadByte(LetterAddress);
        byte levelNumber = gdb.ReadByte(LevelNumberAddress);
        byte facing = gdb.ReadByte(FacingAddress);
        byte controllerInput = gdb.ReadByte(ControllerInputAddress);
        byte verticalControllerInput = gdb.ReadByte(ControllerVerticalInputAddress);

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
        }
        ProcessReceivedItems();
        ProcessLocalTestCommands(gdb, gameMode, levelState);
        SynchronizeSkipCounterMirror(gdb);

        // A received DeathLink is applied only while actively playing a level.
        // Puzzle Bobble 4's safe reset transition is 00; writing 04 directly
        // can trigger game bugs, so never use 04 to force a received death.
        if (ProcessPendingDeathLink(gdb, gameMode, levelState))
        {
            _previousLevelState = 0x00;
            _previousGameMode = gameMode;
            _previousLetterValue = letter;
            _previousFacing = facing;
            _previousControllerInput = controllerInput;
            return;
        }

        // State 04 is the game's own Game Over signal. Send exactly once on the
        // transition into it, and only while DeathLink is enabled for this seed.
        if (_deathLinkEnabled && _deathLinkService is not null &&
            gameMode == InLevelMode && levelState == 0x04 && _previousLevelState != 0x04)
        {
            try
            {
                string cause = $"{_slotName} got a Game Over in Puzzle Bobble 4";
                _deathLinkService.SendDeathLink(new DeathLink(_slotName, cause));
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
            if (gameMode == MenuLetterSelectMode)
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
            else if (gameMode == InLevelMode && levelState == 0x02 && controllerInput == InputR2)
            {
                TryUseLevelSkip(gdb, ref levelState, letter, levelNumber);
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

        _previousGameMode = gameMode;
        _previousLetterValue = letter;

        // While in the selection screen, capture the actual outgoing destination
        // (the letter being looked at), not the base menu ID stored at 0x00167A8F.
        if (gameMode == MenuLetterSelectMode)
            PrepareLookedAtLetterMode(gdb, facing, "seguimiento de destino");

        // Explicit diagnostic for every transition of the level-state byte to 05,
        // regardless of the current game-mode byte.
        if (_previousLevelState.HasValue && _previousLevelState.Value != EnteredLetter1State && levelState == EnteredLetter1State)
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
        if (_previousLevelState != EnteredLetter1State &&
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

        if (_previousLevelState == 0x02 && levelState == 0x03 && gameMode == InLevelMode)
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

                SendLocationCheck(checkId);
                ClientLog.Debug();
            }
        }

        _previousLevelState = levelState;
    }

    private static bool ProcessPendingDeathLink(DuckstationMemory gdb, byte gameMode, byte levelState)
    {
        if (gameMode != InLevelMode || levelState != 0x02)
            return false;

        // /9 simulates a received signal locally, so it must work even when the
        // DeathLink YAML option is disabled or the AP server is disconnected.
        bool localTest = Interlocked.Exchange(ref _localDeathLinkTestPending, 0) == 1;
        DeathLink? receivedDeath = null;
        if (!localTest)
        {
            if (!_deathLinkEnabled || _deathLinkService is null ||
                !_incomingDeathLinks.TryDequeue(out receivedDeath))
                return false;

            // Collapse additional signals already queued while waiting for active gameplay.
            while (_incomingDeathLinks.TryDequeue(out _)) { }
        }

        string cause = localTest
            ? "Local test command /9."
            : string.IsNullOrWhiteSpace(receivedDeath!.Cause)
                ? $"DeathLink received from {receivedDeath.Source}."
                : receivedDeath.Cause;
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
