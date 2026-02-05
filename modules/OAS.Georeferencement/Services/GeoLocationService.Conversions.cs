// Copyright 2026 Open Asphalte Contributors
// Licensed under the Apache License, Version 2.0

using OpenAsphalte.Services;

namespace OpenAsphalte.Modules.Georeferencement.Services;

/// <summary>
/// GeoLocationService — Implémentations des conversions de coordonnées par système de projection.
/// </summary>
public static partial class GeoLocationService
{
    #region Private Methods - Coordinate Conversions

    /// <summary>
    /// Extrait le numéro de zone CC du code (ex: "RGF93.CC49" -> 49).
    /// </summary>
    private static int ExtractCCZone(string code)
    {
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
    /// Extrait les informations de zone UTM du code.
    /// </summary>
    private static (int Zone, bool Northern) ExtractUtmZoneInfo(string code, ProjectionInfo projection)
    {
        bool northern = !code.Contains("S", StringComparison.OrdinalIgnoreCase) ||
                       code.Contains("N", StringComparison.OrdinalIgnoreCase);

        int zone = 0;

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

        if (zone == 0 && projection.CentralMeridian != 0)
        {
            zone = (int)Math.Floor((projection.CentralMeridian + 180) / 6) + 1;
        }

        if (projection.FalseNorthing >= 10000000)
        {
            northern = false;
        }

        return (zone, northern);
    }

    /// <summary>
    /// Conversion NTF Lambert (zones I, II, III, IV) vers WGS84.
    /// </summary>
    private static (double Longitude, double Latitude) NtfLambertToWgs84(double x, double y, string code)
    {
        const double e = 0.08248325676;
        const double parisMeridian = 2.337229167;

        double n, c, xs, ys;

        if (code.Contains("LAMBERT-1") || code.Contains("ZONE-I"))
        {
            n = 0.7604059656; c = 11603796.98; xs = 600000; ys = 5657616.674;
        }
        else if (code.Contains("LAMBERT-2E") || code.Contains("2E"))
        {
            n = 0.7289686274; c = 11745793.39; xs = 600000; ys = 8199695.768;
        }
        else if (code.Contains("LAMBERT-2") || code.Contains("ZONE-II"))
        {
            n = 0.7289686274; c = 11745793.39; xs = 600000; ys = 6199695.768;
        }
        else if (code.Contains("LAMBERT-3") || code.Contains("ZONE-III"))
        {
            n = 0.6959127966; c = 11947992.52; xs = 600000; ys = 6791905.085;
        }
        else if (code.Contains("LAMBERT-4") || code.Contains("ZONE-IV"))
        {
            n = 0.6712679322; c = 12136281.99; xs = 234358; ys = 7053300.189;
        }
        else
        {
            // Défaut: Lambert II étendu
            n = 0.7289686274; c = 11745793.39; xs = 600000; ys = 8199695.768;
        }

        double dx = x - xs;
        double dy = ys - y;
        double R = Math.Sqrt(dx * dx + dy * dy);
        double gamma = Math.Atan2(dx, dy);

        double latIso = Math.Log(c / R) / n;
        double lon = parisMeridian + (gamma / n) * GeometryService.RadToDeg;

        double lat = 2 * Math.Atan(Math.Exp(latIso)) - Math.PI / 2;
        for (int i = 0; i < 10; i++)
        {
            double sinLat = Math.Sin(lat);
            lat = 2 * Math.Atan(Math.Exp(latIso) *
                Math.Pow((1 + e * sinLat) / (1 - e * sinLat), e / 2)) - Math.PI / 2;
        }

        double latDeg = lat * GeometryService.RadToDeg;

        // Correction approximative NTF -> WGS84
        const double dLat = -0.00015;
        const double dLon = 0.00008;

        return (lon + dLon, latDeg + dLat);
    }

    /// <summary>
    /// Conversion coordonnées suisses (LV03/LV95) vers WGS84.
    /// </summary>
    private static (double Longitude, double Latitude) SwissToWgs84(double x, double y, ProjectionInfo projection)
    {
        bool isLv95 = projection.Code.Contains("LV95") || projection.Epsg == 2056;

        double y_aux, x_aux;

        if (isLv95)
        {
            y_aux = (x - 2600000) / 1000000;
            x_aux = (y - 1200000) / 1000000;
        }
        else
        {
            y_aux = (x - 600000) / 1000000;
            x_aux = (y - 200000) / 1000000;
        }

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

        return (lon * 100 / 36, lat * 100 / 36);
    }

    /// <summary>
    /// Conversion Lambert belge (BD72 / Lambert 2008) vers WGS84.
    /// </summary>
    private static (double Longitude, double Latitude) BelgianLambertToWgs84(double x, double y, ProjectionInfo projection)
    {
        bool isLambert2008 = projection.Epsg == 3812 || projection.Code.Contains("2008");

        double xs, ys, n, c;

        if (isLambert2008)
        {
            xs = 649328.0;
            ys = 665262.0;
            double lat0 = 50.797815 * GeometryService.DegToRad;
            n = Math.Sin(lat0);
            c = 6378137.0 * Math.Cos(lat0) / (n * Math.Sqrt(1 - 0.00669438 * Math.Sin(lat0) * Math.Sin(lat0)));
            c *= Math.Exp(n * LatitudeIsometric(lat0, 0.0818191910435));
        }
        else
        {
            xs = 150000.013;
            ys = 5400088.438;
            n = 0.7716421928;
            c = 11598255.97;
        }

        double dx = x - xs;
        double dy = ys - y;
        double R = Math.Sqrt(dx * dx + dy * dy);
        double gamma = Math.Atan2(dx, dy);

        double lon0 = 4.367486666667 * GeometryService.DegToRad;
        double lon = lon0 + gamma / n;

        double latIso = Math.Log(c / R) / n;
        double lat = LatitudeFromIsometric(latIso, 0.0818191910435);

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Conversion Luxembourg LUREF vers WGS84.
    /// </summary>
    private static (double Longitude, double Latitude) LuxembourgToWgs84(double x, double y)
    {
        const double lon0 = 6.166666666667;
        const double lat0 = 49.833333333333;
        const double k0 = 1.0;
        const double fe = 80000;
        const double fn = 100000;
        const double a = 6378388.0;
        const double e2 = 0.006722670022;

        return TransverseMercatorToWgs84(x, y, lon0, lat0, k0, fe, fn, a, e2);
    }

    /// <summary>
    /// Conversion RD néerlandais vers WGS84 (polynômes Kadaster).
    /// </summary>
    private static (double Longitude, double Latitude) DutchRdToWgs84(double x, double y)
    {
        double dx = (x - 155000) * 1e-5;
        double dy = (y - 463000) * 1e-5;

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
    /// Conversion British National Grid (OSGB36) vers WGS84.
    /// </summary>
    private static (double Longitude, double Latitude) BritishNationalGridToWgs84(double x, double y)
    {
        const double a = 6377563.396;
        const double e2 = 0.00667054;
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

        // Conversion OSGB36 -> WGS84 (corrections moyennes)
        return (lon * GeometryService.RadToDeg + 0.000089, lat * GeometryService.RadToDeg + 0.000050);
    }

    /// <summary>
    /// Conversion Transverse Mercator générique vers WGS84.
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
    /// Approximation de la projection inverse basée sur les paramètres.
    /// </summary>
    private static (double Longitude, double Latitude) ApproximateInverseProjection(double x, double y, ProjectionInfo projection)
    {
        double lon = projection.CentralMeridian + (x - projection.FalseEasting) / (111320 * Math.Cos(projection.LatitudeOrigin * GeometryService.DegToRad));
        double lat = projection.LatitudeOrigin + (y - projection.FalseNorthing) / 110540;

        return (Math.Max(-180, Math.Min(180, lon)), Math.Max(-90, Math.Min(90, lat)));
    }

    /// <summary>
    /// Approximation de la projection directe basée sur les paramètres.
    /// </summary>
    private static (double X, double Y) ApproximateForwardProjection(double longitude, double latitude, ProjectionInfo projection)
    {
        double x = projection.FalseEasting + (longitude - projection.CentralMeridian) * 111320 * Math.Cos(projection.LatitudeOrigin * GeometryService.DegToRad);
        double y = projection.FalseNorthing + (latitude - projection.LatitudeOrigin) * 110540;
        return (x, y);
    }

    /// <summary>
    /// Calcule la latitude isométrique.
    /// </summary>
    private static double LatitudeIsometric(double lat, double e)
    {
        double sinLat = Math.Sin(lat);
        return Math.Log(Math.Tan(Math.PI / 4 + lat / 2) *
            Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2));
    }

    /// <summary>
    /// Calcule la latitude depuis la latitude isométrique (itératif).
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
}
