# Scaler maison — Périmètre v1

**Projet :** un scaler construit de zéro (code 100 % original), pendant qu'OptiScaler F5 et RenoDX continuent de servir au quotidien. Nom à choisir — sans « DLSS » (marque NVIDIA) et sans laisser croire à un produit officiel.
**Équipe :** Alexandre décide, supervise, valide en jeu et mesure. Démarrage avec **ChatGPT (GPT-6 Astra)** : architecture et code, en travaillant directement dans le dossier du projet sur le PC d'Alexandre (son mode work, comme Claude Cowork) — la boucle compiler→tester roule dans ce dossier-là. Claude reste sur le launcher (renfort possible plus tard). Kai : documentation et mémoire du projet.
**Règle d'or héritée de ses autres projets :** 2-3 systèmes poussés au maximum plutôt que tout, mal.

## Décision du jour 1 — la licence (TRANCHÉE le 30 octobre 2026)

**Licence propre (MIT, comme le launcher). Pas de GPL.** Décision d'Alexandre : le scaler doit lui appartenir pour vrai — code créé de zéro, qui n'appartient à personne d'autre.

Conséquence directe sur la méthode : **clean-room total**. Aucune copie, aucune adaptation, aucune traduction de code d'**OptiScaler** ou de **RenoDX**. Lire pour comprendre les concepts est permis ; l'implémentation part de la documentation et des SDK officiels + ses propres mesures. (L'alternative GPL v3 — emprunts permis mais licence imposée pour toujours — a été écartée.)

## Objectif v1

Un injecteur minimal qui fait rouler **DLSS Super Resolution + Neural Rendering (via NGX)** dans des jeux **DirectX 12** ciblés, sur la RTX 4070 d'Alexandre d'abord — avec des mesures comparables à ses chiffres F5 existants.

## Dans le périmètre

- **API graphique :** DirectX 12 seulement.
- **Backends :** DLSS SR et DLSS NR via le SDK NVIDIA NGX ; FSR permis comme premier upscaler d'apprentissage (SDK ouvert et documenté).
- **Injection :** DLL proxy (dxgi.dll), hook du swap chain, capture couleur / profondeur / vecteurs de mouvement.
- **Jeux cibles (validation) :** Dead as Disco et Species: Unknown — possédés, déjà mesurés, baselines F5 chiffrées disponibles pour comparer.
- **Mesure :** coût NR en ms GPU et FPS sur la même scène que les baselines, une variable à la fois.
- **Configuration :** fichier .ini + journal de bord (logs). Pas d'interface graphique en v1.

## Hors périmètre v1 (explicitement)

Vulkan, DirectX 11, FSR, XeSS, frame generation, AMD/multi-GPU, autres jeux que les cibles, anti-cheat (jeux solo seulement), distribution publique (elle vient après les critères ci-dessous).

## Ordre de construction — chaque étape se valide en jeu avant la suivante

1. **Squelette.** La DLL proxy se charge dans Dead as Disco sans rien modifier ; le jeu roule, un log prouve sa présence.
2. **Capture.** Hook du swap chain ; journaliser par frame : résolutions, formats, présence de la profondeur et des vecteurs de mouvement.
3. **Premier upscaler documenté.** DLSS SR via NGX (documenté publiquement) ou FSR : évaluation avec jitter et reset corrects ; image upscalée stable, sans ghosting massif.
4. **Comparaison.** A/B contre F5 sur la même scène (références : Dead as Disco 82,8 FPS et NR 3,43-3,65 ms en preset B ; Species: Unknown preset B ≈ 3,9 ms).
5. **Durcissement.** Deuxième jeu cible (Species: Unknown), logs propres, procédure de build reproductible écrite.
6. **DLSS NR — en dernier.** L'interface de `nvngx_dlssnr.dll` n'est pas documentée publiquement (aucun SDK public ne la contient) : c'est la partie la plus dure, elle vient quand tout le reste roule. Appel du feature NR, mesure du coût en ms, comparaison aux baselines.

## Règles du projet

- **Clean-room total (licence propre décidée)** : sources = documentation et SDK officiels + ses propres mesures. Aucun code d'OptiScaler/F5 ou de RenoDX copié, adapté ou traduit — sinon ce n'est plus « de zéro » (voir la décision licence ci-dessus).
- **Un seul écrivain à la fois** dans le code ; les autres IA conseillent en texte.
- **Git dès le jour 1**, petits commits séparés ; inconnu = « inconnu (temporaire) » dans le journal.
- **Log lisible dès le départ** : lignes claires et régulières (coût par image, modèle créé, erreurs GPU), dans un format que le launcher pourra parser plus tard comme il parse `OptiScaler.log` — historique, fiche de test et Conseils viendront presque gratuitement.
- **Le modèle NR reste la DLL de NVIDIA**, fournie par l'utilisateur depuis ses propres jeux — jamais redistribuée.

## Critères « publiable » — la v1 est finie quand :

- [ ] Le build se reproduit depuis le code source sur une machine propre, en suivant seulement la procédure écrite.
- [ ] Les 2 jeux cibles roulent en SR + NR sans crash sur une session d'au moins 1 h.
- [ ] Les chiffres A/B contre F5 sont documentés honnêtement — même s'ils sont moins bons.
- [ ] Licence propre appliquée au dépôt (MIT, comme le launcher), README qui dit ce que c'est et ce que ce n'est pas : non affilié à NVIDIA, aucun fichier tiers inclus, jeux solo seulement (risque anti-cheat ailleurs).

## Premier pas concret

1. ~~Trancher la licence~~ — **fait** : licence propre (MIT), pas de GPL, clean-room total.
2. ~~Choisir l'hébergement du code~~ — **fait** : branche `ai/chatgpt/scaler` créée dans travail-avec-ia le 30 octobre 2026 (licences LICENSE + LICENSE-MIT présentes dès la création, ligne ajoutée au tableau de `main` — règles 1 et 9 respectées). Montrer à ChatGPT la section « Règles de travail » du README de `main`.
3. Ouvrir le journal de bord et construire l'étape 1.
