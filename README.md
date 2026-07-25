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
| `docs/` | Liste du roster et recherches de remplaçants |

**Ne sont pas versionnés** : les fichiers de personnages (sprites, sons), beaucoup trop lourds —
le pack complet dépasse 8 Go.

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
| `/2` | 400 |
| `/3` | 267 |
| `/4` | 200 |
| `/8` | 100 |

13 personnages du roster sont concernés.

### 2. Ne jamais créer de statedef négatif

Si deux fichiers chargés par le même `.def` définissent le même `[Statedef -1]`, `-2` ou `-3`,
**l'un écrase l'autre** et la logique du personnage meurt silencieusement.

C'était la cause racine de presque tous les bugs du projet : auras invisibles, projectiles
absents, transformations bloquées. Toujours insérer dans le statedef qui existe déjà.

Ordre de chargement MUGEN : `cns`/`st`, puis `st1`…`stN`, puis **`stcommon` en dernier**.
Le `stcommon` gagne donc en cas de conflit.

### 3. Pièges d'analyse des fichiers

- Les en-têtes s'écrivent `[Statedef 200]` **ou** `[Statedef 200, nom du coup]`.
- Pour savoir si un état est une attaque, n'examiner que son **en-tête** : un `movetype = A`
  plus bas appartient à un contrôleur interne.
- Les valeurs du `.def` peuvent contenir des espaces en fin de ligne et des antislash Windows.
- Les dossiers `OP/`, `Nerf/`, `1.0/` sont des variantes que MUGEN **ne charge jamais**.
- Les fichiers de personnages s'écrivent en **latin-1**, jamais en UTF-8.

### 4. Mémoire

MUGEN 1.1 est en 32 bits. Le menu complet chargeait 1663 personnages au démarrage — MUGEN ouvre
le fichier de sprites de chacun pour lire son portrait, ce qui saturait la mémoire avant même
qu'un combat commence.

D'où le menu réduit à 75 personnages, devenu le menu par défaut. Réglages associés dans
`mugen.cfg` : `PlayerCache = 0`, `Precache = 0`, `BufferedRead = 0`, `UnloadSystem = 1`.

> Quand MUGEN manque de mémoire, des sprites échouent au chargement et **s'affichent en noir**.
> Ce symptôme a longtemps été pris pour un problème de palette.

---

## Suivi automatique

Une base SQLite (`roster_tournoi.db`) conserve l'état du roster et l'historique des contrôles :

- `Personnages` — état de référence de chacun
- `Controles` — une ligne par exécution hebdomadaire
- `Mesures` — une ligne par personnage et par semaine

Un contrôle automatique tourne chaque dimanche : il mesure les 75, enregistre en base et
**compare à la semaine précédente** pour détecter les régressions.
