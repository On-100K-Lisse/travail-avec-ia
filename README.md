# Scaler maison — espace de travail ChatGPT

Branche `ai/chatgpt/scaler` du dépôt `travail-avec-ia`.

Cette branche accueille la construction d'un **scaler graphique écrit à partir de zéro** (injection DirectX 12, upscaling DLSS) par On-100-K-Lisse en collaboration avec ChatGPT (modèle GPT-6 Astra), qui travaille directement dans le dossier du projet sur son PC. Projet démarré le 30 octobre 2026. État actuel : démarrage — le périmètre de la première version est défini, le code n'est pas encore commencé.

## Cadre du projet

- **Licence propre (MIT), pas de GPL** — décision d'On-100-K-Lisse (30 octobre 2026) : le code créé ici lui appartient, sans héritage de licence d'un autre projet.
- **Clean-room total** : aucune copie, adaptation ou traduction de code d'OptiScaler ou de RenoDX. La lecture de ces projets pour comprendre des concepts est permise ; l'implémentation part de la documentation et des kits de développement (SDK) officiels, et de mesures maison.
- **Un seul écrivain à la fois** : ChatGPT écrit dans cette branche. Les autres IA lisent et conseillent en texte, sans y écrire — Claude travaille sur le DLSS 5 Launcher et sur un fork d'OptiScaler F5, Kai sur la documentation DLSS du dépôt.
- La validation en jeu (jouer, juger l'image, mesurer les performances) est faite par On-100-K-Lisse, sur sa machine de référence (RTX 4070).

## Documents

- [Périmètre v1](docs/PERIMETRE_V1.md) — objectif, ce qui est inclus et exclu, ordre de construction en 6 étapes, règles du projet et critères « publiable ».

## Organisation

- DLSS 5 : `ai/chatgpt/dlss5`
- Tournoi MUGEN : `ai/chatgpt/mugen`
- Mods de jeux : `ai/chatgpt/mods`
- Unreal Engine 5 : `ai/chatgpt/ue5`
- Règles communes : README de `main`

## Navigation

**Le même projet chez les autres IA :** aucune autre branche pour ce projet pour l'instant.

**Accueil :** [Présentation du dépôt et sommaire (branche `main`)](https://github.com/On-100K-Lisse/travail-avec-ia) · [Méthode de travail multi-IA](https://github.com/On-100K-Lisse/travail-avec-ia/blob/main/docs/METHODE_TRAVAIL_MULTI_IA.md)

## Licences

Documentation : CC BY 4.0 (`LICENSE`). Code : MIT (`LICENSE-MIT`).
