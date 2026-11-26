# Working with several AIs — the method used in this repository

**Version française :** [METHODE_TRAVAIL_MULTI_IA.md](METHODE_TRAVAIL_MULTI_IA.md)

**What this document is for:** explaining, for a human reader, how this repository is
produced by several artificial intelligences working together — and how anyone can
reproduce this way of working.

## The principle

No AI is 100 % reliable on its own: each one can be wrong, misunderstand, or lack
information. The idea behind this repository is to make them work **together**, with
different roles, so they review and correct each other. The human (On-100-K-Lisse)
directs, decides, and arbitrates.

## The roles

| AI | Made by | Main role in this repository |
|---|---|---|
| Claude | Anthropic | Technical diagnosis and code; local access to projects via Claude Cowork |
| ChatGPT | OpenAI | Synthesis, counter-expertise, and running tests |
| Gemini | Google | Content and code generation (e.g. C++ code) |
| Kai | Muse (Meta) | Project memory across tools, verified research, day-to-day coordination and documentation |

A clear role beats four identical opinions: when the AIs disagree, the disagreement
itself is information.

## Working principles

1. **Cross-verification.** An important claim is not taken on faith: it is confirmed
   by another AI or by a verifiable source. Real example: for a rebuilt DLSS DLL, the
   file's **hash** settled the question, not the label shown by the tools ("labels
   lie, hashes don't").
2. **Rotation on limits.** When an AI hits its usage limit or refuses a task, another
   one takes over. A blocked AI is never forced: the work is routed to the one that
   can do it.
3. **Faithful relays.** When On-100-K-Lisse's words are passed from one AI to
   another, they are quoted **word for word**, with no rewording that would distort
   the request.
4. **The human arbitrates.** If outside information (web research, another AI)
   contradicts what On-100-K-Lisse observed himself, his observation is the
   reference — the contradiction becomes a question to verify, not an automatic
   correction.
5. **Each in its own space, free reading.** Each AI writes in its own branches
   (`ai/<ai>/<project>`, see the README rules) but can read everything: that is what
   makes cross-review possible.

## The shared infrastructure

The AIs connect to the same tools through **MCP** (Model Context Protocol, a standard
that lets an AI use external software). The reference setup has 7 servers: files
(filesystem), database (sqlite), GitHub, web search (brave-search), page reading
(fetch), memory (memory), and step-by-step reasoning (sequential-thinking). Each AI
has its own access and its own sections; none writes in another's section.

## What it produced, concretely

- **The DLSS evening of October 19, 2026**: diagnosis by Claude, test protocol by
  ChatGPT, research and build by Kai — three different angles that unblocked a
  problem a single AI had closed too quickly. Details in the `ai/kai/dlss5` branch.
- **This repository's rules** (licences everywhere, no spillover between branches,
  credit to authors, readable documentation, "unknown (temporary)"): applied by each
  AI in its own branches from their adoption on October 23, 2026.

## The limits, honestly

- Each AI can still be wrong; the method reduces errors, it does not eliminate them.
  That is why documents carry their dates, their progress status, and the mention
  "unknown (temporary)" when information is not verified (rule 5 of the README).
- Quality depends on the human who arbitrates: roles and rules do not replace his
  judgment, they make it more effective.
