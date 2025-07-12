// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Camera
{
    private GraphicsDevice _device;
    private Matrix _viewMatrix;
    private Matrix _projectionMatrix;

    private Vector3 _smoothedTarget;

    // Do we warp the camera into position this frame.
    private bool _warp = true;

    // Camera positioning properties
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 UpDirection { get; set; } = Vector3.Up;
    
    // Camera rotation and zoom properties
    public float Yaw { get; private set; } = 0f;
    public float Pitch { get; private set; } = 0.5f; // Slight downward angle
    public float Distance { get; private set; } = 600f;
    public float MinDistance { get; set; } = 300f;
    public float MaxDistance { get; set; } = 1000f;

    public float MinPitch { get; set; } = 0.08f;
    public float MaxPitch { get; set; } = MathHelper.PiOver2 - 0.1f;

    // Input sensitivity
    public float TargetSpeed { get; set; } = 4.0f;
    public float RotationSpeed { get; set; } = 0.05f;
    public float ZoomSpeed { get; set; } = 5f;
    
    // Camera direction vector
    public Vector3 ForwardDirection => Vector3.Normalize(Target - Position);
    
    // Previous input states
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamePadState;
    private Vector2 _cameraRotation = Vector2.Zero;

    public Matrix ViewMatrix => _viewMatrix;
    public Matrix ProjectionMatrix => _projectionMatrix;

    public Camera(GraphicsDevice graphicsDevice)
    {
        _device = graphicsDevice;
        _previousKeyboardState = Keyboard.GetState();
        _previousGamePadState = GamePad.GetState(PlayerIndex.One);
        UpdateViewMatrix();
        UpdateProjectionMatrix();
    }

    public void RotateCamera(float yawChange, float pitchChange)
    {
        Yaw += yawChange;
        Pitch += pitchChange;
        
        // Constrain pitch to prevent flipping
        Pitch = MathHelper.Clamp(Pitch, MinPitch, MaxPitch);
        
        // Keep yaw within 0 to 2π range
        if (Yaw > MathHelper.TwoPi)
            Yaw -= MathHelper.TwoPi;
        else if (Yaw < 0)
            Yaw += MathHelper.TwoPi;
    }

    public void Zoom(float amount)
    {
        Distance -= amount * ZoomSpeed;
        Distance = MathHelper.Clamp(Distance, MinDistance, MaxDistance);
    }
    
    public void Update(GameTime gameTime)
    {
        HandleInput(gameTime);
        UpdateCameraPosition(gameTime);
        UpdateViewMatrix();
        _warp = false;
    }
    
    private void HandleInput(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
        // Handle gamepad rotation
        Vector2 rightStick = gamePadState.ThumbSticks.Right;
        // Handle keyboard rotation
        if (keyboardState.IsKeyDown(Keys.Left))
            rightStick.X = Math.Clamp(rightStick.X - 0.01f, -1f, 1f);
        if (keyboardState.IsKeyDown(Keys.Right))
            rightStick.X = Math.Clamp(rightStick.X + 0.01f, -1f, 1f);
        if (keyboardState.IsKeyDown(Keys.Up))
            rightStick.Y = Math.Clamp(rightStick.Y + 0.01f, -1f, 1f);
        if (keyboardState.IsKeyDown(Keys.Down))
            rightStick.Y = Math.Clamp(rightStick.Y - 0.01f, -1f, 1f);

        if (rightStick != Vector2.Zero)
        {
            // Apply rotation based on right stick input
            _cameraRotation.X = MathHelper.Clamp(_cameraRotation.X + rightStick.X * (float)gameTime.ElapsedGameTime.TotalMilliseconds, -RotationSpeed, RotationSpeed);
            _cameraRotation.Y = MathHelper.Clamp(_cameraRotation.Y + rightStick.Y * (float)gameTime.ElapsedGameTime.TotalMilliseconds, -RotationSpeed, RotationSpeed);
        }

        if (_cameraRotation != Vector2.Zero)
        {
            RotateCamera(-_cameraRotation.X, _cameraRotation.Y);
            _cameraRotation.X = MathHelper.Lerp(_cameraRotation.X, 0f, RotationSpeed * 5f);
            _cameraRotation.Y = MathHelper.Lerp(_cameraRotation.Y, 0f, RotationSpeed * 5f);
        }
        
        // Handle zoom (example using keyboard)
        if (keyboardState.IsKeyDown(Keys.OemComma))
            Zoom(1f);
        if (keyboardState.IsKeyDown(Keys.OemPeriod))
            Zoom(-1f);
            
        // Handle zoom with gamepad triggers
        float triggerDifference = gamePadState.Triggers.Left - gamePadState.Triggers.Right;
        if (triggerDifference != 0)
            Zoom(triggerDifference);
            
        _previousKeyboardState = keyboardState;
        _previousGamePadState = gamePadState;
    }
    
    private void UpdateCameraPosition(GameTime gameTime)
    {

        // Calculate orbit position based on spherical coordinates
        // In this coordinate system:
        // - Yaw rotates around the Y axis (horizontal orbit)
        // - Pitch controls the height (vertical orbit angle)
        // - Distance controls how far from the target (zoom)
        float x = Distance * (float)System.Math.Sin(MathHelper.PiOver2 - Pitch) * (float)System.Math.Cos(Yaw);
        float z = Distance * (float)System.Math.Sin(MathHelper.PiOver2 - Pitch) * (float)System.Math.Sin(Yaw);
        float y = Distance * (float)System.Math.Cos(MathHelper.PiOver2 - Pitch);

        if (_warp)
            _smoothedTarget = Target;
        else
        {
            // Don't instantly move to the target.
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var speed = MathHelper.Clamp(deltaTime * TargetSpeed, 0.0f, 1.0f);
            _smoothedTarget = Vector3.Lerp(_smoothedTarget, Target, speed);
        }

        // Set camera position relative to target (orbit point)
        Position = _smoothedTarget + new Vector3(x, y, z);
    }

    public void UpdateViewMatrix()
    {
        _viewMatrix = Matrix.CreateLookAt(Position, _smoothedTarget, UpDirection);
    }

    public void UpdateProjectionMatrix()
    {
        float aspectRatio = (float)_device.Viewport.Width / _device.Viewport.Height;
        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45f), aspectRatio, 0.1f, 5000f);
    }
}