# Open Asphalte Installer

Ce dossier contient les fichiers nécessaires pour créer l'installateur du plugin Open Asphalte.

## Compatibilité

- **AutoCAD 2025** ou supérieur (R25.0+)
- **.NET 8.0** (inclus avec AutoCAD 2025+)
- Windows 10/11 64-bit

## Prérequis

1.  **Inno Setup Compiler** (gratuit) : Télécharger et installer depuis [jrsoftware.org](https://jrsoftware.org/isdl.php).
2.  Le projet doit être compilé (`dotnet build -c Release`).

## Structure

*   `PackageContents.xml` : Fichier de définition du Bundle Autodesk. C'est ce qui permet à AutoCAD de charger le plugin automatiquement.
*   `OAS.iss` : Script Inno Setup qui génère l'exécutable `.exe`.
*   `WizardImage.bmp` : Image 164x314 affichée sur le côté gauche du wizard.
*   `WizardSmallImage.bmp` : Petite image 55x58 en haut à droite du wizard.

## Comment créer l'installeur

1.  Assurez-vous d'avoir compilé le projet en Release :
    ```bash
    dotnet build -c Release
    ```
2.  Ouvrez le fichier `OAS.iss` avec Inno Setup Compiler.
3.  Cliquez sur le bouton **Compile** (ou appuyez sur `F9`).
4.  L'installateur `OAS_Setup_v{version}.exe` sera généré dans `installer/Output/`.

Ou utilisez le script automatisé :
```bash
.\scripts\build-install.bat
```

## Fonctionnalités de l'installateur

### Détection des versions AutoCAD
L'installateur scanne automatiquement le registre pour détecter les versions d'AutoCAD compatibles (2025+) et affiche un résumé à l'utilisateur.

### Acceptation de licence
L'utilisateur doit accepter la licence GNU GPL v3 avant de continuer l'installation.

### Personnalisation du nom du menu
L'installateur propose une page optionnelle permettant de personnaliser le nom du menu principal. Si l'utilisateur entre un nom (ex: "MonEntreprise"), le menu et le ruban dans AutoCAD afficheront "MonEntreprise - OA".

Cette valeur est stockée dans `%APPDATA%\Open Asphalte\config.json` sous la clé `mainMenuName`.

### Vérification AutoCAD en cours d'exécution
L'installateur vérifie si AutoCAD est en cours d'exécution et demande à l'utilisateur de le fermer avant de continuer (installation et désinstallation).

## Structure installée

L'installateur crée la structure suivante sur le poste utilisateur :

```
%APPDATA%\Autodesk\ApplicationPlugins\OAS.bundle\
├── PackageContents.xml
└── Contents\
    ├── OAS.Core.dll
    ├── OAS.Core.deps.json
    ├── OAS.Core.runtimeconfig.json
    ├── version.json
    ├── Modules\           # Dossier pour les modules (téléchargés via OAS_MODULES)
    └── Resources\
        └── OAS_Logo.png

%APPDATA%\Open Asphalte\
└── config.json            # Configuration utilisateur
```

AutoCAD détecte automatiquement le dossier `.bundle` au démarrage.

## Notes techniques

- L'installateur utilise les **fonctions natives Inno Setup** pour manipuler les fichiers JSON de configuration (aucune dépendance PowerShell).
- Les modules ne sont PAS inclus dans l'installateur. Ils sont téléchargés via le Gestionnaire de Modules (`OAS_MODULES`) depuis GitHub.

AutoCAD détecte automatiquement ce dossier au démarrage.
