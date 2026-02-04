# LayerService

Service de gestion des calques AutoCAD. Fournit des méthodes utilitaires pour créer, modifier et interroger les calques.

## Namespace

```csharp
using OpenRoad.Services;
```

## Dépendances

```csharp
using AcColor = Autodesk.AutoCAD.Colors.Color;
using AcColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;
```

## Méthodes

### EnsureLayer

Crée un calque s'il n'existe pas, ou récupère son ObjectId s'il existe déjà.

```csharp
public static ObjectId EnsureLayer(
    Database db, 
    Transaction tr, 
    string layerName, 
    AcColor? color = null, 
    string? linetype = null)
```

**Paramètres :**
- `db` : Database AutoCAD cible
- `tr` : Transaction active (doit être en cours)
- `layerName` : Nom du calque à créer ou récupérer
- `color` : Couleur du calque (défaut: blanc/noir selon fond)
- `linetype` : Type de ligne (défaut: CONTINUOUS)

**Exemple :**
```csharp
ExecuteInTransaction(tr =>
{
    var layerId = LayerService.EnsureLayer(Database, tr, "OR_AXES",
        AcColor.FromColorIndex(AcColorMethod.ByAci, 1)); // Rouge
});
```

### GetAllLayers

Récupère tous les calques du dessin avec leurs propriétés.

```csharp
public static List<LayerInfo> GetAllLayers(Database db, Transaction tr)
```

**Retourne :** Liste de `LayerInfo` contenant les propriétés de chaque calque.

### GetVisibleLayers

Récupère les calques visibles (non éteints, non gelés).

```csharp
public static List<LayerInfo> GetVisibleLayers(Database db, Transaction tr)
```

### LayerExists

Vérifie si un calque existe.

```csharp
public static bool LayerExists(Database db, Transaction tr, string layerName)
```

### SetCurrentLayer

Définit le calque courant.

```csharp
public static void SetCurrentLayer(Database db, Transaction tr, string layerName)
```

### SetLayerOn

Active ou désactive un calque.

```csharp
public static void SetLayerOn(Database db, Transaction tr, string layerName, bool on)
```

### SetLayerFrozen

Gèle ou dégèle un calque.

```csharp
public static void SetLayerFrozen(Database db, Transaction tr, string layerName, bool frozen)
```

## Classes associées

### LayerInfo

Informations sur un calque AutoCAD.

| Propriété | Type | Description |
|-----------|------|-------------|
| `Name` | `string` | Nom du calque |
| `Color` | `AcColor` | Couleur du calque |
| `ObjectId` | `ObjectId` | ObjectId du calque |
| `IsOn` | `bool` | Calque allumé |
| `IsFrozen` | `bool` | Calque gelé |
| `IsLocked` | `bool` | Calque verrouillé |

## Bonnes pratiques

- Préfixer vos calques par `OR_` pour les identifier comme calques Open Road
- Toujours utiliser dans une transaction (`ExecuteInTransaction`)
- Vérifier l'existence d'un calque avant de le modifier
