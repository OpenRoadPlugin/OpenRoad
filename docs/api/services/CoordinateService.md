# CoordinateService

> Service de conversion de coordonnees et gestion des projections

## Namespace

```csharp
using OpenAsphalte.Services;
```

## Description

`CoordinateService` fournit des fonctions de conversion entre differents systemes de coordonnees geographiques et de gestion des projections cartographiques. Il est utilise par le module Georeferencement mais peut etre appele par n'importe quel module.

## Constantes

| Constante | Type | Valeur | Description |
|-----------|------|--------|-------------|
| `OriginThreshold` | double | 1000.0 | Seuil en metres pour ignorer les points proches de l'origine |
| `SemiMajorAxis` | double | 6378137.0 | Demi-grand axe de l'ellipsoide WGS84 (metres) |
| `Flattening` | double | 1/298.257223563 | Aplatissement de l'ellipsoide WGS84 |

## Classe ProjectionInfo

Represente un systeme de projection.

```csharp
public class ProjectionInfo
{
    public string Code { get; set; }          // Code AutoCAD (ex: "Lambert-93")
    public string Name { get; set; }          // Nom complet
    public int Epsg { get; set; }             // Code EPSG
    public string Country { get; set; }       // Pays
    public string Region { get; set; }        // Region/Zone
    public string Unit { get; set; }          // Unite (metre, etc.)
    public string Description { get; set; }   // Description
    public ProjectionBounds Bounds { get; set; } // Limites geographiques
}
```

## Classe ProjectionBounds

Definit les limites geographiques d'une projection (en coordonnees projetees).

```csharp
public class ProjectionBounds
{
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
}
```

## Methodes

### GetAllProjections

Retourne la liste complete des projections disponibles.

```csharp
public static IEnumerable<ProjectionInfo> GetAllProjections()
```

**Retour** : Collection de toutes les projections de la base de donnees.

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

Recherche des projections par criteres.

```csharp
public static IEnumerable<ProjectionInfo> SearchProjections(string searchText)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| searchText | string | Texte de recherche (nom, code, pays, region) |

**Retour** : Projections correspondant aux criteres de recherche.

**Exemple** :
```csharp
var results = CoordinateService.SearchProjections("lambert");
// Retourne Lambert-93, CC42-50, NTF zones, etc.
```

---

### GetProjectionByCode

Recupere une projection par son code AutoCAD.

```csharp
public static ProjectionInfo? GetProjectionByCode(string code)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| code | string | Code AutoCAD de la projection |

**Retour** : La projection correspondante ou `null` si non trouvee.

**Exemple** :
```csharp
var lambert93 = CoordinateService.GetProjectionByCode("Lambert-93");
```

---

### DetectProjection

Detecte la projection probable a partir de coordonnees moyennes.

```csharp
public static ProjectionInfo? DetectProjection(double x, double y)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| x | double | Coordonnee X moyenne |
| y | double | Coordonnee Y moyenne |

**Retour** : La projection la plus probable ou `null` si indeterminable.

**Exemple** :
```csharp
// Coordonnees typiques Lambert 93
var detected = CoordinateService.DetectProjection(652000, 6862000);
// detected.Code == "Lambert-93"
```

---

### Lambert93ToWgs84

Convertit des coordonnees Lambert 93 en WGS84 (lat/lon).

```csharp
public static (double lat, double lon) Lambert93ToWgs84(double x, double y)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| x | double | Coordonnee Est Lambert 93 (metres) |
| y | double | Coordonnee Nord Lambert 93 (metres) |

**Retour** : Tuple (latitude, longitude) en degres decimaux.

**Exemple** :
```csharp
var (lat, lon) = CoordinateService.Lambert93ToWgs84(652000, 6862000);
// lat ≈ 48.85, lon ≈ 2.35 (Paris)
```

---

### Wgs84ToLambert93

Convertit des coordonnees WGS84 en Lambert 93.

```csharp
public static (double x, double y) Wgs84ToLambert93(double lat, double lon)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| lat | double | Latitude en degres decimaux |
| lon | double | Longitude en degres decimaux |

**Retour** : Tuple (X, Y) en metres Lambert 93.

**Exemple** :
```csharp
var (x, y) = CoordinateService.Wgs84ToLambert93(48.8566, 2.3522);
// x ≈ 652000, y ≈ 6862000
```

---

### CCToWgs84

Convertit des coordonnees Conique Conforme (CC) en WGS84.

```csharp
public static (double lat, double lon) CCToWgs84(double x, double y, int zoneNumber)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| x | double | Coordonnee Est CC (metres) |
| y | double | Coordonnee Nord CC (metres) |
| zoneNumber | int | Numero de zone CC (42 a 50) |

**Retour** : Tuple (latitude, longitude) en degres decimaux.

**Exemple** :
```csharp
// Zone CC44 (Nantes)
var (lat, lon) = CoordinateService.CCToWgs84(1350000, 6220000, 44);
```

---

### UtmToWgs84

Convertit des coordonnees UTM en WGS84.

```csharp
public static (double lat, double lon) UtmToWgs84(double easting, double northing, int zone, bool northern)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| easting | double | Coordonnee Est UTM (metres) |
| northing | double | Coordonnee Nord UTM (metres) |
| zone | int | Numero de zone UTM (1-60) |
| northern | bool | `true` si hemisphere nord |

**Retour** : Tuple (latitude, longitude) en degres decimaux.

**Exemple** :
```csharp
// Paris en UTM31N
var (lat, lon) = CoordinateService.UtmToWgs84(452000, 5412000, 31, true);
```

---

### Wgs84ToUtm

Convertit des coordonnees WGS84 en UTM.

```csharp
public static (double easting, double northing, int zone) Wgs84ToUtm(double lat, double lon)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| lat | double | Latitude en degres decimaux |
| lon | double | Longitude en degres decimaux |

**Retour** : Tuple (Easting, Northing, Zone).

**Exemple** :
```csharp
var (e, n, zone) = CoordinateService.Wgs84ToUtm(48.8566, 2.3522);
// zone = 31
```

---

### VincentyDistance

Calcule la distance geodesique entre deux points WGS84 (formule de Vincenty).

```csharp
public static double VincentyDistance(double lat1, double lon1, double lat2, double lon2)
```

**Parametres** :
| Parametre | Type | Description |
|-----------|------|-------------|
| lat1 | double | Latitude du point 1 (degres) |
| lon1 | double | Longitude du point 1 (degres) |
| lat2 | double | Latitude du point 2 (degres) |
| lon2 | double | Longitude du point 2 (degres) |

**Retour** : Distance en metres.

**Exemple** :
```csharp
// Distance Paris-Lyon
double dist = CoordinateService.VincentyDistance(48.8566, 2.3522, 45.7640, 4.8357);
// dist ≈ 393 km
```

## Base de donnees des projections

La base de donnees est stockee dans `bin/Data/projections.json` et contient plus de 40 projections :

- **France metropolitaine** : Lambert 93, zones CC (42-50), NTF zones
- **DOM-TOM** : Guyane, Antilles, Reunion, Mayotte, Saint-Pierre-et-Miquelon
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

## Precision des conversions

Les conversions utilisent des formules geodesiques rigoureuses :

- **Lambert -> WGS84** : Projection conique conforme secante
- **UTM -> WGS84** : Transverse Mercator universelle
- **Distance** : Formule iterative de Vincenty sur ellipsoide

Precision typique : < 1 metre pour la plupart des applications de voirie.

## Voir aussi

- [Module Georeferencement](../modules/georeferencement.md)
- [GeometryService](./GeometryService.md)
