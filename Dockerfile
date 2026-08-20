# Dockerfile à la racine du repo : Railway (et la plupart des PaaS) le détectent
# automatiquement, sans dépendre d'un réglage "Root Directory" dans leur UI.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore juste le projet Api (et ses dépendances Domain/Infrastructure via project reference) :
# pas besoin du .sln ni du projet de tests dans l'image runtime, les tests tournent en CI à part.
COPY backend/src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj src/CorrigeTesCours.Api/
COPY backend/src/CorrigeTesCours.Domain/CorrigeTesCours.Domain.csproj src/CorrigeTesCours.Domain/
COPY backend/src/CorrigeTesCours.Infrastructure/CorrigeTesCours.Infrastructure.csproj src/CorrigeTesCours.Infrastructure/
RUN dotnet restore src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj

COPY backend/ .
RUN dotnet publish src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render injecte PORT dynamiquement à chaque déploiement. reloadConfigOnChange=false désactive
# le FileSystemWatcher sur appsettings.json : les conteneurs Render ont un ulimit de descripteurs
# de fichiers trop bas pour ça (crash au démarrage sinon), et on n'a de toute façon pas besoin de
# hot-reload de config en prod (les secrets viennent de variables d'environnement).
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} DOTNET_hostBuilder__reloadConfigOnChange=false dotnet CorrigeTesCours.Api.dll"]
