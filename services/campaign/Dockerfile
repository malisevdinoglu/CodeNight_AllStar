# Build context = REPO KOKU (docker-compose.yml'de context: . olarak ayarli).
# Sebep: BuildingBlocks paylasilan proje services/shared altinda, servis klasorunun
# disinda — Docker build context'i servis klasoruyle sinirli olsaydi gorunmezdi.

# ---- Build asamasi ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once csproj'lar (restore layer cache)
COPY services/shared/BuildingBlocks/BuildingBlocks.csproj services/shared/BuildingBlocks/
COPY services/campaign/src/Campaign.Domain/Campaign.Domain.csproj services/campaign/src/Campaign.Domain/
COPY services/campaign/src/Campaign.Application/Campaign.Application.csproj services/campaign/src/Campaign.Application/
COPY services/campaign/src/Campaign.Infrastructure/Campaign.Infrastructure.csproj services/campaign/src/Campaign.Infrastructure/
COPY services/campaign/src/Campaign.Api/Campaign.Api.csproj services/campaign/src/Campaign.Api/
RUN dotnet restore services/campaign/src/Campaign.Api/Campaign.Api.csproj

COPY services/shared/BuildingBlocks/ services/shared/BuildingBlocks/
COPY services/campaign/src/ services/campaign/src/
RUN dotnet publish services/campaign/src/Campaign.Api/Campaign.Api.csproj -c Release --no-restore -o /app/publish

# ---- Runtime asamasi ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Campaign.Api.dll"]
