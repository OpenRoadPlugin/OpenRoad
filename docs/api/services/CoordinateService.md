# CoordinateService

> Service de conversion de coordonnées et gestion des projections

## Namespace

```csharp
using OpenAsphalte.Services;
```

## Description

`CoordinateService` fournit des fonctions de conversion entre différents systèmes de coordonnées géographiques et de gestion des projections cartographiques. Il est utilisé par le module Géoréférencement mais peut être appelé par n'importe quel module.

## Organisation des fichiers

`CoordinateService` est implémenté en tant que classe partielle (`partial class`) répartie sur 3 fichiers :

| Fichier | Contenu |
|---------|----------|
| `CoordinateService.cs` | API publique, recherche et détection de projections |
| `CoordinateService.ProjectionData.cs` | Base de données des projections (~700 lignes) |
| `CoordinateService.Transformations.cs` | Conversions Lambert93, CC, UTM et distance de Vincenty |

> **Note** : L'API publique reste identique — toutes les méthodes sont accessibles via `CoordinateService.*`.

---

## Constantes

| Constante | Type | Valeur | Description |
|-----------|------|--------|-------------|
| `OriginThreshold` | double | 1000.0 | Seuil en mètres pour ignorer les points proches de l'origine |
| `SemiMajorAxis` | double | 6378137.0 | Demi-grand axe de l'ellipsoïde WGS84 (mètres) |
| `Flattening` | double | 1/298.257223563 | Aplatissement de l'ellipsoïde WGS84 |

## Classe ProjectionInfo

Représente un système de projection.

```csharp
public class ProjectionInfo
{
    public string Code { get; set; }          // Code AutoCAD (ex: "Lambert-93")
    public string Name { get; set; }          // Nom complet
    public int Epsg { get; set; }             // Code EPSG
    public string Country { get; set; }       // Pays
    public string Region { get; set; }        // Région/Zone
    public string Unit { get; set; }          // Unité (mètre, etc.)
    public string Description { get; set; }   // Description
    public ProjectionBounds Bounds { get; set; } // Limites géographiques
}
```

## Classe ProjectionBounds

Définit les limites géographiques d'une projection (en coordonnées projetées).

```csharp
public class ProjectionBounds
{
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
}
```

## Méthodes

### GetAllProjections

Retourne la liste complète des projections disponibles.

```csharp
public static IEnumerable<ProjectionInfo> GetAllProjections()
```

**Retour** : Collection de toutes les projections de la base de données.

**Exemple** :
```csharp
var projections = CoordinateService.GetAllProjections();
foreach (var proj in projections)
{
    Logger.Info($"{proj.Code}: {proj.Name}");
}
```

---

### SearchProjections

Recherche des projections par critères.

```csharp
public static IEnumerable<ProjectionInfo> SearchProjections(string searchText)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| searchText | string | Texte de recherche (nom, code, pays, région) |

**Retour** : Projections correspondant aux critères de recherche.

**Exemple** :
```csharp
var results = CoordinateService.SearchProjections("lambert");
// Retourne Lambert-93, CC42-50, NTF zones, etc.
```

---

### GetProjectionByCode

Récupère une projection par son code AutoCAD.

```csharp
public static ProjectionInfo? GetProjectionByCode(string code)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| code | string | Code AutoCAD de la projection |

**Retour** : La projection correspondante ou `null` si non trouvée.

**Exemple** :
```csharp
var lambert93 = CoordinateService.GetProjectionByCode("Lambert-93");
```

---

### DetectProjection

Détecte la projection probable à partir de coordonnées moyennes.

```csharp
public static ProjectionInfo? DetectProjection(double x, double y)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| x | double | Coordonnée X moyenne |
| y | double | Coordonnée Y moyenne |

**Retour** : La projection la plus probable ou `null` si indéterminable.

**Exemple** :
```csharp
// Coordonnées typiques Lambert 93
var detected = CoordinateService.DetectProjection(652000, 6862000);
// detected.Code == "Lambert-93"
```

---

### Lambert93ToWgs84

Convertit des coordonnées Lambert 93 en WGS84 (lat/lon).

```csharp
public static (double lat, double lon) Lambert93ToWgs84(double x, double y)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| x | double | Coordonnée Est Lambert 93 (mètres) |
| y | double | Coordonnée Nord Lambert 93 (mètres) |

**Retour** : Tuple (latitude, longitude) en degrés décimaux.

**Exemple** :
```csharp
var (lat, lon) = CoordinateService.Lambert93ToWgs84(652000, 6862000);
// lat ≈ 48.85, lon ≈ 2.35 (Paris)
```

