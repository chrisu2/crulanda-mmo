using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Crulanda.Encounter
{
    // Keyboard bindings are centralized here; supports an already-open editor before its input backend restarts.
    public static class EncounterInput
    {
        /// <summary>Action-bar key for a slot: 1-9 then 0 (slot index 9), matching the HUD labels.</summary>
        public static KeyCode SlotKey(int slot) { return slot == 10 ? KeyCode.Minus : slot == 9 ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha1 + slot); }
        public static string SlotLabel(int slot) { return slot == 10 ? "-" : slot == 9 ? "0" : (slot + 1).ToString(); }
        /// <summary>True while you type in the chat (EncounterHud.DrawChat): no key moves you or works the bar.</summary>
        public static bool Typing;
        public static bool Press(KeyCode key)
        {
            if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            if (KeyBindings.Stands(key, out var game)) return KeyBindings.Pressed(game);   // a rebindable key (playtest note 75)
            switch (key) {
                case KeyCode.Tab: return k.tabKey.wasPressedThisFrame;
                case KeyCode.E: return k.eKey.wasPressedThisFrame;
                case KeyCode.R: return k.rKey.wasPressedThisFrame;
                case KeyCode.B: return k.bKey.wasPressedThisFrame;
                case KeyCode.M: return k.mKey.wasPressedThisFrame;
                case KeyCode.I: return k.iKey.wasPressedThisFrame;
                case KeyCode.K: return k.kKey.wasPressedThisFrame;
                case KeyCode.L: return k.lKey.wasPressedThisFrame;
                case KeyCode.O: return k.oKey.wasPressedThisFrame;   // the who list
                case KeyCode.C: return k.cKey.wasPressedThisFrame;
                case KeyCode.Escape: return k.escapeKey.wasPressedThisFrame;
                case KeyCode.F5: return k.f5Key.wasPressedThisFrame;
                case KeyCode.F8: return k.f8Key.wasPressedThisFrame;
                case KeyCode.F9: return k.f9Key.wasPressedThisFrame;
                case KeyCode.F10: return k.f10Key.wasPressedThisFrame;
                case KeyCode.F11: return k.f11Key.wasPressedThisFrame;
                case KeyCode.Alpha1: return k.digit1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return k.digit2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return k.digit3Key.wasPressedThisFrame;
                case KeyCode.Alpha4: return k.digit4Key.wasPressedThisFrame;
                case KeyCode.Alpha5: return k.digit5Key.wasPressedThisFrame;
                case KeyCode.Alpha6: return k.digit6Key.wasPressedThisFrame;
                case KeyCode.Alpha7: return k.digit7Key.wasPressedThisFrame;
                case KeyCode.Alpha8: return k.digit8Key.wasPressedThisFrame;
                case KeyCode.Alpha9: return k.digit9Key.wasPressedThisFrame;
                case KeyCode.Alpha0: return k.digit0Key.wasPressedThisFrame;
                case KeyCode.Minus: return k.minusKey.wasPressedThisFrame;   // the eleventh slot (2026-10-08)
                case KeyCode.Space: return k.spaceKey.wasPressedThisFrame;
                case KeyCode.Slash: return k.slashKey.wasPressedThisFrame || k.numpadDivideKey.wasPressedThisFrame;   // the run toggle
            }
            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }
        /// <summary>Held Ctrl: sneak.</summary>
        public static bool Sneak
        {
            get {
                if (Typing) return false;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current; return k != null && (k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed);
#else
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
            }
        }
        /// <summary>Running on by itself (the Auto-run key) until forward, back or the key again.</summary>
        public static bool AutoRunning; static int autoFrame = -1;
        public static Vector2 Move
        {
            get {
                if (Typing) return Vector2.zero;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                // Auto-run (Chris, 2026-10-08): its key toggles it; forward or back takes the walking back into your hands.
                if (KeyBindings.Pressed(GameKey.AutoRun) && Time.frameCount != autoFrame) { autoFrame = Time.frameCount; AutoRunning = !AutoRunning; }   // once a frame, however often Move is read
                bool fwd = KeyBindings.Held(GameKey.Forward), back = KeyBindings.Held(GameKey.Back);
                if (fwd || back) AutoRunning = false;
                return new Vector2((KeyBindings.Held(GameKey.Right) ? 1 : 0) - (KeyBindings.Held(GameKey.Left) ? 1 : 0),
                    (fwd || AutoRunning ? 1 : 0) - (back ? 1 : 0));   // the bound keys (note 75)
#else
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
            }
        }
        public static Vector2 Pointer { get {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current == null ? Vector2.zero : Mouse.current.position.ReadValue();
#else
            return Input.mousePosition;
#endif
        } }
        public static bool Orbit { get {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
            return Input.GetMouseButton(1);
#endif
        } }
        public static bool Click { get {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        } }
        public static float Zoom { get {
#if ENABLE_INPUT_SYSTEM
            // A notch is 1 under the Input System's uniform scroll (1.20's default) and 120 under the old Windows units: either is
            // one step (playtest note 18: the wheel moved the camera about a centimetre a notch).
            if (Mouse.current == null) return 0; float y = Mouse.current.scroll.ReadValue().y;
            return Mathf.Abs(y) > 10 ? y / 120f : y;
#else
            return Input.mouseScrollDelta.y;
#endif
        } }
    }
}




