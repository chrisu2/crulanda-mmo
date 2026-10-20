using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Crulanda.Gameplay;
using Crulanda.World;

namespace Crulanda.Encounter
{
    /// <summary>
    /// Hallow's Creek's Cellars as Book 1 chapter 4 shows the Hollow (Docs/REWORK_LOWTOWNS_HALLOWS.md, Hallow's Creek pick 6; HollowRules has
    /// the rules and the citations; CANON creature and behaviour, GAME-ONLY numbers and places):
    /// - out of the cellar doors (ch.4:161): a camp whose data says after "pull:&lt;camp&gt;" waits under a cellar trapdoor; when a mob of that camp
    ///   is first drawn into a fight, the trapdoor bangs up (a plank lid on the floor, thrown open) and the camp comes up it a beat later behind
    ///   the party, sent at whoever pulled (EncounterSession.Release: once a visit, never respawning);
    /// - the dust (ch.4:177): whom a Hollow's puff catches is blinded a few seconds and their blows miss now and then (<see cref="BlindMisses"/>);
    /// - the anchor-well (ch.4:191-245): a stone well-mouth in the Cellars under the well-house (the zone's anchor "the anchor-well"), a violet
    ///   crystal pulsing in its black water and a violet light breathing over it; while it is whole a Hollow that falls near it stands up again
    ///   (EncounterEnemy.Hollow). E at it: four seconds of work (a fight allowed; a blow taken stops it) and the crystal shatters, the light goes
    ///   white and out, and every Hollow near it crumbles. Not saved: it pulses again on the next visit, as every camp comes back.
    /// No machines (DUNGEON_DESIGN 0.05): a stone well, a crystal, a plank trapdoor.
    /// </summary>
    public sealed partial class EncounterSession
    {
        readonly Dictionary<string, float> blindUntil = new Dictionary<string, float>();
        bool wellWhole; Vector3 wellAt; Transform wellRoot, wellCrystal; Light wellLight; ZoneInteractable wellUse; bool workInFight;
        float nextDustLine, nextHalfLine, nextRiseLine;
        /// <summary>Whether the anchor-well stands this visit and is not yet broken.</summary>
        public bool WellWhole { get { return wellWhole; } }
        /// <summary>The anchor-well's use (E), or null where the zone has none.</summary>
        public ZoneInteractable AnchorWell { get { return wellUse; } }
        /// <summary>How far a point is from the anchor-well (infinite with none, or once broken it does not matter).</summary>
        public float DistanceToWell(Vector3 p) { return wellRoot != null ? Vector3.Distance(p, wellAt) : float.PositiveInfinity; }

        // ---------- the cellar doors (ch.4:161) ----------
        /// <summary>A camp's mob first drawn into a fight (EncounterEnemy.Engage): any camp waiting on that camp's pull comes up its trapdoor.</summary>
        public void CampPulled(EncounterEnemy e, Actor foe)
        {
            if (e == null || !e.Camp || Zone == null || Zone.Zone.camps == null || e.CampIndex < 0 || e.CampIndex >= Zone.Zone.camps.Length) return;
            var camp = Zone.Zone.camps[e.CampIndex]; if (camp == null || string.IsNullOrEmpty(camp.name)) return;
            string key = HollowRules.PullKey + camp.name;
            var waiting = CampsAfter(key); if (waiting == null) return;
            var doors = new List<int>(); foreach (int c in waiting) if (!released.Contains(c)) doors.Add(c);
            if (ReleaseAfter(key, foe, null, "A cellar trapdoor bangs up behind you, and the Hollow come up out of the dark under it, silent.", ReleaseDelay) == 0) return;
            foreach (int c in doors) ThrowOpenTrapdoor(Zone.Zone.camps[c]);
        }
        /// <summary>A plank trapdoor on the cellar floor where a camp comes up, thrown open, its square of dark under it.</summary>
        void ThrowOpenTrapdoor(ZoneCamp cp)
        {
            var want = Zone.StandAt(cp.center, DeckHeight(cp, cp.center), 1);
            var floor = NavMesh.SamplePosition(want, out var hit, 3, NavMesh.AllAreas) ? hit.position : want - Vector3.up;
            var root = new GameObject("Cellar trapdoor (" + cp.name + ")").transform; root.position = floor;
            Block(root, "Hole", new Vector3(0, .01f, 0), new Vector3(1.1f, .02f, 1.1f), new Color(.03f, .03f, .035f), Vector3.zero);
            Block(root, "Lid", new Vector3(0, .55f, -.6f), new Vector3(1.15f, 1.1f, .08f), new Color(.33f, .27f, .2f), new Vector3(-12, 0, 0));
            Block(root, "Strap", new Vector3(0, .8f, -.66f), new Vector3(1.17f, .07f, .03f), new Color(.2f, .2f, .21f), new Vector3(-12, 0, 0));
        }

