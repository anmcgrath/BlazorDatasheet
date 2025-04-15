using BlazorDatasheet.Extensions;
using BlazorDatasheet.SharedPages;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorDatasheet.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBlazorDatasheet();
builder.RootComponents.Add<AppWasm>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();