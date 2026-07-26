# Les sprites et effets noirs

> Diagnostic du 26 juillet 2026. **Ce document corrige une conclusion précédente** :
> les sprites noirs avaient été attribués à la saturation mémoire. C'était faux, ou du
> moins incomplet.

---

## La cause est écrite dans mugen.cfg

Le fichier de configuration contient ce commentaire d'origine, jamais relié au problème :

> `RenderMode` — Screen rendering mode.
> `OpenGL` - Experimental OpenGL renderer (recommended)
> `System` - default SDL rendering mode (e.g. windib in Windows)
> **`System and DirectX modes do not support advanced features such as RGB sprites`**

Le jeu tournait en `RenderMode = system`.

**Un sprite RGB 32 bits affiché dans ce mode ne s'affiche pas : il sort noir.**

S'y ajoute une seconde limite : en mode logiciel, une surface trop grande échoue à
l'allocation — et échoue en silence, donc en noir elle aussi.

---

## Comment mesurer ça soi-même

L'en-tête d'un SFF v2 se lit directement, sans outil externe :

| Offset | Champ |
|---|---|
| 0–11 | signature `ElecbyteSpr\0` |
| 12–15 | version (`verlo3, verlo2, verlo1, verhi`) — `verhi >= 2` = SFF v2 |
| 36 | offset du premier nœud de sprite |
| 40 | nombre de sprites |
| 56 | `ldatalength` — taille des données non compressées |
| 64 | `tdatalength` |

Chaque nœud de sprite fait **28 octets** :

| Offset | Champ |
|---|---|
| +4 | largeur (uint16) |
| +6 | hauteur (uint16) |
| +15 | profondeur de couleur (uint8) — **≥ 24 = sprite RGB** |

```python
import struct
h = open(sff, "rb").read(80)
assert h[:11] == b"ElecbyteSpr" and h[15] >= 2
off = struct.unpack_from("<I", h, 36)[0]
n   = struct.unpack_from("<I", h, 40)[0]
f = open(sff, "rb"); f.seek(off); blob = f.read(28 * n)
for i in range(n):
    w, hh = struct.unpack_from("<HH", blob, i*28 + 4)
    depth = blob[i*28 + 15]          # >= 24 -> RGB, invisible en RenderMode System
    trop_grand = w > 2048 or hh > 2048
```

> Bonus : sur ce pack, `ldatalength` ≈ taille du fichier. Les SFF v2 y sont quasi non
> compressés, donc **taille disque = coût mémoire**.

---

## Résultat sur le roster

**21 des 75 personnages** ont des sprites RGB ou surdimensionnés.

| Personnage | RGB 32 bits | > 2048 px | Plus grande dimension |
|---|---|---|---|
| **Sonic_Nitro** | **2** | **58** | **3253 px** |
| Sonic V1.0 JUS AI Patch | 2 | 58 | 3253 px |
| **Link** | **43** | 0 | 1600 px |
| Metal Shadow | 0 | **223** | 2147 px |
| Fleetway Shadow1 | 0 | 42 | 3253 px |
| Shadow JUS Updated | 0 | 24 | 3253 px |
| Sonow (Canon) | 0 | 22 | 3253 px |
| Shadic / Shadic v2 | 0 | 22 | 3253 px |
| Android Shadow | 0 | 20 | 2198 px |
| Sonic.Exe | 0 | 16 | 3253 px |
| Kamba The Hedgehog | 0 | 13 | 3253 px |
| Sonic Black V3 Nitro | 0 | 10 | 2198 px |
| Nazo, Metal Scourge | 0 | 6 | 2147 px |
| Seethe, Dark Shadow, Aeon, Fire Sonic, Classic Sonic, Scourge | 1–2 | 0–3 | — |

Sonic est le pire cas du pack — exactement le personnage signalé comme buggé.

---

## Le correctif

`data/mugen.cfg` : `RenderMode = system` → **`RenderMode = OpenGL`**

En OpenGL les sprites vont en VRAM : plus de limite de taille de surface, et les sprites
RGB sont gérés.

**Un seul réglage changé à la fois**, pour pouvoir conclure. La ligne de retour arrière
est écrite en commentaire juste sous le patch dans le fichier.

---

## Ce que ce diagnostic n'explique pas

**Seelkadoom (New) n'a ni sprite RGB ni sprite surdimensionné** (max 892×936). Si son bug
noir persiste après le passage en OpenGL, c'est bien la pression mémoire chez lui, et il
faudra le traiter séparément.

Les deux causes coexistent : le menu réduit à 75 personnages reste nécessaire.
