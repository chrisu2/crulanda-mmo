"""
make_forge.py: writes the Forge District (zone.forge, Docs/ZONE_FORGE_DISTRICT.md; mobs 19-22, the cap 20, no dungeon) and checks it,
and opens the Lowtowns' Forge District Gate onto it.

Writes (byte for byte the same each run; JSON with two-space indents and CRLF, as the other content files):
  EncounterContent/Zones/forge.json     the zone: the Tithe Gate, the Rust & Ruin, the Forge Warrens, the West Fire Pits, the Great
                                        Crucibles, the River Aureum and the Molten Vein, the Foundry, the Spoke and its yard, the East Fire
                                        Pits, the Slag Fields; people; camps 19-22 (no elites yet: see the doc); nodes; stations; secrets
  EncounterContent/Quests/forge.json    the breadcrumb from the Lowtowns, the main chain of four, four side quests, the Chronicle pages
  EncounterContent/Lore/forge.json      the zone's chapter of the book of lore
  EncounterContent/Zones/lowtowns.json  (edited in place) the exit through the Forge District Gate, the gate open, its sign
Run from anywhere: python tools/wip/forge/make_forge.py. It stops on the first problem its checks find.
Lore labels: every line says CANON (with book and chapter), CANON-EXPANDED, GAME-ONLY or PROVISIONAL. No machines (Chris, 2026-10-11).
"""
import json, math, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent'))
HALF = 180
def V(x, z): return {"x": round(x, 2), "y": round(z, 2)}
def problem(msg): sys.exit('PROBLEM: ' + msg)
def seg_dist(p, a, b):
    ax, az = a; bx, bz = b; px, pz = p; dx, dz = bx - ax, bz - az; L = dx * dx + dz * dz
    t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (pz - az) * dz) / L))
    return math.hypot(px - ax - t * dx, pz - az - t * dz)
def path_dist(p, pts): return min(seg_dist(p, pts[i], pts[i + 1]) for i in range(len(pts) - 1))
def dist(a, b): return math.hypot(a[0] - b[0], a[1] - b[1])

# ======================================================================================================================================
# The ground: roads, the molten channels, the buildings
# ======================================================================================================================================
ROADS = [
    ("Gate Street", 7, [(-180, 0), (-150, 2), (-120, 2), (-100, 2), (-80, 2), (-60, 3), (-45, 1)]),
    ("Warrens Lane", 5, [(-120, 2), (-128, -40), (-132, -90), (-120, -140), (-110, -172)]),
    ("Aureum Crossing", 5, [(-132, -90), (-95, -95), (-58, -95), (-36, -70), (-28, -42)]),
    ("Slag Road", 5, [(-28, -42), (10, -48), (40, -52), (59, -55), (85, -58), (105, -90), (112, -128)]),
    ("Foundry Lane", 5, [(-52, 3), (-48, 50), (-42, 100), (-38, 150)]),
    ("Crucible Way", 6, [(-42, 100), (0, 106), (24, 110), (60, 112), (95, 108)]),
    ("East Road", 5, [(95, 108), (118, 62), (128, 22), (110, -20), (85, -58)]),
]
AUREUM = [(-100, 180), (-90, 90), (-80, 0), (-60, -90), (-30, -180)]
VEIN = [(20, 180), (25, 90), (40, 30), (60, -60), (90, -180)]
CHANNEL_W = 8
SPOKE = (-12, 0); YARD_R = 26

PROPS = []
def P(kind, name, x, z, rot=0, size=None, variant=0, **kw):
    p = {"kind": kind, "name": name, "at": V(x, z), "rotation": rot}
    if size: p["size"] = V(*size)
    if variant: p["variant"] = variant
    p.update(kw); PROPS.append(p); return p
P("molten_channel", "The River Aureum", 0, 0, size=(CHANNEL_W, 0), points=[V(*q) for q in AUREUM], canonStatus="CANON: the River Aureum (the author's map, maps/forge_district_map.png); molten metal behind kerbs GAME-ONLY")
P("molten_channel", "The Molten Vein", 0, 0, size=(CHANNEL_W, 0), points=[V(*q) for q in VEIN], canonStatus="CANON: the Molten Vein (the author's map); its course here GAME-ONLY")
BRIDGES = [("The Aureum Bridge", -80, 2, 90), ("The Low Aureum Bridge", -58.3, -95, 90), ("The North Vein Bridge", 23.9, 110, 90), ("The Slag Bridge", 58.9, -55, 90)]
for n, x, z, r in BRIDGES: P("forge_bridge", n, x, z, r, size=(16, 7))
P("forge_gate", "The Tithe Gate", -166, 0, 90, size=(8, 0))
P("inn", "The Rust & Ruin", -132, 24, 0, size=(16, 10), variant=5)   # its own house (ZoneBuilder.RustRuin; Chris 2026-10-19: "Fully its own now")
P("the_spoke", "The Spoke", SPOKE[0], SPOKE[1], 0, size=(14, 48), canonStatus="CANON: the Spoke, a rune-carved stone monolith (world_bible.md Magic), raised in the Forge District's Foundry (book1 ch.7:237); its look GAME-ONLY from the author's map")
P("spoke_yard", "The Spoke's yard", SPOKE[0], SPOKE[1], 90, size=(YARD_R, 0))
# the Forge Warrens: smelters' lodgings either side of Warrens Lane
HOMES = []
for i, (x, z, r) in enumerate([(-150, -28, 270), (-152, -56, 270), (-154, -84, 270), (-106, -34, 90), (-108, -62, 90), (-160, 30, 0), (-104, 28, 0)]):
    HOMES.append(P("lodging_row", "Smelters' lodgings", x, z, r, size=(18, 7), variant=i))
P("fire_pit", "The west fire pit", -160, -118, size=(9, 9)); P("fire_pit", "The small west fire pit", -146, -134, size=(7, 7))
P("crucible", "The south-west Great Crucible", -92, -150, 0, size=(14, 14))
P("furnace", "Warrens furnace", -164, -150, 0, size=(7, 7), variant=1)
# the Foundry
for x, z, r in [(-68, 80, 270), (-18, 75, 90), (-18, 140, 90), (-70, 135, 270)]: P("foundry_hall", "Foundry hall", x, z, r, size=(24, 12))
P("furnace", "Foundry furnace", -64, 40, 0, size=(7, 7)); P("furnace", "Foundry furnace", -64, 108, 0, size=(6, 6), variant=2)
# the east bank
P("crucible", "The Great Crucible", 115, 125, 0, size=(16, 16))
P("fire_pit", "The east fire pit", 140, 38, size=(9, 9)); P("fire_pit", "The small east fire pit", 140, 8, size=(7, 7))
P("furnace", "East furnace", 150, 75, 0, size=(7, 7)); P("furnace", "East furnace", 70, 145, 0, size=(7, 7), variant=1)
for x, z, s in [(92, -122, 10), (132, -142, 9), (145, -100, 9), (100, -158, 8)]: P("slag_heap", "Slag heap", x, z, size=(s, s))
# small things
for x, z in [(-150, 8), (-112, 8), (-88, 8), (-56, 9), (-38, 98), (12, 114), (34, 104), (70, 106), (122, 50), (116, 0), (70, -62), (16, -43), (-44, -64)]: P("cinder_lamp", "Cinder lamp", x, z)
for x, z in [(-56, 70), (-6, 90), (-6, 126), (100, 140), (-80, -158), (130, 115)]: P("ingot_stack", "Ingots", x, z, 20)
STATIONS = [{"kind": "forge", "name": "Forge: the Warrens' hearth", "at": V(-140, 10), "rotation": 180, "variant": 0, "canonStatus": "GAME-ONLY (a smith's hearth, the bellows worked by hand)"},
            {"kind": "fire", "name": "The Tithe-booth's brazier", "at": V(-156, -12), "rotation": 0, "canonStatus": "GAME-ONLY"}]
