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
using System.Windows.Input;
using OpenAsphalte.Services;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Modules.Georeferencement.Views;

/// <summary>
/// Fenêtre de sélection du système de coordonnées.
/// Permet de rechercher, filtrer et sélectionner une projection cartographique.
/// </summary>
public partial class SetProjectionWindow : Window
{
    private readonly string? _currentCs;
    private readonly ProjectionInfo? _detectedProjection;
    private readonly List<ProjectionInfo> _allProjections;
    private List<ProjectionInfo> _filteredProjections;

    /// <summary>
    /// Projection sélectionnée par l'utilisateur
    /// </summary>
    public ProjectionInfo? SelectedProjection { get; private set; }

    /// <summary>
    /// Indique si l'utilisateur veut supprimer le système actuel
    /// </summary>
    public bool ClearProjection { get; private set; }

    /// <summary>
    /// Indique si l'utilisateur veut activer GEOMAP (Bing Maps)
    /// </summary>
    public bool EnableGeoMap { get; private set; }

    /// <summary>
    /// Crée une nouvelle fenêtre de sélection de projection
    /// </summary>
    /// <param name="currentCs">Code du système de coordonnées actuel (peut être null)</param>
    /// <param name="detectedProjection">Projection détectée automatiquement (peut être null)</param>
    public SetProjectionWindow(string? currentCs, ProjectionInfo? detectedProjection)
    {
        InitializeComponent();

        _currentCs = currentCs;
        _detectedProjection = detectedProjection;
        _allProjections = CoordinateService.Projections.ToList();
        _filteredProjections = _allProjections;

        // Appliquer les traductions
        ApplyTranslations();

        // Initialiser l'affichage
        InitializeDisplay();

        // Charger les projections
        RefreshProjectionList();

        // Sélectionner la projection détectée ou actuelle
        SelectInitialProjection();

        // Focus sur la recherche
        Loaded += (s, e) => SearchBox.Focus();
    }

    /// <summary>
    /// Applique les traductions à l'interface
    /// </summary>
    private void ApplyTranslations()
    {
        Title = T("georef.window.title");

        // En-tête
        CurrentSystemLabel.Text = T("georef.window.current");
        DetectedSystemLabel.Text = T("georef.window.detected");

        // Checkbox
        GroupByCountryCheckBox.Content = T("georef.window.groupby");

        // Boutons
        ClearButton.Content = T("georef.window.clear");
        ClearButton.ToolTip = T("georef.window.clear.tooltip");
        ApplyButton.Content = T("georef.window.apply");
        CancelButton.Content = T("georef.window.cancel");

        // Option GeoMap
        EnableGeoMapCheckBox.Content = T("georef.window.enablegeomap");
        EnableGeoMapCheckBox.ToolTip = T("georef.window.enablegeomap.tooltip");

        // Placeholder recherche (via ToolTip car le placeholder est dans le style)
        SearchBox.ToolTip = T("georef.window.search.tooltip");

        // Panneau détails
        DetailsHeader.Text = T("georef.window.details");
        NameLabel.Text = T("georef.window.details.name");
        CodeLabel.Text = T("georef.window.details.code");
        EpsgLabel.Text = T("georef.window.details.epsg");
        CountryLabel.Text = T("georef.window.list.header.country") + " :";
        RegionLabel.Text = T("georef.window.list.header.region") + " :";
        UnitLabel.Text = T("georef.window.details.unit");
        DescriptionLabel.Text = T("georef.window.details.description");
        BoundsHeader.Text = T("georef.window.details.bounds");

        // En-têtes de colonnes
        ColumnName.Header = T("georef.window.list.header.name");
        ColumnCode.Header = T("georef.window.list.header.code");
        ColumnCountry.Header = T("georef.window.list.header.country");
    }

    /// <summary>
    /// Initialise l'affichage des informations actuelles
    /// </summary>
    private void InitializeDisplay()
    {
        // Système actuel - le code peut être au format "EPSG:xxxx" ou un code interne
        if (!string.IsNullOrEmpty(_currentCs))
        {
            var currentProj = GetProjectionFromCgeocs(_currentCs);
            CurrentSystemText.Text = currentProj != null
                ? currentProj.DisplayName
                : _currentCs;
        }
        else
        {
            CurrentSystemText.Text = T("georef.window.none");
        }

        // Système détecté
        if (_detectedProjection != null)
        {
            DetectedSystemText.Text = _detectedProjection.DisplayName;
        }
        else
        {
            DetectedSystemText.Text = T("georef.window.none");
        }
    }

    /// <summary>
    /// Récupère une projection à partir d'une valeur CGEOCS (format "EPSG:xxxx" ou code interne)
    /// </summary>
    private static ProjectionInfo? GetProjectionFromCgeocs(string cgeocs)
    {
        if (string.IsNullOrWhiteSpace(cgeocs)) return null;

        // Format EPSG:xxxx (retourné par AutoCAD)
        if (cgeocs.StartsWith("EPSG:", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(cgeocs.AsSpan(5), out int epsg))
            {
                return CoordinateService.GetProjectionByEpsg(epsg);
            }
        }

        // Sinon, essayer par code interne
        return CoordinateService.GetProjectionByCode(cgeocs);
    }

