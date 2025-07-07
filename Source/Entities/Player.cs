// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

public class Player : AnimatedEntity
{
    public bool IsJumping = false;
    public bool IsFalling = true;

    public int Score = 0;
    public Vector3 Forward { get; set; } = new Vector3(0, 0, -1); // Default forward is negative Z

    public bool IsMoving
    {
        get
        {
            return _moveDirection != Vector3.Zero;
        }
    }

    private Vector3 _velocity;
    private KeyboardState _previousKeyboardState;
    private GamePadState _previousGamePadState;
    private int _jumpCount = 0;
    private int _maxJumps = 2; // Allow double jump
    private Vector3 _moveDirection = Vector3.Zero;
    private float _targetRotationAngle = 1.5f; // The angle we want to rotate towards
    private float _currentRotationAngle = 1.5f; // Current rotation angle, start facing the player
    private SoundEffect _jumpSound;
    private SoundEffectInstance _landSound;
    private SoundEffectInstance _walkSound;
    private Texture2D _shadowTexture;
    private float _maxShadowDistance = 200f; // Maximum distance to cast ray for shadow
    private Vector3 _shadowPosition = Vector3.Zero;
    private float _shadowSize = 10.0f; // Base shadow size
    private float _minShadowSize = 10.0f; // Minimum shadow size when far away
    private float _maxShadowSize = 5.0f; // Maximum shadow size when close
    private float _currentShadowDistance = 0f; // Current distance to the surface below

    // Used to do effects when the player lands from a fall/jump.
    private float _landVelocity = 0.0f;

    // Used to animate the player scale during jumps, falls, and landings.
    private Vector3 _scaleAnimation = Vector3.One;

    // 3D shadow quad resources
    private VertexBuffer _shadowVertexBuffer;
    private IndexBuffer _shadowIndexBuffer;
    private BasicEffect _shadowEffect;

    private GraphicsDevice _graphicsDevice;

    public Player(GraphicsDevice graphicsDevice, Model model, ContentManager contentManager) : base(model, contentManager)
    {
        _collisionMesh.GenerateFromCylinder(new Vector3(0, 40, 0), 30, 80, 8);

        Position = new Vector3(0, 0, 0);
        Scale = new Vector3(1, 1, 1);
        Rotation = Quaternion.Identity;
        _previousKeyboardState = Keyboard.GetState();
        _graphicsDevice = graphicsDevice;
        // Initialize 3D shadow resources
        InitializeShadowQuad();
    }

    override protected void LoadContent()
    {
        // Load any additional content here
        base.LoadContent();
        _jumpSound = Content.Load<SoundEffect>("Sounds/jump");
        _landSound = Content.Load<SoundEffect>("Sounds/land").CreateInstance();
        _walkSound = Content.Load<SoundEffect>("Sounds/walking").CreateInstance();
        _shadowTexture = Content.Load<Texture2D>("Textures/blob_shadow");
    }

    private void InitializeShadowQuad()
    {
        // Create a BasicEffect for the shadow quad
        _shadowEffect = new BasicEffect(_graphicsDevice);
        _shadowEffect.TextureEnabled = true;
        _shadowEffect.Texture = _shadowTexture;

        // Enable alpha blending for transparency
        _shadowEffect.Alpha = 0.5f;

        // Create a quad on the X/Z plane
        VertexPositionTexture[] vertices = new VertexPositionTexture[4];
        float halfSize = _shadowSize / 2.0f;

        // Create quad vertices on the XZ plane (Y = 0)
        vertices[0] = new VertexPositionTexture(new Vector3(-halfSize, 0, -halfSize), new Vector2(0, 0));
        vertices[1] = new VertexPositionTexture(new Vector3(halfSize, 0, -halfSize), new Vector2(1, 0));
        vertices[2] = new VertexPositionTexture(new Vector3(-halfSize, 0, halfSize), new Vector2(0, 1));
        vertices[3] = new VertexPositionTexture(new Vector3(halfSize, 0, halfSize), new Vector2(1, 1));

        // Create vertex buffer
        _shadowVertexBuffer = new VertexBuffer(
            _graphicsDevice,
            typeof(VertexPositionTexture),
            4,
            BufferUsage.WriteOnly);
        _shadowVertexBuffer.SetData(vertices);

        // Create index buffer (two triangles forming a quad)
        short[] indices = { 0, 1, 2, 2, 1, 3 };
        _shadowIndexBuffer = new IndexBuffer(
            _graphicsDevice,
            IndexElementSize.SixteenBits,
            6,
            BufferUsage.WriteOnly);
        _shadowIndexBuffer.SetData(indices);
    }

