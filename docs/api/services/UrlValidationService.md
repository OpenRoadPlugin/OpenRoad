# UrlValidationService

Service de validation des URLs pour la sécurité. Centralise les contrôles de sécurité pour éviter les duplications.

## Namespace

```csharp
using OpenRoad.Services;
```

## Sécurité

Ce service implémente une **liste blanche** de domaines autorisés pour les mises à jour :
- `github.com`
- `gitlab.com`
- `bitbucket.org`

Seul le protocole **HTTPS** est autorisé.

## Méthodes

### IsValidUpdateUrl

Valide qu'une URL de mise à jour est sécurisée.

```csharp
public static bool IsValidUpdateUrl(string? url)
```

**Paramètres :**
- `url` : URL à valider

**Retourne :** `true` si l'URL est valide et sécurisée, `false` sinon.

**Critères de validation :**
- URL non nulle et non vide
- URL absolue valide
- Protocole HTTPS uniquement
- Domaine dans la liste blanche

**Exemple :**
```csharp
// ✅ Valide
UrlValidationService.IsValidUpdateUrl("https://github.com/user/repo/releases/download/v1.0/file.dll");

// ❌ Invalide (HTTP non sécurisé)
UrlValidationService.IsValidUpdateUrl("http://github.com/user/repo/file.dll");

// ❌ Invalide (domaine non autorisé)
UrlValidationService.IsValidUpdateUrl("https://malicious-site.com/file.dll");
```

### IsSecureUrl

Valide qu'une URL est sécurisée (HTTPS uniquement).

```csharp
public static bool IsSecureUrl(string? url)
```

**Paramètres :**
- `url` : URL à valider

**Retourne :** `true` si l'URL utilise HTTPS, `false` sinon.

**Remarque :** Cette méthode ne vérifie pas la liste blanche, seulement le protocole.

### IsAllowedHost

Vérifie si un domaine est dans la liste blanche des mises à jour.

```csharp
public static bool IsAllowedHost(string? host)
```

**Paramètres :**
- `host` : Nom de domaine à vérifier

**Retourne :** `true` si le domaine est autorisé.

**Exemple :**
```csharp
UrlValidationService.IsAllowedHost("github.com");        // true
UrlValidationService.IsAllowedHost("api.github.com");    // true (sous-domaine)
UrlValidationService.IsAllowedHost("evil.com");          // false
```

## Bonnes pratiques

1. **Toujours valider** les URLs provenant de sources externes avant téléchargement
2. **Utiliser `IsValidUpdateUrl`** pour les URLs de mise à jour (plus strict)
3. **Utiliser `IsSecureUrl`** pour les URLs générales quand HTTPS suffit

## Extension de la liste blanche

Pour ajouter de nouveaux domaines autorisés, modifier `AllowedUpdateHosts` dans le code source :

```csharp
private static readonly string[] AllowedUpdateHosts = 
{ 
    "github.com", 
    "gitlab.com", 
    "bitbucket.org",
    // Ajouter ici
};
```

⚠️ **Attention** : N'ajoutez que des domaines de confiance pour éviter les attaques de type supply-chain.
