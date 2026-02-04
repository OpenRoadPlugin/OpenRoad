// Copyright 2026 Open Road Contributors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;
using OpenRoad.Discovery;
using OpenRoad.Logging;
using L10n = OpenRoad.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenRoad.UI;

/// <summary>
/// Wrapper IDisposable pour les objets COM AutoCAD.
/// Assure une libération propre des ressources COM même en cas d'exception.
/// </summary>
internal sealed class ComWrapper<T> : IDisposable where T : class
{
    public T? Object { get; private set; }
    private bool _disposed;
    
    public ComWrapper(T? obj) => Object = obj;
    
    /// <summary>
    /// Libère manuellement l'objet COM et le remplace par un autre.
    /// Utile pour réassigner sans créer un nouveau wrapper.
    /// </summary>
    public void ReplaceWith(T? newObj)
    {
        SafeRelease();
        Object = newObj;
        _disposed = false;
    }
    
    /// <summary>
    /// Libère l'objet COM de manière sécurisée
    /// </summary>
    private void SafeRelease()
    {
        if (Object != null)
        {
            try
            {
                Marshal.ReleaseComObject(Object);
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"COM release warning: {ex.Message}");
            }
            Object = null;
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SafeRelease();
    }
}

/// <summary>
/// Construction dynamique du menu contextuel Open Road.
/// Génère automatiquement le menu basé sur les modules découverts.
/// </summary>
public static class MenuBuilder
{
    private const string DefaultMenuName = "Open Road";
    private static bool _menuCreated = false;
    
