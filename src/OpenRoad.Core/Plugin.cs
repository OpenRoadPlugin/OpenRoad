// Copyright 2026 Open Road Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Autodesk.AutoCAD.Runtime;
using OpenRoad.Commands;
using OpenRoad.Configuration;
using OpenRoad.Discovery;
using OpenRoad.Diagnostics;
using OpenRoad.Logging;
using OpenRoad.UI;
using L10n = OpenRoad.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

// Enregistrer la classe de commandes système
[assembly: CommandClass(typeof(SystemCommands))]

// Point d'entrée du plugin
[assembly: ExtensionApplication(typeof(OpenRoad.Plugin))]

namespace OpenRoad;

/// <summary>
/// Point d'entrée principal du plugin Open Road pour AutoCAD.
/// Gère le cycle de vie de l'application et la découverte automatique des modules.
/// </summary>
/// <remarks>
/// <para>
/// Open Road est un plugin modulaire. Le coeur (cette DLL) fournit :
/// - La decouverte automatique des modules
/// - Le systeme de menus et rubans dynamiques
/// - Les services partages (geometrie, calques, coordonnees)
/// - Les commandes systeme (aide, version, parametres)
/// </para>
/// <para>
/// Les modules sont des DLL separees placees dans le dossier Modules/.
/// Ils sont decouverts et charges automatiquement au demarrage.
/// </para>
/// </remarks>
public class Plugin : IExtensionApplication
{
    private static readonly Lazy<string> _version = new(LoadVersionFromJson);
    private static bool _diagnosticsEnabled;
    private static int _firstChanceCount;
    private const int MaxFirstChanceLogs = 5;
    
    /// <summary>
    /// Version du coeur Open Road (chargée depuis version.json)
    /// </summary>
    public static string Version => _version.Value;
    
    /// <summary>
    /// Nom de l'application
    /// </summary>
    public static string AppName => "Open Road";
    
    /// <summary>
    /// Chemin du dossier contenant OpenRoad.Core.dll
    /// </summary>
    public static string BasePath { get; private set; } = "";
    
    private static bool _idleEventSubscribed = false;
    private static readonly object _idleLock = new object();
    
