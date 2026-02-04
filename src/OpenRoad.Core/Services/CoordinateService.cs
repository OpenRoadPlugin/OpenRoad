// Copyright 2026 Open Road Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Autodesk.AutoCAD.Geometry;
using System.IO;
using System.Text.Json;

namespace OpenRoad.Services;

/// <summary>
/// Service de gestion des syst�mes de coordonn�es et de conversion.
/// Fournit des m�thodes pour identifier, convertir et g�rer les projections cartographiques.
/// </summary>
/// <remarks>
/// Ce service g�re les syst�mes de coordonn�es compatibles avec AutoCAD (variable CGEOCS).
/// Il peut charger des d�finitions de projections depuis un fichier JSON externe.
/// </remarks>
public static class CoordinateService
{
    #region Constants

    /// <summary>
    /// Rayon �quatorial de la Terre (WGS84) en m�tres
    /// </summary>
    public const double EarthRadiusEquatorial = 6378137.0;

    /// <summary>
    /// Rayon polaire de la Terre (WGS84) en m�tres
    /// </summary>
    public const double EarthRadiusPolar = 6356752.314245;

    /// <summary>
    /// Aplatissement de la Terre (WGS84)
    /// </summary>
    public const double EarthFlattening = 1.0 / 298.257223563;

    /// <summary>
    /// Excentricit� au carr� (WGS84)
    /// </summary>
    public const double EarthEccentricitySquared = 0.00669437999014;