---

### Wgs84ToLambert93

Convertit des coordonnées WGS84 en Lambert 93.

```csharp
public static (double x, double y) Wgs84ToLambert93(double lat, double lon)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| lat | double | Latitude en degrés décimaux |
| lon | double | Longitude en degrés décimaux |

**Retour** : Tuple (X, Y) en mètres Lambert 93.

**Exemple** :
```csharp
var (x, y) = CoordinateService.Wgs84ToLambert93(48.8566, 2.3522);
// x ≈ 652000, y ≈ 6862000
```

---

### CCToWgs84

Convertit des coordonnées Conique Conforme (CC) en WGS84.

```csharp
public static (double lat, double lon) CCToWgs84(double x, double y, int zoneNumber)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| x | double | Coordonnée Est CC (mètres) |
| y | double | Coordonnée Nord CC (mètres) |
| zoneNumber | int | Numéro de zone CC (42 à 50) |

**Retour** : Tuple (latitude, longitude) en degrés décimaux.

**Exemple** :
```csharp
// Zone CC44 (Nantes)
var (lat, lon) = CoordinateService.CCToWgs84(1350000, 6220000, 44);
```

---

### UtmToWgs84

Convertit des coordonnées UTM en WGS84.

```csharp
public static (double lat, double lon) UtmToWgs84(double easting, double northing, int zone, bool northern)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| easting | double | Coordonnée Est UTM (mètres) |
| northing | double | Coordonnée Nord UTM (mètres) |
| zone | int | Numéro de zone UTM (1-60) |
| northern | bool | `true` si hémisphère nord |

**Retour** : Tuple (latitude, longitude) en degrés décimaux.

**Exemple** :
```csharp
// Paris en UTM31N
var (lat, lon) = CoordinateService.UtmToWgs84(452000, 5412000, 31, true);
```

---

### Wgs84ToUtm

Convertit des coordonnées WGS84 en UTM.

```csharp
public static (double easting, double northing, int zone) Wgs84ToUtm(double lat, double lon)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| lat | double | Latitude en degrés décimaux |
| lon | double | Longitude en degrés décimaux |

**Retour** : Tuple (Easting, Northing, Zone).

**Exemple** :
```csharp
var (e, n, zone) = CoordinateService.Wgs84ToUtm(48.8566, 2.3522);
// zone = 31
```

---

### VincentyDistance

Calcule la distance géodésique entre deux points WGS84 (formule de Vincenty).

```csharp
public static double VincentyDistance(double lat1, double lon1, double lat2, double lon2)
```

**Paramètres** :
| Paramètre | Type | Description |
|-----------|------|-------------|
| lat1 | double | Latitude du point 1 (degrés) |
| lon1 | double | Longitude du point 1 (degrés) |
| lat2 | double | Latitude du point 2 (degrés) |
| lon2 | double | Longitude du point 2 (degrés) |

**Retour** : Distance en mètres.

**Exemple** :
```csharp
// Distance Paris-Lyon
double dist = CoordinateService.VincentyDistance(48.8566, 2.3522, 45.7640, 4.8357);
// dist ≈ 393 km
```

## Base de données des projections

La base de données est stockée dans `bin/Data/projections.json` et contient plus de 40 projections :

- **France métropolitaine** : Lambert 93, zones CC (42-50), NTF zones
- **DOM-TOM** : Guyane, Antilles, Réunion, Mayotte, Saint-Pierre-et-Miquelon
- **Belgique** : Lambert 2008, Lambert 72
- **Suisse** : CH1903+ LV95, CH1903 LV03
- **Luxembourg** : LUREF
- **Allemagne** : ETRS89 UTM zones
- **Espagne** : ETRS89 zones
- **Italie** : Monte Mario, UTM zones
- **Royaume-Uni** : British National Grid
- **Pays-Bas** : RD New
- **Canada** : NAD83 MTM zones
- **UTM mondial** : Zones 20-40N, zones 20-40S

## Précision des conversions

Les conversions utilisent des formules géodésiques rigoureuses :

- **Lambert -> WGS84** : Projection conique conforme sécante
- **UTM -> WGS84** : Transverse Mercator universelle
- **Distance** : Formule itérative de Vincenty sur ellipsoïde

Précision typique : < 1 mètre pour la plupart des applications de voirie.

## Voir aussi

- [Module Géoréférencement](../modules/georeferencement.md)
- [GeometryService](./GeometryService.md)
