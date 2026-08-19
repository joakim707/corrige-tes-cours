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

### Backend — Render (Blueprint, 100% gratuit, pas de CB)

Le fichier [render.yaml](render.yaml) décrit le service : Render le lit automatiquement au lieu de devoir tout cliquer à la main dans un dashboard.

1. Sur [dashboard.render.com](https://dashboard.render.com), **New → Blueprint**, connecte le repo GitHub `corrige-tes-cours`.
2. Render détecte `render.yaml` et propose de créer le service `corrige-tes-cours-api` (runtime Docker, plan free). Valide.
3. Un formulaire demande les variables marquées `sync: false` dans `render.yaml` — à saisir une seule fois à la création :

   | Variable | Valeur |
   |---|---|
   | `ConnectionStrings__Postgres` | La connection string Neon (format Npgsql, voir section 1bis ci-dessus) |
   | `Jwt__Secret` | Chaîne aléatoire ≥ 32 caractères (différente de celle de dev) |
   | `Ai__ApiKey` | Clé OpenRouter |
   | `Cors__AllowedOrigins__0` | URL du frontend déployé (ex. `https://corrige-tes-cours.vercel.app`) — peut être mise à jour après coup une fois connue |

4. Render build l'image depuis le `Dockerfile` à la racine et lit `$PORT` dynamiquement (voir `ENTRYPOINT` du Dockerfile) — rien à configurer côté port.
5. Les migrations EF sont appliquées **automatiquement au démarrage** (`Database.MigrateAsync()` dans `Program.cs`) : pas besoin d'accès shell.
6. Chaque `git push` sur `main` redéploie automatiquement.

⚠️ **Limite du plan free Render** : le service se met en veille après 15 min d'inactivité. La requête suivante réveille le conteneur en ~30-50s (cold start) avant de répondre normalement — normal, pas un bug.

### Frontend — Vercel (ou Netlify)

1. Importer le dépôt sur [vercel.com](https://vercel.com), **Root Directory** → `frontend`, framework auto-détecté (Vite).
2. Variable d'environnement : `VITE_API_URL` = l'URL Render du backend (ex. `https://corrige-tes-cours-api.onrender.com`).
3. Une fois l'URL Vercel connue, revenir sur Render (service → Environment) et mettre à jour `Cors__AllowedOrigins__0` avec cette URL exacte (sinon le navigateur bloquera les requêtes en CORS).

## Tests

```bash
# Backend (xUnit — logique métier pure : correction de quiz, hachage mot de passe, signature de fichiers)
cd backend
dotnet test

# Frontend (Vitest + ESLint)
cd frontend
npm run lint
npm run test
```

## CI/CD

[.github/workflows/ci.yml](.github/workflows/ci.yml) s'exécute à chaque push/PR sur `main` :

1. **Job `backend`** — restore, build, `dotnet test` (résultats publiés en artifact).
2. **Job `frontend`** — `npm ci`, lint (ESLint), tests (Vitest), build (`tsc` + Vite).
3. **Job `deploy`** — ne se déclenche que sur un push direct sur `main`, et seulement si `backend` et `frontend` ont réussi. Il déclenche les *Deploy Hooks* Render/Vercel via `curl` (URLs stockées en secrets GitHub `RENDER_DEPLOY_HOOK_URL` / `VERCEL_DEPLOY_HOOK_URL`).

**Pour que le déploiement soit réellement conditionné aux tests** (et pas juste déclenché en parallèle par l'auto-deploy natif de Render/Vercel sur push GitHub) :

1. Render → service → Settings → **Auto-Deploy** → désactiver. Puis Settings → **Deploy Hook** → copier l'URL.
2. Vercel → projet → Settings → Git → désactiver le déploiement automatique sur push (ou limiter aux preview). Puis Settings → Git → **Deploy Hooks** → en créer un pour `main`.
3. GitHub repo → Settings → Secrets and variables → Actions → ajouter `RENDER_DEPLOY_HOOK_URL` et `VERCEL_DEPLOY_HOOK_URL` avec ces URLs.

Sans cette étape, Render/Vercel redéploient quand même automatiquement sur chaque push (comportement par défaut), en parallèle de la CI plutôt qu'après elle — la CI reste alors une vérification de qualité mais ne bloque pas techniquement un déploiement cassé. Pour un gate strict au niveau GitHub (bloquer le merge d'une PR si la CI échoue), ajouter une **branch protection rule** sur `main` (Settings → Branches → Require status checks to pass) exigeant les jobs `backend` et `frontend`.

## Sécurité

- **Secrets** — clé OpenRouter, secret JWT, connection string Postgres : jamais commités, jamais en dur dans le code. En local via `dotnet user-secrets` ; en production via les variables d'environnement Render/Vercel. `.gitignore` exclut `.env*` et `appsettings.Local.json`.
- **Mots de passe** — hashés avec BCrypt (work factor 12), jamais stockés ni loggés en clair.
- **CORS** — origines explicitement whitelistées via `Cors:AllowedOrigins` (pas de wildcard `*`), avec `AllowCredentials` pour le cookie de refresh token.
- **HTTPS** — forcé de bout en bout : `UseHttpsRedirection()` + `UseHsts()` en production côté API ; Vercel sert le frontend exclusivement en HTTPS (SSL auto, redirection HTTP→HTTPS native). Le backend est derrière le proxy TLS de Render — `ForwardedHeaders` restaure le schéma HTTPS réel dans `Request.Scheme` pour que cookies `Secure` et HSTS fonctionnent correctement.
- **Headers de sécurité** — `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` restrictive, appliqués à toutes les réponses (voir middleware dans `Program.cs`).
- **Rate limiting** — 10 req/min/IP sur `/api/auth/*` (anti brute-force).
- **Erreurs** — page de diagnostic détaillée uniquement en `Development` ; en production, réponse générique sans stack trace (l'exception complète est loggée côté serveur, jamais exposée au client).
- **Uploads** — extension whitelistée + vérification de la signature binaire réelle du fichier (magic bytes), taille max 15 Mo.
- **Sessions** — JWT 15 min, refresh token 7 jours en cookie `HttpOnly` + `Secure` + rotation à chaque usage ; seul le hash SHA-256 du refresh token est persisté.

## Observabilité

- **Logging structuré** — Serilog en sortie JSON sur stdout (capturé nativement par le viewer de logs Render). Chaque requête HTTP génère une ligne structurée (méthode, route, status, durée, trace ID) via `UseSerilogRequestLogging()`. Le bruit des logs EF Core/ASP.NET internes est filtré à `Warning` pour ne garder que le pertinent.
- **Suivi d'erreurs** — intégration Sentry prête à l'emploi (`Sentry.AspNetCore`), activée uniquement si `Sentry:Dsn` est configuré (sinon no-op silencieux, aucun impact). Pour l'activer : créer un projet sur [sentry.io](https://sentry.io) et définir la variable d'environnement `Sentry__Dsn` sur Render.

## Suite de la roadmap

Sprint 6 — stats dashboard avancées, tests E2E, polish UX.
