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

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using OpenAsphalte.Modules.PrezOrganizer.Models;
using OpenAsphalte.Modules.PrezOrganizer.Services;
using OpenAsphalte.UI;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.PrezOrganizer.Views;

/// <summary>
/// Boîte de dialogue unifiée pour le renommage des présentations.
/// Combine les fonctionnalités de préfixe/suffixe et de pattern avec variables.
/// Variables supportées : {N}, {N:00}, {ORIG}, {DATE}.
/// </summary>
public partial class RenameToolDialog : Window
{
    private readonly List<LayoutItem> _allItems;
    private readonly List<LayoutItem> _selectedItems;
    private readonly Dictionary<LayoutItem, string> _pendingChanges = new();

    /// <summary>
    /// Indique si des modifications ont été faites et confirmées.
    /// </summary>
    public bool HasChanges => _pendingChanges.Count > 0;

    /// <summary>
    /// Crée le dialogue de renommage.
    /// </summary>
    /// <param name="allItems">Toutes les présentations</param>
    /// <param name="selectedItems">Présentations sélectionnées</param>
    public RenameToolDialog(List<LayoutItem> allItems, List<LayoutItem> selectedItems)
    {
        InitializeComponent();

        _allItems = allItems;
        _selectedItems = selectedItems;

        // Restaurer la taille/position de la fenêtre
        WindowStateHelper.RestoreState(this, "prezorganizer.renametool", 520, 550);
        Closing += (s, e) => WindowStateHelper.SaveState(this, "prezorganizer.renametool");

        // Si aucune sélection, forcer "Toutes"
        if (_selectedItems.Count == 0)
        {
            ScopeAll.IsChecked = true;
            ScopeSelected.IsEnabled = false;
        }

        ApplyTranslations();
        UpdatePreview();

        Loaded += (s, e) =>
        {
            PrefixTextBox.Focus();
        };
    }

    private void ApplyTranslations()
    {
        Title = T("prezorganizer.renameTool.title");
        ModeLabel.Text = T("prezorganizer.renameTool.mode");
        ModePrefixSuffix.Content = T("prezorganizer.renameTool.mode.prefixSuffix");
        ModePattern.Content = T("prezorganizer.renameTool.mode.pattern");

        PrefixLabel.Text = T("prezorganizer.renameTool.prefix");
        SuffixLabel.Text = T("prezorganizer.renameTool.suffix");

        PatternLabel.Text = T("prezorganizer.renameTool.pattern");
        PatternHelp.Text = T("prezorganizer.renameTool.pattern.help");
        StartNumLabel.Text = T("prezorganizer.renameTool.startNum");
        IncrementLabel.Text = T("prezorganizer.renameTool.increment");

        ScopeLabel.Text = T("prezorganizer.renameTool.scope");
        ScopeSelected.Content = T("prezorganizer.renameTool.scope.selected");
        ScopeAll.Content = T("prezorganizer.renameTool.scope.all");

        PreviewHeader.Text = T("prezorganizer.renameTool.preview");
        ColBefore.Header = T("prezorganizer.renameTool.preview.before");
        ColAfter.Header = T("prezorganizer.renameTool.preview.after");

        ApplyButton.Content = T("prezorganizer.renameTool.apply");
        CancelButton.Content = T("prezorganizer.renameTool.cancel");
    }

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        if (ModePrefixSuffix == null || ModePattern == null) return;

        bool isPrefixSuffixMode = ModePrefixSuffix.IsChecked == true;
        PrefixSuffixPanel.Visibility = isPrefixSuffixMode ? Visibility.Visible : Visibility.Collapsed;
        PatternPanel.Visibility = isPrefixSuffixMode ? Visibility.Collapsed : Visibility.Visible;

