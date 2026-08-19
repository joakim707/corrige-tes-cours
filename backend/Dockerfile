# Contexte de build attendu : la racine du repo (railway.json pointe dockerfilePath sur backend/Dockerfile
# sans changer le "Root Directory" du service, donc tous les chemins sont préfixés par backend/).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/CorrigeTesCours.sln ./
COPY backend/src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj src/CorrigeTesCours.Api/
COPY backend/src/CorrigeTesCours.Domain/CorrigeTesCours.Domain.csproj src/CorrigeTesCours.Domain/
COPY backend/src/CorrigeTesCours.Infrastructure/CorrigeTesCours.Infrastructure.csproj src/CorrigeTesCours.Infrastructure/
RUN dotnet restore CorrigeTesCours.sln

COPY backend/ .
RUN dotnet publish src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway/Render injectent PORT dynamiquement à chaque déploiement.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet CorrigeTesCours.Api.dll"]
