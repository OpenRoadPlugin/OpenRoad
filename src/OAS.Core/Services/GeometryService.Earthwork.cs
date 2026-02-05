// Copyright 2026 Open Asphalte Contributors
// Licensed under the Apache License, Version 2.0

using Autodesk.AutoCAD.Geometry;

namespace OpenAsphalte.Services;

/// <summary>
/// GeometryService — Cubature, terrassement, tranchées et surfaces/MNT.
/// </summary>
public static partial class GeometryService
{
    #region Cubature et Terrassement

    /// <summary>
    /// Aires déblai/remblai d'un profil en travers par la méthode des trapèzes.
    /// </summary>
    public static (double CutArea, double FillArea) CrossSectionAreas(
        IList<Point3d> profilePoints, double referenceLevel)
    {
        if (profilePoints.Count < 2) return (0, 0);

        double cutArea = 0, fillArea = 0;

        for (int i = 0; i < profilePoints.Count - 1; i++)
        {
            double width = Math.Abs(profilePoints[i + 1].X - profilePoints[i].X);
            double h1 = profilePoints[i].Z - referenceLevel;
            double h2 = profilePoints[i + 1].Z - referenceLevel;

            if (h1 >= 0 && h2 >= 0)
            {
                cutArea += (h1 + h2) * width / 2;
            }
            else if (h1 <= 0 && h2 <= 0)
            {
                fillArea += (Math.Abs(h1) + Math.Abs(h2)) * width / 2;
            }
            else
            {
                double xIntersect = Math.Abs(h1) / (Math.Abs(h1) + Math.Abs(h2)) * width;
                if (h1 > 0)
                {
                    cutArea += h1 * xIntersect / 2;
                    fillArea += Math.Abs(h2) * (width - xIntersect) / 2;
                }
                else
                {
                    fillArea += Math.Abs(h1) * xIntersect / 2;
                    cutArea += h2 * (width - xIntersect) / 2;
                }
            }
        }

        return (cutArea, fillArea);
    }

    /// <summary>
    /// Volume entre deux profils par la moyenne des aires.
    /// </summary>
    public static double VolumeByAverageEndArea(double area1, double area2, double distance)
        => (area1 + area2) * distance / 2;

    /// <summary>
    /// Volume entre deux profils par la formule prismoïdale (plus précis).
    /// </summary>
    public static double VolumeByPrismoidal(double area1, double areaMiddle, double area2, double distance)
        => (area1 + 4 * areaMiddle + area2) * distance / 6;

    /// <summary>
    /// Volumes totaux de terrassement à partir d'une série de profils.
    /// </summary>
    public static (double CutVolume, double FillVolume) TotalEarthworkVolumes(
        IList<(double Pk, double CutArea, double FillArea)> sections)
    {
        if (sections.Count < 2) return (0, 0);

        double totalCut = 0, totalFill = 0;

        for (int i = 0; i < sections.Count - 1; i++)
        {
            double dist = Math.Abs(sections[i + 1].Pk - sections[i].Pk);
            totalCut += VolumeByAverageEndArea(sections[i].CutArea, sections[i + 1].CutArea, dist);
            totalFill += VolumeByAverageEndArea(sections[i].FillArea, sections[i + 1].FillArea, dist);
        }

        return (totalCut, totalFill);
    }

    /// <summary>
    /// Coefficient de foisonnement/compactage.
    /// </summary>
    public static double BulkingFactor(double volumeInPlace, double volumeLoose)
    {
        if (volumeInPlace < Tolerance) return 1;
        return volumeLoose / volumeInPlace;
    }

    /// <summary>
    /// Coefficients de foisonnement courants.
    /// </summary>
    public static class BulkingFactors
    {
        public const double TerreVegetale = 1.25;
        public const double Argile = 1.30;
        public const double Sable = 1.10;
        public const double Gravier = 1.15;
        public const double RocheFragmentee = 1.50;
        public const double RocheMassive = 1.65;
        public const double Enrobes = 1.30;
    }

    /// <summary>
    /// Applique le coefficient de foisonnement à un volume en place.
    /// </summary>
    public static double ApplyBulking(double volumeInPlace, double bulkingFactor)
        => volumeInPlace * bulkingFactor;

