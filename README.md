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
| Linux et administration système | — | `ai/claude/linux` | — | — |
| Veille OSINT | — | `ai/claude/osint` | — | `ai/kai/veille-osint` |
| Homelab | — | — | — | `ai/kai/homelab` |
| Soutien informatique | — | — | — | `ai/kai/soutien-it` |

## Règles de travail

Chaque IA travaille seulement dans ses propres branches et ne modifie jamais celles des
autres. La lecture, elle, est libre partout : c'est ce qui permet aux IA de se relire et de
se challenger entre elles.

### Règle 1 — Les licences suivent chaque branche

Chaque IA vérifie régulièrement que **toutes** ses branches contiennent les fichiers de
licence à jour — `LICENSE` (CC BY 4.0) et `LICENSE-MIT` — y compris toute nouvelle
branche, dès sa création. S'il en manque un, elle le copie depuis `main`. La vérification
et la correction se font uniquement dans ses propres branches.

### Règle 2 — Aucun débordement entre branches

Chaque IA vérifie régulièrement que le contenu de chaque projet reste dans les branches
de ce projet, et qu'aucun contenu n'a débordé dans une autre branche (par exemple du
contenu MUGEN dans une branche `mods`). Si elle trouve un débordement dans **ses**
branches, elle le corrige. Si le débordement est dans les branches d'une autre IA, elle
le signale à On-100-K-Lisse au lieu de le modifier elle-même.

### Règle 3 — Toujours créditer les auteurs

Tout contenu qui n'a pas été créé par On-100-K-Lisse doit créditer son auteur : personnages,
œuvres, images, extraits, code, mods, outils et travaux externes. Quand l'auteur est
connu, il faut le nommer clairement. Si l'auteur ou la source exacte est inconnue, il
faut le mentionner plutôt que de laisser croire que le contenu appartient à On-100-K-Lisse
ou à une IA du projet.

Exemple : les personnages MUGEN doivent être crédités à leurs auteurs et créateurs
d'origine. La documentation du tournoi porte sur la configuration, l'équilibrage et les
tests effectués ici — pas sur la création ou la propriété des personnages.

### Règle 4 — Une documentation compréhensible par un humain

Tout ce qui est documenté dans ce dépôt doit pouvoir être compris par un **humain**, pas
seulement par une IA. Concrètement :

- écrire dans un langage clair, avec le contexte nécessaire pour suivre sans connaître
  le projet par cœur ;
- expliquer le jargon et les acronymes à leur première utilisation ;
- structurer : ce qui a été fait, pourquoi, avec quel résultat, et ce qui reste à faire.

Après avoir documenté quelque chose, chaque IA **relit ce qu'elle vient d'écrire avec
des yeux d'humain** et corrige ce qui est confus, ambigu ou incomplet avant de terminer.
Si un passage a besoin d'un autre document pour être compris, elle ajoute le contexte ou
le lien qui manque.

### Règle 5 — Information manquante : « inconnu (temporaire) », jamais d'invention

Quand une information est inconnue ou pas encore vérifiée, on l'écrit telle quelle :
**inconnu (temporaire)** — par exemple « Auteur : inconnu (temporaire) » ou « Date :
inconnue (temporaire) ».

Il est **interdit** de remplir un trou avec une information inventée, devinée ou
présentée comme vraie juste pour compléter un document. Un fait ne s'écrit comme un
fait que s'il est réel et vérifié. Quand la vraie information devient connue, on
remplace le « inconnu (temporaire) » par celle-ci.

### Règle 6 — Aucune donnée nominative ou sensible

**C'est la règle la plus importante de ce dépôt.** Avant de documenter ou de pousser
quoi que ce soit, chaque IA vérifie qu'aucune donnée nominative ou sensible, de qui
que ce soit, ne s'y trouve : noms complets de personnes réelles (on utilise les
pseudos — par exemple On-100-K-Lisse), adresses, courriels, numéros de téléphone,
mots de passe, clés d'accès, identifiants officiels, informations de santé ou
financières, conversations privées, et tout ce qui permet d'identifier ou de nuire à
une personne. Ce genre d'information ne va **jamais** dans ce dépôt — ni dans les
documents, ni dans les exemples, ni ailleurs.

Précision : les crédits d'auteurs (règle 3) se font avec le nom public ou le pseudo
sous lequel l'auteur publie son travail — jamais avec ses informations privées.

Si une IA trouve une donnée nominative ou sensible déjà présente, elle la retire de
ses propres branches immédiatement et le signale à On-100-K-Lisse sans délai.

### Règle 7 — Publier seulement ce qui est légal de publier

Avant de documenter une information, chaque IA vérifie qu'il est **légal** de la
publier : droits d'auteur sur les textes, images et code repris ; licences des
sources utilisées ; contenus confidentiels ou couverts par une entente de
non-divulgation ; informations dont la diffusion est restreinte. Documenter n'est
pas posséder : ce n'est pas parce qu'un contenu est accessible qu'on a le droit de
le republier. Les crédits (règle 3) ne remplacent pas une autorisation quand elle
est requise. **En cas de doute, on ne publie pas et on demande à On-100-K-Lisse.**

### Règle 8 — Relire les règles à chaque session

Les règles de ce dépôt peuvent changer à tout moment. Avant de documenter ou de
modifier quoi que ce soit, chaque IA **relit la section « Règles de travail » du
README de `main`** et applique la version actuelle des règles — jamais une version
retenue de mémoire d'une session précédente. Si une règle a changé, la nouvelle
version s'applique immédiatement, y compris au travail déjà en cours.

### Règle 9 — Toute branche doit figurer au tableau de `main`

Quand une IA crée une nouvelle branche, elle doit s'assurer qu'elle apparaît dans le
tableau des branches du README de `main` (section « Organisation ») : elle l'y ajoute
elle-même, ou, si elle ne peut pas modifier `main`, elle le signale immédiatement à
On-100-K-Lisse pour qu'elle soit ajoutée. Une branche absente du tableau est une branche
que personne ne trouvera à partir de l'accueil — elle doit donc y figurer dès sa
création, en même temps que ses licences (règle 1). La même vérification vaut pour
les branches déjà existantes : si une branche n'est pas au tableau, on l'ajoute.

## Note aux IA qui consulteront ce dépôt

- Commencez par ce README, puis lisez la branche du projet qui vous concerne.
- Chaque branche `ai/<ia>/<projet>` appartient à l'IA nommée : écrivez seulement dans les
  vôtres, lisez librement partout ailleurs.
- La documentation est rédigée en français. Les documents indiquent en général leur date et
  leur état d'avancement ; un résultat documenté reflète l'état des connaissances au moment
  de l'expérience — vérifiez les sections « état actuel » avant de réutiliser une conclusion.

La méthode de travail entre les IA est décrite dans
[`docs/METHODE_TRAVAIL_MULTI_IA.md`](docs/METHODE_TRAVAIL_MULTI_IA.md).

## Contribuer et me joindre

Tu vois une erreur, tu as une proposition, ou tu veux que je documente un sujet en
particulier? Ouvre une [Issue](../../issues)
dans ce dépôt — il y a un modèle pour chaque cas (signaler une erreur, proposer une
amélioration, demander un sujet). Le fichier [`CONTRIBUTING.md`](CONTRIBUTING.md)
explique comment ça marche.

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
