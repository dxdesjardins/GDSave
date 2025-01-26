using Godot;
using System;
using System.Collections.Generic;

namespace Chomp.Essentials;

public static class ExtensionsInput
{
    #region Key Extensions

    public static bool IsJustPressed(this InputEventKey v, Key key) {
        return v.Keycode == key && v.Pressed && !v.Echo;
    }

    public static bool IsJustReleased(this InputEventKey v, Key key) {
        return v.Keycode == key && !v.Pressed && !v.Echo;
    }

    /// <summary> Convert to a human readable key. </summary>
    public static string Readable(this InputEventKey v) {
        // If Keycode is not set than use PhysicalKeycode
        Key keyWithModifiers = v.Keycode == Key.None ? v.GetPhysicalKeycodeWithModifiers() : v.GetKeycodeWithModifiers();
        return OS.GetKeycodeString(keyWithModifiers).Replace("+", " + ");
    }

    #endregion

    #region Mouse Extensions

    static bool IsPressed(this InputEventMouseButton @event, MouseButton button) {
        return @event.ButtonIndex == button && @event.Pressed;
    }

    static bool IsReleased(this InputEventMouseButton @event, MouseButton button) {
        return @event.ButtonIndex == button && !@event.Pressed;
    }

    public static bool IsWheelUp(this InputEventMouseButton @event) => @event.IsPressed(MouseButton.WheelUp);
    public static bool IsWheelDown(this InputEventMouseButton @event) => @event.IsPressed(MouseButton.WheelDown);
    public static bool IsLeftClickPressed(this InputEventMouseButton @event) => @event.IsPressed(MouseButton.Left);
    public static bool IsLeftClickReleased(this InputEventMouseButton @event) => @event.IsReleased(MouseButton.Left);
    public static bool IsRightClickPressed(this InputEventMouseButton @event) => @event.IsPressed(MouseButton.Right);
    public static bool IsRightClickReleased(this InputEventMouseButton @event) => @event.IsReleased(MouseButton.Right);

    #endregion
}
