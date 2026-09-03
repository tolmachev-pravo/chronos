using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Chronos.Application.Common.Behaviors;
using Chronos.Application.Events;
using Chronos.Application.Storage;
using Chronos.Application.Time;
using Chronos.Application.Tracing;
using Chronos.Application.Users;
using Chronos.Application.Worklogs;
using Chronos.Application.Worklogs.Dto;
using Chronos.Domain.Models.Users;
using System.Reflection;
using FluentValidation;

namespace Chronos.Application
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

			// The orchestrator over the registered IEventProvider implementations; the
			// providers themselves are registered in the infrastructure layer, so nothing
			// here names a source. See issue #299.
			services.AddTransient<IEventDataSource, EventDataSource>();

			services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
			services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			return services;
        }
    }
}
