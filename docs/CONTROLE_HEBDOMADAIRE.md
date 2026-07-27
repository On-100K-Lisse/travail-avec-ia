# Le contrôle hebdomadaire

`outils/controle_hebdo.py` tourne chaque dimanche. Il est **en lecture seule** : il ne
modifie aucun fichier du jeu. Il écrit un rapport, un JSON, et avec `--sql` les `INSERT`
pour la base.

```
python3 controle_hebdo.py          # rapport lisible + controle_resultat.json
python3 controle_hebdo.py --sql    # INSERT pour les tables Controles et Mesures
```

---

## Ce qu'il vérifie

### Anomalies — à corriger

| Contrôle | Pourquoi |
|---|---|
| Stats vs barème | le socle |
| Réaction IA vs cible (±12 %) | un boss doit *lire* le combat plus vite |
| Statedefs négatifs en double | cause racine historique de presque tous les bugs |
| Bloc de patch pointant un état inexistant | MUGEN échoue **en silence** |
| `AILevel` sur un perso pré-1.0 | fait planter le chargement |
| Fichier déclaré dans le `.def` mais absent | |
| **Dégâts au-dessus du plafond du tier** | *ajouté le 27/07* |
| **Soin instantané `lifemax/N` trop gros** | *ajouté le 27/07* |
| **`TargetLifeAdd` hors normes** | *ajouté le 27/07* |
| **Dégâts indexés sur `enemy,lifemax` ou `enemy,const(data.defence)`** | *ajouté le 27/07* |

### Avertissements — défauts d'origine, pas des régressions

| Contrôle | Note |
|---|---|
| Dégâts sur garde supérieurs au coup | 24 personnages concernés depuis toujours ; une partie vit dans les fichiers moteur `.reb1` des Mythos, qu'on ne peut pas juger sûrement |

---

## Pourquoi les quatre contrôles ajoutés le 27 juillet

Parce que trois affaires ont montré que **`life` / `attack` / `defence` ne pilotent pas tout**.

1. **Solid Snake** — ses dégâts étaient indexés sur la vie max *et* la défense de sa cible.
   Chaque montée en tier rendait l'adversaire **plus** vulnérable. ×4,12 contre un T0.
2. **Seelkadoom** — chacune de ses formes le soignait de 25 % de sa vie max. Le faire se
   transformer plus souvent, ce qui était demandé comme un nerf, était donc un buff.
3. **Fleetway Shadow** — un coup à `999 999 999`. Personne ne l'avait jamais mesuré.

---

## Comment les seuils sont calibrés

**Sur des mesures, pas sur une intuition.**

- **Plafond de dégâts** : médiane des maxima par tier (T0 200, T1 265, T2 200, T3 187),
  arrondie avec une marge pour les hypers légitimes → `DEGATS_MAX`.
- **Dégâts sur garde** : sur **4375 paires** coup/garde du roster, la médiane du ratio
  garde/coup est **0,47** et le 95ᵉ centile 0,75. Seuls **2,3 %** dépassent 1,0.
  Le seuil est donc « garde > coup ».

  > Un premier essai à « 25 % du coup » signalait **66 personnages sur 75**. Du bruit pur.
  > Quand un contrôle alerte sur presque tout le monde, c'est le contrôle qui a tort.

---

## Le test de non-régression

Rejoué sur la sauvegarde de l'ancien Solid Snake de STG, le contrôle étendu détecte :

- dégâts max **666**
- **77** dégâts indexés sur l'adversaire
- **9** `TargetLifeAdd` à quatre chiffres ou plus

Il aurait donc signalé ce personnage dès la première semaine. C'est la preuve que les règles
mordent vraiment.

---

## Deux pièges rencontrés en l'écrivant

**Le `.def` que charge le menu fait foi.** Deviner `dossier/dossier.def` puis prendre le
premier par ordre alphabétique donnait de faux écarts : sur `Kirby_OHMSBY` on lisait
`Kirby.def` (non patché, 980 PV) alors que le menu charge `KirbyAI.def` (patché, 2400 PV).
Le contrôle lit maintenant `select_tournoi.def`.

**Mes propres commentaires de patch déclenchaient mes propres contrôles.** En citant la
formule d'origine (`;PATCH : etait 'enemy,lifemax*0.08'`), le commentaire contenait
`enemy,`. Toujours couper la ligne au point-virgule avant de l'analyser.

---

## Exceptions légitimes

- **`p2name` / `p4name`** : ne jamais plafonner un bloc conditionné au nom d'un adversaire
  précis. Ce sont des contres dédiés. Neo Metal Sonic a des coups à 1500-2200 réservés à
  *Broli Remix ver1.2*, qui n'est même pas au roster.
- **Échelle d'attaque relative** (`ATTAQUE_RELATIVE`) : plafond de dégâts relevé d'autant.
- **`VOULUS`** : écarts validés avec On-100-K-Lisse, jamais signalés (Solaire, metalknuckles).
