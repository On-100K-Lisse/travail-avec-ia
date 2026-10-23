# Plan de session — Tests MUGEN en mode Watch (13 cas)

**Préparé le :** 2026-10-07
**Source :** recherche du 2026-10-03 (`docs/RECHERCHE_VERSIONS.md`, dépôt mugen-recherches-kai)
**Périmètre :** les 13 cas ouverts uniquement. Les 55 autres personnages sont tranchés — on n'y touche pas.
**Règle d'or :** rien n'est définitif avant TON test en Watch. Ce plan propose un ordre et une méthode, pas des conclusions.

---

## Protocole de test (à appliquer à chaque cas)

- Moteur : MUGEN 1.1, mode Watch (CPU vs CPU)
- Adversaire de référence : un perso stable du roster (ex. Shadow JUS ou Sonic JUS d'Andrew55 — la norme T3)
- Format suggéré : 5 matchs, rounds par défaut du fullgame
- Stage neutre, sans gimmick
- Noter **pendant** le match, pas après (la mémoire est courte en Watch)

---

## Points à observer (chaque match)

**Qualité de l'IA :**
- Agressivité : attaque-t-il, ou reste-t-il planté / recule-t-il en boucle ?
- Variété : utilise-t-il son moveset, ou spamme-t-il 1-2 coups ?
- Hypers/supers : jamais utilisés ? utilisés à bon escient ? spammés ?
- Défense : garde, esquives, anti-airs — ou passoire ?
- Gestion de la distance : cohérente avec l'archétype du perso ?
- Cheap/bugs : infinites, patterns abusifs, dégâts absurdes, freeze, coups qui whiffent en boucle, IA inactive

**Fidélité au lore :**
- Moveset : les coups existent-ils dans l'univers d'origine ?
- Sprites/animations : style cohérent avec le personnage ?
- Hypers : tirés du canon ou inventés ?

**Stabilité technique :**
- Chargement OK ? Pas de crash ? (à surveiller en priorité sur les gros SFF)

---

## Ordre de passage

### Bloc A — Remplacements à valider (les plus impactants d'abord)
1. **Donkey Kong (MvC) + patch IA Holn** — actuel : DMSderProgamer. Meilleur ratio effort/gain : on garde le perso, on change l'IA. → Tester le perso avec et sans patch si possible.
2. **Rayman → Eliphus & Hiperhazz (2021)** — actuel : Ali (déséquilibré : infinites, hypers absurdes). → Vérifier que la version E&H est équilibrée ET que son IA joue vraiment.
3. **Knuckles → Andrew55** — actuel : Micheal22. Aligne le perso sur la norme T3 (même auteur que Sonic/Shadow). → Comparer directement contre le roster T3 existant.
4. **Cream → Gladiacloud** — actuel : Micheal22. Auteur déjà éprouvé dans le roster (son Vector tourne depuis le 26/07). → Tester.
5. **Solid Snake (Chibi) → SeanAltly** — actuel : Ryon (chibi non-canon, disqualifié d'office au barème lore). → Tester.
6. **Solid Snake (STG) → SeanAltly** — actuel : STG (non documenté). Même auteur alternatif que le n°5 → tester les deux Snakes à la suite pour comparer.
7. **Mighty → Sonikku edit (2019)** — actuel : Greensonic2006 (pré-1.0). → Vérifier MUGEN 1.0 + qualité réelle de l'IA.

### Bloc B — Patch IA seul
8. **Sonic Classic + patch IA NitroMugenGaming** — actuel : Suga Saki R. Patch conçu pour cette version exacte. → Tester avec et sans patch.

### Bloc C — Cas conditionnels et comparatifs
9. **Bowser Jr → Diamondswag12** — conditionnel : le lien n'a pas été vérifié pendant la recherche. → D'abord confirmer que la version existe et se lance, puis tester.
10. **Nazo : Micheal2200 (actuel) vs Inseph 2020** — comparatif direct : faire jouer chaque version contre la référence, puis l'une contre l'autre.
11. **Silver : Micheal22 (actuel) vs Gladiacloud** — même protocole comparatif.

### Bloc D — Pistes (seulement si besoin)
12. **Mario : Planeptune (actuel) vs ShinRyoga & NeOaNkH (piste)** — seulement si l'IA Planeptune déçoit en Watch.
13. **Tails : Micheal22 (actuel) vs Andrew55 (piste)** — seulement si tu veux pousser la cohérence T3.

---

## Grille de notation (prête à coller dans le dépôt)

Copier le bloc ci-dessous pour chaque cas testé :

```
### [Nom du perso] — testé le [date]

**Versions comparées :** [actuelle] vs [alternative]

**Fidélité au lore : __/10**
- Moveset canon : __/4
- Sprites/animations fidèles : __/3
- Hypers/supers canoniques : __/3

**Qualité de l'IA : __/10**
- Agressivité équilibrée (ni passive ni suicidaire) : __/3
- Variété du moveset utilisé : __/2
- Défense et gestion de la distance : __/2
- Usage des hypers (ni jamais, ni spam) : __/2
- Absence de cheap/bugs/infinites : __/1
  (retirer le point si cheap ou bug bloquant observé)

**Stabilité technique :** [OK / ⚠️ préciser]

**Verdict (à trancher après le test) :** [GARDER / REMPLACER / À REVOIR]
**Notes :**
```

**Règle de lecture :** les deux notes sont à égalité — un 9/10 en lore ne compense pas un 3/10 en IA, et inversement. En cas d'égalité ou de doute → « À REVOIR », pas de décision forcée.

---

## Rappels du projet

- Aucun téléchargement ni remplacement sans ta validation — ce plan prépare l'ordre et la méthode, rien de plus.
- Les 55 cas tranchés ne sont pas rouverts ici.
- Les liens directs de téléchargement sont à récupérer au moment d'agir (pages conteneurs vérifiées vivantes le 2026-10-03 ; liens directs non extraits : patch Holn, Drive Nitro, thread Cream Gladiacloud, Bowser Jr Diamondswag12).
- Les 4 personnages déjà remplacés avec Claude (Kirby→OHMSBY, Master Hand→N64Mario, Bowser→K.Y-Shanxi, Vector→Gladiacloud) ne font pas partie de cette session.
