using System.Collections.Generic;
using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>An aura's ring (ArchivistKit.Aura): kept round its actor's body in world space and turned slowly, so it reads as a working.</summary>
    public sealed class AuraSpin : MonoBehaviour
    {
        public static readonly List<AuraSpin> All = new List<AuraSpin>();
        public Transform Follow; public float Height = 1;
        float turn;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public void Place() { if (Follow != null) { transform.position = Follow.position + Vector3.up * Height; transform.rotation = Quaternion.Euler(0, turn, 0); } }
        void LateUpdate() { if (Follow == null) { Destroy(gameObject); return; } turn += 60 * Time.deltaTime; Place(); }
    }
}