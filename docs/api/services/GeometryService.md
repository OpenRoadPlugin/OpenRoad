# GeometryService - Référence API

> Référence pour les développeurs de modules Open Asphalte.

Le `GeometryService` est une bibliothèque statique de calculs géométriques, hydrauliques et de cubature.
Toutes les méthodes sont accessibles via `OpenAsphalte.Services.GeometryService`.

## Organisation des fichiers

`GeometryService` est implémenté en tant que classe partielle (`partial class`) répartie sur 5 fichiers pour une meilleure maintenabilité :

| Fichier | Contenu |
|---------|----------|
| `GeometryService.cs` | Constantes, distances, angles, points, polylignes, tests, aires, cercles/arcs, surfaces/MNT |
| `GeometryService.Intersections.cs` | Intersections de droites, segments et cercles, points de tangence |
| `GeometryService.Voirie.cs` | Clothoïdes, rayon de courbure, dévers, distances d'arrêt/dépassement, courbes verticales |
| `GeometryService.Hydraulics.cs` | Manning-Strickler, hydraulique des conduites et canaux, coefficients de Strickler |
| `GeometryService.Earthwork.cs` | Profils en travers, volumes (cubature), foisonnement, tranchées |

> **Note** : L'API publique reste identique — toutes les méthodes sont accessibles via `GeometryService.*`.

---

## Table des matières

