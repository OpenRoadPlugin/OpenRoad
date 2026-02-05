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

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.DynamicSnap.Models;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Service de détection des points d'accrochage sur les entités AutoCAD.
/// Analyse les polylignes, cercles, arcs, etc. pour trouver les points d'accrochage.
/// </summary>
public static class SnapDetector
{
    /// <summary>
    /// Détecte tous les points d'accrochage sur une entité
    /// </summary>
    /// <param name="entity">Entité à analyser</param>
    /// <param name="cursorPoint">Position du curseur</param>
    /// <param name="tolerance">Tolérance de détection</param>
    /// <param name="modes">Modes d'accrochage actifs</param>
    /// <returns>Liste des points d'accrochage détectés</returns>
    public static List<SnapPoint> DetectSnapPoints(
        Entity entity,
        Point3d cursorPoint,
        double tolerance,
        SnapMode modes)
    {
        var snapPoints = new List<SnapPoint>();

        if (entity == null) return snapPoints;

        // Dispatcher selon le type d'entité
        switch (entity)
        {
            case Polyline polyline:
                DetectPolylineSnapPoints(polyline, cursorPoint, tolerance, modes, snapPoints);
                break;

            case Line line:
                DetectLineSnapPoints(line, cursorPoint, tolerance, modes, snapPoints);
                break;

            case Circle circle:
                DetectCircleSnapPoints(circle, cursorPoint, tolerance, modes, snapPoints);
                break;

            case Arc arc:
                DetectArcSnapPoints(arc, cursorPoint, tolerance, modes, snapPoints);
                break;

            case DBPoint point:
                DetectPointSnapPoints(point, cursorPoint, tolerance, modes, snapPoints);
                break;

            case BlockReference blockRef:
                DetectBlockSnapPoints(blockRef, cursorPoint, tolerance, modes, snapPoints);
                break;

            case Curve curve:
                DetectCurveSnapPoints(curve, cursorPoint, tolerance, modes, snapPoints);
                break;
        }

        // Filtrer par tolérance et trier par priorité
        return snapPoints
            .Where(sp => sp.Distance <= tolerance)
            .OrderBy(sp => sp, SnapPointComparer.Instance)
            .ToList();
    }

    /// <summary>
    /// Détecte les points d'accrochage sur une polyligne
    /// </summary>
    private static void DetectPolylineSnapPoints(
        Polyline polyline,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        int numVertices = polyline.NumberOfVertices;

        // Sommets (Vertex)
        if (modes.HasMode(SnapMode.Vertex))
        {
            for (int i = 0; i < numVertices; i++)
            {
                Point3d vertex = polyline.GetPoint3dAt(i);
                double dist = cursor.DistanceTo(vertex);

                if (dist <= tolerance)
                {
                    double distAlongCurve = GetDistanceAtVertex(polyline, i);
                    results.Add(SnapPoint.FromVertex(vertex, dist, polyline.ObjectId, i, distAlongCurve));
                }
            }
        }

        // Extrémités (Endpoint)
        if (modes.HasMode(SnapMode.Endpoint))
        {
            // Point de départ
            Point3d startPt = polyline.GetPoint3dAt(0);
            double startDist = cursor.DistanceTo(startPt);
            if (startDist <= tolerance)
            {
                results.Add(SnapPoint.FromEndpoint(startPt, startDist, polyline.ObjectId, true));
            }

            // Point de fin
            if (!polyline.Closed && numVertices > 1)
            {
                Point3d endPt = polyline.GetPoint3dAt(numVertices - 1);
                double endDist = cursor.DistanceTo(endPt);
                if (endDist <= tolerance)
                {
                    results.Add(SnapPoint.FromEndpoint(endPt, endDist, polyline.ObjectId, false));
                }
            }
        }

        // Milieux de segments (Midpoint)
        if (modes.HasMode(SnapMode.Midpoint))
        {
            int segmentCount = polyline.Closed ? numVertices : numVertices - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                Point3d segStart = polyline.GetPoint3dAt(i);
                Point3d segEnd = polyline.GetPoint3dAt((i + 1) % numVertices);

                // Point milieu du segment
                Point3d midpoint = new Point3d(
                    (segStart.X + segEnd.X) / 2.0,
                    (segStart.Y + segEnd.Y) / 2.0,
                    (segStart.Z + segEnd.Z) / 2.0);

                double dist = cursor.DistanceTo(midpoint);
                if (dist <= tolerance)
                {
                    double distAlongCurve = GetDistanceAtVertex(polyline, i) +
                                           segStart.DistanceTo(midpoint);
                    results.Add(SnapPoint.FromMidpoint(midpoint, dist, polyline.ObjectId, i, distAlongCurve));
                }
            }
        }

