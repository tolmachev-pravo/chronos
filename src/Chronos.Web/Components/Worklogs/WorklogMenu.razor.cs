using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Chronos.Application.Issues.Queries;
using Chronos.Application.TextBuilder;
using Chronos.Application.Worklogs.Dto;
using Chronos.Infrastructure.TextBuilder;
using Chronos.Web.Components.Clipboard;
using Chronos.Web.Shared;
using System;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Worklogs
{
    public partial class WorklogMenu : ComponentBase
    {
        [Parameter] public EventCallback<WorkingDayWorklog> OnCreatedPressed { get; set; }
        [Parameter] public WorkingDayWorklog Entity { get; set; }
        [Parameter] public Color Color { get; set; } = Color.Default;
        [Parameter] public string Icon { get; set; } = Icons.Material.Filled.MoreVert;
        [Parameter] public string Label { get; set; } = "Default";

        [CascadingParameter] public ErrorHandler ErrorHandler { get; set; }

        [Inject] private IClipboard _clipboard { get; set; }
        [Inject] private ISnackbar _snackbar { get; set; }
        [Inject] private IMediator Mediator { get; set; }
        [Inject] IDialogService DialogService { get; set; }

        private string PullRequestUrl { get; set; }

        private async Task CopyToClipboardInReviewAsync()
        {
            await InitPullRequestUrl();
            var clipboardItemElements = BuildClipboardElementCollection(InReviewText);
            await CopyToClipboardAsync(clipboardItemElements);
        }

        private async Task CopyToClipboardInProgressAsync()
        {
            var clipboardItemElements = BuildClipboardElementCollection(InProgressText);
            await CopyToClipboardAsync(clipboardItemElements);
        }

        private async Task InitPullRequestUrl()
        {
            var result = await Mediator.Send(new GetIssueOpenPullRequestUrl.Query { Identifier = Entity.Issue.Identifier });
            PullRequestUrl = result.Url;
        }

        private static ClipboardItemElementCollection BuildClipboardElementCollection(Func<ITextBuilder, string> buildText)
        {
            return new ClipboardItemElementCollection
            {
                new ClipboardItemElement
                {
                    MimeType = ClipboardMimeType.Html,
                    Data = buildText(new HtmlTextBuilder())
                },
                new ClipboardItemElement
                {
                    MimeType = ClipboardMimeType.Plain,
                    Data = buildText(new MarkdownTextBuilder())
                }
            };
        }

        private string InReviewText(ITextBuilder textBuilder)
        {
            textBuilder
               .AddText("IN REVIEW", TextOption.Bold)
               .AddLink(Entity.Issue.Link, Entity.Issue.Key)
               .AddText(Entity.Issue.Summary);

            if (!string.IsNullOrEmpty(PullRequestUrl))
            {
                textBuilder.AddNewLine();
                textBuilder.AddLink(PullRequestUrl, PullRequestUrl);
            }
                
            return textBuilder.Build();
        }

        private string InProgressText(ITextBuilder textBuilder)
        {
            return textBuilder
                .AddText("IN PROGRESS", TextOption.Bold)
                .AddLink(Entity.Issue.Link, Entity.Issue.Key)
                .AddText(Entity.Issue.Summary)
                .Build();
        }

        private async Task CopyToClipboardAsync(ClipboardItemElementCollection clipboardItemElements)
        {
            try
            {
                var isClipboardSupported = await _clipboard.IsSupportedAsync();
                if (isClipboardSupported)
                {
                    await _clipboard.WriteAsync(clipboardItemElements);
                    _snackbar.Add(
                        $"Copied to clipboard",
                        Severity.Success,
                        config => { config.ActionColor = Color.Success; });
                }
                else
                {
                    throw new NotSupportedException("Clipboard is not supported in this browser");
                }
            }
            catch (Exception e)
            {
                ErrorHandler.ProcessError(e);
            }
        }

        private async Task AddCustomWorklogAsync()
        {
            var worklogTemplate = WorkingDayWorklog.CreateActualByEstimated(Entity);
            var options = new DialogOptions { };
            var parameters = new DialogParameters
            {
                { "WorklogTemplate", worklogTemplate }
            };
            var dialog = await DialogService.ShowAsync<WorklogDayItemDialog>("New worklog", parameters, options);
            var result = await dialog.Result;
            if (result.Data is WorkingDayWorklog worklog)
            {
                await OnCreatedPressed.InvokeAsync(worklog);
            }
        }
    }
}
