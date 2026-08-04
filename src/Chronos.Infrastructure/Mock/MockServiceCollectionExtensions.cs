using Microsoft.Extensions.DependencyInjection;
using Chronos.Application.Authentication;
using Chronos.Application.Issues;
using Chronos.Application.Storage;
using Chronos.Application.Worklogs;
using Chronos.Domain.Models.Issues;
using Chronos.Domain.Models.Users;

namespace Chronos.Infrastructure.Mock
{
    public static class MockServiceCollectionExtensions
    {
        public static IServiceCollection AddMockInfrastructureLayer(this IServiceCollection services)
        {
            services.AddTransient<IWorklogDataSource, MockWorklogDataSource>();
            services.AddTransient<IWorklogRepository, MockWorklogRepository>();
            services.AddTransient<IAuthenticationService, MockAuthenticationService>();
            services.AddTransient<IIssueDataSource, MockIssueDataSource>();
            services.AddTransient<IStorage<string, UserProfile>, MockUserProfileStorage>();
            services.AddTransient<IMemoryCache<string, Issue>, MockIssueMemoryCache>();
            return services;
        }
    }
}
