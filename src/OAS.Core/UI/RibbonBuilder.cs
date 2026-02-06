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

using Autodesk.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Discovery;
using OpenAsphalte.Logging;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.UI;

/// <summary>
/// Construction dynamique du ruban (Ribbon) Open Asphalte.
/// Génère automatiquement le ruban basé sur les modules découverts.
/// </summary>
/// <remarks>
/// Le ruban supporte une mise à jour incrémentale lors du changement de langue
/// pour éviter le scintillement causé par une reconstruction complète.
/// </remarks>
public static class RibbonBuilder
{
    private const string TabId = "OPENASPHALTE_TAB";
    private const string DefaultTabTitle = "Open Asphalte";
    private const string LogoResourcePath = "pack://application:,,,/OAS.Core;component/Resources/OAS_Logo.png";
    private static bool _ribbonCreated = false;

    // Cache pour mise à jour incrémentale des textes
    private static readonly Dictionary<string, RibbonButton> _buttonCache = new();
    private static readonly Dictionary<string, RibbonPanelSource> _panelCache = new();

    // Images redimensionnées pour le ruban
    private static BitmapImage? _largeImage = null;  // 32x32
    private static BitmapImage? _smallImage = null;  // 16x16

    /// <summary>
    /// Retourne le titre de l'onglet (nom personnalisé ou défaut)
    /// </summary>
    private static string GetTabTitle()
    {
        // Récupérer le nom personnalisé depuis la configuration
        var customName = Configuration.Configuration.MainMenuName;

        // Si le nom est personnalisé (différent du défaut), l'utiliser
        if (!string.IsNullOrEmpty(customName) &&
            !customName.Equals(DefaultTabTitle, StringComparison.OrdinalIgnoreCase))
        {
            return customName;
        }

        // Sinon utiliser la traduction (ou défaut)
        return L10n.T("app.name", DefaultTabTitle);
    }

