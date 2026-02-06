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

namespace OpenAsphalte.Modules.PrezOrganizer.Models;

/// <summary>
/// Représente une présentation (layout) AutoCAD dans la liste de l'organiseur.
/// Stocke l'état original et les modifications en attente.
/// </summary>
public class LayoutItem
{
    /// <summary>
    /// Nom de la présentation au moment du chargement depuis AutoCAD.
    /// </summary>
    public string OriginalName { get; }

    /// <summary>
    /// Nom actuel (potentiellement modifié par l'utilisateur).
    /// </summary>
    public string CurrentName { get; set; }

    /// <summary>
    /// Position (TabOrder) originale dans AutoCAD.
    /// </summary>
    public int OriginalTabOrder { get; }

    /// <summary>
    /// Indique si cette présentation a été créée pendant la session (pas encore dans AutoCAD).
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Indique si cette présentation est marquée pour suppression.
    /// </summary>
    public bool IsMarkedForDeletion { get; set; }

    /// <summary>
    /// Indique si cette présentation est une copie d'une autre (pas encore dans AutoCAD).
    /// </summary>
    public bool IsCopy { get; set; }

    /// <summary>
    /// Nom de la présentation source en cas de copie.
    /// </summary>
    public string? CopySourceName { get; set; }

    /// <summary>
    /// Indique si cet item a été modifié par rapport à son état original.
    /// </summary>
    public bool IsModified => OriginalName != CurrentName || IsNew || IsMarkedForDeletion || IsCopy;

    /// <summary>
    /// Nom affiché dans la liste (avec indicateurs visuels).
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (IsMarkedForDeletion)
                return $"\u2716 {CurrentName}";    // ? barré
            if (IsNew || IsCopy)
                return $"\u2726 {CurrentName}";    // ? nouveau
            if (OriginalName != CurrentName)
                return $"\u270E {CurrentName}";    // ? modifié
            return CurrentName;
        }
    }

    /// <summary>
    /// Crée un LayoutItem à partir d'une présentation existante.
    /// </summary>
    /// <param name="name">Nom de la présentation</param>
    /// <param name="tabOrder">Position dans l'ordre des onglets</param>
    public LayoutItem(string name, int tabOrder)
    {
        OriginalName = name;
        CurrentName = name;
        OriginalTabOrder = tabOrder;
    }

    /// <summary>
    /// Crée un LayoutItem pour une nouvelle présentation.
    /// </summary>
    /// <param name="name">Nom de la nouvelle présentation</param>
    /// <param name="isNew">True si c'est un ajout, false si c'est une copie</param>
    /// <param name="copySource">Nom de la source si copie</param>
    public LayoutItem(string name, bool isNew, string? copySource = null)
    {
        OriginalName = name;
        CurrentName = name;
        OriginalTabOrder = -1;
        IsNew = isNew;
        IsCopy = copySource != null;
        CopySourceName = copySource;
    }

    /// <summary>
    /// Crée un clone profond de cet item (pour l'historique Undo).
    /// </summary>
    public LayoutItem Clone()
    {
        return new LayoutItem(OriginalName, OriginalTabOrder)
        {
            CurrentName = CurrentName,
            IsNew = IsNew,
            IsMarkedForDeletion = IsMarkedForDeletion,
            IsCopy = IsCopy,
            CopySourceName = CopySourceName,
        };
    }

    /// <inheritdoc/>
    public override string ToString() => DisplayName;
}
