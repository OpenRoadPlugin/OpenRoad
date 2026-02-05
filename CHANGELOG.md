# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

## [0.0.2] - 2026-02-05

### Modifications CORE
- **Vérification des mises à jour au démarrage** : Nouvelle fonctionnalité qui vérifie automatiquement si une nouvelle version est disponible via l'API GitHub.
  - Comparaison de version avec la dernière release
  - Contrôle de compatibilité AutoCAD (vérifie que la version minimale AutoCAD est respectée)
  - Notification non-bloquante proposant d'ouvrir la page de téléchargement
  - Désactivable dans les paramètres (`checkUpdatesOnStartup`)
- **Modules Personnalisés** : Ajout d'une source personnalisable (Dossier local ou URL) pour installer des modules privés/beta.
- **UI Modules** : Identification visuelle des modules "Custom" (texte violet/gras) et tri prioritaire.
- **Changement de nom** pour empecher toute confusion avec des soft existants.
    - Open Road s'appelle maintenant **Open Asphalte**
- **Personnalisation du menu** : L'installateur permet de définir un nom personnalisé (ex: "MonEntreprise - OA")
- **Page de licence** : Acceptation obligatoire de la licence Apache 2.0 dans l'installateur
- **Gestionnaire de modules amélioré** :
  - Auto-refresh de la liste lors de l'arrivée sur l'onglet Modules
  - Gestion automatique des dépendances (téléchargement récursif)
  - Multi-sélection avec checkboxes pour installation par lot
  - Groupement des modules par catégorie
  - Message au démarrage si aucun module installé
  - **Désinstallation des modules** :
    - Bouton "Désinstaller" pour les modules installés
    - Renommage sécurisé en `.del` pour contourner le verrouillage fichier Windows
    - Nettoyage automatique des fichiers supprimés au démarrage suivant
    - Vérification des dépendances inversées (empêche de supprimer un module requis par un autre)
- **Traductions** : Nouvelles clés FR/EN/ES pour le gestionnaire de modules
- **CI/CD** : Workflow GitHub Actions pour validation des PRs
- **Thread Safety** : Configuration utilise maintenant `ConcurrentDictionary` pour les accès concurrents
- **Gestion mémoire** : 
  - HttpClient avec `PooledConnectionLifetime` pour éviter les problèmes DNS stale
  - Désabonnement du handler `FirstChanceException` dans `Terminate()`
  - Nettoyage des caches ribbon lors du rebuild

### Corrigé
- Bug TFormat avec placeholders {0} {1} non remplacés dans les messages de dépendances (surcharge ambiguë)
- Logo dans la fenêtre "À propos" : adapté au nouveau format rectangulaire (200x105)
- Modules non détectés après installation : préfixe de fichier corrigé (`OAS.*.dll` au lieu de `OpenRoad.*.dll`) + migration automatique des anciens modules
- Dépendances non affichées comme installées : correction du binding WPF avec `INotifyPropertyChanged`
- Onglet Modules de Settings non rafraîchi : ajout du chargement automatique à l'ouverture directe
- Nom du menu persistant après désinstallation : suppression du fichier `config.json` lors de la désinstallation
- **Traductions manquantes** : Ajout des clés `core.credits.*`, `about.credits`, `settings.tab.*`, `settings.modules.*` pour FR/EN/ES
- **Patterns DLL obsolètes** : Correction de `OpenRoad.*.dll` → `OAS.*.dll` dans SettingsWindow et ModuleManagerWindow
- **Caractère invalide** : Correction `?` → `→` dans SetProjectionWindow (limites de projection)
- **Internationalisation modules** :
  - Cota2Lign : MessageBox de validation utilisent maintenant les traductions
  - DynamicSnap : `GetDisplayName()` utilise le système de localisation
  - StreetView : Prompt Oui/Non utilise les traductions `common.yes`/`common.no`
- **Race condition Configuration.Load()** : Toute la logique de chargement est maintenant dans le bloc lock pour éviter les accès concurrents
- **Exception masquée dans CommandBase** : `tr.Abort()` est maintenant wrappé dans try-catch pour préserver l'exception originale
- **Nommage obsolète** : Renommé `openroad_*.log.old` → `openasphalte_*.log.old` et `OpenRoad_Setup.exe` → `OpenAsphalte_Setup.exe`
- **Empty catch blocks** : Ajout de commentaires explicatifs dans SnapDetector.cs pour les catch vides
- **Traductions XAML Cota2Lign** : Labels "Interdistance", "Décalage", "Calque" maintenant traduisibles (FR/EN/ES)
- **Traductions XAML SetProjection** : Labels "Système actuel", "Détecté", "Détails", etc. maintenant traduisibles (FR/EN/ES)

### Supprimé
- Fichiers obsolètes du renommage OpenRoad → OpenAsphalte :
  - `src/OpenRoad.Core/` (dossier entier)
  - `src/OpenRoad.sln`
  - `installer/OpenRoad.iss`
  - `templates/OpenRoad.Module.Template.csproj`

### Documentation
- Correction des chemins `OpenAsphalte.Core` → `OAS.Core` dans CONTRIBUTING.md
- Correction de la référence `OAS_ABOUT` → `OAS_VERSION` dans CONTRIBUTING.md
- Correction de la typo `oirie` → `voirie` dans CONTRIBUTING.md
- Mise à jour des exemples de modules dans README.md (modules réels)
- Ajout des modules `cota2lign` et `dynamicsnap` dans marketplace.json

### Nouveaux modules
- Module de Cotations entre 2 polylignes
- Module d'acrochage intelligent (snap amélioré) pour les modules OAS

---

## [0.0.1] - 2026-02-04

### Ajouté
- **Architecture modulaire** : Système de découverte automatique des modules (DLL)
- **Interface dynamique** : Menu et ruban générés automatiquement selon les modules installés
- **Système de localisation** : Support multilingue (Français, English, Español)
- **Services partagés** :
  - GeometryService : Calculs géométriques (distances, angles, interpolation)
  - LayerService : Gestion des calques AutoCAD

- **Commandes système** :
  - OAS_HELP : Liste des commandes disponibles
  - OAS_VERSION : Informations de version et modules chargés
  - OAS_SETTINGS : Fenêtre de paramètres utilisateur
  - OAS_RELOAD : Rechargement de la configuration
  - OAS_UPDATE : Vérification des mises à jour
- **Classes de base pour modules** :
  - ModuleBase : Classe abstraite pour créer des modules
  - CommandBase : Classe de base pour les commandes avec gestion des erreurs
  - CommandInfoAttribute : Métadonnées pour l'affichage UI
- **Configuration utilisateur** : Stockage JSON dans AppData
- **Logging unifié** : Messages formatés dans la console AutoCAD
- **Templates** : Fichiers modèles pour créer de nouveaux modules
- **Documentation** : README, DEVELOPER.md, CONTRIBUTING.md

### Technique
- Cible : AutoCAD 2025+ (.NET 8.0)
- Détection automatique du chemin d'installation AutoCAD
- Gestion propre du cycle de vie (Initialize/Terminate)
- Libération correcte des objets COM (menus)

---

## Types de changements

- Ajouté pour les nouvelles fonctionnalités
- Modifié pour les changements dans les fonctionnalités existantes
- Déprécié pour les fonctionnalités qui seront supprimées prochainement
- Supprimé pour les fonctionnalités supprimées
- Corrigé pour les corrections de bugs
- Sécurité pour les vulnérabilités corrigées
