# Cyberpunk 2077 — journal d'investigation des crashs en environnement moddé

**Période :** expériences discutées en 2026.  
**Rédigé le :** 23 octobre 2026.  
**Statut :** cas pratique, **diagnostic non définitivement établi**.

## Contexte

Des crashs ont été rapportés pendant des sessions de **Cyberpunk 2077** utilisant un environnement moddé et des réglages graphiques avancés. Le GPU employé est une **GeForce RTX 4070 12 Go** ; l'utilisateur joue habituellement en 1440p. Ne pas supposer que tous les essais utilisaient exactement les mêmes réglages.

Un message d'erreur discuté : `DXGI_ERROR_DEVICE_HUNG (0x887A0006)`.

Ce code signale un problème de réponse du GPU/pilote dans la chaîne DirectX. **Il ne suffit pas à identifier la cause racine**. Il ne prouve pas, à lui seul, que la carte graphique, un mod ou une DLL en particulier est défectueux.

## Éléments ayant fait partie de l'investigation

Plusieurs composants ou couches ont été mentionnés dans les discussions :
- **RED4ext**
- **redscript**
- **Cyber Engine Tweaks**
- **TweakXL**
- **ArchiveXL**
- Différentes options de rendu dont ReShade et des DLL DLSS expérimentales, dans certaines séances

Chaque composant doit être identifié par sa version exacte lors d'une nouvelle reproduction. L'existence de tous ces éléments dans les discussions **ne démontre pas** qu'ils étaient tous actifs simultanément au moment d'un même crash.

## Hypothèses de dépannage, non confirmées comme causes

- Conflits entre versions de frameworks ou incompatibilité avec une mise à jour du jeu.
- Surcharge ou instabilité dans la chaîne graphique (ray/path tracing, DLSS, post-traitement).
- Pression sur les **12 Go de VRAM** de la RTX 4070 dans les conditions les plus exigeantes.
- État instable du pilote graphique.

Aucune de ces hypothèses ne doit être présentée comme une correction vérifiée sans logs comparables et reproduction contrôlée.

## Protocole recommandé pour les prochains essais

1. **Sauvegarder** la liste des mods, versions, profils Vortex, fichiers de configuration et journaux d'erreur avant de modifier quoi que ce soit.
2. Noter version du jeu, pilote GPU, résolution, options RT/PT, DLSS et injecteurs utilisés.
3. Vérifier si le crash se reproduit **sans les injecteurs additionnels**, puis avec un élément à la fois.
4. Si nécessaire, tester séparément les frameworks et leurs dépendances, plutôt que désactiver des dizaines de mods sans suivi.
5. Relever les journaux et l'horodatage, l'utilisation de la VRAM et la séquence menant au crash.
6. Documenter explicitement **résolu**, **non reproduit** et **cause incertaine**, qui ne veulent pas dire la même chose.

## Tableau de suivi à remplir

| Date | Version jeu | Modifications actives | Message/trace | Test | Résultat |
|---|---|---|---|---|---|
| Inconnue (temporaire) | Inconnue (temporaire) | Environnement moddé et expérimentations de rendu | `DXGI_ERROR_DEVICE_HUNG (0x887A0006)` discuté | Plusieurs hypothèses examinées | Attribution causale non confirmée |

## Crédits

Cyberpunk 2077 appartient à **CD PROJEKT RED**. Les frameworks nommés sont des projets d'auteurs tiers ; voir leurs dépôts/pages de publication pour les licences, versions et auteurs. Ce document rapporte une investigation utilisateur assistée par ChatGPT, **pas** la création de ces frameworks.

Ne pas publier de crash dumps bruts avant de vérifier s'ils contiennent des chemins personnels, identifiants ou autres informations sensibles.
