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

using System.Globalization;
using System.Windows;
using OpenAsphalte.Modules.Cota2Lign.Services;
using OpenAsphalte.Discovery;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.Cota2Lign.Views;

/// <summary>
/// Fenêtre de paramétrage du module Cotation entre deux lignes.
/// Permet de configurer l'interdistance, le calque cible, etc.
/// </summary>
public partial class Cota2LignSettingsWindow : Window
{
    #region Fields

    private readonly Cota2LignSettings _settings;
    private readonly bool _isDynamicSnapAvailable;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialise la fenêtre avec les paramètres actuels
    /// </summary>
    /// <param name="settings">Paramètres à éditer</param>
    public Cota2LignSettingsWindow(Cota2LignSettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _isDynamicSnapAvailable = CheckDynamicSnapAvailable();

        // Appliquer les traductions
        ApplyTranslations();

        // Charger les valeurs dans les contrôles
        LoadSettingsToUI();

        // Configurer l'état du contrôle OAS Snap
        ConfigureOasSnapControls();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Applique les traductions aux éléments de l'interface
    /// </summary>
    private void ApplyTranslations()
    {
        Title = L10n.T("cota2lign.settings.title", "Paramètres - Cotation entre deux lignes");

        // Labels
        TitleLabel.Text = L10n.T("cota2lign.settings.header", "Paramètres de cotation");
        InterdistanceLabel.Text = L10n.T("cota2lign.settings.interdist", "Interdistance (m) :");
        OffsetLabel.Text = L10n.T("cota2lign.settings.offset", "Décalage cotation (m) :");
        LayerLabel.Text = L10n.T("cota2lign.settings.layer", "Calque de destination :");

        ApplyButton.Content = L10n.T("cota2lign.settings.apply", "Appliquer");
        CancelButton.Content = L10n.T("cota2lign.settings.cancel", "Annuler");
        ResetButton.Content = L10n.T("cota2lign.settings.reset", "Réinitialiser");

        VerticesCheckBox.Content = L10n.T("cota2lign.settings.vertices", "Coter aux sommets de la polyligne");
        ReverseSideCheckBox.Content = L10n.T("cota2lign.settings.reverse", "Inverser le côté de décalage");

        // Traductions pour l'accrochage OAS
        UseOasSnapCheckBox.Content = L10n.T("cota2lign.settings.useoassnap", "Utiliser l'accrochage OAS");

        // Tooltips
        InterdistanceLabel.ToolTip = L10n.T("cota2lign.settings.interdist.tooltip", "Distance entre chaque cotation (0 pour désactiver)");
        InterdistanceTextBox.ToolTip = InterdistanceLabel.ToolTip;
        OffsetLabel.ToolTip = L10n.T("cota2lign.settings.offset.tooltip", "Distance de décalage du texte de cotation");
        OffsetTextBox.ToolTip = OffsetLabel.ToolTip;
        LayerLabel.ToolTip = L10n.T("cota2lign.settings.layer.tooltip", "Calque sur lequel créer les cotations (vide = calque courant)");
        LayerTextBox.ToolTip = LayerLabel.ToolTip;
        VerticesCheckBox.ToolTip = L10n.T("cota2lign.settings.vertices.tooltip", "Ajouter une cotation à chaque sommet de la polyligne");
        ReverseSideCheckBox.ToolTip = L10n.T("cota2lign.settings.reverse.tooltip", "Place les cotations de l'autre côté de la polyligne");
        UseOasSnapCheckBox.ToolTip = L10n.T("cota2lign.settings.useoassnap.tooltip", "Active l'accrochage intelligent OAS (nécessite le module DynamicSnap)");
        ResetButton.ToolTip = L10n.T("cota2lign.settings.reset.tooltip", "Rétablir les valeurs par défaut");
    }

    /// <summary>
    /// Charge les paramètres dans l'interface utilisateur
    /// </summary>
    private void LoadSettingsToUI()
    {
        InterdistanceTextBox.Text = _settings.Interdistance.ToString("F2", CultureInfo.CurrentCulture);
        OffsetTextBox.Text = _settings.DimensionOffset.ToString("F2", CultureInfo.CurrentCulture);
        LayerTextBox.Text = _settings.TargetLayer ?? string.Empty;
        VerticesCheckBox.IsChecked = _settings.DimensionAtVertices;
        ReverseSideCheckBox.IsChecked = _settings.ReverseSide;

        // Accrochage OAS
        UseOasSnapCheckBox.IsChecked = _settings.UseOasSnap;

        // Placeholder pour le calque
        if (string.IsNullOrWhiteSpace(_settings.TargetLayer))
        {
            LayerTextBox.Text = string.Empty;
        }
    }

    /// <summary>
    /// Configure l'état du contrôle d'accrochage OAS selon la disponibilité du module
    /// </summary>
    private void ConfigureOasSnapControls()
    {
        if (!_isDynamicSnapAvailable)
        {
            // Module non installé : griser le checkbox
            UseOasSnapCheckBox.IsEnabled = false;
            UseOasSnapCheckBox.IsChecked = false;
            UseOasSnapCheckBox.ToolTip = L10n.T("cota2lign.settings.oassnap.unavailable",
                "Module DynamicSnap non installé - fonctionnalité désactivée");
        }
    }

    /// <summary>
    /// Vérifie si le module DynamicSnap est disponible
    /// </summary>
    private static bool CheckDynamicSnapAvailable()
    {
        try
        {
            var module = ModuleDiscovery.GetModule("dynamicsnap");
            return module != null && module.IsInitialized;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lit les valeurs de l'interface et les applique aux paramètres
    /// </summary>
    /// <returns>True si les valeurs sont valides</returns>
    private bool ApplyUIToSettings()
    {
        // Valider et appliquer l'interdistance
        if (double.TryParse(InterdistanceTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double interdist))
        {
            _settings.Interdistance = Math.Max(0, interdist);
        }
        else
        {
            MessageBox.Show(
                L10n.T("cota2lign.validation.invalidInterdist", "Veuillez saisir une valeur numérique valide pour l'interdistance."),
                L10n.T("cota2lign.validation.error", "Erreur de validation"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            InterdistanceTextBox.Focus();
            return false;
        }

        // Valider et appliquer le décalage
        if (double.TryParse(OffsetTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double offset))
        {
            _settings.DimensionOffset = offset;
        }
        else
        {
            MessageBox.Show(
                L10n.T("cota2lign.validation.invalidOffset", "Veuillez saisir une valeur numérique valide pour le décalage."),
                L10n.T("cota2lign.validation.error", "Erreur de validation"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            OffsetTextBox.Focus();
            return false;
        }

        // Appliquer le calque (peut être vide)
        _settings.TargetLayer = string.IsNullOrWhiteSpace(LayerTextBox.Text)
            ? null
            : LayerTextBox.Text.Trim();

        // Appliquer les checkbox
        _settings.DimensionAtVertices = VerticesCheckBox.IsChecked ?? false;
        _settings.ReverseSide = ReverseSideCheckBox.IsChecked ?? false;

        // Appliquer les paramètres d'accrochage OAS
        _settings.UseOasSnap = UseOasSnapCheckBox.IsChecked ?? false;

        return true;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Applique les paramètres et ferme la fenêtre
    /// </summary>
    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (ApplyUIToSettings())
        {
            DialogResult = true;
            Close();
        }
    }

    /// <summary>
    /// Annule et ferme la fenêtre
    /// </summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Réinitialise les paramètres aux valeurs par défaut
    /// </summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.ResetToDefaults();
        LoadSettingsToUI();
    }

    #endregion
}
