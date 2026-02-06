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
/// GeometryService — Calculs de voirie : tracé en plan et profil en long.
/// </summary>
public static partial class GeometryService
{
    #region Voirie - Tracé en plan

    /// <summary>
    /// Calcule les coordonnées sur une clothoïde (spirale de Cornu).
    /// </summary>
    /// <param name="A">Paramètre de la clothoïde (A² = R × L)</param>
    /// <param name="L">Longueur développée sur la clothoïde</param>
    /// <returns>Coordonnées (X, Y) et angle de rotation t</returns>
    public static (double X, double Y, double Tau) ClothoidCoordinates(double A, double L)
    {
        if (A < Tolerance) return (0, 0, 0);

        double tau = L * L / (2 * A * A);
        double tau2 = tau * tau;
        double tau4 = tau2 * tau2;

        double x = L * (1 - tau2 / 10 + tau4 / 216);
        double y = L * (tau / 3 - tau2 * tau / 42 + tau4 * tau / 1320);

        return (x, y, tau);
    }

    /// <summary>
    /// Calcule le paramètre A de la clothoïde (A² = R × L).
    /// </summary>
    public static double ClothoidParameter(double radius, double length)
        => Math.Sqrt(radius * length);

    /// <summary>
    /// Longueur minimale de transition clothoïde selon la règle du confort.
    /// L = V³ / (46.656 × R) où V en km/h.
    /// </summary>
    public static double MinClothoidLength(double radius, double speedKmh)
    {
        const double ComfortFactor = 46.656;
        return speedKmh * speedKmh * speedKmh / (ComfortFactor * radius);
    }

    /// <summary>
    /// Rayon minimum en fonction de la vitesse et du dévers.
    /// </summary>
    public static double MinCurveRadius(double speedKmh, double superelevationPercent, double frictionCoef = 0.13)
    {
        double v = speedKmh / 3.6;
        double e = superelevationPercent / 100.0;
        return v * v / (Gravity * (e + frictionCoef));
    }

    /// <summary>
    /// Dévers recommandé pour un rayon donné.
    /// </summary>
    public static double RecommendedSuperelevation(double radius, double speedKmh, double maxSuperelevation = 7.0)
    {
        double v = speedKmh / 3.6;
        double ideal = v * v / (Gravity * radius) * 100;
        return Math.Min(ideal, maxSuperelevation);
    }

    /// <summary>
    /// Surlargeur nécessaire en courbe. Formule : S = L² / (2R).
    /// </summary>
    public static double CurveWidening(double radius, double vehicleLength = 12.0)
    {
        if (radius < Tolerance) return 0;
        return vehicleLength * vehicleLength / (2 * radius);
    }

    /// <summary>
    /// Distance de visibilité d'arrêt.
    /// </summary>
    public static double StoppingDistance(double speedKmh, double reactionTime = 2.0,
        double frictionCoef = 0.35, double slopePercent = 0)
    {
        double v = speedKmh / 3.6;
        double slope = slopePercent / 100.0;
        double dr = v * reactionTime;
        double df = v * v / (2 * Gravity * (frictionCoef + slope));
        return dr + df;
    }

    /// <summary>
    /// Distance de visibilité de dépassement (formule empirique : 6 × V).
    /// </summary>
    public static double OvertakingDistance(double speedKmh)
        => 6 * speedKmh;

    #endregion

    #region Voirie - Profil en long

    /// <summary>
    /// Pente entre deux points en %.
    /// </summary>
    public static double SlopePercent(Point3d p1, Point3d p2)
    {
        double dx = Distance2D(p1, p2);
        if (dx < Tolerance) return 0;
        return (p2.Z - p1.Z) / dx * 100;
    }

    /// <summary>
    /// Pente entre deux points en ‰.
    /// </summary>
    public static double SlopePerMille(Point3d p1, Point3d p2)
        => SlopePercent(p1, p2) * 10;

    /// <summary>
    /// Paramètres d'un raccordement parabolique vertical.
    /// </summary>
    public static (double Radius, double Deflection, bool IsCrest) VerticalCurveParameters(
        double slope1, double slope2, double length)
    {
        double deltaSlope = (slope2 - slope1) / 100;
        bool isCrest = slope1 > slope2;
        double radius = length / Math.Abs(deltaSlope);
        double deflection = length * Math.Abs(deltaSlope) / 8;
        return (radius, deflection, isCrest);
    }

    /// <summary>
    /// Longueur minimale d'un raccordement convexe (point haut).
    /// </summary>
    public static double MinCrestCurveLength(double slope1, double slope2, double stoppingDistance,
        double eyeHeight = 1.10, double objectHeight = 0.15)
    {
        double A = Math.Abs(slope1 - slope2);
        double denom = 200 * (Math.Sqrt(eyeHeight) + Math.Sqrt(objectHeight));
        return A * stoppingDistance * stoppingDistance / (denom * denom);
    }

    /// <summary>
    /// Longueur minimale d'un raccordement concave (point bas).
    /// </summary>
    public static double MinSagCurveLength(double slope1, double slope2, double stoppingDistance,
        double headlightHeight = 0.60, double headlightAngle = 1.0)
    {
        double A = Math.Abs(slope1 - slope2);
        double tanAngle = Math.Tan(headlightAngle * DegToRad);
        return A * stoppingDistance * stoppingDistance / (200 * (headlightHeight + stoppingDistance * tanAngle));
    }

    /// <summary>
    /// Altitude sur une courbe parabolique verticale.
    /// </summary>
    public static double VerticalCurveElevation(double startZ, double startSlope, double curveLength,
        double position, double endSlope)
    {
        double i1 = startSlope / 100;
        double i2 = endSlope / 100;
        double r = (i2 - i1) / curveLength;
        return startZ + i1 * position + 0.5 * r * position * position;
    }

    #endregion
}
