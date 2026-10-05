using UnityEngine;

namespace Crulanda.World
{
    /// <summary>
    /// A treasure chest's lid (quiArt's Animated PBR Chest, Asset Store; Resources/Props/Chest_Wood): the pack's own clips played by
    /// hand, so the demo's controller (which opens it at once and again) never runs. Closed, it holds the idle clip's first frame;
    /// <see cref="Open"/> plays the opening once and holds its last frame; <see cref="Close"/> shuts it again (the chest has filled).
    /// </summary>
    public sealed class ChestLid : MonoBehaviour
    {
        AnimationClip idle, opening; GameObject model; float time; bool open;
        public bool IsOpen { get { return open; } }
        public void Init(GameObject chest)
        {
            model = chest;
            var a = chest.GetComponentInChildren<Animator>(true);
            if (a != null)
            {
                if (a.runtimeAnimatorController != null)
                    foreach (var c in a.runtimeAnimatorController.animationClips)
                    {
                        if (c.name.Contains("Idle")) idle = c;
                        if (c.name.Contains("Opening")) opening = c;
                    }
                a.runtimeAnimatorController = null; a.enabled = false;
            }
            Close();
        }
        public void Open() { open = true; time = 0; enabled = true; }
        public void Close() { open = false; if (idle != null && model != null) idle.SampleAnimation(model, 0); enabled = false; }
        void Update()
        {
            if (!open || opening == null || model == null) { enabled = false; return; }
            time += Time.deltaTime; opening.SampleAnimation(model, Mathf.Min(time, opening.length));
            if (time >= opening.length) enabled = false;
        }
    }
}
