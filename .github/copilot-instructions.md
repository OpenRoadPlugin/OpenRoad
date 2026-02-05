# Open Asphalte – Context IA Complet

> **Document optimisé pour agents IA** | Version 2026.02.05 | .NET 8.0 / AutoCAD 2025+

---

## 🤖 CONTEXTE IA – RÔLE ET EXPERTISE REQUISE

**Agis comme un Expert Technique Polyvalent sur le projet Open Asphalte.**

Tu es capable de jongler entre deux casquettes selon la demande :
1. **Architecte Core** : Garant de la stabilité, de l'API et de l'infrastructure.
2. **Développeur Module** : Créateur de fonctionnalités métier respectant les standards.

⚠️ **Important** : Analyse la requête de l'utilisateur. Si elle concerne une nouvelle fonctionnalité métier, bascule en mode "Développeur Module". Si elle concerne l'infrastructure ou un bug système, bascule en mode "Architecte Core".

### Ton profil d'expertise
|------------------|--------|--------------------------------------------------------------------|
| Domaine          | Niveau | Détails                                                            |
|------------------|--------|--------------------------------------------------------------------|
| **C#**           | Expert | C# 12, .NET 8.0, async/await, LINQ, pattern matching               |
| **AutoCAD API**  | Expert | ObjectARX .NET, transactions, entités, Database, Editor            |
| **Architecture** | Expert | Plugins modulaires, découverte dynamique, injection de dépendances |
| **WPF**          | Avancé | Fenêtres modales, XAML, binding                                    |
| **Géométrie**    | Avancé | Point3d, Vector3d, polylignes, transformations                     |
|------------------|--------|--------------------------------------------------------------------|

### Ton comportement

1. **Tu identifies le contexte** — Core vs Module avant de répondre.
2. **Tu respectes l'architecture** — Le Core est stable, les modules sont additifs.
3. **Tu écris du code production-ready** — Gestion d'erreurs, traductions, conventions.
4. **Tu utilises les services existants** — `GeometryService`, `LayerService`, `Logger`...
5. **Tu fournis toujours les 3 langues** — FR, EN, ES pour les traductions.

### Patterns obligatoires (Mode Module)

```csharp
// ✅ TOUJOURS utiliser ExecuteSafe pour les commandes
public void Execute()
{
    ExecuteSafe(() =>
    {
        // Code ici
    });
}

// ✅ TOUJOURS utiliser ExecuteInTransaction pour modifier la DB
ExecuteInTransaction(tr =>
{
    // Modifications AutoCAD ici
});

// ✅ TOUJOURS utiliser T() pour les messages
Logger.Success(T("monmodule.success"));
WriteMessage($"\n{T("select.point")}: ");
```

### Ce que tu NE FAIS JAMAIS

- ❌ Modifier les fichiers dans `src/OpenAsphalte.Core/` pour ajouter des fonctionnalités métier
- ❌ Ajouter des commandes dans `SystemCommands.cs`
- ❌ Créer des commandes sans le préfixe `OAS_`
- ❌ Oublier les traductions (FR, EN, ES obligatoires)
- ❌ Manipuler la Database sans transaction
- ❌ Ignorer `ExecuteSafe()` dans une commande

---

## 🎯 IDENTITÉ DU PROJET

**Open Asphalte** est un plugin **C# modulaire** pour AutoCAD, destiné aux professionnels de la voirie et de l'aménagement urbain.

### Caractéristiques techniques
|--------------|----------------------------------------------|
| Propriété    | Valeur                                       |
|--------------|----------------------------------------------|
| Framework    | .NET 8.0-windows                             |
| Langage      | C# 12 (latest)                               |
| Cible        | AutoCAD 2025+                                |
| Architecture | Plugin modulaire avec découverte automatique |
| Interface    | Menu contextuel + Ruban dynamiques           |
| Multilingue  | FR, EN, ES                                   |
| Licence      | Apache 2.0                                   |
|--------------|----------------------------------------------|

---

## 🔴 RÈGLE ABSOLUE

```
╔══════════════════════════════════════════════════════════════════╗
║  LE CŒUR (OpenAsphalte.Core) NE DOIT JAMAIS ÊTRE MODIFIÉ POUR    ║
║  AJOUTER UN MODULE OU UNE FONCTIONNALITÉ MÉTIER.                 ║
║                                                                  ║
║  Les modules sont des DLL séparées, découvertes automatiquement. ║
╚══════════════════════════════════════════════════════════════════╝
```

**Pourquoi ?** Isolation des bugs, flexibilité utilisateur, évolutivité sans régression.

---

## 📁 ARCHITECTURE FICHIERS

