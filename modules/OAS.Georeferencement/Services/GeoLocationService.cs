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
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Services;
using OpenAsphalte.Logging;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.Georeferencement.Services;

/// <summary>
/// Service de géolocalisation pour appliquer et gérer les systèmes de coordonnées.
/// Utilise l'API GeoLocationData native d'AutoCAD avec PostToDb() pour une compatibilité
/// complète avec Bing Maps, GEOMAP et la géolocalisation.
/// </summary>
/// <remarks>
/// IMPORTANT: Cette implémentation utilise la méthode PostToDb() qui est CRITIQUE
/// pour que les cartes Bing Maps fonctionnent correctement. L'ancienne méthode
/// d'ajout manuel au dictionnaire ne permettait pas une intégration complète.
///
/// Référence: Kean Walmsley (Autodesk) - "Through the Interface" blog
/// </remarks>
public static partial class GeoLocationService
{
    #region Constants

    /// <summary>
    /// Clé du dictionnaire pour les données géographiques (référence uniquement)
    /// </summary>
    private const string GeoDataDictionaryKey = "ACAD_GEOGRAPHICDATA";

    #endregion

    #region Public Methods - Main API

    /// <summary>
    /// Applique un système de coordonnées au dessin avec configuration complète pour Bing Maps.
    /// Cette méthode utilise PostToDb() et les transformations natives d'AutoCAD.
    /// </summary>
    /// <param name="db">Base de données AutoCAD</param>
    /// <param name="tr">Transaction active</param>
    /// <param name="projection">Informations sur la projection à appliquer</param>
    /// <param name="designPoint">Point de référence dans le dessin (coordonnées projetées).
    /// Si null, calcule automatiquement un point approprié.</param>
    /// <returns>True si l'opération a réussi</returns>
    public static bool ApplyCoordinateSystem(Database db, Transaction tr, ProjectionInfo projection, Point3d? designPoint = null)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (tr == null) throw new ArgumentNullException(nameof(tr));
        if (projection == null) throw new ArgumentNullException(nameof(projection));

