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
using Autodesk.AutoCAD.GraphicsInterface;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.DynamicSnap.Models;
using AcColor = Autodesk.AutoCAD.Colors.Color;
using AcColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Service de surbrillance des entités sélectionnées.
/// Utilise TransientManager pour superposer des clones colorés temporaires
/// sur les entités du dessin, sans modifier la base de données.
///
/// Deux niveaux visuels :
/// - Primary : trait continu + épaisseur forte (entité active)
/// - Secondary : trait pointillé + épaisseur fine (entités en arrière-plan)
/// </summary>
public static class EntityHighlightService
{
    #region Fields

    /// <summary>
    /// Entités transitoires par ObjectId source
    /// </summary>
    private static readonly Dictionary<ObjectId, List<Drawable>> _highlightedEntities = new();

    /// <summary>
    /// Ensemble ordonné des ObjectIds actuellement mis en surbrillance
    /// </summary>
    private static readonly List<ObjectId> _highlightedIds = new();

    /// <summary>
    /// ObjectId de l'entité principale (active) — ObjectId.Null si pas de distinction
    /// </summary>
    private static ObjectId _primaryEntityId = ObjectId.Null;

    /// <summary>
    /// Cache du linetype DASHED pour les entités secondaires
    /// </summary>
    private static ObjectId _dashedLinetypeId = ObjectId.Null;

    /// <summary>
    /// Verrou pour accès thread-safe
    /// </summary>
    private static readonly object _lock = new();

    /// <summary>
    /// Configuration de surbrillance courante
    /// </summary>
    private static HighlightConfiguration _config = new();

    #endregion

    #region Properties

    /// <summary>
    /// Indique si la surbrillance est activée dans la configuration
    /// </summary>
    public static bool IsEnabled => _config.Enabled;

    /// <summary>
    /// Configuration courante de surbrillance
    /// </summary>
    public static HighlightConfiguration Configuration
    {
        get => _config;
        set => _config = value ?? new HighlightConfiguration();
    }