```
OpenAsphalte/
├── src/
│   └── OpenAsphalte.Core/                # ⛔ CŒUR - NE PAS MODIFIER POUR MODULES
│       ├── Plugin.cs                     # Point d'entrée IExtensionApplication
│       ├── Abstractions/                 # Interfaces publiques pour modules
│       │   ├── IModule.cs                # Interface module (à implémenter)
│       │   ├── ModuleBase.cs             # Classe de base module (à hériter)
│       │   ├── CommandBase.cs            # Classe de base commandes (à hériter)
│       │   └── CommandInfoAttribute.cs   # Métadonnées UI des commandes
│       ├── Discovery/
│       │   └── ModuleDiscovery.cs        # Scan DLL, réflexion, chargement auto
│       ├── Configuration/
│       │   └── Configuration.cs          # Paramètres JSON dans AppData
│       ├── Localization/
│       │   └── Localization.cs           # Traductions FR/EN/ES + événements
│       ├── Logging/
│       │   └── Logger.cs                 # Logs console AutoCAD
│       ├── Services/                     # Services réutilisables par modules
│       │   ├── GeometryService.cs        # Calculs géométriques
│       │   └── LayerService.cs           # Gestion des calques
│       ├── UI/
│       │   ├── MenuBuilder.cs            # Menu contextuel auto-généré
│       │   └── RibbonBuilder.cs          # Ruban auto-généré
│       └── Commands/
│           ├── SystemCommands.cs         # OAS_HELP, OAS_VERSION, OAS_SETTINGS...
│           └── SettingsWindow.xaml(.cs)  # Fenêtre paramètres WPF
│
├── templates/                            # ✅ TEMPLATES POUR NOUVEAUX MODULES
│   ├── OAS.Module.Template.csproj
│   ├── ModuleTemplate.cs
│   └── CommandTemplate.cs
│
├── bin/
│   ├── OAS.Core.dll                      # DLL principale compilée
│   └── Modules/                          # 📦 DOSSIER MODULES EXTERNES
│       └── (DLL OAS.*.dll)               # Découvertes automatiquement
│
└── version.json                          # Version centralisée du projet
```

---

## 🔄 FLUX DE DÉMARRAGE

```
1. AutoCAD → NETLOAD OAS.Core.dll
2. Plugin.Initialize() appelé
3. Configuration.Load() → charge config JSON
4. Localization.Initialize() → charge traductions
5. ModuleDiscovery.DiscoverAndLoad() :
   └─ Scan Modules/*.dll (pattern OAS.*.dll)
   └─ Pour chaque DLL :
      ├─ Recherche classes IModule
      ├─ Instanciation + validation dépendances
      ├─ Découverte commandes [CommandMethod]
      └─ Enregistrement traductions module
6. ModuleDiscovery.InitializeAll() → appelle Initialize() sur chaque module
7. MenuBuilder.CreateMenu() + RibbonBuilder.CreateRibbon()
8. Plugin prêt
```

---

## 📋 COMMANDES SYSTÈME (toujours disponibles)

| Commande       | Description                          | Fichier           |
|----------------|--------------------------------------|-------------------|
| `OAS_HELP`     | Liste des commandes disponibles      | SystemCommands.cs |
| `OAS_VERSION`  | Version et modules chargés           | SystemCommands.cs |
| `OAS_SETTINGS` | Fenêtre paramètres (langue, devmode) | SystemCommands.cs |
| `OAS_RELOAD`   | Recharge configuration + UI          | SystemCommands.cs |
| `OAS_UPDATE`   | Ouvre page releases GitHub           | SystemCommands.cs |
|----------------|--------------------------------------|-------------------|

---

## 📦 MODULES OFFICIELS (Référence pour non-duplication)

| Module | ID | Namespace | Description |
|--------|----|-----------|-------------|
| **Géoréférencement** | `georeferencement` | `OAS.Georeferencement` | Systèmes de coordonnées, grilles |
| **Street View** | `streetview` | `OAS.StreetView` | Google Street View dynamique |
| **Cotation** | `cota2lign` | `OAS.Cota2Lign` | Cotation automatique voirie |
| **Dynamic Snap** | `dynamicsnap` | `OAS.DynamicSnap` | Moteur d'accrochage intelligent |
| **Organiseur** | `prezorganizer` | `OAS.PrezOrganizer` | Gestion des présentations |

---

## 🛠️ CRÉER UN MODULE (Workflow complet)

### Étape 1 : Structure projet

```
modules/
└── OpenAsphalte.MonModule/
    ├── OpenAsphalte.MonModule.csproj
    ├── MonModuleModule.cs          # Hérite ModuleBase
    └── Commands/
        └── MaCommande.cs           # Hérite CommandBase
```

### Étape 2 : Fichier .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    
    <!-- ⚠️ OBLIGATOIRE: Doit commencer par "OAS." -->
    <AssemblyName>OAS.MonModule</AssemblyName>
    <RootNamespace>OpenAsphalte.Modules.MonModule</RootNamespace>
    
    <!-- Output dans Modules/ -->
    <OutputPath>..\..\bin\Modules\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="OAS.Core">
      <HintPath>..\..\bin\OAS.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <!-- Références AutoCAD (Private=false car déjà chargées) -->
    <Reference Include="accoremgd"><HintPath>$(AutoCADPath)\accoremgd.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="acdbmgd"><HintPath>$(AutoCADPath)\acdbmgd.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="acmgd"><HintPath>$(AutoCADPath)\acmgd.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
