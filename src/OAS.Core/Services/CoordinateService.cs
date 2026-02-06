// Open Asphalte
// Copyright (C) 2026 Open Asphalte Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Autodesk.AutoCAD.Geometry;
using System.IO;
using System.Text.Json;

namespace OpenAsphalte.Services;

/// <summary>
/// Service de gestion des systèmes de coordonnées et de conversion.
/// Fournit des méthodes pour identifier, convertir et gérer les projections cartographiques.
/// </summary>
/// <remarks>
/// Ce service gère les systèmes de coordonnées compatibles avec AutoCAD (variable CGEOCS).
/// Il peut charger des définitions de projections depuis un fichier JSON externe.
/// </remarks>
public static partial class CoordinateService
{
    #region Constants

    /// <summary>
    /// Rayon équatorial de la Terre (WGS84) en mètres
    /// </summary>
    public const double EarthRadiusEquatorial = 6378137.0;

    /// <summary>
    /// Rayon polaire de la Terre (WGS84) en mètres
    /// </summary>
    public const double EarthRadiusPolar = 6356752.314245;

    /// <summary>
    /// Aplatissement de la Terre (WGS84)
    /// </summary>
    public const double EarthFlattening = 1.0 / 298.257223563;

    /// <summary>
    /// Excentricité au carré (WGS84)
    /// </summary>
    public const double EarthEccentricitySquared = 0.00669437999014;

    /// <summary>
    /// Seuil de distance au centre (origine) en dessous duquel un point est ignoré
    /// pour la détection automatique de projection (en unités du dessin)
    /// </summary>
    public const double OriginThreshold = 1000.0;

    #endregion

    #region Projection Database

    private static List<ProjectionInfo>? _projections;
    private static readonly object _lock = new();

    /// <summary>
    /// Liste de toutes les projections connues
    /// </summary>
    public static IReadOnlyList<ProjectionInfo> Projections
    {
        get
        {
            EnsureProjectionsLoaded();
            return _projections!;
        }
    }

    /// <summary>
    /// Charge les projections depuis le fichier JSON ou utilise les projections intégrées
    /// </summary>
    private static void EnsureProjectionsLoaded()
    {
        if (_projections != null) return;

        lock (_lock)
        {
            if (_projections != null) return;

            _projections = new List<ProjectionInfo>();

            // Essayer de charger depuis le fichier externe
            var dataPath = Path.Combine(
                Configuration.Configuration.ConfigurationFolder,
                "..", "Data", "projections.json");

            if (File.Exists(dataPath))
            {
                try
                {
                    var json = File.ReadAllText(dataPath);
                    var loaded = JsonSerializer.Deserialize<List<ProjectionInfo>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (loaded != null)
                    {
                        _projections = loaded;
                        return;
                    }
                }
                catch
                {
                    // Fallback aux projections intégrées
                }
            }

            // Projections intégrées (France, Belgique, Suisse, etc.)
            LoadBuiltInProjections();
        }
    }

    /// <summary>
    /// Recharge les projections depuis le fichier externe ou la liste intégrée.
    /// </summary>
    public static void ReloadProjections()
    {
        lock (_lock)
        {
            _projections = null;
        }
        EnsureProjectionsLoaded();
    }

    /// <summary>
    /// Recherche une projection par son code AutoCAD.
    /// </summary>
    public static ProjectionInfo? GetProjectionByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return Projections.FirstOrDefault(p =>
            string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Recherche une projection par son code EPSG.
    /// </summary>
    public static ProjectionInfo? GetProjectionByEpsg(int epsg)
    {
        return Projections.FirstOrDefault(p => p.Epsg == epsg);
    }

    /// <summary>
    /// Recherche des projections par texte (nom, code, pays, région).
    /// </summary>
    public static IEnumerable<ProjectionInfo> SearchProjections(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Projections;

        return Projections.Where(p =>
            p.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            p.Country.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            p.Region.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            p.Epsg.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retourne les projections groupées par pays.
    /// </summary>
    public static IEnumerable<IGrouping<string, ProjectionInfo>> GetProjectionsByCountry()
    {
        return Projections.GroupBy(p => p.Country).OrderBy(g => g.Key);
    }

    #endregion

    #region Projection Detection

    /// <summary>
    /// Détecte automatiquement la projection la plus probable à partir d'un ensemble de points.
    /// </summary>
    public static ProjectionInfo? DetectProjection(IEnumerable<Point3d> points)
    {
        var validPoints = points
            .Where(p => Math.Abs(p.X) > OriginThreshold || Math.Abs(p.Y) > OriginThreshold)
            .ToList();

        if (validPoints.Count == 0)
            return null;

        double avgX = validPoints.Average(p => p.X);
        double avgY = validPoints.Average(p => p.Y);

        return DetectProjection(avgX, avgY);
    }

    /// <summary>
    /// Détecte automatiquement la projection la plus probable à partir de coordonnées moyennes.
    /// </summary>
    public static ProjectionInfo? DetectProjection(double x, double y)
    {
        if (Math.Abs(x) <= OriginThreshold && Math.Abs(y) <= OriginThreshold)
            return null;

        var candidates = Projections
            .Where(p => p.Unit == "m")
            .Where(p => x >= p.MinX && x <= p.MaxX && y >= p.MinY && y <= p.MaxY)
            .ToList();

        if (candidates.Count == 0)
            return null;

        return candidates
            .OrderByDescending(p => p.Code.Contains("CC"))
            .ThenByDescending(p => p.Code.Contains("LAMB93"))
            .ThenBy(p => p.Code)
            .FirstOrDefault();
    }

    #endregion
}

/// <summary>
/// Informations sur un système de projection cartographique.
/// </summary>
public class ProjectionInfo
{
    /// <summary>Code AutoCAD pour CGEOCS (ex: "RGF93.CC49")</summary>
    public string Code { get; init; } = "";

    /// <summary>Nom complet de la projection</summary>
    public string Name { get; init; } = "";

    /// <summary>Pays principal d'utilisation</summary>
    public string Country { get; init; } = "";

    /// <summary>Région ou zone géographique</summary>
    public string Region { get; init; } = "";

    /// <summary>Code EPSG (pour référence et interopérabilité)</summary>
    public int Epsg { get; init; }

    /// <summary>Unité de mesure ("m" pour mètres, "deg" pour degrés)</summary>
    public string Unit { get; init; } = "m";

    /// <summary>Méridien central en degrés</summary>
    public double CentralMeridian { get; init; }

    /// <summary>Latitude d'origine en degrés</summary>
    public double LatitudeOrigin { get; init; }

    /// <summary>Faux Est (False Easting) en mètres</summary>
    public double FalseEasting { get; init; }

    /// <summary>Faux Nord (False Northing) en mètres</summary>
    public double FalseNorthing { get; init; }

    /// <summary>Coordonnée X minimale typique</summary>
    public double MinX { get; init; }

    /// <summary>Coordonnée X maximale typique</summary>
    public double MaxX { get; init; }

    /// <summary>Coordonnée Y minimale typique</summary>
    public double MinY { get; init; }

    /// <summary>Coordonnée Y maximale typique</summary>
    public double MaxY { get; init; }

    /// <summary>Description détaillée</summary>
    public string Description { get; init; } = "";

    /// <summary>Affichage formaté pour UI</summary>
    public string DisplayName => $"{Name} [{Code}]";

    /// <summary>Vérifie si un point est dans les bornes typiques de cette projection</summary>
    public bool ContainsPoint(double x, double y)
        => x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;

    public override string ToString() => DisplayName;
}
