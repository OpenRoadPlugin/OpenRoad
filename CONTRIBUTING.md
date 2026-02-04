# Contribuer à Open Road

Merci de votre intérêt pour contribuer à Open Road ! 

---

##  Environnement de développement

### Prérequis

- **.NET 8 SDK**  [Télécharger](https://dotnet.microsoft.com/download/dotnet/8.0)
- **AutoCAD 2026**  Pour les DLL de référence
- **Visual Studio 2022** ou **VS Code** avec extensions :
  - C# (ms-dotnettools.csharp)
  - C# Dev Kit (ms-dotnettools.csdevkit)

### Configuration

1. Clonez le repository :
   ```bash
   git clone https://github.com/openroadplugin/openroad.git
   cd openroad
   ```

2. Ouvrez le projet dans votre IDE

3. Vérifiez les chemins AutoCAD dans src/OpenRoad.Core/OpenRoad.Core.csproj :
   `xml
   <HintPath>C:\Program Files\Autodesk\AutoCAD 2026\accoremgd.dll</HintPath>
   `

### Compilation

```bash
cd src/OpenRoad.Core
dotnet build -c Release
```

Le fichier OpenRoad.Core.dll sera généré dans in/.

---

##  Structure du projet

```
OpenRoad/
 src/
    OpenRoad.Core/           # Cœur du plugin
        Plugin.cs            # Point d'entrée
        Abstractions/        # Interfaces pour modules
        Discovery/           # Découverte automatique
        Configuration/       # Paramètres utilisateur
        Localization/        # Traductions
        Logging/             # Logs
        Services/            # Services partagés
        UI/                  # Menu et ruban
        Commands/            # Commandes système

 templates/                   # Templates pour modules
 bin/
     OpenRoad.Core.dll
     Modules/                 # DLL des modules
```

---

##  Types de contributions

### 1. Améliorer le Core

Le Core doit rester **générique** et bénéficier à tous les modules.

**À faire :**
- Corriger des bugs
- Améliorer les performances
- Ajouter des services partagés utiles à plusieurs modules
- Améliorer la documentation

**À ne PAS faire :**
- Ajouter des fonctionnalités spécifiques à un métier
- Modifier la découverte des modules pour un cas particulier
- Ajouter des commandes utilisateur (sauf commandes système)

### 2. Créer un module

Les nouvelles fonctionnalités doivent être créées comme des **modules séparés**.

Consultez le [Guide développeur](docs/guides/developer_guide.md) pour créer un module.

### 3. Améliorer la documentation

- Corriger les erreurs
- Ajouter des exemples
- Traduire en d'autres langues

---

##  Conventions de code

### Nommage

| Élément | Convention | Exemple |
|---------|------------|---------|
| Namespace Core | OpenRoad.* | OpenRoad.Services |
| Namespace Module | OpenRoad.Modules.{Module} | OpenRoad.Modules.Voirie |
| Commande | OR_{MODULE}_{ACTION} | OR_VOIRIE_PARKING |
| Clé traduction | {module}.{section}.{key} | oirie.parking.title |

### Style de code

- Utilisez les alias recommandés :
  `csharp
  using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
  using AcColor = Autodesk.AutoCAD.Colors.Color;
  using L10n = OpenRoad.Localization.Localization;
  `

- Utilisez ExecuteSafe() pour la gestion d'erreurs
- Utilisez ExecuteInTransaction() pour les modifications du dessin
- Documentez avec des commentaires XML (///)

---

##  Processus de contribution

### 1. Fork et clone

`ash
git clone https://github.com/VOTRE_USERNAME/openroad.git
cd openroad
`

### 2. Créer une branche

`ash
git checkout -b feature/ma-fonctionnalite
# ou
git checkout -b fix/mon-correctif
`

### 3. Faire vos modifications

- Respectez les conventions de code
- Testez vos modifications dans AutoCAD
- Mettez à jour la documentation si nécessaire

### 4. Committer

Utilisez des messages de commit clairs :

`ash
# Pour le Core
git commit -m "feat(core): ajoute support des arcs dans GeometryService"
git commit -m "fix(core): corrige le crash au déchargement"

# Pour un module
git commit -m "feat(module-voirie): ajoute commande de marquage au sol"

# Pour la documentation
git commit -m "docs: améliore le guide développeur"
`

### 5. Push et Pull Request

`ash
git push origin feature/ma-fonctionnalite
`

Puis ouvrez une Pull Request sur GitHub.

---

##  Checklist avant PR

- [ ] Le code compile sans erreur
- [ ] Les conventions de nommage sont respectées
- [ ] La documentation est à jour
- [ ] Les traductions FR/EN/ES sont fournies (si applicable)
- [ ] Aucun texte utilisateur n'est codé en dur (utiliser Localization)
- [ ] Les tests dans AutoCAD passent

---

##  Questions ?

- Ouvrez une [Issue](https://github.com/openroadplugin/openroad/issues)
- Rejoignez les [Discussions](https://github.com/openroadplugin/openroad/discussions)

Merci pour votre contribution ! 

---

##  Licence et responsabilité

En contribuant à Open Road, vous acceptez que :

1. **Vos contributions sont sous licence Apache 2.0**  Elles peuvent être utilisées, modifiées et distribuées librement
2. **Vous accordez une licence de brevet**  Pour toute contribution que vous soumettez
3. **Vous renoncez à toute réclamation** concernant l'utilisation de votre code
4. **Open Road est fourni "tel quel"**  Sans garantie d'aucune sorte

### Marques

"Open Road" est une marque réservée. Les contributions n'accordent aucun droit sur le nom ou le logo.

Pour plus de détails, consultez notre [Politique de Sécurité](SECURITY.md), le fichier [LICENSE](LICENSE) et le fichier [NOTICE](NOTICE).