        try
        {
            // Vérifier si le code de projection est valide dans AutoCAD
            string validCode = projection.Code;
            if (!IsValidCoordinateSystemCode(projection.Code))
            {
                Logger.Warning($"Code de projection '{projection.Code}' non trouvé. Recherche d'alternatives...");

                var foundCode = FindValidCoordinateSystemCode(projection);
                if (foundCode == null)
                {
                    Logger.Error($"Aucun code de projection valide trouvé pour {projection.Name} (EPSG:{projection.Epsg})");
                    return false;
                }
                validCode = foundCode;
                Logger.Info($"Code de projection alternatif trouvé: {validCode}");
            }

            var msId = SymbolUtilityServices.GetBlockModelSpaceId(db);
            GeoLocationData geoData;

            // ═══════════════════════════════════════════════════════════════════
            // ÉTAPE 1: Récupérer ou créer GeoLocationData avec PostToDb()
            // CRITIQUE: PostToDb() est OBLIGATOIRE pour que Bing Maps fonctionne
            // ═══════════════════════════════════════════════════════════════════

            // Méthode 1: Via la propriété GeoDataObject (méthode préférée)
            ObjectId existingId = ObjectId.Null;
            try
            {
                existingId = db.GeoDataObject;
            }
            catch (System.Exception ex) { Logger.Debug($"GeoDataObject access: {ex.Message}"); }

            if (existingId != ObjectId.Null && existingId.IsValid)
            {
                geoData = (GeoLocationData)tr.GetObject(existingId, OpenMode.ForWrite);
                Logger.Debug("GeoLocationData existant trouvé via GeoDataObject");
            }
            else
            {
                // Méthode 2: Vérifier dans le dictionnaire nommé
                var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);

                if (nod.Contains(GeoDataDictionaryKey))
                {
                    try
                    {
                        var geoId = nod.GetAt(GeoDataDictionaryKey);
                        geoData = (GeoLocationData)tr.GetObject(geoId, OpenMode.ForWrite);
                        Logger.Debug("GeoLocationData existant trouvé via dictionnaire");
                    }
                    catch
                    {
                        // Dictionnaire corrompu, supprimer et recréer
                        nod.UpgradeOpen();
                        try { nod.Remove(GeoDataDictionaryKey); } catch { }
                        geoData = CreateNewGeoLocationData(db, tr, msId);
                    }
                }
                else
                {
                    // Créer nouveau avec PostToDb() - CRITIQUE!
                    geoData = CreateNewGeoLocationData(db, tr, msId);
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // ÉTAPE 2: Configurer le système de coordonnées
            // ═══════════════════════════════════════════════════════════════════

            // Type de coordonnées: Grid = système projeté
            geoData.TypeOfCoordinates = TypeOfCoordinates.CoordinateTypeGrid;

            // Définir le système de coordonnées (met à jour CGEOCS automatiquement)
            geoData.CoordinateSystem = validCode;

            // ═══════════════════════════════════════════════════════════════════
            // ÉTAPE 3: Calculer les points de référence
            // ═══════════════════════════════════════════════════════════════════

            Point3d refDesignPoint;
            Point3d refGeoPoint;

            if (designPoint.HasValue)
            {
                refDesignPoint = designPoint.Value;
            }
            else
            {
                refDesignPoint = GetDefaultDesignPoint(db, tr, projection);
            }

            // Essayer d'utiliser la transformation native d'AutoCAD
            bool useNativeTransform = false;
            try
            {
                refGeoPoint = geoData.TransformToLonLatAlt(refDesignPoint);
                useNativeTransform = true;
                Logger.Debug($"Transformation native: ({refDesignPoint.X:N2}, {refDesignPoint.Y:N2}) -> " +
                           $"(Lon:{refGeoPoint.X:F6}°, Lat:{refGeoPoint.Y:F6}°)");
            }
            catch
            {
                // Fallback: calcul manuel
                var (lon, lat) = ProjectedToGeographic(refDesignPoint.X, refDesignPoint.Y, projection);
                refGeoPoint = new Point3d(lon, lat, refDesignPoint.Z);
                Logger.Debug($"Transformation manuelle: ({refDesignPoint.X:N2}, {refDesignPoint.Y:N2}) -> " +
                           $"(Lon:{lon:F6}°, Lat:{lat:F6}°)");
            }

            // ═══════════════════════════════════════════════════════════════════
            // ÉTAPE 4: Appliquer les points de référence
            // CRITICAL: ReferencePoint = (longitude, latitude, altitude)
            // ═══════════════════════════════════════════════════════════════════
            geoData.DesignPoint = refDesignPoint;
            geoData.ReferencePoint = refGeoPoint;

            // ═══════════════════════════════════════════════════════════════════
            // ÉTAPE 5: Configurer les propriétés additionnelles
            // ═══════════════════════════════════════════════════════════════════

            // Unités horizontales
            var units = db.Insunits;
            geoData.HorizontalUnits = (units != UnitsValue.Undefined) ? units : UnitsValue.Meters;

            // Note: UpDirection et NorthDirection sont en lecture seule dans cette version de l'API
            // Ils sont configurés automatiquement par AutoCAD

            Logger.Debug($"GeoLocationData configuré: CS={validCode}, " +
                        $"Design=({refDesignPoint.X:N2}, {refDesignPoint.Y:N2}), " +
                        $"Geo=({refGeoPoint.X:F6}, {refGeoPoint.Y:F6})" +
                        (useNativeTransform ? " [Native]" : " [Manual]"));

            return true;
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Erreur lors de l'application du système de coordonnées: {ex.Message}");
            Logger.Debug(ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Crée un nouveau GeoLocationData et l'ajoute à la base de données avec PostToDb()
    /// </summary>
    private static GeoLocationData CreateNewGeoLocationData(Database db, Transaction tr, ObjectId modelSpaceId)
    {
        var geoData = new GeoLocationData();
        geoData.BlockTableRecordId = modelSpaceId;

        // CRITIQUE: PostToDb() est ce qui permet à Bing Maps de fonctionner !
        // Cette méthode ajoute correctement le GeoLocationData à la structure de la base de données
        geoData.PostToDb();
        tr.AddNewlyCreatedDBObject(geoData, true);

        Logger.Debug("Nouveau GeoLocationData créé avec PostToDb()");
        return geoData;
    }

    /// <summary>
    /// Supprime le système de coordonnées du dessin
    /// </summary>
    /// <param name="db">Base de données AutoCAD</param>
    /// <param name="tr">Transaction active</param>
    /// <returns>True si le système a été supprimé, False s'il n'existait pas</returns>
    public static bool ClearCoordinateSystem(Database db, Transaction tr)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (tr == null) throw new ArgumentNullException(nameof(tr));

        bool found = false;

        // Méthode 1: Via GeoDataObject
        try
        {
            var geoId = db.GeoDataObject;
            if (geoId != ObjectId.Null && geoId.IsValid)
            {
                var geoData = tr.GetObject(geoId, OpenMode.ForWrite);
                geoData.Erase();
                found = true;
            }
        }
        catch { }

        // Méthode 2: Via le dictionnaire nommé (nettoyage supplémentaire)
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
        if (nod.Contains(GeoDataDictionaryKey))
        {
            try
            {
                var geoId = nod.GetAt(GeoDataDictionaryKey);
                var geoObj = tr.GetObject(geoId, OpenMode.ForWrite);
                if (!geoObj.IsErased)
                    geoObj.Erase();
            }
            catch { }
            try { nod.Remove(GeoDataDictionaryKey); } catch { }
            found = true;
        }

        return found;
    }

    /// <summary>
    /// Récupère le GeoLocationData actuel du dessin
    /// </summary>
    /// <param name="db">Base de données AutoCAD</param>
    /// <param name="tr">Transaction active</param>
    /// <returns>GeoLocationData ou null si non défini</returns>
    public static GeoLocationData? GetGeoLocationData(Database db, Transaction tr)
    {
        if (db == null) throw new ArgumentNullException(nameof(db));
        if (tr == null) throw new ArgumentNullException(nameof(tr));

        // Méthode 1: Via GeoDataObject (préférée)
        try
        {
            var geoId = db.GeoDataObject;
            if (geoId != ObjectId.Null && geoId.IsValid)
            {
                return (GeoLocationData)tr.GetObject(geoId, OpenMode.ForRead);
            }
        }
        catch (System.Exception ex) { Logger.Debug($"GetGeoLocationData: {ex.Message}"); }

        // Méthode 2: Via le dictionnaire nommé
        var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
        if (nod.Contains(GeoDataDictionaryKey))
        {
            var geoId = nod.GetAt(GeoDataDictionaryKey);
            return (GeoLocationData)tr.GetObject(geoId, OpenMode.ForRead);
        }

        return null;
    }

    /// <summary>
    /// Active ou désactive l'affichage de la carte Bing Maps
    /// </summary>
    /// <param name="mode">Mode: "AERIAL", "ROAD", "HYBRID", ou "OFF"</param>
    public static void SetGeoMapMode(string mode)
    {
        try
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Exécuter la commande GEOMAP de manière asynchrone
            doc.SendStringToExecute($"_.GEOMAP _{mode} ", true, false, false);

            Logger.Debug($"GEOMAP mode défini: {mode}");
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"Impossible d'activer GEOMAP: {ex.Message}");
        }
    }

