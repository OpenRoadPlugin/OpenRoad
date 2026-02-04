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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenRoad.Discovery;
using OpenRoad.Logging;
using OpenRoad.Services;
using L10n = OpenRoad.Localization.Localization;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;

namespace OpenRoad.Commands;

/// <summary>
/// Fenêtre de gestion des modules Open Road.
/// Permet d'installer, mettre à jour et gérer les modules depuis le catalogue GitHub.
/// </summary>
public partial class ModuleManagerWindow : Window
{
    private ObservableCollection<ModuleViewModel> _allModules = new();
    private ObservableCollection<ModuleViewModel> _filteredModules = new();
    private MarketplaceManifest? _manifest;
    private bool _isLoading = false;
    
    public ModuleManagerWindow()
    {
        InitializeComponent();
        lstModules.ItemsSource = _filteredModules;
        UpdateLocalizedText();
        
        // Charger automatiquement au démarrage
        Loaded += async (s, e) => await RefreshModulesAsync();
    }
    
    /// <summary>
    /// Met à jour les textes de la fenêtre selon la langue
    /// </summary>
    private void UpdateLocalizedText()
    {
        Title = L10n.T("modules.manager.title", "Gestionnaire de Modules");
        txtTitle.Text = L10n.T("modules.manager.title", "Gestionnaire de Modules");
        txtSubtitle.Text = L10n.T("modules.manager.subtitle", "Installez et gérez les modules Open Road depuis le catalogue officiel.");
        btnRefresh.Content = "🔄 " + L10n.T("modules.manager.refresh", "Actualiser");
        txtFilter.Text = L10n.T("modules.manager.filter", "Filtrer:") ;
        btnClose.Content = L10n.T("settings.cancel", "Fermer");
        txtEmptyMessage.Text = L10n.T("modules.manager.empty", "Cliquez sur Actualiser pour charger le catalogue");
        
        // Filtres
        if (cmbFilter.Items.Count >= 4)
        {
            ((ComboBoxItem)cmbFilter.Items[0]).Content = L10n.T("modules.filter.all", "Tous");
            ((ComboBoxItem)cmbFilter.Items[1]).Content = L10n.T("modules.filter.installed", "Installés");
            ((ComboBoxItem)cmbFilter.Items[2]).Content = L10n.T("modules.filter.available", "Disponibles");
            ((ComboBoxItem)cmbFilter.Items[3]).Content = L10n.T("modules.filter.updates", "Mises à jour");
        }
        
        // Chemin modules
        var modulesPath = ModuleDiscovery.ModulesPath ?? "N/A";
        txtModulesPath.Text = $"{L10n.T("modules.manager.folder", "Dossier modules")}: {modulesPath} ";
    }
    