# ruins of fallen lodgings, where the slag-iron and the cinder-beams are found
RUINS = [(-140, 60), (-112, 70), (-150, 120), (-118, 140), (-56, -36), (82, 70), (150, -40), (40, -128), (150, 150)]
for x, z in RUINS: P("ruin", "Fallen lodging", x, z, size=(6, 6))

BUILD = {"lodging_row", "foundry_hall", "furnace", "crucible", "the_spoke", "inn"}
def half_of(p):
    s = (p.get("size") or {"x": 0, "y": 0}); k = p["kind"]
    if k in ("furnace", "crucible", "the_spoke"): return (s["x"] / 2 + 1.5, s["x"] / 2 + 1.5)
    return (s["x"] / 2 + .5, s["y"] / 2 + .5)
def in_building(x, z, pad=0):
    for p in PROPS:
        if p["kind"] not in BUILD: continue
        hx, hz = half_of(p); r = math.radians(p["rotation"]); c, s = math.cos(r), math.sin(r)
        dx, dz = x - p["at"]["x"], z - p["at"]["y"]
        if abs(dx * c - dz * s) <= hx + pad and abs(dx * s + dz * c) <= hz + pad: return p["name"]
    return None
def near_channel(x, z, pad): return path_dist((x, z), AUREUM) < CHANNEL_W / 2 + 2 + pad or path_dist((x, z), VEIN) < CHANNEL_W / 2 + 2 + pad
def near_road(x, z, pad): return any(path_dist((x, z), pts) < w / 2 + pad for _, w, pts in ROADS)
def in_yard(x, z, pad=0): return abs(dist((x, z), SPOKE) - YARD_R) < 1.5 + pad
BAD = []
for p in PROPS:
    if p["kind"] in BUILD:
        x, z = p["at"]["x"], p["at"]["y"]; hx, hz = half_of(p)
        if near_channel(x, z, max(hx, hz) - 1): BAD.append(p["name"] + " at " + str((x, z)) + " is in a molten channel")
        for q in PROPS:
            if q is p or q["kind"] not in BUILD: continue
            if dist((x, z), (q["at"]["x"], q["at"]["y"])) < 4: BAD.append(p["name"] + " overlaps " + q["name"])
        # a building is clear of the roads by its depth (corners checked)
        r = math.radians(p["rotation"]); c, s = math.cos(r), math.sin(r)
        for ax, az in [(-hx, -hz), (hx, -hz), (-hx, hz), (hx, hz), (0, 0)]:
            wx, wz = x + ax * c + az * s, z - ax * s + az * c
            if near_road(wx, wz, .2) and p["kind"] != "the_spoke": BAD.append(p["name"] + " at " + str((x, z)) + " stands in a road")
for n, x, z, r in BRIDGES:
    if not near_road(x, z, 0): BAD.append(n + " is on no road")

# ======================================================================================================================================
# Camps (19-22). No elites in this pass (the elites need their moves and a loot band past 20: Docs/ZONE_FORGE_DISTRICT.md question 8).
# Beasts keep to 20 (the hide tables stop at 20).
# ======================================================================================================================================
def C(name, mob, tag, look, x, z, lo, hi, n, r=9, **kw):
    c = {"name": name, "mob": mob, "tag": tag, "look": look, "center": V(x, z), "radius": r, "count": n, "levelMin": lo, "levelMax": hi, "respawn": 90}
    c.update(kw); return c
GO = "GAME-ONLY"
CAMPS = [
    C("Slag-hounds on the Warrens' tips", "Slag-hound", "slaghound", "wolf", -164, -42, 19, 20, 4, canonStatus=GO + ": feral dogs of the slag-heaps (slag-piles are CANON, book1 ch.16:349)"),
    C("Slag-hounds by the west pits", "Slag-hound", "slaghound", "wolf", -168, -160, 19, 20, 4, canonStatus=GO),
    C("Blacklegs at the west pits", "Blackleg", "blackleg", "deserter", -138, -116, 19, 20, 4, canonStatus=GO + ": thugs the foremen hire against the smelters"),
    C("Crucible overseers (south-west)", "Foundry overseer", "overseer", "warden", -88, -126, 20, 21, 3, r=8, canonStatus="CANON-EXPANDED: the Council's foremen and overseers (DUNGEON_DESIGN.md: shared freely); these GAME-ONLY"),
    C("Foundry overseers", "Foundry overseer", "overseer", "warden", -30, 50, 20, 21, 4, canonStatus="CANON-EXPANDED: the Spoke raised in the Foundry (book1 ch.7:237); the overseers GAME-ONLY"),
    C("The Spoke's leak", "Weave-Eater", "weaveeater", "weaveeater", 2, 60, 21, 22, 3, r=8, canonStatus="CANON: Weave-Eaters, born of the High City's magical pollution (book1 ch.1:251); drawn to the Spoke GAME-ONLY"),
    C("Iron Pact at the yard gate", "Iron Pact soldier", "pactsoldier", "outrider", -60, -16, 21, 22, 4, r=8, canonStatus="CANON faction: the Iron Pact (world_bible.md); as the Council's guard here GAME-ONLY (DUNGEON_DESIGN.md: shared freely)"),
    C("Iron Pact crossbowmen in the yard", "Iron Pact crossbowman", "pactbow", "outrider", -26, -14, 21, 22, 3, r=4, canonStatus="CANON faction: the Iron Pact (world_bible.md); GAME-ONLY camp"),
    C("Blacklegs at the east pits", "Blackleg", "blackleg", "deserter", 112, 34, 20, 21, 4, canonStatus=GO),
    C("Slag-hounds in the Slag Fields", "Slag-hound", "slaghound", "wolf", 122, -118, 20, 20, 5, r=11, canonStatus=GO),
    C("Crucible overseers (north-east)", "Foundry overseer", "overseer", "warden", 92, 136, 21, 22, 4, r=8, canonStatus="CANON-EXPANDED: the Council's foremen; GAME-ONLY camp"),
    C("Iron Pact on the Crucible Way", "Iron Pact soldier", "pactsoldier", "outrider", 58, 84, 21, 22, 3, r=8, canonStatus="CANON faction: the Iron Pact (world_bible.md); GAME-ONLY camp"),
    # the rares last (a visit without them moves no other camp's mobs)
    C("Old Clinker", "Old Clinker", "clinker", "wolf", 158, -150, 20, 20, 1, r=6, rare=.3, social="solitary", canonStatus=GO),
    C("The Spoke-Leech", "The Spoke-Leech", "spokeleech", "weaveeater", -60, 165, 22, 22, 1, r=6, rare=.25, social="solitary", canonStatus="CANON-EXPANDED: Weave-Eaters (book1 ch.1:251); the Spoke-Leech GAME-ONLY"),
]
for c in CAMPS:
    x, z = c["center"]["x"], c["center"]["y"]
    if not (19 <= c["levelMin"] <= c["levelMax"] <= 22): BAD.append(c["name"] + " is outside 19-22")
    if c["look"] == "wolf" and c["levelMax"] > 20: BAD.append(c["name"] + ": beasts keep to 20")
    if in_building(x, z, 1): BAD.append(c["name"] + " is in " + in_building(x, z, 1))
    if near_channel(x, z, c["radius"] * .5): BAD.append(c["name"] + " is in a molten channel")
    if max(abs(x), abs(z)) > HALF - 12: BAD.append(c["name"] + " is at the wall")

