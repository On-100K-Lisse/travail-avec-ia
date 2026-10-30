# Tournoi MUGEN — configuration et outils

Configuration d'équilibrage du tournoi MUGEN d'On-100-K-Lisse (Jump Force Mugen V14, moteur MUGEN 1.1).
**75 personnages** classés selon un barème *Lore-Accurate* — la puissance canonique du personnage
dans son univers d'origine.

Le jeu tourne en **mode Watch** (IA contre IA) : tout l'équilibrage vise le comportement de
l'intelligence artificielle, pas le confort d'un joueur humain.

---

## Ce que contient ce dépôt

| Dossier | Contenu |
|---|---|
| `config/` | `select_tournoi.def` (le menu du tournoi), `mugen.cfg` (réglages moteur et mémoire) |
| `outils/` | Scripts d'équilibrage et de vérification, en Python |
| `docs/` | Roster, recherches de remplaçants, et les diagnostics détaillés |

**Ne sont pas versionnés** : les fichiers de personnages (sprites, sons), beaucoup trop lourds —
le pack complet dépasse 8 Go.

### Documents

| Fichier | Sujet |
|---|---|
| [`docs/SPRITES_NOIRS.md`](docs/SPRITES_NOIRS.md) | Pourquoi certains sprites et effets s'affichent en noir, et comment le mesurer |
| [`docs/GOD_CHARACTERS.md`](docs/GOD_CHARACTERS.md) | Quand l'équilibrage par les stats ne suffit pas — cas Solid Snake |
| [`docs/REMPLACANTS_A_TELECHARGER.md`](docs/REMPLACANTS_A_TELECHARGER.md) | Recherche de meilleures versions pour les personnages faibles |
| [`docs/ROSTER_TOURNOI.md`](docs/ROSTER_TOURNOI.md) | La liste des 75 |
| [`docs/PATCHS_IA_SONIC_SHADOW.md`](docs/PATCHS_IA_SONIC_SHADOW.md) | Transformations équiprobables de Sonic et équilibrage de la rivalité avec Shadow (patchs v1 à v5) |

---

## Le barème

| Tier | Description | Stats | Cible de réaction |
|---|---|---|---|
| **T0** | Entités cosmiques, tueurs de dieux | 2400 / 130 / 130 | 381 |
| **T1** | Menaces planétaires, héros légendaires | 1900 / 125 / 120 | 353 |
| **T2** | Destructeurs de villes | 1600 / 115 / 115 | 325 |
| **T3** | L'élite, les héros, les mortels | selon sous-tier | 282,5 |

**Règle d'or** : le Tier 3 est le plancher. Personne ne descend en dessous. Sonic et Shadow y
fixent la norme.

Sous-tiers du T3 : Polyvalent 1350/105/105 · Cogneur 1450/115/115 (vitesse ×0,9) ·
Rapide 1250/105/100 (vitesse ×1,08) · Zoner 1250/105/95 · Stratège 1300/100/105

### La « réaction »

C'est une mesure maison : la probabilité moyenne qu'un personnage décide d'agir, par frame,
au niveau d'IA 8 — exprimée sur 1000. Elle se calcule sur tous les fichiers réellement chargés
par le `.def`, en excluant les blocs ajoutés par l'équilibrage. Tolérance ±12 %.

Concrètement : un boss ne frappe pas seulement plus fort, il *lit* le combat plus vite.

### Les classes de comportement

| Classe | Taux de garde | Comportement |
|---|---|---|
| Cogneur | 1 chance sur 1,6 | Encaisse au lieu de fuir, reste au contact |
| Stratège | 1 sur 2,5 | Garde haute, repli tactique pour laisser agir ses helpers |
| Polyvalent | 1 sur 3 | Équilibré, pas de règle de fuite |
| Zoner | 1 sur 4 | Recule quand l'ennemi colle, garde ses distances |
| Rapide | 1 sur 8 | Garde peu, fonce dès que l'adversaire s'éloigne |

---

## Règles techniques à ne jamais oublier

