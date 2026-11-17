using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Services;
using GhostHoldings.Infrastructure.Data;
using GhostHoldings.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers
builder.Services.AddControllers();

// 2. Add Database Context (PostgreSQL)
builder.Services.AddDbContext<IApplicationDbContext, GhostHoldingsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Add MediatR (Scans the Application assembly for all Handlers)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GhostHoldings.Application.Features.Users.RegisterUserCommand).Assembly));

// 4. Add Application Services
builder.Services.AddScoped<IFeeCalculationService, FeeCalculationService>();

// 5. Add Infrastructure Services (Paystack HttpClient)
builder.Services.AddHttpClient<IPaystackService, PaystackService>();

// 6. Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing in configuration"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// ==========================================
// AUTO-MIGRATE DATABASE ON STARTUP
// ==========================================
using (var scope = app.Services.CreateScope())
{
    // We use the concrete DbContext here to guarantee access to .Database.Migrate()
    var dbContext = scope.ServiceProvider.GetRequiredService<GhostHoldingsDbContext>();
    dbContext.Database.Migrate(); 
}
// ==========================================

// Configure the HTTP request pipeline.
// (Swagger removed to avoid NuGet conflicts and keep production build lean)

app.UseAuthentication(); // MUST come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();