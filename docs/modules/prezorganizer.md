# Module : Organiseur de Présentations (OAS.PrezOrganizer)

L'Organiseur de Présentations est un module puissant conçu pour faciliter la gestion des onglets de présentation (Layouts) dans AutoCAD. Il offre une interface centralisée pour renommer, trier et réorganiser rapidement un grand nombre de mises en page sans avoir à manipuler les onglets un par un en bas de l'écran.

## 📋 Commandes

| Commande | Menu | Description |
|----------|------|-------------|
| `OAS_PREZORG` | Mise en page | Ouvre l'interface de gestion des présentations |

## 🚀 Fonctionnalités principales

L'interface graphique dédiée vous permet d'effectuer les opérations suivantes sur vos mises en page :

### 1. Réorganisation des onglets
*   **Déplacement manuel** : Utilisez les boutons "Monter", "Descendre", "Tout en haut", "Tout en bas" pour ajuster l'ordre l'manuellement.
*   **Glisser-Déposer** : Réorganisez la liste intuitivement à la souris.
*   **Tri automatique** :
    *   **Alphabétique** : Trie les noms de A à Z.
    *   **Numérique (Smart Sort)** : Trie intelligemment les nombres (ex: *Layout 1, Layout 2, Layout 10* au lieu de *1, 10, 2*).
    *   **Architectural** : Tri spécifique respectant les conventions courantes.

### 2. Renommage avancé (Batch Rename)
*   **Modèles de nommage** : Appliquez un modèle à toutes les présentations sélectionnées.
    *   `{N}` : Compteur incrémental (ex: 01, 02...).
    *   `{ORIG}` : Conserve le nom original.
    *   `{DATE}` : Date actuelle (format AA-MM-JJ).
*   **Préfixe / Suffixe** : Ajoutez rapidement du texte au début ou à la fin d'une série d'onglets.
*   **Rechercher / Remplacer** : Remplacez des chaînes de caractères dans tous les onglets sélectionnés.
*   **Gestion de la casse** : Convertissez les noms en MAJUSCULES, minuscules ou Titre.

### 3. Outils de gestion
*   **Création** : Ajoutez rapidement de nouvelles présentations blanches.
*   **Duplication** : Copiez une présentation existante (avec tout son contenu et ses réglages de mise en page).
*   **Suppression** : Supprimez des onglets par lots (sécurisé, impossible de supprimer la dernière présentation restante).

### 4. Filtrage et Recherche
*   Une barre de recherche permet de filtrer instantanément la liste pour ne travailler que sur un sous-groupe de présentations.
*   Le compteur en bas de fenêtre indique le nombre total et le nombre de présentations sélectionnées.

## ⚙️ Utilisation

1.  Lancez la commande **`OAS_PREZORG`** dans AutoCAD (ou via le ruban *Mise en page*).
2.  Une fenêtre s'ouvre listant toutes les présentations du fichier `.dwg` actuel.
3.  **Sélectionnez** une ou plusieurs lignes (Ctrl+Clic ou Maj+Clic).
4.  Utilisez les panneaux à droite pour **modifier** ou **déplacer** votre sélection.
5.  Les modifications sont généralement appliquées immédiatement et supportent le `CTRL+Z` (Undo) global d'AutoCAD après fermeture de la commande.

---
*Note : Ce module est essentiel pour les projets contenant des dizaines de carnets de détails ou de profils en travers.*