# ======================================================================================================================================
# Nodes: ten slag-iron heaps and eight cinder-beams, each by a fallen lodging; ten soot-wort (tier 90, the Lowtowns' top tier)
# ======================================================================================================================================
NODES = []
for i, (x, z) in enumerate(RUINS):
    kinds = ["node.slagiron", "node.cinderbeam"] if i < 8 else ["node.slagiron", "node.slagiron"]
    for k, side in zip(kinds, (-1, 1)):
        NODES.append({"node": k, "at": V(x + side * 5.5, z), "rotation": (i * 47) % 360})
HERBS = [(-170, 90), (-122, 166), (-170, -100), (-84, -40), (-8, 160), (8, 30), (80, -22), (150, -10), (160, 100), (60, -150)]
for i, (x, z) in enumerate(HERBS): NODES.append({"node": "node.sootwort", "at": V(x, z), "rotation": (i * 61) % 360})
for nd in NODES:
    x, z = nd["at"]["x"], nd["at"]["y"]
    if in_building(x, z, 1): BAD.append("node at " + str((x, z)) + " is in " + in_building(x, z, 1))
    if near_road(x, z, 1): BAD.append("node at " + str((x, z)) + " is in a road")
    if near_channel(x, z, 1): BAD.append("node at " + str((x, z)) + " is in a molten channel")
    if in_yard(x, z, 1) or dist((x, z), SPOKE) < 12: BAD.append("node at " + str((x, z)) + " is in the Spoke's yard wall")
    for c in CAMPS:
        if dist((x, z), (c["center"]["x"], c["center"]["y"])) < c["radius"] * 1.42 + 2: BAD.append("node at " + str((x, z)) + " is in the camp " + c["name"])
    for q in NODES:
        if q is not nd and dist((x, z), (q["at"]["x"], q["at"]["y"])) < 5: BAD.append("nodes too close at " + str((x, z)))
    for p in PROPS:
        if p["kind"] in ("fire_pit", "slag_heap", "cinder_lamp", "ingot_stack") and dist((x, z), (p["at"]["x"], p["at"]["y"])) < 6: BAD.append("node at " + str((x, z)) + " is on " + p["name"])
if sum(n["node"] == "node.slagiron" for n in NODES) != 10 or sum(n["node"] == "node.cinderbeam" for n in NODES) != 8 or sum(n["node"] == "node.sootwort" for n in NODES) != 10: BAD.append("nodes: not 10 ore, 8 windfalls, 10 herbs")

# ======================================================================================================================================
# Landmarks, people, secrets
# ======================================================================================================================================
def L(name, at, r, text, label, view, pitch=15, zoom=14):
    return {"name": name, "text": text, "at": V(*at), "radius": r, "canonStatus": label, "view": V(*view), "viewPitch": pitch, "viewZoom": zoom}
LANDMARKS = [
    L("The Tithe Gate", (-166, 0), 12, "The gate from the Lowtowns, open by day. The Copper-Tithe keep a booth on the inside and write down everyone who comes through, and what they carry, and what they owe.",
      "CANON-EXPANDED: the Copper-Tithe (book1 ch.1:145, ch.24:140); their gate GAME-ONLY", (-150, -14), 14, 16),
    L("The Rust & Ruin", (-132, 24), 9, "A tavern behind an iron-banded oak door, down a lane where the light never quite reaches. Stale beer, smoke, and nobody asking your name.",
      "CANON: the Rust & Ruin, its iron-banded oak door (book1 ch.24:81-114); here in the Forge District GAME-ONLY", (-132, 4), 12, 15),
    L("The Forge Warrens", (-130, -60), 30, "The smelters' lodgings: low stone rows under iron roofs, a family a room, the gutters black with soot. Valen grew up in gutters like these.",
      "CANON: the Forge District Warrens (book1 ch.3:159) and Valen's gutters (book1 ch.25:175); the rows GAME-ONLY", (-112, -40), 15, 18),
    L("The West Fire Pits", (-155, -125), 14, "Pits of coals that never go out, ringed with blackened stone. The Blacklegs warm their hands here and watch the lodgings.",
      "CANON: the Fire Pits (the author's map); their use GAME-ONLY", (-136, -104), 14, 15),
    L("The River Aureum", (-80, 40), 16, "A channel of molten gold-bright metal running down from the furnaces, behind a kerb of stone. The heat comes off it like a wall.",
      "CANON: the River Aureum (the author's map); GAME-ONLY course", (-100, 30), 13, 15),
    L("The Foundry", (-40, 100), 30, "Long soot-stone halls with their arches glowing, the heavy Foundry where the Council raises the Spoke. Overseers with tally-sticks on every door.",
      "CANON: the Foundry District, where the Spoke is being built (book1 ch.7:237); its halls GAME-ONLY", (-60, 100), 15, 18),
    L("The Spoke", SPOKE, 14, "A rune-carved monolith of black stone, taller than anything in the district, its runes glowing like coals. It hums. The air near it tastes of copper.",
      "CANON: the Spoke, a rune-carved stone monolith (world_bible.md Magic; book1 ch.7:237); its look GAME-ONLY from the author's map", (-56, 6), 22, 30),
    L("The Molten Vein", (40, 30), 16, "The second channel of molten metal, out of the north and away south past the Spoke. The stone bridges over it are the only ways across.",
      "CANON: the Molten Vein (the author's map); GAME-ONLY course", (24, 20), 13, 15),
    L("The Great Crucible", (115, 125), 12, "A stone bowl the size of a house, molten at the brim. Gangs pour it by ladle and by hand, a little at a time, all day and all night.",
      "CANON: the Great Crucible (the author's map); poured by hand GAME-ONLY", (90, 112), 15, 18),
    L("The East Fire Pits", (138, 24), 12, "More pits of coals, and more Blacklegs round them. The smelters walk wide of this side.",
      "CANON: the Fire Pits (the author's map); GAME-ONLY", (118, 10), 14, 15),
    L("The Slag Fields", (120, -125), 30, "Heaps of black slag cracked with ember, tipped out of the furnaces for a hundred years. The hounds live in them.",
      "CANON-EXPANDED: slag-piles (book1 ch.16:349); the fields GAME-ONLY", (98, -96), 15, 18),
]
EXITS = [{"to": "zone.lowtowns", "name": "West through the Tithe Gate to the Lowtowns", "at": V(-172, 0), "arrive": V(192, 0), "radius": 5}]
SPAWN = (-158, 1)

