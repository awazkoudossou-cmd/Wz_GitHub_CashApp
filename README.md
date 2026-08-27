# CashApp — Webapp de gestion de caisse

Application desktop-orientée pour gérer ouvertures/fermetures de caisse, opérations d'entrée/sortie, journal des opérations, dashboards et sauvegardes locales. Pensée **V1 MVP + V2 préparée** dès l'architecture.

---

## Stack

| Couche | Techno |
|---|---|
| Backend | ASP.NET Core 8 Web API, Entity Framework Core 8, SQLite, JWT, FluentValidation |
| Exports | ClosedXML (Excel), QuestPDF (PDF, license Community) |
| Frontend | React 18, TypeScript, Vite, MUI 5, TanStack Query 5, React Hook Form + Zod, Axios, Zustand, dayjs |
| Stockage | SQLite local (`cashapp.db`) — sauvegardes physiques `.db` versionnées dans `./backups` |

---

## Architecture

```
new_apps/
├─ backend/                     # solution .NET (Clean / 4 layers)
│  ├─ src/
│  │  ├─ CashApp.Api             ASP.NET Core, controllers, middleware, auth
│  │  ├─ CashApp.Application     DTO, interfaces, services métier, validators
│  │  ├─ CashApp.Domain          Entités, enums, constantes
│  │  ├─ CashApp.Infrastructure  EF Core, DbContext, seed, JWT, backup, exports
│  │  └─ CashApp.Shared          Helpers
│  └─ tests/
│     ├─ CashApp.Application.Tests
│     └─ CashApp.Api.Tests
└─ frontend/cashapp-web/         # Vite + React + TS
   ├─ src/api                    Axios + 10 modules
   ├─ src/app/{providers,router,store}
   ├─ src/components/{common,layout,forms,dialogs,tables}
   ├─ src/modules/<domaine>/hooks.ts
   ├─ src/pages                  18 pages routées
   ├─ src/hooks
   ├─ src/types
   └─ src/utils
```

---

## Modèle V1 / V2 et modules activables

Deux dimensions de configuration, gérées en base via `app_settings` et `feature_settings` :

- **`APP_MODE`** : `ESSENTIAL` (V1) | `INTERMEDIATE` | `ADVANCED` (V2)
- **`FeatureCodes`** : un code par module activable côté admin

| Catégorie | Codes |
|---|---|
| V1 — Core (seedés activés) | `CORE_AUTH`, `CORE_USERS`, `CORE_CASH_REGISTERS`, `CORE_CASH_SESSIONS`, `CORE_OPERATIONS`, `CORE_CATEGORIES`, `CORE_DASHBOARD`, `CORE_EXPORTS`, `CORE_BACKUP`, `CORE_SETTINGS` |
| V2 — Advanced (seedés désactivés) | `ADV_VALIDATION`, `ADV_TRANSFERS`, `ADV_BANK_DEPOSITS`, `ADV_ATTACHMENTS`, `ADV_ADVANCED_REPORTS`, `ADV_IMPORTS`, `ADV_RECONCILIATION`, `ADV_VARIANCE_MANAGEMENT`, `ADV_ANOMALIES` |

Côté frontend :
- `useIsFeatureEnabled(code)` lit le store auth
- `<FeatureGuard feature="...">` masque visuellement
- `<ProtectedRoute feature="..." roles={[...]}>` bloque la navigation (redir `/403`)
- Le menu latéral disparaît automatiquement quand le module est désactivé

---

## Rôles

| Code | Description |
|---|---|
| `ADMIN` | Tout droit, configuration, sauvegardes |
| `SUPERVISOR` | Caisses, catégories, dashboard superviseur, clôture (selon setting) |
| `CASHIER` | Ouvre/ferme sa propre session, saisit ses opérations |

Compte seedé : **`admin / Admin@123`** (changer après le premier login).

---

## Prérequis

