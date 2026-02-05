# Modules Open Asphalte

La force d'Open Asphalte réside dans sa modularité. Voici la liste des modules officiels disponibles :

## 🏗️ Modules Métier

Ces modules ajoutent des fonctionnalités directement utilisables pour la production.

*   **[Géoréférencement](georeferencement.md) (`setprojection`)**
    *   Configuration du système de coordonnées (Lambert 93, Coniques...).
    *   Transformation de coordonnées.
    *   Insertion de grilles dynamiques.

*   **[Cotation Linéaire](cota2lign.md) (`cota2lign`)**
    *   Cotation automatique entre deux polylignes (bords de chaussée).
    *   Gestion des interdistances et styles.

*   **[Google Street View](streetview.md) (`streetview`)**
    *   Ouverture dynamique de Street View depuis une position dans AutoCAD.
    *   Synchronisation vue/plan.

## 🔧 Modules Système

Ces modules fournissent des services aux autres modules ou au noyau.

*   **[Accrochage Dynamique](dynamicsnap.md) (`dynamicsnap`)**
    *   Bibliothèque de visualisation et d'accrochage intelligent.
    *   Utilisé par les modules Cota2Lign et autres outils de saisie.

---

## 📦 Installation

Pour installer un module :
1. Utilisez le gestionnaire de modules via la commande `OAS_MODULES`, ou
2. Copiez le fichier `.dll` correspondant (ex: `OAS.Georeferencement.dll`) dans le dossier `Modules/`.
3. Redémarrez AutoCAD.
