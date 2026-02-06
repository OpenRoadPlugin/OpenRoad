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

using Autodesk.AutoCAD.DatabaseServices;
using OpenAsphalte.Discovery;

namespace OpenAsphalte.Modules.DynamicSnap.Services;

/// <summary>
/// Helper statique pour la surbrillance d'entités depuis d'autres modules.
/// Fournit une API simple avec fallback silencieux si DynamicSnap n'est pas installé.
///
/// Utilisation recommandée dans les autres modules OAS :
/// <code>
/// // 1. Mettre en surbrillance les entités sélectionnées
/// HighlightHelper.HighlightEntities(polyline1Id, polyline2Id);
///
/// // 2. Lors d'une action sur une entité, la définir comme principale
/// HighlightHelper.SetPrimaryEntity(polyline1Id);
///
/// // 3. Nettoyer la surbrillance en fin de commande
/// HighlightHelper.ClearHighlight();
/// </code>
///
/// Toutes les méthodes sont des no-op silencieux si :
/// - Le module DynamicSnap n'est pas installé
/// - La surbrillance est désactivée dans les paramètres
/// </summary>
public static class HighlightHelper
{
    /// <summary>
    /// Cache pour vérifier si le module est disponible
    /// </summary>
    private static bool? _isModuleAvailable;

    /// <summary>
    /// Vérifie si le module DynamicSnap est installé, disponible, et que la surbrillance est activée
    /// </summary>
    public static bool IsAvailable
    {
        get
        {
            if (!_isModuleAvailable.HasValue)
            {
                _isModuleAvailable = CheckModuleAvailable();
            }
            return _isModuleAvailable.Value
                && DynamicSnapService.IsAvailable
                && EntityHighlightService.IsEnabled;
        }
    }

    /// <summary>
    /// Force la revérification de la disponibilité du module
    /// </summary>
    public static void RefreshAvailability()
    {
        _isModuleAvailable = null;
    }

    /// <summary>
    /// Met en surbrillance une ou plusieurs entités.
    /// Toutes les entités reçoivent le style Primary (trait continu, épaisseur forte).
    /// No-op si DynamicSnap non disponible ou surbrillance désactivée.
    /// </summary>
    /// <param name="entityIds">ObjectIds des entités à mettre en surbrillance</param>
    public static void HighlightEntities(params ObjectId[] entityIds)
    {
        if (!IsAvailable) return;

        try
        {
            EntityHighlightService.HighlightEntities(entityIds);
        }
        catch
        {
            // Silencieux — la surbrillance est cosmétique, pas critique
        }
    }

    /// <summary>
    /// Définit une entité comme "principale" (active).
    /// L'entité principale garde le trait continu épais.
    /// Les autres passent en trait pointillé fin (opacité simulée).
    /// No-op si DynamicSnap non disponible ou surbrillance désactivée.
    /// </summary>
    /// <param name="primaryId">ObjectId de l'entité principale</param>
    public static void SetPrimaryEntity(ObjectId primaryId)
    {
        if (!IsAvailable) return;

        try
        {
            EntityHighlightService.SetPrimaryEntity(primaryId);
        }
        catch
        {
            // Silencieux
        }
    }

    /// <summary>
    /// Supprime la surbrillance de toutes les entités.
    /// No-op si DynamicSnap non disponible.
    /// </summary>
    public static void ClearHighlight()
    {
        // On tente le clear même si IsAvailable est false,
        // pour garantir le nettoyage dans les blocs finally
        try
        {
            EntityHighlightService.ClearHighlight();
        }
        catch
        {
            // Silencieux
        }
    }

    /// <summary>
    /// Supprime la surbrillance d'une entité spécifique.
    /// No-op si DynamicSnap non disponible.
    /// </summary>
    /// <param name="entityId">ObjectId de l'entité</param>
    public static void ClearHighlight(ObjectId entityId)
    {
        try
        {
            EntityHighlightService.ClearHighlight(entityId);
        }
        catch
        {
            // Silencieux
        }
    }

    /// <summary>
    /// Vérifie si le module DynamicSnap est chargé via ModuleDiscovery
    /// </summary>
    private static bool CheckModuleAvailable()
    {
        try
        {
            var module = ModuleDiscovery.GetModule("dynamicsnap");
            return module != null && module.IsInitialized;
        }
        catch
        {
            return false;
        }
    }
}
