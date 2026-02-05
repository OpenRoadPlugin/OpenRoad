// Copyright 2026 Open Asphalte Contributors
// Licensed under the Apache License, Version 2.0

namespace OpenAsphalte.Services;

/// <summary>
/// CoordinateService — Données de projections intégrées (built-in).
/// </summary>
public static partial class CoordinateService
{
    /// <summary>
    /// Charge les projections intégrées au programme
    /// </summary>
    private static void LoadBuiltInProjections()
    {
        _projections =
        [
            // ═══════════════════════════════════════════════════════════════
            // FRANCE - RGF93 / Lambert 93 (système national)
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "RGF93.LAMB93",
                Name = "RGF93 / Lambert 93",
                Country = "France",
                Region = "France métropolitaine",
                Epsg = 2154,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 46.5,
                FalseEasting = 700000,
                FalseNorthing = 6600000,
                MinX = 100000, MaxX = 1200000,
                MinY = 6000000, MaxY = 7200000,
                Description = "Projection conique conforme de Lambert - Système national français"
            },

            // ═══════════════════════════════════════════════════════════════
            // FRANCE - RGF93 / CC42 à CC50 (zones coniques conformes)
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "RGF93.CC42",
                Name = "RGF93 / CC42",
                Country = "France",
                Region = "Corse, Côte d'Azur (sud)",
                Epsg = 3942,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 42.0,
                FalseEasting = 1700000,
                FalseNorthing = 1200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 1000000, MaxY = 1400000,
                Description = "Zone CC42 - Latitude origine 42°N"
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
                Description = "Zone CC43 - Latitude origine 43°N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC44",
                Name = "RGF93 / CC44",
                Country = "France",
                Region = "Aquitaine, Midi-Pyrénées (sud)",
                Epsg = 3944,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 44.0,
                FalseEasting = 1700000,
                FalseNorthing = 3200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 3000000, MaxY = 3400000,
                Description = "Zone CC44 - Latitude origine 44°N"
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
                Description = "Zone CC45 - Latitude origine 45°N"
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
                Description = "Zone CC46 - Latitude origine 46°N"
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
                Description = "Zone CC47 - Latitude origine 47°N"
            },
            new ProjectionInfo
            {
                Code = "RGF93.CC48",
                Name = "RGF93 / CC48",
                Country = "France",
                Region = "Île-de-France, Normandie, Bretagne",
                Epsg = 3948,
                Unit = "m",
                CentralMeridian = 3.0,
                LatitudeOrigin = 48.0,
                FalseEasting = 1700000,
                FalseNorthing = 7200000,
                MinX = 1200000, MaxX = 2200000,
                MinY = 7000000, MaxY = 7400000,
                Description = "Zone CC48 - Latitude origine 48°N"
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
                Description = "Zone CC49 - Latitude origine 49°N"
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
                Description = "Zone CC50 - Latitude origine 50°N"
            },