def R(name, title, x, z, stories, role="stranger", label=GO, **kw):
    r = {"name": name, "role": role, "title": title, "place": "", "at": V(x, z), "canonStatus": label, "stories": stories}; r.update(kw); return r
RESIDENTS = [
    R("Hesta Wode", "Copper-Tithe Tally-Woman", -156, -7, [
        "The Tithe takes a copper in ten off every pour for the Concord, and the Concord takes the other nine. I write both down. Somebody should.",
        "I've a son in the Warrens and a brother in the Foundry. One of them coughs and one of them doesn't talk about his work. I don't know which is worse.",
        "We're not thieves, whatever they say in the Lowtowns. Thieves get to keep what they take."],
      label="CANON-EXPANDED: the Copper-Tithe, sick of the Concord's taxes (book1 ch.24:140); Hesta GAME-ONLY"),
    {"name": "Berrow Orr", "role": "innkeeper", "title": "Keeper of the Rust & Ruin", "place": "bar", "works": True, "canonStatus": "GAME-ONLY: the books give the Rust & Ruin no keeper's name", "stories": [
        "The door's iron-banded because the last one got kicked in. The one before that too. I stopped counting doors and started counting bands.",
        "Beer's stale because the cellar's warm. Everything's warm here. Even the rats sweat.",
        "The Tithe drink here, the smelters drink here, and once a week an overseer comes in and nobody drinks at all till he goes."]},
    R("Old Aelric", "Crucible-Hand", -116, -126, [
        "Forty years at the pour. I've the burns to show it, up both arms like sleeves. The Crucible doesn't care who you are.",
        "When I was a boy the halls made pots and nails. Now they make whatever it is they're making up by the monolith, and nobody asks.",
        "They take the bright ones. Children who light a lamp by looking at it. They write them on a list and then one day they're gone up."]),
    R("Elfa Raike", "Herb-Woman of the Warrens", -136, -72, [
        "Ash-lung. Half the Warrens has it. Soot-wort eases it. Nothing cures it but leaving, and nobody leaves.",
        "My mother said the air was sweet here once, before the halls were built over the houses. I don't believe her. I'd like to.",
        "Since they started on the monolith the coughing's worse at night. I don't know why. The night air should be cleaner."]),
    R("Ade Heath", "Bellows-Boy", -100, 12, [
        "I work the bellows at the Warrens' hearth. Up, down, up, down, all day. My arms are bigger than my brother's and he's fourteen.",
        "The Blacklegs take things off you if you're small. Bellows, bread, boots. They took my brother's boots and he walked home on the cinders.",
        "One day I'll have my own hearth. A little one. Nails and hinges. Nothing for the monolith."], role="stranger"),
    R("Hamo Glaze", "Slag-Glass Blower", 104, 82, [
        "There's glass in the slag if you know where to look. Black glass, green glass, once a blue one. I blow it into beads for the Tithe-women.",
        "The overseers don't mind me. I'm not making anything they want. That's the safest thing to be, here.",
        "I made a bead the colour of the sea once. Nobody here had seen the sea. They all said it was the right colour anyway."]),
]
SECRETS = [
    {"id": "secret.forge.slag-top", "name": "On Top of the Slag", "kind": "vista", "at": V(160, -60), "radius": 6,
     "text": "From the top of the heap the whole district lies below in the smog: the two bright rivers, the halls glowing, and the monolith in the middle of it all, humming. Far off in the north-east, over everything, the Crown.",
     "clue": "Out in the east, where the black heaps stand highest, climb up and look back at everything.",
     "hint": "Hamo Glaze says if you climb the tallest heap out east you can see the whole district at once. He goes up there to look for blue glass, and to look.",
     "xp": 220, "canonStatus": "CANON-EXPANDED: the Spoke (world_bible.md) and the Crown (world_bible.md Geography); the lookout GAME-ONLY"},
    {"id": "secret.forge.tithe-stash", "name": "A Runner's Stash", "kind": "cache", "at": V(-168, 150), "prompt": "Search the loose stones",
     "text": "Behind a loose stone at the foot of the district wall: a tin of coppers, a twist of salt and a scrap of the Tithe's own tally, every number crossed out.",
     "clue": "In the far north-west corner, where the district wall comes down to the ground, one stone is looser than the rest.",
     "hint": "Hesta Wode says the Tithe's runners used to hide their takings in the wall up at the north-west corner, before she came. She never went to look.",
     "xp": 220, "gold": 45, "canonStatus": "CANON-EXPANDED: the Copper-Tithe (book1 ch.24:140); the stash GAME-ONLY"},
    {"id": "secret.forge.chalk-w", "name": "A Chalked Letter", "kind": "note", "at": V(-46, -24), "prompt": "Look at the chalk",
     "text": "Low on the yard's wall, where a child would sit, somebody has chalked a W. Under it, smaller: UP.",
     "clue": "Outside the ring wall round the monolith, low down on its west side, a child's letter in chalk.",
     "hint": "Old Aelric says the children they take sit by the ring wall's west side while they wait, and somebody chalks on it. He won't go and read it.",
     "xp": 220, "document": "doc.forge.chalk_w", "canonStatus": "GAME-ONLY (Wren: peaks.json, lowtowns.json)"},
]
for s in SECRETS:
    x, z = s["at"]["x"], s["at"]["y"]
    for nd in NODES:
        if dist((x, z), (nd["at"]["x"], nd["at"]["y"])) < 6: BAD.append(s["id"] + " is by a node")
    if in_building(x, z, 1) or near_channel(x, z, 1): BAD.append(s["id"] + " is in a building or a channel")
    for l in LANDMARKS:
        for line in (s["clue"], s["hint"]):
            if l["name"].lower() in line.lower(): BAD.append(s["id"] + " names the landmark " + l["name"])
    if s["clue"] == s["hint"]: BAD.append(s["id"] + ": clue and hint are one line")
for st in STATIONS:
    x, z = st["at"]["x"], st["at"]["y"]
    if near_road(x, z, .5) or in_building(x, z, 1) or near_channel(x, z, 1): BAD.append(st["name"] + " is in a road, a building or a channel")
    for nd in NODES:
        if dist((x, z), (nd["at"]["x"], nd["at"]["y"])) < 4: BAD.append(st["name"] + " is by a node")
for r in RESIDENTS:
    if "at" in r and r.get("role") != "innkeeper":
        x, z = r["at"]["x"], r["at"]["y"]
        if in_building(x, z, .5) or near_channel(x, z, 1): BAD.append(r["name"] + " stands in a building or a channel")
for name, (x, z) in [("arrival", SPAWN), ("recovery", (-150, -6)), ("companion", (-152, -8))]:
    if in_building(x, z, .5): BAD.append(name + " is in a building")
if BAD: problem('\n  '.join(BAD))

