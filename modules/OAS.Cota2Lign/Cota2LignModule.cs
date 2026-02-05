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
using OpenAsphalte.Modules.Cota2Lign.Commands;

namespace OpenAsphalte.Modules.Cota2Lign;

/// <summary>
/// Module de cotation entre deux polylignes pour Open Asphalte.
/// Permet de créer automatiquement des cotations alignées entre deux polylignes
/// avec différentes options (interdistance, cotation aux sommets, etc.).
/// </summary>
public class Cota2LignModule : ModuleBase
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICATION DU MODULE
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Identifiant unique du module
    /// </summary>
    public override string Id => "cota2lign";

    /// <summary>
    /// Nom affiché dans les menus et rubans
    /// </summary>
    public override string Name => "Cotation entre deux lignes";

    /// <summary>
    /// Description du module
    /// </summary>
    public override string Description => "Crée des cotations alignées entre deux polylignes";

    /// <summary>
    /// Contributeurs du module
    /// </summary>
    public override IEnumerable<Contributor> Contributors =>
    [
        new Contributor("Charles TILLY", "Lead Developer", "https://linkedin.com/in/charlestilly"),
        new Contributor("IA Copilot", "Code Assistant")
    ];

    /// <summary>
    /// Version du module
    /// </summary>
    public override string Version => "0.0.1";

    /// <summary>
    /// Auteur du module
    /// </summary>
    public override string Author => "Charles TILLY";

    // ═══════════════════════════════════════════════════════════
    // AFFICHAGE UI
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Ordre d'affichage (modules officiels entre 10-50)
    /// </summary>
    public override int Order => 20;

    /// <summary>
    /// Clé de traduction pour le nom
    /// </summary>
    public override string? NameKey => "cota2lign.name";

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
        return
        [
            typeof(Cota2LignCommand),
        ];
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
                // Menus / Catégories
                ["menu.dessin"] = "Dessin",
                ["menu.cotations"] = "Cotations",

                // Module
                ["cota2lign.name"] = "Cotation entre deux lignes",

                // Commande principale
                ["cota2lign.cmd.title"] = "Cotation entre 2 lignes",
                ["cota2lign.cmd.desc"] = "Crée des cotations alignées entre deux polylignes",

                // Messages console
                ["cota2lign.select.pl1"] = "Sélectionnez la polyligne de référence (n°1)",
                ["cota2lign.select.pl2"] = "Sélectionnez la polyligne cible (n°2)",
                ["cota2lign.select.start"] = "Point de départ sur la polyligne 1",
                ["cota2lign.select.end"] = "Point d'arrivée sur la polyligne 1",
                ["cota2lign.error.notpolyline"] = "L'entité sélectionnée n'est pas une polyligne",
                ["cota2lign.error.sameentity"] = "Veuillez sélectionner une polyligne différente",
                ["cota2lign.cancelled"] = "Opération annulée",

                // Options de commande
                ["cota2lign.option.params"] = "Paramètres",
                ["cota2lign.option.interdist"] = "Interdistance",
                ["cota2lign.option.vertices"] = "Sommets",
                ["cota2lign.option.reverse"] = "Inverser",
                ["cota2lign.prompt.interdist"] = "Interdistance entre cotations (0 = désactivé)",
                ["cota2lign.prompt.vertices"] = "Coter aux sommets",
                ["cota2lign.prompt.reverse"] = "Inverser le côté de décalage",

                // Résultats
                ["cota2lign.success"] = "{0} cotation(s) créée(s)",
                ["cota2lign.nostations"] = "Aucune station de cotation générée",
                ["cota2lign.preview"] = "Prévisualisation de {0} cotation(s) - Appuyez sur Entrée pour valider",

                // Fenêtre paramètres
                ["cota2lign.settings.title"] = "Paramètres - Cotation entre deux lignes",
                ["cota2lign.settings.header"] = "Paramètres de cotation",
                ["cota2lign.settings.interdist"] = "Interdistance (m) :",
                ["cota2lign.settings.interdist.tooltip"] = "Distance entre chaque cotation (0 pour désactiver)",
                ["cota2lign.settings.vertices"] = "Coter aux sommets",
                ["cota2lign.settings.vertices.tooltip"] = "Ajouter une cotation à chaque sommet de la polyligne",
                ["cota2lign.settings.offset"] = "Décalage des cotations (m) :",
                ["cota2lign.settings.offset.tooltip"] = "Distance de décalage du texte de cotation",
                ["cota2lign.settings.layer"] = "Calque de destination :",
                ["cota2lign.settings.layer.tooltip"] = "Calque sur lequel créer les cotations (vide = calque courant)",
                ["cota2lign.settings.layer.current"] = "(Calque courant)",
                ["cota2lign.settings.reverse"] = "Inverser le côté",
                ["cota2lign.settings.reverse.tooltip"] = "Place les cotations de l'autre côté de la polyligne",
                ["cota2lign.settings.apply"] = "Appliquer",
                ["cota2lign.settings.cancel"] = "Annuler",
                ["cota2lign.settings.reset"] = "Réinitialiser",
                ["cota2lign.settings.saved"] = "Paramètres enregistrés dans le dessin",

                // Intégration DynamicSnap
                ["cota2lign.snap.dynamicsnap"] = "[Cota2Lign] Module DynamicSnap détecté - accrochage intelligent activé",
                ["cota2lign.snap.fallback"] = "[Cota2Lign] Module DynamicSnap non disponible - accrochage AutoCAD utilisé",

                // Paramètres d'accrochage OAS
                ["cota2lign.settings.useoassnap"] = "Utiliser l'accrochage OAS",
                ["cota2lign.settings.useoassnap.tooltip"] = "Utiliser le module DynamicSnap pour l'accrochage intelligent",
                ["cota2lign.settings.oassnap.unavailable"] = "Module DynamicSnap non installé - fonctionnalité désactivée",
                ["cota2lign.settings.reset.tooltip"] = "Réinitialiser tous les paramètres aux valeurs par défaut",

                // Validation
                ["cota2lign.validation.error"] = "Erreur de validation",
                ["cota2lign.validation.invalidInterdist"] = "Veuillez saisir une valeur numérique valide pour l'interdistance.",
                ["cota2lign.validation.invalidOffset"] = "Veuillez saisir une valeur numérique valide pour le décalage.",
            },

            // ═══════════════════════════════════════════════════════
            // ENGLISH
            // ═══════════════════════════════════════════════════════
            ["en"] = new Dictionary<string, string>
            {
                // Menus / Categories
                ["menu.dessin"] = "Drawing",
                ["menu.cotations"] = "Dimensions",

                // Module
                ["cota2lign.name"] = "Dimension between two lines",

                // Main command
                ["cota2lign.cmd.title"] = "Dimension between 2 lines",
                ["cota2lign.cmd.desc"] = "Creates aligned dimensions between two polylines",

                // Console messages
                ["cota2lign.select.pl1"] = "Select reference polyline (#1)",
                ["cota2lign.select.pl2"] = "Select target polyline (#2)",
                ["cota2lign.select.start"] = "Start point on polyline 1",
                ["cota2lign.select.end"] = "End point on polyline 1",
                ["cota2lign.error.notpolyline"] = "Selected entity is not a polyline",
                ["cota2lign.error.sameentity"] = "Please select a different polyline",
                ["cota2lign.cancelled"] = "Operation cancelled",

                // Command options
                ["cota2lign.option.params"] = "Parameters",
                ["cota2lign.option.interdist"] = "Spacing",
                ["cota2lign.option.vertices"] = "Vertices",
                ["cota2lign.option.reverse"] = "Reverse",
                ["cota2lign.prompt.interdist"] = "Spacing between dimensions (0 = disabled)",
                ["cota2lign.prompt.vertices"] = "Dimension at vertices",
                ["cota2lign.prompt.reverse"] = "Reverse offset side",

                // Results
                ["cota2lign.success"] = "{0} dimension(s) created",
                ["cota2lign.nostations"] = "No dimension stations generated",
                ["cota2lign.preview"] = "Preview of {0} dimension(s) - Press Enter to confirm",

                // Settings window
                ["cota2lign.settings.title"] = "Settings - Dimension between two lines",
                ["cota2lign.settings.header"] = "Dimension settings",
                ["cota2lign.settings.interdist"] = "Spacing (m):",
                ["cota2lign.settings.interdist.tooltip"] = "Distance between each dimension (0 to disable)",
                ["cota2lign.settings.vertices"] = "Dimension at vertices",
                ["cota2lign.settings.vertices.tooltip"] = "Add a dimension at each polyline vertex",
                ["cota2lign.settings.offset"] = "Dimension offset (m):",
                ["cota2lign.settings.offset.tooltip"] = "Offset distance for dimension text",
                ["cota2lign.settings.layer"] = "Destination layer:",
                ["cota2lign.settings.layer.tooltip"] = "Layer for dimensions (empty = current layer)",
                ["cota2lign.settings.layer.current"] = "(Current layer)",
                ["cota2lign.settings.reverse"] = "Reverse side",
                ["cota2lign.settings.reverse.tooltip"] = "Place dimensions on the other side of the polyline",
                ["cota2lign.settings.apply"] = "Apply",
                ["cota2lign.settings.cancel"] = "Cancel",
                ["cota2lign.settings.reset"] = "Reset",
                ["cota2lign.settings.saved"] = "Settings saved to drawing",

                // DynamicSnap integration
                ["cota2lign.snap.dynamicsnap"] = "[Cota2Lign] DynamicSnap module detected - smart snapping enabled",
                ["cota2lign.snap.fallback"] = "[Cota2Lign] DynamicSnap module not available - using AutoCAD OSNAP",

                // OAS snap settings
                ["cota2lign.settings.useoassnap"] = "Use OAS snapping",
                ["cota2lign.settings.useoassnap.tooltip"] = "Use DynamicSnap module for smart snapping",
                ["cota2lign.settings.oassnap.unavailable"] = "DynamicSnap module not installed - feature disabled",
                ["cota2lign.settings.reset.tooltip"] = "Reset all settings to default values",

                // Validation
                ["cota2lign.validation.error"] = "Validation error",
                ["cota2lign.validation.invalidInterdist"] = "Please enter a valid numeric value for spacing.",
                ["cota2lign.validation.invalidOffset"] = "Please enter a valid numeric value for offset.",
            },

            // ═══════════════════════════════════════════════════════
            // ESPAÑOL
            // ═══════════════════════════════════════════════════════
            ["es"] = new Dictionary<string, string>
            {
                // Menus / Categorías
                ["menu.dessin"] = "Dibujo",
                ["menu.cotations"] = "Cotas",

                // Módulo
                ["cota2lign.name"] = "Cota entre dos líneas",

                // Comando principal
                ["cota2lign.cmd.title"] = "Cota entre 2 líneas",
                ["cota2lign.cmd.desc"] = "Crea cotas alineadas entre dos polilíneas",

                // Mensajes consola
                ["cota2lign.select.pl1"] = "Seleccione la polilínea de referencia (n°1)",
                ["cota2lign.select.pl2"] = "Seleccione la polilínea destino (n°2)",
                ["cota2lign.select.start"] = "Punto inicial en la polilínea 1",
                ["cota2lign.select.end"] = "Punto final en la polilínea 1",
                ["cota2lign.error.notpolyline"] = "La entidad seleccionada no es una polilínea",
                ["cota2lign.error.sameentity"] = "Por favor seleccione una polilínea diferente",
                ["cota2lign.cancelled"] = "Operación cancelada",

                // Opciones de comando
                ["cota2lign.option.params"] = "Parámetros",
                ["cota2lign.option.interdist"] = "Espaciado",
                ["cota2lign.option.vertices"] = "Vértices",
                ["cota2lign.option.reverse"] = "Invertir",
                ["cota2lign.prompt.interdist"] = "Espaciado entre cotas (0 = desactivado)",
                ["cota2lign.prompt.vertices"] = "Cotar en vértices",
                ["cota2lign.prompt.reverse"] = "Invertir lado de desplazamiento",

                // Resultados
                ["cota2lign.success"] = "{0} cota(s) creada(s)",
                ["cota2lign.nostations"] = "No se generaron estaciones de cota",
                ["cota2lign.preview"] = "Vista previa de {0} cota(s) - Presione Enter para confirmar",

                // Ventana parámetros
                ["cota2lign.settings.title"] = "Parámetros - Cota entre dos líneas",
                ["cota2lign.settings.header"] = "Parámetros de cota",
                ["cota2lign.settings.interdist"] = "Espaciado (m):",
                ["cota2lign.settings.interdist.tooltip"] = "Distancia entre cada cota (0 para desactivar)",
                ["cota2lign.settings.vertices"] = "Cotar en vértices",
                ["cota2lign.settings.vertices.tooltip"] = "Agregar una cota en cada vértice de la polilínea",
                ["cota2lign.settings.offset"] = "Desplazamiento de cotas (m):",
                ["cota2lign.settings.offset.tooltip"] = "Distancia de desplazamiento del texto de cota",
                ["cota2lign.settings.layer"] = "Capa de destino:",
                ["cota2lign.settings.layer.tooltip"] = "Capa para las cotas (vacío = capa actual)",
                ["cota2lign.settings.layer.current"] = "(Capa actual)",
                ["cota2lign.settings.reverse"] = "Invertir lado",
                ["cota2lign.settings.reverse.tooltip"] = "Colocar cotas del otro lado de la polilínea",
                ["cota2lign.settings.apply"] = "Aplicar",
                ["cota2lign.settings.cancel"] = "Cancelar",
                ["cota2lign.settings.reset"] = "Restablecer",
                ["cota2lign.settings.saved"] = "Parámetros guardados en el dibujo",

                // Integración DynamicSnap
                ["cota2lign.snap.dynamicsnap"] = "[Cota2Lign] Módulo DynamicSnap detectado - enganche inteligente activado",
                ["cota2lign.snap.fallback"] = "[Cota2Lign] Módulo DynamicSnap no disponible - usando OSNAP de AutoCAD",

                // Parámetros de enganche OAS
                ["cota2lign.settings.useoassnap"] = "Usar enganche OAS",
                ["cota2lign.settings.useoassnap.tooltip"] = "Usar el módulo DynamicSnap para enganche inteligente",
                ["cota2lign.settings.oassnap.unavailable"] = "Módulo DynamicSnap no instalado - función desactivada",
                ["cota2lign.settings.reset.tooltip"] = "Restablecer todos los parámetros a los valores predeterminados",

                // Validación
                ["cota2lign.validation.error"] = "Error de validación",
                ["cota2lign.validation.invalidInterdist"] = "Por favor ingrese un valor numérico válido para el espaciado.",
                ["cota2lign.validation.invalidOffset"] = "Por favor ingrese un valor numérico válido para el desplazamiento.",
            }
        };
    }
}
