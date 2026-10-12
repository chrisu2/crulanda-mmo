using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>Turns an aura's ring slowly, so it reads as a working, not a prop (ArchivistKit.Aura).</summary>
    public sealed class AuraSpin : MonoBehaviour { void Update() { transform.Rotate(0, 60 * Time.deltaTime, 0, Space.Self); } }
}
