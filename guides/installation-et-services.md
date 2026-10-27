# Recycler un vieux portable en homelab Debian : installation et services

**Rédigé par :** Claude, à partir de sessions de travail avec On-100-K-Lisse.
**Première version :** juillet 2026. **Mise à jour :** 27 octobre 2026, à partir du journal
de relance de Kai (voir « Sources » en bas de page).

**À quoi sert ce guide :** transformer un vieux portable en petit serveur maison (un
*homelab*, un laboratoire personnel) qui reste allumé dans un coin et qu'on contrôle à distance
depuis un autre ordinateur. Le service principal est un **bloqueur de pub par DNS** (AdGuard
Home). C'est aussi un terrain de pratique pour l'administration système et réseau.

Ce guide suit l'ordre des étapes. Pour chaque étape, il dit **ce qui a été fait sur la vraie
machine, et par qui**, puis **comment le refaire**. Les détails de la relance d'octobre
(journal complet, preuves, plan de continuité) sont dans la branche de Kai,
[`ai/kai/homelab`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/kai/homelab) : ce guide ne les recopie pas, il y renvoie.

## État d'avancement (au 26 octobre 2026)

| Étape | État | Par qui |
|---|---|---|
| 1. Faire marcher le wifi (carte Broadcom BCM4322) | ✅ Fait et testé (juillet) | Claude |
| 2. Accès à distance par SSH | ✅ Fait (juillet), puis passé en **clés seulement** (26 octobre) | Claude, puis Kai |
| 3. Stabiliser le wifi pour un serveur | Sans objet depuis octobre : le serveur est branché en Ethernet | — |
| 4. Adresse IP fixe | Réservation DHCP impossible (passerelle verrouillée). L'adresse Tailscale est stable; l'adresse locale tient par le renouvellement du bail DHCP | Kai |
| 5. Empêcher la mise en veille | ✅ Fait et prouvé (26 octobre) | Kai |
| 6. Installer AdGuard Home | ✅ En service (26 octobre) | Kai |
| 7. Diriger les appareils vers le bloqueur | 🟡 En cours : 2 appareils branchés à la main | Kai |
| 8. Accès à distance via Tailscale + surveillance | ✅ Actif (26 octobre) | Kai |

## La machine

| Élément | Valeur |
|---|---|
| Portable | Dell Studio 1537 (modèle PP33L), vers 2008-2009 |
| Processeur, mémoire, disque | Pentium Dual T3400 à 2,16 GHz, 4 Go de DDR2, disque de 250 Go (relevé par Kai) |
| Système | Debian 13 « Trixie », bureau Xfce, installé avec l'ISO *netinst with firmware* |
| Noyau | 6.12 (6.12.113 après les mises à jour du 26 octobre) |
| Carte wifi | Broadcom BCM4322 (identifiant PCI `14e4:432b`, vendue par Dell comme « Wireless 1510 ») |
| Réseau | Wifi seulement en juillet; **Ethernet** depuis la relance d'octobre |
| Batterie | **Morte** : débranché du secteur, le portable s'éteint net (constat du 26 octobre) |

---

## Étape 1 — Faire marcher le wifi Broadcom

*Fait et testé en juillet 2026. Le serveur est maintenant en Ethernet, mais le wifi reste
utile comme connexion de secours, et cette étape sert à quiconque refait l'installation.*

### Identifier la carte

Selon la configuration d'origine, Dell mettait trois cartes différentes dans ce modèle
(Broadcom BCM4312, Broadcom BCM4322 ou Intel WiFi Link 5100/5300). Le traitement n'est pas
le même, donc on vérifie d'abord :

```bash
lspci -nn | grep -i network
```

- **Intel** : `sudo apt install firmware-iwlwifi`, redémarrer, c'est fini.
- **Broadcom** : suivre la suite.

### Le problème de l'œuf et de la poule

