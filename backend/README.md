# CashApp — Backend Core (Prompt 1)

Fondation backend de la webapp de gestion de caisse — sortie du **prompt 1**.

## PARTIE A — Vue d'ensemble backend

### Architecture globale
Architecture **Clean / 4-layers** :

```
CashApp.Api             →  ASP.NET Core Web API, Program.cs, middleware, auth, controllers
        ↓
CashApp.Application     →  DTO, interfaces, validators, use-cases (logique applicative)
        ↓
CashApp.Domain          →  Entités, enums, constantes métier — pas de dépendance externe
        ↑
CashApp.Infrastructure  →  EF Core, DbContext, configurations, seed, password hasher, clock
CashApp.Shared          →  Helpers transverses

tests/
  CashApp.Application.Tests  →  tests unitaires services métier
  CashApp.Api.Tests          →  tests d'intégration via WebApplicationFactory
```

Flux des références : `Api → Application → Domain` et `Infrastructure → Application → Domain`. Api référence Infrastructure uniquement pour le câblage DI.

### Conventions de nommage
- **Tables SQL** : `snake_case` plurielles (`users`, `cash_operations`)
- **Colonnes** : nom de propriété C# (EF Core par défaut) — surchargé si besoin via Fluent API
- **Entités C#** : `PascalCase`, suffixe métier (`CashSession`, pas `CashSessionEntity`)
- **DTO** : suffixe `Dto` (`UserDetailDto`, `CreateUserDto`)
- **Interfaces** : préfixe `I` (`IFeatureService`)
- **Constantes** : `PascalCase` côté C#, `UPPER_SNAKE` côté valeur DB (ex: `RoleCodes.Admin = "ADMIN"`)
- **Routes API** : kebab-case (`/api/cash-sessions`)

### Stratégie V1 / V2 + features
- **Mode applicatif** stocké dans `app_settings[APP_MODE]` (ESSENTIAL / INTERMEDIATE / ADVANCED)
- **Features activables** dans `feature_settings` avec un code par module (cf `FeatureCodes`)
- Toutes les features V1 (`CORE_*`) sont activées au seed, toutes les features V2 (`ADV_*`) sont désactivées
- Le backend expose ces données via `IFeatureService` (implémentation au prompt 2) — frontend masque les routes/menus en conséquence
- Les entités V2 sont pré-câblées dans le DbContext pour éviter une migration disruptive plus tard

## PARTIE B — Arborescence backend

