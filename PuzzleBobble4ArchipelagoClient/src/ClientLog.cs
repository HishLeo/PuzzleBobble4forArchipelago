namespace PuzzleBobble4ArchipelagoClient;

/// <summary>Writes diagnostic messages to the console when debug output is enabled.</summary>
internal static class ClientLog
{
    // Disabled by default; set to true to enable diagnostic output.
    public static bool ShowDebug { get; set; } = false;

    public static void Debug(string? message)
    {
        if (!ShowDebug || string.IsNullOrWhiteSpace(message))
            return;

        Console.WriteLine($"[DEBUG] {message}");
    }

    public static void Debug()
    {
        if (ShowDebug)
            Console.WriteLine();
    }

    public static void ItemSent(string itemName, string recipientName) { }
    public static void ItemReceived(string itemName, string senderName) { }
}
