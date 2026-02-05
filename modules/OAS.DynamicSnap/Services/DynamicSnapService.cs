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
using Config = OpenAsphalte.Configuration.Configuration;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Service principal d'accrochage dynamique OAS.
/// Fournit une API pour sélectionner des points avec accrochage visuel interactif
/// en temps réel via PointMonitor + TransientManager.
/// </summary>
public static class DynamicSnapService
{
    #region Constants

    private const string ConfigKeyActiveModes = "dynamicsnap.activeModes";
    private const string ConfigKeyMarkerColor = "dynamicsnap.markerColor";
    private const string ConfigKeyActiveMarkerColor = "dynamicsnap.activeMarkerColor";
    private const string ConfigKeyToleranceRatio = "dynamicsnap.toleranceRatio";
    private const string ConfigKeyMarkerSizeRatio = "dynamicsnap.markerSizeRatio";
    private const string ConfigKeyFilledMarkers = "dynamicsnap.filledMarkers";
    private const string ConfigKeyMarkerLineWeight = "dynamicsnap.markerLineWeight";

    #endregion

    #region Fields

    private static bool _isInitialized;

    #endregion

    #region Properties

    /// <summary>
    /// Indique si le service est initialisé et disponible
    /// </summary>
    public static bool IsAvailable => _isInitialized;

    /// <summary>
    /// Configuration par défaut utilisée si aucune n'est spécifiée.
    /// Chargée depuis les paramètres globaux (config.json).
    /// </summary>
    public static SnapConfiguration DefaultConfiguration { get; set; } = new();

    #endregion

    #region Initialization

