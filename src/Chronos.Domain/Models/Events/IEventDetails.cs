namespace Chronos.Domain.Models.Events
{
    /// <summary>
    /// What the source knows about an event beyond its interval and its issue.
    ///
    /// Carries no members of its own on purpose: the implementations have nothing in
    /// common to promise, and a shared one-line summary would decide in the domain what
    /// the page should decide for itself. This is the list of allowed kinds — the nearest
    /// thing C# gives to a discriminated union — and readers switch on the concrete type.
    /// See issue #156.
    /// </summary>
    public interface IEventDetails
    {
    }
}
