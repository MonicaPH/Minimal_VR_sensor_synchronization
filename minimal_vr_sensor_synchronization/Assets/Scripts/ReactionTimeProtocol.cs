using System.Globalization;

/// <summary>
/// Defines the small messages exchanged by Unity and the AtomS3.
/// Keeping parsing here makes the protocol easy to test without serial hardware.
/// </summary>
public static class ReactionTimeProtocol
{
    public const string StartTrial = "T";

    public static bool TryParseReactionTime(string line, out uint microseconds)
    {
        microseconds = 0;
        return line != null &&
               line.StartsWith("R:", System.StringComparison.Ordinal) &&
               uint.TryParse(
                   line.Substring(2),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out microseconds);
    }

    public static bool IsBusy(string line)
    {
        return string.Equals(line, "B", System.StringComparison.Ordinal);
    }
}