ZONE = {
    "id": "zone.forge",
    "displayName": "The Forge District",
    "subtitle": "The Forge District, behind the Lowtowns' east wall: furnaces, crucibles and rivers of molten metal round the Council's Spoke",
    "canonStatus": "CANON-EXPANDED",
    "canonNote": "Canon (book1 ch.1:147, ch.2:19 and 197, ch.3:7 and 149-159, ch.7:237-299, ch.9:5, ch.16:131 and 349, ch.17:296-316, ch.24:7 and 81-140, ch.25:175; world_bible.md Magic; the author's map, maps/forge_district_map.png): the Forge District beyond the Rust Market's wall, its soot, sulfur and smelting copper, its manufactories like canyon walls with guard towers, its Warrens and gutters, the Foundry where the Spoke, a rune-carved stone monolith, is raised; the Council's Resonance Target List of the Forge District; the Copper-Tithe; the Rust & Ruin; slag-piles; the River Aureum, the Molten Vein, the Great Crucibles and the Fire Pits of the author's map. Left out by Chris's rule (CRULANDA_CANON.md): the iron walkers of Valen's account and the turning wheels and chains the map draws; and the Seekers (the Transit Hub's). GAME-ONLY: every person, building, camp and quest here. The game is set shortly before Book 1. Docs/ZONE_FORGE_DISTRICT.md.",
    "size": HALF * 2,
    "levelMin": 19,
    "levelMax": 22,
    "biome": "forge",
    "weather": [{"kind": "overcast", "weight": 3}, {"kind": "mist", "weight": 2}, {"kind": "ashsquall", "weight": 1.5}, {"kind": "rain", "weight": 1.5}, {"kind": "fair", "weight": .8}],
    "worldMapPosition": {"x": .5, "y": .46},
    "worldMapNote": "Behind the Lowtowns' east wall, at the foot of Argentis under the floating city; the game map does not draw it, so the pin is provisional (placed 2026-10-19 where its label clears the Crown's and the Lowtowns', WorldMapLabelTests).",
    "flatRadius": 400,
    "hillHeight": 0,
    "seed": 2219,
    "squallTint": "#3A302A",
    "squallText": "A hot wind comes off the furnaces carrying soot and grit, and the light goes brown.",
    "lighting": {"sunPitch": 28, "sunYaw": -30, "sunIntensity": .85, "sunColor": "#E8B47E", "ambientSky": "#7A6656", "ambientEquator": "#5E4A3C", "ambientGround": "#2A1E18",
                 "fogColor": "#6E5A48", "fogStart": 16, "fogEnd": 135, "skyTint": "#8A6E58", "skyExposure": .85, "skyHaze": 2.4, "nightClear": .4, "nightFog": "#3A2A22"},
    "spawns": {"player": V(*SPAWN), "playerFacing": 90, "companion": V(-152, -8), "recovery": V(-150, -6), "leash": 28, "enemies": []},
    "objectives": ["Find the Copper-Tithe's tally-woman at the Tithe Gate", "Follow the Council's list to the Spoke"],
    "exits": EXITS,
    "roads": [{"name": n, "width": w, "points": [V(*p) for p in pts]} for n, w, pts in ROADS],
    "landmarks": LANDMARKS,
    "props": PROPS,
    "camps": CAMPS,
    "life": {"villagers": 0, "mood": "afraid", "names": [],
             "critters": [{"kind": "crow", "center": V(120, -120), "radius": 18, "count": 4}, {"kind": "cat", "center": V(-130, -60), "radius": 16, "count": 3}],
             "residents": RESIDENTS, "households": [], "workshops": [], "stories": []},
    "secrets": SECRETS,
    "nodes": NODES,
    "stations": STATIONS,
}

# ======================================================================================================================================
# Quests (no quest items: talk, kill and visit, the pages given as they are read)
# ======================================================================================================================================
DOCUMENTS = [
    {"id": "doc.forge.tithe_book", "title": "The Tithe's Gate-Book", "text": "From the Copper-Tithe's book at the Tithe Gate, in Hesta Wode's hand:\n\nCarters, eleven. Smelters back from the Lowtowns, forty-two. Overseers, three, owe nothing (they never do). One stranger from the Lowtowns, sent by the toll-reeve Holt, asking after a child. Let in. Owes: a favour.",
     "canonStatus": "CANON-EXPANDED: the Copper-Tithe (book1 ch.24:140); the book GAME-ONLY"},
    {"id": "doc.forge.resonance_list", "title": "The Resonance List", "text": "Project Spoke. Primary Resonance Target List - The Forge District.\n\nA column of names, each with an address, an age and a grade. Most are struck through, and beside the struck names, in a neater hand: Moved up.\n\nNear the bottom, added later: Wren. Oakhaven. Age 9. Grade: High. Moved up.",
     "canonStatus": "CANON: the Resonance Target List of the Forge District (book1 ch.3:149-159); Wren's line GAME-ONLY"},
    {"id": "doc.forge.foundry_pass", "title": "A Foundry Pass", "text": "A square of stamped copper on a cord: the Council's seal on one side, a number on the other. Whoever holds it walks through the Foundry and into the yard, and nobody asks.",
     "canonStatus": "GAME-ONLY"},
    {"id": "doc.forge.aelric_burns", "title": "Aelric's Tally of Pours", "text": "Notches on a stick, kept forty years: one for every pour Old Aelric worked at the Crucible. Near the end the notches change. They're not pours any more, he says. They're children he saw go through the Foundry.",
     "canonStatus": "GAME-ONLY"},
    {"id": "doc.forge.warrens_cough", "title": "Elfa's Slate", "text": "A slate by Elfa Raike's door: who coughs, how long, whether soot-wort helped. The column for 'better' is short. The column for 'worse since the monolith' is long.",
     "canonStatus": "GAME-ONLY"},
    {"id": "doc.forge.chalk_w", "title": "The Chalked W", "text": "Low on the Spoke's yard wall, in chalk: a W, and under it, smaller, UP. The same hand as the W scratched into the Holding House's doorstep in the Lowtowns.",
     "canonStatus": "GAME-ONLY (Wren: peaks.json, lowtowns.json)"},
]
def Q(id, title, kind, giver, turn, level, req, label, summary, offer, progress, complete, steps, xp, gold, zone="zone.forge"):
    q = {"id": id, "title": title, "kind": kind, "giver": giver, "turnIn": turn, "zone": zone, "level": level}
    if req: q["requires"] = req
    q.update({"canonStatus": label, "summary": summary, "offer": offer, "progress": progress, "complete": complete, "steps": steps, "rewards": {"xp": xp, "gold": gold}}); return q
