// Copyright 2026 Open Asphalte Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace OpenAsphalte.Services;

/// <summary>
/// Service de calculs géométriques pour AutoCAD.
/// Fournit des méthodes utilitaires pour les opérations géométriques courantes,
/// la voirie, l'assainissement et les calculs de cubature.
/// </summary>
/// <remarks>
/// Documentation complète: docs/GeometryService.md
/// </remarks>
public static class GeometryService
{
    #region Constantes
    
    /// <summary>Tolérance par défaut pour les comparaisons de doubles</summary>
    public const double Tolerance = 1e-10;
    
    /// <summary>Accélération gravitationnelle (m/s²)</summary>
    public const double Gravity = 9.81;
    
    /// <summary>Pi / 180 pour conversion degrés → radians</summary>
    public const double DegToRad = Math.PI / 180.0;
    
    /// <summary>180 / Pi pour conversion radians → degrés</summary>
    public const double RadToDeg = 180.0 / Math.PI;
    
    #endregion
    
    #region Distance et Angles
    
    /// <summary>
    /// Calcule la distance euclidienne entre deux points 3D.
    /// </summary>
    /// <param name="p1">Premier point</param>
    /// <param name="p2">Deuxième point</param>
    /// <returns>Distance entre les deux points (toujours positive)</returns>
    /// <example>
    /// <code>
    /// var distance = GeometryService.Distance(new Point3d(0, 0, 0), new Point3d(3, 4, 0));
    /// // Résultat: 5.0
    /// </code>
    /// </example>
    public static double Distance(Point3d p1, Point3d p2)
    {
        return p1.DistanceTo(p2);
    }
    
    /// <summary>
    /// Calcule la distance 2D (ignore Z) entre deux points.
    /// </summary>
    public static double Distance2D(Point3d p1, Point3d p2)
    {
        double dx = p2.X - p1.X;
        double dy = p2.Y - p1.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// Calcule la distance horizontale (projetée) entre deux points.
    /// Alias de Distance2D pour clarté dans les contextes de topographie.
    /// </summary>
    public static double HorizontalDistance(Point3d p1, Point3d p2) => Distance2D(p1, p2);
    
    /// <summary>
    /// Calcule la différence d'altitude entre deux points.
    /// </summary>
    /// <returns>Différence positive si p2 est plus haut que p1</returns>
    public static double DeltaZ(Point3d p1, Point3d p2) => p2.Z - p1.Z;
    
    /// <summary>
    /// Calcule l'angle formé par le vecteur allant de <paramref name="from"/> vers <paramref name="to"/>.
    /// L'angle est mesuré dans le sens trigonométrique depuis l'axe X positif.
    /// </summary>
    /// <param name="from">Point d'origine du vecteur</param>
    /// <param name="to">Point de destination du vecteur</param>
    /// <returns>Angle en radians dans l'intervalle [-π, π]</returns>
    /// <remarks>
    /// Utilise <see cref="Math.Atan2"/> pour gérer correctement tous les quadrants.
    /// Pour un résultat en degrés, utilisez <see cref="AngleBetweenDegrees"/>.
    /// </remarks>
    public static double AngleBetween(Point3d from, Point3d to)
    {
        return Math.Atan2(to.Y - from.Y, to.X - from.X);
    }
    
    /// <summary>
    /// Calcule l'angle entre deux points (en degrés)
    /// </summary>
    public static double AngleBetweenDegrees(Point3d from, Point3d to)
    {
        return AngleBetween(from, to) * RadToDeg;
    }
    
    /// <summary>
    /// Normalise un angle en radians dans l'intervalle [0, 2π].
    /// </summary>
    /// <param name="angle">Angle à normaliser (peut être négatif ou > 2π)</param>
    /// <returns>Angle normalisé dans [0, 2π]</returns>
    /// <example>
    /// <code>
    /// var normalized = GeometryService.NormalizeAngle(-Math.PI / 2);
    /// // Résultat: 3π/2 (environ 4.712)
    /// </code>
    /// </example>
    public static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 2 * Math.PI;
        while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
        return angle;
    }
    
    /// <summary>
    /// Normalise un angle en degrés dans l'intervalle [0, 360].
    /// </summary>
    public static double NormalizeAngleDegrees(double angleDegrees)
    {
        while (angleDegrees < 0) angleDegrees += 360;
        while (angleDegrees >= 360) angleDegrees -= 360;
        return angleDegrees;
    }
    
    /// <summary>
    /// Calcule l'angle entre deux vecteurs.
    /// </summary>
    /// <returns>Angle en radians [0, π]</returns>
    public static double AngleBetweenVectors(Vector3d v1, Vector3d v2)
    {
        double dot = v1.DotProduct(v2);
        double mag = v1.Length * v2.Length;
        if (mag < Tolerance) return 0;
        return Math.Acos(Math.Clamp(dot / mag, -1.0, 1.0));
    }
    
    /// <summary>
    /// Calcule le gisement (bearing) d'un vecteur par rapport au Nord (axe Y+).
    /// Le gisement est mesuré dans le sens horaire depuis le Nord.
    /// </summary>
    /// <returns>Gisement en grades [0, 400]</returns>
    public static double Bearing(Point3d from, Point3d to)
    {
        double angle = Math.Atan2(to.X - from.X, to.Y - from.Y);
        double grades = angle * 200.0 / Math.PI;
        if (grades < 0) grades += 400;
        return grades;
    }
    
    /// <summary>
    /// Convertit un gisement (grades) en angle trigonométrique (radians).
    /// </summary>
    public static double BearingToAngle(double bearingGrades)
    {
        return (100 - bearingGrades) * Math.PI / 200.0;
    }
    
    #endregion
    
    #region Points
    
    /// <summary>
    /// Calcule un point décalé à partir d'un point et d'un angle
    /// </summary>
    public static Point3d OffsetPoint(Point3d point, double angle, double distance)
    {
        return new Point3d(
            point.X + distance * Math.Cos(angle),
            point.Y + distance * Math.Sin(angle),
            point.Z
        );
    }
    
    /// <summary>
    /// Calcule un point décalé perpendiculairement
    /// </summary>
    /// <param name="point">Point de départ</param>
    /// <param name="angle">Angle de la direction principale</param>
    /// <param name="distance">Distance de décalage</param>
    /// <param name="leftSide">True pour décaler à gauche, false à droite</param>
    public static Point3d PerpendicularOffset(Point3d point, double angle, double distance, bool leftSide)
    {
        double perpAngle = leftSide ? angle + Math.PI / 2 : angle - Math.PI / 2;
        return OffsetPoint(point, perpAngle, distance);
    }
    
    /// <summary>
    /// Calcule le milieu entre deux points
    /// </summary>
    public static Point3d MidPoint(Point3d p1, Point3d p2)
    {
        return new Point3d(
            (p1.X + p2.X) / 2,
            (p1.Y + p2.Y) / 2,
            (p1.Z + p2.Z) / 2
        );
    }
    
