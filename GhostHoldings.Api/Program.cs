using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Services;
using GhostHoldings.Infrastructure.Data;
using GhostHoldings.Infrastructure.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting GhostHoldings API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // ✅ FIX #1: AddControllers was missing in the previous version
    builder.Services.AddControllers();

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

    builder.Services.AddDbContext<IApplicationDbContext, GhostHoldingsDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ✅ FIX #2: Redis is OPTIONAL. Falls back to in-memory cache when not configured (e.g. Render free tier)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "GhostHoldings_";
        });
        Log.Information("Distributed cache: Redis ({Host})", redisConnectionString.Split(',')[0]);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Information("Distributed cache: In-Memory (Redis not configured)");
    }

    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GhostHoldings.Application.Features.Users.RegisterUserCommand).Assembly));

    builder.Services.AddScoped<IFeeCalculationService, FeeCalculationService>();
    builder.Services.AddHttpClient<IPaystackService, PaystackService>();

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

    // ✅ The AddAuthorization line you already added stays here
    builder.Services.AddAuthorization();

    var app = builder.Build();

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