1. [Constantes](#constantes)
2. [Distances et angles](#distances-et-angles)
3. [Points et projections](#points-et-projections)
4. [Polylignes](#polylignes)
5. [Tests géométriques](#tests-géométriques)
6. [Aires et périmètres](#aires-et-périmètres)
7. [Intersections](#intersections)
8. [Cercles et arcs](#cercles-et-arcs)
9. [Voirie - tracé en plan](#voirie---tracé-en-plan)
10. [Voirie - profil en long](#voirie---profil-en-long)
11. [Assainissement - hydraulique](#assainissement---hydraulique)
12. [Cubature et terrassement](#cubature-et-terrassement)
13. [Surfaces et MNT](#surfaces-et-mnt)

---

## Constantes

```csharp
using OpenAsphalte.Services;

GeometryService.Tolerance   // 1e-10 - Tolérance pour comparaisons
GeometryService.Gravity     // 9.81 m/s^2
GeometryService.DegToRad    // pi/180 - Degrés vers radians
GeometryService.RadToDeg    // 180/pi - Radians vers degrés
```

---

## Distances et angles

### Distances

| Méthode | Description | Retour |
|---------|-------------|--------|
| `Distance(p1, p2)` | Distance 3D entre deux points | `double` |
| `Distance2D(p1, p2)` | Distance 2D (ignore Z) | `double` |
| `HorizontalDistance(p1, p2)` | Alias de `Distance2D` | `double` |
| `DeltaZ(p1, p2)` | Différence d'altitude (p2.Z - p1.Z) | `double` |

### Angles

| Méthode | Description | Retour |
|---------|-------------|--------|
| `AngleBetween(from, to)` | Angle en radians [-pi, pi] | `double` |
| `AngleBetweenDegrees(from, to)` | Angle en degrés | `double` |
| `NormalizeAngle(angle)` | Normalise en [0, 2*pi] | `double` |
| `NormalizeAngleDegrees(angle)` | Normalise en [0, 360] | `double` |
| `AngleBetweenVectors(v1, v2)` | Angle entre vecteurs [0, pi] | `double` |
| `Bearing(from, to)` | Gisement en grades [0, 400] | `double` |
| `BearingToAngle(bearing)` | Convertit gisement -> angle trigo | `double` |

---

## Points et projections

### Manipulation de points

| Méthode | Description |
|---------|-------------|
| `OffsetPoint(point, angle, distance)` | Point décalé selon angle et distance |
| `PerpendicularOffset(point, angle, distance, leftSide)` | Décalage perpendiculaire |
| `MidPoint(p1, p2)` | Point milieu |
| `Lerp(p1, p2, t)` | Interpolation linéaire (t dans [0,1]) |
| `RotatePoint(point, center, angle)` | Rotation autour d'un centre |
| `TranslatePoint(point, vector)` | Translation par vecteur |
| `TranslatePoint(point, dx, dy, dz)` | Translation par composantes |

### Projections

| Méthode | Description |
|---------|-------------|
| `ProjectPointOnLine(point, lineStart, lineEnd)` | Projection sur droite infinie |
| `ProjectPointOnSegment(point, segStart, segEnd)` | Projection sur segment |
| `DistancePointToLine(point, lineStart, lineEnd)` | Distance point-droite |
| `DistancePointToSegment(point, segStart, segEnd)` | Distance point-segment |

**Exemple**

```csharp
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Services;

var p1 = new Point3d(0, 0, 0);
var p2 = new Point3d(10, 0, 0);

var pt = GeometryService.OffsetPoint(Point3d.Origin, Math.PI / 4, 10);
var proj = GeometryService.ProjectPointOnSegment(pt, p1, p2);
```

---

## Polylignes

| Méthode | Description |
|---------|-------------|
| `GetPolylinePoints(polyline)` | Liste des sommets |
| `GetPolylineLength(polyline)` | Longueur totale |
| `GetPointAtDistance(polyline, distance)` | Point à une abscisse curviligne |
| `GetTangentAngle(polyline, distance)` | Angle tangent à une position |

---

## Tests géométriques

| Méthode | Description | Retour |
|---------|-------------|--------|
| `IsPointOnLeftSide(lineStart, lineEnd, point)` | Point à gauche de la ligne | `bool` |
| `IsPointInPolygon(point, polygonPoints)` | Point dans le polygone | `bool` |

---

## Aires et périmètres

| Méthode | Description |
|---------|-------------|
| `CalculatePolygonArea(points)` | Aire 2D (formule du lacet) |
| `CalculatePolygonPerimeter(points)` | Périmètre d'un polygone |
| `CalculateCentroid(points)` | Centre de gravité |
| `CalculateTriangleArea(p1, p2, p3)` | Aire d'un triangle 2D |
| `CalculateTriangleArea3D(p1, p2, p3)` | Aire réelle 3D d'un triangle |

---

## Intersections

| Méthode | Description |
|---------|-------------|
| `IntersectLines(l1Start, l1End, l2Start, l2End)` | Intersections de droites (résultat détaillé) |
| `IntersectSegments(s1Start, s1End, s2Start, s2End)` | Intersection de segments (point ou null) |
| `IntersectLineCircle(lineStart, lineEnd, center, radius)` | Intersection droite / cercle |
| `IntersectCircles(center1, r1, center2, r2)` | Intersection entre cercles |
| `TangentPointsFromExternalPoint(extPoint, center, radius)` | Points de tangence depuis un point externe |

**Exemple**

```csharp
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Services;

var a1 = new Point3d(0, 0, 0);
var a2 = new Point3d(10, 0, 0);
var b1 = new Point3d(5, -5, 0);
var b2 = new Point3d(5, 5, 0);

var result = GeometryService.IntersectLines(a1, a2, b1, b2);
if (result.HasIntersection && result.IsOnBothSegments)
{
    var pt = result.Point;
}
```

---

## Cercles et arcs

| Méthode | Description |
|---------|-------------|
| `CircleFrom3Points(p1, p2, p3)` | Cercle passant par 3 points |
| `ArcLength(radius, angleRadians)` | Longueur d'arc |
| `ChordLength(radius, angleRadians)` | Longueur de corde |
| `Sagita(radius, angleRadians)` | Flèche d'arc |

---

## Voirie - tracé en plan

| Méthode | Description |
|---------|-------------|
| `ClothoidParameter(radius, length)` | Paramètre de clothoïde |
| `ClothoidCoordinates(A, L)` | Coordonnées clothoïde (x, y, tau) |
| `MinClothoidLength(radius, speedKmh)` | Longueur min de clothoïde |
| `MinCurveRadius(speedKmh, deversPercent, frictionCoef)` | Rayon minimal de courbe |
| `RecommendedSuperelevation(radius, speedKmh)` | Dévers recommandé |
| `CurveWidening(radius, vehicleLength)` | Surlargeur en courbe |
| `StoppingDistance(speedKmh, reactionTime, frictionCoef, slopePercent)` | Distance d'arrêt |
| `OvertakingDistance(speedKmh)` | Distance de dépassement |

---

## Voirie - profil en long

| Méthode | Description |
|---------|-------------|
| `SlopePercent(p1, p2)` | Pente en % |
| `SlopePerMille(p1, p2)` | Pente en pour mille |
| `VerticalCurveParameters(slope1, slope2, length)` | Paramètres de raccordement vertical |
| `MinCrestCurveLength(slope1, slope2, stoppingDist)` | Longueur min de raccord convexe |
| `MinSagCurveLength(slope1, slope2, stoppingDist)` | Longueur min de raccord concave |
| `VerticalCurveElevation(startZ, startSlope, curveLength, position, endSlope)` | Z sur une courbe verticale |

---

## Assainissement - hydraulique

| Méthode | Description |
|---------|-------------|
| `ManningStricklerFlow(K, section, Rh, slope)` | Débit (Manning-Strickler) |
| `ManningStricklerVelocity(K, Rh, slope)` | Vitesse (Manning-Strickler) |
| `HydraulicRadius(wettedArea, wettedPerimeter)` | Rayon hydraulique |
| `CircularPipeHydraulics(diameter, fillRatio)` | Hydraulique conduite circulaire |
| `FullPipeFlow(diameter, slopePercent, K)` | Débit en charge |
| `RequiredPipeDiameter(flowRate, slopePercent, K)` | Diamètre requis |
| `SelfCleaningSlope(diameter, minVelocity)` | Pente d'auto-curage |
| `OvoidPipeHydraulics(height, fillRatio)` | Hydraulique conduite ovoïde |
| `RectangularChannelHydraulics(width, height, waterDepth)` | Canal rectangulaire |
| `TrapezoidalChannelHydraulics(bottomWidth, waterDepth, sideSlope)` | Canal trapézoïdal |

---

## Cubature et terrassement

| Méthode | Description |
|---------|-------------|
| `CrossSectionAreas(profilePoints, referenceLevel)` | Aires de coupe/remblai |
| `VolumeByAverageEndArea(area1, area2, distance)` | Volume par aire moyenne |
| `VolumeByPrismoidal(area1, areaMiddle, area2, distance)` | Volume par prismoïde |
| `TotalEarthworkVolumes(sectionsList)` | Volumes totaux coupe/remblai |
| `ApplyBulking(volumeEnPlace, bulkingFactor)` | Foisonnement |
| `CompactedVolume(volumeFoisonne, compactionRatio)` | Volume compacté |
| `TrenchVolume(width, depth, length)` | Volume de tranchée |
| `TrenchVolumeWithSlope(bottomWidth, depth, length, sideSlope)` | Volume de tranchée avec talus |
| `BeddingVolume(pipeOuterDiameter, thickness, trenchWidth, length)` | Lit de pose |
| `SurroundVolume(pipeOuterDiameter, trenchWidth, length, coverAbovePipe)` | Enrobage |

---

## Surfaces et MNT

| Méthode | Description |
|---------|-------------|
| `InterpolateZFromPlane(point, p1, p2, p3)` | Interpolation Z sur plan |
| `PlaneSlope(p1, p2, p3)` | Pente du plan en % |
| `PlaneAspect(p1, p2, p3)` | Azimut en grades |
| `TriangularPrismVolume(p1, p2, p3, referenceZ)` | Volume d'un prisme triangulaire |

---

## Exemple rapide

```csharp
using OpenAsphalte.Services;
using Autodesk.AutoCAD.Geometry;

var p1 = new Point3d(0, 0, 0);
var p2 = new Point3d(10, 0, 0);

var dist = GeometryService.Distance(p1, p2);
var mid = GeometryService.MidPoint(p1, p2);
```
