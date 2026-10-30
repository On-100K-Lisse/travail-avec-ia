# Journal des essais en jeu — 24 et 25 octobre 2026

**But :** garder une trace vérifiable de ce qui a été essayé en jeu avec le DLSS 5 Launcher, des
coûts mesurés et des résultats. Les essais ont été faits par On-100-K-Lisse. Les chiffres viennent
des fichiers `OptiScaler.log` archivés par le launcher et des fiches de test. L'analyse est de
Claude.

**Matériel :** une RTX 4070 (12 Go), pilote NVIDIA 621.01, à 2560×1440. La configuration complète
est dans `HARDWARE_ENV.md` sur `main`.

**Logiciels :** OptiScaler F5 0.1.32, la DLL NR openNR compat-fp16 310.9.16
(SHA-256 `CDFA6C1E…ABF3ED`), et le DLSS 5 Launcher (ce dossier).

> **Comment lire le « coût NR » :** c'est le temps que le modèle prend sur le GPU à chaque image,
> tel qu'OptiScaler le mesure (`DLSS-NR elapsed: … ms total`). À 60 images par seconde, le budget
> total d'une image est de 16,7 ms. Un NR à 12 ms en mange donc les trois quarts. Ce n'est pas un
> benchmark contrôlé : les scènes et les réglages changeaient d'une session à l'autre.

## Résumé

| Jeu | Moteur / API | Méthode | Réglages NR | Coût NR mesuré | Résultat |
|---|---|---|---|---|---|
| Dead as Disco | Unreal, DX12, DLSS | OptiScaler F5 | Preset A, modèle 1705×960 | ≈ 5,5 ms (87 mesures) | Fonctionne, pas de crash |
| Dead as Disco | idem | OptiScaler F5 | Preset A, échelle 0,7 → modèle 1200×672 | ≈ 4,5 ms (41 mesures) | Fonctionne, pas de crash |
| Crime Scene Cleaner | Unity, DX11, sans upscaler | Feeder ReShade (1er essai) | — | — | **Échec** : bug de détection du launcher, corrigé (voir plus bas) |
| Life is Strange: Before the Storm Remastered | Unity, DX11, sans upscaler | Feeder ReShade (1er essai) | — | — | **Échec** : même bug |
| Life is Strange: Before the Storm Remastered | idem | **Feeder F5Low** | Preset A, modèle 2560×1440 | ≈ 12,1 ms | **Fonctionne** (« parfaitement », selon On-100-K-Lisse) ; profondeur du jeu trouvée |
| Crime Scene Cleaner | idem | **Feeder F5Low** | Preset : inconnu (temporaire) ; image 2560×1440 | ≈ 12,1 ms | **Fonctionne** ; profondeur trouvée |
| Cyberpunk 2077 (sans mods) | REDengine, DX12, DLSS + Ray Reconstruction | OptiScaler F5 | Preset A, échelle 0,7 | ≈ 7,1 ms | **Crash GPU** après ~3 min (voir [`analyse-crashs.md`](analyse-crashs.md)) |
| Crimson Desert Enhanced | DX12, DLSS + RR + génération d'images du jeu | OptiScaler F5 | Preset A, échelle 0,8 → 2048×1152 | ≈ 8,4 ms (7,1 à 13,9) | **Crash** après ~9 min |
| Species: Unknown | Unreal 5.7.4, DX12, sans upscaler | **Feeder F5Low** | Preset A, échelle 0,67 → 1552×864 | ≈ 6,0 ms (854 mesures, 5,6 à 8,3) | **Stable 2 h 36**, puis crash (cause inconnue (temporaire)) |

## Ce qu'on en retient

1. **Le feeder fonctionne.** Le mode Optical F5Low d'OptiScaler F5 a fait tourner le NR dans trois
   jeux sans DLSS : deux jeux Unity en DX11 et un jeu Unreal en DX12. Dans Species: Unknown, il a
   tenu 2 h 36 d'affilée.
2. **Le Preset A coûte cher.** Il a été choisi à la main dans le menu du jeu lors de plusieurs
   sessions. À pleine résolution, il coûte ≈ 12 ms. Pour comparer, le Preset B à 1705×960 coûtait
   ~3,5 ms dans le test 03 du 19 octobre (branche `ai/kai/dlss5`). C'est ce qui a motivé l'affichage
   du coût réel dans le launcher, et les préréglages orientés FPS.
