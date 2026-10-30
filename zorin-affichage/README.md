# Zorin OS : écran en « bouillie de pixels », `nomodeset` et résolution

**Date :** juillet 2026. **Rédigé par :** Claude, à partir d'une session de dépannage avec
On-100-K-Lisse.

**À quoi sert ce guide :** comprendre pourquoi un vieil ordinateur sous Linux affiche une image
brouillée au démarrage, pourquoi l'option `nomodeset` règle l'image mais casse la détection de
l'écran, et comment obtenir la meilleure résolution possible malgré ça.

## État d'avancement

**Problème contourné, pas résolu.** L'ordinateur fonctionne avec une résolution correcte mais
pas idéale. La vraie solution dépend de la carte graphique, dont le modèle est **inconnu
(temporaire)** : la commande pour l'identifier n'a pas encore été lancée sur la machine.

## La situation de départ

| Élément | Valeur |
|---|---|
| Système | Zorin OS 18 Lite (bureau Xfce, base Ubuntu 24.04, noyau 6.17, serveur graphique X11) |
| Démarrage | BIOS « legacy » (ancien mode), pas UEFI |
| Carte graphique | Inconnue (temporaire) |
| Écran visé | Une télé dont la résolution est inconnue |
| Symptôme | Sans `nomodeset` : image en bouillie de pixels. Avec `nomodeset` : image correcte, mais en basse résolution et sans détection de l'écran |

## Comprendre le problème

Quelques termes d'abord :

- **KMS** (*Kernel Mode Setting*) : le mécanisme par lequel le pilote de la carte graphique
  règle lui-même l'affichage (résolution, fréquence).
- **EDID** : un petit bloc de données que l'écran envoie pour dire « je suis un 1920×1080 à
  60 Hz ». C'est grâce à lui que la résolution est détectée automatiquement.
- **`nomodeset`** : une option de démarrage qui **désactive le KMS**. Le pilote de la carte
  graphique ne prend pas le contrôle de l'écran.
- **Framebuffer** : une zone d'image « bête » que le système affiche sans pilote spécialisé.
- **VBE** (*VESA BIOS Extensions*) : la liste des modes d'affichage que le BIOS de la carte
  graphique sait offrir, en démarrage legacy.

**Le conflit :** sans KMS, le noyau ne lit jamais l'EDID. Le système tombe sur un framebuffer
générique, figé sur la résolution réglée avant son démarrage. `nomodeset` et la détection
automatique de l'écran sont donc **incompatibles par nature**.

**Ce que veut dire « bouillie de pixels » :** le pilote KMS se charge, prend le contrôle de
l'écran, mais écrit mal dans la mémoire vidéo (mauvais format de pixel, mauvaise largeur de
ligne, ou mémoire vidéo défectueuse). Le problème vient souvent de l'**accélération** du pilote,
pas du réglage d'affichage lui-même.

## Chemin 1 — Garder `nomodeset` et forcer la résolution dans GRUB

Le framebuffer hérite du mode d'affichage réglé par **GRUB**, le programme qui lance le système.
C'est donc là qu'on agit.

### Voir les modes disponibles

Au démarrage, faire apparaître le menu GRUB (tenir `Shift` en BIOS legacy, ou appuyer sur
`Échap`), appuyer sur `c` pour la console, puis taper :

```
set pager=1
videoinfo
```

`videoinfo` liste les modes que le BIOS de la carte offre réellement. Sur les anciennes versions
de GRUB, la commande s'appelait `vbeinfo`.

### Régler GRUB

```bash
sudo nano /etc/default/grub
```

```
GRUB_CMDLINE_LINUX_DEFAULT="quiet splash nomodeset"
GRUB_GFXMODE=1280x1024x32
GRUB_GFXPAYLOAD_LINUX=keep
```

- `GRUB_GFXMODE` : la résolution voulue. Elle doit **apparaître dans `videoinfo`**, et la ligne
  ne doit pas commencer par `#` (sinon elle est ignorée).
- `GRUB_GFXPAYLOAD_LINUX=keep` : **la ligne clé**. Elle dit au système de garder le mode réglé
  par GRUB. Elle n'existe pas par défaut, il faut l'ajouter.
