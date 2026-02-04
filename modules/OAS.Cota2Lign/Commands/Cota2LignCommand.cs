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

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using OpenAsphalte.Modules.Cota2Lign.Services;
using OpenAsphalte.Modules.Cota2Lign.Views;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.Cota2Lign.Commands;

/// <summary>
/// Commande pour créer des cotations alignées entre deux polylignes.
/// Supporte l'interdistance, la cotation aux sommets et la prévisualisation.
/// </summary>
public class Cota2LignCommand : CommandBase
{
    #region Constants

    /// <summary>
    /// Nom de l'application pour les XData (stockage paramètres dans le dessin)
    /// </summary>
    private const string XDataAppName = "OAS_COTA2LIGN";

    #endregion

    /// <summary>
    /// Exécute la commande de cotation entre deux lignes
    /// </summary>
    [CommandMethod("OAS_COTA2LIGN")]
    [CommandInfo("Cotation entre 2 lignes",
        Description = "Crée des cotations alignées entre deux polylignes",
        DisplayNameKey = "cota2lign.cmd.title",
        DescriptionKey = "cota2lign.cmd.desc",
        MenuCategory = "Dessin",
        MenuCategoryKey = "menu.dessin",
        MenuSubCategory = "Cotations",
        MenuSubCategoryKey = "menu.cotations",
        Order = 10,
        RibbonSize = CommandSize.Large,
        Group = "Cotations",
        ShowInMenu = true,
        ShowInRibbon = true)]
    public void Execute()
    {
        ExecuteSafe(() =>
        {
            // Charger les paramètres depuis le dessin
            var settings = Cota2LignSettings.LoadFromDrawing(Database!);

            // ═══════════════════════════════════════════════════════
            // SÉLECTION POLYLIGNE 1 (RÉFÉRENCE)
            // ═══════════════════════════════════════════════════════
            WriteMessage($"\n{T("cota2lign.select.pl1")} [{T("cota2lign.option.params")}]: ");

            var pl1Result = SelectPolylineWithOptions(T("cota2lign.select.pl1"));
            if (pl1Result == null) return;

            if (pl1Result.Value.openSettings)
            {
                // Ouvrir les paramètres
                OpenSettingsWindow(settings);
                return;
            }

            var polyline1Id = pl1Result.Value.objectId;

            // ═══════════════════════════════════════════════════════
            // SÉLECTION POLYLIGNE 2 (CIBLE)
            // ═══════════════════════════════════════════════════════
            var pl2Result = SelectPolyline(T("cota2lign.select.pl2"), polyline1Id);
            if (pl2Result == null)
            {
                Logger.Info(T("cota2lign.cancelled"));
                return;
            }

            var polyline2Id = pl2Result.Value;

            // ═══════════════════════════════════════════════════════
            // SÉLECTION POINTS DÉPART/ARRIVÉE
            // ═══════════════════════════════════════════════════════
            var startPoint = GetPointOnPolyline(T("cota2lign.select.start"), polyline1Id, settings);
            if (startPoint == null)
            {
                Logger.Info(T("cota2lign.cancelled"));
                return;
            }

            var endPoint = GetPointOnPolyline(T("cota2lign.select.end"), polyline1Id, settings);
            if (endPoint == null)
            {
                Logger.Info(T("cota2lign.cancelled"));
                return;
            }

            // ═══════════════════════════════════════════════════════
            // GÉNÉRATION DES STATIONS ET CRÉATION DES COTATIONS
            // ═══════════════════════════════════════════════════════
            int dimCount = 0;

            ExecuteInTransaction(tr =>
            {
                var polyline1 = (Polyline)tr.GetObject(polyline1Id, OpenMode.ForRead);
                var polyline2 = (Polyline)tr.GetObject(polyline2Id, OpenMode.ForRead);

                // Calculer les distances sur la polyligne 1
                double startDist = GetDistanceAtPoint(polyline1, startPoint.Value);
                double endDist = GetDistanceAtPoint(polyline1, endPoint.Value);

                // Générer les stations
                var stations = StationService.BuildStations(
                    polyline1,
                    startDist,
                    endDist,
                    settings.Interdistance,
                    settings.DimensionAtVertices
                );

                if (stations.Count == 0)
                {
                    Logger.Warning(T("cota2lign.nostations"));
                    return;
                }

                // Préparation du BlockTableRecord
                var btr = (BlockTableRecord)tr.GetObject(Database!.CurrentSpaceId, OpenMode.ForWrite);

                // Déterminer le calque de destination
                string targetLayer = GetTargetLayer(tr, settings);

                // Créer les cotations avec annulation groupée
                foreach (var stationDist in stations)
                {
                    var pt1 = polyline1.GetPointAtDist(stationDist);
                    var pt2 = polyline2.GetClosestPointTo(pt1, false);

                    // Calculer la direction perpendiculaire pour le placement
                    var tangentAngle = GeometryService.GetTangentAngle(polyline1, stationDist);
                    var perpAngle = tangentAngle + (settings.ReverseSide ? -Math.PI / 2 : Math.PI / 2);

                    // Créer la cotation alignée
                    var dimension = CreateAlignedDimension(pt1, pt2, settings.DimensionOffset, perpAngle);
                    dimension.Layer = targetLayer;

                    btr.AppendEntity(dimension);
                    tr.AddNewlyCreatedDBObject(dimension, true);
                    dimCount++;
                }
            });

            if (dimCount > 0)
            {
                Logger.Success(TFormat("cota2lign.success", dimCount));
            }
        });
    }

