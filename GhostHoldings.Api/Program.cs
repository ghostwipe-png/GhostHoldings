using Microsoft.AspNetCore.Identity;
using GhostHoldings.Domain.Entities; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Services;
using GhostHoldings.Infrastructure.Data;
using GhostHoldings.Infrastructure.Services;

// 1. Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting GhostHoldings API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // ✅ CRITICAL 1: Controllers MUST be added
    builder.Services.AddControllers();

    // ✅ CRITICAL 2: CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.SetIsOriginAllowed(origin => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // ✅ CRITICAL 3: Database
    builder.Services.AddDbContext<IApplicationDbContext, GhostHoldingsDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ✅ CRITICAL 4: Distributed Cache (🔥 FIXED: Ignore "localhost" and use Memory Cache)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString) && !redisConnectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "GhostHoldings_";
        });
        Log.Information("Using Redis Cache: {Host}", redisConnectionString.Split(',')[0]);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Using In-Memory Cache (Redis not configured or is localhost)");
    }

    // ✅ CRITICAL 5: MediatR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GhostHoldings.Application.Features.Users.RegisterUserCommand).Assembly));

    // ✅ CRITICAL 6: Custom Services
    builder.Services.AddScoped<IFeeCalculationService, FeeCalculationService>();
    builder.Services.AddHttpClient<IPaystackService, PaystackService>();
    builder.Services.AddHttpClient<IEmailService, ResendEmailService>();
    builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

    // ✅ CRITICAL 7: JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

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

    // ✅ CRITICAL 8: Authorization
    builder.Services.AddAuthorization();

    var app = builder.Build();

    // Auto-migrate database
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GhostHoldingsDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("GhostHoldings API started successfully on {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}