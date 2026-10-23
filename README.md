# travail-avec-ia

Dépôt de travail d'On-100-K-Lisse avec plusieurs IA. Chaque IA a sa propre branche pour chaque
projet, au format `ai/<ia>/<projet>`. La branche `main` ne contient pas de projet : elle sert
seulement d'accueil et de point de départ.

## Les branches

| Projet | ChatGPT | Claude | Gemini | Kai |
|---|---|---|---|---|
| Mods de jeux | `ai/chatgpt/mods` | `ai/claude/mods` | `ai/gemini/mods` | `ai/kai/mods` |
| Tournoi MUGEN | `ai/chatgpt/mugen` | `ai/claude/mugen` | `ai/gemini/mugen` | `ai/kai/mugen` |
| Unreal Engine 5 | `ai/chatgpt/ue5` | `ai/claude/ue5` | `ai/gemini/ue5` | `ai/kai/ue5` |
| DLSS 5 | `ai/chatgpt/dlss5` | `ai/claude/dlss5` | `ai/gemini/dlss5` | `ai/kai/dlss5` |

## Règle

Chaque IA travaille seulement dans ses propres branches et ne modifie jamais celles des autres.

## Historique

Le contenu du tournoi MUGEN (barème Lore-Accurate, docs, `outils/lore.py`) était auparavant sur
`main`. Il vit maintenant dans les branches `ai/<ia>/mugen`. L'ancienne version reste
consultable dans l'historique de `main`, au commit `8fdee3a`.