    /// <summary>
    /// Actualise la liste des modules depuis le catalogue
    /// </summary>
    private async Task RefreshModulesAsync()
    {
        if (_isLoading) return;
        _isLoading = true;
        
        SetLoadingState(true, L10n.T("modules.manager.loading", "Chargement du catalogue..."));
        _allModules.Clear();
        _filteredModules.Clear();
        
        try
        {
            var result = await UpdateService.CheckForUpdatesAsync();
            
            if (!result.Success)
            {
                ShowError(result.ErrorMessage);
                return;
            }
            
            _manifest = result.Manifest;
            
            if (_manifest == null)
            {
                ShowEmpty(L10n.T("modules.manager.noManifest", "Catalogue vide ou inaccessible"));
                return;
            }
            
            // Construire la liste des modules
            foreach (var moduleDef in _manifest.Modules)
            {
                var installedModule = ModuleDiscovery.Modules
                    .FirstOrDefault(m => m.Id.Equals(moduleDef.Id, StringComparison.OrdinalIgnoreCase));
                    
                var isInstalled = installedModule != null;
                var updateInfo = result.Updates.FirstOrDefault(u => u.ModuleId == moduleDef.Id);
                var hasUpdate = updateInfo != null && !updateInfo.IsNewInstall;
                
                var vm = new ModuleViewModel
                {
                    Definition = moduleDef,
                    Id = moduleDef.Id,
                    Name = moduleDef.Name,
                    Description = moduleDef.Description,
                    Author = moduleDef.Author,
                    Category = GetCategoryFromDefinition(moduleDef),
                    RemoteVersion = moduleDef.Version,
                    LocalVersion = installedModule?.Version,
                    IsInstalled = isInstalled,
                    HasUpdate = hasUpdate,
                };
                
                vm.UpdateDisplay();
                _allModules.Add(vm);
            }
            
            ApplyFilter();
            
            // Mise à jour du statut
            var installedCount = _allModules.Count(m => m.IsInstalled);
            var updateCount = _allModules.Count(m => m.HasUpdate);
            txtStatus.Text = L10n.TFormat("modules.manager.status", 
                "{0} module(s) disponible(s), {1} installé(s), {2} mise(s) à jour",
                _allModules.Count, installedCount, updateCount);
        }
        catch (Exception ex)
        {
            Logger.Error($"Module refresh error: {ex}");
            ShowError(ex.Message);
        }
        finally
        {
            _isLoading = false;
            SetLoadingState(false);
        }
    }
    
    /// <summary>
    /// Extrait la catégorie du module (depuis les tags ou un champ dédié)
    /// </summary>
    private string GetCategoryFromDefinition(ModuleDefinition def)
    {
        // Le marketplace.json n'a pas de champ category dans ModuleDefinition actuel
        // On pourrait l'ajouter ou utiliser le premier tag
        return def.GetType().GetProperty("Category")?.GetValue(def)?.ToString() ?? "Extension";
    }
    