    public override bool CheckCollision(Entity other)
    {
        bool collision = base.CheckCollision(other);
        if (collision)
        {
            if (!other.IsBlockingMovement)
            {
                // If the other entity is not blocking movement, we can ignore the collision
                return false;
            }

            // Calculate centers of both bounding boxes
            Vector3 thisCenter = (BoundingBox.Min + BoundingBox.Max) / 2;
            Vector3 otherCenter = (other.BoundingBox.Min + other.BoundingBox.Max) / 2;

            // Calculate penetration depth in all directions
            float overlapX = Math.Min(
                BoundingBox.Max.X - other.BoundingBox.Min.X,
                other.BoundingBox.Max.X - BoundingBox.Min.X);

            float overlapY = Math.Min(
                BoundingBox.Max.Y - other.BoundingBox.Min.Y,
                other.BoundingBox.Max.Y - BoundingBox.Min.Y);

            float overlapZ = Math.Min(
                BoundingBox.Max.Z - other.BoundingBox.Min.Z,
                other.BoundingBox.Max.Z - BoundingBox.Min.Z);

            //Determine the direction of least penetration
            Vector3 resolveDirection = Vector3.Zero;

            //The smallest overlap indicates the most efficient resolution direction
            if (overlapX < overlapY && overlapX < overlapZ)
            {
                // X-axis collision
                resolveDirection.X = (thisCenter.X < otherCenter.X) ? -overlapX : overlapX;
            }
            else if (overlapY < overlapZ)
            {
                // Y-axis collision
                resolveDirection.Y = (thisCenter.Y < otherCenter.Y) ? -overlapY : overlapY;

                // If we're resolving upward, we're standing on something
                if (resolveDirection.Y > 0)
                {
                    if (IsJumping)
                    {
                        _landSound.Play();
                        _landVelocity = Math.Max(-_velocity.Y, 0.0f);
                        IsJumping = false;
                    }

                    _velocity.Y = 0;
                    _jumpCount = 0; // Reset jump count when landing
                    _currentShadowDistance = 0f; // Reset shadow distance when landing                 
                    IsFalling = false;
                }
                // If we're hitting our head on something
                else if (resolveDirection.Y < 0 && _velocity.Y > 0)
                {
                    _velocity.Y = 0;
                }
            }
            else
            {
                // Z-axis collision
                resolveDirection.Z = (thisCenter.Z < otherCenter.Z) ? -overlapZ : overlapZ;
            }

            // Apply the resolution vector with a small buffer to prevent sticking
            Position += resolveDirection * 1.01f;

            // Update the entity's world matrix and bounding box immediately to prevent
            // further collision detection issues in the same frame
            WorldMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);
        }
        // If we're standing on a platform, show shadow
        if (other is Platform)
        {
            // Cast a ray downward to find the exact surface point
            Vector3 rayStart = Position;
            Vector3 rayDirection = Vector3.Down;

            if (IsJumping && RayIntersectsEntity(rayStart, rayDirection, other, out float distance))
            {
                _shadowPosition = rayStart + rayDirection * distance;
                _currentShadowDistance = distance; // Store the distance for shadow sizing
            }
        }

        if (_velocity.Y < 0.0f)
            IsFalling = true;

