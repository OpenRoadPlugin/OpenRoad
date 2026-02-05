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

using System.Diagnostics;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using OpenAsphalte.Modules.Georeferencement.Services;
using OpenAsphalte.Modules.Georeferencement.Views;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.StreetView.Commands;

/// <summary>
/// Commande pour ouvrir Google Street View depuis un point du dessin.
/// Convertit les coordonnées AutoCAD (système projeté) vers WGS84 et ouvre le navigateur.
/// </summary>
/// <remarks>
/// Workflow:
/// 1. Vérifier qu'une projection est définie (sinon proposer de la définir)
/// 2. Demander à l'utilisateur de sélectionner un point de vue
/// 3. Demander une direction de visée (2ème point)
/// 4. Convertir les coordonnées vers WGS84 via GeoLocationService
/// 5. Calculer le cap (heading) en degrés
/// 6. Ouvrir Google Street View dans le navigateur
/// </remarks>
public class StreetViewCommand : CommandBase
{
    #region Constants

    /// <summary>
    /// URL de base pour Google Street View
    /// </summary>
    private const string StreetViewBaseUrl = "https://www.google.com/maps/@?api=1&map_action=pano";

    /// <summary>
    /// Angle de vue par défaut (champ de vision)
    /// </summary>
    private const int DefaultFov = 90;

    /// <summary>
    /// Pitch par défaut (angle vertical, 0 = horizontal)
    /// </summary>
    private const int DefaultPitch = 0;

    #endregion

