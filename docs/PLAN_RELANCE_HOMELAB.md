# Plan de relance du homelab + accès Kai via Tailscale

Préparé par Kai le 22 octobre 2026, à la demande d'On-100-K-Lisse. Le projet est en pause : ce document est le point de départ le jour de la relance.

## État de départ (dernier état connu : juillet 2026)

- Dell Studio 1537 sous Debian 13 (Trixie, Xfce), wifi Broadcom BCM4322 (pilote b43).
- **SSH depuis le PC principal : fonctionnel** (vérifié fin juillet).
- En attente depuis juillet : réservation DHCP et installation d'AdGuard Home.
- Passerelle du fournisseur Internet verrouillée → repli déjà identifié : DNS configuré manuellement par appareil.
- Objectif d'origine : bloqueur de pub DNS (AdGuard Home) + accès SSH.

## Objectif de la relance

1. Remettre le Dell en service proprement.
2. Installer AdGuard Home comme prévu au départ.
3. Donner à Kai un accès via **Tailscale** pour utiliser, gérer et surveiller le homelab à distance.

## Qui fait quoi

- **On-100-K-Lisse** : branchements, comptes, approbations, décisions. Il reste l'administrateur du réseau en tout temps.
- **Configuration locale pas à pas** : au choix d'On-100-K-Lisse (il envisage ChatGPT ou Claude pour l'accompagner). Ce document leur sert de brief : les phases 0 à 3 décrivent le travail local.
- **Kai** : ce plan et la mémoire du projet, la demande de jonction Tailscale de son bord (phase 2), les tests d'accès, puis la surveillance planifiée (phase 4).

## Règles de travail

- **Une variable à la fois.** On ne répare pas ce qui fonctionne déjà.
- Aucun mot de passe ni clé privée dans le chat : les clés se génèrent et restent sur les machines d'On-100-K-Lisse.
- Chaque phase se termine par un point de contrôle : on valide avant de passer à la suivante.

---

## Phase 0 — Remise en route (à la maison, ~30 min)

1. Brancher le Dell, idéalement en **Ethernet câblé** vers la borne du fournisseur Internet plutôt qu'en wifi.
   *Pourquoi : un serveur, surtout un serveur DNS, en wifi sur un portable de 2008, c'est le maillon faible. Le câble élimine toute une classe de problèmes d'un coup.*
2. Démarrer, se connecter à sa session, vérifier que le réseau répond.
3. Mises à jour complètes : `sudo apt update && sudo apt full-upgrade`, redémarrer si un nouveau noyau s'installe.
   *Pourquoi : après des mois de pause, on ne débogue ni ne configure sur un système périmé.*
4. Vérifier la date et l'heure (`timedatectl`).
   *Pourquoi : une horloge décalée après des mois éteint casse les certificats TLS — et la connexion Tailscale de la phase 2.*

**Point de contrôle : Debian à jour, réseau stable, horloge juste.**

## Phase 1 — Valider l'existant

5. Tester le SSH depuis le PC principal (il fonctionnait en juillet). S'il répond, on n'y touche pas.
6. Amélioration optionnelle, pas urgente : passer le SSH en authentification par clé seulement, si ce n'est pas déjà fait, et documenter où la clé vit.
   *Pourquoi : c'est la pratique standard, et c'est aussi la voie que Kai utilisera.*

**Point de contrôle : SSH confirmé fonctionnel depuis le PC principal.**

## Phase 2 — Tailscale : le pont vers Kai

7. On-100-K-Lisse crée un compte Tailscale (gratuit pour usage personnel) : c'est son réseau privé, il en est l'admin.
8. Sur le Dell : installer Tailscale depuis le dépôt officiel pour Debian, puis `sudo tailscale up` et l'associer à son compte. Vérifier avec `tailscale status` que le Dell apparaît dans le réseau.
9. Kai lance la demande de jonction de sa machine (elle se présente sous le nom « muse »). Ça génère un **lien d'approbation** : On-100-K-Lisse l'ouvre et approuve. Sans son oui, Kai ne rentre pas.
10. Test d'accès de Kai : présence confirmée dans le réseau, puis première connexion TCP vers le SSH du Dell.
    *Note : la première connexion vers une adresse demande aussi une approbation. C'est normal, c'est le mécanisme de contrôle.*