    /// <summary>
    /// Sélectionne la projection initiale (détectée ou actuelle)
    /// </summary>
    private void SelectInitialProjection()
    {
        ProjectionInfo? toSelect = null;

        // Priorité à la projection détectée
        if (_detectedProjection != null)
        {
            toSelect = _filteredProjections.FirstOrDefault(p => p.Code == _detectedProjection.Code);
        }
        // Sinon, sélectionner la projection actuelle (via CGEOCS au format EPSG:xxxx)
        else if (!string.IsNullOrEmpty(_currentCs))
        {
            var currentProj = GetProjectionFromCgeocs(_currentCs);
            if (currentProj != null)
            {
                toSelect = _filteredProjections.FirstOrDefault(p => p.Epsg == currentProj.Epsg);
            }
        }

        if (toSelect != null)
        {
            ProjectionListView.SelectedItem = toSelect;
            ProjectionListView.ScrollIntoView(toSelect);
        }
    }

    /// <summary>
    /// Rafraîchit la liste des projections selon le filtre actuel
    /// </summary>
    private void RefreshProjectionList()
    {
        string searchText = SearchBox.Text?.Trim() ?? "";

        // Filtrer les projections
        if (string.IsNullOrEmpty(searchText))
        {
            _filteredProjections = _allProjections.ToList();
        }
        else
        {
            _filteredProjections = CoordinateService.SearchProjections(searchText).ToList();
        }

        // Trier selon l'option de groupement
        if (GroupByCountryCheckBox.IsChecked == true)
        {
            _filteredProjections = _filteredProjections
                .OrderBy(p => p.Country)
                .ThenBy(p => p.Name)
                .ToList();
        }
        else
        {
            _filteredProjections = _filteredProjections
                .OrderBy(p => p.Name)
                .ToList();
        }

        // Mettre à jour la liste
        ProjectionListView.ItemsSource = _filteredProjections;

        // Mettre à jour le compteur
        UpdateResultCount();
    }

    /// <summary>
    /// Met à jour le compteur de résultats
    /// </summary>
    private void UpdateResultCount()
    {
        int count = _filteredProjections.Count;
        string text = count switch
        {
            0 => T("georef.window.results.none"),
            1 => T("georef.window.results.one"),
            _ => string.Format(T("georef.window.results.many"), count)
        };
        ResultCountText.Text = text;
    }

    /// <summary>
    /// Met à jour le panneau de détails avec la projection sélectionnée
    /// </summary>
    private void UpdateDetails(ProjectionInfo? projection)
    {
        if (projection == null)
        {
            DetailsPanel.Visibility = Visibility.Collapsed;
            ApplyButton.IsEnabled = false;
            return;
        }

        DetailsPanel.Visibility = Visibility.Visible;
        ApplyButton.IsEnabled = true;

        DetailName.Text = projection.Name;
        DetailCode.Text = projection.Code;
        DetailEpsg.Text = projection.Epsg.ToString();
        DetailCountry.Text = projection.Country;
        DetailRegion.Text = projection.Region;
        DetailUnit.Text = projection.Unit == "m" ? T("georef.window.unit.meters") : T("georef.window.unit.degrees");
        DetailDescription.Text = projection.Description;

        // Limites
        DetailBoundsX.Text = $"X: {projection.MinX:N0} → {projection.MaxX:N0}";
        DetailBoundsY.Text = $"Y: {projection.MinY:N0} → {projection.MaxY:N0}";
    }

    /// <summary>
    /// Raccourci pour les traductions
    /// </summary>
    private static string T(string key, string? defaultValue = null) => L10n.T(key, defaultValue);

    #region Event Handlers

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshProjectionList();

        // Essayer de garder la sélection si elle est encore dans la liste filtrée
        if (SelectedProjection != null)
        {
            var match = _filteredProjections.FirstOrDefault(p => p.Code == SelectedProjection.Code);
            if (match != null)
            {
                ProjectionListView.SelectedItem = match;
            }
        }
    }

    private void GroupByCountry_Changed(object sender, RoutedEventArgs e)
    {
        RefreshProjectionList();
    }

    private void ProjectionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ProjectionListView.SelectedItem as ProjectionInfo;
        SelectedProjection = selected;
        UpdateDetails(selected);
    }

    private void ProjectionListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedProjection != null)
        {
            ApplyButton_Click(sender, e);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearProjection = true;
        SelectedProjection = null;
        DialogResult = true;
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedProjection != null)
        {
            ClearProjection = false;
            EnableGeoMap = EnableGeoMapCheckBox.IsChecked == true;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #endregion
}
