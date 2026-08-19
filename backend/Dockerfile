# Contexte de build attendu : backend/ (racine de la solution)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CorrigeTesCours.sln ./
COPY src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj src/CorrigeTesCours.Api/
COPY src/CorrigeTesCours.Domain/CorrigeTesCours.Domain.csproj src/CorrigeTesCours.Domain/
COPY src/CorrigeTesCours.Infrastructure/CorrigeTesCours.Infrastructure.csproj src/CorrigeTesCours.Infrastructure/
RUN dotnet restore CorrigeTesCours.sln

COPY . .
RUN dotnet publish src/CorrigeTesCours.Api/CorrigeTesCours.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway/Render injectent PORT dynamiquement à chaque déploiement.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet CorrigeTesCours.Api.dll"]
