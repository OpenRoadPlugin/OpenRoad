# Open Road

<p align="center">
  <img src="OpenAsphalte_Logo.png" alt="Open Asphalte" width="350"/>
</p>

**Plugin modulaire pour AutoCAD**  Voirie et aménagement urbain

[![AutoCAD 2024+](https://img.shields.io/badge/AutoCAD-2024+-blue.svg)](https://www.autodesk.com/products/autocad)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

---

##  Vue d'ensemble

Open Road est un **framework extensible** pour AutoCAD, conçu pour les professionnels de la voirie et de l'aménagement urbain. Son architecture modulaire permet d'ajouter facilement de nouvelles fonctionnalités **sans jamais modifier le cœur du programme**.

###  Philosophie

> **Le cœur ne change jamais.** Les modules s'ajoutent, le cœur reste intact.

-  **Architecture modulaire**  Ajoutez des fonctionnalités en déposant simplement des DLL
-  **Découverte automatique**  Les modules sont détectés au démarrage sans configuration
-  **Interface dynamique**  Menu et ruban générés automatiquement selon les modules installés
-  **Multilingue**  Français, Anglais, Espagnol
-  **Zéro configuration**  Fonctionne dès l'installation

###  Principe fondamental

Si un module n'est pas installé, il **n'existe nulle part** :
- Pas dans le menu
- Pas dans le ruban  
- Pas dans les commandes
- Pas dans la mémoire

Le programme s'adapte automatiquement aux modules présents.

---

##  Installation

### Prérequis

- **AutoCAD 2024** ou supérieur
- Windows 10/11

### Installation rapide

1. **Téléchargez** la dernière version depuis [Releases](https://github.com/openroadplugin/openroad/releases)
2. **Extrayez** le contenu dans un dossier (ex: C:\OpenRoad\)
3. Dans AutoCAD, tapez **NETLOAD**
4. Sélectionnez **OpenRoad.Core.dll**
5. Tapez **OR_HELP** pour voir les commandes disponibles

### Structure des fichiers

`
OpenRoad/
 OpenRoad.Core.dll      # Cœur du plugin (obligatoire)
 Modules/               # Dossier des modules (créé automatiquement)
     OpenRoad.Voirie.dll
     OpenRoad.Dessin.dll
     ...
`

### Chargement automatique (optionnel)

Ajoutez à votre fichier cad.lsp ou caddoc.lsp :
`lisp
(command "NETLOAD" "C:\\chemin\\vers\\OpenRoad.Core.dll")
`

---

## 🚀 Utilisation

### Commandes système

Ces commandes sont **toujours disponibles**, même sans aucun module installé :

| Commande | Description |
|----------|-------------|
| OR_HELP | Affiche la liste des commandes disponibles |
| OR_VERSION | Informations de version et modules chargés |
| OR_SETTINGS | Ouvre la fenêtre des paramètres |
| OR_RELOAD | Recharge la configuration |
| OR_UPDATE | Vérifie les mises à jour |

### Interface automatique

Open Road génère automatiquement :
- Un **menu** avec le **nom localisé** de l'application
- Un **onglet ruban** avec le **nom localisé** de l'application

L'interface s'adapte dynamiquement :
- Module installé ? Visible dans menu et ruban
- Module absent ? Aucune trace dans l'interface

---

##  Modules

Les modules étendent les fonctionnalités d'Open Road. Ils sont **découverts automatiquement** au démarrage.

### Installation d'un module

1. Téléchargez le fichier .dll du module
2. Placez-le dans le dossier **Modules/** (à côté de OpenRoad.Core.dll)
3. Redémarrez AutoCAD

Le module apparaîtra automatiquement dans l'interface ! 

### Suppression d'un module

1. Fermez AutoCAD
2. Supprimez le fichier .dll du dossier Modules/
3. Relancez AutoCAD

Le module disparaîtra complètement de l'interface.

### Créer vos propres modules

Consultez le **[Guide développeur](docs/guides/developer_guide.md)** pour créer vos modules personnalisés.

---

## 🌐 Langues supportées

- 🇫🇷 **Français** (par défaut)
- 🇬🇧 English
- 🇪🇸 Español

Changez la langue avec `OR_SETTINGS` ou dans le fichier de configuration.
Tous les textes du **Core** (UI, commandes système, logs) sont localisés.

---

## 🛠️ Configuration

La configuration est stockée dans :
```
%APPDATA%\Open Road\config.json
```

### Paramètres disponibles

| Paramètre | Description | Défaut |
|-----------|-------------|--------|
| language | Langue (fr, en, es) | fr |
| devMode | Mode développeur (logs détaillés) | false |
| checkUpdatesOnStartup | Vérifier les mises à jour au démarrage | true |

---

##  Architecture

`
OpenRoad/
 src/
    OpenRoad.Core/           # Cœur du plugin (NE JAMAIS MODIFIER)
        Plugin.cs            # Point d'entrée IExtensionApplication
        Abstractions/        # Interfaces pour créer des modules
           IModule.cs       # Interface module
           ModuleBase.cs    # Classe de base module
           CommandBase.cs   # Classe de base commandes
           CommandInfoAttribute.cs
        Discovery/           # Découverte automatique des modules
        Configuration/       # Gestion de la configuration
        Localization/        # Système de traduction
        Logging/             # Logs unifiés
        Services/            # Services partagés
           GeometryService.cs
           LayerService.cs
        UI/                  # Construction dynamique du menu et ruban
        Commands/            # Commandes système (OR_HELP, OR_SETTINGS...)

 templates/                   # Templates pour créer de nouveaux modules
    OpenRoad.Module.Template.csproj
    ModuleTemplate.cs
    CommandTemplate.cs

 bin/
     OpenRoad.Core.dll        # DLL principale compilée
     Modules/                 # Dossier des modules (DLL externes)
`

### Flux de chargement

`
AutoCAD démarre
     NETLOAD OpenRoad.Core.dll
         1. Chargement configuration
         2. Initialisation localisation
         3. Scan du dossier Modules/
            Pour chaque OpenRoad.*.dll trouvée :
                Recherche des classes IModule
                Validation des dépendances
                Chargement des traductions
                Appel Initialize()
         4. Génération du menu dynamique
         5. Génération du ruban dynamique
         6. Prêt !
`

---

##  Compilation

### Prérequis développeur

- Visual Studio 2022 ou VS Code avec C#
- .NET 8.0 SDK
- AutoCAD 2024 (pour les DLL de référence)

### Compiler le Core

```bash
cd src/OpenRoad.Core
dotnet build -c Release
```

Le fichier OpenRoad.Core.dll sera généré dans bin/.

---

## 🤝 Contribution

Les contributions sont les bienvenues ! Consultez [CONTRIBUTING.md](CONTRIBUTING.md).

### Comment contribuer

1. Fork le projet
2. Créez une branche (git checkout -b feature/ma-fonctionnalite)
3. Committez (git commit -m 'Ajout de ma fonctionnalité')
4. Push (git push origin feature/ma-fonctionnalite)
5. Ouvrez une Pull Request

---

##  Licence

Ce projet est sous licence **[Apache 2.0](LICENSE)**  libre d'utilisation, modification et distribution selon les termes de la licence.
Voir aussi le fichier [NOTICE](NOTICE) pour les mentions et marques.

###  Avertissement (Disclaimer)

Ce logiciel est fourni **"tel quel"**, sans aucune garantie d'aucune sorte, expresse ou implicite. 

**Open Road et ses contributeurs ne peuvent en aucun cas être tenus responsables** de :
- Tout dommage direct, indirect, accessoire ou consécutif
- Toute perte de données ou de profits
- Toute interruption d'activité
- Tout préjudice résultant de l'utilisation ou de l'impossibilité d'utiliser ce logiciel

L'utilisation de ce plugin dans AutoCAD se fait **à vos propres risques**. Vérifiez toujours vos dessins et données avant toute opération critique.
---

##  Support

-  Issues: [GitHub Issues](https://github.com/openroadplugin/openroad/issues)
-  Discussions: [GitHub Discussions](https://github.com/openroadplugin/openroad/discussions)
