# Patchs d'IA de Sonic et Shadow — transformations équiprobables et rivalité équilibrée

Juillet 2026. Personnages : **Sonic V1.0 JUS AI Patch** (installé sous `Sonic_Nitro/`, avec ses
variantes `OP/` et `OP Buff/`) et **Shadow JUS Updated**. MUGEN 1.1, mode Watch (IA contre IA).

> Ce document décrit **nos modifications** et la méthode de diagnostic. Les fichiers des
> personnages appartiennent à leurs auteurs et ne sont pas inclus dans ce dépôt.

## Résumé

| Version | Problème visé | Cause trouvée | Correction |
|---|---|---|---|
| v1 | L'IA spamme Dark Sonic | Blocs « Activar » par palette dans `AI.cns`, à `random < ailevel*100` | Un seul tirage équiprobable entre les 8 formes |
| v2 | v1 sans effet visible | Les intros de round transforment l'IA selon sa palette | Redirections d'intro bloquées pour l'IA, palette par défaut à 1 |
| v3 | — (ajout) | — | Mémoire de round perdu : transformation plus facile et plus fréquente après une défaite |
| v4 | Shadow gagne toujours, Fleetway trop fréquent | Bloc de transfo de Sonic placé trop bas, Shadow 50× plus rapide à décider, desperation garantie en Fleetway | Bloc déplacé en tête, stats égalisées, desperation équiprobable pour l'IA, Shadow plus proactif |
| v5 | Sonic encore pas assez transformé | — | Fréquence ×8 (×16 après un round perdu) |

## v1 — Un seul dé pour les 8 formes

### Le symptôme

Contrôlé par l'IA, Sonic se transformait presque toujours en Dark Sonic, et dès qu'il pouvait.

### La cause

Le `[Statedef -2]` de `AI.cns` contenait un bloc `ChangeState` par forme, chacun lié à une
palette (`palno`) et déclenché par `random < (ailevel * 100)`. Au niveau d'IA 8, ça fait
**80 % de chances par tick**, et MUGEN tourne à 60 ticks par seconde. Dès que le power atteignait
le seuil, la transfo partait quasi instantanément. En prime, le bloc de la palette 4 (Darkspine)
envoyait vers `801` (Dark Sonic) au lieu de `804` : une erreur de copier-coller.

### La correction

Tous les blocs « Activar » de l'IA sont retirés et remplacés par une seule porte :

```ini
[State -2, De transfo]
type = VarSet
trigger1 = 1
v = 9
value = random
ignorehitpause = 1

[State -2, Transfo IA equiprobable]
type = ChangeState
triggerall = statetype != A
triggerall = power >= 1500
triggerall = var(2) = 0
triggerall = ailevel > 0
triggerall = ctrl
triggerall = p2bodydist x >= 100
trigger1 = random < (ailevel * 2)
value = ifelse(var(9) < 125, 800, ifelse(var(9) < 250, 801, ifelse(var(9) < 375, 802, ifelse(var(9) < 500, 804, ifelse(var(9) < 625, 805, ifelse(var(9) < 750, 806, ifelse(var(9) < 875, 808, 950)))))))
```

Les 8 formes : `800` Super, `801` Dark, `802` Hyper, `804` Darkspine, `805` Excalibur,
`806` Werehog, `808` Ultra, `950` Fleetway. Chaque tranche de 125 sur 1000 vaut exactement 12,5 %.

> **Piège :** ne jamais écrire `ifelse(random < X, …, ifelse(random < Y, …))`. MUGEN tire un
> **nouveau** nombre à chaque `random` d'une expression, et les pourcentages sont faussés. On tire
> le dé une seule fois dans une variable libre, puis on branche dessus.

Si les numéros d'états sont consécutifs, `value = 800 + (random % 8)` suffit.

## v2 — Le vrai coupable était l'intro

### Le symptôme

Après v1, rien n'avait changé en jeu.

### La cause

Le personnage a un système « costume = forme ». Dans les états d'intro `190` et `194` de
`Sonic.cns`, des `ChangeState` redirigent selon la palette dès la première frame du round :
palette 5 vers l'intro Dark Sonic, palette 6 vers Super, etc. Le `.def` contenait
`pal.defaults = 6,5`. L'IA qui choisissait le personnage tombait donc sur ces palettes et
**commençait chaque round déjà transformée**, avant que `AI.cns` ait son mot à dire.

### La correction

- Chaque redirection d'intro par palette reçoit une condition qui exclut l'IA :

  ```ini
  triggerall = ailevel = 0  ; PATCH: l'IA commence en forme normale (transfos via AI.cns)
  ```

  Seule l'intro cinématique sans transformation (palette 1 vers `193`) reste libre.
- Dans le `.def` : `pal.defaults = 1,1`.

Le joueur humain ne perd rien : choisir un costume pour commencer transformé marche encore,
comme les commandes de transfo manuelles.

> **Leçon :** quand un correctif ne change rien, chercher **tous** les chemins vers l'état visé
> (`ChangeState` et `SelfState` vers les états de transfo, dans tous les fichiers, avec ou sans
> `ailevel`), pas seulement celui qu'on croit responsable.

## v3 — Plus dangereux après un round perdu

Le trigger `lose` de MUGEN se remet à zéro entre les rounds. On mémorise donc la défaite dans une
variable, qui, elle, persiste d'un round à l'autre dans un même match et se remet à zéro au match
suivant :