    /// <summary>
    /// Nombre d'entités actuellement mises en surbrillance
    /// </summary>
    public static int HighlightedCount
    {
        get
        {
            lock (_lock) { return _highlightedIds.Count; }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Met en surbrillance une ou plusieurs entités.
    /// Toutes les entités reçoivent le style Primary (trait continu, épaisseur forte).
    /// Appeler <see cref="SetPrimaryEntity"/> ensuite pour distinguer une entité active.
    /// </summary>
    /// <param name="entityIds">ObjectIds des entités à mettre en surbrillance</param>
    public static void HighlightEntities(params ObjectId[] entityIds)
    {
        lock (_lock)
        {
            if (!_config.Enabled || entityIds.Length == 0) return;

            // Nettoyer les highlights existants
            EraseAllTransients();
            _highlightedIds.Clear();
            _primaryEntityId = ObjectId.Null;

            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                using var tr = doc.TransactionManager.StartTransaction();

                var color = AcColor.FromColorIndex(AcColorMethod.ByAci, _config.HighlightColor);
                var primaryLw = IntToLineWeight(_config.PrimaryLineWeight);

                foreach (var entityId in entityIds)
                {
                    if (entityId.IsNull || entityId.IsErased) continue;

                    try
                    {
                        var entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                        if (entity == null) continue;

                        var drawables = CreateHighlightClone(entity, color, primaryLw, ObjectId.Null);
                        if (drawables.Count > 0)
                        {
                            AddTransients(entityId, drawables);
                            _highlightedIds.Add(entityId);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Debug($"EntityHighlightService: Highlight entity {entityId}: {ex.Message}");
                    }
                }

                tr.Commit();
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"EntityHighlightService: HighlightEntities: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Définit une entité comme "principale" (active).
    /// L'entité principale garde le trait continu épais.
    /// Les autres entités passent en trait pointillé fin (opacité simulée).
    /// </summary>
    /// <param name="primaryId">ObjectId de l'entité principale</param>
    public static void SetPrimaryEntity(ObjectId primaryId)
    {
        lock (_lock)
        {
            if (!_config.Enabled) return;
            if (_highlightedIds.Count == 0) return;

            _primaryEntityId = primaryId;

            // Reconstruire tous les transients avec le nouveau style
            RebuildAllTransients();
        }
    }

    /// <summary>
    /// Supprime la surbrillance de toutes les entités
    /// </summary>
    public static void ClearHighlight()
    {
        lock (_lock)
        {
            EraseAllTransients();
            _highlightedIds.Clear();
            _primaryEntityId = ObjectId.Null;
            _dashedLinetypeId = ObjectId.Null;
        }
    }

    /// <summary>
    /// Supprime la surbrillance d'une entité spécifique
    /// </summary>
    /// <param name="entityId">ObjectId de l'entité à dé-surbriller</param>
    public static void ClearHighlight(ObjectId entityId)
    {
        lock (_lock)
        {
            EraseTransients(entityId);
            _highlightedIds.Remove(entityId);

            if (_primaryEntityId == entityId)
                _primaryEntityId = ObjectId.Null;
        }
    }

    /// <summary>
    /// Vérifie si une entité est actuellement mise en surbrillance
    /// </summary>
    public static bool IsHighlighted(ObjectId entityId)
    {
        lock (_lock)
        {
            return _highlightedIds.Contains(entityId);
        }
    }

    #endregion

    #region Private Methods - Transient Management

    /// <summary>
    /// Reconstruit tous les transients en appliquant le style Primary/Secondary
    /// selon l'entité principale courante.
    /// </summary>
    private static void RebuildAllTransients()
    {
        // Sauvegarder les IDs avant de tout effacer
        var ids = _highlightedIds.ToArray();
        EraseAllTransients();

        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc == null) return;

        try
        {
            // Charger le linetype DASHED si nécessaire et si on a un primary
            if (!_primaryEntityId.IsNull)
            {
                _dashedLinetypeId = GetDashedLinetypeId(doc.Database);
            }

            using var tr = doc.TransactionManager.StartTransaction();

            var color = AcColor.FromColorIndex(AcColorMethod.ByAci, _config.HighlightColor);
            var primaryLw = IntToLineWeight(_config.PrimaryLineWeight);
            var secondaryLw = IntToLineWeight(_config.SecondaryLineWeight);

            foreach (var id in ids)
            {
                if (id.IsNull || id.IsErased) continue;

                try
                {
                    var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null) continue;

                    bool isPrimary = _primaryEntityId.IsNull || id == _primaryEntityId;
                    var lw = isPrimary ? primaryLw : secondaryLw;
                    var linetypeId = isPrimary ? ObjectId.Null : _dashedLinetypeId;

                    var drawables = CreateHighlightClone(entity, color, lw, linetypeId);
                    if (drawables.Count > 0)
                    {
                        AddTransients(id, drawables);
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Debug($"EntityHighlightService: Rebuild {id}: {ex.Message}");
                }
            }

            tr.Commit();
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"EntityHighlightService: RebuildAllTransients: {ex.Message}");
        }
    }

    /// <summary>
    /// Crée un clone transient d'une entité avec les propriétés visuelles spécifiées.
    /// Supporte : Polyline, Line, Circle, Arc, Ellipse, Spline et toute Entity clonable.
    /// </summary>
    private static List<Drawable> CreateHighlightClone(
        Entity entity,
        AcColor color,
        LineWeight lineWeight,
        ObjectId linetypeId)
    {
        var drawables = new List<Drawable>();

        try
        {
            var clone = (Entity)entity.Clone();
            clone.Color = color;
            clone.LineWeight = lineWeight;

            // Appliquer le linetype (DASHED) pour les entités secondaires
            if (!linetypeId.IsNull)
            {
                try
                {
                    clone.LinetypeId = linetypeId;
                }
                catch
                {
                    // Si le linetype ne peut pas être appliqué, on continue sans
                    Logger.Debug("EntityHighlightService: LinetypeId assignment failed, using continuous");
                }
            }

            drawables.Add(clone);
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"EntityHighlightService: Clone failed for {entity.GetType().Name}: {ex.Message}");
        }

        return drawables;
    }

    /// <summary>
    /// Ajoute des drawables transients pour une entité et les enregistre dans le tracking
    /// </summary>
    private static void AddTransients(ObjectId entityId, List<Drawable> drawables)
    {
        if (!_highlightedEntities.ContainsKey(entityId))
            _highlightedEntities[entityId] = new List<Drawable>();

        foreach (var drawable in drawables)
        {
            try
            {
                TransientManager.CurrentTransientManager.AddTransient(
                    drawable,
                    TransientDrawingMode.DirectTopmost,
                    128,
                    new IntegerCollection());
                _highlightedEntities[entityId].Add(drawable);
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"EntityHighlightService: AddTransient: {ex.Message}");
                drawable.Dispose();
            }
        }
    }

    /// <summary>
    /// Efface tous les transients de surbrillance
    /// </summary>
    private static void EraseAllTransients()
    {
        foreach (var kvp in _highlightedEntities)
        {
            foreach (var drawable in kvp.Value)
            {
                try
                {
                    TransientManager.CurrentTransientManager.EraseTransient(
                        drawable, new IntegerCollection());
                    drawable.Dispose();
                }
                catch (System.Exception ex)
                {
                    Logger.Debug($"EntityHighlightService: EraseTransient: {ex.Message}");
                }
            }
        }
        _highlightedEntities.Clear();
    }

    /// <summary>
    /// Efface les transients d'une entité spécifique
    /// </summary>
    private static void EraseTransients(ObjectId entityId)
    {
        if (!_highlightedEntities.TryGetValue(entityId, out var drawables)) return;

        foreach (var drawable in drawables)
        {
            try
            {
                TransientManager.CurrentTransientManager.EraseTransient(
                    drawable, new IntegerCollection());
                drawable.Dispose();
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"EntityHighlightService: EraseTransient {entityId}: {ex.Message}");
            }
        }
        _highlightedEntities.Remove(entityId);
    }

