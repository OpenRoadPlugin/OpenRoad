# Logger

Service de logging unifié pour Open Asphalte. Écrit dans la ligne de commande AutoCAD et optionnellement dans un fichier.

## Namespace

```csharp
using OpenAsphalte.Logging;
```

## Propriété et comportement

- `DebugMode` (bool) : active les messages `Debug`.
- `FileLoggingEnabled` (bool) : active l'écriture dans un fichier (par défaut true).
- `Prefix` (string) : préfixe affiché avant les messages (par défaut "Open Asphalte").
- `LogFilePath` (string) : chemin du fichier de log dans `%AppData%/Open Asphalte/logs/`.

Le fichier de log quotidien est créé avec le format :
`openasphalte_YYYY-MM-DD.log`.

Rotation :
- Rotation à 5 MB.
- Archive en `*.log.old`.
- Conserve les 5 derniers fichiers.

## Méthodes

| Methode | Description |
|---------|-------------|
| `Debug(message)` | Message de debug (visible uniquement si `DebugMode` est true). |
| `Info(message)` | Message d'information. |
| `Success(message)` | Message de succès. |
| `Warning(message)` | Message d'avertissement. |
| `Error(message)` | Message d'erreur. |
| `Raw(message)` | Message brut sans prefixe. |

## Exemple

```csharp
using OpenAsphalte.Logging;

Logger.Info("Debut traitement...");
Logger.Success("Operation terminee");
```

## Exemple avancé

```csharp
using OpenAsphalte.Logging;

Logger.DebugMode = true;
Logger.FileLoggingEnabled = false;
Logger.Debug("Trace technique");
Logger.Warning("Attention: valeur limite");
```