    /// <summary>
    /// Initialise le service d'accrochage dynamique et charge les paramètres globaux
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized) return;

        LoadSettings();

        Logger.Debug(L10n.T("dynamicsnap.init", "[DynamicSnap] Service initialisé"));
        _isInitialized = true;
    }

    /// <summary>
    /// Arrête le service d'accrochage dynamique
    /// </summary>
    public static void Shutdown()
    {
        MarkerRenderer.ClearAllMarkers();
        _isInitialized = false;
        Logger.Debug(L10n.T("dynamicsnap.shutdown", "[DynamicSnap] Service arrêté"));
    }

    #endregion

    #region Settings

    /// <summary>
    /// Charge les paramètres depuis la configuration globale (config.json)
    /// </summary>
    public static void LoadSettings()
    {
        var config = DefaultConfiguration;

        config.ActiveModes = (SnapMode)Config.Get<int>(
            ConfigKeyActiveModes, (int)SnapMode.PolylineFull);
        config.MarkerColor = (short)Config.Get<int>(
            ConfigKeyMarkerColor, 3); // Vert
        config.ActiveMarkerColor = (short)Config.Get<int>(
            ConfigKeyActiveMarkerColor, 1); // Rouge
        config.ToleranceViewRatio = Config.Get<double>(
            ConfigKeyToleranceRatio, 0.02);
        config.MarkerViewRatio = Config.Get<double>(
            ConfigKeyMarkerSizeRatio, 0.015);
        config.FilledMarkers = Config.Get<bool>(
            ConfigKeyFilledMarkers, false);
        config.MarkerLineWeight = Config.Get<int>(
            ConfigKeyMarkerLineWeight, 30);

        DefaultConfiguration = config;
    }

    /// <summary>
    /// Sauvegarde les paramètres dans la configuration globale (config.json)
    /// </summary>
    public static void SaveSettings()
    {
        var config = DefaultConfiguration;

        Config.Set(ConfigKeyActiveModes, (int)config.ActiveModes);
        Config.Set(ConfigKeyMarkerColor, (int)config.MarkerColor);
        Config.Set(ConfigKeyActiveMarkerColor, (int)config.ActiveMarkerColor);
        Config.Set(ConfigKeyToleranceRatio, config.ToleranceViewRatio);
        Config.Set(ConfigKeyMarkerSizeRatio, config.MarkerViewRatio);
        Config.Set(ConfigKeyFilledMarkers, config.FilledMarkers);
        Config.Set(ConfigKeyMarkerLineWeight, config.MarkerLineWeight);

        Config.Save();
    }

    #endregion

    #region Main API

    /// <summary>
    /// Sélectionne un point sur une entité spécifique avec accrochage visuel en temps réel.
    /// Les marqueurs s'affichent dynamiquement pendant le déplacement de la souris.
    /// </summary>
    /// <param name="entity">Entité sur laquelle accrocher</param>
    /// <param name="prompt">Message de prompt à afficher</param>
    /// <param name="config">Configuration d'accrochage (null = défaut global)</param>
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
            tr.Commit();
            return null;
        }

        var result = GetPointOnEntity(entity, prompt, config);
        tr.Commit();
        return result;
    }

    /// <summary>
    /// Sélectionne un point sur plusieurs entités
    /// </summary>
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
    /// Exécute une session de sélection interactive avec PointMonitor temps réel.
    /// Les marqueurs d'accrochage s'affichent pendant le déplacement de la souris
    /// et le point le plus proche est automatiquement sélectionné au clic.
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

        // Calculer les paramètres dynamiques basés sur la vue
        double viewSize = Convert.ToDouble(AcadApp.GetSystemVariable("VIEWSIZE"));
        double tolerance = config.Tolerance > 0
            ? config.Tolerance
            : viewSize * config.ToleranceViewRatio;
        double markerSize = config.MarkerSize > 0
            ? config.MarkerSize
            : viewSize * config.MarkerViewRatio;

        var sessionConfig = config.Clone();
        sessionConfig.Tolerance = tolerance;
        sessionConfig.MarkerSize = markerSize;

        // Variable capturée par le PointMonitor pour stocker le meilleur snap courant
        SnapPoint? currentBestSnap = null;

        // Handler du PointMonitor : appelé en temps réel pendant le déplacement de la souris
        void OnPointMonitor(object? sender, PointMonitorEventArgs e)
        {
            try
            {
                var cursorPoint = e.Context.ComputedPoint;

                // Détecter les snap points sur toutes les entités
                var snapPoints = new List<SnapPoint>();
                foreach (var entity in entities)
                {
                    var points = SnapDetector.DetectSnapPoints(
                        entity, cursorPoint, sessionConfig.Tolerance, sessionConfig.ActiveModes);
                    snapPoints.AddRange(points);
                }

                if (snapPoints.Count > 0)
                {
                    // Stocker le meilleur candidat (trié par priorité puis distance)
                    currentBestSnap = snapPoints[0];

                    // Mettre à jour les marqueurs visuels en temps réel
                    MarkerRenderer.UpdateMarkers(snapPoints, sessionConfig);
                }
                else
                {
                    currentBestSnap = null;
                    MarkerRenderer.ClearAllMarkers();
                }
            }
            catch
            {
                // Le PointMonitor ne doit jamais crasher
            }
        }

        try
        {
            // S'abonner au PointMonitor pour le suivi temps réel
            editor.PointMonitor += OnPointMonitor;

            // Demander le point à l'utilisateur (bloquant, mais le PointMonitor tourne en parallèle)
            var ppo = new PromptPointOptions($"\n{prompt}")
            {
                AllowNone = true
            };

            var result = editor.GetPoint(ppo);

            if (result.Status == PromptStatus.OK)
            {
                // Si on a un snap candidat proche du clic, l'utiliser
                if (currentBestSnap != null)
                {
                    return currentBestSnap;
                }

                // Sinon, tenter une dernière détection au point cliqué
                var clickPoint = result.Value;
                var fallbackSnaps = new List<SnapPoint>();
                foreach (var entity in entities)
                {
                    var points = SnapDetector.DetectSnapPoints(
                        entity, clickPoint, sessionConfig.Tolerance, sessionConfig.ActiveModes);
                    fallbackSnaps.AddRange(points);
                }

                if (fallbackSnaps.Count > 0)
                {
                    return fallbackSnaps[0];
                }

                // Aucun snap trouvé — informer l'utilisateur
                Logger.Info(L10n.T("dynamicsnap.nosnap",
                    "Aucun point d'accrochage trouvé. Cliquez plus près d'un point."));
            }

            // Annulation ou aucun snap
            return null;
        }
        finally
        {
            // Toujours se désabonner et nettoyer
            editor.PointMonitor -= OnPointMonitor;
            MarkerRenderer.ClearAllMarkers();

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
            return 1.0;
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
            return 0.5;
        }
    }

    #endregion
}