- **.NET 8 SDK** — https://dotnet.microsoft.com/download
- **Node.js 20+** + npm
- (optionnel) Outil EF Core : `dotnet tool install -g dotnet-ef`

---

## Installation backend

```bash
cd backend

# 1. Restauration + build
dotnet restore
dotnet build

# 2. Configuration de l'environnement
# Éditer src/CashApp.Api/appsettings.json :
#   - "Jwt:SigningKey" -> chaîne aléatoire >= 32 caractères
#   - "ConnectionStrings:Default" -> ajuster si besoin (défaut: cashapp.db dans le cwd)

# 3. Migration EF (une seule fois pour créer le schéma)
dotnet ef migrations add InitialCreate \
  --project src/CashApp.Infrastructure \
  --startup-project src/CashApp.Api

# 4. Lancement
dotnet run --project src/CashApp.Api
```

Au démarrage, l'app exécute `EnsureCreated` + `DbSeeder` (admin, app mode, features, catégories par défaut).

- API : `http://localhost:5080`
- Swagger : `http://localhost:5080/swagger`

Tests backend :
```bash
dotnet test
```

---

## Installation frontend

```bash
cd frontend/cashapp-web

cp .env.example .env
# .env :
#   VITE_API_BASE_URL=http://localhost:5080

npm install
npm run dev
```

Frontend : `http://localhost:5173` (Vite proxifie `/api` vers `:5080`).

---

## Exports

Implémentés dans `CashApp.Infrastructure/Services/ExportService.cs`.

| Endpoint | Format | Colonnes / contenu |
|---|---|---|
| `POST /api/exports/operations` | `xlsx` (ClosedXML) ou `pdf` (QuestPDF) | Référence, Date, Caisse, Direction, Catégorie, Montant, Devise, Moyen, Libellé, Tiers, Statut annulation |
| `POST /api/exports/sessions` | `xlsx` ou `pdf` | ID, Caisse, Ouverte par, Ouverte le, Solde ouv./théo./phy./écart, Statut |
| `POST /api/exports/cash-state` | `pdf` | État synthétique d'une session : ouverture, totaux IN/OUT, théorique, physique, écart, table des opérations |

Pourquoi ces libs :
- **ClosedXML** : API simple, sortie Excel native (pas un CSV déguisé), pas de dépendance Office
- **QuestPDF** : moderne, fluent API, licence Community gratuite (`QuestPDF.Settings.License = LicenseType.Community` est posé dans `ExportService`)

Formatage :
- Montants : `#,##0.00` (Excel) / `N2` (PDF)
- Dates : `yyyy-MM-dd` ou `yyyy-MM-dd HH:mm`
- Fichiers : `{module}_{from:yyyyMMdd}_{to:yyyyMMdd}.{ext}` ou `cash_state_{sessionId}.pdf`

Côté frontend : `useExportOperations / useExportSessions / useExportCashState` déclenchent un download blob via `apiClient` (header `content-disposition` exploité pour le nom de fichier).

---

## Backup / Restore SQLite

Implémenté dans `CashApp.Infrastructure/Services/BackupService.cs`.

| Étape | Stratégie |
|---|---|
| Création | `VACUUM INTO '<file>'` exécuté via EF Core — snapshot cohérent même si l'API tourne |
| Nommage | `cashapp_{yyyyMMdd_HHmmss}.db` |
| Stockage | Dossier configurable via setting `BACKUP_DIRECTORY` (défaut `./backups`) |
| Journalisation | Table `backup_logs` (file_name, file_path, created_by, created_at) |
| Restauration | Ferme la connexion EF → copie de sécurité du fichier actuel en `cashapp.db.pre-restore-<stamp>` → remplace le fichier physique |
| Sécurité | Endpoints `/api/backups/*` réservés au rôle `ADMIN` |

