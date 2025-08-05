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
    private Queue<Entity> _entitiesToRemove = new Queue<Entity>();

    private List<Entity> _drawList = new List<Entity>();

    public Vector3 LightPosition = new Vector3(0, 10, 0);

    private GraphicsDevice _graphicsDevice;

    private Camera _camera;
    private Player _player;
    private Dust _dust;
    private Color _skyColor = new Color(0.752941f, 0.776471f, 0.827451f);

    public List<Entity> Entities => _entities;
    public Camera Camera => _camera;
    public Vector3 LightDirection => Vector3.Normalize(LightPosition);
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

    public bool AcceptInput { get; set; } = true;

    public bool HasPlayer { get; set; } = true;

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
    }

    public void Update(GameTime gameTime)
    {
        if (!_player.IsDead)
        {
            _player.InputEnabled = AcceptInput;

            // TODO: Should the player really update before the world?
            _player.Forward = _camera.ForwardDirection;
            _player.Update(gameTime);

            // TODO: Maybe all entities should have this callback?
            _player.PreCollision();
        }

        _dust.Update(gameTime);

        foreach (var entity in _entities)
        {
            entity.Update(gameTime);
            entity.CheckCollision(_player);
            _player.CheckCollision(entity);
            if (entity.Dead())
            {
                _entitiesToRemove.Enqueue(entity);
            }
        }
        while (_entitiesToRemove.Count > 0)
        {
            var entity = _entitiesToRemove.Dequeue();
            _entities.Remove(entity);
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

    public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ShadowProcessor shadowProcessor, PostProcessor postProcessor, SpriteBatch spriteBatch)
    {
        DrawShadownMaps(shadowProcessor);
        postProcessor.BeginScene();
        graphicsDevice.Clear(_skyColor);
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
        spriteBatch.End();
    }

    private void DrawShadownMaps(ShadowProcessor shadowProcessor)
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

    private void DrawScene(ShadowProcessor shadowProcessor, SpriteBatch spriteBatch)
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

            shadowProcessor.DrawModelWithShadow(entity, _camera, false);
        }

        // Now draw the transparent objects reversing the list furthest to closest.
        _drawList.Reverse();
        foreach (var entity in _drawList)
        {
            if (entity.Model is null)
                continue;

            shadowProcessor.DrawModelWithShadow(entity, _camera, true);
        }

        _dust.Draw(_graphicsDevice, spriteBatch, _camera);
    }
}