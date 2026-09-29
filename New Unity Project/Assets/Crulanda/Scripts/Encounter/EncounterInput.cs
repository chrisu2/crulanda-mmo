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
        public static KeyCode SlotKey(int slot) { return slot == 9 ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha1 + slot); }
        public static string SlotLabel(int slot) { return slot == 9 ? "0" : (slot + 1).ToString(); }
        public static bool Press(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key) {
                case KeyCode.Tab: return k.tabKey.wasPressedThisFrame;
                case KeyCode.E: return k.eKey.wasPressedThisFrame;
                case KeyCode.R: return k.rKey.wasPressedThisFrame;
                case KeyCode.B: return k.bKey.wasPressedThisFrame;
                case KeyCode.M: return k.mKey.wasPressedThisFrame;
                case KeyCode.I: return k.iKey.wasPressedThisFrame;
                case KeyCode.L: return k.lKey.wasPressedThisFrame;
                case KeyCode.C: return k.cKey.wasPressedThisFrame;
                case KeyCode.Escape: return k.escapeKey.wasPressedThisFrame;
                case KeyCode.F5: return k.f5Key.wasPressedThisFrame;
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
                case KeyCode.Space: return k.spaceKey.wasPressedThisFrame;
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
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current; return k != null && (k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed);
#else
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
            }
        }
        public static Vector2 Move
        {
            get {
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                return new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
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
            return Mouse.current == null ? 0 : Mouse.current.scroll.ReadValue().y / 120f;
#else
            return Input.mouseScrollDelta.y;
#endif
        } }
    }
}




