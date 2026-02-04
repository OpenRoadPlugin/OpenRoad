# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

## [0.0.2] - En développement

### Modifications CORE
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
- **Traductions** : Nouvelles clés FR/EN/ES pour le gestionnaire de modules
- **CI/CD** : Workflow GitHub Actions pour validation des PRs

### Corrigé
- Bug TFormat avec placeholders {0} {1} non remplacés dans les messages de dépendances (surcharge ambiguë)
- Logo dans la fenêtre "À propos" : adapté au nouveau format rectangulaire (200x105)
- Modules non détectés après installation : préfixe de fichier corrigé (`OAS.*.dll` au lieu de `OpenRoad.*.dll`) + migration automatique des anciens modules
- Dépendances non affichées comme installées : correction du binding WPF avec `INotifyPropertyChanged`
- Onglet Modules de Settings non rafraîchi : ajout du chargement automatique à l'ouverture directe
- Nom du menu persistant après désinstallation : suppression du fichier `config.json` lors de la désinstallation

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