```

### Étape 3 : Classe Module

```csharp
using OpenAsphalte.Abstractions;

namespace OpenAsphalte.Modules.MonModule;

public class MonModuleModule : ModuleBase
{
    // ═══════════════════════════════════════════════════════════
    // IDENTIFICATION (obligatoire)
    // ═══════════════════════════════════════════════════════════
    public override string Id => "monmodule";           // minuscules, sans espaces
    public override string Name => "Mon Module";        // affiché dans UI
    public override string Description => "Description du module";
    
    // ═══════════════════════════════════════════════════════════
    // OPTIONNEL (avec valeurs par défaut)
    // ═══════════════════════════════════════════════════════════
    public override string Version => "1.0.0";          // semver
    public override string Author => "Votre Nom";
    public override int Order => 50;                    // 1-899 user, 900+ système
    public override string? NameKey => "monmodule.name"; // clé traduction
    public override string? IconPath => null;           // pack://...
    public override IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public override string MinCoreVersion => "1.0.0";
    
    // ═══════════════════════════════════════════════════════════
    // COMMANDES DU MODULE
    // ═══════════════════════════════════════════════════════════
    public override IEnumerable<Type> GetCommandTypes()
    {
        yield return typeof(Commands.MaCommande);
        // yield return typeof(Commands.AutreCommande);
    }
    
    // ═══════════════════════════════════════════════════════════
    // TRADUCTIONS (FR/EN/ES)
    // ═══════════════════════════════════════════════════════════
    public override IDictionary<string, IDictionary<string, string>> GetTranslations()
    {
        return new Dictionary<string, IDictionary<string, string>>
        {
            ["fr"] = new Dictionary<string, string>
            {
                ["monmodule.name"] = "Mon Module",
                ["monmodule.cmd.title"] = "Ma Commande",
                ["monmodule.cmd.desc"] = "Description de ma commande",
                ["monmodule.success"] = "Opération réussie",
            },
            ["en"] = new Dictionary<string, string>
            {
                ["monmodule.name"] = "My Module",
                ["monmodule.cmd.title"] = "My Command",
                ["monmodule.cmd.desc"] = "Description of my command",
                ["monmodule.success"] = "Operation successful",
            },
            ["es"] = new Dictionary<string, string>
            {
                ["monmodule.name"] = "Mi Módulo",
                ["monmodule.cmd.title"] = "Mi Comando",
                ["monmodule.cmd.desc"] = "Descripción de mi comando",
                ["monmodule.success"] = "Operación exitosa",
            },
        };
    }
    
    // ═══════════════════════════════════════════════════════════
    // CYCLE DE VIE (optionnel)
    // ═══════════════════════════════════════════════════════════
    public override void Initialize()
    {
        base.Initialize();
        // Code d'initialisation du module
    }
    
    public override void Shutdown()
    {
        base.Shutdown();
        // Libération des ressources
    }
}
```

### Étape 4 : Classe Commande

```csharp
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OpenAsphalte.Abstractions;
using OpenAsphalte.Logging;
using OpenAsphalte.Services;

namespace OpenAsphalte.Modules.MonModule.Commands;

public class MaCommande : CommandBase
{
    [CommandMethod("OAS_MONMODULE_ACTION")]  // ⚠️ Préfixe OAS_ obligatoire
    [CommandInfo("Ma Commande",
        Description = "Description de la commande",
        DisplayNameKey = "monmodule.cmd.title",
        DescriptionKey = "monmodule.cmd.desc",
        Order = 10,
        RibbonSize = CommandSize.Large,     // Large ou Standard
        Group = "Général",                  // Groupe dans le ruban
        ShowInMenu = true,
        ShowInRibbon = true)]
    public void Execute()
    {
        // ExecuteSafe gère automatiquement:
        // - Vérification document actif
        // - Capture exceptions
        // - Annulation utilisateur (Escape)
        ExecuteSafe(() =>
        {
            // ═══════════════════════════════════════════════════
            // SÉLECTION UTILISATEUR
            // ═══════════════════════════════════════════════════
            var ppo = new PromptPointOptions($"\n{T("select.point")}: ");
            var ppr = Editor!.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK) return;
            var point = ppr.Value;
            
            // ═══════════════════════════════════════════════════
            // OPÉRATIONS AVEC TRANSACTION
            // ═══════════════════════════════════════════════════
            ExecuteInTransaction(tr =>
            {
                var btr = (BlockTableRecord)tr.GetObject(
                    Database!.CurrentSpaceId, 
                    OpenMode.ForWrite);
                
                // Exemple: créer un cercle
                using var circle = new Circle(point, Vector3d.ZAxis, 1.0);
                circle.Layer = "OAS_MONMODULE_CERCLES"; // Convention calque
                
                btr.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);
            });
            
            Logger.Success(T("monmodule.success"));
        });
    }
}
```

---

## 📚 API DES CLASSES DE BASE

### CommandBase (hériter pour commandes)

```csharp
public abstract class CommandBase
{
    // ═══════════════════════════════════════════════════════════
    // PROPRIÉTÉS AUTOCAD (lecture seule)
    // ═══════════════════════════════════════════════════════════
    protected Document? Document { get; }      // Document actif
    protected Database? Database { get; }      // Database du document
    protected Editor? Editor { get; }          // Éditeur pour interactions
    protected bool IsDocumentValid { get; }    // true si document accessible
    
