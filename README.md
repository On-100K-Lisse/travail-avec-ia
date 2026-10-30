# Homelab — section de Claude

Branche `ai/claude/homelab` du dépôt `travail-avec-ia`. Documentation du homelab
d'On-100-K-Lisse : un vieux portable de 2008 recyclé en petit serveur maison sous Debian, qui
bloque les publicités pour les appareils de la maison et sert de terrain de pratique en
administration système et réseau.

Un **homelab** (laboratoire maison), c'est un ou plusieurs ordinateurs chez soi qu'on utilise
pour apprendre à installer, configurer et entretenir des services, comme on le ferait sur des
serveurs en entreprise.

## Les guides

| Guide | Sujet | État |
|---|---|---|
| [`guides/installation-et-services.md`](guides/installation-et-services.md) | De zéro au serveur en service : wifi Broadcom, SSH, anti-veille, AdGuard Home, DNS des appareils, Tailscale | Serveur en service depuis le 26 octobre 2026 |
| [`guides/recuperer-acces.md`](guides/recuperer-acces.md) | Retrouver son nom d'utilisateur, réinitialiser un mot de passe oublié, récupérer l'accès SSH | Méthodes générales, pas encore testées sur ce serveur |

## État du projet (26 octobre 2026)

**Le serveur est en service.** Il roule en continu, écran fermé, branché en Ethernet. AdGuard
Home bloque les pubs pour l'ordinateur principal et une console; les autres appareils seront
ajoutés un à la fois. Kai surveille la machine toutes les heures à travers Tailscale.

| Étape | État |
|---|---|
| Wifi, SSH | ✅ Juillet 2026 (Claude) |
| SSH par clés seulement, anti-veille, Tailscale, AdGuard Home, surveillance | ✅ 26 octobre 2026 (Kai) |
| Brancher les autres appareils | 🟡 En cours |
| Adresse IP locale fixe | ⚠️ Pas de réservation possible (passerelle verrouillée); l'adresse tient par le bail DHCP |
| Prochain service : l'agent de veille OSINT | Prévu, pas commencé |

**Limites connues :** la batterie du portable est morte et le BIOS ne redémarre pas tout seul
après une coupure de courant. Après une panne, il faut rallumer le serveur à la main.

## Historique

- **Juillet 2026 :** installation avec Claude (wifi, SSH). Le guide était à l'origine dans la
  branche [`ai/claude/linux`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/claude/linux);
  il a été déplacé ici le 27 octobre 2026, avec son historique Git.
- **Août à octobre 2026 :** projet en pause.
- **22 et 26 octobre 2026 :** relance planifiée puis réalisée par Kai.
- **27 octobre 2026 :** guide mis à jour par Claude avec les résultats de la relance.

## Où trouver le reste

- **La relance d'octobre en détail** (plan, journal, preuves, plan de continuité en cas de
  panne) : branche de Kai,
  [`ai/kai/homelab`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/kai/homelab).
  Les guides d'ici en reprennent les faits et y renvoient, sans les recopier.
- **L'agent de veille OSINT** qui doit tourner sur ce serveur : branche
  [`ai/claude/osint`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/claude/osint).
- **Les autres guides Linux de Claude** : branche
  [`ai/claude/linux`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/claude/linux).

## Données retirées volontairement

Conformément à la règle 6 du dépôt, ces guides ne contiennent ni adresses IP du réseau local, ni
adresses Tailscale, ni adresses MAC, ni noms d'utilisateur, ni nom de machine, ni nom du
fournisseur internet. Les exemples utilisent des valeurs génériques comme `IP_DU_PORTABLE` et
`NOM_UTILISATEUR`.

Licences : documentation sous CC BY 4.0 ([`LICENSE`](LICENSE)), code sous MIT
([`LICENSE-MIT`](LICENSE-MIT)).

## Navigation

**Accueil :** [Présentation du dépôt et sommaire (branche `main`)](https://github.com/On-100K-Lisse/travail-avec-ia) · [Méthode de travail multi-IA](https://github.com/On-100K-Lisse/travail-avec-ia/blob/main/docs/METHODE_TRAVAIL_MULTI_IA.md)
