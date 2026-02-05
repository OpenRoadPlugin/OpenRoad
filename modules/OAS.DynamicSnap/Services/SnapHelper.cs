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
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Discovery;
using OpenAsphalte.Modules.DynamicSnap.Models;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Helper statique pour l'intégration du module DynamicSnap dans d'autres modules.
/// Fournit des méthodes avec fallback automatique vers l'accrochage AutoCAD si le module n'est pas installé.
///
/// Utilisation recommandée dans les autres modules OAS:
/// <code>
/// // Au lieu de GetPoint classique:
/// var result = SnapHelper.GetPointOnPolylineOrFallback(
///     polyline,
///     "Sélectionnez un sommet:",
///     Editor,
///     SnapMode.Vertex);
///
/// if (result.HasValue)
/// {
///     Point3d point = result.Value;
///     // ...
/// }
/// </code>
/// </summary>
public static class SnapHelper
{
    /// <summary>
    /// Cache pour vérifier si le module est disponible
    /// </summary>
    private static bool? _isModuleAvailable;

    /// <summary>
    /// Vérifie si le module DynamicSnap est installé et disponible
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            if (!_isModuleAvailable.HasValue)
            {
                _isModuleAvailable = CheckModuleAvailable();
            }
            return _isModuleAvailable.Value && DynamicSnapService.IsAvailable;
        }
    }

    /// <summary>
    /// Force la revérification de la disponibilité du module
    /// </summary>
    public static void RefreshAvailability()
    {
        _isModuleAvailable = null;
    }

    /// <summary>
    /// Vérifie si le module DynamicSnap est chargé via ModuleDiscovery
    /// </summary>
    private static bool CheckModuleAvailable()
    {
        try
        {
            var module = ModuleDiscovery.GetModule("dynamicsnap");
            return module != null && module.IsInitialized;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sélectionne un point sur une polyligne avec accrochage OAS ou fallback AutoCAD.
    /// </summary>
    /// <param name="polyline">Polyligne sur laquelle sélectionner</param>
    /// <param name="prompt">Message à afficher</param>
    /// <param name="editor">Éditeur AutoCAD</param>
    /// <param name="modes">Modes d'accrochage souhaités (défaut: sommets + milieux)</param>
    /// <returns>Point sélectionné ou null si annulé</returns>
    public static Point3d? GetPointOnPolylineOrFallback(
        Polyline polyline,
        string prompt,
        Editor editor,
        SnapMode? modes = null)
    {
        if (IsAvailable)
        {
            // null = utiliser la configuration globale (config.json)
            SnapConfiguration? config = modes.HasValue
                ? new SnapConfiguration { ActiveModes = modes.Value }
                : null;
            var snapResult = DynamicSnapService.GetPointOnEntity(polyline, prompt, config);

            if (snapResult != null)
            {
                return snapResult.Point;
            }
            return null;
        }
        else
        {
            // Fallback vers l'accrochage AutoCAD classique
            return GetPointWithAutoCADSnap(editor, prompt);
        }
    }

    /// <summary>
    /// Sélectionne un sommet de polyligne avec accrochage OAS ou fallback.
    /// </summary>
    /// <param name="polyline">Polyligne source</param>
    /// <param name="prompt">Message à afficher</param>
    /// <param name="editor">Éditeur AutoCAD</param>
    /// <returns>Point du sommet ou null si annulé</returns>
    public static Point3d? GetVertexOrFallback(
        Polyline polyline,
        string prompt,
        Editor editor)
    {
        return GetPointOnPolylineOrFallback(
            polyline,
            prompt,
            editor,
            SnapMode.Vertex | SnapMode.Endpoint);
    }

    /// <summary>
    /// Sélectionne un point sur une entité avec accrochage OAS ou fallback.
    /// </summary>
    /// <param name="entity">Entité source</param>
    /// <param name="prompt">Message à afficher</param>
    /// <param name="editor">Éditeur AutoCAD</param>
    /// <param name="modes">Modes d'accrochage</param>
    /// <returns>Point sélectionné ou null si annulé</returns>
    public static Point3d? GetPointOnEntityOrFallback(
        Entity entity,
        string prompt,
        Editor editor,
        SnapMode? modes = null)
    {
        if (IsAvailable)
        {
            // null = utiliser la configuration globale (config.json)
            SnapConfiguration? config = modes.HasValue
                ? new SnapConfiguration { ActiveModes = modes.Value }
                : null;
            var snapResult = DynamicSnapService.GetPointOnEntity(entity, prompt, config);

            if (snapResult != null)
            {
                return snapResult.Point;
            }
            return null;
        }
        else
        {
            return GetPointWithAutoCADSnap(editor, prompt);
        }
    }

    /// <summary>
    /// Sélectionne un point sur une polyligne et retourne le SnapPoint complet
    /// (avec informations sur le mode, l'index de sommet, etc.)
    /// </summary>
    /// <param name="polyline">Polyligne source</param>
    /// <param name="prompt">Message à afficher</param>
    /// <param name="modes">Modes d'accrochage</param>
    /// <returns>SnapPoint complet ou null si annulé ou module non disponible</returns>
    public static SnapPoint? GetSnapPointOnPolyline(
        Polyline polyline,
        string prompt,
        SnapMode modes = SnapMode.PolylineFull)
    {
        if (!IsAvailable) return null;

        var config = new SnapConfiguration { ActiveModes = modes };
        return DynamicSnapService.GetPointOnEntity(polyline, prompt, config);
    }

    /// <summary>
    /// Retourne la distance curviligne du point d'accrochage sur une polyligne.
    /// Méthode utilitaire pour les calculs de station.
    /// </summary>
    /// <param name="polyline">Polyligne</param>
    /// <param name="prompt">Message</param>
    /// <returns>Distance curviligne ou null si annulé</returns>
    public static double? GetDistanceAlongPolyline(
        Polyline polyline,
        string prompt)
    {
        if (IsAvailable)
        {
            var snapPoint = GetSnapPointOnPolyline(polyline, prompt);
            if (snapPoint != null)
            {
                // Si on a la distance curviligne directement
                if (!double.IsNaN(snapPoint.DistanceAlongCurve))
                {
                    return snapPoint.DistanceAlongCurve;
                }

                // Sinon calculer à partir du point
                try
                {
                    var closestPt = polyline.GetClosestPointTo(snapPoint.Point, false);
                    return polyline.GetDistanceAtParameter(polyline.GetParameterAtPoint(closestPt));
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }
        else
        {
            // Fallback: GetPoint puis calcul de distance
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;

            var point = GetPointWithAutoCADSnap(doc.Editor, prompt);
            if (point == null) return null;

            try
            {
                var closestPt = polyline.GetClosestPointTo(point.Value, false);
                return polyline.GetDistanceAtParameter(polyline.GetParameterAtPoint(closestPt));
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Fallback vers l'accrochage AutoCAD classique
    /// </summary>
    private static Point3d? GetPointWithAutoCADSnap(Editor editor, string prompt)
    {
        var options = new PromptPointOptions($"\n{prompt}")
        {
            AllowNone = true
        };

        var result = editor.GetPoint(options);

        if (result.Status == PromptStatus.OK)
        {
            return result.Value;
        }

        return null;
    }

}
