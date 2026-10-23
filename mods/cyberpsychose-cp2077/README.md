# Cyberpsychose — mod Cyberpunk 2077

Fait vivre à V une vraie cyberpsychose : une jauge de stress qui monte par paliers avec des
symptômes visibles, un avertissement, une crise avec des buffs et des debuffs adaptés au build,
des hallucinations, des pertes de contrôle, la police et MaxTac, puis un réveil à un endroit
aléatoire si V survit.

| | |
|---|---|
| **Version du jeu visée** | 2.5 (Edgerunners 2 Update, sortie le 22 octobre 2026) |
| **État** | Design en cours. Le code attend la mise à jour des frameworks pour la 2.5. |
| **Design complet** | [`DESIGN.md`](DESIGN.md) |

## Prérequis prévus

- [Cyber Engine Tweaks](https://www.nexusmods.com/cyberpunk2077/mods/107) (Lua) — logique principale
- [RED4ext](https://docs.red4ext.com/) — requis par CET et Codeware
- [redscript](https://wiki.redmodding.org/redscript/) — hooks dans le code du jeu (police, dégâts)
- TweakXL — effets de statut (buffs, debuffs, tremblements)
- ArchiveXL — ressources ajoutées (texto de Viktor)
- [Codeware](https://www.nexusmods.com/cyberpunk2077/mods/7780) — utilitaires (spawn des hallucinations)
- Native Settings UI (optionnel) — menu de réglages en jeu

## Plan de développement

1. Jauge de stress, paliers, avertissement, glitchs, buffs et debuffs
2. Police ×2 et réveil aléatoire
3. Hallucinations
4. Perte de contrôle

## Conseil d'installation après une mise à jour du jeu

Mettre à jour les frameworks **avant** les autres mods, et lancer le jeu une fois avec seulement
eux pour vérifier qu'il démarre.

## Crédits

- *Cyberpunk 2077*, ses personnages (dont Viktor Vektor) et l'univers Edgerunners appartiennent
  à CD PROJEKT RED. Ce mod est un projet de fan, non affilié.
- Frameworks prévus, chacun sous sa propre licence :
  - Cyber Engine Tweaks : yamashi et les contributeurs du projet;
  - RED4ext : WopsS;
  - redscript : jac3km4;
  - TweakXL, ArchiveXL et Codeware : psiberx;
  - Native Settings UI : auteur inconnu (temporaire).
- Concept du mod : On-100-K-Lisse. Document de design : Claude.
