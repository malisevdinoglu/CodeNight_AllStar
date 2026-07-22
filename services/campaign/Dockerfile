# ---- Build asamasi ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Once csproj'lar (restore layer cache)
COPY src/Campaign.Domain/Campaign.Domain.csproj src/Campaign.Domain/
COPY src/Campaign.Application/Campaign.Application.csproj src/Campaign.Application/
COPY src/Campaign.Infrastructure/Campaign.Infrastructure.csproj src/Campaign.Infrastructure/
COPY src/Campaign.Api/Campaign.Api.csproj src/Campaign.Api/
RUN dotnet restore src/Campaign.Api/Campaign.Api.csproj

COPY src/ src/
RUN dotnet publish src/Campaign.Api/Campaign.Api.csproj -c Release --no-restore -o /app/publish

# ---- Runtime asamasi ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Campaign.Api.dll"]
