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

namespace OpenAsphalte.Services;

/// <summary>
/// GeometryService — Assainissement et hydraulique (Manning-Strickler, sections, débits).
/// </summary>
public static partial class GeometryService
{
    #region Assainissement - Hydraulique

    /// <summary>
    /// Débit par Manning-Strickler : Q = K × S × Rh^(2/3) × I^(1/2).
    /// </summary>
    public static double ManningStricklerFlow(double stricklerK, double section,
        double hydraulicRadius, double slopeDecimal)
        => stricklerK * section * Math.Pow(hydraulicRadius, 2.0 / 3.0) * Math.Sqrt(slopeDecimal);

    /// <summary>
    /// Vitesse d'écoulement par Manning-Strickler : V = K × Rh^(2/3) × I^(1/2).
    /// </summary>
    public static double ManningStricklerVelocity(double stricklerK, double hydraulicRadius, double slopeDecimal)
        => stricklerK * Math.Pow(hydraulicRadius, 2.0 / 3.0) * Math.Sqrt(slopeDecimal);

    /// <summary>
    /// Rayon hydraulique : Rh = Section mouillée / Périmètre mouillé.
    /// </summary>
    public static double HydraulicRadius(double wettedArea, double wettedPerimeter)
    {
        if (wettedPerimeter < Tolerance) return 0;
        return wettedArea / wettedPerimeter;
    }

    /// <summary>
    /// Paramètres hydrauliques d'une section circulaire partiellement remplie.
    /// </summary>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        CircularPipeHydraulics(double diameter, double fillRatio)
    {
        if (fillRatio <= 0) return (0, 0, 0);
        if (fillRatio >= 1)
        {
            double fullArea = Math.PI * diameter * diameter / 4;
            double fullPerimeter = Math.PI * diameter;
            return (fullArea, fullPerimeter, diameter / 4);
        }

        double r = diameter / 2;
        double h = fillRatio * diameter;
        double theta = 2 * Math.Acos(1 - h / r);
        double area = r * r * (theta - Math.Sin(theta)) / 2;
        double perimeter = r * theta;

        return (area, perimeter, area / perimeter);
    }

    /// <summary>
    /// Débit à pleine section pour une canalisation circulaire.
    /// </summary>
    public static double FullPipeFlow(double diameter, double slopePercent, double stricklerK = 70)
    {
        double area = Math.PI * diameter * diameter / 4;
        return ManningStricklerFlow(stricklerK, area, diameter / 4, slopePercent / 100);
    }

    /// <summary>
    /// Diamètre nécessaire pour un débit donné (pleine section).
    /// D = (Q × 4^(5/3) / (K × p × vI))^(3/8).
    /// </summary>
    public static double RequiredPipeDiameter(double flowRate, double slopePercent, double stricklerK = 70)
    {
        double slope = slopePercent / 100;
        double num = flowRate * Math.Pow(4, 5.0 / 3.0);
        double denom = stricklerK * Math.PI * Math.Sqrt(slope);
        return Math.Pow(num / denom, 3.0 / 8.0);
    }

    /// <summary>
    /// Pente minimale d'auto-curage.
    /// </summary>
    public static double SelfCleaningSlope(double diameter, double minVelocity = 0.60, double stricklerK = 70)
    {
        double rh = diameter / 4;
        double slope = Math.Pow(minVelocity / (stricklerK * Math.Pow(rh, 2.0 / 3.0)), 2);
        return slope * 100;
    }

    /// <summary>
    /// Coefficients de Strickler courants.
    /// </summary>
    public static class StricklerCoefficients
    {
        public const double BetonLisse = 80;
        public const double BetonCentrifuge = 90;
        public const double BetonOrdinary = 70;
        public const double BetonRuqueux = 60;
        public const double Gres = 75;
        public const double FonteDuctile = 80;
        public const double PVCNeuf = 100;
        public const double PVCUsage = 90;
        public const double PEHD = 100;
        public const double Acier = 85;
        public const double FosseEnTerre = 35;
        public const double FosseEnherbe = 25;
        public const double Enrochement = 30;
    }

    /// <summary>
    /// Hauteur de chute dans un regard.
    /// </summary>
    public static double ManholeDrop(double upstreamInvert, double downstreamInvert)
        => Math.Max(0, upstreamInvert - downstreamInvert);

    /// <summary>
    /// Vérifie si une chute nécessite un dispositif de dissipation d'énergie.
    /// </summary>
    public static bool RequiresEnergyDissipation(double dropHeight, double threshold = 0.80)
        => dropHeight > threshold;

    /// <summary>
    /// Paramètres hydrauliques d'une section ovoïde (T150).
    /// </summary>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        OvoidPipeHydraulics(double height, double fillRatio)
    {
        double fullArea = 0.510 * height * height;
        double fullPerimeter = 2.64 * height;

        if (fillRatio <= 0) return (0, 0, 0);
        if (fillRatio >= 1) return (fullArea, fullPerimeter, fullArea / fullPerimeter);

        double area = fullArea * fillRatio * (2 - fillRatio);
        double perimeter = fullPerimeter * Math.Sqrt(fillRatio);
        double rh = perimeter > 0 ? area / perimeter : 0;
        return (area, perimeter, rh);
    }

    /// <summary>
    /// Paramètres hydrauliques d'une section rectangulaire (cadre ou dalot).
    /// </summary>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        RectangularChannelHydraulics(double width, double height, double waterDepth)
    {
        double h = Math.Min(waterDepth, height);
        if (h <= 0) return (0, 0, 0);

        double area = width * h;
        double perimeter = width + 2 * h;
        return (area, perimeter, area / perimeter);
    }

    /// <summary>
    /// Paramètres hydrauliques d'une section trapézoïdale (fossé).
    /// </summary>
    public static (double WettedArea, double WettedPerimeter, double HydraulicRadius)
        TrapezoidalChannelHydraulics(double bottomWidth, double waterDepth, double sideSlope)
    {
        if (waterDepth <= 0) return (0, 0, 0);

        double topWidth = bottomWidth + 2 * sideSlope * waterDepth;
        double area = (bottomWidth + topWidth) * waterDepth / 2;
        double sideLength = waterDepth * Math.Sqrt(1 + sideSlope * sideSlope);
        double perimeter = bottomWidth + 2 * sideLength;
        return (area, perimeter, area / perimeter);
    }

    #endregion
}
