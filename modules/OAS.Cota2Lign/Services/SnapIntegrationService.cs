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
using OpenAsphalte.Logging;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.Cota2Lign.Services;

/// <summary>
/// Service d'accrochage intelligent pour le module Cota2Lign.
/// Utilise le module DynamicSnap si disponible et activé dans les settings,
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
            var module = ModuleDiscovery.GetModule("dynamicsnap");
            _isDynamicSnapAvailable = module != null && module.IsInitialized;

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
        catch
        {
            _isDynamicSnapAvailable = false;
        }
    }

    /// <summary>
    /// Sélectionne un point sur une polyligne avec l'accrochage configuré.
    /// Utilise DynamicSnap si disponible et activé dans les settings,
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
            var result = GetPointWithDynamicSnap(polylineId, prompt, editor, database, settings!);
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
    /// Sélection avec le module DynamicSnap.
    /// Utilise la réflexion pour éviter une dépendance directe sur le module.
    /// </summary>
    private static Point3d? GetPointWithDynamicSnap(
        ObjectId polylineId,
        string prompt,
        Editor editor,
        Database database,
        Cota2LignSettings settings)
    {
        try
        {
            // Accès dynamique au module DynamicSnap via réflexion
            var snapHelperType = Type.GetType(
                "OpenAsphalte.Modules.DynamicSnap.Services.SnapHelper, OAS.DynamicSnap");

            if (snapHelperType == null)
            {
                Logger.Debug("[Cota2Lign] Type SnapHelper non trouvé");
                return null;
            }

            // Vérifier IsAvailable
            var isAvailableProp = snapHelperType.GetProperty("IsAvailable",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (isAvailableProp == null)
            {
                Logger.Debug("[Cota2Lign] Propriété IsAvailable non trouvée");
                return null;
            }

            var isAvailable = isAvailableProp.GetValue(null);
            if (isAvailable == null || !(bool)isAvailable)
            {
                Logger.Debug("[Cota2Lign] DynamicSnap.IsAvailable = false");
                return null;
            }

            // Construire le SnapMode selon les settings
            var snapModeType = Type.GetType(
                "OpenAsphalte.Modules.DynamicSnap.Models.SnapMode, OAS.DynamicSnap");

            if (snapModeType == null)
            {
                Logger.Debug("[Cota2Lign] Type SnapMode non trouvé");
                return null;
            }

            // Combiner les modes selon les settings
            int modeValue = 0;

            if (settings.SnapVertex)
            {
                // Vertex = 1, Endpoint = 2 (on prend les deux)
                modeValue |= 1 | 2;
            }
            if (settings.SnapMidpoint)
            {
                // Midpoint = 4
                modeValue |= 4;
            }
            if (settings.SnapNearest)
            {
                // Nearest = 8
                modeValue |= 8;
            }

            // Au moins un mode doit être actif
            if (modeValue == 0)
            {
                modeValue = 1 | 2 | 4 | 8; // PolylineFull par défaut
            }

            var snapMode = Enum.ToObject(snapModeType, modeValue);

            // Récupérer la polyligne dans une transaction courte pour la lecture
            // Note: On garde la transaction ouverte pendant toute l'opération de sélection
            // pour éviter les problèmes d'accès à l'entité pendant le GetPoint interactif
            using var tr = database.TransactionManager.StartTransaction();
            try
            {
                var polyline = tr.GetObject(polylineId, OpenMode.ForRead) as Polyline;
                if (polyline == null)
                {
                    tr.Abort();
                    return null;
                }

                // Appeler GetPointOnPolylineOrFallback via réflexion
                var method = snapHelperType.GetMethod("GetPointOnPolylineOrFallback",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (method == null)
                {
                    Logger.Debug("[Cota2Lign] Méthode GetPointOnPolylineOrFallback non trouvée");
                    tr.Abort();
                    return null;
                }

                var args = new object[] { polyline, prompt, editor, snapMode };
                var pointResult = method.Invoke(null, args);

                Point3d? result = null;
                if (pointResult is Point3d pt)
                {
                    // Projeter le résultat sur la polyligne pour garantir la précision
                    result = polyline.GetClosestPointTo(pt, false);
                }

                tr.Commit();
                return result;
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                // Extraction de l'exception interne pour un meilleur diagnostic
                var innerEx = tie.InnerException ?? tie;
                Logger.Debug($"[Cota2Lign] Erreur DynamicSnap: {innerEx.Message}");
                try { tr.Abort(); } catch { }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Debug($"[Cota2Lign] Erreur dans transaction DynamicSnap: {ex.Message}");
                try { tr.Abort(); } catch { }
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"[Cota2Lign] Erreur générale DynamicSnap: {ex.Message}");
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
            catch
            {
                tr.Abort();
                return point; // Retourner le point original en cas d'erreur
            }
        }
        catch
        {
            return point;
        }
    }

    /// <summary>
    /// Obtient la distance curviligne d'un point sur une polyligne
    /// </summary>
    public static double GetDistanceAlongPolyline(
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
                    return 0;
                }

                var closestPoint = polyline.GetClosestPointTo(point, false);
                var distance = polyline.GetDistAtPoint(closestPoint);
                tr.Commit();
                return distance;
            }
            catch
            {
                tr.Abort();
                return 0;
            }
        }
        catch
        {
            return 0;
        }
    }
}
