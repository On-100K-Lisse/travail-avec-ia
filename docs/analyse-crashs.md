# Analyse des crashs — DLSS 5 Neural Rendering

**But :** expliquer, à partir des logs et d'un dump de crash, ce qui s'est passé dans les trois
crashs du 24 octobre 2026, ce qui est établi, ce qui ne l'est pas, et ce qui a été changé dans le
launcher.

**Sources :**
- les `OptiScaler.log` archivés par le launcher ;
- un `OptiScaler_crash.dmp` (minidump Windows) lu avec un petit lecteur écrit pour l'occasion ;
- les crashs de Cyberpunk du 19 octobre, documentés dans `ai/kai/dlss5`.

**Matériel :** RTX 4070, OptiScaler F5 0.1.32, DLL openNR 310.9.16.

## Deux termes à connaître

- **`DXGI_ERROR_DEVICE_HUNG` / `0x887A0006`** : Windows a jugé que la carte graphique était
  bloquée et a réinitialisé son pilote. Le jeu perd alors son accès au GPU et plante. Par défaut,
  Windows attend 2 secondes avant de décider ça (réglage `TdrDelay`).
- **Recréation du modèle** : chaque fois qu'un réglage du modèle NR change (preset, style,
  intensité…), ou que le jeu change de chemin de rendu (par exemple quand la Ray Reconstruction
  s'active), OptiScaler détruit le modèle et le recrée. Le log l'écrit
  `DLSS-NR model feature created`.

## 1. Cyberpunk 2077 (sans mods) — crash GPU

Ce que dit le log, dans l'ordre :

1. Le NR tourne normalement, à ≈ 7,1 ms par image, avec un modèle de 1792×1008 (échelle 0,7).
2. À 18:43:04, le modèle est **recréé** sur le chemin « *running after RR+SR* » (après la Ray
   Reconstruction et l'upscaling), cette fois en **Preset A à pleine résolution, 2560×1440**.
   La mémoire vidéo utilisée est de 8,91 Go sur 12.
3. **0,1 seconde plus tard** : `Device removed reason: DXGI_ERROR_DEVICE_HUNG`, puis
   `Failed to create a constants buffer`.

C'est le même schéma que les 3 crashs du 19 octobre (code `0x887A0006`, juste après des
recréations du modèle quand la RR s'active).

## 2. Crimson Desert Enhanced — crash

1. La Ray Reconstruction et la génération d'images du jeu sont actives (Streamline DLSS-G dans le
   log). Le NR tourne « *after RR+SR* », en Preset A, avec un modèle de 2048×1152.
2. Le modèle est **recréé 8 fois en une minute**. Les réglages ont été changés en direct dans le
   menu (intensité 1,4 puis 0,85, etc.).
3. Le coût du NR monte par pics jusqu'à 13,9 ms.
4. Le log se termine par `Failed to map buffer` (Streamline), puis `timestampData is null`. Ce sont
   des signes d'une perte du GPU ou d'un manque de ressource. Aucune raison « device removed »
   n'est écrite.

## Ce qui est établi pour 1 et 2, et ce qui ne l'est pas

- **Établi :** dans les deux cas, la Ray Reconstruction était active, le NR tournait après elle en
  Preset A, à une grande résolution de modèle, et le crash a suivi une ou plusieurs recréations du
  modèle.
- **Pas établi :** le mécanisme exact. Il peut s'agir d'un pic de travail GPU au-delà des 2 s de
  Windows pendant la recréation, d'un bug dans le chemin RR + NR, ou d'autre chose. Cause exacte :
  inconnue (temporaire). Le guide d'OptiScaler F5 qualifie d'ailleurs le NR avec la Ray
  Reconstruction d'« encore expérimental ».

## 3. Species: Unknown — crash après 2 h 36

1. Le Feeder F5Low tourne de façon stable pendant 2 h 36 (NR ≈ 6 ms, Preset A, modèle 1552×864).
2. Juste avant le crash, le log montre une dizaine de « *scene cut seen* » en quelques secondes.
   C'est sûrement un chargement ou une cinématique, mais c'est une hypothèse.
3. `[C] Unhandled exception 8000 … writing OptiScaler_crash.dmp`.

**Lecture du dump :**
- l'exception a le code `0x00008000` ;
- elle est levée dans `KERNELBASE.dll` par un appel volontaire à `RaiseException` du jeu, pas par une
  violation d'accès dans OptiScaler ;
- le jeu tourne sur Unreal Engine 5.7.4.

À notre connaissance, `0x8000` est le code qu'Unreal Engine 5 utilise pour signaler un **crash GPU
détecté par son moteur de rendu**. **Ce n'est pas confirmé :** il faut le vérifier dans le code
d'Unreal ou dans le log du jeu. Le message exact d'Unreal n'était pas lisible dans le dump.

Le 25 octobre, le launcher n'a trouvé **aucun log Unreal** pour ce jeu, ni à l'emplacement habituel
(`AppData\Local\SpeciesUnknown\Saved\Logs`), ni dans le dossier d'installation (vérifié : pas de
dossier `Saved`). Beaucoup de jeux Unreal en version finale (« Shipping ») n'écrivent pas de log ;
c'est peut-être le cas ici, sans certitude, car tous les dossiers d'`AppData` n'ont pas pu être
vérifiés. Le launcher cherche maintenant à plus d'endroits, y compris les rapports du Crash Reporter
d'Unreal. Cause exacte : inconnue (temporaire).

