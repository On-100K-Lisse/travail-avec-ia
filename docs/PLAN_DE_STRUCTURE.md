# Notre scaler — plan de structure (fait de zéro)

30 octobre 2026 · Plan de **Claude**, écrit pour être remis à ChatGPT. Déposé dans cette branche par Kai le 30 octobre 2026, à la demande d'On-100-K-Lisse.

> **Corrections appliquées par Kai avant le dépôt** (à la demande d'On-100-K-Lisse, chacune marquée dans le texte) :
> 1. La boucle de compilation peut rouler par ChatGPT dans le dossier du projet — seul le test en jeu appartient à Alexandre (section « Outils »).
> 2. Species: Unknown : ce qui est fragile chez les outils actuels, c'est la recherche de profondeur, pas l'ensemble (section « Comment marche un scaler ») — et le jalon 7 est le plus dur du projet (note au jalon 7).
> 3. Le jalon 3 (menu) est déplaçable : il ne bloque pas le jalon 4 (note aux jalons).
> 4. Jalon 8 : ajout de la question de l'observation en boîte noire aux options à qualifier (section « Risques »).
> Alignement factuel (pas une correction du fond) : l'hébergement du code est tranché — cette branche (section « Outils »).

Ce scaler appartient à On-100-K-Lisse : il est écrit à partir de zéro, sans une ligne copiée d'OptiScaler, pour que son auteur en choisisse librement la licence. Ce plan a été écrit à partir de la documentation publique des API et des SDK, pas du code source d'OptiScaler.

## Le but et la règle d'or

**Le but.** Un programme qui se glisse dans un jeu, récupère ce dont un upscaler a besoin (image, profondeur, mouvement) et le donne à l'upscaler de notre choix, puis, plus tard, au Neural Rendering. Le premier jeu cible est déjà connu : Species: Unknown (Unreal 5, DX12, sans upscaler).

**À qui il appartient.** Le code écrit pour ce scaler appartient à On-100-K-Lisse, qui choisit sa licence (MIT comme le launcher, ou une autre). Les bibliothèques tierces utilisées restent à leurs auteurs, sous leurs propres licences (section « Briques tierces »).

**La règle d'or : on comprend, on ne copie pas.** OptiScaler est sous GPL v3. Une seule fonction copiée ou adaptée de son code obligerait tout le scaler à passer en GPL v3. Pour l'éviter :

1. Sources permises : la documentation de Microsoft (Direct3D 11 et 12, DXGI, Vulkan), les SDK publics des upscalers (NVIDIA, AMD, Intel), les livres et articles techniques, et le comportement observé en jeu.
2. Interdit : copier, traduire, réécrire ligne par ligne ou adapter du code d'OptiScaler, ou de n'importe quel projet sous GPL. Lire ses notes de version ou son README pour savoir ce qu'il fait est correct ; s'inspirer de comment il est écrit ne l'est pas.
3. Journal des sources : chaque fichier commence par son auteur, sa date et sa licence. Un fichier SOURCES.md liste les documents consultés pour chaque module. Si on doit un jour prouver que le code est le nôtre, ce journal et l'historique Git le montrent.
4. Séparation des équipes : Claude travaille aussi sur le fork d'OptiScaler F5. Quand Claude rejoindra le scaler, il écrira à partir de la documentation publique et de ce plan seulement, jamais à partir du code du fork. Une revue par une autre IA (ChatGPT ou Kai) vérifie que rien n'a glissé.

## Comment marche un scaler

Un upscaler ne travaille jamais seul : le jeu lui fournit, à chaque image, des ingrédients précis, et un scaler n'est qu'un intermédiaire qui attrape ces ingrédients au bon moment.

Ce que le jeu fait à chaque image (avec un upscaler) :

1. Il calcule la scène en plus petit que l'écran, par exemple à 67 % de la résolution.
2. Il décale la caméra d'une fraction de pixel, différente à chaque image (le jitter). C'est ce qui permet de récupérer du détail en combinant plusieurs images.
3. Il prépare les ingrédients : l'image couleur, la profondeur (distance de chaque pixel), les vecteurs de mouvement (où chaque pixel était à l'image d'avant), l'exposition, et parfois des masques (transparences, particules).
4. Il appelle l'upscaler, qui combine l'image actuelle avec l'historique pour sortir une image pleine résolution.
5. Il dessine le HUD et les menus par-dessus, puis envoie l'image à l'écran (Present).

Les trois endroits où un scaler peut s'accrocher :

| Point d'accroche | Ce qu'on reçoit | Pour quels jeux | Difficulté |
|---|---|---|---|
| L'appel à l'upscaler du jeu (DLSS, FSR ou XeSS) | Tous les ingrédients, propres, au bon moment | Jeux avec un upscaler | Moyenne : il faut parler la langue de chaque upscaler |
| L'envoi à l'écran (Present) | Seulement l'image finale, HUD compris | Tous les jeux, dont ceux sans upscaler | Haute : profondeur et mouvement sont à fabriquer ou à chercher |
| Le Neural Rendering, avant ou après l'upscaler | L'image, plus ce que le modèle demande | Après les deux autres | Très haute : la DLL de NVIDIA n'est pas documentée publiquement |

**Pourquoi les jeux sans upscaler sont durs.** Sans appel à un upscaler, il n'y a ni vecteurs de mouvement ni profondeur fournis. Il faut estimer le mouvement en comparant deux images (le flux optique) et deviner quelle texture du jeu contient la profondeur. C'est le point fragile des outils actuels dans Species: Unknown *(correction de Kai, 30 oct. 2026 : le Neural Rendering d'OptiScaler F5 y fonctionne — mesuré à environ 3,9 ms en preset B — mais sa recherche de profondeur est son maillon faible ; c'est elle que le travail sur le fork va étudier en premier)*, et c'est là qu'un scaler à nous pourrait se démarquer : pas en faisant exister la fonction, mais en la rendant plus fiable.

## Architecture en modules

Le scaler est une seule DLL faite de six modules séparés, chacun testable seul. Quatre forment le chemin de l'image (1 à 4), deux servent autour (5 et 6).

Le jeu charge notre DLL sans le savoir ; elle intercepte ses appels, rassemble les ingrédients et les donne à l'upscaler choisi, puis l'image part à l'écran. Le menu modifie les réglages, et le journal permet au launcher de suivre chaque partie.

*(Note de Kai au dépôt : le schéma détaillé des six modules ne faisait pas partie du texte transmis ; il reste dans l'artefact original de Claude.)*

## Étapes de fabrication

On construit par petits jalons : chacun ajoute une seule chose, se teste en quelques minutes et laisse le jeu intact s'il échoue. On ne passe au suivant que quand le précédent est réussi.

| Jalon | Ce qu'on construit | Ce qu'Alexandre teste | Réussi quand |
|---|---|---|---|
| 0. L'atelier | Le projet vide qui compile : Visual Studio, CMake, Git, la licence choisie | Compiler sur son PC | Une DLL sort de la compilation sans erreur |
| 1. Se faire charger | Une dxgi.dll qui transmet tout à la vraie dxgi.dll de Windows et écrit une ligne dans son journal | La mettre dans Species: Unknown, jouer 2 minutes | Le jeu tourne comme avant et le journal dit « chargé » |
| 2. Voir chaque image | L'interception de Present : on compte les images | Jouer 5 minutes | Le journal donne un FPS qui colle à celui du jeu |
| 3. Le menu en jeu | Un menu par-dessus le jeu, sur une touche configurable | Ouvrir et fermer le menu en jouant | Le menu s'affiche, le jeu ne ralentit pas |
| 4. Écouter l'upscaler | Dans un jeu avec DLSS ou FSR, on note ses appels sans rien changer : résolution d'entrée et de sortie, jitter | Dead as Disco ou Cyberpunk, 5 minutes | Le journal donne la même résolution que le menu du jeu |
| 5. Remplacer l'upscaler | On donne les ingrédients à un autre upscaler (FSR ou XeSS) | Comparer l'image et le FPS, 15 minutes | Image propre, pas de crash, coût mesuré |
| 6. Réglages et journal | Un fichier INI, un journal lisible par le launcher, une méthode « notre scaler » dans le launcher | Installer et réinitialiser avec le launcher | Le jeu revient exactement comme avant ; l'historique note la partie |
| 7. Jeux sans upscaler | Trouver la profondeur du jeu et estimer le mouvement nous-mêmes | Species: Unknown, 30 minutes | La profondeur est trouvée et le mouvement suit la caméra |
| 8. Neural Rendering | Décision d'abord (section « Risques »), puis le branchement du modèle | À définir après la décision | À définir |

Les jalons 1 à 3 sont le squelette de tout scaler et ne demandent aucun SDK d'upscaler. C'est le bon premier objectif pour ChatGPT : s'ils marchent dans Species: Unknown, la base est solide.

*(Correction de Kai, 30 oct. 2026 : le jalon 3, le menu, est un confort de débogage, pas une dépendance du chemin critique. S'il s'étire, il peut être déplacé après le jalon 4 sans rien casser ; il ne doit pas retarder l'écoute des appels de l'upscaler.)*

*(Note de Kai, 30 oct. 2026 : le jalon 7 est le plus dur techniquement du projet — plus que le jalon 8, qui est une décision à prendre. Prévoir qu'il prenne plus de temps que sa place dans le tableau ne le suggère ; durée : inconnu (temporaire).)*

## Briques tierces et leurs licences

Se servir d'une bibliothèque tierce n'est pas copier : elle reste à ses auteurs, on respecte sa licence, et notre code à nous reste le nôtre. On évite tout ce qui est sous GPL. Licences lues le 30 octobre 2026 dans chaque dépôt officiel.

| Brique | À quoi elle sert | Licence | Ce que ça implique |
|---|---|---|---|
| MinHook | Intercepter une fonction (Present, appels de l'upscaler) | BSD 2 clauses | Libre ; garder sa mention de copyright |
| Microsoft Detours | Même rôle, alternative à MinHook | MIT | Libre ; garder sa mention |
| Dear ImGui | Le menu en jeu | MIT | Libre ; garder sa mention |
| NVIDIA Streamline | Comprendre comment les jeux récents appellent DLSS | Type MIT | Libre ; garder sa mention |
| NVIDIA DLSS SDK | La documentation de DLSS (NGX) | NVIDIA RTX SDKs License | Licence propriétaire : à lire en entier avant de s'en servir dans un outil injecté |
| Intel XeSS SDK | Upscaler de remplacement | Intel Simplified Software License | Redistribution des DLL permise sous conditions ; à relire avant de les livrer |
| AMD FidelityFX SDK (2.x, actuel) | FSR comme upscaler de remplacement | Licence AMD : binaires seulement | DLL redistribuables, mais décompilation interdite et code non modifiable |

Les anciennes versions de FSR publiées avec leur code source (FSR 2, FSR 3) avaient une autre licence : inconnu (temporaire), à vérifier avant de les choisir. La DLL NR de NVIDIA n'est dans aucune de ces boîtes : elle n'est livrée qu'avec un jeu (NBA 2K27) et ne sera jamais distribuée avec notre scaler.

## Outils de compilation, de test et de débogage

Tout ce qu'il faut est gratuit, et tout tourne sur le PC d'Alexandre. *(Correction de Kai, 30 oct. 2026 : ChatGPT travaille directement dans le dossier du projet sur ce PC, en mode work, comme Claude Cowork — la boucle de compilation et les tests automatisés roulent donc là. Ce qui reste à Alexandre seul, c'est le test en jeu : jouer, juger l'image et mesurer ; aucune IA ne peut faire ça à sa place.)*

Pour compiler :

- Visual Studio Community (2022 ou plus récent), avec la charge « Développement Desktop en C++ » : elle installe le compilateur, le SDK Windows et CMake.
- Git, pour l'historique : c'est aussi la preuve que le code a été écrit par nous, étape par étape.

Pour voir ce qui se passe dans le jeu :

- Le journal du scaler : la première source d'information, à chaque jalon.
- RenderDoc : capture une image du jeu et montre chaque texture, dont la profondeur et les vecteurs de mouvement. Indispensable pour le jalon 7.
- PIX pour Windows (Microsoft) et NVIDIA Nsight Graphics : même idée, avec les temps GPU détaillés.
- La couche de débogage Direct3D 12 (fonction Windows « Outils graphiques ») : signale les erreurs d'utilisation de l'API au lieu de laisser le jeu planter sans explication.

Pour tester proprement : le même protocole que le launcher. Une étape de test nommée, une seule variable à la fois, la même scène, et le résultat noté. Jamais dans un jeu en ligne protégé par un anti-cheat.

Où vit le code : la branche `ai/chatgpt/scaler` du dépôt travail-avec-ia *(alignement de Kai, 30 oct. 2026 : tranché par On-100-K-Lisse, branche créée le 30 octobre 2026)*. Les règles du dépôt s'appliquent : fichiers de licence présents (règle 1), la branche au tableau de main (règle 9), aucune donnée personnelle (règle 6).

## Risques, limites légales et intégration au launcher

Le plus gros risque n'est pas technique : c'est le Neural Rendering, parce que la seule façon connue de l'appeler passe par une DLL de NVIDIA non documentée. Cette question se tranche avant le jalon 8, pas pendant.

Les risques :

- **Le Neural Rendering (jalon 8).** Apprendre à appeler la DLL NR en lisant le code d'OptiScaler briserait la règle d'or. La décompiler peut aller contre la licence de NVIDIA. Options à décider le moment venu : attendre une documentation officielle de NVIDIA, ou faire un scaler qui excelle sans NR (upscaling, jeux sans upscaler). Décision : On-100-K-Lisse. *(Question ajoutée par Kai, 30 oct. 2026 : une troisième voie est à qualifier au même moment — l'observation en boîte noire. La règle d'or permet déjà « le comportement observé en jeu » comme source ; où finit cette observation et où commence l'ingénierie inverse visée par la licence de NVIDIA reste à définir avant de s'en servir.)*
- **Les anti-cheats.** Une DLL injectée dans un jeu en ligne protégé peut faire bannir le compte. Le launcher bloque déjà ces jeux ; le scaler devra le respecter aussi.
- **Faire planter le jeu.** Chaque interception doit pouvoir s'éteindre seule : si quelque chose échoue, le scaler laisse passer l'image d'origine, écrit l'erreur dans son journal et ne touche plus à rien.
- **Les noms et marques.** Pas de « DLSS » dans le nom du scaler (marque de NVIDIA), et aucune présentation comme un produit officiel.
- **La durée.** Combien de temps prendra chaque jalon : inconnu (temporaire). On le saura après les jalons 1 à 3.

L'intégration au launcher (jalon 6) :

1. Une nouvelle méthode « notre scaler », à côté d'OptiScaler F5 et du Feeder.
2. Le manifeste et la réinitialisation, comme pour les autres méthodes : le jeu revient au fichier près.
3. Le docteur connaît les empreintes SHA-256 de chaque version du scaler.
4. Le journal du scaler est lu par l'historique, la fiche de test et les Conseils.
