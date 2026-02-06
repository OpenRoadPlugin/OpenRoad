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

namespace OpenAsphalte.Modules.DynamicSnap.Models;

/// <summary>
/// Configuration pour la surbrillance des entités sélectionnées.
/// Permet de personnaliser l'apparence des entités mises en évidence
/// lors des interactions utilisateur (sélection, actions, etc.).
/// </summary>
public sealed class HighlightConfiguration
{
    /// <summary>
    /// Indique si la surbrillance est activée
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Couleur de surbrillance (index AutoCAD 1-255).
    /// Utilisée pour les entités Primary et Secondary.
    /// 1=Rouge, 2=Jaune, 3=Vert, 4=Cyan, 5=Bleu, 6=Magenta, 7=Blanc
    /// </summary>
    public short HighlightColor { get; set; } = 4; // Cyan

    /// <summary>
    /// Épaisseur de ligne pour l'entité principale (active).
    /// Trait continu + épaisseur forte = mise en évidence maximale.
    /// (valeurs AutoCAD LineWeight : 15, 20, 25, 30, 40, 50, 70, etc.)
    /// </summary>
    public int PrimaryLineWeight { get; set; } = 50;

    /// <summary>
    /// Épaisseur de ligne pour les entités secondaires (arrière-plan).
    /// Trait pointillé + épaisseur fine = opacité simulée.
    /// </summary>
    public int SecondaryLineWeight { get; set; } = 20;

    /// <summary>
    /// Clone la configuration
    /// </summary>
    public HighlightConfiguration Clone()
    {
        return new HighlightConfiguration
        {
            Enabled = Enabled,
            HighlightColor = HighlightColor,
            PrimaryLineWeight = PrimaryLineWeight,
            SecondaryLineWeight = SecondaryLineWeight,
        };
    }

    /// <summary>
    /// Réinitialise aux valeurs par défaut
    /// </summary>
    public static HighlightConfiguration Default() => new();
}
