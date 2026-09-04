using Blazored.LocalStorage;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;
using Chronos.Application;
using Chronos.Application.Authentication;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Data.Contexts;
using Chronos.Infrastructure.Mock;
using Chronos.Web.Authentication;
using Chronos.Web.Common;
using Chronos.Web.Components.Clipboard;
using Chronos.Web.Components.Features;
using Chronos.Web.Components.Markdown;
using Chronos.Web.Components.Releases;
using Chronos.Web.Logging;
using Chronos.Web.Mcp;
using System;
using Thinktecture.Blazor.AsyncClipboard;

namespace Chronos.Web
{
	public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        private McpOptions McpOptions => Configuration
            .GetSection(Mcp.McpOptions.SectionName)
            .Get<McpOptions>() ?? new McpOptions();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor(options =>
                {
                    options.DetailedErrors = Environment.IsDevelopment();
                    // Retain the disconnected circuit so a reconnect within this window
                    // restores page state instead of forcing a reload (see _Host.cshtml).
                    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
                    options.DisconnectedCircuitMaxRetained = 100;
                })
                .AddHubOptions(options =>
                {
                    // Looser timeouts to survive brief drops on unstable networks / VPN.
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                });

			services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 10000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
            });
            services.AddMudMarkdownServices();
            // Two ways in, one IIdentityService (issue #298): the browser brings a Blazor
            // circuit, a token client brings only its request.
            services.AddScoped<RequestUserAccessor>();
            services.AddTransient<CircuitIdentityService>();
            services.AddTransient<IIdentityService>(provider => new CompositeIdentityService(
                provider.GetRequiredService<RequestUserAccessor>(),
                provider.GetRequiredService<CircuitIdentityService>));
            services.AddTransient<IMarkdownService, MarkdownService>();
            services.AddTransient<IFeatureCatalogService, FeatureCatalogService>();

            // Releases (GitHub Releases API)
            services.AddMemoryCache();
            services.Configure<ReleaseOptions>(Configuration.GetSection(ReleaseOptions.SectionName));
            services.AddHttpClient<IReleaseService, GitHubReleaseService>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ReleaseOptions>>().Value;
                client.BaseAddress = new Uri("https://api.github.com/");
                client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Chronos");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            });

            // Layers
            services.AddInfrastructureLayer(Configuration.GetSection("Jira"));
            services.AddApplicationLayer();
            // Mock
            if (EnvironmentExtensions.IsMock())
            {
                services.AddMockInfrastructureLayer();
            }

            // Authentication
            services.AddHttpContextAccessor();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                })
                // Not a default scheme: only endpoints that ask for it by policy are opened
                // to a personal access token, the site itself stays on cookies.
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, PersonalAccessTokenAuthenticationHandler>(
                    PersonalAccessTokenDefaults.AuthenticationScheme, configureOptions: null);

            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    PersonalAccessTokenDefaults.AuthorizationPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(PersonalAccessTokenDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser());
            });

            // Local storage
            services.AddBlazoredLocalStorage();

            // Clipboard
            services.AddAsyncClipboardService();
            services.AddTransient<IClipboard, Clipboard>();

            // Health checks
            services.AddHealthChecks()
                .AddInfrastructureHealthChecks();
            services
                .AddHealthChecksUI(setupSettings: setup =>
                {
                    setup.AddHealthCheckEndpoint("Application health checks", "/health");
                    setup.SetEvaluationTimeInSeconds(30);
                    setup.SetApiMaxActiveRequests(1);
                    setup.SetMinimumSecondsBetweenFailureNotifications(120);
                }).AddInMemoryStorage();

            // MCP server (issue #298): off unless the configuration turns it on, so a
            // deployment opts into the endpoint deliberately.
            if (McpOptions.Enabled)
            {
                services.AddChronosMcpServer();
            }

			services.AddControllers();
			services.AddEndpointsApiExplorer();
			services.AddSwaggerGen();
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCookiePolicy();

            app.UseMiddleware<AuthenticationMiddleware>();
            app.UseMiddleware<UserProvisioningMiddleware>();
            app.UseMiddleware<LogEnrichmentMiddleware>();

			app.UseSwagger();
			app.UseSwaggerUI();

			app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                // Liveness: runs no checks at all, so it answers 200 whenever the process is
                // serving requests. /health above pings Jira and weighs the process memory, so
                // it turns 503 when a dependency is down - true, but useless for telling apart
                // "the deployment did not come back up" from "Jira is having a bad afternoon".
                // The deploy smoke check wants the former, and moves onto this endpoint once
                // a release carrying it has reached production.
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions()
                {
                    Predicate = _ => false
                });

                endpoints.MapHealthChecksUI(setup =>
                {
                    setup.AddCustomStylesheet("wwwroot/css/dotnet.css");
                });
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();

                if (McpOptions.Enabled)
                {
                    // The policy is what makes the endpoint answer 401 to a request without
                    // a personal access token, instead of running a scenario as nobody.
                    endpoints
                        .MapMcp(McpOptions.Path)
                        .RequireAuthorization(PersonalAccessTokenDefaults.AuthorizationPolicy);
                }
			});

			using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
			{
				scope.ServiceProvider.GetService<ApplicationDbContext>().Database.Migrate();
			}
		}
    }
}