    #region Private Methods - Selection

    /// <summary>
    /// Sélectionne une polyligne avec option pour ouvrir les paramètres
    /// </summary>
    private (ObjectId objectId, bool openSettings)? SelectPolylineWithOptions(string prompt)
    {
        var peo = new PromptEntityOptions($"\n{prompt} [{T("cota2lign.option.params")}(P)]: ");
        peo.SetRejectMessage($"\n{T("cota2lign.error.notpolyline")}");
        peo.AddAllowedClass(typeof(Polyline), exactMatch: false);
        peo.Keywords.Add("Parametres", "P", T("cota2lign.option.params"));

        var result = Editor!.GetEntity(peo);

        if (result.Status == PromptStatus.Keyword && result.StringResult == "Parametres")
        {
            return (ObjectId.Null, true);
        }

        if (result.Status != PromptStatus.OK)
        {
            return null;
        }

        return (result.ObjectId, false);
    }

    /// <summary>
    /// Sélectionne une polyligne (exclut une ObjectId si spécifiée)
    /// </summary>
    private ObjectId? SelectPolyline(string prompt, ObjectId? excludeId = null)
    {
        while (true)
        {
            var peo = new PromptEntityOptions($"\n{prompt}: ");
            peo.SetRejectMessage($"\n{T("cota2lign.error.notpolyline")}");
            peo.AddAllowedClass(typeof(Polyline), exactMatch: false);

            var result = Editor!.GetEntity(peo);

            if (result.Status != PromptStatus.OK)
            {
                return null;
            }

            if (excludeId.HasValue && result.ObjectId == excludeId.Value)
            {
                WriteMessage($"\n{T("cota2lign.error.sameentity")}");
                continue;
            }

            return result.ObjectId;
        }
    }

    /// <summary>
    /// Demande un point à l'utilisateur.
    /// Utilise le module DynamicSnap si disponible et activé, sinon OSNAP AutoCAD.
    /// </summary>
    private Point3d? GetPointOnPolyline(string prompt, ObjectId polylineId, Cota2LignSettings settings)
    {
        // Utiliser le service d'intégration qui gère automatiquement le fallback
        return SnapIntegrationService.GetPointOnPolyline(
            polylineId,
            prompt,
            Editor!,
            Database!,
            settings
        );
    }

    /// <summary>
    /// Calcule la distance curviligne d'un point sur une polyligne
    /// </summary>
    private double GetDistanceAtPoint(Polyline polyline, Point3d point)
    {
        var closestPoint = polyline.GetClosestPointTo(point, false);
        return polyline.GetDistAtPoint(closestPoint);
    }

    #endregion

    #region Private Methods - Dimension Creation

    /// <summary>
    /// Crée une cotation alignée entre deux points
    /// </summary>
    private AlignedDimension CreateAlignedDimension(Point3d pt1, Point3d pt2, double offset, double perpAngle)
    {
        // Point milieu pour le placement de la ligne de cotation
        var midPoint = GeometryService.MidPoint(pt1, pt2);

        // Décalage perpendiculaire
        var dimLinePoint = new Point3d(
            midPoint.X + offset * Math.Cos(perpAngle),
            midPoint.Y + offset * Math.Sin(perpAngle),
            midPoint.Z
        );

        var dimension = new AlignedDimension(pt1, pt2, dimLinePoint, string.Empty, Database!.Dimstyle);
        return dimension;
    }

    /// <summary>
    /// Détermine le calque de destination pour les cotations
    /// </summary>
    private string GetTargetLayer(Transaction tr, Cota2LignSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.TargetLayer))
        {
            // Utiliser le calque courant
            var layerTable = (LayerTable)tr.GetObject(Database!.LayerTableId, OpenMode.ForRead);
            var currentLayerId = Database.Clayer;
            var currentLayer = (LayerTableRecord)tr.GetObject(currentLayerId, OpenMode.ForRead);
            return currentLayer.Name;
        }

        // Utiliser le calque spécifié (le créer si nécessaire via LayerService)
        return settings.TargetLayer;
    }

    #endregion

    #region Private Methods - Settings

    /// <summary>
    /// Ouvre la fenêtre de paramètres
    /// </summary>
    private void OpenSettingsWindow(Cota2LignSettings settings)
    {
        var window = new Cota2LignSettingsWindow(settings);
        var result = AcadApp.ShowModalWindow(window);

        if (result == true)
        {
            // Sauvegarder les paramètres dans le dessin
            settings.SaveToDrawing(Database!);
            Logger.Info(T("cota2lign.settings.saved"));
        }
    }

    #endregion
}
