# Passation du scaler maison — de ChatGPT à Claude

**Rédigé par Kai, le 1er novembre 2026**, à la demande d'On-100-K-Lisse, à partir de la lecture intégrale de la conversation partagée ChatGPT « Préparer le projet Scaler » (session du 31 octobre 2026, ~21 h → minuit, heure de Toronto).

**Pourquoi cette passation :** On-100-K-Lisse a épuisé ses crédits d'utilisation ChatGPT. Claude reprend le rôle de bâtisseur du code du scaler. On-100-K-Lisse reste, comme toujours, le décideur et le seul validateur en jeu.

**Limites de lecture (à connaître avant de te fier à ce document) :** dans la page partagée, les blocs de modifications de code sont partiellement repliés — seuls les 3 premiers fichiers de chaque édition sont nommés, le reste est masqué (« Show N more files »). La liste complète des fichiers et le contenu des diffs ne sont donc pas visibles ici ; seuls les fichiers explicitement nommés dans la conversation sont cités. Plusieurs dossiers d'étape apparaissent sous forme de libellés sans chemin complet (« jalon-4 », « A-jeu », « B-scaler », « F5-SR », « Notre-scaler »). **Le code source lui-même est sur le PC d'On-100-K-Lisse** (voir §3) : c'est lui ta source de vérité, pas ce résumé.

---

## 1. Le projet en bref

