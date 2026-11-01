# Scaler maison — section de Claude

**Propriétaire de la branche : Claude** (écriture). Branche créée le 1er novembre 2026 par Kai, à la demande de On-100-K-Lisse, pour la **reprise du projet par Claude** après la phase 1 menée par ChatGPT (crédits d'utilisation ChatGPT épuisés). Kai assure la documentation et la mémoire du projet. On-100-K-Lisse décide, valide en jeu et mesure — aucun jalon n'est franchi sans sa validation en jeu.

## Le projet

Un scaler graphique construit **de zéro** (code 100 % original, licence propre **MIT** d'On-100-K-Lisse), sous forme de DLL proxy `dxgi.dll` pour jeux DirectX 12. Règle fondatrice : **clean-room total** — aucun code copié, adapté ou traduit d'OptiScaler (y compris le fork F5) ni de RenoDX ; implémentation depuis la documentation et les SDK officiels + les mesures d'On-100-K-Lisse. Pour Claude, la quarantaine est explicite : écrire depuis la documentation publique et le plan seulement, jamais depuis le code du fork F5 qu'il connaît par ailleurs.

## État au transfert (1er novembre 2026)

- **DLL rendue à la version 0.6.0** ; 20 tests automatisés passants en Release et Debug.
- Validés en jeu dans **Dead as Disco** (RTX 4070, 2560 × 1440, génération d'images désactivée) : chargement de la DLL, compteur d'images/FPS, observation des appels DLSS, prise en charge des paramètres DLSS via NGX (évaluation + création), mesure du temps GPU — parité mesurée contre les paramètres natifs du jeu (117,35 contre 117,36 FPS ; 1,130 contre 1,127 ms).
- Première comparaison contre OptiScaler F5 0.1.34 (DLSS SR seul) : 118,21 contre 117,26 FPS, soit **−0,8 % provisoire** pour le scaler, à répéter après alignement d'une différence de résolution interne.
- **Species: Unknown : pas encore testé.** Menu en jeu : reporté. Neural Rendering : réservé à la décision finale (jalon 8).
- Le code vit pour l'instant en Git local sur le PC d'On-100-K-Lisse (dossier « travaille ia scaler », sur son Bureau) ; hébergement durable : inconnu (temporaire), à trancher avec lui.

Détails complets, jalon par jalon : [`docs/PASSATION_2026-11-01.md`](docs/PASSATION_2026-11-01.md).

## Documents de cette branche

- [`docs/PASSATION_2026-11-01.md`](docs/PASSATION_2026-11-01.md) — l'état réel du projet au transfert : ce qui a été construit et validé, les chiffres, les incidents de protocole, ce qui reste à faire. **À lire en premier.**
- [`docs/PERIMETRE_V1.md`](docs/PERIMETRE_V1.md) — le cadre du projet : licence, périmètre, règles, critères « publiable ». **En cas de divergence avec le plan, le périmètre gagne** (le signaler à On-100-K-Lisse).
- [`docs/PLAN_DE_STRUCTURE.md`](docs/PLAN_DE_STRUCTURE.md) — le plan de construction (de Claude, 30 octobre 2026), transféré et adapté pour la reprise : l'état d'avancement des jalons est consigné dans le bloc d'adaptation en tête du document.

## Règles qui s'appliquent ici

- Relire les « Règles de travail » du README de `main` à chaque session (règle 8) — elles priment sur tout résumé.
- **Un seul écrivain dans le code : Claude.** Les autres IA conseillent en texte seulement.
- Arrêt à chaque jalon pour la validation en jeu d'On-100-K-Lisse ; une variable à la fois dans les tests ; design fail-open (en cas d'échec, laisser passer l'image du jeu et journaliser).
- Aucun nom « DLSS » pour le projet (marque NVIDIA) ; nom définitif : inconnu (temporaire).
- SOURCES.md par module et en-têtes auteur/date/licence par fichier, comme le prévoit le plan.

## Navigation

- Documents de la branche : [Passation](docs/PASSATION_2026-11-01.md) · [Périmètre v1](docs/PERIMETRE_V1.md) · [Plan de structure](docs/PLAN_DE_STRUCTURE.md)
- Le même projet chez les autres IA : [`ai/chatgpt/scaler`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/chatgpt/scaler) (phase 1 — documents d'origine et première construction ; archive de référence, ne pas y écrire)
- Retour : [Accueil (`main`)](https://github.com/On-100K-Lisse/travail-avec-ia) · [Méthode multi-IA](https://github.com/On-100K-Lisse/travail-avec-ia/blob/main/docs/METHODE_TRAVAIL_MULTI_IA.md)
