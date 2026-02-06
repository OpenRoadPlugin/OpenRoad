# 📚 Documentation Open Asphalte

> Documentation technique pour les développeurs de modules Open Asphalte

---

## 🗂️ Structure de la documentation

```
docs/
├── README.md                          # Ce fichier (index)
├── api/                               # Référence API
│   └── services/                      # Services du Core
│       ├── GeometryService.md         # Calculs géométriques, voirie, hydraulique
│       ├── LayerService.md            # Gestion des calques
│       ├── CoordinateService.md       # Conversions de coordonnées
│       ├── UpdateService.md           # Vérification des mises à jour
│       ├── UrlValidationService.md    # Validation sécurisée des URLs
│       ├── Logger.md                  # Logs AutoCAD et fichier
│       ├── Configuration.md           # Paramètres utilisateur
│       └── Localization.md            # Traductions FR/EN/ES
├── guides/                            # Guides pratiques
│   └── developer_guide.md             # Guide complet du développeur
├── copilot/                           # Contextes IA et Vibe-Coding
│   ├── VIBE_CODING_GUIDE.md           # Guide Vibe-Coding avec Copilot
│   ├── copilot-instructions-core.md   # Pour le développement du Core
│   └── copilot-instructions-module.md # Pour le développement de Modules
├── modules/                           # Documentation des modules
│   ├── georeferencement.md            # Module Géoréférencement
│   └── streetview.md                  # Module Street View
└── architecture/                      # Architecture technique
    └── overview.md                    # Vue d'ensemble
```

---

## 🚀 Démarrage rapide

### Pour les développeurs de modules

1. **Lire le guide Vibe-Coding** : [copilot/VIBE_CODING_GUIDE.md](copilot/VIBE_CODING_GUIDE.md)
2. **Lire le contexte IA** : [../.github/copilot-instructions.md](../.github/copilot-instructions.md)
3. **Comprendre les services** : [api/services/](api/services/)
4. **Utiliser les templates** : [../templates/](../templates/)

---

## 🧩 Référence API

### Services disponibles

| Service | Description | Documentation |
|---------|-------------|---------------|
| **GeometryService** | Calculs géométriques, voirie, hydraulique, cubature | [📄 Voir](api/services/GeometryService.md) |
| **LayerService** | Création et gestion des calques AutoCAD | [📄 Voir](api/services/LayerService.md) |
| **CoordinateService** | Conversions de coordonnées projetées/WGS84 | [📄 Voir](api/services/CoordinateService.md) |
| **UpdateService** | Vérification des mises à jour | [📄 Voir](api/services/UpdateService.md) |
| **UrlValidationService** | Validation sécurisée des URLs | [📄 Voir](api/services/UrlValidationService.md) |
| **Logger** | Logs dans la console AutoCAD et fichier | [📄 Voir](api/services/Logger.md) |
| **Configuration** | Paramètres utilisateur (JSON) | [📄 Voir](api/services/Configuration.md) |
| **Localization** | Traductions FR/EN/ES | [📄 Voir](api/services/Localization.md) |

---

## 📖 Guides

| Guide | Description |
|-------|-------------|
| [Guide développeur](guides/developer_guide.md) | Création de modules, conventions et compilation |
| [Vibe-Coding](copilot/VIBE_CODING_GUIDE.md) | Bonnes pratiques Copilot et prompting |

---

## 🔗 Liens utiles

- **Repository GitHub** : [Open Asphalte](https://github.com/openasphalteplugin/openasphalte)
- **Changelog** : [CHANGELOG.md](../CHANGELOG.md)
- **Contributing** : [CONTRIBUTING.md](../CONTRIBUTING.md)
- **License** : GNU GPL v3

---

*Documentation Open Asphalte v0.0.1*