    // ═══════════════════════════════════════════════════════════
    // EXÉCUTION SÉCURISÉE
    // ═══════════════════════════════════════════════════════════
    protected void ExecuteSafe(Action action, string? successKey = null, string? errorKey = null);
    
    // ═══════════════════════════════════════════════════════════
    // TRANSACTIONS
    // ═══════════════════════════════════════════════════════════
    protected void ExecuteInTransaction(Action<Transaction> action);
    protected T? ExecuteInTransaction<T>(Func<Transaction, T> action);
    
    // ═══════════════════════════════════════════════════════════
    // UTILITAIRES
    // ═══════════════════════════════════════════════════════════
    protected void WriteMessage(string message);           // Affiche dans console
    protected static string T(string key, string? defaultValue = null);  // Traduction
    protected static string TFormat(string key, params object[] args);   // Traduction formatée
}
```

### ModuleBase (hériter pour modules)

```csharp
public abstract class ModuleBase : IModule
{
    // ═══════════════════════════════════════════════════════════
    // PROPRIÉTÉS ABSTRAITES (À IMPLÉMENTER)
    // ═══════════════════════════════════════════════════════════
    public abstract string Id { get; }          // ex: "voirie"
    public abstract string Name { get; }        // ex: "Voirie"
    public abstract string Description { get; } // Description complète
    
    // ═══════════════════════════════════════════════════════════
    // PROPRIÉTÉS VIRTUELLES (AVEC DÉFAUT)
    // ═══════════════════════════════════════════════════════════
    public virtual string Version => "1.0.0";
    public virtual string Author => "Open Road Contributors";
    public virtual int Order => 100;                              // Ordre affichage
    public virtual string? IconPath => null;                      // Icône 32x32
    public virtual string? NameKey => null;                       // Clé traduction nom
    public virtual IReadOnlyList<string> Dependencies => [];      // Modules requis
    public virtual string MinCoreVersion => "1.0.0";              // Version Core min
    public bool IsInitialized { get; }                            // État init
    
    // ═══════════════════════════════════════════════════════════
    // MÉTHODES VIRTUELLES (À SURCHARGER SI BESOIN)
    // ═══════════════════════════════════════════════════════════
    public virtual void Initialize();                              // Appelé au chargement
    public virtual void Shutdown();                                // Appelé à la fermeture
    public virtual IEnumerable<Type> GetCommandTypes();            // Types de commandes
    public virtual IDictionary<string, IDictionary<string, string>> GetTranslations();
}
```

### CommandInfoAttribute (métadonnées UI)

```csharp
[CommandInfo("Nom Affiché",
    Description = "Description pour infobulle",
    DisplayNameKey = "module.cmd.title",     // Traduction nom
    DescriptionKey = "module.cmd.desc",      // Traduction description
    IconPath = "pack://application:,,,/Assembly;component/Resources/icon.png",
    Order = 10,                              // Ordre dans le groupe
    RibbonSize = CommandSize.Large,          // Large (32x32) ou Standard (16x16)
    Group = "Groupe",                        // Groupe dans le ruban
    ShowInMenu = true,                       // Visible dans menu
    ShowInRibbon = true)]                    // Visible dans ruban
```

---

## 🔧 SERVICES DISPONIBLES

### Logger

```csharp
using OpenAsphalte.Logging;

Logger.Debug("Message debug");    // Seulement si DevMode=true
Logger.Info("Information");       // [INFO] ...
Logger.Success("Réussi");         // [OK] ...
Logger.Warning("Attention");      // [WARN] ...
Logger.Error("Erreur");           // [ERROR] ...
Logger.Raw("Brut");               // Sans préfixe
```

### GeometryService

```csharp
using OpenAsphalte.Services;
using Autodesk.AutoCAD.Geometry;

// ═══════════════════════════════════════════════════════════════════
// CONSTANTES
// ═══════════════════════════════════════════════════════════════════
GeometryService.Tolerance   // 1e-10 pour comparaisons
GeometryService.Gravity     // 9.81 m/s²
GeometryService.DegToRad    // π/180
GeometryService.RadToDeg    // 180/π

