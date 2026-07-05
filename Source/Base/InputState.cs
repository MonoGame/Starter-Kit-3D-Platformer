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

    public static MouseState MouseState;
    private static MouseState _previousMouseState;

    public static void Update()
    {
        _previousKeyboardState = _keyboardState;
        _previousGamePadState = GamepadState;
        _previousMouseState = MouseState;

        _keyboardState = Keyboard.GetState();
        GamepadState = GamePad.GetState(0);
        MouseState = Mouse.GetState();
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

    public static Vector2 MousePosition => new Vector2(MouseState.X, MouseState.Y);

    public static Vector2 MouseDelta => new Vector2(MouseState.X - _previousMouseState.X, MouseState.Y - _previousMouseState.Y);

    public static int ScrollWheelDelta => MouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;

    public static bool IsMouseButtonDown(MouseButton button)
    {
        return GetButtonState(MouseState, button) == ButtonState.Pressed;
    }

    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return GetButtonState(MouseState, button) == ButtonState.Pressed
            && GetButtonState(_previousMouseState, button) == ButtonState.Released;
    }

    public static bool IsMouseButtonReleased(MouseButton button)
    {
        return GetButtonState(MouseState, button) == ButtonState.Released
            && GetButtonState(_previousMouseState, button) == ButtonState.Pressed;
    }

    private static ButtonState GetButtonState(MouseState state, MouseButton button)
    {
        switch (button)
        {
            default:
                return state.LeftButton;
            case MouseButton.RightButton:
                return state.RightButton;
            case MouseButton.MiddleButton:
                return state.MiddleButton;
        }
    }
}
