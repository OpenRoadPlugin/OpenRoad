# Contribuer à Open Asphalte

Merci de votre intérêt pour contribuer à Open Asphalte ! 

---

##  Environnement de développement

### Prérequis

- **.NET 8 SDK**  [Télécharger](https://dotnet.microsoft.com/download/dotnet/8.0)
- **AutoCAD 2025**  Pour les DLL de référence
- **Visual Studio 2022** ou **VS Code** avec extensions :
  - C# (ms-dotnettools.csharp)
  - C# Dev Kit (ms-dotnettools.csdevkit)

### Configuration

1. Clonez le repository :
   ```bash
   git clone https://github.com/openasphalteplugin/openasphalte.git
   cd openasphalte
   ```

2. Ouvrez le projet dans votre IDE

3. Vérifiez les chemins AutoCAD dans src/OAS.Core/OAS.Core.csproj :
   `xml
   <HintPath>C:\Program Files\Autodesk\AutoCAD 2025\accoremgd.dll</HintPath>
   `

### Compilation

```bash
cd src/OAS.Core
dotnet build -c Release
```

Le fichier OAS.Core.dll sera généré dans bin/.

---

##  Structure du projet

```
OpenAsphalte/
 src/
    OAS.Core/                    # Cœur du plugin
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
     OAS.Core.dll
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
| Namespace Core | OpenAsphalte.* | OpenAsphalte.Services |
| Namespace Module | OpenAsphalte.Modules.{Module} | OpenAsphalte.Modules.Voirie |
| Commande | OAS_{MODULE}_{ACTION} | OAS_VOIRIE_PARKING |
| Clé traduction | {module}.{section}.{key} | voirie.parking.title |

### Style de code

- Utilisez les alias recommandés :
  `csharp
  using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
  using AcColor = Autodesk.AutoCAD.Colors.Color;
  using L10n = OpenAsphalte.Localization.Localization;
  `

- Utilisez ExecuteSafe() pour la gestion d'erreurs
- Utilisez ExecuteInTransaction() pour les modifications du dessin
- Documentez avec des commentaires XML (///)

---

##  Processus de contribution

### 1. Fork et clone

`ash
git clone https://github.com/VOTRE_USERNAME/openasphalte.git
cd openasphalte
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

- Ouvrez une [Issue](https://github.com/openasphalteplugin/openasphalte/issues)
- Rejoignez les [Discussions](https://github.com/openasphalteplugin/openasphalte/discussions)

Merci pour votre contribution ! 

---

## ?? Reconnaissance des contributeurs

Open Asphalte valorise ses contributeurs ! Selon votre niveau de participation, vous pouvez être crédité directement dans le programme.

### Crédits automatiques pour les développeurs de modules

**Si vous développez un module**, vos crédits sont **automatiquement affichés** via les propriétés de votre classe module :

```csharp
public class MonModule : ModuleBase
{
    public override string Author => "Votre Nom";           // Affiché dans OAS_VERSION et OAS_MODULES
    public override string Version => "1.0.0";              // Version de votre module
    public override string Description => "Mon super module";
    
    // Vous pouvez aussi ajouter un lien dans la description :
    // "Module de géolocalisation - https://monsite.com"
}
```

Ces informations apparaissent dans :
- La commande `OAS_VERSION` — Liste des modules chargés avec auteurs
- Le gestionnaire de modules `OAS_MODULES` — Détails de chaque module
- Le marketplace (si votre module est publié)

### Niveaux de reconnaissance additionnels

| Niveau | Critères | Reconnaissance |
|--------|----------|----------------|
| **Contributeur** | 1-3 contributions acceptées (PR, corrections, traductions) | Nom dans le fichier NOTICE |
| **Contributeur actif** | 4-10 contributions significatives | Nom + lien vers profil GitHub dans `OAS_VERSION` |
| **Développeur Core** | Contributions majeures au Core | Nom + lien site personnel dans `OAS_VERSION` et documentation |
| **Testeur reconnu** | Tests réguliers + rapports de bugs détaillés (5+) | Mention dans `OAS_VERSION` section testeurs |

### Comment demander vos crédits ?

1. **Lors de votre Pull Request**, ajoutez dans la description :
   ```
   ## Crédits souhaités
   - Nom/Pseudo : [Votre nom]
   - Site web : [URL de votre site] (optionnel)
   - Rôle : Développeur / Testeur / Traducteur
   ```

2. **Après plusieurs contributions**, ouvrez une Issue avec le label `credits` en listant vos contributions.

### Informations affichées

Vous pouvez choisir d'afficher :
- ? Votre nom ou pseudonyme
- ? Un lien vers votre site personnel ou portfolio
- ? Un lien vers votre profil GitHub
- ? Votre entreprise (si applicable)

Toutes ces informations sont **optionnelles**. Vous pouvez contribuer anonymement si vous le préférez.

### Où apparaissent les crédits ?

- **Fichier [NOTICE](NOTICE)** — Liste complète des contributeurs
- **Commande `OAS_VERSION`** — Fenêtre "À propos" dans AutoCAD
- **Documentation** — Page des contributeurs (pour contributions majeures)

---

##  Licence et responsabilité

En contribuant à Open Asphalte, vous acceptez que :

1. **Vos contributions sont sous licence GNU GPL v3**  Elles peuvent être utilisées, modifiées et distribuées librement
2. **Vous accordez une licence de brevet**  Pour toute contribution que vous soumettez
3. **Vous renoncez à toute réclamation** concernant l'utilisation de votre code
4. **Open Asphalte est fourni "tel quel"**  Sans garantie d'aucune sorte

### Marques

"Open Asphalte" est une marque réservée. Les contributions n'accordent aucun droit sur le nom ou le logo.

Pour plus de détails, consultez notre [Politique de Sécurité](SECURITY.md), le fichier [LICENSE](LICENSE) et le fichier [NOTICE](NOTICE).
