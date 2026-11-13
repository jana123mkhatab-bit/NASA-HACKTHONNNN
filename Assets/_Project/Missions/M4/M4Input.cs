using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastSignal.M4
{
    /// <summary>Keyboard/mouse reads for M4 and the Final. Works with the new and the old input system.</summary>
    public static class M4Input
    {
        /// <summary>WASD / arrow keys as a -1..1 vector (x = right, y = forward).</summary>
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return v;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
#endif
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static bool ThrustHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.spaceKey.isPressed;
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        public static bool RunHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        public static bool InteractDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool SurveyDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.tabKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Tab);
#endif
        }

        public static bool LampDown()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; return k != null && k.fKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F);
#endif
        }

        /// <summary>Mouse movement while the right mouse button is held (camera orbit).</summary>
        public static Vector2 LookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null || !m.rightButton.isPressed) return Vector2.zero;
            return m.delta.ReadValue() * 0.1f;
#else
            if (!Input.GetMouseButton(1)) return Vector2.zero;
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2f;
#endif
        }

        /// <summary>Editor-only debug skips (F5..F8). Always false in a build.</summary>
        public static int DebugSkip()
        {
#if UNITY_EDITOR
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return 0;
            if (k.f5Key.wasPressedThisFrame) return 5;
            if (k.f6Key.wasPressedThisFrame) return 6;
            if (k.f7Key.wasPressedThisFrame) return 7;
            if (k.f8Key.wasPressedThisFrame) return 8;
#else
            if (Input.GetKeyDown(KeyCode.F5)) return 5;
            if (Input.GetKeyDown(KeyCode.F6)) return 6;
            if (Input.GetKeyDown(KeyCode.F7)) return 7;
            if (Input.GetKeyDown(KeyCode.F8)) return 8;
#endif
#endif
            return 0;
        }
    }
}
