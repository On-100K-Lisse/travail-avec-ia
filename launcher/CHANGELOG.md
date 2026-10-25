# Historique du DLSS 5 Launcher

Toutes les versions datent du 24 et du 25 octobre 2026. Elles n'ont pas de numéro : elles sont
listées dans l'ordre où elles ont été livrées, avec ce qui a déclenché chaque changement.

## 1. Première version — 24 oct.

- Scan Steam / Epic / GOG + dossiers manuels.
- Deux méthodes : OptiScaler F5 et RenoDX/ReShade. Elles reprennent le montage validé lors des tests 03
  et 04 du 19 octobre sur Dead as Disco (documentés dans `ai/kai/dlss5`).
- Manifeste écrit avant chaque copie, sauvegarde vérifiée par SHA-256, réinitialisation exacte.
- Réglages NR de base, fiche de test Markdown.

## 2. Correctif : le scan ne trouvait rien — 24 oct.

- **Symptôme (rapporté par On-100-K-Lisse) :** les 134 jeux apparaissaient « Compatible », avec
  « exe introuvable », et rien ne s'installait.
- **Cause :** l'exe compilé sous Linux appelait `String.TrimEnd(char)`, qui existe dans Mono mais pas
  dans le .NET Framework de Windows. Le scan plantait avant d'analyser le premier jeu, et
  « Compatible » était seulement la valeur par défaut. `Split(char, …)` posait le même problème dans
  l'installation.
- **Correction :** compilation contre les assemblies de référence de .NET Framework 4.8. Un jeu dont
  l'analyse échoue s'affiche maintenant « Analyse échouée ».

## 3. Feeder pour les jeux sans DLSS — 24 oct.