```ini
[State -2, Memoire round perdu]
type = VarSet
triggerall = var(16) = 0
trigger1 = roundstate >= 3 && lose
v = 16
value = 1
ignorehitpause = 1
```

La porte de transfo devient `power >= 1500 - (var(16) * 500)` et
`random < (ailevel * (2 + var(16) * 3))`. Après un round perdu : coût réduit à 1000 et fréquence
multipliée par 2,5.

> **À vérifier avant :** qu'aucun `VarRangeSet` du personnage n'efface la variable choisie entre
> les rounds.

Effet observé : l'IA qui vient de perdre un round revient plus agressive et se dépêche de remplir
sa jauge, sans retomber dans le spam d'avant.

## v4 — Rendre Sonic digne de son rival

### Les symptômes

- Shadow gagnait tous les matchs contre Sonic.
- Shadow semblait attendre que son adversaire se transforme pour se transformer.
- Fleetway était devenu la forme la plus fréquente de Sonic, les autres étaient rares.

### Le diagnostic

| Observation | Explication |
|---|---|
| Shadow « attend » l'adversaire | Faux en apparence seulement : aucune condition sur l'ennemi. Ses transfos exigent d'avoir perdu de la vie (sous 50 % pour Super Shadow, sous 33 % pour Chaos et Hyper). Il perd rarement autant de vie avant que Sonic soit transformé, d'où la corrélation. |
| Shadow décide plus vite | Vrai : ses transfos tournent à `ailevel*100` (jusqu'à 40 % par tick), contre `ailevel*2` à `*5` pour Sonic en v3 (1,6 à 4 %). |
| Sonic se transforme peu | Le bloc de transfo était placé **après** des dizaines de blocs d'attaque dans le `[Statedef -2]`. Le premier `ChangeState` qui se déclenche gagne : les attaques lui volaient son tour presque à chaque tick. |
| Fleetway trop fréquent | Le « desperation » de Sonic (dans `Sonic.cmd` et le bloc `SecondChance` des variantes OP) envoyait toujours vers Fleetway (`950`). |
| Écart de stats | Sonic 1250/100/100, Shadow 1350/105/105. |

### Les corrections

Contrainte : renforcer Sonic **sans affaiblir** Shadow.

**Sonic**

- Stats égalisées sur Shadow : 1350 de vie, 105 d'attaque, 105 de défense.
- Le bloc de transformation est déplacé **en tête** du `[Statedef -2]`, avec `roundstate = 2`.
- Fréquence `random < (ailevel * (6 + var(16) * 6))`.
- Desperation scindée en trois blocs : le joueur humain garde son Fleetway d'urgence garanti;
  l'IA en forme normale fait un tirage parmi les 8 formes; l'IA déjà transformée
  (`var(2) = [1,3]`) garde le Fleetway d'origine. Même logique pour `SecondChance`.

**Shadow** (un seul fichier, son `AI.cns`)

- Conditions de vie assouplies : Super Shadow dès environ 5 % de vie perdue (seuil
  `lifemax/1.05`), Chaos et Hyper dès environ 33 % perdus (`lifemax/1.5`). Il se transforme de
  lui-même en début ou milieu de round. Ses probabilités et sa vitesse de décision sont intactes.

## v5 — Réglage final

Fréquence montée à `random < (ailevel * (8 + var(16) * 8))` : ×8 en temps normal, ×16 après un
round perdu.

## Les réglages, pour ajuster soi-même

Tout se règle dans le bloc en tête du `[Statedef -2]` de `AI.cns` (dans les trois variantes) :

| Réglage | Valeur finale | Effet |
|---|---|---|
| `power >= 1500 - (var(16) * 500)` | 1500, puis 1000 après un round perdu | Coût d'entrée de la transfo |
| `random < (ailevel * (8 + var(16) * 8))` | ×8, puis ×16 | Fréquence de décision |
| Bornes de `var(9)` | Tranches de 125 | Poids de chaque forme |
| `p2bodydist x >= 100` | 100 | Distance minimale de l'adversaire pour se transformer |

## Méthode de livraison

- Les correctifs sont appliqués par script Python, avec une vérification que chaque texte remplacé
  apparaît **exactement une fois** avant de l'écrire.
- Les fichiers sont lus et réécrits en **latin-1** en gardant les fins de ligne **CRLF**.
- Les zips gardent les noms de dossiers et de fichiers d'origine, pour un glisser-déposer direct
  dans le dossier `chars`.
- Chaque bloc ajouté porte un commentaire `PATCH` qui documente ses réglages dans le fichier
  lui-même.

## Leçons à retenir

1. Dans un `[Statedef -2]`, **l'ordre compte** : un bloc placé après des dizaines de
   `ChangeState` d'attaque ne se déclenche presque jamais.
2. Comparer deux IA, c'est d'abord comparer leurs **échelles de probabilité**
   (`ailevel*2` contre `ailevel*100`, c'est 50 fois plus rapide), avant les stats.
3. Une forme garantie peut se cacher dans l'intro, dans le `.cmd` ou dans une desperation, pas
   seulement dans `AI.cns`.
4. Toujours séparer le comportement de l'IA (`ailevel > 0`) de celui du joueur humain
   (`ailevel = 0`), pour corriger l'un sans casser l'autre.
5. L'équilibrage IA contre IA se juge sur une série de matchs (une dizaine au moins), pas sur un
   seul.
