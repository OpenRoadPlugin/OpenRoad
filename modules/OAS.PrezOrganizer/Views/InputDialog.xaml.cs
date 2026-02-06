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
using System.Windows.Input;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.PrezOrganizer.Views;

/// <summary>
/// Boîte de dialogue simple pour saisir un texte (renommage, ajout...).
/// </summary>
public partial class InputDialog : Window
{
    /// <summary>
    /// Texte saisi par l'utilisateur.
    /// </summary>
    public string ResultText => InputTextBox.Text?.Trim() ?? string.Empty;

    /// <summary>
    /// Crée une boîte de dialogue de saisie.
    /// </summary>
    /// <param name="title">Titre de la fenêtre</param>
    /// <param name="prompt">Texte du label</param>
    /// <param name="defaultValue">Valeur par défaut dans le champ</param>
    public InputDialog(string title, string prompt, string defaultValue = "")
    {
        InitializeComponent();

        Title = title;
        PromptLabel.Text = prompt;
        InputTextBox.Text = defaultValue;

        OkButton.Content = T("prezorganizer.rename.ok", "OK");
        CancelButton.Content = T("prezorganizer.rename.cancel", "Annuler");

        Loaded += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            ErrorText.Text = T("prezorganizer.error.emptyName", "Le nom ne peut pas être vide");
            ErrorText.Visibility = System.Windows.Visibility.Visible;
            InputTextBox.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OkButton_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            CancelButton_Click(sender, e);
        }

        // Masquer l'erreur quand on tape
        if (ErrorText.Visibility == System.Windows.Visibility.Visible)
            ErrorText.Visibility = System.Windows.Visibility.Collapsed;
    }

    private static string T(string key, string? defaultValue = null) => L10n.T(key, defaultValue ?? key);
}
