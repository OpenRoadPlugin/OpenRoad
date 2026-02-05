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

using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.DynamicSnap.Models;

/// <summary>
/// Modes d'accrochage disponibles pour le système OAS Dynamic Snap.
/// Peut être combiné avec l'opérateur | (flags).
/// </summary>
[Flags]
public enum SnapMode
{
    /// <summary>
    /// Aucun accrochage
    /// </summary>
    None = 0,

    /// <summary>
    /// Accrochage aux sommets (extrémités des segments)
    /// </summary>
    Vertex = 1 << 0,

    /// <summary>
    /// Accrochage aux extrémités de l'entité
    /// </summary>
    Endpoint = 1 << 1,

    /// <summary>
    /// Accrochage au milieu des segments
    /// </summary>
    Midpoint = 1 << 2,

    /// <summary>
    /// Accrochage au point le plus proche sur l'entité
    /// </summary>
    Nearest = 1 << 3,

    /// <summary>
    /// Accrochage au centre (pour cercles, arcs)
    /// </summary>
    Center = 1 << 4,

    /// <summary>
    /// Accrochage aux intersections
    /// </summary>
    Intersection = 1 << 5,

    /// <summary>
    /// Accrochage perpendiculaire
    /// </summary>
    Perpendicular = 1 << 6,

    /// <summary>
    /// Accrochage tangent
    /// </summary>
    Tangent = 1 << 7,

    /// <summary>
    /// Accrochage aux quadrants (0°, 90°, 180°, 270°)
    /// </summary>
    Quadrant = 1 << 8,

    /// <summary>
    /// Accrochage au point d'insertion
    /// </summary>
    Insertion = 1 << 9,

    /// <summary>
    /// Accrochage aux nœuds (objets Point)
    /// </summary>
    Node = 1 << 10,

    /// <summary>
    /// Accrochage parallèle
    /// </summary>
    Parallel = 1 << 11,

    // ═══════════════════════════════════════════════════════════
    // COMBINAISONS PRÉDÉFINIES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Accrochage aux points sur polyligne (sommets + extrémités)
    /// </summary>
    PolylinePoints = Vertex | Endpoint,

    /// <summary>
    /// Accrochage complet pour polylignes
    /// </summary>
    PolylineFull = Vertex | Endpoint | Midpoint | Nearest,

    /// <summary>
    /// Accrochage pour cercles et arcs
    /// </summary>
    CircleArc = Center | Quadrant | Nearest,

    /// <summary>
    /// Accrochage géométrique de base
    /// </summary>
    Basic = Vertex | Endpoint | Midpoint | Center,

    /// <summary>
    /// Tous les modes d'accrochage
    /// </summary>
    All = Vertex | Endpoint | Midpoint | Nearest | Center |
          Intersection | Perpendicular | Tangent | Quadrant |
          Insertion | Node | Parallel
}

/// <summary>
/// Extensions pour SnapMode
/// </summary>
public static class SnapModeExtensions
{
    /// <summary>
    /// Vérifie si un mode spécifique est activé
    /// </summary>
    public static bool HasMode(this SnapMode mode, SnapMode flag)
    {
        return (mode & flag) == flag;
    }

    /// <summary>
    /// Retourne le nom localisé du mode
    /// </summary>
    public static string GetDisplayName(this SnapMode mode)
    {
        return mode switch
        {
            SnapMode.Vertex => L10n.T("dynamicsnap.mode.vertex", "Sommet"),
            SnapMode.Endpoint => L10n.T("dynamicsnap.mode.endpoint", "Extrémité"),
            SnapMode.Midpoint => L10n.T("dynamicsnap.mode.midpoint", "Milieu"),
            SnapMode.Nearest => L10n.T("dynamicsnap.mode.nearest", "Proche"),
            SnapMode.Center => L10n.T("dynamicsnap.mode.center", "Centre"),
            SnapMode.Intersection => L10n.T("dynamicsnap.mode.intersection", "Intersection"),
            SnapMode.Perpendicular => L10n.T("dynamicsnap.mode.perpendicular", "Perpendiculaire"),
            SnapMode.Tangent => L10n.T("dynamicsnap.mode.tangent", "Tangent"),
            SnapMode.Quadrant => L10n.T("dynamicsnap.mode.quadrant", "Quadrant"),
            SnapMode.Insertion => L10n.T("dynamicsnap.mode.insertion", "Insertion"),
            SnapMode.Node => L10n.T("dynamicsnap.mode.node", "Nœud"),
            SnapMode.Parallel => L10n.T("dynamicsnap.mode.parallel", "Parallèle"),
            _ => mode.ToString()
        };
    }
}