        // Point le plus proche (Nearest)
        if (modes.HasMode(SnapMode.Nearest))
        {
            try
            {
                Point3d nearestPt = polyline.GetClosestPointTo(cursor, false);
                double dist = cursor.DistanceTo(nearestPt);

                if (dist <= tolerance)
                {
                    double param = polyline.GetParameterAtPoint(nearestPt);
                    double distAlongCurve = polyline.GetDistanceAtParameter(param);
                    results.Add(SnapPoint.FromNearest(nearestPt, dist, polyline.ObjectId, param, distAlongCurve));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Détecte les points d'accrochage sur une ligne
    /// </summary>
    private static void DetectLineSnapPoints(
        Line line,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        // Extrémités
        if (modes.HasMode(SnapMode.Endpoint) || modes.HasMode(SnapMode.Vertex))
        {
            double startDist = cursor.DistanceTo(line.StartPoint);
            if (startDist <= tolerance)
            {
                results.Add(SnapPoint.FromEndpoint(line.StartPoint, startDist, line.ObjectId, true));
            }

            double endDist = cursor.DistanceTo(line.EndPoint);
            if (endDist <= tolerance)
            {
                results.Add(SnapPoint.FromEndpoint(line.EndPoint, endDist, line.ObjectId, false));
            }
        }

        // Milieu
        if (modes.HasMode(SnapMode.Midpoint))
        {
            Point3d midpoint = new Point3d(
                (line.StartPoint.X + line.EndPoint.X) / 2.0,
                (line.StartPoint.Y + line.EndPoint.Y) / 2.0,
                (line.StartPoint.Z + line.EndPoint.Z) / 2.0);

            double dist = cursor.DistanceTo(midpoint);
            if (dist <= tolerance)
            {
                results.Add(SnapPoint.FromMidpoint(midpoint, dist, line.ObjectId, 0, line.Length / 2.0));
            }
        }

        // Point le plus proche
        if (modes.HasMode(SnapMode.Nearest))
        {
            try
            {
                Point3d nearestPt = line.GetClosestPointTo(cursor, false);
                double dist = cursor.DistanceTo(nearestPt);

                if (dist <= tolerance)
                {
                    double param = line.GetParameterAtPoint(nearestPt);
                    double distAlongCurve = line.GetDistanceAtParameter(param);
                    results.Add(SnapPoint.FromNearest(nearestPt, dist, line.ObjectId, param, distAlongCurve));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Détecte les points d'accrochage sur un cercle
    /// </summary>
    private static void DetectCircleSnapPoints(
        Circle circle,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        // Centre
        if (modes.HasMode(SnapMode.Center))
        {
            double dist = cursor.DistanceTo(circle.Center);
            if (dist <= tolerance)
            {
                results.Add(SnapPoint.FromCenter(circle.Center, dist, circle.ObjectId));
            }
        }

        // Quadrants
        if (modes.HasMode(SnapMode.Quadrant))
        {
            var quadrants = new[]
            {
                new Point3d(circle.Center.X + circle.Radius, circle.Center.Y, circle.Center.Z),
                new Point3d(circle.Center.X, circle.Center.Y + circle.Radius, circle.Center.Z),
                new Point3d(circle.Center.X - circle.Radius, circle.Center.Y, circle.Center.Z),
                new Point3d(circle.Center.X, circle.Center.Y - circle.Radius, circle.Center.Z)
            };

            foreach (var quad in quadrants)
            {
                double dist = cursor.DistanceTo(quad);
                if (dist <= tolerance)
                {
                    results.Add(new SnapPoint(quad, SnapMode.Quadrant, dist, circle.ObjectId));
                }
            }
        }

        // Point le plus proche
        if (modes.HasMode(SnapMode.Nearest))
        {
            try
            {
                Point3d nearestPt = circle.GetClosestPointTo(cursor, false);
                double dist = cursor.DistanceTo(nearestPt);

                if (dist <= tolerance)
                {
                    double param = circle.GetParameterAtPoint(nearestPt);
                    results.Add(SnapPoint.FromNearest(nearestPt, dist, circle.ObjectId, param, 0));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Détecte les points d'accrochage sur un arc
    /// </summary>
    private static void DetectArcSnapPoints(
        Arc arc,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        // Centre
        if (modes.HasMode(SnapMode.Center))
        {
            double dist = cursor.DistanceTo(arc.Center);
            if (dist <= tolerance)
            {
                results.Add(SnapPoint.FromCenter(arc.Center, dist, arc.ObjectId));
            }
        }

        // Extrémités
        if (modes.HasMode(SnapMode.Endpoint))
        {
            double startDist = cursor.DistanceTo(arc.StartPoint);
            if (startDist <= tolerance)
            {
                results.Add(SnapPoint.FromEndpoint(arc.StartPoint, startDist, arc.ObjectId, true));
            }

            double endDist = cursor.DistanceTo(arc.EndPoint);
            if (endDist <= tolerance)
            {
                results.Add(SnapPoint.FromEndpoint(arc.EndPoint, endDist, arc.ObjectId, false));
            }
        }

        // Milieu
        if (modes.HasMode(SnapMode.Midpoint))
        {
            try
            {
                double midParam = (arc.StartParam + arc.EndParam) / 2.0;
                Point3d midpoint = arc.GetPointAtParameter(midParam);
                double dist = cursor.DistanceTo(midpoint);

                if (dist <= tolerance)
                {
                    results.Add(SnapPoint.FromMidpoint(midpoint, dist, arc.ObjectId, 0, arc.Length / 2.0));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }

        // Point le plus proche
        if (modes.HasMode(SnapMode.Nearest))
        {
            try
            {
                Point3d nearestPt = arc.GetClosestPointTo(cursor, false);
                double dist = cursor.DistanceTo(nearestPt);

                if (dist <= tolerance)
                {
                    double param = arc.GetParameterAtPoint(nearestPt);
                    double distAlongCurve = arc.GetDistanceAtParameter(param);
                    results.Add(SnapPoint.FromNearest(nearestPt, dist, arc.ObjectId, param, distAlongCurve));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Détecte les points d'accrochage sur un objet Point
    /// </summary>
    private static void DetectPointSnapPoints(
        DBPoint point,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        if (modes.HasMode(SnapMode.Node))
        {
            double dist = cursor.DistanceTo(point.Position);
            if (dist <= tolerance)
            {
                results.Add(new SnapPoint(point.Position, SnapMode.Node, dist, point.ObjectId));
            }
        }
    }

    /// <summary>
    /// Détecte les points d'accrochage sur une référence de bloc
    /// </summary>
    private static void DetectBlockSnapPoints(
        BlockReference blockRef,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        if (modes.HasMode(SnapMode.Insertion))
        {
            double dist = cursor.DistanceTo(blockRef.Position);
            if (dist <= tolerance)
            {
                results.Add(new SnapPoint(blockRef.Position, SnapMode.Insertion, dist, blockRef.ObjectId));
            }
        }
    }

    /// <summary>
    /// Détecte les points d'accrochage sur une courbe générique
    /// </summary>
    private static void DetectCurveSnapPoints(
        Curve curve,
        Point3d cursor,
        double tolerance,
        SnapMode modes,
        List<SnapPoint> results)
    {
        // Extrémités
        if (modes.HasMode(SnapMode.Endpoint))
        {
            try
            {
                double startDist = cursor.DistanceTo(curve.StartPoint);
                if (startDist <= tolerance)
                {
                    results.Add(SnapPoint.FromEndpoint(curve.StartPoint, startDist, curve.ObjectId, true));
                }

                double endDist = cursor.DistanceTo(curve.EndPoint);
                if (endDist <= tolerance)
                {
                    results.Add(SnapPoint.FromEndpoint(curve.EndPoint, endDist, curve.ObjectId, false));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }

        // Point le plus proche
        if (modes.HasMode(SnapMode.Nearest))
        {
            try
            {
                Point3d nearestPt = curve.GetClosestPointTo(cursor, false);
                double dist = cursor.DistanceTo(nearestPt);

                if (dist <= tolerance)
                {
                    double param = curve.GetParameterAtPoint(nearestPt);
                    double distAlongCurve = curve.GetDistanceAtParameter(param);
                    results.Add(SnapPoint.FromNearest(nearestPt, dist, curve.ObjectId, param, distAlongCurve));
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Snap detection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Calcule la distance curviligne jusqu'à un sommet de polyligne
    /// </summary>
    private static double GetDistanceAtVertex(Polyline polyline, int vertexIndex)
    {
        if (vertexIndex <= 0) return 0.0;

        try
        {
            return polyline.GetDistanceAtParameter(vertexIndex);
        }
        catch
        {
            // Fallback: calcul manuel
            double distance = 0.0;
            for (int i = 0; i < vertexIndex && i < polyline.NumberOfVertices - 1; i++)
            {
                Point3d pt1 = polyline.GetPoint3dAt(i);
                Point3d pt2 = polyline.GetPoint3dAt(i + 1);
                distance += pt1.DistanceTo(pt2);
            }
            return distance;
        }
    }
}
