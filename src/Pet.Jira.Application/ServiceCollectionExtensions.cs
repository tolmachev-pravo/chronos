using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pet.Jira.Application.Common.Behaviors;
using Pet.Jira.Application.Storage;
using Pet.Jira.Application.Time;
using Pet.Jira.Application.Tracing;
using Pet.Jira.Application.Users;
using Pet.Jira.Application.Worklogs;
using Pet.Jira.Application.Worklogs.Dto;
using Pet.Jira.Domain.Models.Users;
using System.Reflection;
using FluentValidation;

namespace Pet.Jira.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddSingleton<ITimeProvider, TimeProvider>();
            services.AddSingleton<IMemoryCache<string, UserProfile>, UserProfileMemoryCache>();
            services.AddSingleton<IMemoryCache<string, UserTheme>, UserThemeMemoryCache>();
            services.AddSingleton<IMemoryCache<string, UserWorklogFilter>, UserWorklogFilterMemoryCache>();

			services.AddSingleton<IPerformanceStatsCollector, PerformanceStatsCollector>();

			services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
			services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

			return services;
        }
    }
}
