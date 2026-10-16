# ======================================================================================================================================
# People (Docs/ZONE_LOWTOWNS.md section 3; GAME-ONLY unless marked). Residents keep a post day and night (a quest giver, a stall-keeper,
# the watch on the bridges); the villagers live in the named terraces and have the village's working day. Every one of them has two or
# three stories (ZoneLoreTests). Kept out on purpose (section 9): Valen, Rill, Silas Vane, Veyra, Kaelen, Aethelgard, the Architect;
# Seekers and Inquisitors are only spoken of.
# ======================================================================================================================================
def R(name, role, title, at=None, place=None, look=None, works=False, stories=()):
    r = {"name": name, "role": role, "title": title}
    if place: r["place"] = place
    if look: r["look"] = look
    if at: r["at"] = V(*at)
    if works: r["works"] = True
    r["stories"] = list(stories); return r
RESIDENTS = [
    R("Edda Holt", "stranger", "Toll-Reeve", (-188, 4.2), stories=[
        "Thirty years I've kept the toll-book. Carts, bales, pilgrims, a man with a bear once. You write what comes through the gate and you don't ask what it's for.",
        "The warrant page I keep in the back of the book, where the Council's men can't see me reading it twice.",
        "My mother said the gate used to shut at dusk. Now it never shuts. Nobody ever decided that. It just stopped closing."]),
    R("Ludo Brand", "stranger", "Carter", (-168, -23.5), stories=[
        "I've carted up the south-east road from Oakhaven every week since I could hold the reins. Grain up, rags and pots down. The road doesn't care which.",
        "Somebody's been cutting bales off the barges at night and selling them back to me in the morning. My own bales. At a price."]),
    R("Mother Sall", "innkeeper", "Keeper of the Rusty Spoon", place="bar", works=True, stories=[
        "The ale's watered because the water's cheaper than the ale. Everybody knows. Nobody complains. That's the Spoon's whole secret.",
        "Chairs get broken in here most nights. I keep a cellar of legs. Nobody's ever asked why there are so many legs.",
        "I don't take sides at my tables. I take coin. The games are high and the stakes are other people's."]),
    R("Gorm Rock-Wall", "stranger", "Lowtown Muscle", place="inn", look="orc", stories=[
        "The ale here tastes like rust. I drink it anyway. You get used to rust in the Lowtowns. You get used to most things.",
        "My mother told me of a pier in the north where the water runs clear and you can just sit. One day I'll have a pier. And a fishing pole that isn't scrap wire.",
        "Sap-root for a split knuckle. Chew it, spit it on, don't look at it. Orc remedy. Works on you lot too, mostly."]),
    R("Gytha Pell", "stranger", "Mother", (-106, -141.5), stories=[
        "My Ysa sparked over the washing. A little light in her hands, like a moth's. I told her to put it out. As if she could.",
        "They came for her with a paper. A paper. I can't read, but I know a paper is worth more than a girl, up there.",
        "I keep her cup on the sill. The neighbours think I'm mad. Let them. It's a cup. It's hers."]),
    R("Brother Aldo", "stranger", "Keeper of the Soup-Hall", (-51.5, -133.5), stories=[
        "This was a chapel once. Nobody remembers to what. I took the god's chair out and put a copper in its place. The copper does more good.",
        "Bones, turnips and whatever the market throws out at closing. I call it soup. The Rows call it Thursday.",
        "I counted the children at the tables before the warrant came. I count them now. I don't tell anyone the difference."]),
    R("Old Wulf", "stranger", "Former Militia Sergeant", (-74, -92), stories=[
        "Lowtown militia, twenty years. We had bucklers that rusted if you looked at them and spears we pulled off a scrap heap. We were the only ones who came when the drains rose.",
        "Steel's no good on a Weave-Eater. Goes black and breaks like old glass. Salt's what you want. Salt, and something with a rope on it.",
        "They disbanded us. Not enough coin for watchmen, the Council said. Then they hired warders for the Holding House out of the same coin."]),
    R("Tibb Marrow", "stranger", "Salt-Seller", (124, 55.5), stories=[
        "Rock salt, sea salt, grey salt off the Ash-Walkers' carts. Throw it at what comes out of the drains and watch it go white. Two crowns a twist.",
        "People laugh at a salt-seller till the lamps go out in their lane. Then they come to me in their nightshirts."]),
    R("Ketta Rune", "stranger", "Bone-Harpooner", (106, 81), stories=[
        "Bone for the head, amber for the rope. The old craft. Steel goes through them; bone catches. Don't ask me why. My grandmother didn't know either.",
        "I learned it off an Ash-Walker who came down to sell salt and stayed to drink. He paid his slate in lessons.",
        "A good harpoon pins an Eater the way a nail pins a shadow. It shouldn't work. It does."]),
    R("Odo Sark", "stranger", "Clerk of the Antiquities", (121, 94.5), stories=[
        "The Antiquities buys and sells old things. The back room is not for sale and not for looking at. That's the only rule. It's a good rule.",
        "I keep the front of the shop very clean. Clean fronts are restful. People stop asking questions about clean fronts."]),
    R("Grimbold", "stranger", "Iron Teeth Factor", (168, 86), stories=[
        "Up here in the market the Teeth are traders, friend. What we are below is our business. Buy something or move along.",
        "Every lamp in the Lowtowns burns a shard, and every shard comes through somebody's hands. Mostly ours. Think on that when you light yours."]),
    R("Osric Fenn", "stranger", "Shard-Burner", (134, -106), stories=[
        "A shard is a little light that used to be somebody. We buy them back for the families, when the families can pay. A night's light, a shard. Sometimes two.",
        "Burn one wrong and what's left walks. We keep those in the cellar. I don't go down there after dark, and I'm the one with the key.",
        "My mother keeps the old Fenn house in the Rows. She thinks I sell lamp-oil. Let her."]),
    R("Hale Dunning", "stranger", "Ward-Watch", (-16.5, -24), stories=[
        "The Ward-watch: two of us on the bridges, a lamp each and no pay worth the name. We watch. That's the job. We watch.",
        "Old Wulf drills us when he's sober. He says the militia stood when the drains rose. I believe him. I've seen the drains."]),
    R("Ebba Strand", "stranger", "Ward-Watch", (24.5, 101), stories=[
        "Rag Bridge's mine. Rags hang off it because the wind brings them. Nobody hangs them. Nobody takes them down.",
        "At night I count the lamps on Weir Lane. When one goes out I don't go and look. I count again in the morning."]),
    R("Rudd Crake", "stranger", "Dock Ganger", (-25.5, -60), stories=[
        "Honest labour at the docks. Lift, carry, pole, sleep. The barges go down to the river towns and back with whatever's cheap.",
        "No cranes on my quays. Backs and ropes. A crane's a thing that breaks and someone has to pay for. A back mends."]),
    R("Nan Tallow", "merchant", "Skewer-Seller", (112, 87), stories=[
        "Roasting skewered rat, two a copper, three if you don't ask. They're rats. They're hot. That's the whole of the promise.",
        "Smog makes the fat spit. I've a burn on every finger. My mother had the same burns. It's the family trade."]),
    R("Meg Brisk", "merchant", "Wine-Seller", (97, 76), stories=[
        "Aether-spiced wine. It's cheap, it's hot, and nobody knows what the aether is. Don't drink the bottom of the jug.",
        "The High City throws its old wine down the drains. Some of it ends up here, and some of that ends up in my jugs. Waste not."]),
    R("Wynn Madder", "stranger", "Dyer", (-116.5, -117), stories=[
        "Madder for red, woad for blue, weld for yellow, soot for everything else. The Lowtowns wear a lot of everything else.",
        "My arms are blue to the elbow and red past it. The children call me the Flag."]),
]
# The villagers: the Lowtowners at their trades (the roles in the village's order: the smith at the market hearth, the stall-keeper,
# the baker, the leatherworker, the tanner; the rest drink at the Spoon or gossip in the Rows). Short Lowtown names (Germanic roots).
NAMES = ["Aldred Coke", "Hild Barrow", "Cuth Penny", "Elga Sump", "Brand Tolley", "Wyn Garth", "Osgar Rook", "Tilda Grime", "Edwy Larch",
         "Mabb Soot", "Leof Crane", "Gisla Fenn", "Ricca Stave", "Wat Mallow", "Duna Slake", "Sigga Nail"]
