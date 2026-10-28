# Politique de sécurité et signalements sensibles

## La nature de ce dépôt

`travail-avec-ia` est un dépôt de **documentation** : il ne distribue aucun logiciel, n'opère aucun service en ligne et ne collecte aucune donnée sur ses lecteurs. Il n'y a donc pas de « versions supportées » ni de failles logicielles à corriger au sens habituel.

Il reste trois choses qui méritent un signalement rapide et, quand c'est sensible, **privé** :

1. **Une donnée personnelle ou sensible publiée par erreur** — c'est le cas le plus important. La règle 6 de ce dépôt est stricte : pseudonymes seulement, aucune donnée nominative ou sensible. Si vous en trouvez une malgré tout (un nom réel, une adresse, un identifiant, un secret), elle doit être retirée immédiatement.
2. **Un contenu publié sans le droit de l'être** — par exemple un document ou un extrait qui n'aurait pas dû devenir public (règle 7).
3. **Une procédure documentée qui peut endommager le système d'un lecteur** — une commande ou une étape décrite ici qui, suivie telle quelle, présente un risque réel (perte de données, système inutilisable). Les erreurs ordinaires passent par les Issues normales ; ceci vise les cas dangereux.

## Comment signaler

**Ne recopiez jamais la donnée sensible dans une Issue publique** — vous la republieriez.

- **Voie privée (préférée)** : onglet **Security** de ce dépôt → **Report a vulnerability**. Le signalement n'est visible que par le mainteneur.
- **Si cette voie n'est pas disponible** : ouvrez une Issue avec le modèle « Signaler une erreur », en écrivant seulement « donnée sensible à retirer » et l'endroit concerné (branche, dossier, document) — **sans le contenu lui-même**. Le mainteneur vous répondra dans l'Issue.

## Ce qui se passe ensuite

- Le signalement est examiné dès sa lecture.
- Si le problème est confirmé : **retrait immédiat** du contenu, correction dans l'historique du dépôt si nécessaire, et confirmation au signaleur.
- Pour une procédure dangereuse : le document est corrigé ou assorti d'un avertissement visible, et le commit de correction le mentionne.

## Périmètre

Cette politique couvre **toutes les branches** du dépôt, pas seulement `main`.

Ce dépôt n'a pas de programme de récompenses (bug bounty) : les signalements sont bénévoles et traités de bonne foi, dans le même esprit que le reste du projet.
