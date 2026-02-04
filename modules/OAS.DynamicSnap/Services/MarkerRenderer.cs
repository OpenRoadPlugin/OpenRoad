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
using OpenAsphalte.Modules.DynamicSnap.Models;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Service de rendu des marqueurs visuels pour l'accrochage dynamique.
/// Utilise grdraw pour dessiner des marqueurs temporaires sans modifier le dessin.
/// </summary>
public static class MarkerRenderer
{
    /// <summary>
    /// Dessine un marqueur au point spécifié
    /// </summary>
    /// <param name="point">Centre du marqueur</param>
    /// <param name="size">Taille du marqueur</param>
    /// <param name="color">Couleur AutoCAD (1-255)</param>
    /// <param name="style">Style du marqueur</param>
    /// <param name="segments">Nombre de segments pour les formes courbes</param>
    public static void DrawMarker(
        Point3d point,
        double size,
        short color,
        MarkerStyle style,
        int segments = 32)
    {
        switch (style)
        {
            case MarkerStyle.Circle:
                DrawCircle(point, size, color, segments);
                break;

            case MarkerStyle.Cross:
                DrawCross(point, size, color);
                break;

            case MarkerStyle.XMark:
                DrawXMark(point, size, color);
                break;

            case MarkerStyle.Square:
                DrawSquare(point, size, color);
                break;

            case MarkerStyle.Diamond:
                DrawDiamond(point, size, color);
                break;

            case MarkerStyle.Triangle:
                DrawTriangle(point, size, color);
                break;

            case MarkerStyle.CircleWithCross:
                DrawCircle(point, size, color, segments);
                DrawCross(point, size * 0.7, color);
                break;
        }
    }

    /// <summary>
    /// Dessine un cercle temporaire avec grdraw
    /// </summary>
    public static void DrawCircle(Point3d center, double radius, short color, int segments = 32)
    {
        double step = 2.0 * Math.PI / segments;
        double z = center.Z;

        for (int i = 0; i < segments; i++)
        {
            double angle1 = i * step;
            double angle2 = (i + 1) * step;

            var pt1 = new Point3d(
                center.X + radius * Math.Cos(angle1),
                center.Y + radius * Math.Sin(angle1),
                z);

            var pt2 = new Point3d(
                center.X + radius * Math.Cos(angle2),
                center.Y + radius * Math.Sin(angle2),
                z);

            DrawLine(pt1, pt2, color);
        }
    }

    /// <summary>
    /// Dessine une croix (+)
    /// </summary>
    public static void DrawCross(Point3d center, double size, short color)
    {
        double halfSize = size / 2.0;
        double z = center.Z;

        // Ligne horizontale
        DrawLine(
            new Point3d(center.X - halfSize, center.Y, z),
            new Point3d(center.X + halfSize, center.Y, z),
            color);

        // Ligne verticale
        DrawLine(
            new Point3d(center.X, center.Y - halfSize, z),
            new Point3d(center.X, center.Y + halfSize, z),
            color);
    }

    /// <summary>
    /// Dessine un X
    /// </summary>
    public static void DrawXMark(Point3d center, double size, short color)
    {
        double halfSize = size / 2.0;
        double z = center.Z;

        // Diagonale 1
        DrawLine(
            new Point3d(center.X - halfSize, center.Y - halfSize, z),
            new Point3d(center.X + halfSize, center.Y + halfSize, z),
            color);

        // Diagonale 2
        DrawLine(
            new Point3d(center.X - halfSize, center.Y + halfSize, z),
            new Point3d(center.X + halfSize, center.Y - halfSize, z),
            color);
    }

