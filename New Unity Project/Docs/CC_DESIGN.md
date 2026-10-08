# Crowd control: design (draft 1, 2026-10-08)

Chris, after his first run of the Sealed Adit: "good start overall but needs to be deeper, and will be hard to do since we don't have
any CC classes yet, so that is something we need to look at next." Classic dungeons are built on it: a pull of three to five, one or two
taken out of the fight while the group kills the rest, casters interrupted. Crulanda has slows and roots (Ranger Pin and Snare, Druid
Stillroot and Briar Snare) but nothing that takes a mob out of a fight, no interrupts, and sims that would break CC anyway.

## Decisions for Chris (my picks first)
1. **Classic strength (pick):** long CC (20-30 s) that breaks on damage, short stuns (3-4 s) that don't. (Or modern: everything short.)
2. **Marking with keys (pick):** Ctrl+1 skull (kill first), Ctrl+2 moon (CC), Ctrl+3 cross (second CC), Ctrl+4 clear. Sims read the marks.
   (Or a right-click menu on the target frame.)
3. **One CC and one interrupt per class now;** a Rogue with Sap comes with Phase 9.

## 1. The engine (on every mob)
| State | What it does | Breaks | Length |
|---|---|---|---|
| Incapacitated (hex, sleep, trap) | stands still, out of the fight: no swings, no casts, no calls, no threat | any damage (a damage-over-time too) | 25 s (elite 18 s) |
| Stunned | can't act | never | 3-4 s |
| Feared | runs away from its fearer, then comes back | 3 hits, or the time | 6-8 s |
| Silenced (interrupt) | its cast is stopped; it can't cast again for a while | never | 4 s |
| Rooted / Slowed | (already in the game) | damage past a share of its health (roots) | as now |

Rules:
- **Diminishing returns:** the same kind of CC on the same mob within 18 s lasts half as long; the third time it's immune.
- **Who's immune:** bosses take no long CC (stuns and interrupts only); beasts and plants can be slept, people hexed, the unmade
  only turned (Paladin). Each mob's kind says what works on it; the target frame says "Immune" when you try.
- When CC ends the mob comes back for whoever CC'd it (threat on them), so a CCer re-applies or the tank picks it up.
- A CC'd mob neither calls for help nor answers a call.

## 2. One CC and one interrupt per class (GAME-ONLY names)
| Class | CC | Interrupt |
|---|---|---|
| Mage (fire) | **Ash Hex**: turns a person or beast to a smouldering ash statue, 25 s, breaks on damage | **Quench** (the existing ability) stops a cast and silences 4 s |
| Druid | **Sleep of the Wood**: a beast or plant sleeps 25 s, breaks on damage | **Thorn Lash**: stops a cast |
| Ranger | **Snare Trap**: a trap on the ground; the first mob to step in is held 20 s, breaks on damage | **Pin** (existing) gains a 2 s interrupt |
| Paladin | **Rebuke**: stun 4 s; **Turn the Unmade**: the Hollow and the greyed flee 12 s | **Censure** (existing) silences 3 s |
| Warrior | **Shout**: nearby mobs flee 6 s (breaks easily) | **Shield Bash**: stops a cast, silences 3 s |
| Mira (healer) | none | **Hush**: silences a caster 4 s, when you ask (or by herself on a sleep or a heal) |

## 3. Marks and the sims
- Marks over heads (skull, moon, cross) and on the target frame. You set them (keys above); a sim leading a dungeon run marks for you
  on a pull of three or more: skull on the caster or the hardest hitter, moon and cross on what its party can CC.
- Party sims: the tank opens on skull; the damage assists skull; a sim with a CC puts it on its mark at the pull and puts it back
  when it breaks; nobody hits a moon or a cross. Sims say it in Party: "hexing moon", "trap down", "moon's loose!".
- Interrupts: each sim with one watches for casts in its reach and kicks the dangerous ones (sleeps, heals, fear).

## 4. What you see
- The CC on the mob: an ash statue, a sleep's drifting motes, a trap's jaws, a stun's stars, a fear's flight; a ring under it with
  its time running down; its name plate dimmed.
- The target frame: the state and its seconds ("Ash Hex 18 s"); "Immune" when it won't take.
- Combat text: "Hexed", "Broken!", "Interrupted".

## 5. The Sealed Adit around it
Pulls of three or four with one caster; the Grey Breach's sleepers want interrupts; the Ember Vent's fear and menders want CC on the
menders; the Rail Hall's waves want stuns and a trap. With CC in, the dungeon gets deeper (more rooms, longer branches), its pulls
tuned so a group without CC struggles and a group that uses it doesn't.

## 6. Build order
1. **C1 the engine:** the states, the breaks, the diminishing returns, the immunities, threat on release; tests.
2. **C2 the abilities:** one CC and one interrupt per class, on the action bars, with their looks; Mira's Hush.
3. **C3 marks and sims:** the keys and markers; sims using CC and interrupts and marking on a sim-led run.
4. **C4 feedback:** the visuals, rings, frame text, combat text.
5. **C5 the Adit:** pulls re-tuned around CC, casters given real casts to interrupt, then the dungeon deepened.