        // ---------- the dust (ch.4:177) ----------
        /// <summary>Blinds one of the party for <paramref name="seconds"/> (a Hollow's dust): their blows miss now and then.</summary>
        public void Blind(Actor a, float seconds)
        {
            if (a == null) return; string id = a.EntityId.Value;
            blindUntil[id] = Mathf.Max(blindUntil.TryGetValue(id, out var was) ? was : 0, Time.time + seconds);
            FloatText(a.transform.position, "Blinded", new Color(.7f, .7f, .68f));
        }
        /// <summary>Whether <paramref name="a"/> is blinded by the dust now.</summary>
        public bool Blinded(Actor a) { return a != null && blindUntil.TryGetValue(a.EntityId.Value, out var until) && Time.time < until; }
        /// <summary>Whether a blow of <paramref name="a"/>'s misses for the dust (rolled).</summary>
        public bool BlindMisses(Actor a) { return blindUntil.Count > 0 && HollowRules.Misses(Blinded(a), HollowRules.Dice()); }
        public void DustLine()
        {
            if (Time.time < nextDustLine) return; nextDustLine = Time.time + 60;
            Message("The Hollow puffs a thick, choking cloud of grey dust. It stings your eyes to water and fills your mouth with the taste of old ash.");
        }
        public void HalfLine()
        {
            if (Time.time < nextHalfLine) return; nextHalfLine = Time.time + 60;
            Message("Cut in half, it does not stop. What is left of it drags itself across the floor at you.");
        }
        public void WellRiseLine()
        {
            if (Time.time < nextRiseLine) return; nextRiseLine = Time.time + 45;
            Message("The violet light in the well pulses, and the Hollow you put down gets up again. Something in the well is feeding them.");
        }

        // ---------- the anchor-well (ch.4:191-245) ----------
        /// <summary>On arrival (StartHallows): the anchor-well whole at its anchor, pulsing, if the zone has one; nobody blinded.</summary>
        void StartWell()
        {
            blindUntil.Clear(); wellWhole = false; wellRoot = null; wellUse = null; workInFight = false;
            if (Zone == null || !Zone.Anchors.TryGetValue(HollowRules.WellAnchor, out var at)) return;
            if (NavMesh.SamplePosition(at + Vector3.up * .5f, out var hit, 3, NavMesh.AllAreas)) at = hit.position;
            wellAt = at; wellWhole = true;
            wellRoot = new GameObject(HollowRules.WellName).transform; wellRoot.position = at;
            // The well-mouth: a ring of eight dressed stones round black water.
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4; var p = new Vector3(Mathf.Cos(a), .3f, Mathf.Sin(a)) * .95f; p.y = .3f;
                Block(wellRoot, "Rim stone", p, new Vector3(.62f, .6f, .34f), new Color(.42f, .41f, .4f), new Vector3(0, -k * 45 + 90, 0));
            }
            Block(wellRoot, "Black water", new Vector3(0, .42f, 0), new Vector3(1.5f, .02f, 1.5f), new Color(.02f, .015f, .03f), Vector3.zero);
            wellCrystal = Block(wellRoot, "Violet crystal", new Vector3(0, .5f, 0), new Vector3(.28f, .5f, .28f), new Color(.55f, .15f, .85f), new Vector3(20, 45, 15), true);
            var glow = new GameObject("Violet light"); glow.transform.SetParent(wellRoot, false); glow.transform.localPosition = new Vector3(0, 1.2f, 0);
            wellLight = glow.AddComponent<Light>(); wellLight.type = LightType.Point; wellLight.color = new Color(.65f, .25f, 1f); wellLight.range = 9; wellLight.intensity = 2;
            wellUse = new ZoneInteractable { name = HollowRules.WellName, prompt = "Break the crystal in the well", kind = HollowRules.WellKind, position = at, root = wellRoot };
            Zone.Interactables.Add(wellUse);
        }
        /// <summary>Each frame: the well's violet light breathes while it is whole.</summary>
        void TickWell()
        {
            if (wellLight == null) return;
            if (!wellWhole) { wellLight.intensity = Mathf.MoveTowards(wellLight.intensity, 0, Time.deltaTime * 3); return; }
            float pulse = .5f + .5f * Mathf.Sin(Time.time * 2.4f);
            wellLight.intensity = 1.2f + 2.2f * pulse;
            if (wellCrystal != null) wellCrystal.localScale = new Vector3(.28f, .5f, .28f) * (.92f + .16f * pulse);
        }
        /// <summary>E at the well: the work of breaking it (a fight allowed; a blow taken stops it).</summary>
        void UseWell(ZoneInteractable i)
        {
            if (!wellWhole) { Message("The well is still now: black water, a few violet splinters, nothing pulsing."); return; }
            Message("You climb onto the slick rim and drive at the crystal pulsing in the black water.");
            StartWork("Breaking the anchor", HollowRules.WellBreakSeconds, BreakWell);
            // after StartWork, which clears it
            workInFight = true;
        }
        /// <summary>The crystal shatters (ch.4:236-245): white light, and every Hollow near it crumbles; a Hollow lying beside it stays down.</summary>
        public void BreakWell()
        {
            workInFight = false;
            if (!wellWhole) return;
            wellWhole = false;
            if (wellLight != null) { wellLight.color = Color.white; wellLight.intensity = 6; }
            if (wellCrystal != null) wellCrystal.gameObject.SetActive(false);
            if (wellUse != null) wellUse.prompt = "Look into the well";
            FloatText(wellAt, "The anchor breaks", new Color(.9f, .9f, 1f));
            Message("The crystal shatters. White light bursts up out of the well and the violet is gone. Every Hollow near it throws its head back and wails, and starts to crumble.");
            foreach (var e in Enemies.ToArray())
                if (e != null && Vector3.Distance(e.transform.position, wellAt) <= HollowRules.WellReach) e.WellBroken(Player);
        }
        /// <summary>A plain part (no collider): a scaled cube of one colour, glowing when <paramref name="glow"/>.</summary>
        static Transform Block(Transform parent, string name, Vector3 pos, Vector3 scale, Color c, Vector3 euler, bool glow = false)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name;
            Object.Destroy(o.GetComponent<Collider>());
            o.transform.SetParent(parent, false); o.transform.localPosition = pos; o.transform.localScale = scale; o.transform.localEulerAngles = euler;
            var m = new Material(Shader.Find("Standard")) { color = c }; m.SetFloat("_Glossiness", glow ? .7f : .15f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2.2f); }
            var r = o.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return o.transform;
        }
    }
}
