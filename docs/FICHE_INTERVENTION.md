# Modèle de fiche d'intervention — atelier de réparation

Deux formats : la fiche Markdown (rapide à remplir sur place) et la spec de la liste SharePoint/Microsoft Lists « Fiches d'intervention » (ta base de connaissances qui grandit à chaque client).

---

## 1. Fiche Markdown (copier-coller par intervention)

```markdown
# FICHE-____ — [Nom du client]

- **Date :** AAAA-MM-JJ
- **Appareil :** [marque + modèle] — [OS]
- **Mot de passe de session :** [si nécessaire — confidentiel, ne pas partager]

## Symptôme (dans les mots du client)
> « ... »

## Contexte
- **Depuis quand :**
- **Changement récent :** [mise à jour / chute / orage / nouveau logiciel / aucun / inconnu]
- **Reproductible :** [oui / non / intermittent]

## Hypothèses et tests (UNE variable à la fois)
| # | Hypothèse | Test | Résultat |
|---|-----------|------|----------|
| 1 |           |      |          |
| 2 |           |      |          |
| 3 |           |      |          |

## Diagnostic
[Le POURQUOI, pas juste le quoi]

## Actions effectuées
1.
2.

## Pièces remplacées
- [pièce] — [coût $ CAD] — [fournisseur]

## Résultat
[réparé / réparé partiellement / non réparable / en attente de pièce / référé]

## Temps passé
[heures]

## Notes (portfolio)
[Ce que cette réparation t'a appris — 2-3 lignes max]
```

---

## 2. Liste Microsoft Lists « Fiches d'intervention »

Créer une liste (SharePoint ou personnelle) avec ces colonnes :

| Colonne | Type | Détails |
|---|---|---|
| Titre | Titre (auto) | Format suggéré : `FICHE-0001`, `FICHE-0002`… |
| Client | Texte (une ligne) | Nom du client |
| Type d'appareil | Choix | Portable / Tour / Téléphone / Tablette / Périphérique / Autre |
| Appareil | Texte (une ligne) | Marque + modèle |
| OS | Choix | Windows 11 / Windows 10 / macOS / Android / iOS / Linux / Autre |
| Symptôme | Texte (plusieurs lignes) | Dans les mots du client |
| Depuis quand | Date | Début approximatif du problème |
| Changement récent | Choix | Mise à jour / Chute ou choc / Orage ou coupure / Nouveau logiciel / Aucun / Inconnu |
| Diagnostic | Texte (plusieurs lignes) | Le POURQUOI |
| Actions | Texte (plusieurs lignes) | Ce qui a été fait, étape par étape |
| Résultat | Choix | Réparé / Réparé partiellement / Non réparable / En attente de pièce / Référé |
| Pièces | Texte (une ligne) | Pièces remplacées |
| Coût pièces | Devise | En $ CAD |
| Temps (h) | Nombre | Temps passé, en heures |
| Date d'intervention | Date | Date de la réparation |
| Statut | Choix | Ouvert / En cours / En attente / Fermé |

Vues suggérées : **« En cours »** (filtrée Statut = En cours ou En attente), **« Par résultat »** (groupée par Résultat — pratique pour réviser tes patterns de diagnostic).

---

## 3. Idée Power Automate (quand tu voudras)

Déclencheur : quand un élément passe à **Statut = Fermé**.
Actions : générer le Markdown de la fiche (même structure qu'en section 1) → enregistrer dans OneDrive/SharePoint (`/Atelier/Fiches/FICHE-XXXX.md`).

Résultat : chaque réparation devient automatiquement une page de documentation, sans double saisie. Ton portfolio se construit tout seul.
