# mRemoteNG — Refonte « Modern UI »

Branche : `feature/modern-ui` (fork `Gus0711/mRemoteNG`), basée sur `v1.78.2-dev`.
Usage personnel : refonte complète de l'interface dans un style Windows 11 / Fluent.

## Ce qui change

| Zone | Nouveautés |
|---|---|
| Thèmes | **Modern Dark** (par défaut) et **Modern Light** (`Themes/ModernDark.vstheme`, `ModernLight.vstheme`) : accent indigo `#5E6AD2`, surfaces en couches. Bascule automatique vers Modern Dark au premier lancement (setting `ModernUiMigrated`), le choix est ensuite conservé. |
| Barre de titre | Couleur fondue dans l'interface + coins arrondis (DWM, Windows 11). |
| Barre du haut | Menu + connexion rapide (champ arrondi, sélecteur de protocole, bouton Connexion accent, historique). Menus arrondis, icônes Fluent vectorielles. |
| Onglets / panneaux | Onglets façon navigateur (accent sur l'onglet actif, × au survol, couleur de connexion en filet), titres de panneaux épurés, séparateurs d'un pixel. |
| Arbre des connexions | Icône colorée par protocole (RDP bleu, SSH vert, VNC violet, HTTPS turquoise), pastille verte si connecté, chevrons, recherche « Rechercher… ». Panneaux Config et Notifications restylés. |
| Page d'accueil | Salutation, connexion rapide, statistiques, cartes **Récents** / **Favoris**, actions rapides, option « Afficher l'accueil au démarrage ». Historique dans `home.json` (dossier des réglages). |
| Palette de commandes | **Ctrl+K** (ou Ctrl+Shift+P) : recherche floue dans les connexions et les commandes, connexion rapide (`ssh://hôte`, `user@hôte:port`…), bascule de thème. `>` = commandes uniquement. |
| Options & dialogues | Fenêtre Options façon Paramètres Windows 11, contrôles modernes (boutons, champs, cases, listes), À propos, mot de passe, splash screen. |

Briques réutilisables pour du code dessiné à la main :

- `Themes/ModernPalette.cs` — jetons de couleur (`ModernPalette.Current.Accent`, `.Surface`, `.Text`…), suivent le thème clair/sombre.
- `UI/FluentIcons.cs` — icônes de la police Segoe Fluent Icons (`FluentIcons.Get(FluentIcons.Settings, 16)`).
- `UI/GraphicsUtilities/ModernGraphics.cs` — police UI, rectangles arrondis, mélange de couleurs.
- `UI/ModernToolStripRenderer.cs` — rendu des menus et barres d'outils.

### Bugs corrigés au passage

- Crash « Parameter is not valid » (police libérée encore utilisée par une zone de texte).
- « Couldn't add panel » et double onglet « General » au démarrage.
- Barre de connexion rapide invisible / placée avant le menu.
- Crash à la fermeture (ou « Reconnecter tout ») d'un onglet dont la session n'a jamais démarré.
- Libellés japonais dans la page Thème des Options.

## Installer

Chaque push sur `feature/modern-ui` lance le workflow **Build_Modern_UI_Installer**
(`.github/workflows/build-modern-ui.yml`) sur GitHub Actions. Il produit, en artefacts (pas de release ni de tag) :

- `mRemoteNG-ModernUI-<date>-x64-SC.msi` — installeur x64 **autonome** (runtime .NET 10 inclus) ;
- `mRemoteNG-ModernUI-<date>-x64-SC.zip` — même contenu sans installeur.

Récupérer le dernier MSI :

```powershell
gh run list --repo Gus0711/mRemoteNG --branch feature/modern-ui --limit 1
gh run download <run-id> --repo Gus0711/mRemoteNG --name mRemoteNG-ModernUI-installer --dir "$env:USERPROFILE\Downloads\mRemoteNG-ModernUI"
```

Le workflow peut aussi être relancé à la main (onglet Actions → *Build_Modern_UI_Installer* → *Run workflow*).
Le workflow upstream `Build_mR-NB.yml` n'est pas déclenché (il ne réagit qu'à `v1.78.2-dev` et crée une release publique).

L'installeur WiX (`mRemoteNGInstaller/Installer`) prend désormais automatiquement tout le contenu du build
(élément `<Files>` dans `Fragments/FilesFragment.wxs`) ; le dossier source se change avec `-p:HarvestPath=...`.

### Avant d'installer

- **Données réelles** : la version installée lit/écrit `%APPDATA%\mRemoteNG` (tes vraies connexions).
  Sauvegarde faite le 24/10/2026 : `%APPDATA%\mRemoteNG-backup-20261024-1946`.
- Cette version de dev peut réenregistrer `confCons.xml` au format **2.9** : une ancienne version de mRemoteNG risque de ne plus le relire.
- Même UpgradeCode que mRemoteNG officiel → **remplace** l'installation existante.
- MSI **non signé** : SmartScreen → « Informations complémentaires » → « Exécuter quand même ».
- Une demande **« Profile password »** s'affiche à chaque démarrage (fonction de profils de l'upstream `v1.78.2-dev`, pas de la refonte) : *Annuler* suffit.

## Compiler en local (sans Visual Studio)

Le projet référence le contrôle RDP via une référence COM (`aximp`), que seul MSBuild de Visual Studio sait résoudre.
Sans Visual Studio, la machine de dev utilise :

- le SDK .NET 10 installé dans `%LOCALAPPDATA%\dotnet-sdk10` (sans droits admin) ;
- une interop RDP 64 bits générée depuis `C:\Windows\System32\mstscax.dll` (Windows PowerShell 5.1 : `TypeLibConverter` + `AxImporter`) ;
- un fichier `LocalBuild.targets` passé via `-p:CustomAfterMicrosoftCommonTargets=...` qui remplace la référence COM par ces DLL et active `PORTABLE` (les réglages et connexions restent à côté de l'exe, `%APPDATA%` n'est pas touché).

Ces fichiers sont hors du repo (dossier temporaire de la session). Pour une vraie version, passer par le workflow GitHub ou par Visual Studio.

Lancer un build portable local : `$env:DOTNET_ROOT="$env:LOCALAPPDATA\dotnet-sdk10"` puis l'exe de `mRemoteNG\bin\x64\Release` (ou `Release2`).

## Tests

`mRemoteNGTests` : 253 échecs sur 2362, **identiques** avant et après la refonte (même liste, comparée au commit de départ `e3b0108e`).
Ce sont des tests upstream non mis à jour (ex. liste des propriétés de la fenêtre Config sans `Color` / `TabColor`).

## Problèmes connus

- **Arbre des connexions invisible** après avoir déplacé le séparateur Connexions / Configuration : les lignes existent
  (le clic sélectionne) mais ne sont plus dessinées. Non reproduit en automatique ; si ça arrive, laisser l'application ouverte pour diagnostic.
- **Menus en anglais** (File / View / Tools) après un changement de thème, le reste de l'interface reste en français.
- Non vérifié visuellement : anciens thèmes VS2015, zoom > 100 %, panneau auto-masqué déplié, fenêtre détachée, glisser-déposer dans l'arbre.
- La barre de défilement de la grille de propriétés garde la couleur de l'ancien thème jusqu'au redémarrage.
