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

using OpenAsphalte.Abstractions;
using OpenAsphalte.Modules.DynamicSnap.Services;
using OpenAsphalte.Logging;

namespace OpenAsphalte.Modules.DynamicSnap;

/// <summary>
/// Module d'accrochage dynamique intelligent pour Open Asphalte.
/// Fournit un système de snap visuel avancé pour les autres modules OAS.
///
/// Ce module n'a pas de commandes utilisateur directes - il fournit une API
/// que les autres modules peuvent utiliser pour améliorer leur interaction.
///
/// Utilisation dans un autre module:
/// <code>
/// // Vérifier si le module est disponible
/// var snapModule = ModuleDiscovery.GetModule&lt;DynamicSnapModule&gt;();
/// if (snapModule != null &amp;&amp; DynamicSnapService.IsAvailable)
/// {
///     // Utiliser l'accrochage OAS
///     var snapPoint = DynamicSnapService.GetVertexOnPolyline(polyline, "Sélectionnez un sommet:");
/// }
/// else
/// {
///     // Fallback vers l'accrochage AutoCAD classique
///     var point = Editor.GetPoint(...);
/// }
/// </code>
/// </summary>
public class DynamicSnapModule : ModuleBase
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICATION DU MODULE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Identifiant unique du module
    /// </summary>
    public override string Id => "dynamicsnap";

    /// <summary>
    /// Nom affiché (non visible car ShowInMenu/Ribbon = false)
    /// </summary>
    public override string Name => "Accrochage Dynamique OAS";

    /// <summary>
    /// Description du module
    /// </summary>
    public override string Description =>
        "Système d'accrochage objet intelligent avec marqueurs visuels pour les modules Open Asphalte";

    /// <summary>
    /// Contributeurs du module
    /// </summary>
    public override IEnumerable<Contributor> Contributors => new[]
    {
        new Contributor("Charles TILLY", "Lead Developer", "https://linkedin.com/in/charlestilly"),
        new Contributor("IA Copilot", "Code Assistant")
    };

    /// <summary>
    /// Version du module
    /// </summary>
    public override string Version => "0.0.1";

    /// <summary>
    /// Auteur du module
    /// </summary>
    public override string Author => "Charles TILLY";

    // ═══════════════════════════════════════════════════════════
    // CONFIGURATION AFFICHAGE UI
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Ordre d'affichage (priorité basse car module utilitaire)
    /// Ce module doit être chargé en premier pour être disponible aux autres
    /// </summary>
    public override int Order => 1;

    /// <summary>
    /// Clé de traduction pour le nom
    /// </summary>
    public override string? NameKey => "dynamicsnap.name";

    /// <summary>
    /// Version minimale du Core requise
    /// </summary>
    public override string MinCoreVersion => "0.0.1";

    // ═══════════════════════════════════════════════════════════
    // COMMANDES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Ce module ne fournit pas de commandes utilisateur directes.
    /// Il s'agit d'un module de service utilisé par d'autres modules.
    /// </summary>
    public override IEnumerable<Type> GetCommandTypes()
    {
        // Aucune commande exposée - ce module est une API de service
        return Array.Empty<Type>();
    }

    // ═══════════════════════════════════════════════════════════
    // CYCLE DE VIE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Initialisation du module - active le service DynamicSnap
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        // Initialiser le service de snap
        DynamicSnapService.Initialize();

        Logger.Debug(T("dynamicsnap.loaded"));
    }

    /// <summary>
    /// Arrêt du module
    /// </summary>
    public override void Shutdown()
    {
        DynamicSnapService.Shutdown();
        base.Shutdown();
    }

    // ═══════════════════════════════════════════════════════════
    // TRADUCTIONS (FR, EN, ES)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Retourne les traductions spécifiques au module
    /// </summary>
    public override IDictionary<string, IDictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, IDictionary<string, string>>
        {
            ["fr"] = new Dictionary<string, string>
            {
                // Module
                ["dynamicsnap.name"] = "Accrochage Dynamique OAS",
                ["dynamicsnap.loaded"] = "[DynamicSnap] Module d'accrochage dynamique chargé",

                // Service
                ["dynamicsnap.init"] = "[DynamicSnap] Service initialisé",
                ["dynamicsnap.shutdown"] = "[DynamicSnap] Service arrêté",
                ["dynamicsnap.noentity"] = "Aucune entité fournie pour l'accrochage",
                ["dynamicsnap.invalidid"] = "ObjectId invalide",
                ["dynamicsnap.nosnap"] = "Aucun point d'accrochage trouvé. Cliquez plus près d'un point.",

                // Modes d'accrochage
                ["dynamicsnap.mode.vertex"] = "Sommet",
                ["dynamicsnap.mode.endpoint"] = "Extrémité",
                ["dynamicsnap.mode.midpoint"] = "Milieu",
                ["dynamicsnap.mode.nearest"] = "Proche",
                ["dynamicsnap.mode.center"] = "Centre",
                ["dynamicsnap.mode.intersection"] = "Intersection",
                ["dynamicsnap.mode.perpendicular"] = "Perpendiculaire",
                ["dynamicsnap.mode.tangent"] = "Tangent",
                ["dynamicsnap.mode.quadrant"] = "Quadrant",
                ["dynamicsnap.mode.insertion"] = "Insertion",
                ["dynamicsnap.mode.node"] = "Nœud",
                ["dynamicsnap.mode.parallel"] = "Parallèle",

                // Messages utilisateur
                ["dynamicsnap.select.vertex"] = "Sélectionnez un sommet",
                ["dynamicsnap.select.point"] = "Sélectionnez un point d'accrochage",
                ["dynamicsnap.cancelled"] = "Sélection annulée",
            },

            ["en"] = new Dictionary<string, string>
            {
                // Module
                ["dynamicsnap.name"] = "OAS Dynamic Snap",
                ["dynamicsnap.loaded"] = "[DynamicSnap] Dynamic snap module loaded",

                // Service
                ["dynamicsnap.init"] = "[DynamicSnap] Service initialized",
                ["dynamicsnap.shutdown"] = "[DynamicSnap] Service stopped",
                ["dynamicsnap.noentity"] = "No entity provided for snapping",
                ["dynamicsnap.invalidid"] = "Invalid ObjectId",
                ["dynamicsnap.nosnap"] = "No snap point found. Click closer to a point.",

                // Snap modes
                ["dynamicsnap.mode.vertex"] = "Vertex",
                ["dynamicsnap.mode.endpoint"] = "Endpoint",
                ["dynamicsnap.mode.midpoint"] = "Midpoint",
                ["dynamicsnap.mode.nearest"] = "Nearest",
                ["dynamicsnap.mode.center"] = "Center",
                ["dynamicsnap.mode.intersection"] = "Intersection",
                ["dynamicsnap.mode.perpendicular"] = "Perpendicular",
                ["dynamicsnap.mode.tangent"] = "Tangent",
                ["dynamicsnap.mode.quadrant"] = "Quadrant",
                ["dynamicsnap.mode.insertion"] = "Insertion",
                ["dynamicsnap.mode.node"] = "Node",
                ["dynamicsnap.mode.parallel"] = "Parallel",

                // User messages
                ["dynamicsnap.select.vertex"] = "Select a vertex",
                ["dynamicsnap.select.point"] = "Select a snap point",
                ["dynamicsnap.cancelled"] = "Selection cancelled",
            },

            ["es"] = new Dictionary<string, string>
            {
                // Módulo
                ["dynamicsnap.name"] = "Enganche Dinámico OAS",
                ["dynamicsnap.loaded"] = "[DynamicSnap] Módulo de enganche dinámico cargado",

                // Servicio
                ["dynamicsnap.init"] = "[DynamicSnap] Servicio inicializado",
                ["dynamicsnap.shutdown"] = "[DynamicSnap] Servicio detenido",
                ["dynamicsnap.noentity"] = "No se proporcionó ninguna entidad para el enganche",
                ["dynamicsnap.invalidid"] = "ObjectId inválido",
                ["dynamicsnap.nosnap"] = "No se encontró punto de enganche. Haga clic más cerca de un punto.",

                // Modos de enganche
                ["dynamicsnap.mode.vertex"] = "Vértice",
                ["dynamicsnap.mode.endpoint"] = "Extremo",
                ["dynamicsnap.mode.midpoint"] = "Punto medio",
                ["dynamicsnap.mode.nearest"] = "Cercano",
                ["dynamicsnap.mode.center"] = "Centro",
                ["dynamicsnap.mode.intersection"] = "Intersección",
                ["dynamicsnap.mode.perpendicular"] = "Perpendicular",
                ["dynamicsnap.mode.tangent"] = "Tangente",
                ["dynamicsnap.mode.quadrant"] = "Cuadrante",
                ["dynamicsnap.mode.insertion"] = "Inserción",
                ["dynamicsnap.mode.node"] = "Nodo",
                ["dynamicsnap.mode.parallel"] = "Paralelo",

                // Mensajes de usuario
                ["dynamicsnap.select.vertex"] = "Seleccione un vértice",
                ["dynamicsnap.select.point"] = "Seleccione un punto de enganche",
                ["dynamicsnap.cancelled"] = "Selección cancelada",
            },
        };
    }

    // ═══════════════════════════════════════════════════════════
    // HELPER METHOD (pour usage interne)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Raccourci pour les traductions
    /// </summary>
    private static string T(string key, string? defaultValue = null)
    {
        return Localization.Localization.T(key, defaultValue ?? key);
    }
}
