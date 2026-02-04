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
public static class GeoLocationService
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
            catch { }

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
        catch { }

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
    /// Vérifie si le dessin a un système de coordonnées défini
    /// </summary>
    public static bool HasCoordinateSystem(Database db, Transaction tr)
    {
        return GetGeoLocationData(db, tr) != null;
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

    /// <summary>
    /// Rafraîchit la vue après modification de la géolocalisation
    /// </summary>
    public static void RefreshGeoView()
    {
        try
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            doc.SendStringToExecute("_.REGEN ", true, false, false);
        }
        catch { }
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

    /// <summary>
    /// Liste tous les systèmes de coordonnées disponibles (pour debug)
    /// Retourne une liste de tuples (Id, Description, IsProjected)
    /// </summary>
    public static IEnumerable<(string Id, string Description, bool IsProjected)> GetAllCoordinateSystems()
    {
        var allCs = GeoCoordinateSystem.CreateAll();
        foreach (var cs in allCs)
        {
            yield return (cs.ID, cs.Description ?? "", cs.Type == GeoCSType.Projected);
        }
    }

    /// <summary>
    /// Recherche des systèmes de coordonnées par mot-clé
    /// </summary>
    public static IEnumerable<(string Id, string Description)> SearchCoordinateSystems(string keyword)
    {
        var allCs = GeoCoordinateSystem.CreateAll();
        var upper = keyword.ToUpperInvariant();
        int count = 0;
        
        foreach (var cs in allCs)
        {
            if (cs.Type != GeoCSType.Projected)
                continue;
                
            var id = cs.ID ?? "";
            var desc = cs.Description ?? "";
            
            if (id.ToUpperInvariant().Contains(upper) || desc.ToUpperInvariant().Contains(upper))
            {
                yield return (id, desc);
                count++;
                if (count >= 50) yield break;
            }
        }
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

    #region Private Methods - Coordinate Conversions

    /// <summary>
    /// Extrait le numéro de zone CC du code (ex: "RGF93.CC49" -> 49)
    /// </summary>
    private static int ExtractCCZone(string code)
    {
        // Recherche du pattern CC suivi de 2 chiffres
        int idx = code.IndexOf("CC", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && idx + 4 <= code.Length)
        {
            if (int.TryParse(code.AsSpan(idx + 2, 2), out int zone))
            {
                return zone;
            }
        }
        return 0;
    }

    /// <summary>
    /// Extrait les informations de zone UTM du code
    /// </summary>
    private static (int Zone, bool Northern) ExtractUtmZoneInfo(string code, ProjectionInfo projection)
    {
        // Recherche du pattern "zone-XXN" ou "zone-XXS"
        bool northern = !code.Contains("S", StringComparison.OrdinalIgnoreCase) || 
                       code.Contains("N", StringComparison.OrdinalIgnoreCase);
        
        // Extraire le numéro de zone
        int zone = 0;
        
        // Pattern: UTM-zone-32N, UTM-32N, UTM32N
        var parts = code.Split('-', '_');
        foreach (var part in parts)
        {
            var cleaned = part.TrimEnd('N', 'S', 'n', 's');
            if (int.TryParse(cleaned, out int z) && z >= 1 && z <= 60)
            {
                zone = z;
                northern = part.EndsWith("N", StringComparison.OrdinalIgnoreCase) || 
                          !part.EndsWith("S", StringComparison.OrdinalIgnoreCase);
                break;
            }
        }

        // Si pas trouvé, calculer depuis le méridien central
        if (zone == 0 && projection.CentralMeridian != 0)
        {
            zone = (int)Math.Floor((projection.CentralMeridian + 180) / 6) + 1;
        }

        // Déterminer l'hémisphère depuis FalseNorthing (10000000 = sud)
        if (projection.FalseNorthing >= 10000000)
        {
            northern = false;
        }

        return (zone, northern);
    }

    /// <summary>
    /// Conversion NTF Lambert (zones I, II, III, IV) vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) NtfLambertToWgs84(double x, double y, string code)
    {
        // Paramètres de l'ellipsoïde Clarke 1880 (IGN)
        const double e = 0.08248325676;
        
        // Méridien de Paris en degrés par rapport à Greenwich
        const double parisMeridian = 2.337229167;

        // Paramètres selon la zone
        double n, c, xs, ys;
        
        if (code.Contains("LAMBERT-1") || code.Contains("ZONE-I"))
        {
            n = 0.7604059656;
            c = 11603796.98;
            xs = 600000;
            ys = 5657616.674;
        }
        else if (code.Contains("LAMBERT-2E") || code.Contains("2E"))
        {
            n = 0.7289686274;
            c = 11745793.39;
            xs = 600000;
            ys = 8199695.768;
        }
        else if (code.Contains("LAMBERT-2") || code.Contains("ZONE-II"))
        {
            n = 0.7289686274;
            c = 11745793.39;
            xs = 600000;
            ys = 6199695.768;
        }
        else if (code.Contains("LAMBERT-3") || code.Contains("ZONE-III"))
        {
            n = 0.6959127966;
            c = 11947992.52;
            xs = 600000;
            ys = 6791905.085;
        }
        else if (code.Contains("LAMBERT-4") || code.Contains("ZONE-IV"))
        {
            n = 0.6712679322;
            c = 12136281.99;
            xs = 234358;
            ys = 7053300.189;
        }
        else
        {
            // Défaut: Lambert II étendu
            n = 0.7289686274;
            c = 11745793.39;
            xs = 600000;
            ys = 8199695.768;
        }

        // Calcul inverse
        double dx = x - xs;
        double dy = ys - y;
        double R = Math.Sqrt(dx * dx + dy * dy);
        double gamma = Math.Atan2(dx, dy);

        double latIso = Math.Log(c / R) / n;
        double lon = parisMeridian + (gamma / n) * GeometryService.RadToDeg;

        // Calcul itératif de la latitude (NTF -> WGS84 nécessite transformation)
        double lat = 2 * Math.Atan(Math.Exp(latIso)) - Math.PI / 2;
        for (int i = 0; i < 10; i++)
        {
            double sinLat = Math.Sin(lat);
            lat = 2 * Math.Atan(Math.Exp(latIso) *
                Math.Pow((1 + e * sinLat) / (1 - e * sinLat), e / 2)) - Math.PI / 2;
        }

        // Conversion NTF -> WGS84 (transformation à 7 paramètres simplifiée)
        double latDeg = lat * GeometryService.RadToDeg;
        
        // Correction approximative NTF -> WGS84 (valeurs moyennes pour la France)
        double dLat = -0.00015; // environ -5.5" en latitude
        double dLon = 0.00008;  // environ +2.9" en longitude
        
        return (lon + dLon, latDeg + dLat);
    }

    /// <summary>
    /// Conversion coordonnées suisses vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) SwissToWgs84(double x, double y, ProjectionInfo projection)
    {
        // Déterminer si LV03 ou LV95
        bool isLv95 = projection.Code.Contains("LV95") || projection.Epsg == 2056;

        double y_aux, x_aux;
        
        if (isLv95)
        {
            // LV95: origine à 2600000 / 1200000
            y_aux = (x - 2600000) / 1000000;
            x_aux = (y - 1200000) / 1000000;
        }
        else
        {
            // LV03: origine à 600000 / 200000
            y_aux = (x - 600000) / 1000000;
            x_aux = (y - 200000) / 1000000;
        }

        // Formules de conversion suisses
        double lon = 2.6779094 +
                    4.728982 * y_aux +
                    0.791484 * y_aux * x_aux +
                    0.1306 * y_aux * x_aux * x_aux -
                    0.0436 * y_aux * y_aux * y_aux;

        double lat = 16.9023892 +
                    3.238272 * x_aux -
                    0.270978 * y_aux * y_aux -
                    0.002528 * x_aux * x_aux -
                    0.0447 * y_aux * y_aux * x_aux -
                    0.0140 * x_aux * x_aux * x_aux;

        // Conversion sexagésimales -> décimales
        lon = lon * 100 / 36;
        lat = lat * 100 / 36;

        return (lon, lat);
    }

    /// <summary>
    /// Conversion Lambert belge vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) BelgianLambertToWgs84(double x, double y, ProjectionInfo projection)
    {
        bool isLambert2008 = projection.Epsg == 3812 || projection.Code.Contains("2008");

        double xs, ys, n, c, lat0;

        if (isLambert2008)
        {
            // ETRS89 / Belgian Lambert 2008
            xs = 649328.0;
            ys = 665262.0;
            lat0 = 50.797815 * GeometryService.DegToRad;
            // Paramètres GRS80
            n = Math.Sin(lat0);
            c = 6378137.0 * Math.Cos(lat0) / (n * Math.Sqrt(1 - 0.00669438 * Math.Sin(lat0) * Math.Sin(lat0)));
            c *= Math.Exp(n * LatitudeIsometric(lat0, 0.0818191910435));
        }
        else
        {
            // BD72 / Belgian Lambert 72
            xs = 150000.013;
            ys = 5400088.438;
            lat0 = 90.0 * GeometryService.DegToRad; // Pôle
            n = 0.7716421928;
            c = 11598255.97;
        }

        double dx = x - xs;
        double dy = ys - y;
        double R = Math.Sqrt(dx * dx + dy * dy);
        double gamma = Math.Atan2(dx, dy);

        double lon0 = 4.367486666667 * GeometryService.DegToRad; // Méridien central belge
        double lon = lon0 + gamma / n;

        double latIso = Math.Log(c / R) / n;
        double lat = LatitudeFromIsometric(latIso, 0.0818191910435);

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Conversion Luxembourg LUREF vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) LuxembourgToWgs84(double x, double y)
    {
        // LUREF utilise une projection Transverse Mercator
        const double lon0 = 6.166666666667;
        const double lat0 = 49.833333333333;
        const double k0 = 1.0;
        const double fe = 80000;
        const double fn = 100000;
        const double a = 6378388.0; // Hayford
        const double e2 = 0.006722670022;

        return TransverseMercatorToWgs84(x, y, lon0, lat0, k0, fe, fn, a, e2);
    }

    /// <summary>
    /// Conversion RD néerlandais vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) DutchRdToWgs84(double x, double y)
    {
        // Conversion RD -> WGS84 avec polynômes de correction
        double dx = (x - 155000) * 1e-5;
        double dy = (y - 463000) * 1e-5;

        // Coefficients officiels Kadaster
        double lat = 52.15517440 +
                    (dy * 3235.65389) +
                    (dx * dx * -32.58297) +
                    (dy * dy * -0.24750) +
                    (dx * dx * dy * -0.84978) +
                    (dy * dy * dy * -0.06550) +
                    (dx * dx * dy * dy * -0.01709) +
                    (dx * dx * dx * dx * -0.00738);

        double lon = 5.38720621 +
                    (dx * 5260.52916) +
                    (dx * dy * 105.94684) +
                    (dx * dy * dy * 2.45656) +
                    (dx * dx * dx * -0.81885) +
                    (dx * dy * dy * dy * 0.05594) +
                    (dx * dx * dx * dy * -0.05607) +
                    (dx * dy * dy * dy * dy * 0.01199);

        return (lon / 3600, lat / 3600);
    }

    /// <summary>
    /// Conversion British National Grid vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) BritishNationalGridToWgs84(double x, double y)
    {
        // Paramètres OSGB36 (Airy 1830)
        const double a = 6377563.396;
        const double e2 = 0.00667054; // Excentricité au carré pour Airy 1830
        const double lon0 = -2.0 * GeometryService.DegToRad;
        const double fe = 400000;
        const double fn = -100000;
        const double k0 = 0.9996012717;

        double x1 = x - fe;
        double y1 = y - fn;

        double M = y1 / k0;
        double mu = M / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64));

        double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
        double lat = mu +
            (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu) +
            (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu) +
            (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);

        double sinLat = Math.Sin(lat);
        double cosLat = Math.Cos(lat);
        double tanLat = sinLat / cosLat;
        double nu = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
        double rho = a * (1 - e2) / Math.Pow(1 - e2 * sinLat * sinLat, 1.5);
        double eta2 = nu / rho - 1;
        double D = x1 / (nu * k0);

        lat = lat - (nu * tanLat / rho) * (D * D / 2 -
            (5 + 3 * tanLat * tanLat + eta2 - 9 * tanLat * tanLat * eta2) * D * D * D * D / 24 +
            (61 + 90 * tanLat * tanLat + 45 * tanLat * tanLat * tanLat * tanLat) * D * D * D * D * D * D / 720);

        double lon = lon0 + (D - (1 + 2 * tanLat * tanLat + eta2) * D * D * D / 6 +
            (5 - 2 * eta2 + 28 * tanLat * tanLat - 3 * eta2 * eta2 + 24 * tanLat * tanLat * tanLat * tanLat) * D * D * D * D * D / 120) / cosLat;

        // Conversion OSGB36 -> WGS84 (Helmert 7 paramètres)
        // Simplification: corrections moyennes pour la GB
        double latWgs = lat * GeometryService.RadToDeg + 0.000050;
        double lonWgs = lon * GeometryService.RadToDeg + 0.000089;

        return (lonWgs, latWgs);
    }

    /// <summary>
    /// Conversion Transverse Mercator générique vers WGS84
    /// </summary>
    private static (double Longitude, double Latitude) TransverseMercatorToWgs84(
        double x, double y, double lon0Deg, double lat0Deg, double k0,
        double fe, double fn, double a, double e2)
    {
        double lon0 = lon0Deg * GeometryService.DegToRad;
        double lat0 = lat0Deg * GeometryService.DegToRad;

        double x1 = x - fe;
        double y1 = y - fn;

        double e4 = e2 * e2;
        double e6 = e4 * e2;

        // M0 pour lat0
        double M0 = a * ((1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256) * lat0 -
                        (3 * e2 / 8 + 3 * e4 / 32 + 45 * e6 / 1024) * Math.Sin(2 * lat0) +
                        (15 * e4 / 256 + 45 * e6 / 1024) * Math.Sin(4 * lat0) -
                        (35 * e6 / 3072) * Math.Sin(6 * lat0));

        double M = M0 + y1 / k0;
        double mu = M / (a * (1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256));

        double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
        double lat = mu +
            (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu) +
            (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu) +
            (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu);

        double sinLat = Math.Sin(lat);
        double cosLat = Math.Cos(lat);
        double tanLat = sinLat / cosLat;
        double nu = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
        double rho = a * (1 - e2) / Math.Pow(1 - e2 * sinLat * sinLat, 1.5);
        double D = x1 / (nu * k0);

        lat = lat - (nu * tanLat / rho) * (D * D / 2 -
            (5 + 3 * tanLat * tanLat) * D * D * D * D / 24);

        double lon = lon0 + (D - (1 + 2 * tanLat * tanLat) * D * D * D / 6) / cosLat;

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Approximation de la projection inverse basée sur les paramètres
    /// </summary>
    private static (double Longitude, double Latitude) ApproximateInverseProjection(double x, double y, ProjectionInfo projection)
    {
        // Utiliser les paramètres de la projection pour une approximation simple
        // basée sur une projection conique ou cylindrique simplifiée
        
        double lon = projection.CentralMeridian + (x - projection.FalseEasting) / (111320 * Math.Cos(projection.LatitudeOrigin * GeometryService.DegToRad));
        double lat = projection.LatitudeOrigin + (y - projection.FalseNorthing) / 110540;

        // Limiter aux bornes raisonnables
        lon = Math.Max(-180, Math.Min(180, lon));
        lat = Math.Max(-90, Math.Min(90, lat));

        return (lon, lat);
    }

    /// <summary>
    /// Approximation de la projection directe basée sur les paramètres
    /// </summary>
    private static (double X, double Y) ApproximateForwardProjection(double longitude, double latitude, ProjectionInfo projection)
    {
        double x = projection.FalseEasting + (longitude - projection.CentralMeridian) * 111320 * Math.Cos(projection.LatitudeOrigin * GeometryService.DegToRad);
        double y = projection.FalseNorthing + (latitude - projection.LatitudeOrigin) * 110540;

        return (x, y);
    }

    /// <summary>
    /// Calcule la latitude isométrique
    /// </summary>
    private static double LatitudeIsometric(double lat, double e)
    {
        double sinLat = Math.Sin(lat);
        return Math.Log(Math.Tan(Math.PI / 4 + lat / 2) *
            Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2));
    }

    /// <summary>
    /// Calcule la latitude depuis la latitude isométrique
    /// </summary>
    private static double LatitudeFromIsometric(double latIso, double e)
    {
        double lat = 2 * Math.Atan(Math.Exp(latIso)) - Math.PI / 2;
        for (int i = 0; i < 10; i++)
        {
            double sinLat = Math.Sin(lat);
            lat = 2 * Math.Atan(Math.Exp(latIso) *
                Math.Pow((1 + e * sinLat) / (1 - e * sinLat), e / 2)) - Math.PI / 2;
        }
        return lat;
    }

    #endregion

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
