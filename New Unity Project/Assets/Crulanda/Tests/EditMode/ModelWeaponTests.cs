using NUnit.Framework;
using UnityEngine;
using Crulanda.Encounter;

namespace Crulanda.Tests
{
    /// <summary>
    /// The model weapons and shields (2026-10-05; ActorVisual.GearModels.cs, Resources/Weapons): every model a look may name is in
    /// the build with something to draw and its own bag icon; generated pieces choose them by quality, the glowing ones only at
    /// epic; the legendaries are the five bosses' and wear the brightest.
    /// </summary>
    public class ModelWeaponTests
    {
        static readonly string[][] Families = { GearLooks.ModelWeapons, GearLooks.ModelTall, GearLooks.ModelShields };

        [Test] public void Every_model_is_in_the_build_with_a_mesh_and_an_icon()
        {
            foreach (var family in Families)
                foreach (var name in family)
                {
                    var go = Resources.Load<GameObject>("Weapons/" + name);
                    Assert.NotNull(go, name + " is in Resources/Weapons.");
                    Assert.Greater(go.GetComponentsInChildren<MeshRenderer>(true).Length, 0, name + " has something to draw.");
                    Assert.NotNull(IconDb.Load("Icons/model/" + name), name + " has its bag icon (Editor/ModelIcons).");
                }
        }

        [Test] public void Generated_pieces_take_models_by_quality()
        {
            Assert.IsNull(GearLooks.GeneratedModel("Blade", 1, 7), "Common gear keeps the drawn families.");
            Assert.IsNull(GearLooks.GeneratedModel("Lantern", 4, 7), "A lantern is drawn.");
            for (int seed = 0; seed < 300; seed++)
            {
                string uncommon = GearLooks.GeneratedModel("Blade", 2, seed), epic = GearLooks.GeneratedModel("Blade", 4, seed);
                Assert.IsFalse(uncommon.StartsWith("Sword13") || uncommon.StartsWith("Sword15"), uncommon + ": the brightest are the legendaries'.");
                Assert.IsFalse(epic.StartsWith("Sword13") || epic.StartsWith("Sword15"), epic + ": the brightest are the legendaries'.");
                CollectionAssert.Contains(GearLooks.ModelWeapons, epic);
            }
            Assert.AreEqual("Shield_Epic", GearLooks.GeneratedModel("Shield", 4, 0));
        }
    }
}
