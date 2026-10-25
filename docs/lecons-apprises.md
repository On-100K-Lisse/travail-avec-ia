# Leçons apprises

**But :** noter les erreurs faites en construisant le launcher, comment elles ont été trouvées, et
ce qu'elles apprennent. La plupart ont été découvertes parce qu'On-100-K-Lisse a testé chaque
version sur son PC et a rapporté exactement ce qu'il voyait.

## 1. « Ça compile » ne veut pas dire « ça tourne sous Windows »

Le launcher est compilé sous Linux avec Mono. La première version appelait `String.TrimEnd(char)`
et `String.Split(char, …)`. Ces deux fonctions existent dans la bibliothèque de Mono, mais **pas**
dans le .NET Framework 4.8 de Windows.

- **Résultat :** le scan plantait dès le départ. Le message `Méthode introuvable` était dans le
  journal du launcher, mais l'interface affichait quand même 134 jeux « Compatible », parce que
  c'était la valeur par défaut.
- **Correction :** compiler contre les *assemblies de référence* de .NET Framework 4.8
  (`/usr/lib/mono/4.8-api`). Avec elles, toute fonction absente de Windows devient une erreur de
  compilation.
- **Leçon :** ne jamais laisser un état « non analysé » ressembler à un résultat. Le statut par
  défaut est maintenant « Analyse échouée ».

## 2. Les imports d'un exe mentent sur l'API graphique

Pour savoir si un jeu tourne en DirectX ou en OpenGL, la première idée était de lire les DLL qu'il
importe. Or :

- `UnityPlayer.dll` importe `OPENGL32.dll` et **aucune** DLL DirectX, alors que les jeux testés
  tournaient en DirectX 11. Unity charge DirectX à la volée.
- Unreal fait pareil avec DirectX 12.

Le launcher a donc installé ReShade sous le nom `opengl32.dll` dans deux jeux DX11, et ReShade ne
voyait aucune image. La preuve était dans `ReShade.log` (chargé comme `OPENGL32.dll`, aucune
chaîne d'affichage vue), puis dans les imports réels de `UnityPlayer.dll`, vérifiés sur les
fichiers du jeu.

- **Correction :** lire d'abord ce que le moteur a réellement utilisé à la dernière partie :
  - `Player.log` de Unity (`Direct3D: Version: Direct3D 11.0`) ;
  - le log d'Unreal (`Using Default RHI: D3D12`) ;
  - sinon, appliquer des règles par moteur.
- **Leçon :** les labels mentent, il faut regarder ce qui s'est vraiment exécuté. C'est le même
  principe que « les hashs ne mentent pas » du document de méthode sur `main`.

## 3. Le menu du jeu peut défaire ce que le launcher a écrit

OptiScaler a son propre menu (touche Inser), qui peut réenregistrer `OptiScaler.ini`. Dans un essai,
la résolution du modèle posée par le launcher (0,67) est revenue à `auto` (1,0). Le coût du NR a
alors presque doublé.

- **Correction :** afficher dans le launcher le coût **mesuré** (lu dans le log) plutôt que le
  réglage prévu. Le bouton « Importer depuis le jeu » garde les changements faits en jeu au lieu de
  les perdre à la réinstallation.

## 4. Supprimer, c'est irréversible ; déplacer, non

La réinitialisation « universelle » doit retirer des fichiers qu'elle n'a pas installés elle-même.
Elle ne peut jamais être sûre à 100 % qu'un fichier est un reste de mod. Une DLL `dxgi.dll` peut
par exemple appartenir à dxvk.

- **Choix :**
  - ne rien supprimer : tout part en quarantaine avec une liste, et un bouton remet l'état d'avant ;
  - n'identifier un fichier comme mod que par des indices forts (nom qu'aucun jeu ne livre, infos
    de version de la DLL, hash connu) ;
  - épargner explicitement les chargeurs d'autres mods.

## 5. Un réglage de l'interface peut changer une valeur sans qu'on le veuille

Sous Windows, une liste déroulante ou une case numérique « attrape » la molette dès que la souris
passe dessus. Elle change alors sa valeur pendant qu'on fait défiler la page (rapporté par
On-100-K-Lisse).

- **Correction :** dans le launcher, la molette ne change jamais une valeur. Elle fait défiler la
  page.
- **Limite :** l'environnement de test sous Linux (Mono) ne reproduit pas le défilement de ces pages.
  Le fait que la page défile bien sous Windows n'a donc pas pu être vérifié ici : inconnu
  (temporaire).

## 6. Un crash n'est pas une cause

Trois jeux ont planté le même jour. Deux crashs avaient un point commun clair, le troisième non.
Plutôt que de conclure trop vite, chaque crash a été lu séparément (log, puis dump), et la
documentation sépare ce qui est **établi** de ce qui est **supposé**. Voir
[`analyse-crashs.md`](analyse-crashs.md).

- **Leçon pratique :** activer le dump de crash avant d'en avoir besoin. C'est lui qui a permis de
  voir que le crash de Species: Unknown venait d'Unreal lui-même et non d'OptiScaler.

## 7. L'outil doit suivre la façon de travailler de son utilisateur

Le premier modèle supposait qu'on fait la fiche de test, puis qu'on réinitialise. En pratique,
On-100-K-Lisse réinitialise dès que quelque chose cloche, et en parle ensuite. La fiche faite après
le reset était vide. Plutôt que de demander de changer d'habitude, le launcher fait maintenant la
fiche tout seul avant le reset, et sait relire les journaux archivés après.

## 8. Les tests automatiques ne remplacent pas le jeu

Le launcher a 110 vérifications automatiques sur de faux jeux. Elles ont attrapé de vraies
erreurs : ordre de restauration, fichiers oubliés, mauvais réglages d'INI. Mais aucun de ces tests
n'aurait trouvé les leçons 1, 2, 3 et 5 : il a fallu Windows, un vrai jeu et un humain devant
l'écran.
