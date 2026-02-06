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

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.DynamicSnap.Models;
using AcColor = Autodesk.AutoCAD.Colors.Color;
using AcColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;
using DbPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Service de rendu des marqueurs visuels pour l'accrochage dynamique.
/// Utilise TransientManager pour dessiner des marqueurs temporaires modernes
/// sans modifier le dessin et sans scintillement.
/// </summary>
public static class MarkerRenderer
{
    #region Fields

    /// <summary>
    /// Liste des entités transitoires actuellement affichées
    /// </summary>
    private static readonly List<Drawable> _transients = new();

    /// <summary>
    /// Verrou pour accès thread-safe aux transitoires
    /// </summary>
    private static readonly object _lock = new();

    #endregion

    #region Public API

    /// <summary>
    /// Met à jour les marqueurs affichés. Efface les anciens et dessine les nouveaux.
    /// Méthode principale appelée par le PointMonitor en temps réel.
    /// </summary>
    /// <param name="snapPoints">Points d'accrochage à afficher</param>
    /// <param name="config">Configuration visuelle</param>
    public static void UpdateMarkers(List<SnapPoint> snapPoints, SnapConfiguration config)
    {
        lock (_lock)
        {
            // Effacer les marqueurs précédents
            EraseTransients();

            if (snapPoints.Count == 0) return;

            // Déterminer les points à afficher
            var pointsToShow = config.ShowOnlyNearest && snapPoints.Count > 0
                ? new List<SnapPoint> { snapPoints[0] }
                : snapPoints.Take(config.MaxVisibleMarkers).ToList();

            // Convertir le LineWeight
            var lw = IntToLineWeight(config.MarkerLineWeight);

            // Dessiner chaque marqueur comme entité transitoire
            bool isFirst = true;
            foreach (var sp in pointsToShow)
            {
                var color = isFirst ? config.ActiveMarkerColor : config.MarkerColor;
                var drawables = CreateMarkerDrawables(
                    sp.Point, sp.Mode, config.MarkerSize, color,
                    config.FilledMarkers, lw);

                foreach (var drawable in drawables)
                {
                    try
                    {
                        TransientManager.CurrentTransientManager.AddTransient(
                            drawable,
                            TransientDrawingMode.DirectTopmost,
                            128,
                            new IntegerCollection());
                        _transients.Add(drawable);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Debug($"MarkerRenderer AddTransient: {ex.Message}");
                        drawable.Dispose();
                    }
                }

                isFirst = false;
            }
        }
    }

    /// <summary>
    /// Efface tous les marqueurs transitoires
    /// </summary>
    public static void ClearAllMarkers()
    {
        lock (_lock)
        {
            EraseTransients();
        }
    }

    #endregion

    #region Marker Geometry Creation

    /// <summary>
    /// Crée les entités géométriques pour un marqueur selon le mode d'accrochage.
    /// Chaque mode a un style visuel distinct pour une identification rapide.
    /// </summary>
    private static List<Drawable> CreateMarkerDrawables(
        Point3d center, SnapMode mode, double size, short colorIndex,
        bool filled, LineWeight lineWeight)
    {
        var acColor = AcColor.FromColorIndex(AcColorMethod.ByAci, colorIndex);

        return mode switch
        {
            SnapMode.Vertex or SnapMode.Endpoint => CreateSquareMarker(center, size, acColor, filled, lineWeight),
            SnapMode.Midpoint => CreateTriangleMarker(center, size, acColor, filled, lineWeight),
            SnapMode.Center => CreateCircleMarker(center, size, acColor, filled, lineWeight),
            SnapMode.Intersection => CreateXMarkMarker(center, size, acColor, lineWeight),
            SnapMode.Nearest => CreateDiamondMarker(center, size, acColor, filled, lineWeight),
            SnapMode.Perpendicular => CreateCrossMarker(center, size, acColor, lineWeight),
            SnapMode.Node => CreateCircleWithCrossMarker(center, size, acColor, filled, lineWeight),
            _ => CreateCircleMarker(center, size, acColor, filled, lineWeight),
        };
    }

