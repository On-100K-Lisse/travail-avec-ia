# Recycler un vieux portable en homelab Debian (wifi Broadcom, SSH, bloqueur de pub)

**Date :** juillet 2026. **Rédigé par :** Claude, à partir d'une session de travail avec
On-100-K-Lisse.

**À quoi sert ce guide :** transformer un vieux portable en petit serveur maison (un
*homelab*, un laboratoire personnel) qui reste allumé dans un coin et qu'on contrôle à distance
depuis un autre ordinateur. Le but final : en faire un **bloqueur de pub pour tout le réseau**
de la maison. C'est aussi un excellent terrain de pratique pour l'administration système et
réseau.

## État d'avancement

| Étape | État |
|---|---|
| 1. Faire marcher le wifi (carte Broadcom BCM4322) | ✅ Fait et testé |
| 2. Accès à distance par SSH | ✅ Fait et testé |
| 3. Stabiliser le wifi pour un serveur | Commandes données, résultat inconnu (temporaire) |
| 4. Adresse IP fixe (réservation DHCP) | ⚠️ Bloqué : la passerelle du fournisseur internet est verrouillée |
| 5. Empêcher la mise en veille | Pas encore fait |
| 6. Installer AdGuard Home | Pas encore fait |
| 7. Diriger le réseau vers le bloqueur | Pas encore fait |

Les étapes 5 à 7 décrivent donc **le plan**, pas un résultat vérifié sur cette machine.

## La machine de départ

| Élément | Valeur |
|---|---|
| Portable | Dell Studio 1537 (modèle PP33L), vers 2008-2009 |
| Système | Debian 13 « Trixie », bureau Xfce, installé avec l'ISO *netinst with firmware* |
| Noyau | 6.12 |
| Carte wifi | Broadcom BCM4322 (identifiant PCI `14e4:432b`, vendue par Dell comme « Wireless 1510 ») |
| Connexion | Wifi seulement : pas de câble ethernet libre près du routeur |

## Étape 1 — Faire marcher le wifi Broadcom

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

**Pourquoi `b43` plutôt que le pilote propriétaire `broadcom-sta-dkms` (`wl`) :** au moment
de ce guide, des utilisateurs rapportaient que `wl` plantait ou ne se chargeait pas avec le
noyau 6.12 de Trixie. Le pilote libre `b43` supporte le BCM4322 depuis longtemps.

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

Sur le portable :

```bash
sudo apt install openssh-server
sudo systemctl status ssh     # doit afficher "active (running)" ; q pour quitter
hostname -I                   # affiche l'adresse IP du portable
```

Depuis l'autre ordinateur (Windows a un client SSH intégré, dans PowerShell) :

```bash
ssh NOM_UTILISATEUR@192.168.1.50
```

La première fois, SSH montre une *empreinte* (fingerprint) du serveur et demande
confirmation : l'ordinateur la mémorise pour détecter plus tard une usurpation. On tape `yes`,
puis le mot de passe.

## Étape 3 — Stabiliser le wifi pour un usage serveur

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

## Étape 4 — Une adresse IP fixe

Le routeur distribue les adresses automatiquement (**DHCP**), avec un bail qui expire : au
prochain redémarrage, le portable pourrait changer d'adresse. Un serveur DNS qui change
d'adresse, c'est un réseau qui ne fonctionne plus.

La bonne méthode est une **réservation DHCP** dans le routeur : on associe l'adresse matérielle
de la carte (l'adresse **MAC**) à une IP fixe. Le routeur reste ainsi la seule autorité sur les
adresses, sans risque de conflit.

```bash
ip link                  # adresse MAC : ligne "link/ether" sous l'interface WIFI (wlan0), pas ethernet
ip route | grep default  # l'IP après "via" est celle du routeur
```

On ouvre ensuite l'IP du routeur dans un navigateur, et on cherche une section du genre
*DHCP → Réservation d'adresse* (*Address Reservation*, *Static Lease*).

> **Limite rencontrée :** certaines passerelles fournies par les fournisseurs internet sont
> verrouillées. Ici, les identifiants par défaut ne fonctionnaient pas et l'étiquette de
> l'appareil ne donnait que le mot de passe wifi, pas l'accès administrateur. Même avec
> l'accès, ces passerelles ne permettent souvent pas de changer les serveurs DNS distribués
> au réseau. Les solutions de rechange sont décrites à l'étape 7.

## Étape 5 — Empêcher la mise en veille (plan)

Un portable se met en veille quand on rabat l'écran : le serveur s'endort, et Internet tombe
pour toute la maison. Il y a **deux couches** à désactiver, et c'est le piège classique de
n'en faire qu'une.

**Couche 1, systemd-logind :**

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

**Couche 2, le gestionnaire d'alimentation de Xfce,** qui passe par-dessus logind sur un
bureau graphique : Paramètres → Gestionnaire d'alimentation → onglet *Système* → « À la
fermeture du portable » → **Éteindre l'écran** (et non suspendre), sur secteur **et** sur
batterie.

## Étape 6 — Installer AdGuard Home (plan)

### Le principe : un « DNS sinkhole »

Le **DNS** traduit un nom de site (`exemple.com`) en adresse IP. Si le portable devient le
serveur DNS du réseau, il peut répondre « ce site n'existe pas » aux noms de domaines
publicitaires : la pub ne se charge jamais, sur **tous** les appareils (téléphone, télé,
console), sans rien installer dessus.

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
systemd.

### Configuration

Depuis l'autre ordinateur, ouvrir `http://IP_DU_PORTABLE:3000` et suivre l'assistant
(choisir un vrai mot de passe administrateur). Ensuite, l'interface est sur le port 80.

Dans *Paramètres → Paramètres DNS*, choisir un serveur amont chiffré, par exemple
`https://dns.quad9.net/dns-query` : les requêtes qui sortent de la maison sont alors chiffrées.

## Étape 7 — Diriger le réseau vers le bloqueur (plan)

**Cas normal :** dans le routeur, *DHCP → Serveurs DNS*, mettre l'IP du portable en serveur
principal, et **laisser le secondaire vide**. Avec un serveur secondaire (comme 8.8.8.8), les
appareils l'utilisent au hasard et contournent le blocage la moitié du temps.

**Si la passerelle est verrouillée,** deux solutions de rechange :

1. **Configurer le DNS à la main sur chaque appareil** pour qu'il pointe vers le portable. Simple,
   mais à refaire sur chaque appareil.
2. **Désactiver le DHCP de la passerelle** (si elle le permet) et laisser AdGuard Home
   distribuer les adresses lui-même : il a un serveur DHCP intégré. Plus avancé, mais très
   formateur.

## Avertissements

- **Un serveur DNS en wifi de 2008, c'est fragile.** Un câble ethernet vers le routeur est
  l'amélioration la plus rentable de tout le projet.
- **Point de défaillance unique :** si le portable plante, plus personne n'a Internet dans la
  maison. Prévenir les autres occupants avant de basculer le DNS du réseau.

## Crédits

- [AdGuard Home](https://github.com/AdguardTeam/AdGuardHome), par AdGuard (licence GPL-3.0).
- [Pi-hole](https://pi-hole.net/), par le projet Pi-hole.
- Le pilote `b43` et `firmware-b43-installer` : projet du noyau Linux et Debian; le firmware
  appartient à Broadcom.
- L'idée de faire du portable un homelab de pratique a été proposée par Gemini.
