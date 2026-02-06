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

using System.IO;
using System.Runtime.CompilerServices;

namespace OpenAsphalte.Diagnostics;

/// <summary>
/// Logging de démarrage et diagnostics. Utilisé pour tracer le chargement du plugin.
/// </summary>
internal static class StartupLog
{
    private static readonly object FileLock = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Open Asphalte", "logs", "openasphalte_startup.log");
    private static bool _directoryEnsured;

#pragma warning disable CA2255 // ModuleInitializer utilisé intentionnellement pour diagnostics au chargement
    [ModuleInitializer]
    internal static void Init()
#pragma warning restore CA2255
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            _directoryEnsured = true;
            File.AppendAllText(LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Assembly loaded: OpenAsphalte.Core\n");
        }
        catch
        {
            // Ignorer toutes erreurs de diagnostics
        }
    }

    public static void Write(string message)
    {
        try
        {
            if (!_directoryEnsured)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                _directoryEnsured = true;
            }
            lock (FileLock)
            {
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Ignorer toutes erreurs de diagnostics
        }
    }
}
