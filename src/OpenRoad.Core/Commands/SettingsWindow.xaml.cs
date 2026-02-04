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

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenAsphalte.Configuration;
using OpenAsphalte.Discovery;
using OpenAsphalte.Localization;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Commands;

/// <summary>
/// Fenêtre des paramètres Open Asphalte
/// </summary>
public partial class SettingsWindow : Window
{
    private string _initialLanguage = "";
    private ObservableCollection<ModuleItemViewModel> _moduleList = new();

    public SettingsWindow()
    {
        InitializeComponent();
        lstModules.ItemsSource = _moduleList;
        LoadSettings();
        UpdateLocalizedText();
    }

    /// <summary>
    /// Constructeur permettant d'ouvrir directement sur un onglet spécifique
    /// </summary>
    public SettingsWindow(int tabIndex) : this()
    {
        if (tabIndex == 1) // Onglet Modules
        {
            tabModules.IsSelected = true;
            // Déclencher le chargement des modules immédiatement
            // car l'événement OnTabSelectionChanged peut ne pas se déclencher
            Loaded += async (s, e) => await RefreshModulesListAsync();
        }
    }

    /// <summary>
    /// Met à jour les textes de la fenêtre selon la langue
    /// </summary>
    private void UpdateLocalizedText()
    {
        Title = L10n.T("settings.title");

        // Tab General
        if (tabGeneral != null) tabGeneral.Header = L10n.T("settings.tab.general", "Général");
        lblLanguage.Text = L10n.T("settings.language");
        chkDevMode.Content = L10n.T("settings.devmode");
        chkCheckUpdates.Content = L10n.T("settings.checkupdates");

        // Tab Modules
        if (tabModules != null) tabModules.Header = L10n.T("settings.tab.modules", "Modules");
        if (btnCheckUpdates != null) btnCheckUpdates.Content = L10n.T("settings.modules.check", "Vérifier Mises à jour");
        if (btnInstallSelected != null) btnInstallSelected.Content = L10n.T("settings.modules.installSelected", "Installer la sélection");

        // Buttons
        btnCancel.Content = L10n.T("settings.cancel");
        btnSave.Content = L10n.T("settings.save");
    }

    /// <summary>
    /// Charge les paramètres actuels
    /// </summary>
    private void LoadSettings()
    {
        // Langue - Créer les items dynamiquement
        _initialLanguage = L10n.CurrentLanguage;
        cmbLanguage.Items.Clear();

        foreach (var lang in L10n.SupportedLanguages)
        {
            var item = new ComboBoxItem
            {
                Content = L10n.LanguageNames.GetValueOrDefault(lang, lang),
                Tag = lang
            };
            cmbLanguage.Items.Add(item);

            if (lang == _initialLanguage)
            {
                cmbLanguage.SelectedItem = item;
            }
        }

        // Mode développeur
        chkDevMode.IsChecked = Configuration.Configuration.DevMode;

        // Mises à jour
        chkCheckUpdates.IsChecked = Configuration.Configuration.CheckUpdatesOnStartup;

        // Infos
        UpdateInfoText();
    }

    /// <summary>
    /// Met à jour le texte d'information
    /// </summary>
    private void UpdateInfoText()
    {
        var modules = ModuleDiscovery.Modules;
        var commands = ModuleDiscovery.AllCommands;
        txtInfo.Text = L10n.TFormat("settings.info", Plugin.Version, modules.Count, commands.Count, Configuration.Configuration.ConfigurationFile);
    }

    /// <summary>
    /// Enregistre les paramètres
    /// </summary>
    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // Langue - Utiliser SetLanguage pour déclencher la mise à jour de l'UI
            if (cmbLanguage.SelectedItem is ComboBoxItem selectedLang)
            {
                var newLanguage = selectedLang.Tag?.ToString() ?? "fr";
                if (newLanguage != _initialLanguage)
                {
                    // SetLanguage déclenche l'événement OnLanguageChanged
                    // qui met à jour automatiquement l'UI
                    L10n.SetLanguage(newLanguage);
                }
            }

            // Mode développeur
            Configuration.Configuration.Set("devMode", chkDevMode.IsChecked == true);

            // Mises à jour
            Configuration.Configuration.Set("checkUpdatesOnStartup", chkCheckUpdates.IsChecked == true);