Le pilote s'installe depuis Internet… mais le wifi ne marche pas encore. Il faut une connexion
temporaire : un câble ethernet, ou le **partage de connexion USB** d'un téléphone Android
(Debian le reconnaît comme une carte réseau ordinaire, sans pilote).

### Activer les dépôts `contrib` et `non-free`

Le paquet nécessaire n'est pas dans la section `main` de Debian. On édite la liste des
sources :

```bash
sudo nano /etc/apt/sources.list
```

On ajoute ` contrib non-free` à la fin des trois lignes qui commencent par `deb` :

```
deb http://deb.debian.org/debian/ trixie main contrib non-free non-free-firmware
deb http://security.debian.org/debian-security trixie-security main contrib non-free non-free-firmware
deb http://deb.debian.org/debian/ trixie-updates main contrib non-free non-free-firmware
```

Dans nano : `Ctrl+O`, `Entrée` pour sauvegarder, puis `Ctrl+X` pour quitter. Ensuite :

```bash
sudo apt update
```

### Installer le pilote libre `b43`

```bash
sudo apt install firmware-b43-installer
sudo reboot
```

**Pourquoi `b43` plutôt que le pilote propriétaire `broadcom-sta-dkms` (`wl`) :** en juillet
2026, des utilisateurs rapportaient que `wl` plantait ou ne se chargeait pas avec le noyau
6.12 de Trixie. Le pilote libre `b43` supporte le BCM4322 depuis longtemps.

Deux pièges :

- `firmware-b43-installer` **télécharge** le firmware chez Broadcom pendant l'installation :
  la connexion temporaire doit rester branchée jusqu'à la fin.
- **Ne pas installer les deux pilotes en même temps.** `broadcom-sta-dkms` met `b43`, `b44`
  et `ssb` sur liste noire, ce qui peut aussi couper le port ethernet, justement la connexion
  de secours.

### Vérifier

```bash
ip link
```

Une interface `wlan0` (ou `wlp…`) doit apparaître. On choisit ensuite son réseau avec l'icône
réseau de Xfce.

Si aucun réseau n'apparaît :

```bash
rfkill list        # "hard blocked: yes" = interrupteur physique (glissière sur le côté, ou Fn+F2)
dmesg | grep b43   # montre si le firmware s'est chargé
```

> **Si `sudo` refuse** (« n'est pas dans le fichier sudoers ») : c'est normal quand on a
> donné un mot de passe root pendant l'installation de Debian. Faire `su -`, puis
> `usermod -aG sudo NOM_UTILISATEUR`, puis se déconnecter et se reconnecter.

## Étape 2 — Accès à distance par SSH

**SSH** (*Secure Shell*) affiche un terminal du portable sur un autre ordinateur, avec une
connexion chiffrée. C'est l'outil de base pour gérer un serveur sans écran ni clavier.

### Installation (faite en juillet)

Sur le portable :

```bash
sudo apt install openssh-server
sudo systemctl status ssh     # doit afficher "active (running)" ; q pour quitter
hostname -I                   # affiche l'adresse IP du portable
```

Depuis l'autre ordinateur (Windows a un client SSH intégré, dans PowerShell) :

```bash
ssh NOM_UTILISATEUR@IP_DU_PORTABLE
```

La première fois, SSH montre une *empreinte* (fingerprint) du serveur et demande
confirmation : l'ordinateur la mémorise pour détecter plus tard une usurpation. On tape `yes`,
puis le mot de passe.

### Passage en clés seulement (fait par Kai le 26 octobre)

Depuis la relance, **la connexion SSH par mot de passe est désactivée** : seules les
*clés SSH* sont acceptées. Une clé SSH, c'est une paire de fichiers : une partie privée qui
reste sur l'ordinateur qui se connecte, et une partie publique déposée sur le serveur. Sans le
fichier privé, impossible d'entrer, même en connaissant le mot de passe. Kai a testé les trois
cas : la clé de l'administrateur est acceptée, le mot de passe est refusé, la clé de Kai (via
Tailscale) est acceptée.

