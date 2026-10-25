# Quelle DLL NR selon la carte graphique

**But :** expliquer pourquoi le launcher n'installe pas le même fichier `nvngx_dlssnr.dll` sur
toutes les cartes, et ce qui est vérifié ou non pour chacune.

## Le problème de départ

La DLL de Neural Rendering de NVIDIA (`nvngx_dlssnr.dll`) contient du code GPU compilé (des
« noyaux » CUDA) pour certaines générations de cartes seulement. La version 310.9.16 d'origine ne
démarrait pas sur la RTX 4070 : `FeatureNotSupported` (`0xBAD00001`) aux tests 01 et 02 du
19 octobre. La build reconstruite avec **openNR** ajoute les noyaux RTX 20/30/40 et garde ceux des
RTX 50. Elle a fonctionné au test 03. Tout ce travail est documenté par Kai dans `ai/kai/dlss5`.

## Le choix du launcher

| Carte | DLL installée | Forwarder `nvngx.dll_dlssnr.dll` | Vérifié en jeu ? |
|---|---|---|---|
| RTX 20 / 30 / 40 | openNR compat-fp16 310.9.16 (`CDFA6C1E…ABF3ED`) | Celui d'OptiScaler F5 (passe par NVIDIA NGX) | Oui, sur RTX 4070 seulement |
| **RTX 50** | 310.9.16 **d'origine**, signée par NVIDIA (`2BC19BC0…28789F`) | Celui d'OptiScaler F5 | **Non** : inconnu (temporaire) |
| **AMD / Intel** | 310.9.16 d'origine | **Port vendor-neutral** d'OptiScaler F5 0.1.32 | **Non** : inconnu (temporaire) |

Le launcher lit le nom de la carte dans le registre de Windows. La série RTX est tirée du numéro de
modèle : 5070 donne la série 50. Une ligne `dllNr=openNR` ou `dllNr=origine` dans
`launcher-config.ini` force l'une ou l'autre DLL. Une ligne `forcerGpu=AMD` permet de tester le mode
AMD sur une carte NVIDIA.

### Pourquoi la DLL d'origine pour les RTX 50

- La 310.9.16 d'origine contient le code natif des RTX 50 (architecture Blackwell). Elle a échoué
  sur la 4070 justement parce que ce code-là ne s'y applique pas.
- Le guide d'installation d'OptiScaler F5 (`INSTALL-DLSSNR.md`) recommande la DLL NVIDIA d'origine
  pour les RTX 50.
- Les notes de version d'OptiScaler F5 citent des mesures faites sur une RTX 5070 Ti.
- La build openNR garde aussi les noyaux RTX 50. Elle sert donc de solution de repli
  (`dllNr=openNR`).

### Le mode AMD / Intel

Les cartes AMD et Intel n'ont pas le cœur NGX de NVIDIA, donc la voie normale ne peut pas
fonctionner. Le projet OptiScaler F5 publie à part un **port « vendor-neutral »** : un
`nvngx.dll_dlssnr.dll` qui exécute lui-même le modèle en DirectX 12 et en DirectML, en lisant les
poids du modèle dans la DLL 310.9 de NVIDIA.

- **Fichier :** `Optional-Vendor-Neutral-DLSS5-Forwarder.zip`, release v0.1.32 du projet
  [janblade/OptiScaler-F5-DLSSNR-Multipass](https://github.com/janblade/OptiScaler-F5-DLSSNR-Multipass).
  Son SHA-256 (`74A24EB1…8AE9`) est identique à celui publié par GitHub. Les fichiers du zip
  correspondent à son `SHA256SUMS.txt`. La release v0.1.33 dit que ce fichier n'a pas changé.
- **Le port exige la 310.9** pour son petit modèle (« network: small » dans `nr_port.log`). Le
  launcher installe donc la 310.9.16 d'origine.
- **Upscaler :** sur AMD, OptiScaler ne peut pas utiliser DLSS. Le launcher règle :
  - `Dx12Upscaler=auto`, qui choisit FSR 4 quand la carte le permet ;
  - `Dx11Upscaler=ffx_12` et `VulkanUpscaler=ffx_12`, parce que le NR ne tourne en DX11 qu'avec un
    backend « _12 », d'après le fichier `TROUBLESHOOTING.md` d'OptiScaler F5.

  Le spoofing NVIDIA d'OptiScaler (`Dxgi=auto`) s'active tout seul sur AMD.
- **Méthodes refusées sur AMD :** RenoDX et le Feeder ReShade, qui passent par NGX.
- **Performance attendue :**
  - Sur une RX 6700 XT, le développeur du port rapporte ~59 ms par image à 50 % de résolution, ce
    qui est injouable (notes de la v0.1.16).
  - Pour la RX 9070 XT, il avance une estimation théorique de 3,7 à 10 fois mieux, mais il précise
    que personne ne l'a mesurée.
  - Résultat réel : inconnu (temporaire).

## Ce qui reste à faire

Les deux configurations non vérifiées (RTX 5070 et RX 9070 XT) sont confiées à deux testeurs. La
fiche de test du launcher dira, pour chacune :
- quelle DLL a été installée ;
- si le NR a démarré ;
- sur AMD, si le port tournait bien sur le petit modèle.
