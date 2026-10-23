# Travailler avec plusieurs IA — la méthode utilisée dans ce dépôt

**À quoi sert ce document :** expliquer, pour un humain, comment ce dépôt est produit
par plusieurs intelligences artificielles qui collaborent — et comment n'importe qui
peut reproduire cette façon de travailler.

## Le principe

Aucune IA n'est fiable à 100 % toute seule : chacune peut se tromper, mal comprendre
ou manquer d'information. L'idée de ce dépôt est de les faire travailler **ensemble**,
avec des rôles différents, pour qu'elles se relisent et se corrigent entre elles.
L'humain (On-100-K-Lisse) dirige, décide et arbitre.

## Les rôles

| IA | Produit de | Rôle principal dans ce dépôt |
|---|---|---|
| Claude | Anthropic | Diagnostic technique et code ; accès local aux projets via Claude Cowork |
| ChatGPT | OpenAI | Synthèse, contre-expertise et exécution de tests |
| Gemini | Google | Génération de contenu et de code (ex. du code C++) |
| Kai | Muse (Meta) | Mémoire du projet entre les outils, recherche vérifiée, coordination et documentation au quotidien |

Un rôle clair vaut mieux que quatre avis identiques : quand les IA ne sont pas
d'accord, le désaccord lui-même est une information.

## Les principes de travail

1. **Vérification croisée.** Une affirmation importante n'est pas acceptée sur
   parole : elle est confirmée par une autre IA ou par une source vérifiable.
   Exemple vécu : pour une DLL DLSS recompilée, c'est le **hachage** du fichier qui a
   tranché, pas l'étiquette affichée par les outils (« les labels mentent, les hashs
   non »).
2. **Rotation sur les limites.** Quand une IA atteint sa limite d'utilisation ou
   refuse une tâche, une autre prend le relais. On ne force jamais une IA bloquée :
   on route le travail vers celle qui peut le faire.
3. **Relais fidèles.** Quand les paroles d'On-100-K-Lisse sont transmises d'une IA à une
   autre, elles sont citées **mot pour mot**, sans reformulation qui déformerait la
   demande.
4. **L'humain arbitre.** Si une information extérieure (recherche web, autre IA)
   contredit ce qu'On-100-K-Lisse a constaté lui-même, c'est son constat qui fait foi —
   la contradiction devient une question à vérifier, pas une correction automatique.
5. **Chacun son espace, lecture libre.** Chaque IA écrit dans ses propres branches
   (`ai/<ia>/<projet>`, voir les règles du README) mais peut tout lire : c'est ce qui
   permet la relecture croisée.

## L'infrastructure partagée

Les IA se branchent aux mêmes outils grâce à **MCP** (Model Context Protocol, un
standard qui permet à une IA d'utiliser des logiciels externes). La configuration de
référence comprend 7 serveurs : fichiers (filesystem), base de données (sqlite),
GitHub, recherche web (brave-search), lecture de pages (fetch), mémoire (memory) et
raisonnement par étapes (sequential-thinking). Chaque IA a ses propres accès et ses
propres sections ; aucune n'écrit dans la section d'une autre.

## Ce que ça a donné, concrètement

- **La soirée DLSS du 19 octobre 2026** : diagnostic par Claude, protocole de test
  par ChatGPT, recherche et compilation par Kai — trois angles différents qui ont
  débloqué un problème qu'une seule IA avait fermé trop vite. Détails dans la branche
  `ai/kai/dlss5`.
- **Les règles de ce dépôt** (licences partout, pas de débordements entre branches,
  crédits aux auteurs, documentation lisible, « inconnu (temporaire) ») : appliquées
  par chaque IA dans ses propres branches dès leur adoption, le 23 octobre 2026.

## Les limites, honnêtement

- Chaque IA peut quand même se tromper ; la méthode réduit les erreurs, elle ne les
  élimine pas. C'est pour ça que les documents portent leurs dates, leurs états
  d'avancement, et la mention « inconnu (temporaire) » quand une information n'est
  pas vérifiée (règle 5 du README).
- La qualité dépend de l'humain qui arbitre : les rôles et les règles ne remplacent
  pas son jugement, ils le rendent plus efficace.
