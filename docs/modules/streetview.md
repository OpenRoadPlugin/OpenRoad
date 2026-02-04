# Module Street View

Module permettant d'ouvrir Google Street View depuis un point sélectionné dans un dessin géoréférencé AutoCAD.

## Informations

| Propriété | Valeur |
|-----------|--------|
| **ID** | `streetview` |
| **Version** | 0.0.1 |
| **Auteur** | Charles TILLY |
| **Dépendances** | `setprojection` |
| **Version Core minimale** | 0.0.1 |

## Description

Ce module permet d'ouvrir Google Street View dans le navigateur web par défaut à partir d'un point sélectionné dans le dessin AutoCAD. Il nécessite que le dessin soit géoréférencé (système de coordonnées défini).

## Dépendances

Ce module **dépend** du module "setprojection" (Géoréférencement) pour :
- Récupérer le système de coordonnées actuel du dessin
- Convertir les coordonnées projetées vers WGS84 (latitude/longitude)
- Accéder à la fenêtre de définition de projection si nécessaire

## Commandes

### OR_STREETVIEW

Ouvre Google Street View depuis un point du dessin.

| Propriété | Valeur |
|-----------|--------|
| **Nom** | Street View |
| **Groupe** | Cartographie |
| **Taille ruban** | Large |

**Fonctionnement :**

1. **Vérification de la projection** : La commande vérifie si un système de coordonnées est défini
2. **Sélection du point de vue** : L'utilisateur clique sur la position de l'observateur
3. **Sélection de la direction** : L'utilisateur clique vers où l'observateur regarde
4. **Conversion WGS84** : Les coordonnées sont converties en latitude/longitude
5. **Ouverture** : Google Street View s'ouvre dans le navigateur

**Messages utilisateur :**
- Si aucune projection n'est définie, propose d'en définir une
- Affiche les coordonnées locales et WGS84 dans la console

## Traductions

Le module supporte les langues :
- 🇫🇷 Français (fr)
- 🇬🇧 English (en)  
- 🇪🇸 Español (es)

## Prérequis

- AutoCAD 2026+
- Module Géoréférencement installé
- Dessin géoréférencé (système de coordonnées défini)
- Connexion Internet pour ouvrir Street View

## Exemple d'utilisation

```
Commande: OR_STREETVIEW
Sélectionnez le point de vue: [clic]
Sélectionnez la direction: [clic]
> Coordonnées WGS84: Lat=48.858844°, Lon=2.294351°
> Direction: 45.0°
> Street View ouvert dans le navigateur
```