    /// <summary>
    /// Seuil de distance au centre (origine) en dessous duquel un point est ignor�
    /// pour la d�tection automatique de projection (en unit�s du dessin)
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
    /// Charge les projections depuis le fichier JSON ou utilise les projections int�gr�es
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
                    // Fallback aux projections int�gr�es
                }
            }

            // Projections int�gr�es (France, Belgique, Suisse, etc.)
            LoadBuiltInProjections();
        }
    }

    /// <summary>
    /// Charge les projections int�gr�es au programme
    /// </summary>
    private static void LoadBuiltInProjections()
    {
        _projections = new List<ProjectionInfo>
        {
            // ???????????????????????????????????????????????????????????????????????????
            // FRANCE - RGF93 / Lambert 93 (syst�me national)
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "RGF93.LAMB93",
                Name = "RGF93 / Lambert 93",
                Country = "France",
                Region = "France m�tropolitaine",
                Epsg = 2154,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 46.5,
                FalseEasting = 700000,
                FalseNorthing = 6600000,
                MinX = 100000, MaxX = 1200000,
                MinY = 6000000, MaxY = 7200000,
                Description = "Projection conique conforme de Lambert - Syst�me national fran�ais"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // FRANCE - RGF93 / CC42 � CC50 (zones c�niques conformes)
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "RGF93.CC42",
                Name = "RGF93 / CC42",
                Country = "France",
                Region = "Corse, C�te d'Azur (sud)",
                Epsg = 3942,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 42.0,
                FalseEasting = 1700000,
                FalseNorthing = 1200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 1000000, MaxY = 1400000,
                Description = "Zone CC42 - Latitude origine 42�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC43",
                Name = "RGF93 / CC43",
                Country = "France",
                Region = "Provence, Languedoc-Roussillon (sud)",
                Epsg = 3943,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 43.0,
                FalseEasting = 1700000,
                FalseNorthing = 2200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 2000000, MaxY = 2400000,
                Description = "Zone CC43 - Latitude origine 43�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC44",
                Name = "RGF93 / CC44",
                Country = "France",
                Region = "Aquitaine, Midi-Pyr�n�es (sud)",
                Epsg = 3944,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 44.0,
                FalseEasting = 1700000,
                FalseNorthing = 3200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 3000000, MaxY = 3400000,
                Description = "Zone CC44 - Latitude origine 44�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC45",
                Name = "RGF93 / CC45",
                Country = "France",
                Region = "Nouvelle-Aquitaine, Auvergne",
                Epsg = 3945,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 45.0,
                FalseEasting = 1700000,
                FalseNorthing = 4200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 4000000, MaxY = 4400000,
                Description = "Zone CC45 - Latitude origine 45�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC46",
                Name = "RGF93 / CC46",
                Country = "France",
                Region = "Centre-Val de Loire, Bourgogne",
                Epsg = 3946,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 46.0,
                FalseEasting = 1700000,
                FalseNorthing = 5200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 5000000, MaxY = 5400000,
                Description = "Zone CC46 - Latitude origine 46�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC47",
                Name = "RGF93 / CC47",
                Country = "France",
                Region = "Pays de la Loire, Bretagne (est)",
                Epsg = 3947,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 47.0,
                FalseEasting = 1700000,
                FalseNorthing = 6200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 6000000, MaxY = 6400000,
                Description = "Zone CC47 - Latitude origine 47�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC48",
                Name = "RGF93 / CC48",
                Country = "France",
                Region = "�le-de-France, Normandie, Bretagne",
                Epsg = 3948,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 48.0,
                FalseEasting = 1700000,
                FalseNorthing = 7200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 7000000, MaxY = 7400000,
                Description = "Zone CC48 - Latitude origine 48�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC49",
                Name = "RGF93 / CC49",
                Country = "France",
                Region = "Hauts-de-France, Grand Est (ouest)",
                Epsg = 3949,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 49.0,
                FalseEasting = 1700000,
                FalseNorthing = 8200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 8000000, MaxY = 8400000,
                Description = "Zone CC49 - Latitude origine 49�N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC50",
                Name = "RGF93 / CC50",
                Country = "France",
                Region = "Nord-Pas-de-Calais, Flandres",
                Epsg = 3950,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 50.0,
                FalseEasting = 1700000,
                FalseNorthing = 9200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 9000000, MaxY = 9400000,
                Description = "Zone CC50 - Latitude origine 50�N"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // FRANCE - NTF / Lambert zones (ancien syst�me)
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "NTF.Lambert-1-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone I",
                Country = "France",
                Region = "Nord de la France",
                Epsg = 27561,
                Unit = "m",
                CentralMeridian = 2.337229167, // M�ridien de Paris
                LatitudeOrigin = 49.5,
                FalseEasting = 600000,
                FalseNorthing = 200000,
                MinX = 0, MaxX = 1200000,
                MinY = 0, MaxY = 400000,
                Description = "Ancien syst�me NTF - Zone I (Nord)"
            },
            new ProjectionInfo
            {
                Code = "NTF.Lambert-2-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone II",
                Country = "France",
                Region = "Centre de la France",
                Epsg = 27562,
                Unit = "m",
                CentralMeridian = 2.337229167,
                LatitudeOrigin = 46.8,
                FalseEasting = 600000,
                FalseNorthing = 200000,
                MinX = 0, MaxX = 1200000,
                MinY = 0, MaxY = 400000,
                Description = "Ancien syst�me NTF - Zone II (Centre)"
            },
            new ProjectionInfo
            {
                Code = "NTF.Lambert-2e-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone II �tendu",
                Country = "France",
                Region = "France m�tropolitaine",
                Epsg = 27572,
                Unit = "m",
                CentralMeridian = 2.337229167,
                LatitudeOrigin = 46.8,
                FalseEasting = 600000,
                FalseNorthing = 2200000,
                MinX = 0, MaxX = 1200000,
                MinY = 1600000, MaxY = 2800000,
                Description = "Ancien syst�me NTF - Zone II �tendu (France enti�re)"
            },
            new ProjectionInfo
            {
                Code = "NTF.Lambert-3-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone III",
                Country = "France",
                Region = "Sud de la France",
                Epsg = 27563,
                Unit = "m",
                CentralMeridian = 2.337229167,
                LatitudeOrigin = 44.1,
                FalseEasting = 600000,
                FalseNorthing = 200000,
                MinX = 0, MaxX = 1200000,
                MinY = 0, MaxY = 400000,
                Description = "Ancien syst�me NTF - Zone III (Sud)"
            },
            new ProjectionInfo
            {
                Code = "NTF.Lambert-4-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone IV",
                Country = "France",
                Region = "Corse",
                Epsg = 27564,
                Unit = "m",
                CentralMeridian = 2.337229167,
                LatitudeOrigin = 42.165,
                FalseEasting = 234358,
                FalseNorthing = 185861,
                MinX = 0, MaxX = 500000,
                MinY = 0, MaxY = 400000,
                Description = "Ancien syst�me NTF - Zone IV (Corse)"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // BELGIQUE
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "BD72.Belgian-Lambert-72",
                Name = "BD72 / Belgian Lambert 72",
                Country = "Belgique",
                Region = "Belgique",
                Epsg = 31370,
                Unit = "m",
                CentralMeridian = 4.367486666667,
                LatitudeOrigin = 90.0,
                FalseEasting = 150000.013,
                FalseNorthing = 5400088.438,
                MinX = 0, MaxX = 300000,
                MinY = 0, MaxY = 300000,
                Description = "Syst�me belge Lambert 72"
            },
            new ProjectionInfo
            {
                Code = "ETRS89.Belgian-Lambert-2008",
                Name = "ETRS89 / Belgian Lambert 2008",
                Country = "Belgique",
                Region = "Belgique",
                Epsg = 3812,
                Unit = "m",
                CentralMeridian = 4.359215833333,
                LatitudeOrigin = 50.797815,
                FalseEasting = 649328.0,
                FalseNorthing = 665262.0,
                MinX = 500000, MaxX = 800000,
                MinY = 500000, MaxY = 800000,
                Description = "Syst�me belge Lambert 2008 (ETRS89)"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // SUISSE
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "CH1903.LV03",
                Name = "CH1903 / LV03",
                Country = "Suisse",
                Region = "Suisse",
                Epsg = 21781,
                Unit = "m",
                CentralMeridian = 7.439583333333,
                LatitudeOrigin = 46.952405555556,
                FalseEasting = 600000,
                FalseNorthing = 200000,
                MinX = 480000, MaxX = 840000,
                MinY = 70000, MaxY = 300000,
                Description = "Ancien syst�me suisse LV03"
            },
            new ProjectionInfo
            {
                Code = "CH1903+.LV95",
                Name = "CH1903+ / LV95",
                Country = "Suisse",
                Region = "Suisse",
                Epsg = 2056,
                Unit = "m",
                CentralMeridian = 7.439583333333,
                LatitudeOrigin = 46.952405555556,
                FalseEasting = 2600000,
                FalseNorthing = 1200000,
                MinX = 2480000, MaxX = 2840000,
                MinY = 1070000, MaxY = 1300000,
                Description = "Syst�me suisse actuel LV95"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // LUXEMBOURG
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "LUREF.Luxembourg-TM",
                Name = "LUREF / Luxembourg TM",
                Country = "Luxembourg",
                Region = "Luxembourg",
                Epsg = 2169,
                Unit = "m",
                CentralMeridian = 6.166666666667,
                LatitudeOrigin = 49.833333333333,
                FalseEasting = 80000,
                FalseNorthing = 100000,
                MinX = 45000, MaxX = 115000,
                MinY = 55000, MaxY = 145000,
                Description = "Syst�me luxembourgeois Transverse Mercator"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // ALLEMAGNE
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "ETRS89.UTM-zone-32N",
                Name = "ETRS89 / UTM zone 32N",
                Country = "Allemagne",
                Region = "Allemagne (ouest), France (est)",
                Epsg = 25832,
                Unit = "m",
                CentralMeridian = 9.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 32N sur datum ETRS89"
            },
            new ProjectionInfo
            {
                Code = "ETRS89.UTM-zone-33N",
                Name = "ETRS89 / UTM zone 33N",
                Country = "Allemagne",
                Region = "Allemagne (est), Pologne (ouest)",
                Epsg = 25833,
                Unit = "m",
                CentralMeridian = 15.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 33N sur datum ETRS89"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // ESPAGNE
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "ETRS89.UTM-zone-30N",
                Name = "ETRS89 / UTM zone 30N",
                Country = "Espagne",
                Region = "Espagne (ouest), Portugal",
                Epsg = 25830,
                Unit = "m",
                CentralMeridian = -3.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 30N sur datum ETRS89"
            },
            new ProjectionInfo
            {
                Code = "ETRS89.UTM-zone-31N",
                Name = "ETRS89 / UTM zone 31N",
                Country = "Espagne",
                Region = "Espagne (est), France (sud-ouest)",
                Epsg = 25831,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 31N sur datum ETRS89"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // ITALIE
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "RDN2008.Italy-zone",
                Name = "RDN2008 / Italy zone (E-N)",
                Country = "Italie",
                Region = "Italie",
                Epsg = 6875,
                Unit = "m",
                CentralMeridian = 12.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 3000000,
                FalseNorthing = 0,
                MinX = 2400000, MaxX = 3600000,
                MinY = 3600000, MaxY = 5300000,
                Description = "Syst�me italien RDN2008"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // ROYAUME-UNI
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "OSGB36.British-National-Grid",
                Name = "OSGB 1936 / British National Grid",
                Country = "Royaume-Uni",
                Region = "Grande-Bretagne",
                Epsg = 27700,
                Unit = "m",
                CentralMeridian = -2.0,
                LatitudeOrigin = 49.0,
                FalseEasting = 400000,
                FalseNorthing = -100000,
                MinX = 0, MaxX = 700000,
                MinY = 0, MaxY = 1300000,
                Description = "British National Grid"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // PAYS-BAS
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "Amersfoort.RD-New",
                Name = "Amersfoort / RD New",
                Country = "Pays-Bas",
                Region = "Pays-Bas",
                Epsg = 28992,
                Unit = "m",
                CentralMeridian = 5.387638888889,
                LatitudeOrigin = 52.156160555556,
                FalseEasting = 155000,
                FalseNorthing = 463000,
                MinX = 0, MaxX = 300000,
                MinY = 300000, MaxY = 630000,
                Description = "Syst�me n�erlandais RD New (Rijksdriehoek)"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // CANADA - Qu�bec
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "NAD83.MTM-zone-7",
                Name = "NAD83 / MTM zone 7",
                Country = "Canada",
                Region = "Qu�bec (Montr�al)",
                Epsg = 32187,
                Unit = "m",
                CentralMeridian = -70.5,
                LatitudeOrigin = 0.0,
                FalseEasting = 304800,
                FalseNorthing = 0,
                MinX = 0, MaxX = 610000,
                MinY = 4800000, MaxY = 5400000,
                Description = "Modified Transverse Mercator zone 7 (Qu�bec)"
            },
            new ProjectionInfo
            {
                Code = "NAD83.MTM-zone-8",
                Name = "NAD83 / MTM zone 8",
                Country = "Canada",
                Region = "Qu�bec (Qu�bec City)",
                Epsg = 32188,
                Unit = "m",
                CentralMeridian = -73.5,
                LatitudeOrigin = 0.0,
                FalseEasting = 304800,
                FalseNorthing = 0,
                MinX = 0, MaxX = 610000,
                MinY = 4800000, MaxY = 5400000,
                Description = "Modified Transverse Mercator zone 8 (Qu�bec)"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // UTM ZONES GLOBALES (WGS84)
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "WGS84.UTM-29N",
                Name = "WGS 84 / UTM zone 29N",
                Country = "Global",
                Region = "Longitude -12� � -6� (Portugal, A�ores)",
                Epsg = 32629,
                Unit = "m",
                CentralMeridian = -9.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 29N sur WGS84"
            },
            new ProjectionInfo
            {
                Code = "WGS84.UTM-30N",
                Name = "WGS 84 / UTM zone 30N",
                Country = "Global",
                Region = "Longitude -6� � 0� (Espagne, UK ouest)",
                Epsg = 32630,
                Unit = "m",
                CentralMeridian = -3.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 30N sur WGS84"
            },
            new ProjectionInfo
            {
                Code = "WGS84.UTM-31N",
                Name = "WGS 84 / UTM zone 31N",
                Country = "Global",
                Region = "Longitude 0� � 6� (France ouest, Benelux)",
                Epsg = 32631,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 31N sur WGS84"
            },
            new ProjectionInfo
            {
                Code = "WGS84.UTM-32N",
                Name = "WGS 84 / UTM zone 32N",
                Country = "Global",
                Region = "Longitude 6� � 12� (France est, Allemagne)",
                Epsg = 32632,
                Unit = "m",
                CentralMeridian = 9.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 32N sur WGS84"
            },
            new ProjectionInfo
            {
                Code = "WGS84.UTM-33N",
                Name = "WGS 84 / UTM zone 33N",
                Country = "Global",
                Region = "Longitude 12� � 18� (Europe centrale)",
                Epsg = 32633,
                Unit = "m",
                CentralMeridian = 15.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 33N sur WGS84"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // FRANCE - DOM-TOM
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "RGAF09.UTM-zone-20N",
                Name = "RGAF09 / UTM zone 20N",
                Country = "France",
                Region = "Antilles fran�aises (Guadeloupe, Martinique)",
                Epsg = 5490,
                Unit = "m",
                CentralMeridian = -63.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 20N pour les Antilles fran�aises"
            },
            new ProjectionInfo
            {
                Code = "RGFG95.UTM-zone-22N",
                Name = "RGFG95 / UTM zone 22N",
                Country = "France",
                Region = "Guyane fran�aise",
                Epsg = 2972,
                Unit = "m",
                CentralMeridian = -51.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 22N pour la Guyane fran�aise"
            },
            new ProjectionInfo
            {
                Code = "RGR92.UTM-zone-40S",
                Name = "RGR92 / UTM zone 40S",
                Country = "France",
                Region = "La R�union",
                Epsg = 2975,
                Unit = "m",
                CentralMeridian = 57.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 10000000,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 10000000,
                Description = "UTM zone 40S pour La R�union"
            },
            new ProjectionInfo
            {
                Code = "RGM04.UTM-zone-38S",
                Name = "RGM04 / UTM zone 38S",
                Country = "France",
                Region = "Mayotte",
                Epsg = 4471,
                Unit = "m",
                CentralMeridian = 45.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 10000000,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 10000000,
                Description = "UTM zone 38S pour Mayotte"
            },

            // ???????????????????????????????????????????????????????????????????????????
            // COORDONN�ES G�OGRAPHIQUES
            // ???????????????????????????????????????????????????????????????????????????
            new ProjectionInfo
            {
                Code = "LL84",
                Name = "WGS 84 (g�ographique)",
                Country = "Global",
                Region = "Monde entier",
                Epsg = 4326,
                Unit = "deg",
                CentralMeridian = 0.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 0,
                FalseNorthing = 0,
                MinX = -180, MaxX = 180,
                MinY = -90, MaxY = 90,
                Description = "Coordonn�es g�ographiques WGS84 (longitude/latitude)"
            },
            new ProjectionInfo
            {
                Code = "LL-RGF93",
                Name = "RGF93 (g�ographique)",
                Country = "France",
                Region = "France m�tropolitaine",
                Epsg = 4171,
                Unit = "deg",
                CentralMeridian = 0.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 0,
                FalseNorthing = 0,
                MinX = -10, MaxX = 15,
                MinY = 40, MaxY = 55,
                Description = "Coordonn�es g�ographiques RGF93 (longitude/latitude)"
            }
        };
    }

    /// <summary>
    /// Recharge les projections depuis le fichier externe (si modifi�)
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
    /// Recherche une projection par son code AutoCAD
    /// </summary>
    /// <param name="code">Code AutoCAD (ex: "RGF93.CC49")</param>
    /// <returns>Projection trouv�e ou null</returns>
    public static ProjectionInfo? GetProjectionByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return Projections.FirstOrDefault(p =>
            string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Recherche une projection par son code EPSG
    /// </summary>
    /// <param name="epsg">Code EPSG (ex: 3949)</param>
    /// <returns>Projection trouv�e ou null</returns>
    public static ProjectionInfo? GetProjectionByEpsg(int epsg)
    {
        return Projections.FirstOrDefault(p => p.Epsg == epsg);
    }

    /// <summary>
    /// Recherche des projections par texte (nom, code, pays, r�gion)
    /// </summary>
    /// <param name="searchText">Texte de recherche</param>
    /// <returns>Liste des projections correspondantes</returns>
    public static IEnumerable<ProjectionInfo> SearchProjections(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Projections;

        var search = searchText.ToLowerInvariant();
        return Projections.Where(p =>
            p.Code.ToLowerInvariant().Contains(search) ||
            p.Name.ToLowerInvariant().Contains(search) ||
            p.Country.ToLowerInvariant().Contains(search) ||
            p.Region.ToLowerInvariant().Contains(search) ||
            p.Description.ToLowerInvariant().Contains(search) ||
            p.Epsg.ToString().Contains(search));
    }

    /// <summary>
    /// Retourne les projections group�es par pays
    /// </summary>
    public static IEnumerable<IGrouping<string, ProjectionInfo>> GetProjectionsByCountry()
    {
        return Projections.GroupBy(p => p.Country).OrderBy(g => g.Key);
    }

    #endregion

    #region Projection Detection

    /// <summary>
    /// D�tecte automatiquement la projection la plus probable � partir d'un ensemble de points
    /// </summary>
    /// <param name="points">Points � analyser</param>
    /// <returns>Projection d�tect�e ou null si aucune correspondance</returns>
    public static ProjectionInfo? DetectProjection(IEnumerable<Point3d> points)
    {
        var validPoints = points
            .Where(p => Math.Abs(p.X) > OriginThreshold || Math.Abs(p.Y) > OriginThreshold)
            .ToList();

        if (validPoints.Count == 0)
            return null;

        // Calculer les coordonn�es moyennes
        double avgX = validPoints.Average(p => p.X);
        double avgY = validPoints.Average(p => p.Y);

        return DetectProjection(avgX, avgY);
    }

    /// <summary>
    /// D�tecte automatiquement la projection la plus probable � partir de coordonn�es moyennes
    /// </summary>
    /// <param name="x">Coordonn�e X moyenne</param>
    /// <param name="y">Coordonn�e Y moyenne</param>
    /// <returns>Projection d�tect�e ou null si aucune correspondance</returns>
    public static ProjectionInfo? DetectProjection(double x, double y)
    {
        // Ignorer si trop proche de l'origine
        if (Math.Abs(x) <= OriginThreshold && Math.Abs(y) <= OriginThreshold)
            return null;

        // Chercher la projection dont les bornes contiennent le point
        var candidates = Projections
            .Where(p => p.Unit == "m") // Ignorer les projections g�ographiques
            .Where(p => x >= p.MinX && x <= p.MaxX && y >= p.MinY && y <= p.MaxY)
            .ToList();

        if (candidates.Count == 0)
            return null;

        // Si plusieurs candidats, pr�f�rer les zones CC (plus pr�cises) puis Lambert 93
        var preferred = candidates
            .OrderByDescending(p => p.Code.Contains("CC"))
            .ThenByDescending(p => p.Code.Contains("LAMB93"))
            .ThenBy(p => p.Code)
            .FirstOrDefault();

        return preferred;
    }

    #endregion

    #region Coordinate Transformations

    /// <summary>
    /// Convertit des coordonn�es Lambert 93 en WGS84 (longitude/latitude)
    /// </summary>
    /// <param name="x">Coordonn�e X (Est) en m�tres</param>
    /// <param name="y">Coordonn�e Y (Nord) en m�tres</param>
    /// <returns>Tuple (longitude, latitude) en degr�s d�cimaux</returns>
    public static (double Longitude, double Latitude) Lambert93ToWgs84(double x, double y)
    {
        // Param�tres Lambert 93
        const double n = 0.7256077650532670;
        const double c = 11754255.426096;
        const double xs = 700000.0;
        const double ys = 12655612.049876;
        const double e = 0.0818191910428158;
        const double lon0 = 3.0 * GeometryService.DegToRad; // M�ridien central

        // Calculs interm�diaires
        double dx = x - xs;
        double dy = ys - y;
        double R = Math.Sqrt(dx * dx + dy * dy);
        double gamma = Math.Atan2(dx, dy);

        double latIso = Math.Log(c / R) / n;
        double lon = lon0 + gamma / n;

        // Calcul it�ratif de la latitude
        double lat = 2 * Math.Atan(Math.Exp(latIso)) - Math.PI / 2;
        for (int i = 0; i < 10; i++)
        {
            double sinLat = Math.Sin(lat);
            double latIsoCalc = Math.Log(Math.Tan(Math.PI / 4 + lat / 2) *
                Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2));
            lat = 2 * Math.Atan(Math.Exp(latIso + e * Math.Asinh(e * sinLat))) - Math.PI / 2;
        }

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Convertit des coordonn�es WGS84 (longitude/latitude) en Lambert 93
    /// </summary>
    /// <param name="longitude">Longitude en degr�s d�cimaux</param>
    /// <param name="latitude">Latitude en degr�s d�cimaux</param>
    /// <returns>Tuple (X, Y) en m�tres</returns>
    public static (double X, double Y) Wgs84ToLambert93(double longitude, double latitude)
    {
        // Param�tres Lambert 93
        const double n = 0.7256077650532670;
        const double c = 11754255.426096;
        const double xs = 700000.0;
        const double ys = 12655612.049876;
        const double e = 0.0818191910428158;
        const double lon0 = 3.0 * GeometryService.DegToRad;

        double lon = longitude * GeometryService.DegToRad;
        double lat = latitude * GeometryService.DegToRad;

        double sinLat = Math.Sin(lat);
        double latIso = Math.Log(Math.Tan(Math.PI / 4 + lat / 2) *
            Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2));

        double R = c * Math.Exp(-n * latIso);
        double gamma = n * (lon - lon0);

        double x = xs + R * Math.Sin(gamma);
        double y = ys - R * Math.Cos(gamma);

        return (x, y);
    }

    /// <summary>
    /// Convertit des coordonnées CC (Conique Conforme) en WGS84
    /// </summary>
    /// <param name="x">Coordonnée X en mètres</param>
    /// <param name="y">Coordonnée Y en mètres</param>
    /// <param name="zone">Zone CC (42 à 50)</param>
    /// <returns>Tuple (longitude, latitude) en degrés décimaux</returns>
    public static (double Longitude, double Latitude) CCToWgs84(double x, double y, int zone)
    {
        if (zone < 42 || zone > 50)
            throw new ArgumentOutOfRangeException(nameof(zone), "Zone CC doit être entre 42 et 50");
        
        // ═══════════════════════════════════════════════════════════════════════════
        // Paramètres officiels EPSG pour RGF93 / CC42-CC50
        // Projection: Lambert Conic Conformal (2SP) - Conique Conforme Sécante
        // Ellipsoïde: GRS 1980
        // ═══════════════════════════════════════════════════════════════════════════
        
        // Paramètres de l'ellipsoïde GRS80
        const double a = 6378137.0;                    // Demi-grand axe
        const double e2 = 0.00669438002290;            // Excentricité au carré (GRS80)
        const double e = 0.0818191910428158;           // Excentricité
        
        // Méridien central = 3° Est
        const double lon0 = 3.0;
        
        // Latitude origine = zone (en degrés)
        double latOrigin = zone;
        
        // Parallèles standard: lat0 - 0.75° et lat0 + 0.75°
        double lat1 = (zone - 0.75) * GeometryService.DegToRad;  // Parallèle sud
        double lat2 = (zone + 0.75) * GeometryService.DegToRad;  // Parallèle nord
        double lat0 = zone * GeometryService.DegToRad;           // Latitude origine
        
        // False Easting = 1 700 000 m
        const double fe = 1700000.0;
        
        // False Northing = (zone - 41) * 1 000 000 + 200 000 m
        double fn = (zone - 41) * 1000000.0 + 200000.0;
        
        // ═══════════════════════════════════════════════════════════════════════════
        // Calcul des constantes de projection (Lambert Conique Conforme 2SP)
        // ═══════════════════════════════════════════════════════════════════════════
        
        double m1 = Math.Cos(lat1) / Math.Sqrt(1 - e2 * Math.Sin(lat1) * Math.Sin(lat1));
        double m2 = Math.Cos(lat2) / Math.Sqrt(1 - e2 * Math.Sin(lat2) * Math.Sin(lat2));
        
        double t0 = CalculateTValue(lat0, e);
        double t1 = CalculateTValue(lat1, e);
        double t2 = CalculateTValue(lat2, e);
        
        double n = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));
        double F = m1 / (n * Math.Pow(t1, n));
        double rho0 = a * F * Math.Pow(t0, n);
        
        // ═══════════════════════════════════════════════════════════════════════════
        // Conversion inverse: (X, Y) -> (Lon, Lat)
        // ═══════════════════════════════════════════════════════════════════════════
        
        double xp = x - fe;
        double yp = rho0 - (y - fn);
        
        double rho = Math.Sign(n) * Math.Sqrt(xp * xp + yp * yp);
        double theta = Math.Atan2(xp, yp);
        
        double t = Math.Pow(rho / (a * F), 1.0 / n);
        
        // Longitude
        double lon = theta / n + lon0 * GeometryService.DegToRad;
        
        // Latitude par itération
        double lat = Math.PI / 2 - 2 * Math.Atan(t);
        for (int i = 0; i < 15; i++)
        {
            double sinLat = Math.Sin(lat);
            double latNew = Math.PI / 2 - 2 * Math.Atan(t * Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2));
            if (Math.Abs(latNew - lat) < 1e-12)
                break;
            lat = latNew;
        }
        
        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }
    
    /// <summary>
    /// Calcule la valeur t pour la projection Lambert Conique Conforme
    /// </summary>
    private static double CalculateTValue(double lat, double e)
    {
        double sinLat = Math.Sin(lat);
        return Math.Tan(Math.PI / 4 - lat / 2) / Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2);
    }

    /// <summary>
    /// Convertit des coordonnées UTM en WGS84
    /// </summary>
    /// <param name="easting">Coordonnée Est en mètres</param>
    /// <param name="northing">Coordonnée Nord en mètres</param>
    /// <param name="zone">Zone UTM (1 à 60)</param>
    /// <param name="northern">True si hémisphère nord</param>
    /// <returns>Tuple (longitude, latitude) en degrés décimaux</returns>
    public static (double Longitude, double Latitude) UtmToWgs84(double easting, double northing, int zone, bool northern = true)
    {
        const double k0 = 0.9996;
        const double a = EarthRadiusEquatorial;
        const double e2 = EarthEccentricitySquared;
        const double e4 = e2 * e2;
        const double e6 = e4 * e2;

        double x = easting - 500000.0;
        double y = northern ? northing : northing - 10000000.0;

        double lon0 = ((zone - 1) * 6 - 180 + 3) * GeometryService.DegToRad;

        double M = y / k0;
        double mu = M / (a * (1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256));

        double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
        double e12 = e1 * e1;
        double e13 = e12 * e1;
        double e14 = e12 * e12;

        double phi1 = mu + (3 * e1 / 2 - 27 * e13 / 32) * Math.Sin(2 * mu)
                        + (21 * e12 / 16 - 55 * e14 / 32) * Math.Sin(4 * mu)
                        + (151 * e13 / 96) * Math.Sin(6 * mu);

        double sinPhi1 = Math.Sin(phi1);
        double cosPhi1 = Math.Cos(phi1);
        double tanPhi1 = sinPhi1 / cosPhi1;
        double N1 = a / Math.Sqrt(1 - e2 * sinPhi1 * sinPhi1);
        double T1 = tanPhi1 * tanPhi1;
        double C1 = e2 / (1 - e2) * cosPhi1 * cosPhi1;
        double R1 = a * (1 - e2) / Math.Pow(1 - e2 * sinPhi1 * sinPhi1, 1.5);
        double D = x / (N1 * k0);
        double D2 = D * D;
        double D4 = D2 * D2;
        double D6 = D4 * D2;

        double lat = phi1 - (N1 * tanPhi1 / R1) * (D2 / 2 - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * e2 / (1 - e2)) * D4 / 24
                    + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * e2 / (1 - e2) - 3 * C1 * C1) * D6 / 720);

        double lon = lon0 + (D - (1 + 2 * T1 + C1) * D2 * D / 6
                    + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * e2 / (1 - e2) + 24 * T1 * T1) * D4 * D / 120) / cosPhi1;

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Convertit des coordonnées WGS84 en UTM
    /// </summary>
    /// <param name="longitude">Longitude en degrés décimaux</param>
    /// <param name="latitude">Latitude en degrés décimaux</param>
    /// <returns>Tuple (zone, easting, northing, northern)</returns>
    public static (int Zone, double Easting, double Northing, bool Northern) Wgs84ToUtm(double longitude, double latitude)
    {
        int zone = (int)Math.Floor((longitude + 180) / 6) + 1;
        bool northern = latitude >= 0;

        const double k0 = 0.9996;
        const double a = EarthRadiusEquatorial;
        const double e2 = EarthEccentricitySquared;
        const double e4 = e2 * e2;
        const double e6 = e4 * e2;

        double lat = latitude * GeometryService.DegToRad;
        double lon = longitude * GeometryService.DegToRad;
        double lon0 = ((zone - 1) * 6 - 180 + 3) * GeometryService.DegToRad;

        double sinLat = Math.Sin(lat);
        double cosLat = Math.Cos(lat);
        double tanLat = sinLat / cosLat;
        double tan2 = tanLat * tanLat;
        double tan4 = tan2 * tan2;

        double N = a / Math.Sqrt(1 - e2 * sinLat * sinLat);
        double T = tan2;
        double C = e2 / (1 - e2) * cosLat * cosLat;
        double A = cosLat * (lon - lon0);
        double A2 = A * A;
        double A4 = A2 * A2;
        double A6 = A4 * A2;

        double M = a * ((1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256) * lat
                       - (3 * e2 / 8 + 3 * e4 / 32 + 45 * e6 / 1024) * Math.Sin(2 * lat)
                       + (15 * e4 / 256 + 45 * e6 / 1024) * Math.Sin(4 * lat)
                       - (35 * e6 / 3072) * Math.Sin(6 * lat));

        double easting = k0 * N * (A + (1 - T + C) * A2 * A / 6
                        + (5 - 18 * T + tan4 + 72 * C - 58 * e2 / (1 - e2)) * A4 * A / 120) + 500000.0;

        double northing = k0 * (M + N * tanLat * (A2 / 2 + (5 - T + 9 * C + 4 * C * C) * A4 / 24
                         + (61 - 58 * T + tan4 + 600 * C - 330 * e2 / (1 - e2)) * A6 / 720));

        if (!northern)
            northing += 10000000.0;

        return (zone, easting, northing, northern);
    }

    /// <summary>
    /// Calcule la distance géodésique (Vincenty) entre deux points WGS84
    /// </summary>
    /// <param name="lon1">Longitude du point 1 (degrés)</param>
    /// <param name="lat1">Latitude du point 1 (degrés)</param>
    /// <param name="lon2">Longitude du point 2 (degrés)</param>
    /// <param name="lat2">Latitude du point 2 (degrés)</param>
    /// <returns>Distance en mètres</returns>
    public static double VincentyDistance(double lon1, double lat1, double lon2, double lat2)
    {
        const double a = EarthRadiusEquatorial;
        const double b = EarthRadiusPolar;
        const double f = EarthFlattening;

        double L = (lon2 - lon1) * GeometryService.DegToRad;
        double U1 = Math.Atan((1 - f) * Math.Tan(lat1 * GeometryService.DegToRad));
        double U2 = Math.Atan((1 - f) * Math.Tan(lat2 * GeometryService.DegToRad));

        double sinU1 = Math.Sin(U1), cosU1 = Math.Cos(U1);
        double sinU2 = Math.Sin(U2), cosU2 = Math.Cos(U2);

        double lambda = L, lambdaP;
        int iterations = 0;
        double sinSigma, cosSigma, sigma, sinAlpha, cos2Alpha, cos2SigmaM;

        do
        {
            double sinLambda = Math.Sin(lambda), cosLambda = Math.Cos(lambda);
            sinSigma = Math.Sqrt((cosU2 * sinLambda) * (cosU2 * sinLambda) +
                (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

            if (sinSigma == 0) return 0;

            cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            sigma = Math.Atan2(sinSigma, cosSigma);
            sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            cos2Alpha = 1 - sinAlpha * sinAlpha;
            cos2SigmaM = cos2Alpha != 0 ? cosSigma - 2 * sinU1 * sinU2 / cos2Alpha : 0;

            double C = f / 16 * cos2Alpha * (4 + f * (4 - 3 * cos2Alpha));
            lambdaP = lambda;
            lambda = L + (1 - C) * f * sinAlpha *
                (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));

        } while (Math.Abs(lambda - lambdaP) > 1e-12 && ++iterations < 200);

        if (iterations >= 200) return double.NaN;

        double u2 = cos2Alpha * (a * a - b * b) / (b * b);
        double A = 1 + u2 / 16384 * (4096 + u2 * (-768 + u2 * (320 - 175 * u2)));
        double B = u2 / 1024 * (256 + u2 * (-128 + u2 * (74 - 47 * u2)));
        double deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
            B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));

        return b * A * (sigma - deltaSigma);
    }

    #endregion
}

