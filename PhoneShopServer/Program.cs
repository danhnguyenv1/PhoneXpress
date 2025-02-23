using Microsoft.EntityFrameworkCore;
using PhoneXpressServer.Data;
using PhoneXpressServer.Repositories;
using PhoneXpressServer.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Starting 
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string not found"));
});
builder.Services.AddScoped<IProduct, ProductServices>();
builder.Services.AddScoped<ICategory, CategoryService>();
builder.Services.AddScoped<IUserAccount, UserAccountService>();

//Ending ... 


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}
app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapRazorPages();
app.UseAuthorization();
app.MapFallbackToFile("index.html");
app.MapControllers();

app.Run();