    /// <summary>
    /// Crée le menu Open Road basé sur les modules découverts.
    /// </summary>
    public static void CreateMenu()
    {
        if (_menuCreated) return;
        
        using var acadAppWrapper = new ComWrapper<AcadApplication>((AcadApplication)AcadApp.AcadApplication);
        if (acadAppWrapper.Object == null) return;
        
        using var menuBarWrapper = new ComWrapper<AcadMenuBar>(acadAppWrapper.Object.MenuBar);
        using var menuGroupsWrapper = new ComWrapper<AcadMenuGroups>(acadAppWrapper.Object.MenuGroups);
        
        if (menuBarWrapper.Object == null || menuGroupsWrapper.Object == null) return;
        if (menuGroupsWrapper.Object.Count == 0) return;
        
        using var menuGroupWrapper = new ComWrapper<AcadMenuGroup>(menuGroupsWrapper.Object.Item(0));
        if (menuGroupWrapper.Object == null) return;
        
        using var menusWrapper = new ComWrapper<AcadPopupMenus>(menuGroupWrapper.Object.Menus);
        if (menusWrapper.Object == null) return;
        
        try
        {
            var menuName = L10n.T("app.name", DefaultMenuName);
            AcadPopupMenu? openRoadMenu = null;
            bool menuExisted = false;
            
            // Chercher si le menu existe déjà (toutes langues possibles)
            for (int i = 0; i < menusWrapper.Object.Count; i++)
            {
                AcadPopupMenu? existingMenu = null;
                try
                {
                    existingMenu = menusWrapper.Object.Item(i);
                    if (existingMenu != null && IsOpenRoadMenu(existingMenu.Name))
                    {
                        openRoadMenu = existingMenu;
                        menuExisted = true;
                        break;
                    }
                }
                finally
                {
                    // Ne pas libérer si c'est notre menu trouvé
                    if (existingMenu != null && existingMenu != openRoadMenu)
                        try { Marshal.ReleaseComObject(existingMenu); } catch { }
                }
            }
            
            // Si le menu existe, vider son contenu pour le reconstruire
            if (openRoadMenu != null && menuExisted)
            {
                ClearMenuItems(openRoadMenu);
                
                // Renommer si nécessaire (changement de langue)
                if (!openRoadMenu.Name.Equals(menuName, StringComparison.Ordinal))
                {
                    try { openRoadMenu.Name = menuName; } catch { }
                }
            }
            else
            {
                // Créer un nouveau menu
                openRoadMenu = menusWrapper.Object.Add(menuName);
            }
            
            if (openRoadMenu == null) return;
            
            try
            {
                int idx = 0;
                
                // Récupérer toutes les commandes visibles
                var allCommands = ModuleDiscovery.AllCommands
                    .Where(c => c.ShowInMenu)
                    .OrderBy(c => c.Order)
                    .ToList();

                // Groupement niveau 1 (Catégorie)
                // Si MenuCategory est null, on utilise un fallback sur le Module (comportement rétro-compatible)
                // Pour éviter les doublons, on utilise une clé composite (id) et on garde le nom affiché (label)
                var lvl1Groups = allCommands
                    .GroupBy(c => new 
                    { 
                        // Clé d'unicité du menu niveau 1
                        Id = c.MenuCategoryKey ?? c.MenuCategory ?? c.Module.NameKey ?? c.Module.Name 
                    })
                    .OrderBy(g => g.Min(c => c.Module.Order)) // On ordonne par l'ordre du premier module trouvé dans le groupe
                    .ToList();
                
                bool isFirst = true;

                foreach (var lvl1Group in lvl1Groups)
                {
                    var commandsLvl1 = lvl1Group.ToList();
                    if (commandsLvl1.Count == 0) continue;

                    if (!isFirst)
                    {
                        openRoadMenu.AddSeparator(idx++);
                    }
                    isFirst = false;

                    // Déterminer le nom du menu niveau 1
                    // On prend la première commande du groupe pour récupérer les infos de traduction
                    var representativeCmd = commandsLvl1.First();
                    
                    // Priorité : 
                    // 1. MenuCategoryKey (ex: "carto.title")
                    // 2. MenuCategory (ex: "Cartographie")
                    // 3. Module.NameKey (fallback)
                    // 4. Module.Name (fallback)
                    string lvl1Name;
                    if (!string.IsNullOrEmpty(representativeCmd.MenuCategoryKey))
                        lvl1Name = L10n.T(representativeCmd.MenuCategoryKey, representativeCmd.MenuCategory ?? "?");
                    else if (!string.IsNullOrEmpty(representativeCmd.MenuCategory))
                        lvl1Name = representativeCmd.MenuCategory;
                    else if (!string.IsNullOrEmpty(representativeCmd.Module.NameKey))
                        lvl1Name = L10n.T(representativeCmd.Module.NameKey, representativeCmd.Module.Name);
                    else
                        lvl1Name = representativeCmd.Module.Name;

                    using var lvl1MenuWrapper = new ComWrapper<AcadPopupMenu>(openRoadMenu.AddSubMenu(idx++, lvl1Name));
                    if (lvl1MenuWrapper.Object == null) continue;

                    int subIdx = 0;

                    // Groupement niveau 2 (Sous-catégorie)
                    // Les commandes sans sous-catégorie vont directement dans le menu niveau 1
                    // Celles avec sous-catégorie vont dans un sous-menu
                    
                    // On sépare celles qui ont une sous-catégorie de celles qui n'en ont pas
                    var commandsWithSub = commandsLvl1.Where(c => !string.IsNullOrEmpty(c.MenuSubCategory) || !string.IsNullOrEmpty(c.MenuSubCategoryKey)).ToList();
                    var commandsDirect = commandsLvl1.Where(c => string.IsNullOrEmpty(c.MenuSubCategory) && string.IsNullOrEmpty(c.MenuSubCategoryKey)).ToList();
                    
                    // 1. D'abord les sous-menus
                    var lvl2Groups = commandsWithSub
                        .GroupBy(c => new { Id = c.MenuSubCategoryKey ?? c.MenuSubCategory })
                        .OrderBy(g => g.Min(c => c.Order)); // Ordre basé sur la première commande

                    foreach (var lvl2Group in lvl2Groups)
                    {
                        var repCmdLvl2 = lvl2Group.First();
                        string lvl2Name;
                        if (!string.IsNullOrEmpty(repCmdLvl2.MenuSubCategoryKey))
                            lvl2Name = L10n.T(repCmdLvl2.MenuSubCategoryKey, repCmdLvl2.MenuSubCategory ?? "?");
                        else
                            lvl2Name = repCmdLvl2.MenuSubCategory!;

                        using var lvl2MenuWrapper = new ComWrapper<AcadPopupMenu>(lvl1MenuWrapper.Object.AddSubMenu(subIdx++, lvl2Name));
                        if (lvl2MenuWrapper.Object == null) continue;

                        int subSubIdx = 0;
                        foreach (var cmd in lvl2Group.OrderBy(c => c.Order))
                        {
                            var displayName = cmd.GetLocalizedDisplayName();
                            lvl2MenuWrapper.Object.AddMenuItem(subSubIdx++, displayName, cmd.CommandName + " ");
                        }
                    }

                    // Séparateur entre sous-menus et commandes directes si besoin
                    if (lvl2Groups.Any() && commandsDirect.Any())
                    {
                        lvl1MenuWrapper.Object.AddSeparator(subIdx++);
                    }

                    // 2. Ensuite les commandes directes
                    string? lastGroup = null;
                    foreach (var cmd in commandsDirect.OrderBy(c => c.Order))
                    {
                        // Gestion des groupes (séparateurs visuels)
                        if (cmd.Group != null && cmd.Group != lastGroup && lastGroup != null)
                        {
                            lvl1MenuWrapper.Object.AddSeparator(subIdx++);
                        }
                        lastGroup = cmd.Group;

                        var displayName = cmd.GetLocalizedDisplayName();
                        lvl1MenuWrapper.Object.AddMenuItem(subIdx++, displayName, cmd.CommandName + " ");
                    }
                }
                
                // === Commandes système (toujours présentes) ===
                openRoadMenu.AddSeparator(idx++);
                openRoadMenu.AddMenuItem(idx++, L10n.T("system.settings"), "OR_SETTINGS ");
                openRoadMenu.AddMenuItem(idx++, L10n.T("system.help"), "OR_HELP ");
                openRoadMenu.AddSeparator(idx++);
                openRoadMenu.AddMenuItem(idx++, L10n.T("about.title"), "OR_VERSION ");
                
                // Insérer dans la barre de menus si pas déjà présent
                if (!menuExisted || !IsMenuInMenuBar(menuBarWrapper.Object, openRoadMenu.Name))
                {
                    int insertIndex = menuBarWrapper.Object.Count - 1;
                    if (insertIndex < 0) insertIndex = 0;
                    openRoadMenu.InsertInMenuBar(insertIndex);
                }
                
                _menuCreated = true;
                Logger.Debug(L10n.T("ui.menu.created"));
            }
            finally
            {
                if (openRoadMenu != null)
                    try { Marshal.ReleaseComObject(openRoadMenu); } catch { }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("ui.menu.createError", ex.Message));
        }
    }
    