Settings liés :
- `AUTO_BACKUP_ENABLED` / `AUTO_BACKUP_TIME` — prévu pour un BackgroundService (à brancher en V2)
- `AUTO_BACKUP_ON_SESSION_CLOSE` — à câbler dans `CashSessionService.CloseAsync` quand activé

Vigilance :
- Toujours **redémarrer l'API** après restauration (le pool de connexions EF garde des handles)
- Toujours conserver la pré-restauration tant que la nouvelle base n'a pas été validée
- Tester périodiquement la procédure de restauration sur un environnement de dev

---

## Tests

Structure :
```
backend/tests/
├─ CashApp.Application.Tests/      # services métier (InMemory DbContext)
│  ├─ Fakes/                       # FakeClock, FakeCurrentUser
│  ├─ Infrastructure/              # TestDbContextFactory
│  ├─ Features/                    # FeatureServiceTests
│  ├─ CashSessions/                # ouverture/fermeture/règle solde théorique
│  └─ CashOperations/              # création, cohérence direction/catégorie, soft delete
└─ CashApp.Api.Tests/              # tests d'intégration via WebApplicationFactory
   └─ Auth/                        # login + bearer
```

Priorités V1 :
1. Calcul du solde théorique (`Opening + ΣIN − ΣOUT`, hors soft-deleted)
2. Unicité de la session OPEN par caisse
3. Cohérence catégorie ↔ direction sur création d'opération
4. Soft delete + recalcul session après annulation
5. Permissions clôture (caissier seulement sa session ; superviseur selon setting)
6. `FeatureService.EnsureEnabled` → 403 lorsque module désactivé
7. Login OK / KO / utilisateur inactif

Exemples fournis sous `CashApp.Application.Tests/` (voir fichiers dans le repo).

---

## Roadmap V2 — branchement sur l'architecture actuelle

Les entités V2 sont **déjà pré-câblées** dans `CashApp.Domain/Entities/V2/` et `AppDbContext` — créer une migration suffit pour les matérialiser. Chaque module suit le même pattern : feature code, service interface dans `Application`, controller dans `Api`, garde côté frontend.

| Module V2 | Feature code | Entités | Services à créer | Endpoints prévus | Garde |
|---|---|---|---|---|---|
| Workflow validation | `ADV_VALIDATION` | `ApprovalRule`, `ApprovalAction` | `IApprovalService` | `POST /api/approvals`, `GET /api/approvals?status=...` | Hook dans `CashOperationService` pour bloquer si règle active |
| Transferts inter-caisses | `ADV_TRANSFERS` | `CashTransfer` | `ITransferService` (déplace OUT côté source + IN côté cible dans la même transaction) | `POST /api/cash-transfers`, `GET /api/cash-transfers` | Visible si feature ON |
| Dépôt banque | `ADV_BANK_DEPOSITS` | `BankDeposit` | `IBankDepositService` | `POST /api/bank-deposits`, `GET /api/bank-deposits` | — |
| Pièces jointes | `ADV_ATTACHMENTS` | `Attachment` | `IAttachmentService` (entityType + entityId) | `POST /api/attachments`, `GET /api/attachments?entity=...&id=...` | UI : drag-drop dans le form opération |
| Rapports avancés | `ADV_ADVANCED_REPORTS` | — | étendre `IExportService` | `POST /api/exports/reports/{type}` | Onglet "Rapports avancés" dans `/exports` |
| Imports Excel/CSV | `ADV_IMPORTS` | `ImportBatch`, `ImportBatchLine` | `IImportService` (parse → batch → lignes en attente → validation) | `POST /api/imports`, `GET /api/imports/{id}` | Page dédiée |
| Rapprochement | `ADV_RECONCILIATION` | `ReconciliationBatch`, `ReconciliationItem` | `IReconciliationService` | `POST /api/reconciliations/match`, `GET /api/reconciliations/{id}` | Module séparé |
| Gestion des écarts | `ADV_VARIANCE_MANAGEMENT` | `VarianceAction` | `IVarianceService` (justification, action manager) | `POST /api/variances/{sessionId}/actions` | Apparait en complément de `CashSessionDetailPage` |
| Anomalies | `ADV_ANOMALIES` | — | `IAnomalyService` (jobs/règles détection) | `GET /api/anomalies` | Widget dashboard |
| Audit log | (transverse) | `AuditLog` | `IAuditLogger` injecté dans les services | `GET /api/audit-logs?entity=...&id=...` | Lecture admin |

