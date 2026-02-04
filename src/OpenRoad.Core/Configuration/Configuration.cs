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

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenRoad.Diagnostics;
using OpenRoad.Logging;
using L10n = OpenRoad.Localization.Localization;

namespace OpenRoad.Configuration;

/// <summary>
/// Gestion de la configuration utilisateur Open Road.
/// Stocke les parametres dans un fichier JSON dans AppData.
/// </summary>
public static class Configuration
{
    private static readonly string ConfigFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Open Road"
    );
    
    private static readonly string ConfigFile = Path.Combine(ConfigFolder, "config.json");
    private const long MaxConfigSizeBytes = 512 * 1024; // 512 KB
    
    private static Dictionary<string, object> _settings = new();
    private static bool _loaded = false;
    private static bool _isLoading = false;
    private static readonly object _loadLock = new();
    
    /// <summary>
    /// Evenement declenche quand une configuration change.
    /// </summary>
    public static event Action<string, object?>? OnSettingChanged;
    
    /// <summary>
    /// Chemin du dossier de configuration
    /// </summary>
    public static string ConfigurationFolder => ConfigFolder;
    
    /// <summary>
    /// Chemin du fichier de configuration
    /// </summary>
    public static string ConfigurationFile => ConfigFile;
    
    /// <summary>
    /// Nettoie les abonnes aux evenements
    /// </summary>
    public static void ClearEventSubscribers()
    {
        OnSettingChanged = null;
    }
    
    /// <summary>
    /// Version actuelle du schéma de configuration.
    /// Incrémentez cette valeur lors de changements breaking dans la structure.
    /// </summary>
    public const int CurrentConfigVersion = 1;
    
    /// <summary>
    /// Version du fichier de configuration chargé
    /// </summary>
    public static int LoadedConfigVersion { get; private set; } = 0;
    
    /// <summary>
    /// Charge la configuration depuis le fichier
    /// </summary>
    public static void Load()
    {
        lock (_loadLock)
        {
            if (_isLoading) return;
            _isLoading = true;
        }

        try
        {
            StartupLog.Write($"Configuration.Load: begin (file={ConfigFile})");
            if (File.Exists(ConfigFile))
            {
                StartupLog.Write("Configuration.Load: file exists");
                var fileInfo = new FileInfo(ConfigFile);
                if (fileInfo.Length > MaxConfigSizeBytes)
                {
                    StartupLog.Write($"Configuration.Load: file too large ({fileInfo.Length} bytes) -> reset");
                    ResetCorruptConfig();
                    _loaded = true;
                    return;
                }
                var json = File.ReadAllText(ConfigFile);
                StartupLog.Write($"Configuration.Load: read {json.Length} chars");
                
                // Validation basique du JSON avant désérialisation
                if (string.IsNullOrWhiteSpace(json))
                {
                    _settings = new();
                    _loaded = true;
                    Logger.Debug(L10n.T("config.empty", "Fichier config vide, nouvelle configuration creee"));
                }
                else
                {
                    var options = new JsonSerializerOptions
                    {
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };
                    _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options) ?? new();
                    _loaded = true;
                    Logger.Debug(string.Format(L10n.T("config.loaded", "Configuration chargee depuis {0}"), ConfigFile));
                    StartupLog.Write("Configuration.Load: deserialized");
                    
                    // Vérifier et migrer la version
                    LoadedConfigVersion = ReadConfigVersion();
                    if (LoadedConfigVersion < CurrentConfigVersion)
                    {
                        StartupLog.Write($"Configuration.Load: migrate {LoadedConfigVersion} -> {CurrentConfigVersion}");
                        MigrateConfiguration(LoadedConfigVersion, CurrentConfigVersion);
                    }
                }
            }
            else
            {
                StartupLog.Write("Configuration.Load: file missing, creating defaults");
                _settings = new();
                // Nouvelle configuration : définir la version
                _settings["_configVersion"] = CurrentConfigVersion;
                LoadedConfigVersion = CurrentConfigVersion;
                _loaded = true;
                Logger.Debug(L10n.T("config.created", "Nouvelle configuration creee"));
            }
            StartupLog.Write("Configuration.Load: end");
        }
        catch (JsonException jsonEx)
        {
            StartupLog.Write($"Configuration.Load: JSON error {jsonEx}");
            Logger.Warning(string.Format(L10n.T("config.corrupt", "Fichier config corrompu, reinitialisation: {0}"), jsonEx.Message));
            ResetCorruptConfig();
            _loaded = true;
        }
        catch (System.Exception ex)
        {
            StartupLog.Write($"Configuration.Load: error {ex}");
            Logger.Error(string.Format(L10n.T("config.loadError", "Erreur chargement config: {0}"), ex.Message));
            _settings = new();
            _loaded = true;
        }
        finally
        {
            lock (_loadLock)
            {
                _isLoading = false;
            }
        }
    }

    private static void ResetCorruptConfig()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var backupPath = ConfigFile + ".corrupt." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Move(ConfigFile, backupPath);
                StartupLog.Write($"Configuration.Load: backup created {backupPath}");
            }
        }
        catch (System.Exception ex)
        {
            // Ignorer les erreurs de backup mais logger
            StartupLog.Write($"Configuration.ResetCorruptConfig: backup failed: {ex.Message}");
        }

        _settings = new();
        _settings["_configVersion"] = CurrentConfigVersion;
        LoadedConfigVersion = CurrentConfigVersion;
        Save();
    }
    
    /// <summary>
    /// Migre la configuration d'une ancienne version vers la nouvelle.
    /// </summary>
    /// <param name="fromVersion">Version source</param>
    /// <param name="toVersion">Version cible</param>
    private static void MigrateConfiguration(int fromVersion, int toVersion)
    {
        Logger.Info(L10n.TFormat("config.migrating", fromVersion, toVersion));
        
        // Migrations séquentielles
        for (int v = fromVersion; v < toVersion; v++)
        {
            switch (v)
            {
                case 0:
                    // Migration v0 -> v1 : ajout des valeurs par défaut manquantes
                    if (!_settings.ContainsKey("language"))
                        _settings["language"] = "fr";
                    if (!_settings.ContainsKey("devMode"))
                        _settings["devMode"] = false;
                    if (!_settings.ContainsKey("checkUpdatesOnStartup"))
                        _settings["checkUpdatesOnStartup"] = true;
                    break;
            }
        }
        
        // Mettre à jour la version
        _settings["_configVersion"] = toVersion;
        LoadedConfigVersion = toVersion;
        
        // Sauvegarder après migration
        Save();
        Logger.Success(L10n.T("config.migrated", "Configuration migrée avec succès"));
    }
    
    /// <summary>
    /// Sauvegarde la configuration
    /// </summary>
    public static void Save()
    {
        try
        {
            // S'assurer que la version est toujours présente
            if (!_settings.ContainsKey("_configVersion"))
            {
                _settings["_configVersion"] = CurrentConfigVersion;
            }
            
            Directory.CreateDirectory(ConfigFolder);
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(ConfigFile, json);
            Logger.Debug(L10n.T("config.saved", "Configuration sauvegardee"));
        }
        catch (System.Exception ex)
        {
            Logger.Error(string.Format(L10n.T("config.saveError", "Erreur sauvegarde config: {0}"), ex.Message));
        }
    }
    
    /// <summary>
    /// Recharge la configuration depuis le fichier
    /// </summary>
    public static void Reload()
    {
        _loaded = false;
        Load();
        Logger.Info(L10n.T("config.reloaded", "Configuration rechargee"));
    }
    
    /// <summary>
    /// Récupère une valeur de configuration
    /// </summary>
    /// <typeparam name="T">Type de la valeur</typeparam>
    /// <param name="key">Clé de configuration</param>
    /// <param name="defaultValue">Valeur par défaut</param>
    /// <returns>Valeur ou défaut si non trouvée</returns>
    public static T Get<T>(string key, T defaultValue)
    {
        EnsureLoaded();
        
        if (_settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    var result = JsonSerializer.Deserialize<T>(element.GetRawText());
                    return result ?? defaultValue;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (JsonException jsonEx)
            {
                Logger.Debug($"Config key '{key}': JSON conversion error - {jsonEx.Message}");
                return defaultValue;
            }
            catch (InvalidCastException castEx)
            {
                Logger.Debug($"Config key '{key}': Type conversion error - {castEx.Message}");
                return defaultValue;
            }
            catch (System.Exception ex)
            {
                Logger.Debug($"Config key '{key}': Unexpected error - {ex.Message}");
                return defaultValue;
            }
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Definit une valeur de configuration
    /// </summary>
    /// <typeparam name="T">Type de la valeur</typeparam>
    /// <param name="key">Cle de configuration</param>
    /// <param name="value">Valeur a definir</param>
    public static void Set<T>(string key, T value)
    {
        EnsureLoaded();
        _settings[key] = value!;
        OnSettingChanged?.Invoke(key, value);
    }
    
    /// <summary>
    /// Supprime une cle de configuration
    /// </summary>
    public static bool Remove(string key)
    {
        EnsureLoaded();
        return _settings.Remove(key);
    }
    
    /// <summary>
    /// Verifie si une cle existe
    /// </summary>
    public static bool Contains(string key)
    {
        EnsureLoaded();
        return _settings.ContainsKey(key);
    }
    
    private static void EnsureLoaded()
    {
        if (_isLoading) return;
        if (!_loaded) Load();
    }

    private static int ReadConfigVersion()
    {
        if (_settings.TryGetValue("_configVersion", out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
                        return number;
                    if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
                        return parsed;
                }

                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        return 0;
    }
    
    #region Propriétés raccourcis
    
    /// <summary>
    /// Langue active (fr, en, es).
    /// Pour changer la langue avec notification de l'UI, utilisez <see cref="Localization.Localization.SetLanguage"/>.
    /// </summary>
    /// <remarks>
    /// Cette propriété accède directement à la configuration.
    /// Si vous voulez que le changement de langue soit propag? ? l'UI,
    /// utilisez plut?t <c>Localization.Localization.SetLanguage(language)</c>.
    /// </remarks>
    public static string Language
    {
        get => Get("language", "fr");
        set => Set("language", value);
    }
    
    /// <summary>
    /// Mode developpeur (affiche les logs de debug)
    /// </summary>
    public static bool DevMode
    {
        get => Get("devMode", false);
        set
        {
            Set("devMode", value);
            Logger.DebugMode = value;
        }
    }
    
    /// <summary>
    /// URL de mise a jour (page des releases GitHub)
    /// </summary>
    public static string UpdateUrl
    {
        get => Get("updateUrl", "https://github.com/openroadplugin/openroad/releases/latest");
        set => Set("updateUrl", value);
    }
    
    /// <summary>
    /// Verifier les mises a jour au demarrage
    /// </summary>
    public static bool CheckUpdatesOnStartup
    {
        get => Get("checkUpdatesOnStartup", true);
        set => Set("checkUpdatesOnStartup", value);
    }
    
    #endregion
}
