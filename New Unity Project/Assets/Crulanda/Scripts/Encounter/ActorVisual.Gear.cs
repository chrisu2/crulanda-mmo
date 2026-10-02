using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Worn gear on the figure (loot DESIGN.md section 2). Each equipment slot has its own root ("Gear Main hand", ...) under the
    /// rig part it follows, so gear swings with the arms and holds every pose. Once gear drives the figure the class kit goes:
    /// the Warrior's sword, shield and shoulder pads; the Druid's staff stays only while the main hand is empty (it yields to a
    /// main-hand item) and the hood stays until a head piece exists. Empty slots show empty. Only slots whose item changed are
    /// rebuilt. Main- and off-hand pieces are built twice, in the hand and slung on the back, and swimming shows the back copy
    /// (the existing held/stowed switch in LateUpdate). Armour (ActorVisual.GearArmor.cs) hangs from the body and, where it
    /// follows a limb, from extra roots on the arm and leg pivots ("Gear Hands (Arm R)"); it recolours or hides the bare body
    /// parts it covers and gives them back when it comes off.
    /// </summary>
    public sealed partial class ActorVisual
    {
        static readonly int GearSlots = ItemDatabase.SlotIds.Length;
        bool gearDriven; Transform[] classHeld, classStowed;
        readonly Transform[] gearRoots = new Transform[ItemDatabase.SlotIds.Length], gearStows = new Transform[ItemDatabase.SlotIds.Length];
        readonly string[] gearSig = new string[ItemDatabase.SlotIds.Length];
        /// <summary>A slot's roots on the limbs (an arm or leg each), beside its root on the body.</summary>
        readonly List<Transform>[] gearMore = new List<Transform>[ItemDatabase.SlotIds.Length];
        /// <summary>True once worn gear has taken over from the class kit (the player in an itemised session, a mannequin).</summary>
        public bool GearDriven { get { return gearDriven; } }
        /// <summary>The slots this build shows on the figure: all nine.</summary>
        public static bool BuildsSlot(EquipSlot slot) { return (int)slot >= 0 && (int)slot < GearSlots; }

        /// <summary>Dresses the figure in this equipment (indexed by EquipSlot). Rebuilds only the slots whose item or look changed.</summary>
        public void ApplyGear(IList<ItemStack> equipment, ItemDatabase items, GearLooks looks)
        {
            if (body == null || armR == null || armL == null || beast || core != null || looks == null) return;
            GearInit();
            var shows = new bool[GearSlots]; var lks = new GearLook[GearSlots]; var sigs = new string[GearSlots];
            for (int s = 0; s < GearSlots; s++)
            {
                var stack = equipment != null && s < equipment.Count ? equipment[s] : null;
                var d = stack == null || stack.Empty || items == null ? null : items.Get(stack.item);
                shows[s] = d != null && d.kind == "gear" && ItemDatabase.SlotIndex(d.slot) == s && BuildsSlot((EquipSlot)s);
                lks[s] = shows[s] ? looks.Resolve(d) : default;
                sigs[s] = shows[s] ? d.id + "|" + GearLooks.LookKey(lks[s]) : stack == null || stack.Empty ? "" : stack.item;
            }
            // What one piece rests on: a neck piece lies on the chest piece or over a mantle or a hood's cape, a hood's drape tucks
            // under shoulder armour, boots go under greaves, a mantle's back drape gives way to the Druid's hung cloak.
            Rest(shows, lks);
            if (shows[(int)EquipSlot.Neck]) sigs[(int)EquipSlot.Neck] += "|" + wornChest + "|" + neckOver;
            if (shows[(int)EquipSlot.Head]) sigs[(int)EquipSlot.Head] += "|" + wornShoulders;
            if (shows[(int)EquipSlot.Feet]) sigs[(int)EquipSlot.Feet] += "|" + wornLegs;
            if (shows[(int)EquipSlot.Shoulders]) sigs[(int)EquipSlot.Shoulders] += "|" + Cloaked;
            for (int s = 0; s < GearSlots; s++)
            {
                if (gearSig[s] == sigs[s]) continue;
                ClearSlot(s); gearSig[s] = sigs[s];
                if (shows[s]) BuildSlot((EquipSlot)s, lks[s]);
            }
            RefreshHeld();
        }
        /// <summary>Dresses a mannequin (the wardrobe capture, the paper doll): each item goes to its own slot, the rest are empty.</summary>
        public void ApplyGearIds(string[] itemIds, ItemDatabase items, GearLooks looks)
        {
            var worn = new ItemStack[GearSlots]; for (int s = 0; s < GearSlots; s++) worn[s] = new ItemStack();
            foreach (var id in itemIds ?? new string[0])
            {
                var d = items?.Get(id); int s = d == null ? -1 : ItemDatabase.SlotIndex(d.slot);
                if (s >= 0) worn[s] = new ItemStack { item = id, count = 1 };
            }
            ApplyGear(worn, items, looks);
        }

        /// <summary>Renderers under the slots' gear roots (on the body and the limbs), held copies only (the slung copies are hidden until you swim).</summary>
        public int GearPartCount { get { int n = 0; for (int s = 0; s < GearSlots; s++) n += GearParts((EquipSlot)s); return n; } }
        public int GearParts(EquipSlot slot)
        {
            int s = (int)slot, n = Count(gearRoots[s]);
            if (gearMore[s] != null) foreach (var t in gearMore[s]) n += Count(t);
            return n;
        }
        /// <summary>A slot's roots on the arms and legs (empty when it has none).</summary>
        public IList<Transform> GearLimbRoots(EquipSlot slot) { return gearMore[(int)slot] ?? (IList<Transform>)new Transform[0]; }
        /// <summary>A slot's gear root (or its slung copy). Null when the slot shows nothing.</summary>
        public Transform GearRoot(EquipSlot slot, bool stowedCopy = false) { return (stowedCopy ? gearStows : gearRoots)[(int)slot]; }
        /// <summary>Class kit still showing: the Warrior's pads, sword and shield, the Druid's hood and staff.</summary>
        public int ClassKitParts
        {
            get
            {
                int n = 0;
                foreach (var set in new[] { classKit, gearDriven ? classHeld : held }) if (set != null) foreach (var t in set) if (t != null && t.gameObject.activeSelf) n++;
                return n;
            }
        }
        static int Count(Transform root) { return root == null ? 0 : root.GetComponentsInChildren<MeshRenderer>(true).Length; }

        void GearInit()
        {
            if (gearDriven) return;
            gearDriven = true; classHeld = held; classStowed = stowed; held = stowed = null;
            for (int s = 0; s < GearSlots; s++) gearMore[s] = new List<Transform>();
            if (built == ActorLook.Druid) return;   // the staff yields to a main-hand item (RefreshHeld); the hood waits for the head slot
            Kill(classKit); Kill(classHeld); Kill(classStowed); classKit = classHeld = classStowed = null;
        }
        /// <summary>Points the held/stowed switch at the gear roots (and the Druid's staff while the main hand is empty) and shows the right set.</summary>
        void RefreshHeld()
        {
            var h = new List<Transform>(); var st = new List<Transform>();
            foreach (var s in new[] { EquipSlot.MainHand, EquipSlot.OffHand })
            {
                if (gearRoots[(int)s] != null) h.Add(gearRoots[(int)s]);
                if (gearStows[(int)s] != null) st.Add(gearStows[(int)s]);
            }
            bool staff = gearRoots[(int)EquipSlot.MainHand] == null;
            foreach (var (set, into) in new[] { (classHeld, h), (classStowed, st) })
                if (set != null) foreach (var t in set) if (t != null) { if (staff) into.Add(t); else t.gameObject.SetActive(false); }
            held = h.ToArray(); stowed = st.ToArray();
            gearStowed = Pose == ActorPose.Swim;
            foreach (var t in held) t.gameObject.SetActive(!gearStowed);
            foreach (var t in stowed) t.gameObject.SetActive(gearStowed);
        }
        void ClearSlot(int s)
        {
            Kill(gearRoots[s]); Kill(gearStows[s]); gearRoots[s] = gearStows[s] = null;
            if (gearMore[s] != null) { foreach (var t in gearMore[s]) Kill(t); gearMore[s].Clear(); }
            Uncover((EquipSlot)s);
        }
        static void Kill(Transform[] set) { if (set != null) foreach (var t in set) Kill(t); }
        /// <summary>Removes a part at once as far as counting goes: hidden and unparented now, destroyed at the end of the frame (or now in edit mode).</summary>
        static void Kill(Transform t)
        {
            if (t == null) return;
            if (!Application.isPlaying) { DestroyImmediate(t.gameObject); return; }
            t.gameObject.SetActive(false); t.SetParent(null, false); Destroy(t.gameObject);
        }

        /// <summary>How much larger than life a held weapon and a shield are drawn.</summary>
        public const float HeldScale = 1.35f, ShieldScale = 1.15f;
        static bool Tall(string family) { return family == "polearm" || family == "staff"; }
        static bool Hung(string family) { return family == "offhand.hung"; }
        void BuildSlot(EquipSlot slot, GearLook look)
        {
            int s = (int)slot;
            if (slot != EquipSlot.MainHand && slot != EquipSlot.OffHand)
            {
                gearRoots[s] = new GameObject("Gear " + ItemDatabase.SlotNames[s]).transform; gearRoots[s].SetParent(body, false);
                BuildArmor(slot, look); return;
            }
            gearRoots[s] = Mount(slot, look.family, false); BuildGear(look, gearRoots[s]);
            if (slot == EquipSlot.MainHand || slot == EquipSlot.OffHand) { gearStows[s] = Mount(slot, look.family, true); BuildGear(look, gearStows[s]); }
            if (Hung(look.family)) gearRoots[s].gameObject.AddComponent<GearHang>().wearer = transform;
        }
        /// <summary>
        /// Where a slot's piece sits (DESIGN.md 2.3 "Roots"). Main hand: at the right hand, the weapon along +Y pointing forward and
        /// 20 degrees down (turned 20 degrees about its length so a flat shows to a camera behind and above); staves and polearms
        /// stand nearly upright just in front of the fist, clear of the sleeve. Off hand: on the left forearm, face outward; hung
        /// pieces from the left hand, the bail or chain inside the fist. Slung copies: the weapon across the back (hilt over the
        /// right shoulder, flat to the back), the shield flat on the back point down, a lantern hanging beside the left hip.
        /// </summary>
        Transform Mount(EquipSlot slot, string family, bool stow)
        {
            bool main = slot == EquipSlot.MainHand, tall = Tall(family), hung = Hung(family);
            Transform parent; Vector3 pos; Quaternion rot;
            if (!stow)
            {
                if (main) { parent = armR; pos = tall ? new Vector3(0, -.62f, .085f) : new Vector3(0, -.62f, 0); rot = tall ? Quaternion.Euler(8, 0, 0) : Quaternion.Euler(110, 0, 0) * Quaternion.Euler(0, 20, 0); }
                else if (hung) { parent = armL; pos = new Vector3(0, -.65f, 0); rot = Quaternion.identity; }
                else { parent = armL; pos = new Vector3(-.1f, -.38f, .05f); rot = Quaternion.Euler(0, 0, 90); }
            }
            else
            {
                parent = body;
                if (main && tall) { pos = new Vector3(0, .25f, -.27f); rot = Quaternion.Euler(0, 0, -40) * Quaternion.Euler(0, 90, 0); }
                else if (main) { pos = new Vector3(.26f, .66f, -.2f); rot = Quaternion.Euler(0, 0, 145) * Quaternion.Euler(0, 90, 0); }
                else if (hung) { pos = new Vector3(-.31f, .08f, -.12f); rot = Quaternion.Euler(0, 90, 0); }   // clear of the hip and thigh; a balance's beam runs fore and aft
                else { pos = new Vector3(0, .3f, -.24f); rot = Quaternion.Euler(0, -90, 90); }
            }
            var t = new GameObject("Gear " + ItemDatabase.SlotNames[(int)slot] + (stow ? " (slung)" : "")).transform;
            t.SetParent(parent, false); t.localPosition = pos; t.localRotation = rot;
            // Heroic proportions, the classic-MMO way: what is held reads larger than life (in the first wardrobe line-up a life-size
            // blade at the hip read as a twig). The grip stays in the fist; the piece grows out from it.
            if (main) t.localScale = Vector3.one * HeldScale; else if (slot == EquipSlot.OffHand) t.localScale = Vector3.one * (hung ? 1.1f : ShieldScale);
            return t;
        }

        // ---------- parts and materials ----------
        /// <summary>What a glowing accent (rare and epic, or a forced glow) is called, so it can be told from the piece's other parts.</summary>
        public const string AccentName = "Gear accent";
        /// <summary>The materials one look builds with (shared, from GearMats), and the accents lit so far (for an epic's pulse).</summary>
        sealed class GearKit
        {
            public GearLook l; public Material metal, edge, dark, wood, leather, cloth, cloth2, trim, bone, glow, rust, plate, mail, fur;
            public readonly List<Renderer> lit = new List<Renderer>();
            /// <summary>An accent's emission: the palette's glow made vivid (GearLooks.Gleam), times the quality's strength.</summary>
            public Color Emission { get { return GearLooks.Gleam(l.glow) * Mathf.Max(1.2f, l.glowPower); } }
            /// <summary>Always-lit parts of a variant (a lantern's flame, buds, a seam): dimmer on poor gear.</summary>
            public Material Lit(float k) { return GearMats.Get(l.glow, .8f, 0, l.glow * k * (l.quality == 0 ? .6f : 1)); }
        }
        static GearKit Kit(GearLook l)
        {
            float q = l.quality;
            var k = new GearKit { l = l };
            k.metal = GearMats.Get(l.metal, .3f + .08f * q, .45f + .08f * q);
            k.edge = GearMats.Get(Color.Lerp(l.metal, Color.white, l.quality == 0 ? .15f : .45f), .7f, .5f);
            k.dark = GearMats.Get(l.metal * .5f, .3f, .4f);
            k.wood = GearMats.Get(l.wood, .15f); k.leather = GearMats.Get(l.leather, .25f);
            k.cloth = GearMats.Get(l.cloth, .1f); k.cloth2 = GearMats.Get(l.cloth2, .1f);
            k.trim = l.hasTrim ? GearMats.Get(l.trimColor, l.trimSmooth, l.trimMetal) : k.dark;
            k.bone = GearMats.Get(l.bone, .35f);
            k.glow = GearMats.Get(GearLooks.Gleam(l.glow), .8f, 0, k.Emission);   // one material per palette and quality, shared by every accent of them
            k.rust = GearMats.Get(new Color(.36f, .2f, .11f), .08f);
            // Armour: polished plate, darker duller mail, matte fur.
            k.plate = GearMats.Get(Color.Lerp(l.metal, Color.white, .08f), .48f + .07f * q, .62f + .06f * q);
            k.mail = GearMats.Get(l.metal * .78f, .3f + .04f * q, .62f);
            k.fur = GearMats.Get(Color.Lerp(l.cloth, l.leather, .3f), .04f);
            return k;
        }
        /// <summary>A gear part: a mesh and a shared material, no collider.</summary>
        static Transform GPart(Transform root, Mesh mesh, Material m, Vector3 pos, Vector3 scale, Vector3? euler = null)
        {
            var go = new GameObject("Gear part"); var t = go.transform;
            t.SetParent(root, false); t.localPosition = pos; t.localScale = scale; if (euler.HasValue) t.localEulerAngles = euler.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return t;
        }
        static Transform GPrim(PrimitiveType type, Transform root, Material m, Vector3 pos, Vector3 scale, Vector3? euler = null) { return GPart(root, GearMeshes.Prim(type), m, pos, scale, euler); }
        static Transform GBox(Transform root, Material m, Vector3 pos, Vector3 size, Vector3? euler = null) { return GPrim(PrimitiveType.Cube, root, m, pos, size, euler); }
        static Transform GBall(Transform root, Material m, Vector3 pos, Vector3 size) { return GPrim(PrimitiveType.Sphere, root, m, pos, size); }
        /// <summary>A rod along local Y from y0 to y1 (a grip, a haft, a shaft).</summary>
        static Transform GRod(Transform root, Material m, float y0, float y1, float radius, float x = 0, float z = 0)
        { return GPrim(PrimitiveType.Cylinder, root, m, new Vector3(x, (y0 + y1) / 2, z), new Vector3(radius * 2, (y1 - y0) / 2, radius * 2)); }
        /// <summary>A thin ring round a rod at height y (a ferrule, a band, a wrap).</summary>
        static Transform GBand(Transform root, Material m, float y, float radius, float height = .014f)
        { return GPrim(PrimitiveType.Cylinder, root, m, new Vector3(0, y, 0), new Vector3(radius * 2, height / 2, radius * 2)); }
        /// <summary>A cube placement stretched between two points (a string, a bar), for GearMeshes.Many.</summary>
        static Matrix4x4 Stick(Vector3 a, Vector3 b, float w) { return Matrix4x4.TRS((a + b) / 2, Quaternion.FromToRotation(Vector3.up, b - a), new Vector3(w, (b - a).magnitude, w)); }

        /// <summary>Accent n (1 at rare, 2 at epic, or 1 when the look asks for a glow): a glowing part (named AccentName), pulsing on epic gear.</summary>
        static void Accent(GearKit k, int n, Transform root, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null)
        {
            if (k.l.accents < n) return;
            var t = GPart(root, mesh, k.glow, pos, scale, euler); t.name = AccentName; k.lit.Add(t.GetComponent<Renderer>());
        }
        /// <summary>The uncommon-and-better extra part, in the trim.</summary>
        static void Extra(GearKit k, Transform root, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { if (k.l.extra) GPart(root, mesh, k.trim, pos, scale, euler); }
        /// <summary>Poor gear's wear: a rust patch or a scuff.</summary>
        static void Wear(GearKit k, Transform root, Vector3 pos, Vector3 size, Vector3? euler = null) { if (k.l.worn) GBox(root, k.rust, pos, size, euler); }
        /// <summary>The generated material's small mark (looks.json "detail") as a ring round a grip or haft at height y.</summary>
        static void Detail(GearKit k, Transform root, float y, float radius)
        {
            switch (k.l.detail)
            {
                case "rivets":
                    GPart(root, GearMeshes.Many("rivets", GearMeshes.Prim(PrimitiveType.Sphere), GearMeshes.At(new Vector3(1, 0, 0), Vector3.zero, Vector3.one * .45f), GearMeshes.At(new Vector3(-.5f, 0, .87f), Vector3.zero, Vector3.one * .45f), GearMeshes.At(new Vector3(-.5f, 0, -.87f), Vector3.zero, Vector3.one * .45f)),
                        k.dark, new Vector3(0, y, 0), Vector3.one * radius); break;
                case "band": case "plate": GBand(root, k.trim, y, radius * 1.18f, .02f); break;
                case "cord": case "stitch": GBand(root, k.cloth2, y, radius * 1.15f, .03f); break;
                case "bone": GBand(root, k.bone, y, radius * 1.2f, .03f); break;
                case "ember": GBand(root, k.Lit(1.4f), y, radius * 1.12f, .012f); break;
                case "leaf": case "root": case "sap": GBand(root, GearMats.Get(new Color(.3f, .45f, .22f), .3f), y, radius * 1.15f, .028f); break;
                case "fray": case "patch": GBand(root, GearMats.Get(Color.Lerp(k.l.leather, k.l.cloth, .5f), .1f), y, radius * 1.15f, .035f); break;
            }
        }
        /// <summary>After a build: an epic's accents breathe.</summary>
        static void Finish(GearKit k, Transform root) { if (k.l.pulse && k.lit.Count > 0) root.gameObject.AddComponent<GearGlow>().Init(k.lit.ToArray(), k.Emission); }
    }
}
