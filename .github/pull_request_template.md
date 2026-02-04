## Description

<!-- Décrivez brièvement vos modifications -->

## Type de changement

- [ ] 🐛 Bug fix (correction qui ne casse pas la compatibilité)
- [ ] ✨ Nouvelle fonctionnalité (ajout qui ne casse pas la compatibilité)
- [ ] 📦 Nouveau module
- [ ] 💥 Breaking change (modification qui casse la compatibilité)
- [ ] 📝 Documentation
- [ ] 🔧 Configuration/CI

## Checklist

### Pour tous les changements
- [ ] Mon code compile sans erreur (`dotnet build -c Release`)
- [ ] J'ai testé mes modifications dans AutoCAD
- [ ] J'ai mis à jour la documentation si nécessaire

### Pour les modules
- [ ] Mon module hérite de `ModuleBase`
- [ ] Mes commandes héritent de `CommandBase`
- [ ] J'utilise `ExecuteSafe()` dans toutes mes commandes
- [ ] J'ai fourni les traductions FR, EN, ES
- [ ] Mes commandes sont préfixées par `OR_`

### Pour les modifications du Core ⚠️
- [ ] J'ai une raison valable de modifier le Core
- [ ] Mes modifications ne cassent pas les modules existants
- [ ] J'ai discuté de ces changements avec les maintainers

## Tests effectués

<!-- Décrivez comment vous avez testé vos modifications -->

## Captures d'écran (si applicable)

<!-- Ajoutez des captures d'écran pour illustrer vos modifications -->

## Crédits (optionnel)

<!-- Si vous souhaitez être crédité dans le programme, remplissez cette section -->
<!-- Laissez vide si vous préférez contribuer anonymement -->

- **Nom/Pseudo** : 
- **Site web** : <!-- URL vers votre site personnel ou portfolio (optionnel) -->
- **GitHub** : <!-- Votre profil GitHub sera utilisé par défaut -->
- **Rôle** : <!-- Développeur / Testeur / Traducteur / Autre -->
