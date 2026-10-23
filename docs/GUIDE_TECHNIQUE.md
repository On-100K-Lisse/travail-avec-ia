# Guide technique — MUGEN (règles et pièges connus)

**À quoi sert ce document :** éviter de redécouvrir à la dure les règles techniques
du projet de tournoi MUGEN. Il rassemble ce qui a été appris en travaillant sur le
roster, dans un langage simple.

**Contexte du projet :** tournoi en mode Watch (l'IA du jeu contrôle les deux
personnages), moteur MUGEN 1.1, pack Jump Force Mugen V14, 75 personnages classés
selon un barème « Lore-Accurate » (voir `ROSTER_TOURNOI.md`).

## Les règles

### 1. Les noms de fichiers ne se touchent pas

Garder les noms de fichiers **exactement** comme à l'origine. MUGEN retrouve ses
ressources par ces noms : renommer un fichier peut casser un personnage sans message
d'erreur clair.

Attention au piège classique : **le nom du dossier d'un personnage n'est pas le nom
de son fichier `.def`** (le fichier de définition). Quand un document parle d'un
personnage, vérifier lequel des deux noms est utilisé.

### 2. Pas de paramètre `AILevel` sur les personnages pré-1.0

Le projet compte **11 personnages** créés pour des versions de MUGEN antérieures à
la 1.0. Règle du projet : ne **pas** leur ajouter le paramètre `AILevel` (le réglage
qui indique au moteur le niveau de l'IA). Sur ces anciens personnages, ce réglage ne
s'applique pas comme prévu.

### 3. Jamais de `statedef` négatif en double

Un `statedef` est un bloc qui définit un état du personnage (attaque, saut, chute...).
Certains blocs utilisent des numéros négatifs pour des comportements spéciaux.
Règle : **jamais deux `statedef` avec le même numéro négatif dans le même fichier** —
le moteur ne garde pas les deux, et le personnage se comporte alors de façon
imprévisible.

### 4. Encodage des fichiers : latin-1

Les fichiers de personnages s'éditent en **latin-1** (un encodage texte ancien que
MUGEN comprend). Enregistrer en UTF-8 peut casser la lecture des caractères spéciaux
et produire des erreurs difficiles à retracer. Après toute édition, vérifier
l'encodage du fichier avant de le remettre en place.

### 5. Tester en Watch, contre une référence stable

Un personnage modifié ou remplacé se valide en mode Watch, pas à l'œil :

- **5 matchs** contre un personnage de référence stable du roster (la norme du
  projet : Sonic et Shadow, qui fixent le niveau du Tier 3) ;
- **stage neutre**, sans élément spécial qui fausserait le résultat ;
- noter **pendant** les matchs (voir `PLAN_SESSION_WATCH.md` pour la grille complète
  de notation : fidélité au lore et qualité de l'IA, notées séparément).

## Crédits et provenance

Les personnages appartiennent à leurs **auteurs d'origine** (règle 3 du dépôt) : les
documents du tournoi les nomment quand ils sont connus. Si l'auteur d'un personnage
est inconnu, on écrit « inconnu (temporaire) » (règle 5) plutôt que de deviner.

## État de ce guide

Règles de travail du projet au 23 octobre 2026. Ce guide grandit à mesure que de
nouveaux pièges sont découverts — et documentés ici plutôt que redécouverts.
