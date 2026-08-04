namespace Chronos.Domain.Models.Issues
{
    public interface IIssue
    {
        string Key { get; set; }
        string Summary { get; set; }
        string Link { get; set; }
        string Identifier { get; set; }
    }
}
