// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

/// <summary>
/// This holds the current global input state for the game.
/// We do this to avoid multiple calls to Keyboard.GetState() and GamePad.GetState().
/// It also allows us to track button presses and releases.
/// Can be extended to support Mouse and Touch input as needed.
/// </summary>
public static class InputState
{
    private static KeyboardState _keyboardState;
    private static KeyboardState _previousKeyboardState;

    public static GamePadState GamepadState;
    private static GamePadState _previousGamePadState;

    public static void Update()
    {
        _previousKeyboardState = _keyboardState;
        _previousGamePadState = GamepadState;

        _keyboardState = Keyboard.GetState();
        GamepadState = GamePad.GetState(0);
    }

    public static bool IsButtonPressed(Buttons button)
    {
        return GamepadState.IsButtonDown(button) && !_previousGamePadState.IsButtonDown(button);
    }
    public static bool IsButtonHeld(Buttons button)
    {
        return GamepadState.IsButtonDown(button) && _previousGamePadState.IsButtonDown(button);
    }

    public static bool IsKeyPressed(Keys key)
    {
        return _keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyHeld(Keys key)
    {
        return _keyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyDown(key);
    }

    public static bool IsKeyDown(Keys key)
    {
        return _keyboardState.IsKeyDown(key);
    }
}
