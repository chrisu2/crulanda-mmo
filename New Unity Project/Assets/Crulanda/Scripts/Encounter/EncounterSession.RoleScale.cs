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
            // DPS toward the median (a Ranger's pet is not scaled: its share is left in, so the Ranger's own blows take more of the cut)
            { "class.ranger/marksman", .67f }, { "class.druid/thornclaw", .80f }, { "class.ranger/skirmisher", .80f }, { "class.paladin/judicator", .92f },
            { "class.mage/combustion", 1.65f }, { "class.archivist/the-unwriting", 2.4f },
            // under the role caps
            { "class.ranger/pathfinder", .43f }, { "class.rogue/thiefs-grace", .82f }, { "class.rogue/swashbuckler", .72f }, { "class.paladin/vanguard", .92f },
            { "class.warrior/support", .93f }, { "class.warrior/jailer", .93f },
            { "class.paladin/sanctuary", .62f }, { "class.saltmender/mender", .85f },
        };
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
