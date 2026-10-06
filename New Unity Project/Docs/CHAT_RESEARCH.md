# Zone chat: what players talk about (research, 2026-10-06)

Chris asked (playtest note 62) for a chat area for all sims and players in the same zone, with the sims "chatting away", modelled
on what real players say in WoW (retail, Classic, and WoW: Forever), EverQuest (and EverQuest Legends) and Monsters & Memories.
Sources are listed at the end. Nothing here copies another game's text; the lines we write are our own (GAME-ONLY).

## The channels these games use

| Channel | Reach | Used for | Our version |
|---|---|---|---|
| Say | a short distance round the speaker | greetings, talk to nearby people and NPCs | **Say**: sims near you (already: villager bubbles) |
| OOC / General | the whole zone | casual talk, questions, "where is", banter, finding groups | **Zone** (the main one) |
| Auction / Trade | the zone (WoW: cities only) | WTS / WTB / WTT, price checks, crafting services | **Trade** (or tagged lines inside Zone at first) |
| LFG / Looking for group | the zone or the world | LFG, LFM, "LF1M healer", camp checks | **Looking for group** |
| Shout | the zone | formal notices, a train coming, a named up | folded into Zone with a marker |
| Tell / Whisper | one person, world-wide | private replies, "pst" (please send tell) | **Whisper** (a sim answering you; later) |
| Group / Guild | members only | the party's talk | **Party** (the invited sims) |

Monsters & Memories (the closest in spirit to Crulanda) has Say by proximity, OOC and Auction zone-wide, Shout zone-wide, tells
world-wide, and a Group Finder behind the Social window (O) with an LFG flag in /who. EverQuest players report that OOC gets
spammed and that "general" and "new players" channels are the lively ones; EverQuest Legends' first days were general chat full
of questions. WoW's Trade chat is famously full of selling and boosting, and players ask for it to be split from LFG. WoW:
Forever ships the Classic Era group finder and is "realmless" (one population), and its community chat is mostly social.

## What the lines are about (by how often they turn up)

1. **Finding a group** (the most common): "LFG [where/what], [class] [level]"; "LFM [place], need healer"; "LF1M tank for
   [place]"; "LF2M [place] then go"; "anyone want to group for [quest]?"; "camp check [place]?" (is someone already there).
2. **Buying and selling**: "WTS [item] [price]"; "WTB [item], paying well"; "WTT"; "PC (price check) [item]?"; crafting offers
   ("can make [thing], bring mats, tips welcome"); "pst" (send me a tell).
3. **Questions from newer players**: "where do I find [NPC/trainer/place]?"; "how do I get to [zone]?"; "what level for [place]?";
   "is [named mob] up?"; "what does [stat] do?"; "how do I [mechanic]?". Others answer, sometimes kindly, sometimes not.
4. **The world going on**: "[named] is up at [place]"; "train to zone!" (a crowd of mobs chasing someone to the zone line);
   "careful, [elite] wandering by [place]"; "rez please at [place]?"; "anyone seen my corpse, died near [place]"; weather and
   night ("dark already?", "rain again").
5. **Milestones**: "ding" (levelled), "ding 10!", "grats" replies; first rare drop; a new skill level.
6. **Social banter**: jokes, complaints about mobs or drops ("third boar, no tusk"), food, "brb", "afk", "ty", "np", "lol",
   arguments about classes, greeting friends, saying goodnight when they log off.
7. **Trouble** (keep light): kill stealing complaints ("that was my mob"), ninja looting, begging.

## Lingo to draw on

LFG, LFM, LF1M, WTS, WTB, WTT, PC, pst, inc (incoming), OOM (out of mana), train, KS (kill steal), camp, camp check, PH
(placeholder) and named, pop (a mob appeared), con (consider), corpse run, rez, ding, grats, ty / np, brb / afk, lol, gz, nub,
kite, aggro, add (an extra mob joined), pull.

## How the sims should chat (design for the next round)

- **Who talks:** each sim by its `chatty` weight (0-1); quiet sims rarely, chatty ones often; at most one line every few
  seconds per zone, so it reads like a busy zone, not a flood. Some sims never speak in Zone (lurkers).
- **What about:** chosen from what the sim is actually doing and has done (SimFigure: hunting where, gathering what, at the inn,
  just levelled, just died, its gear), so the lines are true: "LF1M for the wolves at the Old Fold, I'm a Ranger 5", "WTS
  copper ore x6", "ding 6", "anyone know where the herbalist is?", "careful, Old Hazelmaw is up by the mill".
- **Replies:** other sims answer some questions and congratulate dings ("grats"); a sim answers you when you type in Zone and
  your words match something it knows (place, trainer, a quest giver), and whispers you when it is LFG and you are its level.
- **Voice by personality:** friendly sims say ty and grats; bold ones boast and call for groups; chatty ones banter; a few use
  heavy lingo, some write in full sentences. Names of places, mobs and items come from the zone's data (no WoW names).
- **The window:** tabs for All, Zone, Trade, LFG, Party (and later Whisper); the player types with Enter and picks the channel
  with /z, /t, /lfg, /p, /s (say); lines coloured by channel; a who list (O) already exists.

## Sources

- Blizzard forums: separating WTS/WTB from LFG in trade chat; WoW: Forever discussion (group finder, realmless).
- WoWWiki: Trade Chat (cities only, /2). Reddit r/wownoob, r/classicwow (where general, trade and LFG talk happens).
- EverQuest: "Chatting in EverQuest" tip; Bonzz's EQ channel notes (/ooc reaches the whole zone); EQ2 wiki chat channels;
  r/EQLegends "finding groups" (general and new-player chats are the lively ones).
- Monsters & Memories new player guide (Say, OOC, Shout, Auction, tells, Group Finder, common acronyms and terms).
