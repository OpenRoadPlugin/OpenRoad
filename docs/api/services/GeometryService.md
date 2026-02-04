# ?? GeometryService - Documentation complète

> **Référence API pour les développeurs de modules Open Road**

Le `GeometryService` est une bibliothèque statique de calculs géométriques, hydrauliques et de cubature. Toutes les méthodes sont accessibles via `OpenRoad.Services.GeometryService`.

---

## ?? Table des matières

1. [Constantes](#constantes)
2. [Distance et Angles](#distance-et-angles)
3. [Points et Projections](#points-et-projections)
4. [Polylignes](#polylignes)
5. [Tests géométriques](#tests-géométriques)
6. [Aires et Périmètres](#aires-et-périmètres)
7. [Intersections](#intersections)
8. [Cercles et Arcs](#cercles-et-arcs)
9. [Voirie - Tracé en plan](#voirie---tracé-en-plan)
10. [Voirie - Profil en long](#voirie---profil-en-long)
11. [Assainissement - Hydraulique](#assainissement---hydraulique)
12. [Cubature et Terrassement](#cubature-et-terrassement)
13. [Surfaces et MNT](#surfaces-et-mnt)

---

## Constantes

```csharp
using OpenRoad.Services;

// Constantes de conversion
GeometryService.Tolerance   // 1e-10 - Tolérance pour comparaisons
GeometryService.Gravity     // 9.81 m/s²
GeometryService.DegToRad    // ?/180 - Degrés vers radians
GeometryService.RadToDeg    // 180/? - Radians vers degrés
```

---

## Distance et Angles

### Distances

| Méthode | Description | Retour |
|---------|-------------|--------|
| `Distance(p1, p2)` | Distance 3D entre deux points | `double` |
| `Distance2D(p1, p2)` | Distance 2D (ignore Z) | `double` |
| `HorizontalDistance(p1, p2)` | Alias de Distance2D (topographie) | `double` |
| `DeltaZ(p1, p2)` | Différence d'altitude (p2.Z - p1.Z) | `double` |

```csharp
// Exemple
var dist = GeometryService.Distance(new Point3d(0, 0, 0), new Point3d(3, 4, 5));
// Résultat: 7.07 (?50)

var dist2D = GeometryService.Distance2D(new Point3d(0, 0, 0), new Point3d(3, 4, 5));
// Résultat: 5.0 (Z ignoré)
```

### Angles

| Méthode | Description | Retour |
|---------|-------------|--------|
| `AngleBetween(from, to)` | Angle en radians [-?, ?] | `double` |
| `AngleBetweenDegrees(from, to)` | Angle en degrés | `double` |
| `NormalizeAngle(angle)` | Normalise en [0, 2?] | `double` |
| `NormalizeAngleDegrees(angle)` | Normalise en [0, 360] | `double` |
| `AngleBetweenVectors(v1, v2)` | Angle entre vecteurs [0, ?] | `double` |
| `Bearing(from, to)` | Gisement en grades [0, 400] | `double` |
| `BearingToAngle(bearing)` | Convertit gisement ? angle trigo | `double` |

```csharp
// Angle depuis l'axe X+ (sens trigo)
var angle = GeometryService.AngleBetween(Point3d.Origin, new Point3d(1, 1, 0));
// Résultat: ?/4 (45°)

// Gisement depuis le Nord (sens horaire)
var gisement = GeometryService.Bearing(Point3d.Origin, new Point3d(100, 100, 0));
// Résultat: 50 grades (NE)
```

---

## Points et Projections

### Manipulation de points

| Méthode | Description |
|---------|-------------|
| `OffsetPoint(point, angle, distance)` | Point décalé selon angle et distance |
| `PerpendicularOffset(point, angle, dist, leftSide)` | Décalage perpendiculaire |
| `MidPoint(p1, p2)` | Point milieu |
| `Lerp(p1, p2, t)` | Interpolation linéaire (t ? [0,1]) |
| `RotatePoint(point, center, angle)` | Rotation autour d'un centre |
| `TranslatePoint(point, vector)` | Translation par vecteur |
| `TranslatePoint(point, dx, dy, dz)` | Translation par composantes |

```csharp
// Créer un point à 10m dans la direction de 45°
var pt = GeometryService.OffsetPoint(Point3d.Origin, Math.PI / 4, 10);

// Point à mi-chemin
var mid = GeometryService.Lerp(p1, p2, 0.5);

// Rotation de 90° autour de l'origine
var rotated = GeometryService.RotatePoint(point, Point3d.Origin, Math.PI / 2);
```

### Projections

| Méthode | Description |
|---------|-------------|
| `ProjectPointOnLine(point, lineStart, lineEnd)` | Projection sur droite infinie |
| `ProjectPointOnSegment(point, segStart, segEnd)` | Projection sur segment (contrainte) |
| `DistancePointToLine(point, lineStart, lineEnd)` | Distance point-droite |
| `DistancePointToSegment(point, segStart, segEnd)` | Distance point-segment |

```csharp
// Projection orthogonale sur un axe
var proj = GeometryService.ProjectPointOnLine(point, axeStart, axeEnd);

// Distance d'un point à un segment
var dist = GeometryService.DistancePointToSegment(point, segStart, segEnd);
```

---

## Polylignes

| Méthode | Description |
|---------|-------------|
| `GetPolylinePoints(polyline)` | Liste des sommets |
| `GetPolylineLength(polyline)` | Longueur totale |
| `GetPointAtDistance(polyline, distance)` | Point à une abscisse curviligne |
| `GetTangentAngle(polyline, distance)` | Angle tangent à une position |

```csharp
ExecuteInTransaction(tr =>
{
    var poly = /* sélection polyligne */;
    
    // Points tous les 10m
    double length = GeometryService.GetPolylineLength(poly);
    for (double d = 0; d <= length; d += 10)
    {
        var pt = GeometryService.GetPointAtDistance(poly, d);
        var tangent = GeometryService.GetTangentAngle(poly, d);
        // ...
    }
});
```

---

## Tests géométriques

| Méthode | Description | Retour |
|---------|-------------|--------|
| `IsPointOnLeftSide(lineStart, lineEnd, point)` | Point à gauche de la ligne? | `bool` |
| `IsPointInPolygon(point, polygon)` | Point dans le polygone? | `bool` |

```csharp
// Déterminer le côté d'un point par rapport à un axe
bool isLeft = GeometryService.IsPointOnLeftSide(axeStart, axeEnd, point);

// Test d'inclusion dans une parcelle
var boundary = new List<Point3d> { /* sommets */ };
bool inside = GeometryService.IsPointInPolygon(testPoint, boundary);
```

---

## Aires et Périmètres

| Méthode | Description |
|---------|-------------|
| `CalculatePolygonArea(points)` | Aire 2D (formule du lacet) |
| `CalculatePolygonPerimeter(points)` | Périmètre d'un polygone |
| `CalculateCentroid(points)` | Centre de gravité |
| `CalculateTriangleArea(p1, p2, p3)` | Aire d'un triangle 2D |
| `CalculateTriangleArea3D(p1, p2, p3)` | Aire réelle 3D d'un triangle |

```csharp
var parcelle = new List<Point3d> { /* sommets */ };
double surface = GeometryService.CalculatePolygonArea(parcelle);
var centroide = GeometryService.CalculateCentroid(parcelle);
```

---

## Intersections

### Lignes et segments

```csharp
// Intersection de deux droites
var result = GeometryService.IntersectLines(line1Start, line1End, line2Start, line2End);
if (result.HasIntersection)
{
    Point3d pt = result.Point;
    bool onSeg1 = result.IsOnSegment1; // T1 ? [0,1]
    bool onSeg2 = result.IsOnSegment2; // T2 ? [0,1]
    bool onBoth = result.IsOnBothSegments;
}

// Intersection de deux segments (plus simple)
Point3d? intersection = GeometryService.IntersectSegments(seg1Start, seg1End, seg2Start, seg2End);
if (intersection.HasValue) { /* ... */ }
```

### Ligne et cercle

```csharp
var result = GeometryService.IntersectLineCircle(lineStart, lineEnd, center, radius);
// result.Count: 0 (aucune), 1 (tangent), 2 (sécante)
if (result.Count >= 1) { var p1 = result.Point1.Value; }
if (result.Count == 2) { var p2 = result.Point2.Value; }
```

### Deux cercles

```csharp
var result = GeometryService.IntersectCircles(center1, radius1, center2, radius2);
// result.Count: -1 (identiques), 0 (disjoints), 1 (tangents), 2 (sécants)
```

### Tangentes

```csharp
// Points de tangence depuis un point externe
var tangents = GeometryService.TangentPointsFromExternalPoint(externalPoint, center, radius);
if (tangents.HasValue)
{
    var (t1, t2) = tangents.Value;
    // t1 et t2 sont les deux points de tangence
}
```

---

## Cercles et Arcs

| Méthode | Description |
|---------|-------------|
| `CircleFrom3Points(p1, p2, p3)` | Cercle passant par 3 points |
| `ArcLength(radius, angle)` | Longueur d'arc |
| `SectorArea(radius, angle)` | Aire d'un secteur |
| `CircularSegmentArea(radius, angle)` | Aire segment (entre corde et arc) |
| `ChordLength(radius, angle)` | Longueur de corde |
| `Sagita(radius, angle)` | Flèche de l'arc |

```csharp
// Trouver le cercle passant par 3 points
var circle = GeometryService.CircleFrom3Points(p1, p2, p3);
if (circle.HasValue)
{
    Point3d center = circle.Value.Center;
    double radius = circle.Value.Radius;
}
```

---

## Voirie - Tracé en plan

### Clothoïdes (transitions de courbure)

La **clothoïde** est la courbe idéale pour les transitions en voirie car sa courbure varie linéairement.

```csharp
// Paramètre A de la clothoïde
double A = GeometryService.ClothoidParameter(radius: 200, length: 50);
// A² = R × L ? A = ?(200 × 50) = 100

// Coordonnées sur la clothoïde
var (x, y, tau) = GeometryService.ClothoidCoordinates(A, L: 25);
// tau = angle de rotation en radians

// Longueur minimale selon la règle du confort
double Lmin = GeometryService.MinClothoidLength(radius: 200, speedKmh: 90);
```

### Rayons et dévers

```csharp
// Rayon minimum pour une vitesse et un dévers donnés
double Rmin = GeometryService.MinCurveRadius(
    speedKmh: 90, 
    superelevationPercent: 7, 
    frictionCoef: 0.13
);

// Dévers recommandé pour un rayon donné
double devers = GeometryService.RecommendedSuperelevation(
    radius: 300, 
    speedKmh: 90
);
```

### Surlargeur et visibilité

```csharp
// Surlargeur en courbe pour PL 12m
double surlargeur = GeometryService.CurveWidening(radius: 100, vehicleLength: 12);

// Distance de visibilité d'arrêt
double Dv = GeometryService.StoppingDistance(
    speedKmh: 90, 
    reactionTime: 2.0, 
    frictionCoef: 0.35, 
    slopePercent: -3  // descente
);

// Distance de dépassement
double Dd = GeometryService.OvertakingDistance(speedKmh: 90);
```

---

## Voirie - Profil en long

### Pentes

```csharp
// Pente entre deux points
double pente = GeometryService.SlopePercent(p1, p2);      // en %
double penteMillieme = GeometryService.SlopePerMille(p1, p2);  // en ‰
```

### Raccordements verticaux

```csharp
// Paramètres d'une parabole verticale
var (R, fleche, isConvexe) = GeometryService.VerticalCurveParameters(
    slope1: 3,    // 3% montée
    slope2: -2,   // 2% descente
    length: 100   // 100m de raccordement
);

// Longueur minimale point haut (convexe)
double Lmin = GeometryService.MinCrestCurveLength(
    slope1: 3, slope2: -2,
    stoppingDistance: 120,
    eyeHeight: 1.10, objectHeight: 0.15
);

// Longueur minimale point bas (concave)
double Lmin = GeometryService.MinSagCurveLength(
    slope1: -3, slope2: 2,
    stoppingDistance: 120
);

// Altitude sur la parabole
double z = GeometryService.VerticalCurveElevation(
    startZ: 100,
    startSlope: 3,
    curveLength: 100,
    position: 50,  // au milieu
    endSlope: -2
);
```

---

## Assainissement - Hydraulique

### Formule de Manning-Strickler

```csharp
// Q = K × S × Rh^(2/3) × ?I

// Débit
double Q = GeometryService.ManningStricklerFlow(
    stricklerK: 70,        // béton ordinaire
    section: 0.5,          // m²
    hydraulicRadius: 0.15, // m
    slopeDecimal: 0.01     // 1%
);

// Vitesse
double V = GeometryService.ManningStricklerVelocity(stricklerK: 70, hydraulicRadius: 0.15, slopeDecimal: 0.01);

// Rayon hydraulique
double Rh = GeometryService.HydraulicRadius(wettedArea: 0.5, wettedPerimeter: 2.0);
```

### Coefficients de Strickler

```csharp
using static OpenRoad.Services.GeometryService.StricklerCoefficients;

// Valeurs disponibles:
BetonLisse       // 80
BetonCentrifuge  // 90
BetonOrdinary    // 70
BetonRuqueux     // 60
Gres             // 75
FonteDuctile     // 80
PVCNeuf          // 100
PVCUsage         // 90
PEHD             // 100
Acier            // 85
FosseEnTerre     // 35
FosseEnherbe     // 25
Enrochement      // 30
```

### Sections hydrauliques

#### Canalisation circulaire

```csharp
// Paramètres à remplissage partiel
var (S, Pm, Rh) = GeometryService.CircularPipeHydraulics(
    diameter: 0.400,    // Ø400
    fillRatio: 0.80     // 80% rempli
);

// Débit à pleine section
double Qps = GeometryService.FullPipeFlow(
    diameter: 0.400,
    slopePercent: 1.0,
    stricklerK: 70
);

// Diamètre nécessaire pour un débit
double D = GeometryService.RequiredPipeDiameter(
    flowRate: 0.150,    // m³/s
    slopePercent: 1.0,
    stricklerK: 70
);

// Pente d'auto-curage
double Imin = GeometryService.SelfCleaningSlope(
    diameter: 0.300,
    minVelocity: 0.60   // m/s
);
```

#### Ovoïde T150

```csharp
var (S, Pm, Rh) = GeometryService.OvoidPipeHydraulics(
    height: 1.50,       // Ovoïde T150
    fillRatio: 0.70
);
```

#### Section rectangulaire (dalot/cadre)

```csharp
var (S, Pm, Rh) = GeometryService.RectangularChannelHydraulics(
    width: 1.50,
    height: 1.00,
    waterDepth: 0.60
);
```

#### Section trapézoïdale (fossé)

```csharp
var (S, Pm, Rh) = GeometryService.TrapezoidalChannelHydraulics(
    bottomWidth: 0.50,
    waterDepth: 0.30,
    sideSlope: 1.5      // talus 3/2
);
```

### Regards

```csharp
// Hauteur de chute
double chute = GeometryService.ManholeDrop(
    upstreamInvert: 95.50,
    downstreamInvert: 94.70
);

// Dissipation d'énergie nécessaire?
bool dissipation = GeometryService.RequiresEnergyDissipation(chute, threshold: 0.80);
```

---

## Cubature et Terrassement

### Aires de profils en travers

```csharp
// Points du profil (X = distance à l'axe, Z = altitude TN)
var profilTN = new List<Point3d>
{
    new Point3d(-6, 0, 102.50),
    new Point3d(-3, 0, 101.80),
    new Point3d(0, 0, 101.20),
    new Point3d(3, 0, 101.50),
    new Point3d(6, 0, 102.30),
};

// Niveau projet
double niveauProjet = 101.00;

// Calcul des aires
var (aireDeblai, aireRemblai) = GeometryService.CrossSectionAreas(profilTN, niveauProjet);
```

### Volumes entre profils

```csharp
// Méthode de la moyenne des aires (approchée)
double volume = GeometryService.VolumeByAverageEndArea(
    area1: 12.5,
    area2: 15.8,
    distance: 20
);

// Méthode prismoïdale (plus précise)
double volume = GeometryService.VolumeByPrismoidal(
    area1: 12.5,
    areaMiddle: 14.0,
    area2: 15.8,
    distance: 20
);
```

### Volumes totaux de terrassement

```csharp
// Liste des profils (PK, aire déblai, aire remblai)
var profils = new List<(double Pk, double CutArea, double FillArea)>
{
    (0, 12.5, 0),
    (20, 15.8, 1.2),
    (40, 8.3, 5.6),
    (60, 2.1, 12.4),
};

var (volumeDeblai, volumeRemblai) = GeometryService.TotalEarthworkVolumes(profils);
```

### Foisonnement et compactage

```csharp
using static OpenRoad.Services.GeometryService.BulkingFactors;

// Coefficients disponibles:
TerreVegetale      // 1.25
Argile             // 1.30
Sable              // 1.10
Gravier            // 1.15
RocheFragmentee    // 1.50
RocheMassive       // 1.65
Enrobes            // 1.30

// Application
double volumeFoisonne = GeometryService.ApplyBulking(volumeEnPlace: 1000, Argile);
// 1000 × 1.30 = 1300 m³

double volumeCompacte = GeometryService.CompactedVolume(volumeFoisonne: 1300, compactionRatio: 0.90);
// 1300 × 0.90 = 1170 m³
```

### Tranchées

```csharp
// Tranchée à parois verticales
double vol = GeometryService.TrenchVolume(width: 0.80, depth: 1.20, length: 100);

// Tranchée avec talutage
double vol = GeometryService.TrenchVolumeWithSlope(
    bottomWidth: 0.80,
    depth: 1.50,
    length: 100,
    sideSlope: 0.5    // talus 1/2
);

// Volume de lit de pose
double litPose = GeometryService.BeddingVolume(
    pipeOuterDiameter: 0.450,
    beddingThickness: 0.10,
    trenchWidth: 0.90,
    length: 100
);

// Volume d'enrobage
double enrobage = GeometryService.SurroundVolume(
    pipeOuterDiameter: 0.450,
    trenchWidth: 0.90,
    length: 100,
    coverAbovePipe: 0.10
);
```

### Excavations

```csharp
// Volume d'un tronc de pyramide (fouille à talus)
double vol = GeometryService.FrustumVolume(
    topArea: 50,      // aire au sol
    bottomArea: 30,   // aire au fond
    height: 2.5
);
```

---

## Surfaces et MNT

### Interpolation d'altitude

```csharp
// Z interpolé dans un triangle (TIN)
double z = GeometryService.InterpolateZFromPlane(
    point: new Point3d(10, 15, 0),
    p1: triangleVertex1,
    p2: triangleVertex2,
    p3: triangleVertex3
);
```

### Pente et orientation

```csharp
// Pente d'un plan (gradient max)
double pente = GeometryService.PlaneSlope(p1, p2, p3);  // en %

// Exposition (orientation de la pente)
double azimut = GeometryService.PlaneAspect(p1, p2, p3);  // en grades
```

### Volumes TIN

```csharp
// Volume d'un prisme triangulaire
double vol = GeometryService.TriangularPrismVolume(p1, p2, p3, referenceZ: 100);
```

---

## ?? Bonnes pratiques

### Toujours utiliser les constantes de conversion

```csharp
// ? Bon
double angleRad = angleDeg * GeometryService.DegToRad;
double angleDeg = angleRad * GeometryService.RadToDeg;

// ? Éviter
double angleRad = angleDeg * Math.PI / 180;
```

### Vérifier la tolérance pour les comparaisons

```csharp
// ? Bon
if (Math.Abs(value) < GeometryService.Tolerance) { /* zéro */ }

// ? Éviter
if (value == 0) { /* risque d'erreur flottante */ }
```

### Utiliser les structures de résultat

```csharp
// ? Les résultats d'intersection ont des propriétés utiles
var result = GeometryService.IntersectLines(/* ... */);
if (result.HasIntersection && result.IsOnBothSegments)
{
    // Intersection valide sur les deux segments
}
```

---

## ?? Références

- **Voirie** : Guide technique SETRA/CERTU
- **Hydraulique** : Formule de Manning-Strickler (1890)
- **Clothoïdes** : Spirale de Cornu / intégrales de Fresnel
- **Cubature** : Méthode des profils en travers

---

*Document généré pour Open Road v0.0.1 | .NET 8.0*
