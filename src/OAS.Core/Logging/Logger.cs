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
using Autodesk.AutoCAD.EditorInput;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using L10n = OpenAsphalte.Localization.Localization;

namespace OpenAsphalte.Logging;

/// <summary>
/// Système de logging unifié pour Open Asphalte.
/// Affiche les messages dans la ligne de commande AutoCAD et optionnellement dans un fichier.
/// </summary>
public static class Logger
{
    private static readonly object _fileLock = new();
    private static string? _logFilePath;
    private static StreamWriter? _logWriter;
    private static int _writeCount;
    private const int RotateCheckInterval = 50;
    private static readonly int MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Mode debug activé (affiche les messages Debug)
    /// </summary>
    public static bool DebugMode { get; set; } = false;

    /// <summary>
    /// Active l'écriture dans un fichier log (activé par défaut pour le diagnostic)
    /// </summary>
    public static bool FileLoggingEnabled { get; set; } = true;

    /// <summary>
    /// Préfixe des messages
    /// </summary>
    public static string Prefix { get; set; } = "Open Asphalte";

    /// <summary>
    /// Chemin du fichier de log (dans AppData/Open Asphalte/logs/)
    /// </summary>
    public static string LogFilePath
    {
        get
        {
            if (_logFilePath == null)
            {
                var logFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Open Asphalte", "logs"
                );
                Directory.CreateDirectory(logFolder);
                _logFilePath = Path.Combine(logFolder, $"openasphalte_{DateTime.Now:yyyy-MM-dd}.log");
            }
            return _logFilePath;
        }
    }

    private static Editor? Editor => AcadApp.DocumentManager.MdiActiveDocument?.Editor;

    /// <summary>
    /// Message de debug (seulement si DevMode activé)
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public static void Debug(string message)
    {
        if (DebugMode)
        {
            Write($"{L10n.T("log.level.debug", "[DEBUG]")} {message}", LogLevel.Debug);
        }
    }

    /// <summary>
    /// Message d'information
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public static void Info(string message)
    {
        Write($"{L10n.T("log.level.info", "[INFO]")} {message}", LogLevel.Info);
    }

    /// <summary>
    /// Message de succès
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public static void Success(string message)
    {
        Write($"{L10n.T("log.level.success", "[OK]")} {message}", LogLevel.Success);
    }

    /// <summary>
    /// Message d'avertissement
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public static void Warning(string message)
    {
        Write($"{L10n.T("log.level.warn", "[WARN]")} {message}", LogLevel.Warning);
    }

    /// <summary>
    /// Message d'erreur
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public static void Error(string message)
    {
        Write($"{L10n.T("log.level.error", "[ERROR]")} {message}", LogLevel.Error);
    }

    /// <summary>
    /// Message brut sans préfixe
    /// </summary>
    /// <param name="message">Message à afficher</param>
    public static void Raw(string message)
    {
        Editor?.WriteMessage($"\n{message}");
        WriteToFile(message, LogLevel.Info);
    }

    /// <summary>
    /// Écrit un message formaté avec préfixe
    /// </summary>
    private static void Write(string message, LogLevel level)
    {
        var prefix = L10n.T("app.name", Prefix);
        var fullMessage = $"{prefix} {message}";
        Editor?.WriteMessage($"\n{fullMessage}");
        WriteToFile(fullMessage, level);
    }

    /// <summary>
    /// Écrit dans le fichier de log si activé
    /// </summary>
    private static void WriteToFile(string message, LogLevel level)
    {
        if (!FileLoggingEnabled) return;

        try
        {
            lock (_fileLock)
            {
                // Rotation vérifiée tous les N writes pour éviter le check I/O systématique
                if (++_writeCount >= RotateCheckInterval)
                {
                    _writeCount = 0;
                    RotateLogIfNeeded();
                }

                EnsureLogWriter();
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                _logWriter!.WriteLine($"[{timestamp}] [{level}] {message}");
                _logWriter.Flush();
            }
        }
        catch
        {
            // Ne pas propager les erreurs de logging fichier
            CloseLogWriter();
        }
    }

    /// <summary>
    /// Initialise ou réutilise le StreamWriter persistant
    /// </summary>
    private static void EnsureLogWriter()
    {
        if (_logWriter != null) return;

        var logDir = Path.GetDirectoryName(LogFilePath);
        if (logDir != null) Directory.CreateDirectory(logDir);

        _logWriter = new StreamWriter(LogFilePath, append: true, System.Text.Encoding.UTF8)
        {
            AutoFlush = false
        };
    }

    /// <summary>
    /// Ferme proprement le StreamWriter
    /// </summary>
    private static void CloseLogWriter()
    {
        try
        {
            _logWriter?.Dispose();
        }
        catch { /* Ignore */ }
        _logWriter = null;
    }

    /// <summary>
    /// Effectue une rotation du fichier de log si nécessaire
    /// </summary>
    private static void RotateLogIfNeeded()
    {
        try
        {
            if (!File.Exists(LogFilePath)) return;

            var fileInfo = new FileInfo(LogFilePath);
            if (fileInfo.Length < MaxLogFileSizeBytes) return;

            // Fermer le writer avant de déplacer le fichier
            CloseLogWriter();

            var archivePath = Path.Combine(
                Path.GetDirectoryName(LogFilePath)!,
                $"openasphalte_{DateTime.Now:yyyy-MM-dd_HHmmss}.log.old"
            );
            File.Move(LogFilePath, archivePath);

            CleanupOldLogs();
        }
        catch
        {
            // Ignorer les erreurs de rotation
        }
    }

    /// <summary>
    /// Supprime les anciens fichiers de log
    /// </summary>
    private static void CleanupOldLogs()
    {
        try
        {
            var logFolder = Path.GetDirectoryName(LogFilePath);
            if (logFolder == null) return;

            var oldLogs = Directory.GetFiles(logFolder, "*.log.old")
                .OrderByDescending(f => File.GetCreationTime(f))
                .Skip(5);

            foreach (var oldLog in oldLogs)
            {
                File.Delete(oldLog);
            }
        }
        catch
        {
            // Ignorer les erreurs de nettoyage
        }
    }

    /// <summary>
    /// Niveaux de log
    /// </summary>
    private enum LogLevel
    {
        Debug,
        Info,
        Success,
        Warning,
        Error
    }
}
