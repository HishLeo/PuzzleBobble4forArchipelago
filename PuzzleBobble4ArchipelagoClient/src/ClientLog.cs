namespace PuzzleBobble4ArchipelagoClient;

/// <summary>Writes diagnostic messages to the console when DEBUG is enabled by the caller.</summary>
internal static class ClientLog
{
    public static void Debug(string? message) { }

    public static void Debug() { }
    public static void ItemSent(string itemName, string recipientName) { }
    public static void ItemReceived(string itemName, string senderName) { }
}
