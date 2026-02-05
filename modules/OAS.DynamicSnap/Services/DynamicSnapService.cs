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

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.DynamicSnap.Models;
using L10n = OpenAsphalte.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Service principal d'accrochage dynamique OAS.
/// Fournit une API pour sélectionner des points avec accrochage visuel interactif.
/// </summary>
public static class DynamicSnapService
{
    #region Fields

    private static bool _isInitialized = false;

    #endregion

    #region Properties

    /// <summary>
    /// Indique si le service est initialisé et disponible
    /// </summary>
    public static bool IsAvailable => _isInitialized;

    /// <summary>
    /// Configuration par défaut utilisée si aucune n'est spécifiée
    /// </summary>
    public static SnapConfiguration DefaultConfiguration { get; set; } = new();

    #endregion

    #region Initialization

    /// <summary>
    /// Initialise le service d'accrochage dynamique
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        Logger.Debug(L10n.T("dynamicsnap.init", "[DynamicSnap] Service initialisé"));
        _isInitialized = true;
    }

    /// <summary>
    /// Arrête le service d'accrochage dynamique
    /// </summary>
    public static void Shutdown()
    {
        _isInitialized = false;
        Logger.Debug(L10n.T("dynamicsnap.shutdown", "[DynamicSnap] Service arrêté"));
    }

    #endregion

    #region Main API

    /// <summary>
    /// Sélectionne un point sur une entité spécifique avec accrochage visuel.
    /// C'est la méthode principale pour sélectionner un sommet, milieu, etc.
    /// </summary>
    /// <param name="entity">Entité sur laquelle accrocher</param>
    /// <param name="prompt">Message de prompt à afficher</param>
    /// <param name="config">Configuration d'accrochage (null = défaut)</param>
    /// <returns>Point sélectionné ou null si annulé</returns>
    public static SnapPoint? GetPointOnEntity(
        Entity entity,
        string prompt,
        SnapConfiguration? config = null)
    {
        if (entity == null)
        {
            Logger.Warning(L10n.T("dynamicsnap.noentity", "Aucune entité fournie"));
            return null;
        }

        var cfg = config ?? DefaultConfiguration;
        return RunSnapSession(new[] { entity }, prompt, cfg);
    }

    /// <summary>
    /// Sélectionne un point sur une entité spécifiée par ObjectId
    /// </summary>
    /// <param name="entityId">ObjectId de l'entité</param>
    /// <param name="prompt">Message de prompt</param>
    /// <param name="config">Configuration d'accrochage</param>
    /// <returns>Point sélectionné ou null si annulé</returns>
    public static SnapPoint? GetPointOnEntity(
        ObjectId entityId,
        string prompt,
        SnapConfiguration? config = null)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc == null) return null;

        using var tr = doc.TransactionManager.StartTransaction();
        var entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;

        if (entity == null)
        {
            Logger.Warning(L10n.T("dynamicsnap.invalidid", "ObjectId invalide"));
            return null;
        }

        var result = GetPointOnEntity(entity, prompt, config);
        tr.Commit();
        return result;
    }

    /// <summary>
    /// Sélectionne un point sur plusieurs entités
    /// </summary>
    /// <param name="entities">Collection d'entités</param>
    /// <param name="prompt">Message de prompt</param>
    /// <param name="config">Configuration d'accrochage</param>
    /// <returns>Point sélectionné ou null si annulé</returns>
    public static SnapPoint? GetPointOnEntities(
        IEnumerable<Entity> entities,
        string prompt,
        SnapConfiguration? config = null)
    {
        var cfg = config ?? DefaultConfiguration;
        return RunSnapSession(entities.ToArray(), prompt, cfg);
    }

    /// <summary>
    /// Sélectionne un sommet sur une polyligne (mode Vertex uniquement)
    /// </summary>
    /// <param name="polyline">Polyligne source</param>
    /// <param name="prompt">Message de prompt</param>
    /// <returns>Point du sommet ou null si annulé</returns>
    public static SnapPoint? GetVertexOnPolyline(
        Polyline polyline,
        string prompt)
    {
        var config = SnapConfiguration.VerticesOnly();
        return GetPointOnEntity(polyline, prompt, config);
    }

    /// <summary>
    /// Sélectionne un point quelconque sur une polyligne
    /// </summary>
    /// <param name="polyline">Polyligne source</param>
    /// <param name="prompt">Message de prompt</param>
    /// <returns>Point d'accrochage ou null si annulé</returns>
    public static SnapPoint? GetPointOnPolyline(
        Polyline polyline,
        string prompt)
    {
        var config = SnapConfiguration.ForPolylines();
        return GetPointOnEntity(polyline, prompt, config);
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Exécute une session de sélection interactive avec grread
    /// </summary>
    private static SnapPoint? RunSnapSession(
        Entity[] entities,
        string prompt,
        SnapConfiguration config)
    {
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc == null) return null;

        var editor = doc.Editor;

        // Sauvegarder et désactiver OSNAP AutoCAD si demandé
        int savedOsmode = 0;
        if (config.DisableAutoCADOsnap)
        {
            savedOsmode = Convert.ToInt32(AcadApp.GetSystemVariable("OSMODE"));
            AcadApp.SetSystemVariable("OSMODE", 0);
        }

        try
        {
            // Afficher le prompt
            editor.WriteMessage($"\n{prompt}");

            // Calculer les paramètres dynamiques
            double viewSize = Convert.ToDouble(AcadApp.GetSystemVariable("VIEWSIZE"));
            double tolerance = config.Tolerance > 0
                ? config.Tolerance
                : viewSize * config.ToleranceViewRatio;
            double markerSize = config.MarkerSize > 0
                ? config.MarkerSize
                : viewSize * config.MarkerViewRatio;

            // Créer une copie de config avec les valeurs calculées
            var sessionConfig = config.Clone();
            sessionConfig.Tolerance = tolerance;
            sessionConfig.MarkerSize = markerSize;

            SnapPoint? selectedPoint = null;

            // Boucle de sélection
            while (selectedPoint == null)
            {
                var grResult = editor.GetPoint(new PromptPointOptions("") { AllowNone = true });

                // Si l'utilisateur a cliqué (pas juste déplacé la souris)
                if (grResult.Status == PromptStatus.OK)
                {
                    var clickPoint = grResult.Value;

                    // Chercher le meilleur snap point
                    var snapPoints = new List<SnapPoint>();
                    foreach (var entity in entities)
                    {
                        var points = SnapDetector.DetectSnapPoints(
                            entity, clickPoint, tolerance, sessionConfig.ActiveModes);
                        snapPoints.AddRange(points);
                    }

                    if (snapPoints.Count > 0)
                    {
                        // Prendre le meilleur (déjà trié par priorité et distance)
                        selectedPoint = snapPoints[0];
                    }
                    else
                    {
                        // Aucun point d'accrochage trouvé dans la tolérance
                        Logger.Info(L10n.T("dynamicsnap.nosnap",
                            "Aucun point d'accrochage trouvé. Cliquez plus près d'un point."));
                    }
                }
                else if (grResult.Status == PromptStatus.Cancel ||
                         grResult.Status == PromptStatus.None)
                {
                    // Annulation par l'utilisateur
                    break;
                }
            }

            // Effacer les marqueurs
            MarkerRenderer.ClearAllMarkers();

            return selectedPoint;
        }
        finally
        {
            // Restaurer OSNAP
            if (config.DisableAutoCADOsnap)
            {
                AcadApp.SetSystemVariable("OSMODE", savedOsmode);
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Vérifie si le module DynamicSnap est disponible dans l'environnement actuel
    /// </summary>
    public static bool IsModuleAvailable()
    {
        return _isInitialized;
    }

    /// <summary>
    /// Retourne la liste des points d'accrochage possibles sur une entité
    /// (sans interaction utilisateur)
    /// </summary>
    public static List<SnapPoint> GetAvailableSnapPoints(
        Entity entity,
        Point3d referencePoint,
        SnapMode modes,
        double tolerance)
    {
        return SnapDetector.DetectSnapPoints(entity, referencePoint, tolerance, modes);
    }

    /// <summary>
    /// Trouve le point d'accrochage le plus proche
    /// </summary>
    public static SnapPoint? FindNearestSnapPoint(
        Entity entity,
        Point3d referencePoint,
        SnapMode modes,
        double tolerance)
    {
        var points = SnapDetector.DetectSnapPoints(entity, referencePoint, tolerance, modes);
        return points.FirstOrDefault();
    }

    /// <summary>
    /// Calcule la tolérance dynamique basée sur la vue actuelle
    /// </summary>
    public static double CalculateDynamicTolerance(double viewRatio = 0.02)
    {
        try
        {
            double viewSize = Convert.ToDouble(AcadApp.GetSystemVariable("VIEWSIZE"));
            return viewSize * viewRatio;
        }
        catch
        {
            return 1.0; // Valeur par défaut
        }
    }

    /// <summary>
    /// Calcule la taille du marqueur basée sur la vue actuelle
    /// </summary>
    public static double CalculateDynamicMarkerSize(double viewRatio = 0.015)
    {
        try
        {
            double viewSize = Convert.ToDouble(AcadApp.GetSystemVariable("VIEWSIZE"));
            return viewSize * viewRatio;
        }
        catch
        {
            return 0.5; // Valeur par défaut
        }
    }

    #endregion
}
