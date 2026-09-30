using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// Shared material palette for generated zones. These are real assets (made by Crulanda > World > Build Oakhaven)
    /// so their shader variants - transparency, emission - are kept in player builds; the runtime builder only
    /// clones them for tints. Colours are placeholders for a stylised, readable look until authored art exists.
    /// </summary>
    [CreateAssetMenu(menuName = "Crulanda/Zone Art Palette")]
    public sealed class ZoneArt : ScriptableObject
    {
        public Material ground, plaster, timber, thatch, slate, stone, bark, foliage, pine, soil, hay, cloth, metal;
        public Material glass;      // emissive warm windows / lanterns
        public Material water;      // transparent
        public Material veil;       // transparent + emissive, the Wasting's static curtain
        public Material ash;        // opaque grey unmade ground props
        public Material particle;   // motes
        public Material skybox;
        [Tooltip("Instancing-enabled alpha-cutout grass tuft materials (colour variations).")]
        public Material[] grass = new Material[0];
        [Tooltip("Instancing-enabled wildflower tuft materials.")]
        public Material[] flowers = new Material[0];
        [Tooltip("Overworld map image (from the Crulanda lore folder) shown as the world map.")]
        public Texture2D worldMap;
        [Tooltip("Crulanda/Water surface: rippling, reflective, with bank foam.")]
        public Material waterSurface;
        [Tooltip("Falling leaf particles (autumn colours come from the particle system).")]
        public Material leaf;
        [Tooltip("Falling ash flakes in ash zones.")]
        public Material ashFlake;
        [Tooltip("Splash spray and ripple rings.")]
        public Material splash;
        [Tooltip("Hidden/Crulanda/Post: bloom, sun shafts, tone map and grade (camera post-processing).")]
        public Material post;
        [Tooltip("Crulanda/Fade: what a tree turns into while it blocks the camera's view of the player (TreeFade).")]
        public Material fade;
        [Tooltip("Crulanda/Leaf painted leaf-cluster cards for broadleaf and orchard crowns: fresh green, yellow-green, autumn (ZoneBuilder maps its Leaf palette onto these and tints per tree).")]
        public Material[] leafCards = new Material[0];
        [Tooltip("Crulanda/Leaf painted pine-bough card (needle fronds) for pine tiers.")]
        public Material pineBough;
    }
}
