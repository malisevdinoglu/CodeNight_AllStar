# ---- Build asamasi ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once csproj'lar (restore layer cache)
COPY src/Gamification.Domain/Gamification.Domain.csproj src/Gamification.Domain/
COPY src/Gamification.Application/Gamification.Application.csproj src/Gamification.Application/
COPY src/Gamification.Infrastructure/Gamification.Infrastructure.csproj src/Gamification.Infrastructure/
COPY src/Gamification.Api/Gamification.Api.csproj src/Gamification.Api/
RUN dotnet restore src/Gamification.Api/Gamification.Api.csproj

COPY src/ src/
RUN dotnet publish src/Gamification.Api/Gamification.Api.csproj -c Release --no-restore -o /app/publish

# ---- Runtime asamasi ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gamification.Api.dll"]
