# Build context = REPO KOKU (docker-compose.yml'de context: . olarak ayarli).
# Sebep: BuildingBlocks paylasilan proje services/shared altinda, servis klasorunun
# disinda — Docker build context'i servis klasoruyle sinirli olsaydi gorunmezdi.

# ---- Build asamasi ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once csproj'lar (restore layer cache)
COPY services/shared/BuildingBlocks/BuildingBlocks.csproj services/shared/BuildingBlocks/
COPY services/identity/src/Identity.Domain/Identity.Domain.csproj services/identity/src/Identity.Domain/
COPY services/identity/src/Identity.Application/Identity.Application.csproj services/identity/src/Identity.Application/
COPY services/identity/src/Identity.Infrastructure/Identity.Infrastructure.csproj services/identity/src/Identity.Infrastructure/
COPY services/identity/src/Identity.Api/Identity.Api.csproj services/identity/src/Identity.Api/
RUN dotnet restore services/identity/src/Identity.Api/Identity.Api.csproj

COPY services/shared/BuildingBlocks/ services/shared/BuildingBlocks/
COPY services/identity/src/ services/identity/src/
RUN dotnet publish services/identity/src/Identity.Api/Identity.Api.csproj -c Release --no-restore -o /app/publish

# ---- Runtime asamasi ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