// ═══════════════════════════════════════════════════════════════════
// DISTANCE ET ANGLES
// ═══════════════════════════════════════════════════════════════════
double dist = GeometryService.Distance(p1, p2);           // Distance 3D
double dist2D = GeometryService.Distance2D(p1, p2);       // Distance 2D (ignore Z)
double dz = GeometryService.DeltaZ(p1, p2);               // Différence altitude

double angle = GeometryService.AngleBetween(p1, p2);           // radians [-π, π]
double angleDeg = GeometryService.AngleBetweenDegrees(p1, p2); // degrés
double normalized = GeometryService.NormalizeAngle(angle);     // [0, 2π]
double bearing = GeometryService.Bearing(p1, p2);              // grades [0, 400]

// ═══════════════════════════════════════════════════════════════════
// POINTS ET PROJECTIONS
// ═══════════════════════════════════════════════════════════════════
Point3d offset = GeometryService.OffsetPoint(point, angle, distance);
Point3d perp = GeometryService.PerpendicularOffset(point, angle, distance, leftSide: true);
Point3d mid = GeometryService.MidPoint(p1, p2);
Point3d lerp = GeometryService.Lerp(p1, p2, t: 0.5);
Point3d rotated = GeometryService.RotatePoint(point, center, angle);
Point3d proj = GeometryService.ProjectPointOnLine(point, lineStart, lineEnd);
Point3d projSeg = GeometryService.ProjectPointOnSegment(point, segStart, segEnd);
double distLine = GeometryService.DistancePointToLine(point, lineStart, lineEnd);

// ═══════════════════════════════════════════════════════════════════
// POLYLIGNES
// ═══════════════════════════════════════════════════════════════════
List<Point3d> pts = GeometryService.GetPolylinePoints(polyline);
double len = GeometryService.GetPolylineLength(polyline);
Point3d ptAt = GeometryService.GetPointAtDistance(polyline, distance);
double tangent = GeometryService.GetTangentAngle(polyline, distance);

// ═══════════════════════════════════════════════════════════════════
// TESTS GÉOMÉTRIQUES
// ═══════════════════════════════════════════════════════════════════
bool left = GeometryService.IsPointOnLeftSide(lineStart, lineEnd, point);
bool inside = GeometryService.IsPointInPolygon(point, polygonPoints);

// ═══════════════════════════════════════════════════════════════════
// AIRES ET PÉRIMÈTRES
// ═══════════════════════════════════════════════════════════════════
double area = GeometryService.CalculatePolygonArea(points);
double perim = GeometryService.CalculatePolygonPerimeter(points);
Point3d centroid = GeometryService.CalculateCentroid(points);
double triArea = GeometryService.CalculateTriangleArea(p1, p2, p3);
double triArea3D = GeometryService.CalculateTriangleArea3D(p1, p2, p3);

// ═══════════════════════════════════════════════════════════════════
// INTERSECTIONS
// ═══════════════════════════════════════════════════════════════════
var lineResult = GeometryService.IntersectLines(l1Start, l1End, l2Start, l2End);
if (lineResult.HasIntersection && lineResult.IsOnBothSegments) { /* ... */ }

Point3d? segIntersect = GeometryService.IntersectSegments(s1Start, s1End, s2Start, s2End);

var circleResult = GeometryService.IntersectLineCircle(lineStart, lineEnd, center, radius);
// circleResult.Count: 0, 1 (tangent), ou 2

var circlesResult = GeometryService.IntersectCircles(center1, r1, center2, r2);
// circlesResult.Count: -1 (identiques), 0, 1, ou 2

var tangents = GeometryService.TangentPointsFromExternalPoint(extPoint, center, radius);

// ═══════════════════════════════════════════════════════════════════
// CERCLES ET ARCS
// ═══════════════════════════════════════════════════════════════════
var circle = GeometryService.CircleFrom3Points(p1, p2, p3);
double arcLen = GeometryService.ArcLength(radius, angleRadians);
double chord = GeometryService.ChordLength(radius, angleRadians);
double sagita = GeometryService.Sagita(radius, angleRadians);

// ═══════════════════════════════════════════════════════════════════
// VOIRIE - TRACÉ EN PLAN
// ═══════════════════════════════════════════════════════════════════
double A = GeometryService.ClothoidParameter(radius, length);
var (x, y, tau) = GeometryService.ClothoidCoordinates(A, L);
double Lmin = GeometryService.MinClothoidLength(radius, speedKmh);
double Rmin = GeometryService.MinCurveRadius(speedKmh, deversPercent, frictionCoef);
double devers = GeometryService.RecommendedSuperelevation(radius, speedKmh);
double surlargeur = GeometryService.CurveWidening(radius, vehicleLength);
double Dv = GeometryService.StoppingDistance(speedKmh, reactionTime, frictionCoef, slopePercent);
double Dd = GeometryService.OvertakingDistance(speedKmh);

