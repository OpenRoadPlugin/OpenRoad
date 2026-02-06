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

using System.Windows;
using Config = OpenAsphalte.Configuration.Configuration;
using OpenAsphalte.Logging;

namespace OpenAsphalte.UI;

/// <summary>
/// Helper statique pour gérer la persistance de taille et position des fenêtres.
/// Mémorise l'état entre les sessions et vérifie que la fenêtre reste visible à l'écran.
/// </summary>
/// <remarks>
/// Utilisation dans une fenêtre :
/// <code>
/// public MyWindow()
/// {
///     InitializeComponent();
///     WindowStateHelper.RestoreState(this, "mywindow", 800, 600);
///     Closing += (s, e) => WindowStateHelper.SaveState(this, "mywindow");
/// }
/// </code>
/// </remarks>
public static class WindowStateHelper
{
    private const double DefaultMarginPercent = 0.95; // 95% de la zone de travail
    private const double MinVisiblePortion = 100.0;   // Minimum 100px visible pour considérer la fenêtre "à l'écran"

    /// <summary>
    /// Restaure la taille et la position d'une fenêtre depuis la configuration.
    /// Si aucune configuration n'existe ou si la fenêtre serait hors écran,
    /// utilise les dimensions par défaut centrées sur l'écran.
    /// </summary>
    /// <param name="window">Fenêtre à configurer</param>
    /// <param name="windowId">Identifiant unique pour stocker les paramètres (ex: "prezorganizer")</param>
    /// <param name="defaultWidth">Largeur par défaut si aucune sauvegarde</param>
    /// <param name="defaultHeight">Hauteur par défaut si aucune sauvegarde (null = 95% de la hauteur de travail)</param>
    public static void RestoreState(Window window, string windowId, double defaultWidth, double? defaultHeight = null)
    {
        try
        {
            // Calculer la hauteur par défaut basée sur la zone de travail
            var workArea = SystemParameters.WorkArea;
            double calculatedDefaultHeight = defaultHeight ?? (workArea.Height * DefaultMarginPercent);

            // Récupérer les valeurs sauvegardées
            double width = Config.Get($"{windowId}.width", defaultWidth);
            double height = Config.Get($"{windowId}.height", calculatedDefaultHeight);
            double left = Config.Get($"{windowId}.left", double.NaN);
            double top = Config.Get($"{windowId}.top", double.NaN);

            // Appliquer les contraintes minimales
            if (window.MinWidth > 0 && width < window.MinWidth) width = window.MinWidth;
            if (window.MinHeight > 0 && height < window.MinHeight) height = window.MinHeight;

            // Appliquer les dimensions
            window.Width = width;
            window.Height = height;

            // Vérifier si on a une position sauvegardée valide
            if (!double.IsNaN(left) && !double.IsNaN(top))
            {
                // Créer un rectangle représentant la fenêtre à la position sauvegardée
                var windowRect = new Rect(left, top, width, height);

                if (IsRectOnScreen(windowRect))
                {
                    // Position valide, l'appliquer
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                    window.Left = left;
                    window.Top = top;
                    Logger.Debug($"[WindowStateHelper] Restored {windowId}: {width}x{height} at ({left}, {top})");
                    return;
                }
                else
                {
                    Logger.Debug($"[WindowStateHelper] Saved position for {windowId} is off-screen, centering");
                }
            }

            // Pas de position valide : centrer sur l'écran
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Logger.Debug($"[WindowStateHelper] Restored {windowId}: {width}x{height} (centered)");
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"[WindowStateHelper] Error restoring state for {windowId}: {ex.Message}");
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    /// <summary>
    /// Sauvegarde la taille et la position actuelles d'une fenêtre dans la configuration.
    /// </summary>
    /// <param name="window">Fenêtre à sauvegarder</param>
    /// <param name="windowId">Identifiant unique utilisé pour RestoreState</param>
    public static void SaveState(Window window, string windowId)
    {
        try
        {
            // Ne pas sauvegarder si la fenêtre est minimisée ou maximisée
            if (window.WindowState != WindowState.Normal)
            {
                Logger.Debug($"[WindowStateHelper] Skip save for {windowId}: window state is {window.WindowState}");
                return;
            }

            Config.Set($"{windowId}.width", window.Width);
            Config.Set($"{windowId}.height", window.Height);
            Config.Set($"{windowId}.left", window.Left);
            Config.Set($"{windowId}.top", window.Top);
            Config.Save();

            Logger.Debug($"[WindowStateHelper] Saved {windowId}: {window.Width}x{window.Height} at ({window.Left}, {window.Top})");
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"[WindowStateHelper] Error saving state for {windowId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Calcule la hauteur de fenêtre recommandée (95% de la zone de travail).
    /// Utile pour définir la hauteur par défaut d'une fenêtre.
    /// </summary>
    /// <returns>Hauteur en pixels</returns>
    public static double GetRecommendedHeight()
    {
        return SystemParameters.WorkArea.Height * DefaultMarginPercent;
    }

    /// <summary>
    /// Calcule la largeur de fenêtre recommandée pour un ratio donné.
    /// </summary>
    /// <param name="heightRatio">Ratio largeur/hauteur (ex: 0.6 pour une fenêtre plus large que haute)</param>
    /// <returns>Largeur en pixels</returns>
    public static double GetRecommendedWidth(double heightRatio = 1.0)
    {
        return GetRecommendedHeight() * heightRatio;
    }

    /// <summary>
    /// Vérifie si un rectangle est suffisamment visible sur au moins un écran.
    /// </summary>
    /// <param name="rect">Rectangle à vérifier</param>
    /// <returns>True si au moins MinVisiblePortion pixels sont visibles</returns>
    private static bool IsRectOnScreen(Rect rect)
    {
        // Utiliser la zone de travail principale comme référence simple
        // Pour un support multi-écran complet, on pourrait utiliser System.Windows.Forms.Screen.AllScreens
        var workArea = SystemParameters.WorkArea;

        // Calculer l'intersection avec la zone de travail
        var intersection = Rect.Intersect(rect, workArea);

        if (intersection.IsEmpty)
            return false;

        // Vérifier qu'une portion suffisante est visible
        return intersection.Width >= MinVisiblePortion && intersection.Height >= MinVisiblePortion;
    }

    /// <summary>
    /// Réinitialise les paramètres de fenêtre sauvegardés pour un identifiant donné.
    /// La prochaine ouverture utilisera les valeurs par défaut.
    /// </summary>
    /// <param name="windowId">Identifiant de la fenêtre</param>
    public static void ResetState(string windowId)
    {
        try
        {
            Config.Set<object?>($"{windowId}.width", null);
            Config.Set<object?>($"{windowId}.height", null);
            Config.Set<object?>($"{windowId}.left", null);
            Config.Set<object?>($"{windowId}.top", null);
            Config.Save();
            Logger.Debug($"[WindowStateHelper] Reset state for {windowId}");
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"[WindowStateHelper] Error resetting state for {windowId}: {ex.Message}");
        }
    }
}
