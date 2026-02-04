# Architecture Open Asphalte

> Documentation de l'architecture modulaire d'Open Asphalte

## Vue d'ensemble

Open Asphalte est un plugin **C# modulaire** pour AutoCAD, basé sur une architecture à découverte automatique de modules.

```
┌─────────────────────────────────────────────────────────────────┐
│                          AutoCAD                                │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    OAS.Core.dll                             │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌─────────────────┐   │  │
│  │  │   Plugin    │  │  Discovery   │  │    Services     │   │  │
│  │  │ (entrypoint)│  │ (auto-scan)  │  │ (Geometry,etc)  │   │  │
│  │  └──────┬──────┘  └───────┬──────┘  └────────┬────────┘   │  │
│  │         │                 │                  │            │  │
│  │         └─────────────────┼──────────────────┘            │  │
│  │                           │                               │  │
│  │  ┌────────────────────────┼────────────────────────────┐  │  │
│  │  │                    IModule                          │  │  │
│  │  └────────────────────────┼────────────────────────────┘  │  │
│  └───────────────────────────┼───────────────────────────────┘  │
│                              │                                  │
│  ┌───────────────────────────┴───────────────────────────────┐  │
│  │                       Modules/                            │  │
│  │  ┌──────────────────┐  ┌──────────────────┐               │  │
│  │  │ Georeferencement │  │    StreetView    │    ...        │  │
│  │  │      Module      │  │      Module      │               │  │
│  │  └──────────────────┘  └──────────────────┘               │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Composants principaux

### 1. Core (`OAS.Core.dll`)

Le cœur du système, chargé une seule fois par AutoCAD via `NETLOAD`.

| Composant | Rôle |
|-----------|------|
| `Plugin.cs` | Point d'entrée `IExtensionApplication` |
| `ModuleDiscovery` | Scan et chargement automatique des modules |
| `Configuration` | Gestion des paramètres utilisateur (JSON) |
| `Localization` | Système de traduction FR/EN/ES |
| `Logger` | Logging dans la console AutoCAD |
| `Services/` | Services partagés (Geometry, Layer, etc.) |
| `UI/` | Constructeurs de menu et ruban dynamiques |

### 2. Modules (`bin/Modules/*.dll`)

Extensions découvertes automatiquement au démarrage.

**Convention de nommage :** `OAS.{ModuleName}.dll`

Chaque module implémente :
- `IModule` ou hérite de `ModuleBase`
- Fournit des commandes via `[CommandMethod]`
- Déclare ses traductions via `GetTranslations()`

## Flux de démarrage

```
1. AutoCAD → NETLOAD OAS.Core.dll
2. Plugin.Initialize()
   ├── Configuration.Load()          → Charge config JSON
   ├── Localization.Initialize()     → Charge traductions Core
   └── ModuleDiscovery.DiscoverAndLoad()
       └── Pour chaque OAS.*.dll dans Modules/:
           ├── Vérification signature (optionnel)
           ├── Recherche classes IModule
           ├── Validation dépendances
           ├── Validation version Core min
           └── Enregistrement traductions
3. ModuleDiscovery.InitializeAll()   → Appelle Initialize() sur chaque module
4. UI.CreateMenu() + UI.CreateRibbon()
5. Plugin prêt ✓
```

## Découverte des modules

Le système scanne automatiquement :
```
bin/Modules/OAS.*.dll
```

### Critères de chargement

1. **Nom de fichier** : Doit commencer par `OAS.`
2. **Interface** : Doit contenir une classe implémentant `IModule`
3. **Dépendances** : Tous les modules listés dans `Dependencies` doivent être chargés
4. **Version Core** : `MinCoreVersion` doit être satisfaite

### Ordre de chargement

Les modules sont chargés par ordre de `Dependencies` puis par `Order` croissant.

## Abstraction des commandes

```
CommandBase (abstract)
    │
    ├── Propriétés AutoCAD (Document, Database, Editor)
    ├── ExecuteSafe(Action)           → Gestion erreurs automatique
    ├── ExecuteInTransaction(Action)  → Transaction avec commit/rollback
    └── T(), TFormat()                → Traduction rapide
```

## Services partagés

| Service | Description |
|---------|-------------|
| `GeometryService` | Calculs géométriques, clothoïdes, hydraulique |
| `LayerService` | Gestion des calques AutoCAD |
| `CoordinateService` | Projections cartographiques |
| `UpdateService` | Vérification et installation des mises à jour |
| `UrlValidationService` | Validation sécurisée des URLs |

## Traductions

Système à 3 niveaux :

1. **Core** : Traductions système protégées (`app.`, `error.`, etc.)
2. **Module** : Traductions déclarées via `GetTranslations()`
3. **Fallback** : FR → EN → clé brute

```csharp
// Utilisation
L10n.T("key");                    // Traduction simple
L10n.TFormat("key", arg1, arg2);  // Avec formatage
```

## Configuration

Fichier : `%AppData%/Open Asphalte/config.json`

```json
{
  "_configVersion": 1,
  "language": "fr",
  "devMode": false,
  "checkUpdatesOnStartup": true
}
```

## Sécurité

- **Signature modules** : Vérification optionnelle des DLL
- **Liste blanche URLs** : github.com, gitlab.com, bitbucket.org
- **HTTPS obligatoire** : Pour tous les téléchargements

## Conventions

| Élément | Convention |
|---------|------------|
| Commandes | `OAS_{MODULE}_{ACTION}` |
| Calques | `OAS_{MODULE}_{ELEMENT}` |
| Clés traduction | `{module.id}.{section}.{key}` |
| Assemblies | `OpenAsphalte.{ModuleName}` |
