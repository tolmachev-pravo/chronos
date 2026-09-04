namespace Chronos.Web.Mcp.Contracts
{
    /// <summary>
    /// The working day from the user's profile — the frame every suggestion is fitted into.
    /// </summary>
    public record UserSettingsView(
        string Username,
        string WorkingStartTime,
        string WorkingEndTime,
        int LunchMinutes,
        int WorkingMinutes);
}