    /// <summary>
    /// Dessine un carré
    /// </summary>
    public static void DrawSquare(Point3d center, double size, short color)
    {
        double halfSize = size / 2.0;
        double z = center.Z;

        var corners = new[]
        {
            new Point3d(center.X - halfSize, center.Y - halfSize, z),
            new Point3d(center.X + halfSize, center.Y - halfSize, z),
            new Point3d(center.X + halfSize, center.Y + halfSize, z),
            new Point3d(center.X - halfSize, center.Y + halfSize, z)
        };

        for (int i = 0; i < 4; i++)
        {
            DrawLine(corners[i], corners[(i + 1) % 4], color);
        }
    }

    /// <summary>
    /// Dessine un losange
    /// </summary>
    public static void DrawDiamond(Point3d center, double size, short color)
    {
        double halfSize = size / 2.0;
        double z = center.Z;

        var corners = new[]
        {
            new Point3d(center.X, center.Y - halfSize, z),      // Bas
            new Point3d(center.X + halfSize, center.Y, z),      // Droite
            new Point3d(center.X, center.Y + halfSize, z),      // Haut
            new Point3d(center.X - halfSize, center.Y, z)       // Gauche
        };

        for (int i = 0; i < 4; i++)
        {
            DrawLine(corners[i], corners[(i + 1) % 4], color);
        }
    }

    /// <summary>
    /// Dessine un triangle
    /// </summary>
    public static void DrawTriangle(Point3d center, double size, short color)
    {
        double halfSize = size / 2.0;
        double z = center.Z;
        double height = size * 0.866; // sqrt(3)/2

        var corners = new[]
        {
            new Point3d(center.X, center.Y + height * 0.5, z),              // Sommet
            new Point3d(center.X - halfSize, center.Y - height * 0.5, z),   // Bas gauche
            new Point3d(center.X + halfSize, center.Y - height * 0.5, z)    // Bas droite
        };

        for (int i = 0; i < 3; i++)
        {
            DrawLine(corners[i], corners[(i + 1) % 3], color);
        }
    }

    /// <summary>
    /// Dessine une ligne avec grdraw via l'API AutoCAD
    /// </summary>
    private static void DrawLine(Point3d from, Point3d to, short color)
    {
        try
        {
            // Utilisation de l'API Editor.DrawVector pour le dessin temporaire
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            var editor = doc.Editor;

            // DrawVector trace une ligne temporaire entre deux points
            // color: index couleur AutoCAD
            // highlightVector: true pour que la ligne soit visible immédiatement
            editor.DrawVector(from, to, color, true);
        }
        catch
        {
            // Ignorer les erreurs de dessin (peut arriver si le document n'est pas prêt)
        }
    }

    /// <summary>
    /// Efface tous les marqueurs temporaires (via Redraw)
    /// </summary>
    public static void ClearAllMarkers()
    {
        try
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Redraw efface les graphiques temporaires
            doc.Editor.UpdateScreen();
        }
        catch
        {
            // Ignorer les erreurs
        }
    }

    /// <summary>
    /// Dessine un marqueur spécifique pour un mode d'accrochage
    /// </summary>
    public static void DrawSnapMarker(SnapPoint snapPoint, SnapConfiguration config)
    {
        var style = GetStyleForMode(snapPoint.Mode, config.MarkerStyle);
        var color = snapPoint.IsHighlighted ? config.ActiveMarkerColor : config.MarkerColor;

        DrawMarker(snapPoint.Point, config.MarkerSize, color, style, config.MarkerSegments);
    }

    /// <summary>
    /// Retourne un style adapté au mode d'accrochage
    /// </summary>
    private static MarkerStyle GetStyleForMode(SnapMode mode, MarkerStyle defaultStyle)
    {
        return mode switch
        {
            SnapMode.Vertex or SnapMode.Endpoint => MarkerStyle.Square,
            SnapMode.Midpoint => MarkerStyle.Triangle,
            SnapMode.Center => MarkerStyle.Circle,
            SnapMode.Intersection => MarkerStyle.XMark,
            SnapMode.Perpendicular => MarkerStyle.Cross,
            SnapMode.Node => MarkerStyle.CircleWithCross,
            _ => defaultStyle
        };
    }
}
