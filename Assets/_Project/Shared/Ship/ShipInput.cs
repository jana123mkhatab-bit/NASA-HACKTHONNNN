using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastSignal.Ship
{
    /// <summary>
    /// One frame of ship input. All axes go from -1 to 1.
    /// pitch +1 = nose up, yaw +1 = turn right, roll +1 = roll right, lift +1 = up.
    /// </summary>
    public struct ShipInputFrame
    {
        public float thrust;
        public float lift;
        public float yaw;
        public float pitch;
        public float roll;
        public bool stabilize;
        public bool boost;

        public bool IsIdle
        {
            get
            {
                return Mathf.Abs(thrust) < 0.01f && Mathf.Abs(lift) < 0.01f && Mathf.Abs(yaw) < 0.01f
                    && Mathf.Abs(pitch) < 0.01f && Mathf.Abs(roll) < 0.01f && !stabilize;
            }
        }

        public static ShipInputFrame Thrust(float throttle)
        {
            ShipInputFrame f = new ShipInputFrame();
            f.thrust = throttle;
            return f;
        }
    }

    /// <summary>
    /// The agreed input scheme (see SHIP_INTERFACE.md). Keyboard and gamepad are read together.
    /// Works with the Input System package and falls back to the legacy Input Manager.
    /// </summary>
    public static class ShipInput
    {
        public static bool InvertPitch;

        public static ShipInputFrame Read()
        {
            ShipInputFrame f = new ShipInputFrame();
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                f.thrust += Axis(kb.wKey.isPressed, kb.sKey.isPressed);
                f.yaw += Axis(kb.dKey.isPressed, kb.aKey.isPressed);
                f.pitch += Axis(kb.upArrowKey.isPressed, kb.downArrowKey.isPressed);
                f.roll += Axis(kb.eKey.isPressed, kb.qKey.isPressed);
                f.lift += Axis(kb.spaceKey.isPressed, kb.leftCtrlKey.isPressed || kb.cKey.isPressed);
                f.stabilize |= kb.xKey.isPressed;
                f.boost |= kb.leftShiftKey.isPressed;
            }
            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                f.thrust += pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue();
                f.yaw += stick.x;
                f.pitch += stick.y;
                f.roll += Axis(pad.rightShoulder.isPressed, pad.leftShoulder.isPressed);
                f.lift += Axis(pad.buttonNorth.isPressed, pad.buttonEast.isPressed);
                f.stabilize |= pad.buttonWest.isPressed;
                f.boost |= pad.leftStickButton.isPressed;
            }
#else
            f.thrust = Axis(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S));
            f.yaw = Axis(Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A));
            f.pitch = Axis(Input.GetKey(KeyCode.UpArrow), Input.GetKey(KeyCode.DownArrow));
            f.roll = Axis(Input.GetKey(KeyCode.E), Input.GetKey(KeyCode.Q));
            f.lift = Axis(Input.GetKey(KeyCode.Space), Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C));
            f.stabilize = Input.GetKey(KeyCode.X);
            f.boost = Input.GetKey(KeyCode.LeftShift);
#endif
            if (InvertPitch) f.pitch = -f.pitch;
            f.thrust = Mathf.Clamp(f.thrust, -1f, 1f);
            f.lift = Mathf.Clamp(f.lift, -1f, 1f);
            f.yaw = Mathf.Clamp(f.yaw, -1f, 1f);
            f.pitch = Mathf.Clamp(f.pitch, -1f, 1f);
            f.roll = Mathf.Clamp(f.roll, -1f, 1f);
            return f;
        }

        /// <summary>Fire / interact pressed this frame: F, left mouse, or gamepad A.</summary>
        public static bool FirePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
#endif
        }

        /// <summary>Number key 1..9 pressed this frame (used by menus such as triage). Returns 0 if none.</summary>
        public static int NumberPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return 0;
            if (kb.digit1Key.wasPressedThisFrame) return 1;
            if (kb.digit2Key.wasPressedThisFrame) return 2;
            if (kb.digit3Key.wasPressedThisFrame) return 3;
            if (kb.digit4Key.wasPressedThisFrame) return 4;
            if (kb.digit5Key.wasPressedThisFrame) return 5;
            if (kb.digit6Key.wasPressedThisFrame) return 6;
            if (kb.digit7Key.wasPressedThisFrame) return 7;
            if (kb.digit8Key.wasPressedThisFrame) return 8;
            if (kb.digit9Key.wasPressedThisFrame) return 9;
            return 0;
#else
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i)) return i;
            }
            return 0;
#endif
        }

        /// <summary>Enter / Return pressed this frame (confirm in menus).</summary>
        public static bool ConfirmPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
