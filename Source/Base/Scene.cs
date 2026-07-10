// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Represents a scene in the game, containing entities, camera, and lighting information.
/// The scene can be updated and drawn, and it manages a list of entities that are part of the scene.
/// It also handles the removal of entities that are marked for deletion.
/// </summary>
public class Scene
{
    private List<Entity> _entities = new List<Entity>();

    private List<Entity> _drawList = new List<Entity>();

    public Vector3 LightPosition = new Vector3(0, 10, 0);

    private GraphicsDevice _graphicsDevice;

    private Camera _camera;
    private Player _player;
    private Dust _dust;
    private Sparkles _sparkles;

    public List<Entity> Entities => _entities;
    public Camera Camera => _camera;
    public Color LightColor { get; set; } = Color.White;
    public float LightIntensity { get; set; } = 1.0f;
    public Player Player => _player;
    public Dust Dust => _dust;
    public Goal Goal
    {
        get
        {
            return _entities.Find(e => e is Goal) as Goal;
        }
    }
    public float ResetTimer { get; set; } = 0.0f;
    public float CelebrationTimer { get; set; } = 0.0f;
    public float CelebrationJumpInterval { get; set; } = 0.8f;

    public bool AcceptInput { get; set; } = true;

    public bool HasPlayer { get; set; } = true;

    public Color SkyColor { get; set; } = GameConstants.DEFAULT_BACKGROUND_COLOR;

