using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>Goods a villager carries between trades, built on the arm or the shoulder by <see cref="LoadProps"/>.</summary>
    public enum Load { None, Eggs, Bucket, Grain, Flour, Bread, Logs, Hide, Herbs, Game, Goods, Papers, Broom, Pole }

    /// <summary>
    /// An errand in a trade's day, run once a day from <see cref="at"/> (and dropped if not started by <see cref="until"/>):
    /// walk to <see cref="from"/>, pick up the <see cref="load"/>, carry it to <see cref="to"/>, hand it over (the village's stock
    /// of <see cref="good"/> there grows by <see cref="amount"/>) and say so. A task with no <see cref="to"/> is done where it is
    /// picked up (scattering feed). Places are the village's place kinds ("well", "stall", "inn"...), "home", or the hen-wife's
    /// "nest", "trough" and "pan". "{n}" in a line is the count carried. <see cref="door"/> is the kind of place the goods are
    /// handed over at when the village has one (the kitchen's back door, the bar); the stock key stays <see cref="to"/>'s.
    /// <see cref="toRole"/> sends a delivery to a place of that trade (the merchant's stall, not the baker's), and
    /// <see cref="fromRole"/> fetches from one. An errand with a <see cref="need"/> buys it for the household: it runs only while
    /// the household's purse has claimed the coin, which is paid when the goods are picked up (see <see cref="VillageEconomy"/>).
    /// </summary>
    public sealed class Errand
    {
        public string id; public float at, until; public string from, to; public Load load; public string good; public int amount = 1;
        public string door, toRole, fromRole, need;
        public string line, pickupLine; public float work = 5; public ActorPose pose = ActorPose.Work, dropPose = ActorPose.Work;
        public Errand(string id, float at, float until, string from, string to, Load load = Load.None, string good = null, string line = null, string pickupLine = null)
        { this.id = id; this.at = at; this.until = until; this.from = from; this.to = to; this.load = load; this.good = good; this.line = line; this.pickupLine = pickupLine; }
    }

    /// <summary>A block of a trade's day: where they work between two hours (place kinds; a repeat weights the choice).</summary>
    public sealed class Shift
    {
        public float from, to; public string[] places;
        public Shift(float from, float to, params string[] places) { this.from = from; this.to = to; this.places = places; }
    }

    /// <summary>A trade's whole day: its shifts and its errands. Hours are the <see cref="Crulanda.World.WorldClock"/>'s.</summary>
    public sealed class WorkDay
    {
        public Shift[] shifts; public Errand[] errands;
        public WorkDay(Shift[] shifts, params Errand[] errands) { this.shifts = shifts; this.errands = errands; }
    }

    /// <summary>
    /// Daily schedules by trade (GAME-ONLY): who works where through the day, and the errands that tie the trades together: the
    /// farmer's grain goes to the mill, the miller's flour to the bakehouse and the stall, the first loaves to the inn, the hunter's
    /// hides to the tannery and his hares to the inn's pot, wood to the forge and the inn's hearth, herbs to the stall and to Mira,
    /// the smith's and the leatherworker's wares to the stall, and the hen-wife's eggs to the inn, the stall and her own pot. The
    /// leatherworker keeps her own shop (9 to 12 and 2 to 6, the tannery yard either side, the tanned hides fetched from the yard
    /// first thing, her wares to the stall from the shop after her midday meal) and the herbalist calls in at her drying hut and
    /// hangs the evening's herbs there; in a village without them the leatherworker works the tannery yard and the herbalist
    /// passes the hut over. The innkeeper keeps the Cask's kitchen and the bar from first light to the last table; eggs, the first
    /// loaves and the hares go round the back to the kitchen's door, and the cask is rolled behind the bar. Deliveries to the stall
    /// go to a merchant's stall; the baker takes her own loaves to her own.
    /// </summary>
    public static class VillageWork
    {
        const float Dawn = 5.8f, Night = 20.6f;
        /// <summary>The innkeeper's hours: up before the hen-wives' eggs, abed through the rooms door after the last table.</summary>
        public const float InnkeeperUp = 5.4f, InnkeeperBed = 22.8f;
        /// <summary>Where goods for the inn's kitchen are handed over, and where the cask goes (place kinds; see <see cref="Errand.door"/>).</summary>
        public const string KitchenDoor = "kitchendoor", Bar = "bar";
        static readonly Dictionary<string, WorkDay> Days = new Dictionary<string, WorkDay>
        {
            { "farmer", new WorkDay(new[] { new Shift(Dawn, 12, "field", "field", "field", "field", "well", "green"), new Shift(12, 13.2f, "inn", "inn", "well"), new Shift(13.2f, 18, "field", "field", "field", "field", "well", "green"), new Shift(18, Night, "green", "inn", "home") },
                new Errand("grain to the mill", 10, 12.5f, "field", "mill", Load.Grain, "grain", "Barley. Last of it till harvest; go easy on the stone."),
                new Errand("seed-corn home", 16.5f, 18.5f, "field", "home", Load.Grain, "grain", "Seed-corn. It sleeps under my bed, where the grey-coats won't look.")) },
            { "miller", new WorkDay(new[] { new Shift(6.5f, 12, "mill", "mill", "mill", "well"), new Shift(12, 13, "inn"), new Shift(13, 17.5f, "mill", "mill", "mill", "green"), new Shift(17.5f, Night, "inn", "inn", "green") },
                new Errand("flour to the bakehouse", 11, 13.5f, "mill", "oven", Load.Flour, "flour", "Flour for the morning's loaves. Don't ask me what it tastes of."),
                new Errand("flour to the stall", 15.5f, 17.5f, "mill", "stall", Load.Flour, "flour", "Flour, ground fine. Sell it dear; there's less every year.") { toRole = "merchant" }) },
            { "baker", new WorkDay(new[] { new Shift(4.6f, 7.5f, "oven"), new Shift(7.5f, 11.5f, "stall", "stall", "stall", "oven"), new Shift(11.5f, 13, "oven", "oven", "well"), new Shift(13, 17, "stall", "stall", "stall", "oven"), new Shift(17, 19.6f, "well", "green", "home") },
                new Errand("first loaves to the inn", 7, 9.5f, "oven", "inn", Load.Bread, "bread", "First loaves, still warm. The Cask gets the best of them.") { door = KitchenDoor },
                new Errand("loaves to the stall", 12.5f, 14.5f, "oven", "stall", Load.Bread, "bread", "Second batch. Mind, the crust's dark; the flour's thin.")) },
            { "blacksmith", new WorkDay(new[] { new Shift(6.5f, 12.5f, "forge", "forge", "forge", "forge", "forge", "well"), new Shift(12.5f, 13.3f, "well", "inn"), new Shift(13.3f, 18, "forge", "forge", "forge", "forge", "forge"), new Shift(18, Night, "inn", "inn", "green") },
                new Errand("oak for the forge", 8.5f, 11, "woodpile", "forge", Load.Logs, "wood", "Oak for the fire. Charcoal's dear; oak's not.", "Dry, this. Good."),
                new Errand("ironwork to the stall", 16.8f, 18.5f, "forge", "stall", Load.Goods, "goods", "Nails, hinges and a hook. Sell what you can.") { toRole = "merchant" }) },
            { "lumberjack", new WorkDay(new[] { new Shift(Dawn, 11, "woods", "woods", "woods", "woodpile"), new Shift(11, 12, "inn", "well"), new Shift(12, 17, "woods", "woods", "woods", "woodpile"), new Shift(17, Night, "inn", "inn", "green") },
                new Errand("logs to the woodyard", 9, 11.5f, "woods", "woodpile", Load.Logs, "logs", "Two more loads and that's the big oak down."),
                new Errand("firewood to the inn", 14.5f, 16.5f, "woodpile", "inn", Load.Logs, "wood", "Firewood for the Cask. Dry, mind, not the grey stuff."),
                new Errand("firewood to the forge", 16, 18, "woodpile", "forge", Load.Logs, "wood", "For the hearth. Don't burn it all at once.")) },
            // Where he lives at a lodge (Oakhaven's Garet Moss), the hunter keeps to his lodge yard in the evening and goes to bed early
            // (Villager.HunterBed): it is a long walk out. Without one he keeps the inn and the green (see PlacesFor).
            { "hunter", new WorkDay(new[] { new Shift(Dawn, 10.5f, "woods", "woods", "woods", "meadow", "meadow", "lodge"), new Shift(10.5f, 13, "tannery", "inn", "inn"), new Shift(13, 18, "woods", "woods", "woods", "meadow", "meadow"), new Shift(18, Night, "lodge", "lodge", "inn") },
                new Errand("the hide to the tannery", 10, 12.5f, "woods", "tannery", Load.Game, "hides", "A buck's hide, and the hares are for the Cask. Pay me for the hide."),
                new Errand("hares to the inn", 11, 13.5f, "tannery", "inn", Load.Game, "meat", "Two hares for the pot. Don't let the drinkers see them.") { door = KitchenDoor }) },
            { "skinner", new WorkDay(new[] { new Shift(7, 12, "tannery", "tannery", "tannery", "well"), new Shift(12, 13, "inn", "well"), new Shift(13, 17.5f, "tannery", "tannery", "tannery", "woods"), new Shift(17.5f, Night, "green", "inn") },
                new Errand("pelts from the snares", 14.5f, 17, "woods", "tannery", Load.Hide, "hides", "Rabbit, mostly. Snares were full.", "Snares. Let's see... rabbit. Rabbit. Rabbit.")) },
            { "leatherworker", new WorkDay(new[] { new Shift(7, 9, "tannery", "leathershop"), new Shift(9, 12, "leathershop"), new Shift(12, 13, "inn"), new Shift(13, 14, "tannery", "leathershop"), new Shift(14, 18, "leathershop"), new Shift(18, Night, "green", "inn") },
                new Errand("tanned hides from the yard", 7.5f, 9, "tannery", "leathershop", Load.Hide, "leather", "Three sides, dry enough. Fen says mind the grey one."),
                new Errand("leather to the stall", 12.2f, 14, "leathershop", "stall",Load.Goods, "goods", "Belts, a bridle, two purses. Coin or trade.") { toRole = "merchant" }) },
            { "herbalist", new WorkDay(new[] { new Shift(Dawn, 10.5f, "meadow", "meadow", "meadow", "woods"), new Shift(10.5f, 12.5f, "stall", "inn", "green", "dryhut"), new Shift(12.5f, 16.5f, "meadow", "meadow", "woods", "woods"), new Shift(16.5f, Night, "green", "green", "inn", "dryhut") },
                new Errand("herbs to the stall", 10.3f, 12.5f, "meadow", "stall", Load.Herbs, "herbs", "Comfrey and yarrow for the stall. The marigold's for Mira.") { toRole = "merchant" },
                new Errand("marigold for Mira", 11, 13.5f, "stall", "inn", Load.Herbs, "herbs", "Marigold and comfrey, for whoever's bleeding this week."),
                new Errand("herbs to dry", 16.3f, 18.5f, "meadow", "dryhut", Load.Herbs, "herbs")) },
            { "merchant", new WorkDay(new[] { new Shift(7.3f, 12.5f, "stall", "stall", "stall", "stall", "green"), new Shift(12.5f, 13.3f, "well", "inn"), new Shift(13.3f, 18, "stall", "stall", "stall", "stall"), new Shift(18, Night, "inn", "inn", "green") },
                new Errand("a cask for the inn", 8.6f, 11, "stall", "inn", Load.Goods, "ale", "Ale for the Cask. Make it last the night, for once.") { amount = 12, door = Bar },
                new Errand("shutters up", 17.8f, 19.5f, "stall", "home", Load.Goods, "goods", "Shutters up. What didn't sell comes home.")) },
            { "gossip", new WorkDay(new[] { new Shift(Dawn, 8, "well", "well", "green"), new Shift(8, 12, "green", "green", "green", "well"), new Shift(12, 13.5f, "inn", "inn", "well"), new Shift(13.5f, 17, "green", "green", "well", "wander"), new Shift(17, Night, "inn", "green", "home") },
                new Errand("the morning's water", 6.5f, 8.5f, "well", "home", Load.Bucket, "water", "Water's bitter again. Iron, my gran says."),
                new Errand("the evening's water", 16.5f, 18.5f, "well", "home", Load.Bucket, "water")) },
            { "child", new WorkDay(new[] { new Shift(6.5f, 11.5f, "green", "green", "wander", "wander", "well"), new Shift(11.5f, 13, "home"), new Shift(13, 17, "green", "green", "wander", "wander", "well"), new Shift(17, 20, "green", "home") },
                new Errand("a loaf for Mum", 11, 13, "oven", "home", Load.Bread, "bread", null, "Mum says a loaf, and no eating the crust.") { need = "bread" },
                new Errand("chores", 16.5f, 18.5f, "well", "home", Load.Bucket, "water", "Chores.")) },
            { "elder", new WorkDay(new[] { new Shift(7, 12, "green", "green", "green", "inn"), new Shift(12, 14, "inn", "inn", "green"), new Shift(14, 18, "green", "green", "green", "inn"), new Shift(18, Night, "inn", "home") }) },
            // The out of work (the drinkers, and any trade whose workplace the village lacks): loiter the morning away, then the inn, where
            // they drink until the ale is gone or they are (Villager.DrinkRound).
            { "drinker", new WorkDay(new[] { new Shift(7.5f, 11, "wander", "green", "well"), new Shift(11, 23.6f, "inn") }) },
            // The hen-wife: feed at first light and again mid-afternoon, water from the well through the day, eggs three times:
            // the first to the inn's kitchen, the second to the produce stall, the last home for the pot. Dusk is the hens' (Villager.KeeperNext).
            { "henwife", new WorkDay(new[] { new Shift(5.7f, 12.2f, "yard", "yard", "yard", "well", "green"), new Shift(12.2f, 13.5f, "home"), new Shift(13.5f, 18, "yard", "yard", "yard", "well", "green"), new Shift(18, 19.3f, "yard") },
                new Errand("the morning feed", 5.7f, 9.6f, "trough", null, Load.None, null, "Chook-chook-chook-chook!") { work = 16 },
                new Errand("eggs to the inn", 8.4f, 11.5f, "nest", "inn", Load.Eggs, "eggs", "{n} eggs for the Cask's kitchen. Mind, they're warm yet.") { door = KitchenDoor },
                new Errand("water for the hens", 9.6f, 12, "well", "pan", Load.Bucket, "water", "There. Drink up, girls.", "Bitter or not, they'll drink it."),
                new Errand("the afternoon's water", 13.5f, 15.5f, "well", "pan", Load.Bucket, "water", "There. Drink up, girls."),
                new Errand("the afternoon feed", 14.6f, 17, "trough", null, Load.None, null, "Chook-chook-chook-chook!") { work = 16 },
                new Errand("eggs to the stall", 15.4f, 17.2f, "nest", "stall", Load.Eggs, "eggs", "{n} eggs, fresh. And don't start about the Concord's tax.") { toRole = "merchant" },
                new Errand("eggs for the pot", 17.2f, 18.6f, "nest", "home", Load.Eggs, "eggs", "{n} for the pot.")) },
            // The shepherd (GAME-ONLY; Chris 2026-10-14): the flock counted at the gate at first light, sheared through the morning, the
            // fleeces to the produce stall after the midday meal, fed in the afternoon, and the fold until dusk.
            { "shepherd", new WorkDay(new[] { new Shift(5.8f, 12.2f, "fold", "fold", "foldgate", "well"), new Shift(12.2f, 13.5f, "home"), new Shift(13.5f, 18.5f, "fold", "fold", "foldgate", "green"), new Shift(18.5f, Night, "foldgate", "home") },
                new Errand("the morning count", 5.8f, 8, "foldgate", null, Load.None, null, "Hup, hup. Thirty... thirty-one. All there.") { work = 14, pose = ActorPose.Talk },
                new Errand("the shearing", 8.2f, 11.6f, "fold", null, Load.None, null, "Hold still, you great lump. There.") { work = 34, pose = ActorPose.Gather },
                new Errand("fleeces to the stall", 13.5f, 16, "fold", "stall", Load.Goods, "wool", "{n} fleeces off the fold. Washed, mostly.") { toRole = "merchant", pose = ActorPose.Gather },
                new Errand("the afternoon feed", 15.5f, 17.5f, "foldtrough", null, Load.None, null, "Hup! Hup! Come on, then, before the crows do.") { work = 14 }) },
            // The innkeeper (GAME-ONLY; Oakhaven's Hob Linden, who sleeps over the Golden Cask): the kitchen from first light, then the
            // bar and the kitchen turn about until the last table. Water for the pot, the pot on, and dinner out to the tables.
            { "innkeeper", new WorkDay(new[] { new Shift(InnkeeperUp, 7, "kitchen"), new Shift(7, 11.5f, "kitchen", "kitchen", "bar"), new Shift(11.5f, 14, "bar", "bar", "kitchen"), new Shift(14, 17, "kitchen", "bar", "well"), new Shift(17, InnkeeperBed, "bar", "bar", "bar", "kitchen") },
                new Errand("water for the pot", 6, 7.5f, "well", "kitchen", Load.Bucket, "water", "Bitter or not, it boils."),
                new Errand("the pot on", 10.5f, 12.5f, "kitchen", null, Load.None, null, "Pot's on. Hare if Garet's been by; barley if he hasn't.") { work = 14 },
                new Errand("dinner to the tables", 12.2f, 14, "kitchen", "inn", Load.Bread, "dinner", "Dinner. Mind the bowl; it's hotter than it looks.")) },

            // ---------- the Crown's trades (GAME-ONLY, p9-cityinn; Chris, 2026-10-16: "will need lots more npcs in the castle with real jobs
            // like in oakhaven") ----------
            // The places are the city's own (ZoneBuilder.Crown registers them): the Council Keep's desks ("keep"), the Council Hall
            // ("councilhall"), the Archive's desks ("archive"), the Exchange's floor ("exchange"), the Treasury's door and counter
            // ("treasury"), the guild halls ("guildhall"), the Gilt Gate's inner yard ("gate"), the street lamps ("lamp"), the parterres
            // ("garden"), the streets ("street", along the roads), the fountains (as "well"), and the Gilded Lantern's kitchen, bar and
            // floor ("kitchen", "kitchendoor", "bar", "serve", "inn"). Papers go between the Keep, the Hall and the Archive; crates
            // between the gate, the Exchange, the Treasury and the Row; water from the fountains; the lamps are lit at dusk.
            { "clerk", new WorkDay(new[] { new Shift(7.5f, 12, "keep", "keep", "councilhall", "archive"), new Shift(12, 13, "inn", "inn", "green"), new Shift(13, 17.5f, "keep", "councilhall", "councilhall", "archive"), new Shift(17.5f, Night, "inn", "green", "home") },
                new Errand("the morning's edicts", 8, 10.5f, "keep", "councilhall", Load.Papers, "papers", "Today's edicts, for the board. Mind the seal; the Herald counts them."),
                new Errand("copies for the Archive", 10.5f, 12, "councilhall", "archive", Load.Papers, "papers", "Copies for the shelves. Three of everything, as the charter says."),
                new Errand("the Archive's answer", 14, 16, "archive", "keep", Load.Papers, "papers", "The Archive's answer. It says no, at some length."),
                new Errand("petitions to the Keep", 15.8f, 17.4f, "councilhall", "keep", Load.Papers, "papers", "The day's petitions. Most of them will be read.")) },
            { "scribe", new WorkDay(new[] { new Shift(7, 12, "archive", "archive", "archive", "keep"), new Shift(12, 13, "inn", "green"), new Shift(13, 18, "archive", "archive", "archive", "councilhall"), new Shift(18, Night, "inn", "inn", "home") },
                new Errand("a register to the Keep", 9.5f, 11.5f, "archive", "keep", Load.Papers, "papers", "The register, copied fair. My hand aches in Council ink."),
                new Errand("the day's copies", 16, 18, "archive", "councilhall", Load.Papers, "papers", "The day's copies, for the Hall's chest.")) },
            { "page", new WorkDay(new[] { new Shift(7, 12, "keep", "green", "exchange", "treasury", "guildhall"), new Shift(12, 13, "inn", "green"), new Shift(13, 18, "keep", "green", "street", "exchange", "guildhall"), new Shift(18, 19.6f, "green", "home") },
                new Errand("a letter to the Exchange", 7.6f, 9.5f, "keep", "exchange", Load.Papers, "papers", "From the Keep. Sealed, so don't ask me what it says."),
                new Errand("a summons to the Row", 9.5f, 11.5f, "keep", "guildhall", Load.Papers, "papers", "The Council asks for your master. Asks. Nicely."),
                new Errand("the Treasury's tally", 13.2f, 15, "treasury", "keep", Load.Papers, "papers", "The Treasury's tally, for the Clerk of the Council."),
                new Errand("the Exchange's lists", 15, 17, "exchange", "archive", Load.Papers, "papers", "The day's sales, for the record. Everything's for the record.")) },
            { "lamplighter", new WorkDay(new[] { new Shift(8.5f, 12, "street", "lamp", "green", "well"), new Shift(12, 14, "inn", "inn", "green"), new Shift(14, 17.6f, "lamp", "street", "green", "inn"), new Shift(17.6f, 18.5f, "inn"), new Shift(18.5f, 22, "lamp", "lamp", "lamp", "street"), new Shift(22, 23.4f, "inn", "inn", "home") },
                new Errand("wicks trimmed", 9, 11.5f, "lamp", "lamp", Load.Pole, null, "Trimmed. A clean wick burns gold; a dirty one burns Lowtown."),
                new Errand("the Way's lamps", 18.6f, 20, "lamp", "lamp", Load.Pole, null, "There. The Way's lit from the gate to the Stair."),
                new Errand("the Square's lamps", 19.2f, 21, "lamp", "lamp", Load.Pole, null, "The Square's done. The Council likes it bright before the bells."),
                new Errand("the Row's lamps", 19.8f, 21.6f, "lamp", "lamp", Load.Pole, null, "Last of them. The Row can drink by its own light now.")) },
            { "gardener", new WorkDay(new[] { new Shift(6, 12, "garden", "garden", "garden", "well"), new Shift(12, 13, "inn", "green"), new Shift(13, 17.5f, "garden", "garden", "garden", "well"), new Shift(17.5f, Night, "inn", "home") },
                new Errand("water for the beds", 6.6f, 9, "well", "garden", Load.Bucket, "fountain", "There. Nothing grows white and gold on its own."),
                new Errand("lilies for the Hall", 9.5f, 11.5f, "garden", "councilhall", Load.Herbs, "flowers", "White lilies for the Council's table. White, as ordered."),
                new Errand("the evening's water", 15.4f, 17.2f, "well", "garden", Load.Bucket, "fountain")) },
            { "sweeper", new WorkDay(new[] { new Shift(5.8f, 11.5f, "street", "street", "street", "street", "green"), new Shift(11.5f, 12.5f, "inn", "well"), new Shift(12.5f, 17, "street", "street", "street", "green"), new Shift(17, Night, "inn", "green", "home") },
                new Errand("the Way swept", 6, 9, "street", "street", Load.Broom, null, "The Way's clean. It'll be dust again by noon; the carts see to that."),
                new Errand("the Square swept", 13, 15.5f, "street", "green", Load.Broom, null, "Petitioners, pigeons and petals. Same every day.")) },
            { "watercarrier", new WorkDay(new[] { new Shift(6, 11.5f, "well", "well", "well", "green"), new Shift(11.5f, 12.5f, "inn"), new Shift(12.5f, 17, "well", "well", "green", "street"), new Shift(17, Night, "inn", "green", "home") },
                new Errand("water for the Lantern", 6.2f, 8.4f, "well", "inn", Load.Bucket, "fountain", "Water for the kitchen. Fountain-sweet, not Lowtown grey.") { door = KitchenDoor },
                new Errand("water for the Keep", 8.6f, 11, "well", "keep", Load.Bucket, "fountain", "Fresh for the Keep's ewers. The Councilor drinks it cold."),
                new Errand("water for the Hall", 13, 15, "well", "councilhall", Load.Bucket, "fountain", "For the Hall's basins. They wash their hands a great deal, up here."),
                new Errand("water home", 16.4f, 18.4f, "well", "home", Load.Bucket, "fountain")) },
            // The Gilded Lantern's staff: the cook keeps the kitchen from first light, the servers carry to the tables, the pot-boy fetches
            // and clears; Corin Bellweather keeps the bar (his post).
            { "cook", new WorkDay(new[] { new Shift(5.6f, 11.5f, "kitchen", "kitchen", "kitchen", "serve"), new Shift(11.5f, 14, "kitchen", "kitchen", "serve"), new Shift(14, 17, "kitchen", "inn", "well"), new Shift(17, 22.4f, "kitchen", "kitchen", "kitchen", "serve") },
                new Errand("the pot on", 10.2f, 12, "kitchen", null, Load.None, null, "Pot's on. Capon for the Councillors' clerks; barley for the rest.") { work = 14 },
                new Errand("the noon dinners", 12.2f, 13.8f, "kitchen", "inn", Load.Bread, "dinner", "Dinner. Mind the dish; it's the good silver.")) },
            { "server", new WorkDay(new[] { new Shift(7, 11.5f, "serve", "serve", "kitchen", "inn"), new Shift(11.5f, 15, "serve", "serve", "kitchen"), new Shift(15, 17, "inn", "serve", "green"), new Shift(17, 22.6f, "serve", "serve", "serve", "kitchen") },
                new Errand("the noon trays", 12, 13.6f, "kitchen", "inn", Load.Bread, "dinner", "Your dinner. Bread's warm; the butter's from the Vale."),
                new Errand("the supper trays", 18.4f, 20.2f, "kitchen", "inn", Load.Bread, "dinner", "Supper. Corin says the second jug's on the house. It isn't."),
                new Errand("the late trays", 20.4f, 22, "kitchen", "inn", Load.Bread, "dinner", "Last of the kitchen. Cook's gone to sit down.")) },
            { "potboy", new WorkDay(new[] { new Shift(6, 11.5f, "kitchen", "serve", "well"), new Shift(11.5f, 15, "serve", "kitchen", "inn"), new Shift(15, 17, "green", "well", "serve"), new Shift(17, 21.5f, "serve", "kitchen", "kitchen") },
                new Errand("water for the kitchen", 6.4f, 8.4f, "well", "kitchen", Load.Bucket, "fountain", "Water, Cook. Two buckets, no spilling. Well. Hardly any."),
                new Errand("pots from the tables", 14, 15.6f, "inn", "kitchen", Load.Goods, "pots", "Pots. Somebody's left a spoon in the gravy. A silver one."),
                new Errand("the evening's water", 16, 17.8f, "well", "kitchen", Load.Bucket, "fountain")) },
            { "porter", new WorkDay(new[] { new Shift(7, 12, "exchange", "exchange", "gate", "treasury"), new Shift(12, 13, "inn", "green"), new Shift(13, 18, "exchange", "gate", "treasury", "guildhall"), new Shift(18, Night, "inn", "inn", "home") },
                new Errand("crates up from the gate", 7.6f, 10, "gate", "exchange", Load.Goods, "goods", "Up from the gate. Copper, by the weight of it. My back says copper."),
                new Errand("the Exchange's coin", 10.6f, 12, "exchange", "treasury", Load.Goods, "coin", "The morning's coin, for the vault. Two wardens walked behind me the whole way."),
                new Errand("lots to the Row", 13.6f, 15.6f, "exchange", "guildhall", Load.Goods, "goods", "Your hall's lot from the Exchange. Paid for. Sign here."),
                new Errand("a crate from the gate", 15.6f, 17.6f, "gate", "exchange", Load.Goods, "goods", "Glass from the coast. Don't drop it, they said. As if I drop things.")) },
            // The Treasury's guards change the watch at six and at six: the day guard at the vault from first light, the night guard
            // through the dark (her bed is by day; VillageWork.HoursOf).
            { "vaultguard", new WorkDay(new[] { new Shift(5.8f, 17.9f, "treasury", "treasury", "treasury", "treasury", "treasury", "gate"), new Shift(17.9f, Night, "inn", "inn", "home") },
                new Errand("the watch taken", 5.8f, 6.6f, "treasury", null, Load.None, null, "The watch is mine. Go home and sleep, you look like the Lowtowns.") { work = 10, pose = ActorPose.Talk },
                new Errand("the watch handed over", 17.6f, 18.6f, "treasury", null, Load.None, null, "Watch is yours. Vault's shut and the clerk's gone home.") { work = 10, pose = ActorPose.Talk }) },
            { "nightguard", new WorkDay(new[] { new Shift(16.4f, 17.8f, "inn", "green"), new Shift(17.8f, 24, "treasury", "treasury", "treasury"), new Shift(0, 6.2f, "treasury"), new Shift(6.2f, 8.2f, "inn", "home") },
                new Errand("the watch taken", 17.8f, 18.8f, "treasury", null, Load.None, null, "Mine till dawn, then. Go and eat; you've earned it.") { work = 10, pose = ActorPose.Talk },
                new Errand("the watch handed over", 5.6f, 6.6f, "treasury", null, Load.None, null, "Quiet night. Nobody tried the door. Nobody ever tries the door.") { work = 10, pose = ActorPose.Talk }) },
            { "steward", new WorkDay(new[] { new Shift(7.5f, 12, "guildhall", "guildhall", "guildhall", "exchange"), new Shift(12, 13, "inn"), new Shift(13, 18, "guildhall", "guildhall", "exchange", "treasury"), new Shift(18, Night, "guildhall", "inn", "home") },
                new Errand("the hall's order", 9, 11, "guildhall", "exchange", Load.Papers, "papers", "The hall's order. Bandages, oil, and whatever the Hounds broke this week."),
                new Errand("the hall's dues", 15, 17, "guildhall", "treasury", Load.Goods, "coin", "The hall's dues, counted twice. Count them again; I'll wait.")) },
            { "trader", new WorkDay(new[] { new Shift(7.5f, 12, "exchange", "exchange", "exchange", "green"), new Shift(12, 13, "inn"), new Shift(13, 18, "exchange", "exchange", "treasury", "green"), new Shift(18, Night, "inn", "inn", "home") },
                new Errand("samples to the Exchange", 8, 10, "home", "exchange", Load.Goods, "goods", "Samples, for the floor. The rest is still on the road from the coast."),
                new Errand("the day's takings", 16.4f, 18, "exchange", "treasury", Load.Goods, "coin", "The day's takings. A good day; don't tell the tithe-clerk.")) },
            { "patron", new WorkDay(new[] { new Shift(8.5f, 12, "exchange", "exchange", "green", "garden", "treasury"), new Shift(12, 13.6f, "inn", "inn", "green"), new Shift(13.6f, 17.5f, "exchange", "garden", "green", "guildhall"), new Shift(17.5f, 21.4f, "inn", "inn", "green", "home") },
                new Errand("a parcel home", 15, 17, "exchange", "home", Load.Goods, null, "Bought it. Don't ask what it cost; I've stopped asking myself.")) },
            { "noble", new WorkDay(new[] { new Shift(9, 12, "green", "garden", "garden", "councilhall", "street"), new Shift(12, 14, "inn", "inn", "garden"), new Shift(14, 18, "garden", "green", "exchange", "councilhall"), new Shift(18, 22, "inn", "inn", "green", "home") }) },
            { "servant", new WorkDay(new[] { new Shift(6, 9, "well", "home", "street"), new Shift(9, 12, "exchange", "green", "garden"), new Shift(12, 13, "home"), new Shift(13, 18, "exchange", "garden", "green", "street"), new Shift(18, Night, "home", "inn") },
                new Errand("the house's water", 6.3f, 8.5f, "well", "home", Load.Bucket, "fountain", "Water for the house. The fountain's, not the cistern's; the mistress can tell."),
                new Errand("the master's letters", 9.2f, 11, "home", "keep", Load.Papers, "papers", "Letters for the Keep, from the house. Petitions, mostly, dressed as compliments."),
                new Errand("a parcel from the Exchange", 14, 16.5f, "exchange", "home", Load.Goods, null, "Silk from the coast for the mistress. Third this month.")) },
        };
        /// <summary>The Crown's trades (above), in order: each has a working day here and a place of its own in a city.</summary>
        public static readonly string[] CityRoles = { "clerk", "scribe", "page", "lamplighter", "gardener", "sweeper", "watercarrier", "cook", "server", "potboy", "porter", "vaultguard", "nightguard", "steward", "trader", "patron", "noble", "servant" };
        /// <summary>When a trade goes to bed and gets up, where it is not the villagers' own (VillageLife's Villager.Spawn): the lamplighter
        /// is out till the last lamp, the inn's staff last to bed, the night guard abed by day. Null: the usual hours.</summary>
        public static Vector2? HoursOf(string role)
        {
            switch (role)
            {
                case "lamplighter": return new Vector2(23.4f, 8.2f);
                case "cook": return new Vector2(22.4f, 5.4f);
                case "server": return new Vector2(22.6f, 6.8f);
                case "potboy": return new Vector2(21.5f, 5.8f);
                case "nightguard": return new Vector2(8.2f, 16.4f);
                case "noble": return new Vector2(22.2f, 8.6f);
                case "patron": return new Vector2(21.4f, 8.2f);
                default: return null;
            }
        }
        /// <summary>The outfit a city trade wears (ActorVisual's trades): the nobles, stewards, traders and their customers in a
        /// merchant's coat and feathered hat, the inn's staff in the innkeeper's apron, the gardener in the herbalist's; the rest plain.</summary>
        public static string OutfitOf(string role)
        {
            switch (role)
            {
                case "noble": case "steward": case "trader": case "patron": return "merchant";
                case "cook": case "server": case "potboy": return "innkeeper";
                case "gardener": return "herbalist";
                default: return role;
            }
        }
        /// <summary>Every place a trade's day sends it to (its shifts' places and its errands' ends), but home and the coop's and fold's.</summary>
        public static List<string> PlacesOf(string role)
        {
            var list = new List<string>(); var d = DayFor(role); if (d == null) return list;
            void Add(string p) { if (p != null && p != "home" && !list.Contains(p)) list.Add(p); }
            foreach (var s in d.shifts) foreach (var p in s.places) Add(p);
            foreach (var e in d.errands) { Add(e.from); Add(e.to); Add(e.door); }
            return list;
        }
        public static WorkDay DayFor(string role) { return Days.TryGetValue(role, out var d) ? d : null; }
        /// <summary>
        /// A household's shopping (GAME-ONLY; ADDENDUM C.2): bread from the baker's stall, a bundle from the woodyard, eggs from a
        /// merchant's stall, each carried home in its window. Run by a member with hands to spare, and only while the purse has the
        /// coin claimed for it; a household with nobody to send (one person, or a farm out past the village) buys on the books at
        /// the hour the window opens. "{seller}" in the pick-up line is the seller's first name. Goods carried home deliver no stock.
        /// </summary>
        public static readonly Errand[] Shopping = {
            new Errand("bread for the house", 9, 18, "stall", "home", Load.Bread, null, "Bread for the board.", "A loaf, {seller}. The small one.") { need = "bread", fromRole = "baker" },
            new Errand("firewood for the hearth", 14, 19.5f, "woodpile", "home", Load.Logs, null, "Wood for the fire. We'll be warm tonight.", "A bundle, {seller}. Dry, if you've any.") { need = "firewood" },
            new Errand("eggs for the house", 15.5f, 18.5f, "stall", "home", Load.Eggs, null, "Eggs for the pot.", "Three eggs, {seller}. Whatever the hens could spare.") { need = "eggs", fromRole = "merchant", amount = 3 } };
        /// <summary>The shopping errand that fetches a need, or null.</summary>
        public static Errand ShopFor(string need) { return System.Array.Find(Shopping, e => e.need == need); }
        /// <summary>What the member who would have gone says when the household cannot stretch to a need.</summary>
        public static string ShortLine(string need, bool child)
        {
            switch (need)
            {
                case "bread": return child ? "Mum says there's no loaf today." : "No bread today. The heel of yesterday's will have to do.";
                case "firewood": return "Can't stretch to firewood today.";
                case "eggs": return "No eggs this week. Coin's thin.";
                default: return null;
            }
        }
        /// <summary>The shift a trade is on at an hour, or null when their day has no shift for it (bed, or a role without a day).</summary>
        public static Shift ShiftFor(string role, float hour)
        {
            var d = DayFor(role); if (d == null) return null;
            foreach (var s in d.shifts) if (hour >= s.from && hour < s.to) return s;
            return null;
        }
        static readonly string[] NoLodgeEvening = { "inn", "inn", "green" };
        /// <summary>The places a shift sends a trade to in a village: a hunter whose village has no lodge passes it over by day and
        /// spends the evening at the inn and on the green.</summary>
        public static string[] PlacesFor(Shift s, string role, bool hasLodge)
        {
            if (s == null || role != "hunter" || hasLodge || System.Array.IndexOf(s.places, "lodge") < 0) return s?.places;
            return s.from >= 18 ? NoLodgeEvening : System.Array.FindAll(s.places, p => p != "lodge");
        }
        /// <summary>What whoever takes a delivery says.</summary>
        public static string Reply(string good, float r)
        {
            switch (good)
            {
                case "eggs": return r < .5f ? "Lovely. Put them by the hearth." : "Ta, Goody. Warm, are they?";
                case "grain": return r < .5f ? "On the pile. Thin, is it?" : "Barley. Right. The stone's waiting.";
                case "flour": return r < .5f ? "Ta. Sacks by the oven." : "That all of it?";
                case "bread": return r < .5f ? "Still warm. Good." : "Put them on the board.";
                case "logs": case "wood": return r < .5f ? "Stack it by the wall." : "Dry? Good. By the wall.";
                case "hides": return r < .5f ? "Let's see it... Fair. I'll pay fair." : "Grey at the edges, this one.";
                case "meat": return r < .5f ? "Hares. Good. Stew tonight." : "Round the back, before the drinkers see.";
                case "herbs": return r < .5f ? "Put them to dry." : "Comfrey. Bless you.";
                case "goods": return r < .5f ? "Leave it on the counter." : "I'll see what sells.";
                case "ale": return r < .5f ? "About time. They're through the last one." : "Roll it behind the bar.";
                case "dinner": return r < .5f ? "Stew again? ...Go on, then." : "Smells like hare. Bless you.";
                // The Crown's (GAME-ONLY, p9-cityinn).
                case "papers": return r < .5f ? "On the pile. Is it sealed? Then it waits its turn." : "Initial here. And here. And here.";
                case "coin": return r < .5f ? "Counted and entered. The vault thanks you." : "Two wardens saw it in. Good.";
                case "flowers": return r < .5f ? "Lilies. The Councillors won't notice, but they'd notice if they weren't there." : "Put them by the lectern.";
                case "pots": return r < .5f ? "In the tub. Mind the good silver." : "About time. I'm down to the chipped ones.";
                case "fountain": return r < .5f ? "Set it down. Fountain water?" : "Ta. By the basin.";
                default: return null;
            }
        }
    }

    /// <summary>Carried goods built from primitives at the hand or on the shoulder of a villager (the root sits at hip height).</summary>
    public static class LoadProps
    {
        static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        static Material Mat(Color c)
        {
            if (!mats.TryGetValue(c, out var m)) { m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", .1f); mats[c] = m; }
            return m;
        }
        static Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3? euler = null)
        {
            var o = GameObject.CreatePrimitive(type); Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale;
            if (euler.HasValue) o.transform.localEulerAngles = euler.Value;
            var r = o.GetComponent<Renderer>(); r.sharedMaterial = Mat(c); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return o.transform;
        }
        static readonly Color Wicker = new Color(.55f, .4f, .22f), Timber = new Color(.36f, .26f, .16f), Sack = new Color(.68f, .6f, .46f), Water = new Color(.35f, .5f, .62f);
        /// <summary>Build a load under <paramref name="owner"/>; <paramref name="count"/> is how many (eggs in the basket).</summary>
        /// <summary>A drinker's tankard, hung from the right arm's pivot at the hand and tilted so that lifting the arm tips it to the mouth.</summary>
        public static GameObject Tankard(Transform arm)
        {
            var go = new GameObject("Tankard"); var t = go.transform; t.SetParent(arm, false); t.localPosition = new Vector3(0, -.64f, .1f); t.localEulerAngles = new Vector3(55, 0, 0);
            Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(.11f, .075f, .11f), new Color(.46f, .33f, .19f));
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .078f, 0), new Vector3(.118f, .006f, .118f), new Color(.62f, .6f, .56f));   // the pewter rim
            Part(PrimitiveType.Cylinder, t, new Vector3(0, .07f, 0), new Vector3(.095f, .004f, .095f), new Color(.86f, .8f, .62f));    // the head on the ale
            Part(PrimitiveType.Cube, t, new Vector3(.075f, 0, 0), new Vector3(.03f, .09f, .02f), new Color(.36f, .26f, .16f));         // the handle
            return go;
        }
        public static GameObject Build(Transform owner, Load load, int count)
        {
            var go = new GameObject("Load " + load); go.transform.SetParent(owner, false); var t = go.transform;
            var hand = new Vector3(.42f, -.1f, .15f); var shoulder = new Vector3(.28f, .5f, -.02f);
            switch (load)
            {
                case Load.Eggs:
                    t.localPosition = hand;
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(.34f, .1f, .26f), Wicker);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, .12f, 0), new Vector3(.3f, .02f, .22f), new Color(.45f, .32f, .18f));
                    for (int i = 0; i < Mathf.Clamp(count, 0, 6); i++)
                        Part(PrimitiveType.Sphere, t, new Vector3((i % 3 - 1) * .08f, .14f, (i / 3) * .07f - .035f), new Vector3(.07f, .09f, .07f), i % 3 == 1 ? new Color(.78f, .6f, .42f) : new Color(.95f, .92f, .85f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, .12f, 0), new Vector3(.02f, .3f, .02f), Wicker, new Vector3(0, 0, 0));   // the handle, up the arm
                    break;
                case Load.Bucket:
                    t.localPosition = new Vector3(.42f, -.42f, .12f);
                    Part(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(.26f, .15f, .26f), Timber);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, .13f, 0), new Vector3(.22f, .01f, .22f), Water);
                    Part(PrimitiveType.Cube, t, new Vector3(0, .22f, 0), new Vector3(.02f, .2f, .02f), new Color(.3f, .3f, .32f));
                    break;
                case Load.Grain: case Load.Flour:
                {
                    // Burlap for grain (dark enough to read against skin and shirt), a floury pale sack for flour; a tied neck.
                    var c = load == Load.Flour ? new Color(.9f, .87f, .8f) : new Color(.5f, .4f, .26f);
                    t.localPosition = shoulder + new Vector3(.04f, .04f, .1f); t.localEulerAngles = new Vector3(0, 0, -30);
                    Part(PrimitiveType.Capsule, t, Vector3.zero, new Vector3(.34f, .36f, .32f), c);
                    Part(PrimitiveType.Sphere, t, new Vector3(0, .38f, 0), new Vector3(.13f, .11f, .13f), c * .75f);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, .33f, 0), new Vector3(.2f, .015f, .2f), new Color(.3f, .22f, .14f));   // the cord
                    break;
                }
                case Load.Bread:
                    t.localPosition = new Vector3(0, .05f, .42f);
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(.52f, .03f, .3f), Timber);
                    for (int i = 0; i < 3; i++) Part(PrimitiveType.Capsule, t, new Vector3((i - 1) * .16f, .07f, 0), new Vector3(.12f, .1f, .12f), new Color(.72f, .5f, .28f), new Vector3(90, 0, 0));
                    break;
                case Load.Logs:
                    t.localPosition = shoulder; t.localEulerAngles = new Vector3(0, 0, -10);
                    foreach (var o in new[] { new Vector3(0, 0, 0), new Vector3(.1f, .05f, .08f), new Vector3(-.02f, .1f, -.06f) })
                        Part(PrimitiveType.Cylinder, t, o, new Vector3(.12f, .45f, .12f), new Color(.4f, .28f, .17f), new Vector3(90, 0, 0));
                    break;
                case Load.Hide:
                    t.localPosition = new Vector3(.42f, -.05f, .12f);
                    Part(PrimitiveType.Cube, t, new Vector3(0, 0, .12f), new Vector3(.14f, .05f, .36f), new Color(.6f, .45f, .3f), new Vector3(0, 0, 0));
                    Part(PrimitiveType.Cube, t, new Vector3(0, -.2f, .28f), new Vector3(.14f, .4f, .05f), new Color(.6f, .45f, .3f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, -.2f, -.04f), new Vector3(.14f, .4f, .05f), new Color(.52f, .38f, .26f));
                    break;
                case Load.Herbs:
                    t.localPosition = new Vector3(.42f, -.12f, .15f); t.localEulerAngles = new Vector3(0, 0, 25);
                    for (int i = 0; i < 4; i++)
                    {
                        var p = new Vector3((i - 1.5f) * .04f, .12f, (i % 2) * .04f);
                        Part(PrimitiveType.Cylinder, t, p, new Vector3(.025f, .22f, .025f), new Color(.35f, .5f, .25f));
                        Part(PrimitiveType.Sphere, t, p + new Vector3(0, .24f, 0), new Vector3(.08f, .06f, .08f), i == 1 ? new Color(.9f, .7f, .25f) : i == 2 ? new Color(.85f, .85f, .8f) : new Color(.4f, .6f, .3f));
                    }
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, -.02f, .02f), new Vector3(.1f, .03f, .1f), new Color(.5f, .4f, .25f));
                    break;
                case Load.Game:
                    t.localPosition = new Vector3(.42f, -.45f, .12f);
                    Part(PrimitiveType.Capsule, t, Vector3.zero, new Vector3(.14f, .2f, .14f), new Color(.45f, .35f, .25f));
                    foreach (int s in new[] { -1, 1 }) Part(PrimitiveType.Capsule, t, new Vector3(s * .035f, -.25f, 0), new Vector3(.04f, .08f, .03f), new Color(.45f, .35f, .25f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, .25f, 0), new Vector3(.015f, .14f, .015f), new Color(.6f, .55f, .45f));   // the string it hangs by
                    break;
                case Load.Goods:
                    t.localPosition = new Vector3(0, 0, .4f);
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(.42f, .3f, .3f), Timber);
                    Part(PrimitiveType.Cube, t, new Vector3(0, .16f, 0), new Vector3(.44f, .02f, .32f), new Color(.5f, .38f, .24f));
                    foreach (float x in new[] { -.14f, .14f }) Part(PrimitiveType.Cube, t, new Vector3(x, 0, -.16f), new Vector3(.03f, .32f, .01f), new Color(.5f, .38f, .24f));
                    break;
                // The Crown's loads (GAME-ONLY, p9-cityinn): a clerk's bundle of papers under a ledger, tied with the Council's white
                // ribbon and sealed in red; a sweeper's besom; a lamplighter's long pole, its wick-hook lit at the top.
                case Load.Papers:
                    t.localPosition = new Vector3(0, .05f, .38f);
                    Part(PrimitiveType.Cube, t, Vector3.zero, new Vector3(.34f, .05f, .26f), new Color(.36f, .2f, .12f));   // the ledger's boards
                    for (int i = 0; i < 4; i++) Part(PrimitiveType.Cube, t, new Vector3((i % 2) * .02f - .01f, .04f + i * .022f, (i % 3) * .015f), new Vector3(.3f, .02f, .22f), new Color(.94f, .92f, .84f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, .07f, 0), new Vector3(.36f, .11f, .025f), new Color(.97f, .96f, .92f));   // the ribbon
                    Part(PrimitiveType.Cylinder, t, new Vector3(.08f, .14f, 0), new Vector3(.05f, .008f, .05f), new Color(.62f, .1f, .08f));   // the seal
                    break;
                case Load.Broom:
                    t.localPosition = new Vector3(.42f, -.1f, .15f);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, .25f, 0), new Vector3(.035f, .62f, .035f), new Color(.5f, .38f, .22f));
                    Part(PrimitiveType.Cube, t, new Vector3(0, -.45f, 0), new Vector3(.2f, .2f, .1f), new Color(.66f, .56f, .3f), new Vector3(0, 0, 0));   // the twigs
                    Part(PrimitiveType.Cube, t, new Vector3(0, -.33f, 0), new Vector3(.12f, .05f, .08f), new Color(.3f, .22f, .14f));   // the binding
                    break;
                case Load.Pole:
                    t.localPosition = new Vector3(.42f, -.1f, .15f);
                    Part(PrimitiveType.Cylinder, t, new Vector3(0, .55f, 0), new Vector3(.03f, 1.15f, .03f), new Color(.34f, .24f, .14f));
                    Part(PrimitiveType.Cube, t, new Vector3(.04f, 1.72f, 0), new Vector3(.03f, .12f, .03f), new Color(.8f, .64f, .3f));   // the brass hook
                    Part(PrimitiveType.Sphere, t, new Vector3(0, 1.74f, 0), new Vector3(.05f, .07f, .05f), new Color(1f, .82f, .42f));   // the wick, alight
                    break;
            }
            return go;
        }
    }
}
