# Open Road Installer

Ce dossier contient les fichiers nécessaires pour créer l'installateur du plugin Open Road.

## Compatibilité

- **AutoCAD 2024** ou supérieur (R24.0+)
- **.NET 8.0** (inclus avec AutoCAD 2024+)
- Windows 10/11 64-bit

## Prérequis

1.  **Inno Setup Compiler** (gratuit) : Télécharger et installer depuis [jrsoftware.org](https://jrsoftware.org/isdl.php).
2.  Le projet doit être compilé (`dotnet build -c Release`).

## Structure

*   `PackageContents.xml` : Fichier de définition du Bundle Autodesk. C'est ce qui permet à AutoCAD de charger le plugin automatiquement.
*   `OpenRoad.iss` : Script Inno Setup qui génère l'exécutable `.exe`.

## Comment créer l'installeur

1.  Assurez-vous d'avoir compilé le projet en Release :
    ```bash
    dotnet build -c Release
    ```
2.  Ouvrez le fichier `OpenRoad.iss` avec Inno Setup Compiler.
3.  Cliquez sur le bouton **Compile** (ou appuyez sur `F9`).
4.  L'installateur `OpenRoad_Setup_v0.0.1.exe` sera généré dans ce dossier `installer/Output` (ou à la racine selon la config).

## Fonctionnement

L'installateur va créer la structure suivante sur le poste utilisateur :
`%APPDATA%\Autodesk\ApplicationPlugins\OpenRoad.bundle\`
*   `PackageContents.xml`
*   `Contents\`
    *   `OpenRoad.Core.dll`
    *   `Modules\` ...

AutoCAD détecte automatiquement ce dossier au démarrage.
