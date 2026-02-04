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

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenRoad.Configuration;
using OpenRoad.Discovery;
using OpenRoad.Localization;
using OpenRoad.Logging;
using OpenRoad.Services;
using L10n = OpenRoad.Localization.Localization;

namespace OpenRoad.Commands;

/// <summary>
/// Fenêtre des paramètres Open Road
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

    private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
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
                System.Windows.MessageBox.Show(result.ErrorMessage, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (result.CoreUpdateAvailable)
            {
                 txtUpdateStatus.Text = L10n.T("settings.modules.coreAvailable", "Mise à jour Open Road dispo !");
                 if (System.Windows.MessageBox.Show(
                     L10n.T("settings.modules.coreUpdateMsg", "Une nouvelle version d'Open Road est disponible. Mettre à jour ?"), 
                     "Mise à jour", 
                     MessageBoxButton.YesNo, 
                     MessageBoxImage.Information) == MessageBoxResult.Yes)
                 {
                     if (result.Manifest != null)
                        await UpdateService.InstallCoreUpdateAsync(result.Manifest.Core.DownloadUrl);
                     
                     DialogResult = true;
                     Close();
                     return;
                 }
            }
            else
            {
                txtUpdateStatus.Text = L10n.T("settings.modules.uptodate", "À jour");
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
                        ActionText = isInstalled ? (updateInfo != null ? "Mettre à jour" : "Installé") : "Installer"
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
            txtUpdateStatus.Text = "Erreur.";
        }
        finally
        {
            btnCheckUpdates.IsEnabled = true;
        }
    }

    private async void OnModuleActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.CommandParameter is ModuleItemViewModel item)
        {
             try 
             {
                 btn.IsEnabled = false;
                 btn.Content = "Téléchargement...";
                 
                 await UpdateService.InstallModuleAsync(item.Definition);
                 
                 btn.Content = "Installé";
                 item.CanUpdate = false;
                 item.StatusIcon = "✓";
                 item.StatusColor = System.Windows.Media.Brushes.Green;
                 
                 System.Windows.MessageBox.Show(L10n.TFormat("modules.manager.restartRequired", item.Name), L10n.T("common.success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information);
             }
             catch(System.Exception ex)
             {
                 System.Windows.MessageBox.Show(ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                 btn.Content = "Réessayer";
                 btn.IsEnabled = true;
             }
        }
    }
}

public class ModuleItemViewModel
{
    public ModuleDefinition Definition { get; set; } = new();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string VersionDisplay { get; set; } = "";
    public string StatusIcon { get; set; } = "";
    public System.Windows.Media.Brush StatusColor { get; set; } = System.Windows.Media.Brushes.Black;
    public bool CanUpdate { get; set; }
    public string ActionText { get; set; } = "";
}

