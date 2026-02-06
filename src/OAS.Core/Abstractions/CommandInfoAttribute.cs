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

namespace OpenAsphalte.Abstractions;

/// <summary>
/// Métadonnées d'une commande pour l'affichage dans les menus et rubans.
/// Appliqué sur les méthodes marquées avec [CommandMethod].
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CommandInfoAttribute : Attribute
{
    /// <summary>
    /// Nom affiché dans les menus et rubans
    /// </summary>
    public string DisplayName { get; }
    
    /// <summary>
    /// Description/infobulle de la commande
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Clé de traduction pour le nom (si multilingue)
    /// </summary>
    public string? DisplayNameKey { get; set; }
    
    /// <summary>
    /// Clé de traduction pour la description (si multilingue)
    /// </summary>
    public string? DescriptionKey { get; set; }
    
    /// <summary>
    /// Catégorie menu niveau 1 (ex: "Cartographie"). Si null, utilise le nom du Module.
    /// </summary>
    public string? MenuCategory { get; set; }

    /// <summary>
    /// Clé de traduction pour la catégorie niveau 1.
    /// </summary>
    public string? MenuCategoryKey { get; set; }

    /// <summary>
    /// Sous-catégorie menu niveau 2 (ex: "Géoréférencement").
    /// </summary>
    public string? MenuSubCategory { get; set; }

    /// <summary>
    /// Clé de traduction pour la sous-catégorie niveau 2.
    /// </summary>
    public string? MenuSubCategoryKey { get; set; }
    
    /// <summary>
    /// Chemin de l'icône (16x16 pour menu, 32x32 pour ruban)
    /// Format: "pack://application:,,,/Assembly;component/Resources/icon.png"
    /// </summary>
    public string? IconPath { get; set; }
    
    /// <summary>
    /// Ordre d'affichage dans le menu/ruban du module
    /// </summary>
    public int Order { get; set; } = 100;
    
    /// <summary>
    /// Taille du bouton dans le ruban (Large ou Standard)
    /// </summary>
    public CommandSize RibbonSize { get; set; } = CommandSize.Standard;
    
    /// <summary>
    /// Groupe de la commande dans le ruban (pour regrouper les boutons)
    /// </summary>
    public string? Group { get; set; }
    
    /// <summary>
    /// Indique si la commande doit apparaître dans le menu
    /// </summary>
    public bool ShowInMenu { get; set; } = true;
    
    /// <summary>
    /// Indique si la commande doit apparaître dans le ruban
    /// </summary>
    public bool ShowInRibbon { get; set; } = true;

    /// <summary>
    /// Crée une nouvelle instance de CommandInfoAttribute
    /// </summary>
    /// <param name="displayName">Nom affiché de la commande</param>
    public CommandInfoAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

/// <summary>
/// Taille des boutons dans le ruban
/// </summary>
public enum CommandSize
{
    /// <summary>
    /// Bouton standard (petit)
    /// </summary>
    Standard,
    
    /// <summary>
    /// Grand bouton avec icône 32x32
    /// </summary>
    Large
}
