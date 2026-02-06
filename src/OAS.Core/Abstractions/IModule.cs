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
/// Interface pour tous les modules Open Asphalte.
/// Un module représente une extension fonctionnelle du plugin.
/// </summary>
/// <remarks>
/// Pour créer un nouveau module :
/// 1. Créer une classe qui hérite de <see cref="ModuleBase"/>
/// 2. Implémenter les propriétés requises (Id, Name, Description)
/// 3. Ajouter les commandes avec <see cref="CommandInfoAttribute"/>
/// 4. Compiler en DLL séparée et placer dans le dossier Modules/
/// </remarks>
public interface IModule : IDisposable
{
    #region Identification

    /// <summary>
    /// Identifiant unique du module (en minuscules, sans espaces)
    /// Exemple: "voirie", "dessin", "topographie"
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Nom affiché du module dans les menus et rubans
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description complète du module
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Version du module (format semver: "1.0.0")
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Auteur(s) du module
    /// </summary>
    string Author { get; }

    #endregion

    #region Affichage UI

    /// <summary>
    /// Ordre d'affichage dans les menus et rubans (plus petit = plus haut)
    /// Modules système : 900+
    /// Modules utilisateur : 1-899
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Chemin de l'icône du module (32x32 pour ruban)
    /// Format: "pack://application:,,,/Assembly;component/Resources/icon.png"
    /// </summary>
    string? IconPath { get; }

    /// <summary>
    /// Clé de traduction pour le nom du module
    /// </summary>
    string? NameKey { get; }

    #endregion

    #region Dépendances

    /// <summary>
    /// Liste des IDs de modules requis pour ce module
    /// Le module ne sera pas chargé si une dépendance est manquante
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Version minimale du Core requise (format semver)
    /// </summary>
    string MinCoreVersion { get; }

    /// <summary>
    /// Version maximale du Core supportée (format semver)
    /// Null si aucune limite haute (compatible avec toutes les versions futures)
    /// </summary>
    string? MaxCoreVersion { get; }

    /// <summary>
    /// Liste des contributeurs ayant participé au développement du module.
    /// </summary>
    IEnumerable<Contributor> Contributors { get; }


    #endregion

    #region Cycle de vie

    /// <summary>
    /// Appelé lors du chargement du module
    /// Permet d'initialiser les ressources
    /// </summary>
    void Initialize();

    /// <summary>
    /// Appelé lors de la fermeture du plugin
    /// Permet de libérer les ressources
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Indique si le module est correctement initialisé
    /// </summary>
    bool IsInitialized { get; }

    #endregion

    #region Commandes et Traductions

    /// <summary>
    /// Retourne tous les types contenant des commandes [CommandMethod]
    /// </summary>
    IEnumerable<Type> GetCommandTypes();

    /// <summary>
    /// Retourne les traductions spécifiques au module
    /// Structure: { "fr": { "key": "value" }, "en": { "key": "value" } }
    /// </summary>
    IDictionary<string, IDictionary<string, string>> GetTranslations();

    #endregion
}
