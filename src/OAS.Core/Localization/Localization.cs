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

using System.Collections.Concurrent;
using OpenAsphalte.Configuration;

namespace OpenAsphalte.Localization;

/// <summary>
/// Système de traduction multilingue pour Open Asphalte.
/// Supporte les traductions par module avec fallback automatique.
/// </summary>
/// <remarks>
/// <para>
/// Le Système de localisation supporte FR, EN et ES.
/// Les modules peuvent enregistrer leurs propres traductions via <see cref="RegisterTranslations"/>.
/// </para>
/// <para>
/// Lorsque la langue change, l'événement <see cref="OnLanguageChanged"/> est déclenché,
/// permettant à l'UI et aux modules de se Mettre à jour.
/// </para>
/// </remarks>
public static class Localization
{
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _translations = new();
    private static readonly string[] _supportedLanguages = { "fr", "en", "es" };
    private static string _currentLanguage = "fr";
    private static readonly object _languageLock = new();

    /// <summary>
    /// Préfixes des clés système protégées (ne peuvent pas être écrasées par les modules)
    /// </summary>
    private static readonly string[] _systemKeyPrefixes =
    {
        "app.", "cmd.", "error.", "select.", "system.", "about.", "settings.",
        "modules.", "common.", "config.", "module.", "ui.", "plugin.",
        "welcome.", "log."
    };

    /// <summary>
    /// événement déclenché lorsque la langue change.
    /// Les modules peuvent s'y abonner pour Mettre à jour leur UI.
    /// </summary>
    /// <example>
    /// <code>
    /// Localization.OnLanguageChanged += (oldLang, newLang) =>
    /// {
    ///     // Mettre à jour l'interface utilisateur
    /// };
    /// </code>
    /// </example>
    public static event Action<string, string>? OnLanguageChanged;

    /// <summary>
    /// Langues supportées
    /// </summary>
    public static IReadOnlyList<string> SupportedLanguages => _supportedLanguages;

    /// <summary>
    /// Langue active (depuis la configuration)
    /// </summary>
    public static string CurrentLanguage
    {
        get
        {
            lock (_languageLock)
            {
                return _currentLanguage;
            }
        }
    }

    /// <summary>
    /// Noms des langues pour l'affichage dans l'UI
    /// </summary>
    public static IReadOnlyDictionary<string, string> LanguageNames => new Dictionary<string, string>
    {
        ["fr"] = "Français",
        ["en"] = "English",
        ["es"] = "Español"
    };

    /// <summary>
    /// Nettoie les abonnés aux événements (appelé lors de la fermeture)
    /// </summary>
    public static void ClearEventSubscribers()
    {
        OnLanguageChanged = null;
    }

