# Build context = REPO KOKU (docker-compose.yml'de context: . olarak ayarli).
# Sebep: BuildingBlocks paylasilan proje services/shared altinda, servis klasorunun
# disinda — Docker build context'i servis klasoruyle sinirli olsaydi gorunmezdi.

# ---- Build asamasi ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once csproj'lar (restore layer cache)
COPY services/shared/BuildingBlocks/BuildingBlocks.csproj services/shared/BuildingBlocks/
COPY services/gamification/src/Gamification.Domain/Gamification.Domain.csproj services/gamification/src/Gamification.Domain/
COPY services/gamification/src/Gamification.Application/Gamification.Application.csproj services/gamification/src/Gamification.Application/
COPY services/gamification/src/Gamification.Infrastructure/Gamification.Infrastructure.csproj services/gamification/src/Gamification.Infrastructure/
COPY services/gamification/src/Gamification.Api/Gamification.Api.csproj services/gamification/src/Gamification.Api/
RUN dotnet restore services/gamification/src/Gamification.Api/Gamification.Api.csproj

COPY services/shared/BuildingBlocks/ services/shared/BuildingBlocks/
COPY services/gamification/src/ services/gamification/src/
RUN dotnet publish services/gamification/src/Gamification.Api/Gamification.Api.csproj -c Release --no-restore -o /app/publish

# ---- Runtime asamasi ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gamification.Api.dll"]
