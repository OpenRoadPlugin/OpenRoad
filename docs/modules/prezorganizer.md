# Module : Organiseur de Présentations (OAS.PrezOrganizer)

L'Organiseur de Présentations est un module puissant conçu pour faciliter la gestion des onglets de présentation (Layouts) dans AutoCAD. Il offre une interface centralisée pour renommer, trier et réorganiser rapidement un grand nombre de mises en page sans avoir à manipuler les onglets un par un en bas de l'écran.

## 📋 Commandes

| Commande | Menu | Description |
|----------|------|-------------|
| `OAS_PREZORG` | Mise en page | Ouvre l'interface de gestion des présentations |

## 🚀 Fonctionnalités principales

L'interface graphique dédiée vous permet d'effectuer les opérations suivantes sur vos mises en page :

### 1. Réorganisation des onglets
*   **Déplacement manuel** : Utilisez les boutons "Monter", "Descendre", "Tout en haut", "Tout en bas" pour ajuster l'ordre manuellement.
*   **Glisser-Déposer** : Réorganisez la liste intuitivement à la souris.
*   **Tri automatique** :
    *   **Alphabétique** : Trie les noms de A à Z.
    *   **Numérique (Smart Sort)** : Trie intelligemment les nombres (ex: *Layout 1, Layout 2, Layout 10* au lieu de *1, 10, 2*).
    *   **Architectural** : Tri spécifique respectant les conventions courantes.

### 2. Outil de Renommage Unifié
Accessible via le bouton **"Renommer"**, cet outil offre deux modes de travail :

#### Mode Préfixe / Suffixe
*   Ajoutez du texte au début (préfixe) ou à la fin (suffixe) des noms de présentations.
*   Idéal pour préfixer rapidement une série (*"Phase1_"*, *"Client_"*).

#### Mode Pattern (Modèle)
*   `{N}` : Compteur incrémental simple (1, 2, 3...).
*   `{N:00}` : Compteur formaté (01, 02, 03...).
*   `{ORIG}` : Conserve le nom original de la présentation.
*   `{DATE}` : Date actuelle (format AA-MM-JJ).

Exemple : Pattern `P{N:00}-{ORIG}` sur "PlanA, PlanB" → "P01-PlanA, P02-PlanB"

#### Options communes
*   **Numéro de départ** : Définit le premier numéro du compteur.
*   **Incrément** : Pas entre chaque numéro.
*   **Portée** : Appliquer à la sélection uniquement ou à toutes les présentations.
*   **Aperçu en temps réel** : Visualisez le résultat avant validation.

### 3. Autres outils de renommage
*   **Rechercher / Remplacer** : Remplacez des chaînes de caractères dans tous les onglets sélectionnés.
*   **Gestion de la casse** : Convertissez les noms en MAJUSCULES, minuscules ou Titre.

### 4. Outils de gestion
*   **Création** : Ajoutez rapidement de nouvelles présentations blanches.
*   **Duplication** : Copiez une présentation existante (avec tout son contenu et ses réglages de mise en page).
*   **Suppression** : Supprimez des onglets par lots (sécurisé, impossible de supprimer la dernière présentation restante).

### 5. Filtrage et Recherche
*   Une barre de recherche permet de filtrer instantanément la liste pour ne travailler que sur un sous-groupe de présentations.
*   Le compteur en bas de fenêtre indique le nombre total et le nombre de présentations sélectionnées.

## 🪟 Interface Adaptative

*   **Taille mémorisée** : La fenêtre conserve sa taille et sa position entre les sessions.
*   **Hauteur optimisée** : Par défaut, la fenêtre s'ouvre à 95% de la hauteur disponible (respectant la barre des tâches et le menu Démarrer).
*   **Remise à zéro automatique** : Si la fenêtre se retrouve hors écran (changement de moniteur), elle revient automatiquement au centre.
*   **Barre d'outils adaptable** : Les boutons de la barre de droite s'adaptent à la hauteur disponible via un ScrollViewer.

## ⚙️ Utilisation

1.  Lancez la commande **`OAS_PREZORG`** dans AutoCAD (ou via le ruban *Mise en page*).
2.  Une fenêtre s'ouvre listant toutes les présentations du fichier `.dwg` actuel.
3.  **Sélectionnez** une ou plusieurs lignes (Ctrl+Clic ou Maj+Clic).
4.  Utilisez les panneaux à droite pour **modifier** ou **déplacer** votre sélection.
5.  Cliquez sur **Valider** pour appliquer les modifications.
6.  Les modifications supportent le `CTRL+Z` (Undo) global d'AutoCAD après fermeture de la commande.

---
*Note : Ce module est essentiel pour les projets contenant des dizaines de carnets de détails ou de profils en travers.*
