# ======================================================================================================================================
# The book of lore's chapter (EncounterContent/Lore/lowtowns.json; LoreBook): places, people, creatures, herbs and ores. A line is known
# by doing; its better half ("more") waits on a page or a hidden find.
# ======================================================================================================================================
def E(chapter, subject, label, line=None, more=None, page=None, secret=None):
    slug = subject.lower().replace("'", "").replace(".", "-").replace(" ", "-").replace(",", "")
    e = {"id": "lore.lowtowns." + chapter + "." + slug, "chapter": chapter, "subject": subject}
    if line: e["line"] = line
    if more: e["more"] = more
    if page: e["page"] = page
    if secret: e["secret"] = secret
    e["canonStatus"] = label; return e
GO = "GAME-ONLY"
LORE_ENTRIES = [
    # places
    E("places", "The Gate Ward", "GAME-ONLY", more="The reeve before Edda Holt stopped writing the warrants into the book, and hid the last page she did write in the wall.", page="doc.lowtowns.old_toll"),
    E("places", "The Holding House", "CANON-EXPANDED: the Council's hired men; GAME-ONLY", more="The warders' orders: hold a child one night, give it up at first light, ask nothing. One warder wrote on the back that he asked.", page="doc.lowtowns.warder_orders"),
    E("places", "The Rusty Spoon", "CANON: the Rusty Spoon (book1 ch.1:9); the slate GAME-ONLY", more="Mother Sall's slate of debts has a column for broken chairs, and the column has run off the slate and onto the wall.", page="doc.lowtowns.spoon_slate"),
    E("places", "The Tenement Rows", "CANON: Rule One of the Lowtowns (book1 ch.1:257)", more="Every child in the Rows learns Rule One before their letters, and somebody keeps chalking it under the bridge.", page="doc.lowtowns.rule_one"),
    E("places", "The Soup-Hall", "GAME-ONLY", more="Brother Aldo's slate counts who ate. Gytha's girl had a second bowl for her hands, which were cold, and bright.", page="doc.lowtowns.soup_slate"),
    E("places", "The Rust Market", "CANON: the Rust Market's skewered rat and aether-spiced wine (book1 ch.3:7); the board GAME-ONLY", more="The price-board by the long stalls has every price crossed out and written higher, and EVERYTHING DEARER in soot across the bottom.", page="doc.lowtowns.price_board"),
    E("places", "The Shard-Burners' Yard", "GAME-ONLY reading (the shards as burned memory)", more="The burners' tally: names, dates and nights of light bought. Osric's rule at the bottom: never sell a family its own shard twice.", page="doc.lowtowns.burner_tally"),
    E("places", "The Drain Courts", "CANON-EXPANDED: the former Lowtown militia (book1 ch.20:127); GAME-ONLY", more="The militia held the tanners' court a whole winter and cut the nights into the brick.", secret="secret.lowtowns.court-marks"),
    E("places", "The Docks", "GAME-ONLY", more="The dockers keep a lamp lit in a niche on the riverside walk, a copper in the dish for the river. It is for no god; it is for the lamp.", secret="secret.lowtowns.lamp-shrine"),
    E("places", "The Chemical River", "CANON: the Chemical River (book1 ch.15-16)", line="Black, slow, iridescent and poisonous. Nothing natural lives in it. The barges go through it anyway, poled by people who try not to splash."),
    E("places", "The Outfall", "GAME-ONLY (the Sludge-Tunnels, DUNGEON_DESIGN.md)", line="The Forge District's drain under the wall, grated and signed. Past the grating the culvert turns and goes down, and a warm breath comes up it."),
    # people
    E("people", "Old Wulf", "CANON: the former Lowtown militia (book1 ch.20:127); Wulf GAME-ONLY", more="The militia's last roll is cut into the brick by the well: twenty-two names, nineteen struck through, and Wulf's with room after it.", page="doc.lowtowns.militia_roll"),
    E("people", "Ketta Rune", "CANON: bone harpoons on amber rope (book1 ch.20:177-191); Ketta GAME-ONLY", more="Her grandmother's count, knotted in amber cloth: forty-one Eaters pinned, and three more in Ketta's hand. Bone holds. Steel lies.", page="doc.lowtowns.harpoon_count"),
    E("people", "Rudd Crake", "CANON-EXPANDED: Weave-Eaters and salt (book1 ch.20); GAME-ONLY", more="One of Rudd's gang went into the far courts with a twist of salt to find what took his sister, and lies there yet, the salt in his fist.", page="doc.lowtowns.drowned_docker"),
    E("people", "Ebba Strand", "CANON-EXPANDED: the Crown, white stone and gold (world_bible.md Geography); Ebba GAME-ONLY", more="On her days off Ebba walks to the top of the town where the smog thins, to look at the Crown. Just to look.", secret="secret.lowtowns.smog-thin"),
    E("people", "Mabb Soot", "GAME-ONLY", more="Grey-bell grows only in the cinders the burners tip out. Mabb won't pick it. She says it remembers.", secret="secret.lowtowns.grey-bell"),
    E("people", "Gorm Rock-Wall", "CANON: Gorm Rock-Wall, an orc of the Lowtowns (book1 ch.1:21); his dream of a pier is CANON (book1 ch.1:153)", line="An orc of the Lowtowns, built like a wall and about as easy to move. He drinks at the Spoon and stops fights there; he dreams of a pier in the north where the water runs clear."),
    E("people", "Gytha Pell", "GAME-ONLY", line="A mother in the Rows whose girl, Ysa, sparked over the washing and was taken up by warrant. She stands on her step every day."),
    # creatures
    E("creatures", "Gutter-hound", GO, line="Feral dogs of the back yards, bred from the strays of a hundred years. They run in packs and they are always hungry."),
    E("creatures", "Dock-rat cutter", GO, line="River thieves who work the barges at night with short knives, cutting bales loose and purses too."),
    E("creatures", "Dock-rat poler", GO, line="The dock-rats' polers: they keep the stolen barges moving and use the pole on anyone who comes too close."),
    E("creatures", "Weave-Eater", "CANON: Weave-Eaters, parasitic spirits born of the High City's magical pollution (book1 ch.1:251); steel through one turns black and brittle (book1 ch.20:155)", line="Shadowy spirits of the drains, born of the High City's magical waste. Steel that passes through one comes out black and brittle; salt makes it solid; bone holds it."),
    E("creatures", "Mother of Drains", "CANON-EXPANDED: Weave-Eaters (book1 ch.1:251); the Mother GAME-ONLY", line="The militia's name for the great Eater under the courts. She comes up only after dark, and the lamps gutter when she does."),
    E("creatures", "Iron Teeth runner", "CANON: the Iron Teeth (B1WB:26; book1 ch.7:21); GAME-ONLY camp", line="The Iron Teeth's runners keep the back lanes of the market for the gang. Up here they trade. Back there, they don't."),
    E("creatures", "Iron Teeth sifter", "CANON: the Iron Teeth (B1WB:26; book1 ch.7:21); the sifting GAME-ONLY", line="The Teeth's sifters work the slag that comes out of the Outfall, picking out the bits that glow."),
    E("creatures", "Knuckle Brannoc", "CANON faction: the Iron Teeth (book1 ch.7:21); Brannoc GAME-ONLY", line="The Iron Teeth's yard-boss by the Outfall. Iron on his knuckles, a ledger in his coat, and two sifters always at his back."),
    E("creatures", "Hired warder", "CANON-EXPANDED: the Council's hired men; GAME-ONLY", line="The Council's hired men at the Holding House. They do not read the warrants and they do not ask the children's names."),
    E("creatures", "Grey-touched", "GAME-ONLY (the shards as burned memory)", line="What is left when a shard is burned wrong: grey, slow, and almost somebody. The burners keep them in the cellar."),
    E("creatures", "The Lantern-Eater", "CANON-EXPANDED: Weave-Eaters (book1 ch.1:251); the Lantern-Eater GAME-ONLY", line="A Weave-Eater that drinks the light out of the lamps of one lane, a lamp a night, the glass frosted black behind it."),
    # herbs and ores (the level-20 helper's tier 6)
    E("herbs", "node.slagiron", GO + ": the trades' herbs and metals", line="Slag-iron: the foundries' cast-offs heaped in the yards of fallen terraces. Hard to work and never clean, but iron."),
    E("herbs", "node.cinderbeam", GO + ": the trades' herbs and metals", line="Cinder-beams: the old roof timbers of fallen terraces, charred black outside and sound within."),
    E("herbs", "node.sootwort", GO + ": the trades' herbs and metals", line="Soot-wort grows in the gutters and on the ash heaps where nothing else will. The Lowtowns cough, and soot-wort is for coughs."),
    E("herbs", "node.lamp_lichen", GO + ": the trades' herbs and metals", line="Lamp-lichen glows by the drains, a dim gold-green. Pick it at night and your hands shine till morning."),
    E("herbs", "herb.grey_bell", GO + ": the trades' herbs and metals", line="Pale bells with a light in them like a held breath. Rare, and the Lowtowners leave it be."),
]
LORE_FILE = {"zone": "zone.lowtowns", "canonStatus": "GAME-ONLY: the Lowtowns' chapter of the book of lore, its lines the game's; the Lowtowns, the Rust Market, the Rusty Spoon, the Iron Teeth, the Weave-Eaters and the Chemical River are CANON (world_bible.md; book1 ch.1, 3, 7, 15-16, 20), as each line says.", "entries": LORE_ENTRIES}

# ======================================================================================================================================
# Items: salt and the bone harpoon (thrown; WeaveEaterRules), and who sells them.
# ======================================================================================================================================
ITEM_FILE = {
    "items": [
        {"id": "throw.salt_twist", "name": "Twist of salt", "kind": "consumable", "thrown": "salt", "quality": 1, "level": 15, "value": 2, "stack": 20,
         "description": "Grey salt in a paper twist. Thrown at a Weave-Eater it goes white and still for a few heartbeats; at anything else it only stings.",
         "canonStatus": "CANON: salt against the Weave-Eaters (book1 ch.20:199-203); the twist GAME-ONLY"},
        {"id": "throw.bone_harpoon", "name": "Bone harpoon", "kind": "consumable", "thrown": "harpoon", "quality": 2, "level": 16, "value": 6, "stack": 5,
         "description": "A barbed head of old bone on a length of amber rope. Thrown into a spirit, it pins it where it is.",
         "canonStatus": "CANON: bone harpoons on amber rope (book1 ch.20:177-191); this one GAME-ONLY"},
    ],
    "vendors": [{"npc": "Tibb Marrow", "items": ["throw.salt_twist", "potion.salt"]}, {"npc": "Ketta Rune", "items": ["throw.bone_harpoon"]}],
}
