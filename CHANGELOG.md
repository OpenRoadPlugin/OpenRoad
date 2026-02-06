# Changelog

Toutes les modifications notables de ce projet seront document�es dans ce fichier.

Le format est bas� sur [Keep a Changelog](https://keepachangelog.com/fr/1.0.0/),
et ce projet adh�re au [Semantic Versioning](https://semver.org/lang/fr/).

## [0.0.3] - 2026-02-06

### Modifications CORE
- **WindowStateHelper** : Nouveau helper statique pour la persistance de taille/position des fen�tres WPF
  - `RestoreState()` / `SaveState()` pour m�moriser entre les sessions
  - D�tection automatique si la fen�tre est hors �cran ? recentrage
  - `GetRecommendedHeight()` retourne 95% de la zone de travail (respecte barre des t�ches)
  - R�utilisable par tous les modules
- **Fen�tre Param�tres (`OAS_SETTINGS`)** :
  - Fen�tre agrandie (900x600) et redimensionnable
  - M�morisation de la taille et position via `WindowStateHelper`
  - Affichage du "Vrai Nom" des modules install�s (depuis la DLL) au lieu de l'ID du catalogue

### S�curit�
- **Isolation des modules personnalis�s** : Les modules provenant d'une source personnalis�e (dossier local ou URL) ne sont plus charg�s directement depuis leur emplacement distant
  - Seuls les modules pr�sents dans `%LOCALAPPDATA%\Autodesk\ApplicationPlugins\OAS.bundle\Contents\Modules\` sont charg�s par le Core
  - Le bouton "Installer" copie les modules dans ce dossier avant utilisation
  - Correction d'une faille permettant l'ex�cution de code arbitraire depuis une source externe

### Module PrezOrganizer (Organiseur de Pr�sentations)

#### Ajout�
- **Interface adaptative** :
  - Hauteur fen�tre � 95% de l'�cran par d�faut (respect barre des t�ches/menu D�marrer)
  - Persistance taille et position de la fen�tre entre les sessions
  - Remise au centre automatique si la fen�tre serait hors �cran
  - ScrollViewer sur la barre d'outils pour s'adapter � la hauteur disponible
- **Outil de renommage unifi�** (`RenameToolDialog`) :
  - Mode Pr�fixe/Suffixe : ajouter du texte au d�but ou � la fin
  - Mode Pattern avec variables `{N}`, `{N:00}`, `{ORIG}`, `{DATE}`
  - Num�ro de d�part et incr�ment configurables
  - Port�e : s�lection ou toutes les pr�sentations
  - Aper�u en temps r�el des modifications

#### Corrig�
- **TabOrder non appliqu�** : Le r�ordonnancement par glisser-d�poser et boutons fonctionne maintenant correctement
  - Correction via algorithme en 2 passes (valeurs temporaires hautes puis valeurs finales)
- **Fusion des dialogues** : Les anciens dialogues `PrefixSuffixDialog` et `BatchRenameDialog` sont remplac�s par `RenameToolDialog`

#### Supprim�
- **Mode "Appliquer imm�diatement"** : Toutes les modifications passent par le bouton Valider (comportement plus pr�visible)
- Fichiers obsol�tes :
  - `PrefixSuffixDialog.xaml` / `.xaml.cs`
  - `BatchRenameDialog.xaml` / `.xaml.cs`

## [0.0.2] - 2026-02-06

### Modifications CORE
- **V�rification des mises � jour au d�marrage** : Nouvelle fonctionnalit� qui v�rifie automatiquement si une nouvelle version est disponible via l'API GitHub.
  - Comparaison de version avec la derni�re release
  - Contr�le de compatibilit� AutoCAD (v�rifie que la version minimale AutoCAD est respect�e)
  - Notification non-bloquante proposant d'ouvrir la page de t�l�chargement
  - D�sactivable dans les param�tres (`checkUpdatesOnStartup`)
- **Modules Personnalis�s** : Ajout d'une source personnalisable (Dossier local ou URL) pour installer des modules priv�s/beta.
- **UI Modules** : Identification visuelle des modules "Custom" (texte violet/gras) et tri prioritaire.
- **Changement de nom** pour empecher toute confusion avec des soft existants.
    - Open Road s'appelle maintenant **Open Asphalte**
- **Personnalisation du menu** : L'installateur permet de d�finir un nom personnalis� (ex: "MonEntreprise - OA")
- **Page de licence** : Acceptation obligatoire de la licence GNU GPL v3 dans l'installateur
- **Gestionnaire de modules am�lior�** :
  - Auto-refresh de la liste lors de l'arriv�e sur l'onglet Modules
  - Gestion automatique des d�pendances (t�l�chargement r�cursif)
  - Multi-s�lection avec checkboxes pour installation par lot
  - Groupement des modules par cat�gorie
  - Message au d�marrage si aucun module install�
  - **D�sinstallation des modules** :
    - Bouton "D�sinstaller" pour les modules install�s
    - Renommage s�curis� en `.del` pour contourner le verrouillage fichier Windows
    - Nettoyage automatique des fichiers supprim�s au d�marrage suivant
    - V�rification des d�pendances invers�es (emp�che de supprimer un module requis par un autre)
- **Traductions** : Nouvelles cl�s FR/EN/ES pour le gestionnaire de modules
- **CI/CD** : Workflow GitHub Actions pour validation des PRs
- **Thread Safety** : Configuration utilise maintenant `ConcurrentDictionary` pour les acc�s concurrents
- **Gestion m�moire** : 
  - HttpClient avec `PooledConnectionLifetime` pour �viter les probl�mes DNS stale
  - D�sabonnement du handler `FirstChanceException` dans `Terminate()`
  - Nettoyage des caches ribbon lors du rebuild
- **OasStyles.xaml** : Nouveau `ResourceDictionary` WPF partag� (palette de couleurs `Oas*`, styles de champs/boutons/labels, brushes fig�s avec `Freeze()`)
- **Thread safety renforc�** : Utilisation de `volatile` et `Interlocked` dans le Core en compl�ment du `ConcurrentDictionary`
- **Logger optimis�** : `StreamWriter` persistant pour de meilleures performances d'�criture
- **Tooltips localis�s** : Tous les tooltips cod�s en dur dans Cota2Lign, DynamicSnap et SetProjection sont maintenant traduits via `L10n.T()`
- **Freeze() WPF** : Appel syst�matique de `Freeze()` sur les `SolidColorBrush` pour les performances

### Refactoris�
- **GeometryService** : R�organisation en 5 fichiers partiels (`partial class`) � Intersections, Voirie, Hydraulics, Earthwork
- **CoordinateService** : R�organisation en 3 fichiers partiels � ProjectionData, Transformations
- **GeoLocationService** (module G�or�f�rencement) : R�organisation en 2 fichiers partiels � Conversions

### Corrig�
- Bug TFormat avec placeholders {0} {1} non remplac�s dans les messages de d�pendances (surcharge ambigu�)
- Logo dans la fen�tre "� propos" : adapt� au nouveau format rectangulaire (200x105)
- Modules non d�tect�s apr�s installation : pr�fixe de fichier corrig� (`OAS.*.dll` au lieu de `OpenRoad.*.dll`) + migration automatique des anciens modules
- D�pendances non affich�es comme install�es : correction du binding WPF avec `INotifyPropertyChanged`
- Onglet Modules de Settings non rafra�chi : ajout du chargement automatique � l'ouverture directe
- Nom du menu persistant apr�s d�sinstallation : suppression du fichier `config.json` lors de la d�sinstallation
- **Traductions manquantes** : Ajout des cl�s `core.credits.*`, `about.credits`, `settings.tab.*`, `settings.modules.*` pour FR/EN/ES
- **Patterns DLL obsol�tes** : Correction de `OpenRoad.*.dll` ? `OAS.*.dll` dans SettingsWindow et ModuleManagerWindow
- **Caract�re invalide** : Correction `?` ? `?` dans SetProjectionWindow (limites de projection)
- **Internationalisation modules** :
  - Cota2Lign : MessageBox de validation utilisent maintenant les traductions
  - DynamicSnap : `GetDisplayName()` utilise le syst�me de localisation
  - StreetView : Prompt Oui/Non utilise les traductions `common.yes`/`common.no`
- **Race condition Configuration.Load()** : Toute la logique de chargement est maintenant dans le bloc lock pour �viter les acc�s concurrents
- **Exception masqu�e dans CommandBase** : `tr.Abort()` est maintenant wrapp� dans try-catch pour pr�server l'exception originale
- **Nommage obsol�te** : Renomm� `openroad_*.log.old` ? `openasphalte_*.log.old` et `OpenRoad_Setup.exe` ? `OpenAsphalte_Setup.exe`
- **Empty catch blocks** : Ajout de commentaires explicatifs dans SnapDetector.cs pour les catch vides
- **Traductions XAML Cota2Lign** : Labels "Interdistance", "D�calage", "Calque" maintenant traduisibles (FR/EN/ES)
- **Traductions XAML SetProjection** : Labels "Syst�me actuel", "D�tect�", "D�tails", etc. maintenant traduisibles (FR/EN/ES)
- **Fuite d'�v�nement update** : Correction de la fuite lors de la d�sinscription des �v�nements de mise � jour
- **CommandBase.Translate** : Correction du bug de traduction dans `CommandBase`

### Supprim�
- **Code mort** : Extensions SnapMode, propri�t�s mortes SnapConfiguration, m�thode morte SnapHelper, m�thodes mortes GeoLocationService, imports inutilis�s
- Fichiers obsol�tes du renommage OpenRoad ? OpenAsphalte :
  - `src/OpenRoad.Core/` (dossier entier)
  - `src/OpenRoad.sln`
  - `installer/OpenRoad.iss`
  - `templates/OpenRoad.Module.Template.csproj`

### Documentation
- Correction des chemins `OpenAsphalte.Core` ? `OAS.Core` dans CONTRIBUTING.md
- Correction de la r�f�rence `OAS_ABOUT` ? `OAS_VERSION` dans CONTRIBUTING.md
- Correction de la typo `oirie` ? `voirie` dans CONTRIBUTING.md
- Mise � jour des exemples de modules dans README.md (modules r�els)
- Ajout des modules `cota2lign` et `dynamicsnap` dans marketplace.json

### Nouveaux modules
- Module de Cotations entre 2 polylignes
- Module d'acrochage intelligent (snap am�lior�) pour les modules OAS

---

## [0.0.1] - 2026-02-04

### Ajout�
- **Architecture modulaire** : Syst�me de d�couverte automatique des modules (DLL)
- **Interface dynamique** : Menu et ruban g�n�r�s automatiquement selon les modules install�s
- **Syst�me de localisation** : Support multilingue (Fran�ais, English, Espa�ol)
- **Services partag�s** :
  - GeometryService : Calculs g�om�triques (distances, angles, interpolation)
  - LayerService : Gestion des calques AutoCAD

- **Commandes syst�me** :
  - OAS_HELP : Liste des commandes disponibles
  - OAS_VERSION : Informations de version et modules charg�s
  - OAS_SETTINGS : Fen�tre de param�tres utilisateur
  - OAS_RELOAD : Rechargement de la configuration
  - OAS_UPDATE : V�rification des mises � jour
- **Classes de base pour modules** :
  - ModuleBase : Classe abstraite pour cr�er des modules
  - CommandBase : Classe de base pour les commandes avec gestion des erreurs
  - CommandInfoAttribute : M�tadonn�es pour l'affichage UI
- **Configuration utilisateur** : Stockage JSON dans AppData
- **Logging unifi�** : Messages format�s dans la console AutoCAD
- **Templates** : Fichiers mod�les pour cr�er de nouveaux modules
- **Documentation** : README, DEVELOPER.md, CONTRIBUTING.md

### Technique
- Cible : AutoCAD 2025+ (.NET 8.0)
- D�tection automatique du chemin d'installation AutoCAD
- Gestion propre du cycle de vie (Initialize/Terminate)
- Lib�ration correcte des objets COM (menus)

---

## Types de changements

- Ajout� pour les nouvelles fonctionnalit�s
- Modifi� pour les changements dans les fonctionnalit�s existantes
- D�pr�ci� pour les fonctionnalit�s qui seront supprim�es prochainement
- Supprim� pour les fonctionnalit�s supprim�es
- Corrig� pour les corrections de bugs
- S�curit� pour les vuln�rabilit�s corrig�es