// ═══════════════════════════════════════════════════════════════════
// VOIRIE - PROFIL EN LONG
// ═══════════════════════════════════════════════════════════════════
double pente = GeometryService.SlopePercent(p1, p2);      // en %
double penteMillieme = GeometryService.SlopePerMille(p1, p2);  // en ‰
var (R, fleche, isConvexe) = GeometryService.VerticalCurveParameters(slope1, slope2, length);
double LminCrest = GeometryService.MinCrestCurveLength(slope1, slope2, stoppingDist);
double LminSag = GeometryService.MinSagCurveLength(slope1, slope2, stoppingDist);
double z = GeometryService.VerticalCurveElevation(startZ, startSlope, curveLength, position, endSlope);

// ═══════════════════════════════════════════════════════════════════
// ASSAINISSEMENT - HYDRAULIQUE
// ═══════════════════════════════════════════════════════════════════
double Q = GeometryService.ManningStricklerFlow(K, section, Rh, slope);
double V = GeometryService.ManningStricklerVelocity(K, Rh, slope);
double Rh = GeometryService.HydraulicRadius(wettedArea, wettedPerimeter);

// Sections circulaires
var (S, Pm, Rh) = GeometryService.CircularPipeHydraulics(diameter, fillRatio);
double Qps = GeometryService.FullPipeFlow(diameter, slopePercent, K);
double D = GeometryService.RequiredPipeDiameter(flowRate, slopePercent, K);
double Imin = GeometryService.SelfCleaningSlope(diameter, minVelocity);

// Autres sections
var ovoide = GeometryService.OvoidPipeHydraulics(height, fillRatio);
var rect = GeometryService.RectangularChannelHydraulics(width, height, waterDepth);
var trap = GeometryService.TrapezoidalChannelHydraulics(bottomWidth, waterDepth, sideSlope);

// Coefficients de Strickler (via StricklerCoefficients)
// BetonLisse=80, BetonCentrifuge=90, BetonOrdinary=70, PVCNeuf=100, PEHD=100...

// ═══════════════════════════════════════════════════════════════════
// CUBATURE ET TERRASSEMENT
// ═══════════════════════════════════════════════════════════════════
var (cutArea, fillArea) = GeometryService.CrossSectionAreas(profilePoints, referenceLevel);
double vol = GeometryService.VolumeByAverageEndArea(area1, area2, distance);
double volPrism = GeometryService.VolumeByPrismoidal(area1, areaMiddle, area2, distance);
var (cutVol, fillVol) = GeometryService.TotalEarthworkVolumes(sectionsList);

// Foisonnement (via BulkingFactors)
// TerreVegetale=1.25, Argile=1.30, Sable=1.10, RocheFragmentee=1.50...
double foisonne = GeometryService.ApplyBulking(volumeEnPlace, bulkingFactor);
double compacte = GeometryService.CompactedVolume(volumeFoisonne, compactionRatio);

// Tranchées
double volTranchee = GeometryService.TrenchVolume(width, depth, length);
double volTrancheeSlope = GeometryService.TrenchVolumeWithSlope(bottomWidth, depth, length, sideSlope);
double litPose = GeometryService.BeddingVolume(pipeOuterDiameter, thickness, trenchWidth, length);
double enrobage = GeometryService.SurroundVolume(pipeOuterDiameter, trenchWidth, length, coverAbovePipe);

// ═══════════════════════════════════════════════════════════════════
// SURFACES ET MNT
// ═══════════════════════════════════════════════════════════════════
double z = GeometryService.InterpolateZFromPlane(point, p1, p2, p3);
double pente = GeometryService.PlaneSlope(p1, p2, p3);      // en %
double azimut = GeometryService.PlaneAspect(p1, p2, p3);    // en grades
double vol = GeometryService.TriangularPrismVolume(p1, p2, p3, referenceZ);
```

### LayerService

```csharp
using OpenAsphalte.Services;
using AcColor = Autodesk.AutoCAD.Colors.Color;

ExecuteInTransaction(tr =>
{
    // Créer ou récupérer un calque
    ObjectId layerId = LayerService.EnsureLayer(Database, tr, "OAS_MONMODULE_LAYER",
        color: AcColor.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 1),
        linetype: "CONTINUOUS");
    
    // Vérifications
    bool exists = LayerService.LayerExists(Database, tr, "LayerName");
    List<LayerInfo> all = LayerService.GetAllLayers(Database, tr);
    List<LayerInfo> visible = LayerService.GetVisibleLayers(Database, tr);
    
    // Manipulation
    LayerService.SetCurrentLayer(Database, tr, "LayerName");
    LayerService.SetLayerOn(Database, tr, "LayerName", on: true);
    LayerService.SetLayerFrozen(Database, tr, "LayerName", frozen: false);
});
```

### Configuration

```csharp
using OpenAsphalte.Configuration;

// Propriétés raccourcis
string lang = Configuration.Language;         // "fr", "en", "es"
bool devMode = Configuration.DevMode;         // Mode debug
string updateUrl = Configuration.UpdateUrl;
bool checkUpdates = Configuration.CheckUpdatesOnStartup;