        UpdatePreview();
    }

    private void OnFieldChanged(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void OnFieldChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewListView == null) return;

        PreviewListView.Items.Clear();
        _pendingChanges.Clear();

        // Déterminer les items cibles
        var targets = (ScopeAll.IsChecked == true)
            ? _allItems.Where(i => !i.IsMarkedForDeletion).ToList()
            : _selectedItems.Where(i => !i.IsMarkedForDeletion).ToList();

        if (targets.Count == 0)
        {
            ApplyButton.IsEnabled = false;
            return;
        }

        bool isPrefixSuffixMode = ModePrefixSuffix.IsChecked == true;
        bool hasChanges = false;

        if (isPrefixSuffixMode)
        {
            string prefix = PrefixTextBox.Text ?? string.Empty;
            string suffix = SuffixTextBox.Text ?? string.Empty;

            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
            {
                ApplyButton.IsEnabled = false;
                return;
            }

            foreach (var item in targets)
            {
                string newName = $"{prefix}{item.CurrentName}{suffix}";
                if (newName != item.CurrentName)
                {
                    PreviewListView.Items.Add(new PreviewRow
                    {
                        Before = item.CurrentName,
                        After = newName
                    });
                    _pendingChanges[item] = newName;
                    hasChanges = true;
                }
            }
        }
        else
        {
            string pattern = PatternTextBox.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(pattern))
            {
                ApplyButton.IsEnabled = false;
                return;
            }

            // Parser les paramètres numériques
            if (!int.TryParse(StartNumTextBox.Text, out int startNum))
                startNum = 1;
            if (!int.TryParse(IncrementTextBox.Text, out int increment) || increment == 0)
                increment = 1;

            int number = startNum;

            foreach (var item in targets)
            {
                string newName = ApplyPattern(pattern, item.CurrentName, number);
                PreviewListView.Items.Add(new PreviewRow
                {
                    Before = item.CurrentName,
                    After = newName
                });

                if (newName != item.CurrentName)
                {
                    _pendingChanges[item] = newName;
                    hasChanges = true;
                }

                number += increment;
            }
        }

        ApplyButton.IsEnabled = hasChanges;
    }

    /// <summary>
    /// Applique le pattern de renommage à un nom.
    /// </summary>
    private static string ApplyPattern(string pattern, string originalName, int number)
    {
        string result = pattern;

        // {ORIG} ? nom actuel
        result = result.Replace("{ORIG}", originalName, StringComparison.OrdinalIgnoreCase);

        // {DATE} ? date courte
        result = result.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);

        // {N:format} ? numéro formaté (ex: {N:000} ? 001, 002...)
        result = Regex.Replace(result, @"\{N:([^}]+)\}", m =>
        {
            string format = m.Groups[1].Value;
            try
            {
                return number.ToString(format);
            }
            catch
            {
                return number.ToString();
            }
        }, RegexOptions.IgnoreCase);

        // {N} ? numéro simple
        result = result.Replace("{N}", number.ToString(), StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        // Valider tous les nouveaux noms avant d'appliquer
        var allNames = _allItems
            .Where(i => !i.IsMarkedForDeletion)
            .Select(i => _pendingChanges.TryGetValue(i, out var newName) ? newName : i.CurrentName)
            .ToList();

        foreach (var (item, newName) in _pendingChanges)
        {
            var otherNames = allNames.Where(n => n != newName || _pendingChanges.Count(x => x.Value == newName) > 1);
            var (isValid, error) = LayoutService.ValidateName(newName, otherNames, item.CurrentName);

            if (!isValid)
            {
                MessageBox.Show(
                    $"{T("prezorganizer.renameTool.error.invalid")}\n\n{item.CurrentName} ? {newName}\n\n{T(error!)}",
                    T("prezorganizer.renameTool.title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        // Appliquer les modifications aux items
        foreach (var (item, newName) in _pendingChanges)
        {
            item.CurrentName = newName;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string T(string key, string? defaultValue = null) => L10n.T(key, defaultValue ?? key);

    private class PreviewRow
    {
        public string Before { get; init; } = string.Empty;
        public string After { get; init; } = string.Empty;
    }
}
