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

using OpenAsphalte.Abstractions;
using OpenAsphalte.Modules.PrezOrganizer.Commands;

namespace OpenAsphalte.Modules.PrezOrganizer;

/// <summary>
/// Module organiseur de présentations pour Open Asphalte.
/// Fournit une interface graphique complète pour gérer, trier, renommer
/// et réorganiser les onglets de présentation d'un dessin AutoCAD.
///
/// Fonctionnalités :
/// - Réorganisation par boutons (monter, descendre, haut, bas) et drag &amp; drop
/// - Tri alphabétique, numérique et architectural
/// - Renommage par double-clic, batch rename avec patterns ({N}, {ORIG}, {DATE})
/// - Préfixe/Suffixe et changement de casse (UPPER, lower, Title)
/// - Rechercher &amp; Remplacer dans les noms
/// - Ajout, copie et suppression de présentations
/// - Filtrage par nom et compteur de présentations
/// - Mode batch avec Undo et mode immédiat au choix
/// </summary>
public class PrezOrganizerModule : ModuleBase
{
    // -----------------------------------------------------------
    // IDENTIFICATION DU MODULE
    // -----------------------------------------------------------

    /// <summary>
    /// Identifiant unique du module
    /// </summary>
    public override string Id => "prezorganizer";

    /// <summary>
    /// Nom affiché dans l'UI
    /// </summary>
    public override string Name => "Classer les présentations";

    /// <summary>
    /// Description du module
    /// </summary>
    public override string Description =>
        "Organiseur avancé de présentations AutoCAD : tri, renommage, copie, batch rename et réorganisation complète des onglets";

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

    // -----------------------------------------------------------
    // CONFIGURATION AFFICHAGE UI
    // -----------------------------------------------------------

    /// <summary>
    /// Ordre d'affichage dans le menu et le ruban
    /// </summary>
    public override int Order => 30;

    /// <summary>
    /// Clé de traduction pour le nom du module
    /// </summary>
    public override string? NameKey => "prezorganizer.name";

    /// <summary>
    /// Version minimale du Core requise
    /// </summary>
    public override string MinCoreVersion => "0.0.3";

    // -----------------------------------------------------------
    // COMMANDES
    // -----------------------------------------------------------

    /// <summary>
    /// Retourne la commande de l'organiseur de présentations.
    /// </summary>
    public override IEnumerable<Type> GetCommandTypes()
    {
        return [typeof(PrezOrganizerCommand)];
    }

    // -----------------------------------------------------------
    // TRADUCTIONS (FR, EN, ES)
    // -----------------------------------------------------------

