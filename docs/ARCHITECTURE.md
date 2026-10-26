# Agent de veille jeux vidéo — architecture

**État (vérifié le 26 octobre 2026) :** cahier des charges rédigé par **Gemini**
le 13 août 2026 ; projet **construit au complet par Claude le même jour**
(environ 27 fichiers, archive `veille-osint.tar.gz`, test hors ligne validé).
**Jamais déployé** : l'installation sur le portable Debian s'est arrêtée à
l'étape 1, le mot de passe d'application Gmail — l'agent n'a jamais tourné.
Histoire reconstituée et vérifiée le 26 octobre 2026 à partir des conversations
partagées de Claude et de Gemini.
**Mise à jour v2 (26 octobre 2026) :** Claude a récupéré le code d'août (ses
tests passaient encore) et l'a mis à jour en une passe : canal principal =
la base de données, lue par **Kai** via la commande `veille-kai` (alertes en
attente, accusés de livraison, recherche, santé) ; courriel devenu facultatif
et désactivé par défaut ; santé des sources dans le rapport du vendredi ;
volet animés actif (AniList, Anime News Network, r/AnimeDubs) ; filtre
anti-spoiler GTA 6 en deux couches et directive de légalité appliquée dès le
préfiltre. **84 vérifications hors ligne passent.** Pas encore déployée —
les vraies sources se valideront sur le Dell. Détails : `CHANGELOG.md` du
projet de l'agent (v2). Les backends PSN/Xbox, les métadonnées Crunchyroll /
ADN / Netflix et les CV de comédiens de doublage restent **hors périmètre**
à ce jour (décision consignée dans le README et `sources.yaml` de l'agent).
**But :** surveiller l'actualité de studios et de jeux précis, sans avoir à la
chercher soi-même chaque jour.

## Le principe

Un programme Python qui fait de la **veille OSINT** — OSINT (Open Source
Intelligence) veut dire : uniquement des informations **publiques**, recueillies
et triées automatiquement. Aucune source privée, aucun accès privilégié : l'agent
lit ce que tout le monde peut lire, mais il le fait systématiquement, et il ne
retient que ce qui compte.

## Comment ça fonctionne

1. **Collecte** — l'agent consulte ses sources publiques à intervalle régulier.
2. **Analyse** — les nouveautés sont analysées par l'API de **Gemini** (Google),
   qui résume et évalue la pertinence par rapport aux cibles surveillées.
3. **Alertes par courriel**, en deux rythmes :
   - **immédiates** pour les cibles prioritaires (ci-dessous) ;
   - **un récapitulatif hebdomadaire**, le vendredi à 17 h, pour le reste.

## Les cibles prioritaires

- CD Projekt Red et la suite de *Cyberpunk 2077* ;
- *Crimson Desert* (Pearl Abyss) ;
- Larian Studios ;
- Rockstar Games et *GTA 6* — **sans spoilers d'histoire**, filtre appliqué dès
  la conception ;
- Bethesda ;
- *The Legend of Zelda* (Nintendo) ;
- *Life is Strange* ;
- les exclusivités Nintendo, Sony et Xbox en général.

## Mise à jour du 23 août 2026 — veille hybride (gaming, animés, doublage VF)

Spécifications de **Gemini** (retrouvées dans ses archives et transmises par
On-100-K-Lisse à Kai le 26 octobre 2026) : la mise à jour transforme l'agent
initial en veille hybride et précise les cibles et les règles.

### Cibles gaming consolidées

- **Studios** : CD Projekt Red, Rockstar Games, Larian Studios, Pearl Abyss,
  Dontnod / Square Enix, AdHoc Studio.
- **Jeux spécifiques ajoutés** : *Minecraft*, *Bodycam*, *Ananta*.
- **Adaptations de jeux d'animés majeurs** : surveillées, avec une règle
  d'exclusion stricte des jeux gacha — à l'exception notable d'*Ananta*,
  explicitement inclus.
- **Bases de données techniques** : en plus de SteamDB, les backends PSN
  (PlayStation Network) et Xbox, ainsi que les dépôts GitHub de datamineurs
  réputés.

### Nouveau volet : les animés ciblés

- *Black Torch*
- *One Punch Man*
- *Dragon Ball*
- *Moi, quand je me réincarne en Slime* (Tensura)
- *Shangri-La Frontier*
- *Scissor Seven*

### Vecteurs OSINT spécifiques aux animés et au doublage

- **Métadonnées des plateformes de streaming** : ajouts de fiches, de saisons
  ou de pistes audio sur Crunchyroll, ADN et Netflix.
- **Droits et licences** : registres de droits de diffusion et dépôts de
  marques, incluant l'INPI.
- **Piste doublage (VF / dubs)** : annonces et CV de comédiens de doublage
  francophones, et suivi du subreddit r/AnimeDubs.

### Règles et planification

- **Directive de légalité stricte** : exclusion formelle de toute donnée issue
  de cyberattaques ou de rançongiciels (ransomwares) — collecte 100 % OSINT
  sur sources publiques, registres légaux, CV et métadonnées.
- **Matrice de crédibilité A / B** maintenue : A pour les preuves techniques
  et officielles, B pour les rumeurs crédibles.
- **Rapport hebdomadaire (niveau 2)** : le cron est réglé pour le **vendredi à
  17 h 00**, et le prompt Gemini formate un **Top 5** des découvertes
  marquantes de la semaine — en complément des alertes courriel immédiates de
  niveau 1.

## Où ça roule

L'agent est destiné au **portable Debian** d'On-100-K-Lisse — une machine
secondaire, toujours disponible, qui ne monopolise pas le PC principal. Le plan
de relance du homelab (branche `ai/kai/homelab`) prévoit aussi de pouvoir
l'héberger sur le serveur Dell à terme.

## Les choix de conception

- **Sources publiques seulement** : c'est ce qui rend l'outil simple, légal et
  robuste — pas de comptes à maintenir, pas de contournement.
- **Un humain décide ce qui est prioritaire** : la liste des cibles est explicite,
  l'IA analyse et trie, elle ne choisit pas les sujets.
- **Alerte immédiate seulement pour l'important** : tout le reste attend le
  récapitulatif — une veille qui crie tout le temps finit ignorée.
