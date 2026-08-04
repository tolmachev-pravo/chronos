namespace Chronos.Infrastructure.Jira
{
    public interface IJiraLinkGenerator
    {
        string Generate(string issueKey);
    }
}
