# UpdateService

Service de gestion des mises à jour et du téléchargement de modules pour Open Road.

## Namespace

```csharp
using OpenRoad.Services;
```

## Méthodes

### CheckForUpdatesAsync

Vérifie les mises à jour disponibles pour le Core et les modules.

```csharp
public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
```

**Retourne :** Un `UpdateCheckResult` contenant les informations sur les mises à jour disponibles.

**Exemple :**
```csharp
var result = await UpdateService.CheckForUpdatesAsync();
if (result.Success)
{
    if (result.CoreUpdateAvailable)
    {
        Console.WriteLine($"Nouvelle version Core disponible: {result.LatestCoreVersion}");
    }
    
    foreach (var update in result.Updates)
    {
        Console.WriteLine($"Module {update.ModuleId}: {update.NewVersion}");
    }
}
```

### InstallCoreUpdateAsync

Télécharge et lance l'installateur pour le Core.

```csharp
public static async Task InstallCoreUpdateAsync(string downloadUrl)
```

**Paramètres :**
- `downloadUrl` : URL de téléchargement de l'installateur

**Remarque :** Cette méthode télécharge l'installateur dans le dossier temporaire et le lance avec les arguments Inno Setup `/SILENT /CLOSEAPPLICATIONS`.

### InstallModuleAsync

Installe ou met à jour un module.

```csharp
public static async Task InstallModuleAsync(ModuleDefinition moduleDef)
```

**Paramètres :**
- `moduleDef` : Définition du module à installer (depuis le manifeste)

## Classes associées

### UpdateCheckResult

Résultat de la vérification des mises à jour.

| Propriété | Type | Description |
|-----------|------|-------------|
| `Success` | `bool` | Indique si la vérification a réussi |
| `ErrorMessage` | `string` | Message d'erreur en cas d'échec |
| `Manifest` | `MarketplaceManifest?` | Manifeste du marketplace |
| `CoreUpdateAvailable` | `bool` | Mise à jour Core disponible |
| `LatestCoreVersion` | `Version?` | Dernière version du Core |
| `Updates` | `List<ModuleUpdateInfo>` | Liste des mises à jour modules |

### ModuleUpdateInfo

Informations sur une mise à jour de module.

| Propriété | Type | Description |
|-----------|------|-------------|
| `ModuleId` | `string` | Identifiant du module |
| `CurrentVersion` | `Version?` | Version installée (null si nouveau) |
| `NewVersion` | `Version` | Nouvelle version disponible |
| `IsNewInstall` | `bool` | True si c'est une nouvelle installation |

### MarketplaceManifest

Structure du manifeste du marketplace.

| Propriété | Type | Description |
|-----------|------|-------------|
| `Core` | `CoreDefinition` | Informations sur le Core |
| `Modules` | `List<ModuleDefinition>` | Liste des modules disponibles |

### ModuleDefinition

Définition d'un module dans le marketplace.

| Propriété | Type | Description |
|-----------|------|-------------|
| `Id` | `string` | Identifiant unique |
| `Name` | `string` | Nom affiché |
| `Description` | `string` | Description |
| `Version` | `string` | Version disponible |
| `MinCoreVersion` | `string` | Version Core minimale requise |
| `DownloadUrl` | `string` | URL de téléchargement |
| `Author` | `string` | Auteur du module |

## Configuration

L'URL du marketplace peut être configurée :

```json
{
  "updateUrl": "https://raw.githubusercontent.com/openroadplugin/openroad/main/docs/marketplace.json"
}
```

## Gestion des erreurs

Le service gère les erreurs réseau avec des messages traduits :

| Code HTTP | Clé de traduction |
|-----------|-------------------|
| 404 | `update.error.notFound` |
| 403 | `update.error.forbidden` |
| Timeout | `update.error.timeout` |
| Autre | `update.error.http` |
