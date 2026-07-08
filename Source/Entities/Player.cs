// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

/// <summary>
/// A player entity that can move, jump, and interact with the environment.
/// This is probably one of the most complex classes in the game with the
/// exception of the collision system.
/// It handles input, movement, jumping, physics, collision response,
/// animation, sound effects, and player state (like score and whether the player is dead).
/// </summary>
public class Player : AnimatedEntity
{
    public bool InputEnabled = true;

    public bool IsJumping = false;
    public bool IsGrounded = false;

    public int Score = 0;
    public Vector3 Forward { get; set; } = new Vector3(0, 0, -1); // Default forward is negative Z

    public bool IsMoving
    {
        get
        {
            return _moveDirection != Vector3.Zero;
        }
    }

    private Platform _platform;
    private readonly List<Contact> _contacts = new List<Contact>();

    private Vector3 _velocity;
    private Vector3 _physicsForce;
    private int _jumpCount = 0;
    private int _maxJumps = 2; // Allow double jump
    private bool _forceJump = false;
    private Vector3 _moveDirection = Vector3.Zero;
    private float _targetRotationAngle = 1.5f; // The angle we want to rotate towards
    private float _currentRotationAngle = 1.5f; // Current rotation angle, start facing the player
    private SoundEffect _jumpSound;
    private SoundEffect _landSound;
    private SoundEffectInstance _walkSound;
    private SoundEffect _playerDied;

    // Used to do effects when the player lands from a fall/jump.
    private float _landVelocity = 0.0f;

    // Used to animate the player scale during jumps, falls, and landings.
    private Vector3 _scaleAnimation = Vector3.One;
    private Vector3 _squish = Vector3.Zero;

    private GraphicsDevice _graphicsDevice;

    public Player(GraphicsDevice graphicsDevice, Model model, ContentManager contentManager) : base(model, contentManager)
    {
        _collisionMesh.GenerateFromCylinder(new Vector3(0, 40, 0), 30, 80, 6);

        Position = new Vector3(0, 0, 0);
        Scale = new Vector3(1, 1, 1);
        Rotation = Quaternion.Identity;
        _graphicsDevice = graphicsDevice;
        _squish = Vector3.Zero;

        CastPlacementShadow = true;
    }

    override protected void LoadContent()
    {
        // Load any additional content here
        base.LoadContent();
        _jumpSound = Content.Load<SoundEffect>("Sounds/jump");
        _landSound = Content.Load<SoundEffect>("Sounds/land");
        _walkSound = Content.Load<SoundEffect>("Sounds/walking").CreateInstance();
        _playerDied = Content.Load<SoundEffect>("Sounds/burst");
    }

    public override bool CheckCollision(Entity other)
    {
        // No collision once dead.
        if (IsDead)
            return false;

        bool collision = base.CheckCollision(other, out var contact);
        if (collision)
        {
            if (!other.IsBlockingMovement)
            {
                // If the other entity is not blocking movement, we can ignore the collision
                return false;
            }

            // TODO: Sometimes this isn't normalized which is weird.            
            contact.normal = Vector3.Normalize(contact.normal);

            _contacts.Add(contact);

            var resolveDirection = contact.normal * contact.depth;
            var upwardPenetration = Vector3.Dot(contact.normal, Vector3.Up);

            // If we're resolving upward, we're standing on something
            if (upwardPenetration > 0.75f)
            {
                if (IsJumping)
                {
                    _landSound.Play();
                    _landVelocity = Math.Clamp(-(_velocity.Y + _physicsForce.Y), 0.0f, GameConstants.PLAYER_MAX_FALL_SPEED);
                    IsJumping = false;
                }

                _jumpCount = 0; // Reset jump count when landing
                IsGrounded = true;
                _platform = other as Platform;
                _velocity.Y = 0.0f;
                _physicsForce.Y = 0.0f;

                // This keeps the player from sliding when on
                // the ground from the collision resolve force.
                if (MathF.Abs(_velocity.X) < 0.1f)
                    resolveDirection.X = 0;
                if (MathF.Abs(_velocity.Z) < 0.1f)
                    resolveDirection.Z = 0;
            }

            // Apply the resolution vector with a small buffer to prevent sticking
            Position += resolveDirection * 1.01f;

            // Remove existing velocity into the contact.
            float intoSurface = Vector3.Dot(_velocity + _physicsForce, contact.normal);
            if (intoSurface < 0)
            {
                _velocity -= contact.normal * Vector3.Dot(_velocity, contact.normal);
                _physicsForce -= contact.normal * Vector3.Dot(_physicsForce, contact.normal);
            }

            // Update the entity's world matrix and bounding box immediately to prevent
            // further collision detection issues in the same frame
            WorldMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);
        }