    /// <summary>
    /// Crée un marqueur carré (pour Vertex/Endpoint)
    /// </summary>
    private static List<Drawable> CreateSquareMarker(Point3d center, double size,
        AcColor color, bool filled, LineWeight lineWeight)
    {
        double h = size / 2.0;
        double z = center.Z;
        var drawables = new List<Drawable>();

        if (filled)
        {
            var solid = new Solid(
                new Point3d(center.X - h, center.Y - h, z),
                new Point3d(center.X + h, center.Y - h, z),
                new Point3d(center.X - h, center.Y + h, z),
                new Point3d(center.X + h, center.Y + h, z));
            solid.Color = color;
            drawables.Add(solid);
        }

        var poly = new DbPolyline(4);
        poly.AddVertexAt(0, new Point2d(center.X - h, center.Y - h), 0, 0, 0);
        poly.AddVertexAt(1, new Point2d(center.X + h, center.Y - h), 0, 0, 0);
        poly.AddVertexAt(2, new Point2d(center.X + h, center.Y + h), 0, 0, 0);
        poly.AddVertexAt(3, new Point2d(center.X - h, center.Y + h), 0, 0, 0);
        poly.Closed = true;
        poly.Elevation = z;
        poly.Color = color;
        poly.LineWeight = lineWeight;
        drawables.Add(poly);

        return drawables;
    }

    /// <summary>
    /// Crée un marqueur triangle (pour Midpoint)
    /// </summary>
    private static List<Drawable> CreateTriangleMarker(Point3d center, double size,
        AcColor color, bool filled, LineWeight lineWeight)
    {
        double h = size / 2.0;
        double z = center.Z;
        double height = size * 0.866; // sqrt(3)/2
        var drawables = new List<Drawable>();

        if (filled)
        {
            var solid = new Solid(
                new Point3d(center.X, center.Y + height * 0.5, z),
                new Point3d(center.X - h, center.Y - height * 0.5, z),
                new Point3d(center.X + h, center.Y - height * 0.5, z));
            solid.Color = color;
            drawables.Add(solid);
        }

        var poly = new DbPolyline(3);
        poly.AddVertexAt(0, new Point2d(center.X, center.Y + height * 0.5), 0, 0, 0);
        poly.AddVertexAt(1, new Point2d(center.X - h, center.Y - height * 0.5), 0, 0, 0);
        poly.AddVertexAt(2, new Point2d(center.X + h, center.Y - height * 0.5), 0, 0, 0);
        poly.Closed = true;
        poly.Elevation = z;
        poly.Color = color;
        poly.LineWeight = lineWeight;
        drawables.Add(poly);

        return drawables;
    }

    /// <summary>
    /// Crée un marqueur cercle (pour Center)
    /// </summary>
    private static List<Drawable> CreateCircleMarker(Point3d center, double size,
        AcColor color, bool filled, LineWeight lineWeight)
    {
        var drawables = new List<Drawable>();

        if (filled)
        {
            // Solid triangles approximating a filled circle (4 quadrants)
            double r = size / 2.0;
            double z = center.Z;
            var p0 = new Point3d(center.X, center.Y, z);
            var pN = new Point3d(center.X, center.Y + r, z);
            var pE = new Point3d(center.X + r, center.Y, z);
            var pS = new Point3d(center.X, center.Y - r, z);
            var pW = new Point3d(center.X - r, center.Y, z);
            var s1 = new Solid(p0, pN, pE); s1.Color = color; drawables.Add(s1);
            var s2 = new Solid(p0, pE, pS); s2.Color = color; drawables.Add(s2);
            var s3 = new Solid(p0, pS, pW); s3.Color = color; drawables.Add(s3);
            var s4 = new Solid(p0, pW, pN); s4.Color = color; drawables.Add(s4);
        }

        var circle = new Circle(center, Vector3d.ZAxis, size / 2.0);
        circle.Color = color;
        circle.LineWeight = lineWeight;
        drawables.Add(circle);

        return drawables;
    }

    /// <summary>
    /// Crée un marqueur X (pour Intersection)
    /// </summary>
    private static List<Drawable> CreateXMarkMarker(Point3d center, double size,
        AcColor color, LineWeight lineWeight)
    {
        double h = size / 2.0;
        double z = center.Z;
        var drawables = new List<Drawable>();

        var line1 = new Line(
            new Point3d(center.X - h, center.Y - h, z),
            new Point3d(center.X + h, center.Y + h, z));
        line1.Color = color;
        line1.LineWeight = lineWeight;

        var line2 = new Line(
            new Point3d(center.X - h, center.Y + h, z),
            new Point3d(center.X + h, center.Y - h, z));
        line2.Color = color;
        line2.LineWeight = lineWeight;

        drawables.Add(line1);
        drawables.Add(line2);
        return drawables;
    }

