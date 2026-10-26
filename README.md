# DLSS 5 — section de Claude

Branche `ai/claude/dlss5` du dépôt `travail-avec-ia`. Elle documente le travail fait avec Claude
autour de **DLSS 5 Neural Rendering** : un launcher Windows qui installe l'effet dans des jeux PC
en un clic et le désinstalle proprement, plus les tests en jeu, les crashs analysés et les leçons
apprises.

> **Neural Rendering (NR)**, aussi appelé « DLSS 5 » dans la communauté : un modèle d'IA de NVIDIA
> (le fichier `nvngx_dlssnr.dll`) qui retouche l'image d'un jeu (éclairage, détail, couleur).
> Il n'est pas encore intégré officiellement aux jeux. Des mods comme OptiScaler permettent de
> l'« injecter » dans des jeux existants. Ce dépôt ne contient aucun de ces fichiers : seulement
> notre code et notre documentation.

## Contenu

| Dossier / document | Contenu |
|---|---|
| [`launcher/`](launcher/) | Le **DLSS 5 Launcher** : ce qu'il fait, comment l'utiliser, comment il fonctionne, son code source (C#) et ses tests |
| [`launcher/CHANGELOG.md`](launcher/CHANGELOG.md) | Historique des versions, avec ce qui a été corrigé et pourquoi |
| [`docs/journal-tests-2026-10-24.md`](docs/journal-tests-2026-10-24.md) | Journal des essais en jeu des 24 et 25 octobre 2026 (ce qui a marché, les coûts mesurés) |
| [`docs/analyse-crashs.md`](docs/analyse-crashs.md) | Analyse des crashs (Cyberpunk 2077, Crimson Desert, Species: Unknown) à partir des logs et d'un dump |
| [`docs/cartes-graphiques.md`](docs/cartes-graphiques.md) | Quelle DLL selon la carte : RTX 20/30/40, RTX 50, AMD / Intel |
| [`docs/lecons-apprises.md`](docs/lecons-apprises.md) | Les erreurs faites en route et ce qu'elles ont appris |

## État au 25 octobre 2026

- **Fonctionne sur RTX 4070** (la carte d'On-100-K-Lisse) : installation en un clic, réinitialisation
  vérifiée au hash près, feeder pour les jeux sans DLSS, plusieurs jeux testés en jeu.
- **Stabilité :** des crashs ont été observés dans 3 jeux. Deux d'entre eux ont un point commun
  clair (la *Ray Reconstruction* de NVIDIA active). La cause du troisième est **inconnue
  (temporaire)**. Voir [`docs/analyse-crashs.md`](docs/analyse-crashs.md).
- **RTX 50 et AMD (RX 9070 XT) :** pris en charge dans le launcher, mais **pas encore testés sur du
  vrai matériel**. Un test sur RX 9070 XT est prévu ; aucun test RTX 50 n'est prévu pour l'instant.
  Résultats : inconnus (temporaire). Voir [`docs/cartes-graphiques.md`](docs/cartes-graphiques.md).
- **Suivi des tests :** le launcher garde l'historique de chaque partie, compare des étapes de test
  (une variable à la fois), surveille les parties lancées depuis lui et propose des corrections.
  OptiScaler 0.1.34 a été essayé à côté de la 0.1.32 (Species: Unknown, 1 h 27 sans crash).

## Lien avec le travail des autres IA

La recherche sur la DLL compatible RTX 20/30/40 (openNR, compilation, validation par hachage) et le
suivi des premiers crashs de Cyberpunk 2077 sont documentés par Kai dans la branche
`ai/kai/dlss5`. Cette branche-ci part de ce résultat et documente le launcher construit ensuite.

## Licence

Documentation sous licence CC BY 4.0 ([`LICENSE`](LICENSE)), code sous licence MIT
([`LICENSE-MIT`](LICENSE-MIT)). Les logiciels de tiers mentionnés gardent leurs propres licences :
voir les crédits dans [`launcher/README.md`](launcher/README.md#crédits).
