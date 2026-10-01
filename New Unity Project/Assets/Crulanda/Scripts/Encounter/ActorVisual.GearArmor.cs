using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using M = Crulanda.Encounter.GearMeshes;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Armour on the body (loot DESIGN.md 2.3 and 2.5, step A2): every head, neck, shoulder, chest, hand, leg and foot family and
    /// variant. Pieces hang from the body or, where they follow a limb, from the arm and leg pivots, so they swing and hold every
    /// pose; left and right are built as mirrored pairs. A piece replaces what the bare body shows instead of stacking on it:
    /// chest pieces recolour the torso, chest and shoulders (and the sleeves, by family) and take the belt's place; legs and feet
    /// recolour the breeches and boots; hoods, coifs, barbutes and masks hide the hair, caps and kettle hats tuck it under the
    /// brim; the Druid's hood yields to any head piece and her cloak hangs clear of chest armour. All of it is given back when the
    /// piece comes off. Proportions are painted and slightly chunky. The level band (the look's tier) adds rims, lames, ridges and
    /// spikes; the quality adds trim, an extra part, glowing accents and a wear mark, as on the weapons. Parts of one material on
    /// one limb are joined into a single part, so a full kit stays inside the part budget. GAME-ONLY designs.
    /// </summary>
    public sealed partial class ActorVisual
    {
        const int OnBody = 0, OnArmL = 1, OnArmR = 2, OnLegL = 3, OnLegR = 4;
        /// <summary>What the other worn pieces are, for the ones that rest on them: the chest piece (family:variant) and whether shoulders are worn.</summary>
        string wornChest = ""; bool wornShoulders;
        readonly Dictionary<Renderer, Material> bareMats = new Dictionary<Renderer, Material>();
        Vector3 cloakPos, hairLongPos, hairLongScale, hairBunPos; Quaternion cloakRot; bool cloakHung, hairTucked;
        static Vector3 V0 { get { return Vector3.zero; } }
        static Vector3 One { get { return Vector3.one; } }
        /// <summary>True while the figure's hair shows (a bare head, a cap, a crown); false under a hood, coif, barbute or mask.</summary>
        public bool HairShowing { get { if (hairParts == null) return false; foreach (var h in hairParts) if (h != null && h.gameObject.activeSelf) return true; return false; } }
        bool Bald { get { return hairParts == null || hairParts.Length == 0; } }

        // ---------- building a piece ----------
        sealed class Piece { public int at; public string role; public Mesh mesh; public Matrix4x4 m; }
        /// <summary>
        /// One armour piece being put together: its parts by where they hang (the body, an arm, a leg) and what they are made of
        /// (a role: "plate", "mail", "trim", "glow"...). Built as one part per role and limb.
        /// </summary>
        sealed class Fit
        {
            public readonly GearKit k; public readonly GearLook l; public readonly string key, v; public readonly int tier;
            public bool bald, shoulders; public string chest = "";
            public readonly List<Piece> parts = new List<Piece>();
            public readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
            public Fit(GearKit k, string key) { this.k = k; l = k.l; v = l.variant; tier = l.tier; this.key = key; }
            static readonly Matrix4x4 Flip = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            /// <summary>The same placement seen in a mirror across the body (x flipped): still a proper turn, so faces keep facing out.</summary>
            public static Matrix4x4 Mirror(Matrix4x4 m) { return Flip * m * Flip; }
            public static Matrix4x4 At(Vector3 pos, Vector3 euler, Vector3 scale) { return Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale); }
            public void Add(int at, string role, Mesh mesh, Matrix4x4 m) { parts.Add(new Piece { at = at, role = role, mesh = mesh, m = m }); }
            public void Add(int at, string role, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { Add(at, role, mesh, At(pos, euler ?? Vector3.zero, scale)); }
            public void Add(string role, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { Add(OnBody, role, mesh, pos, scale, euler); }
            /// <summary>On the right limb as given and on the left mirrored (OnArmR or OnLegR).</summary>
            public void Pair(int right, string role, Mesh mesh, Matrix4x4 m) { Add(right, role, mesh, m); Add(right - 1, role, mesh, Mirror(m)); }
            public void Pair(int right, string role, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { Pair(right, role, mesh, At(pos, euler ?? Vector3.zero, scale)); }
            /// <summary>On the body at the right as given and at the left mirrored (pauldrons, flaps).</summary>
            public void Both(string role, Mesh mesh, Matrix4x4 m) { Add(OnBody, role, mesh, m); Add(OnBody, role, mesh, Mirror(m)); }
            public void Both(string role, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { Both(role, mesh, At(pos, euler ?? Vector3.zero, scale)); }
            /// <summary>Accent n (1 at rare, 2 at epic, or 1 when the look asks for a glow): a glowing part, pulsing on epic gear.</summary>
            public void Glow(int n, int at, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { if (l.accents >= n) Add(at, "glow", mesh, pos, scale, euler); }
            public void Glow(int n, int at, Mesh mesh, Matrix4x4 m) { if (l.accents >= n) Add(at, "glow", mesh, m); }
            public void GlowPair(int n, int right, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { if (l.accents >= n) Pair(right, "glow", mesh, pos, scale, euler); }
            /// <summary>The uncommon-and-better extra part, in the trim.</summary>
            public void Fancy(int at, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { if (l.extra) Add(at, "trim", mesh, pos, scale, euler); }
            public void Fancy(int at, Mesh mesh, Matrix4x4 m) { if (l.extra) Add(at, "trim", mesh, m); }
            public void FancyPair(int right, Mesh mesh, Vector3 pos, Vector3 scale, Vector3? euler = null) { if (l.extra) Pair(right, "trim", mesh, pos, scale, euler); }
            /// <summary>Poor gear's wear: a rust patch on metal, a stain or tear on cloth and leather.</summary>
            public void Rust(int at, Vector3 pos, Vector3 size, Vector3? euler = null) { if (l.worn) Add(at, "rust", Cube, pos, size, euler); }
            public void RustPair(int right, Vector3 pos, Vector3 size, Vector3? euler = null) { if (l.worn) Pair(right, "rust", Cube, pos, size, euler); }
        }
        static string K(params float[] n) { var s = new System.Text.StringBuilder(); foreach (var x in n) s.Append(x.ToString("0.####", CultureInfo.InvariantCulture)).Append(','); return s.ToString(); }
        static Vector4 Rg(float hw, float y, float hd, float zc = 0) { return new Vector4(hw, y, hd, zc); }
        static T[] Cat<T>(params T[][] parts) { var all = new List<T>(); foreach (var p in parts) all.AddRange(p); return all.ToArray(); }
        /// <summary>A round ring about local Y (a band on a limb, a collar, a brim's edge): radius r0 to r1, from y0 to y1.</summary>
        static Mesh Ring(float r0, float r1, float y0, float y1, int sides = 16) { return M.Lathe("a.ring." + K(r0, r1, y0, y1, sides), Pts(r0, y0, r1, y0, r1, y1, r0, y1, r0, y0), sides); }
        /// <summary>An oval band round the body (a belt, a hem): <paramref name="h"/> tall from the ring's height, its wall <paramref name="t"/> deep.</summary>
        static Mesh Band(Vector4 ring, float h, float t, float square = 6) { return M.Shell("a.band." + K(ring.x, ring.y, ring.z, ring.w, h, t, square), new[] { ring, new Vector4(ring.x, ring.y + h, ring.z, ring.w) }, t, square); }
        /// <summary>A small leaf (in the XY plane, stem at the origin, facing +Z).</summary>
        static Mesh Leaf { get { return M.Panel("a.leaf", Pts(0, 0, .02f, .022f, .024f, .045f, 0, .075f, -.024f, .045f, -.02f, .022f), .006f, .003f); } }
        /// <summary>A flat fin standing along the head from front to back (a helmet's comb), drawn as (z, y).</summary>
        static Mesh Fin(string key, Vector2[] outline, float thick, Vector2 fan) { return M.Plate(key, outline, thick, 0, null, null, 0, 1, Quaternion.LookRotation(Vector3.up, Vector3.right), fan); }

        void BuildArmor(EquipSlot slot, GearLook l)
        {
            string context = slot == EquipSlot.Neck ? wornChest : slot == EquipSlot.Head ? (wornShoulders ? "sh" : "") + (Bald ? "bald" : "") : "";
            var f = new Fit(Kit(l), l.family + ":" + l.variant + ":t" + l.tier + ":q" + l.quality + (l.forceGlow ? "+g" : "") + ":" + l.detail + ":" + context) { bald = Bald, shoulders = wornShoulders, chest = wornChest };
            switch (slot)
            {
                case EquipSlot.Head: Head(f); break;
                case EquipSlot.Neck: Neck(f); break;
                case EquipSlot.Shoulders: Shoulders(f); break;
                case EquipSlot.Chest: Chest(f); break;
                case EquipSlot.Hands: Hands(f); break;
                case EquipSlot.Legs: Legs(f); break;
                case EquipSlot.Feet: Feet(f); break;
            }
            Fitted(slot, f);
        }
        /// <summary>Builds a piece's parts: one per role and limb, joined from everything of that role there.</summary>
        void Fitted(EquipSlot slot, Fit f)
        {
            int s = (int)slot; var order = new List<(int, string)>(); var groups = new Dictionary<(int, string), List<Piece>>();
            foreach (var p in f.parts)
            {
                var g = (p.at, p.role);
                if (!groups.TryGetValue(g, out var list)) { groups[g] = list = new List<Piece>(); order.Add(g); }
                list.Add(p);
            }
            foreach (var g in order)
            {
                var list = groups[g]; var root = SlotRoot(s, g.Item1); var mat = RoleMat(f, g.Item2); Transform t;
                if (list.Count == 1)
                {
                    var p = list[0]; t = GPart(root, p.mesh, mat, p.m.GetColumn(3), p.m.lossyScale); t.localRotation = p.m.rotation;
                }
                else
                {
                    var join = new (Mesh, Matrix4x4)[list.Count]; for (int i = 0; i < list.Count; i++) join[i] = (list[i].mesh, list[i].m);
                    t = GPart(root, M.Join(f.key + "|" + g.Item1 + "|" + g.Item2 + "|" + list.Count, join), mat, Vector3.zero, Vector3.one);
                }
                if (g.Item2 == "glow") f.k.lit.Add(t.GetComponent<Renderer>());
            }
            Finish(f.k, gearRoots[s]);
        }
        /// <summary>A slot's root on the body, or its root on an arm or leg (made the first time it is needed).</summary>
        Transform SlotRoot(int s, int at)
        {
            if (at == OnBody) return gearRoots[s];
            var pivot = at == OnArmL ? armL : at == OnArmR ? armR : at == OnLegL ? legL : legR;
            foreach (var t in gearMore[s]) if (t.parent == pivot) return t;
            var r = new GameObject("Gear " + ItemDatabase.SlotNames[s] + " (" + pivot.name + ")").transform; r.SetParent(pivot, false); gearMore[s].Add(r); return r;
        }
        /// <summary>The shared material a role stands for in this look (GearMats, never edited).</summary>
        static Material RoleMat(Fit f, string role)
        {
            if (f.mats.TryGetValue(role, out var m)) return m;
            var k = f.k; var l = f.l;
            switch (role)
            {
                case "metal": return k.metal; case "plate": return k.plate; case "mail": return k.mail; case "edge": return k.edge; case "dark": return k.dark;
                case "wood": return k.wood; case "leather": return k.leather; case "cloth2": return k.cloth2; case "trim": return k.trim; case "bone": return k.bone;
                case "glow": return k.glow; case "rust": return k.rust; case "fur": return k.fur; case "lit": return k.Lit(1.5f);
                case "leather2": return GearMats.Get(Color.Lerp(l.leather, l.cloth, .25f) * 1.18f, .32f);   // boiled leather, a folded boot top
                case "hide": return GearMats.Get(Color.Lerp(l.leather, l.cloth, .45f), .18f);
                case "oil": return GearMats.Get(Color.Lerp(l.leather, l.cloth, .3f) * .9f, .58f);
                case "scale": return GearMats.Get(Color.Lerp(l.metal, l.trim, .15f), .42f + .05f * l.quality, .58f);
                case "silk": return GearMats.Get(l.cloth2, .66f, .05f);
                case "glass": return GearMats.Glass(Color.Lerp(l.cloth, l.glow, .35f), .55f, l.glow * .35f);
                case "bark": return GearMats.Get(Color.Lerp(l.wood, new Color(.3f, .24f, .17f), .4f), .1f);
                case "rime": return GearMats.Get(Color.Lerp(l.trim, Color.white, .55f), .86f, .05f, l.glow * .18f);
                case "tin": return GearMats.Get(Color.Lerp(l.metal, new Color(.66f, .67f, .64f), .7f), .4f, .45f, new Color(.07f, .07f, .065f));
                case "bead": return GearMats.Get(new Color(.55f, .1f, .07f), .85f, 0, new Color(.14f, .02f, .01f));   // red glass
                case "gem": return GearMats.Get(Color.Lerp(l.glow, l.cloth2, .35f), .9f, .1f);
                case "antler": return GearMats.Get(Color.Lerp(l.bone, l.wood, .35f), .3f);
                case "sap": return GearMats.Get(new Color(.85f, .55f, .15f), .85f, 0, new Color(.25f, .12f, .02f));
                case "patch": return GearMats.Get(Color.Lerp(l.leather, l.cloth, .5f) * .85f, .1f);
                case "green": return GearMats.Get(Color.Lerp(l.cloth, new Color(.32f, .5f, .24f), .6f), .3f);
                case "slot": return GearMats.Get(new Color(.04f, .035f, .035f), .2f);
                default: return k.cloth;
            }
        }
        /// <summary>
        /// The generated material's small mark (looks.json "detail") on a surface whose outward side is local +Z after
        /// <paramref name="euler"/>: rivets, a band, stitching, a bone toggle, an ember seam, a leaf, a root curl, a drop of sap or
        /// a patch. Each joins the part already made of its material, if there is one; on a limb (<paramref name="join"/>) it is
        /// left off unless it can join one, so paired pieces stay inside the part budget.
        /// </summary>
        static void Mark(Fit f, int at, Vector3 pos, Vector3 euler, float size = 1, bool join = false)
        {
            var m = Fit.At(pos, euler, Vector3.one * size);
            if (join && !f.parts.Exists(p => p.at == at && p.role == MarkRole(f.l.detail))) return;
            switch (f.l.detail)
            {
                case "rivets": f.Add(at, "dark", M.Many("a.mark.rivets", Sphere, M.At(V(-.03f, 0, 0), V0, V(.02f, .02f, .012f)), M.At(V0, V0, V(.02f, .02f, .012f)), M.At(V(.03f, 0, 0), V0, V(.02f, .02f, .012f))), m); break;
                case "band": case "plate": f.Add(at, "trim", Cube, m * Matrix4x4.Scale(V(.08f, .016f, .008f))); break;
                case "cord": case "stitch": f.Add(at, "cloth2", M.Many("a.mark.stitch", Cube, M.At(V(-.03f, 0, 0), V(0, 0, 20), V(.014f, .005f, .004f)), M.At(V(-.01f, 0, 0), V(0, 0, -20), V(.014f, .005f, .004f)), M.At(V(.01f, 0, 0), V(0, 0, 20), V(.014f, .005f, .004f)), M.At(V(.03f, 0, 0), V(0, 0, -20), V(.014f, .005f, .004f))), m); break;
                case "bone": f.Add(at, "bone", Cylinder, m * Fit.At(V0, V(0, 0, 90), V(.018f, .02f, .018f))); break;
                case "ember": f.Add(at, "lit", Cube, m * Matrix4x4.Scale(V(.07f, .008f, .008f))); break;
                case "leaf": f.Add(at, "green", Leaf, m * Fit.At(V(0, -.03f, 0), V0, One * .8f)); break;
                case "root": f.Add(at, "wood", M.Rod("a.mark.root", new[] { V(-.03f, -.012f, 0), V(-.012f, .022f, .006f), V(.018f, .016f, .006f), V(.032f, -.01f, 0) }, .007f, .003f), m); break;
                case "sap": f.Add(at, "sap", Sphere, m * Matrix4x4.Scale(V(.024f, .03f, .014f))); break;
                case "fray": case "patch": f.Add(at, "patch", Cube, m * Matrix4x4.Scale(V(.05f, .045f, .006f))); break;
            }
        }
        static string MarkRole(string detail)
        {
            switch (detail)
            {
                case "rivets": return "dark"; case "band": case "plate": return "trim"; case "cord": case "stitch": return "cloth2"; case "bone": return "bone";
                case "ember": return "lit"; case "leaf": return "green"; case "root": return "wood"; case "sap": return "sap"; case "fray": case "patch": return "patch";
                default: return "";
            }
        }

        // ---------- what the bare body shows ----------
        /// <summary>Puts a piece's material on bare body parts (the first time, their own material is kept to give back).</summary>
        void Cover(Renderer[] parts, Material m)
        {
            if (parts == null || m == null) return;
            foreach (var r in parts) if (r != null) { if (!bareMats.ContainsKey(r)) bareMats[r] = r.sharedMaterial; r.sharedMaterial = m; }
        }
        void Bare(Renderer[] parts) { if (parts != null) foreach (var r in parts) if (r != null && bareMats.TryGetValue(r, out var m)) r.sharedMaterial = m; }
        void ShowHair(bool on) { if (hairParts != null) foreach (var h in hairParts) if (h != null) h.gameObject.SetActive(on); }
        /// <summary>Under a cap, a kettle hat or a head-wrap the long fall of hair stops at the brim and a bun sits below it.</summary>
        void Tuck(bool on)
        {
            if (on == hairTucked) return;
            hairTucked = on;
            if (hairLong != null)
            {
                if (on) { hairLongPos = hairLong.localPosition; hairLongScale = hairLong.localScale; hairLong.localPosition = new Vector3(0, .665f, -.12f); hairLong.localScale = new Vector3(.3f, .27f, .08f); }
                else { hairLong.localPosition = hairLongPos; hairLong.localScale = hairLongScale; }
            }
            if (hairBun != null) { if (on) { hairBunPos = hairBun.localPosition; hairBun.localPosition = new Vector3(0, .77f, -.165f); } else hairBun.localPosition = hairBunPos; }
        }
        /// <summary>The Druid's cloak hangs from the back of the shoulders, falling clear of whatever is worn over the chest.</summary>
        void HangCloak(bool on)
        {
            if (druidCloak == null || on == cloakHung) return;
            cloakHung = on;
            if (on) { cloakPos = druidCloak.localPosition; cloakRot = druidCloak.localRotation; druidCloak.localPosition = new Vector3(0, .19f, -.271f); druidCloak.localEulerAngles = new Vector3(8, 0, 0); }
            else { druidCloak.localPosition = cloakPos; druidCloak.localRotation = cloakRot; }
        }
        void DruidHood(bool on) { if (built == ActorLook.Druid && classKit != null) foreach (var t in classKit) if (t != null) t.gameObject.SetActive(on); }
        /// <summary>Gives back what a slot's piece covered (called whenever the slot is cleared).</summary>
        void Uncover(EquipSlot slot)
        {
            switch (slot)
            {
                case EquipSlot.Head: ShowHair(true); Tuck(false); DruidHood(true); break;
                case EquipSlot.Chest: Bare(baseChest); Bare(baseSleeves); if (baseBelt != null) baseBelt.enabled = true; HangCloak(false); break;
                case EquipSlot.Hands: Bare(baseHands); break;
                case EquipSlot.Legs: Bare(baseLegs); break;
                case EquipSlot.Feet: Bare(baseBoots); break;
            }
        }

        // ---------- head ----------
        void Head(Fit f)
        {
            switch (f.l.family)
            {
                case "head.cap": Cap(f); Tuck(true); break;
                case "head.hood": Hood(f, false); ShowHair(false); break;
                case "head.coif": Coif(f); ShowHair(false); break;
                case "head.wrap": HeadWrap(f); Tuck(true); break;
                case "head.kettle": Kettle(f); Tuck(true); break;
                case "head.barbute": Barbute(f); ShowHair(false); break;
                case "head.mask": Hood(f, true); ShowHair(false); break;
                case "head.circlet": Circlet(f); break;
                case "head.crown": Crown(f); break;
            }
            DruidHood(false);   // the Druid's own hood yields to any head piece
        }
        /// <summary>A skull-cap over the hair, a band round its brim. Flaps: ear flaps hanging from under the band. Leaf: leaves stitched round the brim.</summary>
        void Cap(Fit f)
        {
            string main = f.v == "leaf" || f.tier == 0 ? "cloth" : "leather", band = f.tier >= 1 ? "trim" : "cloth2";
            f.Add(main, M.Lathe("a.cap.dome", Pts(.178f, .83f, .181f, .87f, .165f, .935f, .116f, .982f, .05f, .998f, 0, 1.0f), 16, true), V0, One);
            f.Add(band, Ring(.174f, .188f, .826f, .858f, 18), V0, One);
            if (f.v == "flaps") f.Both(main, Sphere, V(.178f, .775f, -.005f), V(.045f, .14f, .11f), V(0, 0, 8));
            if (f.v == "leaf")
            {
                var leaves = new List<Matrix4x4>();
                for (int i = 0; i < 8; i++) { float a = 20 + i * 20; leaves.Add(Fit.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .19f, .838f, Mathf.Sin(a * Mathf.Deg2Rad) * .19f), V(-25, 90 - a, 0), One)); }
                f.Add(OnBody, "green", M.Many("a.cap.leaves", Leaf, leaves.ToArray()), Matrix4x4.identity);
                f.Add(OnBody, "green", M.Many("a.cap.sprig", Leaf, M.At(V0, V(0, 0, 0), One), M.At(V0, V(0, 0, 35), V(.8f, .8f, .8f)), M.At(V0, V(0, 0, -35), V(.8f, .8f, .8f))), Fit.At(V(0, .99f, .03f), V(-15, 0, 0), One));
            }
            if (f.tier >= 1 || f.l.extra) f.Add(band, Sphere, V(0, 1.0f, 0), V(.035f, .022f, .035f));   // the button on top
            f.Fancy(OnBody, M.Arc("a.cap.seam", .172f, .181f, -70, 70, .012f), V(0, .83f, 0), One, V(0, 0, 90));   // a raised seam over the crown
            f.Glow(1, OnBody, Sphere, V(0, .842f, .19f), V(.032f, .03f, .016f));
            f.Glow(2, OnBody, Sphere, V(0, 1.008f, 0), V(.034f, .024f, .034f));
            f.Rust(OnBody, V(.1f, .925f, .118f), V(.05f, .04f, .012f), V(-30, 40, 0));
            Mark(f, OnBody, V(.15f, .842f, .118f), V(0, 52, 0));
        }
        /// <summary>
        /// A hood with a face opening from chin to brow and a drape over the shoulders (tucked short under shoulder armour).
        /// Oilskin: a stiff peaked brim. Mask (head.mask): the Cult's bone mask in the opening; tear: its weeping violet light.
        /// </summary>
        void Hood(Fit f, bool mask)
        {
            var rings = new List<Vector4>(); var gaps = new List<float>();
            if (!f.shoulders) { rings.Add(Rg(.39f, .5f, .25f, -.03f)); rings.Add(Rg(.37f, .56f, .24f, -.03f)); gaps.Add(0); gaps.Add(0); }
            rings.AddRange(new[] { Rg(.3f, .64f, .21f, -.03f), Rg(.2f, .69f, .18f, -.03f), Rg(.19f, .76f, .19f, -.025f), Rg(.19f, .85f, .2f, -.03f), Rg(.175f, .95f, .19f, -.04f), Rg(.115f, 1.025f, .135f, -.05f), Rg(0, 1.055f, 0, -.06f) });
            gaps.AddRange(new[] { 0f, 30, 80, 110, 80, 0, 0 });
            string main = f.v == "oilskin" ? "oil" : "cloth";
            f.Add(main, M.Shell("a.hood" + (f.shoulders ? ".short" : ""), rings.ToArray(), .02f, 2.2f, gaps.ToArray(), 90, 28), V0, One);
            if (f.v == "oilskin") f.Add(main, M.Plate("a.hood.brim", Pts(-.13f, 0, .13f, 0, .11f, .06f, .06f, .09f, 0, .1f, -.06f, .09f, -.11f, .06f), .012f), V(0, .962f, .085f), One, V(12, 0, 0));
            var hem = rings[0]; string hemRole = f.tier >= 1 ? "trim" : "cloth2";
            f.Add(hemRole, Band(new Vector4(hem.x + .006f, hem.y - .004f, hem.z + .006f, hem.w), .03f, .012f, 2.2f), V0, One);
            if (f.tier >= 2 || f.l.extra) f.Add("trim", Sphere, V(0, .66f, .178f), V(.034f, .034f, .02f));   // the clasp at the throat
            if (mask)
            {
                f.Add("bone", Sphere, V(0, .8f, .1f), V(.26f, .3f, .17f));
                f.Add("slot", M.Many("a.mask.eyes", Sphere, M.At(V(-.052f, 0, 0), V0, V(.055f, .034f, .02f)), M.At(V(.052f, 0, 0), V0, V(.055f, .034f, .02f))), V(0, .835f, .17f), One);
                if (f.v == "tear") f.Add("lit", Cube, V(.07f, .75f, .165f), V(.022f, .08f, .008f), V(0, 0, 5));
                else f.Add("bone", M.Arc("a.mask.brow", .155f, .177f, 55, 125, .024f), V(0, .88f, 0), One);
                f.Glow(1, OnBody, Sphere, V(0, .9f, .164f), V(.03f, .03f, .014f));
            }
            else f.Glow(1, OnBody, Sphere, V(0, .66f, .19f), V(.03f, .03f, .016f));
            f.Glow(2, OnBody, Sphere, mask ? V(0, .66f, .19f) : V(0, 1.0f, .098f), V(.028f, .028f, .014f));
            f.Rust(OnBody, V(.15f, .9f, -.12f), V(.05f, .05f, .012f), V(0, -50, 0));
            Mark(f, OnBody, V(.16f, .62f, .1f), V(0, 55, 0));
        }
        /// <summary>A mail coif: rows of rings over the head and down to a collar, open round the face. Better ones add a brow band, a steel skull-plate, a nasal.</summary>
        void Coif(Fit f)
        {
            var st = new[] { Rg(.215f, .635f, .17f, -.01f), Rg(.185f, .68f, .165f, -.01f), Rg(.192f, .74f, .18f, -.01f), Rg(.196f, .84f, .195f, -.01f), Rg(.18f, .94f, .18f, -.02f) };
            var rows = M.Rows(st, .022f, .0045f, false, new[] { 0f, 20, 60, 100, 70 }, out var rowGaps);
            f.Add("mail", M.Shell("a.coif", Cat(rows, new[] { Rg(.11f, 1.015f, .115f, -.02f), Rg(0, 1.04f, 0, -.02f) }), .016f, 2.2f, Cat(rowGaps, new[] { 0f, 0 }), 90, 28), V0, One);
            f.Add(f.tier >= 1 ? "leather" : "cloth2", Band(Rg(.222f, .62f, .177f, -.01f), .035f, .014f, 2.2f), V0, One);   // the collar
            if (f.tier >= 2) f.Add("trim", Band(Rg(.19f, .915f, .19f, -.02f), .03f, .012f, 2.2f), V0, One);                  // a brow band over the mail
            if (f.tier >= 3) f.Add("plate", M.Lathe("a.coif.plate", Pts(.19f, .925f, .192f, .96f, .16f, 1.01f, .09f, 1.045f, 0, 1.06f), 16, true), V(0, 0, -.015f), One);
            if (f.tier >= 4) f.Add("plate", Cube, V(0, .85f, .2f), V(.022f, .12f, .012f), V(-6, 0, 0));                    // the nasal
            if (f.l.extra)
            {
                var studs = new Matrix4x4[8]; for (int i = 0; i < 8; i++) { float a = 10 + i * 22.5f; studs[i] = M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .226f, .638f, Mathf.Sin(a * Mathf.Deg2Rad) * .18f - .01f), V0, S(.02f)); }
                f.Add(OnBody, "trim", M.Many("a.coif.studs", Sphere, studs), Matrix4x4.identity);
            }
            f.Glow(1, OnBody, Sphere, V(0, .64f, .172f), V(.03f, .03f, .016f));
            f.Glow(2, OnBody, Sphere, f.tier >= 2 ? V(0, .93f, .172f) : V(0, 1.045f, -.02f), V(.03f, .03f, .016f));
            f.Rust(OnBody, V(-.15f, .8f, .09f), V(.05f, .06f, .012f), V(0, -55, 0));
            Mark(f, OnBody, V(.17f, .64f, .07f), V(0, 70, 0));
        }
        /// <summary>The Outrider's head-wrap: wound cloth over the head, a twisted band and two tails hanging down the back.</summary>
        void HeadWrap(Fit f)
        {
            f.Add("cloth", M.Lathe("a.wrap.dome", Pts(.172f, .79f, .184f, .84f, .179f, .9f, .15f, .962f, .08f, 1.0f, 0, 1.012f), 16, true), V0, One);
            var band = M.Lathe("a.wrap.band", Pts(.178f, .826f, .196f, .83f, .198f, .862f, .18f, .866f, .178f, .826f), 18);
            f.Add("cloth2", band, V0, One, V(5, 0, 0)); f.Add("cloth2", band, V(0, .02f, 0), V(.985f, 1, .985f), V(-6, 0, 3));
            f.Add("cloth2", Cube, V(.04f, .72f, -.205f), V(.07f, .3f, .02f), V(12, 0, -6)); f.Add("cloth2", Cube, V(-.03f, .7f, -.198f), V(.06f, .22f, .018f), V(8, 0, 8));
            f.Fancy(OnBody, Sphere, V(.15f, .86f, -.11f), V(.05f, .045f, .045f));   // the knot
            f.Glow(1, OnBody, Sphere, V(.172f, .85f, .07f), V(.016f, .03f, .03f));
            f.Glow(2, OnBody, Sphere, V(0, .848f, .2f), V(.03f, .028f, .014f));
            f.Rust(OnBody, V(-.12f, .93f, .1f), V(.05f, .04f, .012f), V(-30, -40, 0));
            Mark(f, OnBody, V(-.15f, .846f, .11f), V(0, -55, 0));
        }
        /// <summary>A kettle hat: a steel dome with a wide drooping brim. Half: no brim, a nasal bar. Rivets from the third band, a comb from the fourth, a taller one at the top.</summary>
        void Kettle(Fit f)
        {
            bool half = f.v == "half"; string main = f.tier >= 1 ? "plate" : "leather2";
            if (half) f.Add(main, M.Lathe("a.kettle.half", Pts(.178f, .832f, .184f, .872f, .175f, .945f, .132f, 1.008f, .06f, 1.042f, 0, 1.05f), 16, true), V0, One);
            else
            {
                f.Add(main, M.Lathe("a.kettle.dome", Pts(.174f, .855f, .18f, .892f, .17f, .962f, .126f, 1.026f, .056f, 1.056f, 0, 1.062f), 16, true), V0, One);
                f.Add(main, M.Lathe("a.kettle.brim", Pts(.168f, .846f, .262f, .822f, .266f, .835f, .178f, .868f, .168f, .846f), 24), V0, One);
            }
            if (half) f.Add(main, Cube, V(0, .8f, .197f), V(.024f, .1f, .012f), V(-8, 0, 0));   // the nasal
            float rimY = half ? .844f : .871f, rimR = half ? .19f : .186f;
            f.Add("trim", half ? Ring(.18f, .192f, .83f, .858f, 18) : Ring(.176f, .188f, .858f, .884f, 18), V0, One);
            if (f.tier >= 2)
            {
                var rivets = new Matrix4x4[10]; for (int i = 0; i < 10; i++) { float a = i * 36 + 18; rivets[i] = M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * (rimR + .003f), 0, Mathf.Sin(a * Mathf.Deg2Rad) * (rimR + .003f)), V0, S(.016f)); }
                f.Add(OnBody, "dark", M.Many("a.kettle.rivets." + (half ? "h" : "k"), Sphere, rivets), Fit.At(V(0, rimY, 0), V0, One));
            }
            if (f.tier >= 3)
            {
                float top = half ? 1.05f : 1.062f, up = f.tier >= 4 ? .06f : .03f;
                f.Add("trim", Fin("a.kettle.comb." + (half ? "h" : "k") + f.tier, Pts(-.13f, top - .042f, -.06f, top - .009f, 0, top - .002f, .06f, top - .009f, .13f, top - .042f, .1f, top - .02f + up * .6f, 0, top + up, -.1f, top - .02f + up * .6f), .016f, new Vector2(0, top + up * .4f)), V0, One);
            }
            if (!half) f.Fancy(OnBody, Ring(.256f, .27f, .818f, .84f, 24), V0, One);   // a rolled brim edge
            else f.Fancy(OnBody, Ring(.17f, .182f, .9f, .915f, 18), V0, One);
            f.Glow(1, OnBody, Sphere, V(0, rimY, rimR + .004f), V(.032f, .028f, .014f));
            f.Glow(2, OnBody, Sphere, V(0, (half ? 1.05f : 1.062f) + (f.tier >= 3 ? (f.tier >= 4 ? .06f : .03f) : .005f), 0), V(.032f, .03f, .032f));
            f.Rust(OnBody, V(.1f, .97f, .1f), V(.05f, .05f, .012f), V(-35, 45, 0));
            Mark(f, OnBody, V(Mathf.Cos(30 * Mathf.Deg2Rad) * (rimR + .002f), rimY, Mathf.Sin(30 * Mathf.Deg2Rad) * (rimR + .002f)), V(0, 60, 0));
        }
        /// <summary>
        /// A barbute: a closed steel helm to the jaw with a T-shaped opening (an eye slit and a slot to the chin) and a gorget on the
        /// shoulders; it hides the hair. On epic gear the T glows. Fourth band: a comb, a brow band and cheek rivets. Rimed: frost
        /// crusted on the rims, icicles at the jaw.
        /// </summary>
        void Barbute(Fit f)
        {
            f.Add("plate", M.Lathe("a.barbute", Pts(.15f, .652f, .196f, .664f, .205f, .73f, .207f, .81f, .198f, .9f, .165f, .97f, .1f, 1.025f, 0, 1.046f), 20, true), V0, One);
            string slot = f.l.accents >= 2 ? "glow" : "slot";
            f.Add(slot, M.Arc("a.barbute.slit", .2f, .2105f, 54, 126, .026f), V(0, .842f, 0), One);
            f.Add(slot, Cube, V(0, .765f, .204f), V(.042f, .12f, .012f));
            f.Add(f.tier >= 4 ? "plate" : "mail", M.Lathe("a.barbute.gorget", Pts(.225f, .565f, .19f, .6f, .16f, .645f, .15f, .66f), 18, true), V0, One);
            if (f.tier >= 4 || f.l.extra) f.Add("trim", Ring(.19f, .2f, .885f, .91f, 20), V0, One);   // the brow band
            if (f.tier >= 4)
            {
                f.Add("trim", Fin("a.barbute.comb", Pts(-.16f, .93f, -.1f, 1.0f, 0, 1.044f, .1f, 1.0f, .14f, .955f, .1f, 1.04f, 0, 1.09f, -.12f, 1.03f), .016f, new Vector2(0, 1.03f)), V0, One);
                f.Add(OnBody, "dark", M.Many("a.barbute.rivets", Sphere, M.At(V(.198f, .7f, .06f), V0, S(.016f)), M.At(V(-.198f, .7f, .06f), V0, S(.016f)), M.At(V(.2f, .76f, .07f), V0, S(.016f)), M.At(V(-.2f, .76f, .07f), V0, S(.016f))), Matrix4x4.identity);
            }
            if (f.v == "rimed")
            {
                var frost = new List<Matrix4x4>();
                for (int i = 0; i < 9; i++) { float a = i * 40; frost.Add(M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .198f, .672f, Mathf.Sin(a * Mathf.Deg2Rad) * .198f), V0, V(.045f, .022f, .045f))); }
                for (int i = 0; i < 6; i++) { float a = 30 + i * 60; frost.Add(M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .17f, 1.0f, Mathf.Sin(a * Mathf.Deg2Rad) * .17f), V0, V(.05f, .02f, .05f))); }
                f.Add(OnBody, "rime", M.Many("a.barbute.rime", Sphere, frost.ToArray()), Matrix4x4.identity);
                var icicles = new Matrix4x4[6]; for (int i = 0; i < 6; i++) { float a = 200 + i * 28; icicles[i] = M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .2f, .664f, Mathf.Sin(a * Mathf.Deg2Rad) * .2f), V(180, 0, 0), V(.018f, .04f + (i % 3) * .015f, .018f)); }
                f.Add(OnBody, "rime", M.Many("a.barbute.icicles", M.Cone(6), icicles), Matrix4x4.identity);
            }
            f.Glow(1, OnBody, Sphere, V(0, .93f, .186f), V(.034f, .03f, .016f));
            f.Rust(OnBody, V(-.13f, .95f, .1f), V(.05f, .05f, .012f), V(-35, -45, 0));
            Mark(f, OnBody, V(.19f, .78f, .07f), V(0, 70, 0));
        }
        /// <summary>A thin band round the brow (lower and smaller on a bald head). Band: a set stone at the front. Briar: a ring of thorns and leaves.</summary>
        void Circlet(Fit f)
        {
            var at = Fit.At(V(0, f.bald ? .868f : .9f, 0), V(4, 0, 0), One * (f.bald ? .87f : 1));
            f.Add(OnBody, "trim", Ring(.158f, .172f, -.011f, .011f, 22), at);
            if (f.v == "briar")
            {
                var thorns = new Matrix4x4[12];
                for (int i = 0; i < 12; i++) { float a = i * 30; var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), .7f, Mathf.Sin(a * Mathf.Deg2Rad)); thorns[i] = Matrix4x4.TRS(V(d.x * .17f, .005f, d.z * .17f), Quaternion.FromToRotation(Vector3.up, d), V(.018f, .05f, .018f)); }
                f.Add(OnBody, "wood", M.Many("a.circlet.thorns", M.Cone(6), thorns), at);
                f.Add(OnBody, "green", M.Many("a.circlet.leaves", Leaf, M.At(V(.06f, 0, .165f), V(-20, 20, 25), One), M.At(V(-.06f, 0, .165f), V(-20, -20, -25), One), M.At(V(0, .01f, .174f), V(-30, 0, 0), One)), at);
            }
            else f.Add(OnBody, "trim", Cube, at * Fit.At(V(0, 0, .174f), V(0, 0, 45), V(.045f, .045f, .012f)));   // the setting
            f.Add(OnBody, f.l.accents >= 1 ? "glow" : "gem", Sphere, at * Fit.At(V(0, 0, .182f), V0, V(.026f, .026f, .014f)));
            if (f.l.accents >= 2) f.Add(OnBody, "glow", M.Many("a.circlet.gems", Sphere, M.At(V(.147f, 0, .085f), V0, S(.02f)), M.At(V(-.147f, 0, .085f), V0, S(.02f))), at);
            f.Fancy(OnBody, M.Many("a.circlet.studs", Sphere, M.At(V(.12f, 0, .122f), V0, S(.014f)), M.At(V(-.12f, 0, .122f), V0, S(.014f)), M.At(V(.17f, 0, 0), V0, S(.014f)), M.At(V(-.17f, 0, 0), V0, S(.014f))), at);
            f.Rust(OnBody, V(.12f, .9f, -.12f), V(.03f, .02f, .01f), V(0, -45, 0));
            Mark(f, OnBody, V(-.14f, .9f, -.1f), V(0, -125, 0), .6f);
        }
        /// <summary>
        /// A crown (lower and smaller on a bald head). Tin: the Bandit King's hammered ring of twelve plates and seven uneven prongs
        /// with a bead of red glass, worn crooked. Root: living roots woven round the head, tines curling up to lit buds. Antler:
        /// a wooden band and nine tines.
        /// </summary>
        void Crown(Fit f)
        {
            float s = f.bald ? .78f : 1;
            switch (f.v)
            {
                case "tin":
                {
                    var at = Fit.At(V(0, f.bald ? .89f : .925f, -.02f * s), V(4, 0, -6), One * s); var tin = new List<(Mesh, Matrix4x4)>();
                    for (int i = 0; i < 12; i++) { float a = i * 30; tin.Add((Cube, M.At(new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0, Mathf.Cos(a * Mathf.Deg2Rad)) * .165f, V(0, a, 0), V(.092f, .05f, .024f)))); }
                    float[] off = { 0, 7, -5, 4, -8, 6, -3 }, tall = { .145f, .1f, .125f, .11f, .15f, .095f, .12f };
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * 360f / 7 + off[i]; var up = Quaternion.Euler(8, a, 0) * Vector3.up; var foot = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * .168f, .02f, Mathf.Cos(a * Mathf.Deg2Rad) * .168f);
                        tin.Add((Cube, M.At(foot + up * (tall[i] / 2), V(8, a, 0), V(.036f, tall[i], .02f)))); tin.Add((Cube, M.At(foot + up * tall[i], V(8, a, 45), V(.028f, .028f, .02f))));
                    }
                    f.Add(OnBody, "tin", M.Join("a.crown.tin", tin), at);
                    f.Add(OnBody, f.l.accents >= 1 ? "glow" : "bead", Sphere, at * Fit.At(V(0, 0, .18f), V0, V(.045f, .045f, .03f)));
                    if (f.l.accents >= 2) f.Add(OnBody, "glow", Sphere, at * Fit.At(V(0, .02f, -.18f), V0, V(.035f, .035f, .025f)));
                    f.Fancy(OnBody, M.Many("a.crown.tin.studs", Sphere, M.At(V(.16f, 0, .06f), V0, S(.022f)), M.At(V(-.16f, 0, .06f), V0, S(.022f))), at);
                    f.Rust(OnBody, at.MultiplyPoint3x4(V(.12f, 0, .11f)), V(.04f, .03f, .01f), V(0, 45, 0));
                    break;
                }
                case "root":
                {
                    var at = Fit.At(V(0, f.bald ? .88f : .91f, -.01f), V(3, 0, 0), One * s); var roots = new List<(Mesh, Matrix4x4)>();
                    for (int i = 0; i < 6; i++)
                    {
                        float a0 = i * 60, a1 = a0 + 60, am = a0 + 30; float h = i % 2 == 0 ? .018f : -.014f;
                        Vector3 P(float a, float r, float y) { return new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * r, y, Mathf.Sin(a * Mathf.Deg2Rad) * r); }
                        roots.Add((M.Rod("a.crown.root.arc" + i, new[] { P(a0, .168f, -h), P(am, .19f, h * 1.6f), P(a1, .168f, -h) }, .016f, .014f), Matrix4x4.identity));
                    }
                    foreach (var (a, tall) in new[] { (90f, .16f), (55f, .12f), (125f, .12f), (20f, .09f), (160f, .09f) })
                    {
                        var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad));
                        roots.Add((M.Rod("a.crown.root.tine" + a, new[] { d * .17f, d * .18f + Vector3.up * tall * .6f, d * .2f + Vector3.up * tall, d * .17f + Vector3.up * tall * 1.15f }, .014f, .004f), Matrix4x4.identity));
                    }
                    f.Add(OnBody, "wood", M.Join("a.crown.roots", roots), at);
                    var buds = new List<Matrix4x4>();
                    foreach (var (a, tall) in new[] { (90f, .16f), (55f, .12f), (125f, .12f) }) { var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)); buds.Add(M.At(d * .172f + Vector3.up * tall * 1.15f, V0, S(.026f))); }
                    f.Add(OnBody, f.l.accents >= 1 ? "glow" : "lit", M.Many("a.crown.root.buds", Sphere, buds.ToArray()), at);
                    if (f.l.accents >= 2) f.Add(OnBody, "glow", Sphere, at * Fit.At(V(0, .02f, .19f), V0, V(.03f, .03f, .02f)));
                    f.Fancy(OnBody, M.Many("a.crown.root.leaves", Leaf, M.At(V(.09f, .01f, .16f), V(-10, 30, 20), One), M.At(V(-.09f, .01f, .16f), V(-10, -30, -20), One)), at);
                    f.Rust(OnBody, at.MultiplyPoint3x4(V(-.12f, 0, .12f)), V(.03f, .03f, .01f), V(0, -45, 0));
                    break;
                }
                default:   // antler: nine tines
                {
                    var at = Fit.At(V(0, f.bald ? .875f : .905f, -.01f), V(3, 0, 0), One * s); var tines = new List<(Mesh, Matrix4x4)>();
                    tines.Add((Ring(.16f, .178f, -.016f, .016f, 22), Matrix4x4.identity));
                    float[] angles = { 0, 25, 50, 130, 155, 180, 70, 110, 90 }; float[] lean = { 30, 25, 15, 15, 25, 30, 8, 8, 0 };
                    for (int i = 0; i < 9; i++)
                    {
                        float a = angles[i]; var d = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0, Mathf.Sin(a * Mathf.Deg2Rad)); float h = i < 6 ? .13f : .1f; var tip = d * (.17f + h * Mathf.Sin(lean[i] * Mathf.Deg2Rad) * 1.4f) + Vector3.up * h;
                        tines.Add((M.Rod("a.crown.tine" + i, new[] { d * .169f, d * .175f + Vector3.up * h * .55f, tip }, .013f, .004f), Matrix4x4.identity));
                    }
                    f.Add(OnBody, "antler", M.Join("a.crown.antler", tines), at);
                    f.Add(OnBody, f.l.accents >= 1 ? "glow" : "gem", Sphere, at * Fit.At(V(0, 0, .182f), V0, V(.028f, .028f, .014f)));
                    if (f.l.accents >= 2) f.Add(OnBody, "glow", Sphere, at * Fit.At(V(0, .13f, .17f), V0, S(.024f)));
                    f.Fancy(OnBody, M.Many("a.crown.antler.studs", Sphere, M.At(V(.13f, 0, .115f), V0, S(.016f)), M.At(V(-.13f, 0, .115f), V0, S(.016f))), at);
                    f.Rust(OnBody, at.MultiplyPoint3x4(V(-.12f, 0, .12f)), V(.03f, .02f, .01f), V(0, -45, 0));
                    break;
                }
            }
            Mark(f, OnBody, V(.15f, f.bald ? .87f : .9f, -.08f), V(0, 115, 0), .6f);
        }

        // ---------- neck ----------
        /// <summary>How far forward the chest stands at height .5, where a pendant lies, for the chest piece worn (family:variant).</summary>
        static float ChestFront(string chest)
        {
            if (chest.StartsWith("chest.jerkin")) return .195f;
            if (chest.StartsWith("chest.hauberk")) return .2f;
            if (chest == "chest.coat:grey") return .228f;
            if (chest.StartsWith("chest.coat")) return .212f;
            if (chest.StartsWith("chest.cuirass")) return .212f;
            return .166f;   // bare, a tunic or a robe: the chest itself
        }
        /// <summary>
        /// Neck pieces lie round the neck (on the collar of a breastplate, mail or a jerkin) and hang onto the chest. Pendant: a
        /// cord and a drop (glass, a seal, a signet, an ember in a cage, a leaf, a vial, a jar). Cord: beads or one hung piece (a
        /// knot, a fang, a tooth, a tine). Torc: an open ring with heavy ends.
        /// </summary>
        void Neck(Fit f)
        {
            bool shell = f.chest.StartsWith("chest.jerkin") || f.chest.StartsWith("chest.hauberk") || f.chest.StartsWith("chest.coat") || f.chest.StartsWith("chest.cuirass");
            float ringY = shell ? .688f : .615f, ringR = shell ? .108f : .09f, front = ChestFront(f.chest);
            var ring = Fit.At(V(0, ringY, 0), V(14, 0, 0), One);
            var ringFront = ring.MultiplyPoint3x4(V(0, 0, ringR + .004f)); var drop = V(0, .5f, front + .012f);
            string family = f.l.family;
            if (family == "neck.torc")
            {
                string role = f.v == "wood" ? "wood" : f.tier >= 2 ? "trim" : "metal"; float h = f.v == "band" ? .032f : .017f;
                f.Add(OnBody, role, M.Arc("a.torc." + K(ringR, h), ringR + .002f, ringR + .018f, 118, 422, h), ring);
                f.Add(OnBody, role == "wood" ? "wood" : "trim", M.Many("a.torc.ends." + K(ringR), Sphere, M.At(V(Mathf.Cos(118 * Mathf.Deg2Rad) * (ringR + .01f), 0, Mathf.Sin(118 * Mathf.Deg2Rad) * (ringR + .01f)), V0, S(.03f)), M.At(V(Mathf.Cos(62 * Mathf.Deg2Rad) * (ringR + .01f), 0, Mathf.Sin(62 * Mathf.Deg2Rad) * (ringR + .01f)), V0, S(.03f))), ring);
                if (f.v == "charm") f.Add(OnBody, "fur", M.Lump("a.torc.charm", 13, .4f), ringFront + V(0, -.05f, .012f), V(.03f, .065f, .03f));   // a hare's foot
                f.Glow(1, OnBody, Sphere, ringFront + V(0, -.004f, .006f), S(.024f));
                f.Glow(2, OnBody, M.Many("a.torc.gems." + K(ringR), Sphere, M.At(V(Mathf.Cos(118 * Mathf.Deg2Rad) * (ringR + .01f), .016f, Mathf.Sin(118 * Mathf.Deg2Rad) * (ringR + .01f)), V0, S(.016f)), M.At(V(Mathf.Cos(62 * Mathf.Deg2Rad) * (ringR + .01f), .016f, Mathf.Sin(62 * Mathf.Deg2Rad) * (ringR + .01f)), V0, S(.016f))), ring);
            }
            else
            {
                bool pendant = family == "neck.pendant"; string cord = pendant && f.tier >= 2 ? "metal" : "leather";
                f.Add(OnBody, cord, Ring(ringR, ringR + .008f, -.005f, .005f, 18), ring);
                if (pendant)
                {
                    var mid = shell ? V(0, .61f, front + .012f) : V(0, .56f, .15f);
                    f.Add(OnBody, cord, M.Rod("a.pendant.chain." + K(ringFront.y, ringFront.z, front), new[] { ringFront, mid, drop + V(0, .02f, -.004f) }, .0045f, .0045f, 5, 7), Matrix4x4.identity);
                    Pendant(f, drop);
                }
                else Cord(f, ring, ringR);
            }
            if (f.tier >= 3) f.Add(OnBody, "trim", M.Many("a.neck.drops." + K(ringR), Sphere, M.At(V(Mathf.Cos(65 * Mathf.Deg2Rad) * (ringR + .006f), -.008f, Mathf.Sin(65 * Mathf.Deg2Rad) * (ringR + .006f)), V0, S(.018f)), M.At(V(Mathf.Cos(115 * Mathf.Deg2Rad) * (ringR + .006f), -.008f, Mathf.Sin(115 * Mathf.Deg2Rad) * (ringR + .006f)), V0, S(.018f))), ring);
            f.Fancy(OnBody, M.Many("a.neck.studs." + K(ringR), Sphere, M.At(V(Mathf.Cos(30 * Mathf.Deg2Rad) * (ringR + .006f), 0, Mathf.Sin(30 * Mathf.Deg2Rad) * (ringR + .006f)), V0, S(.016f)), M.At(V(Mathf.Cos(150 * Mathf.Deg2Rad) * (ringR + .006f), 0, Mathf.Sin(150 * Mathf.Deg2Rad) * (ringR + .006f)), V0, S(.016f))), ring);
            f.Rust(OnBody, ringFront + V(.03f, -.004f, -.01f), V(.02f, .012f, .012f));
            Mark(f, OnBody, ring.MultiplyPoint3x4(V(Mathf.Cos(150 * Mathf.Deg2Rad) * (ringR + .01f), 0, Mathf.Sin(150 * Mathf.Deg2Rad) * (ringR + .01f))), V(0, -60, 0), .5f, true);
        }
        void Pendant(Fit f, Vector3 at)
        {
            string gem = f.l.accents >= 1 ? "glow" : "gem"; float big = 1 + .08f * f.tier;
            switch (f.v)
            {
                case "glass": f.Add("glass", M.Lump("a.pendant.shard", 17, .5f, true), at + V(0, -.02f, 0), V(.03f, .06f, .02f) * big); break;
                case "seal":
                    f.Add("cloth2", Cylinder, at + V(0, -.02f, 0), V(.05f, .006f, .05f) * big, V(90, 0, 0));
                    f.Add("trim", Ring(.024f, .029f, -.004f, .004f, 16), at + V(0, -.02f, .001f), One * big, V(90, 0, 0)); break;
                case "signet":
                    f.Add("trim", Ring(.015f, .022f, -.005f, .005f, 16), at + V(0, -.02f, 0), One * big, V(90, 0, 0));
                    f.Add("trim", Cube, at + V(0, -.002f, 0), V(.022f, .012f, .016f) * big); break;
                case "ember":
                    f.Add("metal", M.Many("a.pendant.cage", Cube, M.At(V(.014f, 0, .014f), V0, V(.004f, .05f, .004f)), M.At(V(-.014f, 0, .014f), V0, V(.004f, .05f, .004f)), M.At(V(.014f, 0, -.014f), V0, V(.004f, .05f, .004f)), M.At(V(-.014f, 0, -.014f), V0, V(.004f, .05f, .004f))), at + V(0, -.025f, 0), One * big);
                    f.Add("lit", Sphere, at + V(0, -.025f, 0), V(.024f, .032f, .024f) * big); break;
                case "leaf": f.Add("green", Leaf, at + V(0, -.07f * big, 0), One * big); break;
                case "vial":
                    f.Add("glass", M.Lathe("a.pendant.vial", Pts(0, -.04f, .016f, -.038f, .018f, -.01f, .008f, 0, .008f, .015f, 0, .016f), 12, true), at + V(0, -.02f, 0), One * big);
                    f.Add("wood", Cylinder, at + V(0, -.002f, 0), V(.012f, .006f, .012f) * big); break;
                case "jar":
                    f.Add("cloth2", M.Lathe("a.pendant.jar", Pts(0, -.035f, .022f, -.032f, .026f, -.015f, .02f, 0, .014f, .004f, 0, .006f), 12, true), at + V(0, -.02f, 0), One * big);
                    f.Add("trim", Cylinder, at + V(0, -.012f, 0), V(.03f, .004f, .03f) * big); break;
                default:   // a drop in a setting
                    f.Add("trim", M.Lathe("a.pendant.drop", Pts(0, -.05f, .02f, -.035f, .022f, -.015f, .012f, 0, 0, .006f), 12, true), at, One * big);
                    break;
            }
            f.Add(gem, Sphere, at + V(0, -.024f * big, .016f * big), V(.02f, .02f, .012f) * big);
            if (f.l.accents >= 2) f.Add("glow", Sphere, at + V(0, .008f, .008f), S(.014f));
        }
        void Cord(Fit f, Matrix4x4 ring, float r)
        {
            Vector3 On(float a) { return new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * (r + .004f), -.006f, Mathf.Sin(a * Mathf.Deg2Rad) * (r + .004f)); }
            string bead = f.tier >= 4 ? "trim" : f.tier >= 2 ? "bone" : "wood";
            switch (f.v)
            {
                case "knot": f.Add(OnBody, "leather", M.Lump("a.cord.knot", 23, .5f), ring * Fit.At(On(90), V0, V(.04f, .035f, .03f))); break;
                case "fang": case "tooth": case "tine":
                {
                    float len = f.v == "tine" ? .1f : f.v == "fang" ? .075f : .055f;
                    f.Add(OnBody, f.v == "tine" ? "antler" : "bone", M.Rod("a.cord." + f.v, new[] { V0, V(0, -len * .5f, .012f), V(.008f, -len, 0) }, f.v == "tine" ? .012f : .011f, .003f), Fit.At(ring.MultiplyPoint3x4(On(90)) + V(0, -.004f, .006f), V0, One));
                    break;
                }
                default: f.Add(OnBody, bead, M.Many("a.cord.beads." + K(r), Sphere, M.At(On(72), V0, S(.024f)), M.At(On(90) + V(0, -.004f, 0), V0, S(.028f)), M.At(On(108), V0, S(.024f))), ring); break;
            }
            f.Glow(1, OnBody, Sphere, ring.MultiplyPoint3x4(On(90)) + V(0, -.008f, .016f), S(.018f));
            f.Glow(2, OnBody, M.Many("a.cord.gems." + K(r), Sphere, M.At(On(55), V0, S(.016f)), M.At(On(125), V0, S(.016f))), ring);
        }

        // ---------- shoulders ----------
        void Shoulders(Fit f)
        {
            switch (f.l.family)
            {
                case "shoulder.mantle": Mantle(f); break;
                case "shoulder.pauldron": Pauldrons(f); break;
                case "shoulder.spaulder": Spaulders(f); break;
            }
        }
        /// <summary>
        /// A mantle: a capelet over both shoulders with a short drape down the back. Cloth: a hem band. Fur: rows of fur and a fur
        /// roll at the collar. Hide: a ragged hide with bone toggles. Frayed: tattered strips at the hem. Shawl: a shawl whose ends
        /// hang down the front. Higher bands add a collar band, a clasp, small shoulder plates and a second layer.
        /// </summary>
        void Mantle(Fit f)
        {
            string v = f.v; bool fur = v == "fur", hide = v == "hide";
            string main = fur ? "fur" : hide ? "hide" : "cloth";
            var sides = new[] { Rg(.42f, .47f, .26f, -.02f), Rg(.4f, .52f, .25f, -.02f), Rg(.33f, .6f, .22f, -.02f), Rg(.25f, .65f, .19f, -.02f) };
            var top = new[] { Rg(.15f, .665f, .145f, -.02f), Rg(.12f, .67f, .12f, -.02f) };
            var rings = fur ? Cat(M.Rows(sides, .032f, .012f, true, null, out _), top) : Cat(sides, top);
            f.Add(main, M.Shell("a.mantle." + (fur ? "fur" : "cloth"), rings, .016f, 3, null, 270, 28, !fur), V0, One);
            var drape = hide || v == "frayed"
                ? M.Panel("a.mantle.drape.rag", Pts(-.23f, 0, .23f, 0, .22f, -.24f, .17f, -.33f, .12f, -.28f, .06f, -.37f, 0, -.31f, -.07f, -.36f, -.13f, -.29f, -.19f, -.34f, -.22f, -.25f), .014f, .015f, false)
                : M.Panel("a.mantle.drape", Pts(-.23f, 0, .23f, 0, .21f, -.3f, .1f, -.34f, 0, -.35f, -.1f, -.34f, -.21f, -.3f), .014f, .015f, false);
            f.Add(main, drape, V(0, .5f, -.265f), One);
            if (v == "frayed")
            {
                var tatters = new List<Matrix4x4>();
                for (int i = 0; i < 14; i++) { float a = 10 + i * 25.7f, len = .05f + (i * 37 % 5) * .016f; tatters.Add(M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .405f, .47f - len / 2, Mathf.Sin(a * Mathf.Deg2Rad) * .25f - .02f), V(-10, 90 - a, 0), V(.045f, len, .01f))); }
                f.Add(OnBody, main, M.Many("a.mantle.tatters", Cube, tatters.ToArray()), Matrix4x4.identity);
            }
            if (hide) f.Add(OnBody, "bone", M.Many("a.mantle.toggles", Cylinder, M.At(V(-.05f, 0, 0), V(0, 0, 90), V(.016f, .025f, .016f)), M.At(V(.05f, -.04f, 0), V(0, 0, 90), V(.016f, .025f, .016f))), Fit.At(V(0, .6f, .2f), V0, One));
            if (v == "shawl")
                f.Both("cloth2", M.Panel("a.mantle.shawl.end", Pts(-.06f, 0, .06f, 0, .05f, -.3f, 0, -.32f, -.05f, -.3f), .01f, .004f, true), V(.09f, .5f, .234f), One, V(-8, 0, 10));
            // The collar: a band, or a thick fur roll.
            if (fur) f.Add("fur", M.Shell("a.mantle.furroll", new[] { Rg(.17f, .63f, .16f, -.02f), Rg(.155f, .67f, .145f, -.02f), Rg(.135f, .695f, .125f, -.02f) }, .035f, 2.4f), V0, One);
            else if (f.tier >= 1) f.Add("cloth2", Band(Rg(.13f, .655f, .13f, -.02f), .03f, .015f, 2.4f), V0, One);
            if (f.tier >= 2 || f.l.extra) f.Add("trim", Band(Rg(.426f, .465f, .266f, -.02f), .03f, .012f, 3), V0, One);   // an embroidered hem
            if (f.tier >= 2) f.Add("trim", Sphere, V(0, .64f, .19f), V(.04f, .04f, .022f));                                // the clasp
            if (f.tier >= 3) f.Both("trim", M.Lathe("a.pauldron.dome", Pts(.135f, -.008f, .152f, 0, .153f, .03f, .132f, .074f, .082f, .104f, 0, .115f), 18, true), V(.31f, .618f, 0), V(.55f, .5f, .6f), V(0, 0, -30));
            if (f.tier >= 4) f.Add("cloth2", M.Shell("a.mantle.upper", new[] { Rg(.36f, .575f, .232f, -.02f), Rg(.3f, .635f, .207f, -.02f), Rg(.24f, .683f, .182f, -.02f), Rg(.15f, .7f, .152f, -.02f), Rg(.13f, .705f, .132f, -.02f) }, .014f, 3, null, 270, 28), V0, One);
            f.Glow(1, OnBody, Sphere, V(0, .64f, .205f), V(.026f, .026f, .014f));
            f.Glow(2, OnBody, M.Many("a.mantle.gems", Sphere, M.At(V(.2f, .5f, .24f), V0, S(.024f)), M.At(V(-.2f, .5f, .24f), V0, S(.024f))), Matrix4x4.identity);
            f.Rust(OnBody, V(.08f, .32f, -.278f), V(.06f, .05f, .01f));
            Mark(f, OnBody, V(.16f, .52f, .228f), V(0, 25, 0), 1, true);
        }
        /// <summary>
        /// Pauldrons: a big dome over each shoulder, tipped outward, with a rim. Boiled leather on a strap in the first bands, plate
        /// from the third, a second lame and a ridge from the fourth, flutes and a spike in the last, growing a little each band.
        /// Bone: bone domes with horns.
        /// </summary>
        void Pauldrons(Fit f)
        {
            bool bone = f.v == "bone"; string main = bone ? "bone" : f.tier >= 2 ? "plate" : "leather2", rim = f.tier >= 2 ? "trim" : "dark";
            float s = 1 + .04f * f.tier;
            var frame = Matrix4x4.TRS(V(.31f, .575f, 0), Quaternion.Euler(0, 0, -22), V(1.12f, 1, 1.18f) * s);
            f.Both(main, M.Lathe("a.pauldron.dome", Pts(.135f, -.008f, .152f, 0, .153f, .03f, .132f, .074f, .082f, .104f, 0, .115f), 18, true), frame);
            f.Both(rim, M.Lathe("a.pauldron.rim", Pts(.146f, -.014f, .162f, -.014f, .162f, .012f, .146f, .012f, .146f, -.014f), 20), frame);
            if (f.tier <= 1 && !bone) f.Pair(OnArmR, "dark", Ring(.072f, .08f, -.15f, -.125f), V0, One);   // the strap round the arm
            if (f.tier >= 1)
            {
                var rivets = new Matrix4x4[6]; for (int i = 0; i < 6; i++) { float a = i * 60 + 30; rivets[i] = M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .142f, .036f, Mathf.Sin(a * Mathf.Deg2Rad) * .142f), V0, S(.018f)); }
                f.Both("dark", M.Many("a.pauldron.rivets", Sphere, rivets), frame);
            }
            if (f.tier >= 3 && !bone)
            {
                f.Both(main, M.Lathe("a.pauldron.lame", Pts(.15f, -.054f, .166f, -.056f, .166f, -.016f, .15f, -.016f, .15f, -.054f), 20), frame);
                f.Both("trim", M.Rod("a.pauldron.ridge", new[] { V(0, .045f, -.155f), V(0, .2f, 0), V(0, .045f, .155f) }, .011f, .011f), frame);
            }
            if (f.tier >= 4 && !bone)
            {
                f.Both("trim", M.Many("a.pauldron.flutes", M.Rod("a.pauldron.flute", new[] { V(0, .03f, -.14f), V(0, .17f, 0), V(0, .03f, .14f) }, .007f, .007f), M.At(V(.05f, 0, 0), V0, V(1, .93f, .95f)), M.At(V(-.05f, 0, 0), V0, V(1, .93f, .95f))), frame);
                f.Both(main, M.Cone(), frame * Fit.At(V(.02f, .1f, 0), V(0, 0, -15), V(.05f, .13f, .05f)));
            }
            if (bone)
            {
                f.Add(OnBody, "bone", M.Rod("a.pauldron.horn.r", new[] { V(-.02f, .09f, 0), V(.03f, .18f, 0), V(.1f, .21f, -.03f) }, .022f, .004f), frame);
                f.Add(OnBody, "bone", M.Rod("a.pauldron.horn.l", new[] { V(.02f, .09f, 0), V(-.03f, .18f, 0), V(-.1f, .21f, -.03f) }, .022f, .004f), Fit.Mirror(frame));
            }
            f.Fancy(OnBody, Ring(.162f, .168f, -.004f, .004f, 20), frame); f.Fancy(OnBody, Ring(.162f, .168f, -.004f, .004f, 20), Fit.Mirror(frame));   // a bead along each rim
            f.Glow(1, OnBody, Sphere, frame * Fit.At(V(0, .085f, .1f), V0, V(.035f, .035f, .02f)));
            f.Glow(1, OnBody, Sphere, Fit.Mirror(frame) * Fit.At(V(0, .085f, .1f), V0, V(.035f, .035f, .02f)));
            if (f.l.accents >= 2) f.Both("glow", Ring(.163f, .167f, -.003f, .003f, 20), frame);
            f.Rust(OnBody, frame.MultiplyPoint3x4(V(.06f, .08f, .05f)), V(.05f, .01f, .04f), V(0, 0, -45));
            Mark(f, OnBody, frame.MultiplyPoint3x4(V(.12f, .06f, 0)), V(0, 90, 0), .8f, true);
        }
        /// <summary>Spaulders: a smaller dome over each shoulder and lames stepping down the upper arm, open on the inside. Glass: glowing panes. Bark: bark plates with moss.</summary>
        void Spaulders(Fit f)
        {
            string v = f.v, main = v == "glass" ? "glass" : v == "bark" ? "bark" : f.tier >= 1 ? "plate" : "leather2";
            var frame = Matrix4x4.TRS(V(.3f, .58f, 0), Quaternion.Euler(0, 0, -15), Vector3.one * (.88f + .03f * f.tier));
            f.Both(main, M.Lathe("a.pauldron.dome", Pts(.135f, -.008f, .152f, 0, .153f, .03f, .132f, .074f, .082f, .104f, 0, .115f), 18, true), frame);
            f.Both(f.tier >= 2 ? "trim" : "dark", M.Lathe("a.pauldron.rim", Pts(.146f, -.014f, .162f, -.014f, .162f, .012f, .146f, .012f, .146f, -.014f), 20), frame);
            int lames = f.tier >= 2 ? 3 : 2;
            var lameR = M.Shell("a.spaulder.lame.r", new[] { Rg(.094f, -.075f, .094f), Rg(.087f, 0, .087f) }, .012f, 2, new[] { 150f }, 180, 16);
            var lameL = M.Shell("a.spaulder.lame.l", new[] { Rg(.094f, -.075f, .094f), Rg(.087f, 0, .087f) }, .012f, 2, new[] { 150f }, 0, 16);
            for (int i = 0; i < lames; i++)
            {
                float top = -.015f - i * .07f;
                f.Add(OnArmR, main, lameR, V(0, top, 0), One); f.Add(OnArmL, main, lameL, V(0, top, 0), One);
            }
            if (v == "bark") f.Both("green", M.Lump("a.spaulder.moss", 31, .5f), frame * Fit.At(V(-.02f, .09f, 0), V0, V(.12f, .04f, .1f)));
            if (f.tier >= 3 && v != "bark") f.Both("trim", M.Rod("a.pauldron.ridge", new[] { V(0, .045f, -.155f), V(0, .2f, 0), V(0, .045f, .155f) }, .011f, .011f), frame);
            if (f.tier >= 4 && v != "bark") f.Both(main, M.Cone(), frame * Fit.At(V(.02f, .1f, 0), V(0, 0, -15), V(.04f, .1f, .04f)));
            f.Fancy(OnBody, M.Many("a.spaulder.studs", Sphere, M.At(V(.07f, .07f, .07f), V0, S(.02f)), M.At(V(.07f, .07f, -.07f), V0, S(.02f))), frame);
            f.Fancy(OnBody, M.Many("a.spaulder.studs", Sphere, M.At(V(.07f, .07f, .07f), V0, S(.02f)), M.At(V(.07f, .07f, -.07f), V0, S(.02f))), Fit.Mirror(frame));
            f.Glow(1, OnBody, Sphere, frame * Fit.At(V(0, .085f, .1f), V0, V(.03f, .03f, .018f)));
            f.Glow(1, OnBody, Sphere, Fit.Mirror(frame) * Fit.At(V(0, .085f, .1f), V0, V(.03f, .03f, .018f)));
            if (f.l.accents >= 2) f.Both("glow", Sphere, frame * Fit.At(V(0, .115f, 0), V0, V(.03f, .022f, .03f)));
            f.Rust(OnBody, frame.MultiplyPoint3x4(V(.06f, .08f, .05f)), V(.05f, .01f, .04f), V(0, 0, -45));
            Mark(f, OnBody, frame.MultiplyPoint3x4(V(.12f, .06f, 0)), V(0, 90, 0), .7f, true);
        }

        // ---------- chest ----------
        /// <summary>The torso's sides from the waist to the top of the shoulders, round enough to hold the square torso and the chest. Grow adds all round; front pushes the chest forward (a breastplate).</summary>
        static Vector4[] Torso(float g = 0, float front = 0)
        {
            return new[] { Rg(.262f + g, -.06f, .162f + g, .004f), Rg(.262f + g, .1f, .162f + g, .006f),
                Rg(.266f + g, .3f, .172f + g + front * .15f, .012f + front * .15f), Rg(.27f + g, .44f, .185f + g + front * .5f, .016f + front * .5f),
                Rg(.266f + g, .56f, .172f + g + front * .2f, .012f + front * .2f), Rg(.262f + g, .63f, .166f + g, .004f) };
        }
        /// <summary>The top ring, drawn in round the neck: the shell's yoke.</summary>
        static Vector4 Yoke(float g = 0) { return Rg(.105f + g * .5f, .68f, .098f + g * .5f, -.005f); }
        void Chest(Fit f)
        {
            if (baseBelt != null) baseBelt.enabled = false;   // every chest piece brings its own belt or covers the waist
            if (built == ActorLook.Druid) HangCloak(true);
            switch (f.l.family)
            {
                case "chest.tunic": Tunic(f); break;
                case "chest.jerkin": Jerkin(f); break;
                case "chest.hauberk": Hauberk(f); break;
                case "chest.coat": Coat(f); break;
                case "chest.cuirass": Cuirass(f); break;
                case "chest.robe": Robe(f); break;
            }
        }
        void Belt(Fit f, string role, float g, float y = .025f)
        {
            f.Add(role, Band(Rg(.272f + g, y, .17f + g, .005f), .075f, .02f), V0, One);
            f.Add(f.tier >= 1 ? "trim" : "dark", Cube, V(0, y + .037f, .177f + g), V(.056f, .05f, .012f));   // the buckle
        }
        /// <summary>A tunic: the torso and sleeves in cloth, a short skirt with a hem band, a belt and a collar. Cuffs from the third band, a tabard front and back from the fourth.</summary>
        void Tunic(Fit f)
        {
            Cover(baseChest, RoleMat(f, "cloth")); Cover(baseSleeves, RoleMat(f, "cloth"));
            f.Add("cloth", M.Shell("a.tunic.skirt", new[] { Rg(.29f, -.24f, .2f, 0), Rg(.272f, -.1f, .178f, .002f), Rg(.262f, .04f, .162f, .004f), Rg(.262f, .09f, .162f, .006f) }, .014f, 5), V0, One);
            f.Add("cloth2", Band(Rg(.293f, -.245f, .203f, 0), .035f, .012f, 5), V0, One);
            f.Add("cloth2", Ring(.078f, .104f, .598f, .632f, 18), V0, One);   // the collar
            Belt(f, "leather", 0);
            if (f.tier >= 2) f.Add(OnBody, "cloth2", M.Many("a.tunic.sash", Cube, M.At(V(-.2f, -.05f, .13f), V(0, -40, 6), V(.06f, .26f, .014f)), M.At(V(-.17f, -.03f, .15f), V(0, -30, -8), V(.055f, .2f, .014f)), M.At(V(-.2f, .07f, .14f), V(0, -40, 0), V(.07f, .06f, .05f))), Matrix4x4.identity);   // a sash knotted at the hip
            if (f.tier >= 3)
            {
                var front = M.Panel("a.tunic.tabard", Pts(-.13f, 0, .13f, 0, .13f, -.62f, 0, -.68f, -.13f, -.62f), .012f, .012f, true);
                f.Add("cloth2", front, V(0, .52f, .19f), One, V(-4, 0, 0));
                f.Add("cloth2", M.Panel("a.tunic.tabard.back", Pts(-.13f, 0, .13f, 0, .13f, -.62f, 0, -.68f, -.13f, -.62f), .012f, .012f, false), V(0, .52f, -.17f), One, V(4, 0, 0));
            }
            if (f.tier >= 4) f.Add("trim", M.Many("a.tunic.tabard.edge", Cube, M.At(V(.13f, -.31f, .008f), V0, V(.012f, .62f, .006f)), M.At(V(-.13f, -.31f, .008f), V0, V(.012f, .62f, .006f))), V(0, .52f, .19f), One, V(-4, 0, 0));
            f.Fancy(OnBody, M.Many("a.belt.studs", Sphere, M.At(V(.12f, 0, 0), V0, S(.018f)), M.At(V(-.12f, 0, 0), V0, S(.018f)), M.At(V(.2f, 0, -.05f), V0, S(.018f)), M.At(V(-.2f, 0, -.05f), V0, S(.018f))), V(0, .062f, .168f), One);
            f.Glow(1, OnBody, Sphere, V(0, .062f, .186f), V(.026f, .026f, .012f));
            f.Glow(2, OnBody, Sphere, V(0, .615f, .106f), V(.024f, .024f, .014f));
            f.Rust(OnBody, V(.15f, -.12f, .183f), V(.06f, .06f, .008f), V(0, 25, 0));
            Mark(f, OnBody, V(-.2f, .062f, .135f), V(0, -45, 0));
        }
        /// <summary>
        /// A jerkin: a sleeveless shell from the hip to the shoulders, the shirt's sleeves showing. Leather and hide: laced up the
        /// front; hide has a fur collar. Scale: rows of overlapping metal scales with leather edging. Riveted ones are studded all
        /// over (brigandine). Shoulder straps from the third band, side panels from the fourth, trimmed edges in the last.
        /// </summary>
        void Jerkin(Fit f)
        {
            string v = f.v; bool scale = v == "scale", hide = v == "hide"; string main = scale ? "scale" : hide ? "hide" : "leather";
            var sides = Cat(new[] { Rg(.276f, -.13f, .178f, 0) }, Torso());
            var rings = scale ? Cat(M.Rows(sides, .045f, .011f, true, null, out _), new[] { Yoke() }) : Cat(sides, new[] { Yoke() });
            f.Add(main, M.Shell(scale ? "a.jerkin.scale" : "a.jerkin", rings, .02f, 8, null, 270, 28, !scale), V0, One);
            float Front(float y) { return y < .1f ? .168f : y < .3f ? Mathf.Lerp(.168f, .184f, (y - .1f) / .2f) : y < .44f ? Mathf.Lerp(.184f, .201f, (y - .3f) / .14f) : Mathf.Lerp(.201f, .184f, (y - .44f) / .12f); }
            float Back(float y) { return y < .1f ? -.157f : y < .3f ? Mathf.Lerp(-.156f, -.16f, (y - .1f) / .2f) : y < .44f ? Mathf.Lerp(-.16f, -.169f, (y - .3f) / .14f) : Mathf.Lerp(-.169f, -.16f, (y - .44f) / .12f); }
            if (!scale)
            {
                var edges = new List<(Mesh, Matrix4x4)>();
                foreach (float x in new[] { -.028f, .028f })
                    edges.Add((M.Rod("a.jerkin.edge" + (x < 0 ? "l" : "r"), new[] { V(x, -.12f, .18f), V(x, .1f, Front(.1f) + .012f), V(x, .3f, Front(.3f) + .012f), V(x, .44f, Front(.44f) + .012f), V(x, .56f, Front(.56f) + .006f) }, .007f, .007f, 6, 14), Matrix4x4.identity));
                for (int i = 0; i < 5; i++)
                {
                    float y = .0f + i * .1f, z = Front(y) + .008f;
                    edges.Add((Cube, M.At(V(0, y, z), V(0, 0, 35), V(.07f, .007f, .006f)))); edges.Add((Cube, M.At(V(0, y, z), V(0, 0, -35), V(.07f, .007f, .006f))));
                }
                f.Add(OnBody, "dark", M.Join("a.jerkin.lacing", edges), Matrix4x4.identity);
            }
            else f.Add(OnBody, "leather", M.Join("a.jerkin.edging", new[] { (Band(Rg(.282f, -.135f, .184f, 0), .03f, .012f, 8), Matrix4x4.identity), (Band(Rg(.13f, .66f, .12f, -.005f), .025f, .014f, 4), Matrix4x4.identity) }), Matrix4x4.identity);
            if (hide) f.Add("fur", M.Shell("a.jerkin.furcollar", new[] { Rg(.16f, .64f, .15f, -.005f), Rg(.14f, .685f, .13f, -.005f) }, .035f, 3), V0, One);
            if (f.l.detail == "rivets")   // brigandine: studded front and back
            {
                var studs = new List<Matrix4x4>();
                for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++)
                {
                    float y = .05f + r * .1f, x = -.16f + c * .08f; if (!scale && Mathf.Abs(x) < .05f) continue;
                    studs.Add(M.At(V(x, y, Front(y) + .002f), V0, V(.02f, .02f, .012f))); studs.Add(M.At(V(x, y, Back(y) - .002f), V0, V(.02f, .02f, .012f)));
                }
                f.Add(OnBody, "dark", M.Many("a.jerkin.studs" + (scale ? ".s" : ""), Sphere, studs.ToArray()), Matrix4x4.identity);
            }
            Belt(f, scale ? "leather" : main == "leather" ? "leather" : "dark", .012f, .02f);
            if (f.tier >= 2) f.Both(scale ? "leather" : main, Cube, V(.2f, .652f, 0), V(.1f, .02f, .22f), V(0, 0, -18));   // shoulder straps
            if (f.tier >= 3) f.Both("cloth2", Cube, V(.272f, .25f, 0), V(.01f, .5f, .08f));                                   // side panels
            if (f.tier >= 4 && !scale) f.Add(OnBody, "trim", M.Many("a.jerkin.trimedge", Cube, M.At(V(.045f, .2f, Front(.2f) + .004f), V0, V(.012f, .5f, .006f)), M.At(V(-.045f, .2f, Front(.2f) + .004f), V0, V(.012f, .5f, .006f))), Matrix4x4.identity);
            f.Fancy(OnBody, M.Many("a.belt.studs", Sphere, M.At(V(.12f, 0, 0), V0, S(.018f)), M.At(V(-.12f, 0, 0), V0, S(.018f)), M.At(V(.2f, 0, -.05f), V0, S(.018f)), M.At(V(-.2f, 0, -.05f), V0, S(.018f))), V(0, .057f, .18f), One);
            f.Glow(1, OnBody, Sphere, V(0, .057f, .2f), V(.026f, .026f, .012f));
            f.Glow(2, OnBody, Sphere, V(0, .62f, .15f), V(.026f, .026f, .014f));
            f.Rust(OnBody, V(-.14f, .25f, .182f), V(.06f, .05f, .008f), V(0, -15, 0));
            Mark(f, OnBody, V(.18f, .4f, .178f), V(0, 20, 0));
        }
        /// <summary>
        /// A hauberk: a mail shirt to mid-thigh, split at the front of the skirt, short mail sleeves over padded ones and a belt. From
        /// the third band a sleeveless surcoat over it (narrow, so the mail shows at the sides), edged from the fourth, with a badge
        /// in the last.
        /// </summary>
        void Hauberk(Fit f)
        {
            Cover(baseChest, RoleMat(f, "mail")); Cover(baseSleeves, RoleMat(f, "leather"));
            var st = Cat(new[] { Rg(.31f, -.3f, .225f, 0), Rg(.29f, -.18f, .2f, .002f) }, Torso(.004f));
            var rows = M.Rows(st, .024f, .005f, false, new[] { 30f, 14, 0 }, out var rowGaps);
            f.Add("mail", M.Shell("a.hauberk", Cat(rows, new[] { Yoke(.004f) }), .016f, 7, Cat(rowGaps, new[] { 0f }), 90, 28), V0, One);
            var sleeve = new List<Vector2> { new Vector2(.074f, -.3f), new Vector2(.098f, -.3f) };
            for (float y = -.288f; y < -.01f; y += .012f) sleeve.Add(new Vector2(sleeve.Count % 2 == 0 ? .089f : .093f, y));
            sleeve.Add(new Vector2(.088f, 0)); sleeve.Add(new Vector2(.07f, 0));
            f.Pair(OnArmR, "mail", M.Lathe("a.hauberk.sleeve", sleeve.ToArray(), 16, true), V0, One);
            Belt(f, "leather", .014f, .02f);
            if (f.tier >= 2)
            {
                f.Add("cloth", M.Panel("a.hauberk.surcoat", Pts(-.12f, 0, .12f, 0, .13f, -.7f, -.13f, -.7f), .01f, .012f, true), V(0, .56f, .214f), One, V(-5, 0, 0));
                f.Add("cloth", M.Panel("a.hauberk.surcoat.back", Pts(-.12f, 0, .12f, 0, .13f, -.7f, -.13f, -.7f), .01f, .012f, false), V(0, .56f, -.198f), One, V(5, 0, 0));
            }
            if (f.tier >= 3) f.Add("trim", M.Many("a.hauberk.surcoat.edge", Cube, M.At(V(.124f, -.35f, .009f), V(0, 0, -.8f), V(.012f, .7f, .006f)), M.At(V(-.124f, -.35f, .009f), V(0, 0, .8f), V(.012f, .7f, .006f)), M.At(V(0, -.7f, .009f), V0, V(.26f, .012f, .006f))), V(0, .56f, .214f), One, V(-5, 0, 0));
            if (f.tier >= 4) f.Add("trim", M.Panel("a.hauberk.badge", Pts(0, .05f, .045f, 0, 0, -.05f, -.045f, 0), .006f, .004f, true), V(0, .38f, .232f), One, V(-5, 0, 0));
            f.Fancy(OnBody, M.Many("a.belt.studs", Sphere, M.At(V(.12f, 0, 0), V0, S(.018f)), M.At(V(-.12f, 0, 0), V0, S(.018f)), M.At(V(.2f, 0, -.05f), V0, S(.018f)), M.At(V(-.2f, 0, -.05f), V0, S(.018f))), V(0, .057f, .184f), One);
            f.Glow(1, OnBody, Sphere, V(0, .057f, .203f), V(.026f, .026f, .012f));
            f.Glow(2, OnBody, Sphere, f.tier >= 4 ? V(0, .38f, .238f) : V(0, .64f, .14f), V(.026f, .026f, .014f));
            f.Rust(OnBody, V(.16f, .2f, .186f), V(.07f, .06f, .008f), V(0, 25, 0));
            Mark(f, OnBody, V(-.21f, .057f, .14f), V(0, -45, 0));
        }
        /// <summary>
        /// A coat: the torso and sleeves in the coat's cloth, skirts to the knee round the hips (open at the front for the legs), a
        /// belt. Grey: the Concord's white tabard and gold badge over it. Skirted: Caddock's long dark coat, mail where it hangs
        /// open, a high collar and the torn sash across it.
        /// </summary>
        void Coat(Fit f)
        {
            bool skirted = f.v == "skirted"; string main = skirted ? "leather" : "cloth";
            Cover(baseChest, RoleMat(f, main)); Cover(baseSleeves, RoleMat(f, main));
            f.Add(main, M.Shell("a.coat", Cat(Torso(.002f), new[] { Yoke(.002f) }), .016f, 8), V0, One);
            var skirt = M.Panel("a.coat.skirt", Pts(-.1f, 0, .1f, 0, .12f, -.5f, -.12f, -.5f), .014f, .01f, true);
            var narrow = M.Panel("a.coat.skirt.narrow", Pts(-.05f, 0, .05f, 0, .06f, -.5f, -.06f, -.5f), .014f, .008f, true);
            foreach (var (a, mesh) in new[] { (45f, narrow), (135f, narrow), (0f, skirt), (180f, skirt), (250f, skirt), (290f, skirt) })
                f.Add(main, mesh, V(Mathf.Cos(a * Mathf.Deg2Rad) * .279f, .02f, Mathf.Sin(a * Mathf.Deg2Rad) * .19f + .004f), One, V(-8, 90 - a, 0));
            Belt(f, "leather", .014f, .02f);
            if (!skirted)
            {
                f.Add("cloth2", M.Panel("a.coat.tabard", Pts(-.17f, 0, .17f, 0, .17f, -.72f, -.17f, -.72f), .012f, .012f, true), V(0, .55f, .216f), One, V(-3, 0, 0));
                f.Add("cloth2", M.Panel("a.coat.tabard.back", Pts(-.17f, 0, .17f, 0, .17f, -.72f, -.17f, -.72f), .012f, .012f, false), V(0, .55f, -.2f), One, V(3, 0, 0));
                f.Add("trim", Cube, V(0, .33f, .238f), V(.07f, .07f, .01f), V(0, 0, 45));   // the badge
            }
            else
            {
                f.Add("mail", M.Panel("a.coat.mail", Pts(-.08f, 0, .08f, 0, .08f, -.5f, -.08f, -.5f), .012f, .01f, true), V(0, .56f, .205f), One, V(-2, 0, 0));
                f.Add(main, Cube, V(0, .66f, -.17f), V(.34f, .17f, .045f), V(-14, 0, 0));   // the high collar
                f.Add("cloth2", Cube, V(-.06f, .27f, .222f), V(.1f, .56f, .02f), V(4, 0, -35));   // the sash
            }
            f.Fancy(OnBody, M.Many("a.coat.buttons", Sphere, M.At(V(.1f, .45f, 0), V0, S(.022f)), M.At(V(.1f, .3f, 0), V0, S(.022f)), M.At(V(-.1f, .45f, 0), V0, S(.022f)), M.At(V(-.1f, .3f, 0), V0, S(.022f))), V(0, 0, .206f), One);
            f.Glow(1, OnBody, Sphere, skirted ? V(0, .057f, .2f) : V(0, .33f, .245f), V(.026f, .026f, .012f));
            f.Glow(2, OnBody, Sphere, V(0, .64f, .15f), V(.026f, .026f, .014f));
            f.Rust(OnBody, V(.25f, -.25f, .12f), V(.05f, .08f, .008f), V(0, 60, 0));
            Mark(f, OnBody, V(-.21f, .057f, .14f), V(0, -45, 0));
        }
        /// <summary>
        /// A cuirass: a polished breastplate and backplate with a ridge down the front, two fauld lames over the hips and a gorget;
        /// the arming coat shows at the shoulders and sleeves. Edged from the fourth band, a plackart over the belly in the last.
        /// Bark: plates of bark with moss and a vine across.
        /// </summary>
        void Cuirass(Fit f)
        {
            bool bark = f.v == "bark"; string main = bark ? "bark" : "plate";
            Cover(baseChest, RoleMat(f, "cloth2")); Cover(baseSleeves, RoleMat(f, "cloth2"));
            f.Add(main, M.Shell("a.cuirass", Cat(Torso(.003f, .02f), new[] { Yoke(.003f) }), .022f, 6), V0, One);
            f.Add(main, M.Shell("a.cuirass.fauld1", new[] { Rg(.276f, -.13f, .18f, .004f), Rg(.27f, -.05f, .172f, .006f) }, .015f, 6), V0, One);
            f.Add(main, M.Shell("a.cuirass.fauld2", new[] { Rg(.29f, -.21f, .196f, .002f), Rg(.282f, -.13f, .186f, .004f) }, .015f, 6), V0, One);
            f.Add(main, M.Lathe("a.cuirass.gorget", Pts(.108f, .632f, .13f, .632f, .126f, .672f, .11f, .676f, .108f, .632f), 20), V0, One);
            if (!bark) f.Add("trim", M.Rod("a.cuirass.ridge", new[] { V(0, -.05f, .176f), V(0, .2f, .196f), V(0, .44f, .25f), V(0, .62f, .18f) }, .008f, .007f, 6, 14), V0, One);
            else
            {
                f.Add("green", M.Many("a.cuirass.moss", M.Lump("a.cuirass.moss.lump", 41, .5f), M.At(V(.16f, .58f, .1f), V0, V(.12f, .05f, .1f)), M.At(V(-.12f, .3f, .2f), V0, V(.1f, .08f, .04f)), M.At(V(.1f, .05f, .175f), V0, V(.09f, .05f, .03f))), V0, One);
                f.Add("green", M.Rod("a.cuirass.vine", new[] { V(-.24f, .1f, .14f), V(-.05f, .3f, .23f), V(.15f, .45f, .22f), V(.24f, .6f, .1f) }, .01f, .008f, 6, 14), V0, One);
            }
            f.Both("dark", Cube, V(.272f, .3f, 0), V(.012f, .06f, .05f));   // the side buckles
            if (f.tier >= 3) f.Add("trim", Band(Rg(.268f, -.068f, .17f, .004f), .016f, .012f), V0, One);
            if (f.tier >= 4) f.Add("trim", M.Panel("a.cuirass.plackart", Pts(-.15f, 0, .15f, 0, .1f, -.16f, 0, -.2f, -.1f, -.16f), .012f, .01f, true), V(0, .28f, .198f), One, V(6, 0, 0));
            f.Fancy(OnBody, M.Many("a.cuirass.rivets", Sphere, M.At(V(.2f, .6f, .1f), V0, S(.018f)), M.At(V(-.2f, .6f, .1f), V0, S(.018f)), M.At(V(.24f, .0f, .13f), V0, S(.018f)), M.At(V(-.24f, .0f, .13f), V0, S(.018f))), V0, One);
            f.Glow(1, OnBody, Sphere, V(0, .44f, .232f), V(.032f, .032f, .016f));
            f.Glow(2, OnBody, Sphere, V(0, .655f, .13f), V(.026f, .026f, .014f));
            f.Rust(OnBody, V(.14f, .32f, .2f), V(.07f, .06f, .008f), V(0, 25, 0));
            Mark(f, OnBody, V(-.17f, .4f, .2f), V(0, -25, 0));
        }
        /// <summary>
        /// A robe: the torso and sleeves in cloth and a skirt to the ankle, a hem band and a cord belt. Vestment: a stole down the
        /// front. Cassock: a row of buttons from collar to hem. Shroud: a tattered hem. Bell sleeves from the third band, an
        /// embroidered hem from the fourth, a badge on the chest in the last.
        /// </summary>
        void Robe(Fit f)
        {
            Cover(baseChest, RoleMat(f, "cloth")); Cover(baseSleeves, RoleMat(f, "cloth"));
            var skirt = new[] { Rg(.31f, -.93f, .235f, 0), Rg(.29f, -.55f, .205f, 0), Rg(.272f, -.2f, .178f, .002f), Rg(.262f, -.04f, .162f, .004f), Rg(.262f, .09f, .162f, .006f) };
            f.Add("cloth", M.Shell("a.robe", skirt, .016f, 4), V0, One);
            f.Add(f.tier >= 3 ? "trim" : "cloth2", Band(Rg(.314f, -.935f, .239f, 0), .045f, .012f, 4), V0, One);
            f.Add("cloth2", Band(Rg(.27f, .06f, .17f, .006f), .036f, .014f), V0, One);   // the cord belt, over the skirt's top edge
            f.Add("cloth2", M.Many("a.robe.cord.ends", Cube, M.At(V(.04f, -.12f, 0), V(0, 0, 4), V(.016f, .26f, .016f)), M.At(V(.08f, -.1f, 0), V(0, 0, -3), V(.016f, .22f, .016f))), V(0, .07f, .186f), One, V(-4, 0, 0));
            float Front(float y) { return y > .35f ? .02f + .155f * Mathf.Sqrt(Mathf.Max(0, 1 - (y - .44f) * (y - .44f) / .0289f)) : y > .09f ? .145f : y > -.2f ? Mathf.Lerp(.18f, .166f, (y + .2f) / .29f) : y > -.55f ? Mathf.Lerp(.205f, .18f, (y + .55f) / .35f) : Mathf.Lerp(.235f, .205f, (y + .93f) / .38f); }
            switch (f.v)
            {
                case "cassock":
                {
                    var buttons = new List<Matrix4x4>(); for (float y = .55f; y > -.88f; y -= .1f) buttons.Add(M.At(V(0, y, Mathf.Max(Front(y), .145f) + .006f), V0, V(.022f, .022f, .014f)));
                    f.Add(OnBody, "trim", M.Many("a.robe.buttons", Sphere, buttons.ToArray()), Matrix4x4.identity);
                    break;
                }
                case "shroud":
                {
                    var tatters = new List<Matrix4x4>();
                    for (int i = 0; i < 16; i++) { float a = i * 22.5f + 8, len = .06f + (i * 7 % 4) * .025f; tatters.Add(M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .31f, -.93f - len / 2 + .01f, Mathf.Sin(a * Mathf.Deg2Rad) * .235f), V(-6, 90 - a, 0), V(.07f, len, .01f))); }
                    f.Add(OnBody, "cloth", M.Many("a.robe.tatters", Cube, tatters.ToArray()), Matrix4x4.identity);
                    break;
                }
                default:   // vestment: the stole over the neck and down the front
                    f.Both("cloth2", Cube, V(.07f, .545f, .1375f), V(.07f, .215f, .012f), V(-26.6f, 0, 0));
                    f.Both("cloth2", Cube, V(.07f, -.025f, .2f), V(.07f, .95f, .012f), V(-1.8f, 0, 0));
                    if (f.tier >= 1) f.Both("trim", Cube, V(.07f, -.49f, .217f), V(.074f, .03f, .014f), V(-1.8f, 0, 0));
                    break;
            }
            if (f.tier >= 2) f.Pair(OnArmR, "cloth", M.Lathe("a.robe.sleeve", Pts(.07f, -.4f, .078f, -.4f, .125f, -.585f, .11f, -.59f), 16), V0, One);   // bell sleeves
            if (f.tier >= 4) f.Add("trim", M.Panel("a.robe.badge", Pts(0, .05f, .045f, 0, 0, -.05f, -.045f, 0), .006f, .004f, true), V(.11f, .44f, .17f), One, V(0, 25, 0));
            f.Fancy(OnBody, M.Many("a.robe.knots", Sphere, M.At(V(.04f, -.25f, 0), V0, S(.024f)), M.At(V(.08f, -.21f, 0), V0, S(.024f))), V(0, .07f, .186f), One, V(-4, 0, 0));
            f.Glow(1, OnBody, Sphere, V(0, .078f, .183f), V(.026f, .026f, .014f));
            f.Glow(2, OnBody, Sphere, V(0, .615f, .106f), V(.024f, .024f, .014f));
            f.Rust(OnBody, V(-.17f, -.6f, .16f), V(.06f, .08f, .008f), V(0, -40, 0));
            Mark(f, OnBody, V(.2f, .078f, .13f), V(0, 45, 0));
        }

        // ---------- hands ----------
        void Hands(Fit f)
        {
            switch (f.l.family)
            {
                case "hands.gloves": Gloves(f); break;
                case "hands.wraps": Wraps(f); break;
                case "hands.gauntlets": Gauntlets(f); break;
            }
        }
        /// <summary>Gloves over each hand with a flared cuff. Fingerless: a palm band, the fingers bare. Mitts: big fur-lined mittens. Stitching, a wrist strap and knuckle studs come with the bands.</summary>
        void Gloves(Fit f)
        {
            string v = f.v, main = "leather";
            if (v == "fingerless") f.Pair(OnArmR, main, M.Lathe("a.glove.palm", Pts(.058f, -.66f, .068f, -.655f, .07f, -.6f, .06f, -.594f), 14), V0, One);
            else f.Pair(OnArmR, main, Sphere, V(0, -.625f, .004f), v == "mitts" ? V(.152f, .17f, .152f) : V(.136f, .148f, .136f));
            if (v == "mitts") f.Pair(OnArmR, "fur", M.Lathe("a.glove.furcuff", Pts(.07f, -.575f, .094f, -.57f, .104f, -.5f, .09f, -.47f, .07f, -.47f), 14, true), V0, One);
            else f.Pair(OnArmR, main, M.Lathe("a.glove.cuff", Pts(.07f, -.575f, .083f, -.575f, .092f, -.48f, .072f, -.47f), 14), V0, One);
            if (f.tier >= 1 && v != "mitts") f.Pair(OnArmR, "trim", Ring(.086f, .097f, -.485f, -.468f, 14), V0, One);
            if (f.tier >= 1 && v == "plain") f.Pair(OnArmR, "trim", Cube, V(.066f, -.625f, 0), V(.008f, .05f, .03f));   // a stitched panel on the back of the hand
            if (f.tier >= 2) { f.Pair(OnArmR, main, Ring(.074f, .08f, -.58f, -.565f, 14), V0, One); f.Pair(OnArmR, "trim", Cube, V(.08f, -.572f, 0), V(.008f, .016f, .022f)); }
            if (f.tier >= 3 && v != "mitts") f.Pair(OnArmR, "trim", M.Many("a.glove.studs", Sphere, M.At(V(0, 0, -.02f), V0, S(.014f)), M.At(V0, V0, S(.014f)), M.At(V(0, 0, .02f), V0, S(.014f))), V(.066f, -.65f, 0), One);
            f.FancyPair(OnArmR, Cube, V(0, -.52f, .089f), V(.03f, .03f, .008f));
            f.GlowPair(1, OnArmR, Sphere, V(.093f, -.505f, 0), V(.012f, .026f, .026f));
            f.GlowPair(2, OnArmR, Sphere, V(.068f, -.625f, .02f), V(.012f, .022f, .022f));
            f.RustPair(OnArmR, V(-.03f, -.63f, .06f), V(.03f, .03f, .008f), V(0, -25, 0));
            Mark(f, OnArmR, V(.088f, -.53f, 0), V(0, 90, 0), .6f, true);
        }
        /// <summary>Wraps: three bands wound up the forearm and one round the palm, the fingers bare. Fur: thick fur bands. Silk: shining bands with trailing ends.</summary>
        void Wraps(Fit f)
        {
            string v = f.v, main = v == "fur" ? "fur" : v == "silk" ? "silk" : "cloth";
            float r0 = v == "fur" ? .072f : .07f, r1 = v == "fur" ? .095f : .081f;
            var band = Ring(r0, r1, -.014f, .014f, 14);
            foreach (var (y, tilt) in new[] { (-.37f, 8f), (-.45f, -7f), (-.53f, 9f) }) f.Pair(OnArmR, main, band, V(0, y, 0), One, V(tilt, 0, 0));
            f.Pair(OnArmR, main, Ring(.06f, .069f, -.016f, .016f, 14), V(0, -.615f, 0), One, V(0, 0, 12));
            if (v == "silk") f.Pair(OnArmR, main, Cube, V(.08f, -.6f, 0), V(.006f, .12f, .025f), V(0, 0, -10));
            if (f.tier >= 2) f.Pair(OnArmR, main, Ring(.05f, .058f, -.012f, .012f, 14), V(0, -.66f, 0), One, V(0, 0, -8));   // round the knuckles
            if (f.tier >= 3) f.Pair(OnArmR, "trim", M.Many("a.wrap.studs", Sphere, M.At(V(r1, 0, .02f), V0, S(.014f)), M.At(V(r1, 0, -.02f), V0, S(.014f))), V(0, -.45f, 0), One);
            if (f.tier >= 4) f.Pair(OnArmR, "trim", Ring(r1 - .004f, r1 + .002f, -.004f, .004f, 14), V(0, -.53f, 0), One, V(9, 0, 0));
            f.FancyPair(OnArmR, Ring(r1, r1 + .005f, -.003f, .003f, 14), V(0, -.37f, 0), One, V(8, 0, 0));
            f.GlowPair(1, OnArmR, Sphere, V(r1 + .004f, -.45f, 0), V(.012f, .024f, .024f));
            f.GlowPair(2, OnArmR, Sphere, V(.066f, -.615f, 0), V(.012f, .02f, .02f));
            f.RustPair(OnArmR, V(0, -.53f, r1 - .002f), V(.03f, .02f, .008f));
            Mark(f, OnArmR, V(r1 + .002f, -.41f, 0), V(0, 90, 0), .6f, true);
        }
        /// <summary>
        /// Gauntlets: a glove with a cuff flaring toward the elbow, a knuckle plate and finger lames. Boiled leather in the first two
        /// bands, plate after; the cuff flares wider each band, its edge is trimmed from the fourth, spikes on the knuckles in the
        /// last. Bone, glass and root variants change the plates.
        /// </summary>
        void Gauntlets(Fit f)
        {
            string v = f.v, main = v == "bone" ? "bone" : v == "glass" ? "glass" : v == "root" ? "wood" : f.tier >= 2 ? "plate" : "leather2";
            float rTop = .108f + .008f * f.tier;
            f.Pair(OnArmR, "leather", Sphere, V(0, -.625f, .004f), V(.136f, .148f, .136f));
            if (v != "root")
            {
                f.Pair(OnArmR, main, M.Lathe("a.gauntlet.cuff." + f.tier, Pts(.074f, -.575f, .088f, -.575f, rTop, -.395f, .074f, -.395f), 14), V0, One);
                f.Pair(OnArmR, main, Sphere, V(.05f, -.632f, 0), V(.05f, .1f, .125f));
                f.Pair(OnArmR, main, M.Many("a.gauntlet.fingers", Cube, M.At(V(0, 0, 0), V0, V(.03f, .012f, .1f)), M.At(V(-.006f, -.02f, 0), V0, V(.03f, .012f, .095f)), M.At(V(-.014f, -.038f, 0), V0, V(.026f, .012f, .085f))), V(.045f, -.665f, 0), One);
            }
            else   // roots wound round the forearm, buds lit along them
            {
                var helix = new Vector3[6]; for (int i = 0; i < 6; i++) { float a = i * 75 * Mathf.Deg2Rad; helix[i] = V(Mathf.Cos(a) * .1f, -.4f - i * .035f, Mathf.Sin(a) * .1f); }
                var mirrored = System.Array.ConvertAll(helix, p => V(-p.x, p.y, p.z));
                f.Add(OnArmR, "wood", M.Rod("a.gauntlet.root.r", helix, .013f, .009f, 6, 16), Matrix4x4.identity);
                f.Add(OnArmL, "wood", M.Rod("a.gauntlet.root.l", mirrored, .013f, .009f, 6, 16), Matrix4x4.identity);
                f.Pair(OnArmR, "lit", Sphere, V(.084f, -.44f, .03f), S(.02f)); f.Pair(OnArmR, "lit", Sphere, V(-.03f, -.5f, .08f), S(.02f));
            }
            if (v == "bone") f.Pair(OnArmR, "bone", M.Many("a.gauntlet.claws", M.Cone(6), M.At(V(0, 0, -.025f), V(180, 0, 0), V(.016f, .035f, .016f)), M.At(V0, V(180, 0, 0), V(.016f, .04f, .016f)), M.At(V(0, 0, .025f), V(180, 0, 0), V(.016f, .035f, .016f))), V(.03f, -.69f, 0), One);
            if (f.tier >= 3 && v != "root") f.Pair(OnArmR, "trim", Ring(rTop - .004f, rTop + .006f, -.4f, -.385f, 14), V0, One);
            if (f.tier >= 4 && v == "plate") f.Pair(OnArmR, main, M.Many("a.gauntlet.spikes", M.Cone(6), M.At(V(0, 0, -.025f), V(0, 0, -90), V(.018f, .035f, .018f)), M.At(V(0, 0, .025f), V(0, 0, -90), V(.018f, .035f, .018f))), V(.072f, -.64f, 0), One);
            f.FancyPair(OnArmR, Ring(.088f, .094f, -.575f, -.56f, 14), V0, One);
            f.GlowPair(1, OnArmR, Sphere, V(rTop * .82f, -.44f, 0), V(.012f, .026f, .026f), V(0, 0, 25));
            f.GlowPair(2, OnArmR, Sphere, V(.078f, -.632f, 0), V(.012f, .022f, .022f));
            f.RustPair(OnArmR, V(.09f, -.48f, .02f), V(.01f, .04f, .03f));
            Mark(f, OnArmR, V(rTop * .85f, -.47f, .04f), V(0, 70, 0), .6f, true);
        }

        // ---------- legs ----------
        void Legs(Fit f)
        {
            switch (f.l.family)
            {
                case "legs.breeches": Breeches(f); break;
                case "legs.leggings": Leggings(f); break;
                case "legs.greaves": Greaves(f); break;
                case "legs.kilt": Kilt(f); break;
            }
        }
        /// <summary>Breeches: the legs and hips in cloth with a seam stripe. Patched: a patch on each knee. A garter below the knee, side lacing, a waistband and a padded knee come with the bands.</summary>
        void Breeches(Fit f)
        {
            Cover(baseLegs, RoleMat(f, "cloth"));
            f.Pair(OnLegR, "cloth2", Cube, V(.089f, -.3f, 0), V(.012f, .42f, .03f));
            if (f.v == "patched")
            {
                f.Pair(OnLegR, "leather", Sphere, V(0, -.44f, .084f), V(.1f, .1f, .03f));   // a leather knee patch
                f.Pair(OnLegR, "leather", M.Many("a.breeches.stitches", Cube, M.At(V(.045f, .03f, 0), V0, V(.004f, .014f, .004f)), M.At(V(-.045f, .03f, 0), V0, V(.004f, .014f, .004f)), M.At(V(.045f, -.03f, 0), V0, V(.004f, .014f, .004f)), M.At(V(-.045f, -.03f, 0), V0, V(.004f, .014f, .004f))), V(0, -.44f, .095f), One);
            }
            if (f.tier >= 1) f.Pair(OnLegR, "leather", Ring(.09f, .097f, -.52f, -.5f), V0, One);
            if (f.tier >= 2) f.Pair(OnLegR, "leather", M.Many("a.breeches.lacing", Cube, M.At(V(0, .03f, 0), V(30, 0, 0), V(.004f, .04f, .004f)), M.At(V(0, .03f, 0), V(-30, 0, 0), V(.004f, .04f, .004f)), M.At(V(0, -.03f, 0), V(30, 0, 0), V(.004f, .04f, .004f)), M.At(V(0, -.03f, 0), V(-30, 0, 0), V(.004f, .04f, .004f))), V(.096f, -.2f, 0), One);
            if (f.tier >= 3) f.Add("trim", Band(Rg(.232f, -.05f, .147f, 0), .04f, .01f), V0, One);
            if (f.tier >= 4) f.Pair(OnLegR, "leather", M.Lathe("a.breeches.knee", Pts(.06f, 0, .055f, .02f, .035f, .04f, 0, .048f), 12, true), V(0, -.44f, .062f), One, V(90, 0, 0));
            f.Fancy(OnBody, M.Many("a.breeches.studs", Sphere, M.At(V(.1f, 0, 0), V0, S(.018f)), M.At(V(-.1f, 0, 0), V0, S(.018f))), V(0, -.03f, .14f), One);
            f.GlowPair(1, OnLegR, Sphere, V(.097f, -.51f, 0), V(.012f, .022f, .022f));
            f.GlowPair(2, OnLegR, Sphere, V(0, -.44f, .1f), V(.022f, .022f, .012f));
            f.RustPair(OnLegR, V(.03f, -.7f, .086f), V(.04f, .05f, .008f), V(0, 20, 0));
            Mark(f, OnLegR, V(.093f, -.24f, 0), V(0, 90, 0), .7f, true);
        }
        /// <summary>Leggings: the legs in cloth, leather cross-garters wound up the shin. Hide: shaggy hide wraps with a fur top and ties.</summary>
        void Leggings(Fit f)
        {
            bool hide = f.v == "hide";
            Cover(baseLegs, RoleMat(f, hide ? "leather" : "cloth"));
            if (hide)
            {
                f.Pair(OnLegR, "hide", M.Lathe("a.leggings.hide", Pts(.075f, -.8f, .1f, -.8f, .104f, -.62f, .098f, -.48f, .08f, -.475f), 14, true), V0, One);
                f.Pair(OnLegR, "fur", M.Lathe("a.leggings.fur", Pts(.08f, -.5f, .108f, -.5f, .115f, -.465f, .1f, -.435f, .08f, -.44f), 14, true), V0, One);
                f.Pair(OnLegR, "leather", Ring(.103f, .11f, -.6f, -.585f), V0, One); f.Pair(OnLegR, "leather", Ring(.1f, .107f, -.72f, -.705f), V0, One);
            }
            else
            {
                var strips = new List<Matrix4x4>();
                for (int i = 0; i < 6; i++)
                {
                    float y = -.52f - i * .05f;
                    foreach (var (phase, tilt) in new[] { (0f, 40f), (180f, -40f) })
                    {
                        float a = i * 60 + phase; var d = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad), 0, Mathf.Cos(a * Mathf.Deg2Rad));
                        strips.Add(M.At(d * .093f + Vector3.up * y, V(0, a, tilt), V(.014f, .075f, .006f)));
                    }
                }
                f.Pair(OnLegR, "leather", M.Many("a.leggings.garters", Cube, strips.ToArray()), Matrix4x4.identity);
            }
            if (f.tier >= 2) f.Pair(OnLegR, "leather", M.Lathe("a.breeches.knee", Pts(.06f, 0, .055f, .02f, .035f, .04f, 0, .048f), 12, true), V(0, -.44f, .062f), One, V(90, 0, 0));
            if (f.tier >= 3) { f.Pair(OnLegR, "leather", Ring(.09f, .097f, -.24f, -.22f), V0, One); f.Pair(OnLegR, "trim", Cube, V(.097f, -.23f, 0), V(.008f, .024f, .02f)); }
            if (f.tier >= 4) f.Pair(OnLegR, "trim", Ring(.096f, .104f, -.5f, -.485f), V0, One);
            f.FancyPair(OnLegR, Ring(.091f, .096f, -.06f, -.045f), V0, One);
            f.GlowPair(1, OnLegR, Sphere, V(0, -.5f, .1f), V(.022f, .022f, .012f));
            f.GlowPair(2, OnLegR, Sphere, V(.097f, -.23f, .02f), V(.012f, .02f, .02f));
            f.RustPair(OnLegR, V(-.03f, -.3f, .086f), V(.04f, .05f, .008f), V(0, -20, 0));
            Mark(f, OnLegR, V(.093f, -.3f, 0), V(0, 90, 0), .7f, true);
        }
        /// <summary>
        /// Greaves: a shin plate open at the back, a knee cop with wings and straps, over padded legs. Full: a thigh plate too.
        /// Boiled leather in the first two bands, plate after; a ridge down the shin from the fourth, a spiked knee in the last.
        /// </summary>
        void Greaves(Fit f)
        {
            bool full = f.v == "full"; string main = f.tier >= 2 || full ? "plate" : "leather2";
            Cover(baseLegs, RoleMat(f, "leather"));
            f.Pair(OnLegR, main, M.Shell("a.greave.shin", new[] { Rg(.1f, -.8f, .1f, .008f), Rg(.108f, -.7f, .108f, .008f), Rg(.11f, -.58f, .11f, .008f), Rg(.1f, -.5f, .1f, .006f) }, .014f, 2.4f, new[] { 150f }, 270, 18), V0, One);
            f.Pair(OnLegR, main, M.Lathe("a.greave.knee", Pts(.075f, 0, .07f, .025f, .045f, .05f, 0, .062f), 14, true), V(0, -.44f, .058f), One, V(90, 0, 0));
            f.Pair(OnLegR, main, M.Many("a.greave.wings", Sphere, M.At(V(.07f, 0, 0), V0, V(.03f, .07f, .06f)), M.At(V(-.07f, 0, 0), V0, V(.03f, .07f, .06f))), V(0, -.44f, .045f), One);
            if (full) f.Pair(OnLegR, main, M.Shell("a.greave.thigh", new[] { Rg(.112f, -.38f, .112f, .008f), Rg(.112f, -.12f, .115f, .01f) }, .014f, 2.4f, new[] { 160f }, 270, 18), V0, One);
            f.Pair(OnLegR, "trim", M.Many("a.greave.straps", M.Lathe("a.greave.strap", Pts(.091f, -.008f, .098f, -.008f, .098f, .008f, .091f, .008f, .091f, -.008f), 14), M.At(V(0, -.56f, 0), V0, One), M.At(V(0, -.74f, 0), V0, One)), Matrix4x4.identity);
            if (f.tier >= 3) f.Pair(OnLegR, "trim", M.Rod("a.greave.ridge", new[] { V(0, -.52f, .112f), V(0, -.65f, .124f), V(0, -.79f, .108f) }, .007f, .006f), Matrix4x4.identity);
            if (f.tier >= 4) f.Pair(OnLegR, main, M.Cone(), V(0, -.44f, .115f), V(.04f, .07f, .04f), V(90, 0, 0));
            f.FancyPair(OnLegR, Ring(.106f, .114f, -.512f, -.5f, 18), V0, One);
            f.GlowPair(1, OnLegR, Sphere, V(0, -.44f, .123f), V(.024f, .024f, .012f));
            f.GlowPair(2, OnLegR, Sphere, V(0, -.64f, .122f), V(.02f, .02f, .01f));
            f.RustPair(OnLegR, V(.05f, -.7f, .098f), V(.04f, .05f, .008f), V(0, 25, 0));
            Mark(f, OnLegR, V(.08f, -.62f, .078f), V(0, 45, 0), .6f, true);
        }
        /// <summary>A kilt of eight bone plates hanging from a belt to the knee, over leather.</summary>
        void Kilt(Fit f)
        {
            Cover(baseLegs, RoleMat(f, "leather"));
            var plate = M.Panel("a.kilt.plate", Pts(-.055f, 0, .055f, 0, .05f, -.38f, 0, -.44f, -.05f, -.38f), .012f, .008f, true); var plates = new List<(Mesh, Matrix4x4)>();
            for (int i = 0; i < 8; i++) { float a = 22.5f + i * 45; plates.Add((plate, M.At(V(Mathf.Cos(a * Mathf.Deg2Rad) * .272f, 0, Mathf.Sin(a * Mathf.Deg2Rad) * .188f), V(-8, 90 - a, 0), One))); }
            f.Add(OnBody, "bone", M.Join("a.kilt.plates", plates), Matrix4x4.identity);
            f.Add("leather", Band(Rg(.282f, -.03f, .195f, 0), .06f, .016f), V0, One);
            f.Fancy(OnBody, M.Many("a.kilt.studs", Sphere, M.At(V(.1f, 0, 0), V0, S(.02f)), M.At(V(-.1f, 0, 0), V0, S(.02f))), V(0, 0, .197f), One);
            f.Glow(1, OnBody, Sphere, V(0, 0, .2f), V(.03f, .03f, .014f));
            f.Glow(2, OnBody, Sphere, V(0, 0, -.2f), V(.03f, .03f, .014f));
            f.Rust(OnBody, V(.18f, -.2f, .19f), V(.04f, .06f, .008f), V(0, 40, 0));
            Mark(f, OnBody, V(.23f, 0, .12f), V(0, 60, 0));
        }

        // ---------- feet ----------
        void Feet(Fit f)
        {
            switch (f.l.family)
            {
                case "feet.shoes": Shoes(f); break;
                case "feet.boots": Boots(f); break;
                case "feet.sabatons": Sabatons(f); break;
            }
        }
        /// <summary>Shoes: rounded leather toes on a sole. A buckle, a tongue and a toe cap come with the bands.</summary>
        void Shoes(Fit f)
        {
            Cover(baseBoots, RoleMat(f, "leather"));
            f.Pair(OnLegR, "dark", Cube, V(0, -.912f, .06f), V(.18f, .024f, .3f));
            f.Pair(OnLegR, "leather", Sphere, V(0, -.87f, .17f), V(.17f, .11f, .13f));
            if (f.tier >= 1) f.Pair(OnLegR, "trim", Cube, V(0, -.795f, .11f), V(.05f, .012f, .035f));
            if (f.tier >= 2) f.Pair(OnLegR, "leather", Cube, V(0, -.785f, .07f), V(.1f, .03f, .06f), V(-25, 0, 0));
            if (f.tier >= 3) f.Pair(OnLegR, "trim", M.Lathe("a.shoes.cap", Pts(.07f, 0, .066f, .012f, 0, .016f), 12), V(0, -.87f, .225f), One, V(90, 0, 0));
            f.FancyPair(OnLegR, Cube, V(0, -.9f, -.09f), V(.15f, .03f, .015f));
            f.GlowPair(1, OnLegR, Sphere, V(0, -.79f, .11f), V(.02f, .012f, .02f));
            f.GlowPair(2, OnLegR, Sphere, V(.086f, -.86f, .05f), V(.01f, .02f, .02f));
            f.RustPair(OnLegR, V(.04f, -.84f, .225f), V(.04f, .03f, .01f), V(0, 30, 0));
            Mark(f, OnLegR, V(.087f, -.86f, .05f), V(0, 90, 0), .6f, true);
        }
        /// <summary>
        /// Boots: a leather shaft to mid-shin with a folded top, rounded toes. Waders: glossy to the knee. Hobnail: a heavy studded
        /// sole. Root: roots wound up the shaft with lit buds. An ankle strap, a shin plate and a trimmed fold come with the bands.
        /// </summary>
        void Boots(Fit f)
        {
            string v = f.v; bool waders = v == "waders"; string main = waders ? "oil" : "leather";
            Cover(baseBoots, RoleMat(f, main));
            f.Pair(OnLegR, main, Sphere, V(0, -.87f, .17f), V(.17f, .11f, .13f));
            if (waders)
            {
                f.Pair(OnLegR, main, M.Lathe("a.boots.wader", Pts(.075f, -.8f, .089f, -.8f, .1f, -.62f, .106f, -.42f, .095f, -.415f), 14, true), V0, One);
                f.Pair(OnLegR, "leather2", M.Lathe("a.boots.wader.top", Pts(.1f, -.43f, .118f, -.43f, .126f, -.37f, .108f, -.37f, .1f, -.43f), 14), V0, One);
            }
            else
            {
                f.Pair(OnLegR, main, M.Lathe("a.boots.shaft", Pts(.075f, -.8f, .089f, -.8f, .1f, -.62f, .09f, -.615f), 14, true), V0, One);
                f.Pair(OnLegR, f.tier >= 4 ? "trim" : "leather2", M.Lathe("a.boots.fold", Pts(.095f, -.635f, .114f, -.63f, .118f, -.575f, .098f, -.575f, .095f, -.635f), 14), V0, One);
            }
            if (v == "hobnail")
            {
                f.Pair(OnLegR, "dark", Cube, V(0, -.912f, .06f), V(.19f, .03f, .31f));
                var nails = new List<Matrix4x4>(); for (int i = 0; i < 6; i++) { float z = -.07f + i * .052f; nails.Add(M.At(V(.093f, -.912f, z), V0, S(.016f))); nails.Add(M.At(V(-.093f, -.912f, z), V0, S(.016f))); }
                f.Pair(OnLegR, "metal", M.Many("a.boots.nails", Sphere, nails.ToArray()), Matrix4x4.identity);
            }
            if (v == "root")
            {
                var helix = new Vector3[6]; for (int i = 0; i < 6; i++) { float a = i * 80 * Mathf.Deg2Rad; helix[i] = V(Mathf.Cos(a) * .11f, -.8f + i * .035f, Mathf.Sin(a) * .11f); }
                f.Add(OnLegR, "wood", M.Rod("a.boots.root.r", helix, .013f, .009f, 6, 16), Matrix4x4.identity);
                f.Add(OnLegL, "wood", M.Rod("a.boots.root.l", System.Array.ConvertAll(helix, p => V(-p.x, p.y, p.z)), .013f, .009f, 6, 16), Matrix4x4.identity);
                f.Pair(OnLegR, "lit", Sphere, V(.095f, -.74f, .04f), S(.02f)); f.Pair(OnLegR, "lit", Sphere, V(-.06f, -.66f, .085f), S(.02f));
            }
            bool plain = v == "plain" || waders;
            if (f.tier >= 2 && plain) { f.Pair(OnLegR, "leather2", Ring(.09f, .097f, -.765f, -.745f), V0, One); f.Pair(OnLegR, "trim", Cube, V(.095f, -.755f, .02f), V(.008f, .024f, .022f)); }
            if (f.tier >= 3 && plain) f.Pair(OnLegR, "trim", M.Lathe("a.shoes.cap", Pts(.07f, 0, .066f, .012f, 0, .016f), 12), V(0, -.87f, .225f), One, V(90, 0, 0));   // a steel toe cap
            f.FancyPair(OnLegR, Ring(.112f, .12f, -.6f, -.59f), V0, One);
            f.GlowPair(1, OnLegR, Sphere, V(0, -.6f, .118f), V(.022f, .022f, .012f));
            f.GlowPair(2, OnLegR, Sphere, V(.086f, -.86f, .05f), V(.01f, .02f, .02f));
            f.RustPair(OnLegR, V(.03f, -.72f, .094f), V(.04f, .04f, .008f), V(0, 20, 0));
            Mark(f, OnLegR, V(.1f, -.7f, 0), V(0, 90, 0), .6f, true);
        }
        /// <summary>Sabatons: a pointed steel toe box, two lames over the instep and a shaft plate round the ankle, on dark leather.</summary>
        void Sabatons(Fit f)
        {
            string main = f.tier >= 1 ? "plate" : "leather2";
            Cover(baseBoots, RoleMat(f, "dark"));
            f.Pair(OnLegR, main, Sphere, V(0, -.865f, .16f), V(.18f, .11f, .2f));
            f.Pair(OnLegR, main, M.Many("a.sabaton.lames", Cube, M.At(V(0, -.795f, .06f), V(-8, 0, 0), V(.185f, .022f, .07f)), M.At(V(0, -.81f, .125f), V(-20, 0, 0), V(.18f, .022f, .07f))), Matrix4x4.identity);
            f.Pair(OnLegR, main, M.Shell("a.sabaton.shaft", new[] { Rg(.096f, -.8f, .096f, .01f), Rg(.1f, -.62f, .1f, .01f) }, .012f, 2.2f, new[] { 130f }, 270, 16), V0, One);
            f.Pair(OnLegR, f.tier >= 2 ? "trim" : "dark", M.Many("a.sabaton.edges", Cube, M.At(V(0, -.79f, .094f), V(-8, 0, 0), V(.187f, .008f, .008f)), M.At(V(0, -.8f, .158f), V(-20, 0, 0), V(.182f, .008f, .008f))), Matrix4x4.identity);
            if (f.tier >= 3) f.Pair(OnLegR, "trim", Ring(.1f, .108f, -.63f, -.615f), V0, One);
            if (f.tier >= 4) f.Pair(OnLegR, main, M.Cone(), V(0, -.66f, .1f), V(.03f, .05f, .03f), V(90, 0, 0));
            f.FancyPair(OnLegR, M.Many("a.sabaton.rivets", Sphere, M.At(V(.07f, 0, 0), V0, S(.014f)), M.At(V(-.07f, 0, 0), V0, S(.014f))), V(0, -.785f, .06f), One);
            f.GlowPair(1, OnLegR, Sphere, V(0, -.7f, .112f), V(.022f, .022f, .012f));
            f.GlowPair(2, OnLegR, Sphere, V(0, -.83f, .25f), V(.02f, .016f, .016f));
            f.RustPair(OnLegR, V(.05f, -.85f, .2f), V(.04f, .03f, .01f), V(0, 30, 0));
            Mark(f, OnLegR, V(.1f, -.7f, 0), V(0, 90, 0), .6f, true);
        }
    }
}