    /// <summary>
    /// Charge la version depuis version.json de manière thread-safe
    /// </summary>
    private static string LoadVersionFromJson()
    {
        try
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var versionFile = Path.Combine(basePath, "version.json");
            
            if (File.Exists(versionFile))
            {
                var json = File.ReadAllText(versionFile);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("version", out var versionElement))
                {
                    return versionElement.GetString() ?? "0.0.1";
                }
            }
        }
        catch (System.Exception ex)
        {
            // En cas d'erreur, utiliser la version par défaut
            StartupLog.Write($"LoadVersionFromJson error: {ex.Message}");
        }
        
        return "0.0.1";
    }
    
    /// <summary>
    /// Appelé au chargement du plugin par AutoCAD
    /// </summary>
    public void Initialize()
    {
        try
        {
            StartupLog.Write("Initialize: begin");
            EnableDiagnostics();
            StartupLog.Write("Diagnostics enabled");
            // 1. Determiner le chemin de base
            BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            StartupLog.Write($"BasePath: {BasePath}");
            
            // 2. La version est chargée automatiquement via Lazy<T>
            
            // 3. Charger la configuration utilisateur
            Configuration.Configuration.Load();
            Logger.DebugMode = Configuration.Configuration.DevMode;
            StartupLog.Write("Configuration loaded");
            
            // 4. Initialiser le systeme de localisation
            L10n.Initialize();
            StartupLog.Write("Localization initialized");
            
            // 5. Decouvrir et charger les modules (DLL separees)
            ModuleDiscovery.DiscoverAndLoad(BasePath);
            StartupLog.Write("Module discovery completed");
            
            // 6. Initialiser tous les modules decouverts
            ModuleDiscovery.InitializeAll();
            StartupLog.Write("Modules initialized");
            
            // 7. Afficher le message de bienvenue
            ShowWelcomeMessage();
            StartupLog.Write("Welcome message displayed");
            
            // 8. Creer le menu et ruban une fois AutoCAD pret
            _idleEventSubscribed = true;
            AcadApp.Idle += OnApplicationIdle;
            StartupLog.Write("Idle event subscribed");
            
            // 9. Verification des mises a jour au demarrage (si active)
            if (Configuration.Configuration.CheckUpdatesOnStartup)
            {
                Logger.Debug(L10n.T("plugin.updateCheckDisabled"));
                // TODO: Implementer la verification automatique quand le serveur sera disponible
            }
            StartupLog.Write("Initialize: end");
        }
        catch (System.Exception ex)
        {
            StartupLog.Write($"Initialize error: {ex}");
            Logger.Error(L10n.TFormat("plugin.initError", ex.Message));
        }
    }

    /// <summary>
    /// Active le journal fichier et capture les exceptions non gérées.
    /// </summary>
    private static void EnableDiagnostics()
    {
        if (_diagnosticsEnabled) return;
        _diagnosticsEnabled = true;

        Logger.FileLoggingEnabled = true;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                var ex = args.ExceptionObject as System.Exception;
                var msg = ex != null ? ex.ToString() : "Unhandled exception";
                Logger.Error(msg);
            }
            catch (System.Exception logEx)
            {
                // Fallback: écrire dans StartupLog si Logger échoue
                StartupLog.Write($"UnhandledException logging failed: {logEx.Message}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            try
            {
                Logger.Error(args.Exception.ToString());
                args.SetObserved();
            }
            catch (System.Exception logEx)
            {
                StartupLog.Write($"UnobservedTaskException logging failed: {logEx.Message}");
            }
        };

        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            try
            {
                if (_firstChanceCount >= MaxFirstChanceLogs) return;
                _firstChanceCount++;
                StartupLog.Write($"FirstChanceException[{_firstChanceCount}/{MaxFirstChanceLogs}]: {args.Exception}");
            }
            catch
            {
                // Silencieux: éviter boucle infinie si StartupLog échoue
            }
        };

        Logger.Info($"Diagnostics activés. Log: {Logger.LogFilePath}");
    }
    
    /// <summary>
    /// Affiche le message de bienvenue dans la console AutoCAD
    /// </summary>
    private void ShowWelcomeMessage()
    {
        var ed = AcadApp.DocumentManager.MdiActiveDocument?.Editor;
        if (ed == null) return;

        const int innerWidth = 68;
        string Line(string text)
        {
            if (text.Length > innerWidth - 1)
            {
                text = text.Substring(0, innerWidth - 4) + "...";
            }
            return $"| {text.PadRight(innerWidth - 1)}|";
        }
        
        var modules = ModuleDiscovery.Modules;
        var commands = ModuleDiscovery.AllCommands;
        
        ed.WriteMessage("\n");
        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        ed.WriteMessage($"\n{Line(L10n.TFormat("welcome.title", Version))}");
        ed.WriteMessage($"\n{Line(L10n.T("welcome.subtitle"))}");
        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        
        if (modules.Count > 0)
        {
            ed.WriteMessage($"\n{Line(L10n.TFormat("welcome.modulesLoaded", modules.Count))}");
            foreach (var module in modules)
            {
                var info = $"  - {module.Name} v{module.Version}";
                ed.WriteMessage($"\n{Line(info)}");
            }
        }
        else
        {
            ed.WriteMessage($"\n{Line(L10n.T("welcome.noModules"))}");
            ed.WriteMessage($"\n{Line(L10n.T("welcome.dropModules"))}");
        }
        
        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        ed.WriteMessage($"\n{Line(L10n.TFormat("welcome.commandsAvailable", commands.Count))}");
        ed.WriteMessage($"\n{Line(L10n.T("welcome.helpHint"))}");
        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        ed.WriteMessage("\n");
    }
    
    /// <summary>
    /// Cree le menu et le ruban une fois AutoCAD completement charge
    /// </summary>
    private void OnApplicationIdle(object? sender, EventArgs e)
    {
        lock (_idleLock)
        {
            if (!_idleEventSubscribed) return;
            
            AcadApp.Idle -= OnApplicationIdle;
            _idleEventSubscribed = false;
        }
        
        try
        {
            StartupLog.Write("Idle: begin UI creation");
            // Creer le menu contextuel
            MenuBuilder.CreateMenu();
            StartupLog.Write("Menu created");
            
            // Creer le ruban
            RibbonBuilder.CreateRibbon();
            StartupLog.Write("Ribbon created");
            
            // S'abonner a l'evenement de changement de langue
            L10n.OnLanguageChanged += OnLanguageChanged;
            StartupLog.Write("Language change subscribed");
            
            Logger.Debug(L10n.T("plugin.uiCreated"));
            
            // Proposer d'ouvrir le gestionnaire de modules si aucun module n'est installé
            CheckFirstRunNoModules();
        }
        catch (System.Exception ex)
        {
            StartupLog.Write($"Idle error: {ex}");
            Logger.Error(L10n.TFormat("plugin.uiCreateError", ex.Message));
        }
    }
    
    /// <summary>
    /// Vérifie si c'est le premier démarrage sans modules et propose d'ouvrir le gestionnaire
    /// </summary>
    private static void CheckFirstRunNoModules()
    {
        try
        {
            // Vérifier si des modules sont chargés
            if (ModuleDiscovery.Modules.Count > 0)
                return;
            
            // Vérifier si on a déjà proposé (via config)
            var hasShownPrompt = Configuration.Configuration.Get("firstRunModulePromptShown", false);
            if (hasShownPrompt)
                return;
            
            // Marquer comme affiché pour ne pas répéter
            Configuration.Configuration.Set("firstRunModulePromptShown", true);
            Configuration.Configuration.Save();
            
            // Afficher la proposition
            var result = System.Windows.MessageBox.Show(
                L10n.T("firstrun.noModules.message"),
                L10n.T("firstrun.noModules.title"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Ouvrir le gestionnaire de modules
                var window = new ModuleManagerWindow();
                AcadApp.ShowModalWindow(window);
            }
        }
        catch (System.Exception ex)
        {
            StartupLog.Write($"CheckFirstRunNoModules error: {ex.Message}");
            // Ne pas bloquer le démarrage
        }
    }
    
    /// <summary>
    /// Appele lorsque la langue change.
    /// Reconstruit l'interface utilisateur avec les nouvelles traductions.
    /// </summary>
    /// <param name="oldLanguage">Ancienne langue</param>
    /// <param name="newLanguage">Nouvelle langue</param>
    private static void OnLanguageChanged(string oldLanguage, string newLanguage)
    {
        try
        {
            Logger.Debug(L10n.TFormat("plugin.languageChange", oldLanguage, newLanguage));
            
            // Reconstruire le menu et le ruban avec les nouvelles traductions
            MenuBuilder.RebuildMenu();
            RibbonBuilder.RebuildRibbon();
            
            Logger.Info(L10n.TFormat("plugin.uiUpdated", L10n.LanguageNames.GetValueOrDefault(newLanguage, newLanguage)));
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("plugin.uiUpdateError", ex.Message));
        }
    }
    
    /// <summary>
    /// Appele a la fermeture d'AutoCAD
    /// </summary>
    public void Terminate()
    {
        try
        {
            // 1. Se desabonner de l'evenement Idle (avec lock)
            lock (_idleLock)
            {
                if (_idleEventSubscribed)
                {
                    AcadApp.Idle -= OnApplicationIdle;
                    _idleEventSubscribed = false;
                }
            }
            
            // 2. Se desabonner de l'evenement de langue
            L10n.OnLanguageChanged -= OnLanguageChanged;
            
            // 3. Supprimer l'interface
            MenuBuilder.RemoveMenu();
            RibbonBuilder.RemoveRibbon();
            
            // 4. Fermer tous les modules
            ModuleDiscovery.ShutdownAll();
            
            // 5. Sauvegarder la configuration
            Configuration.Configuration.Save();
            
            // 6. Nettoyer les evenements
            Configuration.Configuration.ClearEventSubscribers();
            L10n.ClearEventSubscribers();
            
            Logger.Debug(L10n.T("plugin.shutdownClean"));
        }
        catch (System.Exception ex)
        {
            // Log silencieux à la fermeture pour éviter les blocages
            StartupLog.Write($"Terminate error (ignored): {ex.Message}");
        }
    }
}