    /// <summary>
    /// Interpole linéairement entre deux points
    /// </summary>
    /// <param name="p1">Point de départ</param>
    /// <param name="p2">Point d'arrivée</param>
    /// <param name="t">Paramètre d'interpolation (0 = p1, 1 = p2)</param>
    public static Point3d Lerp(Point3d p1, Point3d p2, double t)
    {
        return new Point3d(
            p1.X + (p2.X - p1.X) * t,
            p1.Y + (p2.Y - p1.Y) * t,
            p1.Z + (p2.Z - p1.Z) * t
        );
    }
    
    /// <summary>
    /// Projette un point sur une droite définie par deux points.
    /// </summary>
    /// <param name="point">Point à projeter</param>
    /// <param name="lineStart">Début de la ligne</param>
    /// <param name="lineEnd">Fin de la ligne</param>
    /// <returns>Point projeté sur la droite (peut être en dehors du segment)</returns>
    public static Point3d ProjectPointOnLine(Point3d point, Point3d lineStart, Point3d lineEnd)
    {
        Vector3d lineDir = lineEnd - lineStart;
        double lineLengthSq = lineDir.LengthSqrd;
        if (lineLengthSq < Tolerance) return lineStart;
        
        Vector3d toPoint = point - lineStart;
        double t = toPoint.DotProduct(lineDir) / lineLengthSq;
        
        return lineStart + t * lineDir;
    }
    
    /// <summary>
    /// Projette un point sur un segment (résultat contraint au segment).
    /// </summary>
    public static Point3d ProjectPointOnSegment(Point3d point, Point3d segStart, Point3d segEnd)
    {
        Vector3d segDir = segEnd - segStart;
        double segLengthSq = segDir.LengthSqrd;
        if (segLengthSq < Tolerance) return segStart;
        
        Vector3d toPoint = point - segStart;
        double t = Math.Clamp(toPoint.DotProduct(segDir) / segLengthSq, 0, 1);
        
        return segStart + t * segDir;
    }
    
    /// <summary>
    /// Calcule la distance d'un point à une droite.
    /// </summary>
    public static double DistancePointToLine(Point3d point, Point3d lineStart, Point3d lineEnd)
    {
        Point3d projected = ProjectPointOnLine(point, lineStart, lineEnd);
        return Distance(point, projected);
    }
    
    /// <summary>
    /// Calcule la distance d'un point à un segment.
    /// </summary>
    public static double DistancePointToSegment(Point3d point, Point3d segStart, Point3d segEnd)
    {
        Point3d projected = ProjectPointOnSegment(point, segStart, segEnd);
        return Distance(point, projected);
    }
    
    /// <summary>
    /// Fait pivoter un point autour d'un centre.
    /// </summary>
    /// <param name="point">Point à pivoter</param>
    /// <param name="center">Centre de rotation</param>
    /// <param name="angle">Angle de rotation en radians (sens trigonométrique)</param>
    public static Point3d RotatePoint(Point3d point, Point3d center, double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        double dx = point.X - center.X;
        double dy = point.Y - center.Y;
        
        return new Point3d(
            center.X + dx * cos - dy * sin,
            center.Y + dx * sin + dy * cos,
            point.Z
        );
    }
    
    /// <summary>
    /// Translate un point selon un vecteur.
    /// </summary>
    public static Point3d TranslatePoint(Point3d point, Vector3d vector)
    {
        return point + vector;
    }
    
    /// <summary>
    /// Translate un point selon des deltas X, Y, Z.
    /// </summary>
    public static Point3d TranslatePoint(Point3d point, double dx, double dy, double dz = 0)
    {
        return new Point3d(point.X + dx, point.Y + dy, point.Z + dz);
    }
    
    #endregion
    
    #region Polylignes
    
    /// <summary>
    /// Extrait tous les points d'une polyligne
    /// </summary>
    public static List<Point3d> GetPolylinePoints(Polyline polyline)
    {
        var points = new List<Point3d>();
        for (int i = 0; i < polyline.NumberOfVertices; i++)
        {
            points.Add(polyline.GetPoint3dAt(i));
        }
        return points;
    }
    
    /// <summary>
    /// Calcule la longueur totale d'une polyligne
    /// </summary>
    public static double GetPolylineLength(Polyline polyline)
    {
        return polyline.Length;
    }
    
    /// <summary>
    /// Obtient un point sur la polyligne à une distance donnée du départ
    /// </summary>
    public static Point3d GetPointAtDistance(Polyline polyline, double distance)
    {
        distance = Math.Max(0, Math.Min(distance, polyline.Length));
        return polyline.GetPointAtDist(distance);
    }
    
    /// <summary>
    /// Obtient l'angle tangent à un point sur la polyligne
    /// </summary>
    public static double GetTangentAngle(Polyline polyline, double distance)
    {
        distance = Math.Max(0.001, Math.Min(distance, polyline.Length - 0.001));
        var point = polyline.GetPointAtDist(distance);
        var deriv = polyline.GetFirstDerivative(point);
        return Math.Atan2(deriv.Y, deriv.X);
    }
    
    #endregion
    
    #region Tests Géométriques
    
    /// <summary>
    /// Détermine de quel côté d'une ligne se trouve un point
    /// </summary>
    /// <returns>True si le point est à gauche de la ligne</returns>
    public static bool IsPointOnLeftSide(Point3d lineStart, Point3d lineEnd, Point3d point)
    {
        double cross = (lineEnd.X - lineStart.X) * (point.Y - lineStart.Y) -
                       (lineEnd.Y - lineStart.Y) * (point.X - lineStart.X);
        return cross > 0;
    }
    
    /// <summary>
    /// Vérifie si un point est à l'intérieur d'un polygone.
    /// Utilise l'algorithme de ray-casting (trace un rayon horizontal vers la droite).
    /// </summary>
    /// <param name="point">Point à tester</param>
    /// <param name="polygon">Liste des sommets du polygone</param>
    /// <returns>True si le point est à l'intérieur du polygone</returns>
    /// <remarks>
    /// Les segments horizontaux (même coordonnée Y aux deux extrémités) sont ignorés
    /// car un rayon horizontal ne peut pas les intersecter de manière non-dégénérée.
    /// Cela évite également les divisions par zéro.
    /// </remarks>
    public static bool IsPointInPolygon(Point3d point, IList<Point3d> polygon)
    {
        if (polygon.Count < 3) return false;
        
        bool inside = false;
        int j = polygon.Count - 1;
        
        for (int i = 0; i < polygon.Count; i++)
        {
            double yi = polygon[i].Y;
            double yj = polygon[j].Y;
            
            // Ignorer les segments horizontaux (dy == 0)
            // Un rayon horizontal ne peut pas les traverser de manière significative
            double dy = yj - yi;
            if (Math.Abs(dy) < 1e-10)
            {
                j = i;
                continue;
            }
            
            // Vérifier si le rayon horizontal depuis le point traverse ce segment
            if ((yi < point.Y && yj >= point.Y) || (yj < point.Y && yi >= point.Y))
            {
                // Calculer l'abscisse de l'intersection du rayon avec le segment
                double xi = polygon[i].X;
                double xj = polygon[j].X;
                double xIntersect = xi + (point.Y - yi) / dy * (xj - xi);
                
                if (xIntersect < point.X)
                {
                    inside = !inside;
                }
            }
            j = i;
        }
        
        return inside;
    }
    
