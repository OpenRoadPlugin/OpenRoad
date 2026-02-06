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
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAsphalte.Configuration;
using OpenAsphalte.Discovery;
using OpenAsphalte.Logging;
using L10n = OpenAsphalte.Localization.Localization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OpenAsphalte.Services;

/// <summary>
/// Service de gestion des mises à jour et du téléchargement de modules.
/// </summary>
public static class UpdateService
{
    private static readonly SocketsHttpHandler _httpHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };
    private static readonly HttpClient _httpClient = new(_httpHandler)
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    private const string MarketplaceUrl = "https://raw.githubusercontent.com/OpenAsphaltePlugin/OpenAsphalte/main/docs/marketplace.json";
    private const string GitHubApiReleasesUrl = "https://api.github.com/repos/OpenAsphaltePlugin/OpenAsphalte/releases/latest";

    // User-Agent requis par l'API GitHub
    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("OpenAsphalte-Plugin/1.0");
    }

    #region Startup Update Check

    /// <summary>
    /// Récupère la version majeure d'AutoCAD (ex: 2025, 2026).
    /// </summary>
    /// <returns>Version AutoCAD sous forme de string (ex: "2025")</returns>
    public static string GetAutoCADVersion()
    {
        try
        {
            // ACADVER renvoie la version interne (ex: "25.0" pour AutoCAD 2025)
            var acadVer = AcadApp.GetSystemVariable("ACADVER")?.ToString() ?? "";
            // Format: "25.0s (LMS Tech)" -> extraire "25.0"
            var parts = acadVer.Split(' ')[0].Split('.');
            if (parts.Length >= 1 && int.TryParse(parts[0], out var majorVer))
            {
                // AutoCAD 2025 = version 25.x, AutoCAD 2000 = version 15.x
                // Formule: Année = 2000 + (version - 15) pour versions >= 15
                // Pour version 25 -> 2025
                var year = 2000 + majorVer;
                return year.ToString();
            }
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"GetAutoCADVersion error: {ex.Message}");
        }
        return "2025"; // Fallback
    }

    /// <summary>
    /// Vérifie si une mise à jour est disponible au démarrage.
    /// Cette méthode est appelée de manière asynchrone et non-bloquante.
    /// </summary>
    /// <returns>Résultat de la vérification avec informations de mise à jour</returns>
    public static async Task<StartupUpdateResult> CheckStartupUpdateAsync()
    {
        var result = new StartupUpdateResult();

        try
        {
            // Récupérer les informations de release depuis GitHub API
            var response = await _httpClient.GetAsync(GitHubApiReleasesUrl);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Debug($"GitHub API returned {response.StatusCode}");
                return result;
            }

            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                Logger.Debug("No release found or empty tag");
                return result;
            }

            // Extraire la version depuis le tag (format: v0.0.2 ou 0.0.2)
            var tagVersion = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(tagVersion, out var latestVersion))
            {
                Logger.Debug($"Cannot parse version from tag: {release.TagName}");
                return result;
            }

            // Récupérer la version actuelle
            var currentVersionStr = Plugin.Version;
            // Nettoyer la version (supprimer suffixes comme -dev, +hash)
            var cleanVersion = currentVersionStr.Split('-')[0].Split('+')[0];
            if (!Version.TryParse(cleanVersion, out var currentVersion))
            {
                Logger.Debug($"Cannot parse current version: {currentVersionStr}");
                return result;
            }

            result.CurrentVersion = currentVersion;
            result.LatestVersion = latestVersion;
            result.ReleaseUrl = release.HtmlUrl ?? "";
            result.ReleaseNotes = release.Body ?? "";
            result.DownloadUrl = FindInstallerAsset(release);

            // Comparer les versions
            if (latestVersion <= currentVersion)
            {
                Logger.Debug($"No update available: current={currentVersion}, latest={latestVersion}");
                result.IsUpToDate = true;
                return result;
            }

            // Vérifier la version AutoCAD minimale requise
            var minAutoCAD = ExtractMinAutoCADVersion(release);
            var currentAutoCAD = GetAutoCADVersion();

            if (!string.IsNullOrEmpty(minAutoCAD))
            {
                if (int.TryParse(minAutoCAD, out var minYear) &&
                    int.TryParse(currentAutoCAD, out var currentYear))
                {
                    if (currentYear < minYear)
                    {
                        Logger.Info(L10n.TFormat("update.incompatibleAutoCAD", latestVersion.ToString(), minAutoCAD, currentAutoCAD));
                        result.IncompatibleAutoCAD = true;
                        result.RequiredAutoCADVersion = minAutoCAD;
                        return result;
                    }
                }
            }

            // Mise à jour disponible et compatible!
            result.UpdateAvailable = true;
            Logger.Info(L10n.TFormat("update.available", latestVersion.ToString()));

            return result;
        }
        catch (HttpRequestException ex)
        {
            Logger.Debug($"Update check network error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            Logger.Debug("Update check timed out");
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"Update check error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Extrait la version minimale d'AutoCAD depuis le corps de la release.
    /// Cherche un pattern comme "minAutoCAD: 2025" ou "AutoCAD 2026+" dans les notes.
    /// </summary>
    private static string ExtractMinAutoCADVersion(GitHubRelease release)
    {
        // Par défaut, utiliser 2025
        var defaultVersion = "2025";

        if (string.IsNullOrEmpty(release.Body))
            return defaultVersion;

        // Chercher des patterns courants:
        // - "minAutoCAD: 2025"
        // - "AutoCAD 2026+"
        // - "Requires AutoCAD 2025"
        var body = release.Body;

        // Pattern: minAutoCAD: XXXX
        var match = System.Text.RegularExpressions.Regex.Match(
            body, @"minAutoCAD\s*[:=]\s*(\d{4})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
            return match.Groups[1].Value;

        // Pattern: AutoCAD XXXX+ ou "Requires AutoCAD XXXX"
        match = System.Text.RegularExpressions.Regex.Match(
            body, @"(?:requires\s+)?AutoCAD\s+(\d{4})\+?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
            return match.Groups[1].Value;

        return defaultVersion;
    }

    /// <summary>
    /// Trouve l'URL de l'installateur dans les assets de la release.
    /// </summary>
    private static string FindInstallerAsset(GitHubRelease release)
    {
        if (release.Assets == null || release.Assets.Count == 0)
            return release.HtmlUrl ?? "";

        // Chercher un asset qui ressemble à un installateur
        var installer = release.Assets.FirstOrDefault(a =>
            a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase) ||
            a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        return installer?.BrowserDownloadUrl ?? release.HtmlUrl ?? "";
    }

    /// <summary>
    /// Affiche une notification non-bloquante de mise à jour disponible.
    /// </summary>
    public static void ShowUpdateNotification(StartupUpdateResult result)
    {
        if (!result.UpdateAvailable || result.LatestVersion == null)
            return;

        try
        {
            // Afficher via MessageBox (non-bloquant car appelé dans un contexte approprié)
            var message = L10n.TFormat("update.notification.message",
                result.LatestVersion.ToString(),
                result.CurrentVersion?.ToString() ?? "?");

            var mbResult = System.Windows.MessageBox.Show(
                message,
                L10n.T("update.notification.title"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);

            if (mbResult == System.Windows.MessageBoxResult.Yes)
            {
                // Ouvrir la page de téléchargement
                var url = string.IsNullOrEmpty(result.DownloadUrl) ? result.ReleaseUrl : result.DownloadUrl;
                if (UrlValidationService.IsValidUpdateUrl(url) || url.StartsWith("https://github.com"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Debug($"ShowUpdateNotification error: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Verifie les mises a jour disponibles.
    /// </summary>
    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var url = Configuration.Configuration.Get("updateUrl", MarketplaceUrl);
            var officialTask = SafeGetManifestAsync(url);
            var customTask = GetCustomManifestAsync();

            await Task.WhenAll(officialTask, customTask);

            var officialManifest = officialTask.Result;
            var customManifest = customTask.Result;

            if (officialManifest == null && customManifest == null)
            {
                return new UpdateCheckResult { Success = false, ErrorMessage = "No manifest found" };
            }

            // Merge manifests - Custom takes precedence
            var mergedManifest = new MarketplaceManifest();
            if (officialManifest != null)
            {
                mergedManifest.Core = officialManifest.Core;
                mergedManifest.Modules.AddRange(officialManifest.Modules);
            }

            if (customManifest != null)
            {
                // If official manifest was null, use custom core definition
                if (officialManifest == null)
                {
                    mergedManifest.Core = customManifest.Core;
                }

                foreach (var customModule in customManifest.Modules)
                {
                    customModule.IsCustomSource = true;
                    var existing = mergedManifest.Modules.FirstOrDefault(m => m.Id.Equals(customModule.Id, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        mergedManifest.Modules.Remove(existing);
                    }
                    mergedManifest.Modules.Add(customModule);
                }
            }

            var result = new UpdateCheckResult { Success = true, Manifest = mergedManifest };

            // Verifier maj Core
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            if (Version.TryParse(mergedManifest.Core.Latest, out var latestVersion))
            {
                if (latestVersion > currentVersion)
                {
                    result.CoreUpdateAvailable = true;
                    result.LatestCoreVersion = latestVersion;
                }
            }

            // Verifier modules
            var coreVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

            foreach (var moduleDef in mergedManifest.Modules)
            {
                // RESOLUTION DE VERSION:
                // Trouver la meilleure version compatible avec ce Core
                // 1. Considerer la version "principale" (Latest)
                // 2. Considerer toutes les versions "historiques"
                // 3. Filtrer celles ou MinCoreVersion <= coreVersion
                // 4. Trier par version module decroissante

                var candidates = new List<ModuleVersionInfo>();

                // Ajouter la version "racine"
                candidates.Add(new ModuleVersionInfo
                {
                    Version = moduleDef.Version,
                    MinCoreVersion = moduleDef.MinCoreVersion,
                    DownloadUrl = moduleDef.DownloadUrl
                });

                // Ajouter l'historique s'il existe
                if (moduleDef.PreviousVersions != null)
                {
                    candidates.AddRange(moduleDef.PreviousVersions);
                }

                // Filtrer et trier
                var bestVersion = candidates
                    .Where(v =>
                    {
                        // Vérifier MinCoreVersion
                        if (Version.TryParse(v.MinCoreVersion, out var minCore))
                        {
                            if (minCore > coreVersion) return false;
                        }

                        // Vérifier MaxCoreVersion (si définie)
                        if (!string.IsNullOrEmpty(v.MaxCoreVersion) && Version.TryParse(v.MaxCoreVersion, out var maxCore))
                        {
                            if (maxCore < coreVersion) return false;
                        }

                        return true;
                    })
                    .OrderByDescending(v =>
                    {
                        Version.TryParse(v.Version, out var ver);
                        return ver;
                    })
                    .FirstOrDefault();

                // Si aucune version compatible (bizarre, mais possible si module demande Core vNext)
                if (bestVersion == null) continue;

                // Mettre a jour les metadonnees du module avec la version choisie
                // (Cela permet a InstallModuleAsync d'utiliser la bonne URL et version)
                moduleDef.Version = bestVersion.Version;
                moduleDef.MinCoreVersion = bestVersion.MinCoreVersion;
                moduleDef.DownloadUrl = bestVersion.DownloadUrl;


                // Chercher si le module est installe
                var installed = ModuleDiscovery.Modules.FirstOrDefault(m => m.Id.Equals(moduleDef.Id, StringComparison.OrdinalIgnoreCase));

                if (installed != null)
                {
                    if (Version.TryParse(moduleDef.Version, out var remoteVer) &&
                        Version.TryParse(installed.Version, out var localVer))
                    {
                        if (remoteVer > localVer)
                        {
                            result.Updates.Add(new ModuleUpdateInfo
                            {
                                ModuleId = moduleDef.Id,
                                CurrentVersion = localVer,
                                NewVersion = remoteVer,
                                IsNewInstall = false
                            });
                        }
                    }
                }
                else
                {
                    // Nouveau module disponible
                    if (Version.TryParse(moduleDef.Version, out var remoteVer))
                    {
                        result.Updates.Add(new ModuleUpdateInfo
                        {
                            ModuleId = moduleDef.Id,
                            CurrentVersion = null,
                            NewVersion = remoteVer,
                            IsNewInstall = true
                        });
                    }
                }
            }

            return result;
        }
        catch (HttpRequestException httpEx)
        {
            // Erreurs HTTP spécifiques (404, 500, etc.)
            var statusCode = httpEx.StatusCode;
            string errorMessage;

            if (statusCode == System.Net.HttpStatusCode.NotFound)
            {
                errorMessage = L10n.T("update.error.notFound", "Catalogue de modules introuvable (404). Vérifiez votre connexion ou réessayez plus tard.");
            }
            else if (statusCode == System.Net.HttpStatusCode.Forbidden)
            {
                errorMessage = L10n.T("update.error.forbidden", "Accès au catalogue refusé (403).");
            }
            else if (httpEx.Message.Contains("No such host"))
            {
                errorMessage = L10n.T("update.error.noInternet", "Impossible de contacter le serveur. Vérifiez votre connexion Internet.");
            }
            else
            {
                errorMessage = L10n.TFormat("update.error.http", "Erreur réseau: {0}", httpEx.Message);
            }

            Logger.Error($"Update check failed (HTTP): {httpEx.Message}");
            return new UpdateCheckResult { Success = false, ErrorMessage = errorMessage };
        }
        catch (TaskCanceledException)
        {
            var errorMessage = L10n.T("update.error.timeout", "Délai d'attente dépassé. Le serveur ne répond pas.");
            Logger.Error("Update check failed: timeout");
            return new UpdateCheckResult { Success = false, ErrorMessage = errorMessage };
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Update check failed: {ex.Message}");
            return new UpdateCheckResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private static async Task<MarketplaceManifest?> SafeGetManifestAsync(string url)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MarketplaceManifest>(url);
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"Failed to fetch official manifest: {ex.Message}");
            return null;
        }
    }

    private static async Task<MarketplaceManifest?> GetCustomManifestAsync()
    {
        var customSource = Configuration.Configuration.Get<string>("customModuleSource", "");
        if (string.IsNullOrWhiteSpace(customSource))
            return null;

        try
        {
            // 1. Check if it's a URL
            if (Uri.TryCreate(customSource, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                 // Ensure it points to a json, or append marketplace.json?
                 // Let's assume user points to the folder URL or the JSON file itself.
                 // Ideally user gives folder URL, we append /marketplace.json
                 // But user might give full path.
                 var jsonUrl = customSource;
                 if (!customSource.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                 {
                     jsonUrl = customSource.TrimEnd('/') + "/marketplace.json";
                 }
                 return await _httpClient.GetFromJsonAsync<MarketplaceManifest>(jsonUrl);
            }

            // 2. Check if it's a local folder
            if (Directory.Exists(customSource))
            {
                var jsonPath = Path.Combine(customSource, "marketplace.json");
                if (File.Exists(jsonPath))
                {
                    var jsonContent = await File.ReadAllTextAsync(jsonPath);
                    return JsonSerializer.Deserialize<MarketplaceManifest>(jsonContent);
                }

                // No marketplace.json found - scan for OAS.*.dll files and create dynamic manifest
                return await ScanLocalModulesAsync(customSource);
            }

            // 3. Check if it is a local file
            if (File.Exists(customSource))
            {
                 var jsonContent = await File.ReadAllTextAsync(customSource);
                 return JsonSerializer.Deserialize<MarketplaceManifest>(jsonContent);
            }
        }
        catch (System.Exception ex)
        {
            Logger.Warning($"Failed to load custom manifest from '{customSource}': {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Scans a local folder for OAS.*.dll files and creates a dynamic manifest.
    /// </summary>
    private static Task<MarketplaceManifest?> ScanLocalModulesAsync(string folderPath)
    {
        return Task.Run(() =>
        {
            try
            {
                var dllFiles = Directory.GetFiles(folderPath, "OAS.*.dll", SearchOption.TopDirectoryOnly);
                if (dllFiles.Length == 0)
                    return null;

                var manifest = new MarketplaceManifest();

                foreach (var dllPath in dllFiles)
                {
                    try
                    {
                        // Read assembly metadata without loading
                        var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                        var fileName = Path.GetFileNameWithoutExtension(dllPath);

                        // Extract module ID from filename: OAS.ModuleName.dll -> modulename
                        var moduleId = fileName.StartsWith("OAS.", StringComparison.OrdinalIgnoreCase)
                            ? fileName.Substring(4).ToLowerInvariant()
                            : fileName.ToLowerInvariant();

                        // Determine a friendly name from the ID
                        var moduleName = moduleId.Length > 0
                            ? char.ToUpper(moduleId[0]) + moduleId.Substring(1)
                            : moduleId;

                        var moduleDef = new ModuleDefinition
                        {
                            Id = moduleId,
                            Name = $"{moduleName} (Local)",
                            Description = L10n.TFormat("modules.local.description", "Module local depuis {0}", folderPath),
                            Version = assemblyName.Version?.ToString(3) ?? "1.0.0",
                            Author = "Local",
                            DownloadUrl = dllPath, // Local path as "download" URL
                            IsCustomSource = true,
                            MinCoreVersion = "0.0.1"
                        };

                        manifest.Modules.Add(moduleDef);
                        Logger.Debug($"[UpdateService] Found local module: {moduleId} v{moduleDef.Version}");
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Warning($"Failed to read module metadata from '{dllPath}': {ex.Message}");
                    }
                }

                return manifest.Modules.Count > 0 ? manifest : null;
            }
            catch (System.Exception ex)
            {
                Logger.Warning($"Failed to scan local modules in '{folderPath}': {ex.Message}");
                return null;
            }
        });
    }

    /// <summary>
    /// Telecharge et lance l'installateur pour le Core.
    /// </summary>
    public static async Task InstallCoreUpdateAsync(string downloadUrl)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "OpenAsphalte_Setup.exe");

            // Telecharger
            using (var stream = await _httpClient.GetStreamAsync(downloadUrl))
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            // Lancer l'installateur
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                Arguments = "/SILENT /CLOSEAPPLICATIONS", // Arguments Inno Setup
                UseShellExecute = true
            });

            // Fermer AutoCAD si possible (ou laisser l'installateur le demander)
            // Application.Quit(); // Attention, peut etre brutal
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Core update failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Installe ou met a jour un module.
    /// </summary>
    public static async Task InstallModuleAsync(ModuleDefinition moduleDef)
    {
        try
        {
            var modulesDir = ModuleDiscovery.ModulesPath;
            if (string.IsNullOrEmpty(modulesDir) || !Directory.Exists(modulesDir))
            {
                // Fallback si le dossier n'est pas encore initialise correctement
                var asmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                modulesDir = Path.Combine(asmPath!, "Modules");
                Directory.CreateDirectory(modulesDir);
            }

            // Nom du fichier cible
            var fileName = $"OAS.{char.ToUpper(moduleDef.Id[0]) + moduleDef.Id.Substring(1)}.dll";
            // Note: Idealement le nom de la DLL devrait etre dans le JSON

            // Si l'URL se termine par .dll, on l'utilise comme nom de fichier?
            if (Helpers.TryGetFileNameFromUrl(moduleDef.DownloadUrl, out var urlFileName))
            {
                fileName = urlFileName;
            }

            var targetPath = Path.Combine(modulesDir, fileName);

            // Handle Custom Source Installation
            if (moduleDef.IsCustomSource)
            {
                var customSource = Configuration.Configuration.Get<string>("customModuleSource", "");
                if (!string.IsNullOrEmpty(customSource))
                {
                    // Check if Local Folder
                    bool isLocal = Directory.Exists(customSource) || File.Exists(customSource);

                    if (isLocal)
                    {
                        string sourceFilePath;
                        string sourceDir = Directory.Exists(customSource) ? customSource : Path.GetDirectoryName(customSource)!;

                        // If DownloadUrl is absolute path
                        if (Path.IsPathRooted(moduleDef.DownloadUrl))
                        {
                            sourceFilePath = moduleDef.DownloadUrl;
                        }
                        else
                        {
                            // Relative to custom source directory
                            sourceFilePath = Path.Combine(sourceDir, moduleDef.DownloadUrl);
                        }

                        if (File.Exists(sourceFilePath))
                        {
                            File.Copy(sourceFilePath, targetPath, true);
                            Logger.Success(L10n.TFormat("update.moduleInstalled", moduleDef.Name));
                            return;
                        }
                        else
                        {
                            throw new FileNotFoundException($"Module file not found at: {sourceFilePath}");
                        }
                    }
                    else if (Uri.TryCreate(customSource, UriKind.Absolute, out var uriResult))
                    {
                        // Custom Source is URL
                        // If DownloadUrl is relative, combine with custom source base URL
                        if (!Uri.TryCreate(moduleDef.DownloadUrl, UriKind.Absolute, out _))
                        {
                             // Assume customSource is "http://example.com/marketplace.json" or "http://example.com/"
                             // We need base URL.
                             var baseUri = new Uri(customSource.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                                 ? customSource
                                 : customSource.TrimEnd('/') + "/dummy");

                             var downloadUri = new Uri(baseUri, moduleDef.DownloadUrl);
                             moduleDef.DownloadUrl = downloadUri.ToString();
                        }
                        // Continue to standard HTTP download below
                    }
                }
            }

            // Telecharger
            using (var stream = await _httpClient.GetStreamAsync(moduleDef.DownloadUrl))
            using (var fileStream = new FileStream(targetPath, FileMode.Create))
            {
                await stream.CopyToAsync(fileStream);
            }

            Logger.Success(L10n.TFormat("update.moduleInstalled", moduleDef.Name));
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Module install failed: {ex.Message}");
            throw;
        }
    }
}

// --- Modeles de donnees ---

public class MarketplaceManifest
{
    [JsonPropertyName("core")]
    public CoreDefinition Core { get; set; } = new();

    [JsonPropertyName("modules")]
    public List<ModuleDefinition> Modules { get; set; } = new();
}

public class CoreDefinition
{
    [JsonPropertyName("latest")]
    public string Latest { get; set; } = "0.0.0";

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; set; } = "";

    [JsonPropertyName("minAutoCADVersion")]
    public string MinAutoCADVersion { get; set; } = "2025";

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = "";
}

public class ModuleDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("minCoreVersion")]
    public string MinCoreVersion { get; set; } = "";

    [JsonPropertyName("maxCoreVersion")]
    public string? MaxCoreVersion { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    public bool IsCustomSource { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    [JsonPropertyName("versions")]
    public List<ModuleVersionInfo> PreviousVersions { get; set; } = new();
}

public class ModuleVersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("minCoreVersion")]
    public string MinCoreVersion { get; set; } = "";

    [JsonPropertyName("maxCoreVersion")]
    public string? MaxCoreVersion { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; set; } = "";

    [JsonPropertyName("releaseDate")]
    public string ReleaseDate { get; set; } = "";
}

public class UpdateCheckResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public MarketplaceManifest? Manifest { get; set; }

    public bool CoreUpdateAvailable { get; set; }
    public Version? LatestCoreVersion { get; set; }

    public List<ModuleUpdateInfo> Updates { get; set; } = new();
}

public class ModuleUpdateInfo
{
    public required string ModuleId { get; set; }
    public Version? CurrentVersion { get; set; }
    public required Version NewVersion { get; set; }
    public bool IsNewInstall { get; set; }
}

/// <summary>
/// Résultat de la vérification des mises à jour au démarrage.
/// </summary>
public class StartupUpdateResult
{
    /// <summary>Version actuelle installée</summary>
    public Version? CurrentVersion { get; set; }

    /// <summary>Dernière version disponible</summary>
    public Version? LatestVersion { get; set; }

    /// <summary>True si une mise à jour est disponible et compatible</summary>
    public bool UpdateAvailable { get; set; }

    /// <summary>True si la version actuelle est à jour</summary>
    public bool IsUpToDate { get; set; }

    /// <summary>True si la mise à jour nécessite une version AutoCAD plus récente</summary>
    public bool IncompatibleAutoCAD { get; set; }

    /// <summary>Version AutoCAD minimale requise pour la mise à jour</summary>
    public string RequiredAutoCADVersion { get; set; } = "";

    /// <summary>URL de la page de release GitHub</summary>
    public string ReleaseUrl { get; set; } = "";

    /// <summary>URL de téléchargement de l'installateur</summary>
    public string DownloadUrl { get; set; } = "";

    /// <summary>Notes de version</summary>
    public string ReleaseNotes { get; set; } = "";
}

/// <summary>
/// Modèle pour la réponse de l'API GitHub releases.
/// </summary>
public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset>? Assets { get; set; }
}

/// <summary>
/// Modèle pour un asset de release GitHub.
/// </summary>
public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

internal static class Helpers
{
    public static bool TryGetFileNameFromUrl(string url, out string fileName)
    {
        fileName = "";
        try
        {
            var uri = new Uri(url);
            fileName = Path.GetFileName(uri.LocalPath);
            return !string.IsNullOrEmpty(fileName);
        }
        catch
        {
            return false;
        }
    }
}
