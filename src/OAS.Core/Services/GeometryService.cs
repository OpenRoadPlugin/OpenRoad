// Open Asphalte
// Copyright (C) 2026 Open Asphalte Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace OpenAsphalte.Services;

/// <summary>
/// Service de calculs géométriques pour AutoCAD.
/// Fournit des méthodes utilitaires pour les opérations géométriques courantes,
/// la voirie, l'assainissement et les calculs de cubature.
/// </summary>
/// <remarks>
/// Ce service est découpé en partial classes thématiques :
/// GeometryService.cs, .Intersections.cs, .Voirie.cs, .Hydraulics.cs, .Earthwork.cs
/// </remarks>
public static partial class GeometryService
{
    #region Constantes

    /// <summary>Tolérance par défaut pour les comparaisons de doubles</summary>
    public const double Tolerance = 1e-10;

    /// <summary>Accélération gravitationnelle (m/s²)</summary>
    public const double Gravity = 9.81;

    /// <summary>Pi / 180 pour conversion degrés ? radians</summary>
    public const double DegToRad = Math.PI / 180.0;

    /// <summary>180 / Pi pour conversion radians ? degrés</summary>
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
    /// <returns>Angle en radians dans l'intervalle [-p, p]</returns>
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
    /// Normalise un angle en radians dans l'intervalle [0, 2p].
    /// </summary>
    /// <param name="angle">Angle à normaliser (peut être négatif ou > 2p)</param>
    /// <returns>Angle normalisé dans [0, 2p]</returns>
    /// <example>
    /// <code>
    /// var normalized = GeometryService.NormalizeAngle(-Math.PI / 2);
    /// // Résultat: 3p/2 (environ 4.712)
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
    /// <returns>Angle en radians [0, p]</returns>
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
}