// Accès générique
T value = Configuration.Get<T>("key", defaultValue);
Configuration.Set<T>("key", value);
Configuration.Save();
Configuration.Reload();

// Chemin config: %AppData%/Open Asphalte/config.json
string folder = Configuration.ConfigurationFolder;
string file = Configuration.ConfigurationFile;

// Événement changement
Configuration.OnSettingChanged += (key, value) => { /* ... */ };
```

### Localization

```csharp
using L10n = OpenAsphalte.Localization.Localization;

// Langue courante
string lang = L10n.CurrentLanguage;                  // "fr", "en", "es"
IReadOnlyList<string> supported = L10n.SupportedLanguages;
IReadOnlyDictionary<string, string> names = L10n.LanguageNames;

// Traduction
string text = L10n.T("key");                         // Traduction simple
string text = L10n.T("key", "default");              // Avec défaut
string formatted = L10n.TFormat("key", arg1, arg2);  // Avec formatage

// Changer la langue (reconstruit automatiquement l'UI)
L10n.SetLanguage("en");

// Événement changement de langue
L10n.OnLanguageChanged += (oldLang, newLang) =>
{
    // Mettre à jour UI personnalisée
};

// Enregistrer traductions (fait automatiquement pour modules)
L10n.RegisterTranslations("fr", new Dictionary<string, string>
{
    ["key"] = "valeur"
});
```

### ModuleDiscovery

```csharp
using OpenAsphalte.Discovery;

// Modules chargés
IReadOnlyList<IModule> modules = ModuleDiscovery.Modules;
IReadOnlyList<ModuleDescriptor> loaded = ModuleDiscovery.LoadedModules;
IReadOnlyList<CommandDescriptor> commands = ModuleDiscovery.AllCommands;

// Accès par ID ou type
IModule? module = ModuleDiscovery.GetModule("voirie");
MonModule? typedModule = ModuleDiscovery.GetModule<MonModule>();

// Commandes groupées par module
var grouped = ModuleDiscovery.GetCommandsByModule();
```

---

## ⚙️ CONVENTIONS DE NOMMAGE

### Fichiers et Assemblies

| Élément | Convention | Exemple |
|---------|------------|---------|
| Assembly | `OAS.{Module}` | `OAS.Voirie` |
| Namespace | `OpenAsphalte.Modules.{Module}` | `OpenAsphalte.Modules.Voirie` |
| Classe Module | `{Module}Module` | `VoirieModule` |
| Classe Commande | `{Action}Command` | `ParkingCommand` |

### Commandes AutoCAD

| Règle | Format | Exemple |
|-------|--------|---------|
| Préfixe obligatoire | `OAS_` | `OAS_PARKING` |
| Module + Action | `OAS_{MODULE}_{ACTION}` | `OAS_VOIRIE_PARKING` |
| Tout majuscules | `[A-Z0-9_]+` | `OAS_TOPO_IMPORT` |

### Clés de traduction

| Règle | Format | Exemple |
|-------|--------|---------|
| Préfixe module | `{module.id}.` | `voirie.` |
| Hiérarchie | `{module}.{section}.{key}` | `voirie.parking.title` |

### Calques AutoCAD

| Règle | Format | Exemple |
|-------|--------|---------|
| Préfixe | `OAS_` | `OAS_PARKING` |
| Module + Élément | `OAS_{MODULE}_{ELEMENT}` | `OAS_VOIRIE_AXES` |

---

## 📝 ALIAS OBLIGATOIRES

```csharp
// Dans chaque fichier utilisant ces types
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcColor = Autodesk.AutoCAD.Colors.Color;
using AcColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;
using L10n = OpenAsphalte.Localization.Localization;
```

---

## 🏗️ COMPILATION

### Core (depuis src/OAS.Core/)

```bash
dotnet build -c Release
# Output: bin/OAS.Core.dll
```

### Module (depuis modules/OAS.MonModule/)

```bash
dotnet build -c Release
# Output: bin/Modules/OAS.MonModule.dll
```

---

## 🧪 TEST DANS AUTOCAD

1. Lancer AutoCAD 2025+
2. Commande `NETLOAD` → sélectionner `bin/OAS.Core.dll`
3. Vérifier chargement : `OAS_HELP` → liste des commandes
4. Vérifier modules : `OAS_VERSION` → modules chargés

---

## 🔒 RÈGLES POUR L'AGENT IA

### ✅ FAIRE (Modules)

- Créer une **nouvelle DLL** dans `modules/OAS.{Module}/`
- Hériter de `ModuleBase` pour le module
- Hériter de `CommandBase` pour les commandes
- Utiliser les services existants (`GeometryService`, `LayerService`...)
- Fournir traductions FR, EN, ES dans `GetTranslations()`
- Préfixer commandes par `OAS_`
- Préfixer calques par `OAS_`
- Utiliser `ExecuteSafe()` pour toute commande
- Utiliser `ExecuteInTransaction()` pour modifications DB

### ⛔ NE PAS FAIRE (Core)

- Ne pas ajouter de commandes dans `SystemCommands.cs`
- Ne pas modifier `ModuleDiscovery.cs` pour cas spécifique
- Ne pas modifier les services pour un module particulier
- Ne pas ajouter de traductions dans `Localization.cs` (utiliser `GetTranslations()` du module)

### Conventions de commit

```
feat(core): [description]      # Fonctionnalités Core (rare)
feat(module-xxx): [description] # Fonctionnalités module
fix(core): [description]       # Corrections Core
fix(module-xxx): [description] # Corrections module
docs: [description]            # Documentation
refactor: [description]        # Refactoring
```

---

## 📊 DIAGRAMME DE CLASSES

```
┌─────────────────────────────────────────────────────────────────┐
│                         IModule                                 │
│  (interface)                                                    │
│  ─────────────────────────────────────────────────────────────  │
│  Id, Name, Description, Version, Author                         │
│  Order, IconPath, NameKey, Dependencies, MinCoreVersion         │
│  Initialize(), Shutdown(), Dispose()                            │
│  GetCommandTypes(), GetTranslations()                           │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implémente
┌─────────────────────────────────────────────────────────────────┐
│                       ModuleBase                                │
│  (abstract class)                                               │
│  ─────────────────────────────────────────────────────────────  │
│  abstract: Id, Name, Description                                │
│  virtual: Version, Author, Order, IconPath, NameKey...          │
│  virtual: Initialize(), Shutdown(), GetCommandTypes()           │
│  virtual: GetTranslations()                                     │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ hérite
┌─────────────────────────────────────────────────────────────────┐
│                      VotreModule                                │
│  (votre classe)                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  override: Id => "votre-id"                                     │
│  override: Name => "Votre Module"                               │
│  override: Description => "..."                                 │
│  override: GetCommandTypes() => [VotreCommande]                 │
│  override: GetTranslations() => {...}                           │
└─────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────┐
│                       CommandBase                               │
│  (abstract class)                                               │
│  ─────────────────────────────────────────────────────────────  │
│  Document, Database, Editor (protected)                         │
│  IsDocumentValid (protected)                                    │
│  ExecuteSafe(Action)                                            │
│  ExecuteInTransaction(Action<Transaction>)                      │
│  T(), TFormat() (traduction)                                    │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ hérite
┌─────────────────────────────────────────────────────────────────┐
│                     VotreCommande                               │
│  (votre classe)                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  [CommandMethod("OAS_VOTRE_COMMANDE")]                          │
│  [CommandInfo("Nom", Description="...", ...)]                   │
│  public void Execute() { ExecuteSafe(() => {...}); }            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📁 FICHIERS CLÉS DU CORE (Référence)

