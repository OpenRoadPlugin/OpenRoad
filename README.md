# Open Asphalte

<p align="center">
  <img src="OAS_Logo.png" alt="Open Asphalte" width="200"/>
</p>

**Plugin modulaire pour AutoCAD**  Voirie et aménagement urbain

[![AutoCAD 2025+](https://img.shields.io/badge/AutoCAD-2025+-blue.svg)](https://www.autodesk.com/products/autocad)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

---

##  Vue d'ensemble

Open Asphalte est un **framework extensible** pour AutoCAD, conçu pour les professionnels de la voirie et de l'aménagement urbain. Son architecture modulaire permet d'ajouter facilement de nouvelles fonctionnalités **sans jamais modifier le cœur du programme**.

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

- **AutoCAD 2025** ou supérieur
- Windows 10/11

### Installation rapide

1. **Téléchargez** la dernière version depuis [Releases](https://github.com/openasphalteplugin/openasphalte/releases)
2. **Installer le Core** Directement avec le .exe 
3. Lancez AutoCAD et télécharger les modules que vous souhaitez.

### Structure des fichiers

```
OpenAsphalte/
  OAS.Core.dll          # Cœur du plugin (obligatoire)
  Modules/              # Dossier des modules (créé automatiquement)
    OAS.Georeferencement.dll
    OAS.StreetView.dll
    OAS.Cota2Lign.dll
    OAS.DynamicSnap.dll
    ...
```

---

## 🚀 Utilisation

### Commandes système

Ces commandes sont **toujours disponibles**, même sans aucun module installé :

| Commande | Description |
|----------|-------------|
| OAS_HELP | Affiche la liste des commandes disponibles |
| OAS_VERSION | Informations de version et modules chargés |
| OAS_SETTINGS | Ouvre la fenêtre des paramètres |
| OAS_MODULES | Ouvre le gestionnaire de modules |
| OAS_RELOAD | Recharge la configuration |
| OAS_UPDATE | Vérifie les mises à jour |

### Interface automatique

Open Asphalte génère automatiquement :
- Un **menu** avec le **nom localisé** de l'application
- Un **onglet ruban** avec le **nom localisé** de l'application

L'interface s'adapte dynamiquement :
- Module installé : Visible dans menu et ruban
- Module absent : Aucune trace dans l'interface

---

##  Modules

Les modules étendent les fonctionnalités d'Open Asphalte. Ils sont **découverts automatiquement** au démarrage.

### Gestion des Modules

- **Installation** : Cochez les modules souhaités dans l'onglet *Modules* des paramètres (`OAS_SETTINGS`) et cliquez sur *Installer*.
- **Mise à jour** : Le bouton *Mettre à jour* apparaît lorsqu'une nouvelle version est disponible.
- **Désinstallation** : Cliquez sur *Désinstaller* pour supprimer un module.
  > **Note** : La désinstallation effective se fait au redémarrage suivant d'AutoCAD (suppression des fichiers déverrouillés).

### Modules Officiels

| Module | Description | Documentation |
|--------|-------------|---------------|
| **Géoréférencement** | Systèmes de coordonnées et transformations | [Voir doc](docs/modules/georeferencement.md) |
| **Street View** | Lien dynamique AutoCAD ↔ Google Maps | [Voir doc](docs/modules/streetview.md) |
| **Cotation** | Outils de cotation voirie (Entre 2 lignes) | [Voir doc](docs/modules/cota2lign.md) |
| **Dynamic Snap** | Moteur d'accrochage intelligent (Système) | [Voir doc](docs/modules/dynamicsnap.md) |

### Installation d'un module

**Option A : Utiliser le gestionnaire de modules intégré**

1. Ouvrez AutoCAD
2. Tapez **OAS_MODULES**
3. Sélectionnez le module à installer
4. Redémarrez AutoCAD

Le module apparaîtra automatiquement dans l'interface !

**Option B : Installation manuelle**

1. Téléchargez le fichier .dll du module (ex: `OAS.Georeferencement.dll`)
2. Placez-le dans le dossier **Modules/** (à côté de OAS.Core.dll)
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

Changez la langue avec `OAS_SETTINGS` ou dans le fichier de configuration.
Tous les textes du **Core** (UI, commandes système, logs) sont localisés.

---

## 🛠️ Configuration

La configuration est stockée dans :
```
%APPDATA%\Open Asphalte\config.json
```

### Paramètres disponibles

| Paramètre | Description | Défaut |
|-----------|-------------|--------|
| language | Langue (fr, en, es) | fr |
| devMode | Mode développeur (logs détaillés) | false |
| checkUpdatesOnStartup | Vérifier les mises à jour au démarrage | true |
| mainMenuName | Nom personnalisé du menu et ruban | Open Asphalte |

### Personnalisation du nom du menu

Lors de l'installation, vous pouvez personnaliser le nom du menu principal qui s'affichera dans AutoCAD. Si vous entrez un nom (ex: "MonEntreprise"), le menu et le ruban afficheront "MonEntreprise - OA".

Vous pouvez également modifier ce paramètre manuellement dans le fichier `config.json` :

```json
{
  "mainMenuName": "MonEntreprise - OA"
}
```

---

## Architecture

```
OpenAsphalte/
  src/
    OAS.Core/                 # Core du plugin (NE JAMAIS MODIFIER)
      Plugin.cs               # Point d'entrée IExtensionApplication
      Abstractions/           # Interfaces pour créer des modules
        IModule.cs            # Interface module
        ModuleBase.cs         # Classe de base module
        CommandBase.cs        # Classe de base commandes
        CommandInfoAttribute.cs
      Discovery/              # Découverte automatique des modules
      Configuration/          # Gestion de la configuration
      Localization/           # Système de traduction
      Logging/                # Logs unifiés
      Services/               # Services partagés
        GeometryService.cs
        LayerService.cs
      UI/                     # Construction dynamique du menu et ruban
      Commands/               # Commandes système (OAS_HELP, OAS_SETTINGS...)

  templates/                  # Templates pour créer de nouveaux modules
    OAS.Module.Template.csproj
    ModuleTemplate.cs
    CommandTemplate.cs

  bin/
    OAS.Core.dll              # DLL principale compilée
    Modules/                  # Dossier des modules (DLL externes)
```

### Flux de chargement

```
AutoCAD démarre
  NETLOAD OAS.Core.dll
    1. Chargement configuration
    2. Initialisation localisation
    3. Scan du dossier Modules/
       Pour chaque OAS.*.dll trouvée :
         - Recherche des classes IModule
         - Validation des dépendances
         - Chargement des traductions
         - Appel Initialize()
    4. Génération du menu dynamique
    5. Génération du ruban dynamique
    6. Prêt !
```

---

##  Compilation

### Prérequis développeur

- Visual Studio 2022 ou VS Code avec C#
- .NET 8.0 SDK
- AutoCAD 2025 (pour les DLL de référence)

### Compiler le Core

```bash
cd src/OAS.Core
dotnet build -c Release
```

Le fichier OAS.Core.dll sera généré dans bin/.

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

**Open Asphalte et ses contributeurs ne peuvent en aucun cas être tenus responsables** de :
- Tout dommage direct, indirect, accessoire ou consécutif
- Toute perte de données ou de profits
- Toute interruption d'activité
- Tout préjudice résultant de l'utilisation ou de l'impossibilité d'utiliser ce logiciel

L'utilisation de ce plugin dans AutoCAD se fait **à vos propres risques**. Vérifiez toujours vos dessins et données avant toute opération critique.
---

##  Support

-  Issues: [GitHub Issues](https://github.com/openasphalteplugin/openasphalte/issues)
-  Discussions: [GitHub Discussions](https://github.com/openasphalteplugin/openasphalte/discussions)

---

## ✨ Partenaires

Une immense reconnaissance à nos partenaires qui soutiennent le projet Open Asphalte :

<table width="100%">
  <tr>
    <td align="center" width="50%">
      <a href="https://cadgeneration.com" title="CAD Generation - Forum d'entraide CAO DAO spécialisé AutoCAD">
        <img src="https://cadgeneration.com/uploads/default/original/1X/7a094534a3b665c067075eadfe4208ca43309ac4.png" alt="CAD Generation - Communauté AutoCAD et CAO/DAO" height="100" />
      </a>
      <br />
      <b><a href="https://cadgeneration.com" title="CAD Generation - Forum CAO/DAO boosté à l'IA">CAD Generation</a></b>
      <br />
      Forum d'entraide CAO / DAO boosté à l'IA - Communauté AutoCAD
    </td>
    <td align="center" width="50%">
      <a href="https://www.jcx-projets.fr" title="JCX Projets - Bureau d'étude VRD et infrastructure">
        <img src="https://www.jcx-projets.fr/wp-content/uploads/2019/04/Logo_200_SF.png" alt="JCX Projets - Expert VRD et infrastructure routière" height="100" />
      </a>
      <br />
      <b><a href="https://www.jcx-projets.fr" title="JCX Projets - Bureau d'étude VRD">JCX Projets</a></b>
      <br />
      Bureau d'étude EXE VRD - Expertise infrastructure et voirie
    </td>
  </tr>
</table>

