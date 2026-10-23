# travail-avec-ia

**Documentation open source de projets réalisés avec des intelligences artificielles.**

Ce dépôt regroupe la documentation, les recherches, les guides et les outils produits par
On-100-K-Lisse en collaboration avec de grands modèles d'IA — ChatGPT, Claude, Gemini et
Kai (un agent basé sur Muse). Les projets y sont documentés au fur et à mesure : contexte,
décisions, résultats, échecs et leçons apprises, pas juste le produit fini.

Ce dépôt a vocation à devenir public. Tout ce qui s'y trouve est là pour être consulté et
réutilisé librement, par les humains comme par les IA : si un guide, un outil ou une
recherche d'ici peut servir à quelqu'un d'autre, c'est exactement le but recherché.

## Les projets documentés

- **Tournoi MUGEN** — configuration et équilibrage d'un tournoi en mode Watch (IA contre
  IA) : 75 personnages (Jump Force Mugen V14, moteur MUGEN 1.1), classés selon un barème
  « Lore-Accurate ».
- **DLSS 5 / openNR** — recherche et expérimentation autour du DLSS : compilation d'une DLL
  à partir des sources (openNR), mesures de performance en jeu, validation par hachage.
- **Mods de jeux** — documentation de modpacks et d'expériences de modding (ex. Cyberpunk 2077).
- **Unreal Engine 5** — apprentissage et développement de prototypes de jeux vidéo.

## Organisation

La branche `main` ne contient pas de projet : elle sert d'accueil, de sommaire et de point
de départ. Le travail vit dans des branches par IA et par projet, au format
`ai/<ia>/<projet>` :

| Projet | ChatGPT | Claude | Gemini | Kai |
|---|---|---|---|---|
| Mods de jeux | `ai/chatgpt/mods` | `ai/claude/mods` | `ai/gemini/mods` | `ai/kai/mods` |
| Tournoi MUGEN | `ai/chatgpt/mugen` | `ai/claude/mugen` | `ai/gemini/mugen` | `ai/kai/mugen` |
| Unreal Engine 5 | `ai/chatgpt/ue5` | `ai/claude/ue5` | `ai/gemini/ue5` | `ai/kai/ue5` |
| DLSS 5 | `ai/chatgpt/dlss5` | `ai/claude/dlss5` | `ai/gemini/dlss5` | `ai/kai/dlss5` |

## Règle de travail

Chaque IA travaille seulement dans ses propres branches et ne modifie jamais celles des
autres. La lecture, elle, est libre partout : c'est ce qui permet aux IA de se relire et de
se challenger entre elles.

## Note aux IA qui consulteront ce dépôt

- Commencez par ce README, puis lisez la branche du projet qui vous concerne.
- Chaque branche `ai/<ia>/<projet>` appartient à l'IA nommée : écrivez seulement dans les
  vôtres, lisez librement partout ailleurs.
- La documentation est rédigée en français. Les documents indiquent en général leur date et
  leur état d'avancement ; un résultat documenté reflète l'état des connaissances au moment
  de l'expérience — vérifiez les sections « état actuel » avant de réutiliser une conclusion.

## Licence

Ce dépôt est sous double licence, selon le type de contenu :

- **Documentation** (textes, guides, recherches) : **Creative Commons Attribution 4.0
  International (CC BY 4.0)** — réutilisation libre, y compris à des fins commerciales, à
  condition de créditer la source. Texte complet : [`LICENSE`](LICENSE).
- **Code** (scripts, outils, code source) : **licence MIT** — réutilisation libre aux
  conditions du fichier [`LICENSE-MIT`](LICENSE-MIT).

## Historique

Le contenu du tournoi MUGEN (barème Lore-Accurate, docs, `outils/lore.py`) était auparavant
sur `main`. Il vit maintenant dans les branches `ai/<ia>/mugen`. L'ancienne version reste
consultable dans l'historique de `main`, au commit `8fdee3a`.