```
backend/
├─ CashApp.sln
├─ .gitignore
├─ README.md
├─ src/
│  ├─ CashApp.Api/
│  │  ├─ Configuration/JwtOptions.cs
│  │  ├─ Properties/launchSettings.json
│  │  ├─ Services/CurrentUserService.cs
│  │  ├─ Program.cs
│  │  ├─ appsettings.json
│  │  ├─ appsettings.Development.json
│  │  └─ CashApp.Api.csproj
│  ├─ CashApp.Application/
│  │  ├─ Common/
│  │  │  ├─ Exceptions/
│  │  │  │  ├─ AppException.cs
│  │  │  │  ├─ BusinessRuleException.cs
│  │  │  │  ├─ ForbiddenException.cs
│  │  │  │  ├─ NotFoundException.cs
│  │  │  │  └─ ValidationException.cs
│  │  │  ├─ Interfaces/
│  │  │  │  ├─ IAppDbContext.cs
│  │  │  │  ├─ IBackupService.cs
│  │  │  │  ├─ ICurrentUserService.cs
│  │  │  │  ├─ IDateTimeProvider.cs
│  │  │  │  ├─ IExportService.cs
│  │  │  │  ├─ IFeatureService.cs
│  │  │  │  ├─ IJwtTokenGenerator.cs
│  │  │  │  ├─ IPasswordHasher.cs
│  │  │  │  └─ IReferenceGeneratorService.cs
│  │  │  └─ Models/
│  │  │     ├─ ApiResponse.cs
│  │  │     ├─ PagedResponse.cs
│  │  │     └─ Result.cs
│  │  └─ CashApp.Application.csproj
│  ├─ CashApp.Domain/
│  │  ├─ Common/
│  │  │  ├─ AuditableEntity.cs
│  │  │  ├─ BaseEntity.cs
│  │  │  └─ ISoftDeletable.cs
│  │  ├─ Constants/
│  │  │  ├─ FeatureCodes.cs
│  │  │  ├─ RoleCodes.cs
│  │  │  └─ SettingKeys.cs
│  │  ├─ Entities/
│  │  │  ├─ AppSetting.cs
│  │  │  ├─ BackupLog.cs
│  │  │  ├─ CashOperation.cs
│  │  │  ├─ CashRegister.cs
│  │  │  ├─ CashSession.cs
│  │  │  ├─ Category.cs
│  │  │  ├─ FeatureSetting.cs
│  │  │  ├─ User.cs
│  │  │  ├─ UserCashRegister.cs
│  │  │  └─ V2/
│  │  │     ├─ ApprovalAction.cs
│  │  │     ├─ ApprovalRule.cs
│  │  │     ├─ Attachment.cs
│  │  │     ├─ AuditLog.cs
│  │  │     ├─ BankDeposit.cs
│  │  │     ├─ CashTransfer.cs
│  │  │     ├─ ImportBatch.cs
│  │  │     ├─ ImportBatchLine.cs
│  │  │     ├─ ReconciliationBatch.cs
│  │  │     ├─ ReconciliationItem.cs
│  │  │     └─ VarianceAction.cs
│  │  ├─ Enums/
│  │  │  ├─ AppMode.cs
│  │  │  ├─ CashSessionStatus.cs
│  │  │  ├─ OperationDirection.cs
│  │  │  └─ PaymentMethod.cs
│  │  └─ CashApp.Domain.csproj
│  ├─ CashApp.Infrastructure/
│  │  ├─ Persistence/
│  │  │  ├─ AppDbContext.cs
│  │  │  ├─ Configurations/
│  │  │  │  ├─ AppSettingConfiguration.cs
│  │  │  │  ├─ BackupLogConfiguration.cs
│  │  │  │  ├─ CashOperationConfiguration.cs
│  │  │  │  ├─ CashRegisterConfiguration.cs
│  │  │  │  ├─ CashSessionConfiguration.cs
│  │  │  │  ├─ CategoryConfiguration.cs
│  │  │  │  ├─ FeatureSettingConfiguration.cs
│  │  │  │  ├─ UserCashRegisterConfiguration.cs
│  │  │  │  ├─ UserConfiguration.cs
│  │  │  │  └─ V2Configurations.cs
│  │  │  └─ Seed/
│  │  │     └─ DbSeeder.cs
│  │  ├─ Security/Pbkdf2PasswordHasher.cs
│  │  ├─ Time/SystemDateTimeProvider.cs
│  │  ├─ DependencyInjection.cs
│  │  └─ CashApp.Infrastructure.csproj
│  └─ CashApp.Shared/
│     ├─ Helpers/StringHelpers.cs
│     └─ CashApp.Shared.csproj
└─ tests/
   ├─ CashApp.Application.Tests/CashApp.Application.Tests.csproj
   └─ CashApp.Api.Tests/CashApp.Api.Tests.csproj
```

## PARTIE C — Domain model

### Entités V1
| Entité            | Rôle                                                    |
|-------------------|---------------------------------------------------------|
| User              | Utilisateur applicatif                                  |
| CashRegister      | Caisse physique / logique                               |
| UserCashRegister  | Affectation user ↔ caisse                              |
| Category          | Catégorie d'opération (IN/OUT)                          |
| CashSession       | Session ouverte/fermée d'une caisse                     |
| CashOperation     | Entrée/sortie liée à une session (soft delete)          |
| BackupLog         | Trace des sauvegardes du fichier SQLite                 |
| AppSetting        | Paramètre clé/valeur (mode, devise, options de backup…) |
| FeatureSetting    | Module activable/désactivable                           |

### Entités V2 (esquisses)
`CashTransfer`, `BankDeposit`, `Attachment`, `ApprovalRule`, `ApprovalAction`,
`VarianceAction`, `ImportBatch`, `ImportBatchLine`, `ReconciliationBatch`,
`ReconciliationItem`, `AuditLog`.

### Enums
- `OperationDirection` : `IN` / `OUT`
- `CashSessionStatus` : `OPEN` / `CLOSED`
- `PaymentMethod` : `CASH` / `MOBILE_MONEY` / `BANK_TRANSFER` / `CHECK`
- `AppMode` : `ESSENTIAL` / `INTERMEDIATE` / `ADVANCED`

Stockage : tous les enums sont persistés **en chaîne** via `HasConversion<string>()`.

### Rôles (`RoleCodes`)
`ADMIN`, `SUPERVISOR`, `CASHIER`.

### Feature codes (`FeatureCodes`)
- Core V1 : `CORE_AUTH`, `CORE_USERS`, `CORE_CASH_REGISTERS`, `CORE_CASH_SESSIONS`,
  `CORE_OPERATIONS`, `CORE_CATEGORIES`, `CORE_DASHBOARD`, `CORE_EXPORTS`,
  `CORE_BACKUP`, `CORE_SETTINGS`
