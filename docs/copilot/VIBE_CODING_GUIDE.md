# 🎯 Guide Vibe-Coding avec GitHub Copilot

> **Guide à destination des développeurs Open Asphalte pour maximiser l'efficacité avec les assistants IA**

---

## 🤖 Qu'est-ce que le Vibe-Coding ?

Le **Vibe-Coding** (ou "Coding par l'intention") est une approche de développement où vous collaborez avec une IA (comme GitHub Copilot) en exprimant vos intentions plutôt qu'en écrivant chaque ligne de code manuellement.

### Principes fondamentaux

1. **Exprimez l'intention** — Décrivez ce que vous voulez accomplir, pas comment le faire
2. **Contexte riche** — Fournissez suffisamment de contexte pour que l'IA comprenne le projet
3. **Itération rapide** — Validez, ajustez, affinez en boucle courte
4. **Expertise humaine** — Vous restez le décideur, l'IA est votre assistant

### Avantages

- ⚡ **Rapidité** — Génération de code boilerplate en secondes
- 🎯 **Focus** — Concentrez-vous sur la logique métier, pas la syntaxe
- 📚 **Apprentissage** — Découvrez des patterns et API que vous ne connaissiez pas
- 🔄 **Consistance** — L'IA respecte les conventions établies

---

## 🛠️ Configuration Copilot pour Open Asphalte

### Instructions personnalisées

Le projet Open Asphalte utilise des **instructions Copilot personnalisées** situées dans :

```
.github/copilot-instructions.md    # Instructions principales (contexte complet)
docs/copilot/
├── copilot-instructions-core.md   # Pour les modifications du Core
├── copilot-instructions-module.md # Pour le développement de modules
└── VIBE_CODING_GUIDE.md           # Ce guide
```

### Comment ça fonctionne

Lorsque vous utilisez GitHub Copilot Chat dans VS Code :

1. **Copilot lit automatiquement** le fichier `.github/copilot-instructions.md`
2. **Il comprend** l'architecture modulaire, les conventions, les patterns
3. **Il génère du code** respectant ces règles automatiquement

### Activer les instructions

1. Ouvrez VS Code avec le workspace Open Asphalte
2. Les instructions sont automatiquement chargées par Copilot
3. Commencez à discuter avec Copilot Chat

---

## 📝 Bonnes pratiques de prompting

### Structure d'un bon prompt

```
[CONTEXTE] → Ce que vous faites
[INTENTION] → Ce que vous voulez obtenir
[CONTRAINTES] → Ce qu'il faut respecter
```

### Exemples de prompts efficaces

#### ✅ BON : Créer un nouveau module

```
Je veux créer un module "Signalisation" pour Open Asphalte.
Ce module doit :
- Permettre de dessiner des panneaux routiers (stop, cédez le passage, etc.)
- Stocker les panneaux sur un calque OAS_SIGNALISATION
- Avoir une commande OAS_SIGNALISATION_PANNEAU

Génère la structure complète du module avec les traductions FR/EN/ES.
```

#### ✅ BON : Ajouter une commande à un module existant

```
Dans le module Georeferencement, ajoute une commande OAS_GEOREF_INFO 
qui affiche les informations du système de coordonnées actuel dans 
la console AutoCAD.

Utilise le pattern CommandBase avec ExecuteSafe().
```

#### ✅ BON : Corriger un bug

```
La commande OAS_STREETVIEW ne fonctionne pas quand le dessin 
n'a pas de système de coordonnées défini.

Elle devrait :
1. Détecter l'absence de projection
2. Afficher un message traduit demandant de définir une projection
3. Proposer d'ouvrir la fenêtre SetProjection
```

#### ❌ MAUVAIS : Prompt trop vague

```
Fais-moi une commande AutoCAD
```

#### ❌ MAUVAIS : Ignorer l'architecture

```
Ajoute une commande dans Plugin.cs pour dessiner un parking
```
*(Le Core ne doit jamais contenir de logique métier)*

---

## 🎨 Workflow Vibe-Coding recommandé

### 1. Planification (5 min)

```
💭 "Je veux que le module fasse X, Y, Z"
```

Décrivez à Copilot ce que vous voulez accomplir. Laissez-le proposer une structure.

### 2. Génération (10-15 min)

```
🤖 "Génère le module avec les fichiers suivants..."
```

Demandez à Copilot de générer :
- La classe Module (héritant de `ModuleBase`)
- Les commandes (héritant de `CommandBase`)
- Les traductions FR/EN/ES
- Le fichier .csproj

### 3. Validation (5 min)