STORIES = {
    "Aldred Coke": ["The market hearth's bellows are my arms. No engine in the Lowtowns works as cheap as a smith who needs to eat.", "Slag-iron. The foundries throw it out; I beat it into nails. Their waste, my bread."],
    "Hild Barrow": ["I was a field-hand once, out past the gate. Then the fields went to the Council's tenants. Now I drink in the Spoon and remember barley.", "My Leof says we'll go back out one day. He's been saying it eleven years."],
    "Cuth Penny": ["Penny's stall: nails, buttons, string, knives, and whatever fell off a barge. Don't ask which is which.", "I lodge at the Mallows'. Wat makes my purses. I sell them back to people who've had theirs cut."],
    "Elga Sump": ["The oven's lit at four. Smog bread: it rises grey and nobody minds. Flour comes up from Oakhaven on Ludo's cart.", "My sister tans hides by the river. Between us we smell of the two best and worst things in the Lowtowns."],
    "Brand Tolley": ["You want to know who's sleeping with whose husband in the Rows? Three crowns. You want to know who's been taken? Free. Everybody knows.", "Sigga says I talk too much. Sigga talks more. She just does it quieter."],
    "Wyn Garth": ["I used to run game down from the woods past Oakhaven. No woods here. I lodge with Osgar and drink his cider and wait for something to hunt.", "Rats, if you're hungry. Gutter-hounds, if you're brave. I'm neither tonight."],
    "Osgar Rook": ["First cup's for the dead. Second's for the living. Third's for the Council, so it can choke on it.", "I had a shop once, on Gate Street. A Seeker walked in looking for somebody. I've been drinking since. I don't remember the shop much."],
    "Tilda Grime": ["Lamp-tag! You touch a lamp-post and you're safe. If you're not touching one the Eaters get you. That's the rule.", "My friend Ysa sparked. Her hands went bright. Then she went away. Mum says don't say her name."],
    "Edwy Larch": ["Cut timber in the north for a lumber-master till my back went. Now I pull cinder-beams out of the fallen terraces for burning. Same work, sadder trees.", "The Barrows let me the cupboard under the stair. It's dry. In the Lowtowns that's a palace."],
    "Mabb Soot": ["Soot-wort grows where nothing should: in the gutters, on the ash heaps. It's good for a cough. Everybody here has a cough.", "Lamp-lichen glows by the drains. Pick it at night and your hands shine till morning. Don't eat it."],
    "Leof Crane": ["Fields, Hild says. Barley, Hild says. Hild's never weeded a row in her life. I have. I'm happy with the Spoon.", "My brother went for a watchman and they gave him a lamp and no pay. He gave the lamp back."],
    "Gisla Fenn": ["My Osric sells lamp-oil in the south of the market. Good boy. Always smells of smoke. I don't ask.", "I've lived in the Rows seventy years. The terraces were new brick once. I remember the colour. Red, like a cheek."],
    "Ricca Stave": ["I worked the mills upriver till the mills stopped wanting people. Now I lodge with old Gisla and carry for anyone with a copper.", "The river's black because the High City's sick, my father said. He said it like it was weather."],
    "Wat Mallow": ["Gutter-hound hide makes a good purse. Tough, and nobody wants to steal a thing that smells like that.", "Mabb's my wife. She picks the weeds, I tan the hounds. We keep a clean house anyway."],
    "Duna Slake": ["The tanning yard's by the river because the river can't smell any worse. Neither can I.", "Elga bakes, I tan. When we visit Mother we stand at opposite ends of the room."],
    "Sigga Nail": ["Brand talks. I listen. That's how we know everything between us.", "The warders at the Holding House drink in the Spoon on their nights off. They don't talk about the children. They don't have to. They drink like men who don't."],
}
HOUSEHOLDS = [
    ("Coke", "Coke's tenement", [("Aldred Coke", "head"), ("Tilda Grime", "lodger")]),
    ("Barrow", "Barrow's tenement", [("Hild Barrow", "head"), ("Leof Crane", "husband"), ("Edwy Larch", "lodger")]),
    ("Sump", "Sump's tenement", [("Elga Sump", "head"), ("Duna Slake", "sister")]),
    ("Tolley", "Tolley's tenement", [("Brand Tolley", "head"), ("Sigga Nail", "wife")]),
    ("Rook", "Rook's tenement", [("Osgar Rook", "head"), ("Wyn Garth", "lodger")]),
    ("Fenn", "Fenn's tenement", [("Gisla Fenn", "head"), ("Ricca Stave", "lodger")]),
    ("Mallow", "Mallow's tenement", [("Wat Mallow", "head"), ("Mabb Soot", "wife"), ("Cuth Penny", "lodger")]),
    ("Pell", "Pell's tenement", [("Gytha Pell", "head")]),
]
WORKSHOPS = [("Aldred Coke", "The Rust Market hearth"), ("Elga Sump", "Sump's bakehouse"), ("Cuth Penny", "Penny's stall"), ("Wat Mallow", "Mallow's leather shop"), ("Duna Slake", "The tanning yard")]
CRITTERS = [("cat", -110, -100, 20, 3), ("cat", 110, 66, 14, 2), ("cat", -140, 20, 12, 2), ("crow", -140, 175, 22, 4), ("crow", 140, -118, 12, 3), ("crow", 30, 150, 20, 3)]

