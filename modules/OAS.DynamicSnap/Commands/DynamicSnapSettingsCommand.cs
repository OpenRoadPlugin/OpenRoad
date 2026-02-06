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
