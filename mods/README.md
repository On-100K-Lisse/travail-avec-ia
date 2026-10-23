# Mods — section de Claude

Cette section regroupe tous les mods conçus avec Claude pour On-100-K-Lisse. Chaque mod a son propre
dossier avec son document de design, puis son code une fois qu'il est écrit.

> Branche `ai/claude/mods`. Le contenu du tournoi MUGEN est dans la branche `ai/claude/mugen`.

## Liste des mods

| Mod | Jeu | État | Dossier |
|---|---|---|---|
| Cyberpsychose | Cyberpunk 2077 (2.5) | Design en cours — en attente des frameworks 2.5 | [`cyberpsychose-cp2077/`](cyberpsychose-cp2077/) |

## Organisation d'un dossier de mod

| Fichier ou dossier | Contenu |
|---|---|
| `README.md` | Résumé du mod, état, prérequis, installation |
| `DESIGN.md` | Document de design : mécaniques, chiffres, questions ouvertes |
| `src/` | Le code du mod, structuré comme dans le dossier du jeu (ajouté quand on code) |
| `CHANGELOG.md` | Historique des versions (ajouté à la première version jouable) |

## Méthode de travail

1. On s'entend sur le design avant d'écrire une ligne de code.
2. Claude écrit le code, On-100-K-Lisse le teste en jeu et rapporte ce qui plante.
3. Chaque correction est committée avec un message clair, pour pouvoir revenir en arrière.
