using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PhoneXpressClient;
using PhoneXpressClient.Authentication;
using PhoneXpressClient.Services;
using Syncfusion.Blazor;

//Register Syncfusion license
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzcyMzU5N0AzMjM4MmUzMDJlMzBaR3NDVWp5dHB4NldjenFtVnJFdHlsSkFWaTNsdmxnTGZvanl3c2hTTFlJPQ==");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddSyncfusionBlazor();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IProductService, ClientServices>();
builder.Services.AddScoped<ICategoryService, ClientServices>();

builder.Services.AddScoped<MessageDialogSerrvice>();
builder.Services.AddScoped<AuthenticationService>();
await builder.Build().RunAsync();

