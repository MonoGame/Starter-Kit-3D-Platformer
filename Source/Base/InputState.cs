// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

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

    public static TouchCollection TouchState;
    private static TouchCollection _previousTouchState;

    private static Viewport _viewport;
    private static Matrix _inputTransformation = Matrix.Identity;
    private static bool _hasInputTransformation;

    public static void Update()
    {
        _previousKeyboardState = _keyboardState;
        _previousGamePadState = GamepadState;
        _previousMouseState = MouseState;
        _previousTouchState = TouchState;

        _keyboardState = Keyboard.GetState();
        GamepadState = GamePad.GetState(0);
        MouseState = Mouse.GetState();
        TouchState = TouchPanel.GetState();
    }

    public static bool IsTouchSupported => TouchPanel.GetCapabilities().IsConnected;

    public static TouchCollection PreviousTouchState => _previousTouchState;

    public static void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    public static void UpdateInputTransformation(Matrix transform)
    {
        _inputTransformation = transform;
        _hasInputTransformation = true;
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

    public static Vector2 UiMousePosition => ScreenToUi(MousePosition);

    public static Vector2 MouseDelta => new Vector2(MouseState.X - _previousMouseState.X, MouseState.Y - _previousMouseState.Y);

    public static int ScrollWheelDelta => MouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;

    public static Vector2 ScreenToUi(Vector2 screenPosition)
    {
        if (_hasInputTransformation)
        {
            return Vector2.Transform(screenPosition, _inputTransformation);
        }

        if (_viewport.Width <= 0 || _viewport.Height <= 0)
        {
            return screenPosition;
        }

        return new Vector2(
            screenPosition.X * GameConstants.BASE_RESOLUTION_WIDTH / _viewport.Width,
            screenPosition.Y * GameConstants.BASE_RESOLUTION_HEIGHT / _viewport.Height);
    }

    public static Vector2 TouchToUi(Vector2 touchPosition)
    {
        int displayWidth = TouchPanel.DisplayWidth;
        int displayHeight = TouchPanel.DisplayHeight;
        if (displayWidth > 0 && displayHeight > 0 && _viewport.Width > 0 && _viewport.Height > 0)
        {
            if (displayWidth != _viewport.Width || displayHeight != _viewport.Height)
            {
                touchPosition = new Vector2(
                    touchPosition.X * _viewport.Width / displayWidth,
                    touchPosition.Y * _viewport.Height / displayHeight);
            }
        }

        return ScreenToUi(touchPosition);
    }

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