    #endregion

    #region Public Methods - Coordinate System Validation

    /// <summary>
    /// Vérifie si un code de système de coordonnées est valide dans AutoCAD
    /// </summary>
    public static bool IsValidCoordinateSystemCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            // La méthode la plus fiable est d'essayer de créer le système
            var cs = GeoCoordinateSystem.Create(code);
            return cs != null;
        }
        catch
        {
            // Le code n'est pas valide
            return false;
        }
    }

    /// <summary>
    /// Trouve un code de système de coordonnées valide pour une projection
    /// </summary>
    public static string? FindValidCoordinateSystemCode(ProjectionInfo projection)
    {
        // Variantes du code à essayer
        var codeVariants = new List<string>
        {
            projection.Code,
            projection.Code.ToUpperInvariant(),
            projection.Code.Replace(".", "-"),
            projection.Code.Replace("-", "."),
            projection.Code.Replace(".", "/"),
        };

        // Ajouter variantes EPSG si disponible
        if (projection.Epsg > 0)
        {
            codeVariants.Add($"EPSG:{projection.Epsg}");
        }

        // Mappages spéciaux pour les systèmes français courants
        var specialMappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "RGF93.LAMB93", new[] { "RGF93.LAMB93", "RGF93-LAMB93", "RGF93/LAMB93", "LAMB93" } },
            { "RGF93.CC42", new[] { "RGF93.CC42", "RGF93-CC42", "RGF93/CC42", "CC42" } },
            { "RGF93.CC43", new[] { "RGF93.CC43", "RGF93-CC43", "RGF93/CC43", "CC43" } },
            { "RGF93.CC44", new[] { "RGF93.CC44", "RGF93-CC44", "RGF93/CC44", "CC44" } },
            { "RGF93.CC45", new[] { "RGF93.CC45", "RGF93-CC45", "RGF93/CC45", "CC45" } },
            { "RGF93.CC46", new[] { "RGF93.CC46", "RGF93-CC46", "RGF93/CC46", "CC46" } },
            { "RGF93.CC47", new[] { "RGF93.CC47", "RGF93-CC47", "RGF93/CC47", "CC47" } },
            { "RGF93.CC48", new[] { "RGF93.CC48", "RGF93-CC48", "RGF93/CC48", "CC48" } },
            { "RGF93.CC49", new[] { "RGF93.CC49", "RGF93-CC49", "RGF93/CC49", "CC49" } },
            { "RGF93.CC50", new[] { "RGF93.CC50", "RGF93-CC50", "RGF93/CC50", "CC50" } },
        };

        if (specialMappings.TryGetValue(projection.Code, out var mappings))
        {
            codeVariants.AddRange(mappings);
        }

        // Essayer chaque variante
        foreach (var code in codeVariants.Distinct())
        {
            try
            {
                var cs = GeoCoordinateSystem.Create(code);
                if (cs != null)
                {
                    Logger.Debug($"Code valide trouvé: {code}");
                    return code;
                }
            }
            catch
            {
                // Continuer avec la variante suivante
            }
        }

        Logger.Debug($"Aucun code valide trouvé pour {projection.Name} (EPSG:{projection.Epsg})");
        return null;
    }

    #endregion

    #region Coordinate Conversion

    /// <summary>
    /// Convertit des coordonnées projetées en coordonnées géographiques (WGS84)
    /// selon le type de projection.
    /// </summary>
    /// <param name="x">Coordonnée X (Est) en mètres</param>
    /// <param name="y">Coordonnée Y (Nord) en mètres</param>
    /// <param name="projection">Projection source</param>
    /// <returns>Tuple (longitude, latitude) en degrés décimaux</returns>
    public static (double Longitude, double Latitude) ProjectedToGeographic(double x, double y, ProjectionInfo projection)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        // Si déjà en coordonnées géographiques
        if (projection.Unit == "deg")
            return (x, y);

        var code = projection.Code.ToUpperInvariant();

        // ═══════════════════════════════════════════════════════════
        // RGF93 / Lambert 93 (EPSG:2154)
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("LAMB93") || projection.Epsg == 2154)
        {
            return CoordinateService.Lambert93ToWgs84(x, y);
        }

        // ═══════════════════════════════════════════════════════════
        // RGF93 / CC42 à CC50 (EPSG:3942-3950)
        // Accepte toutes les variantes: RGF93.CC49, RGF93-CC49, CC49, etc.
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("CC") || (projection.Epsg >= 3942 && projection.Epsg <= 3950))
        {
            int zone = ExtractCCZone(code);

            // Si pas trouvé dans le code, essayer via EPSG
            if (zone == 0 && projection.Epsg >= 3942 && projection.Epsg <= 3950)
            {
                zone = projection.Epsg - 3900;  // EPSG 3942 -> zone 42
            }

            if (zone >= 42 && zone <= 50)
            {
                return CoordinateService.CCToWgs84(x, y, zone);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // UTM Zones (WGS84, ETRS89, etc.)
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("UTM"))
        {
            var (zone, northern) = ExtractUtmZoneInfo(code, projection);
            if (zone > 0)
            {
                return CoordinateService.UtmToWgs84(x, y, zone, northern);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // NTF / Lambert zones (ancien système français)
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("NTF") && code.Contains("LAMBERT"))
        {
            return NtfLambertToWgs84(x, y, code);
        }

        // ═══════════════════════════════════════════════════════════
        // Suisse CH1903/CH1903+ (LV03/LV95)
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("CH1903") || code.Contains("LV03") || code.Contains("LV95"))
        {
            return SwissToWgs84(x, y, projection);
        }

        // ═══════════════════════════════════════════════════════════
        // Belgique BD72 / Lambert 72 et ETRS89 / Lambert 2008
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("BELGIAN") || code.Contains("BD72") || projection.Epsg == 31370 || projection.Epsg == 3812)
        {
            return BelgianLambertToWgs84(x, y, projection);
        }

        // ═══════════════════════════════════════════════════════════
        // Luxembourg LUREF
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("LUREF") || code.Contains("LUXEMBOURG") || projection.Epsg == 2169)
        {
            return LuxembourgToWgs84(x, y);
        }

        // ═══════════════════════════════════════════════════════════
        // Pays-Bas Amersfoort / RD New
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("AMERSFOORT") || code.Contains("RD-NEW") || projection.Epsg == 28992)
        {
            return DutchRdToWgs84(x, y);
        }

        // ═══════════════════════════════════════════════════════════
        // Royaume-Uni OSGB36 / British National Grid
        // ═══════════════════════════════════════════════════════════
        if (code.Contains("OSGB") || code.Contains("BRITISH") || projection.Epsg == 27700)
        {
            return BritishNationalGridToWgs84(x, y);
        }

        // ═══════════════════════════════════════════════════════════
        // Fallback: Approximation basée sur les paramètres de projection
        // ═══════════════════════════════════════════════════════════
        return ApproximateInverseProjection(x, y, projection);
    }

    /// <summary>
    /// Convertit des coordonnées géographiques (WGS84) en coordonnées projetées
    /// selon le type de projection.
    /// </summary>
    /// <param name="longitude">Longitude en degrés décimaux</param>
    /// <param name="latitude">Latitude en degrés décimaux</param>
    /// <param name="projection">Projection cible</param>
    /// <returns>Tuple (X, Y) en mètres</returns>
    public static (double X, double Y) GeographicToProjected(double longitude, double latitude, ProjectionInfo projection)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection));

        // Si déjà en coordonnées géographiques
        if (projection.Unit == "deg")
            return (longitude, latitude);

        var code = projection.Code.ToUpperInvariant();

        // Lambert 93
        if (code.Contains("LAMB93") || projection.Epsg == 2154)
        {
            return CoordinateService.Wgs84ToLambert93(longitude, latitude);
        }

        // UTM
        if (code.Contains("UTM"))
        {
            var (zone, _) = ExtractUtmZoneInfo(code, projection);
            if (zone > 0)
            {
                var result = CoordinateService.Wgs84ToUtm(longitude, latitude);
                return (result.Easting, result.Northing);
            }
        }

        // Fallback: approximation
        return ApproximateForwardProjection(longitude, latitude, projection);
    }

    #endregion

    // Private conversion methods are in GeoLocationService.Conversions.cs

    #region Private Methods - Helpers

    /// <summary>
    /// Détermine le point de référence par défaut dans le dessin
    /// </summary>
    private static Point3d GetDefaultDesignPoint(Database db, Transaction tr, ProjectionInfo projection)
    {
        // 1. Essayer de calculer le centroïde des objets du dessin
        var centroid = CalculateDrawingCentroid(db, tr);

        if (centroid.HasValue && projection.ContainsPoint(centroid.Value.X, centroid.Value.Y))
        {
            return centroid.Value;
        }

        // 2. Utiliser le centre des limites de la projection
        double centerX = (projection.MinX + projection.MaxX) / 2;
        double centerY = (projection.MinY + projection.MaxY) / 2;

        // 3. Vérifier les limites du dessin
        try
        {
            var extents = db.Extmin;
            if (extents.X > 0 && extents.Y > 0)
            {
                var drawingCenter = new Point3d(
                    (db.Extmin.X + db.Extmax.X) / 2,
                    (db.Extmin.Y + db.Extmax.Y) / 2,
                    0);

                if (projection.ContainsPoint(drawingCenter.X, drawingCenter.Y))
                {
                    return drawingCenter;
                }
            }
        }
        catch
        {
            // Ignorer si les extents ne sont pas valides
        }

        return new Point3d(centerX, centerY, 0);
    }

    /// <summary>
    /// Calcule le centroïde approximatif des objets du dessin
    /// </summary>
    /// <param name="db">Base de données AutoCAD</param>
    /// <param name="tr">Transaction active</param>
    /// <returns>Centroïde ou null si aucun objet valide</returns>
    public static Point3d? CalculateDrawingCentroid(Database db, Transaction tr)
    {
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var modelSpace = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        double sumX = 0, sumY = 0;
        int count = 0;

        foreach (ObjectId id in modelSpace)
        {
            try
            {
                var entity = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (entity == null) continue;

                var extents = entity.GeometricExtents;
                double cx = (extents.MinPoint.X + extents.MaxPoint.X) / 2;
                double cy = (extents.MinPoint.Y + extents.MaxPoint.Y) / 2;

                // Ignorer les points trop proches de l'origine
                if (Math.Abs(cx) < CoordinateService.OriginThreshold &&
                    Math.Abs(cy) < CoordinateService.OriginThreshold)
                    continue;

                sumX += cx;
                sumY += cy;
                count++;

                // Limiter le nombre d'objets analysés
                if (count > 1000) break;
            }
            catch
            {
                // Ignorer les entités problématiques
            }
        }

        if (count == 0)
            return null;

        return new Point3d(sumX / count, sumY / count, 0);
    }

    #endregion
}
