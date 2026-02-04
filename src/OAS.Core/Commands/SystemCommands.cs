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

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Configuration;
using OpenAsphalte.Discovery;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using OpenAsphalte.UI;
using L10n = OpenAsphalte.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Diagnostics;

namespace OpenAsphalte.Commands;

/// <summary>
/// Commandes systeme integrees au coeur d'Open Asphalte.
/// Ces commandes sont toujours disponibles, meme sans module charge.
/// </summary>
public class SystemCommands : CommandBase
{
    /// <summary>
    /// Affiche l'aide avec la liste de toutes les commandes disponibles
    /// </summary>
    [CommandMethod("OAS_HELP")]
    [CommandInfo("Help",
        Description = "Shows the list of available commands",
        DisplayNameKey = "system.help",
        DescriptionKey = "system.help.desc",
        Order = 10)]
    public void Help()
    {
        var ed = Editor;
        if (ed == null) return;

        const int innerWidth = 68;
        string Line(string text) => $"| {text.PadRight(innerWidth - 1)}|";

        ed.WriteMessage("\n");
        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        ed.WriteMessage($"\n{Line(L10n.T("system.help.title"))}");
        ed.WriteMessage("\n+--------------------------------------------------------------------+");

        // Commandes systeme
        ed.WriteMessage($"\n{Line($"  {L10n.T("system.help.section.system").ToUpperInvariant()}")}");
        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        ed.WriteMessage($"\n{Line($"  OAS_HELP      - {L10n.T("system.help.desc")}")}");
        ed.WriteMessage($"\n{Line($"  OAS_VERSION   - {L10n.T("system.version.desc")}")}");
        ed.WriteMessage($"\n{Line($"  OAS_SETTINGS  - {L10n.T("system.settings.desc")}")}");
        ed.WriteMessage($"\n{Line($"  OAS_MODULES   - {L10n.T("system.modules.desc")}")}");
        ed.WriteMessage($"\n{Line($"  OAS_RELOAD    - {L10n.T("system.reload.desc")}")}");
        ed.WriteMessage($"\n{Line($"  OAS_UPDATE    - {L10n.T("system.update.desc")}")}");

        // Commandes des modules
        var commandsByModule = ModuleDiscovery.GetCommandsByModule();

        foreach (var moduleGroup in commandsByModule)
        {
            var module = moduleGroup.Key;
            var commands = moduleGroup.ToList();

            if (commands.Count == 0) continue;

            ed.WriteMessage("\n+--------------------------------------------------------------------+");
            ed.WriteMessage($"\n{Line($"  {L10n.T("system.help.section.module").ToUpperInvariant()}: {module.Name.ToUpperInvariant()}")}");
            ed.WriteMessage("\n+--------------------------------------------------------------------+");

            foreach (var cmd in commands.OrderBy(c => c.Order))
            {
                var cmdName = cmd.CommandName.PadRight(14);
                var desc = cmd.GetLocalizedDisplayName();
                if (desc.Length > 50) desc = desc.Substring(0, 47) + "...";
                ed.WriteMessage($"\n{Line($"  {cmdName}- {desc}")}");
            }
        }

        ed.WriteMessage("\n+--------------------------------------------------------------------+");
        ed.WriteMessage("\n");
    }

    /// <summary>
    /// Affiche les informations de version
    /// </summary>
    [CommandMethod("OAS_VERSION")]
    [CommandInfo("Version",
        Description = "Shows version information",
        DisplayNameKey = "about.title",
        DescriptionKey = "system.version.desc",
        Order = 50)]
    public void Version()
    {
        try
        {
            var window = new AboutWindow();
            AcadApp.ShowModalWindow(window);
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("settings.openError", ex.Message));
        }
    }

    /// <summary>
    /// Ouvre la fenetre des parametres
    /// </summary>
    [CommandMethod("OAS_SETTINGS")]
    [CommandInfo("Settings",
        Description = "Opens the settings window",
        DisplayNameKey = "system.settings",
        DescriptionKey = "system.settings.desc",
        Order = 20)]
    public void Settings()
    {
        try
        {
            var window = new SettingsWindow();
            var result = AcadApp.ShowModalWindow(window);

            if (result == true)
            {
                // Note: L'UI est automatiquement reconstruite via l'événement OnLanguageChanged
                // si la langue a changé (déclenché par L10n.SetLanguage dans SettingsWindow)
                Logger.Success(T("cmd.success"));
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("settings.openError", ex.Message));
        }
    }

    /// <summary>
    /// Recharge la configuration
    /// </summary>
    [CommandMethod("OAS_RELOAD")]
    [CommandInfo("Reload",
        Description = "Reloads the configuration",
        DisplayNameKey = "system.reload",
        DescriptionKey = "system.reload.desc",
        Order = 30)]
    public void Reload()
    {
        Configuration.Configuration.Reload();
        Logger.DebugMode = Configuration.Configuration.DevMode;
        L10n.SetLanguage(Configuration.Configuration.Language);

        // Reconstruire l'UI
        MenuBuilder.RebuildMenu();
        RibbonBuilder.RebuildRibbon();

        Logger.Success(L10n.T("system.reload.success"));
    }

    /// <summary>
    /// Ouvre le gestionnaire de modules pour installer/mettre à jour les modules
    /// </summary>
    [CommandMethod("OAS_MODULES")]
    [CommandInfo("Modules",
        Description = "Opens the module manager to install and update modules",
        DisplayNameKey = "system.modules",
        DescriptionKey = "system.modules.desc",
        Order = 35)]
    public void Modules()
    {
        try
        {
            var window = new ModuleManagerWindow();
            AcadApp.ShowModalWindow(window);
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("modules.manager.openError", ex.Message));
        }
    }

    /// <summary>
    /// Vérifie les mises à jour en ouvrant la page des releases GitHub
    /// </summary>
    [CommandMethod("OAS_UPDATE")]
    [CommandInfo("Update",
        Description = "Checks for available updates",
        DisplayNameKey = "system.update",
        DescriptionKey = "system.update.desc",
        Order = 40)]
    public void Update()
    {
        var ed = Editor;
        if (ed == null) return;

        ed.WriteMessage("\n");
        Logger.Info(L10n.T("system.update.checking"));
        Logger.Info(L10n.TFormat("system.update.current", Plugin.Version));

        // Ouvrir la page GitHub des releases
        try
        {
            var url = Configuration.Configuration.UpdateUrl;

            // Valider l'URL avant ouverture (sécurité)
            if (!UrlValidationService.IsValidUpdateUrl(url))
            {
                Logger.Error(L10n.TFormat("system.update.openError", "URL non valide ou non sécurisée"));
                return;
            }

            Logger.Info(L10n.T("system.update.opening"));
            Logger.Info(L10n.TFormat("system.update.url", url));
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("system.update.openError", ex.Message));
        }
    }

}
