using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SoftwareVSM.Client;
using SoftwareVSM.Client.GameEngine;
using SoftwareVSM.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<SimulationEngine>();
builder.Services.AddSingleton<CanvasDragService>();
builder.Services.AddScoped<SaveService>();
builder.Services.AddScoped<DragDropService>();

await builder.Build().RunAsync();
