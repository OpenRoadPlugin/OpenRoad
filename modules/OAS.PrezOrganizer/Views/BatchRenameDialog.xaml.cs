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

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using OpenAsphalte.Modules.PrezOrganizer.Models;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.PrezOrganizer.Views;

/// <summary>
/// Boîte de dialogue pour le renommage par lot (batch rename) avec patterns.
/// Variables supportées : {N}, {N:00}, {ORIG}, {DATE}.
/// Prévisualisation en temps réel.
/// </summary>
public partial class BatchRenameDialog : Window
{
    private readonly List<LayoutItem> _allItems;
    private readonly List<LayoutItem> _selectedItems;

    /// <summary>
    /// Pattern de renommage saisi.
    /// </summary>
    public string Pattern => PatternTextBox.Text ?? string.Empty;

    /// <summary>
    /// Numéro de départ pour {N}.
    /// </summary>
    public int StartNumber { get; private set; } = 1;

    /// <summary>
    /// Incrément pour {N}.
    /// </summary>
    public int Increment { get; private set; } = 1;

    /// <summary>
    /// True si on applique à toutes les présentations.
    /// </summary>
    public bool ApplyToAll => ScopeAll.IsChecked == true;

    /// <summary>
    /// Crée le dialogue de batch rename.
    /// </summary>
    /// <param name="allItems">Toutes les présentations</param>
    /// <param name="selectedItems">Présentations sélectionnées</param>
    public BatchRenameDialog(List<LayoutItem> allItems, List<LayoutItem> selectedItems)
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
        UpdatePreview();

        Loaded += (s, e) =>
        {
            PatternTextBox.Focus();
            PatternTextBox.SelectAll();
        };
    }

    private void ApplyTranslations()
    {
        Title = T("prezorganizer.batch.title");
        PatternLabel.Text = T("prezorganizer.batch.pattern");
        PatternHelp.Text = T("prezorganizer.batch.pattern.tooltip");
        StartNumLabel.Text = T("prezorganizer.batch.startNum");
        IncrementLabel.Text = T("prezorganizer.batch.increment");
        ScopeLabel.Text = T("prezorganizer.batch.scope");
        ScopeSelected.Content = T("prezorganizer.batch.scope.selected");
        ScopeAll.Content = T("prezorganizer.batch.scope.all");
        PreviewHeader.Text = T("prezorganizer.batch.preview");
        ColBefore.Header = T("prezorganizer.batch.previewCol.before");
        ColAfter.Header = T("prezorganizer.batch.previewCol.after");
        ApplyButton.Content = T("prezorganizer.batch.apply");
        CancelButton.Content = T("prezorganizer.batch.cancel");
    }

    private void OnFieldChanged(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (PreviewListView == null) return;

        PreviewListView.Items.Clear();

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

        StartNumber = startNum;
        Increment = increment;

        // Déterminer les items cibles
        var targets = (ScopeAll.IsChecked == true)
            ? _allItems.Where(i => !i.IsMarkedForDeletion).ToList()
            : _selectedItems.Where(i => !i.IsMarkedForDeletion).ToList();

        // Générer la prévisualisation
        int number = startNum;
        bool hasChanges = false;

        foreach (var item in targets)
        {
            string newName = ApplyPattern(pattern, item.CurrentName, number);
            PreviewListView.Items.Add(new PreviewRow
            {
                Before = item.CurrentName,
                After = newName
            });

            if (newName != item.CurrentName)
                hasChanges = true;

            number += increment;
        }

        ApplyButton.IsEnabled = hasChanges && targets.Count > 0;
    }

    /// <summary>
    /// Applique le pattern de renommage à un nom.
    /// </summary>
    private static string ApplyPattern(string pattern, string originalName, int number)
    {
        string result = pattern;

        // {ORIG} → nom actuel
        result = result.Replace("{ORIG}", originalName, StringComparison.OrdinalIgnoreCase);

        // {DATE} → date courte
        result = result.Replace("{DATE}", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);

        // {N:format} → numéro formaté (ex: {N:000} → 001, 002...)
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

        // {N} → numéro simple
        result = result.Replace("{N}", number.ToString(), StringComparison.OrdinalIgnoreCase);

        return result;
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