    /// <summary>
    /// Charge et redimensionne l'image du logo Open Asphalte (32x32 pour grands boutons)
    /// </summary>
    private static BitmapImage? GetLargeImage()
    {
        if (_largeImage != null) return _largeImage;

        try
        {
            _largeImage = LoadAndResizeImage(LogoResourcePath, 32, 32);
            return _largeImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Charge et redimensionne l'image du logo Open Asphalte (16x16 pour petits boutons)
    /// </summary>
    private static BitmapImage? GetSmallImage()
    {
        if (_smallImage != null) return _smallImage;

        try
        {
            _smallImage = LoadAndResizeImage(LogoResourcePath, 16, 16);
            return _smallImage;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Charge une image et la redimensionne aux dimensions spécifiées
    /// </summary>
    private static BitmapImage? LoadAndResizeImage(string uri, int width, int height)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(uri);
            image.DecodePixelWidth = width;
            image.DecodePixelHeight = height;
            image.EndInit();
            image.Freeze(); // Optimisation pour le thread UI
            return image;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Retourne l'image du logo pour compatibilité (utilise la grande image)
    /// </summary>
    private static BitmapImage? GetLogoImage() => GetLargeImage();

    /// <summary>
    /// Cree le ruban Open Asphalte base sur les modules decouverts.
    /// </summary>
    public static void CreateRibbon()
    {
        if (_ribbonCreated) return;

        try
        {
            var ribbonControl = ComponentManager.Ribbon;
            if (ribbonControl == null)
            {
                Logger.Debug(L10n.T("ui.ribbon.notAvailable", "Ribbon not available (disabled or classic workspace)"));
                return;
            }

            // Supprimer l'onglet existant et vider les caches
            RemoveRibbon();
            _buttonCache.Clear();
            _panelCache.Clear();

            // Creer l'onglet
            var tab = new RibbonTab
            {
                Title = GetTabTitle(),
                Id = TabId
            };

            // Creer un panneau pour chaque module
            var commandsByModule = ModuleDiscovery.GetCommandsByModule();

            foreach (var moduleGroup in commandsByModule)
            {
                var module = moduleGroup.Key;
                var commands = moduleGroup.Where(c => c.ShowInRibbon).ToList();

                if (commands.Count == 0) continue;

                var panel = CreateModulePanel(module, commands);
                if (panel != null)
                {
                    tab.Panels.Add(panel);
                }
            }

            // Ajouter le panneau systeme
            var systemPanel = CreateSystemPanel();
            tab.Panels.Add(systemPanel);

            // Ajouter l'onglet au ruban
            ribbonControl.Tabs.Add(tab);
            _ribbonCreated = true;

            Logger.Debug(L10n.T("ui.ribbon.created"));
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("ui.ribbon.createError", ex.Message));
        }
    }

    /// <summary>
    /// Crée un panneau pour un module
    /// </summary>
    private static RibbonPanel? CreateModulePanel(IModule module, List<CommandDescriptor> commands)
    {
        try
        {
            var panel = new RibbonPanel();
            var source = new RibbonPanelSource
            {
                Title = module.NameKey != null ? L10n.T(module.NameKey, module.Name) : module.Name
            };
            panel.Source = source;

            // Enregistrer dans le cache pour mise à jour incrémentale
            _panelCache[module.Id] = source;

            // Grouper les commandes par groupe
            var groups = commands.GroupBy(c => c.Group ?? "").ToList();

            foreach (var group in groups)
            {
                var rowPanel = new RibbonRowPanel();
                int itemCount = 0;

                foreach (var cmd in group.OrderBy(c => c.Order))
                {
                    var button = CreateButton(cmd);

                    if (cmd.RibbonSize == CommandSize.Large)
                    {
                        // Les grands boutons prennent une colonne entiere
                        if (itemCount > 0)
                        {
                            source.Items.Add(rowPanel);
                            rowPanel = new RibbonRowPanel();
                            itemCount = 0;
                        }
                        source.Items.Add(button);
                    }
                    else
                    {
                        // Les petits boutons s'empilent verticalement (max 3)
                        if (itemCount > 0 && itemCount % 3 == 0)
                        {
                            source.Items.Add(rowPanel);
                            rowPanel = new RibbonRowPanel();
                        }
                        else if (itemCount > 0)
                        {
                            rowPanel.Items.Add(new RibbonRowBreak());
                        }
                        rowPanel.Items.Add(button);
                        itemCount++;
                    }
                }

                if (rowPanel.Items.Count > 0)
                {
                    source.Items.Add(rowPanel);
                }
            }

            return panel;
        }
        catch (System.Exception ex)
        {
            Logger.Debug(L10n.TFormat("ui.ribbon.panelError", module.Name, ex.Message));
            return null;
        }
    }

    /// <summary>
    /// Crée le panneau système
    /// </summary>
    private static RibbonPanel CreateSystemPanel()
    {
        var panel = new RibbonPanel();
        var source = new RibbonPanelSource { Title = L10n.T("system.name") };
        panel.Source = source;

        // Enregistrer dans le cache pour mise à jour incrémentale
        _panelCache["_system"] = source;

        // Images redimensionnées
        var largeIcon = GetLargeImage();
        var smallIcon = GetSmallImage();

        // Bouton Paramètres - Grand bouton
        var settingsButton = new RibbonButton
        {
            Text = L10n.T("system.settings"),
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            CommandHandler = new RibbonCommandHandler(),
            CommandParameter = "OAS_SETTINGS",
            ToolTip = CreateTooltip(L10n.T("system.settings"), L10n.T("system.settings.desc")),
            LargeImage = largeIcon,
            Image = smallIcon
        };
        _buttonCache["OAS_SETTINGS"] = settingsButton;
        source.Items.Add(settingsButton);

        // Bouton À propos - Grand bouton
        var aboutButton = new RibbonButton
        {
            Text = L10n.T("about.title"),
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Large,
            Orientation = System.Windows.Controls.Orientation.Vertical,
            CommandHandler = new RibbonCommandHandler(),
            CommandParameter = "OAS_VERSION",
            ToolTip = CreateTooltip(L10n.T("about.title"), L10n.T("system.version.desc")),
            LargeImage = largeIcon,
            Image = smallIcon
        };
        _buttonCache["OAS_VERSION"] = aboutButton;
        source.Items.Add(aboutButton);

        // Panneau avec les boutons secondaires (Aide, Recharger, Mise à jour)
        var rowPanel = new RibbonRowPanel();

        // Aide
        var helpButton = CreateSystemButtonSmall(
            L10n.T("system.help"),
            "OAS_HELP",
            L10n.T("system.help.desc")
        );
        rowPanel.Items.Add(helpButton);
        rowPanel.Items.Add(new RibbonRowBreak());

        // Recharger
        var reloadButton = CreateSystemButtonSmall(
            L10n.T("system.reload"),
            "OAS_RELOAD",
            L10n.T("system.reload.desc")
        );
        rowPanel.Items.Add(reloadButton);
        rowPanel.Items.Add(new RibbonRowBreak());

        // Mise à jour
        var updateButton = CreateSystemButtonSmall(
            L10n.T("system.update"),
            "OAS_UPDATE",
            L10n.T("system.update.desc")
        );
        rowPanel.Items.Add(updateButton);

        source.Items.Add(rowPanel);

        return panel;
    }

    /// <summary>
    /// Crée un petit bouton système (16x16)
    /// </summary>
    private static RibbonButton CreateSystemButtonSmall(string text, string command, string tooltip)
    {
        var button = new RibbonButton
        {
            Text = text,
            ShowText = true,
            ShowImage = true,
            Size = RibbonItemSize.Standard,
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            CommandHandler = new RibbonCommandHandler(),
            CommandParameter = command,
            ToolTip = CreateTooltip(text, tooltip),
            Image = GetSmallImage()
        };

        _buttonCache[command] = button;
        return button;
    }

    /// <summary>
    /// Cree un bouton de ruban a partir d'un descripteur de commande
    /// </summary>
    private static RibbonButton CreateButton(CommandDescriptor cmd)
    {
        var isLarge = cmd.RibbonSize == CommandSize.Large;
        var button = new RibbonButton
        {
            Text = cmd.GetLocalizedDisplayName(),
            ShowText = true,
            ShowImage = true,
            Size = isLarge ? RibbonItemSize.Large : RibbonItemSize.Standard,
            Orientation = isLarge ? System.Windows.Controls.Orientation.Vertical : System.Windows.Controls.Orientation.Horizontal,
            CommandHandler = new RibbonCommandHandler(),
            CommandParameter = cmd.CommandName,
            ToolTip = CreateTooltip(cmd.GetLocalizedDisplayName(), cmd.GetLocalizedDescription())
        };

        // Charger l'icône si spécifiée (avec redimensionnement)
        BitmapImage? largeIcon = null;
        BitmapImage? smallIcon = null;

        if (!string.IsNullOrEmpty(cmd.IconPath))
        {
            try
            {
                largeIcon = LoadAndResizeImage(cmd.IconPath, 32, 32);
                smallIcon = LoadAndResizeImage(cmd.IconPath, 16, 16);
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Icon loading failed for {cmd.CommandName}: {ex.Message}");
            }
        }

        // Utiliser le logo Open Asphalte comme fallback
        largeIcon ??= GetLargeImage();
        smallIcon ??= GetSmallImage();

        button.LargeImage = largeIcon;
        button.Image = smallIcon;

        // Enregistrer dans le cache pour mise à jour incrémentale
        _buttonCache[cmd.CommandName] = button;

        return button;
    }

    /// <summary>
    /// Crée une infobulle formatée
    /// </summary>
    private static RibbonToolTip CreateTooltip(string title, string content)
    {
        return new RibbonToolTip
        {
            Title = title,
            Content = content,
            IsHelpEnabled = false
        };
    }

    /// <summary>
    /// Supprime le ruban Open Asphalte
    /// </summary>
    public static void RemoveRibbon()
    {
        try
        {
            var ribbonControl = ComponentManager.Ribbon;
            var tab = ribbonControl?.FindTab(TabId);
            if (tab != null)
            {
                ribbonControl!.Tabs.Remove(tab);
                _ribbonCreated = false;
            }

            // Vider les caches
            _buttonCache.Clear();
            _panelCache.Clear();
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"RemoveRibbon: {ex.Message}");
        }
    }

    /// <summary>
    /// Reconstruit le ruban (utilisé après changement de langue).
    /// Utilise une mise à jour incrémentale si possible pour éviter le scintillement.
    /// </summary>
    public static void RebuildRibbon()
    {
        try
        {
            // Vérifier que le ruban est disponible
            var ribbonControl = ComponentManager.Ribbon;
            if (ribbonControl == null)
            {
                Logger.Debug(L10n.T("ui.ribbon.notAvailable", "Ribbon not available (disabled or classic workspace)"));
                return;
            }

            // Tenter une mise à jour incrémentale si le ruban existe
            if (_ribbonCreated && TryUpdateLocalizedTexts())
            {
                Logger.Debug(L10n.T("ui.ribbon.updated", "Ruban mis à jour (incrémental)"));
                return;
            }

            // Sinon, reconstruction complète
            RemoveRibbon();
            CreateRibbon();
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"RebuildRibbon skipped: {ex.Message}");
        }
    }

    /// <summary>
    /// Met à jour uniquement les textes localisés du ruban sans recréer la structure.
    /// </summary>
    /// <returns>True si la mise à jour a réussi, false si reconstruction nécessaire</returns>
    private static bool TryUpdateLocalizedTexts()
    {
        try
        {
            var ribbonControl = ComponentManager.Ribbon;
            var tab = ribbonControl?.FindTab(TabId);
            if (tab == null) return false;

            // Mettre à jour le titre de l'onglet
            tab.Title = GetTabTitle();

            // Mettre à jour les panneaux en cache
            foreach (var kvp in _panelCache)
            {
                var moduleId = kvp.Key;
                var panelSource = kvp.Value;

                // Trouver le module correspondant
                var module = Discovery.ModuleDiscovery.GetModule(moduleId);
                if (module != null)
                {
                    panelSource.Title = module.NameKey != null
                        ? L10n.T(module.NameKey, module.Name)
                        : module.Name;
                }
            }

            // Mettre à jour le panneau système
            if (_panelCache.TryGetValue("_system", out var systemPanel))
            {
                systemPanel.Title = L10n.T("system.name");
            }

            // Mettre à jour les boutons en cache
            foreach (var kvp in _buttonCache)
            {
                var commandName = kvp.Key;
                var button = kvp.Value;

                // Trouver le descripteur de commande
                var cmd = Discovery.ModuleDiscovery.AllCommands
                    .FirstOrDefault(c => c.CommandName == commandName);

                if (cmd != null)
                {
                    button.Text = cmd.GetLocalizedDisplayName();
                    button.ToolTip = CreateTooltip(
                        cmd.GetLocalizedDisplayName(),
                        cmd.GetLocalizedDescription());
                }
            }

            // Mettre à jour les boutons système
            UpdateSystemButtonText("OAS_SETTINGS", L10n.T("system.settings"), L10n.T("system.settings.desc"));
            UpdateSystemButtonText("OAS_VERSION", L10n.T("about.title"), L10n.T("system.version.desc"));
            UpdateSystemButtonText("OAS_HELP", L10n.T("system.help"), L10n.T("system.help.desc"));
            UpdateSystemButtonText("OAS_RELOAD", L10n.T("system.reload"), L10n.T("system.reload.desc"));
            UpdateSystemButtonText("OAS_UPDATE", L10n.T("system.update"), L10n.T("system.update.desc"));

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Met à jour le texte d'un bouton système en cache
    /// </summary>
    private static void UpdateSystemButtonText(string commandName, string text, string tooltip)
    {
        if (_buttonCache.TryGetValue(commandName, out var button))
        {
            button.Text = text;
            button.ToolTip = CreateTooltip(text, tooltip);
        }
    }
}

/// <summary>
/// Gestionnaire de commandes pour les boutons du ruban
/// </summary>
public class RibbonCommandHandler : System.Windows.Input.ICommand
{
    /// <summary>
    /// Événement requis par ICommand (non utilisé car CanExecute retourne toujours true)
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter)
    {
        if (parameter is string command && !string.IsNullOrWhiteSpace(command))
        {
            try
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                {
                    // Format: ^C^C annule les commandes en cours, puis lance la commande
                    // Le _ rend la commande internationale (non localisée)
                    doc.SendStringToExecute($"^C^C_{command} ", true, false, false);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"RibbonCommandHandler error: {ex.Message}");
            }
        }
    }
}
