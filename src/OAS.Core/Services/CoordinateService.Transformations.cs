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

namespace OpenAsphalte.Services;

/// <summary>
/// CoordinateService — Transformations de coordonnées (Lambert, CC, UTM, Vincenty).
/// </summary>
public static partial class CoordinateService
{
    #region Coordinate Transformations

    /// <summary>
    /// Convertit des coordonnées Lambert 93 en WGS84 (longitude/latitude).
    /// </summary>
    public static (double Longitude, double Latitude) Lambert93ToWgs84(double x, double y)
    {
        const double n = 0.7256077650532670;
        const double c = 11754255.426096;
        const double xs = 700000.0;
        const double ys = 12655612.049876;
        const double e = 0.0818191910428158;
        const double lon0 = 3.0 * GeometryService.DegToRad;

        double dx = x - xs;
        double dy = ys - y;
        double R = Math.Sqrt(dx * dx + dy * dy);
        double gamma = Math.Atan2(dx, dy);

        double latIso = Math.Log(c / R) / n;
        double lon = lon0 + gamma / n;

        double lat = 2 * Math.Atan(Math.Exp(latIso)) - Math.PI / 2;
        for (int i = 0; i < 10; i++)
        {
            double sinLat = Math.Sin(lat);
            lat = 2 * Math.Atan(Math.Exp(latIso + e * Math.Asinh(e * sinLat))) - Math.PI / 2;
        }

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Convertit des coordonnées WGS84 (longitude/latitude) en Lambert 93.
    /// </summary>
    public static (double X, double Y) Wgs84ToLambert93(double longitude, double latitude)
    {
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

        return (xs + R * Math.Sin(gamma), ys - R * Math.Cos(gamma));
    }

    /// <summary>
    /// Convertit des coordonnées CC (Conique Conforme) en WGS84.
    /// </summary>
    /// <param name="x">Coordonnée X en mètres</param>
    /// <param name="y">Coordonnée Y en mètres</param>
    /// <param name="zone">Zone CC (42 à 50)</param>
    public static (double Longitude, double Latitude) CCToWgs84(double x, double y, int zone)
    {
        if (zone < 42 || zone > 50)
            throw new ArgumentOutOfRangeException(nameof(zone), "Zone CC doit être entre 42 et 50");

        // Paramètres de l'ellipsoïde GRS80
        const double a = 6378137.0;
        const double e2 = 0.00669438002290;
        const double e = 0.0818191910428158;
        const double lon0 = 3.0;

        double lat1 = (zone - 0.75) * GeometryService.DegToRad;
        double lat2 = (zone + 0.75) * GeometryService.DegToRad;
        double lat0 = zone * GeometryService.DegToRad;

        const double fe = 1700000.0;
        double fn = (zone - 41) * 1000000.0 + 200000.0;

        double m1 = Math.Cos(lat1) / Math.Sqrt(1 - e2 * Math.Sin(lat1) * Math.Sin(lat1));
        double m2 = Math.Cos(lat2) / Math.Sqrt(1 - e2 * Math.Sin(lat2) * Math.Sin(lat2));

        double t0 = CalculateTValue(lat0, e);
        double t1 = CalculateTValue(lat1, e);
        double t2 = CalculateTValue(lat2, e);

        double n = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));
        double F = m1 / (n * Math.Pow(t1, n));
        double rho0 = a * F * Math.Pow(t0, n);

        double xp = x - fe;
        double yp = rho0 - (y - fn);
        double rho = Math.Sign(n) * Math.Sqrt(xp * xp + yp * yp);
        double theta = Math.Atan2(xp, yp);
        double t = Math.Pow(rho / (a * F), 1.0 / n);

        double lon = theta / n + lon0 * GeometryService.DegToRad;

        double lat = Math.PI / 2 - 2 * Math.Atan(t);
        for (int i = 0; i < 15; i++)
        {
            double sinLat = Math.Sin(lat);
            double latNew = Math.PI / 2 - 2 * Math.Atan(t * Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2));
            if (Math.Abs(latNew - lat) < 1e-12) break;
            lat = latNew;
        }

        return (lon * GeometryService.RadToDeg, lat * GeometryService.RadToDeg);
    }

    /// <summary>
    /// Calcule la valeur t pour la projection Lambert Conique Conforme.
    /// </summary>
    private static double CalculateTValue(double lat, double e)
    {
        double sinLat = Math.Sin(lat);
        return Math.Tan(Math.PI / 4 - lat / 2) / Math.Pow((1 - e * sinLat) / (1 + e * sinLat), e / 2);
    }

    /// <summary>
    /// Convertit des coordonnées UTM en WGS84.
    /// </summary>
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
    /// Convertit des coordonnées WGS84 en UTM.
    /// </summary>
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

        double MVal = a * ((1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256) * lat
                       - (3 * e2 / 8 + 3 * e4 / 32 + 45 * e6 / 1024) * Math.Sin(2 * lat)
                       + (15 * e4 / 256 + 45 * e6 / 1024) * Math.Sin(4 * lat)
                       - (35 * e6 / 3072) * Math.Sin(6 * lat));

        double easting = k0 * N * (A + (1 - T + C) * A2 * A / 6
                        + (5 - 18 * T + tan4 + 72 * C - 58 * e2 / (1 - e2)) * A4 * A / 120) + 500000.0;

        double northing = k0 * (MVal + N * tanLat * (A2 / 2 + (5 - T + 9 * C + 4 * C * C) * A4 / 24
                         + (61 - 58 * T + tan4 + 600 * C - 330 * e2 / (1 - e2)) * A6 / 720));

        if (!northern)
            northing += 10000000.0;

        return (zone, easting, northing, northern);
    }

    /// <summary>
    /// Calcule la distance géodésique (Vincenty) entre deux points WGS84.
    /// </summary>
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
        double ACoeff = 1 + u2 / 16384 * (4096 + u2 * (-768 + u2 * (320 - 175 * u2)));
        double BCoeff = u2 / 1024 * (256 + u2 * (-128 + u2 * (74 - 47 * u2)));
        double deltaSigma = BCoeff * sinSigma * (cos2SigmaM + BCoeff / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
            BCoeff / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));

        return b * ACoeff * (sigma - deltaSigma);
    }

    #endregion
}
