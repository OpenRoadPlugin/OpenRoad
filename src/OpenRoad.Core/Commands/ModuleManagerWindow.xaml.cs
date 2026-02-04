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
using Brush = System.Windows.Media.Brush;
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
    private ObservableCollection<CategoryViewModel> _categories = new();
    private MarketplaceManifest? _manifest;
    private bool _isLoading = false;

    public ModuleManagerWindow()
    {
        InitializeComponent();
        lstCategories.ItemsSource = _categories;
        UpdateLocalizedText();

        // Charger automatiquement dès l'ouverture
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
        btnRefresh.ToolTip = L10n.T("modules.manager.refresh", "Actualiser");
        txtFilter.Text = L10n.T("modules.manager.filter", "Filtrer:");
        btnClose.Content = L10n.T("settings.cancel", "Fermer");
        txtEmptyMessage.Text = L10n.T("modules.manager.loading", "Chargement du catalogue...");

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
        _categories.Clear();

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
    /// Extrait la catégorie du module
    /// </summary>
    private string GetCategoryFromDefinition(ModuleDefinition def)
    {
        // Utiliser la réflexion pour accéder à la propriété Category si elle existe
        var categoryProp = def.GetType().GetProperty("Category");
        if (categoryProp != null)
        {
            var value = categoryProp.GetValue(def)?.ToString();
            if (!string.IsNullOrEmpty(value))
                return value;
        }
        return "Extension";
    }

    /// <summary>
    /// Applique le filtre sélectionné et regroupe par catégorie
    /// </summary>
    private void ApplyFilter()
    {
        _categories.Clear();

        var filterTag = (cmbFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";

        IEnumerable<ModuleViewModel> filtered = filterTag switch
        {
            "installed" => _allModules.Where(m => m.IsInstalled),
            "available" => _allModules.Where(m => !m.IsInstalled),
            "updates" => _allModules.Where(m => m.HasUpdate),
            _ => _allModules
        };

        // Grouper par catégorie
        var groups = filtered
            .GroupBy(m => m.Category)
            .OrderBy(g => g.Key);

        foreach (var group in groups)
        {
            var categoryVm = new CategoryViewModel
            {
                Name = group.Key,
                Icon = GetCategoryIcon(group.Key),
                Modules = new ObservableCollection<ModuleViewModel>(group.OrderBy(m => m.Name))
            };
            categoryVm.UpdateDisplay();
            _categories.Add(categoryVm);
        }

        // Afficher le message vide si nécessaire
        var hasModules = _categories.Any();
        pnlEmpty.Visibility = hasModules ? Visibility.Collapsed : Visibility.Visible;
        lstCategories.Visibility = hasModules ? Visibility.Visible : Visibility.Collapsed;

        if (!hasModules && _allModules.Count > 0)
        {
            txtEmptyIcon.Text = "🔍";
            txtEmptyMessage.Text = L10n.T("modules.manager.noMatch", "Aucun module ne correspond au filtre");
        }
    }

    /// <summary>
    /// Retourne l'icône pour une catégorie
    /// </summary>
    private string GetCategoryIcon(string category)
    {
        return category.ToLower() switch
        {
            "cartographie" => "🗺️",
            "voirie" => "🛣️",
            "topographie" => "📐",
            "hydraulique" => "💧",
            "réseaux" => "🔌",
            "dessin" => "✏️",
            "import/export" => "📁",
            _ => "📦"
        };
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
            pnlEmpty.Visibility = Visibility.Visible;
            lstCategories.Visibility = Visibility.Collapsed;
            txtEmptyIcon.Text = "⏳";
            txtEmptyMessage.Text = message ?? L10n.T("modules.manager.loading", "Chargement...");
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
        lstCategories.Visibility = Visibility.Collapsed;
        txtEmptyIcon.Text = "❌";
        txtEmptyMessage.Text = message;
    }

    /// <summary>
    /// Affiche le message vide
    /// </summary>
    private void ShowEmpty(string message)
    {
        pnlEmpty.Visibility = Visibility.Visible;
        lstCategories.Visibility = Visibility.Collapsed;
        txtEmptyIcon.Text = "📦";
        txtEmptyMessage.Text = message;
    }

    // === ÉVÉNEMENTS ===

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await RefreshModulesAsync();
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_allModules.Count > 0)
            ApplyFilter();
    }

    /// <summary>
    /// Récupère les dépendances manquantes pour un module
    /// </summary>
    private List<ModuleDefinition> GetMissingDependencies(ModuleDefinition moduleDef)
    {
        var missing = new List<ModuleDefinition>();

        if (moduleDef.Dependencies == null || moduleDef.Dependencies.Count == 0)
            return missing;

        foreach (var depId in moduleDef.Dependencies)
        {
            // Vérifier si la dépendance est déjà installée (chargée en mémoire)
            var isLoadedInMemory = ModuleDiscovery.Modules
                .Any(m => m.Id.Equals(depId, StringComparison.OrdinalIgnoreCase));

            // Vérifier si la dépendance a été installée dans cette session (téléchargée mais pas encore chargée)
            var isInstalledThisSession = _allModules
                .Any(m => m.Id.Equals(depId, StringComparison.OrdinalIgnoreCase) && m.IsInstalled);

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
                var depDef = _manifest?.Modules
                    .FirstOrDefault(m => m.Id.Equals(depId, StringComparison.OrdinalIgnoreCase));

                if (depDef != null)
                {
                    // Éviter les doublons
                    if (!missing.Any(m => m.Id.Equals(depDef.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        missing.Add(depDef);

                        // Récursivement vérifier les dépendances de la dépendance
                        var subDeps = GetMissingDependencies(depDef);
                        foreach (var subDep in subDeps)
                        {
                            if (!missing.Any(m => m.Id.Equals(subDep.Id, StringComparison.OrdinalIgnoreCase)))
                            {
                                missing.Add(subDep);
                            }
                        }
                    }
                }
            }
        }

        return missing;
    }

    /// <summary>
    /// Installe un module et ses dépendances
    /// </summary>
    private async Task<List<string>> InstallModuleWithDependenciesAsync(ModuleDefinition moduleDef, List<ModuleDefinition> dependencies)
    {
        var installedModules = new List<string>();

        // D'abord installer les dépendances (dans l'ordre)
        foreach (var dep in dependencies)
        {
            SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0}...", dep.Name));
            await UpdateService.InstallModuleAsync(dep);
            installedModules.Add(dep.Name);

            // Mettre à jour le ViewModel correspondant
            var depVm = _allModules.FirstOrDefault(m => m.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase));
            if (depVm != null)
            {
                depVm.IsInstalled = true;
                depVm.HasUpdate = false;
                depVm.LocalVersion = depVm.RemoteVersion;
                depVm.UpdateDisplay();
            }
        }

        // Ensuite installer le module principal
        SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0}...", moduleDef.Name));
        await UpdateService.InstallModuleAsync(moduleDef);
        installedModules.Add(moduleDef.Name);

        return installedModules;
    }

    private async void OnModuleActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not ModuleViewModel module)
            return;

        if (!module.CanPerformAction)
            return;

        try
        {
            // Vérifier les dépendances manquantes
            var missingDeps = GetMissingDependencies(module.Definition);

            if (missingDeps.Count > 0)
            {
                // Construire le message de confirmation
                var depNames = string.Join(", ", missingDeps.Select(d => d.Name));
                var message = L10n.TFormat("modules.manager.dependencies.confirm",
                    module.Name, depNames, missingDeps.Count);

                var result = MessageBox.Show(
                    message,
                    L10n.T("modules.manager.dependencies.title", "Dépendances requises"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return; // L'utilisateur a annulé
                }
            }

            btn.IsEnabled = false;
            module.ActionText = L10n.T("modules.manager.downloading", "Téléchargement...");
            btn.Content = module.ActionText;

            List<string> installedModules;

            if (missingDeps.Count > 0)
            {
                // Installer avec les dépendances
                SetLoadingState(true, L10n.T("modules.manager.dependencies.installing", "Installation des dépendances..."));
                installedModules = await InstallModuleWithDependenciesAsync(module.Definition, missingDeps);
            }
            else
            {
                // Installation simple
                SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0}...", module.Name));
                await UpdateService.InstallModuleAsync(module.Definition);
                installedModules = new List<string> { module.Name };
            }

            // Succès
            module.IsInstalled = true;
            module.HasUpdate = false;
            module.LocalVersion = module.RemoteVersion;
            module.UpdateDisplay();

            btn.Content = module.ActionText;
            btn.IsEnabled = module.CanPerformAction;

            // Rafraîchir l'affichage des catégories
            ApplyFilter();

            Logger.Success(L10n.TFormat("modules.manager.installed", "Module {0} installé avec succès", module.Name));

            // Message de redémarrage
            if (installedModules.Count > 1)
            {
                var modulesList = string.Join("\n• ", installedModules);
                MessageBox.Show(
                    L10n.TFormat("modules.manager.restartRequired.multiple",
                        "Les modules suivants ont été installés :\n{0}\n\nRedémarrez AutoCAD pour les activer.",
                        "• " + modulesList),
                    L10n.T("modules.manager.installSuccess", "Installation réussie"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    L10n.TFormat("modules.manager.restartRequired",
                        "Le module {0} a été installé.\n\nRedémarrez AutoCAD pour l'activer.", module.Name),
                    L10n.T("modules.manager.installSuccess", "Installation réussie"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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

    /// <summary>
    /// Installe tous les modules d'une catégorie
    /// </summary>
    private async void OnInstallCategoryClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not CategoryViewModel category)
            return;

        var modulesToInstall = category.Modules.Where(m => m.CanPerformAction && !m.IsInstalled).ToList();

        if (modulesToInstall.Count == 0)
        {
            MessageBox.Show(
                L10n.T("modules.manager.category.allInstalled", "Tous les modules de cette catégorie sont déjà installés."),
                L10n.T("modules.manager.title", "Gestionnaire de Modules"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Confirmer l'installation
        var result = MessageBox.Show(
            L10n.TFormat("modules.manager.category.confirmInstall",
                "Voulez-vous installer les {0} module(s) de la catégorie \"{1}\" ?\n\n• {2}",
                modulesToInstall.Count, category.Name, string.Join("\n• ", modulesToInstall.Select(m => m.Name))),
            L10n.T("modules.manager.category.installAll", "Installer la catégorie"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            btn.IsEnabled = false;
            var installedModules = new List<string>();

            // Collecter toutes les dépendances manquantes
            var allDependencies = new List<ModuleDefinition>();
            foreach (var module in modulesToInstall)
            {
                var deps = GetMissingDependencies(module.Definition);
                foreach (var dep in deps)
                {
                    if (!allDependencies.Any(d => d.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase)) &&
                        !modulesToInstall.Any(m => m.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        allDependencies.Add(dep);
                    }
                }
            }

            // Installer les dépendances d'abord
            foreach (var dep in allDependencies)
            {
                SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0}...", dep.Name));
                await UpdateService.InstallModuleAsync(dep);
                installedModules.Add(dep.Name);

                var depVm = _allModules.FirstOrDefault(m => m.Id.Equals(dep.Id, StringComparison.OrdinalIgnoreCase));
                if (depVm != null)
                {
                    depVm.IsInstalled = true;
                    depVm.HasUpdate = false;
                    depVm.LocalVersion = depVm.RemoteVersion;
                    depVm.UpdateDisplay();
                }
            }

            // Installer les modules de la catégorie
            foreach (var module in modulesToInstall)
            {
                SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0}...", module.Name));
                await UpdateService.InstallModuleAsync(module.Definition);
                installedModules.Add(module.Name);

                module.IsInstalled = true;
                module.HasUpdate = false;
                module.LocalVersion = module.RemoteVersion;
                module.UpdateDisplay();
            }

            // Rafraîchir l'affichage
            ApplyFilter();

            // Message de succès
            var modulesList = string.Join("\n• ", installedModules);
            MessageBox.Show(
                L10n.TFormat("modules.manager.restartRequired.multiple",
                    "Les modules suivants ont été installés :\n{0}\n\nRedémarrez AutoCAD pour les activer.",
                    "• " + modulesList),
                L10n.T("modules.manager.installSuccess", "Installation réussie"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Logger.Success(L10n.TFormat("modules.manager.category.installed",
                "{0} module(s) installé(s) depuis la catégorie {1}", installedModules.Count, category.Name));
        }
        catch (Exception ex)
        {
            Logger.Error($"Category install error: {ex}");
            MessageBox.Show(
                L10n.TFormat("modules.manager.installError", "Erreur lors de l'installation: {0}", ex.Message),
                L10n.T("cmd.error", "Erreur"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            btn.IsEnabled = true;
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
/// ViewModel pour une catégorie de modules
/// </summary>
public class CategoryViewModel
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "📦";
    public ObservableCollection<ModuleViewModel> Modules { get; set; } = new();

    // Propriétés calculées
    public string ModuleCountText { get; set; } = "";
    public string InstallAllText { get; set; } = "";
    public Visibility InstallAllVisibility { get; set; } = Visibility.Collapsed;

    public void UpdateDisplay()
    {
        var total = Modules.Count;
        var installed = Modules.Count(m => m.IsInstalled);
        var available = total - installed;

        ModuleCountText = $"{installed}/{total}";

        if (available > 0)
        {
            InstallAllText = L10n.TFormat("modules.manager.category.installCount", "Installer tout ({0})", available);
            InstallAllVisibility = Visibility.Visible;
        }
        else
        {
            InstallAllVisibility = Visibility.Collapsed;
        }
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
    public Brush StatusBackground { get; set; } = Brushes.Gray;
    public string ActionText { get; set; } = "";
    public bool CanPerformAction { get; set; }
    public Brush ActionBackground { get; set; } = Brushes.LightGray;
    public Brush ActionForeground { get; set; } = Brushes.Black;

    // Dépendances
    public string DependenciesText { get; set; } = "";
    public Visibility HasDependenciesVisibility { get; set; } = Visibility.Collapsed;

    /// <summary>
    /// Met à jour les propriétés d'affichage selon l'état
    /// </summary>
    public void UpdateDisplay()
    {
        // Afficher les dépendances
        if (Definition.Dependencies != null && Definition.Dependencies.Count > 0)
        {
            DependenciesText = $"⚠️ {L10n.T("modules.requires", "Requiert")}: {string.Join(", ", Definition.Dependencies)}";
            HasDependenciesVisibility = IsInstalled ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            HasDependenciesVisibility = Visibility.Collapsed;
        }

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
