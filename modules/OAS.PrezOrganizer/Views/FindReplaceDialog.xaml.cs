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

using System.Windows;
using System.Windows.Controls;
using OpenAsphalte.Modules.PrezOrganizer.Models;
using OpenAsphalte.Modules.PrezOrganizer.Services;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.PrezOrganizer.Views;

/// <summary>
/// Boîte de dialogue Chercher et Remplacer dans les noms de présentations.
/// Fournit une prévisualisation en temps réel des modifications.
/// </summary>
public partial class FindReplaceDialog : Window
{
    private readonly List<LayoutItem> _items;
    private List<LayoutItem> _previewItems;

    /// <summary>
    /// Items avec les noms modifiés après remplacement.
    /// </summary>
    public List<LayoutItem> ResultItems => _previewItems;

    /// <summary>
    /// Nombre de remplacements effectués.
    /// </summary>
    public int ChangesMade { get; private set; }

    /// <summary>
    /// Crée le dialogue Find &amp; Replace.
    /// </summary>
    /// <param name="items">Liste des présentations courantes</param>
    public FindReplaceDialog(List<LayoutItem> items)
    {
        InitializeComponent();

        _items = items;
        _previewItems = items.Select(i => i.Clone()).ToList();

        ApplyTranslations();

        Loaded += (s, e) => SearchTextBox.Focus();
    }

    private void ApplyTranslations()
    {
        Title = T("prezorganizer.findrepl.title");
        SearchLabel.Text = T("prezorganizer.findrepl.search");
        ReplaceLabel.Text = T("prezorganizer.findrepl.replace");
        CaseSensitiveCheckBox.Content = T("prezorganizer.findrepl.caseSensitive");
        PreviewHeader.Text = T("prezorganizer.findrepl.preview");
        ColBefore.Header = T("prezorganizer.findrepl.previewCol.before");
        ColAfter.Header = T("prezorganizer.findrepl.previewCol.after");
        ReplaceAllButton.Content = T("prezorganizer.findrepl.replaceAll");
        CancelButton.Content = T("prezorganizer.findrepl.cancel");
    }

    /// <summary>
    /// Met à jour la prévisualisation quand les champs changent.
    /// </summary>
    private void OnFieldChanged(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        string search = SearchTextBox.Text ?? string.Empty;
        string replace = ReplaceTextBox.Text ?? string.Empty;
        bool caseSensitive = CaseSensitiveCheckBox.IsChecked == true;

        PreviewListView.Items.Clear();

        if (string.IsNullOrEmpty(search))
        {
            MatchCountText.Text = string.Empty;
            ReplaceAllButton.IsEnabled = false;
            return;
        }

        // Recréer la prévisualisation
        _previewItems = _items.Select(i => i.Clone()).ToList();
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int matchCount = 0;

        foreach (var item in _previewItems.Where(i => !i.IsMarkedForDeletion))
        {
            string original = item.CurrentName;
            string replaced = ReplaceString(original, search, replace, comparison);

            if (original != replaced)
            {
                item.CurrentName = replaced;
                matchCount++;

                PreviewListView.Items.Add(new PreviewRow
                {
                    Before = original,
                    After = replaced
                });
            }
        }

        if (matchCount == 0)
        {
            MatchCountText.Text = T("prezorganizer.findrepl.noMatch");
            ReplaceAllButton.IsEnabled = false;
        }
        else
        {
            MatchCountText.Text = string.Format(T("prezorganizer.findrepl.matches"), matchCount);
            ReplaceAllButton.IsEnabled = true;
        }

        ChangesMade = matchCount;
    }

    private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChangesMade > 0)
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Remplacement de chaîne avec contrôle de la casse.
    /// </summary>
    private static string ReplaceString(string input, string search, string replace, StringComparison comparison)
    {
        if (string.IsNullOrEmpty(search)) return input;

        var sb = new System.Text.StringBuilder();
        int pos = 0;

        while (pos < input.Length)
        {
            int idx = input.IndexOf(search, pos, comparison);
            if (idx < 0)
            {
                sb.Append(input.AsSpan(pos));
                break;
            }

            sb.Append(input.AsSpan(pos, idx - pos));
            sb.Append(replace);
            pos = idx + search.Length;
        }

        return sb.ToString();
    }

    private static string T(string key, string? defaultValue = null) => L10n.T(key, defaultValue ?? key);

    /// <summary>
    /// Modèle pour les lignes de prévisualisation.
    /// </summary>
    private class PreviewRow
    {
        public string Before { get; init; } = string.Empty;
        public string After { get; init; } = string.Empty;
    }
}