    /// <summary>
    /// Initialise les traductions de base du framework
    /// </summary>
    public static void Initialize()
    {
        // Synchroniser avec la configuration
        _currentLanguage = Configuration.Configuration.Language;

        // === Français ===
        RegisterTranslations("fr", new Dictionary<string, string>
        {
            // Application
            ["app.name"] = "Open Asphalte",
            ["app.loaded"] = "Plugin chargé",
            ["app.version"] = "Version",
            ["app.welcome"] = "Bienvenue dans Open Asphalte",

            // Commandes générales
            ["cmd.cancelled"] = "Commande annulée",
            ["cmd.error"] = "Erreur",
            ["cmd.success"] = "Opération réussie",

            // Erreurs
            ["error.noDocument"] = "Aucun document actif",
            ["error.invalidSelection"] = "Sélection invalide",
            ["error.moduleNotFound"] = "Module non trouvé",

            // Sélection
            ["select.point"] = "Sélectionnez un point",
            ["select.object"] = "Sélectionnez un objet",
            ["select.polyline"] = "Sélectionnez une polyligne",
            ["select.line"] = "Sélectionnez une ligne",

            // Système
            ["system.name"] = "Système",
            ["system.help"] = "Aide",
            ["system.help.desc"] = "Affiche la liste des commandes disponibles",
            ["system.version"] = "Version",
            ["system.version.desc"] = "Affiche les informations de version",
            ["system.settings"] = "Paramètres",
            ["system.settings.desc"] = "Ouvre la fenêtre des Paramètres",
            ["system.reload"] = "Recharger",
            ["system.reload.desc"] = "Recharge la configuration",
            ["system.update"] = "Mise à jour",
            ["system.update.desc"] = "Vérifie les mises à jour disponibles",
            ["system.modules"] = "Modules",
            ["system.modules.desc"] = "Ouvre le gestionnaire de modules",
            ["system.reload.success"] = "Configuration rechargée",
            ["system.help.title"] = "OPEN ASPHALTE - AIDE",
            ["system.help.section.system"] = "COMMANDES Système",
            ["system.help.section.module"] = "MODULE",
            ["system.version.header"] = "OPEN ASPHALTE v{0}",
            ["system.version.subtitle"] = "Plugin modulaire pour AutoCAD",
            ["system.version.modules"] = "Modules chargés: {0}",
            ["system.version.commands"] = "Commandes disponibles: {0}",
            ["system.version.language"] = "Langue: {0}",
            ["system.version.devmode"] = "Mode dev: {0}",
            ["system.update.checking"] = "Vérification des mises à jour...",
            ["system.update.current"] = "Version actuelle: {0}",
            ["system.update.opening"] = "Ouverture de la page des releases...",
            ["system.update.url"] = "URL: {0}",
            ["system.update.openError"] = "Impossible d'ouvrir le navigateur: {0}",

            // Vérification des mises à jour au démarrage
            ["update.available"] = "Mise à jour v{0} disponible !",
            ["update.incompatibleAutoCAD"] = "La version {0} nécessite AutoCAD {1}+ (vous avez {2})",
            ["update.notification.title"] = "Mise à jour disponible",
            ["update.notification.message"] = "Une nouvelle version d'Open Asphalte est disponible !\n\nVersion actuelle: {1}\nNouvelle version: {0}\n\nVoulez-vous ouvrir la page de téléchargement ?",
            ["update.checking.startup"] = "Vérification des mises à jour...",

            // Module Manager
            ["modules.manager.title"] = "Gestionnaire de Modules",
            ["modules.manager.subtitle"] = "Installez et gérez les modules Open Asphalte depuis le catalogue officiel.",
            ["modules.manager.refresh"] = "Actualiser",
            ["modules.manager.filter"] = "Filtrer:",
            ["modules.manager.empty"] = "Cliquez sur Actualiser pour charger le catalogue",
            ["modules.manager.loading"] = "Chargement du catalogue...",
            ["modules.manager.noManifest"] = "Catalogue vide ou inaccessible",
            ["modules.manager.noMatch"] = "Aucun module ne correspond au filtre",
            ["modules.manager.error"] = "Erreur de chargement",
            ["modules.manager.status"] = "{0} module(s) disponible(s), {1} installé(s), {2} mise(s) à jour",
            ["modules.manager.folder"] = "Dossier modules",
            ["modules.manager.folderNotFound"] = "Le dossier des modules n'existe pas encore.",
            ["modules.manager.downloading"] = "Téléchargement...",
            ["modules.manager.installing"] = "Installation de {0}...",
            ["modules.manager.installed"] = "Module {0} installé avec succès",
            ["modules.manager.installSuccess"] = "Installation réussie",
            ["modules.manager.restartRequired"] = "Le module {0} a été installé.\n\nRedémarrez AutoCAD pour l'activer.",
            ["modules.manager.installError"] = "Erreur lors de l'installation: {0}",
            ["modules.manager.retry"] = "Réessayer",
            ["modules.manager.openError"] = "Erreur ouverture gestionnaire: {0}",
            ["modules.manager.dependencies.title"] = "Dépendances requises",
            ["modules.manager.dependencies.confirm"] = "Le module \"{0}\" nécessite {2} dépendance(s) :\n\n• {1}\n\nVoulez-vous les installer maintenant ?",
            ["modules.manager.dependencies.message"] = "Le module \"{0}\" nécessite les modules suivants pour fonctionner :\n\n{1}\n\nVoulez-vous installer ces modules maintenant ?",
            ["modules.manager.dependencies.single"] = "Le module \"{0}\" nécessite le module \"{1}\" pour fonctionner.\n\nVoulez-vous l'installer maintenant ?",
            ["modules.manager.dependencies.installing"] = "Installation des dépendances...",
            ["modules.manager.dependencies.success"] = "Modules installés avec succès: {0}",
            ["modules.manager.dependencies.notFound"] = "Dépendance introuvable dans le catalogue: {0}",
            ["modules.manager.restartRequired.multiple"] = "Les modules suivants ont été installés :\n{0}\n\nRedémarrez AutoCAD pour les activer.",
            ["modules.manager.category.installAll"] = "Installer la catégorie",
            ["modules.manager.category.installCount"] = "Installer tout ({0})",
            ["modules.manager.category.confirmInstall"] = "Voulez-vous installer les {0} module(s) de la catégorie \"{1}\" ?\n\n• {2}",
            ["modules.manager.category.allInstalled"] = "Tous les modules de cette catégorie sont déjà installés.",
            ["modules.manager.category.installed"] = "{0} module(s) installé(s) depuis la catégorie {1}",
            ["modules.requires"] = "Requiert",
            ["modules.filter.all"] = "Tous",
            ["modules.filter.installed"] = "Installés",
            ["modules.filter.available"] = "Disponibles",
            ["modules.filter.updates"] = "Mises à jour",
            ["modules.action.install"] = "Installer",
            ["modules.action.update"] = "Mettre à jour",
            ["modules.action.installed"] = "Installé",
            ["modules.local.description"] = "Module local depuis {0}",

            // Update errors
            ["update.error.notFound"] = "Catalogue de modules introuvable (404). Vérifiez votre connexion ou réessayez plus tard.",
            ["update.error.forbidden"] = "Accès au catalogue refusé (403).",
            ["update.error.noInternet"] = "Impossible de contacter le serveur. Vérifiez votre connexion Internet.",
            ["update.error.http"] = "Erreur réseau: {0}",
            ["update.error.timeout"] = "Délai d'attente dépassé. Le serveur ne répond pas.",
            ["update.moduleInstalled"] = "Module {0} installé",

            // First run
            ["firstrun.noModules.title"] = "Bienvenue dans Open Asphalte !",
            ["firstrun.noModules.message"] = "Aucun module n'est installé.\n\nVoulez-vous ouvrir le Gestionnaire de Modules pour installer des extensions ?",

            // About window
            ["about.title"] = "À propos",
            ["about.subtitle"] = "Plugin modulaire pour AutoCAD",
            ["about.version"] = "Version",
            ["about.buildDate"] = "Date de build",
            ["about.channel"] = "Canal",
            ["about.framework"] = "Framework",
            ["about.modules"] = "Modules chargés",
            ["about.commands"] = "Commandes",
            ["about.license"] = "Ce logiciel est distribué sous licence Apache 2.0.\nCode source disponible sur GitHub.",
            ["about.checkUpdate"] = "Vérifier les mises à jour",
            ["about.close"] = "Fermer",
            ["about.invalidUrl"] = "L'URL de mise à jour est invalide ou non sécurisée.",
            ["about.alphaWarning"] = "⚠ VERSION ALPHA - Pour tests uniquement",
            ["about.betaWarning"] = "⚠ VERSION BETA - Peut contenir des bugs",
            ["about.reportBug"] = "Signaler un bug",
            ["about.reportBug.opening"] = "Ouverture de la page des issues GitHub...",
            ["about.reportBug.error"] = "Impossible d'ouvrir la page des issues : {0}",

            // Settings window
            ["settings.title"] = "Paramètres Open Asphalte",
            ["settings.language"] = "Langue",
            ["settings.devmode"] = "Mode développeur (logs détaillés)",
            ["settings.checkupdates"] = "Vérifier les mises à jour au démarrage",
            ["settings.save"] = "Enregistrer",
            ["settings.cancel"] = "Annuler",
            ["settings.languageChanged"] = "Langue modifiée. L'interface a été mise à jour.",
            ["settings.info"] = "Version {0} • {1} module(s) • {2} commande(s)\nConfiguration: {3}",
            ["settings.saveError"] = "Erreur lors de la sauvegarde: {0}",
            ["settings.openError"] = "Erreur ouverture Paramètres: {0}",
            ["settings.modules.status"] = "{0}/{1} modules installés",
            ["settings.modules.installSelected"] = "Installer la sélection",
            ["settings.modules.selectedCount"] = "{0} module(s) sélectionné(s)",
            ["settings.modules.dependenciesRequired"] = "Les modules suivants seront également installés (dépendances requises) :\n\n• {0}\n\nContinuer ?",
            ["settings.modules.confirmInstall"] = "Installer {0} module(s) ?\n\n• {1}",
            ["settings.modules.installTitle"] = "Installation",
            ["settings.modules.installed"] = "{0} module(s) installé(s)",

            // Modules
            ["modules.loaded"] = "{0} module(s) chargé(s)",
            ["modules.commands"] = "{0} commande(s) disponible(s)",

            // Common
            ["common.yes"] = "Oui",
            ["common.no"] = "Non",
            ["common.success"] = "Succès",
            ["common.error"] = "Erreur",

            // Configuration
            ["config.empty"] = "Fichier config vide, nouvelle configuration créée",
            ["config.loaded"] = "Configuration chargée depuis {0}",
            ["config.created"] = "Nouvelle configuration créée",
            ["config.corrupt"] = "Fichier config corrompu, réinitialisation: {0}",
            ["config.loadError"] = "Erreur chargement config: {0}",
            ["config.saved"] = "Configuration sauvegardée",
            ["config.saveError"] = "Erreur sauvegarde config: {0}",
            ["config.reloaded"] = "Configuration rechargée",
            ["config.migrating"] = "Migration de la configuration v{0} vers v{1}...",
            ["config.migrated"] = "Configuration migrée avec succès",

            // Modules discovery
            ["module.searchPath"] = "Recherche des modules dans: {0}",
            ["module.folderCreated"] = "Dossier Modules créé",
            ["module.dllFound"] = "{0} fichier(s) DLL trouvé(s)",
            ["module.dllFoundInPath"] = "{0} fichier(s) DLL trouvé(s) dans {1}",
            ["module.loadOutside"] = "Tentative de chargement d'une DLL hors du dossier Modules: {0}",
            ["module.dllMissing"] = "Fichier DLL introuvable: {0}",
            ["module.loading"] = "Chargement: {0}",
            ["module.noneFound"] = "Aucun module trouvé dans {0}",
            ["module.duplicate"] = "Module '{0}' déjà chargé, ignoré",
            ["module.loaded"] = "Module '{0}' v{1} chargé ({2} commandes)",
            ["module.instanceError"] = "Erreur instanciation module {0}: {1}",
            ["module.dllError"] = "Erreur chargement DLL {0}: {1}",
            ["module.depMissing"] = "Module '{0}' désactivé: dépendance manquante '{1}'",
            ["module.coreVersionUnknown"] = "Module '{0}': version Core non vérifiable (min='{1}', core='{2}')",
            ["module.coreVersionIncompatible"] = "Module '{0}' désactivé: version Core requise {1}, version actuelle {2}",
            ["module.migrated"] = "Module migré: {0} → {1}",
            ["module.summary"] = "{0} module(s) chargé(s), {1} commande(s) disponible(s)",
            ["module.initialized"] = "Module '{0}' initialisé",
            ["module.initError"] = "Erreur initialisation module '{0}': {1}",
            ["module.shutdown"] = "Module '{0}' fermé",
            ["module.shutdownError"] = "Erreur fermeture module '{0}': {1}",
            ["module.pathAdded"] = "Chemin modules ajouté: {0}",
            ["module.pathAddedTooLate"] = "Impossible d'ajouter un chemin après l'initialisation",
            ["module.pathNotFound"] = "Chemin modules introuvable: {0}",
            ["module.unsigned"] = "⚠️ Module non signé: {0} (non vérifié)",
            ["module.signed"] = "✓ Module signé: {0}",
            ["module.unsignedBlocked"] = "Module non signé bloqué: {0} (désactivez 'allowUnsignedModules' pour autoriser)",

            // UI
            ["ui.menu.created"] = "Menu Open Asphalte créé",
            ["ui.menu.createError"] = "Erreur création menu: {0}",
            ["ui.ribbon.notAvailable"] = "Ruban AutoCAD non disponible",
            ["ui.ribbon.created"] = "Ruban Open Asphalte créé",
            ["ui.ribbon.updated"] = "Ruban mis à jour (incrémental)",
            ["ui.ribbon.createError"] = "Erreur création ruban: {0}",
            ["ui.ribbon.panelError"] = "Erreur création panneau {0}: {1}",

            // Plugin lifecycle
            ["plugin.updateCheckDisabled"] = "Vérification des mises à jour désactivée pour la v1 (pas de serveur)",
            ["plugin.initError"] = "Erreur initialisation Open Asphalte: {0}",
            ["plugin.uiCreated"] = "Interface Open Asphalte créée",
            ["plugin.uiCreateError"] = "Erreur création interface: {0}",
            ["plugin.languageChange"] = "Changement de langue: {0} -> {1}",
            ["plugin.uiUpdated"] = "Interface mise à jour ({0})",
            ["plugin.uiUpdateError"] = "Erreur mise à jour interface: {0}",
            ["plugin.shutdownClean"] = "Open Asphalte fermé proprement",

            // Welcome
            ["welcome.title"] = "OPEN ASPHALTE v{0}",
            ["welcome.subtitle"] = "Plugin modulaire pour AutoCAD",
            ["welcome.modulesLoaded"] = "Modules chargés: {0}",
            ["welcome.noModules"] = "Aucun module chargé",
            ["welcome.dropModules"] = "Placez vos modules (.dll) dans le dossier Modules/",
            ["welcome.commandsAvailable"] = "{0} commande(s) disponible(s)",
            ["welcome.helpHint"] = "Tapez OAS_HELP pour la liste complète",

            // Logs
            ["log.stack"] = "Stack: {0}",
            ["log.level.debug"] = "[DEBUG]",
            ["log.level.info"] = "[INFO]",
            ["log.level.success"] = "[OK]",
            ["log.level.warn"] = "[WARN]",
            ["log.level.error"] = "[ERROR]",

            // Errors (transactions)
            ["error.noDatabase"] = "Database non disponible",

            // Credits window
            ["core.credits.title"] = "Crédits",
            ["core.credits.tab.core"] = "Équipe Core",
            ["core.credits.tab.modules"] = "Modules",
            ["core.credits.modules.list"] = "Modules installés",
            ["core.credits.author"] = "Auteur",
            ["core.credits.contributors"] = "Contributeurs",
            ["about.credits"] = "Crédits",
            ["core.close"] = "Fermer",

            // Settings tabs
            ["settings.tab.general"] = "Général",
            ["settings.tab.modules"] = "Modules",
            ["settings.modules.check"] = "Vérifier les mises à jour",
            ["settings.modules.checking"] = "Vérification en cours...",
            ["settings.modules.coreAvailable"] = "Une nouvelle version du Core est disponible",
            ["settings.modules.coreUpdateMsg"] = "Version {0} disponible (actuelle: {1})",
        }, isSystemRegistration: true);

        // === ENGLISH ===
        RegisterTranslations("en", new Dictionary<string, string>
        {
            // Application
            ["app.name"] = "Open Asphalte",
            ["app.loaded"] = "Plugin loaded",
            ["app.version"] = "Version",
            ["app.welcome"] = "Welcome to Open Asphalte",

            // Commands
            ["cmd.cancelled"] = "Command cancelled",
            ["cmd.error"] = "Error",
            ["cmd.success"] = "Operation successful",

            // Errors
            ["error.noDocument"] = "No active document",
            ["error.invalidSelection"] = "Invalid selection",
            ["error.moduleNotFound"] = "Module not found",

            // Selection
            ["select.point"] = "Select a point",
            ["select.object"] = "Select an object",
            ["select.polyline"] = "Select a polyline",
            ["select.line"] = "Select a line",

            // System
            ["system.name"] = "System",
            ["system.help"] = "Help",
            ["system.help.desc"] = "Shows the list of available commands",
            ["system.version"] = "Version",
            ["system.version.desc"] = "Shows version information",
            ["system.settings"] = "Settings",
            ["system.settings.desc"] = "Opens the settings window",
            ["system.reload"] = "Reload",
            ["system.reload.desc"] = "Reloads the configuration",
            ["system.update"] = "Update",
            ["system.update.desc"] = "Checks for available updates",
            ["system.modules"] = "Modules",
            ["system.modules.desc"] = "Opens the module manager",
            ["system.reload.success"] = "Configuration reloaded",
            ["system.help.title"] = "OPEN ASPHALTE - HELP",
            ["system.help.section.system"] = "SYSTEM COMMANDS",
            ["system.help.section.module"] = "MODULE",
            ["system.version.header"] = "OPEN ASPHALTE v{0}",
            ["system.version.subtitle"] = "Modular plugin for AutoCAD",
            ["system.version.modules"] = "Modules loaded: {0}",
            ["system.version.commands"] = "Commands available: {0}",
            ["system.version.language"] = "Language: {0}",
            ["system.version.devmode"] = "Dev mode: {0}",
            ["system.update.checking"] = "Checking for updates...",
            ["system.update.current"] = "Current version: {0}",
            ["system.update.opening"] = "Opening releases page...",
            ["system.update.url"] = "URL: {0}",
            ["system.update.openError"] = "Unable to open browser: {0}",

            // Startup update check
            ["update.available"] = "Update v{0} available!",
            ["update.incompatibleAutoCAD"] = "Version {0} requires AutoCAD {1}+ (you have {2})",
            ["update.notification.title"] = "Update Available",
            ["update.notification.message"] = "A new version of Open Asphalte is available!\n\nCurrent version: {1}\nNew version: {0}\n\nWould you like to open the download page?",
            ["update.checking.startup"] = "Checking for updates...",

            // Module Manager
            ["modules.manager.title"] = "Module Manager",
            ["modules.manager.subtitle"] = "Install and manage Open Asphalte modules from the official catalog.",
            ["modules.manager.refresh"] = "Refresh",
            ["modules.manager.filter"] = "Filter:",
            ["modules.manager.empty"] = "Click Refresh to load the catalog",
            ["modules.manager.loading"] = "Loading catalog...",
            ["modules.manager.noManifest"] = "Catalog empty or inaccessible",
            ["modules.manager.noMatch"] = "No modules match the filter",
            ["modules.manager.error"] = "Loading error",
            ["modules.manager.status"] = "{0} module(s) available, {1} installed, {2} update(s)",
            ["modules.manager.folder"] = "Modules folder",
            ["modules.manager.folderNotFound"] = "The modules folder does not exist yet.",
            ["modules.manager.downloading"] = "Downloading...",
            ["modules.manager.installing"] = "Installing {0}...",
            ["modules.manager.installed"] = "Module {0} installed successfully",
            ["modules.manager.installSuccess"] = "Installation successful",
            ["modules.manager.restartRequired"] = "Module {0} has been installed.\n\nRestart AutoCAD to activate it.",
            ["modules.manager.installError"] = "Installation error: {0}",
            ["modules.manager.retry"] = "Retry",
            ["modules.manager.openError"] = "Error opening manager: {0}",
            ["modules.manager.dependencies.title"] = "Required Dependencies",
            ["modules.manager.dependencies.confirm"] = "The module \"{0}\" requires {2} dependency(ies):\n\n• {1}\n\nDo you want to install them now?",
            ["modules.manager.dependencies.message"] = "The module \"{0}\" requires the following modules to work:\n\n{1}\n\nDo you want to install these modules now?",
            ["modules.manager.dependencies.single"] = "The module \"{0}\" requires the module \"{1}\" to work.\n\nDo you want to install it now?",
            ["modules.manager.dependencies.installing"] = "Installing dependencies...",
            ["modules.manager.dependencies.success"] = "Modules installed successfully: {0}",
            ["modules.manager.dependencies.notFound"] = "Dependency not found in catalog: {0}",
            ["modules.manager.restartRequired.multiple"] = "The following modules have been installed:\n{0}\n\nRestart AutoCAD to activate them.",
            ["modules.manager.category.installAll"] = "Install category",
            ["modules.manager.category.installCount"] = "Install all ({0})",
            ["modules.manager.category.confirmInstall"] = "Do you want to install the {0} module(s) from the \"{1}\" category?\n\n• {2}",
            ["modules.manager.category.allInstalled"] = "All modules in this category are already installed.",
            ["modules.manager.category.installed"] = "{0} module(s) installed from category {1}",
            ["modules.requires"] = "Requires",
            ["modules.filter.all"] = "All",
            ["modules.filter.installed"] = "Installed",
            ["modules.filter.available"] = "Available",
            ["modules.filter.updates"] = "Updates",
            ["modules.action.install"] = "Install",
            ["modules.action.update"] = "Update",
            ["modules.action.installed"] = "Installed",
            ["modules.local.description"] = "Local module from {0}",

            // Update errors
            ["update.error.notFound"] = "Module catalog not found (404). Check your connection or try again later.",
            ["update.error.forbidden"] = "Access to catalog denied (403).",
            ["update.error.noInternet"] = "Unable to contact the server. Check your Internet connection.",
            ["update.error.http"] = "Network error: {0}",
            ["update.error.timeout"] = "Request timed out. Server not responding.",
            ["update.moduleInstalled"] = "Module {0} installed",

            // First run
            ["firstrun.noModules.title"] = "Welcome to Open Asphalte!",
            ["firstrun.noModules.message"] = "No modules are installed.\n\nWould you like to open the Module Manager to install extensions?",

            // About window
            ["about.title"] = "About",
            ["about.subtitle"] = "Modular plugin for AutoCAD",
            ["about.version"] = "Version",
            ["about.buildDate"] = "Build date",
            ["about.channel"] = "Channel",
            ["about.framework"] = "Framework",
            ["about.modules"] = "Loaded modules",
            ["about.commands"] = "Commands",
            ["about.license"] = "This software is distributed under the Apache 2.0 license.\nSource code available on GitHub.",
            ["about.checkUpdate"] = "Check for updates",
            ["about.close"] = "Close",
            ["about.invalidUrl"] = "The update URL is invalid or insecure.",
            ["about.alphaWarning"] = "⚠ ALPHA VERSION - For testing only",
            ["about.betaWarning"] = "⚠ BETA VERSION - May contain bugs",
            ["about.reportBug"] = "Report a bug",
            ["about.reportBug.opening"] = "Opening GitHub issues page...",
            ["about.reportBug.error"] = "Unable to open issues page: {0}",

            // Settings window
            ["settings.title"] = "Open Asphalte Settings",
            ["settings.language"] = "Language",
            ["settings.devmode"] = "Developer mode (detailed logs)",
            ["settings.checkupdates"] = "Check for updates on startup",
            ["settings.save"] = "Save",
            ["settings.cancel"] = "Cancel",
            ["settings.languageChanged"] = "Language changed. Interface has been updated.",
            ["settings.info"] = "Version {0} • {1} module(s) • {2} command(s)\nConfiguration: {3}",
            ["settings.saveError"] = "Error while saving: {0}",
            ["settings.openError"] = "Error opening settings: {0}",
            ["settings.modules.status"] = "{0}/{1} modules installed",
            ["settings.modules.installSelected"] = "Install selection",
            ["settings.modules.selectedCount"] = "{0} module(s) selected",
            ["settings.modules.dependenciesRequired"] = "The following modules will also be installed (required dependencies):\n\n• {0}\n\nContinue?",
            ["settings.modules.confirmInstall"] = "Install {0} module(s)?\n\n• {1}",
            ["settings.modules.installTitle"] = "Installation",
            ["settings.modules.installed"] = "{0} module(s) installed",

            // Modules
            ["modules.loaded"] = "{0} module(s) loaded",
            ["modules.commands"] = "{0} command(s) available",

            // Common
            ["common.yes"] = "Yes",
            ["common.no"] = "No",
            ["common.success"] = "Success",
            ["common.error"] = "Error",

            // Configuration
            ["config.empty"] = "Empty config file, new configuration created",
            ["config.loaded"] = "Configuration loaded from {0}",
            ["config.created"] = "New configuration created",
            ["config.corrupt"] = "Corrupted config file, reset: {0}",
            ["config.loadError"] = "Error loading config: {0}",
            ["config.saved"] = "Configuration saved",
            ["config.saveError"] = "Error saving config: {0}",
            ["config.reloaded"] = "Configuration reloaded",
            ["config.migrating"] = "Migrating configuration from v{0} to v{1}...",
            ["config.migrated"] = "Configuration migrated successfully",

            // Modules discovery
            ["module.searchPath"] = "Searching modules in: {0}",
            ["module.folderCreated"] = "Modules folder created",
            ["module.dllFound"] = "{0} DLL file(s) found",
            ["module.dllFoundInPath"] = "{0} DLL file(s) found in {1}",
            ["module.loadOutside"] = "Attempt to load DLL outside Modules folder: {0}",
            ["module.dllMissing"] = "DLL file not found: {0}",
            ["module.loading"] = "Loading: {0}",
            ["module.noneFound"] = "No module found in {0}",
            ["module.duplicate"] = "Module '{0}' already loaded, skipped",
            ["module.loaded"] = "Module '{0}' v{1} loaded ({2} commands)",
            ["module.instanceError"] = "Error instantiating module {0}: {1}",
            ["module.dllError"] = "Error loading DLL {0}: {1}",
            ["module.depMissing"] = "Module '{0}' disabled: missing dependency '{1}'",
            ["module.coreVersionUnknown"] = "Module '{0}': Core version not verifiable (min='{1}', core='{2}')",
            ["module.coreVersionIncompatible"] = "Module '{0}' disabled: Core version required {1}, current version {2}",
            ["module.migrated"] = "Module migrated: {0} → {1}",
            ["module.summary"] = "{0} module(s) loaded, {1} command(s) available",
            ["module.initialized"] = "Module '{0}' initialized",
            ["module.initError"] = "Error initializing module '{0}': {1}",
            ["module.shutdown"] = "Module '{0}' closed",
            ["module.shutdownError"] = "Error closing module '{0}': {1}",
            ["module.pathAdded"] = "Modules path added: {0}",
            ["module.pathAddedTooLate"] = "Cannot add path after initialization",
            ["module.pathNotFound"] = "Modules path not found: {0}",
            ["module.unsigned"] = "⚠️ Unsigned module: {0} (not verified)",
            ["module.signed"] = "✓ Signed module: {0}",
            ["module.unsignedBlocked"] = "Unsigned module blocked: {0} (set 'allowUnsignedModules' to allow)",

            // UI
            ["ui.menu.created"] = "Open Asphalte menu created",
            ["ui.menu.createError"] = "Error creating menu: {0}",
            ["ui.ribbon.notAvailable"] = "AutoCAD ribbon not available",
            ["ui.ribbon.created"] = "Open Asphalte ribbon created",
            ["ui.ribbon.updated"] = "Ribbon updated (incremental)",
            ["ui.ribbon.createError"] = "Error creating ribbon: {0}",
            ["ui.ribbon.panelError"] = "Error creating panel {0}: {1}",

            // Plugin lifecycle
            ["plugin.updateCheckDisabled"] = "Update check disabled for v1 (no server)",
            ["plugin.initError"] = "Open Asphalte initialization error: {0}",
            ["plugin.uiCreated"] = "Open Asphalte interface created",
            ["plugin.uiCreateError"] = "Error creating interface: {0}",
            ["plugin.languageChange"] = "Language change: {0} -> {1}",
            ["plugin.uiUpdated"] = "Interface updated ({0})",
            ["plugin.uiUpdateError"] = "Error updating interface: {0}",
            ["plugin.shutdownClean"] = "Open Asphalte closed cleanly",

            // Welcome
            ["welcome.title"] = "OPEN ASPHALTE v{0}",
            ["welcome.subtitle"] = "Modular plugin for AutoCAD",
            ["welcome.modulesLoaded"] = "Modules loaded: {0}",
            ["welcome.noModules"] = "No module loaded",
            ["welcome.dropModules"] = "Place your modules (.dll) in the Modules/ folder",
            ["welcome.commandsAvailable"] = "{0} command(s) available",
            ["welcome.helpHint"] = "Type OAS_HELP for the full list",

            // Logs
            ["log.stack"] = "Stack: {0}",
            ["log.level.debug"] = "[DEBUG]",
            ["log.level.info"] = "[INFO]",
            ["log.level.success"] = "[OK]",
            ["log.level.warn"] = "[WARN]",
            ["log.level.error"] = "[ERROR]",

            // Errors (transactions)
            ["error.noDatabase"] = "Database not available",

            // Credits window
            ["core.credits.title"] = "Credits",
            ["core.credits.tab.core"] = "Core Team",
            ["core.credits.tab.modules"] = "Modules",
            ["core.credits.modules.list"] = "Installed Modules",
            ["core.credits.author"] = "Author",
            ["core.credits.contributors"] = "Contributors",
            ["about.credits"] = "Credits",
            ["core.close"] = "Close",

            // Settings tabs
            ["settings.tab.general"] = "General",
            ["settings.tab.modules"] = "Modules",
            ["settings.modules.check"] = "Check for updates",
            ["settings.modules.checking"] = "Checking...",
            ["settings.modules.coreAvailable"] = "A new Core version is available",
            ["settings.modules.coreUpdateMsg"] = "Version {0} available (current: {1})",
        }, isSystemRegistration: true);

        // === Español ===
        RegisterTranslations("es", new Dictionary<string, string>
        {
            // Aplicación
            ["app.name"] = "Open Asphalte",
            ["app.loaded"] = "Plugin cargado",
            ["app.version"] = "Versión",
            ["app.welcome"] = "Bienvenido a Open Asphalte",

            // Comandos
            ["cmd.cancelled"] = "Comando cancelado",
            ["cmd.error"] = "Error",
            ["cmd.success"] = "Operación exitosa",

            // Errores
            ["error.noDocument"] = "No hay documento activo",
            ["error.invalidSelection"] = "Selección inválida",
            ["error.moduleNotFound"] = "módulo no encontrado",

            // Selección
            ["select.point"] = "Seleccione un punto",
            ["select.object"] = "Seleccione un objeto",
            ["select.polyline"] = "Seleccione una polilínea",
            ["select.line"] = "Seleccione una línea",

            // Sistema
            ["system.name"] = "Sistema",
            ["system.help"] = "Ayuda",
            ["system.help.desc"] = "Muestra la lista de comandos disponibles",
            ["system.version"] = "Versión",
            ["system.version.desc"] = "Muestra información de Versión",
            ["system.settings"] = "Configuración",
            ["system.settings.desc"] = "Abre la ventana de Configuración",
            ["system.reload"] = "Recargar",
            ["system.reload.desc"] = "Recarga la Configuración",
            ["system.update"] = "Actualizar",
            ["system.update.desc"] = "Busca actualizaciones disponibles",
            ["system.modules"] = "Módulos",
            ["system.modules.desc"] = "Abre el gestor de módulos",
            ["system.reload.success"] = "Configuración recargada",
            ["system.help.title"] = "OPEN ASPHALTE - AYUDA",
            ["system.help.section.system"] = "COMANDOS DEL SISTEMA",
            ["system.help.section.module"] = "módulo",
            ["system.version.header"] = "OPEN ASPHALTE v{0}",
            ["system.version.subtitle"] = "Plugin modular para AutoCAD",
            ["system.version.modules"] = "módulos cargados: {0}",
            ["system.version.commands"] = "Comandos disponibles: {0}",
            ["system.version.language"] = "Idioma: {0}",
            ["system.version.devmode"] = "Modo dev: {0}",
            ["system.update.checking"] = "Buscando actualizaciones...",
            ["system.update.current"] = "Versión actual: {0}",
            ["system.update.opening"] = "Abriendo la página de releases...",
            ["system.update.url"] = "URL: {0}",
            ["system.update.openError"] = "No se puede abrir el navegador: {0}",

            // Verificación de actualizaciones al inicio
            ["update.available"] = "¡Actualización v{0} disponible!",
            ["update.incompatibleAutoCAD"] = "La versión {0} requiere AutoCAD {1}+ (tienes {2})",
            ["update.notification.title"] = "Actualización disponible",
            ["update.notification.message"] = "¡Una nueva versión de Open Asphalte está disponible!\n\nVersión actual: {1}\nNueva versión: {0}\n\n¿Desea abrir la página de descarga?",
            ["update.checking.startup"] = "Buscando actualizaciones...",

            // Gestor de Módulos
            ["modules.manager.title"] = "Gestor de Módulos",
            ["modules.manager.subtitle"] = "Instale y gestione los módulos de Open Asphalte desde el catálogo oficial.",
            ["modules.manager.refresh"] = "Actualizar",
            ["modules.manager.filter"] = "Filtrar:",
            ["modules.manager.empty"] = "Haga clic en Actualizar para cargar el catálogo",
            ["modules.manager.loading"] = "Cargando catálogo...",
            ["modules.manager.noManifest"] = "Catálogo vacío o inaccesible",
            ["modules.manager.noMatch"] = "Ningún módulo coincide con el filtro",
            ["modules.manager.error"] = "Error de carga",
            ["modules.manager.status"] = "{0} módulo(s) disponible(s), {1} instalado(s), {2} actualización(es)",
            ["modules.manager.folder"] = "Carpeta de módulos",
            ["modules.manager.folderNotFound"] = "La carpeta de módulos aún no existe.",
            ["modules.manager.downloading"] = "Descargando...",
            ["modules.manager.installing"] = "Instalando {0}...",
            ["modules.manager.installed"] = "Módulo {0} instalado correctamente",
            ["modules.manager.installSuccess"] = "Instalación exitosa",
            ["modules.manager.restartRequired"] = "El módulo {0} ha sido instalado.\n\nReinicie AutoCAD para activarlo.",
            ["modules.manager.installError"] = "Error de instalación: {0}",
            ["modules.manager.retry"] = "Reintentar",
            ["modules.manager.openError"] = "Error al abrir el gestor: {0}",
            ["modules.manager.dependencies.title"] = "Dependencias requeridas",
            ["modules.manager.dependencies.confirm"] = "El módulo \"{0}\" requiere {2} dependencia(s):\n\n• {1}\n\n¿Desea instalarlas ahora?",
            ["modules.manager.dependencies.message"] = "El módulo \"{0}\" requiere los siguientes módulos para funcionar:\n\n{1}\n\n¿Desea instalar estos módulos ahora?",
            ["modules.manager.dependencies.single"] = "El módulo \"{0}\" requiere el módulo \"{1}\" para funcionar.\n\n¿Desea instalarlo ahora?",
            ["modules.manager.dependencies.installing"] = "Instalando dependencias...",
            ["modules.manager.dependencies.success"] = "Módulos instalados correctamente: {0}",
            ["modules.manager.dependencies.notFound"] = "Dependencia no encontrada en el catálogo: {0}",
            ["modules.manager.restartRequired.multiple"] = "Los siguientes módulos han sido instalados:\n{0}\n\nReinicie AutoCAD para activarlos.",
            ["modules.manager.category.installAll"] = "Instalar categoría",
            ["modules.manager.category.installCount"] = "Instalar todo ({0})",
            ["modules.manager.category.confirmInstall"] = "¿Desea instalar los {0} módulo(s) de la categoría \"{1}\"?\n\n• {2}",
            ["modules.manager.category.allInstalled"] = "Todos los módulos de esta categoría ya están instalados.",
            ["modules.manager.category.installed"] = "{0} módulo(s) instalado(s) desde la categoría {1}",
            ["modules.requires"] = "Requiere",
            ["modules.filter.all"] = "Todos",
            ["modules.filter.installed"] = "Instalados",
            ["modules.filter.available"] = "Disponibles",
            ["modules.filter.updates"] = "Actualizaciones",
            ["modules.action.install"] = "Instalar",
            ["modules.action.update"] = "Actualizar",
            ["modules.action.installed"] = "Instalado",
            ["modules.local.description"] = "Módulo local desde {0}",

            // Update errors
            ["update.error.notFound"] = "Catálogo de módulos no encontrado (404). Verifique su conexión o inténtelo más tarde.",
            ["update.error.forbidden"] = "Acceso al catálogo denegado (403).",
            ["update.error.noInternet"] = "No se puede contactar el servidor. Verifique su conexión a Internet.",
            ["update.error.http"] = "Error de red: {0}",
            ["update.error.timeout"] = "Tiempo de espera agotado. El servidor no responde.",
            ["update.moduleInstalled"] = "Módulo {0} instalado",

            // First run
            ["firstrun.noModules.title"] = "¡Bienvenido a Open Asphalte!",
            ["firstrun.noModules.message"] = "No hay módulos instalados.\n\n¿Desea abrir el Gestor de Módulos para instalar extensiones?",

            // Ventana Acerca de
            ["about.title"] = "Acerca de",
            ["about.subtitle"] = "Plugin modular para AutoCAD",
            ["about.version"] = "Versión",
            ["about.buildDate"] = "Fecha de compilación",
            ["about.channel"] = "Canal",
            ["about.framework"] = "Framework",
            ["about.modules"] = "Módulos cargados",
            ["about.commands"] = "Comandos",
            ["about.license"] = "Este software se distribuye bajo la licencia Apache 2.0.\nCódigo fuente disponible en GitHub.",
            ["about.checkUpdate"] = "Buscar actualizaciones",
            ["about.close"] = "Cerrar",
            ["about.invalidUrl"] = "La URL de actualización es inválida o insegura.",
            ["about.alphaWarning"] = "⚠ VERSIÓN ALPHA - Solo para pruebas",
            ["about.betaWarning"] = "⚠ VERSIÓN BETA - Puede contener errores",
            ["about.reportBug"] = "Reportar un error",
            ["about.reportBug.opening"] = "Abriendo la página de issues de GitHub...",
            ["about.reportBug.error"] = "No se puede abrir la página de issues: {0}",

            // Ventana de Configuración
            ["settings.title"] = "Configuración Open Asphalte",
            ["settings.language"] = "Idioma",
            ["settings.devmode"] = "Modo desarrollador (logs detallados)",
            ["settings.checkupdates"] = "Buscar actualizaciones al iniciar",
            ["settings.save"] = "Guardar",
            ["settings.cancel"] = "Cancelar",
            ["settings.languageChanged"] = "Idioma cambiado. La interfaz ha sido actualizada.",
            ["settings.info"] = "Versión {0} • {1} módulo(s) • {2} comando(s)\nConfiguración: {3}",
            ["settings.saveError"] = "Error al guardar: {0}",
            ["settings.openError"] = "Error al abrir Configuración: {0}",
            ["settings.modules.status"] = "{0}/{1} módulos instalados",
            ["settings.modules.installSelected"] = "Instalar selección",
            ["settings.modules.selectedCount"] = "{0} módulo(s) seleccionado(s)",
            ["settings.modules.dependenciesRequired"] = "Los siguientes módulos también se instalarán (dependencias requeridas):\n\n• {0}\n\n¿Continuar?",
            ["settings.modules.confirmInstall"] = "¿Instalar {0} módulo(s)?\n\n• {1}",
            ["settings.modules.installTitle"] = "Instalación",
            ["settings.modules.installed"] = "{0} módulo(s) instalado(s)",

            // módulos
            ["modules.loaded"] = "{0} módulo(s) cargado(s)",
            ["modules.commands"] = "{0} comando(s) disponible(s)",

            // Common
            ["common.yes"] = "Sí",
            ["common.no"] = "No",
            ["common.success"] = "Éxito",
            ["common.error"] = "Error",

            // Configuración
            ["config.empty"] = "Archivo de Configuración vacío, nueva Configuración creada",
            ["config.loaded"] = "Configuración cargada desde {0}",
            ["config.created"] = "Nueva Configuración creada",
            ["config.corrupt"] = "Archivo de Configuración corrupto, reinicialización: {0}",
            ["config.loadError"] = "Error al cargar la Configuración: {0}",
            ["config.saved"] = "Configuración guardada",
            ["config.saveError"] = "Error al guardar la Configuración: {0}",
            ["config.reloaded"] = "Configuración recargada",

            // Descubrimiento de módulos
            ["module.searchPath"] = "Buscando módulos en: {0}",
            ["module.folderCreated"] = "Carpeta Modules creada",
            ["module.dllFound"] = "{0} archivo(s) DLL encontrado(s)",
            ["module.loadOutside"] = "Intento de cargar una DLL fuera de la carpeta Modules: {0}",
            ["module.dllMissing"] = "Archivo DLL no encontrado: {0}",
            ["module.loading"] = "Cargando: {0}",
            ["module.noneFound"] = "No se encontró ningún módulo en {0}",
            ["module.duplicate"] = "Módulo '{0}' ya cargado, ignorado",
            ["module.loaded"] = "Módulo '{0}' v{1} cargado ({2} comandos)",
            ["module.instanceError"] = "Error al instanciar el módulo {0}: {1}",
            ["module.dllError"] = "Error al cargar la DLL {0}: {1}",
            ["module.depMissing"] = "Módulo '{0}' desactivado: dependencia faltante '{1}'",
            ["module.coreVersionUnknown"] = "Módulo '{0}': Versión del Core no verificable (min='{1}', core='{2}')",
            ["module.coreVersionIncompatible"] = "Módulo '{0}' desactivado: Versión del Core requerida {1}, Versión actual {2}",
            ["module.migrated"] = "Módulo migrado: {0} → {1}",
            ["module.summary"] = "{0} módulo(s) cargado(s), {1} comando(s) disponible(s)",
            ["module.initialized"] = "Módulo '{0}' inicializado",
            ["module.initError"] = "Error al inicializar el módulo '{0}': {1}",
            ["module.shutdown"] = "Módulo '{0}' cerrado",
            ["module.shutdownError"] = "Error al cerrar el módulo '{0}': {1}",

            // UI
            ["ui.menu.created"] = "Menú Open Asphalte creado",
            ["ui.menu.createError"] = "Error al crear el menú: {0}",
            ["ui.ribbon.notAvailable"] = "Cinta de AutoCAD no disponible",
            ["ui.ribbon.created"] = "Cinta Open Asphalte creada",
            ["ui.ribbon.updated"] = "Cinta actualizada (incremental)",
            ["ui.ribbon.createError"] = "Error al crear la cinta: {0}",
            ["ui.ribbon.panelError"] = "Error al crear el panel {0}: {1}",

            // Ciclo de vida del plugin
            ["plugin.updateCheckDisabled"] = "Verificación de actualizaciones desactivada para la v1 (sin servidor)",
            ["plugin.initError"] = "Error de inicialización de Open Asphalte: {0}",
            ["plugin.uiCreated"] = "Interfaz de Open Asphalte creada",
            ["plugin.uiCreateError"] = "Error al crear la interfaz: {0}",
            ["plugin.languageChange"] = "Cambio de idioma: {0} -> {1}",
            ["plugin.uiUpdated"] = "Interfaz actualizada ({0})",
            ["plugin.uiUpdateError"] = "Error al actualizar la interfaz: {0}",
            ["plugin.shutdownClean"] = "Open Asphalte se cerró correctamente",

            // Bienvenida
            ["welcome.title"] = "OPEN ASPHALTE v{0}",
            ["welcome.subtitle"] = "Plugin modular para AutoCAD",
            ["welcome.modulesLoaded"] = "módulos cargados: {0}",
            ["welcome.noModules"] = "Ningún módulo cargado",
            ["welcome.dropModules"] = "Coloque sus módulos (.dll) en la carpeta Modules/",
            ["welcome.commandsAvailable"] = "{0} comando(s) disponible(s)",
            ["welcome.helpHint"] = "Escriba OAS_HELP para la lista completa",

            // Logs
            ["log.stack"] = "Pila: {0}",
            ["log.level.debug"] = "[DEPURAR]",
            ["log.level.info"] = "[INFO]",
            ["log.level.success"] = "[OK]",
            ["log.level.warn"] = "[ADVERTENCIA]",
            ["log.level.error"] = "[ERROR]",

            // Configuración (nuevas claves)
            ["config.migrating"] = "Migrando configuración de v{0} a v{1}...",
            ["config.migrated"] = "Configuración migrada correctamente",

            // Módulos (nuevas claves)
            ["module.dllFoundInPath"] = "{0} archivo(s) DLL encontrado(s) en {1}",
            ["module.pathAdded"] = "Ruta de módulos añadida: {0}",
            ["module.pathAddedTooLate"] = "No se puede añadir ruta después de la inicialización",
            ["module.pathNotFound"] = "Ruta de módulos no encontrada: {0}",
            ["module.unsigned"] = "⚠️ Módulo sin firmar: {0} (no verificado)",
            ["module.signed"] = "✓ Módulo firmado: {0}",
            ["module.unsignedBlocked"] = "Módulo sin firmar bloqueado: {0} (configure 'allowUnsignedModules' para permitir)",

            // Errores (transacciones)
            ["error.noDatabase"] = "Base de datos no disponible",

            // Ventana de créditos
            ["core.credits.title"] = "Créditos",
            ["core.credits.tab.core"] = "Equipo Core",
            ["core.credits.tab.modules"] = "Módulos",
            ["core.credits.modules.list"] = "Módulos instalados",
            ["core.credits.author"] = "Autor",
            ["core.credits.contributors"] = "Colaboradores",
            ["about.credits"] = "Créditos",
            ["core.close"] = "Cerrar",

            // Pestañas de configuración
            ["settings.tab.general"] = "General",
            ["settings.tab.modules"] = "Módulos",
            ["settings.modules.check"] = "Buscar actualizaciones",
            ["settings.modules.checking"] = "Verificando...",
            ["settings.modules.coreAvailable"] = "Una nueva versión del Core está disponible",
            ["settings.modules.coreUpdateMsg"] = "Versión {0} disponible (actual: {1})",
        }, isSystemRegistration: true);
    }

