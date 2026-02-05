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
