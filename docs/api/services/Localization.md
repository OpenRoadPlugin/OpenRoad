# Localization

Système de traduction multilingue. Supporte FR, EN, ES et les traductions par module avec fallback automatique.

## Namespace

```csharp
using L10n = OpenAsphalte.Localization.Localization;
```

## Propriété et données

- `CurrentLanguage` : langue active.
- `SupportedLanguages` : liste des langues supportées.
- `LanguageNames` : noms pour affichage UI.

## Méthodes principales

| Methode | Description |
|---------|-------------|
| `Initialize()` | Charge les traductions du Core. |
| `SetLanguage(lang)` | Change la langue active et déclenche l'event. |
| `T(key, defaultValue = null)` | Traduction simple. |
| `TFormat(key, params)` | Traduction avec formatage. |
| `RegisterTranslations(lang, dict)` | Enregistre des traductions (Core ou module). |
| `ClearEventSubscribers()` | Nettoie les abonnés aux événements. |

## Événements

- `OnLanguageChanged(oldLang, newLang)` : déclenché lors d'un changement de langue.

## Exemple

```csharp
using L10n = OpenAsphalte.Localization.Localization;

var text = L10n.T("system.help");
L10n.SetLanguage("en");
```

## Notes

- Les modules déclarent leurs traductions via `GetTranslations()` dans `ModuleBase`.
- Les clés système sont protégées (préfixes `app.`, `system.`, `error.`, etc.).

## Exemple avancé

```csharp
using L10n = OpenAsphalte.Localization.Localization;

L10n.RegisterTranslations("fr", new Dictionary<string, string>
{
	["monmodule.title"] = "Mon module"
});

L10n.OnLanguageChanged += (oldLang, newLang) =>
{
	// Mettre à jour une UI personnalisée
};
```