def S(text, *objs): return {"text": text, "objectives": list(objs)}
def talk(who, text, **kw): o = {"type": "talk", "zone": "zone.forge", "target": who, "text": text}; o.update(kw); return o
def kill(tag, n, text, **kw): o = {"type": "kill", "zone": "zone.forge", "target": "mob." + tag + ".forge.*", "count": n, "text": text}; o.update(kw); return o
def visit(x, z, r, text, say, **kw): o = {"type": "visit", "zone": "zone.forge", "text": text, "at": V(x, z), "radius": r, "say": say}; o.update(kw); return o
QUESTS = [
    Q("main.forge.0", "The Forge Gate", "main", "Edda Holt", "Hesta Wode", 19, ["main.lowtowns.5"], "GAME-ONLY (Wren and the soldier's boy); the Forge District and the Copper-Tithe CANON (book1 ch.3:7, ch.24:140)",
      "Edda Holt says the Forge District's gate is open to carters now. The Copper-Tithe keep a book there of who goes in. Ask their tally-woman.",
      "Edda Holt has the toll-book shut on her knee. \"Up, the ledger said. Up's past the Crown, and there's no road up from here.\" She nods east, at the smoke over the wall. \"But the Forge District's gate is open to carters this week. The Copper-Tithe keep a book on the far side, the same as mine. Better than mine. A woman called Wode. Tell her I sent you. She'll let you in for that. Then she'll want something.\"",
      "\"The Forge District Gate, at the top of the market. Mind your purse past it.\"",
      "The tally-woman writes your name before you've finished saying it. \"Holt's,\" she says. \"Holt sends me nobody. So you're somebody.\" She turns the book round so you can see the line. \"Owes: a favour. We'll come to that.\"",
      [S("Go through the Forge District Gate and find the Copper-Tithe's tally-woman.", {"type": "talk", "zone": "zone.forge", "target": "Hesta Wode", "text": "Speak with Hesta Wode at the Tithe Gate", "document": "doc.forge.tithe_book"})], 900, 60, zone="zone.lowtowns"),
    Q("main.forge.1", "Under the Tithe", "main", "Hesta Wode", "Hesta Wode", 19, ["main.forge.0"], "CANON-EXPANDED: the Copper-Tithe (book1 ch.24:140); slag-piles (book1 ch.16:349); the hounds GAME-ONLY",
      "Hesta Wode's favour: clear the slag-hounds off the tips behind the Warrens, where the smelters' children pick for coal.",
      "\"The favour.\" Hesta taps the book. \"The hounds have the tips behind the Warrens. Children go up there for coal, and the hounds don't care whose children. The Tithe could clear them, but the Tithe would want paying, and the smelters haven't got it. You have.\"",
      "\"South of the gate, past the lodgings. You'll hear them.\"",
      "Hesta writes a line and crosses out the favour. \"Paid.\" She doesn't look up. \"Now ask me what you came to ask.\"",
      [S("Clear the slag-hounds off the Warrens' tips.", kill("slaghound", 6, "Slag-hounds")), S("Go back to Hesta Wode.", talk("Hesta Wode", "Return to Hesta Wode"))], 3200, 150),
    Q("main.forge.2", "The Resonance List", "main", "Hesta Wode", "Old Aelric", 20, ["main.forge.1"], "CANON: the Resonance Target List of the Forge District (book1 ch.3:149-159); the Blacklegs and the list's keeping GAME-ONLY",
      "The Council keeps a list of the district's bright children. Its Blacklegs keep a copy at the West Fire Pits. Take it, and show it to Old Aelric, who has worked the Crucible forty years.",
      "\"A child from Oakhaven.\" Hesta closes the book. \"There's a list. Not mine. The Council's. Every child in the district who lights a lamp by looking at it, their address and their age and a grade, like ore. The Blacklegs at the west pits keep a copy for the overseers.\" She lowers her voice. \"Take it off them. Then don't bring it to me. Take it to Old Aelric in the Warrens. He'll know what the words mean. I'd rather not.\"",
      "\"The west pits. South of the Warrens. The Blacklegs are the ones with clean boots.\"",
      "Old Aelric holds the list at arm's length and reads it with his lips. He stops near the bottom and taps a line with a burned finger. \"Wren. Oakhaven. Moved up.\" He folds it very small. \"I know what that means. Sit down.\"",
      [S("Put down the Blacklegs at the West Fire Pits.", kill("blackleg", 5, "Blacklegs"), visit(-150, -112, 12, "The West Fire Pits", "In a tin box by the pit, wrapped in oilcloth against the heat: a list of names, most struck through.")),
       S("Take the list to Old Aelric in the Warrens.", talk("Old Aelric", "Show Old Aelric the list", document="doc.forge.resonance_list"))], 3800, 180),
    Q("main.forge.3", "What Moved Up Means", "main", "Old Aelric", "Old Aelric", 20, ["main.forge.2"], "CANON: the Spoke raised in the Foundry (book1 ch.7:237); the pass and the overseers GAME-ONLY",
      "The moved go through the Foundry to the Spoke's yard, and nobody gets in without a foundry pass. Clear the Foundry's overseers, then take a pass off the overseers at the Great Crucible.",
      "\"Moved up.\" Aelric turns his cup. \"They go through the Foundry, the moved ones. Into the yard round the monolith. I've seen them go, the last few years, in a line, quiet. Then nothing.\" He shows you his arms. \"Nobody goes in that yard without a pass. The overseers carry them. The ones at the Great Crucible carry the good ones. Over the bridge, north-east. Mind the Foundry on the way. They don't like visitors.\"",
      "\"The Foundry first. Then the Crucible. The pass is copper, on a cord.\"",
      "Aelric turns the pass over in his fingers and gives it back quickly, as if it were hot. \"That'll get you in. It won't get you out. Nothing gets anybody out.\"",
      [S("Clear the overseers in the Foundry and at the Great Crucible.", kill("overseer", 7, "Foundry overseers"), visit(110, 108, 12, "The Great Crucible", "Under the overseers' bench by the pour, on a hook: a square of stamped copper on a cord. A foundry pass.")),
       S("Go back to Old Aelric.", talk("Old Aelric", "Return to Old Aelric", document="doc.forge.foundry_pass"))], 4200, 200),
    Q("main.forge.4", "The Foot of the Spoke", "main", "Old Aelric", "Hesta Wode", 21, ["main.forge.3"], "CANON: the Spoke (world_bible.md Magic; book1 ch.7:237) and the Iron Pact (world_bible.md); the yard, the chalk and the trail up the Spire's pillars GAME-ONLY; the Spire (28-30) PROVISIONAL",
      "Take the pass to the Spoke's yard. The Iron Pact keep it. See where the moved ones go, then tell Hesta Wode.",
      "\"Go and look, then.\" Aelric doesn't get up. \"The Iron Pact keep the yard now. Soldiers, not overseers. They won't care about your pass once you've hit one of them, so don't stop.\" He looks at the floor. \"If there's anything of the girl's there. A ribbon. Anything. I'd want to know. I don't know why. I never knew her.\"",
      "\"West of the Foundry, the ring wall. The monolith in the middle. You can't miss it. Nobody can.\"",
      "Hesta listens with her pen still. When you say UP she writes it down. \"Up the pillars,\" she says. \"To the Spire. There's no road up there for the likes of us.\" She turns a page and starts a new list, in her own hand. \"The Tithe keeps a list too, now. Names that went up. We'll want them back one day.\"",
      [S("Fight through the Iron Pact at the Spoke's yard.", kill("pactsoldier", 5, "Iron Pact soldiers"), kill("pactbow", 3, "Iron Pact crossbowmen")),
       S("Stand at the foot of the Spoke.", visit(-28, 6, 7, "The foot of the Spoke", "Close to, the monolith hums in your teeth. The runes are warm. Round its foot the stone is worn smooth in a ring, where people have stood in a line, waiting. A path is beaten from here to the north, toward the Spire's pillars.")),
       S("Tell Hesta Wode what you found.", talk("Hesta Wode", "Return to Hesta Wode"))], 5000, 260),
    # side quests
    Q("side.ade.bellows", "Ade's Bellows", "side", "Ade Heath", "Ade Heath", 19, ["main.forge.1"], "GAME-ONLY (hand-bellows, worked by hand)",
      "The Blacklegs took Ade Heath's hand-bellows. Teach them not to.",
      "\"They took my bellows.\" Ade's fists are black to the wrist. \"The Blacklegs. At the west pits. I can't work the hearth without them and if I can't work the hearth I don't eat.\" He scowls. \"They're only leather and wood. They took them because they could.\"",
      "\"The west pits. The ones with clean boots.\"",
      "Ade takes the bellows back and squeezes them once, hard, so they wheeze. \"Good,\" he says, and then, gruffly, \"thank you,\" and goes back to the hearth at a run.",
      [S("Take Ade's bellows back from the Blacklegs.", kill("blackleg", 4, "Blacklegs", say="Under a Blackleg's coat: a pair of small hand-bellows, leather and wood, a name burned into the handle. ADE.")),
       S("Give Ade Heath his bellows.", talk("Ade Heath", "Return to Ade Heath"))], 2200, 90),
    Q("side.elfa.cough", "Worse at Night", "side", "Elfa Raike", "Elfa Raike", 20, ["main.forge.1"], "CANON-EXPANDED: Weave-Eaters, born of the High City's magical pollution (book1 ch.1:251); the coughing GAME-ONLY",
      "Elfa Raike thinks something near the Spoke makes the Warrens' coughing worse at night. Go and see, and put down what you find.",
      "\"It's worse at night.\" Elfa's slate is full. \"Since they started on the monolith. Something comes off it after dark, and the wind brings it down to the Warrens. Go and look. North of the ring wall, by the Foundry. If it's something you can kill, kill it.\"",
      "\"North of the ring wall. After dark it's worse, but you'll see them by day too.\"",
      "Elfa writes a line on her slate and looks at it a long while. \"Things that eat magic,\" she says. \"Living off the monolith. And we're living under them.\" She rubs out a name in the 'worse' column. \"Thank you. Come back if you cough.\"",
      [S("Find what comes off the Spoke at night.", visit(2, 48, 12, "North of the ring wall", "The air by the Foundry's south door crawls. Shapes like smoke move in it, close to the ground, drinking the glow off the runes.")),
       S("Put down the Weave-Eaters by the Spoke.", kill("weaveeater", 5, "Weave-Eaters")),
       S("Go back to Elfa Raike.", talk("Elfa Raike", "Return to Elfa Raike", document="doc.forge.warrens_cough"))], 2600, 110),
    Q("side.hamo.glass", "Blue Glass", "side", "Hamo Glaze", "Hamo Glaze", 20, ["main.forge.1"], "GAME-ONLY",
      "Hamo Glaze wants to go into the Slag Fields for glass, and the hounds there won't let him.",
      "\"There's blue glass in the far heaps. I saw it from the top. But the hounds have the fields this season, and I'm not a fighter.\" Hamo holds up a bead, black and shining. \"Clear me a way and I'll make you something. Not a weapon. Something to look at.\"",
      "\"The Slag Fields, south-east. Over the Slag Bridge.\"",
      "Hamo comes back from the fields with his pockets clinking and his face lit up. \"Blue,\" he says. \"Blue as anything.\"",
      [S("Clear the hounds out of the Slag Fields.", kill("slaghound", 6, "Slag-hounds in the Slag Fields")), S("Go back to Hamo Glaze.", talk("Hamo Glaze", "Return to Hamo Glaze"))], 2600, 110),
    Q("side.aelric.tally", "Forty Years of Pours", "side", "Old Aelric", "Old Aelric", 20, ["main.forge.2"], "GAME-ONLY",
      "Old Aelric wants the Blacklegs at the east pits taught a lesson for the smelters they beat there.",
      "\"The east pits.\" Aelric rubs his arm. \"The Blacklegs there broke a smelter's hands last week. Hands. For being slow at the pour.\" He takes a notched stick off the shelf. \"I've cut a notch for every pour I worked. Forty years. Cut me a few for him.\"",
      "\"Over the north bridge, then south. The east pits.\"",
      "Aelric cuts four new notches at the end of his stick, deep and clean. \"For his hands,\" he says.",
      [S("Put down the Blacklegs at the East Fire Pits.", kill("blackleg", 6, "Blacklegs at the east pits")), S("Go back to Old Aelric.", talk("Old Aelric", "Return to Old Aelric", document="doc.forge.aelric_burns"))], 2600, 110),
]
QUEST_FILE = {"factions": [], "items": [], "documents": DOCUMENTS, "quests": QUESTS}