/// <summary>
/// Informations sur un système de projection cartographique
/// </summary>
public class ProjectionInfo
{
    /// <summary>
    /// Code AutoCAD pour CGEOCS (ex: "RGF93.CC49")
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// Nom complet de la projection
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Pays principal d'utilisation
    /// </summary>
    public string Country { get; set; } = "";

    /// <summary>
    /// Région ou zone géographique
    /// </summary>
    public string Region { get; set; } = "";

    /// <summary>
    /// Code EPSG (pour référence et interopérabilité)
    /// </summary>
    public int Epsg { get; set; }

    /// <summary>
    /// Unité de mesure ("m" pour mètres, "deg" pour degrés)
    /// </summary>
    public string Unit { get; set; } = "m";

    /// <summary>
    /// Méridien central en degrés
    /// </summary>
    public double CentralMeridian { get; set; }

    /// <summary>
    /// Latitude d'origine en degrés
    /// </summary>
    public double LatitudeOrigin { get; set; }

    /// <summary>
    /// Faux Est (False Easting) en mètres
    /// </summary>
    public double FalseEasting { get; set; }

    /// <summary>
    /// Faux Nord (False Northing) en mètres
    /// </summary>
    public double FalseNorthing { get; set; }

    /// <summary>
    /// Coordonnée X minimale typique
    /// </summary>
    public double MinX { get; set; }

    /// <summary>
    /// Coordonnée X maximale typique
    /// </summary>
    public double MaxX { get; set; }

    /// <summary>
    /// Coordonnée Y minimale typique
    /// </summary>
    public double MinY { get; set; }

    /// <summary>
    /// Coordonnée Y maximale typique
    /// </summary>
    public double MaxY { get; set; }

    /// <summary>
    /// Description détaillée
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Affichage formaté pour UI
    /// </summary>
    public string DisplayName => $"{Name} [{Code}]";

    /// <summary>
    /// Vérifie si un point est dans les bornes typiques de cette projection
    /// </summary>
    public bool ContainsPoint(double x, double y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }

    public override string ToString() => DisplayName;
}
