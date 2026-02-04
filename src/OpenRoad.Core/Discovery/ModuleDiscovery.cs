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
using System.Reflection;
using OpenRoad;
using OpenRoad.Abstractions;
using OpenRoad.Logging;
using L10n = OpenRoad.Localization.Localization;
using Autodesk.AutoCAD.Runtime;

namespace OpenRoad.Discovery;

/// <summary>
/// Informations sur une commande decouverte
/// </summary>
public class CommandDescriptor
{
    /// <summary>
    /// Nom de la commande AutoCAD (ex: "OR_PARKING")
    /// </summary>
    public required string CommandName { get; init; }
    
    /// <summary>
    /// Nom affiche dans les menus
    /// </summary>
    public required string DisplayName { get; init; }
    
    /// <summary>
    /// Description de la commande
    /// </summary>
    public string Description { get; init; } = "";
    
    /// <summary>
    /// Cle de traduction du nom (si multilingue)
    /// </summary>
    public string? DisplayNameKey { get; init; }
    
    /// <summary>
    /// Cle de traduction de la description
    /// </summary>
    public string? DescriptionKey { get; init; }
    
    /// <summary>
    /// Chemin de l'icone
    /// </summary>
    public string? IconPath { get; init; }
    
    /// <summary>
    /// Ordre d'affichage
    /// </summary>
    public int Order { get; init; } = 100;
    
    /// <summary>
    /// Taille dans le ruban
    /// </summary>
    public CommandSize RibbonSize { get; init; } = CommandSize.Standard;
    
    /// <summary>
    /// Groupe dans le ruban
    /// </summary>
    public string? Group { get; init; }
    
    /// <summary>
    /// Afficher dans le menu
    /// </summary>
    public bool ShowInMenu { get; init; } = true;
    
    /// <summary>
    /// Afficher dans le ruban
    /// </summary>
    public bool ShowInRibbon { get; init; } = true;
    
    /// <summary>
    /// Type contenant la commande
    /// </summary>
    public required Type DeclaringType { get; init; }
    
    /// <summary>
    /// Methode de la commande
    /// </summary>
    public required MethodInfo Method { get; init; }
    
    /// <summary>
    /// Module proprietaire
    /// </summary>
    public required IModule Module { get; init; }

    /// <summary>
    /// Catégorie menu niveau 1
    /// </summary>
    public string? MenuCategory { get; init; }

    /// <summary>
    /// Clé de traduction pour la catégorie niveau 1
    /// </summary>
    public string? MenuCategoryKey { get; init; }

    /// <summary>
    /// Sous-catégorie menu niveau 2
    /// </summary>
    public string? MenuSubCategory { get; init; }

    /// <summary>
    /// Clé de traduction pour la sous-catégorie niveau 2
    /// </summary>
    public string? MenuSubCategoryKey { get; init; }
    
    /// <summary>
    /// Obtient le nom affiché traduit
    /// </summary>
    public string GetLocalizedDisplayName()
    {
        if (!string.IsNullOrEmpty(DisplayNameKey))
        {
            return L10n.T(DisplayNameKey, DisplayName);
        }
        return DisplayName;
    }
    
    /// <summary>
    /// Obtient la description traduite
    /// </summary>
    public string GetLocalizedDescription()
    {
        if (!string.IsNullOrEmpty(DescriptionKey))
        {
            return L10n.T(DescriptionKey, Description);
        }
        return Description;
    }
}

/// <summary>
/// Informations sur un module charge
/// </summary>
public class ModuleDescriptor
{
    /// <summary>
    /// Instance du module
    /// </summary>
    public required IModule Module { get; init; }
    
    /// <summary>
    /// Assembly contenant le module
    /// </summary>
    public required Assembly Assembly { get; init; }
    
    /// <summary>
    /// Chemin du fichier DLL
    /// </summary>
    public required string FilePath { get; init; }
    
    /// <summary>
    /// Commandes decouvertes dans ce module
    /// </summary>
    public List<CommandDescriptor> Commands { get; } = new();
    
    /// <summary>
    /// Indique si les dependances sont satisfaites
    /// </summary>
    public bool DependenciesSatisfied { get; set; } = true;
    
