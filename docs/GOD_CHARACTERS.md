# Quand l'équilibrage par les stats ne suffit pas

> Cas d'école : `Solid_Snake`, 26 juillet 2026.

Un personnage restait écrasant malgré des stats de Tier 3 (1250 PV / 105 atk / 95 déf).
La raison : **ses dégâts ne passaient pas du tout par le système de stats.**

---

## Le mécanisme

Ses 77 `HitDef` utilisaient tous cette formule :

```
damage = N + ceil( ((enemy,const(data.defence) * 0.01) * enemy,lifemax * N/1000) )
```

Ses dégâts **augmentent avec la vie max et la défense de sa cible**.

| Cible | Multiplicateur réel |
|---|---|
| T3 — 1250 PV, 95 déf | **× 2,19** |
| T1 — 1900 PV, 120 déf | **× 3,28** |
| T0 — 2400 PV, 130 déf | **× 4,12** |

Conséquence perverse : **chaque montée en tier rendait la cible plus vulnérable.**
Plus on renforçait le roster, plus Snake dominait. Baisser ses propres stats était sans
effet — la formule ne les regarde pas.

---

## Les autres vecteurs, tous invisibles aux stats

| Vecteur | Ce qu'on trouvait |
|---|---|
| **Dégâts sur garde** (2ᵉ paramètre de `damage`) | jusqu'à **16161** — bloquer l'attaque tuait |
| `TargetLifeAdd` | `-570000`, et `-99999 - 999 % de la vie max` |
| `ReversalDef` | `Reversal.Attr = SCA, NA, SA, HA, NT, ST, HT, NP, SP, HP, AA, AT, AP` — contre absolu |
| `sprpriority` | `2147483647` (entier max) |
| Mode caché | activé **uniquement sur les palettes 11 et 12** |

### Le piège des palettes 11/12

Convention fréquente chez les auteurs japonais : les palettes **11 et 12** activent un
mode « dieu ». Ici, `Option.txt` désactivait la mort instantanée avec
`trigger1 = palno != [11,12]` — donc **sur ces deux palettes, elle restait active**.

En mode Watch, MUGEN choisit les palettes tout seul. Le personnage devenait
aléatoirement invincible.

---

## Méthode de correction

1. **Retirer le terme d'échelle** `+ceil(défense × vie_max_adverse)` — 66 lignes.
   Cela préserve l'équilibre relatif voulu par l'auteur, tout en remettant le personnage
   sous le contrôle du stat `attack`.
2. **Plafonner les dégâts bruts** au plafond du tier (200 pour un T3, valeur fixée par
   Sonic_Nitro) — 5 coups.
3. **Plafonner les dégâts sur garde** à 25 % du coup — 32 lignes.
4. **Borner les `TargetLifeAdd`** entre -70 et -120 — 6 contrôleurs.
5. **Neutraliser la condition de palette** : `palno != [11,12]` → `1`.

### Résultat

| Personnage | Coups | Médiane | Max |
|---|---|---|---|
| Solid_Snake **après** | 134 | 38 | 200 |
| ChibiSolid Snake **après** | 26 | 23 | 200 |
| Link | 109 | 25 | 175 |
| Sonic_Nitro | 546 | 10 | 200 |
| Amy Rose | 84 | 15 | 80 |

---

## Piège de patch à connaître

Chez ce type de personnage, `damage` **n'est pas un nombre** : c'est une expression, avec
un second paramètre optionnel (les dégâts sur garde).

```
damage = 666+ceil(((enemy,const(data.defence)*0.01)*enemy,lifemax*0.666)),8
```

Un regex naïf comme `^\s*damage\s*=\s*(\d+)` ne capture que `666` et laisse le reste de la
formule pendre **après** le point-virgule du commentaire — la ligne devient silencieusement
fausse. Erreur réellement commise ici, rattrapée à la vérification.

**Toujours capturer la ligne entière**, second paramètre et commentaire de fin compris.

---

## Signes qu'on a affaire à un god character

- `sprpriority` ou `SuperMoveTime` proches de l'entier max (2147483647)
- `ReversalDef` couvrant tous les attributs
- des `TargetLifeAdd` à cinq chiffres ou plus
- des dégâts exprimés en pourcentage de `enemy,lifemax`
- un `Option.txt` / `Config.txt` avec des interrupteurs d'auteur
- des états numérotés très haut (8117, 40000, 575757)
- des commentaires en japonais mentionnant 即死 (mort instantanée) ou 神 (dieu)

> Ces personnages ne sont pas cassés : ils sont **conçus** pour battre n'importe qui.
> Les intégrer à un tournoi équilibré demande une passe dédiée, pas un ajustement de stats.
