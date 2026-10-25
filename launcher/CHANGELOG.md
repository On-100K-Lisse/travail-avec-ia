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
