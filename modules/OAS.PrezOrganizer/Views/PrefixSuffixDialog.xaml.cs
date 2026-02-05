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

using System.Windows;
using System.Windows.Controls;
using OpenAsphalte.Modules.PrezOrganizer.Models;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.PrezOrganizer.Views;

/// <summary>
/// Boîte de dialogue pour ajouter un préfixe et/ou suffixe aux noms de présentations.
/// Prévisualisation en temps réel du résultat.
/// </summary>
public partial class PrefixSuffixDialog : Window
{
    private readonly List<LayoutItem> _allItems;
    private readonly List<LayoutItem> _selectedItems;

    /// <summary>
    /// Préfixe saisi.
    /// </summary>
    public string Prefix => PrefixTextBox.Text ?? string.Empty;

    /// <summary>
    /// Suffixe saisi.
    /// </summary>
    public string Suffix => SuffixTextBox.Text ?? string.Empty;

    /// <summary>
    /// True si on applique à toutes les présentations, false si sélection uniquement.
    /// </summary>
    public bool ApplyToAll => ScopeAll.IsChecked == true;

    /// <summary>
    /// Crée le dialogue Préfixe/Suffixe.
    /// </summary>
    /// <param name="allItems">Toutes les présentations</param>
    /// <param name="selectedItems">Présentations sélectionnées</param>
    public PrefixSuffixDialog(List<LayoutItem> allItems, List<LayoutItem> selectedItems)
    {
        InitializeComponent();

        _allItems = allItems;
        _selectedItems = selectedItems;

        // Si aucune sélection, forcer "Toutes"
        if (_selectedItems.Count == 0)
        {
            ScopeAll.IsChecked = true;
            ScopeSelected.IsEnabled = false;
        }

        ApplyTranslations();

        Loaded += (s, e) => PrefixTextBox.Focus();
    }

    private void ApplyTranslations()
    {
        Title = T("prezorganizer.prefix.title");
        PrefixLabel.Text = T("prezorganizer.prefix.prefix");
        SuffixLabel.Text = T("prezorganizer.prefix.suffix");
        ScopeLabel.Text = T("prezorganizer.prefix.scope");
        ScopeSelected.Content = T("prezorganizer.prefix.scope.selected");
        ScopeAll.Content = T("prezorganizer.prefix.scope.all");
        PreviewHeader.Text = T("prezorganizer.prefix.preview");
        ColBefore.Header = T("prezorganizer.prefix.previewCol.before");
        ColAfter.Header = T("prezorganizer.prefix.previewCol.after");
        ApplyButton.Content = T("prezorganizer.prefix.apply");
        CancelButton.Content = T("prezorganizer.prefix.cancel");
    }

    private void OnFieldChanged(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewListView == null) return;

        PreviewListView.Items.Clear();

        string prefix = PrefixTextBox.Text ?? string.Empty;
        string suffix = SuffixTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
        {
            ApplyButton.IsEnabled = false;
            return;
        }

        var targets = (ScopeAll.IsChecked == true)
            ? _allItems.Where(i => !i.IsMarkedForDeletion)
            : _selectedItems.Where(i => !i.IsMarkedForDeletion);

        bool hasChanges = false;

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
                hasChanges = true;
            }
        }

        ApplyButton.IsEnabled = hasChanges;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
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
