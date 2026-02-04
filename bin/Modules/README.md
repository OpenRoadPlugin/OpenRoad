# Dossier des Modules Open Road

Ce dossier est destiné à contenir les modules Open Road (fichiers .dll).

## Comment ça fonctionne

Au démarrage, Open Road scanne automatiquement ce dossier et charge tous les fichiers OpenRoad.*.dll trouvés. Les modules découverts apparaîtront automatiquement dans le menu et le ruban d'AutoCAD (interface localisée selon la langue).

## Installation d'un module

1. Téléchargez le fichier .dll du module
2. Placez-le dans ce dossier
3. Redémarrez AutoCAD

## Suppression d'un module

1. Fermez AutoCAD
2. Supprimez le fichier .dll du module
3. Relancez AutoCAD

Le module disparaîtra complètement de l'interface.

## Créer vos propres modules

Consultez le [Guide développeur](../../docs/guides/developer_guide.md) dans la documentation.

## Notes importantes

- Les DLL **doivent** être nommées OpenRoad.*.dll pour être détectées
- Chaque module doit référencer OpenRoad.Core.dll (sans le copier)
- Les modules sont découverts et chargés **automatiquement** au démarrage
- L'interface et les logs du Core sont **multilingues** (FR/EN/ES)
- Si un module a des dépendances manquantes, il ne sera pas chargé
