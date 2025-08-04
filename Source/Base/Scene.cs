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
    private Color _skyColor = Color.CornflowerBlue;

    public List<Entity> Entities => _entities;
    public Camera Camera => _camera;
    public Vector3 LightDirection => Vector3.Normalize(LightPosition);

    public Scene(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        _graphicsDevice = graphicsDevice;
        // Initialize the scene with a camera and player
        // The camera will be used to view the scene and the player will represent the main character
        // in the game. The content manager is used to load assets for the entities in the scene.
        _camera = new Camera(graphicsDevice);
        _player = new Player(null, null, contentManager);
        _dust = new Dust(contentManager.Load<Model>("Models/dust"), contentManager);
    }

    public void Update(GameTime gameTime)
    {
        _camera.Update(gameTime);
        foreach (var entity in _entities)
        {
            entity.Update(gameTime);
        }

        // Remove entities marked for deletion
        while (_entitiesToRemove.Count > 0)
        {
            _entities.Remove(_entitiesToRemove.Dequeue());
        }
    }

    public void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ShadowProcessor shadowProcessor, PostProcessor postProcessor, SpriteBatch spriteBatch)
    {
        DrawShadownMaps(shadowProcessor);
        postProcessor.BeginScene();
        graphicsDevice.Clear(_skyColor);
        DrawScene(shadowProcessor, spriteBatch);
        postProcessor.EndScene();
    }

    private void DrawShadownMaps(ShadowProcessor shadowProcessor)
    {
        _drawList.Clear();
        _drawList.AddRange(_entities);
        _drawList.Add(_player);
        _drawList.Add(_dust);

        // Draw closest to the camera first.
        shadowProcessor.LightDirection = Vector3.Normalize(LightPosition);
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