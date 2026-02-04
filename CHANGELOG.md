# Changelog

Toutes les modifications notables de ce projet seront documentées dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adhère au [Semantic Versioning](https://semver.org/lang/fr/).

## [0.0.2 - Non publié]

### À venir
- De nouveaux modules officiels.
- Possiblité de personnaliser le nom du menu [name - OR]

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
  - OR_HELP : Liste des commandes disponibles
  - OR_VERSION : Informations de version et modules chargés
  - OR_SETTINGS : Fenêtre de paramètres utilisateur
  - OR_RELOAD : Rechargement de la configuration
  - OR_UPDATE : Vérification des mises à jour
- **Classes de base pour modules** :
  - ModuleBase : Classe abstraite pour créer des modules
  - CommandBase : Classe de base pour les commandes avec gestion des erreurs
  - CommandInfoAttribute : Métadonnées pour l'affichage UI
- **Configuration utilisateur** : Stockage JSON dans AppData
- **Logging unifié** : Messages formatés dans la console AutoCAD
- **Templates** : Fichiers modèles pour créer de nouveaux modules
- **Documentation** : README, DEVELOPER.md, CONTRIBUTING.md

### Technique
- Cible : AutoCAD 2024+ (.NET 8.0)
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