Stratégie de branchement frontend :
- Les routes V2 vivront dans `src/modules/<module>/` et seront déclarées dans `AppRouter` avec `<ProtectedRoute feature="ADV_*">`
- Le menu `Sidebar.tsx` n'a qu'à recevoir les nouvelles entrées avec leur `feature`
- L'API client ajoute un nouveau fichier `src/api/<module>Api.ts`

---

## Plan de livraison (suggéré)

| Sprint | Contenu | État |
|---|---|---|
| Sprint 1 | Backend core (auth, users, registers, categories) | ✅ Livré (prompts 1-2) |
| Sprint 2 | Sessions, opérations, dashboard | ✅ Livré (prompt 2) |
| Sprint 3 | Settings, features, backups, exports | ✅ Livré (prompt 2 + ce prompt) |
| Sprint 4 | Stabilisation : tests étendus, UI polish, doc utilisateur, packaging Electron éventuel | À démarrer |
| Sprint 5 | V2 — workflow validation + transferts + dépôt banque | Plan ci-dessus |
| Sprint 6 | V2 — imports, rapprochement, gestion écarts, anomalies | Plan ci-dessus |

---

## Checklist de mise en route "de 0 à projet qui tourne"

### Backend
- [ ] `.NET 8 SDK` installé (`dotnet --version`)
- [ ] `cd backend && dotnet restore`
- [ ] `dotnet build` — 0 erreur
- [ ] `dotnet tool install -g dotnet-ef` (si pas déjà)
- [ ] Éditer `src/CashApp.Api/appsettings.json` — remplacer `Jwt:SigningKey` par une chaîne aléatoire ≥ 32 chars
- [ ] `dotnet ef migrations add InitialCreate --project src/CashApp.Infrastructure --startup-project src/CashApp.Api`
- [ ] `dotnet run --project src/CashApp.Api`
- [ ] Ouvrir `http://localhost:5080/swagger` — page chargée
- [ ] `POST /api/auth/login` avec `{ "username": "admin", "password": "Admin@123" }` → token reçu
- [ ] `dotnet test` — verts

### Frontend
- [ ] Node 20+ installé (`node --version`)
- [ ] `cd frontend/cashapp-web && npm install`
- [ ] `cp .env.example .env` — `VITE_API_BASE_URL=http://localhost:5080`
- [ ] `npm run dev`
- [ ] Ouvrir `http://localhost:5173` → page login
- [ ] Login `admin / Admin@123` → dashboard
- [ ] Sélecteur de caisse dans la topbar → choisir une caisse seedée
- [ ] Créer une catégorie, ouvrir une session, saisir une opération, fermer la session
- [ ] Exporter en Excel et en PDF
- [ ] `Settings > Modules` → désactiver `CORE_BACKUP` → recharger → l'entrée "Sauvegardes" disparaît du menu

### Production-ready (sprint 4)
- [ ] Changer le mot de passe `admin`
- [ ] `Jwt:SigningKey` provient d'un secret manager (User Secrets / variable d'environnement / KeyVault local)
- [ ] HTTPS configuré
- [ ] CORS restreint aux origines réelles
- [ ] Sauvegarde de la base programmée (manuelle ou `AUTO_BACKUP_*`)
- [ ] Tests xUnit verts en CI

---

## Liens utiles

- Backend détaillé : [`backend/README.md`](backend/README.md)
- Spécifications source : `1.txt`, `2.txt`, `3.txt`, `4.txt`
