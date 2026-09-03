using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Chronos.Application.Articles;
using Chronos.Application.Authentication;
using Chronos.Application.Issues;
using Chronos.Application.Storage;
using Chronos.Application.Users;
using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Users;
using Chronos.Infrastructure.Articles;
using Chronos.Infrastructure.Authentication;
using Chronos.Infrastructure.Data.Contexts;
using Chronos.Infrastructure.Jira;
using Chronos.Infrastructure.Jira.Health;
using Chronos.Infrastructure.Jira.Query;
using Chronos.Infrastructure.Storage;
using Chronos.Infrastructure.Users;
using Chronos.Infrastructure.Worklogs;
using Chronos.Application.Extensions;
using Chronos.Application.Events;
using Chronos.Application.Extensions.Jira;
using Chronos.Application.Extensions.YandexCalendar;
using Chronos.Application.Security;
using Chronos.Infrastructure.Extensions;
using Chronos.Infrastructure.Events;
using Chronos.Infrastructure.Extensions.Jira;
using Chronos.Infrastructure.Extensions.YandexCalendar;
using Chronos.Infrastructure.Security;

namespace Chronos.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration jiraConfigurationSection)
        {
            services.AddTransient<IJiraService, JiraService>();
            services.Configure<JiraConfiguration>(jiraConfigurationSection);

            // Actual-worklog source (issue #245): Tempo (date-filtered, low memory) when
            // enabled, otherwise the per-issue Jira source. JiraWorklogDataSource is also
            // registered as itself so TempoWorklogDataSource can delegate raw worklogs and
            // fall back to it. Bound directly here because IJiraConfiguration is not yet
            // available from the container at registration time.
            services.AddTransient<JiraWorklogDataSource>();
            var tempoEnabled = jiraConfigurationSection
                .GetValue($"{nameof(JiraConfiguration.Tempo)}:{nameof(TempoConfiguration.Enabled)}", true);
            if (tempoEnabled)
            {
                services.AddTransient<IWorklogDataSource, TempoWorklogDataSource>();
            }
            else
            {
                services.AddTransient<IWorklogDataSource, JiraWorklogDataSource>();
            }
            // The event sources behind IEventDataSource (issue #299). Registering them as
            // a collection is what makes a new source a one-line change: nothing in the
            // day assembly names a provider.
            services.AddTransient<IJiraExtensionProvider, JiraExtensionProvider>();
            services.AddTransient<IEventProvider, JiraAssigneeEventProvider>();
            services.AddTransient<IEventProvider, JiraTesterEventProvider>();
            services.AddTransient<IEventProvider, JiraCommentEventProvider>();
            services.AddTransient<IEventProvider, YandexCalendarEventProvider>();

            services.AddSingleton<IJiraLinkGenerator, JiraLinkGenerator>();
            services.AddTransient<IWorklogRepository, WorklogRepository>();
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IJiraQueryFactory, JiraQueryFactory>();
            services.AddTransient<IIssueDataSource, JiraIssueDataSource>();

            services.AddTransient<ILocalStorage<UserProfile>, UserProfileLocalStorage>();
            services.AddTransient<IDataSource<string, UserProfile>, UserProfileDataSource>();
            services.AddTransient<IStorage<string, UserProfile>, UserProfileStorage>();

            services.AddTransient<ILocalStorage<UserTheme>, UserThemeLocalStorage>();
            services.AddTransient<IStorage<string, UserTheme>, UserThemeStorage>();

            services.AddTransient<ILocalStorage<UserWorklogFilter>, UserWorklogFilterLocalStorage>();
            services.AddTransient<IStorage<string, UserWorklogFilter>, UserWorklogFilterStorage>();

            services.AddSingleton<ILoginMemoryCache, LoginMemoryCache>();
            services.AddTransient<IMemoryCache<string, Issue>, IssueMemoryCache>();

			// The file name predates the rename to Chronos (issue #226) and stays as it is on
			// purpose. The path is relative, so on the server it resolves inside the deployed
			// site folder. Renaming it here without renaming the file on the server first, with
			// the AppPool stopped, silently creates an empty database and hides every existing
			// user, their extensions and their encrypted secrets.
			services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source = JiraCopilot.sqlite3"));

			services.AddTransient<IArticleRepository, ArticleRepository>();
			services.AddTransient<IArticleDataSource, ArticleDataSource>();

			services.AddTransient<IUserRepository, UserRepository>();
			services.AddTransient<IUserSettingsRepository, UserSettingsRepository>();

			services.AddDataProtection()
				.PersistKeysToFileSystem(new System.IO.DirectoryInfo(
					System.IO.Path.Combine(System.AppContext.BaseDirectory, "DataProtection-Keys")))
				.SetApplicationName("Chronos");
			services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

			services.AddHttpClient<IYandexCalendarService, YandexCalDavService>();
			services.AddTransient<IUserExtensionRepository, UserExtensionRepository>();
			services.AddTransient<IYandexCalendarSettingsProvider, YandexCalendarSettingsProvider>();

			return services;
        }

        public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IHealthChecksBuilder builder)
        {
            builder
                .AddJiraHealthCheck()
                .AddProcessAllocatedMemoryHealthCheck(
                    maximumMegabytesAllocated: 300,
                    tags: ["system"]);
            return builder;
        }
    }
}
