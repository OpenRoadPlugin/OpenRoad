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
using System.Windows.Media;
using System.Windows.Shapes;
using OpenAsphalte.Modules.DynamicSnap.Models;
using OpenAsphalte.Modules.DynamicSnap.Services;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.DynamicSnap.Views;

/// <summary>
/// Fenêtre de paramétrage de l'accrochage dynamique OAS.
/// Permet de choisir les modes d'accrochage, couleurs et tolérances.
/// Les paramètres sont stockés globalement dans config.json.
/// </summary>
public partial class DynamicSnapSettingsWindow : Window
{
    // ═══════════════════════════════════════════════════════════
    // Mapping slider tick → LineWeight int value
    // ═══════════════════════════════════════════════════════════
    private static readonly int[] LineWeightValues = [15, 20, 25, 30, 40, 50, 70];

    #region Constructor

    public DynamicSnapSettingsWindow()
    {
        InitializeComponent();
        PopulateColorCombos();
        ApplyTranslations();
        LoadSettingsToUI();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Remplit les ComboBox de couleurs avec des pastilles colorées (pas d'emoji).
    /// Chaque item est un StackPanel contenant un Rectangle coloré + un TextBlock.
    /// </summary>
    private void PopulateColorCombos()
    {
        var colors = new (short tag, Color color, string nameFr)[]
        {
            (1, Colors.Red,       "Rouge"),
            (2, Colors.Yellow,    "Jaune"),
            (3, Colors.Lime,      "Vert"),
            (4, Colors.Cyan,      "Cyan"),
            (5, Colors.Blue,      "Bleu"),
            (6, Colors.Magenta,   "Magenta"),
            (7, Colors.White,     "Blanc"),
        };

        PopulateOneColorCombo(MarkerColorCombo, colors);
        PopulateOneColorCombo(ActiveColorCombo, colors);
        PopulateOneColorCombo(HighlightColorCombo, colors);
    }

    /// <summary>
    /// Ajoute les items colorés à un ComboBox
    /// </summary>
    private static void PopulateOneColorCombo(ComboBox combo, (short tag, Color color, string nameFr)[] colors)
    {
        combo.Items.Clear();
        foreach (var (tag, color, nameFr) in colors)
        {
            // Clé de traduction par couleur
            string key = $"dynamicsnap.color.{nameFr.ToLowerInvariant()}";
            string label = L10n.T(key, nameFr);

            var rect = new Rectangle
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Gray,
                StrokeThickness = 0.5,
                RadiusX = 2,
                RadiusY = 2,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var textBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(rect);
            panel.Children.Add(textBlock);

            var item = new ComboBoxItem
            {
                Content = panel,
                Tag = tag.ToString(),
            };
            combo.Items.Add(item);
        }
    }

    /// <summary>
    /// Applique les traductions aux éléments de l'interface
    /// </summary>
    private void ApplyTranslations()
    {
        Title = L10n.T("dynamicsnap.settings.title", "Paramètres d'accrochage OAS");
        TitleLabel.Text = L10n.T("dynamicsnap.settings.header", "Paramètres d'accrochage OAS");
        SnapModesHeader.Text = L10n.T("dynamicsnap.settings.modes", "Modes d'accrochage actifs");
        AppearanceHeader.Text = L10n.T("dynamicsnap.settings.appearance", "Apparence des marqueurs");

        VertexCheckBox.Content = L10n.T("dynamicsnap.settings.vertex", "Sommets (Vertex)");
        EndpointCheckBox.Content = L10n.T("dynamicsnap.settings.endpoint", "Extrémités (Endpoint)");
        MidpointCheckBox.Content = L10n.T("dynamicsnap.settings.midpoint", "Milieux de segments (Midpoint)");
        NearestCheckBox.Content = L10n.T("dynamicsnap.settings.nearest", "Point le plus proche (Nearest)");

        VertexCheckBox.ToolTip = L10n.T("dynamicsnap.settings.vertex.tooltip");
        EndpointCheckBox.ToolTip = L10n.T("dynamicsnap.settings.endpoint.tooltip");
        MidpointCheckBox.ToolTip = L10n.T("dynamicsnap.settings.midpoint.tooltip");
        NearestCheckBox.ToolTip = L10n.T("dynamicsnap.settings.nearest.tooltip");

        MarkerColorLabel.Text = L10n.T("dynamicsnap.settings.color", "Couleur marqueur :");
        ActiveColorLabel.Text = L10n.T("dynamicsnap.settings.activecolor", "Couleur actif :");
        FilledCheckBox.Content = L10n.T("dynamicsnap.settings.filled", "Marqueurs pleins");
        FilledCheckBox.ToolTip = L10n.T("dynamicsnap.settings.filled.tooltip", "Remplir les marqueurs au lieu d'afficher uniquement le contour");
        LineWeightLabel.Text = L10n.T("dynamicsnap.settings.lineweight", "Épaisseur trait :");
        ToleranceLabel.Text = L10n.T("dynamicsnap.settings.tolerance", "Tolérance :");
        MarkerSizeLabel.Text = L10n.T("dynamicsnap.settings.markersize", "Taille marqueur :");

        MarkerColorCombo.ToolTip = L10n.T("dynamicsnap.settings.color.tooltip");
        ActiveColorCombo.ToolTip = L10n.T("dynamicsnap.settings.activecolor.tooltip");
        ToleranceSlider.ToolTip = L10n.T("dynamicsnap.settings.tolerance.tooltip");
        MarkerSizeSlider.ToolTip = L10n.T("dynamicsnap.settings.markersize.tooltip");

        // Traductions surbrillance
        HighlightHeader.Text = L10n.T("dynamicsnap.highlight.header", "Surbrillance des entités");
        HighlightEnabledCheckBox.Content = L10n.T("dynamicsnap.highlight.enabled", "Activer la surbrillance");
        HighlightEnabledCheckBox.ToolTip = L10n.T("dynamicsnap.highlight.enabled.tooltip");
        HighlightColorLabel.Text = L10n.T("dynamicsnap.highlight.color", "Couleur surbrillance :");
        HighlightColorCombo.ToolTip = L10n.T("dynamicsnap.highlight.color.tooltip");
        HighlightPrimaryWeightLabel.Text = L10n.T("dynamicsnap.highlight.primaryweight", "Épaisseur principale :");
        HighlightPrimaryWeightSlider.ToolTip = L10n.T("dynamicsnap.highlight.primaryweight.tooltip");
        HighlightSecondaryWeightLabel.Text = L10n.T("dynamicsnap.highlight.secondaryweight", "Épaisseur secondaire :");
        HighlightSecondaryWeightSlider.ToolTip = L10n.T("dynamicsnap.highlight.secondaryweight.tooltip");

        ApplyButton.Content = L10n.T("dynamicsnap.settings.apply", "Appliquer");
        CancelButton.Content = L10n.T("dynamicsnap.settings.cancel", "Annuler");
        ResetButton.Content = L10n.T("dynamicsnap.settings.reset", "Réinitialiser");
        ResetButton.ToolTip = L10n.T("dynamicsnap.settings.reset.tooltip");
    }

    /// <summary>
    /// Charge les paramètres actuels dans l'interface
    /// </summary>
    private void LoadSettingsToUI()
    {
        var config = DynamicSnapService.DefaultConfiguration;

        // Modes d'accrochage
        VertexCheckBox.IsChecked = config.ActiveModes.HasMode(SnapMode.Vertex);
        EndpointCheckBox.IsChecked = config.ActiveModes.HasMode(SnapMode.Endpoint);
        MidpointCheckBox.IsChecked = config.ActiveModes.HasMode(SnapMode.Midpoint);
        NearestCheckBox.IsChecked = config.ActiveModes.HasMode(SnapMode.Nearest);

        // Couleurs
        SelectComboByTag(MarkerColorCombo, config.MarkerColor);
        SelectComboByTag(ActiveColorCombo, config.ActiveMarkerColor);

        // Marqueurs pleins
        FilledCheckBox.IsChecked = config.FilledMarkers;

        // Épaisseur de ligne (trouver l'index du slider)
        LineWeightSlider.Value = LineWeightValueToSlider(config.MarkerLineWeight);
        UpdateLineWeightLabel();

        // Tolérance et taille
        ToleranceSlider.Value = config.ToleranceViewRatio;
        MarkerSizeSlider.Value = config.MarkerViewRatio;
        UpdateToleranceLabel();
        UpdateMarkerSizeLabel();

        // Charger paramètres surbrillance
        var hlConfig = DynamicSnapService.HighlightConfiguration;
        HighlightEnabledCheckBox.IsChecked = hlConfig.Enabled;
        SelectComboByTag(HighlightColorCombo, hlConfig.HighlightColor);
        HighlightPrimaryWeightSlider.Value = LineWeightValueToSlider(hlConfig.PrimaryLineWeight);
        HighlightSecondaryWeightSlider.Value = LineWeightValueToSlider(hlConfig.SecondaryLineWeight);
        UpdateHighlightPrimaryWeightLabel();
        UpdateHighlightSecondaryWeightLabel();
        UpdateHighlightControlsEnabled();
    }

    /// <summary>
    /// Sélectionne un élément dans un ComboBox par son Tag
    /// </summary>
    private static void SelectComboByTag(ComboBox combo, short tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item &&
                item.Tag is string tagStr &&
                short.TryParse(tagStr, out short itemTag) &&
                itemTag == tag)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    /// <summary>
    /// Récupère la valeur Tag du ComboBox sélectionné
    /// </summary>
    private static short GetComboTag(ComboBox combo, short defaultValue)
    {
        if (combo.SelectedItem is ComboBoxItem item &&
            item.Tag is string tagStr &&
            short.TryParse(tagStr, out short tag))
        {
            return tag;
        }
        return defaultValue;
    }

    /// <summary>
    /// Convertit la valeur int du LineWeight config en index slider
    /// </summary>
    private static int LineWeightValueToSlider(int value)
    {
        for (int i = 0; i < LineWeightValues.Length; i++)
        {
            if (LineWeightValues[i] >= value)
                return i;
        }
        return 3; // 0.30mm par défaut
    }

    /// <summary>
    /// Convertit l'index slider en valeur int de LineWeight
    /// </summary>
    private static int SliderToLineWeightValue(int sliderIndex)
    {
        if (sliderIndex < 0 || sliderIndex >= LineWeightValues.Length)
            return 30;
        return LineWeightValues[sliderIndex];
    }

    /// <summary>
    /// Applique les valeurs de l'interface à la configuration et sauvegarde
    /// </summary>
    private void ApplyUIToSettings()
    {
        var config = DynamicSnapService.DefaultConfiguration;

        // Construire les modes d'accrochage
        var modes = SnapMode.None;
        if (VertexCheckBox.IsChecked == true) modes |= SnapMode.Vertex;
        if (EndpointCheckBox.IsChecked == true) modes |= SnapMode.Endpoint;
        if (MidpointCheckBox.IsChecked == true) modes |= SnapMode.Midpoint;
        if (NearestCheckBox.IsChecked == true) modes |= SnapMode.Nearest;

        // Au moins un mode doit être actif
        if (modes == SnapMode.None)
        {
            modes = SnapMode.PolylineFull;
        }

        config.ActiveModes = modes;
        config.MarkerColor = GetComboTag(MarkerColorCombo, 3);
        config.ActiveMarkerColor = GetComboTag(ActiveColorCombo, 1);
        config.FilledMarkers = FilledCheckBox.IsChecked == true;
        config.MarkerLineWeight = SliderToLineWeightValue((int)LineWeightSlider.Value);
        config.ToleranceViewRatio = ToleranceSlider.Value;
        config.MarkerViewRatio = MarkerSizeSlider.Value;

        DynamicSnapService.DefaultConfiguration = config;

        // Appliquer paramètres surbrillance
        var hlConfig = DynamicSnapService.HighlightConfiguration;
        hlConfig.Enabled = HighlightEnabledCheckBox.IsChecked == true;
        hlConfig.HighlightColor = GetComboTag(HighlightColorCombo, 4);
        hlConfig.PrimaryLineWeight = SliderToLineWeightValue((int)HighlightPrimaryWeightSlider.Value);
        hlConfig.SecondaryLineWeight = SliderToLineWeightValue((int)HighlightSecondaryWeightSlider.Value);
        DynamicSnapService.HighlightConfiguration = hlConfig;

        DynamicSnapService.SaveSettings();
    }

    /// <summary>
    /// Réinitialise tous les paramètres aux valeurs par défaut
    /// </summary>
    private void ResetToDefaults()
    {
        DynamicSnapService.DefaultConfiguration = new SnapConfiguration();
        DynamicSnapService.HighlightConfiguration = new HighlightConfiguration();
        LoadSettingsToUI();
    }

    private void UpdateToleranceLabel()
    {
        ToleranceValue.Text = $"{ToleranceSlider.Value * 100:F1}%";
    }

    private void UpdateMarkerSizeLabel()
    {
        MarkerSizeValue.Text = $"{MarkerSizeSlider.Value * 100:F1}%";
    }

    private void UpdateLineWeightLabel()
    {
        int lw = SliderToLineWeightValue((int)LineWeightSlider.Value);
        LineWeightValue.Text = $"{lw / 100.0:F2} mm";
    }

    private void UpdateHighlightPrimaryWeightLabel()
    {
        int lw = SliderToLineWeightValue((int)HighlightPrimaryWeightSlider.Value);
        HighlightPrimaryWeightValue.Text = $"{lw / 100.0:F2} mm";
    }

    private void UpdateHighlightSecondaryWeightLabel()
    {
        int lw = SliderToLineWeightValue((int)HighlightSecondaryWeightSlider.Value);
        HighlightSecondaryWeightValue.Text = $"{lw / 100.0:F2} mm";
    }

    /// <summary>
    /// Active/désactive les contrôles de surbrillance selon l'état de la checkbox
    /// </summary>
    private void UpdateHighlightControlsEnabled()
    {
        bool enabled = HighlightEnabledCheckBox.IsChecked == true;
        HighlightColorGrid.IsEnabled = enabled;
        HighlightPrimaryWeightGrid.IsEnabled = enabled;
        HighlightSecondaryWeightGrid.IsEnabled = enabled;

        // Opacité visuelle pour les contrôles désactivés
        HighlightColorGrid.Opacity = enabled ? 1.0 : 0.5;
        HighlightPrimaryWeightGrid.Opacity = enabled ? 1.0 : 0.5;
        HighlightSecondaryWeightGrid.Opacity = enabled ? 1.0 : 0.5;
    }

    #endregion

    #region Event Handlers

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyUIToSettings();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetToDefaults();
    }

    private void ToleranceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ToleranceValue != null)
            UpdateToleranceLabel();
    }

    private void MarkerSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MarkerSizeValue != null)
            UpdateMarkerSizeLabel();
    }

    private void LineWeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LineWeightValue != null)
            UpdateLineWeightLabel();
    }

    private void HighlightEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateHighlightControlsEnabled();
    }

    private void HighlightPrimaryWeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HighlightPrimaryWeightValue != null)
            UpdateHighlightPrimaryWeightLabel();
    }

    private void HighlightSecondaryWeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HighlightSecondaryWeightValue != null)
            UpdateHighlightSecondaryWeightLabel();
    }

    #endregion
}