# ======================================================================================================================================
# Camps (section 4; levels 15-20). Tags the level-20 helper's loot tables know: gutterhound, dockrat, drainmother, brannoc; the rest
# fall to the shared tables. The Drain Courts' Weave-Eaters are night camps (gone by day); a few keep the deepest court by day. The rare
# comes last (a visit without it moves no other camp's mobs).
# ======================================================================================================================================
def C(name, mob, tag, look, at, r, count, lo, hi, label, respawn=80, **kw):
    c = {"name": name, "mob": mob, "tag": tag, "look": look, "canonStatus": label, "center": V(*at), "radius": r, "count": count, "levelMin": lo, "levelMax": hi, "respawn": respawn}
    c.update(kw); return c
G = "GAME-ONLY"
WE = "CANON creature (Weave-Eaters: book1 ch.1:251, ch.20); the camp GAME-ONLY"
IT = "CANON faction (the Iron Teeth: B1WB:26; book1 ch.7:21); the camp GAME-ONLY"
CAMPS = [
    C("Gate Ward gutter-hounds", "Gutter-hound", "gutterhound", "wolf", (-150, -52), 9, 4, 15, 16, G),
    C("Rows gutter-hounds", "Gutter-hound", "gutterhound", "wolf", (-140, -178), 10, 5, 15, 16, G),
    C("Pell Row gutter-hounds", "Gutter-hound", "gutterhound", "wolf", (-60, -186), 9, 4, 16, 16, G),
    C("Dock-rat cutters", "Dock-rat cutter", "dockrat", "deserter", (-28, -194), 8, 4, 16, 17, G),
    C("Dock-rat polers", "Dock-rat poler", "dockrat", "deserter", (-30, -176), 6, 3, 17, 17, G),
    C("First court Weave-Eaters", "Weave-Eater", "weaveeater", "weaveeater", (-150, 132), 8, 4, 17, 18, WE, night=True),
    C("Tanners' court Weave-Eaters", "Weave-Eater", "weaveeater", "weaveeater", (-106, 160), 8, 4, 17, 18, WE, night=True),
    C("Deepest court Weave-Eaters", "Weave-Eater", "weaveeater", "weaveeater", (-150, 192), 6, 3, 18, 18, WE),
    C("Mother of Drains", "Mother of Drains", "drainmother", "weaveeater", (-140, 202), 3, 1, 20, 20, WE + "; the Mother of Drains GAME-ONLY", 300, elite=True, night=True, guards="Deepest court Weave-Eaters"),
    C("Iron Teeth runners", "Iron Teeth runner", "ironteeth", "deserter", (180, 124), 7, 4, 17, 18, IT),
    C("Iron Teeth sifters", "Iron Teeth sifter", "sifter", "deserter", (150, -184), 8, 4, 18, 19, IT),
    C("Brannoc's sifters", "Iron Teeth sifter", "sifter", "deserter", (174, -180), 3, 2, 19, 19, IT),
    C("Knuckle Brannoc", "Knuckle Brannoc", "brannoc", "banditking", (176, -186), 2, 1, 20, 20, IT + "; Knuckle Brannoc GAME-ONLY", 300, elite=True, guards="Brannoc's sifters"),
    C("Holding House warders", "Hired warder", "warder", "warden", (-172, 33), 5, 3, 18, 19, "CANON-EXPANDED: the Council's hired men (the Council is CANON, world_bible.md); Seekers are the Transit Hub's and are not here"),
    C("Holding House back warders", "Hired warder", "warder", "warden", (-150, 54), 4, 2, 19, 19, "CANON-EXPANDED: the Council's hired men; GAME-ONLY post"),
    C("Grey-touched in the cellar", "Grey-touched", "greytouched", "hollow", CELLAR, 6, 4, 18, 19, "GAME-ONLY (the shards as burned memory: the Sludge-Tunnels' reading, DUNGEON_DESIGN.md)"),
    C("The Lantern-Eater", "The Lantern-Eater", "lanterneater", "weaveeater", (-68, 52), 3, 1, 19, 19, WE + "; the Lantern-Eater GAME-ONLY", 600, elite=True, rare=.25, guards="none"),
]

