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

namespace OpenAsphalte.Modules.DynamicSnap.Models;

/// <summary>
/// Configuration pour une session d'accrochage dynamique.
/// Permet de personnaliser le comportement du système de snap.
/// </summary>
public sealed class SnapConfiguration
{
    /// <summary>
    /// Modes d'accrochage actifs
    /// </summary>
    public SnapMode ActiveModes { get; set; } = SnapMode.PolylineFull;

    /// <summary>
    /// Tolérance de détection en unités du dessin.
    /// Distance maximale entre le curseur et un point pour qu'il soit détecté.
    /// </summary>
    public double Tolerance { get; set; } = 0.0;

    /// <summary>
    /// Tolérance relative à la taille de vue (0.01 = 1% de la vue).
    /// Utilisé si Tolerance == 0.
    /// </summary>
    public double ToleranceViewRatio { get; set; } = 0.02;

    /// <summary>
    /// Rayon du marqueur visuel en unités du dessin.
    /// </summary>
    public double MarkerSize { get; set; } = 0.0;

    /// <summary>
    /// Taille du marqueur relative à la vue (0.01 = 1% de la vue).
    /// Utilisé si MarkerSize == 0.
    /// </summary>
    public double MarkerViewRatio { get; set; } = 0.015;

    /// <summary>
    /// Couleur du marqueur (index AutoCAD 1-255).
    /// 1 = Rouge, 2 = Jaune, 3 = Vert, 4 = Cyan, 5 = Bleu, 6 = Magenta, 7 = Blanc
    /// </summary>
    public short MarkerColor { get; set; } = 3; // Vert par défaut

    /// <summary>
    /// Couleur du marqueur actif/sélectionné
    /// </summary>
    public short ActiveMarkerColor { get; set; } = 1; // Rouge

    /// <summary>
    /// Nombre de segments pour dessiner les marqueurs circulaires
    /// </summary>
    public int MarkerSegments { get; set; } = 32;

    /// <summary>
    /// Afficher le marqueur du point le plus proche uniquement
    /// </summary>
    public bool ShowOnlyNearest { get; set; } = true;

    /// <summary>
    /// Afficher tous les marqueurs dans la zone de tolérance
    /// </summary>
    public bool ShowAllInRange { get; set; } = false;

    /// <summary>
    /// Nombre maximum de marqueurs à afficher si ShowAllInRange = true
    /// </summary>
    public int MaxVisibleMarkers { get; set; } = 10;

    /// <summary>
    /// Type de marqueur visuel à afficher
    /// </summary>
    public MarkerStyle MarkerStyle { get; set; } = MarkerStyle.Circle;

    /// <summary>
    /// Afficher un tooltip avec les informations du point
    /// </summary>
    public bool ShowTooltip { get; set; } = false;

    /// <summary>
    /// Filtrer par types d'entités AutoCAD spécifiques
    /// </summary>
    public IReadOnlyList<string>? EntityTypeFilter { get; set; }

    /// <summary>
    /// Filtrer par calques spécifiques
    /// </summary>
    public IReadOnlyList<string>? LayerFilter { get; set; }

    /// <summary>
    /// Indique si l'accrochage OSNAP d'AutoCAD doit être désactivé temporairement
    /// </summary>
    public bool DisableAutoCADOsnap { get; set; } = true;

    /// <summary>
    /// Créer une configuration par défaut pour les polylignes
    /// </summary>
    public static SnapConfiguration ForPolylines()
    {
        return new SnapConfiguration
        {
            ActiveModes = SnapMode.PolylineFull,
            EntityTypeFilter = new[] { "LWPOLYLINE", "POLYLINE" }
        };
    }

    /// <summary>
    /// Créer une configuration pour les sommets uniquement
    /// </summary>
    public static SnapConfiguration VerticesOnly()
    {
        return new SnapConfiguration
        {
            ActiveModes = SnapMode.Vertex | SnapMode.Endpoint,
            EntityTypeFilter = new[] { "LWPOLYLINE", "POLYLINE" }
        };
    }

    /// <summary>
    /// Créer une configuration pour tous les types d'entités
    /// </summary>
    public static SnapConfiguration All()
    {
        return new SnapConfiguration
        {
            ActiveModes = SnapMode.All,
            EntityTypeFilter = null
        };
    }

    /// <summary>
    /// Clone la configuration
    /// </summary>
    public SnapConfiguration Clone()
    {
        return new SnapConfiguration
        {
            ActiveModes = ActiveModes,
            Tolerance = Tolerance,
            ToleranceViewRatio = ToleranceViewRatio,
            MarkerSize = MarkerSize,
            MarkerViewRatio = MarkerViewRatio,
            MarkerColor = MarkerColor,
            ActiveMarkerColor = ActiveMarkerColor,
            MarkerSegments = MarkerSegments,
            ShowOnlyNearest = ShowOnlyNearest,
            ShowAllInRange = ShowAllInRange,
            MaxVisibleMarkers = MaxVisibleMarkers,
            MarkerStyle = MarkerStyle,
            ShowTooltip = ShowTooltip,
            EntityTypeFilter = EntityTypeFilter?.ToList(),
            LayerFilter = LayerFilter?.ToList(),
            DisableAutoCADOsnap = DisableAutoCADOsnap
        };
    }
}

/// <summary>
/// Style de marqueur visuel pour l'accrochage
/// </summary>
public enum MarkerStyle
{
    /// <summary>
    /// Cercle simple
    /// </summary>
    Circle,

    /// <summary>
    /// Croix (+)
    /// </summary>
    Cross,

    /// <summary>
    /// Croix diagonale (X)
    /// </summary>
    XMark,

    /// <summary>
    /// Carré
    /// </summary>
    Square,

    /// <summary>
    /// Losange
    /// </summary>
    Diamond,

    /// <summary>
    /// Triangle
    /// </summary>
    Triangle,

    /// <summary>
    /// Cercle avec croix
    /// </summary>
    CircleWithCross
}