| Fichier | Rôle | Modifiable ? |
|---------|------|--------------|
| `Plugin.cs` | Point d'entrée, cycle de vie | ⚠️ Rare |
| `IModule.cs` | Interface module | ⛔ Non |
| `ModuleBase.cs` | Classe de base module | ⛔ Non |
| `CommandBase.cs` | Classe de base commande | ⛔ Non |
| `CommandInfoAttribute.cs` | Métadonnées UI | ⛔ Non |
| `ModuleDiscovery.cs` | Découverte automatique | ⛔ Non |
| `Configuration.cs` | Config JSON | ⚠️ Rare |
| `Localization.cs` | Traductions Core | ⚠️ Rare |
| `Logger.cs` | Logging console | ⛔ Non |
| `GeometryService.cs` | Calculs géométrie | ✅ Extension OK |
| `LayerService.cs` | Gestion calques | ✅ Extension OK |
| `MenuBuilder.cs` | Menu dynamique | ⛔ Non |
| `RibbonBuilder.cs` | Ruban dynamique | ⛔ Non |
| `SystemCommands.cs` | Commandes système | ⛔ Non |

---

## 🎯 CHECKLIST NOUVEAU MODULE

```
□ Créer dossier modules/OAS.{Module}/
□ Créer .csproj avec AssemblyName commençant par "OAS."
□ Créer classe {Module}Module héritant ModuleBase
  □ Implémenter Id, Name, Description
  □ Implémenter GetCommandTypes()
  □ Implémenter GetTranslations() (FR, EN, ES)
□ Créer commandes héritant CommandBase
  □ Attribut [CommandMethod("OAS_...")] 
  □ Attribut [CommandInfo(...)]
  □ Utiliser ExecuteSafe() dans Execute()
  □ Utiliser ExecuteInTransaction() pour modifications
□ Compiler → vérifier DLL dans bin/Modules/
□ Tester dans AutoCAD avec NETLOAD
□ Vérifier OAS_VERSION affiche le module
□ Vérifier OAS_HELP liste les commandes
```

---

*Document généré pour Open Asphalte v0.0.2 | .NET 8.0 | AutoCAD 2025+*
