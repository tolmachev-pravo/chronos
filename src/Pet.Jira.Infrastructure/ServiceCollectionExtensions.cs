using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pet.Jira.Application.Articles;
using Pet.Jira.Application.Authentication;
using Pet.Jira.Application.Issues;
using Pet.Jira.Application.Storage;
using Pet.Jira.Application.Users;
using Pet.Jira.Application.Worklogs;
using Pet.Jira.Application.Worklogs.Dto;
using Pet.Jira.Domain.Models.Issues;
using Pet.Jira.Domain.Models.Users;
using Pet.Jira.Infrastructure.Articles;
using Pet.Jira.Infrastructure.Authentication;
using Pet.Jira.Infrastructure.Data.Contexts;
using Pet.Jira.Infrastructure.Jira;
using Pet.Jira.Infrastructure.Jira.Health;
using Pet.Jira.Infrastructure.Jira.Query;
using Pet.Jira.Infrastructure.Storage;
using Pet.Jira.Infrastructure.Users;
using Pet.Jira.Infrastructure.Worklogs;
using Pet.Jira.Application.Extensions;
using Pet.Jira.Application.Extensions.YandexCalendar;
using Pet.Jira.Application.Security;
using Pet.Jira.Infrastructure.Extensions;
using Pet.Jira.Infrastructure.Extensions.YandexCalendar;
using Pet.Jira.Infrastructure.Security;

namespace Pet.Jira.Infrastructure
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

			services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source = JiraCopilot.sqlite3"));

			services.AddTransient<IArticleRepository, ArticleRepository>();
			services.AddTransient<IArticleDataSource, ArticleDataSource>();

			services.AddTransient<IUserRepository, UserRepository>();

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
