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
using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.PrezOrganizer.Models;
using OpenAsphalte.Modules.PrezOrganizer.Services;
using OpenAsphalte.UI;
using L10n = OpenAsphalte.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.PrezOrganizer.Views;

/// <summary>
/// Fenêtre principale de l'organiseur de présentations.
/// Gère la liste des présentations avec toutes les opérations de manipulation.
/// </summary>
public partial class PrezOrganizerWindow : Window
{
    // ═══════════════════════════════════════════════════════════
    // CHAMPS
    // ═══════════════════════════════════════════════════════════

    private readonly Database _database;
    private readonly List<LayoutItem> _originalItems;
    private List<LayoutItem> _items;
    private readonly Stack<List<LayoutItem>> _undoStack = new();
    private Point _dragStartPoint;
    private bool _isDragging;
    private string _filterText = string.Empty;

    // ═══════════════════════════════════════════════════════════
    // PROPRIÉTÉS PUBLIQUES (accessibles par la commande)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Liste des items dans leur état actuel (pour application par la commande).
    /// </summary>
    public List<LayoutItem> Items => _items;

    /// <summary>
    /// Indique si des modifications ont été faites.
    /// </summary>
    public bool HasChanges => _items.Any(i => i.IsModified) || HasOrderChanges();

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTEUR
    // ═══════════════════════════════════════════════════════════

    public PrezOrganizerWindow(List<LayoutItem> layouts, Database database)
    {
        InitializeComponent();

        _database = database;
        _originalItems = layouts.Select(l => l.Clone()).ToList();
        _items = layouts;

        // Restaurer la taille/position de la fenêtre
        WindowStateHelper.RestoreState(this, "prezorganizer", 780);
        Closing += (s, e) => WindowStateHelper.SaveState(this, "prezorganizer");

        ApplyTranslations();
        RefreshList();
        UpdateStatusBar();
        UpdateSummary();

        Loaded += (s, e) => SearchBox.Focus();
    }

    // ═══════════════════════════════════════════════════════════
    // TRADUCTIONS
    // ═══════════════════════════════════════════════════════════

    private void ApplyTranslations()
    {
        Title = T("prezorganizer.window.title");
        WindowHeader.Text = T("prezorganizer.window.header");

        // Boutons toolbar
        BtnMoveTop.ToolTip = T("prezorganizer.btn.moveTop");
        BtnMoveUp.ToolTip = T("prezorganizer.btn.moveUp");
        BtnMoveDown.ToolTip = T("prezorganizer.btn.moveDown");
        BtnMoveBottom.ToolTip = T("prezorganizer.btn.moveBottom");
        BtnReverse.ToolTip = T("prezorganizer.btn.reverse");
        BtnSort.ToolTip = T("prezorganizer.btn.sort");
        BtnRename.ToolTip = T("prezorganizer.btn.rename");
        BtnCopy.ToolTip = T("prezorganizer.btn.copy");
        BtnAdd.ToolTip = T("prezorganizer.btn.add");
        BtnDelete.ToolTip = T("prezorganizer.btn.delete");
        BtnFindReplace.ToolTip = T("prezorganizer.btn.findReplace");
        BtnRenameTool.ToolTip = T("prezorganizer.btn.renameTool");
        BtnCase.ToolTip = T("prezorganizer.btn.case");

        // Menu de tri
        SortAlphaAsc.Header = T("prezorganizer.sort.alpha");
        SortAlphaDesc.Header = T("prezorganizer.sort.alphaDesc");
        SortNumAsc.Header = T("prezorganizer.sort.num");
        SortNumDesc.Header = T("prezorganizer.sort.numDesc");
        SortArchAsc.Header = T("prezorganizer.sort.arch");
        SortArchDesc.Header = T("prezorganizer.sort.archDesc");

        // Menu de casse
        CaseUpper.Header = T("prezorganizer.case.upper");
        CaseLower.Header = T("prezorganizer.case.lower");
        CaseTitle.Header = T("prezorganizer.case.title");

        // Détails
        DetailHeader.Text = T("prezorganizer.detail.header");
        OriginalNameLabel.Text = T("prezorganizer.detail.originalName");
        NewNameLabel.Text = T("prezorganizer.detail.newName");
        StatusLabel.Text = T("prezorganizer.detail.status");
        PendingHeader.Text = T("prezorganizer.detail.pending");

        // Boutons principaux
        BtnUndo.Content = $"↩ {T("prezorganizer.btn.undo")}";
        BtnUndo.ToolTip = T("prezorganizer.btn.undo.tooltip");
        BtnReset.Content = T("prezorganizer.btn.reset");
        BtnReset.ToolTip = T("prezorganizer.btn.reset.tooltip");
        BtnClose.Content = T("prezorganizer.btn.close");
        BtnApply.Content = T("prezorganizer.btn.apply");
        BtnApply.ToolTip = T("prezorganizer.btn.apply.tooltip");

        // Barre de statut
        BtnSetCurrent.Content = T("prezorganizer.btn.setCurrent");
        BtnSetCurrent.ToolTip = T("prezorganizer.btn.setCurrent.tooltip");
    }

