using UnityEngine;

namespace Crulanda.Encounter
{
    /// <summary>
    /// The character sheet's paper doll is your own figure (Chris, 2026-10-04: "fix the paper doll in the character sheet so he looks
    /// like your character"; it was a few coloured blocks): while the sheet is open, a camera of its own stands in front of you and
    /// draws only your figure (its parts moved for that moment onto a layer nothing else uses), lit by a lamp of its own so it
    /// reads at night, into a texture the sheet shows. Your gear, hat, weapon and pose are what they are in the world.
    /// </summary>
    public sealed partial class EncounterHud
    {
        const int DollLayer = 30;
        Camera dollCam; Light dollLight; RenderTexture dollTex; float nextDoll;
        Renderer[] dollParts; int[] dollLayers;

        /// <summary>The figure drawn into the sheet a dozen times a second while it is open.</summary>
        void TickPaperDoll()
        {
            if (session == null || session.Player == null || !session.CharacterOpen || Hidden) return;
            if (Time.unscaledTime < nextDoll) return; nextDoll = Time.unscaledTime + 1f / 12;
            if (dollCam == null)
            {
                var go = new GameObject("Paper doll camera"); go.transform.SetParent(transform, false);
                dollCam = go.AddComponent<Camera>(); dollCam.enabled = false; dollCam.cullingMask = 1 << DollLayer;
                dollCam.clearFlags = CameraClearFlags.SolidColor; dollCam.backgroundColor = new Color(0, 0, 0, 0); dollCam.fieldOfView = 28; dollCam.nearClipPlane = .1f; dollCam.farClipPlane = 20;
                dollCam.allowHDR = false; dollCam.allowMSAA = true;
                dollLight = new GameObject("Paper doll lamp").AddComponent<Light>(); dollLight.transform.SetParent(go.transform, false);
                dollLight.type = LightType.Directional; dollLight.intensity = 1.1f; dollLight.color = new Color(1, .95f, .86f); dollLight.cullingMask = 1 << DollLayer; dollLight.enabled = false;
                dollLight.transform.localRotation = Quaternion.Euler(20, -25, 0);
                dollTex = new RenderTexture(560, 700, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Paper doll" };
                dollCam.targetTexture = dollTex;
            }
            var t = session.Player.transform;
            var faces = t.forward; faces.y = 0; faces = faces.sqrMagnitude > .01f ? faces.normalized : Vector3.forward;
            // Framed on the figure itself (its parts' bounds, the weapon left out of the height), head to toe with a little room.
            dollParts = session.Player.GetComponentsInChildren<Renderer>();
            var box = new Bounds(t.position, Vector3.zero); bool any = false;
            foreach (var r in dollParts)
            {
                if (r == null || !r.enabled || !(r is SkinnedMeshRenderer)) continue;
                if (!any) { box = r.bounds; any = true; } else box.Encapsulate(r.bounds);
            }
            if (!any) box = new Bounds(t.position + Vector3.up * .9f, new Vector3(.6f, 1.8f, .4f));
            // A skinned mesh's bounds are loose (the doll came out small, Chris 2026-10-05): framed on the skeleton when there is one,
            // feet to the top of the head (and a hat), filling the frame.
            var anim = session.Player.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman && anim.GetBoneTransform(HumanBodyBones.Head) != null && anim.GetBoneTransform(HumanBodyBones.LeftFoot) != null)
            {
                float head = anim.GetBoneTransform(HumanBodyBones.Head).position.y + .3f;
                float feet = Mathf.Min(anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y, anim.GetBoneTransform(HumanBodyBones.RightFoot).position.y) - .1f;
                var c = anim.GetBoneTransform(HumanBodyBones.Hips).position; box = new Bounds(new Vector3(c.x, (head + feet) / 2, c.z), new Vector3(.6f, head - feet, .4f));
            }
            float half = box.size.y * .53f, dist = half / Mathf.Tan(dollCam.fieldOfView * .5f * Mathf.Deg2Rad);
            var aim = box.center;
            dollCam.transform.position = aim + faces * dist + Vector3.up * .15f; dollCam.transform.LookAt(aim);
            // Only your figure: its parts to the doll's layer for the one render, then back.
            if (dollLayers == null || dollLayers.Length < dollParts.Length) dollLayers = new int[dollParts.Length * 2];
            for (int i = 0; i < dollParts.Length; i++) { dollLayers[i] = dollParts[i].gameObject.layer; dollParts[i].gameObject.layer = DollLayer; }
            var fog = RenderSettings.fog; RenderSettings.fog = false; dollLight.enabled = true;
            dollCam.Render();
            dollLight.enabled = false; RenderSettings.fog = fog;
            for (int i = 0; i < dollParts.Length; i++) if (dollParts[i] != null) dollParts[i].gameObject.layer = dollLayers[i];
        }
        /// <summary>The doll in the sheet's middle (the old block figure when there is none yet).</summary>
        bool DrawPaperDoll(Rect r)
        {
            if (dollTex == null) return false;
            float h = r.height, w = h * dollTex.width / dollTex.height;
            GUI.DrawTexture(new Rect(r.center.x - w / 2, r.y, w, h), dollTex, ScaleMode.ScaleToFit, true);
            return true;
        }
        void OnDestroy() { if (dollTex != null) dollTex.Release(); }
    }
}