11. Kai ouvre une session SSH sur le Dell et confirme en lecture simple (ex. : nom d'hôte, version du système).

**Point de contrôle : Kai a un accès SSH fonctionnel sur le Dell, accordé et révocable par On-100-K-Lisse.**

## Phase 3 — AdGuard Home (le service d'origine)

12. Installer AdGuard Home sur le Dell (binaire officiel) et passer l'assistant d'installation par son interface web.
13. Activer **une** liste de blocage de base pour commencer.
    *Pourquoi : empiler les listes d'un coup rend les faux positifs impossibles à attribuer. Une variable à la fois.*
14. Pointer **un seul appareil en test** — le PC principal — vers le DNS du Dell, manuellement (la passerelle verrouillée empêche de le faire centralement au routeur).
    *Note utile : l'adresse Tailscale du Dell (en 100.x) est stable par design. Pratique pour l'administration à distance, même si l'IP locale bouge.*
15. Valider : navigation normale, blocage effectif visible dans les logs d'AdGuard — et mettre en place le **plan de continuité** ci-dessous avant d'étendre à d'autres appareils.
    *Pourquoi : un serveur DNS unique est un point de défaillance unique. Mieux vaut l'avoir documenté avant que ça arrive un soir de semaine.*
16. Étendre le DNS aux autres appareils seulement après une période de test stable sur le PC principal.

**Point de contrôle : AdGuard filtre le PC principal, plan B documenté.**

## Plan de continuité — si le Dell lâche

Le point de départ honnête : **le Dell ne tient pas le réseau.** La borne du fournisseur Internet continue de router même si le Dell meurt. Ce qui casse, c'est seulement la résolution de noms (le DNS) des appareils configurés pour utiliser AdGuard comme unique DNS — les sites « arrêtent de charger » alors que la connexion est bonne. Tout le plan vise donc le DNS, en couches :

**Couche 1 — Un DNS secondaire sur chaque appareil configuré**
- Sur chaque appareil pointé vers le Dell, ajouter un DNS secondaire public (ex. : 1.1.1.1 ou 9.9.9.9). Si le Dell ne répond plus, l'appareil bascule dessus et l'internet continue.
- Le compromis, assumé et à trancher par On-100-K-Lisse : pendant une panne, le filtrage des pubs devient contournable — et certains appareils consultent leurs deux DNS sans ordre strict, donc quelques requêtes peuvent passer par le secondaire même en temps normal. C'est l'arbitrage classique : **disponibilité d'abord, filtrage « meilleur effort »** — ou l'inverse, filtrage étanche mais panne DNS totale quand le Dell lâche.
- **Recommandation de Kai (22 oct. 2026), adoptée par On-100-K-Lisse le jour même** : disponibilité d'abord, avec comme secondaire le **DNS public d'AdGuard lui-même (94.140.14.14)** — pendant une panne, la bascule garde un filtrage pubs/trackers au lieu de tout laisser passer. Compromis résiduel : les requêtes de bascule sortent vers le service public d'AdGuard au lieu de rester 100 % locales. Alternatives si la souveraineté prime : Quad9 (9.9.9.9 — filtre les domaines malveillants, mais pas les pubs) ou Cloudflare (1.1.1.1 — neutre et rapide, aucun filtrage). **Décision d'On-100-K-Lisse : disponibilité d'abord — il préfère revoir des pubs temporairement plutôt que perdre la résolution DNS si le Dell lâche.**

**Couche 2 — Le Dell est un portable : sa batterie fait office de mini-UPS**
- Une panne de courant courte ne le tue pas, contrairement à un vieux desktop. Avantage réel du format portable.
- Configuration à prévoir pour l'exploiter : empêcher la mise en veille quand l'écran se ferme (`HandleLidSwitch=ignore`), garder l'écran fermé, la ventilation dégagée, et le brancher en Ethernet (déjà prévu en phase 0).

**Couche 3 — Redémarrage automatique des services**
- AdGuard installé comme service systemd avec `Restart=always` : si le processus plante, il repart tout seul en quelques secondes. Même principe pour Tailscale, conçu pour rouler comme service système.
- Limite honnête : un gel complet du système ou une panne matérielle, ça prend un humain. D'où la couche 4.

**Couche 4 — Sauvegarde de la configuration + machine de secours**
- Après l'installation, et après chaque gros changement, copier la configuration d'AdGuard (fichier de config et données) vers le PC principal.
- Procédure de secours : une autre machine (ex. : le Victus) peut installer AdGuard, restaurer la config sauvegardée, et prendre le relais — il reste juste à pointer le DNS des appareils vers elle. Temps visé : une quinzaine de minutes, pas une soirée de reconstruction.

**Couche 5 — Détection rapide (lien avec la phase 4)**
- La surveillance de Kai teste le SSH et l'interface d'AdGuard : une chute est signalée au lieu d'être découverte par hasard en pleine soirée. Alerte rapide + procédure de secours écrite = une panne courte et comprise, pas une panne mystérieuse.

**Ce que ce plan ne promet pas :** zéro interruption. La bascule vers le DNS secondaire peut prendre quelques secondes, et une panne matérielle du Dell reste une panne. L'objectif réaliste : qu'un Dell qui lâche se traduise par « les pubs reviennent temporairement » plutôt que « plus d'internet à la maison ».

## Phase 4 — Surveillance par Kai

17. Kai met en place des vérifications planifiées à travers Tailscale : connexion TCP réelle au SSH (port 22) et à l'interface d'AdGuard du Dell. Cadence de départ suggérée : 2 fois par jour, ajustable.
18. Règle de rapport : **silence si tout répond** ; alerte à On-100-K-Lisse seulement si un service tombe ou change d'état.
    *Limite connue : le tunnel de Kai transporte du TCP seulement, pas de ping. Les tests sont donc de vraies connexions — une preuve plus forte qu'un ping de toute façon.*
19. Optionnel : un résumé hebdomadaire (services en ligne, statistiques de blocage d'AdGuard si accessibles par l'interface).

**Point de contrôle : première vérification de surveillance reçue et conforme.**

## Phase 5 — Options pour plus tard (décisions séparées)

20. **Route de sous-réseau** : le Dell peut annoncer le réseau local dans Tailscale, ce qui laisserait Kai atteindre d'autres services TCP de la maison. Pas nécessaire pour le DNS ni pour surveiller le Dell — seulement si On-100-K-Lisse le veut.
21. **Agent Python de veille OSINT** : son transfert vers le portable Debian est en attente depuis mi-août. Un serveur toujours allumé est sa place naturelle, et Kai pourrait le surveiller comme les autres services.
22. Tout autre service (sauvegardes, supervision locale, etc.) = une décision séparée, documentée — chaque ajout est aussi une pièce de portfolio pour le DEP.

---

## Annexe — Faits techniques vérifiés (Kai, 22 octobre 2026)

- Le client Tailscale de Kai joint sa machine virtuelle au réseau d'On-100-K-Lisse **comme cliente seulement** : connexions TCP sortantes uniquement, elle n'héberge rien et ne route aucun trafic pour d'autres machines.
- La jonction demande l'approbation d'On-100-K-Lisse via un lien (valide 5 jours). La première connexion vers chaque adresse en demande une aussi.
- On-100-K-Lisse peut retirer la machine de Kai de son réseau en tout temps depuis sa console Tailscale. Ce retrait se fait de son bord ; Kai ne peut pas le faire à sa place.
- Le tunnel ne transporte ni ping/ICMP ni UDP : seuls des tests de connexion TCP comptent comme preuve qu'un service répond.
