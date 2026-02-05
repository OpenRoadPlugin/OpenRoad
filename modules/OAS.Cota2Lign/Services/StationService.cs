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

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace OpenAsphalte.Modules.Cota2Lign.Services;

/// <summary>
/// Service de calcul des stations de cotation le long d'une polyligne.
/// Gère l'interdistance, les sommets et les arcs.
/// </summary>
public static class StationService
{
    /// <summary>
    /// Tolérance pour la comparaison des distances (évite les doublons)
    /// </summary>
    private const double DistanceTolerance = 1e-6;

    /// <summary>
    /// Construit la liste des stations de cotation entre deux distances sur une polyligne.
    /// </summary>
    /// <param name="polyline">Polyligne de référence</param>
    /// <param name="startDist">Distance curviligne de départ</param>
    /// <param name="endDist">Distance curviligne d'arrivée</param>
    /// <param name="interdistance">Interdistance entre les stations (0 = désactivé)</param>
    /// <param name="addVertices">Ajouter les sommets comme stations</param>
    /// <returns>Liste triée des distances de station</returns>
    public static List<double> BuildStations(
        Polyline polyline,
        double startDist,
        double endDist,
        double interdistance,
        bool addVertices)
    {
        var stations = new HashSet<double>();

        // Normaliser le sens (toujours du plus petit au plus grand)
        double minDist = Math.Min(startDist, endDist);
        double maxDist = Math.Max(startDist, endDist);
        bool isReversed = startDist > endDist;

        // Toujours ajouter le point de départ et d'arrivée
        stations.Add(minDist);
        stations.Add(maxDist);

        // Ajouter les stations à interdistance régulière
        if (interdistance > 0)
        {
            AddInterdistanceStations(stations, minDist, maxDist, interdistance);
        }

        // Ajouter les sommets de la polyligne
        if (addVertices)
        {
            AddVertexStations(polyline, stations, minDist, maxDist);
        }

        // Trier et retourner
        var result = stations.ToList();
        result.Sort();

        // Inverser si nécessaire pour respecter le sens de parcours
        if (isReversed)
        {
            result.Reverse();
        }

        return result;
    }

    /// <summary>
    /// Ajoute les stations à interdistance régulière.
    /// </summary>
    private static void AddInterdistanceStations(
        HashSet<double> stations,
        double minDist,
        double maxDist,
        double interdistance)
    {
        double currentDist = minDist + interdistance;

        while (currentDist < maxDist - DistanceTolerance)
        {
            stations.Add(currentDist);
            currentDist += interdistance;
        }
    }

    /// <summary>
    /// Ajoute les sommets de la polyligne comme stations.
    /// Gère également les arcs (segments avec bulge non nul).
    /// </summary>
    private static void AddVertexStations(
        Polyline polyline,
        HashSet<double> stations,
        double minDist,
        double maxDist)
    {
        // Calculer la distance de chaque sommet
        for (int i = 0; i < polyline.NumberOfVertices; i++)
        {
            double vertexDist = GetDistanceAtVertex(polyline, i);

            // Ajouter seulement si dans la plage
            if (vertexDist >= minDist - DistanceTolerance &&
                vertexDist <= maxDist + DistanceTolerance)
            {
                stations.Add(vertexDist);
            }
        }

        // Pour les arcs (segments avec bulge), ajouter des points intermédiaires
        // pour une meilleure représentation
        AddArcStations(polyline, stations, minDist, maxDist);
    }

    /// <summary>
    /// Ajoute des stations supplémentaires sur les arcs de la polyligne.
    /// </summary>
    private static void AddArcStations(
        Polyline polyline,
        HashSet<double> stations,
        double minDist,
        double maxDist)
    {
        for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
        {
            double bulge = polyline.GetBulgeAt(i);

            // Si c'est un arc (bulge non nul)
            if (Math.Abs(bulge) > DistanceTolerance)
            {
                // Récupérer les distances de début et fin du segment
                double segStartDist = GetDistanceAtVertex(polyline, i);
                double segEndDist = GetDistanceAtVertex(polyline, i + 1);

                // Calculer la longueur de l'arc
                double arcLength = segEndDist - segStartDist;

                // Ajouter le point milieu de l'arc s'il est dans la plage
                double midDist = segStartDist + arcLength / 2;

                if (midDist >= minDist && midDist <= maxDist)
                {
                    stations.Add(midDist);
                }
            }
        }

        // Gérer le dernier segment si la polyligne est fermée
        if (polyline.Closed && polyline.NumberOfVertices > 0)
        {
            int lastIndex = polyline.NumberOfVertices - 1;
            double bulge = polyline.GetBulgeAt(lastIndex);

            if (Math.Abs(bulge) > DistanceTolerance)
            {
                double segStartDist = GetDistanceAtVertex(polyline, lastIndex);
                double segEndDist = polyline.Length;
                double arcLength = segEndDist - segStartDist;
                double midDist = segStartDist + arcLength / 2;

                if (midDist >= minDist && midDist <= maxDist)
                {
                    stations.Add(midDist);
                }
            }
        }
    }

    /// <summary>
    /// Calcule la distance curviligne à un sommet donné de la polyligne.
    /// </summary>
    private static double GetDistanceAtVertex(Polyline polyline, int vertexIndex)
    {
        if (vertexIndex == 0)
        {
            return 0.0;
        }

        if (vertexIndex >= polyline.NumberOfVertices)
        {
            return polyline.Length;
        }

        var point = polyline.GetPoint3dAt(vertexIndex);
        return polyline.GetDistAtPoint(point);
    }

    /// <summary>
    /// Calcule le point projeté sur la polyligne cible à partir d'un point source.
    /// </summary>
    /// <param name="sourcePoint">Point source</param>
    /// <param name="targetPolyline">Polyligne cible</param>
    /// <returns>Point projeté sur la polyligne cible</returns>
    public static Point3d GetProjectedPoint(Point3d sourcePoint, Polyline targetPolyline)
    {
        return targetPolyline.GetClosestPointTo(sourcePoint, extend: false);
    }
}
