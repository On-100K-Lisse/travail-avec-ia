# DLSS 5 — dépannage des artefacts et retour arrière

**Date de rédaction :** 23 octobre 2026  
**Source :** échanges de dépannage avec l'utilisateur, particulièrement les 7 et 19 octobre 2026.  
**Statut :** retour d'expérience, tests non standardisés.

## Effet de « cadre blanc transparent » sur certaines textures

Sur une installation expérimentale, un **cadre blanc/translucide** entourait certaines parties de textures, le centre conservant la couleur attendue.

Essais rapportés :
- Le preset **Naturel** atténuait la visibilité du défaut sans le faire complètement disparaître.
- Le preset **F** ne changeait rien de notable.
- Mettre à zéro **Structure** *ou* **Ton local** supprimait le défaut dans la scène testée, d'après la vérification directe de l'utilisateur.

**Conclusion limitée :** ces deux réglages étaient liés à l'apparition visible de l'artefact dans cette installation, mais la cause interne exacte du bug n'a pas été établie. On ne peut pas extrapoler à tous les jeux.

## Standalone et 1-Click

L'utilisateur a essayé une solution alternative/standalone puis l'a retirée lorsque les résultats ont empiré. Le retour au **1-Click précédemment stable** a rétabli son fonctionnement habituel selon son compte rendu.

**Leçon de dépannage :** garder une copie des fichiers et paramètres d'une configuration qui fonctionne avant de remplacer des DLL ou de cumuler des injecteurs. Ne pas considérer plusieurs installations simultanées comme compatibles sans validation du jeu et des mécanismes de chargement.

## Sécurité des outils téléchargés

Des distributions communautaires et des DLL expérimentales ont été discutées, notamment un lien MEGA relayé par un compte X. Le fait qu'un lien soit partagé par un compte connu **ne vérifie pas, à lui seul, l'intégrité des octets téléchargés**.

Avant tout essai :
1. Identifier la source d'origine et, si possible, la publication officielle ou le dépôt du développeur.
2. Consulter les notes de version, les dépendances et les permissions demandées.
3. Calculer l'empreinte SHA-256 du fichier et la comparer à **une référence indépendante fiable pour cette même version**, si elle existe.
4. Faire analyser le fichier par les protections disponibles. Une absence d'alerte n'est pas une garantie.
5. Sauvegarder la configuration précédente ; documenter comment désinstaller complètement l'outil.
6. Éviter l'exécution avec des privilèges élevés lorsqu'ils ne sont pas nécessaires.

**Important :** une empreinte seule prouve l'identité entre deux fichiers, pas la sûreté d'un fichier malveillant si la référence n'est pas digne de confiance.

## Données manquantes

Noms exacts, versions et hachages des injecteurs utilisés dans chaque séance : **inconnu (temporaire)**. Les résultats rapportés ci-dessus sont des observations pratiques et non des tests de sécurité indépendants.

## Crédits

NVIDIA (DLSS), développeurs tiers des outils expérimentaux (noms et versions à documenter avant citation nominative), créateurs des jeux concernés. Les résultats et essais décrits proviennent des observations de l'utilisateur.
