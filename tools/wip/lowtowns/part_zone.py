# ======================================================================================================================================
# The zone file
# ======================================================================================================================================
def L(name, at, r, text, label, view, pitch=15, zoom=14):
    return {"name": name, "text": text, "at": V(*at), "radius": r, "canonStatus": label, "view": V(*view), "viewPitch": pitch, "viewZoom": zoom}
LANDMARKS = [
    L("The Gate Ward", (-182, 4), 22, "Argentis's outer gate, standing open because nobody remembers how to shut it. The toll-house, the carters' yards, and the smell of the whole city coming the other way.",
      "CANON-EXPANDED: Argentis and its Lowtowns are CANON (world_bible.md Geography); the Gate Ward GAME-ONLY", (-214, -6), 12, 16),
    L("The Holding House", (-172, 46), 10, "Grey stone, barred windows, a heavy door. Children taken by warrant were kept here a night before they went up. It is empty now, and guarded anyway.",
      "GAME-ONLY (the warrant's children: peaks.json, ashrim.json)", (-172, 22), 14, 16),
    L("The Rusty Spoon", (-125, 18), 10, "A decrepit establishment known for watered-down ale and high-stakes games. The chairs are missing an inch on one leg, most of them.",
      "CANON: the Rusty Spoon (book1 ch.1:9); its rooms and people GAME-ONLY", (-125, -2), 12, 15),
    L("The Tenement Rows", (-100, -110), 40, "Brick terraces four storeys high, washing across the lanes, stoops, and puddles of questionable origin. Most of the Lowtowns live here, a family a room.",
      "CANON-EXPANDED: the Lowtowns' poverty and puddles (world_bible.md; book1 ch.1:143); the Rows GAME-ONLY", (-86, -66), 16, 18),
    L("The Soup-Hall", (-55, -122), 8, "A little chapel of no particular god, turned to feeding the Rows. The god's chair is gone; a copper stands in its place.",
      "GAME-ONLY", (-55, -146), 14, 14),
    L("The Chemical River", (8, 40), 20, "Black, slow and iridescent where the lamps catch it. Nothing natural lives in it. The barges are poled through it by people who try not to splash.",
      "CANON: the Chemical River through Argentis (book1 ch.15-16); its course here GAME-ONLY", (24, 10), 12, 16),
    L("The Sett Bridge", (river_x(-18), -18), 8, "A brick bridge over the black river, its setts worn to ridges by carts. The Ward-watch keeps its west end.",
      "GAME-ONLY", (-26, -30), 12, 14),
    L("The Rag Bridge", (river_x(96), 96), 8, "Rags hang off its parapets. Nobody hangs them and nobody takes them down.",
      "GAME-ONLY", (34, 86), 12, 14),
    L("The Docks", (-22, -108), 30, "Honest labour: barges poled in and out, sacks and bales on backs, no cranes. The quays are timber, tarred black, and they creak.",
      "CANON: honest labour at the docks (book1 ch.3:17); the quays GAME-ONLY", (-40, -84), 14, 18),
    L("The Drain Courts", (-128, 165), 32, "Closed courts round old drain mouths of red clay brick. By day they are quiet. By night something comes up out of them.",
      "CANON: the sewers' red clay brick (book1 ch.9:47) and the Weave-Eaters (book1 ch.1:251); the courts GAME-ONLY", (-118, 128), 18, 18),
    L("The Rust Market", (114, 66), 34, "Sector 4, under the Forge District's wall: covered stalls in a tangle of lanes, roasting skewered rat, cheap aether-spiced wine, and smog over everything. It does not sleep.",
      "CANON: the Rust Market, Sector 4 (book1 ch.3:3-9); its stalls and people GAME-ONLY", (88, 66), 14, 20),
    L("The Blind Seer's Antiquities", (118, 101), 6, "A clean shopfront of old things. Everyone in the Lowtowns knows not to ask about the back room.",
      "CANON: the Blind Seer's Antiquities (book1 ch.3:13); its back room is not entered", (118, 84), 10, 12),
    L("The Iron Teeth's Warehouse", (182, 96), 12, "A long brick warehouse with its doors shut. Up here the Iron Teeth are traders. What they are below is their own business.",
      "CANON: the Iron Teeth and their warehouse in the Rust Market (B1WB:26; book1 ch.7:21)", (160, 96), 12, 16),
    L("The Shard-Burners' Yard", (142, -114), 16, "The Lowtowns' dead-house. The poor buy back a shard of their own here, a night's light at a time.",
      "GAME-ONLY reading (the shards as burned memory, DUNGEON_DESIGN.md); spent shards as light are CANON (book1 ch.3:133-137)", (120, -100), 14, 16),
    L("The Outfall", (196, -205), 10, "Where the Forge District's drain comes out under the wall: a great arch of brick, an iron grating, and a smell like a hot coin. A sign says what everybody knows.",
      "GAME-ONLY (the Sludge-Tunnels' door, DUNGEON_DESIGN.md, not yet open)", (176, -205), 10, 14),
    L("The Forge District Gate", (203, 0), 10, "Shut, barred from the far side. Smoke rolls over the wall from the chimney stacks beyond.",
      "CANON: the Forge District's walls (book1 ch.3:5); the gate shut until that zone opens (GAME-ONLY)", (180, 0), 16, 18),
]
EXITS = [{"to": "zone.oakhaven", "name": "West road to Oakhaven", "at": V(-233, 2), "arrive": V(42, -269), "radius": 5}]
SPAWN = (-226, 2)
for name, (x, z) in [("arrival", SPAWN), ("recovery", (-180, -4)), ("companion", (-182, -6))]:
    spot_ok(name, x, z, roads=False)
