using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Chronos.Application.Calendar.Commands;
using Chronos.Infrastructure.WorkingDays;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Web.WorkingDays
{
    /// <summary>
    /// Keeps the production calendar in our table: the current year and the next one are
    /// read again on start and then once per interval, so a newly published year or a
    /// moved holiday arrives on its own. A source that is down only leaves the table as it
    /// was — loading a day never waits on it. See issue #310.
    /// </summary>
    public class ProductionCalendarImportService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ProductionCalendarOptions _options;
        private readonly ILogger<ProductionCalendarImportService> _logger;

        public ProductionCalendarImportService(
            IServiceScopeFactory scopeFactory,
            IHostApplicationLifetime lifetime,
            IOptions<ProductionCalendarOptions> options,
            ILogger<ProductionCalendarImportService> logger)
        {
            _scopeFactory = scopeFactory;
            _lifetime = lifetime;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
                return;

            // The database is migrated while the application starts; the tables may not be
            // there before it has.
            if (!await WaitForStartAsync(stoppingToken))
                return;

            var interval = TimeSpan.FromHours(Math.Max(1, _options.RefreshIntervalHours));
            while (!stoppingToken.IsCancellationRequested)
            {
                var year = DateTime.Today.Year;
                await ImportAsync(year, stoppingToken);
                await ImportAsync(year + 1, stoppingToken);

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task<bool> WaitForStartAsync(CancellationToken stoppingToken)
        {
            var started = new TaskCompletionSource();
            using var onStarted = _lifetime.ApplicationStarted.Register(() => started.TrySetResult());
            using var onStopping = stoppingToken.Register(() => started.TrySetCanceled());
            try
            {
                await started.Task;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private async Task ImportAsync(int year, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var count = await mediator.Send(new ImportProductionCalendar.Command(year), stoppingToken);

                if (count is null)
                    _logger.LogInformation("Production calendar for {Year} is not published yet", year);
                else
                    _logger.LogInformation("Production calendar for {Year} imported: {Count} days", year, count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Production calendar for {Year} was not imported", year);
            }
        }
    }
}
