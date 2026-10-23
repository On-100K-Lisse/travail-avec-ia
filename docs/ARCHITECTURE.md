# Agent de veille jeux vidéo — architecture

**État :** conçu et décrit ; le transfert complet vers le portable Debian était
en attente au dernier état connu (temporaire).
**But :** surveiller l'actualité de studios et de jeux précis, sans avoir à la
chercher soi-même chaque jour.

## Le principe

Un programme Python qui fait de la **veille OSINT** — OSINT (Open Source
Intelligence) veut dire : uniquement des informations **publiques**, recueillies
et triées automatiquement. Aucune source privée, aucun accès privilégié : l'agent
lit ce que tout le monde peut lire, mais il le fait systématiquement, et il ne
retient que ce qui compte.

## Comment ça fonctionne

1. **Collecte** — l'agent consulte ses sources publiques à intervalle régulier.
2. **Analyse** — les nouveautés sont analysées par l'API de **Gemini** (Google),
   qui résume et évalue la pertinence par rapport aux cibles surveillées.
3. **Alertes par courriel**, en deux rythmes :
   - **immédiates** pour les cibles prioritaires (ci-dessous) ;
   - **un récapitulatif hebdomadaire**, le vendredi à 17 h, pour le reste.

## Les cibles prioritaires

- CD Projekt Red et la suite de *Cyberpunk 2077* ;
- *Crimson Desert* (Pearl Abyss) ;
- Larian Studios ;
- Rockstar Games et *GTA 6* — **sans spoilers d'histoire**, filtre appliqué dès
  la conception ;
- Bethesda ;
- *The Legend of Zelda* (Nintendo) ;
- *Life is Strange* ;
- les exclusivités Nintendo, Sony et Xbox en général.

## Où ça roule

L'agent est destiné au **portable Debian** d'On-100-K-Lisse — une machine
secondaire, toujours disponible, qui ne monopolise pas le PC principal. Le plan
de relance du homelab (branche `ai/kai/homelab`) prévoit aussi de pouvoir
l'héberger sur le serveur Dell à terme.

## Les choix de conception

- **Sources publiques seulement** : c'est ce qui rend l'outil simple, légal et
  robuste — pas de comptes à maintenir, pas de contournement.
- **Un humain décide ce qui est prioritaire** : la liste des cibles est explicite,
  l'IA analyse et trie, elle ne choisit pas les sujets.
- **Alerte immédiate seulement pour l'important** : tout le reste attend le
  récapitulatif — une veille qui crie tout le temps finit ignorée.