for l in LANDMARKS: spot_ok("view of " + l["name"], l["view"]["x"], l["view"]["y"], roads=False)

ZONE = {
    "id": "zone.lowtowns",
    "displayName": "The Lowtowns",
    "subtitle": "The Lowtowns and the Rust Market, the shadowed districts at the foot of Argentis, where the poor burn shards for light",
    "canonStatus": "CANON-EXPANDED",
    "canonNote": "Canon (world_bible.md Geography; book1 ch.1, 3, 9, 13, 15-16, 20): Argentis is the capital, the Crown on the mountain white stone and gold, the Lowtowns at its base, sprawling and shadowed, filled with industry and poverty; the Rust Market, Sector 4, against the Forge District's walls under the smog of the High City's foundries, sells roasting skewered rat and cheap aether-spiced wine; the Rusty Spoon; the Iron Teeth; the Blind Seer's Antiquities; the Chemical River; red clay brick sewers; Weave-Eaters, against which salt and bone harpoons on amber rope serve and steel turns black and brittle; sparked children taken from the alleys; spent shards burned for light; Rule One of the Lowtowns. GAME-ONLY: every person, building, camp and quest here, the Holding House, the Shard-Burners' Yard and the reading that the shards are memory sifted from the slag (the Sludge-Tunnels' design). The game is set before Book 1: no purges, no Quarantine Wards, no burning of the Lowtowns; Seekers are only spoken of. Docs/ZONE_LOWTOWNS.md.",
    "size": 480,
    "levelMin": 15,
    "levelMax": 20,
    "biome": "lowtowns",
    "weather": [{"kind": "overcast", "weight": 2.5}, {"kind": "mist", "weight": 2}, {"kind": "rain", "weight": 3.5}, {"kind": "fair", "weight": 1}, {"kind": "storm", "weight": .8}],
    "worldMapPosition": {"x": 0.49, "y": 0.41},
    "worldMapNote": "At the foot of Argentis, just below the Crown, where the game map draws the capital at the centre of the land; the Lowtowns are not drawn on it, so the pin is provisional.",
    "flatRadius": 400,
    "hillHeight": 0,
    "seed": 9161,
    "lighting": {"sunPitch": 30, "sunYaw": -30, "sunIntensity": .95, "sunColor": "#E6C9A2", "ambientSky": "#7E786C", "ambientEquator": "#605850", "ambientGround": "#2A2622",
                 "fogColor": "#837667", "fogStart": 18, "fogEnd": 150, "skyTint": "#8E8476", "skyExposure": .9, "skyHaze": 2.2, "nightClear": .6},
    "spawns": {"player": V(*SPAWN), "playerFacing": 90, "companion": V(-182, -6), "recovery": V(-180, -4), "leash": 28, "enemies": []},
    "objectives": ["Find Toll-Reeve Edda Holt at the Gate Ward", "Learn where the warrant's children were taken"],
    "exits": EXITS,
    "roads": [{"name": n, "width": w, "points": [V(*p) for p in pts]} for n, w, pts in ROADS],
    "water": [{"name": "The Chemical River", "width": RIVER_W, "points": [V(*p) for p in RIVER], "depth": 2.4},
              {"name": "The Outfall run", "width": 6, "points": [V(*p) for p in OUTFALL], "depth": 1.2}],
    "waterTint": "#1B1A1E",
    "waterReflect": .35,
    "shapes": SHAPES,
    "landmarks": LANDMARKS,
    "props": PROPS,
    "camps": CAMPS,
    "life": {"villagers": len(NAMES), "mood": "wary", "names": NAMES,
             "critters": [{"kind": k, "center": V(x, z), "radius": r, "count": n} for k, x, z, r, n in CRITTERS],
             "residents": RESIDENTS,
             "households": [{"name": n, "house": h, "canonStatus": "GAME-ONLY", "stipend": 3, "needs": ["bread", "firewood"], "members": [{"name": m, "kin": k} for m, k in ms]} for n, h, ms in HOUSEHOLDS],
             "workshops": [{"who": w, "prop": p} for w, p in WORKSHOPS],
             "stories": [{"name": n, "lines": STORIES[n]} for n in NAMES]},
    "secrets": SECRETS,
    "nodes": NODES,
    "stations": STATIONS,
}

# ---- checks the tests make, made here first ----
names = set(NAMES) | {r["name"] for r in RESIDENTS}
for n in NAMES:
    if not (2 <= len(STORIES.get(n, [])) <= 3): problem(n + " has no two or three stories")
for r in RESIDENTS:
    if not (2 <= len(r["stories"]) <= 3): problem(r["name"] + " has no two or three stories")
for h in HOUSEHOLDS:
    if h[1] not in HOMES: problem("household " + h[0] + ": no terrace named " + h[1])
    for m, _ in h[2]:
        if m not in names: problem("household member " + m + " is nobody")
for w, p in WORKSHOPS:
    if not any(q.get("name") == p for q in PROPS): problem("workshop " + p + " is no prop")
lm = [l["name"] for l in LANDMARKS]
for s in SECRETS:
    for l in lm:
        for line in (s["clue"], s["hint"]):
            if l.lower() in line.lower(): problem(s["id"] + " names the landmark " + l)
    if s["name"] in lm: problem(s["id"] + " has a landmark's name")
    if s["clue"] == s["hint"]: problem(s["id"] + ": clue and hint are one line")
for c in CAMPS:
    if not (15 <= c["levelMin"] <= c["levelMax"] <= 20): problem(c["name"] + " is outside 15-20")
ZONE_NAMES = {p.get("name") for p in PROPS}