    /// <summary>
    /// Applique le filtre sélectionné
    /// </summary>
    private void ApplyFilter()
    {
        _filteredModules.Clear();
        
        var filterTag = (cmbFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";
        
        IEnumerable<ModuleViewModel> filtered = filterTag switch
        {
            "installed" => _allModules.Where(m => m.IsInstalled),
            "available" => _allModules.Where(m => !m.IsInstalled),
            "updates" => _allModules.Where(m => m.HasUpdate),
            _ => _allModules
        };
        
        foreach (var module in filtered.OrderBy(m => m.Name))
        {
            _filteredModules.Add(module);
        }
        
        // Afficher le message vide si nécessaire
        pnlEmpty.Visibility = _filteredModules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        lstModules.Visibility = _filteredModules.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        
        if (_filteredModules.Count == 0 && _allModules.Count > 0)
        {
            txtEmptyMessage.Text = L10n.T("modules.manager.noMatch", "Aucun module ne correspond au filtre");
        }
    }
    
    /// <summary>
    /// Affiche l'état de chargement
    /// </summary>
    private void SetLoadingState(bool isLoading, string? message = null)
    {
        pnlProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
        btnRefresh.IsEnabled = !isLoading;
        
        if (isLoading && message != null)
        {
            txtProgress.Text = message;
        }
        
        if (isLoading)
        {
            pnlEmpty.Visibility = Visibility.Collapsed;
        }
    }
    
    /// <summary>
    /// Affiche une erreur
    /// </summary>
    private void ShowError(string message)
    {
        txtStatus.Text = L10n.T("modules.manager.error", "Erreur de chargement");
        txtStatus.Foreground = Brushes.Red;
        
        pnlEmpty.Visibility = Visibility.Visible;
        lstModules.Visibility = Visibility.Collapsed;
        txtEmptyMessage.Text = $"❌ {message}";
    }
    
    /// <summary>
    /// Affiche le message vide
    /// </summary>
    private void ShowEmpty(string message)
    {
        pnlEmpty.Visibility = Visibility.Visible;
        lstModules.Visibility = Visibility.Collapsed;
        txtEmptyMessage.Text = message;
    }
    
    // === ÉVÉNEMENTS ===
    
    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshModulesAsync();
    }
    
    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }
    
    private async void OnModuleActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not ModuleViewModel module)
            return;
            
        if (!module.CanPerformAction)
            return;
            
        try
        {
            btn.IsEnabled = false;
            var originalText = module.ActionText;
            module.ActionText = L10n.T("modules.manager.downloading", "Téléchargement...");
            btn.Content = module.ActionText;
            
            SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0}...", module.Name));
            
            await UpdateService.InstallModuleAsync(module.Definition);
            
            // Succès
            module.IsInstalled = true;
            module.HasUpdate = false;
            module.LocalVersion = module.RemoteVersion;
            module.UpdateDisplay();
            
            btn.Content = module.ActionText;
            btn.IsEnabled = module.CanPerformAction;
            
            Logger.Success(L10n.TFormat("modules.manager.installed", "Module {0} installé avec succès", module.Name));
            
            // Message de redémarrage
            MessageBox.Show(
                L10n.TFormat("modules.manager.restartRequired", 
                    "Le module {0} a été installé.\n\nRedémarrez AutoCAD pour l'activer.", module.Name),
                L10n.T("modules.manager.installSuccess", "Installation réussie"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Module install error: {ex}");
            MessageBox.Show(
                L10n.TFormat("modules.manager.installError", "Erreur lors de l'installation: {0}", ex.Message),
                L10n.T("cmd.error", "Erreur"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
                
            module.ActionText = L10n.T("modules.manager.retry", "Réessayer");
            btn.Content = module.ActionText;
            btn.IsEnabled = true;
        }
        finally
        {
            SetLoadingState(false);
        }
    }
    
    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = ModuleDiscovery.ModulesPath;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show(
                    L10n.T("modules.manager.folderNotFound", "Le dossier des modules n'existe pas encore."),
                    L10n.T("cmd.error", "Erreur"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Open folder error: {ex.Message}");
        }
    }
    
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// ViewModel pour un module dans la liste
/// </summary>
public class ModuleViewModel
{
    public ModuleDefinition Definition { get; set; } = new();
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string? RemoteVersion { get; set; }
    public string? LocalVersion { get; set; }
    public bool IsInstalled { get; set; }
    public bool HasUpdate { get; set; }
    
    // Propriétés calculées pour l'affichage
    public string VersionDisplay { get; set; } = "";
    public string StatusIcon { get; set; } = "";
    public System.Windows.Media.Brush StatusBackground { get; set; } = Brushes.Gray;
    public string ActionText { get; set; } = "";
    public bool CanPerformAction { get; set; }
    public System.Windows.Media.Brush ActionBackground { get; set; } = Brushes.LightGray;
    public System.Windows.Media.Brush ActionForeground { get; set; } = Brushes.Black;
    
    /// <summary>
    /// Met à jour les propriétés d'affichage selon l'état
    /// </summary>
    public void UpdateDisplay()
    {
        if (IsInstalled)
        {
            if (HasUpdate)
            {
                // Mise à jour disponible
                VersionDisplay = $"v{LocalVersion} → v{RemoteVersion}";
                StatusIcon = "↑";
                StatusBackground = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                ActionText = L10n.T("modules.action.update", "Mettre à jour");
                CanPerformAction = true;
                ActionBackground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                ActionForeground = Brushes.White;
            }
            else
            {
                // Installé et à jour
                VersionDisplay = $"v{LocalVersion}";
                StatusIcon = "✓";
                StatusBackground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Vert
                ActionText = L10n.T("modules.action.installed", "Installé");
                CanPerformAction = false;
                ActionBackground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                ActionForeground = Brushes.Gray;
            }
        }
        else
        {
            // Non installé
            VersionDisplay = $"v{RemoteVersion}";
            StatusIcon = "+";
            StatusBackground = new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Bleu
            ActionText = L10n.T("modules.action.install", "Installer");
            CanPerformAction = true;
            ActionBackground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
            ActionForeground = Brushes.White;
        }
    }
}
