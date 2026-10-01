using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>Goods a villager carries between trades, built on the arm or the shoulder by <see cref="LoadProps"/>.</summary>
    public enum Load { None, Eggs, Bucket, Grain, Flour, Bread, Logs, Hide, Herbs, Game, Goods }

    /// <summary>
    /// An errand in a trade's day, run once a day from <see cref="at"/> (and dropped if not started by <see cref="until"/>):
    /// walk to <see cref="from"/>, pick up the <see cref="load"/>, carry it to <see cref="to"/>, hand it over (the village's stock
    /// of <see cref="good"/> there grows by <see cref="amount"/>) and say so. A task with no <see cref="to"/> is done where it is
    /// picked up (scattering feed). Places are the village's place kinds ("well", "stall", "inn"...), "home", or the hen-wife's
    /// "nest", "trough" and "pan". "{n}" in a line is the count carried.
    /// </summary>
    public sealed class Errand
    {
        public string id; public float at, until; public string from, to; public Load load; public string good; public int amount = 1;
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
    /// leatherworker keeps her own shop (9 to 12 and 2 to 6, the tannery yard either side, her wares to the stall from the shop
    /// after her midday meal) and the herbalist calls in at her drying
    /// hut; in a village without them the leatherworker works the tannery yard and the herbalist passes the hut over.
    /// </summary>
    public static class VillageWork
    {
        const float Dawn = 5.8f, Night = 20.6f;
        static readonly Dictionary<string, WorkDay> Days = new Dictionary<string, WorkDay>
        {
            { "farmer", new WorkDay(new[] { new Shift(Dawn, 12, "field", "field", "field", "field", "well", "green"), new Shift(12, 13.2f, "inn", "inn", "well"), new Shift(13.2f, 18, "field", "field", "field", "field", "well", "green"), new Shift(18, Night, "green", "inn", "home") },
                new Errand("grain to the mill", 10, 12.5f, "field", "mill", Load.Grain, "grain", "Barley. Last of it till harvest; go easy on the stone."),
                new Errand("seed-corn home", 16.5f, 18.5f, "field", "home", Load.Grain, "grain", "Seed-corn. It sleeps under my bed, where the grey-coats won't look.")) },
            { "miller", new WorkDay(new[] { new Shift(6.5f, 12, "mill", "mill", "mill", "well"), new Shift(12, 13, "inn"), new Shift(13, 17.5f, "mill", "mill", "mill", "green"), new Shift(17.5f, Night, "inn", "inn", "green") },
                new Errand("flour to the bakehouse", 11, 13.5f, "mill", "oven", Load.Flour, "flour", "Flour for the morning's loaves. Don't ask me what it tastes of."),
                new Errand("flour to the stall", 15.5f, 17.5f, "mill", "stall", Load.Flour, "flour", "Flour, ground fine. Sell it dear; there's less every year.")) },
            { "baker", new WorkDay(new[] { new Shift(4.6f, 7.5f, "oven"), new Shift(7.5f, 11.5f, "stall", "stall", "stall", "oven"), new Shift(11.5f, 13, "oven", "oven", "well"), new Shift(13, 17, "stall", "stall", "stall", "oven"), new Shift(17, 19.6f, "well", "green", "home") },
                new Errand("first loaves to the inn", 7, 9.5f, "oven", "inn", Load.Bread, "bread", "First loaves, still warm. The Cask gets the best of them."),
                new Errand("loaves to the stall", 12.5f, 14.5f, "oven", "stall", Load.Bread, "bread", "Second batch. Mind, the crust's dark; the flour's thin.")) },
            { "blacksmith", new WorkDay(new[] { new Shift(6.5f, 12.5f, "forge", "forge", "forge", "forge", "forge", "well"), new Shift(12.5f, 13.3f, "well", "inn"), new Shift(13.3f, 18, "forge", "forge", "forge", "forge", "forge"), new Shift(18, Night, "inn", "inn", "green") },
                new Errand("oak for the forge", 8.5f, 11, "woodpile", "forge", Load.Logs, "wood", "Oak for the fire. Charcoal's dear; oak's not.", "Dry, this. Good."),
                new Errand("ironwork to the stall", 16.8f, 18.5f, "forge", "stall", Load.Goods, "goods", "Nails, hinges and a hook. Sell what you can.")) },
            { "lumberjack", new WorkDay(new[] { new Shift(Dawn, 11, "woods", "woods", "woods", "woodpile"), new Shift(11, 12, "inn", "well"), new Shift(12, 17, "woods", "woods", "woods", "woodpile"), new Shift(17, Night, "inn", "inn", "green") },
                new Errand("logs to the woodyard", 9, 11.5f, "woods", "woodpile", Load.Logs, "logs", "Two more loads and that's the big oak down."),
                new Errand("firewood to the inn", 14.5f, 16.5f, "woodpile", "inn", Load.Logs, "wood", "Firewood for the Cask. Dry, mind, not the grey stuff."),
                new Errand("firewood to the forge", 16, 18, "woodpile", "forge", Load.Logs, "wood", "For the hearth. Don't burn it all at once.")) },
            { "hunter", new WorkDay(new[] { new Shift(Dawn, 10.5f, "woods", "woods", "woods", "meadow", "meadow"), new Shift(10.5f, 13, "tannery", "inn", "inn"), new Shift(13, 18, "woods", "woods", "woods", "meadow", "meadow"), new Shift(18, Night, "inn", "inn", "green") },
                new Errand("the hide to the tannery", 10, 12.5f, "woods", "tannery", Load.Game, "hides", "A buck's hide, and the hares are for the Cask. Pay me for the hide."),
                new Errand("hares to the inn", 11, 13.5f, "tannery", "inn", Load.Game, "meat", "Two hares for the pot. Don't let the drinkers see them.")) },
            { "skinner", new WorkDay(new[] { new Shift(7, 12, "tannery", "tannery", "tannery", "well"), new Shift(12, 13, "inn", "well"), new Shift(13, 17.5f, "tannery", "tannery", "tannery", "woods"), new Shift(17.5f, Night, "green", "inn") },
                new Errand("pelts from the snares", 14.5f, 17, "woods", "tannery", Load.Hide, "hides", "Rabbit, mostly. Snares were full.", "Snares. Let's see... rabbit. Rabbit. Rabbit.")) },
            { "leatherworker", new WorkDay(new[] { new Shift(7, 9, "tannery", "leathershop"), new Shift(9, 12, "leathershop"), new Shift(12, 13, "inn"), new Shift(13, 14, "tannery", "leathershop"), new Shift(14, 18, "leathershop"), new Shift(18, Night, "green", "inn") },
                new Errand("leather to the stall", 12.2f, 14, "leathershop", "stall",Load.Goods, "goods", "Belts, a bridle, two purses. Coin or trade.")) },
            { "herbalist", new WorkDay(new[] { new Shift(Dawn, 10.5f, "meadow", "meadow", "meadow", "woods"), new Shift(10.5f, 12.5f, "stall", "inn", "green", "dryhut"), new Shift(12.5f, 16.5f, "meadow", "meadow", "woods", "woods"), new Shift(16.5f, Night, "green", "green", "inn", "dryhut") },
                new Errand("herbs to the stall", 10.3f, 12.5f, "meadow", "stall", Load.Herbs, "herbs", "Comfrey and yarrow for the stall. The marigold's for Mira."),
                new Errand("marigold for Mira", 11, 13.5f, "stall", "inn", Load.Herbs, "herbs", "Marigold and comfrey, for whoever's bleeding this week."),
                new Errand("herbs to dry", 16.3f, 18.5f, "meadow", "home", Load.Herbs, "herbs")) },
            { "merchant", new WorkDay(new[] { new Shift(7.3f, 12.5f, "stall", "stall", "stall", "stall", "green"), new Shift(12.5f, 13.3f, "well", "inn"), new Shift(13.3f, 18, "stall", "stall", "stall", "stall"), new Shift(18, Night, "inn", "inn", "green") },
                new Errand("a cask for the inn", 8.6f, 11, "stall", "inn", Load.Goods, "ale", "Ale for the Cask. Make it last the night, for once.") { amount = 12 },
                new Errand("shutters up", 17.8f, 19.5f, "stall", "home", Load.Goods, "goods", "Shutters up. What didn't sell comes home.")) },
            { "gossip", new WorkDay(new[] { new Shift(Dawn, 8, "well", "well", "green"), new Shift(8, 12, "green", "green", "green", "well"), new Shift(12, 13.5f, "inn", "inn", "well"), new Shift(13.5f, 17, "green", "green", "well", "wander"), new Shift(17, Night, "inn", "green", "home") },
                new Errand("the morning's water", 6.5f, 8.5f, "well", "home", Load.Bucket, "water", "Water's bitter again. Iron, my gran says."),
                new Errand("the evening's water", 16.5f, 18.5f, "well", "home", Load.Bucket, "water")) },
            { "child", new WorkDay(new[] { new Shift(6.5f, 11.5f, "green", "green", "wander", "wander", "well"), new Shift(11.5f, 13, "home"), new Shift(13, 17, "green", "green", "wander", "wander", "well"), new Shift(17, 20, "green", "home") },
                new Errand("a loaf for Mum", 11, 13, "oven", "home", Load.Bread, "bread", null, "Mum says a loaf, and no eating the crust."),
                new Errand("chores", 16.5f, 18.5f, "well", "home", Load.Bucket, "water", "Chores.")) },
            { "elder", new WorkDay(new[] { new Shift(7, 12, "green", "green", "green", "inn"), new Shift(12, 14, "inn", "inn", "green"), new Shift(14, 18, "green", "green", "green", "inn"), new Shift(18, Night, "inn", "home") }) },
            // The out of work (the drinkers, and any trade whose workplace the village lacks): loiter the morning away, then the inn, where
            // they drink until the ale is gone or they are (Villager.DrinkRound).
            { "drinker", new WorkDay(new[] { new Shift(7.5f, 11, "wander", "green", "well"), new Shift(11, 23.6f, "inn") }) },
            // The hen-wife: feed at first light and again mid-afternoon, water from the well through the day, eggs three times:
            // the first to the inn's kitchen, the second to the produce stall, the last home for the pot. Dusk is the hens' (Villager.KeeperNext).
            { "henwife", new WorkDay(new[] { new Shift(5.7f, 12.2f, "yard", "yard", "yard", "well", "green"), new Shift(12.2f, 13.5f, "home"), new Shift(13.5f, 18, "yard", "yard", "yard", "well", "green"), new Shift(18, 19.3f, "yard") },
                new Errand("the morning feed", 5.7f, 9.6f, "trough", null, Load.None, null, "Chook-chook-chook-chook!") { work = 16 },
                new Errand("eggs to the inn", 8.4f, 11.5f, "nest", "inn", Load.Eggs, "eggs", "{n} eggs for the Cask's kitchen. Mind, they're warm yet."),
                new Errand("water for the hens", 9.6f, 12, "well", "pan", Load.Bucket, "water", "There. Drink up, girls.", "Bitter or not, they'll drink it."),
                new Errand("the afternoon's water", 13.5f, 15.5f, "well", "pan", Load.Bucket, "water", "There. Drink up, girls."),
                new Errand("the afternoon feed", 14.6f, 17, "trough", null, Load.None, null, "Chook-chook-chook-chook!") { work = 16 },
                new Errand("eggs to the stall", 15.4f, 17.2f, "nest", "stall", Load.Eggs, "eggs", "{n} eggs, fresh. And don't start about the Concord's tax."),
                new Errand("eggs for the pot", 17.2f, 18.6f, "nest", "home", Load.Eggs, "eggs", "{n} for the pot.")) },
        };
        public static WorkDay DayFor(string role) { return Days.TryGetValue(role, out var d) ? d : null; }
        /// <summary>The shift a trade is on at an hour, or null when their day has no shift for it (bed, or a role without a day).</summary>
        public static Shift ShiftFor(string role, float hour)
        {
            var d = DayFor(role); if (d == null) return null;
            foreach (var s in d.shifts) if (hour >= s.from && hour < s.to) return s;
            return null;
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
            }
            return go;
        }
    }
}