    /// <summary>
    /// Volume compacté à partir du volume foisonné.
    /// </summary>
    public static double CompactedVolume(double volumeLoose, double compactionRatio)
        => volumeLoose * compactionRatio;

    /// <summary>
    /// Volume d'un tronc de pyramide (excavation à talus).
    /// </summary>
    public static double FrustumVolume(double topArea, double bottomArea, double height)
        => height * (topArea + bottomArea + Math.Sqrt(topArea * bottomArea)) / 3;

    /// <summary>
    /// Volume d'une tranchée à parois verticales.
    /// </summary>
    public static double TrenchVolume(double width, double depth, double length)
        => width * depth * length;

    /// <summary>
    /// Volume d'une tranchée avec talutage.
    /// </summary>
    public static double TrenchVolumeWithSlope(double bottomWidth, double depth, double length, double sideSlope)
    {
        double topWidth = bottomWidth + 2 * sideSlope * depth;
        return (bottomWidth + topWidth) * depth / 2 * length;
    }

    /// <summary>
    /// Volume de lit de pose pour une canalisation.
    /// </summary>
    public static double BeddingVolume(double pipeOuterDiameter, double beddingThickness,
        double trenchWidth, double length)
        => trenchWidth * beddingThickness * length;

    /// <summary>
    /// Volume d'enrobage d'une canalisation.
    /// </summary>
    public static double SurroundVolume(double pipeOuterDiameter, double trenchWidth,
        double length, double coverAbovePipe)
    {
        double totalHeight = pipeOuterDiameter + coverAbovePipe;
        double pipeArea = Math.PI * pipeOuterDiameter * pipeOuterDiameter / 4;
        return (trenchWidth * totalHeight - pipeArea) * length;
    }

    #endregion

    #region Surfaces et MNT

    /// <summary>
    /// Interpole l'altitude Z en un point à partir d'un plan défini par trois points.
    /// </summary>
    public static double InterpolateZFromPlane(Point3d point, Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d normal = v1.CrossProduct(v2);

        if (Math.Abs(normal.Z) < Tolerance)
            return (p1.Z + p2.Z + p3.Z) / 3;

        return p1.Z - (normal.X * (point.X - p1.X) + normal.Y * (point.Y - p1.Y)) / normal.Z;
    }

    /// <summary>
    /// Pente d'un plan défini par trois points, en %.
    /// </summary>
    public static double PlaneSlope(Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d normal = v1.CrossProduct(v2);

        double horizontalLength = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
        if (Math.Abs(normal.Z) < Tolerance) return double.MaxValue;

        return horizontalLength / Math.Abs(normal.Z) * 100;
    }

    /// <summary>
    /// Orientation (exposition) d'un plan en grades (0 = Nord, 100 = Est).
    /// </summary>
    public static double PlaneAspect(Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        Vector3d normal = v1.CrossProduct(v2);

        double azimut = Math.Atan2(normal.X, normal.Y) * 200 / Math.PI;
        if (azimut < 0) azimut += 400;
        return azimut;
    }

    /// <summary>
    /// Volume d'un prisme triangulaire (triangle → plan horizontal de référence).
    /// </summary>
    public static double TriangularPrismVolume(Point3d p1, Point3d p2, Point3d p3, double referenceZ)
    {
        double baseArea = CalculateTriangleArea(p1, p2, p3);
        double avgHeight = ((p1.Z - referenceZ) + (p2.Z - referenceZ) + (p3.Z - referenceZ)) / 3;
        return baseArea * avgHeight;
    }

    /// <summary>
    /// Aire d'un triangle en 2D (projeté).
    /// </summary>
    public static double CalculateTriangleArea(Point3d p1, Point3d p2, Point3d p3)
        => Math.Abs((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y)) / 2;

    /// <summary>
    /// Aire 3D d'un triangle (surface réelle inclinée).
    /// </summary>
    public static double CalculateTriangleArea3D(Point3d p1, Point3d p2, Point3d p3)
    {
        Vector3d v1 = p2 - p1;
        Vector3d v2 = p3 - p1;
        return v1.CrossProduct(v2).Length / 2;
    }

    #endregion
}
