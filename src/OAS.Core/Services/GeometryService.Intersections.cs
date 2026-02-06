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

namespace OpenAsphalte.Services;

/// <summary>
/// GeometryService — Intersections entre lignes, cercles, arcs et points de tangence.
/// </summary>
public static partial class GeometryService
{
    #region Intersections

    /// <summary>
    /// Résultat d'une intersection entre deux lignes.
    /// </summary>
    public readonly struct LineIntersectionResult
    {
        public bool HasIntersection { get; init; }
        public Point3d Point { get; init; }
        public double T1 { get; init; }
        public double T2 { get; init; }
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
        return result.HasIntersection && result.IsOnBothSegments ? result.Point : null;
    }

    /// <summary>
    /// Résultat d'une intersection ligne/cercle.
    /// </summary>
    public readonly struct LineCircleIntersectionResult
    {
        public int Count { get; init; }
        public Point3d? Point1 { get; init; }
        public Point3d? Point2 { get; init; }
    }

    /// <summary>
    /// Calcule les intersections entre une droite et un cercle.
    /// </summary>
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
        public int Count { get; init; }
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

        if (d < Tolerance && Math.Abs(radius1 - radius2) < Tolerance)
            return new CircleCircleIntersectionResult { Count = -1 };

        if (d > radius1 + radius2 + Tolerance || d < Math.Abs(radius1 - radius2) - Tolerance)
            return new CircleCircleIntersectionResult { Count = 0 };

        double a = (radius1 * radius1 - radius2 * radius2 + d * d) / (2 * d);
        double h2 = radius1 * radius1 - a * a;

        double px = center1.X + a * (center2.X - center1.X) / d;
        double py = center1.Y + a * (center2.Y - center1.Y) / d;

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
    public static (Point3d, Point3d)? TangentPointsFromExternalPoint(Point3d externalPoint, Point3d center, double radius)
    {
        double d = Distance2D(externalPoint, center);
        if (d <= radius) return null;

        double angle = AngleBetween(center, externalPoint);
        double tangentAngle = Math.Acos(radius / d);

        return (
            OffsetPoint(center, angle + tangentAngle, radius),
            OffsetPoint(center, angle - tangentAngle, radius)
        );
    }

    #endregion

    #region Cercles et Arcs

    /// <summary>
    /// Calcule le cercle passant par trois points.
    /// </summary>
    public static (Point3d Center, double Radius)? CircleFrom3Points(Point3d p1, Point3d p2, Point3d p3)
    {
        double ax = p1.X, ay = p1.Y;
        double bx = p2.X, by = p2.Y;
        double cx = p3.X, cy = p3.Y;

        double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Math.Abs(d) < Tolerance) return null;

        double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
        double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

        var center = new Point3d(ux, uy, p1.Z);
        return (center, Distance2D(center, p1));
    }

    /// <summary>
    /// Calcule la longueur d'un arc de cercle.
    /// </summary>
    public static double ArcLength(double radius, double angleRadians)
        => radius * Math.Abs(angleRadians);

    /// <summary>
    /// Calcule l'aire d'un secteur circulaire.
    /// </summary>
    public static double SectorArea(double radius, double angleRadians)
        => 0.5 * radius * radius * Math.Abs(angleRadians);

    /// <summary>
    /// Calcule l'aire d'un segment circulaire (partie entre corde et arc).
    /// </summary>
    public static double CircularSegmentArea(double radius, double angleRadians)
        => 0.5 * radius * radius * (Math.Abs(angleRadians) - Math.Sin(Math.Abs(angleRadians)));

    /// <summary>
    /// Calcule la longueur de la corde d'un arc.
    /// </summary>
    public static double ChordLength(double radius, double angleRadians)
        => 2 * radius * Math.Sin(Math.Abs(angleRadians) / 2);

    /// <summary>
    /// Calcule la flèche (sagita) d'un arc.
    /// </summary>
    public static double Sagita(double radius, double angleRadians)
        => radius * (1 - Math.Cos(Math.Abs(angleRadians) / 2));

    #endregion
}
