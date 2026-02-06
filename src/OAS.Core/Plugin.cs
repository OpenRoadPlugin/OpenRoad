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

using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Autodesk.AutoCAD.Runtime;
using OpenAsphalte.Commands;
using OpenAsphalte.Configuration;
using OpenAsphalte.Discovery;
using OpenAsphalte.Diagnostics;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using OpenAsphalte.UI;
using L10n = OpenAsphalte.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

// Enregistrer la classe de commandes syst�me
[assembly: CommandClass(typeof(SystemCommands))]

// Point d'entr�e du plugin
[assembly: ExtensionApplication(typeof(OpenAsphalte.Plugin))]

namespace OpenAsphalte;

/// <summary>
/// Point d'entr�e principal du plugin Open Asphalte pour AutoCAD.
/// G�re le cycle de vie de l'application et la d�couverte automatique des modules.
/// </summary>
/// <remarks>
/// <para>
/// Open Asphalte est un plugin modulaire. Le coeur (cette DLL) fournit :
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
    private static EventHandler<FirstChanceExceptionEventArgs>? _firstChanceHandler;
    private static EventHandler? _updateIdleHandler;

    /// <summary>
    /// Version du coeur Open Asphalte (charg�e depuis version.json)
    /// </summary>
    public static string Version => _version.Value;

    /// <summary>
    /// Nom de l'application
    /// </summary>
    public static string AppName => "Open Asphalte";

    /// <summary>
    /// Chemin du dossier contenant OAS.Core.dll
    /// </summary>
    public static string BasePath { get; private set; } = "";

    private static volatile bool _idleEventSubscribed;
    private static readonly object _idleLock = new();

    /// <summary>
    /// Charge la version depuis version.json de mani�re thread-safe
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
                    return versionElement.GetString() ?? "0.0.3";
                }
            }
        }
        catch (System.Exception ex)
        {
            // En cas d'erreur, utiliser la version par d�faut
            StartupLog.Write($"LoadVersionFromJson error: {ex.Message}");
        }

        return "0.0.3";
    }

    /// <summary>
    /// Appel� au chargement du plugin par AutoCAD
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

            // 2. La version est charg�e automatiquement via Lazy<T>

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
                // V�rification asynchrone non-bloquante
                _ = CheckForUpdatesOnStartupAsync();
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
    /// Active le journal fichier et capture les exceptions non g�r�es.
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
                // Fallback: �crire dans StartupLog si Logger �choue
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

        _firstChanceHandler = (_, args) =>
        {
            try
            {
                var count = Interlocked.Increment(ref _firstChanceCount);
                if (count > MaxFirstChanceLogs) return;
                StartupLog.Write($"FirstChanceException[{count}/{MaxFirstChanceLogs}]: {args.Exception}");
            }
            catch
            {
                // Silencieux: �viter boucle infinie si StartupLog �choue
            }
        };
        AppDomain.CurrentDomain.FirstChanceException += _firstChanceHandler;

        Logger.Info($"Diagnostics activ�s. Log: {Logger.LogFilePath}");
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
                text = string.Concat(text.AsSpan(0, innerWidth - 4), "...");
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

            // Proposer d'ouvrir le gestionnaire de modules si aucun module n'est install�
            CheckFirstRunNoModules();
        }
        catch (System.Exception ex)
        {
            StartupLog.Write($"Idle error: {ex}");
            Logger.Error(L10n.TFormat("plugin.uiCreateError", ex.Message));
        }
    }

    /// <summary>
    /// V�rifie si c'est le premier d�marrage sans modules et propose d'ouvrir le gestionnaire
    /// </summary>
    private static void CheckFirstRunNoModules()
    {
        try
        {
            // V�rifier si des modules sont charg�s
            if (ModuleDiscovery.Modules.Count > 0)
                return;

            // Afficher la proposition � chaque d�marrage si aucun module
            var result = System.Windows.MessageBox.Show(
                L10n.T("firstrun.noModules.message"),
                L10n.T("firstrun.noModules.title"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Ouvrir les param�tres directement sur l'onglet Modules
                var window = new SettingsWindow(1); // 1 = index de l'onglet Modules
                AcadApp.ShowModalWindow(window);
            }
        }
        catch (System.Exception ex)
        {
            StartupLog.Write($"CheckFirstRunNoModules error: {ex.Message}");
            // Ne pas bloquer le d�marrage
        }
    }

    /// <summary>
    /// V�rifie les mises � jour au d�marrage de mani�re asynchrone.
    /// Cette m�thode est non-bloquante et s'ex�cute en arri�re-plan.
    /// </summary>
    private static async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            StartupLog.Write("CheckForUpdatesOnStartupAsync: begin");
            Logger.Debug(L10n.T("update.checking.startup"));

            // Attendre un peu pour ne pas ralentir le d�marrage
            await Task.Delay(2000);

            // V�rifier les mises � jour via GitHub API
            var result = await UpdateService.CheckStartupUpdateAsync();

            if (result.UpdateAvailable)
            {
                // Afficher la notification sur le thread UI (handler auto-d�tach�)
                _updateIdleHandler = (_, _) =>
                {
                    if (_updateIdleHandler != null)
                    {
                        AcadApp.Idle -= _updateIdleHandler;
                        _updateIdleHandler = null;
                    }
                    UpdateService.ShowUpdateNotification(result);
                };
                AcadApp.Idle += _updateIdleHandler;
            }
            else if (result.IncompatibleAutoCAD)
            {
                Logger.Info(L10n.TFormat("update.incompatibleAutoCAD",
                    result.LatestVersion?.ToString() ?? "?",
                    result.RequiredAutoCADVersion,
                    UpdateService.GetAutoCADVersion()));
            }
            else if (result.IsUpToDate)
            {
                Logger.Debug($"Open Asphalte is up to date (v{result.CurrentVersion})");
            }

            StartupLog.Write("CheckForUpdatesOnStartupAsync: end");
        }
        catch (System.Exception ex)
        {
            // Ne jamais bloquer le d�marrage pour une erreur de mise � jour
            StartupLog.Write($"CheckForUpdatesOnStartupAsync error (ignored): {ex.Message}");
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

            // 2b. Se desabonner du handler FirstChanceException
            if (_firstChanceHandler != null)
            {
                AppDomain.CurrentDomain.FirstChanceException -= _firstChanceHandler;
                _firstChanceHandler = null;
            }

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
            // Log silencieux � la fermeture pour �viter les blocages
            StartupLog.Write($"Terminate error (ignored): {ex.Message}");
        }
    }
}
