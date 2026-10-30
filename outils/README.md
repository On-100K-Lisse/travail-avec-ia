# Les outils

Scripts Python écrits au fil du projet. Tous lisent et écrivent les fichiers de personnages en
**latin-1** — jamais en UTF-8.

> Seuls `lore.py` (la table de référence) est versionné intégralement pour l'instant.
> Les autres vivent dans `travaille de claude/outils/` sur la machine. Cet index existe pour
> qu'on sache ce qui a déjà été écrit et ne pas le refaire.

---

## Référence

| Script | Rôle |
|---|---|
| **`lore.py`** | La table officielle : dossier → (tier, classe), stats par tier, cibles de réaction, coefficients de vitesse, taux de garde. **Source de vérité de tout le reste.** |

## Mesure et diagnostic — lecture seule

| Script | Rôle |
|---|---|
| `controle_hebdo.py` | Contrôle hebdomadaire du roster. Sort un rapport lisible, ou des `INSERT` SQL avec `--sql`. **Ne modifie aucun fichier du jeu.** C'est lui que la tâche planifiée du dimanche exécute. |
| `check_char.py` | Analyse unitaire approfondie d'un personnage : fichiers chargés, statedefs, coups, IA, stats. |
| `verif_finale.py` | Vérification d'intégrité : `def`/`sff`/`air`, stats vs barème, marqueurs de patch, réaction, corruptions. |
| `verif_anticoin.py` | Contrôle que **tout `ChangeState` ajouté par un patch vise un état qui existe et qui n'est pas une attaque**. À relancer après chaque passe. |
| `scan_ia.py` | Où vit l'IA de chaque perso : quel fichier, quel statedef négatif porte ses `ChangeState` conditionnels. |
| `scan_dash.py` | États de déplacement sûrs, par personnage — indispensable avant de poser un bloc anti-coin. |
| `ai_analyze.py` | Rythme de décision des IA : gates `random < (ailevel*100)/N`. |
| `ai_gate2.py` | Métrique duale, couvre aussi les écritures `random <= (K*ailevel)`. |
| `reaction_global.py` | Mesure et recalibrage du temps de réaction sur **tous** les fichiers d'un perso. |
| `audit_doublons.py` | Quels personnages du `select.def` portent le même nom affiché qu'un perso de la tier list. |
| `genliste.py` | Génère la liste du roster. |

## Modification — sauvegarder avant

| Script | Rôle |
|---|---|
| `anticoin.py` | Le système anti-coin **M6**, et la correction des blocs M2/M4 qui visaient 100/105 alors que le perso utilise d'autres états. |
| `integ_final.py` | Intégration complète d'un lot de personnages. |
| `ia_shade3.py` | « Protocole Shade » : écrire une IA complète pour un personnage qui n'en a aucune. |
| `m3_fix.py` | Recablage M3 : multiplicateurs sur les vrais états + blocs d'IA cohérents. |
| `m3_transfo.py` | M3 — transformations multi-niveaux. |
| `fix_doublons.py` | Désambiguïse les noms affichés entre personnages homonymes. |
| `metal_stalker.py`, `metal_stalker2.py` | Intégration et équilibrage du Metal Sonic de StaLKeRJoX (M2 Rushdown, M4, M6). |
| `mythos_stats.py`, `mythos_reaction.py`, `mythos_m2m4regen.py` | Les personnages **Mythos** (Shiruzato) — voir l'avertissement ci-dessous. |

---

## Deux avertissements

### Les Mythos ont une échelle d'attaque différente

Les personnages de Shiruzato (`MarioMYTHOS`, `LuigiMYTHOS`, `ZeldaMYTHOS`, `GanonMYTHOS`,
`DLinkMYTHOS`, `CloudMYTHOS`) sont construits sur une base de **75**, pas 100. Leur attaque
s'applique de façon **relative**. Leur écrire la valeur brute du tier **doublerait leurs dégâts**.
D'où leurs valeurs apparemment basses au roster (75, 82, 89).

### Ne jamais refaire de passe de redirection d'animations manquantes

Une animation manquante est **inoffensive** en MUGEN. Une mauvaise redirection ne l'est pas :
celle qui avait été tentée a rendu Blaze invisible et fait passer Seethe sous le sol.

### Ne pas toucher

- les `sprpriority` négatifs
- les Explods activateurs de Clash (`pos 9999`)
- le personnage `Shadow NJPT`

---

Retour : [README de la branche](../README.md) · [Accueil du dépôt](https://github.com/On-100K-Lisse/travail-avec-ia)
