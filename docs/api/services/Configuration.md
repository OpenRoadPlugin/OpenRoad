# Configuration

Service de configuration utilisateur. Stocke les paramètres dans un fichier JSON dans `%AppData%/Open Asphalte/config.json`.

## Namespace

```csharp
using OpenAsphalte.Configuration;
```

## Accès direct

| Propriété | Description | Défaut |
|-----------|-------------|--------|
| `Language` | Langue active (fr, en, es) | `fr` |
| `DevMode` | Mode développeur (logs debug) | `false` |
| `UpdateUrl` | URL des releases | `https://github.com/OpenAsphaltePlugin/OpenAsphalte/releases/latest` |
| `CheckUpdatesOnStartup` | Vérifier les mises à jour au démarrage | `true` |
| `MainMenuName` | Nom du menu principal | `Open Asphalte` |

## Lecture / écriture générique

```csharp
// Lecture
var lang = Configuration.Get("language", "fr");
var devMode = Configuration.Get("devMode", false);

// Écriture
Configuration.Set("language", "en");
Configuration.Set("devMode", true);
Configuration.Save();
```

## Cycle de vie

| Methode | Description |
|---------|-------------|
| `Load()` | Charge la configuration depuis le disque. |
| `Save()` | Sauvegarde la configuration. |
| `Reload()` | Recharge et applique la configuration. |
| `ClearEventSubscribers()` | Nettoie les abonnés à l'événement. |

## Événements

- `OnSettingChanged` : déclenché lors d'une modification d'une clé.

## Exemple

```csharp
using OpenAsphalte.Configuration;

Configuration.Load();
Configuration.Set("language", "es");
Configuration.Save();
```

## Exemple avancé

```csharp
using OpenAsphalte.Configuration;

Configuration.OnSettingChanged += (key, value) =>
{
	// Réagir à la modification des paramètres
};

Configuration.Set("mainMenuName", "MonEntreprise - OA");
Configuration.Save();
```
