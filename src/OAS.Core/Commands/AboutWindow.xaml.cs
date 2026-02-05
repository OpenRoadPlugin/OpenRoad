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

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using OpenAsphalte.Configuration;
using OpenAsphalte.Discovery;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Commands;

/// <summary>
/// Fenêtre "à propos" d'Open Asphalte
/// </summary>
public partial class AboutWindow : Window
{
    private string _updateUrl = "";

    public AboutWindow()
    {
        InitializeComponent();
        LoadLogo();
        LoadVersionInfo();
        UpdateLocalizedText();
    }

    /// <summary>
    /// Charge le logo depuis les ressources
    /// </summary>
    private void LoadLogo()
    {
        try
        {
            var logoUri = new Uri("pack://application:,,,/OAS.Core;component/Resources/OAS_Logo.png");
            imgLogo.Source = new BitmapImage(logoUri);
        }
        catch (System.Exception ex)
        {
            // Logo non trouvé, laisser vide
            Logger.Debug($"Logo loading failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Charge les informations de version depuis version.json
    /// </summary>
    private void LoadVersionInfo()
    {
        string channel = "release";

        try
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var versionFile = Path.Combine(basePath, "version.json");

            if (File.Exists(versionFile))
            {
                var json = File.ReadAllText(versionFile);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                txtVersion.Text = GetJsonString(root, "version", Plugin.Version);
                txtBuildDate.Text = GetJsonString(root, "build", "");
                channel = GetJsonString(root, "channel", "release");
                txtChannel.Text = channel;
                txtFramework.Text = GetJsonString(root, "framework", "net8.0-windows");
                _updateUrl = GetJsonString(root, "updateUrl", Configuration.Configuration.UpdateUrl);
            }
            else
            {
                txtVersion.Text = Plugin.Version;
                txtBuildDate.Text = "";
                txtChannel.Text = "release";
                txtFramework.Text = "net8.0-windows";
                _updateUrl = Configuration.Configuration.UpdateUrl;
            }

            // Informations dynamiques
            var modules = ModuleDiscovery.Modules;
            var commands = ModuleDiscovery.AllCommands;
            txtModules.Text = modules.Count.ToString();
            txtCommands.Text = commands.Count.ToString();
        }
        catch
        {
            txtVersion.Text = Plugin.Version;
            _updateUrl = Configuration.Configuration.UpdateUrl;
        }

        // Afficher le badge warning pour les versions pre-release
        UpdateChannelWarningBadge(channel);
    }

    /// <summary>
    /// Met à jour le badge d'avertissement selon le canal de release
    /// </summary>
    private void UpdateChannelWarningBadge(string channel)
    {
        var lowerChannel = channel.ToLowerInvariant();

        if (lowerChannel == "alpha")
        {
            badgeWarning.Visibility = Visibility.Visible;
            badgeWarning.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(231, 76, 60)); // Rouge
            txtBadge.Text = "⚠ ALPHA";
        }
        else if (lowerChannel == "beta")
        {
            badgeWarning.Visibility = Visibility.Visible;
            badgeWarning.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(243, 156, 18)); // Orange
            txtBadge.Text = "⚠ BETA";
        }
        else
        {
            badgeWarning.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Extrait une valeur string d'un JsonElement
    /// </summary>
    private static string GetJsonString(JsonElement element, string property, string defaultValue)
    {
        if (element.TryGetProperty(property, out var prop))
        {
            return prop.GetString() ?? defaultValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Met à jour les textes localisés
    /// </summary>
    private void UpdateLocalizedText()
    {
        Title = L10n.T("about.title");
        txtSubtitle.Text = L10n.T("about.subtitle");

        lblVersion.Text = L10n.T("about.version");
        lblBuildDate.Text = L10n.T("about.buildDate");
        lblChannel.Text = L10n.T("about.channel");
        lblFramework.Text = L10n.T("about.framework");
        lblModules.Text = L10n.T("about.modules");
        lblCommands.Text = L10n.T("about.commands");

        txtLicense.Text = L10n.T("about.license");

        btnUpdate.Content = "⬇ " + L10n.T("about.checkUpdate");
        btnCredits.Content = "❤ " + L10n.T("about.credits", "Credits");
        btnReportBug.Content = "🐛 " + L10n.T("about.reportBug");
        btnClose.Content = L10n.T("about.close");
    }

    private void OnCreditsClick(object sender, RoutedEventArgs e)
    {
        var creditsWindow = new OpenAsphalte.Commands.CreditsWindow();
        creditsWindow.Owner = this;
        creditsWindow.ShowDialog();
    }

    /// <summary>
    /// Ouvre la page des issues GitHub pour signaler un bug
    /// </summary>
    private void OnReportBugClick(object sender, RoutedEventArgs e)
    {
        const string issuesUrl = "https://github.com/OpenAsphaltePlugin/OpenAsphalte/issues";
        try
        {
            Process.Start(new ProcessStartInfo(issuesUrl) { UseShellExecute = true });
            Logger.Info(L10n.T("about.reportBug.opening"));
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(
                L10n.TFormat("about.reportBug.error", ex.Message),
                L10n.T("cmd.error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Ouvre la page des mises a jour
    /// </summary>
    private void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!UrlValidationService.IsValidUpdateUrl(_updateUrl))
            {
                System.Windows.MessageBox.Show(
                    L10n.T("about.invalidUrl"),
                    L10n.T("cmd.error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo(_updateUrl) { UseShellExecute = true });
            Logger.Info(L10n.TFormat("system.update.opening"));
        }
        catch (System.Exception ex)
        {
            System.Windows.MessageBox.Show(
                L10n.TFormat("system.update.openError", ex.Message),
                L10n.T("cmd.error"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Ferme la fen�tre
    /// </summary>
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
