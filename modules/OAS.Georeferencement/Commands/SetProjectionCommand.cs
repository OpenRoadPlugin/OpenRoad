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
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using OpenAsphalte.Modules.Georeferencement.Services;
using OpenAsphalte.Modules.Georeferencement.Views;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.Georeferencement.Commands;

/// <summary>
/// Commande pour définir le système de coordonnées du dessin.
/// Configure GeoLocationData pour la compatibilité Bing Maps et géolocalisation.
/// </summary>
public class SetProjectionCommand : CommandBase
{
    /// <summary>
    /// Exécute la commande de définition de projection
    /// </summary>
    [CommandMethod("OAS_GEOREF_SETPROJECTION")]
    [CommandInfo("Définir une projection",
        Description = "Définir le système de coordonnées du dessin",
        DisplayNameKey = "georef.setproj.title",
        DescriptionKey = "georef.setproj.desc",
        MenuCategory = "Cartographie",
        MenuCategoryKey = "menu.carto",
        MenuSubCategory = "Géoréférencement",
        MenuSubCategoryKey = "menu.georef",
        Order = 10,
        RibbonSize = CommandSize.Large,
        Group = "Projection",
        ShowInMenu = true,
        ShowInRibbon = true)]
    public void Execute()
    {
        ExecuteSafe(() =>
        {
            // Récupérer le système de coordonnées actuel
            string? currentCs = GetCurrentCoordinateSystem();
            
            // Détecter automatiquement le système probable
            var detectedProjection = DetectProjectionFromDrawing();
            
            // Afficher les informations actuelles dans la console
            LogCurrentStatus(currentCs, detectedProjection);
            
            // Ouvrir la fenêtre de sélection
            var window = new SetProjectionWindow(currentCs, detectedProjection);
            var result = AcadApp.ShowModalWindow(window);
            
            if (result != true)
            {
                Logger.Info(T("georef.setproj.cancelled"));
                return;
            }

            // Traitement selon l'action utilisateur
            if (window.ClearProjection)
            {
                ClearCoordinateSystem();
            }
            else if (window.SelectedProjection != null)
            {
                ApplyProjection(window.SelectedProjection, detectedProjection, window.EnableGeoMap);
            }
        });
    }

    #region Private Methods - Main Operations

