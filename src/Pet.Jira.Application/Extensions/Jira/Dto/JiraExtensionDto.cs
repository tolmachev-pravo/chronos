namespace Pet.Jira.Application.Extensions.Jira.Dto
{
    public record JiraExtensionDto(
        bool IsEnabled,
        JiraExtensionSettingsDto Settings);
}
