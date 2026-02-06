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

namespace OpenAsphalte.Modules.DynamicSnap.Models;

/// <summary>
/// Représente un point d'accrochage détecté par le système OAS Dynamic Snap.
/// Contient les coordonnées, le type d'accrochage et les métadonnées associées.
/// </summary>
public sealed class SnapPoint : IComparable<SnapPoint>
{
    /// <summary>
    /// Coordonnées 3D du point d'accrochage
    /// </summary>
    public Point3d Point { get; init; }

    /// <summary>
    /// Type d'accrochage qui a généré ce point
    /// </summary>
    public SnapMode Mode { get; init; }

    /// <summary>
    /// Distance entre le curseur et ce point d'accrochage
    /// </summary>
    public double Distance { get; init; }

    /// <summary>
    /// ObjectId de l'entité source (si applicable)
    /// </summary>
    public ObjectId EntityId { get; init; }

    /// <summary>
    /// Index du sommet (pour SnapMode.Vertex)
    /// </summary>
    public int VertexIndex { get; init; } = -1;

    /// <summary>
    /// Paramètre sur la courbe (pour les modes basés sur courbe)
    /// </summary>
    public double CurveParameter { get; init; } = double.NaN;

    /// <summary>
    /// Distance curviligne sur l'entité (pour polylignes)
    /// </summary>
    public double DistanceAlongCurve { get; init; } = double.NaN;

    /// <summary>
    /// Priorité d'affichage (plus bas = plus prioritaire)
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Indique si ce point est actuellement surligné
    /// </summary>
    public bool IsHighlighted { get; set; }

    /// <summary>
    /// Constructeur principal
    /// </summary>
    public SnapPoint(Point3d point, SnapMode mode, double distance, ObjectId entityId = default)
    {
        Point = point;
        Mode = mode;
        Distance = distance;
        EntityId = entityId;
        Priority = GetPriorityForMode(mode);
    }

    /// <summary>
    /// Détermine la priorité selon le mode d'accrochage
    /// </summary>
    private static int GetPriorityForMode(SnapMode mode)
    {
        return mode switch
        {
            SnapMode.Endpoint => 10,
            SnapMode.Vertex => 15,
            SnapMode.Midpoint => 20,
            SnapMode.Center => 25,
            SnapMode.Intersection => 30,
            SnapMode.Node => 35,
            SnapMode.Insertion => 40,
            SnapMode.Quadrant => 45,
            SnapMode.Perpendicular => 50,
            SnapMode.Tangent => 55,
            SnapMode.Parallel => 60,
            SnapMode.Nearest => 100,
            _ => 200
        };
    }

    /// <summary>
    /// Crée un SnapPoint pour un sommet de polyligne
    /// </summary>
    public static SnapPoint FromVertex(Point3d point, double distance, ObjectId entityId, int vertexIndex, double distanceAlongCurve)
    {
        return new SnapPoint(point, SnapMode.Vertex, distance, entityId)
        {
            VertexIndex = vertexIndex,
            DistanceAlongCurve = distanceAlongCurve
        };
    }

    /// <summary>
    /// Crée un SnapPoint pour une extrémité
    /// </summary>
    public static SnapPoint FromEndpoint(Point3d point, double distance, ObjectId entityId, bool isStart)
    {
        return new SnapPoint(point, SnapMode.Endpoint, distance, entityId)
        {
            VertexIndex = isStart ? 0 : -1
        };
    }

    /// <summary>
    /// Crée un SnapPoint pour un milieu de segment
    /// </summary>
    public static SnapPoint FromMidpoint(Point3d point, double distance, ObjectId entityId, int segmentIndex, double distanceAlongCurve)
    {
        return new SnapPoint(point, SnapMode.Midpoint, distance, entityId)
        {
            VertexIndex = segmentIndex,
            DistanceAlongCurve = distanceAlongCurve
        };
    }

    /// <summary>
    /// Crée un SnapPoint pour le point le plus proche
    /// </summary>
    public static SnapPoint FromNearest(Point3d point, double distance, ObjectId entityId, double parameter, double distanceAlongCurve)
    {
        return new SnapPoint(point, SnapMode.Nearest, distance, entityId)
        {
            CurveParameter = parameter,
            DistanceAlongCurve = distanceAlongCurve
        };
    }

    /// <summary>
    /// Crée un SnapPoint pour un centre
    /// </summary>
    public static SnapPoint FromCenter(Point3d point, double distance, ObjectId entityId)
    {
        return new SnapPoint(point, SnapMode.Center, distance, entityId);
    }

    /// <summary>
    /// Compare deux SnapPoints par priorité puis par distance
    /// </summary>
    public int CompareTo(SnapPoint? other)
    {
        if (other == null) return -1;

        int priorityCompare = Priority.CompareTo(other.Priority);
        if (priorityCompare != 0) return priorityCompare;

        return Distance.CompareTo(other.Distance);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"SnapPoint[{Mode}] at ({Point.X:F2}, {Point.Y:F2}, {Point.Z:F2}) - Distance: {Distance:F4}";
    }
}

/// <summary>
/// Comparateur pour trier les SnapPoints
/// </summary>
public class SnapPointComparer : IComparer<SnapPoint>
{
    /// <summary>
    /// Instance singleton du comparateur
    /// </summary>
    public static readonly SnapPointComparer Instance = new();

    /// <inheritdoc />
    public int Compare(SnapPoint? x, SnapPoint? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return 1;
        if (y == null) return -1;

        return x.CompareTo(y);
    }
}
