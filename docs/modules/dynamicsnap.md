# Module : Accrochage Dynamique (OAS.DynamicSnap)

Le module **DynamicSnap** est un module système qui fournit des services d'accrochage intelligent et de visualisation pour les autres modules Open Asphalte.

> ℹ️ **Note** : Ce module ne contient pas de commandes utilisateur directes. Il s'active automatiquement lorsqu'un autre module (comme Cota2Lign) en a besoin.

## 🎯 Fonctionnalités

*   **Visualisation temps-réel** : Affiche des marqueurs graphiques (points, lignes de projection) lors des commandes OAS.
*   **Snapping Intelligent** : Permet l'accrochage sur des éléments géométriques complexes (projection orthogonale, sommets de polylignes 2D/3D).

## 👨‍💻 Pour les développeurs

Ce module expose une API statique via `DynamicSnapService` que vous pouvez utiliser dans vos propres modules.

### Utilisation

```csharp
using OpenAsphalte.Discovery;
using OpenAsphalte.Modules.DynamicSnap;

// Dans votre commande
var snapModule = ModuleDiscovery.GetModule<DynamicSnapModule>();
if (snapModule != null)
{
    // Utiliser le service d'accrochage
}
```

### Méthodes clés

*   Fournit des helpers pour visualiser des points temporaires sans polluer la database AutoCAD.
*   Gère les calculs de projection dynamique sur polylignes.

## ℹ️ Détails techniques

*   **Identifiant** : `dynamicsnap`
*   **Ordre de chargement** : 1 (Prioritaire)
*   **Version** : 1.0.0
