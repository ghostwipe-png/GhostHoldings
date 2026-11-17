# =========================================================
# 1. BUILD STAGE
# =========================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
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

# Install EF Core tools for migrations
RUN dotnet tool install --global dotnet-ef --version 10.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy the Infrastructure project (needed for migrations)
COPY --from=build /src/GhostHoldings.Infrastructure ./GhostHoldings.Infrastructure
COPY --from=build /src/GhostHoldings.Api ./GhostHoldings.Api
COPY --from=build /src/GhostHoldings.Domain ./GhostHoldings.Domain
COPY --from=build /src/GhostHoldings.Application ./GhostHoldings.Application

# SECURITY: Create a non-root user and switch to it
RUN useradd -m appuser && chown -R appuser /app
USER appuser

# Expose the port Render will use
EXPOSE 8080

# Tell ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# ENTRYPOINT: Run migrations first, THEN start the API
ENTRYPOINT ["sh", "-c", "dotnet ef database update --project ./GhostHoldings.Infrastructure --startup-project ./GhostHoldings.Api --no-build && dotnet ./GhostHoldings.Api.dll"]