    /// <summary>
    /// Vérifie si un nom correspond au menu Open Road (toutes langues)
    /// </summary>
    private static bool IsOpenRoadMenu(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Equals(DefaultMenuName, StringComparison.OrdinalIgnoreCase)) return true;
        if (name.Contains(DefaultMenuName, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    
    /// <summary>
    /// Vérifie si un menu est présent dans la barre de menus
    /// </summary>
    private static bool IsMenuInMenuBar(AcadMenuBar menuBar, string menuName)
    {
        for (int i = 0; i < menuBar.Count; i++)
        {
            AcadPopupMenu? menu = null;
            try
            {
                menu = menuBar.Item(i);
                if (menu != null && menu.Name.Equals(menuName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch { }
            finally
            {
                if (menu != null)
                    try { Marshal.ReleaseComObject(menu); } catch { }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Vide tous les éléments d'un menu
    /// </summary>
    private static void ClearMenuItems(AcadPopupMenu menu)
    {
        try
        {
            // Supprimer tous les éléments du menu en partant de la fin
            while (menu.Count > 0)
            {
                AcadPopupMenuItem? item = null;
                try
                {
                    item = menu.Item(0);
                    item?.Delete();
                }
                catch (System.Exception ex)
                {
                    Logger.Debug($"ClearMenuItems: Error deleting item: {ex.Message}");
                    break; // Sortir si erreur
                }
                finally
                {
                    if (item != null)
                        try { Marshal.ReleaseComObject(item); } catch { }
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"ClearMenuItems: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Supprime le menu Open Road de la barre de menus.
    /// Note: Le menu reste dans le groupe de menus (limitation API COM AutoCAD).
    /// </summary>
    public static void RemoveMenu()
    {
        _menuCreated = false;
        
        try
        {
            using var acadAppWrapper = new ComWrapper<AcadApplication>((AcadApplication)AcadApp.AcadApplication);
            if (acadAppWrapper.Object == null) return;
            
            using var menuBarWrapper = new ComWrapper<AcadMenuBar>(acadAppWrapper.Object.MenuBar);
            if (menuBarWrapper.Object == null) return;

            // Retirer de la barre de menu
            for (int i = menuBarWrapper.Object.Count - 1; i >= 0; i--)
            {
                AcadPopupMenu? barMenu = null;
                try
                {
                    barMenu = menuBarWrapper.Object.Item(i);
                    if (barMenu != null && IsOpenRoadMenu(barMenu.Name))
                    {
                        barMenu.RemoveFromMenuBar();
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.Debug($"RemoveMenu: Error processing menu at index {i}: {ex.Message}");
                }
                finally
                {
                    if (barMenu != null)
                        try { Marshal.ReleaseComObject(barMenu); } catch { }
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"RemoveMenu: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reconstruit le menu (utilisé après changement de langue)
    /// </summary>
    public static void RebuildMenu()
    {
        _menuCreated = false;
        CreateMenu();
    }
}
