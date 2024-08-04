using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Handlers;
using DigitalBattleMapServer.Hubs;
using DigitalBattleMapServer.Utility;

namespace DigitalBattleMapServer;
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSession();

        services
            .AddMvc(options => { options.EnableEndpointRouting = true; })
            .AddDataAnnotationsLocalization()
            .AddRazorRuntimeCompilation();

        services.AddMemoryCache();
        services.AddSignalR();

        services.AddHttpContextAccessor();
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

        // Session and state
        services.AddScoped<ICookieHandler, CookieHandler>();
        services.AddScoped<IState<Settings>, CookieState<Settings>>();

        // Singletons
        services.AddSingleton<IMemoryCacheHandler, MemoryCacheHandler>();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            application.UseDeveloperExceptionPage();
        }

        application.UseStaticFiles();
        application.UseSession();
        application.UseRouting();

        application.UseEndpoints(endpoints =>
        {
            // Route users by default to the Home controller Index page
            endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}");
            endpoints.MapHub<WebHub>("/WebHub");
            endpoints.MapHub<MapHub>("/MapHub");
        });
    }
}
