using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum K { Up, Down, Left, Right, Confirm, Interact, Pause, Restart, Menu }

// Entrada unificada: funciona com o Input System novo ou o antigo.
public static class Inp
{
    public static Vector2 Move()
    {
        Vector2 v;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        v.x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        v.y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
#else
        v.x = Input.GetAxisRaw("Horizontal");
        v.y = Input.GetAxisRaw("Vertical");
#endif
        return Vector2.ClampMagnitude(v, 1f);
    }

    public static bool Pressed(K key)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return false;
        switch (key)
        {
            case K.Up: return k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame;
            case K.Down: return k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame;
            case K.Left: return k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame;
            case K.Right: return k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame;
            case K.Confirm: return k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame;
            case K.Interact: return k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
            case K.Pause: return k.escapeKey.wasPressedThisFrame;
            case K.Restart: return k.rKey.wasPressedThisFrame;
            case K.Menu: return k.mKey.wasPressedThisFrame;
        }
        return false;
#else
        switch (key)
        {
            case K.Up: return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            case K.Down: return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            case K.Left: return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            case K.Right: return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            case K.Confirm: return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
            case K.Interact: return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
            case K.Pause: return Input.GetKeyDown(KeyCode.Escape);
            case K.Restart: return Input.GetKeyDown(KeyCode.R);
            case K.Menu: return Input.GetKeyDown(KeyCode.M);
        }
        return false;
#endif
    }
}
