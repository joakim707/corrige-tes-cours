# Corrige tes cours

Application web éducative propulsée par l'IA : correction de devoirs, fiches de révision, quiz et organisation.

- **Frontend** — React 18 + TypeScript (Vite), `frontend/`
- **Backend** — ASP.NET Core 8 (C#), `backend/`
- **BDD** — PostgreSQL via Entity Framework Core, hébergée sur [Neon](https://neon.tech) en production

## Prérequis

- .NET SDK 8
- Node.js 20+
- PostgreSQL 14+ en local

## Démarrage

### 1. Base de données

Créer la base puis renseigner la chaîne de connexion en secret local (jamais dans `appsettings.json`) :

```bash
createdb corrige_tes_cours

cd backend/src/CorrigeTesCours.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=corrige_tes_cours;Username=postgres;Password=VOTRE_MDP"
```

Le secret `Jwt:Secret` (≥ 32 caractères) est déjà généré localement. Pour le régénérer :

```bash
dotnet user-secrets set "Jwt:Secret" "<chaîne aléatoire de 48+ caractères>"
```

Appliquer les migrations :

```bash
cd backend
dotnet ef database update \
  --project src/CorrigeTesCours.Infrastructure \
  --startup-project src/CorrigeTesCours.Api
```

### 1bis. Hébergement Neon (staging / production)

1. Créer un projet sur [console.neon.tech](https://console.neon.tech) (branche `main` = prod, une branche par environnement si besoin).
2. Copier la **connection string** "pooled" fournie par Neon — elle inclut déjà `sslmode=require`.
3. Convertir l'URI Neon (`postgresql://user:pass@ep-xxx.neon.tech/dbname?sslmode=require`) au format Npgsql attendu par ce projet :

   ```
   Host=ep-xxx.neon.tech;Port=5432;Database=dbname;Username=user;Password=pass;SSL Mode=Require;Trust Server Certificate=true;Channel Binding=Require
   ```

4. Ne jamais mettre cette chaîne dans `appsettings.json`. En local, `dotnet user-secrets` ; en déploiement, variable d'environnement :

   ```bash
   # local
   dotnet user-secrets set "ConnectionStrings:Postgres" "Host=ep-xxx.neon.tech;...;SSL Mode=Require;Trust Server Certificate=true;Channel Binding=Require"

   # hébergeur (Railway/Render/Azure/etc.) — variable d'environnement équivalente
   ConnectionStrings__Postgres=Host=ep-xxx.neon.tech;...
   ```

5. Appliquer les migrations sur la base Neon en pointant temporairement dessus :

   ```bash
   cd backend
   dotnet ef database update \
     --project src/CorrigeTesCours.Infrastructure \
     --startup-project src/CorrigeTesCours.Api \
     --connection "Host=ep-xxx.neon.tech;...;SSL Mode=Require;Trust Server Certificate=true;Channel Binding=Require"
   ```

Neon met les connexions inactives en veille (cold start ~1s) sur le tier gratuit : pas d'impact fonctionnel, juste une latence sur la première requête après une pause.

### 2. Backend

```bash
cd backend/src/CorrigeTesCours.Api
dotnet run --launch-profile https
```

API sur `https://localhost:7148`, Swagger sur `/swagger`.

### 3. Frontend

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

App sur `http://localhost:5173`.

## Architecture backend

| Projet | Rôle |
|---|---|
| `CorrigeTesCours.Domain` | Entités métier (User, Matiere, Correction, Fiche, Quiz, QuizResult, RefreshToken) |
| `CorrigeTesCours.Infrastructure` | `AppDbContext`, configurations EF, migrations, hachage BCrypt |
| `CorrigeTesCours.Api` | Contrôleurs REST, DTOs, services d'authentification, configuration JWT/CORS |

## Authentification

- Access token JWT de 15 min, gardé **en mémoire** côté React (`AuthContext`), jamais en `localStorage`.
- Refresh token de 7 jours dans un cookie **HttpOnly** scopé sur `/api/auth`, avec rotation à chaque usage.
- Seul le hash SHA-256 du refresh token est persisté en base.
- Rate limiting de 10 requêtes/minute par IP sur `/api/auth/*`.

## Endpoints principaux

Auth (`/api/auth/*`), `users/me`, `matieres`, `corrections`, `fiches` (+ export PDF), `quiz` (+ submit/results),
`documents/extract-text` (import PDF/DOCX/PPTX/MD), `dashboard/stats`.

## Déploiement

### Backend — Railway

1. Sur [railway.app](https://railway.app), **New Project → Deploy from GitHub repo** et sélectionne ce dépôt.
2. Rien à toucher côté Root Directory : le `Dockerfile` est à la racine du repo (détecté automatiquement par Railway) et [railway.json](../railway.json) force explicitement le builder Dockerfile en config-as-code, pour ne pas dépendre des réglages UI.
3. Variables d'environnement à définir sur le service (Settings → Variables) :

   | Variable | Valeur |
   |---|---|
   | `ConnectionStrings__Postgres` | La connection string Neon (format Npgsql, voir section 1bis ci-dessus) |
   | `Jwt__Secret` | Chaîne aléatoire ≥ 32 caractères (différente de celle de dev) |
   | `Jwt__Issuer` / `Jwt__Audience` | Peuvent rester les valeurs par défaut |
   | `Ai__ApiKey` | Clé OpenRouter |
   | `Cors__AllowedOrigins__0` | URL du frontend déployé (ex. `https://corrige-tes-cours.vercel.app`) |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |

4. Railway détecte le `Dockerfile` et build automatiquement. Le conteneur lit `$PORT` fourni par Railway (voir `ENTRYPOINT` du Dockerfile) — rien à configurer côté port.
5. Les migrations EF sont appliquées **automatiquement au démarrage** (`Database.MigrateAsync()` dans `Program.cs`) : pas besoin d'accès shell sur Railway.
6. Chaque `git push` sur `main` redéploie automatiquement (intégration GitHub native de Railway — pas besoin de l'ajouter dans la CI GitHub Actions).

### Frontend — Vercel (ou Netlify)

1. Importer le dépôt sur [vercel.com](https://vercel.com), **Root Directory** → `frontend`, framework auto-détecté (Vite).
2. Variable d'environnement : `VITE_API_URL` = l'URL Railway du backend (ex. `https://corrige-tes-cours-api.up.railway.app`).
3. Une fois l'URL Vercel connue, revenir sur Railway et mettre à jour `Cors__AllowedOrigins__0` avec cette URL exacte (sinon le navigateur bloquera les requêtes en CORS).

### CI

[.github/workflows/ci.yml](.github/workflows/ci.yml) build le backend et le frontend à chaque push/PR sur `main` — c'est une vérification, pas un déploiement (Railway/Vercel s'en chargent nativement via leur intégration GitHub).

## Suite de la roadmap

Sprint 6 — stats dashboard avancées, tests E2E, polish UX.
