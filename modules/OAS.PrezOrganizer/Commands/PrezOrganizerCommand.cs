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

using Autodesk.AutoCAD.Runtime;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;
using OpenAsphalte.Modules.PrezOrganizer.Models;
using OpenAsphalte.Modules.PrezOrganizer.Services;
using OpenAsphalte.Modules.PrezOrganizer.Views;

using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.PrezOrganizer.Commands;

/// <summary>
/// Commande principale de l'organiseur de présentations.
/// Ouvre la fenêtre WPF permettant de gérer les onglets de présentation.
/// </summary>
public class PrezOrganizerCommand : CommandBase
{
    /// <summary>
    /// Exécute la commande d'organisation des présentations
    /// </summary>
    [CommandMethod("OAS_PREZORG")]
    [CommandInfo("Organiseur de présentations",
        Description = "Organiser, trier et renommer les onglets de présentation",
        DisplayNameKey = "prezorganizer.cmd.title",
        DescriptionKey = "prezorganizer.cmd.desc",
        MenuCategory = "Mise en page",
        MenuCategoryKey = "menu.mep",
        Order = 10,
        RibbonSize = CommandSize.Large,
        Group = "Présentations",
        ShowInMenu = true,
        ShowInRibbon = true)]
    public void Execute()
    {
        ExecuteSafe(() =>
        {
            // Charger les présentations dans une transaction de lecture
            var layouts = ExecuteInTransaction<List<LayoutItem>>(tr =>
            {
                return LayoutService.GetAllLayouts(Database!, tr);
            });

            if (layouts == null || layouts.Count == 0)
            {
                Logger.Warning(T("prezorganizer.error.noSelection", "Aucune présentation trouvée"));
                return;
            }

            // Ouvrir la fenêtre de l'organiseur
            var window = new PrezOrganizerWindow(layouts, Database!);
            var result = AcadApp.ShowModalWindow(window);

            if (result == true && window.HasChanges)
            {
                // Appliquer les modifications dans une transaction d'écriture
                ExecuteInTransaction(tr =>
                {
                    int changeCount = LayoutService.ApplyChanges(Database!, tr, window.Items);
                    Logger.Success(TFormat("prezorganizer.success.count", changeCount));
                });
            }
            else
            {
                Logger.Info(T("prezorganizer.cancelled"));
            }
        });
    }
}
