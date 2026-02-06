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
using OpenAsphalte.Modules.DynamicSnap.Commands;
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
        "Système d'accrochage objet intelligent avec marqueurs visuels et surbrillance d'entités pour les modules Open Asphalte";

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
    public override string Version => "0.0.2";

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
    /// Retourne la commande de paramètres d'accrochage.
    /// </summary>
    public override IEnumerable<Type> GetCommandTypes()
    {
        return [typeof(DynamicSnapSettingsCommand)];
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

                // Menu
                ["menu.dessin"] = "Dessin",

                // Commande
                ["dynamicsnap.cmd.title"] = "Accrochage OAS",
                ["dynamicsnap.cmd.desc"] = "Configure les paramètres de l'accrochage dynamique OAS",

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

                // Fenêtre paramètres
                ["dynamicsnap.settings.title"] = "Paramètres d'accrochage OAS",
                ["dynamicsnap.settings.header"] = "Paramètres d'accrochage OAS",
                ["dynamicsnap.settings.modes"] = "Modes d'accrochage actifs",
                ["dynamicsnap.settings.appearance"] = "Apparence des marqueurs",
                ["dynamicsnap.settings.vertex"] = "Sommets (Vertex)",
                ["dynamicsnap.settings.vertex.tooltip"] = "Accrochage aux sommets des polylignes et extrémités",
                ["dynamicsnap.settings.endpoint"] = "Extrémités (Endpoint)",
                ["dynamicsnap.settings.endpoint.tooltip"] = "Accrochage aux extrémités des entités",
                ["dynamicsnap.settings.midpoint"] = "Milieux de segments (Midpoint)",
                ["dynamicsnap.settings.midpoint.tooltip"] = "Accrochage aux milieux des segments",
                ["dynamicsnap.settings.nearest"] = "Point le plus proche (Nearest)",
                ["dynamicsnap.settings.nearest.tooltip"] = "Accrochage au point le plus proche sur l'entité",
                ["dynamicsnap.settings.color"] = "Couleur marqueur :",
                ["dynamicsnap.settings.color.tooltip"] = "Couleur du marqueur d'accrochage inactif",
                ["dynamicsnap.settings.activecolor"] = "Couleur actif :",
                ["dynamicsnap.settings.activecolor.tooltip"] = "Couleur du marqueur d'accrochage actif",
                ["dynamicsnap.settings.tolerance"] = "Tolérance :",
                ["dynamicsnap.settings.tolerance.tooltip"] = "Ratio de tolérance pour la détection d'accrochage",
                ["dynamicsnap.settings.markersize"] = "Taille marqueur :",
                ["dynamicsnap.settings.markersize.tooltip"] = "Taille d'affichage du marqueur d'accrochage",
                ["dynamicsnap.settings.filled"] = "Marqueurs pleins",
                ["dynamicsnap.settings.filled.tooltip"] = "Remplir les marqueurs au lieu d'afficher uniquement le contour",
                ["dynamicsnap.settings.lineweight"] = "Épaisseur trait :",
                ["dynamicsnap.settings.apply"] = "Appliquer",
                ["dynamicsnap.settings.cancel"] = "Annuler",
                ["dynamicsnap.settings.reset"] = "Réinitialiser",
                ["dynamicsnap.settings.reset.tooltip"] = "Réinitialiser tous les paramètres aux valeurs par défaut",
                ["dynamicsnap.settings.saved"] = "Paramètres d'accrochage sauvegardés",

                // Surbrillance des entités
                ["dynamicsnap.highlight.header"] = "Surbrillance des entités",
                ["dynamicsnap.highlight.enabled"] = "Activer la surbrillance",
                ["dynamicsnap.highlight.enabled.tooltip"] = "Met en surbrillance les entités sélectionnées pendant les commandes OAS",
                ["dynamicsnap.highlight.color"] = "Couleur surbrillance :",
                ["dynamicsnap.highlight.color.tooltip"] = "Couleur utilisée pour la surbrillance des entités sélectionnées",
                ["dynamicsnap.highlight.primaryweight"] = "Épaisseur principale :",
                ["dynamicsnap.highlight.primaryweight.tooltip"] = "Épaisseur de trait de l'entité active (trait continu)",
                ["dynamicsnap.highlight.secondaryweight"] = "Épaisseur secondaire :",
                ["dynamicsnap.highlight.secondaryweight.tooltip"] = "Épaisseur de trait des entités en arrière-plan (trait pointillé)",

                // Couleurs
                ["dynamicsnap.color.rouge"] = "Rouge",
                ["dynamicsnap.color.jaune"] = "Jaune",
                ["dynamicsnap.color.vert"] = "Vert",
                ["dynamicsnap.color.cyan"] = "Cyan",
                ["dynamicsnap.color.bleu"] = "Bleu",
                ["dynamicsnap.color.magenta"] = "Magenta",
                ["dynamicsnap.color.blanc"] = "Blanc",
            },

            ["en"] = new Dictionary<string, string>
            {
                // Module
                ["dynamicsnap.name"] = "OAS Dynamic Snap",
                ["dynamicsnap.loaded"] = "[DynamicSnap] Dynamic snap module loaded",

                // Menu
                ["menu.dessin"] = "Drawing",

                // Command
                ["dynamicsnap.cmd.title"] = "OAS Snap",
                ["dynamicsnap.cmd.desc"] = "Configure OAS dynamic snap settings",

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

                // Settings window
                ["dynamicsnap.settings.title"] = "OAS Snap Settings",
                ["dynamicsnap.settings.header"] = "OAS Snap Settings",
                ["dynamicsnap.settings.modes"] = "Active snap modes",
                ["dynamicsnap.settings.appearance"] = "Marker appearance",
                ["dynamicsnap.settings.vertex"] = "Vertices (Vertex)",
                ["dynamicsnap.settings.vertex.tooltip"] = "Snap to polyline vertices and endpoints",
                ["dynamicsnap.settings.endpoint"] = "Endpoints (Endpoint)",
                ["dynamicsnap.settings.endpoint.tooltip"] = "Snap to entity endpoints",
                ["dynamicsnap.settings.midpoint"] = "Segment midpoints (Midpoint)",
                ["dynamicsnap.settings.midpoint.tooltip"] = "Snap to segment midpoints",
                ["dynamicsnap.settings.nearest"] = "Nearest point (Nearest)",
                ["dynamicsnap.settings.nearest.tooltip"] = "Snap to the nearest point on entity",
                ["dynamicsnap.settings.color"] = "Marker color:",
                ["dynamicsnap.settings.color.tooltip"] = "Color of the inactive snap marker",
                ["dynamicsnap.settings.activecolor"] = "Active color:",
                ["dynamicsnap.settings.activecolor.tooltip"] = "Color of the active snap marker",
                ["dynamicsnap.settings.tolerance"] = "Tolerance:",
                ["dynamicsnap.settings.tolerance.tooltip"] = "Tolerance ratio for snap detection",
                ["dynamicsnap.settings.markersize"] = "Marker size:",
                ["dynamicsnap.settings.markersize.tooltip"] = "Display size of the snap marker",
                ["dynamicsnap.settings.filled"] = "Filled markers",
                ["dynamicsnap.settings.filled.tooltip"] = "Fill markers instead of showing outline only",
                ["dynamicsnap.settings.lineweight"] = "Line weight:",
                ["dynamicsnap.settings.apply"] = "Apply",
                ["dynamicsnap.settings.cancel"] = "Cancel",
                ["dynamicsnap.settings.reset"] = "Reset",
                ["dynamicsnap.settings.reset.tooltip"] = "Reset all settings to default values",
                ["dynamicsnap.settings.saved"] = "Snap settings saved",

                // Entity highlighting
                ["dynamicsnap.highlight.header"] = "Entity highlighting",
                ["dynamicsnap.highlight.enabled"] = "Enable highlighting",
                ["dynamicsnap.highlight.enabled.tooltip"] = "Highlights selected entities during OAS commands",
                ["dynamicsnap.highlight.color"] = "Highlight color:",
                ["dynamicsnap.highlight.color.tooltip"] = "Color used to highlight selected entities",
                ["dynamicsnap.highlight.primaryweight"] = "Primary weight:",
                ["dynamicsnap.highlight.primaryweight.tooltip"] = "Line weight of the active entity (solid line)",
                ["dynamicsnap.highlight.secondaryweight"] = "Secondary weight:",
                ["dynamicsnap.highlight.secondaryweight.tooltip"] = "Line weight of background entities (dashed line)",

                // Colors
                ["dynamicsnap.color.rouge"] = "Red",
                ["dynamicsnap.color.jaune"] = "Yellow",
                ["dynamicsnap.color.vert"] = "Green",
                ["dynamicsnap.color.cyan"] = "Cyan",
                ["dynamicsnap.color.bleu"] = "Blue",
                ["dynamicsnap.color.magenta"] = "Magenta",
                ["dynamicsnap.color.blanc"] = "White",
            },

            ["es"] = new Dictionary<string, string>
            {
                // Módulo
                ["dynamicsnap.name"] = "Enganche Dinámico OAS",
                ["dynamicsnap.loaded"] = "[DynamicSnap] Módulo de enganche dinámico cargado",

                // Menú
                ["menu.dessin"] = "Dibujo",

                // Comando
                ["dynamicsnap.cmd.title"] = "Enganche OAS",
                ["dynamicsnap.cmd.desc"] = "Configurar los parámetros de enganche dinámico OAS",

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

                // Ventana de configuración
                ["dynamicsnap.settings.title"] = "Configuración de Enganche OAS",
                ["dynamicsnap.settings.header"] = "Configuración de Enganche OAS",
                ["dynamicsnap.settings.modes"] = "Modos de enganche activos",
                ["dynamicsnap.settings.appearance"] = "Apariencia de los marcadores",
                ["dynamicsnap.settings.vertex"] = "Vértices (Vertex)",
                ["dynamicsnap.settings.vertex.tooltip"] = "Enganche a los vértices de polilíneas y extremos",
                ["dynamicsnap.settings.endpoint"] = "Extremos (Endpoint)",
                ["dynamicsnap.settings.endpoint.tooltip"] = "Enganche a los extremos de entidades",
                ["dynamicsnap.settings.midpoint"] = "Puntos medios (Midpoint)",
                ["dynamicsnap.settings.midpoint.tooltip"] = "Enganche a los puntos medios de segmentos",
                ["dynamicsnap.settings.nearest"] = "Punto más cercano (Nearest)",
                ["dynamicsnap.settings.nearest.tooltip"] = "Enganche al punto más cercano de la entidad",
                ["dynamicsnap.settings.color"] = "Color del marcador:",
                ["dynamicsnap.settings.color.tooltip"] = "Color del marcador de enganche inactivo",
                ["dynamicsnap.settings.activecolor"] = "Color activo:",
                ["dynamicsnap.settings.activecolor.tooltip"] = "Color del marcador de enganche activo",
                ["dynamicsnap.settings.tolerance"] = "Tolerancia:",
                ["dynamicsnap.settings.tolerance.tooltip"] = "Ratio de tolerancia para la detección de enganche",
                ["dynamicsnap.settings.markersize"] = "Tamaño del marcador:",
                ["dynamicsnap.settings.markersize.tooltip"] = "Tamaño de visualización del marcador de enganche",
                ["dynamicsnap.settings.filled"] = "Marcadores rellenos",
                ["dynamicsnap.settings.filled.tooltip"] = "Rellenar los marcadores en lugar de mostrar solo el contorno",
                ["dynamicsnap.settings.lineweight"] = "Grosor de línea:",
                ["dynamicsnap.settings.apply"] = "Aplicar",
                ["dynamicsnap.settings.cancel"] = "Cancelar",
                ["dynamicsnap.settings.reset"] = "Restablecer",
                ["dynamicsnap.settings.reset.tooltip"] = "Restablecer todos los parámetros a los valores predeterminados",
                ["dynamicsnap.settings.saved"] = "Configuración de enganche guardada",

                // Resaltado de entidades
                ["dynamicsnap.highlight.header"] = "Resaltado de entidades",
                ["dynamicsnap.highlight.enabled"] = "Activar resaltado",
                ["dynamicsnap.highlight.enabled.tooltip"] = "Resalta las entidades seleccionadas durante los comandos OAS",
                ["dynamicsnap.highlight.color"] = "Color de resaltado:",
                ["dynamicsnap.highlight.color.tooltip"] = "Color utilizado para resaltar las entidades seleccionadas",
                ["dynamicsnap.highlight.primaryweight"] = "Grosor principal:",
                ["dynamicsnap.highlight.primaryweight.tooltip"] = "Grosor de línea de la entidad activa (línea continua)",
                ["dynamicsnap.highlight.secondaryweight"] = "Grosor secundario:",
                ["dynamicsnap.highlight.secondaryweight.tooltip"] = "Grosor de línea de las entidades de fondo (línea discontinua)",

                // Colores
                ["dynamicsnap.color.rouge"] = "Rojo",
                ["dynamicsnap.color.jaune"] = "Amarillo",
                ["dynamicsnap.color.vert"] = "Verde",
                ["dynamicsnap.color.cyan"] = "Cian",
                ["dynamicsnap.color.bleu"] = "Azul",
                ["dynamicsnap.color.magenta"] = "Magenta",
                ["dynamicsnap.color.blanc"] = "Blanco",
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