# ======================================================================================================================================
# The book of lore's chapter
# ======================================================================================================================================
def E(chapter, subject, label, line=None, more=None, page=None, secret=None):
    slug = subject.lower().replace("'", "").replace(".", "-").replace(" ", "-").replace(",", "").replace("&", "and").replace("--", "-")
    e = {"id": "lore.forge." + chapter + "." + slug, "chapter": chapter, "subject": subject}
    if line: e["line"] = line
    if more: e["more"] = more
    if page: e["page"] = page
    if secret: e["secret"] = secret
    e["canonStatus"] = label; return e
LORE_ENTRIES = [
    E("places", "The Tithe Gate", "CANON-EXPANDED: the Copper-Tithe (book1 ch.24:140); GAME-ONLY", more="The Tithe's gate-book has a column for what each soul owes. Most of it is the Concord's. A little of it is favours.", page="doc.forge.tithe_book"),
    E("places", "The Forge Warrens", "CANON: the Forge District Warrens (book1 ch.3:159); GAME-ONLY", more="Elfa Raike keeps a slate of the Warrens' coughs. The 'worse since the monolith' column runs off the slate.", page="doc.forge.warrens_cough"),
    E("places", "The Spoke", "CANON: the Spoke, a rune-carved stone monolith (world_bible.md Magic; book1 ch.7:237)", line="A rune-carved monolith of black stone in the middle of the Foundry, raised by the Council. It hums, and the air near it tastes of copper.",
      more="On the ring wall round it, low down where a child would sit, a W in chalk.", secret="secret.forge.chalk-w"),
    E("places", "The Foundry", "CANON: the Foundry District (book1 ch.7:237); GAME-ONLY", more="The Council's list of the district's bright children, graded like ore, each struck through and marked: Moved up.", page="doc.forge.resonance_list"),
    E("places", "The Great Crucible", "CANON: the Great Crucible (the author's map); GAME-ONLY", more="A foundry pass: stamped copper on a cord. It gets you into the yard. It does not get you out.", page="doc.forge.foundry_pass"),
    E("places", "The Slag Fields", "CANON-EXPANDED: slag-piles (book1 ch.16:349); GAME-ONLY", line="Black heaps cracked with ember, tipped out of the furnaces for a hundred years.",
      more="From the top of the tallest heap the whole district lies below in the smog, and the Crown far off over it all.", secret="secret.forge.slag-top"),
    E("places", "The River Aureum", "CANON: the River Aureum (the author's map); GAME-ONLY", line="Molten metal running in a stone channel down from the furnaces. The heat comes off it like a wall."),
    E("places", "The Molten Vein", "CANON: the Molten Vein (the author's map); GAME-ONLY", line="The second molten channel, crossed only by its stone bridges."),
    E("places", "The Rust & Ruin", "CANON: the Rust & Ruin (book1 ch.24:81)", line="A tavern behind an iron-banded oak door. Stale beer, smoke, and nobody asking your name."),
    E("people", "Hesta Wode", "CANON-EXPANDED: the Copper-Tithe (book1 ch.24:140); Hesta GAME-ONLY", line="The Copper-Tithe's tally-woman at the gate. She writes down everyone, and what they owe, and keeps a list of her own now.",
      more="The Tithe's runners hid their takings in the district wall once. Hesta never went to look.", secret="secret.forge.tithe-stash"),
    E("people", "Old Aelric", "GAME-ONLY", more="Aelric's notched stick: forty years of pours, and at the end, notches that are not pours.", page="doc.forge.aelric_burns"),
    E("people", "Hamo Glaze", "GAME-ONLY", line="A slag-glass blower who makes beads out of the furnaces' leavings. Once he made one the colour of the sea."),
    E("creatures", "Slag-hound", GO, line="Feral dogs of the slag heaps, grey with ash and burned bald in patches. They hunt the tips in packs."),
    E("creatures", "Blackleg", GO, line="Thugs the foremen hire to keep the smelters working. They warm their hands at the fire pits and take what they like."),
    E("creatures", "Foundry overseer", "CANON-EXPANDED: the Council's foremen; GAME-ONLY", line="The Council's men in the Foundry, with tally-sticks and cudgels. They count pours and children the same way."),
    E("creatures", "Iron Pact soldier", "CANON: the Iron Pact (world_bible.md); here GAME-ONLY", line="Soldiers of the Iron Pact, set to keep the Spoke's yard. They do not ask what the yard is for."),
    E("creatures", "Old Clinker", GO, line="An old slag-hound, half bald and all teeth, who has outlived three packs. The smelters' children dare each other to see him."),
    E("creatures", "The Spoke-Leech", "CANON-EXPANDED: Weave-Eaters (book1 ch.1:251); GAME-ONLY", line="A Weave-Eater grown fat on what leaks off the monolith. It drinks the glow from the runes and the breath from the Warrens."),
]
LORE_FILE = {"zone": "zone.forge", "canonStatus": "GAME-ONLY: the Forge District's chapter of the book of lore, its lines the game's; the Forge District, its Warrens, the Foundry, the Spoke, the Copper-Tithe, the Rust & Ruin and the Weave-Eaters are CANON (world_bible.md; book1 ch.1, 3, 7, 16, 24), the River Aureum, the Molten Vein and the Great Crucible the author's map, as each line says.", "entries": LORE_ENTRIES}

