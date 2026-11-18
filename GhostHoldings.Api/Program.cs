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

// 2. ADD CORS POLICY (Crucial for Flutter Web)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterWeb", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:5001",
                "http://localhost:8080",
                "http://127.0.0.1:5000",
                "http://127.0.0.1:8080",
                "https://ghostholdings.onrender.com" // Allow the live app to talk to itself if needed
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for sending cookies/auth headers
    });
});

// 3. Add Database Context (PostgreSQL)
builder.Services.AddDbContext<IApplicationDbContext, GhostHoldingsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GhostHoldings.Application.Features.Users.RegisterUserCommand).Assembly));

// 5. Add Application Services
builder.Services.AddScoped<IFeeCalculationService, FeeCalculationService>();

// 6. Add Infrastructure Services (Paystack HttpClient)
builder.Services.AddHttpClient<IPaystackService, PaystackService>();

// 7. Add JWT Authentication
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
    var dbContext = scope.ServiceProvider.GetRequiredService<GhostHoldingsDbContext>();
    dbContext.Database.Migrate(); 
}
// ==========================================

// 8. USE CORS (MUST BE BEFORE UseAuthentication and UseAuthorization)
app.UseCors("AllowFlutterWeb");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();