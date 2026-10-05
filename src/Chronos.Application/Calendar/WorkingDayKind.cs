namespace Chronos.Application.Calendar
{
    /// <summary>
    /// What a day turned out to be for a user, once their absences and the production
    /// calendar are laid over the weekday rule. See issue #310.
    /// </summary>
    public enum WorkingDayKind
    {
        Workday,
        ShortDay,
        Weekend,
        Holiday,
        Absence
    }
}