        return collision;
    }

    /// <summary>
    /// This is used by the code which triggers a jump on the player when
    /// they reach the goal.
    /// </summary>
    /// <param name="force"></param>
    public void AddForce(Vector3 force)
    {
        _physicsForce += force;

        if (_physicsForce.Y > 0)
        {
            _velocity = Vector3.Zero;
            IsGrounded = false;
            PlayAnimation("jump");
            IsJumping = true;
        }
    }

    /// <summary>
    /// Reset the collision state before doing collision checks.
    /// </summary>
    public void PreCollision()
    {
        IsGrounded = false;
        _platform = null;
        _contacts.Clear();
    }

    public override bool Dead()
    {
        var dead = IsDead;
        if (dead)
            return true;

        // If we're too squished then we're dead!
        if (_squish.X > 0.35f ||
            _squish.Y > 0.35f ||
            _squish.Z > 0.35f)
        {
            IsDead = true;
            return true;
        }
       
        return base.Dead();
    }

    public void Die()
    {
        // dont play the dead sound if we are not accepting inputs.
        if (InputEnabled)
            _playerDied.Play();
    }

    public void Jump()
    {
        _forceJump = true;
    }

    public void LookAt(GameTime gameTime, Vector3 target)
    {
        // Calculate the direction to the target
        Vector3 direction = target - Position;
        if (direction.LengthSquared() < 0.0001f)
            return; // Avoid division by zero

        // Normalize the direction vector
        direction.Normalize();

        // Calculate the angle to look at the target
        _targetRotationAngle = (float)Math.Atan2(direction.X, direction.Z);

        // Smoothly interpolate towards the target rotation angle
        float angleDifference = MathHelper.WrapAngle(_targetRotationAngle - _currentRotationAngle);
        _currentRotationAngle += angleDifference * GameConstants.PLAYER_ROTATION_SPEED * (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update the rotation quaternion
        Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, _currentRotationAngle);
    }

    public override void Update(GameTime gameTime)
    {
        // No updates once dead!
        if (IsDead)
            return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;


        Vector3 right = Vector3.Cross(Vector3.Up, Forward);
        right = Vector3.Normalize(right);

        // Normalize Forward to ensure it's a unit vector
        Vector3 forward = Vector3.Normalize(Forward);

        // Process movement inputs and convert to world-relative movement
        _moveDirection = Vector3.Zero;
        var jump = _forceJump;
        var jumpHeld = false;
        _forceJump = false;

        // Get current input state.
        if (InputEnabled && deltaTime > 0f)
        {
            jump = InputState.IsButtonPressed(Buttons.A) || InputState.IsKeyPressed(Keys.Space);

            jumpHeld = InputState.IsButtonHeld(Buttons.A) || InputState.IsKeyHeld(Keys.Space);

            var thumbstickLeft = InputState.GamepadState.ThumbSticks.Left;
            if (thumbstickLeft.LengthSquared() > 0)
            {
                // Movement along the forward and right vector
                _moveDirection -= right * thumbstickLeft.X;
                _moveDirection += forward * thumbstickLeft.Y;
            }

            if (InputState.IsKeyDown(Keys.A))
                _moveDirection += right;
            if (InputState.IsKeyDown(Keys.D))
                _moveDirection -= right;
            if (InputState.IsKeyDown(Keys.W))
                _moveDirection += forward;
            if (InputState.IsKeyDown(Keys.S))
                _moveDirection -= forward;
        }

        // Normalize direction if we're moving
        if (_moveDirection.LengthSquared() > 0f)
        {
            if (IsGrounded)
            {
                if (_walkSound.State != SoundState.Playing)
                    _walkSound.Play();

                PlayAnimation("walk");
            }
            else
            {
                _walkSound.Stop();
            }

            _moveDirection.Normalize();
        }
        else
        {
            _walkSound.Stop();

            if (IsGrounded)
                PlayAnimation("idle");
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
        var desiredVelocity = _moveDirection * GameConstants.PLAYER_MOVE_SPEED;
        if (IsGrounded)
        {
            _velocity.X = desiredVelocity.X;
            _velocity.Z = desiredVelocity.Z;

            if (_platform != null)
                _velocity += _platform.Velocity;

            _physicsForce = Vector3.Zero;
        }
        else
        {
            float AirSteeringAmount = 5;
            if (desiredVelocity.X != 0 || desiredVelocity.Z != 0)
            {
                _velocity.X = MathHelper.Lerp(_velocity.X, desiredVelocity.X, AirSteeringAmount * deltaTime);
                _velocity.Z = MathHelper.Lerp(_velocity.Z, desiredVelocity.Z, AirSteeringAmount * deltaTime);
            }
        }

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

            // A jump reduces the physics force by 50%.
            _physicsForce *= 0.5f;

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
        }

        var totalVelocity = _velocity + _physicsForce;

        // Apply velocity to position with time-based movement.
        Position += totalVelocity * deltaTime;

        // Remove out the physics forces over time.
        float PhysicsDrag = 0.75f;
        _physicsForce = Vector3.Lerp(_physicsForce, Vector3.Zero, PhysicsDrag * deltaTime);

        // Do the squish check... we look at the distance between
        // the contact points and the center of the bounds to get
        // our squish factor.
        {
            _squish = Vector3.Zero;

            // We need at least 2 contacts.
            if (_contacts.Count > 1)
            {
                // TODO: Maybe this needs to eventually be in local space?

                var axes = new Vector3[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };
                var min = new Vector3(float.PositiveInfinity);
                var max = new Vector3(float.NegativeInfinity);
                var extents = BoundingBox.Max - BoundingBox.Min;

                foreach (var contact in _contacts)
                {
                    for (var axis = 0; axis < 3; axis++)
                    {
                        float dot = Vector3.Dot(contact.normal, axes[axis]);
                        float proj = Vector3.Dot(contact.point, axes[axis]);

                        if (dot > 0.5f)
                        {
                            if (proj > max.GetAxis(axis))
                                MathHelpers.SetAxis(ref max, axis, proj);
                        }
                        else if (dot < -0.5f)
                        {
                            if (proj < min.GetAxis(axis))
                                MathHelpers.SetAxis(ref min, axis, proj);
                        }
                    }
                }

                var squish = Vector3.Zero;

                for (int axis = 0; axis < 3; axis++)
                {
                    float axisExtent = extents.GetAxis(axis);

                    var mmin = min.GetAxis(axis);
                    var mmax = max.GetAxis(axis);

                    if (mmin < float.PositiveInfinity &&
                            mmax > float.NegativeInfinity)
                    {
                        var span = Math.Abs(mmax - mmin);
                        var compression = 1.0f - (span / axisExtent);
                        compression = Math.Clamp(compression, 0.0f, 1.0f);
                        MathHelpers.SetAxis(ref squish, axis, compression);
                    }
                }

                if (squish.Y > 0)
                {
                    // When squished vertically get fatter!
                    squish.X = squish.Y * -0.25f;
                    squish.Z = squish.Y * -0.25f;
                }

                _squish = squish;
            }
        }

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
                var velocity = _velocity + _physicsForce;
                if (Math.Abs(velocity.Y) < 0.1f)
                    _scaleAnimation = Vector3.One;
                else
                {
                    // When we jump or fall apply a little squash and stretch to the player mesh.
                    var jumpOrFall = MathHelper.Clamp(-velocity.Y / GameConstants.PLAYER_JUMP_FORCE, -1.0f, 1.0f);
                    var scaleXZ = 1.0f + ((1.0f - jumpOrFall) * 0.1f);
                    var scaleY = 1.0f + (jumpOrFall * 0.15f);
                    _scaleAnimation = new Vector3(scaleXZ, scaleY, scaleXZ);
                }
            }

            Scale = Vector3.Lerp(Scale, _scaleAnimation, 1f - (float)Math.Exp(10.0f * -deltaTime));
            Scale *= Vector3.One - _squish;
        }

        base.Update(gameTime);
    }

    public override void DrawBillboards(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Camera camera)
    {
        base.DrawBillboards(graphicsDevice, spriteBatch, camera);

        foreach (var contact in _contacts)
        {
            spriteBatch.DrawSquare(contact.point, 10, camera.ProjectionMatrix, camera.ViewMatrix, Color.Red);
        }
    }
}
