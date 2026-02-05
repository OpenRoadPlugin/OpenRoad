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