**Conséquence pratique à retenir :** le mot de passe du compte ne sert plus pour SSH. Il sert
encore pour ouvrir une session directement sur le portable et pour `sudo`. Si on perd l'accès,
voir le guide [`recuperer-acces.md`](recuperer-acces.md).

#### Comment c'est fait sur le serveur

*Contenu du fichier et ordre des étapes relevés par Kai directement sur le serveur, le
27 octobre 2026.*

Tout tient dans **un seul fichier**, le seul du dossier `sshd_config.d/` :

```conf
# /etc/ssh/sshd_config.d/99-keys-only.conf
PasswordAuthentication no
KbdInteractiveAuthentication no
PubkeyAuthentication yes
```

- `PasswordAuthentication no` : refuse les mots de passe.
- `KbdInteractiveAuthentication no` : ferme une deuxième porte qui peut aussi demander un mot
  de passe (l'authentification « clavier interactif »). Sans cette ligne, un mot de passe
  pourrait encore passer par là.
- `PubkeyAuthentication yes` : accepte les clés (c'est déjà la valeur par défaut, mais l'écrire
  rend l'intention claire).

**Pourquoi un fichier à part plutôt que modifier `sshd_config` :** le fichier principal de
Debian lit tous les fichiers `.conf` de `sshd_config.d/`. Mettre ses réglages dans un fichier à
soi évite les conflits quand une mise à jour de Debian veut remplacer `sshd_config`, et pour
annuler, il suffit d'effacer ce fichier.

**La séquence, dans cet ordre.** L'ordre compte : une erreur dans la configuration SSH peut
fermer la porte à distance, et sur un serveur sans écran, on se retrouve à devoir y brancher un
clavier.

1. **Avant tout :** s'assurer qu'une clé fonctionne déjà (se connecter avec elle). Sinon, on
   s'enferme dehors en refusant les mots de passe.
2. Créer le fichier, par exemple :
   ```bash
   sudo nano /etc/ssh/sshd_config.d/99-keys-only.conf
   ```
   (Kai ne précise pas avec quel outil il l'a créé; le contenu, lui, est celui ci-dessus.)
3. Vérifier que la configuration n'a pas d'erreur de syntaxe. Aucune réponse = aucune erreur :
   ```bash
   sudo sshd -t
   ```
4. Vérifier la configuration **réellement appliquée** :
   ```bash
   sudo sshd -T | grep -i -E "passwordauthentication|kbdinteractive|pubkeyauthentication"
   ```
   On doit voir `no`, `no`, `yes`. Cette étape compte : dans SSH, pour un même réglage, **la
   première valeur lue gagne**. Si un autre fichier du dossier, lu avant celui-ci, disait le
   contraire, c'est lui qui l'emporterait. `sshd -T` montre le résultat final, peu importe d'où
   il vient.
5. Seulement ensuite, redémarrer le service :
   ```bash
   sudo systemctl restart ssh
   ```
   Conseil : garder la session SSH actuelle ouverte pendant ce redémarrage (elle n'est pas
   coupée), et faire les tests dans une **deuxième** fenêtre. Si quelque chose cloche, on peut
   encore corriger depuis la première.
6. Tester les trois cas. Sur ce serveur : la clé de l'administrateur depuis Windows a été
   acceptée; une tentative par mot de passe a été refusée avec
   `Permission denied (publickey)`; la clé de Kai via Tailscale a été acceptée.

Pour forcer un essai par mot de passe et confirmer le refus :

```powershell
ssh -o PubkeyAuthentication=no NOM_UTILISATEUR@IP_DU_PORTABLE
```

## Étape 3 — Stabiliser le wifi pour un usage serveur

*Sans objet depuis que le serveur est en Ethernet. Gardé pour qui doit rester en wifi.*

Un serveur en wifi doit se reconnecter tout seul, même quand personne n'est connecté sur le
bureau. Deux réglages sont nécessaires.

**Rendre la connexion « système ».** Une connexion wifi créée avec l'icône de Xfce est liée à
la session de l'utilisateur : au redémarrage, tant que personne ne se connecte, pas de wifi.

```bash
nmcli connection show                                   # trouver le nom exact
nmcli connection show "NomDuWifi" | grep -i -E "permissions|autoconnect"
```

- `connection.permissions` doit être **vide**, et `connection.autoconnect` doit valoir `yes`.
- Sinon :

  ```bash
  nmcli connection modify "NomDuWifi" connection.autoconnect yes
  sudo nmcli connection modify "NomDuWifi" connection.permissions ""
  ```

**Désactiver l'économie d'énergie du wifi.** Le pilote `b43` est connu pour se déconnecter
souvent quand la carte se met en veille. Le wiki Debian recommande de couper cette économie :

```bash
sudo nano /etc/NetworkManager/conf.d/wifi-powersave.conf
```

```ini
[connection]
wifi.powersave = 2
```

```bash
sudo systemctl restart NetworkManager
```

**Le test qui tranche :** redémarrer le portable, **ne pas** se connecter sur le bureau,
attendre deux ou trois minutes, puis faire un `ping` ou un `ssh` depuis l'autre ordinateur.
S'il répond, le wifi monte bien sans session ouverte.

Ces réglages n'ont jamais été testés sur la machine : leur résultat est inconnu (temporaire).

## Étape 4 — Une adresse IP fixe

Le routeur distribue les adresses automatiquement (**DHCP**), avec un bail qui expire : au
prochain redémarrage, le portable pourrait changer d'adresse. Un serveur DNS qui change
d'adresse, c'est un réseau qui ne fonctionne plus.

La bonne méthode est une **réservation DHCP** dans le routeur : on associe l'adresse matérielle
de la carte réseau (l'adresse **MAC**) à une IP fixe.

```bash
ip link                  # adresse MAC : ligne "link/ether" sous l'interface utilisée (Ethernet ou wifi)
ip route | grep default  # l'IP après "via" est celle du routeur
```

> **Attention :** en passant du wifi à l'Ethernet, l'adresse MAC change (chaque carte a la
> sienne). Une réservation faite pour le wifi ne s'applique pas au câble.

On ouvre ensuite l'IP du routeur dans un navigateur et on cherche une section du genre
*DHCP → Réservation d'adresse* (*Address Reservation*, *Static Lease*).

**Ce qui s'est passé ici :** la passerelle fournie par le fournisseur internet est verrouillée.
Les identifiants par défaut ne fonctionnaient pas et l'étiquette de l'appareil ne donnait que
le mot de passe wifi. La réservation n'a donc pas pu être faite.

**Ce qui compense en partie :** depuis octobre, le serveur a aussi une adresse **Tailscale**
(voir l'étape 8), qui est stable par conception. Pour l'administration à distance, elle suffit.
Par contre, les appareils de la maison pointent leur DNS vers l'adresse **locale** du serveur :
si elle change un jour, il faudra les reconfigurer.

**Comment l'adresse locale tient pour l'instant** (information de Kai, transmise par
On-100-K-Lisse le 27 octobre 2026) : par le **bail DHCP**, que le serveur renouvelle en continu
tant qu'il est allumé. Le routeur lui redonne donc la même adresse à chaque renouvellement.

**La faille de cette solution :** un bail a une durée limitée. Si le serveur reste éteint plus
longtemps que cette durée (une panne de courant la nuit, et personne pour le rallumer, voir
l'étape 5), le routeur peut donner son adresse à un autre appareil, et le serveur en reçoit une
nouvelle au redémarrage. Les appareils de la maison basculent alors sur leur DNS secondaire
(Internet continue de marcher, mais sans le bloqueur), jusqu'à ce qu'on corrige leur DNS.

**Combien de temps ça laisse :** le bail de la passerelle dure **48 heures** (mesuré par Kai
sur le serveur le 27 octobre 2026 : `dhcp_lease_time = 172800` secondes). Un appareil renouvelle
normalement son bail à mi-parcours, donc au moment d'une panne, il lui reste entre 24 et
48 heures. En pratique :

- **panne de moins de 24 heures :** l'adresse attend le serveur, rien à faire;
- **panne de plus de 24 à 48 heures :** la passerelle *peut* donner l'adresse à un autre
  appareil. C'est possible, pas automatique : sur un réseau de maison avec peu d'appareils, la
  passerelle a beaucoup d'adresses libres et réattribue rarement une adresse récente.

**Après une longue panne :** vérifier l'adresse du serveur avec `hostname -I`. Kai peut aussi le
faire à distance : son accès passe par Tailscale, qui ne dépend pas de l'adresse locale, et sa
surveillance remarque déjà les redémarrages imprévus. Si l'adresse a changé, corriger le DNS
principal des appareils branchés (étape 7).

## Étape 5 — Empêcher la mise en veille

*Fait et prouvé par Kai le 26 octobre : écran fermé, la machine ne se met ni en veille ni
hors tension, et reste joignable.*

Un portable se met en veille quand on rabat l'écran : le serveur s'endort, et le blocage de pub
tombe pour toute la maison. Kai a combiné deux mesures :

**1. Dire à systemd-logind d'ignorer la fermeture de l'écran :**

```bash
sudo nano /etc/systemd/logind.conf
```

Retirer le `#` devant ces lignes et mettre `ignore` :

```ini
HandleLidSwitch=ignore
HandleLidSwitchExternalPower=ignore
HandleLidSwitchDocked=ignore
```

```bash
sudo systemctl restart systemd-logind
```

**2. Masquer les cibles de veille de systemd.** « Masquer » rend une unité systemd impossible à
démarrer, peu importe qui le demande. C'est ce qui règle aussi le gestionnaire d'alimentation de
Xfce, qui peut sinon demander une veille par-dessus le réglage de logind :

```bash
sudo systemctl mask sleep.target suspend.target hibernate.target hybrid-sleep.target
```

Pour annuler plus tard : la même commande avec `unmask`.

### Une limite matérielle : batterie morte, pas de redémarrage automatique

La batterie du portable est morte : à la moindre coupure de courant, il s'éteint net. Et le
BIOS (Phoenix SecureCore A07) n'offre **ni redémarrage automatique au retour du courant**
(*AC Recovery*), **ni réveil par le réseau** (*Wake-on-LAN*). Après une panne de courant, le
serveur reste donc éteint jusqu'à ce que quelqu'un appuie sur le bouton. C'est ce qui rend le
DNS de secours de l'étape 7 et la surveillance de l'étape 8 importants.

## Étape 6 — Installer AdGuard Home

*En service depuis le 26 octobre : AdGuard Home v0.107.79, installé par Kai.*

### Le principe : un « DNS sinkhole »

Le **DNS** traduit un nom de site (`exemple.com`) en adresse IP. Si le portable devient le
serveur DNS des appareils de la maison, il peut répondre « ce site n'existe pas » (en pratique,
l'adresse `0.0.0.0`) aux noms de domaines publicitaires : la pub ne se charge jamais, sans
rien installer sur l'appareil.

### Pourquoi AdGuard Home plutôt que Pi-hole

| | AdGuard Home | Pi-hole |
|---|---|---|
| Installation | Un seul programme, aucune dépendance | Script et plusieurs paquets |
| DNS chiffré (DoH/DoT) | Inclus | À ajouter (unbound, cloudflared) |
| Interface | Plus moderne | Plus classique |
| Communauté et tutoriels | Bonne | Très grande |

Pour une vieille machine, AdGuard Home a moins de pièces qui peuvent briser. Les deux sont de
bons choix.

### Installation

Vérifier d'abord que le port 53 (le port du DNS) est libre :

```bash
sudo ss -tulpn | grep :53
```

Si rien ne s'affiche, c'est bon. Si `systemd-resolved` apparaît, il faut le désactiver
proprement avant de continuer.

Installer avec le script officiel d'AdGuard :

```bash
curl -s -S -L https://raw.githubusercontent.com/AdguardTeam/AdGuardHome/master/scripts/install.sh | sudo sh -s -- -v
sudo systemctl status AdGuardHome
```

Ce `curl | sh` télécharge un script et l'exécute directement : à ne faire qu'avec une source de
confiance. Le script installe AdGuard Home dans `/opt/AdGuardHome/` et le déclare comme service
systemd, qui redémarre tout seul au démarrage de la machine.

Kai indique avoir installé le **binaire officiel**; s'il a utilisé ce script ou une autre
méthode : inconnu (temporaire).

### Configuration

Depuis l'autre ordinateur, ouvrir `http://IP_DU_PORTABLE:3000` et suivre l'assistant
(choisir un vrai mot de passe administrateur). Ensuite, l'interface est sur le port 80.

Réglages en service sur la machine :

- **Résolveur en amont** (le serveur DNS à qui AdGuard pose les questions qu'il ne bloque pas) :
  **Quad9 en DNS-over-HTTPS**, `https://dns.quad9.net/dns-query`. Les requêtes qui sortent de la
  maison sont ainsi chiffrées.
- **Une seule liste de blocage** pour commencer, « AdGuard DNS filter ». Empiler les listes d'un
  coup rend les faux positifs impossibles à attribuer.

### Le piège à vérifier absolument : la liste « activée » à zéro règle

Vécu par Kai le 26 octobre : après l'assistant, la liste s'affichait **activée** mais contenait
**0 règle**, parce qu'elle n'avait jamais été téléchargée. Rien n'était bloqué, même si
l'interface donnait l'impression du contraire. Solution : *Filtres → Listes de blocage DNS →
Vérifier les mises à jour*. La liste s'est alors chargée : 184 284 règles.

**Toujours vérifier le compteur de règles**, puis faire un vrai test, par exemple depuis le
serveur :

```bash
dig @127.0.0.1 doubleclick.net +short   # doit répondre 0.0.0.0 (bloqué)
dig @127.0.0.1 debian.org +short        # doit répondre une vraie adresse (résolu)
```

(`dig` vient du paquet `bind9-dnsutils` : `sudo apt install bind9-dnsutils`.)

Résultat observé sur le premier ordinateur branché, dès la première soirée : environ 44 % des
requêtes DNS bloquées.

## Étape 7 — Diriger les appareils vers le bloqueur

### Le cas idéal, impossible ici

Normalement, on va dans le routeur (*DHCP → Serveurs DNS*) et on y met l'IP du serveur : tous
les appareils de la maison l'utilisent automatiquement. La passerelle du fournisseur étant
verrouillée, cette option n'existe pas ici.

### Ce qui est fait : chaque appareil, à la main

Dans les réglages réseau de chaque appareil, on remplace le DNS automatique par :

| | Valeur | Pourquoi |
|---|---|---|
| DNS principal | L'IP locale du serveur | Le blocage de pub |
| DNS secondaire | `94.140.14.14` (DNS public d'AdGuard) | Si le serveur tombe, Internet continue de marcher — et ce DNS public filtre lui aussi une partie des pubs |

Au 26 octobre : l'ordinateur principal et une console de jeu sont branchés. Les autres appareils
seront ajoutés un à la fois.

### Le choix du DNS secondaire : une décision, pas un oubli

Dans la première version de ce guide (juillet), Claude recommandait de **laisser le secondaire
vide**, parce que beaucoup d'appareils interrogent leurs deux DNS sans ordre strict : une partie
des requêtes contourne alors le bloqueur.

Ça reste vrai, mais avec un seul serveur, sur un portable sans batterie et sans redémarrage
automatique, un secondaire vide veut dire : **le serveur tombe = plus aucun site ne charge** sur
les appareils branchés. Kai a présenté les deux options le 22 octobre, et **On-100-K-Lisse a
choisi la disponibilité d'abord** : mieux vaut revoir des pubs temporairement que perdre
Internet. Le DNS public d'AdGuard limite le compromis, puisqu'il bloque lui aussi des pubs.

### Reconnaître un appareil dans le panneau d'AdGuard

Le panneau liste les appareils par adresse IP. Pour savoir qui est qui, Kai a documenté une
méthode en cinq étapes (comparer l'adresse MAC vue par le serveur avec celle affichée par
l'appareil, puis enregistrer un « client persistant » avec l'IP **et** la MAC). Voir son journal
dans [`ai/kai/homelab`](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/kai/homelab).

### Ce qu'un bloqueur DNS ne peut pas bloquer

Les pubs intégrées aux vidéos YouTube passent quand même : YouTube les sert depuis les mêmes
domaines que les vidéos, et bloquer le domaine bloquerait la vidéo. Aucun bloqueur DNS
(AdGuard, Pi-hole ou autre) ne peut séparer les deux. Explication complète dans le journal de
Kai.

## Étape 8 — Accès à distance via Tailscale et surveillance

*Actif depuis le 26 octobre, mis en place par Kai.*

**Tailscale** crée un réseau privé chiffré entre des machines choisies, peu importe où elles
sont. Chaque machine y reçoit une adresse en `100.x.x.x` qui ne change pas. Ici, il sert à deux
choses :

- administrer le serveur à distance par SSH, avec une adresse stable;
- permettre à Kai (un agent IA) de se connecter au serveur pour le surveiller.

Les garde-fous en place : On-100-K-Lisse est l'administrateur du réseau Tailscale; la machine de
Kai n'y est entrée qu'avec son approbation et peut en être retirée en tout temps de son bord; le
compte de Kai sur le serveur n'a pas de droits `sudo` sans mot de passe.

**La surveillance :** une vérification par heure, en lecture seule (la machine répond-elle? a-t-
elle redémarré? le disque dépasse-t-il 90 %? un service est-il en échec? AdGuard résout-il encore
les noms?). Règle : silence si tout va bien, alerte seulement en cas de problème.

Le plan complet (pourquoi Tailscale, plan de continuité en cinq couches, sauvegarde de la
configuration vers une machine de secours) est dans le plan de relance de Kai.

---

## Avertissements

- **Point de défaillance unique :** il n'y a qu'un serveur. Le DNS secondaire de l'étape 7 évite
  la panne d'Internet, mais pendant une panne, le blocage de pub est réduit.
- **Après une panne de courant, il faut rallumer le serveur à la main** (batterie morte, aucune
  option de redémarrage automatique dans le BIOS).
- **Prévenir les autres occupants** avant de changer le DNS d'un appareil qui n'est pas le sien.

## Sources

- Juillet 2026 : sessions de travail entre On-100-K-Lisse et Claude (étapes 1 à 4, et le plan
  initial des étapes 5 à 7).
- 22 et 26 octobre 2026 : [plan de relance et journal de réalisation de Kai](https://github.com/On-100K-Lisse/travail-avec-ia/tree/ai/kai/homelab)
  (machine, étapes 2, 5 à 8, chiffres et constats). Les faits d'octobre de ce guide viennent de
  ce journal; Claude ne les a pas vérifiés lui-même sur la machine.

## Crédits

- [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome), par AdGuard (licence GPL-3.0);
  « AdGuard DNS filter » et le DNS public d'AdGuard, par AdGuard.
- [Pi-hole](https://pi-hole.net/), par le projet Pi-hole.
- [Tailscale](https://tailscale.com/), par Tailscale Inc.
- [Quad9](https://quad9.net/), par la fondation Quad9.
- Le pilote `b43` et `firmware-b43-installer` : projet du noyau Linux et Debian; le firmware
  appartient à Broadcom.
- La relance d'octobre (plan, exécution, surveillance) : Kai.
- L'idée de faire du portable un homelab de pratique : Gemini.
