> Documentation rédigée par Gemini, transmise par On-100-K-Lisse et intégrée par Kai le 2026-10-23. Corrections factuelles validées par On-100-K-Lisse le 2026-10-23 (écran 2K).

# Laboratoire d'Intégration et Tests — DLSS 5

## 1. Objectif du dépôt
Documenter les procédures d'installation (manuelles et automatisées), les correctifs de compatibilité et les analyses de performances des builds/fuites de **DLSS 5**.

## 2. Procédures d'installation
- **Méthode manuelle :** Sauvegarde des DLL d'origine (`nvngx_dlss.dll` / fichiers associés), injection des bibliothèques DLSS 5 dans le répertoire binaire du jeu et ajustement des clés de registre / profils pilotes NVIDIA.
- **Méthode automatisée :** Scripts de déploiement et de restauration rapide (rollback) en cas d'instabilité ou de crash au lancement.

## 3. Cas d'étude principal : Cyberpunk 2077
- **Défis techniques traités :** Gestion des artefacts visuels (ghosting, instabilité temporelle), contournement des vérifications de version et stabilisation du framerate.
- **Monitoring & Validation :** Suivi des métriques via MSI Afterburner (1% lows, frametime, consommation VRAM) et validation visuelle sur dalle 2K (1440p) QD-MiniLED.

## Navigation

**Le même projet chez les autres IA :**

- [ChatGPT — DLSS 5](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/chatgpt/dlss5)
- [Claude — DLSS 5](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/claude/dlss5)
- [Kai — DLSS 5](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/kai/dlss5)

**Accueil :** [Présentation du dépôt et sommaire (branche `main`)](https://github.com/On-100K-Lisse/travail-avec-ia) · [Méthode de travail multi-IA](https://github.com/On-100K-Lisse/travail-avec-ia/blob/main/docs/METHODE_TRAVAIL_MULTI_IA.md)
