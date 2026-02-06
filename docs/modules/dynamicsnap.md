# Module : Accrochage Dynamique (OAS.DynamicSnap)

Le module **DynamicSnap** est un module système qui fournit des services d'accrochage intelligent, de visualisation et de **surbrillance d'entités** pour les autres modules Open Asphalte.

> ℹ️ **Note** : Ce module ne contient pas de commandes utilisateur directes. Il s'active automatiquement lorsqu'un autre module (comme Cota2Lign) en a besoin.

## 🎯 Fonctionnalités

| Fonctionnalité | Description |
|---|---|
| **Visualisation temps-réel** | Marqueurs graphiques (points, lignes de projection) lors des commandes OAS |
| **Snapping Intelligent** | Accrochage sur éléments géométriques complexes (projection orthogonale, sommets de polylignes 2D/3D) |
| **Surbrillance d'entités** *(v0.0.2)* | Mise en évidence visuelle des entités sélectionnées avec distinction Primary / Secondary |

---

## 🔦 Surbrillance d'entités (Highlight API)

La surbrillance permet de mettre en évidence les entités AutoCAD sélectionnées pendant une commande, avec deux niveaux visuels :

| État | Rendu | Usage typique |
|---|---|---|
| **Primary** | Ligne continue, épaisseur forte (défaut 0.50 mm) | Entité principale, celle sur laquelle l'utilisateur agit |
| **Secondary** | Ligne tiretée (DASHED), épaisseur fine (défaut 0.20 mm) | Entités de contexte, sélectionnées mais non actives |

La surbrillance utilise `TransientManager` (clones colorés superposés) et ne modifie jamais les entités source.

### Configuration utilisateur

Accessible via **OAS_DYNAMICSNAP_SETTINGS** → section « Surbrillance des entités » :

| Paramètre | Clé config.json | Défaut |
|---|---|---|
| Activée | `dynamicsnap.highlight.enabled` | `true` |
| Couleur (ACI) | `dynamicsnap.highlight.color` | `4` (Cyan) |
| Épaisseur Primary (1/100 mm) | `dynamicsnap.highlight.primaryweight` | `50` (0.50 mm) |
| Épaisseur Secondary (1/100 mm) | `dynamicsnap.highlight.secondaryweight` | `20` (0.20 mm) |

---

## 👨‍💻 Intégration dans vos modules

### 1. SnapHelper (accrochage)

```csharp
using OpenAsphalte.Modules.DynamicSnap.Services;

// L'accrochage reste inchangé
var point = SnapHelper.GetSnappedPoint(Editor, polylineId, promptMessage, settings);
```

### 2. HighlightHelper (surbrillance)

API statique exposée par `HighlightHelper`. Toutes les méthodes sont **no-op silencieuses** si DynamicSnap n'est pas chargé ou si la surbrillance est désactivée.

```csharp
using OpenAsphalte.Modules.DynamicSnap.Services;

// ── Mettre des entités en surbrillance (état Primary uniforme) ──
HighlightHelper.HighlightEntities(entityId1, entityId2);

// ── Distinguer une entité principale ──
// entityId1 reste Primary, entityId2 passe en Secondary (tirets + fin)
HighlightHelper.SetPrimaryEntity(entityId1);

// ── Nettoyer (à appeler dans un bloc finally) ──
HighlightHelper.ClearHighlight();
```

### Méthodes disponibles

| Méthode | Description |
|---|---|
| `HighlightEntities(params ObjectId[])` | Met en surbrillance une ou plusieurs entités (toutes en Primary) |
| `SetPrimaryEntity(ObjectId)` | Définit l'entité principale ; les autres deviennent Secondary |
| `ClearHighlight()` | Supprime toute surbrillance. Fonctionne même si le module est indisponible (sécurité `finally`) |
| `ClearHighlight(ObjectId)` | Supprime la surbrillance d'une seule entité |
| `IsHighlighted(ObjectId)` | Vérifie si une entité est actuellement en surbrillance |

### Exemple complet (pattern Cota2Lign)

```csharp
using OpenAsphalte.Modules.DynamicSnap.Services;

public void Execute()
{
    ExecuteSafe(() =>
    {
        // 1. Sélection des entités
        var polyline1Id = /* sélection polyligne 1 */;
        var polyline2Id = /* sélection polyligne 2 */;

        // 2. Surbrillance des deux polylignes (Primary uniforme)
        HighlightHelper.HighlightEntities(polyline1Id, polyline2Id);

        try
        {
            // 3. Distinction : polyline1 = Primary, polyline2 = Secondary
            HighlightHelper.SetPrimaryEntity(polyline1Id);

            // 4. Travail sur la polyligne maîtresse
            var startPoint = GetPointOnPolyline("Début", polyline1Id);
            var endPoint = GetPointOnPolyline("Fin", polyline1Id);

            // 5. Traitement...
            ExecuteInTransaction(tr => { /* ... */ });
        }
        finally
        {
            // 6. Nettoyage garanti
            HighlightHelper.ClearHighlight();
        }
    });
}
```

### Référence de projet

Ajoutez une référence à `OAS.DynamicSnap` dans votre `.csproj` :

```xml
<Reference Include="OAS.DynamicSnap">
  <HintPath>..\..\bin\Modules\OAS.DynamicSnap.dll</HintPath>
  <Private>false</Private>
</Reference>
```

---

## 🏗️ Architecture interne

```
OAS.DynamicSnap/
├── DynamicSnapModule.cs              # Module, traductions, commandes
├── Models/
│   ├── DynamicSnapSettings.cs        # Config accrochage
│   └── HighlightConfiguration.cs     # Config surbrillance
├── Services/
│   ├── DynamicSnapService.cs         # Service principal, persistance config
│   ├── EntityHighlightService.cs     # Moteur TransientManager (interne)
│   ├── HighlightHelper.cs            # API publique surbrillance
│   └── SnapHelper.cs                 # API publique accrochage
├── Commands/
│   └── DynamicSnapSettingsCommand.cs  # OAS_DYNAMICSNAP_SETTINGS
└── Views/
    ├── DynamicSnapSettingsWindow.xaml
    └── DynamicSnapSettingsWindow.xaml.cs
```

| Classe | Visibilité | Rôle |
|---|---|---|
| `HighlightHelper` | **Publique** | Point d'entrée pour les modules consommateurs |
| `EntityHighlightService` | Interne | Gère les clones `Drawable`, le `TransientManager` et le cache de linetype |
| `HighlightConfiguration` | Publique | Modèle de configuration (Enabled, Color, Weights) |

---

## ℹ️ Détails techniques

| Propriété | Valeur |
|---|---|
| **Identifiant** | `dynamicsnap` |
| **Ordre de chargement** | 1 (Prioritaire) |
| **Version** | 0.0.2 |
| **Rendu** | `TransientManager` (clones colorés, aucune modification DB) |
| **Linetype Secondary** | `DASHED` chargé depuis `acad.lin`, caché en statique |
| **Thread-safety** | `lock` sur les opérations de transients |
