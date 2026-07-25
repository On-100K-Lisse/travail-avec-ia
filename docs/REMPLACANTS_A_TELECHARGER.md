# Remplaçants trouvés pour les personnages faibles ou cassés

Recherche du 25 juillet. Une meilleure version a été cherchée pour chaque personnage
cassé, désactivé, au moveset pauvre ou sans IA d'auteur.

Rien n'a été téléchargé ni installé — ce sont des pistes vérifiées.

---

## Les 4 remplacements qui valent le coup

### 1. Kirby → version d'OHMSBY  *(Tier 0)*

**Problème actuel** : `kirby8` (mugenx, 2002) a un défaut d'auteur — son `[Statedef -2]` est
écrasé par son propre fichier de states communs, donc une partie de l'IA de son auteur est morte.
Il est aussi pré-1.0.

**Remplaçant** : OHMSBY est l'un des auteurs les plus réputés de la scène MUGEN (personnages
modernes 1.1, très propres). Un **patch d'IA par Holn** existe en plus.

- https://www.andersonkenya1.net/files/category/86-kirby/
- https://sites.google.com/view/ohmsby-mugen

### 2. Bowser → K.Y-Shanxi ou k6666orochi  *(Tier 1)*

**Problème actuel** : les **deux** Bowser d'origine sont morts. Teixeira plante le jeu (SFF de
92 Mo illisible), MMP a la moitié de son moveset absente. La place est tenue par `BowserSuminI`,
qui fonctionne mais reste modeste (32 attaques, pré-1.0).

- **K.Y-Shanxi** — *« IA custom remarquable par sa difficulté »*, moveset fidèle, classé
  explicitement **boss / mid-boss**. Un patch existe pour réduire la difficulté.
  - https://mugen.fandom.com/wiki/Bowser/K.Y-Shanxi's_version
  - https://mugenarchive.com/forums/downloads.php?do=file&id=11325-bowser-k-y-shanxi
- **k6666orochi** — versions 2.7 à 3.0, la 2.7 livrée avec un patch d'IA par Holn.
  - https://www.andersonkenya1.net/files/file/2982-bowser/

### 3. Master Hand → version de N64Mario  *(Tier 0)*

**Problème actuel** : `ssbbmasterhand` a 139 attaques dans ses fichiers mais **seulement 11 sont
reliées à une commande**, et aucune IA d'auteur. Maigre pour un Tier 0.

**Remplaçant** : *« très complète en capacités, ce qui ajoute beaucoup de difficulté »*. Deux
fichiers `.def` : `MastrHnd` **avec super armure**, et `MHand` sans.

- https://mugen.fandom.com/wiki/Master_Hand/N64Mario's_version
- https://mugenarchive.com/forums/downloads.php?do=file&id=46276-master-hand-n64mario

### 4. Vector → version de Gladiacloud  *(Tier 3 Zoner)*

**Problème actuel** : 22 attaques et aucune IA d'auteur.

**Remplaçant** : Gladiacloud, auteur reconnu pour ses personnages Sonic soignés. Patch d'IA
« Stronger » disponible séparément.

- https://mugenfreeforall.com/topic/36689-vector-the-crocodile-released-by-gladiacloud/
- https://www.andersonkenya1.net/files/file/2988-vector/

---

## Les cas où il n'existe rien de mieux — vérifié

| Personnage | Ce qui a été trouvé |
|---|---|
| **Dark Pit** | Une seule version existe (UndeadBoi / J. NEWMAN), et **son auteur écrit lui-même** : *« c'est mon premier personnage, il y a des problèmes que je n'arrive pas à corriger »*. Correspond aux 20 attaques et à l'absence d'IA mesurées. |
| **Koopa (BOWSER2)** | La fiche officielle confirme : *« pas de Specials ni de Hypers, ce qui donne un moveset limité »*. Ses 11 attaques sont un choix d'auteur, pas un bug. |
| **Wave** | Sa seule version (TheJvv15) est référencée dans un wiki de **médias MUGEN perdus** — fichier rare, sans alternative connue. |

---

## Recommandation

1. Commencer par **Kirby et Bowser** — les deux plus gros gains, sur des places de haut de tableau.
2. Vérifier chaque fichier **avant** installation : taille du SFF, `mugenversion`, présence réelle
   d'une IA, coups morts.

> **Attention au poids.** Le pack sature déjà la mémoire de MUGEN. Au-delà de ~60 Mo de SFF, on
> prend un risque — c'est exactement ce qui a tué le Bowser de Teixeira.