```
✅ Vérifiez que le code respecte :
- [ ] Préfixe OAS_ sur les commandes
- [ ] ExecuteSafe() dans chaque commande
- [ ] ExecuteInTransaction() pour les modifications
- [ ] Traductions complètes (3 langues)
- [ ] Convention de nommage des calques
```

### 4. Test (10 min)

```bash
dotnet build -c Release
# Puis NETLOAD dans AutoCAD
OAS_HELP  # Vérifier que la commande apparaît
```

### 5. Itération

```
🔄 "La commande fonctionne mais il manque X..."
```

Affinez avec Copilot jusqu'à satisfaction.

---

## 📋 Checklist Vibe-Coding

### Avant de demander à Copilot

- [ ] J'ai ouvert le workspace Open Asphalte dans VS Code
- [ ] J'ai lu les instructions Copilot (`.github/copilot-instructions.md`)
- [ ] Je sais si je travaille sur le Core ou un Module

### Pendant la génération

- [ ] Je fournis un contexte suffisant
- [ ] Je spécifie les contraintes importantes
- [ ] Je demande les traductions (FR/EN/ES)

### Après la génération

- [ ] Le code respecte les conventions de nommage
- [ ] Les commandes ont le préfixe `OAS_`
- [ ] `ExecuteSafe()` est utilisé
- [ ] Les transactions sont correctement gérées
- [ ] Le code compile sans erreur
- [ ] Le module apparaît dans `OAS_HELP`

---

## 🔮 Commandes utiles pour Copilot

### Questions d'architecture

```
"Explique-moi comment fonctionne le ModuleDiscovery"
"Quelle est la différence entre ModuleBase et IModule ?"
"Comment ajouter un nouveau service au Core ?"
```

### Génération de code

```
"Crée une commande [NOM] qui [DESCRIPTION]"
"Ajoute les traductions pour [CLÉS]"
"Génère les tests unitaires pour [CLASSE]"
```

### Débogage

```
"Pourquoi cette commande ne s'affiche pas dans le ruban ?"
"Analyse ce code et trouve les problèmes potentiels"
"Comment corriger l'erreur [MESSAGE]"
```

### Refactoring

```
"Optimise cette méthode de GeometryService"
"Simplifie ce code en utilisant LINQ"
"Sépare cette classe en plusieurs responsabilités"
```

---

## ⚠️ Pièges à éviter

### 1. Ne pas vérifier le code généré

L'IA peut faire des erreurs. **Toujours relire et tester** avant de commit.

### 2. Ignorer l'architecture modulaire

```
❌ "Ajoute cette fonction dans le Core"
✅ "Crée un module séparé pour cette fonctionnalité"
```

### 3. Oublier les traductions

```
❌ Logger.Info("Operation completed");
✅ Logger.Info(T("monmodule.operation.completed"));
```

### 4. Copier-coller sans comprendre

Si vous ne comprenez pas le code généré, **demandez une explication** :
```
"Explique-moi ligne par ligne ce que fait ce code"
```

### 5. Prompts trop longs ou complexes

Divisez les tâches complexes en sous-tâches :
```
1. "Crée d'abord la structure du module"
2. "Maintenant ajoute la première commande"
3. "Ajoute la logique de calcul"
4. "Termine avec les traductions"
```

---

## 📚 Ressources

### Documentation Open Asphalte

- [Guide développeur](../guides/developer_guide.md) — Créer des modules
- [GeometryService](../api/services/GeometryService.md) — API de calculs géométriques
- [LayerService](../api/services/LayerService.md) — API de gestion des calques

### Contextes Copilot spécialisés

- [copilot-instructions-core.md](copilot-instructions-core.md) — Pour modifier le Core
- [copilot-instructions-module.md](copilot-instructions-module.md) — Pour créer des modules

### Liens externes

- [GitHub Copilot Documentation](https://docs.github.com/copilot)
- [VS Code Copilot Extension](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot)
- [AutoCAD .NET API Reference](https://help.autodesk.com/view/OARX/2026/ENU/)

---

## 🎯 Résumé

| Étape | Action |
|-------|--------|
| **1** | Ouvrez le workspace Open Asphalte |
| **2** | Décrivez votre intention clairement |
| **3** | Laissez Copilot générer le code |
| **4** | Vérifiez les conventions (OAS_, ExecuteSafe, traductions) |
| **5** | Compilez et testez dans AutoCAD |
| **6** | Itérez jusqu'à satisfaction |

> **Rappel** : Vous êtes l'expert, l'IA est votre assistant. Elle accélère votre travail, mais vous restez responsable de la qualité finale.

---

*Document créé le 2026-02-04 | Open Asphalte v0.0.1*