# ======================================================================================================================================
# Hidden finds (14): ten pages of the book of lore (books, notes and two dead), a lookout where the smog thins, militia marks, a
# lamp-shrine and the rare grey-bell. Clues and hints never name a landmark (DiscoveryLog.Validate).
# ======================================================================================================================================
def S(slug, name, kind, at, text, clue, hint, label, xp=150, **kw):
    s = {"id": "secret.lowtowns." + slug, "name": name, "kind": kind, "at": V(*at)}
    if kind != "vista": s["prompt"] = kw.pop("prompt")
    s.update({"text": text, "clue": clue, "hint": hint, "xp": xp}); s.update(kw); s["canonStatus"] = label; return s
SECRETS = [
    S("rule-one", "Rule One, Chalked", "note", (-19, -27), "Chalked on the brick under the bridge's west end, in a dozen hands, the same line over and over, the oldest rubbed nearly away.",
      "Under the west end of the sett-paved bridge, low on the brick where the watch can't see, somebody keeps chalking the same rule.",
      "Hale Dunning says there's a line chalked under the west end of his bridge, low down. Every child in the Lowtowns learns it before their letters.",
      "CANON: Rule One of the Lowtowns (book1 ch.1:257); the chalk GAME-ONLY", prompt="Read the chalk", document="doc.lowtowns.rule_one", radius=2.5),
    S("soup-slate", "The Soup-Hall Slate", "read", (-61.5, -133), "A slate hung by the old chapel's door, chalked and rubbed and chalked again: who ate, who paid in turnips, who owes nothing because nobody could make them.",
      "By the door of the chapel that feeds the Rows, a slate keeps count of who ate and who paid.",
      "Brother Aldo keeps a slate by his door. He says if you want to know the Rows, read who ate this week. And who didn't come.",
      "GAME-ONLY", prompt="Read the slate", document="doc.lowtowns.soup_slate"),
    S("burners-tally", "The Burners' Tally", "read", (145, -97), "A board nailed up behind the racks: names, a date, a number of nights. The numbers are small. Some names have a second date, and nothing after.",
      "Behind the racks where the shards glow, a board is nailed up: names, dates, and how many nights each bought.",
      "Osric Fenn says the tally behind his racks is the truest book in the Lowtowns. Names and nights. He doesn't say whose nights.",
      "GAME-ONLY (the shards as burned memory: the Sludge-Tunnels' reading)", prompt="Read the tally-board", document="doc.lowtowns.burner_tally"),
    S("price-board", "The Price-Board", "note", (88.5, 28), "A board leaned against a post, prices chalked and crossed out and chalked higher, a smear of soot over the lot.",
      "Where the long stalls meet the lane, a chalked board of prices leans on a post, every price crossed out and written higher.",
      "Nan Tallow says the price-board by the long stalls is a history of the Lowtowns. Read it top to bottom and you can watch everyone get poorer.",
      "CANON: skewered rat and aether-spiced wine sold in the Rust Market (book1 ch.3:7); the board GAME-ONLY", prompt="Read the board", document="doc.lowtowns.price_board"),
    S("old-toll", "An Old Toll-Book Page", "note", (-192, -13), "A page torn from a toll-book years ago and stuffed in a crack of the wall's inner face, the ink run, the hand neat and frightened.",
      "Inside the city's wall, just south of the gate, a page from an old toll-book is stuffed in a crack of the brick.",
      "Edda Holt says the reeve before her tore a page out of the book and hid it in the wall south of the gate. She has never gone to look. She says you might.",
      "GAME-ONLY", prompt="Pull the page from the crack", document="doc.lowtowns.old_toll"),
    S("drowned-docker", "A Docker in the Drain", "remains", (-162, 190), "Bones in a docker's leather apron, half in the black water by a drain mouth, one hand still closed round a twist of salt that did him no good. A note is buttoned in the apron.",
      "In the deepest of the courts, by a drain's mouth, a docker lies half in the black water with salt in his fist.",
      "Rudd Crake says one of his gang went into the far courts with a twist of salt to see what took his sister. He didn't come back to the quay.",
      "CANON-EXPANDED: Weave-Eaters and salt (book1 ch.20); the docker GAME-ONLY", prompt="Search the docker's apron", document="doc.lowtowns.drowned_docker", xp=180),
    S("militia-roll", "The Militia Roll", "read", (-71, -103), "A board of names cut with a knife into the wall by the well, the old militia's roll. Some names have a stroke through them. The last is Wulf's, with no stroke.",
      "Cut into the brick by the well in the Rows, a roll of names, some struck through, the last not.",
      "Old Wulf says the names by the well are the militia's. He cut them himself. He'll cut his own stroke when it's time, he says, not before.",
      "CANON: the former Lowtown militia (book1 ch.20:127); the roll GAME-ONLY", prompt="Read the names", document="doc.lowtowns.militia_roll"),
    S("harpoon-count", "The Harpooner's Count", "note", (103, 84), "A strip of amber-dyed cloth tied to a stall post, knotted all along: a knot for every Eater pinned, the old way, and a few words stitched at the end.",
      "On a stall post in the market's heart, a strip of amber cloth is knotted end to end.",
      "Ketta Rune says her grandmother's count is tied to a post in the market where she can see it. A knot for every one pinned. She's added three.",
      "CANON: bone harpoons on amber rope (book1 ch.20:177-191); the count GAME-ONLY", prompt="Untie the strip and read it", document="doc.lowtowns.harpoon_count"),
    S("spoon-slate", "The Spoon's Slate", "read", (-134.5, 21), "A slate on a nail round the side of the inn, by the cellar flap: the Spoon's debts, and a column headed BROKEN CHAIRS, very long.",
      "Round the west side of the inn, by the cellar flap, a slate of debts hangs on a nail.",
      "Mother Sall says her slate of debts hangs round the side by the cellar. Everybody owes. She says reading it is the best entertainment in the Lowtowns.",
      "CANON-EXPANDED: the Rusty Spoon (book1 ch.1:9); the slate GAME-ONLY", prompt="Read the slate", document="doc.lowtowns.spoon_slate"),
    S("warder", "A Warder Who Asked", "remains", (-160, 68), "A man in a warder's grey coat, face down behind the house where they hold people, a long time dead. Nobody moved him. His orders are folded in his glove.",
      "Behind the grey stone house by the gate, where the warders don't walk, one of their own lies face down.",
      "Sigga Nail says one of the warders asked a question about the children, and nobody saw him after. Behind the grey house, she says. Nobody goes round the back.",
      "CANON-EXPANDED: the Council's hired men; Seekers' warrants spoken of only; GAME-ONLY", prompt="Search the warder's glove", document="doc.lowtowns.warder_orders", xp=180),
    S("smog-thin", "Where the Smog Thins", "vista", (58, 206), "For a moment the smog thins and there it is, far up the mountain in the north-east: white stone and gold, towers and a dome and a spire like a needle. The Crown. It doesn't look real from down here. It isn't meant to.",
      "In the far north of the town, past the river, there is a place the smog thins enough to see the High City.",
      "Ebba Strand says if you go north past her bridge, to the open yards at the top of the town, the smog thins some days and you can see the Crown. She goes there when she's off. Just to look.",
      "CANON: the Crown, white stone and gold (world_bible.md Geography; book1 ch.3: the smog of the High City's foundries); the lookout GAME-ONLY", xp=200, radius=6),
    S("court-marks", "Marks on the Court Wall", "carving", (-121, 150), "Scratched into the brick at shoulder height: a row of strokes in fives, and over them, in a militia hand, NIGHTS WE HELD THE COURT.",
      "In the court of the tanners' drains, someone cut a tally of nights into the brick at shoulder height.",
      "Old Wulf says the militia held the tanners' court against the drains for a winter. They cut the nights in the wall. He can't bring himself to count them.",
      "CANON-EXPANDED: the former Lowtown militia (book1 ch.20:127); the marks GAME-ONLY", prompt="Look at the marks", text2=None),
    S("lamp-shrine", "The Lamp-Shrine", "shrine", (-36, -120), "A niche in a quay-side wall with a lamp in it that somebody always keeps lit, and under it a dish of coppers and shard-dust. No god's face. Just the lamp.",
      "On the riverside walk, in a niche in the wall, a lamp somebody always keeps lit sits over a dish of coppers.",
      "Rudd Crake says the dockers keep a lamp lit in the wall along the quays. A copper in the dish for the river. He says it's not for any god. It's for the lamp.",
      "GAME-ONLY", prompt="Leave a copper by the lamp", xp=60),
    S("grey-bell", "Grey-Bell", "herb", (162, -130), "In the ash behind the dead-house, where the burners tip the cinders, a few pale flowers hang their heads: grey bells, with a light in them like a held breath.",
      "Behind the house where the dead are burned for light, pale flowers grow in the tipped cinders.",
      "Mabb Soot says grey-bell grows only where the burners tip their cinders, behind the dead-house. She won't pick it. She says it remembers.",
      "GAME-ONLY: the trades' rare herb", prompt="Pick the grey-bell", item="herb.grey_bell", xp=120),
]
for s in SECRETS: s.pop("text2", None)
# The Lost Lamp's snuffed lamp at the dark end of the lane north of the Spoon (the Lantern-Eater's lane): cold, its glass frosted black.
prop("lane_lamp", "A snuffed lamp", -71.5, 56, 0, variant=2, interact="Take the cold glass", item="item.lamp_glass")
keep(-71.5, 56, 2)
for s in SECRETS: keep(s["at"]["x"], s["at"]["y"], 2.5)
for r in RESIDENTS:
    if "at" in r: keep(r["at"]["x"], r["at"]["y"], 2)
for c in CAMPS: keep(c["center"]["x"], c["center"]["y"], c["radius"] + 2)