            // ═══════════════════════════════════════════════════════════════
            // FRANCE - NTF / Lambert zones (ancien système)
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "NTF.Lambert-1-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone I",
                Country = "France",
                Region = "Nord de la France",
                Epsg = 27561,
                Unit = "m",
                CentralMeridian = 2.337229167,
                LatitudeOrigin = 49.5,
                FalseEasting = 600000,
                FalseNorthing = 200000,
                MinX = 0, MaxX = 1200000,
                MinY = 0, MaxY = 400000,
                Description = "Ancien système NTF - Zone I (Nord)"
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
                Description = "Ancien système NTF - Zone II (Centre)"
            },
            new ProjectionInfo
            {
                Code = "NTF.Lambert-2e-ClrkIGN",
                Name = "NTF (Paris) / Lambert zone II étendu",
                Country = "France",
                Region = "France métropolitaine",
                Epsg = 27572,
                Unit = "m",
                CentralMeridian = 2.337229167,
                LatitudeOrigin = 46.8,
                FalseEasting = 600000,
                FalseNorthing = 2200000,
                MinX = 0, MaxX = 1200000,
                MinY = 1600000, MaxY = 2800000,
                Description = "Ancien système NTF - Zone II étendu (France entière)"
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
                Description = "Ancien système NTF - Zone III (Sud)"
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
                Description = "Ancien système NTF - Zone IV (Corse)"
            },

            // ═══════════════════════════════════════════════════════════════
            // BELGIQUE
            // ═══════════════════════════════════════════════════════════════
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
                Description = "Système belge Lambert 72"
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
                Description = "Système belge Lambert 2008 (ETRS89)"
            },

            // ═══════════════════════════════════════════════════════════════
            // SUISSE
            // ═══════════════════════════════════════════════════════════════
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
                Description = "Ancien système suisse LV03"
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
                Description = "Système suisse actuel LV95"
            },

            // ═══════════════════════════════════════════════════════════════
            // LUXEMBOURG
            // ═══════════════════════════════════════════════════════════════
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
                Description = "Système luxembourgeois Transverse Mercator"
            },

            // ═══════════════════════════════════════════════════════════════
            // ALLEMAGNE
            // ═══════════════════════════════════════════════════════════════
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

            // ═══════════════════════════════════════════════════════════════
            // ESPAGNE
            // ═══════════════════════════════════════════════════════════════
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

            // ═══════════════════════════════════════════════════════════════
            // ITALIE
            // ═══════════════════════════════════════════════════════════════
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
                Description = "Système italien RDN2008"
            },

            // ═══════════════════════════════════════════════════════════════
            // ROYAUME-UNI
            // ═══════════════════════════════════════════════════════════════
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

            // ═══════════════════════════════════════════════════════════════
            // PAYS-BAS
            // ═══════════════════════════════════════════════════════════════
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
                Description = "Système néerlandais RD New (Rijksdriehoek)"
            },

            // ═══════════════════════════════════════════════════════════════
            // CANADA - Québec
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "NAD83.MTM-zone-7",
                Name = "NAD83 / MTM zone 7",
                Country = "Canada",
                Region = "Québec (Montréal)",
                Epsg = 32187,
                Unit = "m",
                CentralMeridian = -70.5,
                LatitudeOrigin = 0.0,
                FalseEasting = 304800,
                FalseNorthing = 0,
                MinX = 0, MaxX = 610000,
                MinY = 4800000, MaxY = 5400000,
                Description = "Modified Transverse Mercator zone 7 (Québec)"
            },
            new ProjectionInfo
            {
                Code = "NAD83.MTM-zone-8",
                Name = "NAD83 / MTM zone 8",
                Country = "Canada",
                Region = "Québec (Québec City)",
                Epsg = 32188,
                Unit = "m",
                CentralMeridian = -73.5,
                LatitudeOrigin = 0.0,
                FalseEasting = 304800,
                FalseNorthing = 0,
                MinX = 0, MaxX = 610000,
                MinY = 4800000, MaxY = 5400000,
                Description = "Modified Transverse Mercator zone 8 (Québec)"
            },

            // ═══════════════════════════════════════════════════════════════
            // UTM ZONES GLOBALES (WGS84)
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "WGS84.UTM-29N",
                Name = "WGS 84 / UTM zone 29N",
                Country = "Global",
                Region = "Longitude -12° à -6° (Portugal, Açores)",
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
                Region = "Longitude -6° à 0° (Espagne, UK ouest)",
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
                Region = "Longitude 0° à 6° (France ouest, Benelux)",
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
                Region = "Longitude 6° à 12° (France est, Allemagne)",
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
                Region = "Longitude 12° à 18° (Europe centrale)",
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

            // ═══════════════════════════════════════════════════════════════
            // FRANCE - DOM-TOM
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "RGAF09.UTM-zone-20N",
                Name = "RGAF09 / UTM zone 20N",
                Country = "France",
                Region = "Antilles françaises (Guadeloupe, Martinique)",
                Epsg = 5490,
                Unit = "m",
                CentralMeridian = -63.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 20N pour les Antilles françaises"
            },
            new ProjectionInfo
            {
                Code = "RGFG95.UTM-zone-22N",
                Name = "RGFG95 / UTM zone 22N",
                Country = "France",
                Region = "Guyane française",
                Epsg = 2972,
                Unit = "m",
                CentralMeridian = -51.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 0,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 9400000,
                Description = "UTM zone 22N pour la Guyane française"
            },
            new ProjectionInfo
            {
                Code = "RGR92.UTM-zone-40S",
                Name = "RGR92 / UTM zone 40S",
                Country = "France",
                Region = "La Réunion",
                Epsg = 2975,
                Unit = "m",
                CentralMeridian = 57.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 500000,
                FalseNorthing = 10000000,
                MinX = 166000, MaxX = 834000,
                MinY = 0, MaxY = 10000000,
                Description = "UTM zone 40S pour La Réunion"
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

            // ═══════════════════════════════════════════════════════════════
            // COORDONNÉES GÉOGRAPHIQUES
            // ═══════════════════════════════════════════════════════════════
            new ProjectionInfo
            {
                Code = "LL84",
                Name = "WGS 84 (géographique)",
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
                Description = "Coordonnées géographiques WGS84 (longitude/latitude)"
            },
            new ProjectionInfo
            {
                Code = "LL-RGF93",
                Name = "RGF93 (géographique)",
                Country = "France",
                Region = "France métropolitaine",
                Epsg = 4171,
                Unit = "deg",
                CentralMeridian = 0.0,
                LatitudeOrigin = 0.0,
                FalseEasting = 0,
                FalseNorthing = 0,
                MinX = -10, MaxX = 15,
                MinY = 40, MaxY = 55,
                Description = "Coordonnées géographiques RGF93 (longitude/latitude)"
            }
        ];
    }
}
