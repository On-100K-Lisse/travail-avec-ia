# ai/kai/homelab

Branche de **Kai** — documentation du homelab d'On-100-K-Lisse : un laboratoire
personnel pour apprendre l'administration système et réseau, et héberger ses
propres services.

## Documentation

- **[Plan de relance du homelab](docs/PLAN_RELANCE_HOMELAB.md)** — le plan complet
  préparé le 22 octobre 2026 : remise en route du serveur Dell sous Debian 13,
  validation du SSH, accès distant via Tailscale, bloqueur de publicités DNS
  (AdGuard Home), surveillance, et plan de continuité en cas de panne.
- **[Journal de réalisation — 26 octobre 2026](docs/RELANCE_2026-10-26.md)** —
  l'exécution du plan, phases 0 à 4 : mises à jour et anti-veille, SSH passé en
  clés seulement, jonction Tailscale approuvée par On-100-K-Lisse, AdGuard Home en
  service (avec le piège de la liste « activée » à zéro règle), identification
  d'un client inconnu dans le panneau, et les leçons DNS de la soirée — dont
  pourquoi les publicités intégrées au flux vidéo de YouTube échappent à tout
  filtre DNS.

## État du projet (26 octobre 2026)

**Relance réalisée : le homelab est en service.** Le serveur roule en continu ;
AdGuard Home filtre le DNS du PC principal et d'une première console ; la
surveillance automatique de Kai vérifie la machine toutes les heures, en silence
sauf anomalie. Reste à venir, en sessions séparées : brancher les autres appareils
au fil de l'eau, et la phase 5 du plan (premier candidat : l'agent de veille
OSINT).

Présentation du dépôt et des autres projets : voir le README de la branche `main`.

## Licence

Documentation sous licence CC BY 4.0 (`LICENSE`), code sous licence MIT
(`LICENSE-MIT`).
