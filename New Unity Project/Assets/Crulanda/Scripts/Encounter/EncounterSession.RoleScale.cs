using Crulanda.Gameplay;
using System.Collections.Generic;
using System.Linq;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The balance retune (note 174; note 177, Chris 18:22: "control classes should not be top tier dps for sure" / "or healers").
    /// The player's own damage is scaled by the build they have spent most in: DPS builds are brought to within about 10% of the DPS
    /// median, and control, support and healer builds sit under their caps (control and support at most 75% of the DPS median, healer at
    /// most 50%). Their worth is crowd control, utility and healing, which this does not touch. Numbers are from the balance harness
    /// (Docs/BALANCE_REPORT.md); 1 is untouched.
    /// </summary>
    public partial class EncounterSession
    {
        public static readonly Dictionary<string, float> BuildDamageScale = new Dictionary<string, float>
        {
            // DPS toward the median
            { "class.ranger/marksman", .75f }, { "class.druid/thornclaw", .80f }, { "class.ranger/skirmisher", .85f }, { "class.paladin/judicator", .92f },
            { "class.mage/combustion", 1.65f }, { "class.druid/thornsong", 2.1f }, { "class.archivist/the-unwriting", 2.4f },
            // under the role caps
            { "class.ranger/pathfinder", .60f }, { "class.rogue/thiefs-grace", .82f }, { "class.rogue/swashbuckler", .72f }, { "class.paladin/vanguard", .92f },
            { "class.warrior/support", .93f }, { "class.warrior/jailer", .93f },
            { "class.paladin/sanctuary", .62f }, { "class.saltmender/mender", .85f },
            // pet builds toward the DPS median (the companion's blows take PetDamageScale, below)
            { "class.ranger/beastbond", .75f }, { "class.mage/bonecaller", 1.6f },
        };
        /// <summary>The same for the build's companion (Ranger pet, thrall): only the pet builds are scaled.</summary>
        public static readonly Dictionary<string, float> BuildPetDamageScale = new Dictionary<string, float> { { "class.ranger/beastbond", .75f }, { "class.mage/bonecaller", 1.6f }, { "class.ranger/pathfinder", .5f } };
        public float PetDamageScale { get { var lead = LeadBuild; return lead != null && BuildPetDamageScale.TryGetValue(lead, out var v) ? v : 1; } }
        /// <summary>The scale on any blow by the party side: the player's, or the player's own companion.</summary>
        public float DamageScaleFor(Actor source)
        {
            if (source == null) return 1;
            if (source == Player) return PlayerDamageScale;
            if (Pet != null && source == Pet.actor) return PetDamageScale;
            return 1;
        }
        /// <summary>The healing retune (note 178): a build's own mends scaled toward the healers' median (Druid kit's HealActor).</summary>
        public static readonly Dictionary<string, float> BuildHealScale = new Dictionary<string, float> { { "class.druid/rootmend", 2.2f } };
        public float PlayerHealScale { get { var lead = LeadBuild; return lead != null && BuildHealScale.TryGetValue(lead, out var v) ? v : 1; } }
        public const float HealerDpsCap = .50f, ControlDpsCap = .75f, SupportDpsCap = .75f;
        int scaleCount = -1; List<TalentRank> scaleList; float scale = 1;
        /// <summary>The build the player has spent most in (class/branch id), or null with no ranks.</summary>
        public string LeadBuild
        {
            get
            {
                if (Talents == null || Progress == null || Progress.talents == null || ClassDef == null) return null;
                string best = null; int most = 0;
                foreach (var b in Talents.Branches)
                {
                    var ids = new HashSet<string>(b.nodes.Select(n => n.id));
                    int spent = Progress.talents.Where(t => t != null && ids.Contains(t.id)).Sum(t => t.rank);
                    if (spent > most) { most = spent; best = b.id; }
                }
                return best == null ? null : ClassDef.id + "/" + best;
            }
        }
        /// <summary>The scale on the player's own blows for the lead build (cached until the ranks change).</summary>
        public float PlayerDamageScale
        {
            get
            {
                var list = Progress != null ? Progress.talents : null; int count = list == null ? 0 : list.Sum(t => t != null ? t.rank : 0);
                if (!ReferenceEquals(list, scaleList) || count != scaleCount)
                {
                    scaleList = list; scaleCount = count; var lead = LeadBuild;
                    scale = lead != null && BuildDamageScale.TryGetValue(lead, out var v) ? v : 1;
                }
                return scale;
            }
        }
    }
}