            // Sauvegarder
            Configuration.Configuration.Save();

            DialogResult = true;
            Close();
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(L10n.TFormat("settings.saveError", ex.Message),
                          L10n.T("cmd.error"),
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Annule les modifications
    /// </summary>
    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Gère le changement d'onglet - rafraîchit la liste des modules quand on arrive sur l'onglet Modules
    /// </summary>
    private async void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is System.Windows.Controls.TabControl && tabModules != null && tabModules.IsSelected)
        {
            // Toujours rafraîchir quand on arrive sur l'onglet Modules
            await RefreshModulesListAsync();
        }
    }

    /// <summary>
    /// Rafraîchit la liste des modules depuis le catalogue
    /// </summary>
    private async Task RefreshModulesListAsync()
    {
        btnCheckUpdates.IsEnabled = false;
        txtUpdateStatus.Text = L10n.T("settings.modules.checking", "Vérification...");
        _moduleList.Clear();

        try
        {
            var result = await UpdateService.CheckForUpdatesAsync();

            if (!result.Success)
            {
                txtUpdateStatus.Text = L10n.T("settings.modules.error", "Erreur réseau");
                return;
            }

            if (result.CoreUpdateAvailable)
            {
                 txtUpdateStatus.Text = L10n.T("settings.modules.coreAvailable", "Mise à jour Open Asphalte dispo !");
            }
            else
            {
                var installedCount = ModuleDiscovery.Modules.Count;
                var totalCount = result.Manifest?.Modules.Count ?? 0;
                txtUpdateStatus.Text = L10n.TFormat("settings.modules.status", "{0}/{1} modules installés", installedCount, totalCount);
            }

            // Afficher les modules
            if (result.Manifest != null)
            {
                foreach (var moduleDef in result.Manifest.Modules)
                {
                    var isInstalled = ModuleDiscovery.Modules.Any(m => m.Id.Equals(moduleDef.Id, StringComparison.OrdinalIgnoreCase));
                    var updateInfo = result.Updates.FirstOrDefault(u => u.ModuleId == moduleDef.Id);

                    var installedVersion = ModuleDiscovery.Modules
                        .FirstOrDefault(m => m.Id.Equals(moduleDef.Id, StringComparison.OrdinalIgnoreCase))?.Version ?? "-";

                    var item = new ModuleItemViewModel
                    {
                        Definition = moduleDef,
                        Name = moduleDef.Name,
                        Description = moduleDef.Description,
                        VersionDisplay = isInstalled ? $"{installedVersion} -> {moduleDef.Version}" : moduleDef.Version,
                        StatusIcon = isInstalled ? (updateInfo != null ? "!" : "✓") : "+",
                        StatusColor = isInstalled ? (updateInfo != null ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.Green) : System.Windows.Media.Brushes.Blue,
                        CanUpdate = !isInstalled || updateInfo != null,
                        ActionText = isInstalled ? (updateInfo != null ? L10n.T("modules.action.update", "Mettre à jour") : L10n.T("modules.action.installed", "Installé")) : L10n.T("modules.action.install", "Installer")
                    };

                    // Forcer bouton gris si déjà installé et à jour
                    if (isInstalled && updateInfo == null)
                    {
                        item.CanUpdate = false;
                    }

                    _moduleList.Add(item);
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Update check error: {ex}");
            txtUpdateStatus.Text = L10n.T("settings.modules.error", "Erreur.");
        }
        finally
        {
            btnCheckUpdates.IsEnabled = true;
        }
    }

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
    {
        await RefreshModulesListAsync();

        // Vérifier si mise à jour Core disponible et proposer
        var result = await UpdateService.CheckForUpdatesAsync();
        if (result.Success && result.CoreUpdateAvailable && result.Manifest != null)
        {
            if (System.Windows.MessageBox.Show(
                L10n.T("settings.modules.coreUpdateMsg", "Une nouvelle version d'Open Asphalte est disponible. Mettre à jour ?"),
                "Mise à jour",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                await UpdateService.InstallCoreUpdateAsync(result.Manifest.Core.DownloadUrl);
                DialogResult = true;
                Close();
            }
        }
    }

    private async void OnModuleActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.CommandParameter is ModuleItemViewModel item)
        {
             try
             {
                 // Récupérer le manifest pour vérifier les dépendances
                 var checkResult = await UpdateService.CheckForUpdatesAsync();
                 if (!checkResult.Success || checkResult.Manifest == null)
                 {
                     System.Windows.MessageBox.Show(L10n.T("settings.modules.error", "Erreur réseau"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                     return;
                 }

                 // Vérifier les dépendances manquantes
                 var missingDeps = GetMissingDependencies(item.Definition, checkResult.Manifest);
                 var installedModules = new List<string>();

                 if (missingDeps.Count > 0)
                 {
                     // Construire le message de confirmation
                     var depNames = string.Join(", ", missingDeps.Select(d => d.Name));
                     var message = L10n.TFormat("modules.manager.dependencies.confirm",
                         new object[] { item.Name, depNames, missingDeps.Count });

                     var result = System.Windows.MessageBox.Show(
                         message,
                         L10n.T("modules.manager.dependencies.title", "Dépendances requises"),
                         MessageBoxButton.YesNo,
                         MessageBoxImage.Question);

                     if (result != MessageBoxResult.Yes)
                     {
                         return; // L'utilisateur a annulé
                     }

                     // Installer les dépendances d'abord
                     foreach (var dep in missingDeps)
                     {
                         btn.Content = L10n.TFormat("modules.manager.installing", "Installation de {0}...", dep.Name);
                         await UpdateService.InstallModuleAsync(dep);
                         installedModules.Add(dep.Name);

                         // Mettre à jour le ViewModel correspondant dans la liste
                         var depVm = _moduleList.FirstOrDefault(m => m.Definition.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase));
                         if (depVm != null)
                         {
                             depVm.CanUpdate = false;
                             depVm.StatusIcon = "✓";
                             depVm.StatusColor = System.Windows.Media.Brushes.Green;
                             depVm.ActionText = L10n.T("modules.action.installed", "Installé");
                         }
                     }
                 }

                 btn.IsEnabled = false;
                 btn.Content = L10n.T("modules.manager.downloading", "Téléchargement...");

                 await UpdateService.InstallModuleAsync(item.Definition);
                 installedModules.Add(item.Name);

                 btn.Content = L10n.T("modules.action.installed", "Installé");
                 item.CanUpdate = false;
                 item.StatusIcon = "✓";
                 item.StatusColor = System.Windows.Media.Brushes.Green;

                 // Message de succès
                 if (installedModules.Count > 1)
                 {
                     var modulesList = string.Join("\n• ", installedModules);
                     System.Windows.MessageBox.Show(
                         L10n.TFormat("modules.manager.restartRequired.multiple",
                             "Les modules suivants ont été installés :\n{0}\n\nRedémarrez AutoCAD pour les activer.",
                             "• " + modulesList),
                         L10n.T("modules.manager.installSuccess", "Installation réussie"),
                         MessageBoxButton.OK,
                         MessageBoxImage.Information);
                 }
                 else
                 {
                     System.Windows.MessageBox.Show(
                         L10n.TFormat("modules.manager.restartRequired", "Le module {0} a été installé.\n\nRedémarrez AutoCAD pour l'activer.", item.Name),
                         L10n.T("common.success", "Succès"),
                         MessageBoxButton.OK,
                         MessageBoxImage.Information);
                 }
             }
             catch(System.Exception ex)
             {
                 System.Windows.MessageBox.Show(ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                 btn.Content = L10n.T("modules.manager.retry", "Réessayer");
                 btn.IsEnabled = true;
             }
        }
    }

    /// <summary>
    /// Récupère les dépendances manquantes pour un module
    /// </summary>
    private List<ModuleDefinition> GetMissingDependencies(ModuleDefinition moduleDef, MarketplaceManifest manifest)
    {
        var missing = new List<ModuleDefinition>();

        if (moduleDef.Dependencies == null || moduleDef.Dependencies.Count == 0)
            return missing;

        foreach (var depId in moduleDef.Dependencies)
        {
            // Vérifier si la dépendance est déjà installée (chargée en mémoire)
            var isLoadedInMemory = ModuleDiscovery.Modules
                .Any(m => m.Id.Equals(depId, StringComparison.OrdinalIgnoreCase));

            // Vérifier si la dépendance a été installée dans cette session
            var isInstalledThisSession = _moduleList
                .Any(m => m.Definition.Id.Equals(depId, StringComparison.OrdinalIgnoreCase) && !m.CanUpdate && m.StatusIcon == "✓");

            // Vérifier si le fichier DLL existe physiquement
            var isDllPresent = false;
            if (!string.IsNullOrEmpty(ModuleDiscovery.ModulesPath))
            {
                var possibleFiles = new[]
                {
                    $"OpenRoad.{depId}.dll",
                    $"OpenRoad.{char.ToUpper(depId[0])}{depId.Substring(1)}.dll"
                };
                isDllPresent = possibleFiles.Any(f =>
                    File.Exists(Path.Combine(ModuleDiscovery.ModulesPath, f)));
            }

            if (!isLoadedInMemory && !isInstalledThisSession && !isDllPresent)
            {
                // Chercher la définition dans le manifest
                var depDef = manifest.Modules
                    .FirstOrDefault(m => m.Id.Equals(depId, StringComparison.OrdinalIgnoreCase));

                if (depDef != null)
                {
                    // Éviter les doublons
                    if (!missing.Any(m => m.Id.Equals(depDef.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        missing.Add(depDef);

                        // Récursivement vérifier les dépendances de la dépendance
                        var subDeps = GetMissingDependencies(depDef, manifest);
                        foreach (var subDep in subDeps)
                        {
                            if (!missing.Any(m => m.Id.Equals(subDep.Id, StringComparison.OrdinalIgnoreCase)))
                            {
                                missing.Insert(0, subDep); // Les sous-dépendances d'abord
                            }
                        }
                    }
                }
            }
        }

        return missing;
    }

    /// <summary>
    /// Gère le changement de sélection des modules (checkbox)
    /// </summary>
    private void OnModuleSelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateSelectionUI();
    }

    /// <summary>
    /// Met à jour l'interface selon la sélection
    /// </summary>
    private void UpdateSelectionUI()
    {
        var selectedCount = _moduleList.Count(m => m.IsSelected && m.CanUpdate);
        btnInstallSelected.IsEnabled = selectedCount > 0;

        if (selectedCount == 0)
        {
            txtSelectedCount.Text = "";
        }
        else
        {
            txtSelectedCount.Text = L10n.TFormat("settings.modules.selectedCount", "{0} module(s) sélectionné(s)", selectedCount);
        }
    }

    /// <summary>
    /// Installe tous les modules sélectionnés
    /// </summary>
    private async void OnInstallSelectedClick(object sender, RoutedEventArgs e)
    {
        var selectedModules = _moduleList.Where(m => m.IsSelected && m.CanUpdate).ToList();

        if (selectedModules.Count == 0)
            return;

        try
        {
            // Récupérer le manifest pour les dépendances
            var checkResult = await UpdateService.CheckForUpdatesAsync();
            if (!checkResult.Success || checkResult.Manifest == null)
            {
                System.Windows.MessageBox.Show(L10n.T("settings.modules.error", "Erreur réseau"), "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Collecter toutes les dépendances manquantes
            var allDependencies = new List<ModuleDefinition>();
            foreach (var module in selectedModules)
            {
                var deps = GetMissingDependencies(module.Definition, checkResult.Manifest);
                foreach (var dep in deps)
                {
                    // Éviter les doublons et les modules déjà sélectionnés
                    if (!allDependencies.Any(d => d.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase)) &&
                        !selectedModules.Any(m => m.Definition.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        allDependencies.Add(dep);
                    }
                }
            }

            // Demander confirmation pour les dépendances supplémentaires
            if (allDependencies.Count > 0)
            {
                var depNames = string.Join("\n• ", allDependencies.Select(d => d.Name));
                var message = L10n.TFormat("settings.modules.dependenciesRequired",
                    "Les modules suivants seront également installés (dépendances requises) :\n\n• {0}\n\nContinuer ?",
                    depNames);

                var result = System.Windows.MessageBox.Show(
                    message,
                    L10n.T("modules.manager.dependencies.title", "Dépendances requises"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Confirmer l'installation
            var selectedNames = string.Join("\n• ", selectedModules.Select(m => m.Name));
            var totalCount = selectedModules.Count + allDependencies.Count;
            var confirmMessage = L10n.TFormat("settings.modules.confirmInstall",
                "Installer {0} module(s) ?\n\n• {1}",
                totalCount, selectedNames);

            if (System.Windows.MessageBox.Show(confirmMessage, L10n.T("settings.modules.installTitle", "Installation"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            // Désactiver les contrôles pendant l'installation
            btnInstallSelected.IsEnabled = false;
            btnCheckUpdates.IsEnabled = false;
            var installedModules = new List<string>();

            // D'abord installer les dépendances
            foreach (var dep in allDependencies)
            {
                txtUpdateStatus.Text = L10n.TFormat("modules.manager.installing", "Installation de {0}...", dep.Name);
                await UpdateService.InstallModuleAsync(dep);
                installedModules.Add(dep.Name);

                // Mettre à jour le ViewModel correspondant
                var depVm = _moduleList.FirstOrDefault(m => m.Definition.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase));
                if (depVm != null)
                {
                    depVm.CanUpdate = false;
                    depVm.IsSelected = false;
                    depVm.StatusIcon = "✓";
                    depVm.StatusColor = System.Windows.Media.Brushes.Green;
                    depVm.ActionText = L10n.T("modules.action.installed", "Installé");
                }
            }

            // Ensuite installer les modules sélectionnés
            foreach (var module in selectedModules)
            {
                txtUpdateStatus.Text = L10n.TFormat("modules.manager.installing", "Installation de {0}...", module.Name);
                await UpdateService.InstallModuleAsync(module.Definition);
                installedModules.Add(module.Name);

                module.CanUpdate = false;
                module.IsSelected = false;
                module.StatusIcon = "✓";
                module.StatusColor = System.Windows.Media.Brushes.Green;
                module.ActionText = L10n.T("modules.action.installed", "Installé");
            }

            // Rafraîchir l'affichage
            lstModules.Items.Refresh();
            UpdateSelectionUI();

            var installedCount = ModuleDiscovery.Modules.Count + installedModules.Count;
            var totalAvailable = checkResult.Manifest?.Modules.Count ?? 0;
            txtUpdateStatus.Text = L10n.TFormat("settings.modules.status", "{0}/{1} modules installés", installedCount, totalAvailable);

            // Message de succès
            var modulesList = string.Join("\n• ", installedModules);
            System.Windows.MessageBox.Show(
                L10n.TFormat("modules.manager.restartRequired.multiple",
                    "Les modules suivants ont été installés :\n{0}\n\nRedémarrez AutoCAD pour les activer.",
                    "• " + modulesList),
                L10n.T("modules.manager.installSuccess", "Installation réussie"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Logger.Success(L10n.TFormat("settings.modules.installed", "{0} module(s) installé(s)", installedModules.Count));
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Batch install error: {ex}");
            System.Windows.MessageBox.Show(
                L10n.TFormat("modules.manager.installError", "Erreur lors de l'installation: {0}", ex.Message),
                L10n.T("cmd.error", "Erreur"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            btnInstallSelected.IsEnabled = true;
            btnCheckUpdates.IsEnabled = true;
        }
    }
}

public class ModuleItemViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public ModuleDefinition Definition { get; set; } = new();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string VersionDisplay { get; set; } = "";

    private string _statusIcon = "";
    public string StatusIcon
    {
        get => _statusIcon;
        set
        {
            if (_statusIcon != value)
            {
                _statusIcon = value;
                OnPropertyChanged(nameof(StatusIcon));
            }
        }
    }

    private System.Windows.Media.Brush _statusColor = System.Windows.Media.Brushes.Black;
    public System.Windows.Media.Brush StatusColor
    {
        get => _statusColor;
        set
        {
            if (_statusColor != value)
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    private bool _canUpdate;
    public bool CanUpdate
    {
        get => _canUpdate;
        set
        {
            if (_canUpdate != value)
            {
                _canUpdate = value;
                OnPropertyChanged(nameof(CanUpdate));
            }
        }
    }

    private string _actionText = "";
    public string ActionText
    {
        get => _actionText;
        set
        {
            if (_actionText != value)
            {
                _actionText = value;
                OnPropertyChanged(nameof(ActionText));
            }
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
