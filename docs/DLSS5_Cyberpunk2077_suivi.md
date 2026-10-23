# DLSS 5 — Suivi Cyberpunk 2077 : crashs 0x887A0006, update 2.5 et modpack Perfect City

**Date : 23 octobre 2026.** Suite de `DLSS5_310.9.16_RTX4070.md` (le build de la DLL
compat-fp16 du 19 octobre). Ce document couvre ce qui n'était pas encore documenté :
l'enquête sur les crashs de Cyberpunk 2077, le protocole de test décisif, et le plan
pour le modpack.

## 1. Le problème

- Crash **0x887A0006** (DEVICE_HUNG) dans **Cyberpunk 2077 seulement** (constaté le
  19 octobre), alors que la même chaîne DLSS 5 (ReShade/OptiScaler + DLL NR compat-fp16)
  est stable dans Dead as Disco et Crime Scene Cleaner.
- Suspects possibles : le modpack, la chaîne NR elle-même, l'overclock GPU, ou la
  pression sur les **12 Go de VRAM** de la RTX 4070 (les packs de textures 4K du modpack
  sont gourmands).
- Méthode retenue : **une variable à la fois**. Aucun verdict sans test isolé.

## 2. Contexte — update 2.5 « Edgerunners 2 » (22 octobre 2026)

- **DLSS 4.5 natif** (Multi Frame Generation jusqu'à 6X) — le jeu n'a plus besoin de la
  chaîne externe pour du DLSS récent, ce qui rend le test vanille propre possible.
- Support mods **officiel via mod.io** (les succès sont désactivés sur les parties
  moddées), quête préquelle « Clampdown », dual-wielding (perk « Double Trouble »).
- Les frameworks de modding (CET, RED4ext, redscript, ArchiveXL/TweakXL/Codeware) ont
  besoin de leurs propres versions 2.5 après le patch. Au 23 octobre, aucune
  confirmation que tout est prêt — c'est ce qui bloque la reprise de la partie moddée.

## 3. Protocole du test décisif

Préparation :

- **Backup des sauvegardes**, gardé intact pendant tous les tests.
- **Purge Vortex** : retire du dossier du jeu tout ce que Vortex a déployé (réversible,
  Deploy restaure tout). ⚠️ La purge ne couvre PAS les installs manuelles :
  **ReShade (`dxgi.dll`), OptiScaler et la DLL NR doivent être retirés à la main** dans
  `bin/x64`, sinon le test « vanille » n'en est pas un.
- **Clocks stock** : dans MSI Afterburner, **Reset + Apply** et décocher l'application
  des réglages au démarrage, vérifié dans HWiNFO. Fermer Afterburner/RTSS ne réinitialise
  RIEN — les offsets vivent dans le pilote NVIDIA. La carte (Gigabyte RTX 4070 Windforce
  OC) est factory-OC : des clocks élevées par défaut sont normales.

Étapes :

1. **Vanille totale** — nouvelle partie (la seule partie avancée est moddée), DLSS 4.5
   natif, aucun mod, aucune chaîne externe, clocks stock.
2. **Chaîne DLSS 5 seule** — ReShade/OptiScaler + DLL NR réintroduits, toujours sans mods.
3. **Modpack à jour** — seulement quand la révision 2.5 du pack existe (voir section 5).

Lecture des verdicts :

- Stable à l'étape 1, crash à l'étape 2 → la chaîne NR (ou l'OC) est le suspect #1.
- Stable aux étapes 1-2, crash à l'étape 3 → le modpack est coupable.
- Nuance : la 2.5 est du code jour 1, elle peut crasher toute seule ; un crash isolé ne
  conclut rien sans reproduction.

## 4. Résultats

- **Étape 1 (22 octobre au soir) : RÉUSSIE** — session vanille sur nouvelle partie,
  **zéro crash, aucun problème**. La base (jeu 2.5 + pilote + matériel aux clocks stock)
  est saine.
- **Étape 2 : prévue le 23 octobre au soir.**
- Étape 3 : en attente de la révision 2.5 du modpack.

## 5. Le modpack « Perfect City » et la reprise de la partie avancée

- Pack utilisé : **Perfect City, de Lilevitchi** — collection Nexus Mods gérée par
  Vortex, **versionnée par patch du jeu** (la version d'avant la 2.5 s'appelait
  « Perfect City 2.31 »).
- **Reprise de la partie avancée** : possible en principe quand le même set de mods
  revient en versions 2.5, aux conditions suivantes :
  - attendre la révision 2.5 du pack publiée par Lilevitchi (qui dépend lui-même des
    frameworks 2.5) — ne pas improviser une mise à jour partielle ;
  - **jamais charger puis resauvegarder** la vieille partie dans un état « mods
    manquants » : les pertes d'objets/systèmes se figeraient dans la sauvegarde ;
  - premier chargement après la mise à jour complète → vérifier inventaire, apparence
    et systèmes, puis sauvegarder dans un **nouveau slot**. Le backup reste le parachute ;
  - risque résiduel : si la révision 2.5 retire ou remplace des mods, leur contenu lié
    peut disparaître de la partie.
- **Plan d'allègement** (à appliquer après les tests, à la réinstallation du pack) :
  retirer les packs de textures **4K**, garder le **2K**, compenser par le DLSS 5 —
  objectif : soulager les 12 Go de VRAM. Les packs 4K seraient optionnels à
  l'installation de la collection (à confirmer à l'écran d'installation de la révision
  2.5 ; sinon, désactivation dans Vortex après coup, à refaire après chaque révision).
  Nuance technique : le DLSS upscale l'image finale, il ne régénère pas les textures —
  à 1440p, la différence 2K/4K est surtout visible sur les éléments de près (arme en
  main, visages).
- **Stratégie mods long terme** (décidée le 21 octobre) : au fil des republications par
  les auteurs, préférer la version mod.io officielle quand elle existe, Vortex pour le
  reste ; jamais les deux versions du même mod ; un seul maître pour le load order ;
  geler les mises à jour automatiques pendant une partie en cours.

## 6. État actuel (23 octobre 2026)

- Build openNR 310.9.16 : documenté et validé (document principal).
- Test décisif : étape 1 validée, étape 2 à faire, étape 3 en attente du pack.
- Partie avancée moddée : en pause, backup intact, reprise conditionnée à la révision
  2.5 de Perfect City.

## 7. Leçons

- **Une variable à la fois** — sans le test vanille propre, le crash aurait pu être
  attribué au mauvais suspect indéfiniment.
- **La purge d'un gestionnaire ne couvre pas les installs manuelles** — un test
  « vanille » se vérifie fichier par fichier.
- **Fermer un logiciel d'overclock ne remet pas les clocks stock** — les offsets sont
  dans le pilote ; seul un Reset explicite les enlève.