3. **Le menu du jeu peut écraser les réglages du launcher.** Dans Life is Strange, l'INI final
   contenait `WorkingScale=auto` (donc 1,0) au lieu du 0,67 posé par le launcher : l'INI avait été
   réenregistré depuis le menu Inser. D'où le bouton « Importer depuis le jeu », qui garde ces
   changements dans le profil au lieu de les perdre.
4. **La profondeur n'est pas toujours trouvée.** Elle l'a été dans les deux jeux Unity. Dans
   Species: Unknown, elle ne l'a jamais été en 2 h 36. Dans ce cas, la recherche de profondeur ne
   sert à rien et coûte du temps CPU (voir [`analyse-crashs.md`](analyse-crashs.md)).
5. **Les crashs ne sont pas tous pareils.** Cyberpunk et Crimson Desert partagent un schéma clair
   (Ray Reconstruction). Species: Unknown est différent.

## Essais du 25 octobre (soir)

Ces chiffres viennent de l'historique du launcher (`historique.csv`), qui les lit dans
`OptiScaler.log`. Carte : RTX 4070, DLL NR openNR 310.9.16 compat-fp16.

| Jeu | Étape de test | Méthode, version | Réglages NR | Durée | Coût NR | Résultat |
|---|---|---|---|---|---|---|
| Cyberpunk 2077 | 2 - DLSS 5 seul | OptiScaler F5 0.1.32 | Preset B, modèle 1376×784 | 3 min | ≈ 3,4 ms | Aucun crash |
| Species: Unknown | — | Feeder F5Low 0.1.32 | Preset B, modèle 1552×864 | 2 min | ≈ 3,7 ms | Aucun crash |
| Species: Unknown | 2 - DLSS 5 seul | **Feeder F5Low 0.1.34** | Preset B, modèle 1552×864 (échelle 0,67) | **1 h 27** | ≈ 3,9 ms (3,5 à 5,3) | **Aucun crash** |

À retenir :

- **OptiScaler 0.1.34 fonctionne** dans Species: Unknown, en Feeder F5Low, sur 1 h 27.
- **En Preset B, le NR coûte environ un tiers de moins** dans Species: Unknown : ≈ 3,9 ms contre
  ≈ 6,0 ms en Preset A le 24 octobre, avec la même taille de modèle (1552×864). La version
  d'OptiScaler avait aussi changé entre les deux (0.1.32 → 0.1.34).
- **Le crash `0x8000` n'est ni confirmé ni écarté.** Le 24 octobre, il était arrivé après 2 h 36 ;
  1 h 27 sans crash ne suffit pas pour conclure. Plusieurs choses ont aussi changé en même temps
  (Preset A → B, OptiScaler 0.1.32 → 0.1.34). Le protocole de test du launcher dira « trop peu pour
  conclure » tant qu'il n'y aura pas assez de temps de jeu.
- **La recherche de profondeur était encore active**, et ne l'a toujours pas trouvée (« matched with
  depth 0 »). Le conseil « couper la recherche de profondeur » reste à essayer.
- **Cyberpunk 2077 :** l'étape « DLSS 5 seul » n'a duré que 3 minutes. Une partie « vanille » a
  aussi été notée à la main, avec les valeurs proposées par défaut (1 h) : c'était probablement un
  essai du bouton, donc elle n'est pas comptée ici. Il faut beaucoup plus de jeu avant de comparer
  les étapes.

## Pas encore testé

| Configuration | État |
|---|---|
| Carte RTX 50 | Inconnu (temporaire) : le testeur prévu ne fera pas de test, aucun autre n'est prévu |
| Carte AMD (une RX 9070 XT chez un testeur), port vendor-neutral | Inconnu (temporaire) |
| Génération d'images XeFG / FSR FG du launcher | Inconnu (temporaire) |
| Étalonnage LUT, protection peau / décor | Inconnu (temporaire) |
| Cyberpunk 2077 avec la Ray Reconstruction coupée et le « profil sûr RR » | Inconnu (temporaire) |
| Injection dans un jeu Xbox / Game Pass | Inconnu (temporaire) : le scan les trouve, aucune installation essayée |
| OptiScaler 0.1.34 avec la méthode OptiScaler F5 (jeux avec upscaler) | Inconnu (temporaire) : seulement essayée en Feeder F5Low |