    #endregion
    
    #region Aires et Périmètres
    
    /// <summary>
    /// Calcule l'aire d'un polygone (formule du lacet/shoelace)
    /// </summary>
    public static double CalculatePolygonArea(IList<Point3d> points)
    {
        if (points.Count < 3) return 0;
        
        double area = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var j = (i + 1) % points.Count;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        return Math.Abs(area / 2);
    }
    
    /// <summary>
    /// Calcule le périmètre d'un polygone
    /// </summary>
    public static double CalculatePolygonPerimeter(IList<Point3d> points)
    {
        if (points.Count < 2) return 0;
        
        double perimeter = 0;
        for (int i = 0; i < points.Count; i++)
        {
            var j = (i + 1) % points.Count;
            perimeter += Distance(points[i], points[j]);
        }
        return perimeter;
    }
    
    /// <summary>
    /// Calcule le centroïde d'un polygone
    /// </summary>
    public static Point3d CalculateCentroid(IList<Point3d> points)
    {
        if (points.Count == 0) return Point3d.Origin;
        if (points.Count == 1) return points[0];
        
        double cx = 0, cy = 0, cz = 0;
        double area = 0;
        
        for (int i = 0; i < points.Count; i++)
        {
            var j = (i + 1) % points.Count;
            double factor = points[i].X * points[j].Y - points[j].X * points[i].Y;
            area += factor;
            cx += (points[i].X + points[j].X) * factor;
            cy += (points[i].Y + points[j].Y) * factor;
            cz += points[i].Z;
        }
        
        area /= 2;
        if (Math.Abs(area) < 1e-10)
        {
            // Dégénéré, retourner moyenne simple
            return new Point3d(
                points.Average(p => p.X),
                points.Average(p => p.Y),
                points.Average(p => p.Z)
            );
        }
        
        cx /= (6 * area);
        cy /= (6 * area);
        cz /= points.Count;
        
        return new Point3d(cx, cy, cz);
    }
    
    #endregion
    
    #region Intersections
    
    /// <summary>
    /// Résultat d'une intersection entre deux lignes.
    /// </summary>
    public readonly struct LineIntersectionResult
    {
        public bool HasIntersection { get; init; }
        public Point3d Point { get; init; }
        public double T1 { get; init; } // Paramètre sur la première ligne [0,1] si sur segment
        public double T2 { get; init; } // Paramètre sur la deuxième ligne [0,1] si sur segment
        public bool IsOnSegment1 => T1 >= 0 && T1 <= 1;
        public bool IsOnSegment2 => T2 >= 0 && T2 <= 1;
        public bool IsOnBothSegments => IsOnSegment1 && IsOnSegment2;
    }
    
    /// <summary>
    /// Calcule l'intersection de deux droites (infinies).
    /// </summary>
    public static LineIntersectionResult IntersectLines(
        Point3d line1Start, Point3d line1End,
        Point3d line2Start, Point3d line2End)
    {
        double x1 = line1Start.X, y1 = line1Start.Y;
        double x2 = line1End.X, y2 = line1End.Y;
        double x3 = line2Start.X, y3 = line2Start.Y;
        double x4 = line2End.X, y4 = line2End.Y;
        
        double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
        
        if (Math.Abs(denom) < Tolerance)
        {
            return new LineIntersectionResult { HasIntersection = false };
        }
        
        double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
        double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;
        
        double ix = x1 + t * (x2 - x1);
        double iy = y1 + t * (y2 - y1);
        double iz = line1Start.Z + t * (line1End.Z - line1Start.Z);
        
        return new LineIntersectionResult
        {
            HasIntersection = true,
            Point = new Point3d(ix, iy, iz),
            T1 = t,
            T2 = u
        };
    }
    
    /// <summary>
    /// Calcule l'intersection de deux segments (retourne null si pas d'intersection sur les segments).
    /// </summary>
    public static Point3d? IntersectSegments(
        Point3d seg1Start, Point3d seg1End,
        Point3d seg2Start, Point3d seg2End)
    {
        var result = IntersectLines(seg1Start, seg1End, seg2Start, seg2End);
        if (result.HasIntersection && result.IsOnBothSegments)
        {
            return result.Point;
        }
        return null;
    }
    
    /// <summary>
    /// Résultat d'une intersection ligne/cercle.
    /// </summary>
    public readonly struct LineCircleIntersectionResult
    {
        public int Count { get; init; } // 0, 1 (tangent), ou 2
        public Point3d? Point1 { get; init; }
        public Point3d? Point2 { get; init; }
    }
    
    /// <summary>
    /// Calcule les intersections entre une droite et un cercle.
    /// </summary>
    /// <param name="lineStart">Point de départ de la ligne</param>
    /// <param name="lineEnd">Point de fin de la ligne</param>
    /// <param name="center">Centre du cercle</param>
    /// <param name="radius">Rayon du cercle</param>
    public static LineCircleIntersectionResult IntersectLineCircle(
        Point3d lineStart, Point3d lineEnd,
        Point3d center, double radius)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        double fx = lineStart.X - center.X;
        double fy = lineStart.Y - center.Y;
        
        double a = dx * dx + dy * dy;
        double b = 2 * (fx * dx + fy * dy);
        double c = fx * fx + fy * fy - radius * radius;
        
        double discriminant = b * b - 4 * a * c;
        
        if (discriminant < -Tolerance)
        {
            return new LineCircleIntersectionResult { Count = 0 };
        }
        
        if (Math.Abs(discriminant) < Tolerance)
        {
            double t = -b / (2 * a);
            return new LineCircleIntersectionResult
            {
                Count = 1,
                Point1 = new Point3d(lineStart.X + t * dx, lineStart.Y + t * dy, lineStart.Z)
            };
        }
        
        double sqrtD = Math.Sqrt(discriminant);
        double t1 = (-b - sqrtD) / (2 * a);
        double t2 = (-b + sqrtD) / (2 * a);
        