    #endregion

    #region Private Methods - Linetype

    /// <summary>
    /// Récupère (ou charge) l'ObjectId du linetype DASHED depuis la base de données.
    /// Utilisé pour l'effet "opacité simulée" des entités secondaires.
    /// </summary>
    private static ObjectId GetDashedLinetypeId(Database db)
    {
        // Vérifier le cache
        if (!_dashedLinetypeId.IsNull)
            return _dashedLinetypeId;

        try
        {
            // Vérifier si DASHED existe déjà
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (ltTable.Has("DASHED"))
                {
                    _dashedLinetypeId = ltTable["DASHED"];
                    tr.Commit();
                    return _dashedLinetypeId;
                }
                tr.Commit();
            }

            // Tenter de charger depuis acad.lin
            try
            {
                db.LoadLineTypeFile("DASHED", "acad.lin");
            }
            catch
            {
                // Fichier lin introuvable ou autre erreur — on continue sans
                Logger.Debug("EntityHighlightService: Could not load DASHED linetype from acad.lin");
                return ObjectId.Null;
            }

            // Relire l'ID après chargement
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ltTable = (LinetypeTable)tr.GetObject(db.LinetypeTableId, OpenMode.ForRead);
                if (ltTable.Has("DASHED"))
                {
                    _dashedLinetypeId = ltTable["DASHED"];
                }
                tr.Commit();
            }

            return _dashedLinetypeId;
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"EntityHighlightService: GetDashedLinetypeId: {ex.Message}");
            return ObjectId.Null;
        }
    }

    #endregion

    #region Private Methods - Utility

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

    #endregion
}