    // ═══════════════════════════════════════════════════════════
    // RAFRAÎCHISSEMENT DE L'AFFICHAGE
    // ═══════════════════════════════════════════════════════════

    private void RefreshList()
    {
        var selectedIndices = GetSelectedIndices();

        LayoutListBox.Items.Clear();

        var displayItems = string.IsNullOrEmpty(_filterText)
            ? _items
            : _items.Where(i => i.CurrentName.Contains(_filterText, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var item in displayItems)
        {
            var lbi = new ListBoxItem
            {
                Content = CreateItemContent(item),
                Tag = item,
                AllowDrop = true,
            };
            LayoutListBox.Items.Add(lbi);
        }

        // Restaurer la sélection
        foreach (int idx in selectedIndices)
        {
            if (idx < LayoutListBox.Items.Count)
                ((ListBoxItem)LayoutListBox.Items[idx]).IsSelected = true;
        }

        UpdateStatusBar();
        UpdateSummary();
        UpdateButtonStates();
    }

    private UIElement CreateItemContent(LayoutItem item)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };

        // Indicateur de statut
        if (item.IsMarkedForDeletion)
        {
            sp.Children.Add(new TextBlock
            {
                Text = "✖ ",
                Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),
                FontWeight = FontWeights.Bold
            });
            sp.Children.Add(new TextBlock
            {
                Text = item.CurrentName,
                TextDecorations = TextDecorations.Strikethrough,
                Foreground = new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))
            });
        }
        else if (item.IsNew || item.IsCopy)
        {
            sp.Children.Add(new TextBlock
            {
                Text = "★ ",
                Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
                FontWeight = FontWeights.Bold
            });
            sp.Children.Add(new TextBlock { Text = item.CurrentName });
        }
        else if (item.OriginalName != item.CurrentName)
        {
            sp.Children.Add(new TextBlock
            {
                Text = "✎ ",
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),
                FontWeight = FontWeights.Bold
            });
            sp.Children.Add(new TextBlock
            {
                Text = item.CurrentName,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
                FontWeight = FontWeights.SemiBold
            });
        }
        else
        {
            sp.Children.Add(new TextBlock { Text = item.CurrentName });
        }

        return sp;
    }

    private void UpdateStatusBar()
    {
        int total = _items.Count(i => !i.IsMarkedForDeletion);
        StatusBarText.Text = string.Format(T("prezorganizer.status.count"), total);

        int pendingCount = CountPendingChanges();
        if (pendingCount > 0)
        {
            PendingBarText.Text = string.Format(T("prezorganizer.status.pending"), pendingCount);
            PendingBarText.Visibility = System.Windows.Visibility.Visible;
        }
        else
        {
            PendingBarText.Text = T("prezorganizer.status.noPending");
            PendingBarText.Visibility = System.Windows.Visibility.Visible;
        }
    }

    private void UpdateSummary()
    {
        int renames = _items.Count(i => !i.IsNew && !i.IsCopy && !i.IsMarkedForDeletion && i.OriginalName != i.CurrentName);
        int moves = CountOrderChanges();
        int additions = _items.Count(i => (i.IsNew || i.IsCopy) && !i.IsMarkedForDeletion);
        int deletions = _items.Count(i => i.IsMarkedForDeletion && !i.IsNew && !i.IsCopy);

        RenameCount.Text = string.Format(T("prezorganizer.detail.renames"), renames);
        MoveCount.Text = string.Format(T("prezorganizer.detail.moves"), moves);
        AddCount.Text = string.Format(T("prezorganizer.detail.additions"), additions);
        DeleteCount.Text = string.Format(T("prezorganizer.detail.deletions"), deletions);

        BtnApply.IsEnabled = HasChanges;
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = LayoutListBox.SelectedItems.Count > 0;
        bool hasFilter = !string.IsNullOrEmpty(_filterText);

        // Déplacement désactivé quand un filtre est actif (l'ordre affiché ≠ ordre réel)
        BtnMoveUp.IsEnabled = hasSelection && !hasFilter;
        BtnMoveDown.IsEnabled = hasSelection && !hasFilter;
        BtnMoveTop.IsEnabled = hasSelection && !hasFilter;
        BtnMoveBottom.IsEnabled = hasSelection && !hasFilter;
        BtnReverse.IsEnabled = LayoutListBox.SelectedItems.Count > 1 && !hasFilter;
        BtnSort.IsEnabled = !hasFilter;

        BtnRename.IsEnabled = LayoutListBox.SelectedItems.Count == 1;
        BtnCopy.IsEnabled = hasSelection;
        BtnDelete.IsEnabled = hasSelection;
        BtnSetCurrent.IsEnabled = LayoutListBox.SelectedItems.Count == 1;
        BtnUndo.IsEnabled = _undoStack.Count > 0;
    }

    private void UpdateDetails(LayoutItem? item)
    {
        if (item == null)
        {
            DetailPanel.Visibility = System.Windows.Visibility.Collapsed;
            return;
        }

        DetailPanel.Visibility = System.Windows.Visibility.Visible;
        OriginalNameValue.Text = item.OriginalName;
        NewNameValue.Text = item.CurrentName;

        if (item.IsMarkedForDeletion)
        {
            StatusValue.Text = T("prezorganizer.detail.status.deleted");
            StatusValue.Foreground = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
        }
        else if (item.IsNew)
        {
            StatusValue.Text = T("prezorganizer.detail.status.new");
            StatusValue.Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
        }
        else if (item.IsCopy)
        {
            StatusValue.Text = T("prezorganizer.detail.status.copy");
            StatusValue.Foreground = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
        }
        else if (item.OriginalName != item.CurrentName)
        {
            StatusValue.Text = T("prezorganizer.detail.status.renamed");
            StatusValue.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
        }
        else
        {
            StatusValue.Text = T("prezorganizer.detail.status.unchanged");
            StatusValue.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // UNDO / SNAPSHOT
    // ═══════════════════════════════════════════════════════════

    private void PushUndo()
    {
        _undoStack.Push(_items.Select(i => i.Clone()).ToList());
        BtnUndo.IsEnabled = true;
    }

    private void PopUndo()
    {
        if (_undoStack.Count == 0) return;
        _items = _undoStack.Pop();
        RefreshList();
    }

    // ═══════════════════════════════════════════════════════════
    // UTILITAIRES
    // ═══════════════════════════════════════════════════════════

    private List<int> GetSelectedIndices()
    {
        var indices = new List<int>();
        for (int i = 0; i < LayoutListBox.Items.Count; i++)
        {
            if (((ListBoxItem)LayoutListBox.Items[i]).IsSelected)
                indices.Add(i);
        }
        return indices;
    }

    private List<LayoutItem> GetSelectedItems()
    {
        return LayoutListBox.SelectedItems.Cast<ListBoxItem>()
            .Select(lbi => (LayoutItem)lbi.Tag)
            .ToList();
    }

    private LayoutItem? GetFirstSelectedItem()
    {
        if (LayoutListBox.SelectedItem is ListBoxItem lbi)
            return lbi.Tag as LayoutItem;
        return null;
    }

    /// <summary>
    /// Convertit les indices filtrés en indices réels dans _items.
    /// </summary>
    private List<int> GetRealIndices(List<int> displayIndices)
    {
        if (string.IsNullOrEmpty(_filterText))
            return displayIndices;

        var filteredItems = _items
            .Where(i => i.CurrentName.Contains(_filterText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return displayIndices
            .Where(i => i < filteredItems.Count)
            .Select(i => _items.IndexOf(filteredItems[i]))
            .Where(i => i >= 0)
            .ToList();
    }

    private IEnumerable<string> GetAllCurrentNames()
    {
        return _items.Where(i => !i.IsMarkedForDeletion).Select(i => i.CurrentName);
    }

    private int CountPendingChanges()
    {
        int count = 0;
        count += _items.Count(i => !i.IsNew && !i.IsCopy && !i.IsMarkedForDeletion && i.OriginalName != i.CurrentName);
        count += CountOrderChanges();
        count += _items.Count(i => (i.IsNew || i.IsCopy) && !i.IsMarkedForDeletion);
        count += _items.Count(i => i.IsMarkedForDeletion && !i.IsNew && !i.IsCopy);
        return count;
    }

    private int CountOrderChanges()
    {
        var nonDeleted = _items.Where(i => !i.IsMarkedForDeletion && !i.IsNew && !i.IsCopy).ToList();
        int moves = 0;
        for (int i = 0; i < nonDeleted.Count; i++)
        {
            if (nonDeleted[i].OriginalTabOrder != i + 1) // TabOrder is 1-based for non-Model
                moves++;
        }
        return moves;
    }

    private bool HasOrderChanges()
    {
        return CountOrderChanges() > 0;
    }

    private void SetSelection(List<int> indices)
    {
        LayoutListBox.SelectedItems.Clear();
        foreach (int idx in indices)
        {
            if (idx >= 0 && idx < LayoutListBox.Items.Count)
                ((ListBoxItem)LayoutListBox.Items[idx]).IsSelected = true;
        }

        // Scroll vers le premier sélectionné
        if (indices.Count > 0 && indices[0] < LayoutListBox.Items.Count)
            LayoutListBox.ScrollIntoView(LayoutListBox.Items[indices[0]]);
    }

    private static string T(string key, string? defaultValue = null) => L10n.T(key, defaultValue ?? key);

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS — DÉPLACEMENT
    // ═══════════════════════════════════════════════════════════

    private void BtnMoveTop_Click(object sender, RoutedEventArgs e)
    {
        var indices = GetRealIndices(GetSelectedIndices());
        if (indices.Count == 0) return;
        PushUndo();
        var newIndices = LayoutService.MoveToTop(_items, indices);
        RefreshList();
        SetSelection(newIndices);

    }

    private void BtnMoveUp_Click(object sender, RoutedEventArgs e)
    {
        var indices = GetRealIndices(GetSelectedIndices());
        if (indices.Count == 0) return;
        PushUndo();
        var newIndices = LayoutService.MoveUp(_items, indices);
        RefreshList();
        SetSelection(newIndices);

    }

    private void BtnMoveDown_Click(object sender, RoutedEventArgs e)
    {
        var indices = GetRealIndices(GetSelectedIndices());
        if (indices.Count == 0) return;
        PushUndo();
        var newIndices = LayoutService.MoveDown(_items, indices);
        RefreshList();
        SetSelection(newIndices);

    }

    private void BtnMoveBottom_Click(object sender, RoutedEventArgs e)
    {
        var indices = GetRealIndices(GetSelectedIndices());
        if (indices.Count == 0) return;
        PushUndo();
        var newIndices = LayoutService.MoveToBottom(_items, indices);
        RefreshList();
        SetSelection(newIndices);

    }

    private void BtnReverse_Click(object sender, RoutedEventArgs e)
    {
        var indices = GetRealIndices(GetSelectedIndices());
        if (indices.Count < 2) return;
        PushUndo();
        LayoutService.ReverseSelected(_items, indices);
        RefreshList();
        SetSelection(indices);

    }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS — TRI
    // ═══════════════════════════════════════════════════════════

    private void BtnSort_Click(object sender, RoutedEventArgs e)
    {
        SortContextMenu.IsOpen = true;
    }

    private void SortAlphaAsc_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        LayoutService.SortAlphabetical(_items, ascending: true);
        RefreshList();

    }

    private void SortAlphaDesc_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        LayoutService.SortAlphabetical(_items, ascending: false);
        RefreshList();

    }

    private void SortNumAsc_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        LayoutService.SortNumerical(_items, ascending: true);
        RefreshList();

    }

    private void SortNumDesc_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        LayoutService.SortNumerical(_items, ascending: false);
        RefreshList();
    }

    private void SortArchAsc_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        LayoutService.SortArchitectural(_items, ascending: true);
        RefreshList();

    }

    private void SortArchDesc_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        LayoutService.SortArchitectural(_items, ascending: false);
        RefreshList();

    }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS — ÉDITION
    // ═══════════════════════════════════════════════════════════

    private void BtnRename_Click(object sender, RoutedEventArgs e)
    {
        RenameSelectedItem();
    }

    private void RenameSelectedItem()
    {
        var item = GetFirstSelectedItem();
        if (item == null) return;

        // Boîte de dialogue de renommage simple
        var inputDialog = new InputDialog(
            T("prezorganizer.rename.title"),
            T("prezorganizer.rename.label"),
            item.CurrentName);

        var result = inputDialog.ShowDialog();
        if (result != true || string.IsNullOrWhiteSpace(inputDialog.ResultText))
            return;

        string newName = inputDialog.ResultText.Trim();

        // Validation
        var (isValid, error) = LayoutService.ValidateName(newName, GetAllCurrentNames(), item.CurrentName);
        if (!isValid)
        {
            MessageBox.Show(T(error!), T("prezorganizer.rename.title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PushUndo();
        item.CurrentName = newName;
        RefreshList();

    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0) return;

        PushUndo();

        foreach (var item in selectedItems)
        {
            if (item.IsMarkedForDeletion) continue;
            string copyName = LayoutService.GenerateCopyName(item.CurrentName, GetAllCurrentNames());
            var copy = new LayoutItem(copyName, isNew: false, copySource: item.IsNew || item.IsCopy ? null : item.OriginalName);
            if (item.IsNew || item.IsCopy)
            {
                // Si l'original est déjà un nouvel item, marquer comme nouveau simple
                copy = new LayoutItem(copyName, isNew: true);
            }
            int idx = _items.IndexOf(item);
            _items.Insert(idx + 1, copy);
        }

        RefreshList();

    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        string newName = LayoutService.GenerateUniqueName(GetAllCurrentNames());

        var inputDialog = new InputDialog(
            T("prezorganizer.rename.title"),
            T("prezorganizer.rename.label"),
            newName);

        var result = inputDialog.ShowDialog();
        if (result != true || string.IsNullOrWhiteSpace(inputDialog.ResultText))
            return;

        newName = inputDialog.ResultText.Trim();

        var (isValid, error) = LayoutService.ValidateName(newName, GetAllCurrentNames());
        if (!isValid)
        {
            MessageBox.Show(T(error!), T("prezorganizer.rename.title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PushUndo();
        _items.Add(new LayoutItem(newName, isNew: true));
        RefreshList();

    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0) return;

        // Vérifier qu'on ne supprime pas tout
        int remaining = _items.Count(i => !i.IsMarkedForDeletion) - selectedItems.Count(i => !i.IsMarkedForDeletion);
        if (remaining < 1)
        {
            MessageBox.Show(T("prezorganizer.confirm.deleteAll"), T("prezorganizer.btn.delete"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmResult = MessageBox.Show(
            string.Format(T("prezorganizer.confirm.delete"), selectedItems.Count),
            T("prezorganizer.btn.delete"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmResult != MessageBoxResult.Yes) return;

        PushUndo();

        foreach (var item in selectedItems)
        {
            if (item.IsNew || item.IsCopy)
            {
                // Les items non encore dans AutoCAD sont simplement retirés
                _items.Remove(item);
            }
            else
            {
                item.IsMarkedForDeletion = true;
            }
        }

        RefreshList();

    }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS — TRANSFORMATIONS
    // ═══════════════════════════════════════════════════════════

    private void BtnFindReplace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FindReplaceDialog(_items);
        var result = dialog.ShowDialog();
        if (result == true && dialog.ChangesMade > 0)
        {
            PushUndo();
            // Les modifications ont été appliquées directement aux items par le dialog
            // On restaure depuis le dialog
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].CurrentName = dialog.ResultItems[i].CurrentName;
            }
            RefreshList();

        }
    }

    private void BtnRenameTool_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = GetSelectedItems();
        var dialog = new RenameToolDialog(_items, selectedItems);
        var result = dialog.ShowDialog();
        if (result == true && dialog.HasChanges)
        {
            PushUndo();
            // Les modifications ont été appliquées aux items via le dialogue
            RefreshList();
        }
    }

    private void BtnCase_Click(object sender, RoutedEventArgs e)
    {
        CaseContextMenu.IsOpen = true;
    }

    private void CaseUpper_Click(object sender, RoutedEventArgs e)
    {
        ApplyCaseToSelection(LayoutService.CaseType.Upper);
    }

    private void CaseLower_Click(object sender, RoutedEventArgs e)
    {
        ApplyCaseToSelection(LayoutService.CaseType.Lower);
    }

    private void CaseTitle_Click(object sender, RoutedEventArgs e)
    {
        ApplyCaseToSelection(LayoutService.CaseType.Title);
    }

    private void ApplyCaseToSelection(LayoutService.CaseType caseType)
    {
        var selectedItems = GetSelectedItems();
        if (selectedItems.Count == 0)
        {
            // Si rien de sélectionné, appliquer à tout
            selectedItems = _items.Where(i => !i.IsMarkedForDeletion).ToList();
        }

        PushUndo();
        LayoutService.ApplyCase(selectedItems, caseType);
        RefreshList();

    }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS — LISTE
    // ═══════════════════════════════════════════════════════════

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _filterText = SearchBox.Text?.Trim() ?? string.Empty;
        RefreshList();
    }

    private void LayoutListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtonStates();
        UpdateDetails(GetFirstSelectedItem());
    }

    private void LayoutListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe)
        {
            // S'assurer qu'on a cliqué sur un item
            var item = GetFirstSelectedItem();
            if (item != null && !item.IsMarkedForDeletion)
            {
                RenameSelectedItem();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DRAG & DROP
    // ═══════════════════════════════════════════════════════════

    private void LayoutListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(LayoutListBox);
        _isDragging = false;
    }

    private void LayoutListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        Point currentPos = e.GetPosition(LayoutListBox);
        Vector diff = _dragStartPoint - currentPos;

        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            if (LayoutListBox.SelectedItem is ListBoxItem selectedItem && !_isDragging)
            {
                _isDragging = true;
                var data = new DataObject("LayoutItem", selectedItem.Tag);
                DragDrop.DoDragDrop(LayoutListBox, data, DragDropEffects.Move);
                _isDragging = false;
            }
        }
    }

    private void LayoutListBox_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("LayoutItem"))
        {
            e.Effects = DragDropEffects.None;
            return;
        }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void LayoutListBox_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("LayoutItem")) return;

        var droppedItem = (LayoutItem)e.Data.GetData("LayoutItem")!;

        // Déterminer la position de drop
        Point dropPos = e.GetPosition(LayoutListBox);
        int targetIndex = -1;

        for (int i = 0; i < LayoutListBox.Items.Count; i++)
        {
            var lbi = (ListBoxItem)LayoutListBox.Items[i];
            var transform = lbi.TransformToAncestor(LayoutListBox);
            var bounds = transform.TransformBounds(new Rect(0, 0, lbi.ActualWidth, lbi.ActualHeight));

            if (dropPos.Y < bounds.Top + bounds.Height / 2)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex < 0)
            targetIndex = LayoutListBox.Items.Count;

        int sourceIndex = _items.IndexOf(droppedItem);
        if (sourceIndex < 0 || sourceIndex == targetIndex) return;

        PushUndo();
        _items.RemoveAt(sourceIndex);
        if (targetIndex > sourceIndex) targetIndex--;
        _items.Insert(targetIndex, droppedItem);
        RefreshList();
        SetSelection([targetIndex]);

    }

    // ═══════════════════════════════════════════════════════════
    // EVENT HANDLERS — BOUTONS PRINCIPAUX
    // ═══════════════════════════════════════════════════════════

    private void BtnUndo_Click(object sender, RoutedEventArgs e)
    {
        PopUndo();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            T("prezorganizer.confirm.reset"),
            T("prezorganizer.btn.reset"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        PushUndo();
        _items = _originalItems.Select(i => i.Clone()).ToList();
        RefreshList();
    }

    private void BtnSetCurrent_Click(object sender, RoutedEventArgs e)
    {
        var item = GetFirstSelectedItem();
        if (item == null || item.IsMarkedForDeletion || item.IsNew || item.IsCopy) return;

        LayoutService.SetCurrentLayout(item.OriginalName);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