**Constat annexe, lui établi :** la recherche de profondeur d'Optical F5Low n'a **jamais** trouvé la
profondeur de ce jeu. Le log dit « *none qualifies* » dans 841 rapports sur 871, et compte 0 image
« *matched with depth* ». Pourtant, elle surveillait tous les appels de dessin du jeu, avec des
blocages jusqu'à **2,1 ms** sur un appel. Pour ce jeu, la couper ne change rien à la qualité et
retire une source de surcharge. On ne sait pas si elle a joué un rôle dans le crash : inconnu
(temporaire).

**Rejeu du 25 octobre :** 1 h 27 sans crash, en Feeder F5Low avec OptiScaler 0.1.34 et le Preset B
(NR ≈ 3,9 ms au lieu de ≈ 6,0 ms). La recherche de profondeur était encore active et n'a toujours
rien trouvé. Comme le crash du 24 était arrivé après 2 h 36, ce rejeu ne permet pas de conclure, et
plusieurs choses ont changé en même temps. Cause : toujours inconnue (temporaire).

## Ce qui a été changé dans le launcher

| Changement | Pourquoi |
|---|---|
| **Profil sûr Ray Reconstruction** (coché par défaut) : dans un jeu où `nvngx_dlssd.dll` est présent, il force Preset B, un modèle ≤ 0,6 et 1 passe. Le profil de l'utilisateur, lui, n'est pas modifié. | Réduire la charge exactement là où les crashs 1 et 2 sont arrivés |
| Le diagnostic de la fiche de test détecte `Device removed reason`, la RR active, les recréations à répétition, `Failed to map buffer`, les exceptions d'Unreal (`0x8000`, `0x8001`), et la profondeur jamais trouvée | Que le prochain crash s'explique tout seul |
| La fiche de test lit les erreurs du **log du moteur** (Unreal / Unity), qui est copié au reset | Le détail des crashs GPU d'Unreal est là, pas dans `OptiScaler.log` |
| Option **dump de crash** d'OptiScaler (`[Log] CrashDump`) | C'est grâce à elle que le crash 3 a pu être lu |
| Bouton **`TdrDelay`** (2 → 8 s, réversible) | Évite une coupure sur un pic. Ça ne corrige pas un vrai bug, et ce n'est pas activé par défaut. |
| Notes par jeu (Cyberpunk, Crimson Desert, Species: Unknown) | Rappeler ces constats dans le détail du jeu |

## Conseils qui en découlent

- Dans les jeux avec Ray Reconstruction : **couper la RR dans le jeu**, ou au moins garder le
  « profil sûr RR ».
- Régler l'effet dans le launcher, ou avec « Importer depuis le jeu », plutôt que de changer les
  réglages en pleine partie : chaque changement recrée le modèle.
- Le Preset B laisse beaucoup plus de marge que le Preset A.
- Quand la fiche dit « profondeur jamais trouvée », décocher « Utiliser la profondeur du jeu » pour
  ce jeu.

## À faire

- Rejouer à Species: Unknown sans la recherche de profondeur (bouton « Conseils »), en changeant une
  seule chose à la fois et assez longtemps : au moins 3 fois les 2 h 36 du premier crash pour que le
  protocole du launcher puisse conclure. Ensuite, confirmer ou corriger l'interprétation de
  `0x8000`.
- Refaire Cyberpunk 2077 avec la RR coupée, puis avec la RR et le « profil sûr RR », pour savoir si
  le schéma se confirme.