    /// <summary>
    /// Crée un marqueur losange (pour Nearest)
    /// </summary>
    private static List<Drawable> CreateDiamondMarker(Point3d center, double size,
        AcColor color, bool filled, LineWeight lineWeight)
    {
        double h = size / 2.0;
        double z = center.Z;
        var drawables = new List<Drawable>();

        if (filled)
        {
            var solid = new Solid(
                new Point3d(center.X, center.Y - h, z),
                new Point3d(center.X + h, center.Y, z),
                new Point3d(center.X - h, center.Y, z),
                new Point3d(center.X, center.Y + h, z));
            solid.Color = color;
            drawables.Add(solid);
        }

        var poly = new DbPolyline(4);
        poly.AddVertexAt(0, new Point2d(center.X, center.Y - h), 0, 0, 0);
        poly.AddVertexAt(1, new Point2d(center.X + h, center.Y), 0, 0, 0);
        poly.AddVertexAt(2, new Point2d(center.X, center.Y + h), 0, 0, 0);
        poly.AddVertexAt(3, new Point2d(center.X - h, center.Y), 0, 0, 0);
        poly.Closed = true;
        poly.Elevation = z;
        poly.Color = color;
        poly.LineWeight = lineWeight;
        drawables.Add(poly);

        return drawables;
    }

    /// <summary>
    /// Crée un marqueur croix + (pour Perpendicular)
    /// </summary>
    private static List<Drawable> CreateCrossMarker(Point3d center, double size,
        AcColor color, LineWeight lineWeight)
    {
        double h = size / 2.0;
        double z = center.Z;
        var drawables = new List<Drawable>();

        var lineH = new Line(
            new Point3d(center.X - h, center.Y, z),
            new Point3d(center.X + h, center.Y, z));
        lineH.Color = color;
        lineH.LineWeight = lineWeight;

        var lineV = new Line(
            new Point3d(center.X, center.Y - h, z),
            new Point3d(center.X, center.Y + h, z));
        lineV.Color = color;
        lineV.LineWeight = lineWeight;

        drawables.Add(lineH);
        drawables.Add(lineV);
        return drawables;
    }

    /// <summary>
    /// Crée un marqueur cercle + croix (pour Node)
    /// </summary>
    private static List<Drawable> CreateCircleWithCrossMarker(Point3d center, double size,
        AcColor color, bool filled, LineWeight lineWeight)
    {
        var drawables = CreateCircleMarker(center, size, color, filled, lineWeight);
        drawables.AddRange(CreateCrossMarker(center, size * 0.7, color, lineWeight));
        return drawables;
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Convertit un entier en valeur LineWeight AutoCAD
    /// </summary>
    private static LineWeight IntToLineWeight(int value) => value switch
    {
        0 => LineWeight.ByLineWeightDefault,
        5 => LineWeight.LineWeight005,
        9 => LineWeight.LineWeight009,
        13 => LineWeight.LineWeight013,
        15 => LineWeight.LineWeight015,
        18 => LineWeight.LineWeight018,
        20 => LineWeight.LineWeight020,
        25 => LineWeight.LineWeight025,
        30 => LineWeight.LineWeight030,
        35 => LineWeight.LineWeight035,
        40 => LineWeight.LineWeight040,
        50 => LineWeight.LineWeight050,
        53 => LineWeight.LineWeight053,
        60 => LineWeight.LineWeight060,
        70 => LineWeight.LineWeight070,
        80 => LineWeight.LineWeight080,
        90 => LineWeight.LineWeight090,
        100 => LineWeight.LineWeight100,
        _ => LineWeight.LineWeight030,
    };

    /// <summary>
    /// Supprime tous les transitoires en cours d'affichage et libère leurs ressources
    /// </summary>
    private static void EraseTransients()
    {
        foreach (var drawable in _transients)
        {
            try
            {
                TransientManager.CurrentTransientManager.EraseTransient(
                    drawable, new IntegerCollection());
                drawable.Dispose();
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"MarkerRenderer EraseTransients: {ex.Message}");
            }
        }
        _transients.Clear();
    }

    #endregion
}
