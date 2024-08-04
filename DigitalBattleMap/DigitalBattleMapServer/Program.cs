using DigitalBattleMapServer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add all settings file to the configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

Startup startup = new(builder.Configuration);
startup.ConfigureServices(builder.Services);

WebApplication app = builder.Build();
startup.Configure(app, app.Environment);
app.Run();