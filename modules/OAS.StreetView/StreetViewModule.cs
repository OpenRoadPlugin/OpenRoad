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
using OpenAsphalte.Modules.StreetView.Commands;

namespace OpenAsphalte.Modules.StreetView;

/// <summary>
/// Module Street View pour Open Asphalte.
/// Permet d'ouvrir Google Street View depuis un point sélectionné dans un dessin géoréférencé.
/// </summary>
/// <remarks>
/// Ce module DÉPEND du module "setprojection" (Géoréférencement) pour :
/// - Récupérer le système de coordonnées actuel du dessin
/// - Convertir les coordonnées projetées vers WGS84 (latitude/longitude)
/// - Accéder à la fenêtre de définition de projection si nécessaire
/// </remarks>
public class StreetViewModule : ModuleBase
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICATION DU MODULE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Identifiant unique du module
    /// </summary>
    public override string Id => "streetview";

    /// <summary>
    /// Nom affiché dans les menus et rubans
    /// </summary>
    public override string Name => "Street View";

    /// <summary>
    /// Description du module
    /// </summary>
    public override string Description => "Ouvrir Google Street View depuis un dessin géoréférencé";

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
    // DÉPENDANCES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Liste des modules requis pour le fonctionnement
    /// </summary>
    /// <remarks>
    /// Le module "setprojection" est OBLIGATOIRE car Street View nécessite :
    /// - Un dessin géoréférencé (système de coordonnées défini)
    /// - Les services de conversion de coordonnées
    /// </remarks>
    public override IReadOnlyList<string> Dependencies => new[] { "setprojection" };

    // ═══════════════════════════════════════════════════════════
    // AFFICHAGE UI
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Ordre d'affichage (modules officiels entre 10-50)
    /// </summary>
    public override int Order => 11;  // Juste après Géoréférencement (10)

    /// <summary>
    /// Clé de traduction pour le nom
    /// </summary>
    public override string? NameKey => "streetview.name";

    /// <summary>
    /// Version minimale du Core requise
    /// </summary>
    public override string MinCoreVersion => "0.0.1";

    // ═══════════════════════════════════════════════════════════
    // COMMANDES
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Retourne tous les types contenant des commandes [CommandMethod]
    /// </summary>
    public override IEnumerable<Type> GetCommandTypes()
    {
        return new[]
        {
            typeof(StreetViewCommand),
        };
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
            // ═══════════════════════════════════════════════════════
            // FRANÇAIS
            // ═══════════════════════════════════════════════════════
            ["fr"] = new Dictionary<string, string>
            {
                // Module
                ["streetview.name"] = "Street View",

                // Commande principale
                ["streetview.cmd.title"] = "Street View",
                ["streetview.cmd.desc"] = "Ouvrir Google Street View depuis un point du dessin",

                // Menus
                ["menu.carto"] = "Cartographie",

                // Messages de sélection
                ["streetview.select.point"] = "Sélectionnez le point de vue (position de l'observateur)",
                ["streetview.select.direction"] = "Sélectionnez un point pour la direction du regard",

                // Messages d'état
                ["streetview.converting"] = "Conversion des coordonnées vers WGS84...",
                ["streetview.opening"] = "Ouverture de Google Street View...",
                ["streetview.success"] = "Street View ouvert dans le navigateur",

                // Messages d'erreur
                ["streetview.error.noprojection"] = "Aucun système de coordonnées défini dans le dessin",
                ["streetview.error.noprojection.detail"] = "Veuillez définir une projection avec la commande OAS_GEOREF_SETPROJECTION",
                ["streetview.error.conversion"] = "Impossible de convertir les coordonnées vers WGS84",
                ["streetview.error.browser"] = "Impossible d'ouvrir le navigateur",
                ["streetview.error.unknownprojection"] = "Projection '{0}' non reconnue pour la conversion",

                // Coordonnées
                ["streetview.coords.local"] = "Coordonnées locales: X={0:F2}, Y={1:F2}",
                ["streetview.coords.wgs84"] = "Coordonnées WGS84: Lat={0:F6}°, Lon={1:F6}°",
                ["streetview.heading"] = "Direction: {0:F1}°",

                // Question pour définir projection
                ["streetview.askprojection"] = "Voulez-vous définir une projection maintenant ?",
                ["streetview.projection.applied"] = "Projection {0} appliquée",
                ["streetview.projection.error"] = "Impossible d'appliquer la projection",
                ["streetview.projection.windowerror"] = "Erreur ouverture fenêtre projection",
                ["common.yes"] = "Oui",
                ["common.no"] = "Non",
            },

            // ═══════════════════════════════════════════════════════
            // ANGLAIS
            // ═══════════════════════════════════════════════════════
            ["en"] = new Dictionary<string, string>
            {
                // Module
                ["streetview.name"] = "Street View",

                // Commande principale
                ["streetview.cmd.title"] = "Street View",
                ["streetview.cmd.desc"] = "Open Google Street View from a drawing point",

                // Menus
                ["menu.carto"] = "Mapping",

                // Messages de sélection
                ["streetview.select.point"] = "Select viewpoint (observer position)",
                ["streetview.select.direction"] = "Select a point for viewing direction",

                // Messages d'état
                ["streetview.converting"] = "Converting coordinates to WGS84...",
                ["streetview.opening"] = "Opening Google Street View...",
                ["streetview.success"] = "Street View opened in browser",

                // Messages d'erreur
                ["streetview.error.noprojection"] = "No coordinate system defined in the drawing",
                ["streetview.error.noprojection.detail"] = "Please define a projection with OAS_GEOREF_SETPROJECTION command",
                ["streetview.error.conversion"] = "Unable to convert coordinates to WGS84",
                ["streetview.error.browser"] = "Unable to open browser",
                ["streetview.error.unknownprojection"] = "Projection '{0}' not recognized for conversion",

                // Coordonnées
                ["streetview.coords.local"] = "Local coordinates: X={0:F2}, Y={1:F2}",
                ["streetview.coords.wgs84"] = "WGS84 coordinates: Lat={0:F6}°, Lon={1:F6}°",
                ["streetview.heading"] = "Heading: {0:F1}°",

                // Question pour définir projection
                ["streetview.askprojection"] = "Do you want to define a projection now?",
                ["streetview.projection.applied"] = "Projection {0} applied",
                ["streetview.projection.error"] = "Unable to apply projection",
                ["streetview.projection.windowerror"] = "Error opening projection window",
                ["common.yes"] = "Yes",
                ["common.no"] = "No",
            },

            // ═══════════════════════════════════════════════════════
            // ESPAGNOL
            // ═══════════════════════════════════════════════════════
            ["es"] = new Dictionary<string, string>
            {
                // Module
                ["streetview.name"] = "Street View",

                // Commande principale
                ["streetview.cmd.title"] = "Street View",
                ["streetview.cmd.desc"] = "Abrir Google Street View desde un punto del dibujo",

                // Menus
                ["menu.carto"] = "Cartografía",

                // Messages de sélection
                ["streetview.select.point"] = "Seleccione el punto de vista (posición del observador)",
                ["streetview.select.direction"] = "Seleccione un punto para la dirección de la mirada",

                // Messages d'état
                ["streetview.converting"] = "Convirtiendo coordenadas a WGS84...",
                ["streetview.opening"] = "Abriendo Google Street View...",
                ["streetview.success"] = "Street View abierto en el navegador",

                // Messages d'erreur
                ["streetview.error.noprojection"] = "No hay sistema de coordenadas definido en el dibujo",
                ["streetview.error.noprojection.detail"] = "Por favor defina una proyección con el comando OAS_GEOREF_SETPROJECTION",
                ["streetview.error.conversion"] = "No se pueden convertir las coordenadas a WGS84",
                ["streetview.error.browser"] = "No se puede abrir el navegador",
                ["streetview.error.unknownprojection"] = "Proyección '{0}' no reconocida para la conversión",

                // Coordonnées
                ["streetview.coords.local"] = "Coordenadas locales: X={0:F2}, Y={1:F2}",
                ["streetview.coords.wgs84"] = "Coordenadas WGS84: Lat={0:F6}°, Lon={1:F6}°",
                ["streetview.heading"] = "Dirección: {0:F1}°",

                // Question pour définir projection
                ["streetview.askprojection"] = "¿Desea definir una proyección ahora?",
                ["streetview.projection.applied"] = "Proyección {0} aplicada",
                ["streetview.projection.error"] = "No se puede aplicar la proyección",
                ["streetview.projection.windowerror"] = "Error al abrir la ventana de proyección",
                ["common.yes"] = "Sí",
                ["common.no"] = "No",
            },
        };
    }
}