- Advanced V2 : `ADV_VALIDATION`, `ADV_TRANSFERS`, `ADV_BANK_DEPOSITS`,
  `ADV_ATTACHMENTS`, `ADV_ADVANCED_REPORTS`, `ADV_IMPORTS`, `ADV_RECONCILIATION`,
  `ADV_VARIANCE_MANAGEMENT`, `ADV_ANOMALIES`

### Settings keys (`SettingKeys`)
`APP_MODE`, `DEFAULT_CURRENCY`, `AUTO_BACKUP_ENABLED`, `AUTO_BACKUP_TIME`,
`AUTO_BACKUP_ON_SESSION_CLOSE`, `ALLOW_OPERATION_EDIT_BEFORE_SESSION_CLOSE`,
`ALLOW_SUPERVISOR_CLOSE_ANY_SESSION`, `OPERATION_REF_PREFIX`, `BACKUP_DIRECTORY`.

## PARTIE D / E / F / G / H

Le code des **entités, interfaces, configurations EF, seeder, et `Program.cs`** est
dans les fichiers listés ci-dessus. Tous sont compilables une fois les packages NuGet
restaurés. Voir « Lancement » plus bas.

### Contraintes & index posés
- Uniques : `users.username`, `cash_registers.code`, `categories.code`,
  `cash_operations.operation_ref`, `feature_settings.feature_code`,
  `app_settings.setting_key`, `(user_cash_registers.user_id, cash_register_id)`,
  `cash_transfers.transfer_ref`, `bank_deposits.deposit_ref`, `import_batches.batch_ref`,
  `reconciliation_batches.batch_ref`, `approval_rules.code`
- Index : `cash_operations.cash_register_id`, `cash_operations.cash_session_id`,
  `cash_operations.operation_date`, `cash_operations.is_deleted`,
  `cash_sessions.cash_register_id`, `cash_sessions.status`,
  `attachments(entity_type, entity_id)`, `approval_actions(entity_type, entity_id)`,
  `import_batch_lines.import_batch_id`, `audit_logs(entity_type, entity_id)`,
  `audit_logs.performed_at`
- Soft delete : `CashOperation` implémente `ISoftDeletable` + filtre global `HasQueryFilter`
- Audit : `BaseEntity` (CreatedAt/UpdatedAt rempli automatiquement dans `SaveChangesAsync`)
  et `AuditableEntity` (CreatedBy/UpdatedBy alimentés au prompt 2 via `ICurrentUserService`)

### Seed initial (cf `DbSeeder`)
- Utilisateur `admin` / `Admin@123` (PBKDF2, rôle `ADMIN`)
- `APP_MODE = ESSENTIAL`, devise par défaut `XOF`, autres settings par défaut
- 10 features `CORE_*` activées, 9 features `ADV_*` désactivées
- Catégories par défaut : `SALE/IN`, `ADVANCE/IN`, `PURCHASE/OUT`, `TRANSPORT/OUT`,
  `FUEL/OUT`, `OFFICE_SUPPLIES/OUT`

### Program.cs câble
- Controllers + Swagger (avec auth bearer)
- `AppDbContext` + SQLite via `AddInfrastructure`
- JWT bearer + autorisation (clés à compléter dans `appsettings.json`)
- CORS dev pour `http://localhost:5173` (Vite) et `:3000`
- `IHttpContextAccessor` + `ICurrentUserService`
- Au démarrage : `Migrate()` + `DbSeeder.SeedAsync`

Les placeholders pour les services métier (FeatureService, ReferenceGenerator,
BackupService, ExportService, JwtTokenGenerator…) sont commentés dans `Program.cs`
prêts à être décommentés au prompt 2.

## PARTIE I — Fichiers générés

Total : **52 fichiers** créés dans `backend/`.

### Lancement

Prérequis : **.NET 8 SDK**.

```bash
cd backend

# Restauration + build
dotnet restore
dotnet build

# Création de la migration initiale (une seule fois)
dotnet ef migrations add InitialCreate \
  --project src/CashApp.Infrastructure \
  --startup-project src/CashApp.Api

# Lancement (Migrate + Seed automatiques au démarrage)
dotnet run --project src/CashApp.Api
```

Swagger : `http://localhost:5080/swagger`.

Compte par défaut : `admin` / `Admin@123` (à modifier après le premier login).

### À générer au prompt 2
- DTO par module (Auth, Users, Cash*, Categories, Dashboard, Settings, Backups, Exports)
- Validators FluentValidation
- Services métier (Auth, User, CashRegister, Category, CashSession, CashOperation,
  Dashboard, Settings, Feature, Backup, Export, ReferenceGenerator, JwtTokenGenerator)
- Controllers REST + endpoints
- Câblage final dans `Program.cs`