        return new LineCircleIntersectionResult
        {
            Count = 2,
            Point1 = new Point3d(lineStart.X + t1 * dx, lineStart.Y + t1 * dy, lineStart.Z),
            Point2 = new Point3d(lineStart.X + t2 * dx, lineStart.Y + t2 * dy, lineStart.Z)
        };
    }
    
    /// <summary>
    /// Résultat d'une intersection cercle/cercle.
    /// </summary>
    public readonly struct CircleCircleIntersectionResult
    {
        public int Count { get; init; } // 0, 1 (tangent), 2, ou -1 (identiques)
        public Point3d? Point1 { get; init; }
        public Point3d? Point2 { get; init; }
    }
    
    /// <summary>
    /// Calcule les intersections entre deux cercles.
    /// </summary>
    public static CircleCircleIntersectionResult IntersectCircles(
        Point3d center1, double radius1,
        Point3d center2, double radius2)
    {
        double d = Distance2D(center1, center2);
        
        // Cercles identiques
        if (d < Tolerance && Math.Abs(radius1 - radius2) < Tolerance)
        {
            return new CircleCircleIntersectionResult { Count = -1 };
        }
        
        // Trop éloignés ou l'un contient l'autre
        if (d > radius1 + radius2 + Tolerance || d < Math.Abs(radius1 - radius2) - Tolerance)
        {
            return new CircleCircleIntersectionResult { Count = 0 };
        }
        
        double a = (radius1 * radius1 - radius2 * radius2 + d * d) / (2 * d);
        double h2 = radius1 * radius1 - a * a;
        
        // Point de base sur la ligne entre les centres
        double px = center1.X + a * (center2.X - center1.X) / d;
        double py = center1.Y + a * (center2.Y - center1.Y) / d;
        
        // Tangent (un seul point)
        if (h2 < Tolerance)
        {
            return new CircleCircleIntersectionResult
            {
                Count = 1,
                Point1 = new Point3d(px, py, center1.Z)
            };
        }
        
        double h = Math.Sqrt(h2);
        double rx = -h * (center2.Y - center1.Y) / d;
        double ry = h * (center2.X - center1.X) / d;
        
        return new CircleCircleIntersectionResult
        {
            Count = 2,
            Point1 = new Point3d(px + rx, py + ry, center1.Z),
            Point2 = new Point3d(px - rx, py - ry, center1.Z)
        };
    }
    
    /// <summary>
    /// Calcule les points de tangence d'un point externe à un cercle.
    /// </summary>
    /// <returns>Tuple des deux points de tangence, ou null si le point est dans le cercle</returns>
    public static (Point3d, Point3d)? TangentPointsFromExternalPoint(Point3d externalPoint, Point3d center, double radius)
    {
        double d = Distance2D(externalPoint, center);
        if (d <= radius) return null;
        
        double angle = AngleBetween(center, externalPoint);
        double tangentAngle = Math.Acos(radius / d);
        
        Point3d t1 = OffsetPoint(center, angle + tangentAngle, radius);
        Point3d t2 = OffsetPoint(center, angle - tangentAngle, radius);
        
        return (t1, t2);
    }
    
    #endregion
    
    #region Cercles et Arcs
    
    /// <summary>
    /// Calcule le cercle passant par trois points.
    /// </summary>
    /// <returns>Tuple (centre, rayon) ou null si points colinéaires</returns>
    public static (Point3d Center, double Radius)? CircleFrom3Points(Point3d p1, Point3d p2, Point3d p3)
    {
        double ax = p1.X, ay = p1.Y;
        double bx = p2.X, by = p2.Y;
        double cx = p3.X, cy = p3.Y;
        
        double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Math.Abs(d) < Tolerance) return null; // Colinéaires
        
        double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
        double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
        
        Point3d center = new Point3d(ux, uy, p1.Z);
        double radius = Distance2D(center, p1);
        
        return (center, radius);
    }
    
    /// <summary>
    /// Calcule la longueur d'un arc de cercle.
    /// </summary>
    /// <param name="radius">Rayon de l'arc</param>
    /// <param name="angleRadians">Angle au centre en radians</param>
    public static double ArcLength(double radius, double angleRadians)
    {
        return radius * Math.Abs(angleRadians);
    }
    
    /// <summary>
    /// Calcule l'aire d'un secteur circulaire.
    /// </summary>
    public static double SectorArea(double radius, double angleRadians)
    {
        return 0.5 * radius * radius * Math.Abs(angleRadians);
    }
    
    /// <summary>
    /// Calcule l'aire d'un segment circulaire (partie entre corde et arc).
    /// </summary>
    public static double CircularSegmentArea(double radius, double angleRadians)
    {
        return 0.5 * radius * radius * (Math.Abs(angleRadians) - Math.Sin(Math.Abs(angleRadians)));
    }
    
    /// <summary>
    /// Calcule la longueur de la corde d'un arc.
    /// </summary>
    public static double ChordLength(double radius, double angleRadians)
    {
        return 2 * radius * Math.Sin(Math.Abs(angleRadians) / 2);
    }
    
    /// <summary>
    /// Calcule la flèche (sagita) d'un arc.
    /// </summary>
    public static double Sagita(double radius, double angleRadians)
    {
        return radius * (1 - Math.Cos(Math.Abs(angleRadians) / 2));
    }
    
    #endregion
    
    #region Voirie - Tracé en plan
    
    /// <summary>
    /// Calcule les paramètres d'une clothoïde (spirale de Cornu / transition de courbure).
    /// </summary>
    /// <param name="A">Paramètre de la clothoïde (A² = R × L)</param>
    /// <param name="L">Longueur développée sur la clothoïde</param>
    /// <returns>Tuple avec les coordonnées (X, Y) et l'angle de rotation τ</returns>
    /// <remarks>
    /// La clothoïde est la courbe de transition idéale pour les routes car sa courbure
    /// varie linéairement avec la longueur parcourue.
    /// </remarks>
    public static (double X, double Y, double Tau) ClothoidCoordinates(double A, double L)
    {
        if (A < Tolerance) return (0, 0, 0);
        
        double tau = L * L / (2 * A * A); // Angle de rotation
        
        // Développement en série de Fresnel (5 termes)
        double x = L * (1 - tau * tau / 10 + tau * tau * tau * tau / 216);
        double y = L * (tau / 3 - tau * tau * tau / 42 + tau * tau * tau * tau * tau / 1320);
        
        return (x, y, tau);
    }
    
    /// <summary>
    /// Calcule le paramètre A de la clothoïde à partir du rayon et de la longueur.
    /// </summary>
    /// <param name="radius">Rayon de courbure à l'extrémité</param>
    /// <param name="length">Longueur de la clothoïde</param>
    /// <returns>Paramètre A (A² = R × L)</returns>
    public static double ClothoidParameter(double radius, double length)
    {
        return Math.Sqrt(radius * length);
    }
    
    /// <summary>
    /// Calcule la longueur minimale de transition (clothoïde) selon la règle du confort.
    /// </summary>
    /// <param name="radius">Rayon de la courbe circulaire</param>
    /// <param name="speedKmh">Vitesse de référence en km/h</param>
    /// <returns>Longueur minimale en mètres</returns>
    public static double MinClothoidLength(double radius, double speedKmh)
    {
        // Règle: L = V³ / (46.656 × R) où V en km/h et R en m
        return speedKmh * speedKmh * speedKmh / (46.656 * radius);
    }
    
    /// <summary>
    /// Calcule le rayon minimum en fonction de la vitesse et du dévers.
    /// </summary>
    /// <param name="speedKmh">Vitesse de référence en km/h</param>
    /// <param name="superelevationPercent">Dévers en % (ex: 7 pour 7%)</param>
    /// <param name="frictionCoef">Coefficient de frottement latéral (défaut: 0.13)</param>
    /// <returns>Rayon minimum en mètres</returns>
    public static double MinCurveRadius(double speedKmh, double superelevationPercent, double frictionCoef = 0.13)
    {
        double v = speedKmh / 3.6; // Conversion en m/s
        double e = superelevationPercent / 100.0;
        return v * v / (Gravity * (e + frictionCoef));
    }
    
    /// <summary>
    /// Calcule le dévers recommandé pour un rayon donné.
    /// </summary>
    /// <param name="radius">Rayon de la courbe en mètres</param>
    /// <param name="speedKmh">Vitesse de référence en km/h</param>
    /// <param name="maxSuperelevation">Dévers maximum autorisé en % (défaut: 7%)</param>
    /// <returns>Dévers en %</returns>
    public static double RecommendedSuperelevation(double radius, double speedKmh, double maxSuperelevation = 7.0)
    {
        double v = speedKmh / 3.6;
        double ideal = (v * v / (Gravity * radius)) * 100;
        return Math.Min(ideal, maxSuperelevation);
    }
    
    /// <summary>
    /// Calcule la surlargeur nécessaire en courbe pour un véhicule type.
    /// </summary>
    /// <param name="radius">Rayon de la courbe en mètres</param>
    /// <param name="vehicleLength">Longueur du véhicule en mètres (défaut: 12m PL)</param>
    /// <returns>Surlargeur en mètres</returns>
    public static double CurveWidening(double radius, double vehicleLength = 12.0)
    {
        if (radius < Tolerance) return 0;
        // Formule classique: S = L² / (2R)
        return vehicleLength * vehicleLength / (2 * radius);
    }
    
    /// <summary>
    /// Calcule la distance de visibilité d'arrêt.
    /// </summary>
    /// <param name="speedKmh">Vitesse en km/h</param>
    /// <param name="reactionTime">Temps de réaction en secondes (défaut: 2s)</param>
    /// <param name="frictionCoef">Coefficient de frottement (défaut: 0.35)</param>
    /// <param name="slopePercent">Pente en % (positive = montée)</param>
    /// <returns>Distance de visibilité d'arrêt en mètres</returns>
    public static double StoppingDistance(double speedKmh, double reactionTime = 2.0, 
        double frictionCoef = 0.35, double slopePercent = 0)
    {
        double v = speedKmh / 3.6;
        double slope = slopePercent / 100.0;
        
        // Distance de réaction
        double dr = v * reactionTime;
        
        // Distance de freinage
        double df = v * v / (2 * Gravity * (frictionCoef + slope));
        
        return dr + df;
    }
    
    /// <summary>
    /// Calcule la distance de visibilité de dépassement.
    /// </summary>
    /// <param name="speedKmh">Vitesse en km/h</param>
    /// <returns>Distance de dépassement en mètres</returns>
    public static double OvertakingDistance(double speedKmh)
    {
        // Formule empirique simplifiée
        return 6 * speedKmh;
    }
    
    #endregion
    
    #region Voirie - Profil en long
    
    /// <summary>
    /// Calcule la pente entre deux points (en %).
    /// </summary>
    /// <param name="p1">Point de départ</param>
    /// <param name="p2">Point d'arrivée</param>
    /// <returns>Pente en % (positive = montée)</returns>
    public static double SlopePercent(Point3d p1, Point3d p2)
    {
        double dx = Distance2D(p1, p2);
        if (dx < Tolerance) return 0;
        return (p2.Z - p1.Z) / dx * 100;
    }
    
    /// <summary>
    /// Calcule la pente en pour mille (‰).
    /// </summary>
    public static double SlopePerMille(Point3d p1, Point3d p2)
    {
        return SlopePercent(p1, p2) * 10;
    }
    
    /// <summary>
    /// Calcule les paramètres d'un raccordement parabolique vertical.
    /// </summary>
    /// <param name="slope1">Pente d'entrée en %</param>
    /// <param name="slope2">Pente de sortie en %</param>
    /// <param name="length">Longueur du raccordement en mètres</param>
    /// <returns>Paramètres du raccordement (rayon, flèche, type)</returns>
    public static (double Radius, double Deflection, bool IsCrest) VerticalCurveParameters(
        double slope1, double slope2, double length)
    {
        double deltaSlope = (slope2 - slope1) / 100; // En décimal
        bool isCrest = slope1 > slope2; // Point haut si pente décroît
        
        double radius = length / Math.Abs(deltaSlope);
        double deflection = length * Math.Abs(deltaSlope) / 8; // Flèche au milieu
        
        return (radius, deflection, isCrest);
    }
    
    /// <summary>
    /// Calcule la longueur minimale d'un raccordement convexe (point haut).
    /// </summary>
    /// <param name="slope1">Pente d'entrée en %</param>
    /// <param name="slope2">Pente de sortie en %</param>
    /// <param name="stoppingDistance">Distance d'arrêt en mètres</param>
    /// <param name="eyeHeight">Hauteur de l'œil du conducteur (défaut: 1.10m)</param>
    /// <param name="objectHeight">Hauteur de l'objet à voir (défaut: 0.15m)</param>
    public static double MinCrestCurveLength(double slope1, double slope2, double stoppingDistance,
        double eyeHeight = 1.10, double objectHeight = 0.15)
    {
        double A = Math.Abs(slope1 - slope2);
        double num = A * stoppingDistance * stoppingDistance;
        double denom = 200 * (Math.Sqrt(eyeHeight) + Math.Sqrt(objectHeight));
        double denom2 = denom * denom;
        return num / denom2;
    }
    
    /// <summary>
    /// Calcule la longueur minimale d'un raccordement concave (point bas).
    /// </summary>
    /// <param name="slope1">Pente d'entrée en %</param>
    /// <param name="slope2">Pente de sortie en %</param>
    /// <param name="stoppingDistance">Distance d'arrêt en mètres</param>
    /// <param name="headlightHeight">Hauteur des phares (défaut: 0.60m)</param>
    /// <param name="headlightAngle">Angle des phares en degrés (défaut: 1°)</param>
    public static double MinSagCurveLength(double slope1, double slope2, double stoppingDistance,
        double headlightHeight = 0.60, double headlightAngle = 1.0)
    {
        double A = Math.Abs(slope1 - slope2);
        double tanAngle = Math.Tan(headlightAngle * DegToRad);
        return A * stoppingDistance * stoppingDistance / (200 * (headlightHeight + stoppingDistance * tanAngle));
    }
    
    /// <summary>
    /// Calcule l'altitude sur une courbe parabolique verticale.
    /// </summary>
    /// <param name="startZ">Altitude au début du raccordement</param>
    /// <param name="startSlope">Pente au début en %</param>
    /// <param name="curveLength">Longueur totale du raccordement</param>
    /// <param name="position">Position sur le raccordement (0 à curveLength)</param>
    /// <param name="endSlope">Pente à la fin en %</param>
    public static double VerticalCurveElevation(double startZ, double startSlope, double curveLength,
        double position, double endSlope)
    {
        double i1 = startSlope / 100;
        double i2 = endSlope / 100;
        double r = (i2 - i1) / curveLength;
        
        return startZ + i1 * position + 0.5 * r * position * position;
    }
    
    #endregion
    
    #region Assainissement - Hydraulique
    
    /// <summary>
    /// Calcule le débit par la formule de Manning-Strickler.
    /// Q = K × S × Rh^(2/3) × I^(1/2)
    /// </summary>
    /// <param name="stricklerK">Coefficient de Strickler (ex: 70 pour béton lisse)</param>
    /// <param name="section">Section mouillée en m²</param>
    /// <param name="hydraulicRadius">Rayon hydraulique en m (S/Pm)</param>
    /// <param name="slopeDecimal">Pente en décimal (ex: 0.01 pour 1%)</param>
    /// <returns>Débit en m³/s</returns>
    public static double ManningStricklerFlow(double stricklerK, double section, 
        double hydraulicRadius, double slopeDecimal)
    {
        return stricklerK * section * Math.Pow(hydraulicRadius, 2.0 / 3.0) * Math.Sqrt(slopeDecimal);
    }
    
    /// <summary>
    /// Calcule la vitesse d'écoulement par Manning-Strickler.
    /// V = K × Rh^(2/3) × I^(1/2)
    /// </summary>
    public static double ManningStricklerVelocity(double stricklerK, double hydraulicRadius, double slopeDecimal)
    {
        return stricklerK * Math.Pow(hydraulicRadius, 2.0 / 3.0) * Math.Sqrt(slopeDecimal);
    }
    
    /// <summary>
    /// Calcule le rayon hydraulique.
    /// Rh = Section mouillée / Périmètre mouillé
    /// </summary>
    public static double HydraulicRadius(double wettedArea, double wettedPerimeter)
    {
        if (wettedPerimeter < Tolerance) return 0;
        return wettedArea / wettedPerimeter;
    }
    
    /// <summary>
    /// Paramètres hydrauliques d'une section circulaire partiellement remplie.
    /// </summary>
    /// <param name="diameter">Diamètre de la canalisation en mètres</param>
    /// <param name="fillRatio">Taux de remplissage (0 à 1, ex: 0.8 pour 80%)</param>
    /// <returns>Section mouillée, périmètre mouillé, rayon hydraulique</returns>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius) 
        CircularPipeHydraulics(double diameter, double fillRatio)
    {
        if (fillRatio <= 0) return (0, 0, 0);
        if (fillRatio >= 1) 
        {
            double fullArea = Math.PI * diameter * diameter / 4;
            double fullPerimeter = Math.PI * diameter;
            return (fullArea, fullPerimeter, diameter / 4);
        }
        
        double r = diameter / 2;
        double h = fillRatio * diameter; // Hauteur d'eau
        
        // Angle au centre (en radians)
        double theta = 2 * Math.Acos(1 - h / r);
        
        // Section mouillée
        double area = r * r * (theta - Math.Sin(theta)) / 2;
        
        // Périmètre mouillé
        double perimeter = r * theta;
        
        // Rayon hydraulique
        double rh = area / perimeter;
        
        return (area, perimeter, rh);
    }
    
    /// <summary>
    /// Calcule le débit à pleine section pour une canalisation circulaire.
    /// </summary>
    /// <param name="diameter">Diamètre en mètres</param>
    /// <param name="slopePercent">Pente en %</param>
    /// <param name="stricklerK">Coefficient de Strickler (défaut: 70 béton)</param>
    public static double FullPipeFlow(double diameter, double slopePercent, double stricklerK = 70)
    {
        double area = Math.PI * diameter * diameter / 4;
        double rh = diameter / 4;
        double slope = slopePercent / 100;
        return ManningStricklerFlow(stricklerK, area, rh, slope);
    }
    
    /// <summary>
    /// Calcule le diamètre nécessaire pour un débit donné (à pleine section).
    /// </summary>
    /// <param name="flowRate">Débit en m³/s</param>
    /// <param name="slopePercent">Pente en %</param>
    /// <param name="stricklerK">Coefficient de Strickler (défaut: 70)</param>
    /// <returns>Diamètre minimum en mètres</returns>
    public static double RequiredPipeDiameter(double flowRate, double slopePercent, double stricklerK = 70)
    {
        double slope = slopePercent / 100;
        // D = (Q × 4^(5/3) / (K × π × √I))^(3/8)
        double num = flowRate * Math.Pow(4, 5.0 / 3.0);
        double denom = stricklerK * Math.PI * Math.Sqrt(slope);
        return Math.Pow(num / denom, 3.0 / 8.0);
    }
    
    /// <summary>
    /// Calcule la pente critique pour éviter les dépôts (auto-curage).
    /// </summary>
    /// <param name="diameter">Diamètre en mètres</param>
    /// <param name="minVelocity">Vitesse minimale d'auto-curage (défaut: 0.60 m/s)</param>
    /// <param name="stricklerK">Coefficient de Strickler (défaut: 70)</param>
    /// <returns>Pente minimale en %</returns>
    public static double SelfCleaningSlope(double diameter, double minVelocity = 0.60, double stricklerK = 70)
    {
        double rh = diameter / 4;
        // I = (V / (K × Rh^(2/3)))²
        double slope = Math.Pow(minVelocity / (stricklerK * Math.Pow(rh, 2.0 / 3.0)), 2);
        return slope * 100; // En %
    }
    
    /// <summary>
    /// Coefficients de Strickler courants.
    /// </summary>
    public static class StricklerCoefficients
    {
        public const double BetonLisse = 80;
        public const double BetonCentrifuge = 90;
        public const double BetonOrdinary = 70;
        public const double BetonRuqueux = 60;
        public const double Gres = 75;
        public const double FonteDuctile = 80;
        public const double PVCNeuf = 100;
        public const double PVCUsage = 90;
        public const double PEHD = 100;
        public const double Acier = 85;
        public const double FosseEnTerre = 35;
        public const double FosseEnherbe = 25;
        public const double Enrochement = 30;
    }
    
    /// <summary>
    /// Calcule la hauteur de chute dans un regard.
    /// </summary>
    /// <param name="upstreamInvert">Cote radier amont</param>
    /// <param name="downstreamInvert">Cote radier aval</param>
    /// <returns>Hauteur de chute en mètres</returns>
    public static double ManholeDrop(double upstreamInvert, double downstreamInvert)
    {
        return Math.Max(0, upstreamInvert - downstreamInvert);
    }
    
    /// <summary>
    /// Détermine si une chute nécessite un dispositif de dissipation d'énergie.
    /// </summary>
    /// <param name="dropHeight">Hauteur de chute en mètres</param>
    /// <param name="threshold">Seuil au-delà duquel une dissipation est nécessaire (défaut: 0.80m)</param>
    public static bool RequiresEnergyDissipation(double dropHeight, double threshold = 0.80)
    {
        return dropHeight > threshold;
    }
    
    /// <summary>
    /// Paramètres hydrauliques d'une section ovoïde (T150).
    /// </summary>
    /// <param name="height">Hauteur nominale de l'ovoïde en mètres</param>
    /// <param name="fillRatio">Taux de remplissage (0 à 1)</param>
    /// <returns>Section mouillée, périmètre mouillé, rayon hydraulique</returns>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        OvoidPipeHydraulics(double height, double fillRatio)
    {
        // Ovoïde normalisé T150: largeur = 2/3 × hauteur
        double width = height * 2.0 / 3.0;
        
        // Approximation pour un remplissage donné
        // Section totale approximée
        double fullArea = 0.510 * height * height; // Coefficient pour ovoïde T150
        double fullPerimeter = 2.64 * height;
        
        if (fillRatio <= 0) return (0, 0, 0);
        if (fillRatio >= 1) return (fullArea, fullPerimeter, fullArea / fullPerimeter);
        
        // Interpolation simplifiée (à affiner selon tables)
        double area = fullArea * fillRatio * (2 - fillRatio);
        double perimeter = fullPerimeter * Math.Sqrt(fillRatio);
        double rh = perimeter > 0 ? area / perimeter : 0;
        
        return (area, perimeter, rh);
    }
    
    /// <summary>
    /// Paramètres hydrauliques d'une section rectangulaire (cadre ou dalot).
    /// </summary>
    /// <param name="width">Largeur en mètres</param>
    /// <param name="height">Hauteur totale en mètres</param>
    /// <param name="waterDepth">Hauteur d'eau en mètres</param>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        RectangularChannelHydraulics(double width, double height, double waterDepth)
    {
        double h = Math.Min(waterDepth, height);
        if (h <= 0) return (0, 0, 0);
        
        double area = width * h;
        double perimeter = width + 2 * h;
        double rh = area / perimeter;
        
        return (area, perimeter, rh);
    }
    
    /// <summary>
    /// Paramètres hydrauliques d'une section trapézoïdale (fossé).
    /// </summary>
    /// <param name="bottomWidth">Largeur au fond en mètres</param>
    /// <param name="waterDepth">Hauteur d'eau en mètres</param>
    /// <param name="sideSlope">Fruit des talus (horizontal/vertical, ex: 1 pour 1/1)</param>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        TrapezoidalChannelHydraulics(double bottomWidth, double waterDepth, double sideSlope)
    {
        if (waterDepth <= 0) return (0, 0, 0);
        
        double topWidth = bottomWidth + 2 * sideSlope * waterDepth;
        double area = (bottomWidth + topWidth) * waterDepth / 2;
        double sideLength = waterDepth * Math.Sqrt(1 + sideSlope * sideSlope);
        double perimeter = bottomWidth + 2 * sideLength;
        double rh = area / perimeter;
        
        return (area, perimeter, rh);
    }
    
    #endregion
    
    #region Cubature et Terrassement
    
    /// <summary>
    /// Calcule l'aire d'un profil en travers par la méthode des trapèzes.
    /// Les points doivent être ordonnés (gauche à droite ou inversement).
    /// </summary>
    /// <param name="profilePoints">Points du profil en travers (X = distance à l'axe, Z = altitude)</param>
    /// <param name="referenceLevel">Niveau de référence (ex: niveau projet)</param>
    /// <returns>Tuple (aire déblai, aire remblai)</returns>
    public static (double CutArea, double FillArea) CrossSectionAreas(
        IList<Point3d> profilePoints, double referenceLevel)
    {
        if (profilePoints.Count < 2) return (0, 0);
        
        double cutArea = 0;
        double fillArea = 0;
        
        for (int i = 0; i < profilePoints.Count - 1; i++)
        {
            double x1 = profilePoints[i].X;
            double z1 = profilePoints[i].Z;
            double x2 = profilePoints[i + 1].X;
            double z2 = profilePoints[i + 1].Z;
            
            double width = Math.Abs(x2 - x1);
            double h1 = z1 - referenceLevel;
            double h2 = z2 - referenceLevel;
            
            // Trapèze entièrement au-dessus (déblai)
            if (h1 >= 0 && h2 >= 0)
            {
                cutArea += (h1 + h2) * width / 2;
            }
            // Trapèze entièrement en-dessous (remblai)
            else if (h1 <= 0 && h2 <= 0)
            {
                fillArea += (Math.Abs(h1) + Math.Abs(h2)) * width / 2;
            }
            // Trapèze mixte - calcul du point d'intersection
            else
            {
                double xIntersect = Math.Abs(h1) / (Math.Abs(h1) + Math.Abs(h2)) * width;
                
                if (h1 > 0)
                {
                    cutArea += h1 * xIntersect / 2;
                    fillArea += Math.Abs(h2) * (width - xIntersect) / 2;
                }
                else
                {
                    fillArea += Math.Abs(h1) * xIntersect / 2;
                    cutArea += h2 * (width - xIntersect) / 2;
                }
            }
        }
        
        return (cutArea, fillArea);
    }
    
    /// <summary>
    /// Calcule le volume entre deux profils en travers par la méthode de la moyenne des aires.
    /// </summary>
    /// <param name="area1">Aire du premier profil en m²</param>
    /// <param name="area2">Aire du deuxième profil en m²</param>
    /// <param name="distance">Distance entre les deux profils en mètres</param>
    public static double VolumeByAverageEndArea(double area1, double area2, double distance)
    {
        return (area1 + area2) * distance / 2;
    }
    
    /// <summary>
    /// Calcule le volume entre deux profils par la formule prismoïdale (plus précis).
    /// </summary>
    /// <param name="area1">Aire du premier profil en m²</param>
    /// <param name="areaMiddle">Aire du profil médian en m²</param>
    /// <param name="area2">Aire du deuxième profil en m²</param>
    /// <param name="distance">Distance entre les profils extrêmes en mètres</param>
    public static double VolumeByPrismoidal(double area1, double areaMiddle, double area2, double distance)
    {
        return (area1 + 4 * areaMiddle + area2) * distance / 6;
    }
    
    /// <summary>
    /// Calcule le volume total de terrassement à partir d'une série de profils.
    /// </summary>
    /// <param name="sections">Liste de tuples (pk, aire déblai, aire remblai)</param>
    /// <returns>Tuple (volume déblai total, volume remblai total)</returns>
    public static (double CutVolume, double FillVolume) TotalEarthworkVolumes(
        IList<(double Pk, double CutArea, double FillArea)> sections)
    {
        if (sections.Count < 2) return (0, 0);
        
        double totalCut = 0;
        double totalFill = 0;
        
        for (int i = 0; i < sections.Count - 1; i++)
        {
            double dist = Math.Abs(sections[i + 1].Pk - sections[i].Pk);
            totalCut += VolumeByAverageEndArea(sections[i].CutArea, sections[i + 1].CutArea, dist);
            totalFill += VolumeByAverageEndArea(sections[i].FillArea, sections[i + 1].FillArea, dist);
        }
        
        return (totalCut, totalFill);
    }
    
    /// <summary>
    /// Calcule le coefficient de foisonnement/compactage.
    /// </summary>
    /// <param name="volumeInPlace">Volume en place (terrain naturel)</param>
    /// <param name="volumeLoose">Volume foisonné (transport)</param>
    public static double BulkingFactor(double volumeInPlace, double volumeLoose)
    {
        if (volumeInPlace < Tolerance) return 1;
        return volumeLoose / volumeInPlace;
    }
    
    /// <summary>
    /// Coefficients de foisonnement courants.
    /// </summary>
    public static class BulkingFactors
    {
        public const double TerreVegetale = 1.25;
        public const double Argile = 1.30;
        public const double Sable = 1.10;
        public const double Gravier = 1.15;
        public const double RocheFragmentee = 1.50;
        public const double RocheMassive = 1.65;
        public const double Enrobes = 1.30;
    }
    
    /// <summary>
    /// Applique le coefficient de foisonnement à un volume en place.
    /// </summary>
    public static double ApplyBulking(double volumeInPlace, double bulkingFactor)
    {
        return volumeInPlace * bulkingFactor;
    }
    
    /// <summary>
    /// Calcule le volume de remblai compacté nécessaire à partir du volume en place.
    /// </summary>
    /// <param name="volumeLoose">Volume foisonné disponible</param>
    /// <param name="compactionRatio">Taux de compactage (ex: 0.90 pour 90% du volume initial)</param>
    public static double CompactedVolume(double volumeLoose, double compactionRatio)
    {
        return volumeLoose * compactionRatio;
    }
    
    /// <summary>
    /// Calcule le volume d'un tronc de pyramide (excavation à talus).
    /// </summary>
    /// <param name="topArea">Aire au niveau supérieur en m²</param>
    /// <param name="bottomArea">Aire au niveau inférieur en m²</param>
    /// <param name="height">Hauteur de l'excavation en mètres</param>
    public static double FrustumVolume(double topArea, double bottomArea, double height)
    {
        return height * (topArea + bottomArea + Math.Sqrt(topArea * bottomArea)) / 3;
    }
    
    /// <summary>
    /// Calcule le volume d'une tranchée à parois verticales.
    /// </summary>
    /// <param name="width">Largeur de la tranchée en mètres</param>
    /// <param name="depth">Profondeur moyenne en mètres</param>
    /// <param name="length">Longueur de la tranchée en mètres</param>
    public static double TrenchVolume(double width, double depth, double length)
    {
        return width * depth * length;
    }
    
    /// <summary>
    /// Calcule le volume d'une tranchée avec talutage.
    /// </summary>
    /// <param name="bottomWidth">Largeur au fond en mètres</param>
    /// <param name="depth">Profondeur en mètres</param>
    /// <param name="length">Longueur en mètres</param>
    /// <param name="sideSlope">Fruit des talus (H/V)</param>
    public static double TrenchVolumeWithSlope(double bottomWidth, double depth, double length, double sideSlope)
    {
        double topWidth = bottomWidth + 2 * sideSlope * depth;
        double area = (bottomWidth + topWidth) * depth / 2;
        return area * length;
    }
    
    /// <summary>
    /// Calcule le volume de lit de pose pour une canalisation.
    /// </summary>
    /// <param name="pipeOuterDiameter">Diamètre extérieur de la canalisation en mètres</param>
    /// <param name="beddingThickness">Épaisseur du lit de pose en mètres</param>
    /// <param name="trenchWidth">Largeur de la tranchée en mètres</param>
    /// <param name="length">Longueur en mètres</param>
    public static double BeddingVolume(double pipeOuterDiameter, double beddingThickness, 
        double trenchWidth, double length)
    {
        return trenchWidth * beddingThickness * length;
    }
    
    /// <summary>
    /// Calcule le volume d'enrobage d'une canalisation.
    /// </summary>
    /// <param name="pipeOuterDiameter">Diamètre extérieur en mètres</param>
    /// <param name="trenchWidth">Largeur de la tranchée en mètres</param>
    /// <param name="length">Longueur en mètres</param>
    /// <param name="coverAbovePipe">Hauteur d'enrobage au-dessus de la génératrice supérieure en mètres</param>
    public static double SurroundVolume(double pipeOuterDiameter, double trenchWidth, 
        double length, double coverAbovePipe)
    {
        double totalHeight = pipeOuterDiameter + coverAbovePipe;
        double pipeArea = Math.PI * pipeOuterDiameter * pipeOuterDiameter / 4;
        double trenchArea = trenchWidth * totalHeight;
        return (trenchArea - pipeArea) * length;
    }
    
    #endregion
    
    #region Surfaces et MNT
    
    /// <summary>
    /// Interpole l'altitude Z en un point à partir d'un plan défini par trois points.
    /// </summary>
    public static double InterpolateZFromPlane(Point3d point, Point3d p1, Point3d p2, Point3d p3)
    {
        // Calcul du vecteur normal au plan
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d normal = v1.CrossProduct(v2);
        
        if (Math.Abs(normal.Z) < Tolerance) 
            return (p1.Z + p2.Z + p3.Z) / 3; // Plan vertical, retourner moyenne
        
        // Équation du plan: a(x-x1) + b(y-y1) + c(z-z1) = 0
        // => z = z1 - (a(x-x1) + b(y-y1)) / c
        double z = p1.Z - (normal.X * (point.X - p1.X) + normal.Y * (point.Y - p1.Y)) / normal.Z;
        return z;
    }
    
    /// <summary>
    /// Calcule la pente d'un plan défini par trois points.
    /// </summary>
    /// <returns>Pente en % (gradient maximum)</returns>
    public static double PlaneSlope(Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d normal = v1.CrossProduct(v2);
        
        double horizontalLength = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
        if (Math.Abs(normal.Z) < Tolerance) return double.MaxValue; // Plan vertical
        
        return horizontalLength / Math.Abs(normal.Z) * 100;
    }
    
    /// <summary>
    /// Calcule l'orientation (exposition) d'un plan.
    /// </summary>
    /// <returns>Azimut de la pente en grades (0 = Nord, 100 = Est)</returns>
    public static double PlaneAspect(Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d normal = v1.CrossProduct(v2);
        
        // Projeter sur le plan horizontal et calculer l'azimut
        double azimut = Math.Atan2(normal.X, normal.Y) * 200 / Math.PI;
        if (azimut < 0) azimut += 400;
        return azimut;
    }
    
    /// <summary>
    /// Calcule le volume d'un prisme triangulaire (entre un triangle et un plan horizontal).
    /// </summary>
    public static double TriangularPrismVolume(Point3d p1, Point3d p2, Point3d p3, double referenceZ)
    {
        double baseArea = CalculateTriangleArea(p1, p2, p3);
        double avgHeight = ((p1.Z - referenceZ) + (p2.Z - referenceZ) + (p3.Z - referenceZ)) / 3;
        return baseArea * avgHeight;
    }
    
    /// <summary>
    /// Calcule l'aire d'un triangle en 2D (projeté).
    /// </summary>
    public static double CalculateTriangleArea(Point3d p1, Point3d p2, Point3d p3)
    {
        return Math.Abs(
            (p2.X - p1.X) * (p3.Y - p1.Y) - 
            (p3.X - p1.X) * (p2.Y - p1.Y)
        ) / 2;
    }
    
    /// <summary>
    /// Calcule l'aire 3D d'un triangle (surface réelle inclinée).
    /// </summary>
    public static double CalculateTriangleArea3D(Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d cross = v1.CrossProduct(v2);
        return cross.Length / 2;
    }
    
    #endregion
}
