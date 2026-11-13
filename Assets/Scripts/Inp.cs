using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Input wrapper that works with the legacy Input Manager and with the new Input System package.</summary>
public enum K { W, A, S, D, Q, E, R, F, G, H, L, M, T, V, X, Z, C, B, J, Space, Shift, Ctrl, Esc, Tab, Enter, Up, Down, Left, Right, Comma, Period, N1, N2, N3, N4, F1, Alt }

public static class Inp
{
    public static bool Held(K k)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; if (kb == null) return false;
        var c = Map(kb, k); return c != null && c.isPressed;
#else
        return Input.GetKey(Code(k));
#endif
    }
    public static bool Down(K k)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; if (kb == null) return false;
        var c = Map(kb, k); return c != null && c.wasPressedThisFrame;
#else
        return Input.GetKeyDown(Code(k));
#endif
    }
    public static Vector2 MouseDelta
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.5f : Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 8f;
#endif
        }
    }
    public static Vector2 MousePos
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
    public static float Scroll
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }
    }
    public static bool MouseHeld(int b)
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; if (m == null) return false;
        return b == 0 ? m.leftButton.isPressed : b == 1 ? m.rightButton.isPressed : m.middleButton.isPressed;
#else
        return Input.GetMouseButton(b);
#endif
    }
    public static bool MouseDown(int b)
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; if (m == null) return false;
        return b == 0 ? m.leftButton.wasPressedThisFrame : b == 1 ? m.rightButton.wasPressedThisFrame : m.middleButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(b);
#endif
    }
    public static float Axis(K neg, K pos) { return (Held(pos) ? 1f : 0f) - (Held(neg) ? 1f : 0f); }

#if ENABLE_INPUT_SYSTEM
    static UnityEngine.InputSystem.Controls.KeyControl Map(Keyboard kb, K k)
    {
        switch (k)
        {
            case K.W: return kb.wKey; case K.A: return kb.aKey; case K.S: return kb.sKey; case K.D: return kb.dKey;
            case K.Q: return kb.qKey; case K.E: return kb.eKey; case K.R: return kb.rKey; case K.F: return kb.fKey;
            case K.G: return kb.gKey; case K.H: return kb.hKey; case K.L: return kb.lKey; case K.M: return kb.mKey;
            case K.T: return kb.tKey; case K.V: return kb.vKey; case K.X: return kb.xKey; case K.Z: return kb.zKey;
            case K.C: return kb.cKey; case K.B: return kb.bKey; case K.J: return kb.jKey;
            case K.Space: return kb.spaceKey; case K.Shift: return kb.leftShiftKey; case K.Ctrl: return kb.leftCtrlKey;
            case K.Esc: return kb.escapeKey; case K.Tab: return kb.tabKey; case K.Enter: return kb.enterKey;
            case K.Up: return kb.upArrowKey; case K.Down: return kb.downArrowKey; case K.Left: return kb.leftArrowKey; case K.Right: return kb.rightArrowKey;
            case K.Comma: return kb.commaKey; case K.Period: return kb.periodKey;
            case K.N1: return kb.digit1Key; case K.N2: return kb.digit2Key; case K.N3: return kb.digit3Key; case K.N4: return kb.digit4Key;
            case K.F1: return kb.f1Key; case K.Alt: return kb.leftAltKey;
        }
        return null;
    }
#else
    static KeyCode Code(K k)
    {
        switch (k)
        {
            case K.W: return KeyCode.W; case K.A: return KeyCode.A; case K.S: return KeyCode.S; case K.D: return KeyCode.D;
            case K.Q: return KeyCode.Q; case K.E: return KeyCode.E; case K.R: return KeyCode.R; case K.F: return KeyCode.F;
            case K.G: return KeyCode.G; case K.H: return KeyCode.H; case K.L: return KeyCode.L; case K.M: return KeyCode.M;
            case K.T: return KeyCode.T; case K.V: return KeyCode.V; case K.X: return KeyCode.X; case K.Z: return KeyCode.Z;
            case K.C: return KeyCode.C; case K.B: return KeyCode.B; case K.J: return KeyCode.J;
            case K.Space: return KeyCode.Space; case K.Shift: return KeyCode.LeftShift; case K.Ctrl: return KeyCode.LeftControl;
            case K.Esc: return KeyCode.Escape; case K.Tab: return KeyCode.Tab; case K.Enter: return KeyCode.Return;
            case K.Up: return KeyCode.UpArrow; case K.Down: return KeyCode.DownArrow; case K.Left: return KeyCode.LeftArrow; case K.Right: return KeyCode.RightArrow;
            case K.Comma: return KeyCode.Comma; case K.Period: return KeyCode.Period;
            case K.N1: return KeyCode.Alpha1; case K.N2: return KeyCode.Alpha2; case K.N3: return KeyCode.Alpha3; case K.N4: return KeyCode.Alpha4;
            case K.F1: return KeyCode.F1; case K.Alt: return KeyCode.LeftAlt;
        }
        return KeyCode.None;
    }
#endif
}
