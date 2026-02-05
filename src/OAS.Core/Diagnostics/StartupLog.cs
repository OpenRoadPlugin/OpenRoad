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
