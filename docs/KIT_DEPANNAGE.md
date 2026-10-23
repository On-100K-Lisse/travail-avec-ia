# Kit de dépannage — atelier de réparation

Conçu pour les interventions à l'école : du vrai matériel pété, des vrais usagers qui décrivent mal leur problème. Méthode = une variable à la fois, comprendre le POURQUOI avant de considérer un problème réglé.

---

## 1. Avant de toucher à quoi que ce soit

1. **Fiche d'intervention** — noter : nom du client, appareil (marque/modèle/OS), mot de passe de session si nécessaire (confidentiel), le problème *dans les mots du client*, depuis quand ça dure, ce qui a changé récemment (mise à jour ? chute ? orage ?), est-ce que ça se reproduit.
2. **Sauvegarde d'abord** — « Tes données sont-tu importantes ? » Si oui : copie sur support externe AVANT toute manip. Toujours. Sans exception.
3. **Photo du setup** — câbles, ports, état physique avant de débrancher. 10 secondes qui sauvent des vies.

---

## 2. La méthode

- **Une hypothèse → un test → noter le résultat.** Une variable à la fois.
- Ne jamais changer deux choses en même temps : si ça marche, tu sauras pas pourquoi.
- Un test qui donne rien = l'hypothèse est fausse. Passer à la suivante, pas forcer la première.
- Documenter PENDANT, pas après (tu oublieras les détails).

---

## 3. Triage matériel (les 5 premières minutes)

**No power / no boot :**
1. Le banal d'abord : prise, câble, interrupteur du PSU, multiprise.
2. Débrancher les USB inutiles ; écran sur le bon port (carte graphique dédiée vs intégrée — l'erreur classique).
3. Reseat la RAM (une barrette à la fois si ça boot pas).
4. Reset CMOS si comportement bizarre post-coupure.
5. Beep codes / LED debug de la carte mère → noter le code avant de Googler.
6. Doute sur le PSU → tester avec une alim connue bonne, pas avec des prières.

**Disque / lenteur :**
- CrystalDiskInfo → SMART : « Caution » ou « Bad » = disque à remplacer, pas de patch magique.
- HWiNFO → températures : CPU > 90 °C en charge = thermal throttling ; vérifier pâte thermique et ventilateurs.

---

## 4. Triage logiciel

1. **Mode sans échec** — ça boot là ? → ça sépare pilote/logiciel (ça marche) de matériel (ça marche pas).
2. **Observateur d'événements** (`eventvwr`) → erreurs critiques à l'heure du crash. C'est ton journal de bord.
3. **Pilotes** : rollback d'abord ; DDU si c'est un problème GPU.
4. `sfc /scannow` puis `DISM /Online /Cleanup-Image /RestoreHealth` — dans cet ordre.
5. **Defender → analyse hors ligne** : le grand classique du « ça marche pu » depuis 3 mois.
6. **Mises à jour Windows** : comparer la date d'apparition du symptôme avec l'historique des mises à jour. Et méfiance envers les KB *optionnelles* — une mise à jour censée réparer peut casser autre chose (tu sais de quoi je parle 😉).

---

## 5. Hygiène d'atelier

- Ne jamais promettre de délai ni de résultat : « Je regarde et je te reviens. »
- Les données des gens = confidentiel. On fouille pas.
- Si la réparation coûte plus cher que l'appareil, le dire honnêtement.
- Tout noter : ces fiches deviennent ton portfolio.

---

## 6. Idée pour ta doc

Tu structures déjà ta doc IT en Microsoft Lists : une liste « Fiches d'intervention » avec les champs *Appareil / Symptôme / Diagnostic / Actions / Résultat / Pièces* te donnerait une base de connaissances qui grandit à chaque client. Si tu veux, je te génère le template prêt à importer.
