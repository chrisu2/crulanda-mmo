using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>Chris 2026-10-12 1:29, "meter should be movable in the ui. pet bar should be movable in the ui.": the damage meter and
    /// the pet bar are panels like the frames and the action bar: moved, the place is kept across a run (PlayerPrefs), and
    /// "Put them back" returns them.</summary>
    public class MovablePanelTests
    {
        static readonly string[] Ids = { "meter", "pet" };
        [Test] public void Meter_and_pet_bar_move_are_kept_and_reset()
        {
            bool was = EncounterHud.DefaultPanels; EncounterHud.DefaultPanels = false;
            try
            {
                EncounterHud.ResetPanels();
                foreach (var id in Ids)
                {
                    var home = EncounterHud.PanelRect(id);
                    Assert.Greater(home.width, 0, id + " is a movable panel");
                    var to = new Vector2(-200, -200);   // (2026-10-13: a panel keeps 40 px on the screen: 300 left took the pet bar off it)
                    EncounterHud.MovePanel(id, to);
                    EncounterHud.ForgetPanelCache();   // as a new run would
                    var moved = EncounterHud.PanelRect(id);
                    Assert.AreEqual(home.x - 200, moved.x, .01f, id + " kept its new x"); Assert.AreEqual(home.y - 200, moved.y, .01f, id + " kept its new y");
                    EncounterHud.ResetPanels(); EncounterHud.ForgetPanelCache();
                    var back = EncounterHud.PanelRect(id);
                    Assert.AreEqual(home.position.x, back.position.x, .01f, id + " back in place"); Assert.AreEqual(home.position.y, back.position.y, .01f, id + " back in place");
                }
            }
            finally { EncounterHud.ResetPanels(); EncounterHud.DefaultPanels = was; }
        }
    }
}