    /// <summary>
    /// Retourne les traductions spécifiques au module dans les 3 langues.
    /// </summary>
    public override IDictionary<string, IDictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, IDictionary<string, string>>
        {
            ["fr"] = new Dictionary<string, string>
            {
                // Module
                ["prezorganizer.name"] = "Présentations",

                // Menu
                ["menu.mep"] = "Mise en page",

                // Commande
                ["prezorganizer.cmd.title"] = "Organiseur de présentations",
                ["prezorganizer.cmd.desc"] = "Organiser, trier et renommer les onglets de présentation",

                // Fenêtre principale
                ["prezorganizer.window.title"] = "Organiseur de présentations",
                ["prezorganizer.window.header"] = "Organiseur de présentations",
                ["prezorganizer.window.search"] = "  Filtrer les présentations...",
                ["prezorganizer.window.immediate"] = "Appliquer immédiatement",
                ["prezorganizer.window.immediate.tooltip"] = "Chaque modification est appliquée directement au dessin (sans prévisualisation)",

                // Boutons de déplacement
                ["prezorganizer.btn.moveTop"] = "Tout en haut",
                ["prezorganizer.btn.moveUp"] = "Monter",
                ["prezorganizer.btn.moveDown"] = "Descendre",
                ["prezorganizer.btn.moveBottom"] = "Tout en bas",
                ["prezorganizer.btn.reverse"] = "Inverser la sélection",

                // Boutons de tri
                ["prezorganizer.btn.sort"] = "Trier...",
                ["prezorganizer.sort.alpha"] = "Alphabétique (A?Z)",
                ["prezorganizer.sort.alphaDesc"] = "Alphabétique (Z?A)",
                ["prezorganizer.sort.num"] = "Numérique (1?9)",
                ["prezorganizer.sort.numDesc"] = "Numérique (9?1)",
                ["prezorganizer.sort.arch"] = "Architectural",
                ["prezorganizer.sort.archDesc"] = "Architectural (inverse)",

                // Boutons d'édition
                ["prezorganizer.btn.rename"] = "Renommer",
                ["prezorganizer.btn.copy"] = "Copier",
                ["prezorganizer.btn.add"] = "Ajouter",
                ["prezorganizer.btn.delete"] = "Supprimer",

                // Boutons de transformation
                ["prezorganizer.btn.findReplace"] = "Chercher / Remplacer",
                ["prezorganizer.btn.renameTool"] = "Outil de renommage",
                ["prezorganizer.btn.case"] = "Changer la casse",

                // Casse
                ["prezorganizer.case.upper"] = "MAJUSCULES",
                ["prezorganizer.case.lower"] = "minuscules",
                ["prezorganizer.case.title"] = "Première Lettre Majuscule",

                // Panneau de détails
                ["prezorganizer.detail.header"] = "Détails",
                ["prezorganizer.detail.originalName"] = "Nom original :",
                ["prezorganizer.detail.newName"] = "Nouveau nom :",
                ["prezorganizer.detail.status"] = "Statut :",
                ["prezorganizer.detail.status.unchanged"] = "Inchangé",
                ["prezorganizer.detail.status.renamed"] = "Renommé",
                ["prezorganizer.detail.status.new"] = "Nouveau",
                ["prezorganizer.detail.status.copy"] = "Copie",
                ["prezorganizer.detail.status.deleted"] = "Supprimé",
                ["prezorganizer.detail.pending"] = "Modifications en attente",
                ["prezorganizer.detail.renames"] = "{0} renommage(s)",
                ["prezorganizer.detail.moves"] = "{0} déplacement(s)",
                ["prezorganizer.detail.additions"] = "{0} ajout(s)",
                ["prezorganizer.detail.deletions"] = "{0} suppression(s)",

                // Barre de statut
                ["prezorganizer.status.count"] = "{0} présentation(s)",
                ["prezorganizer.status.pending"] = "{0} modification(s) en attente",
                ["prezorganizer.status.noPending"] = "Aucune modification",
                ["prezorganizer.btn.setCurrent"] = "Activer",
                ["prezorganizer.btn.setCurrent.tooltip"] = "Rendre cette présentation active dans AutoCAD",

                // Boutons principaux
                ["prezorganizer.btn.undo"] = "Annuler",
                ["prezorganizer.btn.undo.tooltip"] = "Annuler la dernière opération",
                ["prezorganizer.btn.reset"] = "Réinitialiser",
                ["prezorganizer.btn.reset.tooltip"] = "Remettre toutes les présentations à leur état initial",
                ["prezorganizer.btn.close"] = "Fermer",
                ["prezorganizer.btn.apply"] = "Appliquer",
                ["prezorganizer.btn.apply.tooltip"] = "Appliquer toutes les modifications au dessin",

                // Dialogues
                ["prezorganizer.rename.title"] = "Renommer la présentation",
                ["prezorganizer.rename.label"] = "Nouveau nom :",
                ["prezorganizer.rename.ok"] = "OK",
                ["prezorganizer.rename.cancel"] = "Annuler",

                // Batch Rename
                ["prezorganizer.batch.title"] = "Renommage par lot",
                ["prezorganizer.batch.pattern"] = "Pattern :",
                ["prezorganizer.batch.pattern.tooltip"] = "Variables : {N} numéro, {N:00} formaté, {ORIG} nom actuel, {DATE} date",
                ["prezorganizer.batch.startNum"] = "Numéro de départ :",
                ["prezorganizer.batch.increment"] = "Incrément :",
                ["prezorganizer.batch.scope"] = "Appliquer à :",
                ["prezorganizer.batch.scope.selected"] = "Sélection uniquement",
                ["prezorganizer.batch.scope.all"] = "Toutes les présentations",
                ["prezorganizer.batch.preview"] = "Prévisualisation",
                ["prezorganizer.batch.previewCol.before"] = "Avant",
                ["prezorganizer.batch.previewCol.after"] = "Après",
                ["prezorganizer.batch.apply"] = "Appliquer",
                ["prezorganizer.batch.cancel"] = "Annuler",

                // Find & Replace
                ["prezorganizer.findrepl.title"] = "Chercher et Remplacer",
                ["prezorganizer.findrepl.search"] = "Rechercher :",
                ["prezorganizer.findrepl.replace"] = "Remplacer par :",
                ["prezorganizer.findrepl.caseSensitive"] = "Respecter la casse",
                ["prezorganizer.findrepl.preview"] = "Prévisualisation",
                ["prezorganizer.findrepl.previewCol.before"] = "Avant",
                ["prezorganizer.findrepl.previewCol.after"] = "Après",
                ["prezorganizer.findrepl.replaceAll"] = "Remplacer tout",
                ["prezorganizer.findrepl.cancel"] = "Annuler",
                ["prezorganizer.findrepl.noMatch"] = "Aucune correspondance trouvée",
                ["prezorganizer.findrepl.matches"] = "{0} correspondance(s)",

                // Prefix / Suffix
                ["prezorganizer.prefix.title"] = "Préfixe / Suffixe",
                ["prezorganizer.prefix.prefix"] = "Préfixe :",
                ["prezorganizer.prefix.suffix"] = "Suffixe :",
                ["prezorganizer.prefix.scope"] = "Appliquer à :",
                ["prezorganizer.prefix.scope.selected"] = "Sélection uniquement",
                ["prezorganizer.prefix.scope.all"] = "Toutes les présentations",
                ["prezorganizer.prefix.preview"] = "Prévisualisation",
                ["prezorganizer.prefix.previewCol.before"] = "Avant",
                ["prezorganizer.prefix.previewCol.after"] = "Après",
                ["prezorganizer.prefix.apply"] = "Appliquer",
                ["prezorganizer.prefix.cancel"] = "Annuler",

                // Outil de renommage (fusionné)
                ["prezorganizer.renameTool.title"] = "Outil de renommage",
                ["prezorganizer.renameTool.mode"] = "Mode de renommage :",
                ["prezorganizer.renameTool.mode.prefixSuffix"] = "Préfixe / Suffixe",
                ["prezorganizer.renameTool.mode.pattern"] = "Pattern avec variables",
                ["prezorganizer.renameTool.prefix"] = "Préfixe :",
                ["prezorganizer.renameTool.suffix"] = "Suffixe :",
                ["prezorganizer.renameTool.pattern"] = "Pattern :",
                ["prezorganizer.renameTool.pattern.help"] = "Variables : {N} numéro, {N:00} formaté, {ORIG} nom actuel, {DATE} date",
                ["prezorganizer.renameTool.startNum"] = "Numéro de départ :",
                ["prezorganizer.renameTool.increment"] = "Incrément :",
                ["prezorganizer.renameTool.scope"] = "Appliquer à :",
                ["prezorganizer.renameTool.scope.selected"] = "Sélection uniquement",
                ["prezorganizer.renameTool.scope.all"] = "Toutes les présentations",
                ["prezorganizer.renameTool.preview"] = "Prévisualisation",
                ["prezorganizer.renameTool.preview.before"] = "Avant",
                ["prezorganizer.renameTool.preview.after"] = "Après",
                ["prezorganizer.renameTool.apply"] = "Appliquer",
                ["prezorganizer.renameTool.cancel"] = "Annuler",
                ["prezorganizer.renameTool.error.invalid"] = "Nom de présentation invalide",

                // Messages
                ["prezorganizer.success"] = "Modifications appliquées avec succès",
                ["prezorganizer.success.count"] = "{0} modification(s) appliquée(s)",
                ["prezorganizer.cancelled"] = "Organiseur fermé sans modification",
                ["prezorganizer.confirm.delete"] = "Supprimer {0} présentation(s) ?",
                ["prezorganizer.confirm.reset"] = "Réinitialiser toutes les modifications ?",
                ["prezorganizer.confirm.deleteAll"] = "Impossible de supprimer toutes les présentations.\nIl doit rester au moins une présentation.",

                // Erreurs
                ["prezorganizer.error.emptyName"] = "Le nom ne peut pas être vide",
                ["prezorganizer.error.nameTooLong"] = "Le nom ne peut pas dépasser 255 caractères",
                ["prezorganizer.error.reservedName"] = "\"Model\" est un nom réservé",
                ["prezorganizer.error.invalidChars"] = "Le nom contient des caractères interdits (< > / \\ \" : ; ? * | , = `)",
                ["prezorganizer.error.duplicateName"] = "Ce nom est déjà utilisé",
                ["prezorganizer.error.noSelection"] = "Aucune présentation sélectionnée",
                ["prezorganizer.error.applyFailed"] = "Erreur lors de l'application des modifications",
            },

            ["en"] = new Dictionary<string, string>
            {
                // Module
                ["prezorganizer.name"] = "Presentations",

                // Menu
                ["menu.mep"] = "Page Setup",

                // Command
                ["prezorganizer.cmd.title"] = "Presentation Organizer",
                ["prezorganizer.cmd.desc"] = "Organize, sort and rename presentation tabs",

                // Main window
                ["prezorganizer.window.title"] = "Presentation Organizer",
                ["prezorganizer.window.header"] = "Presentation Organizer",
                ["prezorganizer.window.search"] = "  Filter presentations...",
                ["prezorganizer.window.immediate"] = "Apply immediately",
                ["prezorganizer.window.immediate.tooltip"] = "Each change is applied directly to the drawing (no preview)",

                // Move buttons
                ["prezorganizer.btn.moveTop"] = "Move to top",
                ["prezorganizer.btn.moveUp"] = "Move up",
                ["prezorganizer.btn.moveDown"] = "Move down",
                ["prezorganizer.btn.moveBottom"] = "Move to bottom",
                ["prezorganizer.btn.reverse"] = "Reverse selection",

                // Sort buttons
                ["prezorganizer.btn.sort"] = "Sort...",
                ["prezorganizer.sort.alpha"] = "Alphabetical (A?Z)",
                ["prezorganizer.sort.alphaDesc"] = "Alphabetical (Z?A)",
                ["prezorganizer.sort.num"] = "Numerical (1?9)",
                ["prezorganizer.sort.numDesc"] = "Numerical (9?1)",
                ["prezorganizer.sort.arch"] = "Architectural",
                ["prezorganizer.sort.archDesc"] = "Architectural (reverse)",

                // Edit buttons
                ["prezorganizer.btn.rename"] = "Rename",
                ["prezorganizer.btn.copy"] = "Copy",
                ["prezorganizer.btn.add"] = "Add",
                ["prezorganizer.btn.delete"] = "Delete",

                // Transform buttons
                ["prezorganizer.btn.findReplace"] = "Find / Replace",
                ["prezorganizer.btn.renameTool"] = "Rename Tool",
                ["prezorganizer.btn.case"] = "Change case",

                // Case
                ["prezorganizer.case.upper"] = "UPPERCASE",
                ["prezorganizer.case.lower"] = "lowercase",
                ["prezorganizer.case.title"] = "Title Case",

                // Details panel
                ["prezorganizer.detail.header"] = "Details",
                ["prezorganizer.detail.originalName"] = "Original name:",
                ["prezorganizer.detail.newName"] = "New name:",
                ["prezorganizer.detail.status"] = "Status:",
                ["prezorganizer.detail.status.unchanged"] = "Unchanged",
                ["prezorganizer.detail.status.renamed"] = "Renamed",
                ["prezorganizer.detail.status.new"] = "New",
                ["prezorganizer.detail.status.copy"] = "Copy",
                ["prezorganizer.detail.status.deleted"] = "Deleted",
                ["prezorganizer.detail.pending"] = "Pending changes",
                ["prezorganizer.detail.renames"] = "{0} rename(s)",
                ["prezorganizer.detail.moves"] = "{0} move(s)",
                ["prezorganizer.detail.additions"] = "{0} addition(s)",
                ["prezorganizer.detail.deletions"] = "{0} deletion(s)",

                // Status bar
                ["prezorganizer.status.count"] = "{0} presentation(s)",
                ["prezorganizer.status.pending"] = "{0} pending change(s)",
                ["prezorganizer.status.noPending"] = "No changes",
                ["prezorganizer.btn.setCurrent"] = "Set Current",
                ["prezorganizer.btn.setCurrent.tooltip"] = "Make this presentation active in AutoCAD",

                // Main buttons
                ["prezorganizer.btn.undo"] = "Undo",
                ["prezorganizer.btn.undo.tooltip"] = "Undo last operation",
                ["prezorganizer.btn.reset"] = "Reset",
                ["prezorganizer.btn.reset.tooltip"] = "Reset all presentations to their initial state",
                ["prezorganizer.btn.close"] = "Close",
                ["prezorganizer.btn.apply"] = "Apply",
                ["prezorganizer.btn.apply.tooltip"] = "Apply all changes to the drawing",

                // Dialogs
                ["prezorganizer.rename.title"] = "Rename presentation",
                ["prezorganizer.rename.label"] = "New name:",
                ["prezorganizer.rename.ok"] = "OK",
                ["prezorganizer.rename.cancel"] = "Cancel",

                // Batch Rename
                ["prezorganizer.batch.title"] = "Batch Rename",
                ["prezorganizer.batch.pattern"] = "Pattern:",
                ["prezorganizer.batch.pattern.tooltip"] = "Variables: {N} number, {N:00} formatted, {ORIG} current name, {DATE} date",
                ["prezorganizer.batch.startNum"] = "Start number:",
                ["prezorganizer.batch.increment"] = "Increment:",
                ["prezorganizer.batch.scope"] = "Apply to:",
                ["prezorganizer.batch.scope.selected"] = "Selection only",
                ["prezorganizer.batch.scope.all"] = "All presentations",
                ["prezorganizer.batch.preview"] = "Preview",
                ["prezorganizer.batch.previewCol.before"] = "Before",
                ["prezorganizer.batch.previewCol.after"] = "After",
                ["prezorganizer.batch.apply"] = "Apply",
                ["prezorganizer.batch.cancel"] = "Cancel",

                // Find & Replace
                ["prezorganizer.findrepl.title"] = "Find and Replace",
                ["prezorganizer.findrepl.search"] = "Find:",
                ["prezorganizer.findrepl.replace"] = "Replace with:",
                ["prezorganizer.findrepl.caseSensitive"] = "Match case",
                ["prezorganizer.findrepl.preview"] = "Preview",
                ["prezorganizer.findrepl.previewCol.before"] = "Before",
                ["prezorganizer.findrepl.previewCol.after"] = "After",
                ["prezorganizer.findrepl.replaceAll"] = "Replace all",
                ["prezorganizer.findrepl.cancel"] = "Cancel",
                ["prezorganizer.findrepl.noMatch"] = "No match found",
                ["prezorganizer.findrepl.matches"] = "{0} match(es)",

                // Prefix / Suffix
                ["prezorganizer.prefix.title"] = "Prefix / Suffix",
                ["prezorganizer.prefix.prefix"] = "Prefix:",
                ["prezorganizer.prefix.suffix"] = "Suffix:",
                ["prezorganizer.prefix.scope"] = "Apply to:",
                ["prezorganizer.prefix.scope.selected"] = "Selection only",
                ["prezorganizer.prefix.scope.all"] = "All presentations",
                ["prezorganizer.prefix.preview"] = "Preview",
                ["prezorganizer.prefix.previewCol.before"] = "Before",
                ["prezorganizer.prefix.previewCol.after"] = "After",
                ["prezorganizer.prefix.apply"] = "Apply",
                ["prezorganizer.prefix.cancel"] = "Cancel",

                // Rename Tool (merged)
                ["prezorganizer.renameTool.title"] = "Rename Tool",
                ["prezorganizer.renameTool.mode"] = "Rename mode:",
                ["prezorganizer.renameTool.mode.prefixSuffix"] = "Prefix / Suffix",
                ["prezorganizer.renameTool.mode.pattern"] = "Pattern with variables",
                ["prezorganizer.renameTool.prefix"] = "Prefix:",
                ["prezorganizer.renameTool.suffix"] = "Suffix:",
                ["prezorganizer.renameTool.pattern"] = "Pattern:",
                ["prezorganizer.renameTool.pattern.help"] = "Variables: {N} number, {N:00} formatted, {ORIG} current name, {DATE} date",
                ["prezorganizer.renameTool.startNum"] = "Start number:",
                ["prezorganizer.renameTool.increment"] = "Increment:",
                ["prezorganizer.renameTool.scope"] = "Apply to:",
                ["prezorganizer.renameTool.scope.selected"] = "Selection only",
                ["prezorganizer.renameTool.scope.all"] = "All presentations",
                ["prezorganizer.renameTool.preview"] = "Preview",
                ["prezorganizer.renameTool.preview.before"] = "Before",
                ["prezorganizer.renameTool.preview.after"] = "After",
                ["prezorganizer.renameTool.apply"] = "Apply",
                ["prezorganizer.renameTool.cancel"] = "Cancel",
                ["prezorganizer.renameTool.error.invalid"] = "Invalid presentation name",

                // Messages
                ["prezorganizer.success"] = "Changes applied successfully",
                ["prezorganizer.success.count"] = "{0} change(s) applied",
                ["prezorganizer.cancelled"] = "Organizer closed without changes",
                ["prezorganizer.confirm.delete"] = "Delete {0} presentation(s)?",
                ["prezorganizer.confirm.reset"] = "Reset all changes?",
                ["prezorganizer.confirm.deleteAll"] = "Cannot delete all presentations.\nAt least one presentation must remain.",

                // Errors
                ["prezorganizer.error.emptyName"] = "Name cannot be empty",
                ["prezorganizer.error.nameTooLong"] = "Name cannot exceed 255 characters",
                ["prezorganizer.error.reservedName"] = "\"Model\" is a reserved name",
                ["prezorganizer.error.invalidChars"] = "Name contains invalid characters (< > / \\ \" : ; ? * | , = `)",
                ["prezorganizer.error.duplicateName"] = "This name is already in use",
                ["prezorganizer.error.noSelection"] = "No presentation selected",
                ["prezorganizer.error.applyFailed"] = "Error applying changes",
            },

            ["es"] = new Dictionary<string, string>
            {
                // Módulo
                ["prezorganizer.name"] = "Presentaciones",

                // Menú
                ["menu.mep"] = "Configuración de página",

                // Comando
                ["prezorganizer.cmd.title"] = "Organizador de presentaciones",
                ["prezorganizer.cmd.desc"] = "Organizar, ordenar y renombrar las pestañas de presentación",

                // Ventana principal
                ["prezorganizer.window.title"] = "Organizador de presentaciones",
                ["prezorganizer.window.header"] = "Organizador de presentaciones",
                ["prezorganizer.window.search"] = "  Filtrar presentaciones...",
                ["prezorganizer.window.immediate"] = "Aplicar inmediatamente",
                ["prezorganizer.window.immediate.tooltip"] = "Cada cambio se aplica directamente al dibujo (sin vista previa)",

                // Botones de desplazamiento
                ["prezorganizer.btn.moveTop"] = "Mover arriba del todo",
                ["prezorganizer.btn.moveUp"] = "Subir",
                ["prezorganizer.btn.moveDown"] = "Bajar",
                ["prezorganizer.btn.moveBottom"] = "Mover abajo del todo",
                ["prezorganizer.btn.reverse"] = "Invertir selección",

                // Botones de ordenar
                ["prezorganizer.btn.sort"] = "Ordenar...",
                ["prezorganizer.sort.alpha"] = "Alfabético (A?Z)",
                ["prezorganizer.sort.alphaDesc"] = "Alfabético (Z?A)",
                ["prezorganizer.sort.num"] = "Numérico (1?9)",
                ["prezorganizer.sort.numDesc"] = "Numérico (9?1)",
                ["prezorganizer.sort.arch"] = "Arquitectónico",
                ["prezorganizer.sort.archDesc"] = "Arquitectónico (inverso)",

                // Botones de edición
                ["prezorganizer.btn.rename"] = "Renombrar",
                ["prezorganizer.btn.copy"] = "Copiar",
                ["prezorganizer.btn.add"] = "Añadir",
                ["prezorganizer.btn.delete"] = "Eliminar",

                // Botones de transformación
                ["prezorganizer.btn.findReplace"] = "Buscar / Reemplazar",
                ["prezorganizer.btn.renameTool"] = "Herramienta de renombrado",
                ["prezorganizer.btn.case"] = "Cambiar mayúsculas",

                // Mayúsculas/minúsculas
                ["prezorganizer.case.upper"] = "MAYÚSCULAS",
                ["prezorganizer.case.lower"] = "minúsculas",
                ["prezorganizer.case.title"] = "Primera Letra Mayúscula",

                // Panel de detalles
                ["prezorganizer.detail.header"] = "Detalles",
                ["prezorganizer.detail.originalName"] = "Nombre original:",
                ["prezorganizer.detail.newName"] = "Nuevo nombre:",
                ["prezorganizer.detail.status"] = "Estado:",
                ["prezorganizer.detail.status.unchanged"] = "Sin cambios",
                ["prezorganizer.detail.status.renamed"] = "Renombrado",
                ["prezorganizer.detail.status.new"] = "Nuevo",
                ["prezorganizer.detail.status.copy"] = "Copia",
                ["prezorganizer.detail.status.deleted"] = "Eliminado",
                ["prezorganizer.detail.pending"] = "Cambios pendientes",
                ["prezorganizer.detail.renames"] = "{0} renombrado(s)",
                ["prezorganizer.detail.moves"] = "{0} desplazamiento(s)",
                ["prezorganizer.detail.additions"] = "{0} adición(es)",
                ["prezorganizer.detail.deletions"] = "{0} eliminación(es)",

                // Barra de estado
                ["prezorganizer.status.count"] = "{0} presentación(es)",
                ["prezorganizer.status.pending"] = "{0} cambio(s) pendiente(s)",
                ["prezorganizer.status.noPending"] = "Sin cambios",
                ["prezorganizer.btn.setCurrent"] = "Activar",
                ["prezorganizer.btn.setCurrent.tooltip"] = "Hacer esta presentación activa en AutoCAD",

                // Botones principales
                ["prezorganizer.btn.undo"] = "Deshacer",
                ["prezorganizer.btn.undo.tooltip"] = "Deshacer la última operación",
                ["prezorganizer.btn.reset"] = "Restablecer",
                ["prezorganizer.btn.reset.tooltip"] = "Restablecer todas las presentaciones a su estado inicial",
                ["prezorganizer.btn.close"] = "Cerrar",
                ["prezorganizer.btn.apply"] = "Aplicar",
                ["prezorganizer.btn.apply.tooltip"] = "Aplicar todos los cambios al dibujo",

                // Diálogos
                ["prezorganizer.rename.title"] = "Renombrar presentación",
                ["prezorganizer.rename.label"] = "Nuevo nombre:",
                ["prezorganizer.rename.ok"] = "Aceptar",
                ["prezorganizer.rename.cancel"] = "Cancelar",

                // Renombrado por lotes
                ["prezorganizer.batch.title"] = "Renombrado por lotes",
                ["prezorganizer.batch.pattern"] = "Patrón:",
                ["prezorganizer.batch.pattern.tooltip"] = "Variables: {N} número, {N:00} formateado, {ORIG} nombre actual, {DATE} fecha",
                ["prezorganizer.batch.startNum"] = "Número inicial:",
                ["prezorganizer.batch.increment"] = "Incremento:",
                ["prezorganizer.batch.scope"] = "Aplicar a:",
                ["prezorganizer.batch.scope.selected"] = "Solo selección",
                ["prezorganizer.batch.scope.all"] = "Todas las presentaciones",
                ["prezorganizer.batch.preview"] = "Vista previa",
                ["prezorganizer.batch.previewCol.before"] = "Antes",
                ["prezorganizer.batch.previewCol.after"] = "Después",
                ["prezorganizer.batch.apply"] = "Aplicar",
                ["prezorganizer.batch.cancel"] = "Cancelar",

                // Buscar y reemplazar
                ["prezorganizer.findrepl.title"] = "Buscar y Reemplazar",
                ["prezorganizer.findrepl.search"] = "Buscar:",
                ["prezorganizer.findrepl.replace"] = "Reemplazar con:",
                ["prezorganizer.findrepl.caseSensitive"] = "Coincidir mayúsculas",
                ["prezorganizer.findrepl.preview"] = "Vista previa",
                ["prezorganizer.findrepl.previewCol.before"] = "Antes",
                ["prezorganizer.findrepl.previewCol.after"] = "Después",
                ["prezorganizer.findrepl.replaceAll"] = "Reemplazar todo",
                ["prezorganizer.findrepl.cancel"] = "Cancelar",
                ["prezorganizer.findrepl.noMatch"] = "No se encontraron coincidencias",
                ["prezorganizer.findrepl.matches"] = "{0} coincidencia(s)",

                // Prefijo / Sufijo
                ["prezorganizer.prefix.title"] = "Prefijo / Sufijo",
                ["prezorganizer.prefix.prefix"] = "Prefijo:",
                ["prezorganizer.prefix.suffix"] = "Sufijo:",
                ["prezorganizer.prefix.scope"] = "Aplicar a:",
                ["prezorganizer.prefix.scope.selected"] = "Solo selección",
                ["prezorganizer.prefix.scope.all"] = "Todas las presentaciones",
                ["prezorganizer.prefix.preview"] = "Vista previa",
                ["prezorganizer.prefix.previewCol.before"] = "Antes",
                ["prezorganizer.prefix.previewCol.after"] = "Después",
                ["prezorganizer.prefix.apply"] = "Aplicar",
                ["prezorganizer.prefix.cancel"] = "Cancelar",

                // Herramienta de renombrado (fusionada)
                ["prezorganizer.renameTool.title"] = "Herramienta de renombrado",
                ["prezorganizer.renameTool.mode"] = "Modo de renombrado:",
                ["prezorganizer.renameTool.mode.prefixSuffix"] = "Prefijo / Sufijo",
                ["prezorganizer.renameTool.mode.pattern"] = "Patrón con variables",
                ["prezorganizer.renameTool.prefix"] = "Prefijo:",
                ["prezorganizer.renameTool.suffix"] = "Sufijo:",
                ["prezorganizer.renameTool.pattern"] = "Patrón:",
                ["prezorganizer.renameTool.pattern.help"] = "Variables: {N} número, {N:00} formateado, {ORIG} nombre actual, {DATE} fecha",
                ["prezorganizer.renameTool.startNum"] = "Número inicial:",
                ["prezorganizer.renameTool.increment"] = "Incremento:",
                ["prezorganizer.renameTool.scope"] = "Aplicar a:",
                ["prezorganizer.renameTool.scope.selected"] = "Solo selección",
                ["prezorganizer.renameTool.scope.all"] = "Todas las presentaciones",
                ["prezorganizer.renameTool.preview"] = "Vista previa",
                ["prezorganizer.renameTool.preview.before"] = "Antes",
                ["prezorganizer.renameTool.preview.after"] = "Después",
                ["prezorganizer.renameTool.apply"] = "Aplicar",
                ["prezorganizer.renameTool.cancel"] = "Cancelar",
                ["prezorganizer.renameTool.error.invalid"] = "Nombre de presentación no válido",

                // Mensajes
                ["prezorganizer.success"] = "Cambios aplicados con éxito",
                ["prezorganizer.success.count"] = "{0} cambio(s) aplicado(s)",
                ["prezorganizer.cancelled"] = "Organizador cerrado sin cambios",
                ["prezorganizer.confirm.delete"] = "¿Eliminar {0} presentación(es)?",
                ["prezorganizer.confirm.reset"] = "¿Restablecer todos los cambios?",
                ["prezorganizer.confirm.deleteAll"] = "No se pueden eliminar todas las presentaciones.\nDebe quedar al menos una presentación.",

                // Errores
                ["prezorganizer.error.emptyName"] = "El nombre no puede estar vacío",
                ["prezorganizer.error.nameTooLong"] = "El nombre no puede superar los 255 caracteres",
                ["prezorganizer.error.reservedName"] = "\"Model\" es un nombre reservado",
                ["prezorganizer.error.invalidChars"] = "El nombre contiene caracteres no válidos (< > / \\ \" : ; ? * | , = `)",
                ["prezorganizer.error.duplicateName"] = "Este nombre ya está en uso",
                ["prezorganizer.error.noSelection"] = "No hay presentación seleccionada",
                ["prezorganizer.error.applyFailed"] = "Error al aplicar los cambios",
            },
        };
    }
}
