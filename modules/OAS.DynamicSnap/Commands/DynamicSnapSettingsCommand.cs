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
using OpenAsphalte.Modules.DynamicSnap.Services;
using OpenAsphalte.Modules.DynamicSnap.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Modules.DynamicSnap.Commands;

/// <summary>
/// Commande pour ouvrir la fenêtre de paramètres de l'accrochage dynamique OAS.
/// Accessible via le menu Open Asphalte > Dessin ou le ruban.
/// </summary>
public class DynamicSnapSettingsCommand : CommandBase
{
    [CommandMethod("OAS_SNAP_SETTINGS")]
    [CommandInfo("Accrochage OAS",
        Description = "Configure les paramètres de l'accrochage dynamique OAS",
        DisplayNameKey = "dynamicsnap.cmd.title",
        DescriptionKey = "dynamicsnap.cmd.desc",
        MenuCategory = "Dessin",
        MenuCategoryKey = "menu.dessin",
        Order = 5,
        RibbonSize = CommandSize.Large,
        Group = "Accrochage",
        ShowInMenu = true,
        ShowInRibbon = true)]
    public void Execute()
    {
        ExecuteSafe(() =>
        {
            var window = new DynamicSnapSettingsWindow();
            var result = AcadApp.ShowModalWindow(window);

            if (result == true)
            {
                // Les paramètres ont été sauvegardés par la fenêtre
                Logger.Info(T("dynamicsnap.settings.saved"));
            }
        });
    }
}