- **Mission :** construire de zéro un scaler graphique propre à On-100-K-Lisse — une DLL `dxgi.dll` proxy pour jeux DirectX 12, d'abord sur sa RTX 4070 — qui intercepte les appels de l'upscaler du jeu et, à terme, gère/produit l'upscaling.
- **Licence :** MIT (licence propre d'On-100-K-Lisse, comme le launcher). **Pas de GPL.**
- **Clean-room TOTAL :** aucun code copié, adapté ou traduit d'OptiScaler (y compris le fork F5) ni de RenoDX. Lecture pour comprendre les concepts seulement ; implémentation depuis la documentation officielle (Microsoft, SDK Windows, doc NVIDIA/NGX) et les mesures d'On-100-K-Lisse. ChatGPT a confirmé avoir écrit le code de zéro à partir de la documentation Microsoft et du SDK Windows, sans rien copier du launcher.
- **Documents de référence (branche GitHub `ai/chatgpt/scaler` du dépôt `On-100K-Lisse/travail-avec-ia`) :** `docs/PERIMETRE_V1.md` (le cadre — il GAGNE en cas de divergence avec le plan) et `docs/PLAN_DE_STRUCTURE.md` (ton plan de structure, corrigé par Kai avant dépôt). Les 9 règles du README de `main` s'appliquent (dont : un seul écrivain à la fois, SOURCES.md par module, inconnu = « inconnu (temporaire) », aucune donnée nominative/sensible).
- **Design fail-open :** en cas d'échec, laisser passer l'image du jeu, journaliser, se désactiver — jamais de crash ni d'écran noir imputable au scaler.
- **Jeux cibles v1 :** Dead as Disco (principal, validé) et Species: Unknown (pas encore testé). Solo seulement, aucun jeu avec anti-cheat.
- **Le nom du projet ne doit pas contenir « DLSS »** (marque NVIDIA) — nom définitif toujours pas choisi.

## 2. Décisions d'On-100-K-Lisse consignées pendant la session

1. **Premier jeu de test : Dead as Disco** (choisi contre Species: Unknown au départ).
2. **Direction v1 : DLSS SR via NGX.** ChatGPT avait relevé une divergence entre le plan (jalon 5 = remplacer DLSS par FSR/XeSS) et le périmètre v1 (DLSS SR via NGX prioritaire). On-100-K-Lisse a tranché : **on reste sur DLSS SR via NGX pour la v1 ; FSR/XeSS sont écartés pour l'instant.** C'est le « DLSS qu'on utilise actuellement » qui sert de chemin de test.
3. **Menu en jeu : reporté** à plus tard (le jalon « menu ImGui » du plan n'a pas été fait ; la numérotation des jalons dans la conversation saute donc du jalon 2 au jalon 4).
4. **Neural Rendering (NR) : réservé pour la fin**, intégration « à qualifier, non acquise » — décision du jalon 8 du plan, inchangée.
5. ChatGPT travaillait **seul dans le dossier de travail sur le Bureau** d'On-100-K-Lisse ; On-100-K-Lisse faisait les installations dans le jeu et les validations en jeu.

## 3. Environnement de travail et structure connue

- **Dossier de travail (sur le Bureau d'On-100-K-Lisse) :** libellé « travaille ia scaler » ; le projet est dans le sous-dossier **`scaler`** (chemin affiché : `travaille ia scaler\scaler`).
- **DLL compilée :** `scaler\build\Release\dxgi.dll` (builds Release et Debug à chaque version).
- **Git :** dépôt **local** dans le dossier du projet, commits à chaque étape. (Point ouvert : le code n'a pas été poussé sur la branche GitHub `ai/chatgpt/scaler` pendant la session — à confirmer avec On-100-K-Lisse.)
- **Fichiers nommés explicitement dans la conversation :**
  - `scaler/.gitignore`, `scaler/CMakeLists.txt`, `scaler/src/dxgi.def`
  - `scaler/README.md` (suivi des jalons tenu à jour), `scaler/docs/DEMARRAGE.md`, `scaler/docs/JOURNAL.md`
  - `scaler/src/frame_counter.hpp`, `scaler/src/log.cpp`, `scaler/src/dxgi_layout.cpp`
  - `scaler/ScalerMaison.ini` (fichier de configuration, apparu dès la DLL 0.3.0)
  - Journal d'exécution en jeu : `ScalerMaison.log`
- **Installation en jeu (manuelle, par On-100-K-Lisse) :** jeu fermé, copier `dxgi.dll` (et `ScalerMaison.ini` quand requis) dans le dossier Win64 du jeu, lancer, jouer le protocole demandé, fermer.
- **Dossiers d'étape (libellés seulement, chemins complets à récupérer sur place) :** `jalon-4`, `jalon-5a`, `jalon-5b`, `A-jeu` (paramètres du jeu), `B-scaler` (paramètres du scaler), `F5-SR` (paquet F5 en SR seul), `Notre-scaler` (build courant du scaler pour la comparaison F5).
- **Versions livrées :** DLL 0.3.0 (jalon 4), 0.4.0 (5a), 0.5.0 (5b), **0.6.0 (5c, version courante)**. Tests unitaires annoncés à chaque version, **tous passants en Release et Debug** : 3 → 7 → 11 → 13 → 16 → **20 tests**.

## 4. Chronologie détaillée des jalons (tous validés en jeu par On-100-K-Lisse, dans Dead as Disco)

### Préparation — DLL minimale
Première `dxgi.dll` compilée avec journalisation ; elle transmet les appels graphiques à Windows sans modifier le rendu. 17 fichiers modifiés (+421/−10). Guide de compilation et de test produit. Aucun jeu modifié à ce stade.

### Jalon 1 — Chargement dans Dead as Disco ✅
- Journal confirme l'initialisation : `proxy_ready`, `win32=0`, aucune erreur de chargement ; la DLL installée est bien celle compilée.
- Deux lancements confirmés par deux lignes `proxy_ready` ; sessions de plus de 2 minutes puis 3 minutes, aucun problème signalé.
- Aucun changement visuel, aucun gain de FPS attendus ni observés à ce stade.

### Jalon 2 — Compteur d'images / FPS ✅
- Nouvelle DLL comptant les images (Present) et inscrivant les FPS au journal. 28 fichiers (+761/−53). Ancienne DLL sauvegardée avant remplacement.
- Test : génération d'images **désactivée** (condition de tous les tests suivants). Journal : ~7 minutes de mesures, **plus de 58 000 appels d'affichage réussis**, aucune erreur de la DLL, ~121 FPS en fin de session. On-100-K-Lisse confirme : FPS similaires sur son compteur habituel, génération d'images bien désactivée.

### Jalon 4 — Observation des appels DLSS ✅ (DLL 0.3.0)
- La DLL observe les informations DLSS **sans modifier le rendu**. 21 fichiers (+682/−10) ; apparition de `ScalerMaison.ini`.
- Protocole : DLSS désactivé/réactivé en Qualité, 2 minutes ; puis Équilibré, 2 minutes.
- Résultat : plus de 11 minutes enregistrées, **329 échantillons DLSS, tous réussis**. Résolutions relevées : **Qualité : 1708 × 961 → 2560 × 1440 ; Équilibré : 1485 × 836 → 2560 × 1440**.

### Jalon 5a — Préparation des paramètres d'évaluation DLSS (DLL 0.4.0)
- Le code prépare désormais **les paramètres envoyés au DLSS du jeu** pour l'évaluation (c'est le début de la prise en charge active : le scaler ne fait pas encore l'upscaling lui-même, il prépare/gère les paramètres que le DLSS du jeu consomme via NGX). 21 fichiers (+403/−12) ; `dxgi.dll` ET `ScalerMaison.ini` requis.
- Incident de session : première interruption d'On-100-K-Lisse pour **manque de crédits d'utilisation** ChatGPT, avant reprise.
- Test : 5 min Qualité → 5 min Équilibré → 5 min Qualité. Journal : **91 363 évaluations DLSS**, aucune erreur ni repli (fallback) consigné, session de 11 min 47 s.
- **Point de rigueur à connaître :** le retour final en Qualité n'apparaît **pas** dans le journal ; On-100-K-Lisse confirme l'avoir fait pendant le test. ChatGPT l'a noté « confirmé par l'utilisateur, pas par le journal », sans refaire le test. Image normale, aucun problème constaté.

### Jalon 5b — Préparation des paramètres de création DLSS ✅ (DLL 0.5.0)
- Le scaler prépare aussi **les paramètres de création** des instances DLSS. 15 fichiers (+273/−8).
- Protocole : 5 min Qualité → 5 min Équilibré → 5 min Qualité, fermeture, relance, 1 minute de plus.
- Journal : **6 créations DLSS avec les paramètres du scaler**, **au moins 211 793 évaluations gérées sans repli consigné**, **845 échantillons DLSS réussis**. On-100-K-Lisse confirme par question fermée : image restée normale, sans traînées, scintillement, écran noir ni plantage.

### Jalon 5c — Mesure du temps GPU ; comparaison A (jeu) vs B (scaler) ✅ (DLL 0.6.0)
- Ajout de la **mesure du temps GPU autour du DLSS**. 23 fichiers (+637/−20).
- **Premier essai invalide :** l'essai B a été faussé (A constant à 30 FPS, B passé de ~117 à 30 FPS) — cause identifiée par On-100-K-Lisse : il était revenu sur le chat (Alt+Tab) et avait accroché la souris/caméra. **Protocole durci depuis :** écrire « prêt », retourner immédiatement en jeu, **4 minutes sans revenir au chat ni toucher la souris (1 min de chauffe + 3 min de mesure)**, caméra fixe, jeu au premier plan.
- Résultats propres (même scène, même angle, 2560 × 1440, DLSS Qualité, génération d'images désactivée) :
  - **FPS moyens : A (paramètres du jeu) 117,35 — B (paramètres du scaler) 117,36**
  - **Temps GPU autour du DLSS : A 1,130 ms — B 1,127 ms**
  - 360 mesures GPU valides par passage, aucune erreur DLSS, image normale.
  - Lecture : **performances pratiquement identiques** — aucun gain ni surcoût démontré du scaler sur cette scène. À la fin du jalon, le jeu était sur la configuration A (gestion DLSS du scaler désactivée).

### Comparaison préliminaire contre F5 (provisoire — dernier état de la conversation)
- Cadre : **DLSS SR seulement**, sans NR ni génération d'images, même outil de mesure des deux côtés. Le F5 utilisé est **OptiScaler F5 0.1.34**, pris dans le dossier « launcher » du dossier de travail (avec la DLL patch et RenoDX au besoin). DLL du scaler sauvegardée avant l'échange.
- Incident : au premier lancement F5, le chronomètre n'a pas démarré — cause : un réglage de journalisation trop détaillé côté F5 ; corrigé en recopiant seulement `OptiScaler.ini`, puis relance.
- Mesures (3 minutes chaque, même scène/réglages) : **F5 : 118,21 FPS moyens ; notre scaler : 117,26 FPS moyens → −0,8 % pour le scaler sur ce passage unique.**
- **Réserves explicites de ChatGPT :** résultat **provisoire, à répéter** ; les journaux montrent une **résolution interne légèrement différente entre les deux configurations**, à expliquer/aligner avant toute conclusion.
- Image jugée normale des deux côtés (« de mon bord rien à signaler »).
- La conversation se termine sur l'enregistrement de cette première comparaison, sans nouvelle consigne. **Après ce test, le dossier du jeu contient les fichiers du scaler** (copiés depuis « Notre-scaler » pour le passage du scaler).

## 5. Ce que la DLL fait aujourd'hui — et ne fait pas encore

**Fait et validé (Dead as Disco, RTX 4070, 2560 × 1440) :**
- Proxy dxgi complet : se charge, transmet les appels, journalise (`proxy_ready`, erreurs).
- Compteur d'images/Present + mesure FPS au journal.
- Observation des appels DLSS : résolutions d'entrée/sortie par preset relevées et journalisées.
- Préparation des paramètres d'évaluation **et** de création DLSS (chemin NGX), sans repli ni erreur sur > 300 000 évaluations cumulées.
- Mesure du temps GPU autour du DLSS ; surcoût mesuré ≈ nul face aux paramètres natifs du jeu (écart de 0,003 ms, dans le bruit).
- Aucun crash, écran noir, traînée ou scintillement rapporté sur l'ensemble des tests validés.

**Pas encore fait :**
- **Aucun upscaling produit par le scaler lui-même** : à ce stade, le scaler prépare/gère les paramètres que le DLSS du jeu exécute. Le remplacement par un backend propre (plan d'origine : FSR/XeSS) a été **écarté pour la v1** par décision d'On-100-K-Lisse (voir §2.2) — la suite de la v1 reste sur le chemin DLSS SR via NGX.
- **Species: Unknown : jamais testé** (le jalon 7 du plan — jeu sans upscaler intégré, profondeur + flux optique — reste le pôle dur, non entamé).
- **Menu en jeu :** reporté.
- **NR (Neural Rendering) :** non entamé, décision réservée au jalon 8.
- **Jalon 6 du plan (INI + journal exploitable par le launcher) :** partiellement amorcé (`ScalerMaison.ini` et `ScalerMaison.log` existent), mais le pont formel avec le DLSS 5 Launcher (lecture du journal par le launcher) n'a pas été fait.

## 6. Reste à faire, dans l'ordre suggéré par l'état des lieux

1. **Répéter la comparaison F5 vs scaler** et d'abord **expliquer/aligner la différence de résolution interne** relevée dans les journaux entre les deux configurations — sans ça, l'écart de −0,8 % n'est pas interprétable.
2. Décider avec On-100-K-Lisse de la forme de la suite v1 sur le chemin NGX (ce que « gérer DLSS » doit devenir concrètement dans le périmètre v1).
3. Jalon 6 : formaliser INI + journal pour le pont avec le launcher.
4. Jalon 7 : Species: Unknown (profondeur, flux optique, RenderDoc) — le gros morceau.
5. Jalon 8 : décision NR.
6. Questions ouvertes à régler avec On-100-K-Lisse : pousser ou non le code sur la branche GitHub `ai/chatgpt/scaler` ; liste complète des fichiers source (les diffs de la conversation partagée sont repliés) ; nom définitif du projet.

## 7. Note pour Claude, sur ta propre quarantaine

Tu connais le code d'OptiScaler F5 de l'intérieur (travail sur le fork, branche `ai/claude/dlss5`). Pour le scaler maison, la règle clean-room s'applique à toi comme à ChatGPT : **écrire uniquement depuis la documentation publique et les observations en jeu**, jamais depuis le code F5 lu par ailleurs. C'est la condition pour que la licence MIT du scaler reste propre — c'est le projet « vraiment à lui » d'On-100-K-Lisse.

— Kai
