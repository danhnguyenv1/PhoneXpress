using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhoneXpressServer.Data;
using PhoneXpressServer.Repositories;
using PhoneXpressServer.Services;


var builder = WebApplication.CreateBuilder(args);

//Add MVC and API Controllers (optional if only using API)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddOpenApi();

//Add Swagger for API testing
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
builder.Services.AddScoped<IPayment, PaymentService>();

//Add Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"];
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
//Ending Authentication config ... 


var app = builder.Build();

//Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseWebAssemblyDebugging();
}
app.UseHttpsRedirection();

//Serve Blazor WebAssembly UI
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapRazorPages();

//Enable authentication & authorization middleware

app.UseAuthentication();
app.UseAuthorization();

//Map API controllers
app.MapControllers();

//Fallback: Redirect non-API requests to Blazor UI (index.html)
app.MapFallbackToFile("index.html");

app.Run();
