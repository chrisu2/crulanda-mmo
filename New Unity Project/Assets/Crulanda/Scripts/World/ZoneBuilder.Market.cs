using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// The Fantasy pack's market pieces (2026-10-15, Chris 01:20: "Some Unity 6 and FBX fantasy assets you may want to use"):
    /// ithappy's Fantasy FREE (Asset Store EULA), its stall, tables, jugs, plates, sacks, boxes, bucket, barrel, hanging frame,
    /// signpost, logs and hand cart under Resources/Props/Fantasy2 with its one texture; not its crane (no machines), its houses
    /// (the village has its own) or its trees and mushrooms (the woods have theirs). Blender's export with the turn baked in: Y up,
    /// every node unturned, metres (the stall 2.57 m, the table .78), so no PropTurns entry (ZoneBuilder.ModelProps). A "market"
    /// prop in a zone file stands one piece: variant picks it from <see cref="MarketPieces"/>, rotation turns it, scale sizes it
    /// (1 is the pack's own size), lift raises it (a jug set on a table: lift .78, the table's top). Oakhaven's market and the
    /// Golden Cask's yard are the first to use them: sixteen pieces at the end of oakhaven.json. The hook is one line in
    /// BuildProps's switch (ZoneBuilder.cs, after "stall"): case "market": MarketProp(t, p); break;
    /// </summary>
    public sealed partial class ZoneBuilder
    {
        /// <summary>The pieces by variant: a name, the model under Resources/Props, its height in metres as the pack made it, and
        /// whether it blocks the way (the stall, the tables, the barrel, the frame, the signpost, the log store, the cart).</summary>
        public static readonly (string name, string path, float height, bool solid)[] MarketPieces = {
            ("stall", "Fantasy2/stall_001", 2.57f, true), ("stall table", "Fantasy2/stall_table_001", 1.08f, true), ("table", "Fantasy2/table_001", .78f, true),
            ("jug wide", "Fantasy2/jug_001", .34f, false), ("jug with spout", "Fantasy2/jug_002", .51f, false), ("jug tall", "Fantasy2/jug_003", .57f, false), ("jug small", "Fantasy2/jug_004", .33f, false), ("jug", "Fantasy2/jug_005", .45f, false),
            ("plate large", "Fantasy2/plate_001", .07f, false), ("plate small", "Fantasy2/plate_002", .07f, false), ("plate", "Fantasy2/plate_003", .07f, false),
            ("sack", "Fantasy2/bag_001", .57f, false), ("sack 2", "Fantasy2/bag_002", .57f, false), ("sack 3", "Fantasy2/bag_003", .57f, false), ("sack 4", "Fantasy2/bag_004", .57f, false),
            ("box", "Fantasy2/box_001", .54f, false), ("box flat", "Fantasy2/box_001_001", .34f, false), ("tray", "Fantasy2/box_002", .17f, false), ("box low", "Fantasy2/box_003", .32f, false),
            ("bucket", "Fantasy2/bucket_001", .48f, false), ("barrel", WoodBarrelPath, .73f, true), ("hanging frame", "Fantasy2/holder_001", 2.01f, true), ("signpost", "Fantasy2/pointer_001", 2.17f, true),
            ("log store", "Fantasy2/log_001", 1.81f, true), ("stump", "Fantasy2/log_002", .74f, false), ("log", "Fantasy2/log_003", .32f, false), ("logs", "Fantasy2/log_004", .54f, false),
            ("hand cart", "Fantasy2/cart_001", 1.16f, true),
        };
        /// <summary>One market piece on its root (Root: on the ground at the prop's place, turned, scaled): the model stands with its
        /// bottom <paramref name="p"/>.lift above the root (0: on the ground). Nothing is drawn when the model is missing (the tests'
        /// bare scenes): the painted stalls are the market without it.</summary>
        void MarketProp(Transform t, ZoneProp p)
        {
            var piece = MarketPieces[Mathf.Abs(p.variant) % MarketPieces.Length];
            ModelProp(t, piece.path, new Vector3(0, p.lift, 0), 0, piece.height, piece.solid);
        }
    }
}
