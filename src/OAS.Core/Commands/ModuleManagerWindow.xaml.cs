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

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenAsphalte.Discovery;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using L10n = OpenAsphalte.Localization.Localization;
using MessageBox = System.Windows.MessageBox;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using System.Reflection;

namespace OpenAsphalte.Commands;

/// <summary>
/// Statut de compatibilité d'un module avec le Core installé
/// </summary>
public enum ModuleCompatibilityStatus
{
    /// <summary>Module compatible avec le Core actuel</summary>
    Compatible,
    /// <summary>Core trop ancien pour ce module</summary>
    CoreTooOld,
    /// <summary>Core trop récent pour ce module</summary>
    CoreTooNew,
    /// <summary>Module incompatible mais une version antérieure est compatible</summary>
    OlderVersionAvailable
}

/// <summary>
/// Fenêtre de gestion des modules Open Asphalte.
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
        txtSubtitle.Text = L10n.T("modules.manager.subtitle", "Installez et gérez les modules Open Asphalte depuis le catalogue officiel.");
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
            var coreVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

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

                // Vérifier la compatibilité
                var (status, reason, alternative) = CheckModuleCompatibility(moduleDef, coreVersion);
                vm.CompatibilityStatus = status;
                vm.CompatibilityReason = reason;
                vm.AlternativeVersion = alternative;

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
            txtEmptyIcon.Text = "??";
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
            "cartographie" => "???",
            "voirie" => "???",
            "topographie" => "??",
            "hydraulique" => "??",
            "réseaux" => "??",
            "dessin" => "??",
            "import/export" => "??",
            _ => "??"
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
            txtEmptyIcon.Text = "?";
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
        txtEmptyIcon.Text = "?";
        txtEmptyMessage.Text = message;
    }

    /// <summary>
    /// Affiche le message vide
    /// </summary>
    private void ShowEmpty(string message)
    {
        pnlEmpty.Visibility = Visibility.Visible;
        lstCategories.Visibility = Visibility.Collapsed;
        txtEmptyIcon.Text = "??";
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
        if (_manifest == null)
            return new List<ModuleDefinition>();

        // IDs des modules installés dans cette session (pas encore chargés en mémoire)
        var sessionInstalledIds = _allModules
            .Where(m => m.IsInstalled)
            .Select(m => m.Id);

        return ModuleDiscovery.GetMissingDependencies(moduleDef, _manifest, sessionInstalledIds);
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
                    new object[] { module.Name, depNames, missingDeps.Count });

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

    /// <summary>
    /// Vérifie la compatibilité d'un module avec le Core actuel
    /// </summary>
    private (ModuleCompatibilityStatus status, string reason, ModuleVersionInfo? alternative)
        CheckModuleCompatibility(ModuleDefinition moduleDef, Version coreVersion)
    {
        // Vérifier la version principale
        bool isLatestCompatible = IsVersionCompatible(moduleDef.MinCoreVersion, moduleDef.MaxCoreVersion, coreVersion);

        if (isLatestCompatible)
        {
            return (ModuleCompatibilityStatus.Compatible, "", null);
        }

        // La version principale n'est pas compatible - chercher une alternative
        ModuleVersionInfo? alternativeVersion = null;

        if (moduleDef.PreviousVersions != null && moduleDef.PreviousVersions.Count > 0)
        {
            alternativeVersion = moduleDef.PreviousVersions
                .Where(v => IsVersionCompatible(v.MinCoreVersion, v.MaxCoreVersion, coreVersion))
                .OrderByDescending(v =>
                {
                    Version.TryParse(v.Version, out var ver);
                    return ver;
                })
                .FirstOrDefault();
        }

        // Déterminer le type d'incompatibilité
        ModuleCompatibilityStatus status;
        string reason;

        if (Version.TryParse(moduleDef.MinCoreVersion, out var minCore) && coreVersion < minCore)
        {
            status = alternativeVersion != null ? ModuleCompatibilityStatus.OlderVersionAvailable : ModuleCompatibilityStatus.CoreTooOld;
            reason = L10n.TFormat("modules.compatibility.coreTooOld", "Nécessite Core {0}+", moduleDef.MinCoreVersion);
        }
        else if (!string.IsNullOrEmpty(moduleDef.MaxCoreVersion) &&
                 Version.TryParse(moduleDef.MaxCoreVersion, out var maxCore) && coreVersion > maxCore)
        {
            status = alternativeVersion != null ? ModuleCompatibilityStatus.OlderVersionAvailable : ModuleCompatibilityStatus.CoreTooNew;
            reason = L10n.TFormat("modules.compatibility.coreTooNew", "Incompatible Core > {0}", moduleDef.MaxCoreVersion);
        }
        else
        {
            status = ModuleCompatibilityStatus.Compatible;
            reason = "";
        }

        return (status, reason, alternativeVersion);
    }

    private bool IsVersionCompatible(string? minCoreVersion, string? maxCoreVersion, Version coreVersion)
    {
        // Vérifier MinCoreVersion
        if (!string.IsNullOrEmpty(minCoreVersion) && Version.TryParse(minCoreVersion, out var minCore))
        {
            if (coreVersion < minCore) return false;
        }

        // Vérifier MaxCoreVersion
        if (!string.IsNullOrEmpty(maxCoreVersion) && Version.TryParse(maxCoreVersion, out var maxCore))
        {
            if (coreVersion > maxCore) return false;
        }

        return true;
    }

    private async void OnInstallAlternativeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not ModuleViewModel module)
            return;

        if (module.AlternativeVersion == null)
            return;

        try
        {
            btn.IsEnabled = false;

            // Créer une copie de la définition avec la version alternative
            var altDef = new ModuleDefinition
            {
                Id = module.Definition.Id,
                Name = module.Definition.Name,
                Description = module.Definition.Description,
                Author = module.Definition.Author,
                Version = module.AlternativeVersion.Version,
                MinCoreVersion = module.AlternativeVersion.MinCoreVersion,
                MaxCoreVersion = module.AlternativeVersion.MaxCoreVersion,
                DownloadUrl = module.AlternativeVersion.DownloadUrl,
                Dependencies = module.Definition.Dependencies,
                IsCustomSource = module.Definition.IsCustomSource
            };

            SetLoadingState(true, L10n.TFormat("modules.manager.installing", "Installation de {0} v{1}...", module.Name, altDef.Version));
            await UpdateService.InstallModuleAsync(altDef);

            // Succès
            module.IsInstalled = true;
            module.LocalVersion = altDef.Version;
            module.CompatibilityStatus = ModuleCompatibilityStatus.Compatible;
            module.UpdateDisplay();

            ApplyFilter();

            Logger.Success(L10n.TFormat("modules.manager.installed", "Module {0} v{1} installé avec succès", module.Name, altDef.Version));

            MessageBox.Show(
                L10n.TFormat("modules.manager.restartRequired",
                    "Le module {0} v{1} a été installé.\n\nRedémarrez AutoCAD pour l'activer.", module.Name, altDef.Version),
                L10n.T("modules.manager.installSuccess", "Installation réussie"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Logger.Error($"Alternative install error: {ex}");
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
    public string Icon { get; set; } = "??";
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

    // Compatibilité
    public ModuleCompatibilityStatus CompatibilityStatus { get; set; } = ModuleCompatibilityStatus.Compatible;
    public string CompatibilityReason { get; set; } = "";
    public ModuleVersionInfo? AlternativeVersion { get; set; }
    public bool IsCompatible => CompatibilityStatus == ModuleCompatibilityStatus.Compatible;
    public bool HasAlternative => AlternativeVersion != null;

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

    // Compatibilité UI
    public Visibility CompatibilityWarningVisibility { get; set; } = Visibility.Collapsed;
    public Visibility AlternativeButtonVisibility { get; set; } = Visibility.Collapsed;
    public string AlternativeButtonText { get; set; } = "";
    public double ModuleOpacity { get; set; } = 1.0;

    /// <summary>
    /// Met à jour les propriétés d'affichage selon l'état
    /// </summary>
    public void UpdateDisplay()
    {
        // Gestion de la compatibilité
        if (!IsCompatible)
        {
            ModuleOpacity = 0.6;
            CompatibilityWarningVisibility = Visibility.Visible;

            if (HasAlternative)
            {
                AlternativeButtonVisibility = Visibility.Visible;
                AlternativeButtonText = L10n.TFormat("modules.install.alternative", "Installer v{0}", AlternativeVersion!.Version);
            }

            // Version display avec avertissement
            VersionDisplay = $"v{RemoteVersion} ??";
            StatusIcon = "?";
            StatusBackground = CreateFrozenBrush(158, 158, 158); // Gris
            ActionText = CompatibilityReason;
            CanPerformAction = false;
            ActionBackground = CreateFrozenBrush(200, 200, 200);
            ActionForeground = Brushes.Gray;
            return;
        }

        // Reset si compatible
        ModuleOpacity = 1.0;
        CompatibilityWarningVisibility = Visibility.Collapsed;
        AlternativeButtonVisibility = Visibility.Collapsed;

        // Afficher les dépendances
        if (Definition.Dependencies != null && Definition.Dependencies.Count > 0)
        {
            DependenciesText = $"?? {L10n.T("modules.requires", "Requiert")}: {string.Join(", ", Definition.Dependencies)}";
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
                VersionDisplay = $"v{LocalVersion} ? v{RemoteVersion}";
                StatusIcon = "?";
                StatusBackground = CreateFrozenBrush(255, 152, 0); // Orange
                ActionText = L10n.T("modules.action.update", "Mettre à jour");
                CanPerformAction = true;
                ActionBackground = CreateFrozenBrush(255, 152, 0);
                ActionForeground = Brushes.White;
            }
            else
            {
                // Installé et à jour
                VersionDisplay = $"v{LocalVersion}";
                StatusIcon = "?";
                StatusBackground = CreateFrozenBrush(76, 175, 80); // Vert
                ActionText = L10n.T("modules.action.installed", "Installé");
                CanPerformAction = false;
                ActionBackground = CreateFrozenBrush(200, 200, 200);
                ActionForeground = Brushes.Gray;
            }
        }
        else
        {
            // Non installé
            VersionDisplay = $"v{RemoteVersion}";
            StatusIcon = "+";
            StatusBackground = CreateFrozenBrush(33, 150, 243); // Bleu
            ActionText = L10n.T("modules.action.install", "Installer");
            CanPerformAction = true;
            ActionBackground = CreateFrozenBrush(33, 150, 243);
            ActionForeground = Brushes.White;
        }
    }

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
