# Récupérer l'accès au serveur : nom d'utilisateur, mot de passe, clé SSH

**Rédigé par :** Claude, le 27 octobre 2026, à partir d'une question d'On-100-K-Lisse du
26 octobre 2026 (« je ne me souviens plus de mon nom d'utilisateur ni de mon mot de passe »).

**État :** méthodes générales pour Debian. Elles n'ont **pas encore été testées** sur le
serveur du homelab : résultat inconnu (temporaire).

**À quoi sert ce guide :** un serveur qu'on n'utilise pas pendant des semaines, c'est un
serveur dont on oublie les identifiants. Ce guide explique comment retrouver un nom
d'utilisateur, réinitialiser un mot de passe oublié, et récupérer l'accès SSH si on perd sa clé.
Il suppose qu'on a un **accès physique** à la machine (clavier et écran branchés dessus).

## D'abord : de quoi a-t-on vraiment besoin?

Depuis le 26 octobre 2026, le serveur n'accepte plus les connexions SSH par mot de passe :
seulement par **clé SSH** (voir l'étape 2 du [guide d'installation](installation-et-services.md)).
Ça change ce qu'un oubli bloque :

| Ce qu'on a perdu | Ce qui ne marche plus | Ce qui marche encore |
|---|---|---|
| Le mot de passe du compte | Ouvrir une session sur le portable lui-même, utiliser `sudo` | La connexion SSH, si la clé est toujours sur l'ordinateur habituel |
| La clé SSH (ordinateur réinstallé, fichier effacé) | La connexion SSH | La session directement sur le portable, avec le mot de passe |
| Les deux | Tout, sauf l'accès physique | La méthode GRUB ci-dessous |

Donc avant toute chose : **essayer la connexion SSH habituelle**. Si elle marche, on peut
changer le mot de passe à distance avec `passwd` (il demandera l'ancien) — et si on ne le
connaît plus, passer à la méthode GRUB.

## Retrouver le nom d'utilisateur

**Le plus simple : l'ordinateur d'où on se connecte s'en souvient.** Sous Windows, PowerShell
garde un historique des commandes tapées. Celle-ci y cherche les anciennes connexions SSH :

```powershell
Get-Content (Get-PSReadLineOption).HistorySavePath | Select-String "ssh "
```

Une ligne du genre `ssh nom@adresse` apparaît : ce qui est avant le `@` est le nom
d'utilisateur.

**Sinon, sur le portable :** l'écran de connexion de Xfce affiche généralement le nom. Une fois
connecté, `whoami` le donne; `ls /home` liste tous les comptes qui ont un dossier personnel.

> Le nom de la machine (affiché par exemple dans `utilisateur@machine:~$`) n'est **pas** le nom
> d'utilisateur. C'est une confusion courante.

## Réinitialiser un mot de passe oublié

Un mot de passe Linux n'est pas stocké en clair : il est *haché* (transformé de façon
irréversible). Personne ne peut le « retrouver », mais avec un accès physique, on peut en mettre
un nouveau.

### Méthode 1 — Avec le mot de passe root

Si un mot de passe **root** (le compte administrateur) a été défini à l'installation de Debian
et qu'on s'en souvient, c'est la voie la plus simple. Sur le portable :

```bash
su -
passwd NOM_UTILISATEUR
```

Entrer le nouveau mot de passe deux fois. Rien ne s'affiche pendant la frappe, c'est normal.

### Méthode 2 — Par le menu de démarrage (GRUB)

**GRUB** est le menu qui s'affiche au démarrage, avant Debian. On peut lui demander de démarrer
directement sur un terminal administrateur, sans mot de passe.

1. Redémarrer le portable. Au menu GRUB (« Debian GNU/Linux »), appuyer sur **`e`**.
   Si le menu passe trop vite, maintenir **Shift** ou appuyer sur **Échap** pendant le démarrage.
2. Trouver la ligne qui commence par `linux` et se termine par `ro quiet`. Remplacer `ro quiet`
   par :
   ```
   rw init=/bin/bash
   ```
   `rw` monte le disque en écriture (sinon on ne pourrait pas enregistrer le nouveau mot de
   passe), et `init=/bin/bash` démarre un terminal au lieu du système normal.
3. Appuyer sur **Ctrl+X** (ou F10). Un terminal root apparaît.
4. Changer le mot de passe :
   ```bash
   passwd NOM_UTILISATEUR
   ```
5. Écrire les changements sur le disque, puis reprendre le démarrage normal :
   ```bash
   sync
   exec /sbin/init
   ```
   Si l'écran gèle, éteindre avec le bouton et rallumer : le mot de passe est déjà enregistré
   grâce au `sync`.

Cette modification de GRUB n'est **pas permanente** : elle ne vaut que pour ce démarrage.

**Limite :** si le disque est chiffré (LUKS), Debian demande la phrase de chiffrement avant tout
le reste, et cette méthode ne la contourne pas. Le disque du serveur est-il chiffré : inconnu
(temporaire).

**Pourquoi ça marche, et ce que ça veut dire pour la sécurité :** quiconque a le portable entre
les mains peut faire la même chose. C'est normal pour un serveur à la maison; c'est aussi
pourquoi un ordinateur qui contient des données sensibles a besoin d'un disque chiffré.

## Récupérer l'accès SSH après avoir perdu sa clé

Comme le serveur refuse les mots de passe en SSH, on ne peut pas y envoyer une nouvelle clé à
distance. Il faut passer par le portable lui-même.

1. **Sur l'ordinateur d'où on se connecte**, créer une nouvelle paire de clés :
   ```powershell
   ssh-keygen -t ed25519
   ```
   Accepter l'emplacement proposé. La partie publique est le fichier qui finit par `.pub`
   (par exemple `id_ed25519.pub`). **La partie privée (sans `.pub`) ne quitte jamais cet
   ordinateur.**
2. Copier **seulement** le fichier `.pub` sur une clé USB.
3. **Sur le portable**, en session locale, ajouter la clé publique à la liste des clés
   autorisées :
   ```bash
   mkdir -p ~/.ssh
   cat /chemin/vers/la/cle-usb/id_ed25519.pub >> ~/.ssh/authorized_keys
   chmod 700 ~/.ssh
   chmod 600 ~/.ssh/authorized_keys
   ```
   Les `chmod` comptent : SSH refuse une liste de clés que d'autres comptes pourraient
   modifier.
4. Tester depuis l'ordinateur : `ssh NOM_UTILISATEUR@IP_DU_PORTABLE`.

Le `>>` **ajoute** la clé à la fin du fichier sans effacer les autres — dont celle de Kai, qui
sert à la surveillance. Un seul `>` remplacerait tout le fichier.

## Pour ne pas revivre ça

- Noter le nom d'utilisateur et le mot de passe dans un **gestionnaire de mots de passe**.
- Sauvegarder la clé SSH privée dans ce même gestionnaire, ou sur un support chiffré.
- **Ne jamais** donner un mot de passe ou une clé privée à une IA, ni les mettre dans ce dépôt.
  Les IA de ce projet n'en ont pas besoin : Kai a sa propre clé, et les autres guident sans
  jamais voir les identifiants.