    /// <summary>
    /// Liste des dependances manquantes
    /// </summary>
    public List<string> MissingDependencies { get; } = new();
}

/// <summary>
/// Service de decouverte automatique des modules Open Road.
/// Scanne le dossier Modules/ et charge dynamiquement les DLL.
/// </summary>
public static class ModuleDiscovery
{
    private static readonly List<ModuleDescriptor> _loadedModules = new();
    private static readonly Dictionary<string, IModule> _moduleById = new();
    private static readonly List<CommandDescriptor> _allCommands = new();
    private static bool _initialized = false;
    
    /// <summary>
    /// Chemin du dossier Modules
    /// </summary>
    public static string ModulesPath { get; private set; } = "";
    
    /// <summary>
    /// Liste de tous les modules charges
    /// </summary>
    public static IReadOnlyList<ModuleDescriptor> LoadedModules => _loadedModules.AsReadOnly();
    
    /// <summary>
    /// Liste de tous les modules (instances)
    /// </summary>
    public static IReadOnlyList<IModule> Modules => _loadedModules
        .Where(m => m.DependenciesSatisfied)
        .Select(m => m.Module)
        .OrderBy(m => m.Order)
        .ToList()
        .AsReadOnly();
    
    /// <summary>
    /// Liste de toutes les commandes decouvertes
    /// </summary>
    public static IReadOnlyList<CommandDescriptor> AllCommands => _allCommands.AsReadOnly();
    
    /// <summary>
    /// Chemins additionnels pour la découverte de modules (configurables)
    /// </summary>
    public static IReadOnlyList<string> AdditionalModulesPaths => _additionalPaths.AsReadOnly();
    
    private static readonly List<string> _additionalPaths = new();
    
    /// <summary>
    /// Ajoute un chemin additionnel pour la découverte de modules.
    /// Doit être appelé avant DiscoverAndLoad().
    /// </summary>
    /// <param name="path">Chemin vers un dossier contenant des modules OpenRoad.*.dll</param>
    public static void AddModulesPath(string path)
    {
        if (_initialized)
        {
            Logger.Warning(L10n.T("module.pathAddedTooLate", "Impossible d'ajouter un chemin après l'initialisation"));
            return;
        }
        
        if (!string.IsNullOrWhiteSpace(path) && !_additionalPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _additionalPaths.Add(path);
            Logger.Debug(L10n.TFormat("module.pathAdded", path));
        }
    }
    
    /// <summary>
    /// Decouvre et charge tous les modules
    /// </summary>
    /// <param name="basePath">Chemin de base (dossier contenant OpenRoad.Core.dll)</param>
    public static void DiscoverAndLoad(string basePath)
    {
        if (_initialized) return;
        
        // Chemin principal : sous-dossier Modules
        ModulesPath = Path.Combine(basePath, "Modules");
        
        // Ajouter chemin depuis configuration si défini
        var configPath = Configuration.Configuration.Get("modulesPath", "");
        if (!string.IsNullOrWhiteSpace(configPath) && Directory.Exists(configPath))
        {
            AddModulesPath(configPath);
        }
        
        Logger.Debug(L10n.TFormat("module.searchPath", ModulesPath));
        
        // Creer le dossier Modules s'il n'existe pas
        if (!Directory.Exists(ModulesPath))
        {
            Directory.CreateDirectory(ModulesPath);
            Logger.Info(L10n.T("module.folderCreated"));
        }
        
        // Collecter toutes les DLL de tous les chemins
        var allDllFiles = new List<string>();
        
        // Dossier principal
        allDllFiles.AddRange(Directory.GetFiles(ModulesPath, "OpenRoad.*.dll", SearchOption.TopDirectoryOnly));
        
        // Chemins additionnels
        foreach (var additionalPath in _additionalPaths)
        {
            if (Directory.Exists(additionalPath))
            {
                var additionalDlls = Directory.GetFiles(additionalPath, "OpenRoad.*.dll", SearchOption.TopDirectoryOnly);
                allDllFiles.AddRange(additionalDlls);
                Logger.Debug(L10n.TFormat("module.dllFoundInPath", additionalDlls.Length, additionalPath));
            }
            else
            {
                Logger.Warning(L10n.TFormat("module.pathNotFound", additionalPath));
            }
        }
        
        // Dédupliquer par nom de fichier (le premier trouvé gagne)
        var uniqueDlls = allDllFiles
            .GroupBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        
        Logger.Debug(L10n.TFormat("module.dllFound", uniqueDlls.Count));
        
        foreach (var dllPath in uniqueDlls)
        {
            LoadModuleFromDll(dllPath);
        }
        
        // Resoudre les dependances
        ResolveDependencies();
        
        // Trier par ordre
        _loadedModules.Sort((a, b) => a.Module.Order.CompareTo(b.Module.Order));
        
        // Reconstruire la liste des commandes (seulement modules avec dependances satisfaites)
        RebuildCommandList();
        
        _initialized = true;
        Logger.Info(L10n.TFormat("module.summary", _loadedModules.Count(m => m.DependenciesSatisfied), _allCommands.Count));
    }
    
