using DigitalBattleMapServer;
using Microsoft.AspNetCore.SignalR;
using WebServer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<WebHub>("/WebHub");

var hubContext = app.Services.GetService(typeof(IHubContext<WebHub>));
var connectionController = ConnectionController.GetInstance();
connectionController.Initialize(hubContext as IHubContext<WebHub>);

app.Run();
connectionController.Terminate();