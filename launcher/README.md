# DLSS 5 Launcher

Petit programme Windows (une seule fenêtre, en français) qui :

1. **scanne** les jeux installés (Steam, Epic, GOG, plus des dossiers ajoutés à la main) ;
2. dit pour chaque jeu **si et comment** DLSS 5 Neural Rendering peut y être injecté ;
3. **installe** l'effet en un clic ;
4. **remet le dossier du jeu exactement comme avant** en un clic, même si un autre outil l'avait
   modifié.

Écrit par Claude (Anthropic) pour On-100-K-Lisse, le 24 et 25 octobre 2026, en C# (WinForms,
.NET Framework 4.8, déjà présent dans Windows 10 et 11).

> **Ce dépôt ne contient que le code et la documentation.** Les fichiers de NVIDIA
> (`nvngx_dlssnr.dll`, `nvngx_dlss.dll`…), OptiScaler, ReShade, RenoDX, les shaders et les autres
> composants de tiers **ne sont pas publiés ici**. Chacun a sa propre licence (voir
> [Crédits](#crédits)) et doit être obtenu à sa source. Le launcher les copie depuis un dossier
> `payload\` que l'utilisateur prépare lui-même (voir [Ce qu'il faut à côté](#ce-quil-faut-à-côté)).

## Quelques mots de vocabulaire

| Terme | Ce que ça veut dire ici |
|---|---|
| **NR** | Neural Rendering, l'effet « DLSS 5 » : un modèle d'IA qui retouche chaque image du jeu. |
| **Upscaler** | La technologie du jeu qui calcule l'image en plus petit puis l'agrandit (DLSS, FSR, XeSS). Le NR s'y accroche pour avoir les infos dont il a besoin (mouvement, profondeur). |
| **OptiScaler** | Un mod libre qui intercepte l'upscaler du jeu et peut y brancher le NR. |
| **DLL proxy** | Une DLL placée à côté de l'exe du jeu sous un nom que le jeu charge de lui-même (`dxgi.dll`, `winmm.dll`, `version.dll`…). C'est comme ça que les mods se font charger. |
| **Feeder** | Pour les jeux **sans** upscaler : le mod fabrique lui-même les infos de mouvement à partir de l'image (flux optique), pour « nourrir » le NR. |
| **Preset A / B** | Les deux modèles NR de la DLL 310.9 : A = gros modèle, B = petit modèle (environ deux fois moins cher). |
| **Ray Reconstruction (RR)** | Débruiteur de NVIDIA pour le ray tracing. Quand il est actif, le NR doit tourner après lui. |
| **Manifeste** | Le fichier où le launcher note chaque fichier qu'il ajoute ou remplace, pour pouvoir tout défaire. |

## Les quatre méthodes d'installation

En mode **Auto**, le launcher choisit selon le jeu.

| Méthode | Pour quels jeux | Ce qui est copié dans le jeu | Menu en jeu |
|---|---|---|---|
| **OptiScaler F5** (défaut) | Jeux avec DLSS, FSR ou XeSS | OptiScaler F5 sous un nom de DLL proxy, son forwarder `nvngx.dll_dlssnr.dll`, son dossier `OptiScaler\`, un `OptiScaler.ini` généré, la DLL NR | Touche Inser |
| **RenoDX / ReShade** | Jeux avec DLSS natif (cartes NVIDIA seulement) | ReShade (`dxgi.dll`), l'addon RenoDX, un `ReShade.ini` généré, la DLL NR | Touche Début |
| **Feeder F5Low** | Jeux **sans** upscaler, en DX11, DX12 ou Vulkan | Le même OptiScaler F5, réglé en mode « Optical F5Low » (il estime le mouvement de l'image et cherche la profondeur du jeu) | Inser → page Optical F5Low |
| **Feeder ReShade** | Alternative, et jeux OpenGL (cartes NVIDIA seulement) | ReShade + RenoDX + DLSS5-Feeder + shaders LumeniteFX + `nvngx_dlss.dll` | Début |

Les jeux 32-bit et DX9 ne sont pas pris en charge.

## Ce qu'il fait, en détail

### Le scan

- **Bibliothèques :** Steam (`libraryfolders.vdf` et les `appmanifest_*.acf`), Epic (manifestes
  `.item`), GOG (registre), et les dossiers ajoutés à la main.
- **Exe principal :** il cherche le vrai exe du jeu. Par exemple, `…-Win64-Shipping.exe` pour Unreal
  plutôt que le petit lanceur à la racine. Il écarte les exe de crash, de setup et d'anti-cheat.
- **API graphique :**
  - d'abord, ce que le jeu a **vraiment utilisé** à sa dernière partie : `Player.log` de Unity dans
    `AppData\LocalLow\<éditeur>\<jeu>`, logs d'Unreal dans `AppData\Local\<projet>\Saved\Logs` ;
  - sinon, une déduction à partir des DLL importées par l'exe et les autres DLL du jeu (lecture de
    l'en-tête PE), plus quelques règles par moteur.
- **Upscalers :** fichiers `nvngx_dlss.dll`, `nvngx_dlssd.dll` (RR), `sl.interposer.dll`
  (Streamline), `libxess*.dll`, `ffx_fsr*.dll` ou `amd_fidelityfx*.dll`.
- **Anti-cheat :** EasyAntiCheat, BattlEye, EA AntiCheat, PunkBuster et quelques autres. Ces jeux
  sont marqués en rouge, parce qu'injecter une DLL dans un jeu en ligne peut mener à un
  bannissement.
- **Autres mods :** OptiScaler, ReShade, RenoDX, les restes d'un 1-Click DLSS5 ou d'un
  DLSS5-ReShade-AIO.

Couleurs de la liste : vert = DLSS + DX12, jaune = à tester, cyan = sans upscaler (feeder),
bleu = installé, orange = autre mod présent, rouge = anti-cheat, gris = non compatible.

### L'installation

1. Il vérifie que le jeu est fermé, que le payload est complet et que le dossier est accessible en
   écriture. Si le jeu est dans `Program Files`, il propose de se relancer en administrateur.
2. Il **écrit le manifeste avant chaque copie** (`<dossier de l'exe>\_DLSS5_Launcher\manifest.tsv`).
   Même un plantage au milieu reste réversible.
3. Chaque fichier qu'il remplace est d'abord **sauvegardé**, et la copie est vérifiée par SHA-256.
4. Les fichiers d'un autre mod DLSS/ReShade sont **mis de côté**, jamais supprimés.
5. Les DLL d'autres chargeurs de mods, comme le `version.dll` de Cyber Engine Tweaks, un ASI loader
   ou le `dxgi.dll` de dxvk, ne sont **jamais touchées** : il prend un autre nom de proxy.
6. Chaque fichier copié est revérifié par SHA-256.

### La réinitialisation « universelle »

- Elle défait d'abord l'installation d'un **1-Click DLSS5** d'après son propre manifeste
  (`_DLSS5_Backup\manifest.json`), puis la sienne, dans cet ordre.
- Ensuite, elle balaie le dossier de l'exe, la racine du jeu et tout dossier où traîne un fichier de
  mod. Elle reconnaît un reste de mod à son nom, aux infos de version de la DLL (OptiScaler,
  ReShade, dgVoodoo, fakenvapi) ou à un hash connu.
- **Rien n'est supprimé :** tout part en quarantaine dans le dossier du launcher, avec une liste, et
  le bouton **Annuler le nettoyage** remet l'état d'avant.
- Les logs du mod et du moteur du jeu sont archivés dans `journaux\` avant le nettoyage.
- Si une DLL que le jeu avait peut-être d'origine a dû partir sans copie à remettre, il propose la
  vérification des fichiers Steam.

### Les réglages

- **Un profil par jeu**, gardé automatiquement.
- **Préréglages :** Validé, Performance, Qualité (Preset A), Sécuritaire, Effet poussé, Effet doux,
  FPS max, Fluidité ×2 (génération d'images), AMD Radeon.
- **Tous les réglages utiles** des sections `[DlssNr]` d'`OptiScaler.ini` et `[RenoDX.DLSS5]` de
  `ReShade.ini`, chacun avec une explication au survol :
  - modèle, style, passes, résolution du modèle, intensité, détail, ton, peau, transfert, couleur ;
  - génération d'images XeFG / FSR FG, limite de FPS, Reflex Boost, compression des bords ;
  - étalonnage LUT, protection peau / décor ;
  - réglages propres au feeder (profondeur, chemin, profil de mouvement).
- **Importer depuis le jeu :** relit l'INI après qu'on a ajusté l'effet en direct dans le menu du
  jeu, et garde ces valeurs dans le profil.
- **Stabilité :**
  - « profil sûr Ray Reconstruction » : dans les jeux avec RR, Preset B, modèle ≤ 0,6, 1 passe
    (voir [`../docs/analyse-crashs.md`](../docs/analyse-crashs.md)) ;
  - dump de crash d'OptiScaler ;
  - bouton qui change le délai « GPU bloqué » de Windows (`TdrDelay` de 2 à 8 s, droits admin et
    redémarrage, réversible).

### Le suivi (documentation)

- Le détail du jeu affiche le **coût réel du NR** de la dernière session, lu dans
  `OptiScaler.log`, avec un conseil.
- **Réinitialiser crée automatiquement une fiche de test juste avant le reset**. Une fiche faite
  après un reset lit le dernier journal archivé du jeu. On peut donc réinitialiser dès qu'un problème
  arrive, sans perdre les informations.
- La **fiche de test** est un fichier Markdown qui contient :
  - le jeu, l'API, la méthode, la carte graphique et la DLL NR utilisée ;
  - le hash des DLL installées et les réglages actifs ;
  - le coût mesuré et un extrait du log ;
  - les erreurs du log du moteur (Unreal / Unity) ;
  - un **diagnostic automatique** : crash GPU (`DEVICE_HUNG`), exception d'Unreal, RR active,
    modèle reconstruit à répétition, profondeur jamais trouvée, port AMD sur le bon modèle…

### Cartes graphiques

Le launcher lit le nom de la carte et choisit la DLL NR : RTX 20/30/40, RTX 50 ou AMD / Intel (port
« vendor-neutral »). Détails dans [`../docs/cartes-graphiques.md`](../docs/cartes-graphiques.md).

## Ce qu'il faut à côté

L'exe attend un dossier `payload\` à côté de lui. Rien de ce dossier n'est dans ce dépôt.

```
DLSS5-Launcher.exe
payload\
  optiscaler-f5\    contenu du zip OptiScaler-DLSSNR-F5-0.1.32 (OptiScaler.dll, OptiScaler.ini,
                    nvngx.dll_dlssnr.dll, dossier OptiScaler\, licences)
  nr\               nvngx_dlssnr.dll pour RTX 20/30/40 (build openNR compat-fp16 310.9.16)
  nr-origine\       nvngx_dlssnr.dll 310.9.16 de NVIDIA, non modifiée (RTX 50, AMD / Intel)
  amd\              contenu de Optional-Vendor-Neutral-DLSS5-Forwarder.zip (nvngx.dll_dlssnr.dll, nr_port.ini)
  renodx\           dxgi.dll (ReShade avec addons), renodx-dlss5.addon64, ReShade.ini
  feeder-reshade\   dlss5-feed.addon64, dlss5-feed.cfg, shaders\, textures\, nvngx_dlss.dll
```

| Composant | Où l'obtenir |
|---|---|
| OptiScaler F5 et le port vendor-neutral | Releases de [janblade/OptiScaler-F5-DLSSNR-Multipass](https://github.com/janblade/OptiScaler-F5-DLSSNR-Multipass) (version 0.1.32 ici) |
| `nvngx_dlssnr.dll` 310.9.16 | Fichier de NVIDIA, sous licence NVIDIA : il n'est pas redistribué ici. Source utilisée : inconnue (temporaire) dans cette branche, voir `ai/kai/dlss5`. |
| Build compat-fp16 (RTX 20/30/40) | Reconstruite localement avec [clshortfuse/openNR](https://github.com/clshortfuse/openNR) à partir de la DLL 310.9.16 : voir `ai/kai/dlss5` |
| ReShade, RenoDX, DLSS5-Feeder, LumeniteFX | Repris du payload de [1-Click DLSS5](https://github.com/reiluisii/1-Click-DLSS5) v3.0.2 ; chaque projet a sa propre page (voir Crédits) |

SHA-256 des fichiers utilisés, pour vérifier qu'on a les mêmes :

| Fichier | SHA-256 |
|---|---|
| `OptiScaler.dll` (F5 0.1.32) | `14017C79B30814E7EA3297291331A3A3AE5B2E6C4B9F4DD5AA7C4599BF061C2D` |
| `nvngx.dll_dlssnr.dll` (forwarder NVIDIA, F5 0.1.32) | `6198502F1B71D861964F41FC7FA8DE0268FBF994D439C9C6DC9005514B605A7F` |
| `nvngx.dll_dlssnr.dll` (port vendor-neutral, 0.1.32) | `30AEFC7AA19399C2905BFA64F69CCA7EB1574135425D905C0E2F052FC523099C` |
| `Optional-Vendor-Neutral-DLSS5-Forwarder.zip` (0.1.32) | `74A24EB103EF05A54EDE6598D70F4C7093A2C683F59C40972D7A635E36CE8AE9` |
| `nvngx_dlssnr.dll` openNR compat-fp16 310.9.16 | `CDFA6C1EC784867182164C7836F324F087B5C9F374D0C9FE46F54999FEABF3ED` |
| `nvngx_dlssnr.dll` 310.9.16 NVIDIA d'origine | `2BC19BC09A7B89DD44F73E2AC0108FCC586E2154BDD0830F242F4F040E28789F` |
| `dxgi.dll` (ReShade 6.8.0 avec addons, payload 1-Click) | `0CEE63F9C9F13F3AC909C5B4903F4DBB4B719A7AB3B4F13B0DEAF83C814B94F7` |
| `renodx-dlss5.addon64` | `D5ADF82EB44B065F4C590AC91FE824BAB07AFEA0EB9F994BDE936710C8593952` |
| `dlss5-feed.addon64` | `066EEC8C797DF2D656F2AB2324278921B1DD6E9116C9945294F4A00F7FEC608A` |

## Utilisation

1. Double-cliquer sur `DLSS5-Launcher.exe`. L'exe n'est pas signé, donc Windows SmartScreen avertit
   la première fois : « Informations complémentaires », puis « Exécuter quand même ».
2. Attendre la fin du scan, choisir un jeu, laisser la méthode sur **Auto** et cliquer
   **⚡ Installer DLSS 5**.
3. En jeu :
   - jeux avec upscaler : activer DLSS, FSR ou XeSS dans les options, puis touche **Inser** →
     Neural Rendering ;
   - jeux sans upscaler : entrer en partie, puis **Inser** → Optical F5Low.
4. Après la partie : **📝 Fiche de test**.
5. Pour revenir en arrière : fermer le jeu, puis **↺ Réinitialiser le jeu**.

Astuce : lancer chaque jeu une fois **sans mod** avant le scan. Son log existe alors, et le launcher
connaît son API réelle.

## Comment le code est organisé

Le code source est dans [`src/`](src/). Il est sous licence MIT.

| Fichier | Rôle |
|---|---|
| `Core.cs` | Chemins, journal, hash, lecture/écriture d'INI par section, lecture d'en-tête PE, scan des bibliothèques, analyse d'un jeu, logs des moteurs (Unity / Unreal), détection de la carte graphique |
| `Install.cs` | Réglages et préréglages, génération des INI, manifeste, installation, réinitialisation, garde-fous de stabilité, mesure du coût NR, diagnostic, fiche de test, copie pour un ami |
| `Cleaner.cs` | Réinitialisation universelle : manifeste du 1-Click, reconnaissance des restes de mods, quarantaine et annulation |
| `Ui.cs` | Fenêtre (thème sombre), onglets Jeu / Réglages NR / Journal, profils par jeu, molette qui ne change jamais une valeur par accident |
| `COMPILER.bat` | Recompile l'exe avec le compilateur C# inclus dans Windows (`csc.exe` de .NET Framework 4) |

### Compiler

- **Sous Windows :** double-cliquer sur `src\COMPILER.bat`. L'exe est créé un dossier au-dessus.
- **Sous Linux (avec Mono) :** compiler contre les **assemblies de référence de .NET Framework 4.8**,
  pas contre celles de Mono (voir [`../docs/lecons-apprises.md`](../docs/lecons-apprises.md),
  leçon 1) :

  ```sh
  A=/usr/lib/mono/4.8-api
  mcs -langversion:5 -nostdlib -noconfig -target:winexe -optimize+ -win32icon:src/icon.ico \
      -r:$A/mscorlib.dll -r:$A/System.dll -r:$A/System.Core.dll -r:$A/System.Windows.Forms.dll \
      -r:$A/System.Drawing.dll -r:$A/System.Web.Extensions.dll -out:DLSS5-Launcher.exe src/*.cs
  ```

### Tests

[`tests/Test.cs`](tests/Test.cs) contient **110 vérifications automatiques**. Elles tournent sur de
faux jeux, avec un faux payload : installation, réglages, réinitialisation identique au hash près,
nettoyage d'un 1-Click et d'une installation manuelle, détection Unity / Unreal, mode AMD, RTX 50,
diagnostic de crash, fiche de test faite après un reset. Lancement (Mono), depuis un dossier qui contient un `payload\` factice :

```sh
mcs -langversion:5 -out:test.exe tests/Test.cs src/Core.cs src/Install.cs src/Cleaner.cs   # + les -r: ci-dessus
mono test.exe <un exe 64-bit quelconque> <un dxgi.dll de ReShade> <dossier de travail vide>
```

Ces tests ne remplacent pas Windows ni un vrai GPU : ce qui a été vérifié en jeu est dans
[`../docs/journal-tests-2026-10-24.md`](../docs/journal-tests-2026-10-24.md).

## Limites connues

- Le statut « Compatible » est une **déduction** à partir des fichiers du jeu. La preuve reste le
  statut NR dans le menu du jeu, ou la fiche de test.
- Xbox / Game Pass n'est pas scanné (dossiers `WindowsApps` verrouillés).
- Jeux 32-bit et DX9 non pris en charge.
- Le nettoyage reconnaît ReShade, mais pas *pourquoi* il est là. Un ReShade installé pour autre chose
  (presets de couleur) part aussi en quarantaine. Il est listé avant confirmation, et récupérable.
- RTX 50 et AMD : pas encore testés sur du vrai matériel.

## Crédits

Le launcher ne contient le code d'aucun de ces projets. Il copie leurs fichiers depuis le `payload\`
de l'utilisateur, ou lit leurs fichiers de configuration. Chacun garde sa licence.

| Projet | Auteur (nom public ou pseudo) | Licence |
|---|---|---|
| DLSS, NGX, DLSS Neural Rendering (`nvngx_dlssnr.dll`), Streamline, `nvngx_dlss.dll` | NVIDIA | Licences NVIDIA (fichiers non redistribués ici) |
| [OptiScaler](https://github.com/optiscaler/OptiScaler) | cdozdil et les contributeurs du projet (à partir de CyberFSR2 de PotatoOfDoom) | GPL-3.0 |
| [OptiScaler_DLSSNR](https://github.com/Dagherbou/OptiScaler_DLSSNR) | Dagherbou | voir le projet |
| OptiScaler-DLSSNR-PreSR-Multipass | wilsjo2 | voir le projet |
| [OptiScaler-F5-DLSSNR-Multipass](https://github.com/janblade/OptiScaler-F5-DLSSNR-Multipass) (dont Optical F5Low et le port vendor-neutral) | janblade | GPL-3.0 (fichier LICENSE du package) |
| [openNR](https://github.com/clshortfuse/openNR) (build compat-fp16) et [RenoDX](https://github.com/clshortfuse/renodx) | clshortfuse (ShortFuse) | openNR : MIT ; RenoDX : voir le projet |
| [ReShade](https://github.com/crosire/reshade) | crosire | BSD-3-Clause |
| [DLSS5-Feeder](https://github.com/jlrouzies-fr/DLSS5-Feeder) | jlrouzies-fr (le README du 1-Click nomme aussi NIGos) | MIT |
| [LumeniteFX](https://github.com/umar-afzaal/LumeniteFX) (shaders du Feeder ReShade) | umar-afzaal | Tous droits réservés : ses fichiers ne sont pas publiés ici |
| [1-Click DLSS5](https://github.com/reiluisii/1-Click-DLSS5) | reiluisii et « 1 Click DLSS 5 Contributors » | MIT. Les modèles `ReShade.ini` et `ReShadePreset.ini` du Feeder ReShade sont adaptés de sa route « Feeder » (crédit dans `src/Install.cs`). |
| [DLSS5-ReShade-AIO](https://github.com/kibblerz/DLSS5-Reshade-AIO) | kibblerz | voir le projet. Le launcher sait seulement reconnaître et nettoyer ses fichiers. |
| [Mono](https://www.mono-project.com/) | projet Mono | Utilisé seulement pour compiler et tester sous Linux |

Les noms de jeux et de moteurs (Unreal Engine, Unity, Cyberpunk 2077, etc.) appartiennent à leurs
propriétaires. Ce projet n'est affilié à aucun d'eux.

Idée, essais en jeu et décisions : On-100-K-Lisse. Code et documentation : Claude.
