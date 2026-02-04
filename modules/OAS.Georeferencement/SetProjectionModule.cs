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
using OpenAsphalte.Modules.Georeferencement.Commands;

namespace OpenAsphalte.Modules.Georeferencement;

/// <summary>
/// Module de géoréférencement pour Open Asphalte.
/// Gestion des systèmes de coordonnées et projection des dessins AutoCAD.
/// </summary>
public class SetProjectionModule : ModuleBase
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICATION DU MODULE
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Identifiant unique du module
    /// </summary>
    public override string Id => "setprojection";
    
    /// <summary>
    /// Nom affiché dans les menus et rubans
    /// </summary>
    public override string Name => "Définir une projection";
    
    /// <summary>
    /// Description du module
    /// </summary>
    public override string Description => "Outils pour définir le système de projection du dessin";

    /// <summary>
    /// Contributeurs du module
    /// </summary>
    public override IEnumerable<Contributor> Contributors => new[]
    {
        new Contributor("Charles TILLY", "Lead Developer", "https://linkedin.com/in/charlestilly"),
        new Contributor("Open Asphalte Community", "Testing & Support")
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
    // AFFICHAGE UI
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Ordre d'affichage (modules officiels entre 10-50)
    /// </summary>
    public override int Order => 10;
    
    /// <summary>
    /// Clé de traduction pour le nom
    /// </summary>
    public override string? NameKey => "setproj.name";
    
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
            typeof(SetProjectionCommand),
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
                // Menus / Catégories
                ["menu.carto"] = "Cartographie",
                ["menu.georef"] = "Géoréférencement",
                
                // Module
                ["setproj.name"] = "Définir une projection",
                ["georef.name"] = "Géoréférencement",
                
                // Commande: Définir projection
                ["georef.setproj.title"] = "Définir une projection",
                ["georef.setproj.desc"] = "Définir le système de coordonnées du dessin",
                ["georef.setproj.success"] = "Système de coordonnées défini : {0}",
                ["georef.setproj.cancelled"] = "Opération annulée",
                ["georef.setproj.cleared"] = "Système de coordonnées supprimé",
                ["georef.setproj.error"] = "Erreur lors de la définition du système de coordonnées",
                ["georef.setproj.error.notfound"] = "Système de coordonnées non reconnu. Vérifiez qu'AutoCAD Map 3D ou Civil 3D est installé et que le système est disponible.",
                ["georef.setproj.current"] = "Système actuel : {0}",
                ["georef.setproj.none"] = "Aucun système défini",
                ["georef.setproj.detected"] = "Système détecté automatiquement : {0}",
                ["georef.setproj.nodetection"] = "Impossible de détecter automatiquement le système",
                ["georef.setproj.applied_info"] = "✓ Géolocalisation appliquée - Les Bing Maps et cartes sont maintenant géoréférencés",
                
                // Fenêtre de sélection
                ["georef.window.title"] = "Définir le système de coordonnées",
                ["georef.window.search"] = "Rechercher...",
                ["georef.window.search.tooltip"] = "Rechercher par nom, code, pays ou région",
                ["georef.window.list.header.name"] = "Système de coordonnées",
                ["georef.window.list.header.code"] = "Code",
                ["georef.window.list.header.country"] = "Pays",
                ["georef.window.list.header.region"] = "Région",
                ["georef.window.details"] = "Détails",
                ["georef.window.details.code"] = "Code AutoCAD :",
                ["georef.window.details.epsg"] = "EPSG :",
                ["georef.window.details.unit"] = "Unité :",
                ["georef.window.details.bounds"] = "Limites :",
                ["georef.window.details.description"] = "Description :",
                ["georef.window.current"] = "Système actuel :",
                ["georef.window.detected"] = "Système détecté :",
                ["georef.window.none"] = "(Aucun)",
                ["georef.window.clear"] = "Supprimer",
                ["georef.window.clear.tooltip"] = "Supprimer le système de coordonnées actuel",
                ["georef.window.apply"] = "Appliquer",
                ["georef.window.cancel"] = "Annuler",
                ["georef.window.groupby"] = "Grouper par pays",
                ["georef.window.enablegeomap"] = "Activer Bing Maps (GEOMAP)",
                ["georef.window.enablegeomap.tooltip"] = "Active l'affichage de la carte aérienne Bing Maps après application",
                ["georef.window.results.none"] = "Aucun système trouvé",
                ["georef.window.results.one"] = "1 système",
                ["georef.window.results.many"] = "{0} systèmes",
                ["georef.window.unit.meters"] = "Mètres",
                ["georef.window.unit.degrees"] = "Degrés",
                ["georef.setproj.geomap.activating"] = "Activation de Bing Maps (GEOMAP)...",
            },
            
            // ═══════════════════════════════════════════════════════
            // ENGLISH
            // ═══════════════════════════════════════════════════════
            ["en"] = new Dictionary<string, string>
            {
                // Menus / Categories
                ["menu.carto"] = "Cartography",
                ["menu.georef"] = "Georeferencing",

                // Module
                ["setproj.name"] = "Set Projection",
                ["georef.name"] = "Georeferencing",
                
                // Command: Set projection
                ["georef.setproj.title"] = "Set Projection",
                ["georef.setproj.desc"] = "Define the coordinate system for the drawing",
                ["georef.setproj.success"] = "Coordinate system set: {0}",
                ["georef.setproj.cancelled"] = "Operation cancelled",
                ["georef.setproj.cleared"] = "Coordinate system removed",
                ["georef.setproj.error"] = "Error setting coordinate system",
                ["georef.setproj.error.notfound"] = "Coordinate system not recognized. Check that AutoCAD Map 3D or Civil 3D is installed and the system is available.",
                ["georef.setproj.current"] = "Current system: {0}",
                ["georef.setproj.none"] = "No system defined",
                ["georef.setproj.detected"] = "Automatically detected system: {0}",
                ["georef.setproj.nodetection"] = "Unable to automatically detect system",
                ["georef.setproj.applied_info"] = "✓ Geolocation applied - Bing Maps and maps are now georeferenced",
                ["georef.setproj.geomap.activating"] = "Activating Bing Maps (GEOMAP)...",
                
                // Selection window
                ["georef.window.title"] = "Set Coordinate System",
                ["georef.window.search"] = "Search...",
                ["georef.window.search.tooltip"] = "Search by name, code, country or region",
                ["georef.window.list.header.name"] = "Coordinate System",
                ["georef.window.list.header.code"] = "Code",
                ["georef.window.list.header.country"] = "Country",
                ["georef.window.list.header.region"] = "Region",
                ["georef.window.details"] = "Details",
                ["georef.window.details.code"] = "AutoCAD Code:",
                ["georef.window.details.epsg"] = "EPSG:",
                ["georef.window.details.unit"] = "Unit:",
                ["georef.window.details.bounds"] = "Bounds:",
                ["georef.window.details.description"] = "Description:",
                ["georef.window.current"] = "Current system:",
                ["georef.window.detected"] = "Detected system:",
                ["georef.window.none"] = "(None)",
                ["georef.window.clear"] = "Clear",
                ["georef.window.clear.tooltip"] = "Remove current coordinate system",
                ["georef.window.apply"] = "Apply",
                ["georef.window.cancel"] = "Cancel",
                ["georef.window.groupby"] = "Group by country",
                ["georef.window.enablegeomap"] = "Enable Bing Maps (GEOMAP)",
                ["georef.window.enablegeomap.tooltip"] = "Activate Bing Maps aerial imagery after applying projection",
                ["georef.window.results.none"] = "No system found",
                ["georef.window.results.one"] = "1 system",
                ["georef.window.results.many"] = "{0} systems",
                ["georef.window.unit.meters"] = "Meters",
                ["georef.window.unit.degrees"] = "Degrees",
            },
            
            // ═══════════════════════════════════════════════════════
            // ESPAÑOL
            // ═══════════════════════════════════════════════════════
            ["es"] = new Dictionary<string, string>
            {
                // Menus / Categorias
                ["menu.carto"] = "Cartografía",
                ["menu.georef"] = "Georreferenciación",

                // Módulo
                ["setproj.name"] = "Definir proyección",
                ["georef.name"] = "Georreferenciación",
                
                // Comando: Definir proyección
                ["georef.setproj.title"] = "Definir proyección",
                ["georef.setproj.desc"] = "Definir el sistema de coordenadas del dibujo",
                ["georef.setproj.success"] = "Sistema de coordenadas definido: {0}",
                ["georef.setproj.cancelled"] = "Operación cancelada",
                ["georef.setproj.cleared"] = "Sistema de coordenadas eliminado",
                ["georef.setproj.error"] = "Error al definir el sistema de coordenadas",
                ["georef.setproj.error.notfound"] = "Sistema de coordenadas no reconocido. Verifique que AutoCAD Map 3D o Civil 3D esté instalado y que el sistema esté disponible.",
                ["georef.setproj.current"] = "Sistema actual: {0}",
                ["georef.setproj.none"] = "Ningún sistema definido",
                ["georef.setproj.detected"] = "Sistema detectado automáticamente: {0}",
                ["georef.setproj.nodetection"] = "No se puede detectar el sistema automáticamente",
                ["georef.setproj.applied_info"] = "✓ Geolocalización aplicada - Bing Maps y mapas están ahora georreferenciados",
                ["georef.setproj.geomap.activating"] = "Activando Bing Maps (GEOMAP)...",
                
                // Ventana de selección
                ["georef.window.title"] = "Definir sistema de coordenadas",
                ["georef.window.search"] = "Buscar...",
                ["georef.window.search.tooltip"] = "Buscar por nombre, código, país o región",
                ["georef.window.list.header.name"] = "Sistema de coordenadas",
                ["georef.window.list.header.code"] = "Código",
                ["georef.window.list.header.country"] = "País",
                ["georef.window.list.header.region"] = "Región",
                ["georef.window.details"] = "Detalles",
                ["georef.window.details.code"] = "Código AutoCAD:",
                ["georef.window.details.epsg"] = "EPSG:",
                ["georef.window.details.unit"] = "Unidad:",
                ["georef.window.details.bounds"] = "Límites:",
                ["georef.window.details.description"] = "Descripción:",
                ["georef.window.current"] = "Sistema actual:",
                ["georef.window.detected"] = "Sistema detectado:",
                ["georef.window.none"] = "(Ninguno)",
                ["georef.window.clear"] = "Eliminar",
                ["georef.window.clear.tooltip"] = "Eliminar sistema de coordenadas actual",
                ["georef.window.apply"] = "Aplicar",
                ["georef.window.cancel"] = "Cancelar",
                ["georef.window.groupby"] = "Agrupar por país",
                ["georef.window.enablegeomap"] = "Activar Bing Maps (GEOMAP)",
                ["georef.window.enablegeomap.tooltip"] = "Activa la imagen aérea de Bing Maps después de aplicar la proyección",
                ["georef.window.results.none"] = "Ningún sistema encontrado",
                ["georef.window.results.one"] = "1 sistema",
                ["georef.window.results.many"] = "{0} sistemas",
                ["georef.window.unit.meters"] = "Metros",
                ["georef.window.unit.degrees"] = "Grados",
            }
        };
    }
}