        return collision;
    }

    private bool RayIntersectsEntity(Vector3 rayOrigin, Vector3 rayDirection, Entity entity, out float distance)
    {
        // Ray-box intersection test
        distance = 0f;
        Ray ray = new Ray(rayOrigin, rayDirection);
        var bb = entity.BoundingBox;
        var d = ray.Intersects(bb);
        if (!d.HasValue)
            return false;
        distance = d.Value;
        return distance <= _maxShadowDistance;
    }

    public void AddForce(Vector3 force)
    {
        _velocity += force;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Get current keyboard state
        KeyboardState currentKeyboardState = Keyboard.GetState();
        GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

        var jump = (_previousGamePadState.Buttons.A == ButtonState.Released && gamePadState.Buttons.A == ButtonState.Pressed) || (currentKeyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space));
        var jumpHeld = gamePadState.Buttons.A == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Space);

        // Calculate the right vector based on forward (cross product with up)
        Vector3 right = Vector3.Cross(Vector3.Up, Forward);
        right = Vector3.Normalize(right);

        // Normalize Forward to ensure it's a unit vector
        Vector3 forward = Vector3.Normalize(Forward);

        // Process movement inputs and convert to world-relative movement
        _moveDirection = Vector3.Zero;

        var thumbstickLeft = gamePadState.ThumbSticks.Left;
        if (thumbstickLeft.LengthSquared() > 0)
        {
            // Movement along the right vector
            _moveDirection -= right * thumbstickLeft.X;
            // Movement along the forward vector
            _moveDirection += forward * thumbstickLeft.Y;
        }

        if (currentKeyboardState.IsKeyDown(Keys.A))
            _moveDirection += right;
        if (currentKeyboardState.IsKeyDown(Keys.D))
            _moveDirection -= right;
        if (currentKeyboardState.IsKeyDown(Keys.W))
            _moveDirection += forward;
        if (currentKeyboardState.IsKeyDown(Keys.S))
            _moveDirection -= forward;

        // Normalize direction if we're moving
        if (_moveDirection.LengthSquared() > 0)
        {
            if (_walkSound.State != SoundState.Playing && !IsFalling && !IsJumping)
            {
                _walkSound.Play();
                PlayAnimation("walk");
            }
            _moveDirection.Normalize();
        }
        else
        {
            _walkSound.Stop();
            if (!IsJumping)
            {

                PlayAnimation("idle");
            }
        }
            

        // Update the target rotation angle when moving
        if (_moveDirection != Vector3.Zero)
        {
            _targetRotationAngle = (float)Math.Atan2(_moveDirection.X, _moveDirection.Z);
        }

        // Smoothly interpolate between current and target rotation
        // Calculate the shortest path to the target angle using MathHelper.WrapAngle
        float angleDifference = MathHelper.WrapAngle(_targetRotationAngle - _currentRotationAngle);

        // Apply rotation based on rotation speed and delta time
        _currentRotationAngle += angleDifference * GameConstants.PLAYER_ROTATION_SPEED * deltaTime;

        // Ensure current angle stays within proper range
        _currentRotationAngle = MathHelper.WrapAngle(_currentRotationAngle);

        // Apply the smoothed rotation
        Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, _currentRotationAngle);

        // Apply movement to velocity (maintaining Y velocity for jumps/gravity)
        _velocity.X = _moveDirection.X * GameConstants.PLAYER_MOVE_SPEED;
        _velocity.Z = _moveDirection.Z * GameConstants.PLAYER_MOVE_SPEED;

        // Apply the jumps.
        if (jump && _jumpCount < _maxJumps)
        {
            // Instant velocity change on jump including
            // any existing upward forces we have on us.
            //
            // This means hitting your second jump at your peak
            // upward velocity gives you an even bigger jump.
            //
            _velocity.Y = Math.Max(_velocity.Y, 0) + GameConstants.PLAYER_JUMP_FORCE;
            _landVelocity = 0.0f;

            IsJumping = true;
            _jumpCount++;
            PlayAnimation("jump");

            // By using the pooled SoundEffect.Play
            // we can play overlapping jump sounds.
            _jumpSound.Play();
        }

        // Platformer physics isn't realistic.
        //
        // You want a weaker gravity while you jump than when falling.
        //
        // This is done after the jump to ensure we apply the correct
        // gravity this frame.
        //
        if (_velocity.Y > 0 && jumpHeld)
            _velocity.Y += GameConstants.PLAYER_JUMP_GRAVITY * deltaTime;
        else
        {
            _velocity.Y += GameConstants.PLAYER_FALL_GRAVITY * deltaTime;

            // Keep the player from falling too fast.
            _velocity.Y = MathHelper.Max(-GameConstants.PLAYER_MAX_FALL_SPEED, _velocity.Y);
        }

        // Apply velocity to position with time-based movement.
        Position += _velocity * deltaTime;

        // Store current keyboard state for next frame
        _previousKeyboardState = currentKeyboardState;
        _previousGamePadState = gamePadState;

        // Animate the player scale.
        {
            if (_landVelocity > 0.0f)
            {
                // If we've landed the use the land velocity to
                // squash us a bit to take the impact.

                // When we jump or fall apply a little squash and stretch to the player mesh.
                var land = MathHelper.Clamp(_landVelocity / GameConstants.PLAYER_MAX_FALL_SPEED, 0.0f, 1.0f);
                var scaleXZ = 1.0f + (land * 0.55f);
                var scaleY = 1.0f - (land * 0.35f);
                _scaleAnimation = new Vector3(scaleXZ, scaleY, scaleXZ);

                // Relax it over time.
                _landVelocity = Math.Max(0.0f, _landVelocity - (GameConstants.PLAYER_MAX_FALL_SPEED * 2.0f * deltaTime));
            }
            else
            {
                if (Math.Abs(_velocity.Y) < 0.1f)
                    _scaleAnimation = Vector3.One;
                else
                {
                    // When we jump or fall apply a little squash and stretch to the player mesh.
                    var jumpOrFall = MathHelper.Clamp(-_velocity.Y / GameConstants.PLAYER_JUMP_FORCE, -1.0f, 1.0f);
                    var scaleXZ = 1.0f + ((1.0f - jumpOrFall) * 0.1f);
                    var scaleY = 1.0f + (jumpOrFall * 0.15f);
                    _scaleAnimation = new Vector3(scaleXZ, scaleY, scaleXZ);
                }
            }

            Scale = Vector3.Lerp(Scale, _scaleAnimation, 1f - (float)Math.Exp(10.0f * -deltaTime));
        }

        base.Update(gameTime);
    }

    public void DrawShadow(GraphicsDevice graphicsDevice, Camera camera)
    {
        if (!IsJumping)
            return;

        // Calculate shadow size based on distance
        // The closer to the surface, the smaller the shadow, we want is to disappear when its close to the surface
        float distanceFactor = MathHelper.Clamp(1.0f - (_currentShadowDistance / _maxShadowDistance), 0.0f, 1.0f);
        float dynamicShadowSize = MathHelper.Lerp(_minShadowSize, _maxShadowSize, distanceFactor);

        // Create a world matrix that positions the shadow quad where the ray hit
        // Add a small Y offset to prevent Z-fighting with the platform
        Vector3 shadowPos = _shadowPosition + new Vector3(0, 0.05f, 0);
        Matrix shadowWorld = Matrix.CreateScale(dynamicShadowSize) * Matrix.CreateTranslation(shadowPos);

        // Set up the effect with camera matrices
        _shadowEffect.World = shadowWorld;
        _shadowEffect.View = camera.ViewMatrix;
        _shadowEffect.Projection = camera.ProjectionMatrix;

        // Set render states for transparency
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        // Apply the effect for rendering
        foreach (EffectPass pass in _shadowEffect.CurrentTechnique.Passes)
        {
            pass.Apply();

            // Set the vertex and index buffers
            graphicsDevice.SetVertexBuffer(_shadowVertexBuffer);
            graphicsDevice.Indices = _shadowIndexBuffer;

            // Draw the quad
            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                2);  // 2 triangles in the quad
        }
    }

    public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        // Draw the shadow (needs to be drawn before the model for proper transparency)
        DrawShadow(graphicsDevice, camera);

        // Draw the model
        base.Draw(graphicsDevice, spriteBatch, camera);
    }
}