- `GRUB_TERMINAL=console` doit rester commenté (avec `#`), sinon GRUB reste en mode texte.

Appliquer :

```bash
sudo update-grub
sudo reboot
```

Vérifier après le redémarrage :

```bash
cat /sys/class/graphics/fb0/modes
```

### Ce qui s'est passé sur cette machine

- `GRUB_GFXMODE=auto` laisse GRUB demander l'EDID de l'écran au BIOS. **Sur cette machine, ça
  ne marche pas** : le BIOS ne transmet pas l'EDID.
- Une liste de repli comme `1920x1080x32,1280x720x32,auto` a fait **reculer** l'affichage à
  640×480 : aucun de ces modes n'existait dans la table VBE, et GRUB est retombé sur son mode
  minimal, que `keep` a fidèlement transmis.
- `videoinfo` a montré que la carte n'offre **que des modes 4:3 et 5:4**, aucun mode 16:9.
  Le meilleur mode utilisable trouvé est **1280×1024 en 32 bits**.

**Conséquence pour une télé 16:9 :** l'image aura des bandes noires sur les côtés, ou sera
étirée selon les réglages de la télé. C'est utilisable, mais pas idéal.

### Si l'écran devient noir au démarrage

Au menu GRUB, appuyer sur `e` sur l'entrée du système, trouver la ligne qui commence par
`linux`, ajouter `gfxpayload=text` à la fin, puis `Ctrl+X` pour démarrer. On revient en
résolution de base, le temps de corriger le fichier.

## Chemin 2 — Retirer `nomodeset` avec une option ciblée (la vraie solution)

`nomodeset` est une masse, pas un tournevis : il coupe tout le KMS alors que le problème vient
souvent de l'accélération seulement. Une option ciblée désactive l'accélération mais **garde le
KMS**, donc l'EDID, la vraie résolution et la détection de l'écran.

Identifier la carte graphique :

```bash
lspci -k | grep -A3 -i vga
```

Puis remplacer `nomodeset` dans `GRUB_CMDLINE_LINUX_DEFAULT` par l'option correspondante :

| Carte | Option à essayer |
|---|---|
| NVIDIA (pilote libre `nouveau`) | `nouveau.noaccel=1` |
| AMD / ATI (pilote `radeon`) | `radeon.noaccel=1`, ou `radeon.agpmode=-1` sur une vieille carte AGP |
| Intel | `i915.modeset=1 i915.enable_psr=0` |

On perd l'accélération 3D, mais un bureau Xfce n'en a pas besoin.

Deux autres pistes rapides, à tester en éditant la ligne `linux` au menu GRUB :

- Retirer seulement `splash` (garder `quiet`, retirer `nomodeset`) : parfois, la bouillie vient
  seulement de l'écran de démarrage animé.
- Sans `nomodeset`, ajouter `video=VGA-1:1280x1024@60` pour imposer un mode connu au pilote.

**Si rien ne marche :** sur une machine de cet âge, une image brouillée persistante peut aussi
venir d'une **mémoire vidéo défectueuse**. Une carte graphique d'occasion peu chère réglerait
alors le problème pour de bon, mieux que des heures de contournements logiciels.

## Brancher sur une télé

- **Overscan :** beaucoup de télés coupent 3 à 5 % du bord de l'image sur l'entrée HDMI. Chercher
  dans les réglages de la télé un mode « Just Scan », « 1:1 », « Screen Fit », « Full Pixel » ou
  « PC ».
- **Télé 4K sans accélération :** l'affichage sortirait en 3840×2160 sans mise à l'échelle, avec
  un texte minuscule et un bureau lent. Dans ce cas, forcer une résolution plus basse.

## À faire

- [ ] Lancer `lspci -k | grep -A3 -i vga` pour identifier la carte graphique.
- [ ] Tester l'option ciblée du chemin 2 correspondant à la carte.
- [ ] Mettre à jour ce guide avec le résultat.

---

Retour : [README de la branche](../README.md) · [Accueil du dépôt](https://github.com/On-100K-Lisse/travail-avec-ia)