- Deux nouvelles méthodes : **Feeder F5Low** (mode Optical F5Low d'OptiScaler F5) et **Feeder
  ReShade** (DLSS5-Feeder + LumeniteFX, repris du 1-Click).
- Réglages prudents : modèle à 0,67 de la résolution, génération d'images coupée. En Vulkan ou avec
  dxvk, le chemin « upscaler virtuel » est pris automatiquement, et OptiScaler n'est jamais nommé
  `dxgi.dll` avec dxvk.

## 4. Réinitialisation universelle — 24 oct.

- Demande : pouvoir réinitialiser un jeu modifié par un autre launcher.
- Nouveau module `Cleaner.cs` :
  - il défait un 1-Click DLSS5 d'après son manifeste ;
  - il reconnaît les restes de mods (nom, infos de version, hash) ;
  - il met tout en **quarantaine** au lieu de supprimer, avec un bouton **Annuler le nettoyage** ;
  - il épargne les chargeurs d'autres mods (CET, ASI loader, dxvk).

## 5. Correctif : le feeder ne marchait pas dans les jeux Unity — 24 oct.

- **Symptôme :** aucun effet dans Crime Scene Cleaner ni dans Life is Strange: Before the Storm
  Remastered.
- **Cause :** `UnityPlayer.dll` importe `OPENGL32.dll` mais **aucune DLL DirectX**, parce que Unity
  charge DirectX 11 à la volée. Le launcher a cru à des jeux OpenGL et a installé ReShade sous le nom
  `opengl32.dll`. Le `ReShade.log` montrait qu'il se chargeait, mais qu'il ne voyait aucune image.
  Vérifié sur les vrais fichiers des deux jeux.
- **Correction :**
  - l'API vient d'abord du log du moteur (`Player.log` de Unity, logs d'Unreal) ;
  - sinon, des règles par moteur : Unity = DX11, Unreal = DX12/DX11, OpenGL seulement sans aucun
    indice de DirectX ;
  - la fiche de test reçoit un **diagnostic automatique** ;
  - le nettoyeur reconnaît aussi DLSS5-ReShade-AIO (des restes trouvés dans un jeu).

## 6. Personnalisation, FPS — 24 oct.

- Constat dans les logs : NR à ≈ 12,1 ms par image en Preset A à pleine résolution (2560×1440).
  Pour comparer, le test 03 coûtait ~3,5 ms en Preset B.
- Ajouts :
  - **profils par jeu**, et « Importer depuis le jeu » ;
  - génération d'images **XeFG / FSR FG**, limite de FPS, Reflex Boost, compression des bords ;
  - étalonnage **LUT**, protection peau / décor ;
  - préréglages « FPS max » et « Fluidité ×2 » ;
  - **coût NR réel** lu dans le log et affiché avec un conseil.

## 7. Stabilité, AMD — 24 oct.

- Crashs analysés dans Cyberpunk 2077 et Crimson Desert : point commun, la **Ray Reconstruction**
  active (voir [`../docs/analyse-crashs.md`](../docs/analyse-crashs.md)).
- Ajouts pour la stabilité :
  - **profil sûr RR**, activé par défaut ;
  - dump de crash ;
  - bouton `TdrDelay` ;
  - diagnostic de `DEVICE_HUNG`, de la RR et des reconstructions du modèle.
- **Mode AMD / Intel :**
  - port vendor-neutral d'OptiScaler F5 0.1.32 (hash identique à celui publié par GitHub) ;
  - FSR4 / `ffx_12` comme upscaler ;
  - RenoDX et le Feeder ReShade refusés (ils passent par NVIDIA) ;
  - préréglage de départ AMD.
- Bouton **Copie pour un ami** : une copie propre, sans profils, journaux ni dossiers de jeux.

## 8. RTX 50, molette — 24 oct.

- **RTX 50 :** la 310.9.16 NVIDIA d'origine est installée automatiquement, parce qu'elle contient le
  code natif des RTX 50. La build openNR reste pour les RTX 20/30/40. Le réglage `dllNr=` de
  `launcher-config.ini` permet de forcer l'une ou l'autre.
- **Molette (rapporté par On-100-K-Lisse) :** en faisant défiler la page, la molette « attrapait » les
  listes et les cases numériques et changeait leur valeur. Désormais, elle fait seulement défiler
  la page.

## 9. Crash Unreal, logs du moteur — 25 oct.

- Crash de Species: Unknown après 2 h 36 de jeu : exception `0x8000` levée par Unreal Engine.
- Ajouts :
  - la fiche de test inclut les **erreurs du log du moteur** (Unreal / Unity) ;
  - ce log est copié dans les journaux au reset ;
  - le diagnostic reconnaît les exceptions d'Unreal et la « profondeur jamais trouvée » ;
  - correction d'un faux « génération d'images vue » dans la mesure de performance.

## 10. Fiche de test après un reset — 25 oct.

- **Constat (rapporté par On-100-K-Lisse) :** sa façon de travailler, c'est de réinitialiser le jeu
  dès qu'un problème arrive, puis de décrire le problème. Une fiche de test faite *après* le reset
  sortait vide, parce que les logs avaient déjà été déplacés dans `journaux\`.
- **Corrections :**
  - **Réinitialiser crée la fiche de test automatiquement, juste avant le reset**, pendant que les
    logs, les réglages et les DLL sont encore en place. Le message de fin donne son nom.
  - Une fiche faite après un reset lit **le dernier journal archivé** du jeu. Le reset y copie
    maintenant aussi le manifeste. Pour un vieux journal sans manifeste, la méthode est déduite des
    INI archivés.
  - Le détail du jeu montre le coût NR de la dernière session archivée.
  - Le log du moteur est cherché à plus d'endroits : `AppData\Local`, `Documents\My Games`,
    `Documents`, `Saved Games`, le dossier d'installation, et le dernier rapport du Crash Reporter
    d'Unreal (`Saved\Crashes`, dont le message d'erreur de `CrashContext.runtime-xml`). S'il n'en
    trouve aucun, la fiche le dit et liste les endroits cherchés.

## 11. Docteur payload, blocage anti-cheat — 25 oct.

- **Origine :** deux idées proposées par Kai, retenues avec On-100-K-Lisse avant d'envoyer le
  launcher à deux testeurs (une RTX 5070 et une RX 9070 XT).
- **Docteur payload :**
  - au démarrage, il vérifie les 22 éléments de `payload\` : SHA-256 des DLL, présence des
    réglages, nombre de fichiers des dossiers. Le verdict s'affiche en haut de la fenêtre, et un clic
    ouvre le rapport ;
  - cinq états : OK, AUTRE VERSION, VERSION INCONNUE, ABÎMÉ (y compris une DLL coupée par une copie
    incomplète), MANQUANT. Il signale aussi un mélange de versions dans OptiScaler ;
  - avant une installation : un fichier abîmé ou manquant bloque, et rien n'est touché dans le jeu.
    Une autre version demande confirmation ;
  - la fiche de test donne le verdict, et identifie chaque DLL installée par son hash, même après un
    reset ;
  - la copie pour un ami est vérifiée juste après avoir été faite.
- **Blocage anti-cheat :** une case « Bloquer l'installation dans les jeux avec anti-cheat »,
  décochée par défaut et cochée d'office dans une copie pour un ami. La décocher demande d'accepter
  un avertissement.
- **Vérifié :** 130 vérifications automatiques, puis essai sous Windows par On-100-K-Lisse le
  25 octobre : ça fonctionne.
- **Constat au passage :** ReShade dans le payload est bien la version 6.8.0. Le docteur la lit
  dans les infos du fichier.

## 12. Historique des parties — 25 oct.

- **Origine :** idée proposée par Kai, retenue avec On-100-K-Lisse : garder le coût NR de chaque
  partie, et pas seulement celui de la dernière.
- **Ajouts :**
  - `historique.csv` : une ligne par partie (durée, carte, méthode, DLL, preset, modèle, coût NR
    moyen / min / max / p95 / début / fin de partie, créations du modèle, crash) ;
  - une partie est notée au démarrage, à la fiche de test, au reset et avant « Lancer le jeu »,
    sans doublon ; les anciens journaux archivés sont repris ;
  - bouton **Historique** : comparaison des montages d'un jeu (par exemple Preset A et Preset B),
    liste des parties, et vue « Tous les jeux » ;
  - une ligne d'historique dans le détail du jeu, et une section dans la fiche de test ;
  - le manifeste note maintenant le nom de la carte graphique.
- **Correctif au passage :** le docteur payload ne lit plus les infos de version d'une DLL coupée.
  Sous Mono, cette lecture faisait planter les tests. Sous Windows, ce cas n'a pas été rencontré ; le
  fichier est de toute façon déjà classé « abîmé ».
- **Vérifié :** 141 vérifications automatiques, lancées trois fois de suite. Essai de la reprise sur
  les vrais journaux d'On-100-K-Lisse (8 parties retrouvées), puis essai sous Windows par
  On-100-K-Lisse le 25 octobre : ça fonctionne.
