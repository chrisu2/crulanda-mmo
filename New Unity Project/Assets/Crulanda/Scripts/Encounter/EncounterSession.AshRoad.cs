using System.Collections.Generic;
using UnityEngine;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's Ash Road and its bridge as book 1 chapter 17 tells them (Docs/REWORK_LOWTOWNS_HALLOWS.md picks 7 and 10;
    /// Docs/DUNGEON2_DESIGN.md):
    /// - the time-squall (CANON, ch.17:79-121): now and then the sky over the road bleeds purple and a silver shimmer comes up the road
    ///   from the south, warned for <see cref="SquallWarnSeconds"/>; then for <see cref="SquallSeconds"/> silver sparks fall, and each
    ///   second you stand in the open on the road they take a year out of you (a bite of health, never below a sixth of it). Under the
    ///   rock overhang (the book's shelter, ch.17:93) you are safe. The squall wears on you only: in a run a sim leads (the group runs,
    ///   RunCapture) it passes over the party harmlessly, so the run keeps its pace, and the sims are never touched by it;
    /// - the stone leaves (CANON, ch.17:23): walking through a thicket of them off the road cuts you a little (the same floor);
    /// - the slab that tips (CANON-EXPANDED, ch.17:243): it dips and groans under a foot and you keep walking; once the party is past,
    ///   it tips to forty-five degrees over the fog (TippingSlab: its walk and navmesh stay flat and solid; only its stone moves).
    /// The fissures' green light, the shadows that point at the sun and the slabs that hang and turn beside the walk are dressing
    /// (ZoneBuilder.HallowsRoad). Nothing here is a machine.
    /// </summary>
    public sealed partial class EncounterSession
    {
        /// <summary>The road the squalls sweep and the shelter from them (the zone's names).</summary>
        public const string AshRoadName = "The Ash Road", SquallShelter = "The overhang";
        /// <summary>Seconds a time-squall is warned, how long it falls, the seconds to the first one on the road and between them after
        /// (tests shorten them), and the share of health a second in the open takes.</summary>
        public static float SquallWarnSeconds = 10, SquallSeconds = 20, SquallFirst = 75, SquallEvery = 170, SquallBite = .04f;
        /// <summary>How far either side of the road's line the squall reaches.</summary>
        public const float SquallReach = 16;
        /// <summary>The time-squall's state: none, warned (the shimmer coming), falling.</summary>
        public enum SquallState { None, Warned, Falling }
        /// <summary>The squall now (tests read it).</summary>
        public SquallState Squall { get; private set; }
        /// <summary>The tipping slab this visit (null where the zone has none).</summary>
        public TippingSlab TipSlab { get; private set; }
        /// <summary>Starts a time-squall now (a dev command or a test): warned, then falling.</summary>
        public void StartSquall() { if (Squall == SquallState.None && AshRoad != null) BeginSquall(); }

        ZonePath AshRoad; Hollow shelter; float squallAt, squallNext, nextSquallBite, nextLeafCut, leafSaidAt = -99, slabSaidAt = -99;
        bool weatherForced, agedSaid; Transform shimmer; ParticleSystem sparks;

        /// <summary>On arrival: the road and its shelter found (none outside Hallow's Creek), the first squall timed, the tipping slab set.</summary>
        void StartAshRoad()
        {
            Squall = SquallState.None; AshRoad = null; shelter = null; TipSlab = null; weatherForced = false; ClearSquallLooks();
            if (Zone == null || !Zone.Zone.dungeon) return;
            if (Zone.Zone.roads != null) AshRoad = System.Array.Find(Zone.Zone.roads, r => r != null && r.name == AshRoadName && r.points != null && r.points.Length > 1);
            shelter = Hollow.All.Find(h => h.Name == SquallShelter);
            if (shelter == null) AshRoad = null;
            squallNext = Time.time + SquallFirst;
            foreach (var name in Zone.TipSlabs)
            {
                var spot = Zone.DeckSpots.Find(d => d.def != null && d.def.name == name && d.root != null);
                if (spot != null) { TipSlab = TippingSlab.On(spot.root); break; }
            }
        }

        /// <summary>Whether a sim leads this run (the group runs): the road's dangers then pass over the party.</summary>
        bool SimLedRun { get { return PartySims.Exists(c => c != null && c.Dungeon != null); } }
        /// <summary>Whether a point is on the Ash Road, within the squall's reach of its line.</summary>
        public bool OnAshRoad(Vector3 p)
        {
            if (AshRoad == null) return false;
            var q = new Vector2(p.x, p.z); var pts = AshRoad.points;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                var a = pts[i]; var ab = pts[i + 1] - a; float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / Mathf.Max(.001f, ab.sqrMagnitude));
                if (Vector2.Distance(q, a + ab * t) <= SquallReach) return true;
            }
            return false;
        }
        /// <summary>Whether a point is under the rock overhang (safe from the squall).</summary>
        public bool Sheltered(Vector3 p) { return shelter != null && shelter.Inside(p, .2f); }

        // ---------- each frame ----------
        void TickAshRoad()
        {
            if (Zone == null || Player == null) return;
            var p = Player.transform.position; bool simLed = SimLedRun;
            TickSquall(p, simLed);
            // The stone leaves: brushing through a thicket off the road cuts you a little, once a second and a half while you move in it.
            if (Zone.LeafThickets.Count > 0 && Player.IsAlive && !simLed && Time.time >= nextLeafCut && Zone.InStoneLeaves(p - Vector3.up))
            {
                var cc = Player.GetComponent<CharacterController>();
                if (cc == null || new Vector2(cc.velocity.x, cc.velocity.z).sqrMagnitude > .5f)
                {
                    nextLeafCut = Time.time + 1.5f;
                    if (Bite(.02f, 1.5f) && Time.time - leafSaidAt > 30) { leafSaidAt = Time.time; Message("The stone leaves are sharp enough to cut skin. You come out of them with thin red lines across your hands."); }
                }
            }
            // The slab that tips: everyone standing on it counted, and whether anyone is near enough to keep it level.
            if (TipSlab != null)
            {
                int on = 0; bool near = false;
                void Count(Vector3 at) { if (TipSlab.Holds(at)) on++; if (new Vector2(at.x - TipSlab.Middle.x, at.z - TipSlab.Middle.z).magnitude < TippingSlab.Clear) near = true; }
                if (Player.IsAlive) Count(p);
                foreach (var c in PartySims) if (c != null) Count(c.transform.position);
                int said = TipSlab.Feel(on, near);
                bool youClose = Vector3.Distance(p, TipSlab.Middle) < 60;
                if (said == 1 && Time.time - slabSaidAt > 20) { slabSaidAt = Time.time; Message("The slab groans and dips under you, grit pattering off its edge into the fog. It holds. Keep walking."); }
                if (said == 2 && youClose) Message("Behind you the slab you crossed groans, tilts, and hangs at forty-five degrees over the fog, its rubble sliding off into the purple.");
            }
        }

        void TickSquall(Vector3 p, bool simLed)
        {
            if (AshRoad == null) return;
            bool onRoad = OnAshRoad(p);
            switch (Squall)
            {
                case SquallState.None:
                    // A squall only blows up while you are on the road (a squall no one sees is no squall).
                    if (Time.time >= squallNext) { if (onRoad && Player.IsAlive) BeginSquall(); else squallNext = Time.time + 8; }
                    break;
                case SquallState.Warned:
                    // The silver shimmer comes up the road toward you over the warning.
                    if (shimmer != null)
                    {
                        float k = Mathf.Clamp01((Time.time - squallAt) / Mathf.Max(.01f, SquallWarnSeconds));
                        var from = AshRoad.points[0]; float z = Mathf.Lerp(from.y - 10, p.z - 6, k * k);
                        shimmer.position = new Vector3(p.x * .3f, p.y + 2, z);
                    }
                    if (Time.time - squallAt >= SquallWarnSeconds)
                    {
                        Squall = SquallState.Falling; squallAt = Time.time; nextSquallBite = Time.time + 1; agedSaid = false;
                        if (shimmer != null) shimmer.gameObject.SetActive(false);
                        Message("The first drops fall, and they are not water: glowing silver sparks, hissing on the road.");
                        // Beside the road by the overhang a sapling sprouts, shoots up into a twisting tree and rots to ash in seconds (ch.17:121).
                        if (Vector3.Distance(p, shelter.Centre[0]) < 60) StartCoroutine(SquallSapling(Zone.Ground(new Vector2(6.5f, -238), 0)));
                    }
                    break;
                case SquallState.Falling:
                    // The sparks fall round you; under the overhang (it opens east off the road) they fall out on the road, not in with you.
                    if (sparks != null) sparks.transform.position = p + Vector3.up * 9 + (Sheltered(p) ? Vector3.left * 22 : Vector3.zero);
                    // A year out of you each second in the open (ch.17:93: "if the rain touches you, you age fifty years in a second"): never
                    // below a sixth of your health, and not in a run a sim leads.
                    if (Time.time >= nextSquallBite)
                    {
                        nextSquallBite = Time.time + 1;
                        if (onRoad && !Sheltered(p) && Player.IsAlive && !simLed && Bite(SquallBite, 1) && !agedSaid)
                        {
                            agedSaid = true;
                            Message("A silver drop catches the back of your hand. The skin there puckers and goes thin and old. Get under the overhang!");
                        }
                    }
                    if (Time.time - squallAt >= SquallSeconds) EndSquall();
                    break;
            }
        }

        /// <summary>A bite of the road's (a share of the player's health, felt over the resting mend for <paramref name="seconds"/>), never
        /// taking it below a sixth. True when it bit.</summary>
        bool Bite(float share, float seconds)
        {
            // Out of a fight the resting mend (RestRegen, every second) would heal it straight back (at 180 health a squall's 4% is 7,
            // the mend's 7: it did nothing in the open, the Ash Road test of 2026-10-18), so the mend over the bite's interval is added.
            var hp = Player.Health; int bite = Mathf.Max(1, Mathf.RoundToInt(hp.Pool.Max * share)) + (InCombat ? 0 : Mathf.CeilToInt(RestRegen * seconds));
            if (hp.Pool.Current - bite <= hp.Pool.Max / 6f) return false;
            hp.ApplyDamage(bite); return true;
        }

        void BeginSquall()
        {
            Squall = SquallState.Warned; squallAt = Time.time;
            Message("The sky over the road bleeds from grey to a bruised purple, and a silver shimmer is coming up the road from the south. A time-squall: get under the rock overhang!");
            // The squall's look: the zone's own silver squall weather (WorldWeather; its words are the zone's squallText).
            if (WorldWeather.Active != null) { WorldWeather.Active.Force(WeatherKind.AshSquall, false); weatherForced = true; }
            if (shimmer == null)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "The time-squall's shimmer"; Destroy(wall.GetComponent<Collider>());
                wall.transform.localScale = new Vector3(SquallReach * 2 + 8, 9, .6f);
                wall.GetComponent<MeshRenderer>().sharedMaterial = GearMats.Glass(new Color(.82f, .84f, .92f), .28f, new Color(.6f, .62f, .75f));
                shimmer = wall.transform;
            }
            shimmer.gameObject.SetActive(true);
            if (sparks == null) sparks = SilverSparks();
            sparks.Play(true);
        }

        void EndSquall()
        {
            Squall = SquallState.None; squallNext = Time.time + SquallEvery * Random.Range(.85f, 1.2f);
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (shimmer != null) shimmer.gameObject.SetActive(false);
            if (weatherForced && WorldWeather.Active != null) WorldWeather.Active.Release(false);
            weatherForced = false;
            Message("The squall passes as quickly as it came. Out on the road the stone flowers have bloomed, died and bloomed again.");
        }

        void ClearSquallLooks()
        {
            if (shimmer != null) Destroy(shimmer.gameObject);
            if (sparks != null) Destroy(sparks.gameObject);
            shimmer = null; sparks = null;
        }

        /// <summary>A stone sapling that grows, dies and falls to ash in the squall (b1 ch.17:121), seen from the overhang.</summary>
        System.Collections.IEnumerator SquallSapling(Vector3 foot)
        {
            var tree = new GameObject("A sapling in the time-squall").transform; tree.position = foot;
            var bark = GearMats.Get(new Color(.4f, .38f, .36f)); var leaf = GearMats.Get(new Color(.5f, .52f, .5f));
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(trunk.GetComponent<Collider>()); trunk.transform.SetParent(tree, false); trunk.GetComponent<MeshRenderer>().sharedMaterial = bark;
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(crown.GetComponent<Collider>()); crown.transform.SetParent(tree, false); crown.GetComponent<MeshRenderer>().sharedMaterial = leaf;
            // Three seconds from a shoot to a twisting oak, two seconds hollowing, a second to fall and a second and a half to blow away.
            for (float t = 0; t < 3 && tree != null; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / 3), h = Mathf.Lerp(.1f, 5, k);
                trunk.transform.localScale = new Vector3(.08f + .3f * k, h / 2, .08f + .3f * k); trunk.transform.localPosition = new Vector3(0, h / 2, 0);
                trunk.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2) * 6 * k);
                crown.transform.localScale = Vector3.one * Mathf.Lerp(.1f, 3.4f, k); crown.transform.localPosition = new Vector3(0, h + .6f * k, 0);
                yield return null;
            }
            yield return new WaitForSeconds(2);
            if (tree == null) yield break;
            var start = tree.rotation; var fallTo = start * Quaternion.Euler(80, 0, 0);
            for (float t = 0; t < 1 && tree != null; t += Time.deltaTime) { tree.rotation = Quaternion.Slerp(start, fallTo, t * t); yield return null; }
            for (float t = 0; t < 1.5f && tree != null; t += Time.deltaTime) { tree.localScale = Vector3.one * (1 - t / 1.5f); yield return null; }
            if (tree != null) Destroy(tree.gameObject);
        }
        /// <summary>The squall's sparks: glowing silver drops falling round you (they follow you while it falls).</summary>
        static ParticleSystem SilverSparks()
        {
            var go = new GameObject("The time-squall's sparks"); var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 1.6f; main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.06f, .14f);
            main.gravityModifier = .55f; main.startColor = new Color(.86f, .88f, .98f, .95f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 900;
            var em = ps.emission; em.rateOverTime = 420;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(40, 1, 40);
            var r = go.GetComponent<ParticleSystemRenderer>(); var sd = Shader.Find("Sprites/Default"); if (sd != null) r.sharedMaterial = new Material(sd);
            return ps;
        }
    }
}
