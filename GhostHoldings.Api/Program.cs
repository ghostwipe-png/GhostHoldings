using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GhostHoldings.Application.Interfaces;
using GhostHoldings.Application.Services;
using GhostHoldings.Infrastructure.Data;
using GhostHoldings.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
    // This automatically applies all pending EF Core migrations to the Render database
    dbContext.Database.Migrate(); 
}
// ==========================================

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Note: Commented out for Render.com compatibility. 
// Render handles HTTPS at the load balancer level, and this can cause redirect loops on the free tier.
// app.UseHttpsRedirection(); 

app.UseAuthentication(); // MUST come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();