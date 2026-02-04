using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenRoad.Configuration;
using OpenRoad.Discovery;
using OpenRoad.Logging;
using L10n = OpenRoad.Localization.Localization;

namespace OpenRoad.Services;

/// <summary>
/// Service de gestion des mises a jour et du telechargement de modules.
/// </summary>
public static class UpdateService
{
    private static readonly HttpClient _httpClient = new();
    private const string MarketplaceUrl = "https://raw.githubusercontent.com/openroadplugin/openroad/main/docs/marketplace.json";

    /// <summary>
    /// Verifie les mises a jour disponibles.
    /// </summary>
    public static async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        try
        {
            var url = Configuration.Configuration.Get("updateUrl", MarketplaceUrl);
            var manifest = await _httpClient.GetFromJsonAsync<MarketplaceManifest>(url);
            
            if (manifest == null)
            {
                return new UpdateCheckResult { Success = false, ErrorMessage = "Manifest is empty" };
            }

            var result = new UpdateCheckResult { Success = true, Manifest = manifest };

            // Verifier maj Core
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            if (Version.TryParse(manifest.Core.Latest, out var latestVersion))
            {
                if (latestVersion > currentVersion)
                {
                    result.CoreUpdateAvailable = true;
                    result.LatestCoreVersion = latestVersion;
                }
            }

            // Verifier modules
            foreach (var moduleDef in manifest.Modules)
            {
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

    /// <summary>
    /// Telecharge et lance l'installateur pour le Core.
    /// </summary>
    public static async Task InstallCoreUpdateAsync(string downloadUrl)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "OpenRoad_Setup.exe");
            
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
            var fileName = $"OpenRoad.{char.ToUpper(moduleDef.Id[0]) + moduleDef.Id.Substring(1)}.dll"; 
            // Note: Idealement le nom de la DLL devrait etre dans le JSON
            
            // Si l'URL se termine par .dll, on l'utilise comme nom de fichier?
            if (Helpers.TryGetFileNameFromUrl(moduleDef.DownloadUrl, out var urlFileName))
            {
                fileName = urlFileName;
            }

            var targetPath = Path.Combine(modulesDir, fileName);

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
    
    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";
    
    [JsonPropertyName("author")]
    public string Author { get; set; } = "";
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
