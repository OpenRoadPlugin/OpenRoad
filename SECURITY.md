# Politique de Sécurité

## Avertissement et Limitation de Responsabilité

### Clause de non-responsabilité (Disclaimer)

Open Asphalte est distribué sous **licence GNU GPL v3**. Conformément à cette licence :

> **LE LOGICIEL EST FOURNI "TEL QUEL", SANS GARANTIE D'AUCUNE SORTE, EXPRESSE OU IMPLICITE, Y COMPRIS MAIS SANS S'Y LIMITER AUX GARANTIES DE QUALITÉ MARCHANDE, D'ADÉQUATION À UN USAGE PARTICULIER ET D'ABSENCE DE CONTREFAÇON.**

### Limitation de responsabilité

**En aucun cas, les auteurs, contributeurs ou détenteurs des droits d'auteur d'Open Asphalte ne pourront être tenus responsables de :**

- Tout dommage direct, indirect, accessoire, spécial, exemplaire ou consécutif
- Toute perte de données, de profits, de revenus ou d'opportunités commerciales
- Toute interruption d'activité professionnelle
- Tout préjudice résultant de l'utilisation ou de l'impossibilité d'utiliser le logiciel
- Toute erreur de calcul, de dessin ou de conception générée par le logiciel
- Tout dysfonctionnement ou incompatibilité avec AutoCAD ou d'autres logiciels
- Toute conséquence liée à l'utilisation de modules tiers

**Même si Open Asphalte ou ses contributeurs ont été informés de la possibilité de tels dommages.**

### Utilisation à vos propres risques

- L'utilisateur est **seul responsable** de vérifier l'exactitude des résultats produits
- L'utilisateur doit **toujours sauvegarder** ses données avant d'utiliser le plugin
- L'utilisateur est responsable de la **conformité** de ses projets aux normes en vigueur
- Open Asphalte ne se substitue pas à l'expertise professionnelle d'un ingénieur ou d'un technicien qualifié

---

## Versions supportées

| Version | Supportée          |
| ------- | ------------------ |
| 0.0.x   | :white_check_mark: |

## Signaler une vulnérabilité

Si vous découvrez une vulnérabilité de sécurité dans Open Asphalte, merci de la signaler de manière responsable.

### Comment signaler

1. **Ne créez PAS d'issue publique** pour les vulnérabilités de sécurité
2. Envoyez un rapport détaillé via [GitHub Security Advisories](https://github.com/openasphalteplugin/openasphalte/security/advisories/new)
3. Ou contactez les mainteneurs directement via GitHub

### Informations à inclure

- Description de la vulnérabilité
- Étapes pour reproduire le problème
- Impact potentiel
- Suggestion de correction (si vous en avez une)

### Délai de réponse

- **Accusé de réception** : sous 48 heures
- **Évaluation initiale** : sous 7 jours
- **Correction** : selon la sévérité (critique: 7 jours, haute: 30 jours, moyenne: 90 jours)

### Ce que nous nous engageons à faire

- Confirmer la réception de votre rapport
- Vous tenir informé de l'avancement
- Créditer votre découverte (sauf si vous préférez rester anonyme)
- Ne pas engager de poursuites si le signalement est fait de bonne foi

## Bonnes pratiques de sécurité

### Pour les utilisateurs

- Téléchargez Open Asphalte uniquement depuis les [releases officielles](https://github.com/openasphalteplugin/openasphalte/releases)
- Vérifiez l'intégrité des fichiers téléchargés
- N'installez que des modules provenant de sources fiables

### Pour les développeurs de modules

- Ne stockez jamais de credentials en dur dans le code
- Validez toutes les entrées utilisateur
- Utilisez les services fournis par le Core plutôt que d'accéder directement au système de fichiers
- Évitez l'exécution de code dynamique (`eval`, `Assembly.Load` depuis des sources non fiables)

## Scope

Cette politique couvre :
- Le code source du Core (`OpenAsphalte.Core`)
- Les templates officiels
- La documentation officielle

Les modules tiers ne sont **pas couverts** par cette politique.
