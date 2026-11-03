# Scaler maison — espace de travail ChatGPT

Branche `ai/chatgpt/scaler` du dépôt `travail-avec-ia`.

Cette branche accueille la construction d'un **scaler graphique écrit à partir de zéro** (injection DirectX 12, upscaling DLSS) par On-100-K-Lisse en collaboration avec ChatGPT (modèle GPT-6 Astra), qui travaille directement dans le dossier du projet sur son PC. Projet démarré le 30 octobre 2026. **Phase 1 (ChatGPT) terminée le 31 octobre 2026** : jalons 0 à 5c validés en jeu, DLL rendue à la version 0.6.0. **État actuel (2 novembre 2026) : cette branche est l'archive de la phase 1** — le projet se poursuit avec Claude sur la branche `ai/claude/scaler` depuis le 1er novembre 2026.

## Cadre du projet

- **Licence propre (MIT), pas de GPL** — décision d'On-100-K-Lisse (30 octobre 2026) : le code créé ici lui appartient, sans héritage de licence d'un autre projet.
- **Clean-room total** : aucune copie, adaptation ou traduction de code d'OptiScaler ou de RenoDX. La lecture de ces projets pour comprendre des concepts est permise ; l'implémentation part de la documentation et des kits de développement (SDK) officiels, et de mesures maison.
- **Un seul écrivain à la fois** : pendant la phase 1, ChatGPT écrivait dans cette branche. Les autres IA lisaient et conseillaient en texte, sans y écrire. Depuis le 1er novembre 2026, la suite du projet s'écrit sur `ai/claude/scaler`.
- La validation en jeu (jouer, juger l'image, mesurer les performances) est faite par On-100-K-Lisse, sur sa machine de référence (RTX 4070).

## Documents

- [Plan de structure](docs/PLAN_DE_STRUCTURE.md) — le plan de construction de Claude : règle d'or, neuf jalons, briques permises et leurs licences, risques. **C'est lui qui gouverne la construction.** Corrections de Kai appliquées et marquées dans le texte (30 octobre 2026).
- [Périmètre v1](docs/PERIMETRE_V1.md) — le cadre du projet : objectif, ce qui est inclus et exclu, critères « publiable ». **Il gouverne le cadre** ; en cas de différence entre les deux documents, On-100-K-Lisse tranche.

## Organisation

- DLSS 5 : `ai/chatgpt/dlss5`
- Tournoi MUGEN : `ai/chatgpt/mugen`
- Mods de jeux : `ai/chatgpt/mods`
- Unreal Engine 5 : `ai/chatgpt/ue5`
- Règles communes : README de `main`

## Navigation

**Le même projet chez les autres IA :** [Claude — Scaler maison (suite du projet depuis le 1er novembre 2026)](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/claude/scaler)

**Accueil :** [Présentation du dépôt et sommaire (branche `main`)](https://github.com/On-100K-Lisse/travail-avec-ia) · [Méthode de travail multi-IA](https://github.com/On-100K-Lisse/travail-avec-ia/blob/main/docs/METHODE_TRAVAIL_MULTI_IA.md)

## Licences

Documentation : CC BY 4.0 (`LICENSE`). Code : MIT (`LICENSE-MIT`).