    public Scene(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _graphicsDevice = graphicsDevice;

        // Initialize the scene with a camera and player
        // The camera will be used to view the scene and the player will represent the main character
        // in the game. The content manager is used to load assets for the entities in the scene.
        _camera = new Camera(graphicsDevice);
        if (!HasPlayer)
            return;

        _player = new Player(graphicsDevice, contentManager.Load<Model>("Models/character"), contentManager)
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity
        };
        _dust = new Dust(contentManager.Load<Model>("Models/dust"), contentManager);
        _sparkles = new Sparkles(contentManager);
    }

    public void StartCelebration(GameTime gameTime)
    {
        if (CelebrationTimer > 0)
        {
            CelebrationTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            CelebrationJumpInterval -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_player.IsGrounded && CelebrationJumpInterval <= 0)
            {
                _player.Jump(); // Make the player jump to add to the celebration effect
                CelebrationJumpInterval = 1f; // Reset the jump interval
            }
            _player.LookAt(gameTime, _camera.Position);
            return; // Avoid starting celebration if already in progress
        }
        CelebrationTimer = GameConstants.DEFAULT_CELEBRATION_TIMER; // Set a timer for the celebration duration
        _sparkles.Clear(); // Clear existing sparkles
        // Create a burst of sparkles at the player's position
        // This will create a celebratory effect when the goal is reached
        if (Goal != null)
        {
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                float radius = 0.8f + random.NextSingle() * 50f; // Radius around the coin
                float angle = (float)random.NextDouble() * MathHelper.TwoPi;
                float height = (float)random.NextDouble() * 100.0f - 5f;

                Vector3 offset = new Vector3(
                    (float)Math.Cos(angle) * radius,
                    height,
                    (float)Math.Sin(angle) * radius
                );
                _sparkles.CreateSparkle(Goal.Position + offset, new Sparkles.SparkleConfig
                {
                    Scale = 0.15f + (float)random.NextDouble() * 0.1f,
                    Rotation = (float)random.NextDouble() * MathHelper.TwoPi,
                    Type = Sparkles.SparkleType.Celebration,
                    Lifetime = 0.05f + (float)random.NextDouble(),  // Live for 0.5 to 1.5 seconds
                    MaxLifetime = 0.05f + (float)random.NextDouble(),
                    VerticalSpread = 150f,
                    Gravity = new Vector3(0, -50f, 0)
                });
            }
        }
        if (_player != null)
        {
            AcceptInput = false;
            _player.Jump();
        }
    }

    public void Update(GameTime gameTime)
    {
        if (!_player.IsDead)
        {
            _player.InputEnabled = AcceptInput;

            // Pick a good forward vector for player movement based
            // on the camera and knowing the player walks on the ground.
            var forward = _camera.ForwardDirection;
            forward.Y = 0f;
            if (forward.LengthSquared() > 1e-6f)
                forward.Normalize();
            else
            {
                // The camera is looking straight up/down
                // so we use the screen-up vector instead.
                var camWorld = Matrix.Invert(_camera.ViewMatrix);
                forward = camWorld.Up;
                forward.Y = 0f;
                forward.Normalize();
            }
            _player.Forward = forward;

            _player.Update(gameTime);

            // TODO: Maybe all entities should have this callback?
            _player.PreCollision();
        }

        _dust.Update(gameTime);
        _sparkles.Update(gameTime);

        // Update the world entities to let them move and possibly die.
        for (int i = 0; i < _entities.Count; i++)
        {
            var entity = _entities[i];
            entity.Update(gameTime);
            if (entity.Dead())
            {
                _entities.Remove(entity);
                --i;
            }
        }

        // Update the player collision.
        for (int i = 0; i < _entities.Count; i++)
        {
            var entity = _entities[i];
            _player.CheckCollision(entity);
            if (entity.Dead())
            {
                _entities.Remove(entity);
                --i;
            }
        }

        // Update the world collision.
        for (int i = 0; i < _entities.Count; i++)
        {
            var entity = _entities[i];
            entity.CheckCollision(_player);
            if (entity.Dead())
            {
                _entities.Remove(entity);
                --i;
            }
        }

        if (Goal != null && Goal.GoalReached)
        {
            StartCelebration(gameTime);
        }

        // If not dead.
        if (!_player.IsDead)
        {
            // Check to see if the player has died.
            if (_player.Dead())
            {
                _player.Die();
                ResetTimer = 1.5f;
            }
            else
            {
                if (_player.IsMoving && _player.IsGrounded)
                {
                    _dust.AddDust(gameTime, _player.Position);
                }

                if (!AcceptInput)
                    _camera.UpdateViewMatrix();
                else
                {
                    _camera.Target = _player.Position;
                    _camera.Update(gameTime);
                }
            }
        }
    }

    public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, SceneRenderer shadowProcessor, PostProcessor postProcessor, SpriteBatch spriteBatch)
    {
        shadowProcessor.SunColor = LightColor.ToVector3();
        shadowProcessor.SunIntensity = LightIntensity;
        DrawShadownMaps(shadowProcessor);
        postProcessor.BeginScene();
        graphicsDevice.Clear(SkyColor);
        DrawScene(shadowProcessor, spriteBatch);
        DrawBillboards(spriteBatch);
        postProcessor.EndScene();
    }

    public void DrawCollisionMeshs(SpriteBatch spriteBatch)
    {
        foreach (var entity in _entities)
        {
            entity.Draw(_graphicsDevice, spriteBatch, _camera);
        }
        if (HasPlayer)
            _player.Draw(_graphicsDevice, spriteBatch, _camera);
    }

    public void DrawBillboards(SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwise);
        foreach (var entity in _entities)
        {
            entity.DrawBillboards(_graphicsDevice, spriteBatch, _camera);
        }
        _sparkles.DrawBillboards(_graphicsDevice, spriteBatch, _camera);
        spriteBatch.End();
    }

    private void DrawShadownMaps(SceneRenderer shadowProcessor)
    {
        _drawList.Clear();
        _drawList.AddRange(_entities);
        if (!_player.IsDead && HasPlayer)
            _drawList.Add(_player);
        _drawList.Add(_dust);

        // Draw closest to the camera first.
        var cameraPos = shadowProcessor.LightPosition0;
        _drawList.Sort((a, b) =>
        {
            var dista = Vector3.DistanceSquared(a.Position, cameraPos);
            var distb = Vector3.DistanceSquared(b.Position, cameraPos);
            return dista.CompareTo(distb);
        });

        shadowProcessor.BeginShadowMapPass(0);
        foreach (var entity in _drawList)
            if (!entity.CastPlacementShadow)
                shadowProcessor.DrawEntityToShadowMap(entity);
        shadowProcessor.BeginShadowMapPass(1);
        foreach (var entity in _drawList)
            if (entity.CastPlacementShadow)
                shadowProcessor.DrawEntityToShadowMap(entity);

        shadowProcessor.EndShadowMapPass();
    }

    private void DrawScene(SceneRenderer sceneRenderer, SpriteBatch spriteBatch)
    {
        _drawList.Clear();
        _drawList.AddRange(_entities);
        if (!_player.IsDead && HasPlayer)
            _drawList.Add(_player);

        // Draw closest to the camera first.
        var cameraPos = _camera.Position;
        _drawList.Sort((a, b) =>
        {
            var dista = Vector3.DistanceSquared(a.Position, cameraPos);
            var distb = Vector3.DistanceSquared(b.Position, cameraPos);
            return dista.CompareTo(distb);
        });

        // First draw the opaque pass.
        foreach (var entity in _drawList)
        {
            if (entity.Model is null)
                continue;

            sceneRenderer.DrawModelWithShadow(entity, _camera, false);
        }

        // Now draw the transparent objects reversing the list furthest to closest.
        _drawList.Reverse();
        foreach (var entity in _drawList)
        {
            if (entity.Model is null)
                continue;

            sceneRenderer.DrawModelWithShadow(entity, _camera, true);
        }

        _dust.Draw(_graphicsDevice, spriteBatch, _camera);
    }
}