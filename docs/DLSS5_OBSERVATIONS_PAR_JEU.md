# DLSS 5 — observations personnelles par jeu (RTX 4070)

**Période des expériences :** août–octobre 2026  
**Rédaction :** 23 octobre 2026 — ChatGPT, à partir d'échanges et de comptes rendus de tests de l'utilisateur.  
**État :** journal d'observations, **pas** benchmark scientifique, ni garantie de compatibilité.  
**Configuration connue :** GeForce RTX 4070 12 Go, utilisation principale en 2560 × 1440. Versions exactes des pilotes et réglages complets de chaque test : **inconnu (temporaire)**.

## Méthode de lecture

- **Observé** : constat expressément rapporté par la personne qui a réalisé le test.
- **Rapporté sans mesure reproductible** : appréciation subjective ou donnée issue d'un échange, à confirmer.
- **À vérifier** : explication possible ou résultat nécessitant une nouvelle expérience.
- Les tests concernent des **intégrations expérimentales/non officielles** du rendu neuronal surnommé « DLSS 5 » dans les discussions. Ne pas les confondre avec une prise en charge officiellement garantie par le développeur d'un jeu ou par NVIDIA.

## Synthèse par jeu

| Jeu | Observation remontée | Niveau de preuve / réserve |
|---|---|---|
| **Cyberpunk 2077** | Amélioration visuelle marquée selon l'utilisateur ; plusieurs incidents de stabilité dans un environnement très moddé, avec diverses couches de rendu. | Rendu apprécié : **observé**. Cause des crashs : **non déterminée** ; ne pas attribuer automatiquement les crashs au DLL. |
| **Dead as Disco** | Plusieurs configurations ont d'abord provoqué des crashs. Une installation différente via ReShade et l'add-on a ensuite chargé le rendu, et un réglage du point d'insertion dans la chaîne de rendu a affiché une image exploitable. | Installation fonctionnelle sur une session : **observé** ; stabilité à long terme : **inconnu (temporaire)**. |
| **Crime Scene Cleaner** | Problème d'apparence ou de mouvement suivant le mode utilisé ; un réglage de type Feeder/E l'a amélioré. Un « cadre blanc » translucide apparaissait sur certaines textures. | Tests de réglages rapportés directement ; voir le journal de correction associé. |
| **Crimson Desert** | Pixels et couleurs déformés dans la brume ou les effets volumétriques. | Artefact **observé** ; cause technique exacte **à vérifier**. |
| **Baldur's Gate 3** | Effet subtil en vue de dessus ; plus perceptible dans des scènes rapprochées/cinématiques. | Appréciation qualitative, **sans mesures standardisées**. |
| **R.E.P.O.** | Effet visuel jugé presque invisible. | Appréciation qualitative. |
| **Sleeping Dogs**, **GTA IV**, **GTA V** | Effet de modernisation visuelle jugé important sur certains anciens jeux. | Appréciation subjective ; aucune capture comparative contrôlée conservée ici. |
| **Life is Strange** | Rendu jugé particulièrement réussi. | Appréciation qualitative ; épisode/version exacte **inconnu (temporaire)**. |

## Détails utiles

### Dead as Disco — séance du 7 octobre 2026

1. **Problème initial constaté :** des combinaisons dites « Direct » / « Feeder » / « standalone » entraînaient des crashs malgré la présence de DLSS natif dans le jeu. L'outil 1-Click ne produisait pas toujours le mode attendu.
2. **Résultat plus tard rapporté :** installation de ReShade avec un add-on et un chemin de rendu NGX / DirectX 12 actif. Le déplacement du point d'insertion de « Rendu » vers « Upscalé » a rendu le rendu exploitable.
3. **Coût de performance :** significatif dans cette configuration. Les millisecondes mesurées ont varié en fonction des préréglages ; on ne peut pas en tirer une comparaison universelle sans un protocole reproductible.
4. **Non confirmé :** une cause racine unique des anciens crashs ou une stabilité garantie après de longues sessions.

### Cyberpunk 2077

- Les tests combinaient parfois DLSS, Ray Reconstruction, path tracing, ReShade et des frameworks de mods.
- Des plantages de type GPU hang ont été observés dans des séances distinctes ; la forte consommation de mémoire vidéo et l'empilement de couches graphiques sont des **pistes d'investigation**, pas une attribution causale démontrée.
- Le guide des crashs du jeu est documenté séparément dans la branche `ai/chatgpt/mods`.

## Limites et crédits

Les impressions visuelles, réglages et résultats de terrain sont ceux de l'utilisateur. Ce document synthétise ces échanges, il ne revendique pas la création des technologies testées. DLSS et GeForce sont des technologies/marques de **NVIDIA** ; ReShade est un projet tiers, et les jeux appartiennent à leurs studios et ayants droit respectifs. Auteurs de certaines distributions communautaires : à vérifier selon la version utilisée ; **inconnu (temporaire)** pour les exécutables non identifiés avec certitude.

**À documenter lors d'un prochain test :** date, version précise du jeu, pilote NVIDIA, version et hachage du DLL, nom/version de l'outil, type d'injection, résolution, qualité DLSS, Frame Generation, ray/path tracing, préréglage, FPS ou frametime sur séquence fixe, et capture avant/après.