    /// <summary>
    /// Applique une projection au dessin avec géolocalisation complète
    /// </summary>
    private void ApplyProjection(ProjectionInfo projection, ProjectionInfo? detectedProjection, bool enableGeoMap = false)
    {
        try
        {
            bool success = false;
            
            ExecuteInTransaction(tr =>
            {
                // Déterminer le meilleur point de référence
                Point3d? designPoint = null;
                
                // Si une projection a été détectée et c'est la même que celle sélectionnée,
                // utiliser le centroïde du dessin comme point de référence
                if (detectedProjection != null && detectedProjection.Code == projection.Code)
                {
                    designPoint = GeoLocationService.CalculateDrawingCentroid(Database!, tr);
                }

                // Appliquer le système de coordonnées avec géolocalisation
                success = GeoLocationService.ApplyCoordinateSystem(Database!, tr, projection, designPoint);
            });

            if (!success)
            {
                Logger.Error(T("georef.setproj.error.notfound"));
                return;
            }

            // Log de succès avec détails
            Logger.Success(TFormat("georef.setproj.success", projection.DisplayName));
            
            // Afficher les coordonnées géographiques calculées
            DisplayGeoLocationInfo(projection);

            // Activer GEOMAP si demandé
            if (enableGeoMap)
            {
                Logger.Info(T("georef.setproj.geomap.activating"));
                GeoLocationService.SetGeoMapMode("AERIAL");
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error($"{T("georef.setproj.error")}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Supprime le système de coordonnées du dessin
    /// </summary>
    private void ClearCoordinateSystem()
    {
        try
        {
            ExecuteInTransaction(tr =>
            {
                GeoLocationService.ClearCoordinateSystem(Database!, tr);
            });
            
            Logger.Success(T("georef.setproj.cleared"));
        }
        catch (System.Exception ex)
        {
            Logger.Error($"{T("georef.setproj.error")}: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Private Methods - Detection

    /// <summary>
    /// Récupère le système de coordonnées actuel du dessin
    /// </summary>
    private static string? GetCurrentCoordinateSystem()
    {
        try
        {
            object? value = AcadApp.GetSystemVariable("CGEOCS");
            if (value is string cs && !string.IsNullOrWhiteSpace(cs))
            {
                return cs;
            }
        }
        catch
        {
            // Variable non disponible ou erreur
        }
        return null;
    }

    /// <summary>
    /// Détecte automatiquement la projection en analysant les coordonnées des objets
    /// </summary>
    private ProjectionInfo? DetectProjectionFromDrawing()
    {
        var points = new List<Point3d>();
        
        ExecuteInTransaction(tr =>
        {
            var bt = (BlockTable)tr.GetObject(Database!.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            
            foreach (ObjectId id in modelSpace)
            {
                try
                {
                    var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (entity == null) continue;
                    
                    CollectEntityPoints(entity, tr, points);
                    
                    // Limiter pour les performances
                    if (points.Count > 10000) return;
                }
                catch
                {
                    // Ignorer les entités problématiques
                }
            }
        });
        
        return CoordinateService.DetectProjection(points);
    }

    /// <summary>
    /// Collecte les points caractéristiques d'une entité
    /// </summary>
    private static void CollectEntityPoints(Entity entity, Transaction tr, List<Point3d> points)
    {
        const int MaxPointsPerEntity = 100;

        switch (entity)
        {
            case DBPoint dbPoint:
                points.Add(dbPoint.Position);
                break;
                
            case Line line:
                points.Add(line.StartPoint);
                points.Add(line.EndPoint);
                break;
                
            case Circle circle:
                points.Add(circle.Center);
                break;
                
            case Arc arc:
                points.Add(arc.Center);
                points.Add(arc.StartPoint);
                points.Add(arc.EndPoint);
                break;
                
            case Polyline pline:
                for (int i = 0; i < Math.Min(pline.NumberOfVertices, MaxPointsPerEntity); i++)
                {
                    points.Add(pline.GetPoint3dAt(i));
                }
                break;
                
            case Polyline2d pline2d:
                int count2d = 0;
                foreach (ObjectId vertexId in pline2d)
                {
                    if (count2d++ >= MaxPointsPerEntity) break;
                    if (tr.GetObject(vertexId, OpenMode.ForRead) is Vertex2d vertex)
                    {
                        points.Add(new Point3d(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                    }
                }
                break;
                
            case Polyline3d pline3d:
                int count3d = 0;
                foreach (ObjectId vertexId in pline3d)
                {
                    if (count3d++ >= MaxPointsPerEntity) break;
                    if (tr.GetObject(vertexId, OpenMode.ForRead) is PolylineVertex3d vertex)
                    {
                        points.Add(vertex.Position);
                    }
                }
                break;
                
            case BlockReference blockRef:
                points.Add(blockRef.Position);
                break;
                
            case DBText text:
                points.Add(text.Position);
                break;
                
            case MText mtext:
                points.Add(mtext.Location);
                break;
                
            case Hatch hatch:
                try
                {
                    var extents = hatch.GeometricExtents;
                    points.Add(new Point3d(
                        (extents.MinPoint.X + extents.MaxPoint.X) / 2,
                        (extents.MinPoint.Y + extents.MaxPoint.Y) / 2,
                        0));
                }
                catch
                {
                    // Ignorer si extents invalides
                }
                break;
        }
    }

    #endregion

    #region Private Methods - Logging

    /// <summary>
    /// Affiche le statut actuel dans la console
    /// </summary>
    private void LogCurrentStatus(string? currentCs, ProjectionInfo? detectedProjection)
    {
        if (!string.IsNullOrEmpty(currentCs))
        {
            var proj = CoordinateService.GetProjectionByCode(currentCs);
            string displayName = proj?.DisplayName ?? currentCs;
            Logger.Info(TFormat("georef.setproj.current", displayName));
        }
        else
        {
            Logger.Info(T("georef.setproj.none"));
        }
        
        if (detectedProjection != null)
        {
            Logger.Info(TFormat("georef.setproj.detected", detectedProjection.DisplayName));
        }
    }

    /// <summary>
    /// Affiche les informations de géolocalisation après application
    /// </summary>
    private void DisplayGeoLocationInfo(ProjectionInfo projection)
    {
        ExecuteInTransaction(tr =>
        {
            var geoData = GeoLocationService.GetGeoLocationData(Database!, tr);
            if (geoData == null) return;

            var designPt = geoData.DesignPoint;
            var refPt = geoData.ReferencePoint;

            Logger.Debug($"  DesignPoint (projeté):     X={designPt.X:N2}, Y={designPt.Y:N2}");
            Logger.Debug($"  ReferencePoint (WGS84):    Lon={refPt.X:F6}°, Lat={refPt.Y:F6}°");
            Logger.Debug($"  Système:                   {geoData.CoordinateSystem}");
        });

        // Message informatif sur la compatibilité
        Logger.Info(T("georef.setproj.applied_info"));
    }

    #endregion
}