    /// <summary>
    /// Enregistre des traductions pour une langue.
    /// Les clés système (préfixes: app., cmd., error., etc.) ne peuvent pas être écrasées par les modules.
    /// </summary>
    /// <param name="language">Code langue (fr, en, es)</param>
    /// <param name="translations">Dictionnaire de traductions</param>
    /// <param name="isSystemRegistration">True si c'est l'initialisation système (autorise toutes les clés)</param>
    public static void RegisterTranslations(string language, IDictionary<string, string> translations, bool isSystemRegistration = false)
    {
        var langDict = _translations.GetOrAdd(language, _ => new ConcurrentDictionary<string, string>());

        foreach (var (key, value) in translations)
        {
            // Protéger les clés système contre l'écrasement par les modules
            if (!isSystemRegistration && IsSystemKey(key))
            {
                // Vérifier si la clé existe déjà (protection contre écrasement)
                if (langDict.ContainsKey(key))
                {
                    Logging.Logger.Warning($"Tentative d'écrasement de clé système bloquée: {key}");
                    continue;
                }
            }
            langDict[key] = value;
        }
    }

    /// <summary>
    /// Vérifie si une clé est une clé système protégée
    /// </summary>
    private static bool IsSystemKey(string key)
    {
        return _systemKeyPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Traduit une clé dans la langue active
    /// </summary>
    /// <param name="key">clé de traduction</param>
    /// <param name="defaultValue">Valeur par défaut si non trouvée</param>
    /// <returns>Texte traduit</returns>
    public static string T(string key, string? defaultValue = null)
    {
        var lang = CurrentLanguage;

        // Chercher dans la langue active
        if (_translations.TryGetValue(lang, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        // Fallback vers Français
        if (lang != "fr" && _translations.TryGetValue("fr", out var frDict))
        {
            if (frDict.TryGetValue(key, out var frValue))
            {
                return frValue;
            }
        }

        return defaultValue ?? key;
    }

    /// <summary>
    /// Traduit avec des paramètres formatés et un fallback.
    /// Utilise le fallback si la clé n'existe pas, puis formate avec les arguments.
    /// </summary>
    /// <param name="key">Clé de traduction</param>
    /// <param name="fallback">Texte par défaut si la clé n'existe pas (doit contenir {0}, {1}... pour le formatage)</param>
    /// <param name="args">Arguments de formatage</param>
    /// <returns>Texte traduit et formaté</returns>
    public static string TFormat(string key, string fallback, params object[] args)
    {
        var template = T(key, fallback);
        if (args == null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // Si le format échoue, retourner le template tel quel
            return template;
        }
    }

    /// <summary>
    /// Traduit avec des Paramètres formatés (sans fallback).
    /// Note: Préférez TFormat(key, fallback, args) pour plus de robustesse.
    /// </summary>
    /// <param name="key">clé de traduction</param>
    /// <param name="args">Arguments de formatage (au moins 1 élément requis)</param>
    /// <returns>Texte traduit et formaté</returns>
    public static string TFormat(string key, params object[] args)
    {
        if (args == null || args.Length == 0)
        {
            return T(key);
        }

        var template = T(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // Si le format échoue, retourner le template tel quel
            return template;
        }
    }

    /// <summary>
    /// Change la langue active et notifie les abonnés.
    /// </summary>
    /// <param name="language">Code langue (fr, en, es)</param>
    /// <param name="saveToConfig">Si true, sauvegarde dans la configuration</param>
    /// <returns>True si la langue a changé, false sinon</returns>
    /// <remarks>
    /// Cette méthode déclenche l'événement <see cref="OnLanguageChanged"/> si la langue change,
    /// permettant à l'UI de se reconstruire automatiquement.
    /// </remarks>
    public static bool SetLanguage(string language, bool saveToConfig = true)
    {
        if (!_supportedLanguages.Contains(language))
        {
            return false;
        }

        lock (_languageLock)
        {
            if (_currentLanguage == language)
            {
                return false;
            }

            var oldLanguage = _currentLanguage;
            _currentLanguage = language;

            // Sauvegarder dans la configuration si demandé
            if (saveToConfig)
            {
                Configuration.Configuration.Set("language", language);
                Configuration.Configuration.Save();
            }

            // Déclencher l'événement de changement de langue
            OnLanguageChanged?.Invoke(oldLanguage, language);
        }

        return true;
    }

    /// <summary>
    /// Vérifie si une langue est supportée
    /// </summary>
    /// <param name="language">Code langue à vérifier</param>
    /// <returns>True si la langue est supportée</returns>
    public static bool IsLanguageSupported(string language)
    {
        return _supportedLanguages.Contains(language);
    }

    /// <summary>
    /// Obtient toutes les clés de traduction enregistrées pour une langue
    /// </summary>
    /// <param name="language">Code langue (optionnel, utilise la langue courante si null)</param>
    /// <returns>Liste des clés de traduction</returns>
    public static IReadOnlyCollection<string> GetAllKeys(string? language = null)
    {
        var lang = language ?? _currentLanguage;
        if (_translations.TryGetValue(lang, out var dict))
        {
            return dict.Keys.ToArray();
        }
        return Array.Empty<string>();
    }
}