    /// <summary>
    /// Exécute la commande Street View
    /// </summary>
    [CommandMethod("OAS_STREETVIEW")]
    [CommandInfo("Street View",
        Description = "Ouvrir Google Street View depuis un point du dessin",
        DisplayNameKey = "streetview.cmd.title",
        DescriptionKey = "streetview.cmd.desc",
        MenuCategory = "Cartographie",
        MenuCategoryKey = "menu.carto",
        Order = 20,
        RibbonSize = CommandSize.Large,
        Group = "Visualisation",
        ShowInMenu = true,
        ShowInRibbon = true)]
    public void Execute()
    {
        ExecuteSafe(() =>
        {
            // ═══════════════════════════════════════════════════════════
            // ÉTAPE 1: Vérifier la projection
            // ═══════════════════════════════════════════════════════════

            var projection = GetCurrentProjection();

            if (projection == null)
            {
                Logger.Warning(T("streetview.error.noprojection"));
                Logger.Info(T("streetview.error.noprojection.detail"));

                // Proposer d'ouvrir la fenêtre de définition de projection
                if (PromptYesNo(T("streetview.askprojection")))
                {
                    OpenSetProjectionWindow();

                    // Vérifier à nouveau après
                    projection = GetCurrentProjection();
                    if (projection == null)
                    {
                        Logger.Info(T("streetview.error.noprojection"));
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            Logger.Debug($"Projection active: {projection.Name} (EPSG:{projection.Epsg})");

            // ═══════════════════════════════════════════════════════════
            // ÉTAPE 2: Sélectionner le point de vue
            // ═══════════════════════════════════════════════════════════

            var pointOptions = new PromptPointOptions($"\n{T("streetview.select.point")}: ")
            {
                AllowNone = false
            };

            var pointResult = Editor!.GetPoint(pointOptions);
            if (pointResult.Status != PromptStatus.OK)
            {
                return;  // Annulation utilisateur
            }

            // Convertir en coordonnées monde (WCS)
            var viewPoint = pointResult.Value.TransformBy(Editor.CurrentUserCoordinateSystem);

            Logger.Debug(TFormat("streetview.coords.local", viewPoint.X, viewPoint.Y));

            // ═══════════════════════════════════════════════════════════
            // ÉTAPE 3: Sélectionner la direction
            // ═══════════════════════════════════════════════════════════

            var directionOptions = new PromptPointOptions($"\n{T("streetview.select.direction")}: ")
            {
                AllowNone = false,
                BasePoint = pointResult.Value,
                UseBasePoint = true
            };

            var directionResult = Editor.GetPoint(directionOptions);
            if (directionResult.Status != PromptStatus.OK)
            {
                return;  // Annulation utilisateur
            }

            var directionPoint = directionResult.Value.TransformBy(Editor.CurrentUserCoordinateSystem);

            // ═══════════════════════════════════════════════════════════
            // ÉTAPE 4: Conversion vers WGS84
            // ═══════════════════════════════════════════════════════════

            Logger.Info(T("streetview.converting"));
            Logger.Debug($"Projection: Code={projection.Code}, EPSG={projection.Epsg}");
            Logger.Debug($"Coordonnées source: X={viewPoint.X:F2}, Y={viewPoint.Y:F2}");

            (double longitude, double latitude) wgs84;

            try
            {
                wgs84 = GeoLocationService.ProjectedToGeographic(viewPoint.X, viewPoint.Y, projection);
                Logger.Debug($"Résultat brut: Lon={wgs84.longitude:F8}, Lat={wgs84.latitude:F8}");
            }
            catch (System.Exception ex)
            {
                Logger.Error(TFormat("streetview.error.unknownprojection", projection.Code));
                Logger.Debug(ex.Message);
                return;
            }

            // Validation des coordonnées
            if (!IsValidWgs84Coordinates(wgs84.latitude, wgs84.longitude))
            {
                Logger.Error(T("streetview.error.conversion"));
                Logger.Debug($"Coordonnées invalides: Lat={wgs84.latitude}, Lon={wgs84.longitude}");
                return;
            }

            Logger.Info(TFormat("streetview.coords.wgs84", wgs84.latitude, wgs84.longitude));

            // ═══════════════════════════════════════════════════════════
            // ÉTAPE 5: Calcul du cap (heading)
            // ═══════════════════════════════════════════════════════════

            double heading = CalculateHeading(viewPoint, directionPoint);
            Logger.Info(TFormat("streetview.heading", heading));

            // ═══════════════════════════════════════════════════════════
            // ÉTAPE 6: Ouverture de Street View
            // ═══════════════════════════════════════════════════════════

            Logger.Info(T("streetview.opening"));

            var url = BuildStreetViewUrl(wgs84.latitude, wgs84.longitude, heading);

            if (OpenUrl(url))
            {
                Logger.Success(T("streetview.success"));
            }
            else
            {
                Logger.Error(T("streetview.error.browser"));
            }
        });
    }

    #region Private Methods - Projection

    /// <summary>
    /// Récupère la projection actuellement définie dans le dessin
    /// </summary>
    /// <returns>ProjectionInfo ou null si non définie</returns>
    private ProjectionInfo? GetCurrentProjection()
    {
        // Récupérer le code du système de coordonnées via CGEOCS
        string? csCode = null;

        try
        {
            object? value = AcadApp.GetSystemVariable("CGEOCS");
            if (value is string cs && !string.IsNullOrWhiteSpace(cs))
            {
                csCode = cs;
            }
        }
        catch
        {
            // Variable non disponible
        }

        if (string.IsNullOrWhiteSpace(csCode))
        {
            return null;
        }

        // Rechercher dans la base de projections connues
        var projection = CoordinateService.Projections
            .FirstOrDefault(p => p.Code.Equals(csCode, StringComparison.OrdinalIgnoreCase));

        // Si pas trouvé par code exact, chercher par variantes
        if (projection == null)
        {
            projection = CoordinateService.Projections
                .FirstOrDefault(p =>
                    csCode.Contains(p.Code, StringComparison.OrdinalIgnoreCase) ||
                    p.Code.Contains(csCode, StringComparison.OrdinalIgnoreCase));
        }

        // Si toujours pas trouvé, créer une projection générique
        if (projection == null)
        {
            // Essayer d'extraire des infos du code
            projection = CreateGenericProjection(csCode);
        }

        return projection;
    }

    /// <summary>
    /// Crée une projection générique à partir du code
    /// </summary>
    private static ProjectionInfo? CreateGenericProjection(string code)
    {
        var upperCode = code.ToUpperInvariant();

        // Détecter le type de projection
        if (upperCode.Contains("LAMB93") || upperCode.Contains("LAMBERT-93"))
        {
            return new ProjectionInfo
            {
                Code = "RGF93.LAMB93",  // Normaliser le code pour la conversion
                Name = "Lambert 93",
                Epsg = 2154,
                Unit = "m"
            };
        }

        // RGF93 / CCxx - Accepter toutes les variantes (RGF93.CC49, RGF93-CC49, CC49, etc.)
        var ccMatch = System.Text.RegularExpressions.Regex.Match(upperCode, @"CC(\d{2})");
        if (ccMatch.Success && int.TryParse(ccMatch.Groups[1].Value, out int zone) && zone >= 42 && zone <= 50)
        {
            return new ProjectionInfo
            {
                Code = $"RGF93.CC{zone}",  // Normaliser le code pour la conversion
                Name = $"RGF93 / CC{zone}",
                Epsg = 3900 + zone,  // EPSG 3942-3950 pour CC42-CC50
                Unit = "m"
            };
        }

        // UTM
        if (upperCode.Contains("UTM"))
        {
            // Extraire la zone UTM si possible
            var utmMatch = System.Text.RegularExpressions.Regex.Match(upperCode, @"UTM[\-_]?(\d{1,2})([NS])?");
            if (utmMatch.Success)
            {
                return new ProjectionInfo
                {
                    Code = code,
                    Name = $"UTM Zone {utmMatch.Groups[1].Value}",
                    Epsg = 0,
                    Unit = "m"
                };
            }
            return new ProjectionInfo
            {
                Code = code,
                Name = "UTM",
                Epsg = 0,
                Unit = "m"
            };
        }

        return null;
    }

    /// <summary>
    /// Ouvre la fenêtre de définition de projection et applique la sélection
    /// </summary>
    private void OpenSetProjectionWindow()
    {
        try
        {
            var currentCs = GetCurrentProjectionCode();
            var window = new SetProjectionWindow(currentCs, null);
            var result = AcadApp.ShowModalWindow(window);

            // Si l'utilisateur a validé et sélectionné une projection
            if (result == true && window.SelectedProjection != null && !window.ClearProjection)
            {
                // Appliquer la projection au dessin
                ExecuteInTransaction(tr =>
                {
                    bool success = GeoLocationService.ApplyCoordinateSystem(
                        Database!,
                        tr,
                        window.SelectedProjection,
                        null);

                    if (success)
                    {
                        Logger.Success(TFormat("streetview.projection.applied", window.SelectedProjection.DisplayName));
                    }
                    else
                    {
                        Logger.Error(T("streetview.projection.error"));
                    }
                });
            }
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"{T("streetview.projection.windowerror")}: {ex.Message}");
        }
    }

    /// <summary>
    /// Récupère le code de projection actuel (CGEOCS)
    /// </summary>
    private static string? GetCurrentProjectionCode()
    {
        try
        {
            object? value = AcadApp.GetSystemVariable("CGEOCS");
            return value as string;
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Private Methods - Geometry

    /// <summary>
    /// Calcule le cap (heading) en degrés pour Street View
    /// </summary>
    /// <param name="from">Point d'observation</param>
    /// <param name="to">Point de direction</param>
    /// <returns>Cap en degrés (0-360, 0=Nord, 90=Est)</returns>
    private static double CalculateHeading(Point3d from, Point3d to)
    {
        // Angle en radians depuis l'axe X positif
        double angleRad = Math.Atan2(to.Y - from.Y, to.X - from.X);

        // Convertir en degrés
        double angleDeg = angleRad * 180.0 / Math.PI;

        // Convertir de l'angle mathématique (0=Est, anti-horaire)
        // vers le cap géographique (0=Nord, horaire)
        // Angle mathématique: 0° = Est, 90° = Nord
        // Cap géographique: 0° = Nord, 90° = Est
        double heading = 90.0 - angleDeg;

        // Normaliser entre 0 et 360
        while (heading < 0) heading += 360.0;
        while (heading >= 360.0) heading -= 360.0;

        return heading;
    }

    /// <summary>
    /// Vérifie si les coordonnées WGS84 sont valides
    /// </summary>
    private static bool IsValidWgs84Coordinates(double latitude, double longitude)
    {
        // Latitude: -90 à +90
        if (latitude < -90 || latitude > 90)
            return false;

        // Longitude: -180 à +180
        if (longitude < -180 || longitude > 180)
            return false;

        // Vérifier que ce n'est pas NaN ou Infinity
        if (double.IsNaN(latitude) || double.IsNaN(longitude))
            return false;

        if (double.IsInfinity(latitude) || double.IsInfinity(longitude))
            return false;

        return true;
    }

    #endregion

    #region Private Methods - URL & Browser

    /// <summary>
    /// Construit l'URL Google Street View
    /// </summary>
    private static string BuildStreetViewUrl(double latitude, double longitude, double heading)
    {
        // Format des nombres avec point décimal (culture invariante)
        var lat = latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var lon = longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var head = heading.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

        return $"{StreetViewBaseUrl}&viewpoint={lat},{lon}&heading={head}&pitch={DefaultPitch}&fov={DefaultFov}";
    }

    /// <summary>
    /// Ouvre une URL dans le navigateur par défaut
    /// </summary>
    private static bool OpenUrl(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            // Fallback avec explorer.exe
            try
            {
                Process.Start("explorer.exe", $"\"{url}\"");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion

    #region Private Methods - User Interaction

    /// <summary>
    /// Affiche une question Oui/Non à l'utilisateur
    /// </summary>
    private bool PromptYesNo(string message)
    {
        var yes = T("common.yes", "Oui");
        var no = T("common.no", "Non");
        var options = new PromptKeywordOptions($"\n{message} [{yes}/{no}] <{no}>: ");
        options.Keywords.Add(yes);
        options.Keywords.Add(no);
        options.Keywords.Default = no;
        options.AllowNone = true;

        var result = Editor!.GetKeywords(options);

        return result.Status == PromptStatus.OK &&
               result.StringResult.Equals(yes, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
