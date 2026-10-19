using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The food and the buff items as models (playtest note 235, 2026-10-18: "FBX-20261018T221927Z-1-001 for foods and buff items"). The
    /// food models are Quaternius's food pack (CC0; Resources/Props/Food, the medieval ones only: <see cref="Kept"/>; the burgers, sodas,
    /// sushi and the rest stay out, <see cref="LeftOut"/>), a few of the Megapack's market goods (its cheeses) and the Fantasy Props
    /// MegaKit's bottles and pots for the tinctures and salves.
    /// <see cref="Dishes"/>: each food and buff item's look, in one table (FoodModelTests: every one has a model, every model is there).
    /// <see cref="Fare"/>: what each zone's tables, kitchens and stalls are laid with, its own (Chris: every zone unique): Oakhaven's bread,
    /// apples and cheese, Khaven's turnips and grey caps, the Ash Rim's salt and flank, the Peaks' smoked meat, the Crown's pastries and
    /// oranges, the Lowtowns' sausages and fish-bones, the Shore's fish and venison. Which foods were kept and why (the books first):
    /// Docs/CHANGELOG.md, 2026-10-18. The look of a dish is GAME-ONLY.
    /// </summary>
    public static class FoodModels
    {
        /// <summary>A piece of a dish or a zone's fare: a model under Resources/Props, its height in metres (no wider than width, when
        /// given), where it sits (x, z across the dish; y up from its foot: on the plate) and its turn.</summary>
        public struct Bit
        {
            public string path; public float height, width, x, y, z, yaw;
            public Bit(string path, float height, float x = 0, float z = 0, float yaw = 0, float y = 0, float width = 0)
            { this.path = path; this.height = height; this.width = width; this.x = x; this.y = y; this.z = z; this.yaw = yaw; }
        }

        /// <summary>The food pack's models brought in (Resources/Props/Food), each kept for a reason the books or a zone give.</summary>
        public static readonly string[] Kept = {
            "Apple", "Apple_Green", "Orange", "Bread", "Bread_Slice", "Carrot", "Turnip", "Lettuce_Whole", "Mushroom", "Mushroom_Sliced",
            "Fish", "FishBone", "ChickenLeg", "Steak", "Sausage_Raw", "Sausage_Cooked", "Bacon_Uncooked", "Bacon_Cooked", "Egg_Whole",
            "Egg_Whole_White", "Egg_Fried", "Pancake", "Croissant", "CookingPot", "CookingPot_Soup", "CookingPot2", "CookingPot2_Soup",
            "FryingPan", "Plate", "Plate2", "Plate_Square", "Bottle1", "Bottle2", "Jar_Large", "Knife", "Fork", "Spoon" };
        /// <summary>The pack's models left out: not of a medieval world (Chris's list), or with no reason in the books or a zone.</summary>
        public static readonly string[] LeftOut = {
            "Avocado", "Avocado_Empty", "Bacon_Burned", "Banana", "Broccoli", "Burger", "BurgerLarge", "BurgerPatty_Burned", "BurgerPatty_Cooked",
            "BurgerPatty_Raw", "Burger_Bread", "Cheese_Singles", "Cheeseburger", "ChocolateBar", "Chopsticks", "Coconut", "Coconut_Half", "Corndog",
            "Cupcake", "Donut1", "Donut2", "Donut3", "Donut4", "DoubleCheeseburger", "Egg_Burned", "Eggplant", "Fries", "Hotdog", "Hotdog_Bun",
            "IceCream_1", "IceCream_2", "IceCream_3", "IceCream_4", "IceCream_Cone", "IceCream_Cone2", "KetchupBottle", "KetchupMustard", "Lettuce",
            "MayoBottle", "MustardBottle", "Pancakes_Stack", "PeanutButter", "PeanutButter_2", "Pepper_Green", "Pepper_Red", "Pizza", "Pizza_Burned",
            "Pizza_Slice", "Popsicle_Chocolate", "Popsicle_Multiple", "Popsicle_Strawberry", "Pumpkin", "Sashimi_Salmon", "Sashimi_Salmon2", "Soda",
            "SoySauce", "Steak_burned", "Sushi_Nigiri1", "Sushi_Nigiri2", "Sushi_NigiriOctopus", "Sushi_Roll1", "Sushi_Roll2", "Tentacle", "Tomato",
            "Tomato_Slice", "Waffle" };

        // Each model's height at life size (metres; the pack's models are at no one scale): an apple 8 cm, a loaf 16, a plate a hand's width.
        static readonly Dictionary<string, float> Life = new Dictionary<string, float> {
            { "Apple", .08f }, { "Apple_Green", .08f }, { "Orange", .075f }, { "Bread", .15f }, { "Bread_Slice", .1f }, { "Carrot", .2f },
            { "Turnip", .17f }, { "Lettuce_Whole", .17f }, { "Mushroom", .06f }, { "Mushroom_Sliced", .025f }, { "Fish", .035f }, { "FishBone", .05f },
            { "ChickenLeg", .066f }, { "Steak", .036f }, { "Sausage_Raw", .022f }, { "Sausage_Cooked", .022f }, { "Bacon_Uncooked", .006f },
            { "Bacon_Cooked", .006f }, { "Egg_Whole", .055f }, { "Egg_Whole_White", .055f }, { "Egg_Fried", .012f }, { "Pancake", .027f },
            { "Croissant", .06f }, { "CookingPot", .14f }, { "CookingPot_Soup", .14f }, { "CookingPot2", .08f }, { "CookingPot2_Soup", .08f },
            { "FryingPan", .03f }, { "Plate", .035f }, { "Plate2", .033f }, { "Plate_Square", .021f }, { "Bottle1", .3f }, { "Bottle2", .3f },
            { "Jar_Large", .18f }, { "Knife", .003f }, { "Fork", .005f }, { "Spoon", .008f } };
        /// <summary>A food model at life size: Resources/Props/Food/&lt;name&gt;, its height.</summary>
        public static Bit F(string name, float x = 0, float z = 0, float yaw = 0, float y = 0) { return new Bit("Food/" + name, Life[name], x, z, yaw, y); }
        /// <summary>The height a food model stands at life size (0 for one not in the table).</summary>
        public static float LifeHeight(string name) { return Life.TryGetValue(name, out var h) ? h : 0; }
        const float OnPlate = .018f;   // a plate's top over the table (its rim stands higher; the food sits in its well)
        static Bit Cheese(string n, float h, float x = 0, float z = 0, float y = 0) { return new Bit("Megapack/Models/Market/" + n, h, x, z, 0, y); }
        static Bit Kit(string n, float h, float x = 0, float z = 0) { return new Bit("Fantasy/" + n, h, x, z); }

        /// <summary>
        /// Every food and buff item's look, by item id (items.json and trades.json): the plate or pot it comes in first, then what is on it.
        /// The dishes are named for what is in them, so the nearest model in a medieval kitchen stands in: the Harrow pasty is the pack's
        /// crescent pastry (crimped shut), griddle bread and the hearth- and sap-cakes its flat cakes, jerky its cured strips, a haunch its
        /// drumstick, a chop or a flank its steak. The tinctures are small stoppered vials, the cordials taller ones, the draughts bottles,
        /// the salves lidded pots.
        /// </summary>
        public static readonly Dictionary<string, Bit[]> Dishes = new Dictionary<string, Bit[]> {
            // Oakhaven's and the Harrow's plain food
            { "food.brown_loaf", new[] { F("Bread") } },
            { "food.fresh_eggs", new[] { F("Plate_Square"), F("Egg_Whole", -.05f, .02f, 0, OnPlate), F("Egg_Whole", .05f, .03f, 40, OnPlate), F("Egg_Whole_White", 0, -.05f, 80, OnPlate) } },
            { "food.harrow_cheese", new[] { Cheese("Cheese_1", .045f) } },
            { "food.mountain_jerky", new[] { F("Plate_Square"), F("Bacon_Cooked", 0, -.04f, 10, OnPlate), F("Bacon_Cooked", 0, .01f, -8, OnPlate + .006f), F("Bacon_Cooked", 0, .05f, 4, OnPlate) } },
            { "food.griddle_bread", new[] { F("Plate"), F("Pancake", 0, 0, 0, OnPlate) } },
            { "food.boar_stew", new[] { F("CookingPot2_Soup"), F("Spoon", .12f, 0, 70) } },
            { "food.hearth_cake", new[] { F("Plate"), F("Pancake", 0, 0, 0, OnPlate), F("Pancake", .01f, .01f, 30, OnPlate + .02f) } },
            // Cooking 20 on: the dishes that leave you well fed
            { "food.wolf_skewer", new[] { F("Plate"), F("Sausage_Cooked", -.03f, 0, 10, OnPlate), F("Sausage_Cooked", .03f, 0, -6, OnPlate), F("Mushroom_Sliced", .07f, .06f, 0, OnPlate) } },
            { "food.harrow_pasty", new[] { F("Plate"), F("Croissant", 0, 0, 20, OnPlate) } },
            { "food.smoked_loin", new[] { F("Plate_Square"), F("Steak", 0, -.02f, 0, OnPlate), F("Bacon_Cooked", 0, .07f, 90, OnPlate) } },
            { "food.salt_flank", new[] { F("Plate2"), F("Steak", 0, 0, 30, OnPlate) } },
            { "food.cinder_loaf", new[] { F("Bread"), F("Bread_Slice", .13f, 0, 90) } },
            { "food.mossback_chop", new[] { F("Plate"), F("Steak", 0, 0, -20, OnPlate), F("Bread_Slice", .08f, .05f, 60, OnPlate) } },
            { "food.venison_pie", new[] { F("Plate2"), F("Bread", 0, 0, 45, OnPlate) } },
            { "food.sap_cake", new[] { F("Pancake"), F("Pancake", .005f, 0, 25, .02f), F("Pancake", 0, .005f, 50, .04f) } },
            { "food.cask_stew", new[] { F("CookingPot_Soup") } },
            { "food.harvest_feast", new[] { F("Plate2"), F("ChickenLeg", -.05f, 0, 20, OnPlate), F("Bread_Slice", .06f, -.04f, 70, OnPlate), F("Apple", .07f, .07f, 0, OnPlate), F("Carrot", 0, .09f, 0, OnPlate) } },
            { "food.bear_broth", new[] { F("CookingPot2_Soup"), F("Spoon", .12f, 0, 70) } },
            { "food.passfire_feast", new[] { F("Plate2"), F("Steak", -.03f, 0, 10, OnPlate), F("Bacon_Cooked", .07f, -.05f, 80, OnPlate), F("Turnip", .07f, .06f, 0, OnPlate), F("Bread_Slice", -.08f, .07f, 30, OnPlate) } },
            { "food.sap_venison", new[] { F("Plate2"), F("Steak", 0, 0, -10, OnPlate), F("Mushroom", .08f, .06f, 0, OnPlate), F("Mushroom_Sliced", -.08f, .06f, 30, OnPlate) } },
            { "food.shore_feast", new[] { F("Plate2"), F("Fish", 0, 0, 90, OnPlate), F("Bread_Slice", -.09f, .05f, 20, OnPlate), F("Egg_Fried", .08f, .06f, 0, OnPlate), F("Apple_Green", .09f, -.07f, 0, OnPlate) } },
            // The tinctures, cordials, the brine draught and the salves (the buff items)
            { "potion.tincture_ox", new[] { Kit("Potion_1", .1f) } }, { "potion.tincture_hare", new[] { Kit("Potion_1", .1f) } },
            { "potion.tincture_owl", new[] { Kit("Potion_1", .1f) } }, { "potion.tincture_hearth", new[] { Kit("Potion_1", .1f) } },
            { "potion.tincture_bear", new[] { Kit("Potion_1", .1f) } },
            { "potion.cordial_ox", new[] { Kit("Potion_2", .18f) } }, { "potion.cordial_hare", new[] { Kit("Potion_2", .18f) } },
            { "potion.cordial_owl", new[] { Kit("Potion_2", .18f) } }, { "potion.cordial_hearth", new[] { Kit("Potion_2", .18f) } },
            { "potion.cordial_bear", new[] { Kit("Potion_2", .18f) } },
            { "potion.draught_salt", new[] { Kit("Bottle_1", .24f) } },
            { "potion.draught_mana", new[] { Kit("SmallBottle", .12f) } }, { "potion.draught_vigor", new[] { Kit("SmallBottle", .12f) } },
            { "potion.draught_focus", new[] { Kit("SmallBottle", .12f) } }, { "potion.draught_nerve", new[] { Kit("SmallBottle", .12f) } },
            { "potion.draught_breath", new[] { Kit("SmallBottle", .12f) } },
            { "salve.tallow", new[] { Kit("Pot_1_Lid", .07f) } },
            { "salve.scree", new[] { Kit("Vase_4", .1f) } },
        };

        /// <summary>
        /// What each zone sets out (by zone id): on its inn's tables and bar, in its kitchen, on its food stalls. Its own selection; pieces
        /// may repeat between zones as small things, never the whole set (FoodModelTests: no two zones share more than half).
        /// </summary>
        public static readonly Dictionary<string, Bit[]> Fare = new Dictionary<string, Bit[]> {
            { "zone.oakhaven", new[] { F("Bread"), F("Apple"), F("Apple_Green"), F("Egg_Whole"), Cheese("Cheese_3", .05f), F("Carrot"), F("CookingPot_Soup") } },
            { "zone.khaven", new[] { F("Turnip"), F("Mushroom"), F("Mushroom_Sliced"), F("Bread_Slice"), F("Lettuce_Whole"), F("CookingPot2_Soup") } },
            { "zone.ashrim", new[] { F("Steak"), F("Bread"), F("Jar_Large"), F("FryingPan"), F("Bottle2") } },
            { "zone.peaks", new[] { F("Bacon_Cooked"), F("Bacon_Uncooked"), F("ChickenLeg"), Cheese("Cheese_2", .05f), F("Bottle1"), F("CookingPot") } },
            { "zone.crown", new[] { F("Croissant"), F("Orange"), F("ChickenLeg"), F("Fish"), F("Bottle1"), F("Knife"), F("Fork") } },
            { "zone.lowtowns", new[] { F("Sausage_Cooked"), F("Sausage_Raw"), F("FishBone"), F("Bread_Slice"), F("FryingPan"), F("Egg_Fried") } },
            { "zone.verdant", new[] { F("Fish"), F("Steak"), F("Pancake"), F("Mushroom"), F("Apple_Green"), F("Egg_Whole_White") } },
            { "zone.hallows", new[] { F("Jar_Large"), F("Bottle2"), F("Turnip"), F("CookingPot2") } },
            { "zone.adit", new[] { F("CookingPot2"), F("FishBone"), F("Bottle1"), F("Bread_Slice") } },
        };
        /// <summary>What a zone's produce stall heaps in its baskets (its fare's fruit and roots), and what its bread stall lays out.</summary>
        public static readonly Dictionary<string, string[]> Produce = new Dictionary<string, string[]> {
            { "zone.oakhaven", new[] { "Apple", "Apple_Green", "Carrot", "Lettuce_Whole" } },
            { "zone.khaven", new[] { "Turnip", "Mushroom", "Lettuce_Whole" } },
            { "zone.lowtowns", new[] { "Turnip", "FishBone", "Fish" } },
        };
        public static readonly Dictionary<string, string[]> Baked = new Dictionary<string, string[]> {
            { "zone.oakhaven", new[] { "Bread", "Bread" , "Bread_Slice" } },
            { "zone.khaven", new[] { "Bread_Slice", "Bread" } },
            { "zone.lowtowns", new[] { "Sausage_Cooked", "Bread_Slice", "Sausage_Raw" } },
        };

        /// <summary>The zone's fare, or an empty list for a zone with none of its own.</summary>
        public static Bit[] FareOf(string zone) { return zone != null && Fare.TryGetValue(zone, out var f) ? f : new Bit[0]; }
        /// <summary>Every model path the tables name (the dishes, the fare, the stalls), for the tests and PropCapture.</summary>
        public static IEnumerable<string> AllPaths()
        {
            foreach (var d in Dishes.Values) foreach (var b in d) yield return b.path;
            foreach (var d in Fare.Values) foreach (var b in d) yield return b.path;
            foreach (var d in Produce.Values) foreach (var n in d) yield return "Food/" + n;
            foreach (var d in Baked.Values) foreach (var n in d) yield return "Food/" + n;
        }

        /// <summary>A dish set down under <paramref name="parent"/> with its foot at <paramref name="foot"/> (parent-local), turned
        /// <paramref name="yaw"/>: its pieces under one holder, no colliders. Null for an item with no look or no models.</summary>
        public static GameObject Serve(Transform parent, Vector3 foot, float yaw, string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !Dishes.TryGetValue(itemId, out var bits)) return null;
            return Lay(parent, foot, yaw, bits, itemId);
        }
        /// <summary>Bits laid under one holder (named <paramref name="name"/>): each a model at its height, on the holder's foot.</summary>
        public static GameObject Lay(Transform parent, Vector3 foot, float yaw, Bit[] bits, string name)
        {
            var holder = new GameObject("Dish " + name).transform; holder.SetParent(parent, false);
            holder.localPosition = foot; holder.localRotation = Quaternion.Euler(0, yaw, 0);
            int made = 0;
            foreach (var b in bits)
                if (ZoneBuilder.ModelProp(holder, b.path, new Vector3(b.x, b.y, b.z), b.yaw, b.height, false, b.width > 0 ? b.width : (float?)null) != null) made++;
            if (made == 0) { if (Application.isPlaying) Object.Destroy(holder.gameObject); else Object.DestroyImmediate(holder.gameObject); return null; }
            foreach (var r in holder.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // a hand's width: no shadow worth its cost
            return holder.gameObject;
        }
    }
}
