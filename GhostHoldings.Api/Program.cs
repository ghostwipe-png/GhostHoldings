using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Services;
using GhostHoldings.Infrastructure.Data;
using GhostHoldings.Infrastructure.Services;

// 1. CONFIGURE SERILOG EARLY (catches startup errors)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting GhostHoldings API...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logger with Serilog
    builder.Host.UseSerilog();

    // 2. ADD CORS
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

    // 3. ADD DATABASE
    builder.Services.AddDbContext<IApplicationDbContext, GhostHoldingsDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // 4. ADD REDIS DISTRIBUTED CACHE
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "GhostHoldings_";
    });

    // 5. ADD MEDIATR
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GhostHoldings.Application.Features.Users.RegisterUserCommand).Assembly));

    // 6. ADD SERVICES
    builder.Services.AddScoped<IFeeCalculationService, FeeCalculationService>();
    builder.Services.AddHttpClient<IPaystackService, PaystackService>();

    // 7. ADD JWT AUTH
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

    // 🔥 CRITICAL FIX: ADD AUTHORIZATION SERVICES 🔥
    builder.Services.AddAuthorization();

    var app = builder.Build();

    // AUTO-MIGRATE DATABASE
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GhostHoldingsDbContext>();
        dbContext.Database.Migrate();
    }

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("GhostHoldings API started successfully on {Port}", app.Urls);
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