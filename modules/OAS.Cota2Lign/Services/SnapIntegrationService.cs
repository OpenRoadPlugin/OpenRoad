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
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.DynamicSnap.Services;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.Cota2Lign.Services;

/// <summary>
/// Service d'accrochage intelligent pour le module Cota2Lign.
/// Utilise le module DynamicSnap (via SnapHelper) si disponible et activé,
/// sinon fallback vers OSNAP AutoCAD.
/// </summary>
public static class SnapIntegrationService
{
    private static bool? _isDynamicSnapAvailable;
    private static bool _checkedOnce = false;

    /// <summary>
    /// Vérifie si le module DynamicSnap est disponible
    /// </summary>
    public static bool IsDynamicSnapAvailable
    {
        get
        {
            if (!_checkedOnce)
            {
                CheckDynamicSnapAvailability();
                _checkedOnce = true;
            }
            return _isDynamicSnapAvailable ?? false;
        }
    }

    /// <summary>
    /// Force une revérification de la disponibilité
    /// </summary>
    public static void RefreshAvailability()
    {
        _checkedOnce = false;
        _isDynamicSnapAvailable = null;
    }

    /// <summary>
    /// Vérifie si le module DynamicSnap est chargé
    /// </summary>
    private static void CheckDynamicSnapAvailability()
    {
        try
        {
            _isDynamicSnapAvailable = SnapHelper.IsAvailable;

            if (_isDynamicSnapAvailable == true)
            {
                Logger.Debug(L10n.T("cota2lign.snap.dynamicsnap",
                    "[Cota2Lign] Module DynamicSnap détecté - accrochage intelligent disponible"));
            }
            else
            {
                Logger.Debug(L10n.T("cota2lign.snap.fallback",
                    "[Cota2Lign] Module DynamicSnap non disponible - accrochage AutoCAD utilisé"));
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"[Cota2Lign] Exception checking DynamicSnap availability: {ex.Message}");
            _isDynamicSnapAvailable = false;
        }
    }

    /// <summary>
    /// Sélectionne un point sur une polyligne avec l'accrochage configuré.
    /// Utilise DynamicSnap (paramètres globaux) si disponible et activé,
    /// sinon OSNAP AutoCAD.
    /// </summary>
    /// <param name="polylineId">ObjectId de la polyligne</param>
    /// <param name="prompt">Message de prompt</param>
    /// <param name="editor">Éditeur AutoCAD</param>
    /// <param name="database">Database AutoCAD</param>
    /// <param name="settings">Paramètres du module (optionnel)</param>
    /// <returns>Point projeté sur la polyligne ou null si annulé</returns>
    public static Point3d? GetPointOnPolyline(
        ObjectId polylineId,
        string prompt,
        Editor editor,
        Database database,
        Cota2LignSettings? settings = null)
    {
        // Vérifier si on doit utiliser l'accrochage OAS
        bool useOasSnap = settings?.UseOasSnap == true && IsDynamicSnapAvailable;

        if (useOasSnap)
        {
            var result = GetPointWithDynamicSnap(polylineId, prompt, editor, database);
            if (result.HasValue)
            {
                return result;
            }
            // Si DynamicSnap échoue, fallback vers AutoCAD
            Logger.Debug("[Cota2Lign] DynamicSnap retourné null, fallback vers OSNAP");
        }

        // Utiliser l'accrochage AutoCAD classique
        return GetPointWithAutoCADSnap(polylineId, prompt, editor, database);
    }

    /// <summary>
    /// Sélection avec le module DynamicSnap via SnapHelper (appel direct).
    /// Les modes d'accrochage utilisés sont ceux configurés globalement (config.json).
    /// </summary>
    private static Point3d? GetPointWithDynamicSnap(
        ObjectId polylineId,
        string prompt,
        Editor editor,
        Database database)
    {
        try
        {
            if (!SnapHelper.IsAvailable)
            {
                Logger.Debug("[Cota2Lign] DynamicSnap.IsAvailable = false");
                return null;
            }

            // Ouvrir la polyligne en lecture
            using var tr = database.TransactionManager.StartTransaction();
            var polyline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;
            if (polyline == null)
            {
                tr.Abort();
                return null;
            }

            // Appel direct à SnapHelper (modes = null → paramètres globaux config.json)
            var result = SnapHelper.GetPointOnPolylineOrFallback(polyline, prompt, editor);

            if (result.HasValue)
            {
                // Projeter le résultat sur la polyligne pour garantir la précision
                var projected = polyline.GetClosestPointTo(result.Value, false);
                tr.Commit();
                return projected;
            }

            tr.Commit();
            return null;
        }
        catch (Exception ex)
        {
            Logger.Debug($"[Cota2Lign] Erreur DynamicSnap: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sélection avec l'accrochage AutoCAD classique (OSNAP)
    /// </summary>
    private static Point3d? GetPointWithAutoCADSnap(
        ObjectId polylineId,
        string prompt,
        Editor editor,
        Database database)
    {
        var ppo = new PromptPointOptions($"\n{prompt}: ")
        {
            AllowNone = true
        };

        var result = editor.GetPoint(ppo);

        if (result.Status != PromptStatus.OK)
        {
            return null;
        }

        // Projeter le point sur la polyligne
        return ProjectPointOnPolyline(polylineId, result.Value, database);
    }

    /// <summary>
    /// Projette un point sur une polyligne de manière sécurisée
    /// </summary>
    private static Point3d? ProjectPointOnPolyline(
        ObjectId polylineId,
        Point3d point,
        Database database)
    {
        try
        {
            using var tr = database.TransactionManager.StartTransaction();
            try
            {
                var polyline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;
                if (polyline == null)
                {
                    tr.Abort();
                    return null;
                }

                var projectedPoint = polyline.GetClosestPointTo(point, false);
                tr.Commit();
                return projectedPoint;
            }
            catch (Exception ex)
            {
                Logger.Debug($"[Cota2Lign] Error projecting point on polyline: {ex.Message}");
                tr.Abort();
                return point; // Retourner le point original en cas d'erreur
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"[Cota2Lign] Error opening transaction for projection: {ex.Message}");
            return point;
        }
    }

}
