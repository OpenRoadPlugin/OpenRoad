# Module Géoréférencement

> Module officiel Open Asphalte pour la gestion des systèmes de coordonnées dans AutoCAD

## Informations

| Propriété | Valeur |
|-----------|--------|
| **ID** | `setprojection` |
| **Version** | 0.0.1 |
| **Auteur** | Charles TILLY |
| **Contributeurs** | Open Asphalte Community |
| **Catégorie** | Cartographie |
| **Dépendances** | Aucune |

## Description

Le module **Géoréférencement** permet de définir et gérer les systèmes de projection pour les dessins AutoCAD. Il utilise l'API `GeoLocationData` native d'AutoCAD pour une compatibilité complète avec Bing Maps et la géolocalisation.

### Fonctionnalités principales

- **Sélection de projection** : Interface graphique avec recherche et filtrage
- **Détection automatique** : Analyse les coordonnées du dessin pour suggérer une projection probable
- **Base de données étendue** : Plus de 40 systèmes de projection (France, Belgique, Suisse, etc.)
- **Suppression du système** : Permet de retirer le système de coordonnées du dessin

## Commandes

### OAS_GEOREF_SETPROJECTION

Ouvre la fenêtre de sélection du système de coordonnées.

**Utilisation** : Tapez `OAS_GEOREF_SETPROJECTION` dans la ligne de commande AutoCAD ou utilisez le menu/ruban Open Asphalte.

**Interface** :
- **Barre de recherche** : Filtrez par nom, code, pays ou région
- **Liste des projections** : Double-cliquez pour appliquer directement
- **Panneau de détails** : Affiche les informations de la projection sélectionnée
- **Système actuel** : Indique le système de coordonnées actuellement défini
- **Système détecté** : Suggestion basée sur l'analyse des coordonnées du dessin

## Projections supportées

### France

| Code | Nom | EPSG |
|------|-----|------|
| Lambert-93 | RGF93 Lambert 93 | 2154 |
| CC42 | RGF93 CC Zone 42 | 3942 |
| CC43 | RGF93 CC Zone 43 | 3943 |
| CC44 | RGF93 CC Zone 44 | 3944 |
| CC45 | RGF93 CC Zone 45 | 3945 |
| CC46 | RGF93 CC Zone 46 | 3946 |
| CC47 | RGF93 CC Zone 47 | 3947 |
| CC48 | RGF93 CC Zone 48 | 3948 |
| CC49 | RGF93 CC Zone 49 | 3949 |
| CC50 | RGF93 CC Zone 50 | 3950 |
| NTF-Lambert-I | NTF Lambert Zone I | 27561 |
| NTF-Lambert-II | NTF Lambert Zone II Étendu | 27572 |
| NTF-Lambert-III | NTF Lambert Zone III | 27573 |
| NTF-Lambert-IV | NTF Lambert Zone IV | 27574 |

### DOM-TOM

| Code | Nom | EPSG |
|------|-----|------|
| RGFG95-UTM22N | RGFG95 UTM Zone 22N (Guyane) | 2972 |
| UTM20N-WGS84 | UTM Zone 20N WGS84 (Antilles) | 32620 |
| RGR92-UTM40S | RGR92 UTM Zone 40S (Réunion) | 2975 |
| RGSPM06-UTM21N | RGSPM06 UTM Zone 21N (St-Pierre-et-Miquelon) | 4467 |
| RGM04-UTM38S | RGM04 UTM Zone 38S (Mayotte) | 4471 |

### Belgique

| Code | Nom | EPSG |
|------|-----|------|
| Belgian-Lambert-2008 | Belgian Lambert 2008 | 3812 |
| Belgian-Lambert-72 | Belgian Lambert 72 | 31370 |

### Suisse

| Code | Nom | EPSG |
|------|-----|------|
| CH1903+-LV95 | CH1903+ / LV95 | 2056 |
| CH1903-LV03 | CH1903 / LV03 | 21781 |

### Luxembourg

| Code | Nom | EPSG |
|------|-----|------|
| LUREF | LUREF / Luxembourg TM | 2169 |

### Zones UTM

| Code | Nom | EPSG |
|------|-----|------|
| UTM30N-WGS84 | UTM Zone 30N WGS84 | 32630 |
| UTM31N-WGS84 | UTM Zone 31N WGS84 | 32631 |
| UTM32N-WGS84 | UTM Zone 32N WGS84 | 32632 |
| UTM33N-WGS84 | UTM Zone 33N WGS84 | 32633 |

## Détection automatique

Le module analyse les coordonnées moyennes des objets du dessin pour suggérer une projection probable.

**Algorithme** :
1. Collecte les coordonnées de tous les points, lignes, polylignes et autres entités
2. Ignore les points dont la distance à l'origine est inférieure à 1000 m (seuil configurable)
3. Calcule les coordonnées moyennes (X, Y)
4. Compare avec les limites géographiques de chaque projection
5. Retourne la projection la plus probable

**Note** : Si le dessin contient peu d'objets ou des coordonnées proches de l'origine, la détection peut ne pas fonctionner.

## Intégration avec le Core

Le module utilise le service `CoordinateService` du Core Open Asphalte qui fournit :

- Conversions Lambert 93 <-> WGS84
- Conversions CC (Coniques Conformes) -> WGS84
- Conversions UTM <-> WGS84
- Base de données des projections (`projections.json`)
- Algorithme de détection automatique

## Installation

1. Placez `OAS.Georeferencement.dll` dans le dossier `bin/Modules/`
2. Redémarrez AutoCAD ou rechargez le plugin avec `OAS_RELOAD`
3. Le module apparaîtra automatiquement dans le menu et le ruban Open Asphalte

## Historique des versions

### v0.0.1 (2026)
- Version initiale
- Interface de sélection avec recherche
- Détection automatique de projection
- Support de 40+ projections
- Traductions FR/EN/ES

## Captures d'écran

*A venir*

## Voir aussi

- [Guide du développeur](../guides/developer_guide.md)
- [CoordinateService](../api/services/CoordinateService.md)
- [Architecture des modules](../architecture/overview.md)
