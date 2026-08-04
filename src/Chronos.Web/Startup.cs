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
            services.AddTransient<IIdentityService, IdentityService>();
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
                endpoints.MapHealthChecksUI(setup =>
                {
                    setup.AddCustomStylesheet("wwwroot/css/dotnet.css");
                });
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
			});

			using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
			{
				scope.ServiceProvider.GetService<ApplicationDbContext>().Database.Migrate();
			}
		}
    }
}
