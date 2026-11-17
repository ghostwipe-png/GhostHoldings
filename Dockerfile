# =========================================================
# 1. BUILD STAGE
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies (optimizes Docker cache)
COPY ["GhostHoldings.Api/GhostHoldings.Api.csproj", "GhostHoldings.Api/"]
COPY ["GhostHoldings.Application/GhostHoldings.Application.csproj", "GhostHoldings.Application/"]
COPY ["GhostHoldings.Domain/GhostHoldings.Domain.csproj", "GhostHoldings.Domain/"]
COPY ["GhostHoldings.Infrastructure/GhostHoldings.Infrastructure.csproj", "GhostHoldings.Infrastructure/"]
RUN dotnet restore "GhostHoldings.Api/GhostHoldings.Api.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/GhostHoldings.Api"
RUN dotnet build "GhostHoldings.Api.csproj" -c Release -o /app/build

# =========================================================
# 2. PUBLISH STAGE
# =========================================================
FROM build AS publish
RUN dotnet publish "GhostHoldings.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================================================
# 3. FINAL RUNTIME STAGE
# =========================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# SECURITY: Create a non-root user and switch to it
RUN useradd -m appuser && chown -R appuser /app
USER appuser

# Expose the port Render will use
EXPOSE 8080

# Tell ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "GhostHoldings.Api.dll"]