# ---- cross-checks ----
names = {r["name"] for r in RESIDENTS}; lm = {l["name"] for l in LANDMARKS}; mobs = {c["mob"] for c in CAMPS}; docs = {d["id"] for d in DOCUMENTS}
for q in QUESTS:
    for who in (q["giver"], q["turnIn"]):
        if who not in names and who != "Edda Holt": problem(q["id"] + ": nobody named " + who)
    for st in q["steps"]:
        for o in st["objectives"]:
            if o.get("document") and o["document"] not in docs: problem(q["id"] + ": unknown page")
            if o["type"] == "talk" and o["target"] not in names: problem(q["id"] + ": nobody named " + o["target"])
            if o["type"] == "kill" and not any(o["target"].split(".")[1] == c["tag"] for c in CAMPS): problem(q["id"] + ": no camp tagged " + o["target"])
            if o["type"] == "visit" and (in_building(o["at"]["x"], o["at"]["y"]) or near_channel(o["at"]["x"], o["at"]["y"], 0)): problem(q["id"] + ": a visit in a building")
unlocks = set()
for e in LORE_ENTRIES:
    if e["subject"] not in lm | names | mobs: problem(e["id"] + ": no such subject")
    if bool(e.get("more")) != bool(e.get("page") or e.get("secret")): problem(e["id"] + ": more without a page or a secret")
    if e.get("page"): unlocks.add(e["page"])
    if e.get("secret"): unlocks.add(e["secret"])
    for s in SECRETS:
        if e.get("line") and s["name"].lower() in e["line"].lower(): problem(e["id"] + " names " + s["name"])
if len(unlocks) < 8: problem("fewer than 8 lore unlocks: " + str(len(unlocks)))
if len({e["id"] for e in LORE_ENTRIES}) != len(LORE_ENTRIES): problem("lore ids repeat")
BANNED = ["machine", "engine", "clockwork", "gear", "piston", "pump", "valve", "conveyor", "crane", " lift", "golem", "drill", "cog", "boiler", "contraption", "—"]
blob = json.dumps([ZONE, QUEST_FILE, LORE_FILE], ensure_ascii=False).lower()
for w in BANNED:
    if w in blob: problem("a banned word: " + w)

# ---- write ----
def dump(obj, rel):
    path = os.path.join(ROOT, *rel.split('/'))
    text = json.dumps(obj, indent=2, ensure_ascii=False).replace('\n', '\r\n') + '\r\n'
    open(path, 'wb').write(text.encode('utf-8')); print('wrote', rel, len(text), 'bytes')
dump(ZONE, 'Zones/forge.json'); dump(QUEST_FILE, 'Quests/forge.json'); dump(LORE_FILE, 'Lore/forge.json')

# ---- the Lowtowns' gate opens onto it ----
low_path = os.path.join(ROOT, 'Zones', 'lowtowns.json'); low = json.loads(open(low_path, 'rb').read().decode('utf-8'))
low["exits"] = [e for e in low["exits"] if e["to"] != "zone.forge"] + [{"to": "zone.forge", "name": "East through the Forge District Gate", "at": V(198, 0), "arrive": V(-162, 0), "radius": 5}]
for p in low["props"]:
    if p.get("name") == "The Forge District's wall": p["variant"] = 1
    if p.get("kind") == "signpost" and p.get("name", "").startswith("The Forge District"): p["name"] = "The Forge District (19-22)"
for l in low["landmarks"]:
    if l["name"] == "The Forge District Gate":
        l["text"] = "Open by day now, its leaves back against the wall. Smoke rolls over it from the chimney stacks beyond, and the Copper-Tithe count who goes through."
        l["canonStatus"] = "CANON: the Forge District's walls (book1 ch.3:5); the gate open onto the Forge District zone (2026-10-19) GAME-ONLY"
dump(low, 'Zones/lowtowns.json')
print('laid out:', len(PROPS), 'props,', len(CAMPS), 'camps,', len(NODES), 'nodes,', len(QUESTS), 'quests,', len(LORE_ENTRIES), 'lore lines')