Ces règles ont été apprises en cassant des choses. Elles valent d'être relues avant toute
modification.

### 1. `AILevel` n'existe pas avant MUGEN 1.0

Un personnage dont le `.def` déclare une version antérieure (ex. `04,14,2002`) **plante au
chargement** si un bloc utilise `AILevel` :

```
Trigger not valid for mugenversion of character: AILevel
```

**Solution** : écrire les probabilités en valeur directe. `random < (AILevel*100)/1.5` devient
`random < 533` — la même probabilité au niveau d'IA 8, celui des tournois.

| Écriture moderne | Équivalent pré-1.0 |
|---|---|
| `/1.5` | 533 |
| `/1.6` | 500 |
| `/2` | 400 |
| `/3` | 267 |
| `/4` | 200 |
| `/6` | 133 |
| `/8` | 100 |

**Mieux encore** : plutôt que de forcer `mugenversion = 1.0`, garder la version déclarée par
l'auteur et activer le bloc sur **le propre drapeau d'IA du personnage** — `var(58) = 1` chez
Bowser (K.Y-Shanxi), `var(59) != 0` chez Master Hand (N64Mario). Le bloc ne se déclenche alors
que quand l'IA d'origine est active, et aucun trigger moderne n'entre dans un fichier ancien.

> `mugenversion` peut valoir une date non standard (`2011.05.14`). Dans le doute, traiter comme
> pré-1.0 : les valeurs directes ne coûtent rien et ne peuvent pas planter.

### 2. Ne jamais créer de statedef négatif

Si deux fichiers chargés par le même `.def` définissent le même `[Statedef -1]`, `-2` ou `-3`,
**l'un écrase l'autre** et la logique du personnage meurt silencieusement.

C'était la cause racine de presque tous les bugs du projet : auras invisibles, projectiles
absents, transformations bloquées. Toujours insérer dans le statedef qui existe déjà.

Ordre de chargement MUGEN : `cns`/`st`, puis `st1`…`stN`, puis **`stcommon` en dernier**.
Le `stcommon` gagne donc en cas de conflit.

**Où insérer un bloc** : après le `[Statedef -1]`, mais **avant le premier contrôleur de type
`ChangeState`**. Cela respecte la règle « le premier ChangeState gagne » tout en laissant
s'exécuter les `VarSet` d'activation d'IA placés en tête.

### 3. Pièges d'analyse des fichiers

- Les en-têtes s'écrivent `[Statedef 200]` **ou** `[Statedef 200, nom du coup]`.
- Pour savoir si un état est une attaque, n'examiner que son **en-tête** : un `movetype = A`
  plus bas appartient à un contrôleur interne.
- Les valeurs du `.def` peuvent contenir des espaces en fin de ligne et des antislash Windows.
- Les dossiers `OP/`, `Nerf/`, `1.0/` sont des variantes que MUGEN **ne charge jamais**.
- Les fichiers de personnages s'écrivent en **latin-1**, jamais en UTF-8.
- **Le nom du dossier n'est pas toujours le nom du `.def`.** Écrire `MonPerso` dans `select.def`
  fait chercher `chars/MonPerso/MonPerso.def`. Cas rencontrés : `Metal Sonic` (→ `MetalSonic.def`)
  et `Emerl (2)` (→ `Emerl.def`). Dans le doute, écrire `Dossier/fichier.def`.
- **Un dossier peut contenir plusieurs `.def`** : une version nue et une version avec patch d'IA.
  `Vector_Gladiacloud` a `vector.def` (sans IA) et `AI-vector.def` (avec). Pointer le mauvais
  annule tout l'intérêt du personnage.
- **Le nom d'un dossier ne prouve rien sur son contenu** : toujours lire le champ `author` du `.def`.
- Des commandes `cpu1`..`cpu10` déclarées dans le `.cmd` ne prouvent **pas** qu'une IA existe :
  vérifier que le `.cns` lit vraiment le var d'activation.

### 4. Mémoire, et sprites noirs — deux problèmes distincts

