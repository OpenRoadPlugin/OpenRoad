# Module : Cotation entre deux lignes (OAS.Cota2Lign)

Ce module permet de générer automatiquement des cotations alignées entre deux polylignes (ex: bordures de chaussée), ce qui est particulièrement utile pour les plans de voirie et de récolement.

## 📋 Commandes

| Commande | Menu | Description |
|----------|------|-------------|
| `OAS_COTA2LIGN` | Dessin > Cotations | Lance l'outil de cotation automatique |

## 🚀 Utilisation

1. **Lancer la commande** `OAS_COTA2LIGN`.
2. **Configuration (Optionnel)** : Tapez `P` (Paramètres) pour ouvrir la fenêtre de configuration.
3. **Sélectionner la polyligne de référence** (Polyligne 1) : C'est celle sur laquelle les mesures seront basées.
4. **Sélectionner la polyligne cible** (Polyligne 2) : C'est celle vers laquelle les cotes seront tirées.
5. **Définir la zone** : Cliquez un point de départ et un point de fin sur la polyligne de référence.
6. **Résultat** : Les cotations sont créées automatiquement selon vos paramètres.

## ⚙️ Paramètres

Les options suivantes sont configurables via la fenêtre de paramètres (`P` au lancement) :

*   **Interdistance** : Distance entre chaque cotation (ex: 5.0m, 10.0m).
*   **Cotation aux sommets** : Si coché, une cote est créée à chaque sommet de la polyligne de référence, en plus de l'interdistance régulière.
*   **Calque** : Le calque sur lequel placer les cotations (Défaut: `OAS_COTATIONS`).
*   **Style de cote** : Utilise le style de cote courant ou un style spécifique.

## ℹ️ Détails techniques

*   **Identifiant** : `cota2lign`
*   **Dépendances** : Aucune (DynamicSnap optionnel)
*   **Version** : 1.0.0
