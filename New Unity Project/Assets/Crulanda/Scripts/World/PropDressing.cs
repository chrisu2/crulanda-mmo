using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Where the Fantasy Props MegaKit's unused pieces stand (playtest note 234, 2026-10-18: "Fantasy Props MegaKit for dungeons and other
    /// places"), as data, so a test can say they are off the way through (PropDressingTests) and that no two places get the same set
    /// (Chris: every zone unique, its own selection and arrangement). ZoneBuilder.Dressing places them.
    /// <para>Caves (<see cref="Caves"/>, by the cave's dressing key: "crowsfoot", "rootdeep", an Adit or Hallow's Creek cave's style): x is
    /// across the passage as a share of its half-width (+ the right wall; the floor's way through is the middle, |x| under
    /// <see cref="CaveWay"/>), z metres along it from an anchor ("camp", "store", "hall", "gallery", "well", "heart"; "" for z as a share of
    /// the cave's length), y up from the floor. A wall piece hangs on the wall at its height, its back to the rock; a top piece stands on
    /// something already there (a reading desk).</para>
    /// <para>Rooms (<see cref="Rooms"/>): metres in the building's own frame, with the room's ways kept clear (the door to the far end).</para>
    /// GAME-ONLY: how the kit is laid out is the game's.
    /// </summary>
    public static class PropDressing
    {
        public enum Place { Floor, Wall, Top }
        public struct Piece
        {
            public string path, anchor; public float height, x, y, z, yaw; public Place place; public bool solid;
            /// <summary>For the wood barrel: <see cref="BarrelApples"/> or <see cref="BarrelRack"/>, else null (the model as it is).</summary>
            public string dress;
            public Piece(string path, float height, string anchor, float x, float z, float yaw = 0, Place place = Place.Floor, float y = 0, bool solid = false)
            { this.path = path; this.height = height; this.anchor = anchor; this.x = x; this.z = z; this.yaw = yaw; this.place = place; this.y = y; this.solid = solid; dress = null; }
        }
        /// <summary>A cave's floor pieces keep out of the middle of the passage: |x| at least this share of the half-width.</summary>
        public const float CaveWay = .55f;

        static Piece P(string kit, float h, string a, float x, float z, float yaw = 0) { return new Piece("Fantasy/" + kit, h, a, x, z, yaw); }
        static Piece S(string kit, float h, string a, float x, float z, float yaw = 0) { return new Piece("Fantasy/" + kit, h, a, x, z, yaw, Place.Floor, 0, true); }
        static Piece W(string kit, float h, string a, float x, float z, float up) { return new Piece("Fantasy/" + kit, h, a, x, z, 0, Place.Wall, up); }
        static Piece T(string kit, float h, string a, float x, float z, float up, float yaw = 0) { return new Piece("Fantasy/" + kit, h, a, x, z, yaw, Place.Top, up); }
        /// <summary>The kit's barrel pieces are the wood barrel now (play note 247, 2026-10-19: every wooden barrel the Simple Wood Barrel):
        /// "apples", its head heaped with apples (the kit's apple barrel, .9 m to the apples' top); "rack", lying on a timber cradle (the
        /// kit's barrel holder, 1.2 m to the barrel's top). ZoneBuilder.DressingProp builds them.</summary>
        public const string BarrelApples = "apples", BarrelRack = "rack";
        static Piece B(string dress, float h, string a, float x, float z, float yaw = 0, bool solid = false)
        { var p = new Piece(ZoneBuilder.WoodBarrelPath, h, a, x, z, yaw, Place.Floor, 0, solid); p.dress = dress; return p; }
        static Piece Food(string n, string a, float x, float z, float yaw = 0) { return new Piece("Food/" + n, FoodModels.LifeHeight(n), a, x, z, yaw); }

        public static readonly Dictionary<string, Piece[]> Caves = new Dictionary<string, Piece[]> {
            // Crowsfoot Hollow (the Sandthrone deserters' hideout): their camp's pots, rope and gear by the walls, the guard's weapon stand
            // at the camp's mouth, a captive's cage and its chain in the Echoing Hall.
            { "crowsfoot", new[] {
                Food("CookingPot", "camp", .72f, -1.8f), Food("FryingPan", "camp", .74f, -1.1f, 40), Food("Sausage_Raw", "camp", .7f, -.6f, 15),
                P("Bag", .55f, "camp", -.78f, 2.4f, 30), P("Rope_2", .06f, "camp", .76f, 2.6f), P("Bucket_Wooden_1", .3f, "camp", .8f, -2.6f),
                S("WeaponStand", 1.1f, "camp", -.82f, -2.8f, 90), P("Shield_Wooden", .6f, "camp", -.78f, -1.9f, 80),
                B(BarrelApples, .9f, "store", .8f, 3.6f), P("Pickaxe_Bronze", 1.0f, "store", -.78f, -3.4f, 10),
                S("Cage_Small", .85f, "hall", .78f, -6.5f, 15), P("Chain_Coil", .08f, "hall", .74f, -5.2f), P("Bucket_Wooden_1", .3f, "hall", .8f, -7.6f),
            } },
            // The Root-Mother's Deep: the Keepers' offerings, earthenware and candles at the walls, sap in stoppered vials by the Heart, grey caps.
            { "rootdeep", new[] {
                P("Vase_2", .5f, "gallery", -.8f, -3), P("Vase_Rubble_Medium", .16f, "gallery", .8f, 2), P("Candle_2", .25f, "gallery", -.82f, 3),
                P("Candle_1", .13f, "gallery", -.84f, 3.4f), Food("Mushroom", "gallery", .78f, -4), Food("Mushroom", "gallery", .82f, -3.7f, 60),
                P("Vase_4", .45f, "well", -.8f, -3.5f), P("Candle_2", .25f, "well", -.82f, 3.5f),
                P("Potion_4", .2f, "heart", -.75f, -4), P("Potion_2", .26f, "heart", -.78f, -4.3f), P("Book_Stack_1", .2f, "heart", .78f, -5),
            } },
            // The Sealed Adit, a set to a cave by its style. No machines: tools, rope, pails, lamps, books, cages.
            { "workings", new[] {
                P("Pickaxe_Bronze", 1.0f, "", .8f, .3f, 20), P("Bucket_Wooden_1", .3f, "", .8f, .34f), P("Rope_3", .1f, "", -.8f, .5f),
                W("Lantern_Wall", .55f, "", .9f, .2f, 1.5f), S("Crate_Metal", .8f, "", -.8f, .7f, 12),
            } },
            { "pen", new[] { S("Cage_Small", .85f, "", .78f, .4f, 20), P("Chain_Coil", .08f, "", -.75f, .5f), P("Bucket_Wooden_1", .3f, "", .8f, .62f) } },
            { "kennel", new[] { P("Chain_Coil", .08f, "", .78f, .5f, 40), P("Bucket_Wooden_1", .3f, "", -.8f, .4f), Food("FishBone", "", .7f, .6f, 30) } },
            { "scriptorium", new[] {
                S("Bookcase_2", 2.4f, "", .84f, .5f, -90), P("Book_Stack_2", .2f, "", -.78f, .4f), P("CandleStick_Stand", 1.3f, "", -.8f, .6f),
                P("BookStand", 1.4f, "", .78f, .72f), S("Cabinet", 1.0f, "", -.84f, .25f, 90), P("BookGroup_Small_2", .28f, "", -.8f, .44f),
            } },
            { "censored", new[] { P("Book_Stack_1", .2f, "", .75f, .5f), P("Scroll_2", .06f, "", .74f, .56f, 30), P("Book_7", .07f, "", -.74f, .58f, 40), P("Candle_1", .13f, "", -.75f, .5f) } },
            { "musteryard", new[] { S("WeaponStand", 1.1f, "", .84f, .4f, -90), P("Shield_Wooden", .6f, "", .8f, .48f, -80), W("Peg_Rack", .35f, "", -.9f, .55f, 1.4f) } },
            { "chapel", new[] { P("CandleStick_Stand", 1.3f, "", .78f, .8f), P("CandleStick_Stand", 1.3f, "", -.78f, .8f), P("Vase_4", .45f, "", .78f, .3f), P("Chalice", .2f, "", -.76f, .3f) } },
            { "quiet", new[] { P("Pickaxe_Bronze", 1.0f, "", .8f, .5f, 30), P("Pot_1", .2f, "", -.78f, .55f), Food("Bread_Slice", "", -.76f, .58f, 20), W("Lantern_Wall", .55f, "", -.9f, .4f, 1.5f) } },
            // Hallow's Creek's cellars: the town's stores, stopped as the town stopped.
            { "cellar", new[] {
                Food("Jar_Large", "", .8f, .3f), Food("Jar_Large", "", .82f, .34f, 50), Food("Bottle2", "", .78f, .37f), B(BarrelRack, 1.2f, "", -.82f, .6f, -90, true),
                P("Shelf_Small_Bottles", .65f, "", -.8f, .45f, 90), W("Shelf_Simple", .3f, "", .9f, .55f, 1.3f),
            } },
            { "darkcellar", new[] { P("Rope_1", .08f, "", .75f, .5f), P("Bucket_Wooden_1", .3f, "", -.75f, .4f), P("Candle_1", .13f, "", .75f, .3f) } },
        };

        /// <summary>A room: its ways kept clear (metres, the building's frame) and its pieces.</summary>
        public sealed class Room { public Rect[] ways; public Piece[] pieces; }
        public static readonly Dictionary<string, Room> Rooms = new Dictionary<string, Room> {
            // The Crown's Archive (its nave 18 m wide; the door at -z): the way up the middle and across the reading desks' fronts kept
            // clear; a book on a stand and a standing candelabrum by the archivist's lectern, books and scrolls on the reading desks.
            { "archive", new Room {
                ways = new[] { new Rect(-2.6f, -20, 5.2f, 40), new Rect(-9, -6.2f, 18, 1.0f) },
                pieces = new[] {
                    P("BookStand", 1.4f, "", 4.4f, -6.9f, 180), P("CandleStick_Stand", 1.3f, "", -5.5f, -6.9f),
                    T("Book_Stack_1", .2f, "", -7.9f, -3.7f, .82f, 10), T("Scroll_1", .05f, "", -7.0f, -3.9f, .82f, 70), T("CandleStick_Triple", .4f, "", -7.5f, -3.45f, .82f),
                    T("Book_Stack_2", .2f, "", 7.9f, -3.7f, .82f, -20), T("Scroll_2", .05f, "", 7.0f, -3.85f, .82f, 110), T("BookGroup_Small_1", .3f, "", 7.5f, -3.45f, .82f, 180),
                } } },
        };

        /// <summary>Every path the sets name.</summary>
        public static IEnumerable<string> AllPaths()
        {
            foreach (var s in Caves.Values) foreach (var p in s) yield return p.path;
            foreach (var r in Rooms.Values) foreach (var p in r.pieces) yield return p.path;
        }
    }
}