> **Correction du 26/07.** Ce dépôt affirmait auparavant que les sprites noirs venaient de la
> saturation mémoire. C'était faux, ou du moins incomplet.

**Mémoire.** MUGEN 1.1 est en 32 bits. Le menu complet chargeait 1663 personnages au démarrage —
MUGEN ouvre le fichier de sprites de chacun pour lire son portrait, ce qui saturait la mémoire
avant même qu'un combat commence. D'où le menu réduit à 75 personnages, devenu le menu par défaut.
Réglages associés : `PlayerCache = 0`, `Precache = 0`, `BufferedRead = 0`, `UnloadSystem = 1`.

**Sprites noirs.** La vraie cause est écrite dans `mugen.cfg` lui-même :

> `System and DirectX modes do not support advanced features such as RGB sprites`

Un sprite RGB 32 bits affiché en `RenderMode = System` **sort noir**. Idem pour une surface trop
grande, qui échoue silencieusement à l'allocation. 21 des 75 personnages du roster sont concernés.

→ `RenderMode = OpenGL`. Détail complet et méthode de mesure dans
[`docs/SPRITES_NOIRS.md`](docs/SPRITES_NOIRS.md).

### 5. Les stats ne pilotent pas tout

Quand un personnage reste écrasant après un ajustement de stats, chercher :

- `TargetLifeAdd` — retire de la vie **directement**, sans passer par `attack` ni `defence`
- des dégâts exprimés en pourcentage de `enemy,lifemax`
- le **second paramètre** de `damage` — les dégâts sur garde, parfois absurdes
- `ReversalDef` couvrant tous les attributs d'attaque
- un mode caché activé sur les palettes 11 et 12

Cas complet dans [`docs/GOD_CHARACTERS.md`](docs/GOD_CHARACTERS.md).

---

## Suivi automatique

Une base SQLite (`roster_tournoi.db`) conserve l'état du roster et l'historique des contrôles :

- `Personnages` — état de référence de chacun
- `Controles` — une ligne par exécution hebdomadaire
- `Mesures` — une ligne par personnage et par semaine

Un contrôle automatique tourne chaque dimanche : il mesure les 75, enregistre en base et
**compare à la semaine précédente** pour détecter les régressions.

---

## Journal

### 26 juillet 2026

- **4 remplacements** : Kirby → OHMSBY + patch IA Holn · Master Hand → N64Mario ·
  Bowser → K.Y-Shanxi · Vector → Gladiacloud + patch IA. Stats, garde M2 et anti-coin M6 appliqués.
- **Sprites noirs** : cause trouvée (`RenderMode = System` ne gère pas les sprites RGB),
  bascule en OpenGL.
- **Solid Snake** : ses dégâts montaient avec la vie max et la défense de sa cible (×4,12 contre
  un T0). Formule retirée sur 66 lignes, dégâts et chip plafonnés, morts instantanées bornées.
- Références orphelines réparées dans les deux menus, encodage latin-1 rétabli,
  `Emerl (2)` corrigé.

### Avant

- Refonte Lore-Accurate des 75, système anti-coin M6, profils de garde M2,
  base SQLite et contrôle hebdomadaire, menu tournoi séparé du menu complet.

## Navigation

**Documents de cette branche :**

- [Le contrôle hebdomadaire](docs/CONTROLE_HEBDOMADAIRE.md)
- [Les outils](outils/README.md)

**Le même projet chez les autres IA :**

- [ChatGPT — Tournoi MUGEN](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/chatgpt/mugen)
- [Gemini — Tournoi MUGEN](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/gemini/mugen)
- [Kai — Tournoi MUGEN](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/kai/mugen)

**Accueil :** [Présentation du dépôt et sommaire (branche `main`)](https://github.com/On-100K-Lisse/travail-avec-ia) · [Méthode de travail multi-IA](https://github.com/On-100K-Lisse/travail-avec-ia/blob/main/docs/METHODE_TRAVAIL_MULTI_IA.md)