    /// <summary>
    /// Vérifie si une DLL est signée numériquement.
    /// </summary>
    /// <param name="dllPath">Chemin vers la DLL</param>
    /// <returns>True si signée, False sinon</returns>
    private static bool IsModuleSigned(string dllPath)
    {
        try
        {
            var assembly = AssemblyName.GetAssemblyName(dllPath);
            var publicKey = assembly.GetPublicKey();
            return publicKey != null && publicKey.Length > 0;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Vérifie et avertit si un module n'est pas signé.
    /// Dans le futur, cette méthode pourra bloquer les modules non signés.
    /// </summary>
    private static bool ValidateModuleSecurity(string dllPath)
    {
        var fileName = Path.GetFileName(dllPath);
        
        // Vérifier la signature
        if (!IsModuleSigned(dllPath))
        {
            var allowUnsigned = Configuration.Configuration.Get("allowUnsignedModules", true);
            
            if (!allowUnsigned)
            {
                Logger.Error(L10n.TFormat("module.unsignedBlocked", fileName));
                return false;
            }
            
            // Avertissement pour les modules non signés
            Logger.Warning(L10n.TFormat("module.unsigned", fileName));
        }
        else
        {
            Logger.Debug(L10n.TFormat("module.signed", fileName));
        }
        
        return true;
    }
    
    /// <summary>
    /// Charge un module depuis une DLL
    /// </summary>
    private static void LoadModuleFromDll(string dllPath)
    {
        try
        {
            // SÉCURITÉ: Valider que le fichier est bien dans un dossier autorisé
            var fullPath = Path.GetFullPath(dllPath);
            var modulesFullPath = Path.GetFullPath(ModulesPath);
            
            // Vérifier le dossier principal
            bool isInAuthorizedPath = fullPath.StartsWith(modulesFullPath, StringComparison.OrdinalIgnoreCase);
            
            // Vérifier les chemins additionnels
            if (!isInAuthorizedPath)
            {
                foreach (var additionalPath in _additionalPaths)
                {
                    var addFullPath = Path.GetFullPath(additionalPath);
                    if (fullPath.StartsWith(addFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        isInAuthorizedPath = true;
                        break;
                    }
                }
            }
            
            if (!isInAuthorizedPath)
            {
                Logger.Warning(L10n.TFormat("module.loadOutside", dllPath));
                return;
            }
            
            if (!File.Exists(fullPath))
            {
                Logger.Warning(L10n.TFormat("module.dllMissing", dllPath));
                return;
            }
            
            // Validation de sécurité (signature)
            if (!ValidateModuleSecurity(fullPath))
            {
                return;
            }
            
            Logger.Debug(L10n.TFormat("module.loading", Path.GetFileName(dllPath)));
            
            // Charger l'assembly
            var assembly = Assembly.LoadFrom(fullPath);
            
            // Trouver toutes les classes qui implementent IModule
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t) 
                           && !t.IsAbstract 
                           && !t.IsInterface)
                .ToList();
            
            if (moduleTypes.Count == 0)
            {
                Logger.Warning(L10n.TFormat("module.noneFound", Path.GetFileName(dllPath)));
                return;
            }
            
            foreach (var moduleType in moduleTypes)
            {
                try
                {
                    // Creer l'instance du module
                    var module = (IModule)Activator.CreateInstance(moduleType)!;
                    
                    // Verifier si le module n'est pas deja charge
                    if (_moduleById.ContainsKey(module.Id))
                    {
                        Logger.Warning(L10n.TFormat("module.duplicate", module.Id));
                        continue;
                    }
                    
                    // Creer le descripteur
                    var descriptor = new ModuleDescriptor
                    {
                        Module = module,
                        Assembly = assembly,
                        FilePath = dllPath
                    };
                    
                    // Decouvrir les commandes
                    DiscoverCommands(descriptor);
                    
                    // Enregistrer
                    _loadedModules.Add(descriptor);
                    _moduleById[module.Id] = module;
                    
                    Logger.Debug(L10n.TFormat("module.loaded", module.Name, module.Version, descriptor.Commands.Count));
                }
                catch (System.Exception ex)
                {
                    Logger.Error(L10n.TFormat("module.instanceError", moduleType.Name, ex.Message));
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error(L10n.TFormat("module.dllError", Path.GetFileName(dllPath), ex.Message));
        }
    }
    
    /// <summary>
    /// Decouvre les commandes d'un module
    /// </summary>
    private static void DiscoverCommands(ModuleDescriptor descriptor)
    {
        foreach (var cmdType in descriptor.Module.GetCommandTypes())
        {
            var methods = cmdType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<CommandMethodAttribute>() != null);
            
            foreach (var method in methods)
            {
                var cmdAttr = method.GetCustomAttribute<CommandMethodAttribute>()!;
                var infoAttr = method.GetCustomAttribute<CommandInfoAttribute>();
                
                var command = new CommandDescriptor
                {
                    CommandName = cmdAttr.GlobalName,
                    DisplayName = infoAttr?.DisplayName ?? cmdAttr.GlobalName.Replace("OR_", ""),
                    Description = infoAttr?.Description ?? "",
                    DisplayNameKey = infoAttr?.DisplayNameKey,
                    DescriptionKey = infoAttr?.DescriptionKey,
                    IconPath = infoAttr?.IconPath,
                    Order = infoAttr?.Order ?? 100,
                    RibbonSize = infoAttr?.RibbonSize ?? CommandSize.Standard,
                    Group = infoAttr?.Group,
                    MenuCategory = infoAttr?.MenuCategory,
                    MenuCategoryKey = infoAttr?.MenuCategoryKey,
                    MenuSubCategory = infoAttr?.MenuSubCategory,
                    MenuSubCategoryKey = infoAttr?.MenuSubCategoryKey,
                    ShowInMenu = infoAttr?.ShowInMenu ?? true,
                    ShowInRibbon = infoAttr?.ShowInRibbon ?? true,
                    DeclaringType = cmdType,
                    Method = method,
                    Module = descriptor.Module
                };
                
                descriptor.Commands.Add(command);
            }
        }
        
        // Trier les commandes par ordre
        descriptor.Commands.Sort((a, b) => a.Order.CompareTo(b.Order));
    }
    
    /// <summary>
    /// Resout les dependances entre modules
    /// </summary>
    private static void ResolveDependencies()
    {
        foreach (var descriptor in _loadedModules)
        {
            if (!IsCoreVersionCompatible(descriptor))
            {
                descriptor.DependenciesSatisfied = false;
                descriptor.MissingDependencies.Add($"core>={descriptor.Module.MinCoreVersion}");
                continue;
            }

            foreach (var depId in descriptor.Module.Dependencies)
            {
                if (!_moduleById.ContainsKey(depId))
                {
                    descriptor.DependenciesSatisfied = false;
                    descriptor.MissingDependencies.Add(depId);
                    Logger.Warning(L10n.TFormat("module.depMissing", descriptor.Module.Name, depId));
                }
            }
        }
    }

    /// <summary>
    /// Vérifie la compatibilité de version minimale du Core pour un module
    /// </summary>
    private static bool IsCoreVersionCompatible(ModuleDescriptor descriptor)
    {
        var minVersion = ParseVersion(descriptor.Module.MinCoreVersion);
        var coreVersion = ParseVersion(Plugin.Version);

        if (minVersion == null || coreVersion == null)
        {
            Logger.Warning(L10n.TFormat("module.coreVersionUnknown", descriptor.Module.Name, descriptor.Module.MinCoreVersion, Plugin.Version));
            return true;
        }

        if (coreVersion < minVersion)
        {
            Logger.Warning(L10n.TFormat("module.coreVersionIncompatible", descriptor.Module.Name, descriptor.Module.MinCoreVersion, Plugin.Version));
            return false;
        }

        return true;
    }

    /// <summary>
    /// Tente de parser une version semver simple (ignore les suffixes)
    /// </summary>
    private static Version? ParseVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;

        var clean = version.Split('-', '+')[0];
        return Version.TryParse(clean, out var parsed) ? parsed : null;
    }
    
    /// <summary>
    /// Reconstruit la liste globale des commandes
    /// </summary>
    private static void RebuildCommandList()
    {
        _allCommands.Clear();
        
        foreach (var descriptor in _loadedModules.Where(m => m.DependenciesSatisfied))
        {
            _allCommands.AddRange(descriptor.Commands);
        }
        
        // Trier par module puis par ordre
        _allCommands.Sort((a, b) =>
        {
            int moduleCompare = a.Module.Order.CompareTo(b.Module.Order);
            return moduleCompare != 0 ? moduleCompare : a.Order.CompareTo(b.Order);
        });
    }
    
    /// <summary>
    /// Initialise tous les modules charges
    /// </summary>
    public static void InitializeAll()
    {
        foreach (var descriptor in _loadedModules.Where(m => m.DependenciesSatisfied))
        {
            try
            {
                // Charger les traductions
                var translations = descriptor.Module.GetTranslations();
                foreach (var (lang, dict) in translations)
                {
                    L10n.RegisterTranslations(lang, dict);
                }
                
                // Initialiser
                descriptor.Module.Initialize();
                Logger.Debug(L10n.TFormat("module.initialized", descriptor.Module.Name));
            }
            catch (System.Exception ex)
            {
                Logger.Error(L10n.TFormat("module.initError", descriptor.Module.Name, ex.Message));
            }
        }
    }
    
    /// <summary>
    /// Ferme tous les modules
    /// </summary>
    public static void ShutdownAll()
    {
        // Parcourir en ordre inverse pour respecter les dependances
        foreach (var descriptor in _loadedModules.AsEnumerable().Reverse())
        {
            try
            {
                // Appeler explicitement Shutdown() avant Dispose()
                if (descriptor.Module.IsInitialized)
                {
                    descriptor.Module.Shutdown();
                }
                
                // Ensuite Dispose pour libérer les ressources
                descriptor.Module.Dispose();
                Logger.Debug(L10n.TFormat("module.shutdown", descriptor.Module.Name));
            }
            catch (System.Exception ex)
            {
                // Ne pas laisser un module empecher la fermeture des autres
                Logger.Error(L10n.TFormat("module.shutdownError", descriptor.Module.Name, ex.Message));
            }
        }
        
        _loadedModules.Clear();
        _moduleById.Clear();
        _allCommands.Clear();
        _initialized = false;
    }
    
    /// <summary>
    /// Recupere un module par son ID
    /// </summary>
    public static IModule? GetModule(string id)
    {
        return _moduleById.TryGetValue(id, out var module) ? module : null;
    }
    
    /// <summary>
    /// Recupere un module type
    /// </summary>
    public static T? GetModule<T>() where T : class, IModule
    {
        return _loadedModules
            .Where(m => m.DependenciesSatisfied)
            .Select(m => m.Module)
            .OfType<T>()
            .FirstOrDefault();
    }
    
    /// <summary>
    /// Obtient les commandes groupees par module
    /// </summary>
    public static IEnumerable<IGrouping<IModule, CommandDescriptor>> GetCommandsByModule()
    {
        return _allCommands.GroupBy(c => c.Module);
    }
}
