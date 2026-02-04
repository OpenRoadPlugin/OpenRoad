# Guide Développeur Open Asphalte

Ce guide complet vous accompagne dans la création de modules pour étendre Open Asphalte.

---

## ?? Table des matières

1. [Philosophie](#-philosophie)
2. [Prérequis](#-prérequis)
3. [Architecture du Core](#-architecture-du-core)
4. [Créer un module](#-créer-un-module)
5. [Créer des commandes](#-créer-des-commandes)
6. [Système de traduction](#-système-de-traduction)
7. [Services disponibles](#-services-disponibles)
8. [Gestion des dépendances](#-gestion-des-dépendances)
9. [Conventions de code](#-conventions-de-code)
10. [Compilation et déploiement](#-compilation-et-déploiement)
11. [FAQ](#-faq)

---

## ?? Philosophie

### Principe fondamental

> **Le cœur d'Open Asphalte ne doit JAMAIS être modifié pour ajouter un module.**

Les modules sont des DLL séparées, découvertes automatiquement au démarrage. Cette architecture garantit :

- **Isolation** — Un bug dans un module n'affecte pas le Core
- **Flexibilité** — Chaque utilisateur installe uniquement ce dont il a besoin
- **Évolutivité** — Ajouter des fonctionnalités sans toucher au code existant
- **Maintenance** — Mettre à jour un module indépendamment des autres

### Comment ça fonctionne

```
┌─────────────────────────────────────────────────────────────────┐
│                          AutoCAD                                │
├─────────────────────────────────────────────────────────────────┤
│                      OAS.Core.dll                                 │
│      ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│      │  Discovery  │  │     UI      │  │  Services   │          │
│      │   Module    │  │   Builder   │  │  partagés   │          │
│      └─────────────┘  └─────────────┘  └─────────────┘          │
│             │                │                │                 │
│             ▼                ▼                ▼                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Modules/ (DLL)                         │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │  │
│  │  │  Voirie     │  │  Dessin     │  │   Topo      │   ...  │  │
│  │  └─────────────┘  └─────────────┘  └─────────────┘        │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Flux de démarrage

1. AutoCAD charge `OAS.Core.dll` via `NETLOAD`
2. `Plugin.Initialize()` est appelé
3. `ModuleDiscovery` scanne le dossier `Modules/`
4. Pour chaque `OAS.*.dll` trouvée :
   - Recherche des classes implémentant `IModule`
   - Instanciation et validation des dépendances
   - Chargement des traductions du module
   - Appel de `Initialize()`
5. `MenuBuilder` et `RibbonBuilder` génèrent l'interface dynamiquement
6. Les commandes sont prêtes à être utilisées

**Si aucun module n'est présent**, seules les commandes système sont disponibles (`OAS_HELP`, `OAS_VERSION`, etc.).

---

## ??? Prérequis

### Environnement de développement

- **Visual Studio 2022** ou **VS Code** avec extension C#
- **.NET 8.0 SDK**
- **AutoCAD 2025** installé (pour les DLL de référence)

### Fichiers nécessaires

- `OAS.Core.dll` compilé (dans le dossier `bin/`)

### DLL AutoCAD à référencer

```
C:\Program Files\Autodesk\AutoCAD 2025\
??? accoremgd.dll
??? acdbmgd.dll
??? acmgd.dll
??? AcWindows.dll
??? AdWindows.dll
```

---

## ??? Architecture du Core

### Structure des dossiers

```
OpenAsphalte/
??? src/
?   ??? OpenAsphalte.Core/                    # ??? CŒUR DU PLUGIN ???
?       ??? OpenAsphalte.Core.csproj          # Projet principal
?       ??? Plugin.cs                     # Point d'entrée IExtensionApplication
?       ?
?       ??? Abstractions/                 # Interfaces publiques pour modules
?       ?   ??? IModule.cs                # Interface que tout module implémente
?       ?   ??? ModuleBase.cs             # Classe de base abstraite
?       ?   ??? CommandBase.cs            # Classe de base pour commandes
?       ?   ??? CommandInfoAttribute.cs   # Métadonnées UI des commandes
?       ?
?       ??? Discovery/                    # Découverte automatique
?       ?   ??? ModuleDiscovery.cs        # Scan DLL, réflexion, chargement
?       ?
?       ??? Configuration/                # Paramètres utilisateur
?       ?   ??? Configuration.cs          # Lecture/écriture JSON
?       ?
?       ??? Localization/                 # Traductions FR/EN/ES
?       ?   ??? Localization.cs           # Système de traduction
?       ?
?       ??? Logging/                      # Logs console AutoCAD
?       ?   ??? Logger.cs                 
?       ?
?       ??? Services/                     # Services partagés pour modules
?       ?   ??? GeometryService.cs        # Calculs géométriques
?       ?   ??? LayerService.cs           # Gestion des calques
?       ?
?       ??? UI/                           # Construction UI dynamique
?       ?   ??? MenuBuilder.cs            # Menu contextuel auto-généré
?       ?   ??? RibbonBuilder.cs          # Ruban auto-généré
?       ?
?       ??? Commands/                     # Commandes système
?           ??? SystemCommands.cs         # OAS_HELP, OAS_VERSION, etc.
?           ??? SettingsWindow.xaml
?           ??? SettingsWindow.xaml.cs
?
??? templates/                            # Templates pour créer des modules
?   ??? OAS.Module.Template.csproj
?   ??? ModuleTemplate.cs
?   ??? CommandTemplate.cs
?
??? bin/
    ??? OAS.Core.dll                          # DLL principale
    ??? Modules/                          # Dossier des modules externes
        ??? (vos DLL de modules ici)
```

### Commandes système (toujours disponibles)

| Commande | Description |
|----------|-------------|
| `OAS_HELP` | Liste des commandes disponibles |
| `OAS_VERSION` | Version et modules chargés |
| `OAS_SETTINGS` | Paramètres utilisateur |
| `OAS_RELOAD` | Recharge la configuration |
| `OAS_UPDATE` | Vérifie les mises à jour |

---

## 🧩 Créer un module

### 1. Structure du projet

Créez un nouveau projet dans un dossier séparé :

```
modules/
└── OAS.Voirie/                     # Votre projet de module
    ├── OAS.Voirie.csproj           # Fichier projet
    ├── VoirieModule.cs             # Classe principale du module
    └── Commands/
        └── ParkingCommand.cs       # Vos commandes
```

### 2. Fichier .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    
    <!-- IMPORTANT: Le nom DOIT commencer par "OAS." -->
    <AssemblyName>OAS.Voirie</AssemblyName>
    <RootNamespace>OpenAsphalte.Modules.Voirie</RootNamespace>
    
    <!-- Output dans le dossier Modules -->
    <OutputPath>..\..\bin\Modules\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <!-- Référence au Core (ne pas copier) -->
  <ItemGroup>
    <Reference Include="OAS.Core">
      <HintPath>..\..\bin\OAS.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>


  <!-- Références AutoCAD (ne pas copier) -->
  <ItemGroup>
    <Reference Include="accoremgd">
      <HintPath>C:\Program Files\Autodesk\AutoCAD 2025\accoremgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="acdbmgd">
      <HintPath>C:\Program Files\Autodesk\AutoCAD 2025\acdbmgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="acmgd">
      <HintPath>C:\Program Files\Autodesk\AutoCAD 2025\acmgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AcWindows">
      <HintPath>C:\Program Files\Autodesk\AutoCAD 2025\AcWindows.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AdWindows">
      <HintPath>C:\Program Files\Autodesk\AutoCAD 2025\AdWindows.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

> ⚠️ **Important** : `Private="false"` évite de copier les DLL référencées dans la sortie.

### 3. Classe Module

```csharp
using OpenAsphalte.Abstractions;

namespace OpenAsphalte.Modules.Voirie;

/// <summary>
/// Module de création de voirie pour Open Asphalte.
/// </summary>
public class VoirieModule : ModuleBase
{
    #region Identification (obligatoire)
    
    /// <summary>
    /// Identifiant unique du module (minuscules, sans espaces)
    /// </summary>
    public override string Id => "voirie";
    
    /// <summary>
    /// Nom affiché dans l'interface
    /// </summary>
    public override string Name => "Voirie";
    
    /// <summary>
    /// Description du module
    /// </summary>
    public override string Description => "Outils de création de voirie et stationnement";
    
    #endregion
    
    #region Métadonnées (optionnel)
    
    public override string Version => "1.0.0";
    public override string Author => "Mon Nom";
    public override int Order => 10;  // Position dans les menus (1-899)
    public override string? NameKey => "voirie.name";  // Clé de traduction
    
    #endregion
    
    #region Dépendances (optionnel)
    
    // Si ce module nécessite d'autres modules
    public override IReadOnlyList<string> Dependencies => Array.Empty<string>();
    
    // Version minimale du Core requise
    public override string MinCoreVersion => "1.0.0";
    
    #endregion
    
    #region Cycle de vie
    
    public override void Initialize()
    {
        base.Initialize();
        // Initialisation personnalisée (ressources, etc.)
    }
    
    public override void Shutdown()
    {
        // Nettoyage des ressources
        base.Shutdown();
    }
    
    #endregion
    
    #region Commandes
    
    /// <summary>
    /// Retourne tous les types contenant des commandes
    /// </summary>
    public override IEnumerable<Type> GetCommandTypes()
    {
        return new[]
        {
            typeof(Commands.ParkingCommand),
            // Ajoutez vos autres commandes ici
        };
    }
    
    #endregion
    
    #region Traductions
    
    /// <summary>
    /// Retourne les traductions spécifiques au module
    /// </summary>
    public override IDictionary<string, IDictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, IDictionary<string, string>>
        {
            ["fr"] = new Dictionary<string, string>
            {
                ["voirie.name"] = "Voirie",
                ["voirie.parking.title"] = "Stationnement",
                ["voirie.parking.desc"] = "Crée des places de parking",
                ["voirie.parking.success"] = "Places de parking créées",
            },
            ["en"] = new Dictionary<string, string>
            {
                ["voirie.name"] = "Road",
                ["voirie.parking.title"] = "Parking",
                ["voirie.parking.desc"] = "Creates parking spaces",
                ["voirie.parking.success"] = "Parking spaces created",
            },
            ["es"] = new Dictionary<string, string>
            {
                ["voirie.name"] = "Vialidad",
                ["voirie.parking.title"] = "Estacionamiento",
                ["voirie.parking.desc"] = "Crea plazas de aparcamiento",
                ["voirie.parking.success"] = "Plazas de aparcamiento creadas",
            }
        };
    }
    
    #endregion
}
```

---

## 💻 Créer des commandes

### Structure d'une commande

```csharp
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;

namespace OpenAsphalte.Modules.Voirie.Commands;

/// <summary>
/// Commande de création de places de parking
/// </summary>
public class ParkingCommand : CommandBase
{
    /// <summary>
    /// Méthode exécutée quand l'utilisateur tape OAS_PARKING
    /// </summary>
    [CommandMethod("OAS_PARKING")]
    [CommandInfo("Stationnement",
        Description = "Crée des places de parking le long d'une polyligne",
        DisplayNameKey = "voirie.parking.title",
        DescriptionKey = "voirie.parking.desc",
        Order = 10,
        RibbonSize = CommandSize.Large,
        Group = "Création")]
    public void Execute()
    {
        // ExecuteSafe gère automatiquement les erreurs et annulations
        ExecuteSafe(() =>
        {
            // 1. Vérifier qu'un document est ouvert
            if (!IsDocumentValid) return;
            
            // 2. Sélection utilisateur
            var peo = new PromptEntityOptions($"\n{T("select.polyline")}: ");
            peo.SetRejectMessage("\nSélectionnez une polyligne.");
            peo.AddAllowedClass(typeof(Polyline), true);
            
            var per = Editor!.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;
            
            // 3. Modifier le dessin dans une transaction
            ExecuteInTransaction(tr =>
            {
                // Récupérer la polyligne sélectionnée
                var polyline = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                
                // Obtenir le BlockTableRecord courant (espace modèle ou papier)
                var btr = (BlockTableRecord)tr.GetObject(
                    Database!.CurrentSpaceId, 
                    OpenMode.ForWrite
                );
                
                // Créer les entités...
                // Exemple: créer une ligne
                var line = new Line(
                    polyline.StartPoint,
                    polyline.EndPoint
                );
                
                btr.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
            });
            
            // 4. Message de succès
            Logger.Success(T("voirie.parking.success"));
        });
    }
}
```

### L'attribut CommandInfo

| Propriété | Type | Description | Défaut |
|-----------|------|-------------|--------|
| `DisplayName` | string | Nom affiché (paramètre du constructeur) | **Requis** |
| `Description` | string | Description / infobulle | `""` |
| `DisplayNameKey` | string? | Clé de traduction du nom | `null` |
| `DescriptionKey` | string? | Clé de traduction description | `null` |
| `IconPath` | string? | Chemin de l'icône | `null` |
| `Order` | int | Ordre d'affichage | `100` |
| `RibbonSize` | CommandSize | `Standard` ou `Large` | `Standard` |
| `Group` | string? | Groupe dans le ruban | `null` |
| `ShowInMenu` | bool | Afficher dans le menu | `true` |
| `ShowInRibbon` | bool | Afficher dans le ruban | `true` |

### Classe CommandBase

La classe `CommandBase` fournit des fonctionnalités essentielles :

```csharp
public abstract class CommandBase
{
    // === Accès aux objets AutoCAD ===
    protected Document? Document { get; }      // Document actif
    protected Database? Database { get; }      // Database du document
    protected Editor? Editor { get; }          // Éditeur pour interactions utilisateur
    protected bool IsDocumentValid { get; }    // True si document accessible

    // === Exécution sécurisée ===
    // Gère les erreurs et les annulations utilisateur (Escape)
    protected void ExecuteSafe(Action action, string? successKey = null, string? errorKey = null);

    // === Transactions ===
    // Exécute du code dans une transaction AutoCAD (commit automatique)
    protected void ExecuteInTransaction(Action<Transaction> action);
    protected T? ExecuteInTransaction<T>(Func<Transaction, T> action);

    // === Utilitaires ===
    protected void WriteMessage(string message);              // Écrire dans la console
    protected static string T(string key, string? defaultValue = null);  // Traduction
    protected static string T(string key, params object[] args);         // Traduction formatée
}
```

---
🌍 Système de traduction

### Vue d'ensemble

Open Asphalte supporte **3 langues** : Français (fr), Anglais (en), Espagnol (es).
Le **Core est entièrement localisé** (UI, commandes système, logs).

Le système de traduction est **dynamique** : lorsque l'utilisateur change de langue dans les paramètres :
1. Le Core met automatiquement à jour le menu et le ruban
2. Les modules reçoivent l'événement `OnLanguageChanged` s'ils y sont abonnés
3. Toutes les nouvelles traductions sont immédiatement appliquées

### Structure des clés

Convention : `{module_id}.{section}.{element}`

```
voirie.name                 ➜ Nom du module
voirie.parking.title        ➜ Titre de la commande
voirie.parking.desc         ➜ Description de la commande
voirie.parking.success      ➜ Message de succès
voirie.parking.error        ➜ Message d'erreur
```

### Utiliser les traductions

```csharp
// Dans une commande (via CommandBase)
var text = T("voirie.parking.title");
var formatted = T("voirie.msg.count", count);  // "voirie.msg.count" = "{0} éléments créés"

// Directement via Localization
using L10n = OpenAsphalte.Localization.Localization;
var text = L10n.T("key", "valeur par défaut");
```

### Définir les traductions du module (OBLIGATOIRE)

⚠️ **Important** : Fournissez les traductions pour les **3 langues** (fr, en, es).
Si une traduction manque, le français sera utilisé par défaut.
Les modules doivent fournir FR/EN/ES pour être complets.

```csharp
public override IDictionary<string, IDictionary<string, string>> GetTranslations()
{
    return new Dictionary<string, IDictionary<string, string>>
    {
        // === FRANÇAIS (obligatoire) ===
        ["fr"] = new Dictionary<string, string>
        {
            ["monmodule.name"] = "Mon Module",
            ["monmodule.cmd.title"] = "Ma Commande",
            ["monmodule.msg.success"] = "Opération réussie !",
            ["monmodule.msg.count"] = "{0} élément(s) créé(s)",
        },
        
        // === ENGLISH (obligatoire) ===
        ["en"] = new Dictionary<string, string>
        {
            ["monmodule.name"] = "My Module",
            ["monmodule.cmd.title"] = "My Command",
            ["monmodule.msg.success"] = "Operation successful!",
            ["monmodule.msg.count"] = "{0} element(s) created",
        },
        
        // === ESPAÑOL (obligatoire) ===
        ["es"] = new Dictionary<string, string>
        {
            ["monmodule.name"] = "Mi Módulo",
            ["monmodule.cmd.title"] = "Mi Comando",
            ["monmodule.msg.success"] = "¡Operación exitosa!",
            ["monmodule.msg.count"] = "{0} elemento(s) creado(s)",
        }
    };
}
```

### Réagir aux changements de langue (optionnel)

Si votre module a des fenêtres WPF ou une interface personnalisée qui doit se mettre à jour quand l'utilisateur change de langue :

```csharp
public override void Initialize()
{
    base.Initialize();
    
    // S'abonner aux changements de langue
    Localization.Localization.OnLanguageChanged += OnLanguageChanged;
}

public override void Shutdown()
{
    // Se désabonner
    Localization.Localization.OnLanguageChanged -= OnLanguageChanged;
    base.Shutdown();
}

private void OnLanguageChanged(string oldLanguage, string newLanguage)
{
    // Mettre à jour les fenêtres ouvertes, palettes, etc.
    // Le menu et le ruban du Core sont mis à jour automatiquement
}
```

### Propriétés et méthodes utiles de Localization

```csharp
using L10n = OpenAsphalte.Localization.Localization;

// Langue courante
string currentLang = L10n.CurrentLanguage;  // "fr", "en" ou "es"

// Langues supportées
IReadOnlyList<string> langs = L10n.SupportedLanguages;  // ["fr", "en", "es"]

// Noms des langues pour l'UI
IReadOnlyDictionary<string, string> names = L10n.LanguageNames;
// { "fr": "Français", "en": "English", "es": "Español" }

// Changer la langue (déclenche OnLanguageChanged)
bool changed = L10n.SetLanguage("en");  // true si changement effectué

// Vérifier si une langue est supporté
// V?rifier si une langue est support?e
bool isSupported = L10n.IsLanguageSupported("de");  // false
```
```

---

## 🛠️ Services disponibles

### Logger

```csharp
using OpenAsphalte.Logging;

Logger.Debug("Message debug");   // Seulement si DevMode=true
Logger.Info("Information");      // [INFO] Information
Logger.Success("Réussite");      // [OK] Réussite
Logger.Warning("Attention");     // [WARN] Attention
Logger.Error("Erreur");          // [ERROR] Erreur
Logger.Raw("Message brut");      // Sans préfixe
```

### GeometryService

```csharp
using OpenAsphalte.Services;
using Autodesk.AutoCAD.Geometry;

// Distance et angles
double dist = GeometryService.Distance(p1, p2);
double angle = GeometryService.AngleBetween(p1, p2);           // radians
double angleDeg = GeometryService.AngleBetweenDegrees(p1, p2); // degrés

// Points
Point3d offset = GeometryService.OffsetPoint(point, angle, distance);
Point3d perp = GeometryService.PerpendicularOffset(point, angle, dist, leftSide: true);
Point3d mid = GeometryService.MidPoint(p1, p2);
Point3d lerp = GeometryService.Lerp(p1, p2, 0.5);  // Interpolation

// Polylignes
var points = GeometryService.GetPolylinePoints(polyline);
double length = GeometryService.GetPolylineLength(polyline);
Point3d pt = GeometryService.GetPointAtDistance(polyline, 10.0);
double tangent = GeometryService.GetTangentAngle(polyline, 10.0);

// Tests géométriques
bool isLeft = GeometryService.IsPointOnLeftSide(start, end, testPoint);
bool isInside = GeometryService.IsPointInPolygon(point, polygonPoints);

// Calculs
double area = GeometryService.CalculatePolygonArea(points);
Point3d centroid = GeometryService.CalculateCentroid(points);
```

### LayerService

```csharp
using OpenAsphalte.Services;
using AcColor = Autodesk.AutoCAD.Colors.Color;

ExecuteInTransaction(tr =>
{
    // Créer ou obtenir un calque
    var layerId = LayerService.EnsureLayer(
        Database!, tr, 
        "OAS_PARKING",                                    // Nom du calque
        AcColor.FromColorIndex(AcColorMethod.ByAci, 3),  // Couleur (vert)
        "DASHED"                                         // Type de ligne (optionnel)
    );
    
    // Vérifier si un calque existe
    bool exists = LayerService.LayerExists(Database!, tr, "MonCalque");
    
    // Obtenir tous les calques visibles
    var layers = LayerService.GetVisibleLayers(Database!, tr);
    
    // Définir le calque courant
    LayerService.SetCurrentLayer(Database!, tr, "OAS_PARKING");
});
```

### Configuration

```csharp
using OpenAsphalte.Configuration;

// Paramètres globaux
string lang = Configuration.Language;   // "fr", "en", "es"
bool dev = Configuration.DevMode;       // Mode développeur

// Valeurs personnalisées
var myValue = Configuration.Get<int>("monmodule.setting", defaultValue: 10);
Configuration.Set("monmodule.setting", 20);
Configuration.Save();

// Écouter les changements
Configuration.OnSettingChanged += (key, value) =>
{
    if (key == "language") { /* Réagir au changement */ }
};
```

### ModuleDiscovery

```csharp
using OpenAsphalte.Discovery;

// Liste des modules chargés
var modules = ModuleDiscovery.Modules;

// Liste de toutes les commandes
var commands = ModuleDiscovery.AllCommands;

// Obtenir un module par ID
var voirie = ModuleDiscovery.GetModule("voirie");

// Obtenir un module typé
var voirieModule = ModuleDiscovery.GetModule<VoirieModule>();

// Commandes groupées par module
var grouped = ModuleDiscovery.GetCommandsByModule();
```

---

## 📦 Gestion des dépendances

### Déclarer une dépendance

```csharp
public class SignalisationModule : ModuleBase
{
    public override string Id => "signalisation";
    public override string Name => "Signalisation";
    
    // Ce module nécessite le module "voirie"
    public override IReadOnlyList<string> Dependencies => new[] { "voirie" };
    
    // Version minimale du Core requise
    public override string MinCoreVersion => "1.0.0";
}
```

### Comportement

- Si une dépendance est **manquante**, le module n'est **pas chargé**
- Un message d'avertissement est affiché dans la console AutoCAD
- Les commandes du module ne sont pas disponibles
- L'interface (menu/ruban) n'affiche pas le module

### Dépendances tierces (bibliothèques externes)

> ⚠️ **Règle importante** : Les modules officiels doivent éviter au maximum les bibliothèques tierces.

#### Pour les modules officiels (distribués avec Open Asphalte)

| Priorité | Action |
|----------|--------|
| **1. Utiliser le Core** | Privilégier les services existants (`GeometryService`, `LayerService`...) |
| **2. Utiliser .NET natif** | `System.Text.Json` plutôt que `Newtonsoft.Json`, etc. |
| **3. Proposer l'ajout au Core** | Si une lib est utile à plusieurs modules → ouvrir une issue GitHub |
| **4. Dernier recours** | Utiliser [ILMerge](https://github.com/dotnet/ILMerge) pour fusionner la lib dans votre DLL |

#### Pourquoi cette règle ?

Les modules sont chargés dans le même contexte .NET. Si deux modules utilisent des versions différentes d'une même bibliothèque :
- Le premier module chargé "gagne"
- Le second peut crasher si l'API diffère entre versions

#### Bibliothèques déjà disponibles

Le Core utilise uniquement des bibliothèques .NET natives :
- `System.Text.Json` — Sérialisation JSON
- `System.IO` — Fichiers et chemins
- API AutoCAD — Tout le nécessaire pour le dessin

---

## 🏢 Modules privés (entreprises)

Les entreprises peuvent développer leurs propres modules pour un usage interne.

### Ce que nous garantissons

| Modules officiels | Modules privés |
|-------------------|----------------|
| ✅ Compatibilité testée | ❌ Aucune garantie |
| ✅ Review IA obligatoire | ❌ Hors contrôle |
| ✅ Support communautaire | ❌ Pas de support |
| ✅ Dépendances alignées | ❌ À vos risques |

### Ce que nous NE garantissons PAS

```
╔══════════════════════════════════════════════════════════════════╗
║  Les modules privés développés par des entreprises tierces       ║
║  sont HORS DU PÉRIMÈTRE DE SUPPORT d'Open Asphalte.                  ║
║                                                                  ║
║  Si votre module privé casse votre installation :                ║
║  → C'est votre responsabilité.                                   ║
╚══════════════════════════════════════════════════════════════════╝
```

### Bonnes pratiques pour modules privés

1. **Suivez les conventions** documentées dans ce guide
2. **Évitez les bibliothèques tierces** ou utilisez les mêmes versions que le Core
3. **Testez avec les modules officiels** que vous utilisez
4. **Documentez vos dépendances** pour votre équipe interne
5. **Isolez vos libs** avec ILMerge si nécessaire

### En cas de conflit

Si votre module privé cause des problèmes :
1. Désactivez-le (supprimez la DLL du dossier `Modules/`)
2. Identifiez la bibliothèque en conflit
3. Alignez la version avec celle utilisée par les modules officiels
4. Ou utilisez ILMerge pour embarquer votre version isolément

---

## 📏 Conventions de code

### Namespaces

```csharp
// Modules
namespace OpenAsphalte.Modules.Voirie;            // Module principal
namespace OpenAsphalte.Modules.Voirie.Commands;   // Commandes du module
namespace OpenAsphalte.Modules.Voirie.Services;   // Services spécifiques (optionnel)
```

### Nommage

| Élément | Convention | Exemple |
|---------|------------|---------|
| Assembly | `OAS.{Module}` | `OAS.Voirie` |
| Namespace | `OpenAsphalte.Modules.{Module}` | `OpenAsphalte.Modules.Voirie` |
| Commande AutoCAD | `OAS_{MODULE}_{ACTION}` | `OAS_VOIRIE_PARKING` |
| Clé traduction | `{module}.{section}.{key}` | `voirie.parking.title` |
| Calque | `OAS_{MODULE}_{ELEMENT}` | `OAS_VOIRIE_PARKING` |

### Alias recommandés

```csharp
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcColor = Autodesk.AutoCAD.Colors.Color;
using L10n = OpenAsphalte.Localization.Localization;
```

### Bonnes pratiques

```csharp
// ✅ BIEN - Utiliser ExecuteSafe pour la gestion d'erreurs
public void Execute()
{
    ExecuteSafe(() =>
    {
        // Code qui peut échouer
    });
}

// ❌ MAL - Try/catch manuel partout
public void Execute()
{
    try { /* ... */ }
    catch (Exception ex) { /* ... */ }
}

// ✅ BIEN - Utiliser ExecuteInTransaction
ExecuteInTransaction(tr =>
{
    // Modifications du dessin
});

// ❌ MAL - Transaction manuelle risquée
var tr = Database.TransactionManager.StartTransaction();
// Si exception, la transaction n'est pas disposée !
tr.Commit();
```

---

## 🚀 Compilation et déploiement

### Compiler un module

```bash
cd modules/OAS.Voirie
dotnet build -c Release
```

La DLL sera générée dans `bin/Modules/OAS.Voirie.dll`.

### Tester dans AutoCAD

1. Compiler le module
2. Dans AutoCAD, taper `NETLOAD`
3. Sélectionner `OAS.Core.dll`
4. Taper `OAS_HELP` pour voir les commandes disponibles
5. Taper `OAS_VERSION` pour voir les modules chargés

### Distribution

Pour distribuer votre module :

1. **Fichier à fournir** : Uniquement la DLL du module
2. **Instructions utilisateur** : "Placer dans le dossier `Modules/` d'Open Asphalte"
3. **Dépendances** : Documenter les modules requis

---

## ❓ FAQ

### Ma commande n'apparaît pas dans le menu

Vérifiez que :

1. ✅ Le fichier `.dll` est nommé `OAS.*.dll`
2. ✅ La DLL est dans le dossier `Modules/`
3. ✅ La classe du module hérite de `ModuleBase`
4. ✅ `GetCommandTypes()` retourne le type de votre commande
5. ✅ L'attribut `[CommandMethod("OAS_...")]` est présent sur la méthode
6. ✅ `ShowInMenu = true` dans `[CommandInfo]` (c'est le défaut)

### Comment débugger mon module ?

1. Activez le mode développeur : `OAS_SETTINGS` 👉 Mode développeur : Oui
2. Utilisez `Logger.Debug()` pour afficher des messages (visibles seulement en mode dev)
3. Utilisez `OAS_VERSION` pour voir les modules chargés

### Comment accéder à un autre module ?

```csharp
using OpenAsphalte.Discovery;

// Par ID
var voirieModule = ModuleDiscovery.GetModule("voirie");

// Par type (si vous avez la référence au projet)
var voirie = ModuleDiscovery.GetModule<VoirieModule>();
```

### Puis-je modifier le Core pour mon besoin ?

**Non.** C'est le principe fondamental d'Open Asphalte. Si vous avez besoin d'une fonctionnalité :

1. Vérifiez si elle existe déjà dans les services partagés
2. Proposez une amélioration via une Pull Request
3. Créez un module qui encapsule votre besoin

---

## 📚 Ressources

- **Templates** : Copiez les fichiers dans `templates/` pour démarrer rapidement
- **README principal** : [README.md](README.md)
- **Contribution** : [CONTRIBUTING.md](CONTRIBUTING.md)

---

## ⚠️ Avertissement pour les développeurs de modules

### Licence Apache 2.0 et responsabilité

En développant un module pour Open Asphalte, vous reconnaissancez que :

1. **Open Asphalte est fourni "tel quel"** sans garantie d'aucune sorte
2. **Les auteurs d'Open Asphalte ne sont pas responsables** des modules tiers
3. **Vous êtes seul responsable** de votre module et de ses conséquences
4. **Le nom "Open Asphalte" est une marque réservée** — Vous ne pouvez pas l'utiliser pour nommer vos modules

### Responsabilité des modules tiers

- Les modules que vous créez sont **sous votre propre responsabilité**
- Open Asphalte **ne garantit pas** le bon fonctionnement de vos modules
- Les utilisateurs de vos modules doivent être informés des risques

### Recommandations

Si vous distribuez votre module publiquement :

1. **Incluez votre propre fichier LICENSE** avec les limitations de responsabilité
2. **Documentez les risques** potentiels liés à votre module
3. **Testez abondamment** avant toute distribution
4. **Ne faites aucune promesse** de résultat ou de performance
5. **N'utilisez pas le nom "Open Asphalte"** dans le nom de votre module (utilisez votre propre nom/marque)
