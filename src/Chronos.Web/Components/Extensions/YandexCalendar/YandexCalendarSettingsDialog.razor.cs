using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Application.Extensions.YandexCalendar.Commands;
using Chronos.Application.Extensions.YandexCalendar.Dto;
using Chronos.Application.Extensions.YandexCalendar.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Web.Components.Extensions.YandexCalendar
{
    public partial class YandexCalendarSettingsDialog : ComponentBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter] public string Username { get; set; } = string.Empty;
        [Parameter] public YandexCalendarSettingsDto? ExistingSettings { get; set; }

        [Inject] private IMediator Mediator { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private MudForm _form = default!;
        private string _login = string.Empty;
        private string _appPassword = string.Empty;
        private bool _showPassword;
        private bool _isTesting;
        private TestResultInfo? _testResult;
        private List<string> _excludedPhrases = new();
        private string _newPhrase = string.Empty;
        private List<YandexCalendarIssueMapping> _issueMappings = new();
        private string _newMappingPhrase = string.Empty;
        private string _newMappingIssueKey = string.Empty;

        protected override void OnParametersSet()
        {
            if (ExistingSettings is not null)
            {
                _login = ExistingSettings.Login;
                _appPassword = ExistingSettings.AppPassword;
                _excludedPhrases = [.. ExistingSettings.ExcludedPhrases];
                _issueMappings = [.. ExistingSettings.IssueMappings];
            }
        }

        private async Task TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_login) || string.IsNullOrWhiteSpace(_appPassword))
            {
                _testResult = TestResultInfo.Warning("Заполните логин и пароль");
                return;
            }

            _isTesting = true;
            _testResult = null;

            try
            {
                var count = await Mediator.Send(new TestYandexCalendarConnection.Query(_login, _appPassword));
                _testResult = TestResultInfo.Success($"Подключено! Найдено {count} событий на сегодня");
            }
            catch (Exception ex)
            {
                _testResult = TestResultInfo.Error($"Ошибка подключения: {ex.Message}");
            }
            finally
            {
                _isTesting = false;
            }
        }

        private void AddPhrase()
        {
            var phrase = _newPhrase.Trim();
            if (string.IsNullOrEmpty(phrase) || _excludedPhrases.Any(p => p.Equals(phrase, StringComparison.OrdinalIgnoreCase)))
                return;
            _excludedPhrases.Add(phrase);
            _newPhrase = string.Empty;
        }

        private void OnPhraseKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter") AddPhrase();
        }

        private void OnChipClose(MudChip<string> chip) => _excludedPhrases.Remove(chip.Text);

        private void AddMapping()
        {
            var phrase = YandexCalendarIssueMappingMatcher.Normalize(_newMappingPhrase);
            var key = _newMappingIssueKey.Trim();
            if (string.IsNullOrEmpty(phrase) || string.IsNullOrEmpty(key))
                return;
            if (_issueMappings.Any(m => YandexCalendarIssueMappingMatcher.Normalize(m.Phrase)
                    .Equals(phrase, StringComparison.OrdinalIgnoreCase)))
                return;
            _issueMappings.Add(new YandexCalendarIssueMapping(phrase, key));
            _newMappingPhrase = string.Empty;
            _newMappingIssueKey = string.Empty;
        }

        private void OnMappingKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter") AddMapping();
        }

        private void OnMappingChipClose(MudChip<YandexCalendarIssueMapping> chip)
        {
            if (chip.Value is not null)
                _issueMappings.Remove(chip.Value);
        }

        private async Task Save()
        {
            await _form.ValidateAsync();
            if (!_form.IsValid) return;

            var settings = new YandexCalendarSettingsDto(
                _login,
                _appPassword,
                _excludedPhrases.AsReadOnly(),
                _issueMappings.AsReadOnly());

            await Mediator.Send(new UpsertYandexCalendarExtension.Command(Username, settings, IsEnabled: true));
            MudDialog.Close(DialogResult.Ok(settings));
        }

        private void Cancel() => MudDialog.Cancel();

        private sealed record TestResultInfo(Severity AlertSeverity, string Message)
        {
            public static TestResultInfo Success(string msg) => new(Severity.Success, msg);
            public static TestResultInfo Error(string msg)   => new(Severity.Error,   msg);
            public static TestResultInfo Warning(string msg) => new(Severity.Warning,  msg);
        }
    }
}
