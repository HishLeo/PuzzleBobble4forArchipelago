namespace PuzzleBobble4ArchipelagoClient;

/// <summary>
/// Keeps internal game/memory diagnostics out of the player's console.
/// Only Archipelago network events should be shown to the player.
/// </summary>
internal static class ClientLog
{
    // Keep diagnostic logging code available, but hide it in normal client output.
    // Change this to true when you need to inspect internal diagnostics.
    private static bool ShowDebug = false;

    public static void Debug(string? message)
    {
        if (ShowDebug && !string.IsNullOrWhiteSpace(message))
            Console.WriteLine($"[DEBUG] {message}");
    }

    // Preserve blank-line calls in the existing client; they are hidden with debug output.
    public static void Debug()
    {
        if (ShowDebug)
            Console.WriteLine();
    }

    // Call these from the real Archipelago connection/event handlers.
    public static void ItemSent(string itemName, string recipientName)
        => Console.WriteLine($"YOU sent {itemName} to {recipientName}.");

    public static void ItemReceived(string itemName, string senderName)
        => Console.WriteLine($"{senderName} sent {itemName} to YOU.");

    // Deliberately omitted: check submissions are reported only after server confirmation.
}
