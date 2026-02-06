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
